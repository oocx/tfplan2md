using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Diff computation helpers used by diff rendering routines.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Builds a line-level diff between two string arrays using LCS pairing.
    /// </summary>
    /// <param name="before">Original lines.</param>
    /// <param name="after">Updated lines.</param>
    /// <returns>Ordered list of diff entries.</returns>
    private static List<DiffEntry> BuildLineDiff(string[] before, string[] after)
    {
        var pairs = ComputeLcsPairs(before, after);
        var result = new List<DiffEntry>();

        var beforeIndex = 0;
        var afterIndex = 0;

        foreach (var pair in pairs)
        {
            while (beforeIndex < pair.BeforeIndex)
            {
                result.Add(new DiffEntry(DiffKind.Removed, before[beforeIndex]));
                beforeIndex++;
            }

            while (afterIndex < pair.AfterIndex)
            {
                result.Add(new DiffEntry(DiffKind.Added, after[afterIndex]));
                afterIndex++;
            }

            result.Add(new DiffEntry(DiffKind.Unchanged, before[pair.BeforeIndex]));
            beforeIndex++;
            afterIndex++;
        }

        while (beforeIndex < before.Length)
        {
            result.Add(new DiffEntry(DiffKind.Removed, before[beforeIndex]));
            beforeIndex++;
        }

        while (afterIndex < after.Length)
        {
            result.Add(new DiffEntry(DiffKind.Added, after[afterIndex]));
            afterIndex++;
        }

        return result;
    }

    /// <summary>
    /// Computes longest common subsequence pairs for two line arrays.
    /// </summary>
    /// <param name="before">Original lines.</param>
    /// <param name="after">Updated lines.</param>
    /// <returns>List of matching index pairs.</returns>
    private static List<LcsPair> ComputeLcsPairs(string[] before, string[] after)
    {
        var m = before.Length;
        var n = after.Length;
        var lengths = new int[m + 1, n + 1];

        for (var i = m - 1; i >= 0; i--)
        {
            for (var j = n - 1; j >= 0; j--)
            {
                if (string.Equals(before[i], after[j], StringComparison.Ordinal))
                {
                    lengths[i, j] = lengths[i + 1, j + 1] + 1;
                }
                else
                {
                    lengths[i, j] = Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
                }
            }
        }

        var pairs = new List<LcsPair>();
        var x = 0;
        var y = 0;
        while (x < m && y < n)
        {
            if (string.Equals(before[x], after[y], StringComparison.Ordinal))
            {
                pairs.Add(new LcsPair(x, y));
                x++;
                y++;
            }
            else if (lengths[x + 1, y] >= lengths[x, y + 1])
            {
                x++;
            }
            else
            {
                y++;
            }
        }

        return pairs;
    }

    /// <summary>
    /// Computes longest common subsequence pairs for two single-line strings.
    /// </summary>
    /// <param name="before">Original string.</param>
    /// <param name="after">Updated string.</param>
    /// <returns>List of matching character index pairs.</returns>
    private static List<LcsPair> ComputeLcsPairs(string before, string after)
    {
        var m = before.Length;
        var n = after.Length;
        var lengths = new int[m + 1, n + 1];

        for (var i = m - 1; i >= 0; i--)
        {
            for (var j = n - 1; j >= 0; j--)
            {
                if (before[i] == after[j])
                {
                    lengths[i, j] = lengths[i + 1, j + 1] + 1;
                }
                else
                {
                    lengths[i, j] = Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
                }
            }
        }

        var pairs = new List<LcsPair>();
        var x = 0;
        var y = 0;
        while (x < m && y < n)
        {
            if (before[x] == after[y])
            {
                pairs.Add(new LcsPair(x, y));
                x++;
                y++;
            }
            else if (lengths[x + 1, y] >= lengths[x, y + 1])
            {
                x++;
            }
            else
            {
                y++;
            }
        }

        return pairs;
    }

    /// <summary>
    /// Builds a boolean mask indicating shared positions within a collection of indices.
    /// </summary>
    /// <param name="length">Length of the mask.</param>
    /// <param name="indices">Indices to mark as common.</param>
    /// <returns>Boolean mask with common positions marked true.</returns>
    private static bool[] BuildCommonMask(int length, IEnumerable<int> indices)
    {
        var mask = new bool[length];
        foreach (var index in indices)
        {
            if (index >= 0 && index < length)
            {
                mask[index] = true;
            }
        }

        return mask;
    }

    /// <summary>
    /// Represents a diff entry for a single line with its change kind.
    /// </summary>
    /// <param name="Kind">Classification of the diff.</param>
    /// <param name="Text">Content of the line.</param>
    private readonly record struct DiffEntry(DiffKind Kind, string Text);

    /// <summary>
    /// Encapsulates a single attribute change for large value summaries.
    /// </summary>
    /// <param name="Name">Attribute name.</param>
    /// <param name="Before">Original value.</param>
    /// <param name="After">Updated value.</param>
    private readonly record struct AttributeChangeInfo(string Name, string? Before, string? After);

    /// <summary>
    /// Describes the kind of difference detected between two values.
    /// </summary>
    private enum DiffKind
    {
        /// <summary>
        /// Values are unchanged.
        /// </summary>
        Unchanged,

        /// <summary>
        /// Value is removed.
        /// </summary>
        Removed,

        /// <summary>
        /// Value is added.
        /// </summary>
        Added
    }

    /// <summary>
    /// Represents matching index pairs within two sequences as part of LCS calculation.
    /// </summary>
    /// <param name="BeforeIndex">Index in the original sequence.</param>
    /// <param name="AfterIndex">Index in the updated sequence.</param>
    private readonly record struct LcsPair(int BeforeIndex, int AfterIndex);
}
