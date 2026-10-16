using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Model-level tests for compact primitive collection rows: detection, states, unknown and sensitive handling
/// (TC-14, TC-40 to TC-46, TC-50 to TC-56, TC-60 to TC-64, TC-112 to TC-114).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class PrimitiveCollectionModelTests
{
    private static IReadOnlyList<AttributeChangeModel> Rows(Resource resource, ReportModelBuilderOptions? options = null)
        => BuildModel(PlanJson([resource]), options).Changes.Single().AttributeChanges;

    private static AttributeChangeModel Row(Resource resource, string name, ReportModelBuilderOptions? options = null)
        => Rows(resource, options).Single(row => row.Name == name);

    // ---- Task 3: detection and states ----

    [Test]
    public void Create_PrimitiveList_ProducesExactlyOneRowForItsPath()
    {
        var rows = Rows(Res(["create"], null, "{\"name\":\"x\",\"items\":" + L("a", "b", "c") + "}"));

        rows.Select(r => r.Name).Should().Equal("items", "name");
        rows.Single(r => r.Name == "items").Collection!.After.Select(e => e.Value).Should().Equal("a", "b", "c");
        rows.Single(r => r.Name == "name").Collection.Should().BeNull();
    }

    [Test]
    public void Update_PrimitiveListNestedInObjectList_CompactsInnerListOnly()
    {
        var before = "{\"rule\":[{\"name\":\"r\",\"prefixes\":" + L("a", "b") + ",\"port\":\"80\"}]}";
        var after = "{\"rule\":[{\"name\":\"r\",\"prefixes\":" + L("a", "b", "c") + ",\"port\":\"81\"}]}";

        var rows = Rows(Res(["update"], before, after));

        rows.Select(r => r.Name).Should().Equal("rule[0].port", "rule[0].prefixes");
        rows[1].Collection!.After.Count(e => e.Status == CollectionElementStatus.Added).Should().Be(1);
    }

    [Test]
    public void Update_EmptyToElements_HasEmptyBeforeState()
    {
        var row = Row(Res(["update"], "{\"items\":[]}", "{\"items\":" + L("a") + "}"), "items");

        row.Collection!.BeforeState.Should().Be(CollectionState.Empty);
        row.Collection.AfterState.Should().Be(CollectionState.Elements);
        row.Before.Should().Be("[]");
    }

    [Test]
    public void Update_NullAndAbsentToElements_KeepDistinctBeforeStates()
    {
        var fromNull = Row(Res(["update"], "{\"items\":null}", "{\"items\":" + L("a") + "}"), "items");
        var fromAbsent = Row(Res(["update"], "{\"x\":1}", "{\"x\":1,\"items\":" + L("a") + "}"), "items");

        fromNull.Collection!.BeforeState.Should().Be(CollectionState.Null);
        fromAbsent.Collection!.BeforeState.Should().Be(CollectionState.Absent);
        fromNull.Before.Should().Be("null");
        fromAbsent.Before.Should().Be("-");
    }

    [Test]
    public void Update_EmptyToEmpty_ProducesNoRow()
    {
        Rows(Res(["update"], "{\"items\":[],\"n\":1}", "{\"items\":[],\"n\":2}")).Select(r => r.Name).Should().Equal("n");
    }

    [Test]
    public void Update_AbsentToEmpty_ProducesNoRowButKeepsDistinctStatesWhenUnchangedShown()
    {
        var resource = Res(["update"], "{\"n\":1}", "{\"n\":2,\"items\":[]}");

        Rows(resource).Select(r => r.Name).Should().Equal("n");
        var row = Row(resource, "items", new ReportModelBuilderOptions(ShowUnchangedValues: true));
        row.Collection!.BeforeState.Should().Be(CollectionState.Absent);
        row.Collection.AfterState.Should().Be(CollectionState.Empty);
    }

    [Test]
    public void Update_NullToEmpty_ProducesNoRowButKeepsDistinctStatesWhenUnchangedShown()
    {
        var resource = Res(["update"], "{\"items\":null,\"n\":1}", "{\"items\":[],\"n\":2}");

        Rows(resource).Select(r => r.Name).Should().Equal("n");
        var row = Row(resource, "items", new ReportModelBuilderOptions(ShowUnchangedValues: true));
        row.Collection!.BeforeState.Should().Be(CollectionState.Null);
        row.Collection.AfterState.Should().Be(CollectionState.Empty);
    }

    [Test]
    public void Update_StringOneToNumberOne_IsOneRemovedAndOneAdded()
    {
        var row = Row(Res(["update"], "{\"items\":[\"1\"]}", "{\"items\":[1]}"), "items");

        row.Collection!.Before.Single().Status.Should().Be(CollectionElementStatus.Removed);
        row.Collection.After.Single().Status.Should().Be(CollectionElementStatus.Added);
    }

    [Test]
    public void Update_BooleanVersusStringAndNullVersusStringNull_AreDifferentElements()
    {
        var row = Row(Res(["update"], "{\"items\":[true,null]}", "{\"items\":[\"true\",\"null\"]}"), "items");

        row.Collection!.Before.Select(e => e.Status).Should().OnlyContain(s => s == CollectionElementStatus.Removed);
        row.Collection.After.Select(e => e.Status).Should().OnlyContain(s => s == CollectionElementStatus.Added);
    }

    [Test]
    public void Update_IntegerVersusDecimalNumber_AreComparedByRawText()
    {
        var row = Row(Res(["update"], "{\"items\":[1]}", "{\"items\":[1.0]}"), "items");

        row.Collection!.Before.Single().Status.Should().Be(CollectionElementStatus.Removed);
    }

    [Test]
    public void Create_MixedScalarList_KeepsEachElementWithOwnKind()
    {
        var row = Row(Res(["create"], null, "{\"items\":[1,\"a\",true,null]}"), "items");

        row.Collection!.After.Select(e => e.Kind).Should().Equal(
            JsonValueKind.Number, JsonValueKind.String, JsonValueKind.True, JsonValueKind.Null);
        row.Collection.After[3].Value.Should().BeNull();
    }

    [Test]
    public void Update_ListOfObjects_KeepsIndexedRows()
    {
        var rows = Rows(Res(["update"], "{\"items\":[{\"a\":1},{\"a\":2}]}", "{\"items\":[{\"a\":1},{\"a\":3}]}"));

        rows.Select(r => r.Name).Should().Equal("items[1].a");
        rows[0].Collection.Should().BeNull();
    }

    [Test]
    public void Update_ListContainingNestedList_KeepsIndexedRows()
    {
        var rows = Rows(Res(["update"], "{\"items\":[[1],[2]]}", "{\"items\":[[1],[3]]}"));

        rows.Select(r => r.Name).Should().Equal("items[1][0]");
    }

    [Test]
    [Arguments("{\"items\":[\"a\",{\"b\":1}]}", "{\"items\":[\"a\",{\"b\":2}]}")]
    [Arguments("{\"items\":[\"a\"]}", "{\"items\":[{\"b\":2}]}")]
    [Arguments("{\"items\":[{\"b\":2}]}", "{\"items\":[\"a\"]}")]
    public void Update_ObjectElementOnEitherSide_DisqualifiesPath(string before, string after)
    {
        Rows(Res(["update"], before, after)).Should().OnlyContain(r => r.Collection == null);
    }

    [Test]
    [Arguments("{\"items\":[\"a\"]}", "{\"items\":\"scalar\"}")]
    [Arguments("{\"items\":[\"a\"]}", "{\"items\":{\"k\":\"v\"}}")]
    [Arguments("{\"items\":\"scalar\"}", "{\"items\":[\"a\"]}")]
    public void Update_ArrayChangedToScalarOrObject_RendersThroughExistingPathWithoutException(string before, string after)
    {
        var rows = Rows(Res(["update"], before, after));

        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(r => r.Collection == null);
    }

    [Test]
    public void OutputChanges_PrimitiveArray_KeepsCurrentPathBehaviour()
    {
        var plan = PlanJson([], outputChanges: "{\"o\":{\"actions\":[\"create\"],\"before\":null,\"after\":[\"a\",\"b\"]}}");

        var model = BuildModel(plan);

        model.GlobalOutputs.Should().ContainSingle();
    }

    [Test]
    public void Update_UnchangedCollectionWithShowUnchangedValues_ProducesRow()
    {
        var resource = Res(["update"], "{\"items\":" + L("a") + ",\"n\":1}", "{\"items\":" + L("a") + ",\"n\":2}");

        Rows(resource).Select(r => r.Name).Should().Equal("n");
        Rows(resource, new ReportModelBuilderOptions(ShowUnchangedValues: true)).Select(r => r.Name).Should().Equal("items", "n");
    }

    // ---- Task 4: unknown ----

    [Test]
    public void Update_WholeCollectionUnknownAfter_RendersKnownAfterApplyLabel()
    {
        var row = Row(Res(["update"], "{\"items\":" + L("a") + "}", "{}", afterUnknown: "{\"items\":true}"), "items");

        row.After.Should().Be("(known after apply)");
        row.Collection!.AfterState.Should().Be(CollectionState.Unknown);
        row.Collection.IsUnresolved.Should().BeFalse();
    }

    [Test]
    public void Update_PartialUnknownElement_IsUnresolvedWithOnlyUnknownAndUnchangedStatuses()
    {
        var row = Row(
            Res(["update"], "{\"items\":" + L("a", "b", "c") + "}", "{\"items\":[\"a\",null,\"c\"]}", afterUnknown: "{\"items\":[false,true,false]}"),
            "items");

        row.Collection!.IsUnresolved.Should().BeTrue();
        row.Collection.After.Select(e => e.Status).Should().Equal(
            CollectionElementStatus.Unchanged, CollectionElementStatus.Unknown, CollectionElementStatus.Unchanged);
        row.Collection.Before.Select(e => e.Status).Should().OnlyContain(s => s == CollectionElementStatus.Unchanged);
        row.Collection.OrderChanged.Should().BeFalse();
    }

    [Test]
    [Arguments("{\"items\":true}")]
    [Arguments("{\"items\":{\"x\":true}}")]
    [Arguments("{\"items\":\"nonsense\"}")]
    [Arguments("{\"items\":[true,[true]]}")]
    [Arguments("true")]
    [Arguments("[]")]
    public void Update_MalformedOrUnexpectedAfterUnknownShapes_ProduceRowWithoutException(string afterUnknown)
    {
        var rows = Rows(Res(["update"], "{\"items\":" + L("a") + "}", "{\"items\":" + L("a", "b") + "}", afterUnknown: afterUnknown));

        rows.Should().NotBeEmpty();
    }

    // ---- Task 5: sensitive ----

    [Test]
    public void Update_SensitiveWholeCollection_MasksBothSidesAndCarriesNoElements()
    {
        var row = Row(
            Res(["update"], "{\"items\":" + L("s1", "s2") + "}", "{\"items\":" + L("s2", "s1", "s3") + "}",
                beforeSensitive: "{\"items\":true}", afterSensitive: "{\"items\":true}"),
            "items");

        row.Before.Should().Be("(sensitive)");
        row.After.Should().Be("(sensitive)");
        row.IsSensitive.Should().BeTrue();
        row.Collection!.BeforeState.Should().Be(CollectionState.Masked);
        row.Collection.AfterState.Should().Be(CollectionState.Masked);
        row.Collection.Before.Should().BeEmpty();
        row.Collection.After.Should().BeEmpty();
        row.Collection.OrderChanged.Should().BeFalse();
    }

    [Test]
    public void Update_OneSensitiveElement_MasksWholeCollection()
    {
        var row = Row(
            Res(["update"], "{\"items\":" + L("pub", "sec") + "}", "{\"items\":" + L("pub", "sec", "more") + "}",
                beforeSensitive: "{\"items\":[false,true]}", afterSensitive: "{\"items\":[false,true,false]}"),
            "items");

        row.Collection!.AfterState.Should().Be(CollectionState.Masked);
        row.After.Should().Be("(sensitive)");
    }

    [Test]
    public void Update_SensitiveCollectionWithShowSensitive_KeepsNormalDiffAndSensitiveFlag()
    {
        var row = Row(
            Res(["update"], "{\"items\":" + L("a") + "}", "{\"items\":" + L("a", "b") + "}",
                beforeSensitive: "{\"items\":true}", afterSensitive: "{\"items\":true}"),
            "items",
            new ReportModelBuilderOptions(ShowSensitive: true));

        row.IsSensitive.Should().BeTrue();
        row.Collection!.AfterState.Should().Be(CollectionState.Elements);
        row.Collection.After.Count(e => e.Status == CollectionElementStatus.Added).Should().Be(1);
    }

    [Test]
    public void Create_SensitiveOnlyInAfterSensitive_IsMaskedAndSecretsAbsentFromOutput()
    {
        var resource = Res(["create"], null, "{\"items\":[\"SECRETVALUE1\",\"SECRETVALUE2\"]}", afterSensitive: "{\"items\":true}");

        Row(resource, "items").Collection!.AfterState.Should().Be(CollectionState.Masked);
        var markdown = Render(resource);
        markdown.Should().NotContain("SECRETVALUE");
        markdown.Should().Contain("(sensitive)");
    }

    [Test]
    public void Delete_SensitiveOnlyInBeforeSensitive_IsMasked()
    {
        var resource = Res(["delete"], "{\"items\":[\"SECRETVALUE1\"]}", null, beforeSensitive: "{\"items\":[true]}");

        Row(resource, "items").Collection!.BeforeState.Should().Be(CollectionState.Masked);
        Render(resource).Should().NotContain("SECRETVALUE");
    }
}
