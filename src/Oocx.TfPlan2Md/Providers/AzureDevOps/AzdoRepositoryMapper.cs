using System.Collections.Frozen;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Maps Azure DevOps repository IDs to display names.
/// </summary>
/// <remarks>
/// Azure DevOps repositories are identified by unique GUIDs. This mapper resolves
/// repository IDs to human-readable names for improved report readability.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </remarks>
internal sealed class AzdoRepositoryMapper : AzdoEntityMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoRepositoryMapper"/> class.
    /// </summary>
    /// <param name="repositoryMappings">Mapping of repository IDs to display names.</param>
    /// <param name="diagnostics">Optional diagnostic sink for recording failed resolutions.</param>
    public AzdoRepositoryMapper(FrozenDictionary<string, string> repositoryMappings, IDiagnosticSink? diagnostics)
        : base(repositoryMappings, diagnostics)
    {
    }

    /// <inheritdoc />
    protected override FailedResolutionType EntityType => FailedResolutionType.AzdoRepository;

    /// <summary>
    /// Gets the formatted repository name prefixed with the repository icon.
    /// </summary>
    /// <param name="id">The GUID of the repository.</param>
    /// <returns>
    /// Repository icon followed by display name and repository ID in parentheses when a mapping exists;
    /// otherwise the repository icon followed by just the identifier.
    /// </returns>
    public override string GetEntityName(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return id ?? string.Empty;
        }

        var displayName = GetName(id);
        return displayName is null
            ? $"🗃️ {id}"
            : $"🗃️ {displayName} ({id})";
    }
}
