using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Orders attribute paths ordinally, except that numeric <c>[n]</c> index segments compare by numeric value.
/// So <c>a[2]</c> sorts before <c>a[10]</c>.
/// Related feature: docs/features/148-compact-collection-diffs/specification.md.
/// </summary>
internal sealed class AttributePathComparer : IComparer<string>
{
    /// <summary>
    /// Gets the shared comparer instance.
    /// </summary>
    internal static AttributePathComparer Instance { get; } = new();

    /// <inheritdoc />
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var i = 0;
        var j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (x[i] == '[' && y[j] == '['
                && TryReadIndex(x, i, out var xDigitsStart, out var xDigitsEnd)
                && TryReadIndex(y, j, out var yDigitsStart, out var yDigitsEnd))
            {
                var numeric = CompareDigits(x.AsSpan(xDigitsStart, xDigitsEnd - xDigitsStart), y.AsSpan(yDigitsStart, yDigitsEnd - yDigitsStart));
                if (numeric != 0)
                {
                    return numeric;
                }

                i = xDigitsEnd + 1;
                j = yDigitsEnd + 1;
                continue;
            }

            if (x[i] != y[j])
            {
                return x[i] < y[j] ? -1 : 1;
            }

            i++;
            j++;
        }

        var remaining = (x.Length - i).CompareTo(y.Length - j);
        return remaining != 0 ? remaining : string.CompareOrdinal(x, y);
    }

    /// <summary>
    /// Tries to read a <c>[digits]</c> token starting at the given position.
    /// </summary>
    private static bool TryReadIndex(string path, int start, out int digitsStart, out int digitsEnd)
    {
        digitsStart = start + 1;
        digitsEnd = digitsStart;
        while (digitsEnd < path.Length && path[digitsEnd] >= '0' && path[digitsEnd] <= '9')
        {
            digitsEnd++;
        }

        return digitsEnd > digitsStart && digitsEnd < path.Length && path[digitsEnd] == ']';
    }

    /// <summary>
    /// Compares two digit runs by numeric value without overflow.
    /// </summary>
    private static int CompareDigits(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        left = left.TrimStart('0');
        right = right.TrimStart('0');
        if (left.Length != right.Length)
        {
            return left.Length < right.Length ? -1 : 1;
        }

        return Math.Sign(left.CompareTo(right, StringComparison.Ordinal));
    }
}
