using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Tests the pure collection element diff (TC-20 to TC-31, TC-42, TC-106, TC-115).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class CollectionElementDifferTests
{
    private static CollectionElementKey[] Keys(string text)
        => text.Select(c => new CollectionElementKey(JsonValueKind.String, c.ToString())).ToArray();

    private static CollectionDiffResult Diff(string before, string after)
        => CollectionElementDiffer.Diff(Keys(before), Keys(after));

    private static int Count(IReadOnlyList<CollectionElementStatus> statuses, CollectionElementStatus status)
        => statuses.Count(s => s == status);

    [Test]
    public void Diff_InsertionInMiddle_ReportsOneAddedNoRemoved()
    {
        var result = Diff("acd", "abcd");

        result.After.Should().Equal(
            CollectionElementStatus.Unchanged, CollectionElementStatus.Added, CollectionElementStatus.Unchanged, CollectionElementStatus.Unchanged);
        result.Before.Should().OnlyContain(s => s == CollectionElementStatus.Unchanged);
        result.OrderChanged.Should().BeFalse();
    }

    [Test]
    public void Diff_RemovalInMiddle_ReportsOneRemoved()
    {
        var result = Diff("abcd", "acd");

        Count(result.Before, CollectionElementStatus.Removed).Should().Be(1);
        result.Before[1].Should().Be(CollectionElementStatus.Removed);
        result.After.Should().OnlyContain(s => s == CollectionElementStatus.Unchanged);
        result.OrderChanged.Should().BeFalse();
    }

    [Test]
    public void Diff_ReplacedContent_ReportsRemovedAndAddedWithoutMoves()
    {
        var result = Diff("ab", "cd");

        Count(result.Before, CollectionElementStatus.Removed).Should().Be(2);
        Count(result.After, CollectionElementStatus.Added).Should().Be(2);
        Count(result.Before, CollectionElementStatus.Moved).Should().Be(0);
        result.OrderChanged.Should().BeFalse();
    }

    [Test]
    public void Diff_RemoveOneOfTwoEqualValues_RemovesExactlyOneOccurrence()
    {
        var result = Diff("aab", "ab");

        Count(result.Before, CollectionElementStatus.Removed).Should().Be(1);
        Count(result.Before, CollectionElementStatus.Unchanged).Should().Be(2);
        Count(result.After, CollectionElementStatus.Unchanged).Should().Be(2);
    }

    [Test]
    public void Diff_AddOneEqualOccurrence_AddsExactlyOneOccurrence()
    {
        var result = Diff("ab", "aab");

        Count(result.After, CollectionElementStatus.Added).Should().Be(1);
        Count(result.After, CollectionElementStatus.Unchanged).Should().Be(2);
        Count(result.Before, CollectionElementStatus.Removed).Should().Be(0);
    }

    [Test]
    public void Diff_SameOccurrencesReordered_ReportsOrderChangeOnly()
    {
        var result = Diff("ab", "ba");

        Count(result.Before, CollectionElementStatus.Removed).Should().Be(0);
        Count(result.After, CollectionElementStatus.Added).Should().Be(0);
        result.OrderChanged.Should().BeTrue();
        (Count(result.Before, CollectionElementStatus.Moved) + Count(result.After, CollectionElementStatus.Moved)).Should().BeGreaterThan(0);
    }

    [Test]
    public void Diff_ReorderPlusAdd_ReportsBothAddedAndOrderChange()
    {
        var result = Diff("abc", "cabd");

        Count(result.After, CollectionElementStatus.Added).Should().Be(1);
        result.After[3].Should().Be(CollectionElementStatus.Added);
        result.OrderChanged.Should().BeTrue();
    }

    [Test]
    public void Diff_DuplicatesReordered_ReportsOrderChangeWithoutAddOrRemove()
    {
        var result = Diff("aba", "aab");

        Count(result.Before, CollectionElementStatus.Removed).Should().Be(0);
        Count(result.After, CollectionElementStatus.Added).Should().Be(0);
        result.OrderChanged.Should().BeTrue();
    }

    [Test]
    [Arguments("ace", "abce")]
    [Arguments("abce", "ace")]
    [Arguments("0a1", "0a1b")]
    public void Diff_SortedSetMembershipChange_ReportsNoOrderChange(string before, string after)
    {
        Diff(before, after).OrderChanged.Should().BeFalse();
    }

    [Test]
    public void Diff_SortedGuidLists_ReportNoOrderChange()
    {
        var guids = new[] { "0a1b", "3c4d", "7e8f", "b1c2" };
        CollectionElementKey Key(string g) => new(JsonValueKind.String, g);

        var inserted = CollectionElementDiffer.Diff(
            [Key(guids[0]), Key(guids[2])],
            [Key(guids[0]), Key(guids[1]), Key(guids[2]), Key(guids[3])]);

        inserted.OrderChanged.Should().BeFalse();
        inserted.After.Count(s => s == CollectionElementStatus.Added).Should().Be(2);
    }

    [Test]
    public void Diff_StringOneVsNumberOne_AreDifferentElements()
    {
        var result = CollectionElementDiffer.Diff(
            [new(JsonValueKind.String, "1")],
            [new(JsonValueKind.Number, "1")]);

        result.Before.Should().Equal(CollectionElementStatus.Removed);
        result.After.Should().Equal(CollectionElementStatus.Added);
    }

    [Test]
    public void Diff_EqualKeys_AreUnchangedRegardlessOfAnyDisplayName()
    {
        // The differ takes raw values only, so display names cannot take part in equality.
        var result = CollectionElementDiffer.Diff(
            [new(JsonValueKind.String, "guid-1")],
            [new(JsonValueKind.String, "guid-1")]);

        result.Before.Should().Equal(CollectionElementStatus.Unchanged);
        result.After.Should().Equal(CollectionElementStatus.Unchanged);
    }

    [Test]
    public void Diff_EmptyToEmpty_ReportsNoChange()
    {
        var result = Diff(string.Empty, string.Empty);

        result.Before.Should().BeEmpty();
        result.After.Should().BeEmpty();
        result.OrderChanged.Should().BeFalse();
    }

    [Test]
    public void Diff_ExceedsLcsCellCap_FallsBackConservatively()
    {
        var before = Enumerable.Range(0, 5000).Select(i => new CollectionElementKey(JsonValueKind.String, $"v{i}")).ToList();
        var after = before.ToList();
        after.RemoveAt(4900);
        after.RemoveAt(100);
        after.Add(new CollectionElementKey(JsonValueKind.String, "new"));

        var stopwatch = Stopwatch.StartNew();
        var result = CollectionElementDiffer.Diff(before, after);
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
        Count(result.Before, CollectionElementStatus.Removed).Should().Be(2);
        Count(result.After, CollectionElementStatus.Added).Should().Be(1);
        Count(result.Before, CollectionElementStatus.Unchanged).Should().Be(4998);
        result.OrderChanged.Should().BeFalse("retained keys keep their relative order");
    }

    [Test]
    public void Diff_ExceedsLcsCellCapWithInsertionOnly_ReportsNoOrderChange()
    {
        var before = Enumerable.Range(0, 5000).Select(i => new CollectionElementKey(JsonValueKind.String, $"v{i:D5}")).ToList();
        var after = before.ToList();
        after.Insert(4900, new CollectionElementKey(JsonValueKind.String, "x"));
        after.Insert(100, new CollectionElementKey(JsonValueKind.String, "y"));

        var result = CollectionElementDiffer.Diff(before, after);

        Count(result.After, CollectionElementStatus.Added).Should().Be(2);
        result.OrderChanged.Should().BeFalse();
    }

    [Test]
    public void Diff_ExceedsLcsCellCapWithSwappedElements_ReportsOrderChange()
    {
        var before = Enumerable.Range(0, 5000).Select(i => new CollectionElementKey(JsonValueKind.String, $"v{i:D5}")).ToList();
        var after = before.ToList();
        (after[100], after[4900]) = (after[4900], after[100]);

        var result = CollectionElementDiffer.Diff(before, after);

        Count(result.After, CollectionElementStatus.Added).Should().Be(0);
        Count(result.Before, CollectionElementStatus.Removed).Should().Be(0);
        result.OrderChanged.Should().BeTrue();
    }

    [Test]
    public void Diff_SameInputTwice_YieldsIdenticalStatuses()
    {
        var first = Diff("abcab", "bacba");
        var second = Diff("abcab", "bacba");

        first.Before.Should().Equal(second.Before);
        first.After.Should().Equal(second.After);
        first.OrderChanged.Should().Be(second.OrderChanged);
    }
}
