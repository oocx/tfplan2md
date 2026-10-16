using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Absent, <c>null</c> and <c>[]</c> are equivalent for change detection and distinct whenever a row is shown
/// (SC 9; TC-40 to TC-43, TC-116 at row and drift level).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class ElementlessCollectionTests
{
    private static readonly ReportModelBuilderOptions ShowUnchanged = new(ShowUnchangedValues: true);

    private static string Row(string markdown)
        => markdown.Split('\n').Single(line => line.StartsWith("| items |", StringComparison.Ordinal));

    [Test]
    [Arguments("{\"n\":1}", "{\"n\":2,\"items\":[]}", "- | `[]`")]
    [Arguments("{\"n\":1,\"items\":null}", "{\"n\":2,\"items\":[]}", "`null` | `[]`")]
    [Arguments("{\"n\":1,\"items\":[]}", "{\"n\":2,\"items\":[]}", "`[]` | `[]`")]
    [Arguments("{\"n\":1,\"items\":[]}", "{\"n\":2}", "`[]` | -")]
    [Arguments("{\"n\":1,\"items\":[]}", "{\"n\":2,\"items\":null}", "`[]` | `null`")]
    public void Update_ElementlessOnBothSides_NoRowByDefaultAndDistinctCellsWhenUnchangedShown(string before, string after, string cells)
    {
        var resource = Res(["update"], before, after);

        Render(resource).Should().NotContain("| items |");
        Row(Render(resource, ShowUnchanged)).Should().Be($"| items | {cells} |");
    }

    [Test]
    public void Update_EmptyToEmpty_ModelRowOnlyUnderShowUnchanged()
    {
        var resource = Res(["update"], "{\"items\":[],\"n\":1}", "{\"items\":[],\"n\":2}");

        BuildModel(PlanJson([resource])).Changes.Single().AttributeChanges.Select(a => a.Name).Should().Equal("n");
        var row = BuildModel(PlanJson([resource]), ShowUnchanged).Changes.Single().AttributeChanges.Single(a => a.Name == "items");
        row.Collection!.BeforeState.Should().Be(CollectionState.Empty);
        row.Collection.AfterState.Should().Be(CollectionState.Empty);
    }

    [Test]
    public void Update_ElementlessBesideElements_RendersStateOfElementlessSide()
    {
        Row(Render(Res(["update"], "{\"items\":[]}", "{\"items\":[\"a\"]}"))).Should().Be("| items | `[]` | ➕ `a` |");
        Row(Render(Res(["update"], "{\"items\":null}", "{\"items\":[\"a\"]}"))).Should().Be("| items | `null` | ➕ `a` |");
        Row(Render(Res(["update"], "{\"x\":1}", "{\"x\":1,\"items\":[\"a\"]}"))).Should().Be("| items | - | ➕ `a` |");
    }

    [Test]
    [Arguments("{\"items\":true}")]
    [Arguments("{\"items\":[]}")]
    public void CreateAndDelete_SensitiveElementlessCollection_NoRowAndNoDriftEntry(string sensitivity)
    {
        var create = Res(["create"], null, "{\"name\":\"x\",\"items\":[]}", afterSensitive: sensitivity);
        var delete = Res(["delete"], "{\"name\":\"x\",\"items\":null}", null, beforeSensitive: sensitivity);
        var drift = Res(["update"], "{\"name\":\"x\"}", "{\"name\":\"y\",\"items\":[]}", "example_resource.drifted",
            beforeSensitive: sensitivity, afterSensitive: sensitivity);

        var markdown = Render(PlanJson([create, delete], [drift]));

        markdown.Should().NotContain("| items |").And.NotContain("changed collection").And.NotContain("(sensitive)");
    }

    [Test]
    [Arguments("{\"n\":1,\"items\":null}", "{\"n\":2,\"items\":[]}")]
    [Arguments("{\"n\":1,\"items\":[]}", "{\"n\":2,\"items\":[]}")]
    [Arguments("{\"n\":1,\"items\":[]}", "{\"n\":2}")]
    public void Drift_ElementlessOnBothSides_ProducesNoCollectionDriftEntry(string before, string after)
    {
        var markdown = Render(PlanJson([], [Res(["update"], before, after, "example_resource.drifted")]), ShowUnchanged);

        markdown.Should().NotContain("changed collection");
    }

    [Test]
    public void Render_UnchangedCollectionWithShowUnchangedValues_RendersOneRow()
    {
        var resource = Res(["update"], "{\"items\":[\"a\",\"b\"],\"n\":1}", "{\"items\":[\"a\",\"b\"],\"n\":2}");

        var markdown = Render(resource, ShowUnchanged);

        markdown.Split('\n').Count(line => line.StartsWith("| items", StringComparison.Ordinal)).Should().Be(1);
        Row(markdown).Should().Be("| items | `a`<br>`b` | `a`<br>`b` |");
    }
}
