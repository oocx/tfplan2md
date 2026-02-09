using System;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Formats Azure scope details with subscription and management group display names.
/// </summary>
/// <remarks>
/// This formatter composes <see cref="AzureScopeParser"/> output with mapped display names
/// for subscriptions, management groups, and tenants.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </remarks>
internal sealed class EnrichedAzureScopeFormatter
{
    /// <summary>
    /// Icon for subscription identifiers.
    /// </summary>
    private const string SubscriptionIcon = "🔑";

    /// <summary>
    /// Icon for resource group identifiers.
    /// </summary>
    private const string ResourceGroupIcon = "📁";

    /// <summary>
    /// Prefix used to identify tenant root management group labels.
    /// </summary>
    private const string TenantRootPrefix = "Tenant ";

    /// <summary>
    /// Suffix used to identify tenant root management group labels.
    /// </summary>
    private const string TenantRootSuffix = " root";

    /// <summary>
    /// The entity mapper used to resolve display names.
    /// </summary>
    private readonly AzureEntityMapper _entityMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnrichedAzureScopeFormatter"/> class.
    /// </summary>
    /// <param name="entityMapper">Mapper used to resolve entity display names.</param>
    internal EnrichedAzureScopeFormatter(AzureEntityMapper entityMapper)
    {
        ArgumentNullException.ThrowIfNull(entityMapper);
        _entityMapper = entityMapper;
    }

    /// <summary>
    /// Formats an Azure resource scope string with display name enrichment.
    /// </summary>
    /// <param name="scope">The Azure scope string to format.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the scope.</param>
    /// <returns>The formatted scope description.</returns>
    internal string FormatScope(string? scope, string? resourceAddress = null)
    {
        var parsed = AzureScopeParser.Parse(scope);
        return Format(parsed, resourceAddress);
    }

    /// <summary>
    /// Resolves a subscription display label using the configured mappings.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the subscription.</param>
    /// <returns>The subscription label with display name enrichment when available.</returns>
    /// <remarks>
    /// Related feature: docs/features/063-azure-display-enhancements/specification.md.
    /// </remarks>
    internal string GetSubscriptionDisplayName(string? subscriptionId, string? resourceAddress = null)
    {
        return _entityMapper.GetSubscriptionDisplayName(subscriptionId, resourceAddress);
    }

    /// <summary>
    /// Formats an already-parsed scope with display name enrichment.
    /// </summary>
    /// <param name="scopeInfo">The parsed scope information.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the scope.</param>
    /// <returns>The formatted scope description.</returns>
    internal string Format(ScopeInfo scopeInfo, string? resourceAddress = null)
    {
        if (scopeInfo.Level == ScopeLevel.Unknown)
        {
            return scopeInfo.Details;
        }

        var subscriptionDisplay = _entityMapper.GetSubscriptionDisplayName(scopeInfo.SubscriptionId, resourceAddress);
        var subscriptionLabel = string.IsNullOrWhiteSpace(subscriptionDisplay)
            ? scopeInfo.SubscriptionId ?? string.Empty
            : subscriptionDisplay;
        var subscriptionValue = FormatSubscriptionLabel(subscriptionLabel);
        var resourceGroupValue = FormatResourceGroupLabel(scopeInfo.ResourceGroup);

        return scopeInfo.Level switch
        {
            ScopeLevel.ManagementGroup => FormatManagementGroup(scopeInfo.Name, resourceAddress),
            ScopeLevel.Subscription => $"subscription `{subscriptionValue}`",
            ScopeLevel.ResourceGroup => $"`{resourceGroupValue}` in subscription `{subscriptionValue}`",
            ScopeLevel.Resource when !string.IsNullOrWhiteSpace(scopeInfo.ResourceGroup) =>
                $"{scopeInfo.Type} `{scopeInfo.Name}` in resource group `{resourceGroupValue}` of subscription `{subscriptionValue}`",
            ScopeLevel.Resource => $"{scopeInfo.Type} `{scopeInfo.Name}` in subscription `{subscriptionValue}`",
            _ => scopeInfo.Details
        };
    }

    /// <summary>
    /// Formats management group scopes with tenant-root detection.
    /// </summary>
    /// <param name="managementGroupId">The management group identifier.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the management group.</param>
    /// <returns>The formatted management group scope description.</returns>
    private string FormatManagementGroup(string managementGroupId, string? resourceAddress)
    {
        var label = GetManagementGroupLabel(managementGroupId, resourceAddress);
        return FormatManagementGroupScopeLabel(label);
    }

    /// <summary>
    /// Formats management group labels for scope text with inline code around display names.
    /// </summary>
    /// <param name="label">The resolved management group label.</param>
    /// <returns>Formatted scope label with icon and inline code where appropriate.</returns>
    private static string FormatManagementGroupScopeLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        if (TryParseTenantRootLabel(label, out var tenantName))
        {
            var tenantValue = ScribanHelpers.FormatCodeTable(tenantName);
            return $"{AzureLabelFormatter.ManagementGroupIcon}{AzureLabelFormatter.NonBreakingSpace}" +
                $"{TenantRootPrefix}{tenantValue}{TenantRootSuffix}";
        }

        var labelValue = ScribanHelpers.FormatCodeTable(label);
        return $"{AzureLabelFormatter.ManagementGroupIcon}{AzureLabelFormatter.NonBreakingSpace}{labelValue}";
    }

    /// <summary>
    /// Attempts to parse the tenant display name from a tenant root management group label.
    /// </summary>
    /// <param name="label">The label to parse.</param>
    /// <param name="tenantName">The extracted tenant display name when present.</param>
    /// <returns>True when the label matches the tenant root pattern; otherwise false.</returns>
    private static bool TryParseTenantRootLabel(string label, out string tenantName)
    {
        if (!label.StartsWith(TenantRootPrefix, StringComparison.Ordinal)
            || !label.EndsWith(TenantRootSuffix, StringComparison.Ordinal)
            || label.Length <= TenantRootPrefix.Length + TenantRootSuffix.Length)
        {
            tenantName = string.Empty;
            return false;
        }

        tenantName = label.Substring(
            TenantRootPrefix.Length,
            label.Length - TenantRootPrefix.Length - TenantRootSuffix.Length);
        return !string.IsNullOrWhiteSpace(tenantName);
    }

    /// <summary>
    /// Resolves the display label for a management group, including tenant-root naming when applicable.
    /// </summary>
    /// <param name="managementGroupId">The management group identifier.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the management group.</param>
    /// <returns>The management group display label without icon formatting.</returns>
    /// <remarks>
    /// Related feature: docs/features/065-tenant-display-mapping/specification.md.
    /// </remarks>
    internal string GetManagementGroupLabel(string managementGroupId, string? resourceAddress = null)
    {
        var tenantName = _entityMapper.GetTenantDisplayName(managementGroupId, resourceAddress);
        if (!string.IsNullOrWhiteSpace(tenantName) && !tenantName.Equals(managementGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Tenant {tenantName} root";
        }

        var managementGroupName = _entityMapper.GetManagementGroupDisplayName(managementGroupId, resourceAddress);
        return string.IsNullOrWhiteSpace(managementGroupName) ? managementGroupId : managementGroupName;
    }

    /// <summary>
    /// Formats a subscription label with the subscription icon when available.
    /// </summary>
    /// <param name="subscriptionLabel">The subscription label to format.</param>
    /// <returns>Subscription label with icon prefix.</returns>
    private static string FormatSubscriptionLabel(string subscriptionLabel)
    {
        if (string.IsNullOrWhiteSpace(subscriptionLabel))
        {
            return string.Empty;
        }

        return $"{SubscriptionIcon}{AzureLabelFormatter.NonBreakingSpace}{subscriptionLabel}";
    }

    /// <summary>
    /// Formats a resource group label with the resource group icon when available.
    /// </summary>
    /// <param name="resourceGroup">The resource group label to format.</param>
    /// <returns>Resource group label with icon prefix.</returns>
    private static string FormatResourceGroupLabel(string? resourceGroup)
    {
        if (string.IsNullOrWhiteSpace(resourceGroup))
        {
            return string.Empty;
        }

        return $"{ResourceGroupIcon}{AzureLabelFormatter.NonBreakingSpace}{resourceGroup}";
    }
}
