using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Tests natural ordering of attribute paths (TC-01 to TC-03).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class AttributePathComparerTests
{
    private static string[] Sort(params string[] paths)
        => paths.Order(AttributePathComparer.Instance).ToArray();

    [Test]
    public void AttributePathComparer_MultiDigitIndices_OrdersNumerically()
    {
        Sort("a[10]", "a[2]", "a[1]").Should().Equal("a[1]", "a[2]", "a[10]");
    }

    [Test]
    public void AttributePathComparer_NestedIndexedPaths_OrdersNumericallyPerSegment()
    {
        Sort("r[2].x[10]", "r[2].x[9]", "r[10].x[1]", "r[9].y")
            .Should().Equal("r[2].x[9]", "r[2].x[10]", "r[9].y", "r[10].x[1]");
    }

    [Test]
    public void AttributePathComparer_NonIndexSegments_StayOrdinal()
    {
        Sort("abc", "Zed").Should().Equal("Zed", "abc");
        string[] paths = ["a_b", "a.b", "a[0]", "a", "a.b.c", "tags[\"a\"]", "tags.x"];
        Sort(paths).Should().Equal(paths.Order(StringComparer.Ordinal));
    }

    [Test]
    public void AttributePathComparer_EqualAndBracketlessPaths_CompareConsistently()
    {
        AttributePathComparer.Instance.Compare("a[1]", "a[1]").Should().Be(0);
        AttributePathComparer.Instance.Compare("plain", "plain").Should().Be(0);
        AttributePathComparer.Instance.Compare("tags[\"a\"]", "tags[\"b\"]").Should().BeNegative();
        AttributePathComparer.Instance.Compare("a[2]", "a[2].b").Should().BeNegative();
    }

    [Test]
    public void ResourceChangeStage_ObjectListWithTwelveItems_RowsInNumericOrder()
    {
        var before = "{\"rule\":[" + string.Join(',', Enumerable.Range(0, 12).Select(i => $"{{\"p\":{i}}}")) + "]}";
        var after = "{\"rule\":[" + string.Join(',', Enumerable.Range(0, 12).Select(i => $"{{\"p\":{i + 100}}}")) + "]}";

        var markdown = Render(Res(["update"], before, after));

        markdown.IndexOf("rule[2].p", StringComparison.Ordinal).Should().BeGreaterThan(-1);
        markdown.IndexOf("rule[2].p", StringComparison.Ordinal)
            .Should().BeLessThan(markdown.IndexOf("rule[10].p", StringComparison.Ordinal));
    }

    [Test]
    public void GroupDrift_MultiDigitIndices_OrdersAttributePathNumerically()
    {
        var before = "{\"rule\":[" + string.Join(',', Enumerable.Range(0, 12).Select(i => $"{{\"p\":{i}}}")) + "]}";
        var after = "{\"rule\":[" + string.Join(',', Enumerable.Range(0, 12).Select(i => $"{{\"p\":{i + 100}}}")) + "]}";

        var model = BuildModel(PlanJson([], [Res(["update"], before, after)]));

        var paths = model.Drift.Select(g => g.AttributePath).ToList();
        paths.IndexOf("rule[2].p").Should().BeLessThan(paths.IndexOf("rule[10].p"));
    }
}
