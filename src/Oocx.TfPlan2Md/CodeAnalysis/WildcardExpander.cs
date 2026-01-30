using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Expands file patterns (wildcards) to matching file paths.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class WildcardExpander
{
    /// <summary>
    /// Expands a set of file patterns (e.g., *.sarif, path/**/*.sarif) to matching files.
    /// </summary>
    /// <param name="patterns">The file patterns to expand.</param>
    /// <returns>Sorted list of matching file paths.</returns>
    public static IReadOnlyList<string> Expand(IEnumerable<string> patterns)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in patterns)
        {
            foreach (var file in ExpandPattern(pattern))
            {
                files.Add(Path.GetFullPath(file));
            }
        }
        return files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Expands a single file pattern to matching files.
    /// </summary>
    /// <param name="pattern">The file pattern (supports *, **, ? wildcards).</param>
    /// <returns>Matching file paths.</returns>
    private static IEnumerable<string> ExpandPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            yield break;
        }

        var directory = Path.GetDirectoryName(pattern);
        var filePattern = Path.GetFileName(pattern);
        if (pattern.Contains("**"))
        {
            var root = directory ?? Directory.GetCurrentDirectory();
            foreach (var file in Directory.EnumerateFiles(root, filePattern, SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
        else
        {
            var root = directory ?? Directory.GetCurrentDirectory();
            foreach (var file in Directory.EnumerateFiles(root, filePattern, SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }
    }
}
