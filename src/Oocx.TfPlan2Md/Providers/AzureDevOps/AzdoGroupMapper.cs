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
internal sealed class AzdoGroupMapper
{
    /// <summary>
    /// Maps Azure DevOps group descriptors to display names.
    /// </summary>
    private readonly FrozenDictionary<string, string> _groupMappings;

    /// <summary>
    /// Optional diagnostics for recording failed resolutions.
    /// </summary>
    private readonly DiagnosticContext? _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoGroupMapper"/> class.
    /// </summary>
    /// <param name="groupMappings">Mapping of group descriptors to display names.</param>
    /// <param name="diagnostics">Optional diagnostic context for recording failed resolutions.</param>
    public AzdoGroupMapper(FrozenDictionary<string, string> groupMappings, DiagnosticContext? diagnostics)
    {
        _groupMappings = groupMappings;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets only the display name for a group descriptor without resource context.
    /// </summary>
    /// <param name="groupDescriptor">The base64-encoded descriptor of the group.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    public string? GetName(string groupDescriptor)
    {
        if (string.IsNullOrWhiteSpace(groupDescriptor))
        {
            return null;
        }

        return _groupMappings.TryGetValue(groupDescriptor, out var name) ? name : null;
    }

    /// <summary>
    /// Gets only the display name for a group descriptor with optional resource context.
    /// </summary>
    /// <param name="groupDescriptor">The base64-encoded descriptor of the group.</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    /// <remarks>
    /// If a diagnostic context was provided and the group descriptor cannot be resolved,
    /// the failure is recorded with the resource address for troubleshooting.
    /// </remarks>
    public string? GetName(string groupDescriptor, string? resourceAddress)
    {
        if (string.IsNullOrWhiteSpace(groupDescriptor))
        {
            return null;
        }

        var found = _groupMappings.TryGetValue(groupDescriptor, out var name);

        // Record failed resolution for diagnostics
        if (!found && _diagnostics != null && resourceAddress != null)
        {
            _diagnostics.FailedResolutions.Add(
                new FailedResolution(
                    FailedResolutionType.AzdoGroup,
                    groupDescriptor,
                    resourceAddress,
                    "not found in mapping file"));
        }

        return found ? name : null;
    }

    /// <summary>
    /// Gets the formatted entity name for display (DisplayName [Descriptor] or just Descriptor if not mapped).
    /// </summary>
    /// <param name="groupDescriptor">The base64-encoded descriptor of the group.</param>
    /// <returns>
    /// Display name followed by full group descriptor in brackets if mapping exists,
    /// otherwise just the group descriptor. The full descriptor is preserved without truncation.
    /// </returns>
    public string GetEntityName(string groupDescriptor)
    {
        if (string.IsNullOrWhiteSpace(groupDescriptor))
        {
            return groupDescriptor ?? string.Empty;
        }

        var displayName = GetName(groupDescriptor);
        return displayName is null
            ? groupDescriptor
            : $"{displayName} [{groupDescriptor}]";
    }
}
