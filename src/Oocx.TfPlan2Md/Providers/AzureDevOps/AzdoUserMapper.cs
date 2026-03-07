using System.Collections.Frozen;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Maps Azure DevOps user IDs to display names.
/// </summary>
/// <remarks>
/// Azure DevOps users are identified by unique GUIDs. This mapper resolves
/// user IDs to human-readable names for improved report readability.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal sealed class AzdoUserMapper : AzdoEntityMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoUserMapper"/> class.
    /// </summary>
    /// <param name="userMappings">Mapping of user IDs to display names.</param>
    /// <param name="diagnostics">Optional diagnostic sink for recording failed resolutions.</param>
    public AzdoUserMapper(FrozenDictionary<string, string> userMappings, IDiagnosticSink? diagnostics)
        : base(userMappings, diagnostics)
    {
    }

    /// <inheritdoc />
    protected override FailedResolutionType EntityType => FailedResolutionType.AzdoUser;
}
