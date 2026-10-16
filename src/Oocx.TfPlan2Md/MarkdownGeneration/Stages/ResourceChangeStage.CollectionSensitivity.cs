using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Sensitivity reading and masked-entry tokenization for compact primitive collection rows of
/// <see cref="ResourceChangeStage"/>. Masked values are replaced by opaque tokens here, before any element text or
/// comparison key leaves this file.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md (section 5a).
/// </summary>
internal sealed partial class ResourceChangeStage
{
    /// <summary>
    /// The sensitivity of one collection: whether it is marked as a whole, whether anything in it is marked, and the
    /// entry indices each side marks.
    /// </summary>
    private sealed record CollectionSensitivity(bool Whole, bool Any, HashSet<int> BeforeMarks, HashSet<int> AfterMarks);

    /// <summary>
    /// Reads the sensitivity of a collection. A mark beneath the path that is not a plain entry index cannot be
    /// mapped to one entry, so the whole collection is then treated as sensitive rather than risking that a marked
    /// value is shown.
    /// </summary>
    private static CollectionSensitivity ReadSensitivity(
        string path,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
    {
        var beforeMarks = ReadEntryMarks(path, beforeSensitive);
        var afterMarks = ReadEntryMarks(path, afterSensitive);
        if (beforeMarks is null || afterMarks is null || IsSensitiveAttribute(path, beforeSensitive, afterSensitive))
        {
            return new CollectionSensitivity(true, true, [], []);
        }

        return new CollectionSensitivity(false, beforeMarks.Count > 0 || afterMarks.Count > 0, beforeMarks, afterMarks);
    }

    /// <summary>
    /// Reads the entry indices that one side's sensitivity map marks as sensitive beneath a collection path.
    /// </summary>
    /// <param name="path">The collection path.</param>
    /// <param name="sensitivity">One side's flattened sensitivity map.</param>
    /// <returns>The marked indices, or <c>null</c> when a mark beneath the path is not a plain entry index.</returns>
    private static HashSet<int>? ReadEntryMarks(string path, Dictionary<string, string?> sensitivity)
    {
        var prefix = path + "[";
        var marks = new HashSet<int>();
        foreach (var entry in sensitivity)
        {
            if (entry.Value != "true" || !entry.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var rest = entry.Key.AsSpan(prefix.Length);
            if (rest.Length < 2
                || rest[^1] != ']'
                || !int.TryParse(rest[..^1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var index))
            {
                return null;
            }

            marks.Add(index);
        }

        return marks;
    }

    /// <summary>
    /// One collection element ready for diffing. A masked element carries no value and an opaque key.
    /// </summary>
    private readonly record struct PreparedElement(string? Value, CollectionElementKey Key, bool IsMasked);

    /// <summary>
    /// Reads both sides and replaces each masked element by an opaque comparison token. Tokens number the distinct
    /// values of masked elements across both sides, so masked-to-masked equality across sides is value equality,
    /// while a masked element never equals a visible one. The token map lives only in this method.
    /// </summary>
    private static (List<PreparedElement> Before, List<PreparedElement> After) PrepareElements(
        DetectedCollection collection,
        HashSet<int> beforeMarks,
        HashSet<int> afterMarks)
    {
        var tokens = new Dictionary<CollectionElementKey, string>();
        List<PreparedElement> Prepare(JsonElement? side, HashSet<int> marks)
            => CollectionValues.ReadElements(side).Select((element, index) =>
            {
                if (!marks.Contains(index))
                {
                    return new PreparedElement(element.Value, element.Key, false);
                }

                if (!tokens.TryGetValue(element.Key, out var token))
                {
                    token = "#" + tokens.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    tokens[element.Key] = token;
                }

                return new PreparedElement(null, new CollectionElementKey(JsonValueKind.Undefined, token, Masked: true), true);
            }).ToList();

        var before = Prepare(collection.Before, beforeMarks);
        var after = Prepare(collection.After, afterMarks);
        return (before, after);
    }
}
