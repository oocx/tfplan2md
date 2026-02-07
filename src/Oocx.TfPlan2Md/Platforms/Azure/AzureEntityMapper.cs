using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Resolves Azure subscription, management group, and tenant display names.
/// </summary>
/// <remarks>
/// This mapper is used to enrich scope output with human-friendly names.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
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
    /// Initializes a new instance of the <see cref="AzureEntityMapper"/> class.
    /// </summary>
    /// <param name="subscriptions">Subscription mappings for display name resolution.</param>
    /// <param name="managementGroups">Management group mappings for display name resolution.</param>
    /// <param name="tenants">Tenant mappings for display name resolution.</param>
    /// <remarks>
    /// The mappings are cached in case-insensitive dictionaries for fast lookups.
    /// </remarks>
    internal AzureEntityMapper(
        IReadOnlyList<MappingEntry> subscriptions,
        IReadOnlyList<MappingEntry> managementGroups,
        IReadOnlyList<MappingEntry> tenants)
    {
        _subscriptions = CreateLookup(subscriptions);
        _managementGroups = CreateLookup(managementGroups);
        _tenants = CreateLookup(tenants);
    }

    /// <summary>
    /// Gets the subscription display name formatted as "DisplayName (Id)" when available.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>The formatted display name, or the raw ID when no mapping exists.</returns>
    internal string GetSubscriptionDisplayName(string? subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return string.Empty;
        }

        return _subscriptions.TryGetValue(subscriptionId, out var displayName)
            ? $"{displayName} ({subscriptionId})"
            : subscriptionId;
    }

    /// <summary>
    /// Gets the management group display name when available.
    /// </summary>
    /// <param name="managementGroupId">The management group identifier.</param>
    /// <returns>The display name, or the raw ID when no mapping exists.</returns>
    internal string GetManagementGroupDisplayName(string? managementGroupId)
    {
        if (string.IsNullOrWhiteSpace(managementGroupId))
        {
            return string.Empty;
        }

        return _managementGroups.TryGetValue(managementGroupId, out var displayName)
            ? displayName
            : managementGroupId;
    }

    /// <summary>
    /// Gets the tenant display name when available.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The display name, or the raw ID when no mapping exists.</returns>
    internal string GetTenantDisplayName(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return string.Empty;
        }

        return _tenants.TryGetValue(tenantId, out var displayName)
            ? displayName
            : tenantId;
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
