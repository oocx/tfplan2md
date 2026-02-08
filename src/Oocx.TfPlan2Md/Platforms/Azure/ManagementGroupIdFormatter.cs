using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Formats Azure management group identifiers with mapped display names and icons.
/// </summary>
/// <remarks>
/// Applies management group mapping enrichment for Azure provider attribute values.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </remarks>
internal sealed class ManagementGroupIdFormatter : IValueFormatter
{
    /// <summary>
    /// Mapper used to resolve management group display names.
    /// </summary>
    private readonly AzureEntityMapper _entityMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagementGroupIdFormatter"/> class.
    /// </summary>
    /// <param name="entityMapper">Mapper used to resolve management group display names.</param>
    internal ManagementGroupIdFormatter(AzureEntityMapper entityMapper)
    {
        ArgumentNullException.ThrowIfNull(entityMapper);
        _entityMapper = entityMapper;
    }

    /// <summary>
    /// Attempts to format management group identifiers into mapped display names with icons.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted management group label when handled; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var displayName = _entityMapper.GetManagementGroupDisplayName(context.Value);
        var label = string.IsNullOrWhiteSpace(displayName) ? context.Value : displayName;
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        var formattedLabel = AzureLabelFormatter.FormatManagementGroupLabel(label);
        return ScribanHelpers.FormatCodeTable(formattedLabel);
    }
}
