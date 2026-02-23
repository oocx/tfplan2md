using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Formats Azure principal identifiers with mapped display names.
/// </summary>
/// <remarks>
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </remarks>
internal sealed class PrincipalIdFormatter : IValueFormatter
{
    /// <summary>
    /// Mapper used to resolve principal display names.
    /// </summary>
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrincipalIdFormatter"/> class.
    /// </summary>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    internal PrincipalIdFormatter(IPrincipalMapper principalMapper)
    {
        ArgumentNullException.ThrowIfNull(principalMapper);
        _principalMapper = principalMapper;
    }

    /// <summary>
    /// Attempts to format principal identifiers into mapped display names.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted principal name when resolved; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var displayName = _principalMapper.GetName(context.Value, null, null);
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Equals(context.Value, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var enriched = $"👤{ScribanHelpers.NonBreakingSpace}{displayName} ({context.Value})";
        return ScribanHelpers.FormatCodeTable(enriched);
    }
}
