using System;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Shared static helper for Azure DevOps entity ID formatters.
/// </summary>
/// <remarks>
/// Encapsulates the common body shared by all four AzDO formatter classes:
/// guard empty value, call the <c>getName</c> delegate, guard raw == display, and format
/// with an icon plus <see cref="MarkdownHelpers.FormatCodeTable"/>.
/// Each formatter delegates here, passing only its mapper and icon.
/// Related feature: docs/features/111-code-simplification/specification.md (Finding 1.3).
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal static class AzdoFormatterHelper
{
    /// <summary>
    /// Attempts to format an Azure DevOps entity identifier into a mapped display name.
    /// </summary>
    /// <param name="rawValue">The raw identifier value from the Terraform attribute.</param>
    /// <param name="getName">A delegate that resolves the identifier to a display name.</param>
    /// <param name="icon">The emoji icon to prepend to the formatted value.</param>
    /// <returns>
    /// A markdown-safe code-table formatted string combining the icon, display name, and raw
    /// identifier when a mapping is found; otherwise <c>null</c>.
    /// </returns>
    internal static string? TryFormat(string? rawValue, Func<string, string?> getName, string icon)
    {
        ArgumentNullException.ThrowIfNull(getName);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var displayName = getName(rawValue);
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Equals(rawValue, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var enriched = $"{icon}\u00A0{displayName} ({rawValue})";
        return MarkdownHelpers.FormatCodeTable(enriched);
    }
}
