using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Formats Azure tenant identifiers with mapped display names and icons.
/// </summary>
/// <remarks>
/// Applies tenant mapping enrichment for Azure provider attribute values.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </remarks>
internal sealed class TenantIdFormatter : IValueFormatter
{
    /// <summary>
    /// Mapper used to resolve tenant display names.
    /// </summary>
    private readonly AzureEntityMapper _entityMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIdFormatter"/> class.
    /// </summary>
    /// <param name="entityMapper">Mapper used to resolve tenant display names.</param>
    internal TenantIdFormatter(AzureEntityMapper entityMapper)
    {
        ArgumentNullException.ThrowIfNull(entityMapper);
        _entityMapper = entityMapper;
    }

    /// <summary>
    /// Attempts to format tenant identifiers into mapped display names with icons.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted tenant label when resolved; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var displayName = _entityMapper.GetTenantDisplayName(context.Value);
        if (string.IsNullOrWhiteSpace(displayName)
            || displayName.Equals(context.Value, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var label = ScribanHelpers.FormatCodeTable(displayName);
        return AzureLabelFormatter.FormatTenantLabel(label);
    }
}
