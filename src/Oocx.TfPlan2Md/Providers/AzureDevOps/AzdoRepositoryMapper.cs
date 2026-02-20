using System.Collections.Frozen;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Maps Azure DevOps repository IDs to display names.
/// </summary>
/// <remarks>
/// Azure DevOps repositories are identified by unique GUIDs. This mapper resolves
/// repository IDs to human-readable names for improved report readability.
/// Related feature: docs/features/095-azdo-repo-mapping-and-icons/specification.md.
/// </remarks>
internal sealed class AzdoRepositoryMapper
{
    /// <summary>
    /// Maps Azure DevOps repository IDs to display names.
    /// </summary>
    private readonly FrozenDictionary<string, string> _repositoryMappings;

    /// <summary>
    /// Optional diagnostics for recording failed resolutions.
    /// </summary>
    private readonly DiagnosticContext? _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoRepositoryMapper"/> class.
    /// </summary>
    /// <param name="repositoryMappings">Mapping of repository IDs to display names.</param>
    /// <param name="diagnostics">Optional diagnostic context for recording failed resolutions.</param>
    public AzdoRepositoryMapper(FrozenDictionary<string, string> repositoryMappings, DiagnosticContext? diagnostics)
    {
        _repositoryMappings = repositoryMappings;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets only the display name for a repository ID without resource context.
    /// </summary>
    /// <param name="repositoryId">The GUID of the repository.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    public string? GetName(string repositoryId)
    {
        if (string.IsNullOrWhiteSpace(repositoryId))
        {
            return null;
        }

        return _repositoryMappings.TryGetValue(repositoryId, out var name) ? name : null;
    }

    /// <summary>
    /// Gets only the display name for a repository ID with optional resource context.
    /// </summary>
    /// <param name="repositoryId">The GUID of the repository.</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    /// <remarks>
    /// If a diagnostic context was provided and the repository ID cannot be resolved,
    /// the failure is recorded with the resource address for troubleshooting.
    /// </remarks>
    public string? GetName(string repositoryId, string? resourceAddress)
    {
        if (string.IsNullOrWhiteSpace(repositoryId))
        {
            return null;
        }

        var found = _repositoryMappings.TryGetValue(repositoryId, out var name);

        // Record failed resolution for diagnostics
        if (!found && _diagnostics != null && resourceAddress != null)
        {
            _diagnostics.FailedResolutions.Add(
                new FailedResolution(
                    FailedResolutionType.AzdoRepository,
                    repositoryId,
                    resourceAddress,
                    "not found in mapping file"));
        }

        return found ? name : null;
    }

    /// <summary>
    /// Gets the formatted entity name for display (🗃️ DisplayName [ID] or 🗃️ ID if not mapped).
    /// </summary>
    /// <param name="repositoryId">The GUID of the repository.</param>
    /// <returns>
    /// Repository icon followed by display name and repository ID in brackets if mapping exists,
    /// otherwise just the repository icon followed by the ID.
    /// </returns>
    public string GetEntityName(string repositoryId)
    {
        if (string.IsNullOrWhiteSpace(repositoryId))
        {
            return repositoryId ?? string.Empty;
        }

        var displayName = GetName(repositoryId);
        return displayName is null
            ? $"🗃️ {repositoryId}"
            : $"🗃️ {displayName} [{repositoryId}]";
    }
}
