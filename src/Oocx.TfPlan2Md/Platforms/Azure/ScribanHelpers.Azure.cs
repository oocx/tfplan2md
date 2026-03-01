using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Azure-specific markdown formatting helpers.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Formats an Azure scope for table display with semantic icons for resource and resource group names.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    /// <param name="scope">Parsed scope information.</param>
    /// <param name="subscriptionLabel">Optional subscription label override for display name enrichment.</param>
    /// <param name="managementGroupLabel">Optional management group label override for display name enrichment.</param>
    /// <returns>Formatted scope string with semantic icons.</returns>
    internal static string FormatAzureScopeForTable(
        ScopeInfo scope,
        string? subscriptionLabel = null,
        string? managementGroupLabel = null)
    {
        var subscriptionValue = FormatAttributeValueTable(
            "subscription_id",
            subscriptionLabel ?? scope.SubscriptionId ?? string.Empty,
            null);

        switch (scope.Level)
        {
            case ScopeLevel.ResourceGroup:
                var rgName = FormatAttributeValueTable("resource_group_name", scope.ResourceGroup, null);
                return $"{rgName} in subscription {subscriptionValue}";

            case ScopeLevel.Resource when !string.IsNullOrEmpty(scope.ResourceGroup):
                var resourceName = FormatAttributeValueTable("name", scope.Name, null);
                var resourceRgName = FormatAttributeValueTable("resource_group_name", scope.ResourceGroup, null);
                return $"{scope.Type} {resourceName} in resource group {resourceRgName} of subscription {subscriptionValue}";

            case ScopeLevel.Resource:
                var resourceNameOnly = FormatAttributeValueTable("name", scope.Name, null);
                return $"{scope.Type} {resourceNameOnly} in subscription {subscriptionValue}";

            case ScopeLevel.Subscription:
                return $"subscription {subscriptionValue}";

            case ScopeLevel.ManagementGroup:
                var label = string.IsNullOrWhiteSpace(managementGroupLabel) ? scope.Name : managementGroupLabel;
                var formattedLabel = AzureLabelFormatter.FormatManagementGroupLabel(label);
                return FormatCodeTable($"{formattedLabel} (Management Group)");

            default:
                return !string.IsNullOrEmpty(scope.Details) ? EscapeMarkdown(scope.Details) : string.Empty;
        }
    }
}

