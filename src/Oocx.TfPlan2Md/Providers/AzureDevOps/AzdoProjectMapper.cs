using System.Collections.Frozen;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Maps Azure DevOps project IDs to display names.
/// </summary>
/// <remarks>
/// Azure DevOps projects are identified by unique GUIDs. This mapper resolves
/// project IDs to human-readable names for improved report readability.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal sealed class AzdoProjectMapper
{
    /// <summary>
    /// Maps Azure DevOps project IDs to display names.
    /// </summary>
    private readonly FrozenDictionary<string, string> _projectMappings;

    /// <summary>
    /// Optional diagnostics for recording failed resolutions.
    /// </summary>
    private readonly DiagnosticContext? _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoProjectMapper"/> class.
    /// </summary>
    /// <param name="projectMappings">Mapping of project IDs to display names.</param>
    /// <param name="diagnostics">Optional diagnostic context for recording failed resolutions.</param>
    public AzdoProjectMapper(FrozenDictionary<string, string> projectMappings, DiagnosticContext? diagnostics)
    {
        _projectMappings = projectMappings;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets only the display name for a project ID without resource context.
    /// </summary>
    /// <param name="projectId">The GUID of the project.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    public string? GetName(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return null;
        }

        return _projectMappings.TryGetValue(projectId, out var name) ? name : null;
    }

    /// <summary>
    /// Gets only the display name for a project ID with optional resource context.
    /// </summary>
    /// <param name="projectId">The GUID of the project.</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    /// <remarks>
    /// If a diagnostic context was provided and the project ID cannot be resolved,
    /// the failure is recorded with the resource address for troubleshooting.
    /// </remarks>
    public string? GetName(string projectId, string? resourceAddress)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return null;
        }

        var found = _projectMappings.TryGetValue(projectId, out var name);

        // Record failed resolution for diagnostics
        if (!found && _diagnostics != null && resourceAddress != null)
        {
            _diagnostics.FailedResolutions.Add(
                new FailedResolution(
                    FailedResolutionType.AzdoProject,
                    projectId,
                    resourceAddress,
                    "not found in mapping file"));
        }

        return found ? name : null;
    }

    /// <summary>
    /// Gets the formatted entity name for display (DisplayName [ID] or just ID if not mapped).
    /// </summary>
    /// <param name="projectId">The GUID of the project.</param>
    /// <returns>
    /// Display name followed by project ID in brackets if mapping exists,
    /// otherwise just the project ID.
    /// </returns>
    public string GetEntityName(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return projectId ?? string.Empty;
        }

        var displayName = GetName(projectId);
        return displayName is null
            ? projectId
            : $"{displayName} [{projectId}]";
    }
}
