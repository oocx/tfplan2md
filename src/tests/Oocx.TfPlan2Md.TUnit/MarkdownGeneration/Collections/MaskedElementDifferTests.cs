using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Diff-level tests for masked element keys (TC-57, TC-59, TC-121, TC-122 at diff level).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class MaskedElementDifferTests
{
    private static CollectionElementKey Visible(string raw) => new(JsonValueKind.String, raw);

    private static CollectionElementKey Masked(string token) => new(JsonValueKind.Undefined, token, Masked: true);

    [Test]
    public void Diff_MaskedKeyAgainstVisibleKeyWithSameRawText_IsRemovedAndAdded()
    {
        var result = CollectionElementDiffer.Diff([Visible("#0")], [new CollectionElementKey(JsonValueKind.String, "#0", Masked: true)]);

        result.Before.Should().Equal(CollectionElementStatus.Removed);
        result.After.Should().Equal(CollectionElementStatus.Added);
        result.OrderChanged.Should().BeFalse();
    }

    [Test]
    public void Diff_MaskedKeysWithSameToken_AreUnchanged()
    {
        var result = CollectionElementDiffer.Diff([Visible("a"), Masked("#0"), Visible("b")], [Visible("a"), Masked("#0"), Visible("c")]);

        result.Before.Should().Equal(CollectionElementStatus.Unchanged, CollectionElementStatus.Unchanged, CollectionElementStatus.Removed);
        result.After.Should().Equal(CollectionElementStatus.Unchanged, CollectionElementStatus.Unchanged, CollectionElementStatus.Added);
    }

    [Test]
    public void Diff_MaskedKeysWithDifferentTokens_AreRemovedAndAdded()
    {
        var result = CollectionElementDiffer.Diff([Visible("a"), Masked("#0"), Visible("b")], [Visible("a"), Masked("#1"), Visible("c")]);

        result.Before.Should().Equal(CollectionElementStatus.Unchanged, CollectionElementStatus.Removed, CollectionElementStatus.Removed);
        result.After.Should().Equal(CollectionElementStatus.Unchanged, CollectionElementStatus.Added, CollectionElementStatus.Added);
    }

    [Test]
    public void Diff_ReorderedMaskedOccurrences_ReportsOrderChangeWithMovedStatuses()
    {
        var result = CollectionElementDiffer.Diff([Masked("#0"), Masked("#1")], [Masked("#1"), Masked("#0")]);

        result.OrderChanged.Should().BeTrue();
        result.Before.Should().NotContain(CollectionElementStatus.Removed);
        result.After.Should().NotContain(CollectionElementStatus.Added);
        result.Before.Concat(result.After).Should().Contain(CollectionElementStatus.Moved);
    }

    [Test]
    public void Diff_MaskedOccurrenceAddedOrRemoved_CarriesAddedOrRemoved()
    {
        var added = CollectionElementDiffer.Diff([Visible("a")], [Visible("a"), Masked("#0")]);
        var removed = CollectionElementDiffer.Diff([Visible("a"), Masked("#0")], [Visible("a")]);

        added.After.Should().Equal(CollectionElementStatus.Unchanged, CollectionElementStatus.Added);
        removed.Before.Should().Equal(CollectionElementStatus.Unchanged, CollectionElementStatus.Removed);
    }

    [Test]
    public void ElementModel_DefaultIsNotMasked()
    {
        new CollectionElementModel(0, "a", JsonValueKind.String, CollectionElementStatus.Unchanged).IsMasked.Should().BeFalse();
        new CollectionElementModel(0, null, JsonValueKind.Undefined, CollectionElementStatus.Unchanged, IsMasked: true).IsMasked.Should().BeTrue();
    }
}
