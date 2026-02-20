using System.Collections.Frozen;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Represents the parsed result of an Azure mapping file load.
/// </summary>
/// <param name="Principals">Resolved principal display names keyed by ID.</param>
/// <param name="PrincipalTypes">Resolved principal type labels keyed by ID.</param>
/// <param name="Subscriptions">Subscription mappings for display name resolution.</param>
/// <param name="ManagementGroups">Management group mappings for display name resolution.</param>
/// <param name="Tenants">Tenant mappings for display name resolution.</param>
/// <param name="Roles">Custom role definition mappings for display name resolution.</param>
/// <param name="AzdoUsers">Azure DevOps user ID to display name mappings.</param>
/// <param name="AzdoGroups">Azure DevOps group descriptor to display name mappings.</param>
/// <param name="AzdoProjects">Azure DevOps project ID to display name mappings.</param>
/// <param name="AzdoRepositories">Azure DevOps repository ID to display name mappings.</param>
/// <remarks>
/// Related features:
/// <list type="bullet">
/// <item><description>docs/features/063-azure-display-enhancements/specification.md.</description></item>
/// <item><description>docs/features/085-azdo-principal-mapping/specification.md.</description></item>
/// <item><description>docs/features/095-azdo-repo-mapping-and-icons/specification.md.</description></item>
/// </list>
/// </remarks>
internal sealed record AzureMappingFileResult(
    FrozenDictionary<string, string> Principals,
    FrozenDictionary<string, string> PrincipalTypes,
    IReadOnlyList<MappingEntry> Subscriptions,
    IReadOnlyList<MappingEntry> ManagementGroups,
    IReadOnlyList<MappingEntry> Tenants,
    IReadOnlyList<MappingEntry> Roles,
    FrozenDictionary<string, string> AzdoUsers,
    FrozenDictionary<string, string> AzdoGroups,
    FrozenDictionary<string, string> AzdoProjects,
    FrozenDictionary<string, string> AzdoRepositories)
{
    /// <summary>
    /// Gets an empty mapping file result with no data.
    /// </summary>
    /// <value>
    /// An <see cref="AzureMappingFileResult"/> instance with empty mappings.
    /// </value>
    internal static AzureMappingFileResult Empty { get; } = new(
        FrozenDictionary<string, string>.Empty,
        FrozenDictionary<string, string>.Empty,
        Array.Empty<MappingEntry>(),
        Array.Empty<MappingEntry>(),
        Array.Empty<MappingEntry>(),
        Array.Empty<MappingEntry>(),
        FrozenDictionary<string, string>.Empty,
        FrozenDictionary<string, string>.Empty,
        FrozenDictionary<string, string>.Empty,
        FrozenDictionary<string, string>.Empty);
}
