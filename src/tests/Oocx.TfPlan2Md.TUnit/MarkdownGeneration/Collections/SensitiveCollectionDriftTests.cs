using System.Linq;
using AwesomeAssertions;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Collection drift with per-entry masked entries (TC-55 per-entry, TC-121 at drift level, SC 19).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class SensitiveCollectionDriftTests
{
    private const string Nbsp = " ";

    private static string Members(params string[] values) => "{\"members\":" + L(values) + "}";

    private static string Flags(params bool[] flags) => "{\"members\":[" + string.Join(',', flags.Select(f => f ? "true" : "false")) + "]}";

    private static string RenderDrift(string before, bool[] beforeFlags, string after, bool[] afterFlags)
        => Render(PlanJson([], [Res(["update"], before, after, "azuread_group.admins", "azuread_group",
            beforeSensitive: Flags(beforeFlags), afterSensitive: Flags(afterFlags))]));

    [Test]
    public void Drift_PerEntryMaskedChange_ShowsMarkerLineWithoutValueAndVisibleMembers()
    {
        var markdown = RenderDrift(
            Members("m1", "SECRET-AAA-148"), [false, true],
            Members("m1", "SECRET-BBB-148", "m2"), [false, true, false]);

        markdown.Should().Contain($"  - ➖{Nbsp}<code>(sensitive)</code>");
        markdown.Should().Contain($"  - ➕{Nbsp}<code>(sensitive)</code>");
        markdown.Should().Contain($"  - ➕{Nbsp}<code>m2</code>");
        markdown.Should().Contain("1 changed collection: <code>members</code>");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void Drift_UnchangedMaskedEntry_HasNoMarkerLine()
    {
        var markdown = RenderDrift(
            Members("m1", "SECRET-AAA-148"), [false, true],
            Members("m1", "SECRET-AAA-148", "m2"), [false, true, false]);

        markdown.Should().NotContain("(sensitive)");
        markdown.Should().Contain($"  - ➕{Nbsp}<code>m2</code>");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void Drift_OrderChangeWithMaskedEntries_ListsThemAsPlaceholders()
    {
        var markdown = RenderDrift(
            Members("SECRET-AAA-148", "m1"), [true, false],
            Members("m1", "SECRET-AAA-148"), [false, true]);

        markdown.Should().Contain(
            $"🔀{Nbsp}order changed: <code>(sensitive)</code>, <code>m1</code> → <code>m1</code>, <code>(sensitive)</code>");
        markdown.Should().NotContain("SECRET").And.NotContain($"➖{Nbsp}").And.NotContain($"➕{Nbsp}");
    }

    [Test]
    public void Drift_TwoPathsOneWithMaskedChange_CountsPaths()
    {
        var markdown = Render(PlanJson([], [Res(
            ["update"],
            "{\"members\":" + L("SECRET-AAA-148") + ",\"owners\":" + L("o1") + "}",
            "{\"members\":" + L("SECRET-BBB-148") + ",\"owners\":" + L("o1", "o2") + "}",
            "azuread_group.admins",
            "azuread_group",
            beforeSensitive: "{\"members\":[true]}",
            afterSensitive: "{\"members\":[true]}")]));

        markdown.Should().Contain("2 changed collections: <code>members</code>, <code>owners</code>");
        markdown.Should().NotContain("SECRET");
    }
}
