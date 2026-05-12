using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Encodings.Web;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Builds summary HTML strings and badges for resource changes.
/// </summary>
/// <remarks>
/// Extracted from ReportModelBuilder to reduce class coupling.
/// Related feature: docs/features/024-visual-report-enhancements/specification.md.
/// </remarks>
internal static class ResourceSummaryHtmlBuilder
{
    /// <summary>
    /// Builds a summary-safe HTML string for use inside summary elements, including action icon, type, name, location, address space, and changed attributes.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="model">Resource change model containing the source data.</param>
    /// <returns>HTML string safe for use inside a summary element.</returns>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Feature formatting logic for docs/features/051-display-enhancements/specification.md.")]
    public static string BuildSummaryHtml(ResourceChangeModel model)
    {
        var state = model.AfterJson ?? model.BeforeJson;
        var flatState = JsonFlattener.ConvertToFlatDictionary(state);

        flatState.TryGetValue("name", out var nameValue);
        flatState.TryGetValue("resource_group_name", out var resourceGroup);
        flatState.TryGetValue("location", out var location);
        flatState.TryGetValue("address_space[0]", out var addressSpace);
        flatState.TryGetValue("subscription", out var subscriptionName);
        flatState.TryGetValue("subscription_id", out var subscriptionId);

        // Fall back to principal_name as primary identifier when "name" is absent.
        // Tracks whether we switched to principal_name so we can include remaining
        // mapped attributes in the summary line (e.g., account_license_type, licensing_source
        // for azuredevops_user_entitlement).
        // Related feature: docs/features/115-azuredevops-user-entitlement-summary/specification.md.
        var usedPrincipalNameFallback = false;
        if (string.IsNullOrWhiteSpace(nameValue))
        {
            flatState.TryGetValue("principal_name", out nameValue);
            usedPrincipalNameFallback = !string.IsNullOrWhiteSpace(nameValue);
        }

        // For AzAPI resources without a friendly name, fall back to the Terraform resource name
        var displayName = !string.IsNullOrWhiteSpace(model.Name)
            ? model.Name
            : ExtractTerraformLocalName(model.Address);

        var encodedType = HtmlEncoder.Default.Encode(model.Type);
        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{encodedType} <b>{FormatCodeSummary(displayName)}</b>";
        var detailParts = new List<string>();
        var refactoringContext = BuildRefactoringContext(model);

        var nameAttributeKey = usedPrincipalNameFallback ? "principal_name" : "name";
        var primaryContext = !string.IsNullOrWhiteSpace(nameValue)
            ? FormatAttributeValueSummary(nameAttributeKey, nameValue!, null)
            : null;

        if (!string.IsNullOrWhiteSpace(resourceGroup))
        {
            var groupText = FormatAttributeValueSummary("resource_group_name", resourceGroup!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} in {groupText}" : groupText;
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationText = FormatAttributeValueSummary("location", location!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} {locationText}" : locationText;
        }

        if (primaryContext != null)
        {
            detailParts.Add(primaryContext);
        }

        if (!string.IsNullOrWhiteSpace(addressSpace))
        {
            detailParts.Add(FormatAttributeValueSummary("address_space[0]", addressSpace!, null));
        }

        var isSubscriptionResource = model.Type.Equals("azurerm_subscription", StringComparison.OrdinalIgnoreCase);
        if (isSubscriptionResource)
        {
            if (!string.IsNullOrWhiteSpace(subscriptionName) && !string.IsNullOrWhiteSpace(subscriptionId))
            {
                detailParts.Add(FormatAttributeValueSummary("subscription", $"{subscriptionName} ({subscriptionId})", null));
            }
            else if (!string.IsNullOrWhiteSpace(subscriptionName))
            {
                detailParts.Add(FormatAttributeValueSummary("subscription", subscriptionName!, null));
            }
            else if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                detailParts.Add(FormatAttributeValueSummary("subscription_id", subscriptionId!, null));
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(subscriptionName))
            {
                detailParts.Add(FormatAttributeValueSummary("subscription", subscriptionName!, null));
            }

            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                detailParts.Add(FormatAttributeValueSummary("subscription_id", subscriptionId!, null));
            }
        }

        if (!string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            detailParts.Add($"| {model.ChangedAttributesSummary!}");
        }

        // When principal_name was used as the primary identifier, include remaining mapped
        // attributes in the summary line so all key fields appear in the details header
        // (e.g., account_license_type and licensing_source for azuredevops_user_entitlement).
        // Related feature: docs/features/115-azuredevops-user-entitlement-summary/specification.md.
        if (usedPrincipalNameFallback && string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            AppendRemainingMappedAttributes(detailParts, flatState, model.Type);
        }

        if (!string.IsNullOrWhiteSpace(refactoringContext))
        {
            if (detailParts.Count > 0 && !detailParts[0].StartsWith('|'))
            {
                detailParts[0] = $"| {detailParts[0]}";
            }

            detailParts.Insert(0, refactoringContext);
        }

        return detailParts.Count == 0
            ? prefix
            : $"{prefix} — {string.Join(" ", detailParts)}";
    }

    /// <summary>
    /// Builds the refactoring annotation for summary lines when import or moved metadata is present.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    /// <param name="model">Resource change model containing refactoring metadata.</param>
    /// <returns>Formatted refactoring annotation or empty string when not applicable.</returns>
    private static string BuildRefactoringContext(ResourceChangeModel model)
    {
        if (model.ImportId is null && model.MovedFromAddress is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (model.ImportId is not null)
        {
            parts.Add($"📥{NonBreakingSpace}*Imported*{BuildAlreadyAppliedSuffix(model, "Import")}");
        }

        if (model.MovedFromAddress is not null)
        {
            var movedFrom = FormatCodeSummary(model.MovedFromAddress);
            parts.Add($"🔀{NonBreakingSpace}*Moved from* {movedFrom}{BuildAlreadyAppliedSuffix(model, "Move")}");
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Builds the suffix for already-applied refactoring warnings.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    /// <param name="model">Resource change model indicating already-applied status.</param>
    /// <param name="operation">The refactoring operation name (Import or Move) used to pick consistent warning wording.</param>
    /// <returns>Warning suffix or empty string when not applicable.</returns>
    private static string BuildAlreadyAppliedSuffix(ResourceChangeModel model, string operation)
    {
        var isAlreadyApplied = operation.Equals("Import", StringComparison.OrdinalIgnoreCase)
            ? model.IsImportAlreadyApplied
            : operation.Equals("Move", StringComparison.OrdinalIgnoreCase) && model.IsMoveAlreadyApplied;

        if (!isAlreadyApplied)
        {
            return string.Empty;
        }

        var warning = "already applied";
        if (operation.Equals("Import", StringComparison.OrdinalIgnoreCase))
        {
            warning = "already imported";
        }
        else if (operation.Equals("Move", StringComparison.OrdinalIgnoreCase))
        {
            warning = "already moved";
        }

        return $" (⚠️{NonBreakingSpace}*{warning}*)";
    }

    /// <summary>
    /// Builds a concise changed-attributes summary for update operations (e.g., "2 🔧 attr1, attr2, +N more").
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeChanges">The list of attribute changes.</param>
    /// <param name="action">The resource action (create, update, delete, etc.).</param>
    /// <returns>Summary string for changed attributes, or empty if not an update.</returns>
    public static string BuildChangedAttributesSummary(IReadOnlyList<AttributeChangeModel> attributeChanges, string action)
    {
        if (!string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (attributeChanges.Count == 0)
        {
            return string.Empty;
        }

        var names = attributeChanges.Select(a => a.Name).ToList();
        var displayedNames = names.Take(3).ToList();
        var remaining = names.Count - displayedNames.Count;

        var nameList = string.Join(", ", displayedNames);
        if (remaining > 0)
        {
            nameList += $", +{remaining} more";
        }

        return $"{names.Count}{NonBreakingSpace}🔧{NonBreakingSpace}{nameList}";
    }


    /// <summary>
    /// Builds inline tag badges for create/delete operations, keeping templates free from tag formatting logic.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="afterJson">The after state JSON.</param>
    /// <param name="beforeJson">The before state JSON.</param>
    /// <param name="action">The resource action (create, update, delete, etc.).</param>
    /// <returns>Tags badge string, or null if no tags or on updates.</returns>
    public static string? BuildTagsBadges(object? afterJson, object? beforeJson, string action)
    {
        if (!string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var state = string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase) ? beforeJson : afterJson;
        var flat = JsonFlattener.ConvertToFlatDictionary(state);

        var tags = flat.Where(kvp => kvp.Key.StartsWith("tags.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => new { Key = kvp.Key[5..], Value = kvp.Value })
            .ToList();

        if (tags.Count == 0)
        {
            return null;
        }

        var badges = tags.Select(tag => FormatCodeTable($"{tag.Key}: {tag.Value}"));
        return $"**🏷️{NonBreakingSpace}Tags:**\n{string.Join('\n', badges.Select(badge => $" {badge}"))}";
    }

    /// <summary>
    /// Appends detail parts for the remaining resource-type-specific mapped attributes, skipping
    /// the primary identifier key (principal_name) already used as the summary context.
    /// </summary>
    /// <remarks>
    /// Used when a resource type has no "name" attribute and uses a non-standard primary
    /// identifier (e.g., principal_name for azuredevops_user_entitlement). The remaining
    /// mapped keys (e.g., account_license_type, licensing_source) are added to the detail
    /// parts so they appear in the summary line of the details block.
    /// Related feature: docs/features/115-azuredevops-user-entitlement-summary/specification.md.
    /// </remarks>
    /// <param name="detailParts">The list of detail parts to append to.</param>
    /// <param name="flatState">The flattened resource state dictionary.</param>
    /// <param name="resourceType">The Terraform resource type used to look up the mapping.</param>
    private static void AppendRemainingMappedAttributes(
        List<string> detailParts,
        Dictionary<string, string?> flatState,
        string resourceType)
    {
        var mappingKeys = ResourceSummaryMappings.ResolveKeys(resourceType);
        foreach (var key in mappingKeys)
        {
            // Skip principal_name — it is already used as the primary context.
            if (key.Equals("principal_name", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (flatState.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                detailParts.Add($"| {FormatAttributeValueSummary(key, value, null)}");
            }
        }
    }

    /// <summary>
    /// Extracts the Terraform local resource name from a resource address.
    /// Related issue: docs/issues/086-style-guide-compliance-fixes/issue-analysis.md (Violation 2).
    /// </summary>
    /// <param name="address">The full Terraform resource address (e.g., "azapi_resource.automation_account").</param>
    /// <returns>The local resource name (e.g., "automation_account").</returns>
    /// <remarks>
    /// For module resources, extracts the name after the last dot (e.g., "module.network.azapi_resource.vm" returns "vm").
    /// </remarks>
    private static string ExtractTerraformLocalName(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        // Address format: "resource_type.local_name" or "module.name.resource_type.local_name"
        var lastDotIndex = address.LastIndexOf('.');
        return lastDotIndex >= 0 ? address[(lastDotIndex + 1)..] : address;
    }
}
