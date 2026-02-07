using System;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Formats Azure scope details with subscription and management group display names.
/// </summary>
/// <remarks>
/// This formatter composes <see cref="AzureScopeParser"/> output with mapped display names
/// for subscriptions, management groups, and tenants.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </remarks>
internal sealed class EnrichedAzureScopeFormatter
{
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
    /// <returns>The formatted scope description.</returns>
    internal string FormatScope(string? scope)
    {
        var parsed = AzureScopeParser.Parse(scope);
        return Format(parsed);
    }

    /// <summary>
    /// Formats an already-parsed scope with display name enrichment.
    /// </summary>
    /// <param name="scopeInfo">The parsed scope information.</param>
    /// <returns>The formatted scope description.</returns>
    internal string Format(ScopeInfo scopeInfo)
    {
        if (scopeInfo.Level == ScopeLevel.Unknown)
        {
            return scopeInfo.Details;
        }

        var subscriptionDisplay = _entityMapper.GetSubscriptionDisplayName(scopeInfo.SubscriptionId);
        var subscriptionLabel = string.IsNullOrWhiteSpace(subscriptionDisplay)
            ? scopeInfo.SubscriptionId ?? string.Empty
            : subscriptionDisplay;

        return scopeInfo.Level switch
        {
            ScopeLevel.ManagementGroup => FormatManagementGroup(scopeInfo.Name),
            ScopeLevel.Subscription => $"subscription `{subscriptionLabel}`",
            ScopeLevel.ResourceGroup => $"`{scopeInfo.ResourceGroup}` in subscription `{subscriptionLabel}`",
            ScopeLevel.Resource when !string.IsNullOrWhiteSpace(scopeInfo.ResourceGroup) =>
                $"{scopeInfo.Type} `{scopeInfo.Name}` in resource group `{scopeInfo.ResourceGroup}` of subscription `{subscriptionLabel}`",
            ScopeLevel.Resource => $"{scopeInfo.Type} `{scopeInfo.Name}` in subscription `{subscriptionLabel}`",
            _ => scopeInfo.Details
        };
    }

    /// <summary>
    /// Formats management group scopes with tenant-root detection.
    /// </summary>
    /// <param name="managementGroupId">The management group identifier.</param>
    /// <returns>The formatted management group scope description.</returns>
    private string FormatManagementGroup(string managementGroupId)
    {
        var tenantName = _entityMapper.GetTenantDisplayName(managementGroupId);
        if (!string.IsNullOrWhiteSpace(tenantName) && !tenantName.Equals(managementGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Tenant `{tenantName}` root";
        }

        var managementGroupName = _entityMapper.GetManagementGroupDisplayName(managementGroupId);
        var label = string.IsNullOrWhiteSpace(managementGroupName) ? managementGroupId : managementGroupName;
        return $"management group `{label}`";
    }
}
