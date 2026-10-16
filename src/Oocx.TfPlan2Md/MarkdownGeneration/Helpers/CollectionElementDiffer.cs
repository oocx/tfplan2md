using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Type-accurate identity of a collection element: whether it is masked, the JSON kind and the raw text.
/// Display names never take part, so <c>"1"</c> and <c>1</c> are different elements. A masked key carries an
/// opaque token instead of the value and never equals a non-masked key, whatever the texts
/// (docs/features/148-compact-collection-diffs/specification.md, rule 4).
/// </summary>
/// <param name="Kind">The JSON value kind; <see cref="JsonValueKind.Undefined"/> for a masked element.</param>
/// <param name="Raw">The raw text of the value, or the opaque token of a masked element.</param>
/// <param name="Masked">Whether the element is masked as sensitive.</param>
internal readonly record struct CollectionElementKey(JsonValueKind Kind, string Raw, bool Masked = false);

/// <summary>
/// The outcome of diffing two element sequences.
/// </summary>
/// <param name="Before">Status of each before-side occurrence.</param>
/// <param name="After">Status of each after-side occurrence.</param>
/// <param name="OrderChanged">Whether retained occurrences changed relative order.</param>
internal sealed record CollectionDiffResult(
    IReadOnlyList<CollectionElementStatus> Before,
    IReadOnlyList<CollectionElementStatus> After,
    bool OrderChanged);

/// <summary>
/// Pure diff over collection element keys: prefix/suffix trim, LCS, then multiset matching of the remainder.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md (section 4).
/// </summary>
internal static class CollectionElementDiffer
{
    /// <summary>
    /// Diffs two element sequences. Duplicates keep their multiplicity; an order-only change yields
    /// <see cref="CollectionElementStatus.Moved"/> occurrences and no removed/added pair.
    /// </summary>
    /// <param name="before">Element keys before the change.</param>
    /// <param name="after">Element keys after the change.</param>
    /// <returns>Statuses for each occurrence and the order-change flag.</returns>
    internal static CollectionDiffResult Diff(IReadOnlyList<CollectionElementKey> before, IReadOnlyList<CollectionElementKey> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var beforeStatus = new CollectionElementStatus?[before.Count];
        var afterStatus = new CollectionElementStatus?[after.Count];

        var start = 0;
        while (start < before.Count && start < after.Count && before[start] == after[start])
        {
            beforeStatus[start] = CollectionElementStatus.Unchanged;
            afterStatus[start] = CollectionElementStatus.Unchanged;
            start++;
        }

        var beforeEnd = before.Count;
        var afterEnd = after.Count;
        while (beforeEnd > start && afterEnd > start && before[beforeEnd - 1] == after[afterEnd - 1])
        {
            beforeStatus[beforeEnd - 1] = CollectionElementStatus.Unchanged;
            afterStatus[afterEnd - 1] = CollectionElementStatus.Unchanged;
            beforeEnd--;
            afterEnd--;
        }

        var beforeMiddle = Enumerable.Range(start, beforeEnd - start).ToArray();
        var afterMiddle = Enumerable.Range(start, afterEnd - start).ToArray();

        var withinCap = (long)beforeMiddle.Length * afterMiddle.Length <= MarkdownHelpers.MaxLcsMatrixCells;
        if (withinCap)
        {
            MarkLcsAsUnchanged(before, after, beforeMiddle, afterMiddle, beforeStatus, afterStatus);
        }

        var moved = MatchRemainingByKey(before, after, beforeMiddle, afterMiddle, beforeStatus, afterStatus, withinCap);

        var orderChanged = withinCap ? moved > 0 : RetainedSequencesDiffer(before, after, beforeStatus, afterStatus);

        return new CollectionDiffResult(
            Finish(beforeStatus, CollectionElementStatus.Removed),
            Finish(afterStatus, CollectionElementStatus.Added),
            orderChanged);
    }

    /// <summary>
    /// Marks the longest common subsequence of the trimmed middle as unchanged.
    /// </summary>
    private static void MarkLcsAsUnchanged(
        IReadOnlyList<CollectionElementKey> before,
        IReadOnlyList<CollectionElementKey> after,
        int[] beforeMiddle,
        int[] afterMiddle,
        CollectionElementStatus?[] beforeStatus,
        CollectionElementStatus?[] afterStatus)
    {
        if (beforeMiddle.Length == 0 || afterMiddle.Length == 0)
        {
            return;
        }

        var ids = new Dictionary<CollectionElementKey, string>();
        string Id(CollectionElementKey key)
        {
            if (!ids.TryGetValue(key, out var id))
            {
                id = ids.Count.ToString(CultureInfo.InvariantCulture);
                ids[key] = id;
            }

            return id;
        }

        var beforeIds = beforeMiddle.Select(index => Id(before[index])).ToArray();
        var afterIds = afterMiddle.Select(index => Id(after[index])).ToArray();

        foreach (var pair in MarkdownHelpers.ComputeLcsPairs(beforeIds, afterIds))
        {
            beforeStatus[beforeMiddle[pair.BeforeIndex]] = CollectionElementStatus.Unchanged;
            afterStatus[afterMiddle[pair.AfterIndex]] = CollectionElementStatus.Unchanged;
        }
    }

    /// <summary>
    /// Matches remaining occurrences by key in positional order. Within the cap the matches are
    /// <see cref="CollectionElementStatus.Moved"/>; above the cap they are retained as unchanged.
    /// </summary>
    /// <returns>The number of matched pairs.</returns>
    private static int MatchRemainingByKey(
        IReadOnlyList<CollectionElementKey> before,
        IReadOnlyList<CollectionElementKey> after,
        int[] beforeMiddle,
        int[] afterMiddle,
        CollectionElementStatus?[] beforeStatus,
        CollectionElementStatus?[] afterStatus,
        bool withinCap)
    {
        var pending = new Dictionary<CollectionElementKey, Queue<int>>();
        foreach (var index in afterMiddle.Where(index => afterStatus[index] is null))
        {
            if (!pending.TryGetValue(after[index], out var queue))
            {
                queue = new Queue<int>();
                pending[after[index]] = queue;
            }

            queue.Enqueue(index);
        }

        var status = withinCap ? CollectionElementStatus.Moved : CollectionElementStatus.Unchanged;
        var matches = 0;
        foreach (var index in beforeMiddle.Where(index => beforeStatus[index] is null))
        {
            if (pending.TryGetValue(before[index], out var queue) && queue.Count > 0)
            {
                beforeStatus[index] = status;
                afterStatus[queue.Dequeue()] = status;
                matches++;
            }
        }

        return matches;
    }

    /// <summary>
    /// Above the cap the retained occurrences are matched without ordering information, so the
    /// order counts as changed only when the retained key sequences differ.
    /// </summary>
    private static bool RetainedSequencesDiffer(
        IReadOnlyList<CollectionElementKey> before,
        IReadOnlyList<CollectionElementKey> after,
        CollectionElementStatus?[] beforeStatus,
        CollectionElementStatus?[] afterStatus)
    {
        var retainedBefore = before.Where((_, index) => beforeStatus[index] == CollectionElementStatus.Unchanged);
        var retainedAfter = after.Where((_, index) => afterStatus[index] == CollectionElementStatus.Unchanged);
        return !retainedBefore.SequenceEqual(retainedAfter);
    }

    /// <summary>
    /// Replaces unassigned statuses with the side's default for unmatched occurrences.
    /// </summary>
    private static CollectionElementStatus[] Finish(CollectionElementStatus?[] statuses, CollectionElementStatus unmatched)
        => statuses.Select(status => status ?? unmatched).ToArray();
}
