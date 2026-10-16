using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Specification rule 7: a masked value can affect the report only through masked-to-masked comparisons across
/// sides. Replacing masked values while keeping that relation must leave the model and the Markdown unchanged in
/// every output path (SC 25; TC-124, TC-126, TC-127, TC-128). Also sweeps sentinel secrets through every output
/// (SC 11, SC 23; TC-56).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class MaskedValueInvarianceTests
{
    /// <summary>
    /// Three masked values as raw JSON. Every plan below uses them so that they are pairwise different; the relation
    /// between masked entries across sides is therefore the same for every substitution that keeps them distinct.
    /// </summary>
    private sealed record Secrets(string S1, string S2, string S3);

    private static string Str(string value) => JsonSerializer.Serialize(value);

    private static readonly string LargeVisible = new('v', 150);

    private static Secrets Scenario(string name) => name switch
    {
        "baseline" => new(Str("SENTINEL-148-AAA"), Str("SENTINEL-148-BBB"), Str("SENTINEL-148-CCC")),
        "a-same-kind" => new(Str("other-one"), Str("other-two"), Str("other-three")),
        "b-equals-visible-before" => new(Str("q1"), Str("vis-b"), Str("q3")),
        "c-equals-visible-after" => new(Str("q1"), Str("q2"), Str("vis-c")),
        "d-equals-visible-both" => new(Str("10"), Str("q2"), Str("x")),
        "e-lengths" => new(Str(string.Empty), Str("z"), Str(new string('L', 500))),
        "f-kinds" => new("42", "true", Str("/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg")),
        "g-casing" => new(Str("sentinel-148-aaa"), Str("SENTINEL-148-bbb"), Str("Sentinel-148-Ccc")),
        "h-markdown" => new(Str("a|b"), Str("tick`x"), Str("<br>")),
        "i-large" => new(Str(new string('A', 300)), Str(new string('B', 300)), Str(new string('C', 300))),
        "j-numbers" => new("7", "8", "9"),
        "negative-control" => new(Str("SENTINEL-148-AAA"), Str("SENTINEL-148-BBB"), Str("SENTINEL-148-BBB")),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    private static string Arr(params string[] rawElements) => "[" + string.Join(',', rawElements) + "]";

    private static string Flags(params bool[] flags) => "{\"items\":[" + string.Join(',', flags.Select(f => f ? "true" : "false")) + "]}";

    private static string Items(string array, string name = "n") => "{\"name\":\"" + name + "\",\"items\":" + array + "}";

    /// <summary>
    /// One plan that reaches every output path: update, replace, create, delete, an order change, a large-value row,
    /// an unresolved row, a visible-to-masked transition, and the same collections as drift.
    /// </summary>
    private static string Plan(Secrets s)
    {
        var update = Res(["update"],
            Items(Arr(Str("10"), s.S1, Str("vis-b"), s.S2), "n1"), Items(Arr(Str("10"), s.S1, Str("vis-c"), s.S3), "n2"),
            "example_resource.update", beforeSensitive: Flags(false, true, false, true), afterSensitive: Flags(false, true, false, true));
        var order = Res(["update"],
            Items(Arr(s.S1, Str("x"))), Items(Arr(Str("x"), s.S1)),
            "example_resource.order", beforeSensitive: Flags(true, false), afterSensitive: Flags(false, true));
        var large = Res(["update"],
            Items(Arr(Str(LargeVisible), s.S2)), Items(Arr(Str(LargeVisible), s.S3, Str("y"))),
            "example_resource.large", beforeSensitive: Flags(false, true), afterSensitive: Flags(false, true, false));
        var replace = Res(["delete", "create"],
            Items(Arr(Str("r"), s.S1)), Items(Arr(Str("r"), s.S2)),
            "example_resource.replace", beforeSensitive: Flags(false, true), afterSensitive: Flags(false, true));
        var create = Res(["create"], null, Items(Arr(Str("c"), s.S1, s.S2)),
            "example_resource.create", afterSensitive: Flags(false, true, true));
        var delete = Res(["delete"], Items(Arr(Str("d"), s.S3)), null,
            "example_resource.delete", beforeSensitive: Flags(false, true));
        var unknown = Res(["update"],
            Items(Arr(Str("u"), s.S1)), Items(Arr(Str("u"), "null", s.S2)),
            "example_resource.unknown", afterUnknown: Flags(false, true, false),
            beforeSensitive: Flags(false, true), afterSensitive: Flags(false, true, true));
        var mirror = Res(["update"],
            Items(Arr(Str("vis-m"))), Items(Arr(s.S1)),
            "example_resource.mirror", beforeSensitive: Flags(false), afterSensitive: Flags(true));
        var driftUpdate = update with { Address = "example_resource.drifted" };
        var driftOrder = order with { Address = "example_resource.drifted_order" };

        return PlanJson([update, order, large, replace, create, delete, unknown, mirror], [driftUpdate, driftOrder]);
    }

    /// <summary>
    /// Writes every model property a renderer, summary or count can read.
    /// </summary>
    private static string DumpModel(ReportModel model)
    {
        var builder = new StringBuilder();
        foreach (var change in model.Changes)
        {
            builder.Append(change.Address).Append('|').Append(change.Summary).Append('|').Append(change.ChangedAttributesSummary)
                .Append('|').Append(change.SummaryHtml).Append('\n');
            foreach (var attribute in change.AttributeChanges)
            {
                builder.Append(SensitiveCollectionModelTests.Dump(attribute));
            }
        }

        foreach (var entry in model.CollectionDrift)
        {
            builder.Append(entry.Address).Append('\n');
            foreach (var path in entry.Paths)
            {
                builder.Append(SensitiveCollectionModelTests.Dump(new AttributeChangeModel { Name = path.Path, Collection = path.Collection }));
            }
        }

        foreach (var group in model.Drift)
        {
            builder.Append(group.ResourceType).Append('|').Append(group.AttributePath).Append('|').Append(group.Before).Append('|')
                .Append(group.After).Append('|').AppendJoin(',', group.Addresses).Append('\n');
        }

        return builder.ToString();
    }

    private static (string Model, string Markdown) Run(string scenario)
    {
        // Metadata is hidden because its generation timestamp would differ between two runs.
        var (model, markdown) = RenderWithCliDefaults(Plan(Scenario(scenario)), "--debug", "--hide-metadata");
        return (DumpModel(model), markdown);
    }

    [Test]
    [Arguments("a-same-kind")]
    [Arguments("b-equals-visible-before")]
    [Arguments("c-equals-visible-after")]
    [Arguments("d-equals-visible-both")]
    [Arguments("e-lengths")]
    [Arguments("f-kinds")]
    [Arguments("g-casing")]
    [Arguments("h-markdown")]
    [Arguments("i-large")]
    [Arguments("j-numbers")]
    public void Stage_ReplacingMaskedValue_RelationUnchanged_ModelAndMarkdownIdentical(string scenario)
    {
        var baseline = Run("baseline");

        var substituted = Run(scenario);

        substituted.Model.Should().Be(baseline.Model);
        substituted.Markdown.Should().Be(baseline.Markdown);
    }

    [Test]
    public void Stage_ChangingMaskedToMaskedRelation_ChangesOutput()
    {
        var baseline = Run("baseline");

        var changed = Run("negative-control");

        changed.Markdown.Should().NotBe(baseline.Markdown);
        changed.Model.Should().NotBe(baseline.Model);
    }

    [Test]
    public void Plan_ReachesEveryOutputPath()
    {
        var (_, markdown) = Run("baseline");

        markdown.Should().Contain("Large values: items");
        markdown.Should().Contain("Drift Detected").And.Contain("changed collection");
        markdown.Should().Contain("🔀 order changed");
        markdown.Should().Contain("known after apply");
        markdown.Should().Contain("➕ `(sensitive)`").And.Contain("➖ `vis-m`");
        markdown.Should().Contain("`(sensitive)`<br>");
        markdown.Should().Contain("Debug Information");
    }

    [Test]
    [NotInParallel("CLI")]
    public void Output_SentinelSecrets_AbsentFromEveryOutputPath()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var log = new StringWriter();
        Console.SetOut(log);
        Console.SetError(log);
        string markdown;
        string model;
        try
        {
            (model, markdown) = Run("baseline");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        foreach (var output in new[] { markdown, model, log.ToString() })
        {
            output.Should().NotContainEquivalentOf("SENTINEL");
            output.Should().NotContainEquivalentOf("148-AAA").And.NotContainEquivalentOf("148-BBB").And.NotContainEquivalentOf("148-CCC");
        }
    }

    private sealed class NameFormatter(IReadOnlyDictionary<string, string> names) : IValueFormatter
    {
        public string? TryFormat(ServiceResolutionContext context)
            => context.Value is { } value && names.TryGetValue(value, out var name) ? $"`{value}` ({name})" : null;
    }

    [Test]
    public void Render_MaskedGuidThatNameMappingWouldResolve_OutputIdenticalToUnmappedGuid()
    {
        const string mapped = "aaaaaaaa-0000-0000-0000-000000000001";
        const string unmapped = "bbbbbbbb-0000-0000-0000-000000000002";
        var formatters = new ValueFormatterRegistry();
        formatters.Register(new MatchPattern(null, null, ".*", null), new NameFormatter(new Dictionary<string, string> { [mapped] = "Alice" }));
        string RenderGuid(string guid) => new MarkdownRenderer(valueFormatterRegistry: formatters).Render(BuildModel(PlanJson([Res(
            ["update"], Items(Arr(Str("v"))), Items(Arr(Str("v"), Str(guid))),
            beforeSensitive: Flags(false), afterSensitive: Flags(false, true))]), new ReportModelBuilderOptions(HideMetadata: true)));

        var withMapping = RenderGuid(mapped);

        withMapping.Should().Be(RenderGuid(unmapped));
        withMapping.Should().NotContain("Alice").And.NotContain("aaaaaaaa");
    }
}
