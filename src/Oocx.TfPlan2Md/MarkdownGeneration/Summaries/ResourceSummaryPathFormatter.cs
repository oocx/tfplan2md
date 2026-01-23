using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

/// <summary>
/// Formats replacement path segments for resource change summaries.
/// </summary>
/// <remarks>
/// Extracted from ResourceSummaryBuilder to improve maintainability.
/// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md.
/// </remarks>
internal static class ResourceSummaryPathFormatter
{
    /// <summary>
    /// Formats a replacement path from the Terraform plan into a user-friendly attribute path.
    /// </summary>
    /// <param name="path">The path segments from the plan (mix of strings and integers).</param>
    /// <returns>A formatted path string (e.g., "tags.Name" or "network_interface[0]"), or null if the path is empty or invalid.</returns>
    public static string? FormatReplacePath(IReadOnlyList<object> path)
    {
        if (path.Count == 0)
        {
            return null;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < path.Count; i++)
        {
            var segment = FormatPathSegment(path[i]);
            if (segment is null)
            {
                continue;
            }

            if (i == 0)
            {
                builder.Append(segment);
                continue;
            }

            if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                builder.Append('[');
                builder.Append(segment);
                builder.Append(']');
            }
            else if (segment.Length > 0 && segment[0] == '[')
            {
                builder.Append(segment);
            }
            else
            {
                builder.Append('.');
                builder.Append(segment);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Filters out "name" and "context" keys from replacement path display.
    /// </summary>
    /// <param name="key">The attribute key to check.</param>
    /// <returns>True if the key is "name" or "context" (case-insensitive); otherwise false.</returns>
    public static bool IsNameOrContextKey(string key)
    {
        return key.Equals("name", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("context", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FormatPathSegment(object? segment)
    {
        return segment switch
        {
            string s when IsNameOrContextKey(s) => null,
            string s => s,
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            _ => null
        };
    }
}
