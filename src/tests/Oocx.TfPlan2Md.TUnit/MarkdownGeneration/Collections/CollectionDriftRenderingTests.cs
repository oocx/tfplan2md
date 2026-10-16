using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Rendering tests for collection drift (TC-55, TC-70, TC-80b, TC-80c, TC-83, TC-46 drift variant).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class CollectionDriftRenderingTests
{
    private const string Nbsp = " ";

    private static Resource Group(string address, string before, string after, string type = "azuread_group", string? afterUnknown = null,
        string? beforeSensitive = null, string? afterSensitive = null)
        => Res(["update"], before, after, address, type, afterUnknown, beforeSensitive, afterSensitive);

    private static string RenderDrift(IEnumerable<Resource> drift, IEnumerable<Resource>? changes = null)
        => Render(PlanJson(changes ?? [], drift));

    [Test]
    public void Drift_GroupMemberInserted_RendersDeltaLinesWithoutBeforeAfterColumns()
    {
        var markdown = RenderDrift([Group("azuread_group.admins", "{\"members\":" + L("1111", "3333") + "}", "{\"members\":" + L("1111", "2222", "3333") + "}")]);

        markdown.Should().Contain("Drift Detected");
        markdown.Should().Contain($"<summary>🌀{Nbsp}azuread_group <code>azuread_group.admins</code> — 1 changed collection: <code>members</code></summary>");
        markdown.Should().Contain($"  - ➕{Nbsp}<code>2222</code>");
        markdown.Should().NotContain("members[");
        markdown.Should().NotContain("➖");
        markdown.Should().NotContain("| Before |");
    }

    [Test]
    public void Drift_CollectionOnly_StillRendersDriftSection()
    {
        var markdown = RenderDrift([Group("azuread_group.admins", "{\"members\":" + L("a") + "}", "{\"members\":" + L("a", "b") + "}")]);

        markdown.Should().Contain("## 🌀" + Nbsp + "Drift Detected");
    }

    [Test]
    public void Drift_OrderChange_RendersRetainedSequences()
    {
        var markdown = RenderDrift([Group("azuread_group.admins", "{\"members\":" + L("a", "b") + "}", "{\"members\":" + L("b", "a") + "}")]);

        markdown.Should().Contain($"🔀{Nbsp}order changed: <code>a</code>, <code>b</code> → <code>b</code>, <code>a</code>");
    }

    [Test]
    public void Drift_EntryWithThreePaths_ReportsThreeChangedCollectionsInSummary()
    {
        var markdown = RenderDrift([Group(
            "azuread_group.admins",
            "{\"a\":" + L("1") + ",\"b\":" + L("1") + ",\"c\":" + L("1") + "}",
            "{\"a\":" + L("1", "2") + ",\"b\":" + L("1", "2") + ",\"c\":" + L("1", "2") + "}")]);

        markdown.Should().Contain("3 changed collections: <code>a</code>, <code>b</code>, <code>c</code>");
    }

    [Test]
    public void Drift_OrderingIsByTypeThenScalarBeforeCollectionThenAddress()
    {
        var scalarB = Res(["update"], "{\"name\":\"a\"}", "{\"name\":\"b\"}", "b_type.one", "b_type");
        var scalarA = Res(["update"], "{\"name\":\"a\"}", "{\"name\":\"b\"}", "a_type.one", "a_type");
        var collectionA2 = Group("a_type.two", "{\"members\":" + L("x") + "}", "{\"members\":" + L("x", "y") + "}", "a_type");
        var combined = Res(["update"], "{\"name\":\"a\",\"members\":" + L("x") + "}", "{\"name\":\"b\",\"members\":" + L("x", "y") + "}", "a_type.one", "a_type");

        var markdown = RenderDrift([scalarB, collectionA2, scalarA, combined]);

        var scalarAIndex = markdown.IndexOf("a_type resources", StringComparison.Ordinal);
        var collectionOneIndex = markdown.IndexOf("<code>a_type.one</code> — 1 changed", StringComparison.Ordinal);
        var collectionTwoIndex = markdown.IndexOf("<code>a_type.two</code> — 1 changed", StringComparison.Ordinal);
        var scalarBIndex = markdown.IndexOf("b_type resources", StringComparison.Ordinal);

        scalarAIndex.Should().BeGreaterThan(-1);
        scalarAIndex.Should().BeLessThan(collectionOneIndex);
        collectionOneIndex.Should().BeLessThan(collectionTwoIndex);
        collectionTwoIndex.Should().BeLessThan(scalarBIndex);
    }

    [Test]
    public void Drift_SensitiveCollection_MasksAndHasNoElementLines()
    {
        var markdown = RenderDrift([Group(
            "azuread_group.admins",
            "{\"members\":" + L("SECRETA") + "}",
            "{\"members\":" + L("SECRETB", "SECRETA") + "}",
            beforeSensitive: "{\"members\":true}",
            afterSensitive: "{\"members\":true}")]);

        markdown.Should().Contain("<code>(sensitive)</code>");
        markdown.Should().NotContain("SECRET");
        markdown.Should().NotContain($"➕{Nbsp}").And.NotContain($"➖{Nbsp}").And.NotContain("order changed");
    }

    [Test]
    public void Drift_UnknownCollection_RendersAsUnresolvedNotElementDiff()
    {
        var markdown = RenderDrift([Group(
            "azuread_group.admins",
            "{\"members\":" + L("a", "b") + "}",
            "{\"members\":[\"a\",null]}",
            afterUnknown: "{\"members\":[false,true]}")]);

        markdown.Should().Contain("unresolved");
        markdown.Should().NotContain($"➖{Nbsp}").And.NotContain($"➕{Nbsp}");
    }

    [Test]
    public void Drift_StateOnlyChange_RendersStateTransition()
    {
        var markdown = RenderDrift([Group("azuread_group.admins", "{\"n\":1}", "{\"n\":1,\"members\":[]}")]);

        markdown.Should().Contain("absent → empty");
    }

    [Test]
    public void Drift_ScalarDriftOnly_RendersExactlyAsBefore()
    {
        var markdown = RenderDrift([
            Res(["update"], "{\"name\":\"a\"}", "{\"name\":\"b\"}", "example_resource.one"),
            Res(["update"], "{\"name\":\"a\"}", "{\"name\":\"b\"}", "example_resource.two")]);

        markdown.Should().Contain("2 example_resource resources — <code>name</code>: <code>a</code> → <code>b</code>");
        markdown.Should().NotContain("changed collection");
    }
}
