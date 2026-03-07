using System.Collections.Frozen;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Maps Azure DevOps group descriptors to display names.
/// </summary>
/// <remarks>
/// Azure DevOps groups are identified by base64-encoded descriptors which can be
/// very long (100+ characters). This mapper resolves descriptors to human-readable
/// team/group names for improved report readability.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal sealed class AzdoGroupMapper : AzdoEntityMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoGroupMapper"/> class.
    /// </summary>
    /// <param name="groupMappings">Mapping of group descriptors to display names.</param>
    /// <param name="diagnostics">Optional diagnostic sink for recording failed resolutions.</param>
    public AzdoGroupMapper(FrozenDictionary<string, string> groupMappings, IDiagnosticSink? diagnostics)
        : base(groupMappings, diagnostics)
    {
    }

    /// <inheritdoc />
    protected override FailedResolutionType EntityType => FailedResolutionType.AzdoGroup;
}
