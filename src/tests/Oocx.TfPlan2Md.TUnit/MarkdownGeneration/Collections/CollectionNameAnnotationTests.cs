using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Verifies display names annotate collection members beside the raw value without changing equality
/// (TC-32, TC-33, TC-35). Feature 147 is not merged on this branch, so the tests use the value formatter
/// extension point that feature 147 plugs into.
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class CollectionNameAnnotationTests
{
    private const string Alice = "11111111-1111-1111-1111-111111111111";
    private const string Bob = "22222222-2222-2222-2222-222222222222";

    private sealed class NameFormatter(IReadOnlyDictionary<string, string> names) : IValueFormatter
    {
        public string? TryFormat(ServiceResolutionContext context)
            => context.Value is { } value && names.TryGetValue(value, out var name) ? $"`{value}` ({name})" : null;
    }

    private static string RenderWithNames(Resource resource, IReadOnlyDictionary<string, string> names)
    {
        var formatters = new ValueFormatterRegistry();
        formatters.Register(new MatchPattern(null, null, "^items(?:\\[\\d+\\])?$", null), new NameFormatter(names));
        var model = BuildModel(PlanJson([resource]));
        return new MarkdownRenderer(valueFormatterRegistry: formatters).Render(model);
    }

    [Test]
    public void Update_SameGuidBeforeAndAfter_WithNameMappingOnly_ProducesNoRow()
    {
        var resource = Res(["update"], "{\"items\":" + L(Alice) + ",\"n\":1}", "{\"items\":" + L(Alice) + ",\"n\":2}");

        var markdown = RenderWithNames(resource, new Dictionary<string, string> { [Alice] = "Alice" });

        markdown.Should().NotContain("| items |");
        BuildModel(PlanJson([resource])).Changes.Single().AttributeChanges.Select(a => a.Name).Should().Equal("n");
    }

    [Test]
    public void Update_TwoDistinctGuidsMappedToSameName_RemainDistinct()
    {
        var resource = Res(["update"], "{\"items\":" + L(Alice) + "}", "{\"items\":" + L(Bob) + "}");

        var markdown = RenderWithNames(resource, new Dictionary<string, string> { [Alice] = "Same Name", [Bob] = "Same Name" });

        var row = markdown.Split('\n').Single(line => line.StartsWith("| items |", StringComparison.Ordinal));
        row.Should().Contain($"➖ `{Alice}` (Same Name)");
        row.Should().Contain($"➕ `{Bob}` (Same Name)");
        BuildModel(PlanJson([resource])).Changes.Single().AttributeChanges.Single().Collection!.Before.Single().Status
            .Should().Be(CollectionElementStatus.Removed);
    }

    [Test]
    public void Update_GuidCollectionWithMapping_AnnotatesNameBesideGuid()
    {
        var resource = Res(["update"], "{\"items\":" + L(Alice) + "}", "{\"items\":" + L(Alice, Bob) + "}");

        var row = RenderWithNames(resource, new Dictionary<string, string> { [Alice] = "Alice", [Bob] = "Bob" })
            .Split('\n').Single(line => line.StartsWith("| items |", StringComparison.Ordinal));

        row.Should().Contain($"`{Alice}` (Alice)");
        row.Should().Contain($"➕ `{Bob}` (Bob)");
    }
}
