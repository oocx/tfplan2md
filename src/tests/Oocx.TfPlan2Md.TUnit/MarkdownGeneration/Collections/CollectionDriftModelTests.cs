using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Tests collection drift grouping and drift mode selection (TC-70 to TC-79, TC-83).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class CollectionDriftModelTests
{
    private const string GroupType = "azuread_group";

    private static Resource Group(string address, string before, string after, string? afterUnknown = null)
        => Res(["update"], before, after, address, GroupType, afterUnknown);

    private static Resource Kept(string address = "example_resource.kept")
        => Res(["update"], "{\"name\":\"a\"}", "{\"name\":\"b\"}", address);

    private static ReportModel Build(IEnumerable<Resource> drift, IEnumerable<Resource>? changes = null, DriftDisplayMode mode = DriftDisplayMode.All)
        => BuildModel(PlanJson(changes ?? [Kept()], drift), new ReportModelBuilderOptions(DriftDisplayMode: mode));

    [Test]
    public void Drift_GroupMemberInserted_OneCollectionEntryOnePath()
    {
        var model = Build([Group("azuread_group.admins", "{\"members\":" + L("a", "c") + "}", "{\"members\":" + L("a", "b", "c") + "}")]);

        model.Drift.Should().BeEmpty();
        var entry = model.CollectionDrift.Should().ContainSingle().Subject;
        entry.Address.Should().Be("azuread_group.admins");
        entry.ResourceType.Should().Be(GroupType);
        entry.Paths.Should().ContainSingle().Which.Path.Should().Be("members");
        entry.Paths[0].Collection.After.Count(e => e.Status == CollectionElementStatus.Added).Should().Be(1);
    }

    [Test]
    public void Drift_ManyShiftedIndices_NoPerIndexEntries()
    {
        var before = Enumerable.Range(0, 15).Select(i => $"m{i:D2}").ToArray();
        var after = before.Prepend("new").ToArray();

        var model = Build([Group("azuread_group.big", "{\"members\":" + L(before) + "}", "{\"members\":" + L(after) + "}")]);

        model.Drift.Should().BeEmpty();
        model.CollectionDrift.Single().Paths.Should().ContainSingle();
    }

    [Test]
    public void Drift_TwoCollectionPathsOneResource_SingleEntryWithBothPathsNaturallyOrdered()
    {
        var model = Build([Group(
            "azuread_group.admins",
            "{\"owners\":" + L("o1") + ",\"members\":" + L("m1") + "}",
            "{\"owners\":" + L("o1", "o2") + ",\"members\":" + L("m1", "m2") + "}")]);

        model.CollectionDrift.Should().ContainSingle().Which.Paths.Select(p => p.Path).Should().Equal("members", "owners");
    }

    [Test]
    public void Drift_EachPathInEntry_HasOwnDiff()
    {
        var model = Build([Group(
            "azuread_group.admins",
            "{\"members\":" + L("a", "b") + ",\"owners\":" + L("x", "y") + "}",
            "{\"members\":" + L("a", "b", "c") + ",\"owners\":" + L("y", "x") + "}")]);

        var paths = model.CollectionDrift.Single().Paths.ToDictionary(p => p.Path, p => p.Collection);
        paths["members"].OrderChanged.Should().BeFalse();
        paths["members"].After.Count(e => e.Status == CollectionElementStatus.Added).Should().Be(1);
        paths["owners"].OrderChanged.Should().BeTrue();
        paths["owners"].After.Should().NotContain(e => e.Status == CollectionElementStatus.Added);
    }

    [Test]
    public void Drift_EntryWithThreePaths_ReportsThreeChangedCollections()
    {
        var model = Build([Group(
            "azuread_group.admins",
            "{\"a\":" + L("1") + ",\"b\":" + L("1") + ",\"c\":" + L("1") + "}",
            "{\"a\":" + L("1", "2") + ",\"b\":" + L("1", "2") + ",\"c\":" + L("1", "2") + "}")]);

        model.CollectionDrift.Should().ContainSingle().Which.Paths.Should().HaveCount(3);
    }

    [Test]
    public void Drift_TwoResourcesIdenticalCollectionChange_TwoEntries()
    {
        var before = "{\"members\":" + L("a") + "}";
        var after = "{\"members\":" + L("a", "b") + "}";

        var model = Build([Group("azuread_group.b[\"k2\"]", before, after), Group("azuread_group.a[\"k1\"]", before, after)]);

        model.CollectionDrift.Select(e => e.Address).Should().Equal("azuread_group.a[\"k1\"]", "azuread_group.b[\"k2\"]");
    }

    [Test]
    public void Drift_ScalarChangeOnTwoResources_StillGrouped()
    {
        var model = Build([Kept("example_resource.one"), Kept("example_resource.two")]);

        model.Drift.Should().ContainSingle().Which.Addresses.Should().Equal("example_resource.one", "example_resource.two");
        model.CollectionDrift.Should().BeEmpty();
    }

    [Test]
    public void Drift_ResourceWithScalarAndCollectionDrift_ScalarGroupedCollectionPerResource()
    {
        var one = Res(["update"], "{\"name\":\"a\",\"members\":" + L("m") + "}", "{\"name\":\"b\",\"members\":" + L("m", "n") + "}", "example_resource.one");
        var two = Res(["update"], "{\"name\":\"a\",\"members\":" + L("m") + "}", "{\"name\":\"b\",\"members\":" + L("m", "o") + "}", "example_resource.two");

        var model = Build([one, two]);

        model.Drift.Should().ContainSingle().Which.Addresses.Should().HaveCount(2);
        model.CollectionDrift.Should().HaveCount(2);
    }

    [Test]
    public void Drift_ModeAllAndDefault_IncludeCollectionEntries()
    {
        var drift = new[] { Group("azuread_group.admins", "{\"members\":" + L("a") + "}", "{\"members\":" + L("a", "b") + "}") };

        Build(drift).CollectionDrift.Should().ContainSingle();
        Build(drift, mode: DriftDisplayMode.All).CollectionDrift.Should().ContainSingle();
        BuildModel(PlanJson([Kept()], drift)).CollectionDrift.Should().ContainSingle();
    }

    [Test]
    public void Drift_ModeRelevant_OnlyRelevantResourcesHaveCollectionEntries()
    {
        var before = "{\"members\":" + L("a") + "}";
        var after = "{\"members\":" + L("a", "b") + "}";
        var relevant = Group("azuread_group.relevant", before, after);
        var unrelated = Group("azuread_group.unrelated", before, after);
        var noOpChange = Res(["no-op"], before, before, "azuread_group.unrelated", GroupType);

        var model = Build([relevant, unrelated], [Res(["update"], before, after, "azuread_group.relevant", GroupType), noOpChange], DriftDisplayMode.Relevant);

        model.CollectionDrift.Select(e => e.Address).Should().Equal("azuread_group.relevant");
    }

    [Test]
    public void Drift_ModeNone_NoCollectionEntriesAndNoSection()
    {
        var model = Build([Group("azuread_group.admins", "{\"members\":" + L("a") + "}", "{\"members\":" + L("a", "b") + "}")], mode: DriftDisplayMode.None);

        model.CollectionDrift.Should().BeEmpty();
        new MarkdownRenderer().Render(model).Should().NotContain("Drift Detected");
    }

    [Test]
    public void Drift_NoOpCollectionDrift_StaysSuppressed()
    {
        var same = "{\"members\":" + L("a") + "}";

        Build([Group("azuread_group.admins", same, same)]).CollectionDrift.Should().BeEmpty();
    }
}
