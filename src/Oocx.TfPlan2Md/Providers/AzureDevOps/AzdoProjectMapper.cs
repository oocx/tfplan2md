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
internal sealed class AzdoProjectMapper : AzdoEntityMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoProjectMapper"/> class.
    /// </summary>
    /// <param name="projectMappings">Mapping of project IDs to display names.</param>
    /// <param name="diagnostics">Optional diagnostic sink for recording failed resolutions.</param>
    public AzdoProjectMapper(FrozenDictionary<string, string> projectMappings, IDiagnosticSink? diagnostics)
        : base(projectMappings, diagnostics)
    {
    }

    /// <inheritdoc />
    protected override FailedResolutionType EntityType => FailedResolutionType.AzdoProject;
}
