using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Formats Azure DevOps group descriptors with mapped display names.
/// </summary>
/// <remarks>
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal sealed class AzdoGroupDescriptorFormatter : IValueFormatter
{
    /// <summary>
    /// Mapper used to resolve Azure DevOps group display names.
    /// </summary>
    private readonly AzdoGroupMapper _groupMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoGroupDescriptorFormatter"/> class.
    /// </summary>
    /// <param name="groupMapper">Mapper used for group name resolution.</param>
    internal AzdoGroupDescriptorFormatter(AzdoGroupMapper groupMapper)
    {
        ArgumentNullException.ThrowIfNull(groupMapper);
        _groupMapper = groupMapper;
    }

    /// <summary>
    /// Attempts to format Azure DevOps group descriptors into mapped display names.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted group name when resolved; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var displayName = _groupMapper.GetName(context.Value);
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Equals(context.Value, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var enriched = $"👥\u00A0{displayName} ({context.Value})";
        return ScribanHelpers.FormatCodeTable(enriched);
    }
}
