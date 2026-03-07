using System.Collections.Frozen;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Abstract base class for Azure DevOps entity mappers that resolve IDs to display names.
/// </summary>
/// <remarks>
/// Consolidates the shared constructor, field declarations, both <c>GetName</c> overloads,
/// the <c>GetEntityName</c> default implementation, and the <c>RecordFailedResolution</c>
/// pattern. Concrete mappers only need to supply the <see cref="EntityType"/> property
/// and, where required, override <see cref="GetEntityName"/>.
/// Related feature: docs/features/111-code-simplification/specification.md (Finding 1.2).
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal abstract class AzdoEntityMapper
{
    /// <summary>
    /// Maps entity identifiers (GUIDs or descriptors) to human-readable display names.
    /// </summary>
    private readonly FrozenDictionary<string, string> _mappings;

    /// <summary>
    /// Optional sink for recording failed name resolution attempts.
    /// </summary>
    private readonly IDiagnosticSink? _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoEntityMapper"/> class.
    /// </summary>
    /// <param name="mappings">Mapping of entity identifiers to display names.</param>
    /// <param name="diagnostics">Optional diagnostic sink for recording failed resolutions.</param>
    protected AzdoEntityMapper(FrozenDictionary<string, string> mappings, IDiagnosticSink? diagnostics)
    {
        _mappings = mappings;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the <see cref="FailedResolutionType"/> used when recording diagnostics for this entity.
    /// </summary>
    /// <value>The entity-specific <see cref="FailedResolutionType"/> enum value.</value>
    protected abstract FailedResolutionType EntityType { get; }

    /// <summary>
    /// Gets the display name for an entity identifier without resource context.
    /// </summary>
    /// <param name="id">The entity identifier (GUID or descriptor).</param>
    /// <returns>The display name when found; otherwise <c>null</c>.</returns>
    public string? GetName(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _mappings.TryGetValue(id, out var name) ? name : null;
    }

    /// <summary>
    /// Gets the display name for an entity identifier with optional resource context for diagnostics.
    /// </summary>
    /// <param name="id">The entity identifier (GUID or descriptor).</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>The display name when found; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// If the entity identifier cannot be resolved and a <paramref name="resourceAddress"/> is provided,
    /// the failure is recorded in the diagnostic sink for troubleshooting.
    /// </remarks>
    public string? GetName(string id, string? resourceAddress)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var found = _mappings.TryGetValue(id, out var name);

        // Record failed resolution for diagnostics
        if (!found && _diagnostics != null && resourceAddress != null)
        {
            _diagnostics.RecordFailedResolution(
                new FailedResolution(
                    EntityType,
                    id,
                    resourceAddress,
                    "not found in mapping file"));
        }

        return found ? name : null;
    }

    /// <summary>
    /// Gets the formatted entity name for display, combining the display name and identifier.
    /// </summary>
    /// <param name="id">The entity identifier (GUID or descriptor).</param>
    /// <returns>
    /// Display name followed by the identifier in brackets when a mapping exists;
    /// otherwise just the identifier.
    /// </returns>
    public virtual string GetEntityName(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return id ?? string.Empty;
        }

        var displayName = GetName(id);
        return displayName is null
            ? id
            : $"{displayName} [{id}]";
    }
}
