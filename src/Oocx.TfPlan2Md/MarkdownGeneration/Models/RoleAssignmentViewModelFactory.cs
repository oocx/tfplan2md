using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds <see cref="RoleAssignmentViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md
/// </summary>
internal static class RoleAssignmentViewModelFactory
{
    private static readonly string[] DesiredOrder =
    [
        "scope",
        "role_definition_id",
        "principal_id",
        "principal_type",
        "name",
        "description",
        "condition",
        "skip_service_principal_aad_check",
        "delegated_managed_identity_resource_id"
    ];

    /// <summary>
    /// Creates a view model for the provided role assignment change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="action">The Terraform action string.</param>
    /// <param name="attributeChanges">The attribute changes for this resource.</param>
    /// <param name="principalMapper">Mapper for principal name resolution.</param>
    /// <returns>Populated <see cref="RoleAssignmentViewModel"/>.</returns>
    public static RoleAssignmentViewModel Build(
        ResourceChange change,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges,
        IPrincipalMapper principalMapper)
    {
        var beforeState = change.Change.Before as JsonElement?;
        var afterState = change.Change.After as JsonElement?;

        var activeState = action == "delete" ? beforeState : afterState;
        var description = ExtractDescription(activeState);

        var beforeScope = GetScopeInfo(beforeState);
        var afterScope = GetScopeInfo(afterState);
        var beforeRole = GetRoleInfo(beforeState);
        var afterRole = GetRoleInfo(afterState);
        var beforePrincipal = GetPrincipalInfo(beforeState, principalMapper);
        var afterPrincipal = GetPrincipalInfo(afterState, principalMapper);

        var activeScope = action == "delete" ? beforeScope : afterScope;
        var activeRole = action == "delete" ? beforeRole : afterRole;
        var activePrincipal = action == "delete" ? beforePrincipal : afterPrincipal;

        var summaryText = BuildSummaryText(action, activeScope, activeRole, activePrincipal);

        var allAttributes = attributeChanges.Count > 0
            ? attributeChanges
            : BuildDefaultAttributes();

        var smallAttrs = new List<RoleAssignmentAttributeViewModel>();
        var largeAttrs = new List<RoleAssignmentAttributeViewModel>();

        foreach (var attr in allAttributes)
        {
            var beforeValue = FormatRoleValue(attr.Name, beforeState, beforeScope, beforeRole, beforePrincipal);
            var afterValue = FormatRoleValue(attr.Name, afterState, afterScope, afterRole, afterPrincipal);

            var attrViewModel = new RoleAssignmentAttributeViewModel
            {
                Name = attr.Name,
                Before = beforeValue,
                After = afterValue
            };

            if (attr.IsLarge)
            {
                largeAttrs.Add(attrViewModel);
            }
            else
            {
                smallAttrs.Add(attrViewModel);
            }
        }

        var orderedSmall = OrderAttributes(smallAttrs);
        var orderedLarge = OrderAttributes(largeAttrs);

        return new RoleAssignmentViewModel
        {
            ResourceName = ExtractResourceName(change.Address),
            Description = description,
            SummaryText = summaryText,
            SmallAttributes = orderedSmall,
            LargeAttributes = orderedLarge
        };
    }

    /// <summary>
    /// Extracts the resource name from the full address (last component after the last dot).
    /// </summary>
    private static string ExtractResourceName(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return address;
        }

        var lastDot = address.LastIndexOf('.');
        return lastDot >= 0 && lastDot < address.Length - 1
            ? address[(lastDot + 1)..]
            : address;
    }

    /// <summary>
    /// Extracts the description field from the state.
    /// </summary>
    private static string? ExtractDescription(JsonElement? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty("description", out var desc) || desc.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = desc.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>
    /// Builds the summary text combining principal, role, and scope information.
    /// </summary>
    private static string BuildSummaryText(
        string action,
        Azure.ScopeInfo scope,
        RoleInfo role,
        PrincipalInfo principal)
    {
        var scopeSummary = scope.SummaryLabel + ScribanHelpers.FormatCodeSummary(scope.SummaryName);
        var roleSummary = $"<code>🛡️ {ScribanHelpers.EscapeMarkdown(role.Name)}</code>";
        var principalIcon = principal.Type switch
        {
            "User" => "👤 ",
            "Group" => "👥 ",
            "ServicePrincipal" => "💻 ",
            _ => string.Empty
        };
        var principalSummary = $"<code>{principalIcon}{ScribanHelpers.EscapeMarkdown(principal.Name)}</code>";

        return action switch
        {
            "replace" => $"recreate as {principalSummary} → {roleSummary} on {scopeSummary}",
            "delete" => $"remove {roleSummary} on {scopeSummary} from {principalSummary}",
            _ => $"{principalSummary} → {roleSummary} on {scopeSummary}"
        };
    }

    /// <summary>
    /// Formats a role assignment attribute value for display.
    /// </summary>
    private static string? FormatRoleValue(
        string attrName,
        JsonElement? state,
        ScopeInfo scope,
        RoleInfo role,
        PrincipalInfo principal)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        switch (attrName)
        {
            case "scope":
                if (!string.IsNullOrEmpty(scope.ResourceGroup) && !string.IsNullOrEmpty(scope.SubscriptionId))
                {
                    return $"`{ScribanHelpers.EscapeMarkdown(scope.ResourceGroup)}` in subscription `{ScribanHelpers.EscapeMarkdown(scope.SubscriptionId)}`";
                }
                if (!string.IsNullOrEmpty(scope.SummaryName))
                {
                    return scope.SummaryLabel + " " + $"`{ScribanHelpers.EscapeMarkdown(scope.SummaryName)}`";
                }
                if (!string.IsNullOrEmpty(scope.Details))
                {
                    return ScribanHelpers.EscapeMarkdown(scope.Details);
                }
                return null;

            case "role_definition_id":
                var roleName = !string.IsNullOrEmpty(role.Name)
                    ? ScribanHelpers.FormatAttributeValueTable("role_definition_name", role.Name, null)
                    : string.Empty;
                var roleId = !string.IsNullOrEmpty(role.Id)
                    ? ScribanHelpers.FormatCodeTable(role.Id)
                    : string.Empty;

                if (string.IsNullOrEmpty(roleName) && string.IsNullOrEmpty(roleId))
                {
                    return null;
                }
                if (string.IsNullOrEmpty(roleId))
                {
                    return roleName;
                }
                if (string.IsNullOrEmpty(roleName))
                {
                    return roleId;
                }
                return $"{roleName} ({roleId})";

            case "principal_id":
                var principalIcon = principal.Type switch
                {
                    "User" => "👤",
                    "Group" => "👥",
                    "ServicePrincipal" => "💻",
                    _ => string.Empty
                };
                var nameValue = !string.IsNullOrEmpty(principal.Name)
                    ? ScribanHelpers.FormatCodeTable(principalIcon + " " + principal.Name)
                    : string.Empty;
                var principalType = !string.IsNullOrEmpty(principal.Type)
                    ? ScribanHelpers.FormatAttributeValueTable("principal_type", principal.Type, null)
                    : string.Empty;
                var idValue = !string.IsNullOrEmpty(principal.Id)
                    ? $"[{ScribanHelpers.FormatCodeTable(principal.Id)}]"
                    : string.Empty;

                var text = nameValue;
                if (!string.IsNullOrEmpty(principalType))
                {
                    text = text + " (" + principalType + ")";
                }
                if (!string.IsNullOrEmpty(idValue))
                {
                    text = text + " " + idValue;
                }
                return !string.IsNullOrEmpty(text) ? text : null;

            case "principal_type":
                if (element.TryGetProperty(attrName, out var propType) && propType.ValueKind == JsonValueKind.String)
                {
                    var value = propType.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        return ScribanHelpers.FormatAttributeValueTable("principal_type", value, null);
                    }
                }
                return null;

            case "role_definition_name":
                if (element.TryGetProperty(attrName, out var propRole) && propRole.ValueKind == JsonValueKind.String)
                {
                    var value = propRole.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        return ScribanHelpers.FormatAttributeValueTable("role_definition_name", value, null);
                    }
                }
                return null;

            default:
                if (element.TryGetProperty(attrName, out var prop))
                {
                    // Convert JsonElement to string with proper casing (lowercase for booleans)
                    var value = prop.ValueKind switch
                    {
                        JsonValueKind.String => prop.GetString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => prop.ToString()
                    };
                    if (!string.IsNullOrEmpty(value))
                    {
                        return ScribanHelpers.FormatAttributeValueTable(attrName, value, null);
                    }
                }
                return null;
        }
    }

    /// <summary>
    /// Builds default attributes when no attribute changes exist.
    /// </summary>
    private static AttributeChangeModel[] BuildDefaultAttributes()
    {
        return new[]
        {
            new AttributeChangeModel { Name = "scope", Before = null, After = null, IsSensitive = false, IsLarge = false },
            new AttributeChangeModel { Name = "role_definition_id", Before = null, After = null, IsSensitive = false, IsLarge = false },
            new AttributeChangeModel { Name = "principal_id", Before = null, After = null, IsSensitive = false, IsLarge = false },
            new AttributeChangeModel { Name = "principal_type", Before = null, After = null, IsSensitive = false, IsLarge = false },
            new AttributeChangeModel { Name = "name", Before = null, After = null, IsSensitive = false, IsLarge = false },
            new AttributeChangeModel { Name = "description", Before = null, After = null, IsSensitive = false, IsLarge = false }
        };
    }

    /// <summary>
    /// Orders attributes by the desired order, with unspecified attributes at the end.
    /// </summary>
    private static List<RoleAssignmentAttributeViewModel> OrderAttributes(List<RoleAssignmentAttributeViewModel> attributes)
    {
        var ordered = new List<RoleAssignmentAttributeViewModel>();
        var remaining = new HashSet<RoleAssignmentAttributeViewModel>(attributes);

        foreach (var desiredName in DesiredOrder)
        {
            var match = attributes.FirstOrDefault(a => string.Equals(a.Name, desiredName, StringComparison.Ordinal));
            if (match != null)
            {
                ordered.Add(match);
                remaining.Remove(match);
            }
        }

        ordered.AddRange(remaining);
        return ordered;
    }

    /// <summary>
    /// Extracts scope information from the state using Azure helpers.
    /// </summary>
    private static Azure.ScopeInfo GetScopeInfo(JsonElement? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Azure.ScopeInfo.Empty;
        }

        if (!element.TryGetProperty("scope", out var scopeProp) || scopeProp.ValueKind != JsonValueKind.String)
        {
            return Azure.ScopeInfo.Empty;
        }

        var scopeValue = scopeProp.GetString();
        return AzureScopeParser.Parse(scopeValue);
    }

    /// <summary>
    /// Extracts role information from the state.
    /// </summary>
    private static RoleInfo GetRoleInfo(JsonElement? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return new RoleInfo(string.Empty, string.Empty);
        }

        var roleDefId = element.TryGetProperty("role_definition_id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString() ?? string.Empty
            : string.Empty;

        var roleDefName = element.TryGetProperty("role_definition_name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
            ? nameProp.GetString() ?? string.Empty
            : string.Empty;

        // Use the same logic as the template helper to get consistent output
        var roleInfo = AzureRoleDefinitionMapper.GetRoleDefinition(roleDefId, roleDefName);

        return new RoleInfo(roleInfo.Name, roleInfo.Id);
    }

    /// <summary>
    /// Extracts principal information from the state.
    /// </summary>
    private static PrincipalInfo GetPrincipalInfo(JsonElement? state, IPrincipalMapper principalMapper)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return new PrincipalInfo(string.Empty, string.Empty, string.Empty);
        }

        var principalId = element.TryGetProperty("principal_id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString() ?? string.Empty
            : string.Empty;

        var principalType = element.TryGetProperty("principal_type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
            ? typeProp.GetString() ?? string.Empty
            : string.Empty;

        var principalName = !string.IsNullOrEmpty(principalId)
            ? principalMapper.GetName(principalId, principalType) ?? principalId
            : string.Empty;

        return new PrincipalInfo(principalName, principalId, principalType);
    }

    private sealed record RoleInfo(string Name, string Id);

    private sealed record PrincipalInfo(string Name, string Id, string Type);
}
