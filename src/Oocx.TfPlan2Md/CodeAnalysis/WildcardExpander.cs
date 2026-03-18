using System;
using System.Collections.Generic;
using System.IO;

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

        if (pattern.Contains("**"))
        {
            var root = ResolveRecursiveRoot(pattern);
            var filePattern = Path.GetFileName(pattern);
            foreach (var file in Directory.EnumerateFiles(root, filePattern, SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
        else
        {
            var directory = Path.GetDirectoryName(pattern);
            var filePattern = Path.GetFileName(pattern);
            var root = ResolveEnumerationRoot(directory);
            foreach (var file in Directory.EnumerateFiles(root, filePattern, SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }
    }

    /// <summary>
    /// Resolves the root directory for recursive patterns containing **.
    /// </summary>
    /// <param name="pattern">The pattern containing a recursive segment.</param>
    /// <returns>The resolved root directory.</returns>
    private static string ResolveRecursiveRoot(string pattern)
    {
        var recursiveIndex = pattern.IndexOf("**", StringComparison.Ordinal);
        if (recursiveIndex <= 0)
        {
            return ResolveEnumerationRoot(null);
        }

        var rootCandidate = pattern[..recursiveIndex].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return ResolveEnumerationRoot(rootCandidate);
    }

    /// <summary>
    /// Resolves and validates an enumeration root before it reaches filesystem APIs.
    /// </summary>
    /// <param name="rootCandidate">The directory portion derived from the user-supplied pattern.</param>
    /// <returns>A canonical full path that is safe to enumerate.</returns>
    /// <exception cref="ArgumentException">Thrown when the root contains parent traversal segments.</exception>
    private static string ResolveEnumerationRoot(string? rootCandidate)
    {
        var root = string.IsNullOrWhiteSpace(rootCandidate)
            ? Directory.GetCurrentDirectory()
            : rootCandidate;

        ValidateRootSegments(root);
        return Path.GetFullPath(root);
    }

    /// <summary>
    /// Rejects explicit parent traversal segments so wildcard roots cannot escape via relative navigation.
    /// </summary>
    /// <param name="root">The root path to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the root includes a <c>..</c> segment.</exception>
    private static void ValidateRootSegments(string root)
    {
        var normalizedRoot = root
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar);
        var parentSegment = $"..{Path.DirectorySeparatorChar}";
        var trailingParentSegment = $"{Path.DirectorySeparatorChar}..";
        var embeddedParentSegment = $"{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}";

        if (string.Equals(normalizedRoot, "..", StringComparison.Ordinal) ||
            normalizedRoot.StartsWith(parentSegment, StringComparison.Ordinal) ||
            normalizedRoot.EndsWith(trailingParentSegment, StringComparison.Ordinal) ||
            normalizedRoot.Contains(embeddedParentSegment, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Wildcard root '{root}' must not contain parent traversal segments.",
                nameof(root));
        }
    }
}
