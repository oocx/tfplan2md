using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Resolves Azure subscription, management group, and tenant display names.
/// </summary>
/// <remarks>
/// This mapper is used to enrich scope output with human-friendly names.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </remarks>
internal sealed class AzureEntityMapper
{
    /// <summary>
    /// Maps subscription IDs to display names.
    /// </summary>
    private readonly FrozenDictionary<string, string> _subscriptions;

    /// <summary>
    /// Maps management group IDs to display names.
    /// </summary>
    private readonly FrozenDictionary<string, string> _managementGroups;

    /// <summary>
    /// Maps tenant IDs to display names.
    /// </summary>
    private readonly FrozenDictionary<string, string> _tenants;

    /// <summary>
    /// Optional diagnostics for recording failed resolutions.
    /// </summary>
    private readonly DiagnosticContext? _diagnosticContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureEntityMapper"/> class.
    /// </summary>
    /// <param name="subscriptions">Subscription mappings for display name resolution.</param>
    /// <param name="managementGroups">Management group mappings for display name resolution.</param>
    /// <param name="tenants">Tenant mappings for display name resolution.</param>
    /// <param name="diagnosticContext">Optional diagnostics used to record missing mappings.</param>
    /// <remarks>
    /// The mappings are cached in case-insensitive dictionaries for fast lookups.
    /// </remarks>
    internal AzureEntityMapper(
        IReadOnlyList<MappingEntry> subscriptions,
        IReadOnlyList<MappingEntry> managementGroups,
        IReadOnlyList<MappingEntry> tenants,
        DiagnosticContext? diagnosticContext = null)
    {
        _diagnosticContext = diagnosticContext;
        _subscriptions = CreateLookup(subscriptions);
        _managementGroups = CreateLookup(managementGroups);
        _tenants = CreateLookup(tenants);
    }

    /// <summary>
    /// Gets the subscription display name formatted as "DisplayName (Id)" when available.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the subscription.</param>
    /// <returns>The formatted display name, or the raw ID when no mapping exists.</returns>
    internal string GetSubscriptionDisplayName(string? subscriptionId, string? resourceAddress = null)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return string.Empty;
        }

        if (_subscriptions.TryGetValue(subscriptionId, out var displayName))
        {
            return $"{displayName} ({subscriptionId})";
        }

        RecordFailure(FailedResolutionType.Subscription, subscriptionId, resourceAddress);
        return subscriptionId;
    }

    /// <summary>
    /// Gets the subscription name without ID suffix when a mapping is available.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>
    /// The display name when a mapping exists, or the raw subscription ID as a fallback.
    /// Unlike <see cref="GetSubscriptionDisplayName"/>, no "(id)" suffix is appended.
    /// </returns>
    /// <remarks>
    /// Use this method in summary contexts where showing the subscription name alone
    /// (without the subscription ID) is preferred when a human-readable name is available.
    /// Related feature: docs/features/improve-summary-for-role-assignments/specification.md.
    /// </remarks>
    internal string? GetSubscriptionName(string? subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return subscriptionId;
        }

        return _subscriptions.TryGetValue(subscriptionId, out var displayName)
            ? displayName
            : subscriptionId;
    }

    /// <summary>
    /// Gets the management group display name when available.
    /// </summary>
    /// <param name="managementGroupId">The management group identifier.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the management group.</param>
    /// <returns>The display name, or the raw ID when no mapping exists.</returns>
    internal string GetManagementGroupDisplayName(string? managementGroupId, string? resourceAddress = null)
    {
        if (string.IsNullOrWhiteSpace(managementGroupId))
        {
            return string.Empty;
        }

        if (_managementGroups.TryGetValue(managementGroupId, out var displayName))
        {
            return displayName;
        }

        RecordFailure(FailedResolutionType.ManagementGroup, managementGroupId, resourceAddress);
        return managementGroupId;
    }

    /// <summary>
    /// Gets the tenant display name formatted as "DisplayName (Id)" when available.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the tenant.</param>
    /// <returns>The formatted display name, or the raw ID when no mapping exists.</returns>
    internal string GetTenantDisplayName(string? tenantId, string? resourceAddress = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return string.Empty;
        }

        if (_tenants.TryGetValue(tenantId, out var displayName))
        {
            return $"{displayName} ({tenantId})";
        }

        RecordFailure(FailedResolutionType.Tenant, tenantId, resourceAddress);
        return tenantId;
    }

    /// <summary>
    /// Records a failed resolution when diagnostics are enabled.
    /// </summary>
    /// <param name="type">The resolution type that failed.</param>
    /// <param name="id">The identifier that could not be resolved.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the ID.</param>
    private void RecordFailure(FailedResolutionType type, string id, string? resourceAddress)
    {
        if (_diagnosticContext == null || string.IsNullOrWhiteSpace(resourceAddress))
        {
            return;
        }

        _diagnosticContext.FailedResolutions.Add(new FailedResolution(
            type,
            id,
            resourceAddress,
            "not found in mapping file"));
    }

    /// <summary>
    /// Creates a case-insensitive lookup dictionary for mapping entries.
    /// </summary>
    /// <param name="entries">The mapping entries to load.</param>
    /// <returns>A frozen dictionary for fast lookups.</returns>
    private static FrozenDictionary<string, string> CreateLookup(IReadOnlyList<MappingEntry> entries)
    {
        if (entries.Count == 0)
        {
            return FrozenDictionary<string, string>.Empty;
        }

        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                continue;
            }

            lookup[entry.Id] = entry.DisplayName ?? string.Empty;
        }

        return lookup.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
