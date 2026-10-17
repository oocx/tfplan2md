using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Rendering tests for per-entry masked collection entries in the generic table and the large-value section
/// (TC-56, TC-57, TC-58, TC-59, TC-120, TC-121, TC-122, TC-123, TC-125, TC-128).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class SensitiveCollectionRenderingTests
{
    private const string Secret1 = "SECRET-AAA-148";
    private const string Secret2 = "SECRET-BBB-148";
    private const string Nbsp = " ";

    private static string Items(params string[] values) => "{\"items\":" + L(values) + "}";

    private static string Flags(params bool[] flags) => "{\"items\":[" + string.Join(',', flags.Select(f => f ? "true" : "false")) + "]}";

    private static string Row(string markdown)
        => markdown.Split('\n').Single(line => line.StartsWith("| items |", StringComparison.Ordinal));

    /// <summary>
    /// Renders an update without the metadata line, whose timestamp would make two renders differ.
    /// </summary>
    private static string RenderUpdate(string[] before, bool[] beforeFlags, string[] after, bool[] afterFlags)
        => Render(
            Res(["update"], Items(before), Items(after), beforeSensitive: Flags(beforeFlags), afterSensitive: Flags(afterFlags)),
            new ReportModelBuilderOptions(HideMetadata: true));

    [Test]
    public void Update_MaskedEntryChanged_ShowsRemovedAndAddedPlaceholders()
    {
        var markdown = RenderUpdate(["a", Secret1, "b"], [false, true, false], ["a", Secret2, "c"], [false, true, false]);

        Row(markdown).Should().Be(
            $"| items | `a`<br>➖{Nbsp}`(sensitive)`<br>➖{Nbsp}`b` | `a`<br>➕{Nbsp}`(sensitive)`<br>➕{Nbsp}`c` |");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void Update_MaskedEntryUnchanged_ShowsPlaceholderWithoutMarker()
    {
        var markdown = RenderUpdate(["a", Secret1, "b"], [false, true, false], ["a", Secret1, "c"], [false, true, false]);

        Row(markdown).Should().Be(
            $"| items | `a`<br>`(sensitive)`<br>➖{Nbsp}`b` | `a`<br>`(sensitive)`<br>➕{Nbsp}`c` |");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void Update_VisibleBecomesMasked_SameValue_ShowsRemovedAndAdded()
    {
        var same = RenderUpdate(["x"], [false], ["x"], [true]);
        var other = RenderUpdate(["x"], [false], ["something-else"], [true]);

        Row(same).Should().Be($"| items | ➖{Nbsp}`x` | ➕{Nbsp}`(sensitive)` |");
        same.Should().Be(other);
    }

    [Test]
    public void Update_MaskedBecomesVisible_SameValue_ShowsRemovedAndAdded()
    {
        var same = RenderUpdate(["x"], [true], ["x"], [false]);
        var other = RenderUpdate(["something-else"], [true], ["x"], [false]);

        Row(same).Should().Be($"| items | ➖{Nbsp}`(sensitive)` | ➕{Nbsp}`x` |");
        same.Should().Be(other);
    }

    [Test]
    public void Update_SensitiveOnOneSideOnly_MasksPerSide()
    {
        var markdown = RenderUpdate(["a", "shown-before"], [false, false], ["a", Secret1], [false, true]);

        Row(markdown).Should().Be($"| items | `a`<br>➖{Nbsp}`shown-before` | `a`<br>➕{Nbsp}`(sensitive)` |");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void Update_MaskedEntriesReordered_RendersOrderChangeWithoutMarkers()
    {
        var markdown = RenderUpdate([Secret1, "a"], [true, false], ["a", Secret1], [false, true]);

        var row = Row(markdown);
        row.Should().EndWith($"🔀{Nbsp}order changed |");
        row.Should().NotContain("➖").And.NotContain("➕");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void Update_DistinctMaskedEntriesSwapped_RendersOrderChange()
    {
        var markdown = RenderUpdate([Secret1, Secret2], [true, true], [Secret2, Secret1], [true, true]);

        Row(markdown).Should().Be($"| items | `(sensitive)`<br>`(sensitive)` | `(sensitive)`<br>`(sensitive)`<br>🔀{Nbsp}order changed |");
    }

    [Test]
    public void Update_AllMaskedEntriesReplaced_RendersRemovedAndAddedWithoutOrderChange()
    {
        var markdown = RenderUpdate([Secret1, Secret2], [true, true], ["SECRET-CCC", "SECRET-DDD"], [true, true]);

        Row(markdown).Should().Be(
            $"| items | ➖{Nbsp}`(sensitive)`<br>➖{Nbsp}`(sensitive)` | ➕{Nbsp}`(sensitive)`<br>➕{Nbsp}`(sensitive)` |");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void Update_MaskedEntryAddedOrRemoved_CarriesMarker()
    {
        Row(RenderUpdate(["a"], [false], ["a", Secret1], [false, true]))
            .Should().Be($"| items | `a` | `a`<br>➕{Nbsp}`(sensitive)` |");
        Row(RenderUpdate(["a", Secret1], [false, true], ["a"], [false]))
            .Should().Be($"| items | `a`<br>➖{Nbsp}`(sensitive)` | `a` |");
    }

    [Test]
    public void Render_SameSideEqualMaskedEntries_NotCollapsedCountedOrLabelled()
    {
        var equal = RenderUpdate([Secret1, Secret1], [true, true], ["SECRET-T"], [true]);
        var distinct = RenderUpdate([Secret1, Secret2], [true, true], ["SECRET-T"], [true]);

        equal.Should().Be(distinct);
        Row(equal).Should().Be($"| items | ➖{Nbsp}`(sensitive)`<br>➖{Nbsp}`(sensitive)` | ➕{Nbsp}`(sensitive)` |");
        equal.Should().NotContain("×").And.NotContain("x2").And.NotContain("duplicate");
    }

    [Test]
    public void CreateAndDelete_MaskedEntries_ShowPlaceholderWithoutMarkers()
    {
        var create = Render(Res(["create"], null, Items("a", Secret1), afterSensitive: Flags(false, true)));
        var delete = Render(Res(["delete"], Items("a", Secret1), null, beforeSensitive: Flags(false, true)));

        Row(create).Should().Be("| items | `a`<br>`(sensitive)` |");
        Row(delete).Should().Be("| items | `a`<br>`(sensitive)` |");
        (create + delete).Should().NotContain("SECRET");
    }

    private sealed class RecordingNameFormatter(IReadOnlyDictionary<string, string> names, List<string?> seen) : IValueFormatter
    {
        public string? TryFormat(ServiceResolutionContext context)
        {
            seen.Add(context.Value);
            return context.Value is { } value && names.TryGetValue(value, out var name) ? $"`{value}` ({name})" : null;
        }
    }

    [Test]
    public void Render_MaskedGuidWithNameMapping_ShowsNoAnnotationAndFormatterNeverSeesIt()
    {
        const string guid = "33333333-3333-3333-3333-333333333333";
        const string visible = "44444444-4444-4444-4444-444444444444";
        var names = new Dictionary<string, string> { [guid] = "Secret Person", [visible] = "Visible Person" };
        var seen = new List<string?>();
        var formatters = new ValueFormatterRegistry();
        formatters.Register(new MatchPattern(null, null, ".*", null), new RecordingNameFormatter(names, seen));
        var resource = Res(["update"], Items(visible), Items(visible, guid), beforeSensitive: Flags(false), afterSensitive: Flags(false, true));

        var markdown = new MarkdownRenderer(valueFormatterRegistry: formatters).Render(BuildModel(PlanJson([resource])));

        Row(markdown).Should().Be($"| items | `{visible}` (Visible Person) | `{visible}` (Visible Person)<br>➕{Nbsp}`(sensitive)` |");
        markdown.Should().NotContain("Secret Person").And.NotContain("3333");
        seen.Should().NotContain(value => value != null && value.Contains("3333", StringComparison.Ordinal));
    }

    [Test]
    public void LargeSection_MaskedEntries_RenderAsPlaceholdersWithMarkers()
    {
        var large = new string('v', 150);
        var markdown = RenderUpdate([large, Secret1], [false, true], [large, Secret2, "b"], [false, true, false]);

        markdown.Should().Contain("Large values: items");
        markdown.Should().Contain($"➖{Nbsp}(sensitive)\n").And.Contain($"➕{Nbsp}(sensitive)\n").And.Contain($"➕{Nbsp}b\n");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void LargeSection_OnlyLargeElementIsMasked_RowStaysInTable()
    {
        var largeSecret = "SECRET-" + new string('S', 200);
        var markdown = RenderUpdate(["a", largeSecret], [false, true], ["a", largeSecret, "b"], [false, true, false]);

        markdown.Should().NotContain("Large values");
        Row(markdown).Should().Be($"| items | `a`<br>`(sensitive)` | `a`<br>`(sensitive)`<br>➕{Nbsp}`b` |");
        markdown.Should().NotContain("SECRET");
    }

    [Test]
    public void UatFixture_RevisedSensitiveScenarios_RenderPerEntryMasksWithoutSecrets()
    {
        var (_, markdown) = RenderWithCliDefaults(System.IO.File.ReadAllText("TestData/feature-148-uat-plan.json"));

        markdown.Should().NotContainEquivalentOf("SENTINEL");
        markdown.Should().Contain($"| changed | `a`<br>➖{Nbsp}`(sensitive)`<br>➖{Nbsp}`b` | `a`<br>➕{Nbsp}`(sensitive)`<br>➕{Nbsp}`c` |");
        markdown.Should().Contain($"| visible_to_masked | ➖{Nbsp}`x` | ➕{Nbsp}`(sensitive)` |");
        markdown.Should().Contain("| whole | `(sensitive)` | `(sensitive)` |");
        markdown.Should().Contain($"| ids_case_masked | ➖{Nbsp}`(sensitive)` | ➕{Nbsp}`(sensitive)` |");
        markdown.Should().Contain("`🔒(known after apply)`");
        markdown.Should().NotContain("secret_list").And.NotContain("ids_case_only");
        markdown.Should().Contain($"  - ➕{Nbsp}<code>(sensitive)</code>");
    }
}
