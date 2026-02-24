using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Scriban.Runtime;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Builds <see cref="RoleAssignmentViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
internal static class RoleAssignmentViewModelFactory
{
    /// <summary>
    /// Terraform action name for delete operations.
    /// </summary>
    private const string DeleteAction = "delete";

    /// <summary>
    /// Attribute name for scope values.
    /// </summary>
    private const string ScopeAttribute = "scope";

    /// <summary>
    /// Attribute name for role definition IDs.
    /// </summary>
    private const string RoleDefinitionIdAttribute = "role_definition_id";

    /// <summary>
    /// Attribute name for role definition names.
    /// </summary>
    private const string RoleDefinitionNameAttribute = "role_definition_name";

    /// <summary>
    /// Attribute name for principal IDs.
    /// </summary>
    private const string PrincipalIdAttribute = "principal_id";

    /// <summary>
    /// Attribute name for principal types.
    /// </summary>
    private const string PrincipalTypeAttribute = "principal_type";

    /// <summary>
    /// Principal type label for users.
    /// </summary>
    private const string UserPrincipalType = "User";

    /// <summary>
    /// Principal type label for groups.
    /// </summary>
    private const string GroupPrincipalType = "Group";

    /// <summary>
    /// Principal type label for service principals.
    /// </summary>
    private const string ServicePrincipalType = "ServicePrincipal";

    private static readonly string[] DesiredOrder =
    [
        ScopeAttribute,
        RoleDefinitionIdAttribute,
        PrincipalIdAttribute,
        PrincipalTypeAttribute,
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
    /// <param name="scopeFormatter">Optional formatter for enriched scope display.</param>
    /// <returns>Populated <see cref="RoleAssignmentViewModel"/>.</returns>
    public static RoleAssignmentViewModel Build(
        ResourceChange change,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges,
        IPrincipalMapper principalMapper,
        EnrichedAzureScopeFormatter? scopeFormatter = null)
    {
        var beforeState = change.Change.Before as JsonElement?;
        var afterState = change.Change.After as JsonElement?;

        var activeState = action == DeleteAction ? beforeState : afterState;
        var description = ExtractDescription(activeState);

        var beforeScope = GetScopeInfo(beforeState);
        var afterScope = GetScopeInfo(afterState);
        var beforeRole = GetRoleInfo(beforeState, change.Address);
        var afterRole = GetRoleInfo(afterState, change.Address);
        var beforePrincipal = GetPrincipalInfo(beforeState, principalMapper, change.Address);
        var afterPrincipal = GetPrincipalInfo(afterState, principalMapper, change.Address);

        var activeScope = action == DeleteAction ? beforeScope : afterScope;
        var activeRole = action == DeleteAction ? beforeRole : afterRole;
        var activePrincipal = action == DeleteAction ? beforePrincipal : afterPrincipal;

        var summaryText = BuildSummaryText(action, activeScope, activeRole, activePrincipal, scopeFormatter, change.Address);

        var allAttributes = attributeChanges.Count > 0
            ? attributeChanges
            : BuildDefaultAttributes();

        var smallAttrs = new List<RoleAssignmentAttributeViewModel>();
        var largeAttrs = new List<RoleAssignmentAttributeViewModel>();

        foreach (var attr in allAttributes)
        {
            var beforeValue = FormatRoleValue(
                attr.Name,
                beforeState,
                beforeScope,
                beforeRole,
                beforePrincipal,
                scopeFormatter,
                change.Address);
            var afterValue = FormatRoleValue(
                attr.Name,
                afterState,
                afterScope,
                afterRole,
                afterPrincipal,
                scopeFormatter,
                change.Address);

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
    /// <param name="action">The Terraform action string.</param>
    /// <param name="scope">The parsed scope information.</param>
    /// <param name="role">The resolved role information.</param>
    /// <param name="principal">The resolved principal information.</param>
    /// <param name="scopeFormatter">Optional formatter for display name enrichment.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the scope.</param>
    /// <returns>Formatted summary text for the role assignment.</returns>
    private static string BuildSummaryText(
        string action,
        Platforms.Azure.ScopeInfo scope,
        RoleInfo role,
        PrincipalInfo principal,
        EnrichedAzureScopeFormatter? scopeFormatter,
        string resourceAddress)
    {
        var scopeSummary = BuildScopeSummary(scope, scopeFormatter, resourceAddress);
        var roleSummary = $"<code>🛡️{NonBreakingSpace}{EscapeMarkdown(role.Name)}</code>";
        var principalIcon = principal.Type switch
        {
            UserPrincipalType => $"👤{NonBreakingSpace}",
            GroupPrincipalType => $"👥{NonBreakingSpace}",
            ServicePrincipalType => $"💻{NonBreakingSpace}",
            _ => string.Empty
        };
        var principalSummary = $"<code>{principalIcon}{EscapeMarkdown(principal.Name)}</code>";

        return action switch
        {
            "replace" => $"recreate as {principalSummary} → {roleSummary} on {scopeSummary}",
            DeleteAction => $"remove {roleSummary} on {scopeSummary} from {principalSummary}",
            _ => $"{principalSummary} → {roleSummary} on {scopeSummary}"
        };
    }

    /// <summary>
    /// Builds scope summary text with icon formatting for resource group scopes.
    /// </summary>
    /// <param name="scope">The parsed scope information.</param>
    /// <param name="scopeFormatter">Optional formatter for display name enrichment.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the scope.</param>
    /// <returns>Formatted scope summary text.</returns>
    private static string BuildScopeSummary(
        Platforms.Azure.ScopeInfo scope,
        EnrichedAzureScopeFormatter? scopeFormatter,
        string resourceAddress)
    {
        if (scope.Level == ScopeLevel.ResourceGroup)
        {
            var resourceGroup = scope.ResourceGroup ?? scope.SummaryName;
            return FormatAttributeValueSummary("resource_group_name", resourceGroup, null);
        }

        if (scope.Level == ScopeLevel.Subscription)
        {
            var subscriptionId = scope.SubscriptionId ?? scope.SummaryName;
            var subscriptionDisplay = scopeFormatter?.GetSubscriptionName(subscriptionId, resourceAddress) ?? subscriptionId;
            return $"subscription {FormatAttributeValueSummary("subscription_id", subscriptionDisplay, null)}";
        }

        if (scope.Level == ScopeLevel.ManagementGroup)
        {
            var label = scopeFormatter?.GetManagementGroupLabel(scope.Name, resourceAddress) ?? scope.SummaryName;
            var formattedLabel = AzureLabelFormatter.FormatManagementGroupLabel(label);
            return $"{scope.SummaryLabel}{FormatCodeSummary(formattedLabel)}";
        }

        return scope.SummaryLabel + FormatCodeSummary(scope.SummaryName);
    }

    /// <summary>
    /// Formats a role assignment attribute value for display.
    /// </summary>
    /// <param name="attrName">The attribute name to format.</param>
    /// <param name="state">The JSON element to read from.</param>
    /// <param name="scope">The parsed scope information.</param>
    /// <param name="role">The resolved role information.</param>
    /// <param name="principal">The resolved principal information.</param>
    /// <param name="scopeFormatter">Optional scope formatter for display name enrichment.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the scope.</param>
    /// <returns>Formatted value for the attribute, or null when unavailable.</returns>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.")]
    private static string? FormatRoleValue(
        string attrName,
        JsonElement? state,
        ScopeInfo scope,
        RoleInfo role,
        PrincipalInfo principal,
        EnrichedAzureScopeFormatter? scopeFormatter,
        string resourceAddress)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return attrName switch
        {
            ScopeAttribute => FormatScopeValue(scope, scopeFormatter, resourceAddress),
            RoleDefinitionIdAttribute => FormatRoleDefinitionIdValue(role),
            PrincipalIdAttribute => FormatPrincipalIdValue(principal),
            PrincipalTypeAttribute => FormatPrincipalTypeValue(element),
            RoleDefinitionNameAttribute => FormatRoleDefinitionNameValue(element),
            _ => FormatDefaultValue(element, attrName)
        };
    }

    /// <summary>
    /// Formats the scope attribute for table display.
    /// </summary>
    /// <param name="scope">The parsed scope information.</param>
    /// <param name="scopeFormatter">Optional scope formatter for display name enrichment.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the scope.</param>
    /// <returns>Formatted scope string.</returns>
    private static string? FormatScopeValue(
        ScopeInfo scope,
        EnrichedAzureScopeFormatter? scopeFormatter,
        string resourceAddress)
    {
        var subscriptionLabel = scopeFormatter?.GetSubscriptionDisplayName(scope.SubscriptionId, resourceAddress);
        var managementGroupLabel = scope.Level == ScopeLevel.ManagementGroup && scopeFormatter is not null
            ? scopeFormatter.GetManagementGroupLabel(scope.Name, resourceAddress)
            : null;
        return FormatAzureScopeForTable(scope, subscriptionLabel, managementGroupLabel);
    }

    /// <summary>
    /// Formats the role definition attribute using resolved role info.
    /// </summary>
    /// <param name="role">The resolved role information.</param>
    /// <returns>Formatted role definition string.</returns>
    private static string? FormatRoleDefinitionIdValue(RoleInfo role)
    {
        var roleName = !string.IsNullOrEmpty(role.Name)
            ? FormatAttributeValueTable(RoleDefinitionNameAttribute, role.Name, null)
            : string.Empty;
        var roleId = !string.IsNullOrEmpty(role.Id)
            ? FormatCodeTable(role.Id)
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
    }

    /// <summary>
    /// Formats the principal ID attribute with icons and type information.
    /// </summary>
    /// <param name="principal">The resolved principal information.</param>
    /// <returns>Formatted principal string.</returns>
    private static string? FormatPrincipalIdValue(PrincipalInfo principal)
    {
        var principalIcon = principal.Type switch
        {
            UserPrincipalType => "👤",
            GroupPrincipalType => "👥",
            ServicePrincipalType => "💻",
            _ => string.Empty
        };
        var typeLabel = principal.Type switch
        {
            UserPrincipalType => UserPrincipalType,
            GroupPrincipalType => GroupPrincipalType,
            ServicePrincipalType => "Service Principal",
            _ => principal.Type
        };

        var namePart = principal.Name;
        var hasTypeAlready = !string.IsNullOrEmpty(namePart)
            && !string.IsNullOrEmpty(typeLabel)
            && namePart.TrimEnd().EndsWith($"({typeLabel})", StringComparison.Ordinal);

        var decoratedName = !string.IsNullOrEmpty(namePart) && !string.IsNullOrEmpty(typeLabel) && !hasTypeAlready
            ? $"{namePart} ({typeLabel})"
            : namePart;

        var needsIconPrefix = !string.IsNullOrEmpty(principalIcon)
            && !string.IsNullOrEmpty(decoratedName)
            && !decoratedName.StartsWith(principalIcon, StringComparison.Ordinal);

        string nameAndType;
        if (string.IsNullOrEmpty(decoratedName))
        {
            nameAndType = string.Empty;
        }
        else if (needsIconPrefix)
        {
            nameAndType = $"{principalIcon}{NonBreakingSpace}{decoratedName}";
        }
        else
        {
            nameAndType = decoratedName;
        }

        var nameValue = !string.IsNullOrEmpty(nameAndType)
            ? FormatCodeTable(nameAndType)
            : string.Empty;

        var idValue = !string.IsNullOrEmpty(principal.Id)
            ? $"[{FormatCodeTable(principal.Id)}]"
            : string.Empty;

        var text = nameValue;
        if (!string.IsNullOrEmpty(idValue))
        {
            text = string.IsNullOrEmpty(text) ? idValue : $"{text} {idValue}";
        }

        return !string.IsNullOrEmpty(text) ? text : null;
    }

    /// <summary>
    /// Formats the principal type attribute.
    /// </summary>
    /// <param name="element">The JSON element to read from.</param>
    /// <returns>Formatted principal type value.</returns>
    private static string? FormatPrincipalTypeValue(JsonElement element)
    {
        if (!TryGetStringProperty(element, PrincipalTypeAttribute, out var value))
        {
            return null;
        }

        return FormatAttributeValueTable(PrincipalTypeAttribute, value, null);
    }

    /// <summary>
    /// Formats the role definition name attribute.
    /// </summary>
    /// <param name="element">The JSON element to read from.</param>
    /// <returns>Formatted role definition name value.</returns>
    private static string? FormatRoleDefinitionNameValue(JsonElement element)
    {
        if (!TryGetStringProperty(element, RoleDefinitionNameAttribute, out var value))
        {
            return null;
        }

        return FormatAttributeValueTable(RoleDefinitionNameAttribute, value, null);
    }

    /// <summary>
    /// Formats a default attribute when no special handling exists.
    /// </summary>
    /// <param name="element">The JSON element to read from.</param>
    /// <param name="attrName">The attribute name to format.</param>
    /// <returns>Formatted attribute value.</returns>
    private static string? FormatDefaultValue(JsonElement element, string attrName)
    {
        if (!element.TryGetProperty(attrName, out var prop))
        {
            return null;
        }

        var value = prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => prop.ToString()
        };

        return string.IsNullOrEmpty(value) ? null : FormatAttributeValueTable(attrName, value, null);
    }

    /// <summary>
    /// Attempts to read a string property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element to read from.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <param name="value">The extracted string value.</param>
    /// <returns>True when the property was found and non-empty.</returns>
    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;

        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = prop.GetString();
        if (string.IsNullOrEmpty(rawValue))
        {
            return false;
        }

        value = rawValue;
        return true;
    }

    /// <summary>
    /// Builds default attributes when no attribute changes exist.
    /// </summary>
    private static AttributeChangeModel[] BuildDefaultAttributes()
    {
        return new[]
        {
            new AttributeChangeModel { Name = ScopeAttribute, Before = null, After = null, IsSensitive = false, IsLarge = false },
            new AttributeChangeModel { Name = RoleDefinitionIdAttribute, Before = null, After = null, IsSensitive = false, IsLarge = false },
            new AttributeChangeModel { Name = PrincipalIdAttribute, Before = null, After = null, IsSensitive = false, IsLarge = false },
            new AttributeChangeModel { Name = PrincipalTypeAttribute, Before = null, After = null, IsSensitive = false, IsLarge = false },
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
            var match = attributes.Find(a => string.Equals(a.Name, desiredName, StringComparison.Ordinal));
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
    private static Platforms.Azure.ScopeInfo GetScopeInfo(JsonElement? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Platforms.Azure.ScopeInfo.Empty;
        }

        if (!element.TryGetProperty(ScopeAttribute, out var scopeProp) || scopeProp.ValueKind != JsonValueKind.String)
        {
            return Platforms.Azure.ScopeInfo.Empty;
        }

        var scopeValue = scopeProp.GetString();
        return AzureScopeParser.Parse(scopeValue);
    }

    /// <summary>
    /// Extracts role information from the state.
    /// </summary>
    private static RoleInfo GetRoleInfo(JsonElement? state, string resourceAddress)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return new RoleInfo(string.Empty, string.Empty);
        }

        var roleDefId = element.TryGetProperty(RoleDefinitionIdAttribute, out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString() ?? string.Empty
            : string.Empty;

        var roleDefName = element.TryGetProperty(RoleDefinitionNameAttribute, out var nameProp) && nameProp.ValueKind == JsonValueKind.String
            ? nameProp.GetString() ?? string.Empty
            : string.Empty;

        // Use the same logic as the template helper to get consistent output
        var roleInfo = AzureRoleDefinitionMapper.GetRoleDefinition(roleDefId, roleDefName, resourceAddress);

        return new RoleInfo(roleInfo.Name, roleInfo.Id);
    }

    /// <summary>
    /// Extracts principal information from the state.
    /// </summary>
    /// <param name="state">JSON element containing principal information.</param>
    /// <param name="principalMapper">Mapper used to resolve principal names.</param>
    /// <param name="resourceAddress">Terraform resource address for diagnostic tracking of failed resolutions.</param>
    /// <returns>Resolved principal name, id, and type information for formatting.</returns>
    private static PrincipalInfo GetPrincipalInfo(JsonElement? state, IPrincipalMapper principalMapper, string resourceAddress)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return new PrincipalInfo(string.Empty, string.Empty, string.Empty);
        }

        var principalId = element.TryGetProperty(PrincipalIdAttribute, out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString() ?? string.Empty
            : string.Empty;

        var principalType = element.TryGetProperty(PrincipalTypeAttribute, out var typeProp) && typeProp.ValueKind == JsonValueKind.String
            ? typeProp.GetString() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrEmpty(principalType)
            && !string.IsNullOrEmpty(principalId)
            && principalMapper.TryGetPrincipalType(principalId, out var inferredType)
            && !string.IsNullOrEmpty(inferredType))
        {
            principalType = inferredType;
        }

        var principalName = !string.IsNullOrEmpty(principalId)
            ? principalMapper.GetName(principalId, principalType, resourceAddress) ?? principalId
            : string.Empty;

        return new PrincipalInfo(principalName, principalId, principalType);
    }

    private sealed record RoleInfo(string Name, string Id);

    private sealed record PrincipalInfo(string Name, string Id, string Type);
}
