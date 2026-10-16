using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Providers.MsGraph;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Rendering tests for compact primitive collections in generic tables (TC-10 to TC-13, TC-22 to TC-34,
/// TC-40 to TC-45, TC-50 to TC-52, TC-54, TC-60 to TC-64, TC-104, TC-105, TC-107, TC-110 to TC-112, TC-116).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class CollectionRenderingTests
{
    private static string Items(params string[] values) => "{\"items\":" + L(values) + "}";

    private static string Row(string markdown, string name)
        => markdown.Split('\n').Single(line => line.StartsWith($"| {name} |", StringComparison.Ordinal));

    private static int Count(string text, string value) => text.Split([value], StringSplitOptions.None).Length - 1;

    // ---- Task 7: update and replace ----

    [Test]
    public void Update_PrimitiveList_RendersOneRowWithBeforeAndAfterColumns()
    {
        var markdown = Render(Res(["update"], Items("a", "b"), Items("a", "b", "c")));

        markdown.Should().Contain("| Attribute | Before | After |");
        Row(markdown, "items").Should().Be("| items | `a`<br>`b` | `a`<br>`b`<br>➕\u00A0`c` |");
        markdown.Should().NotContain("items[0]");
    }

    [Test]
    public void Replace_PrimitiveList_RendersOneRow()
    {
        var markdown = Render(Res(["delete", "create"], Items("a"), Items("b")));

        markdown.Should().Contain("| Attribute | Before | After |");
        Row(markdown, "items").Should().Be("| items | ➖\u00A0`a` | ➕\u00A0`b` |");
    }

    [Test]
    public void Update_InsertionIntoLongList_NoReplacementCascade()
    {
        var before = Enumerable.Range(0, 12).Select(i => $"v{i:D2}").ToArray();
        var after = before.Take(3).Append("inserted").Concat(before.Skip(3)).ToArray();

        var markdown = Render(Res(["update"], Items(before), Items(after)));

        Count(markdown, "➕").Should().Be(1);
        Count(markdown, "➖").Should().Be(0);
    }

    [Test]
    public void Update_DuplicateOccurrences_RenderedWithMultiplicity()
    {
        var row = Row(Render(Res(["update"], Items("a", "b"), Items("a", "a", "b"))), "items");

        row.Should().Be("| items | `a`<br>`b` | `a`<br>➕\u00A0`a`<br>`b` |");
        Count(row, "➕").Should().Be(1);
    }

    [Test]
    public void Update_OrderOnlyChange_RendersRowWithOrderChangedLine()
    {
        var row = Row(Render(Res(["update"], Items("a", "b"), Items("b", "a"))), "items");

        row.Should().Contain("🔀\u00A0order changed");
        row.Should().NotContain("➖").And.NotContain("➕");
        row.Should().EndWith("🔀\u00A0order changed |");
    }

    [Test]
    public void Update_SortedSetMembershipChange_OutputHasNoOrderChangedLine()
    {
        var markdown = Render(Res(["update"], Items("a", "c", "e"), Items("a", "b", "c", "e")));

        markdown.Should().NotContain("order changed");
        Count(markdown, "➕").Should().Be(1);
    }

    [Test]
    public void Update_GuidCollectionWithoutMappings_ShowsRawGuidsWithMarkers()
    {
        const string keep = "11111111-1111-1111-1111-111111111111";
        const string removed = "22222222-2222-2222-2222-222222222222";
        const string added = "33333333-3333-3333-3333-333333333333";
        var resource = Res(
            ["update"],
            "{\"api_version\":\"v1.0\",\"url\":\"/policies\",\"ignore_missing_property\":true,\"body\":{\"conditions\":{\"users\":{\"excludeUsers\":" + L(keep, removed) + "}}}}",
            "{\"api_version\":\"v1.0\",\"url\":\"/policies\",\"ignore_missing_property\":true,\"body\":{\"conditions\":{\"users\":{\"excludeUsers\":" + L(keep, added) + "}}}}",
            address: "msgraph_resource.policy",
            type: "msgraph_resource",
            provider: "registry.terraform.io/microsoft/msgraph");
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new MsGraphModule());
        var plan = new TerraformPlanParser().Parse(PlanJson([resource]));
        var model = new ReportModelBuilder(
            new ReportModelBuilderOptions(),
            new ReportModelBuilderServices(ProviderRegistry: registry)).Build(plan);

        var markdown = new MarkdownRenderer(providerRegistry: registry).Render(model);

        Row(markdown, "body.conditions.users.excludeUsers").Should().Contain($"`{keep}`").And.Contain($"➖\u00A0`{removed}`").And.Contain($"➕\u00A0`{added}`");
        markdown.Should().Contain("api_version").And.Contain("/policies");
        markdown.Should().NotContain("excludeUsers[0]");
    }

    [Test]
    public void Update_ElementTextIdenticalToMarker_StaysDistinguishableFromRealMarker()
    {
        var row = Row(Render(Res(["update"], Items("➕\u00A0x"), Items("➕\u00A0x", "x"))), "items");

        row.Should().Contain("`➕\u00A0x`");
        row.Should().Contain("➕\u00A0`x`");
        row.Should().NotContain("➕\u00A0`➕\u00A0x`");
    }

    // ---- Task 8: create, delete, values ----

    [Test]
    public void Create_PrimitiveList_RendersOneRowWithValuesSeparatedByBreaks()
    {
        var markdown = Render(Res(["create"], null, Items("a", "b", "c")));

        Row(markdown, "items").Should().Be("| items | `a`<br>`b`<br>`c` |");
    }

    [Test]
    public void Delete_PrimitiveList_RendersFullBeforeCollection()
    {
        var markdown = Render(Res(["delete"], Items("a", "b"), null));

        Row(markdown, "items").Should().Be("| items | `a`<br>`b` |");
    }

    [Test]
    public void Update_EmptyAbsentAndNull_RenderDistinctCells()
    {
        Row(Render(Res(["update"], "{\"items\":[]}", Items("a"))), "items").Should().Be("| items | `[]` | ➕\u00A0`a` |");
        Row(Render(Res(["update"], "{\"items\":null}", Items("a"))), "items").Should().Be("| items | `null` | ➕\u00A0`a` |");
        Row(Render(Res(["update"], "{\"n\":1}", "{\"n\":1,\"items\":[\"a\"]}")), "items").Should().Be("| items | - | ➕\u00A0`a` |");
        Row(Render(Res(["update"], Items("a"), "{\"items\":[]}")), "items").Should().Be("| items | ➖\u00A0`a` | `[]` |");
    }

    [Test]
    public void Create_EmptyCollection_RendersEmptyBrackets()
    {
        Row(Render(Res(["create"], null, "{\"items\":[]}")), "items").Should().Be("| items | `[]` |");
    }

    [Test]
    public void Render_ElementsWithMarkdownSpecialCharacters_AreEscaped()
    {
        var markdown = Render(Res(["create"], null, Items("a|b", "tick`tick", "<br>", "<b>&amp;", "")));

        var rowText = Row(markdown.Replace("\r", string.Empty, StringComparison.Ordinal), "items");
        rowText.Should().Contain("a\\|b").And.Contain("tick\\`tick");
        rowText.Should().Contain("`<br>`");
        rowText.Should().Contain("`\"\"`");
        rowText.Replace("\\|", string.Empty, StringComparison.Ordinal).Count(c => c == '|').Should().Be(3);
    }

    [Test]
    public void Render_UnchangedCollectionWithShowUnchangedValues_RendersOneRow()
    {
        var resource = Res(["update"], "{\"items\":[\"a\"],\"n\":1}", "{\"items\":[\"a\"],\"n\":2}");

        Render(resource).Should().NotContain("| items |");
        Row(Render(resource, new ReportModelBuilderOptions(ShowUnchangedValues: true)), "items").Should().Be("| items | `a` | `a` |");
    }

    [Test]
    public void Render_MixedScalarListWithNull_ShowsNullElement()
    {
        var row = Row(Render(Res(["create"], null, "{\"items\":[1,\"a\",true,null]}")), "items");

        row.Should().StartWith("| items | `1`<br>`a`<br>`✅").And.EndWith("true`<br>`null` |");
    }

    [Test]
    public void Render_ListOfObjectsAndArrayTypeChange_KeepExistingRowsWithoutException()
    {
        Render(Res(["update"], "{\"items\":[{\"a\":1}]}", "{\"items\":[{\"a\":2}]}")).Should().Contain("| items[0].a |");
        Render(Res(["update"], "{\"items\":[\"a\"]}", "{\"items\":\"b\"}")).Should().Contain("items");
    }

    [Test]
    public void Render_AzureDevOpsMembersFormatter_StillAppliesPerElement()
    {
        const string member = "aad.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5LTI0MDI5ODY0MTMtMjE3OTQwODYxNi0zLTM4Mzg1ODYwMTUtMzIyMTk1OTc5OC0xMjM0NTY3ODkw";
        const string group = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5LTI0MDI5ODY0MTMtMjE3OTQwODYxNi0zLTM4Mzg1ODYwMTUtMzIyMTk1OTc5OC0xMjM0NTY3ODkw";
        var module = new AzureDevOpsModule(
            azdoUserMapper: new AzdoUserMapper(new Dictionary<string, string> { [member] = "Build Service" }.ToFrozenDictionary(), diagnostics: null),
            azdoGroupMapper: new AzdoGroupMapper(new Dictionary<string, string> { [group] = "Readers" }.ToFrozenDictionary(), diagnostics: null));
        var registry = new ProviderRegistry();
        registry.RegisterProvider(module);
        var resource = Res(
            ["create"], null, "{\"group\":\"" + group + "\",\"members\":" + L(member) + "}",
            address: "azuredevops_group_membership.example",
            type: "azuredevops_group_membership",
            provider: "registry.terraform.io/microsoft/azuredevops");
        var model = new ReportModelBuilder(new ReportModelBuilderOptions(), new ReportModelBuilderServices(ProviderRegistry: registry))
            .Build(new TerraformPlanParser().Parse(PlanJson([resource])));

        var markdown = new MarkdownRenderer(providerRegistry: registry).Render(model);

        markdown.Should().Contain("Build Service");
        markdown.Should().NotContain("members[0]");
    }

    // ---- Task 3/4/5 rendering ----

    [Test]
    public void Update_WholeCollectionUnknownAfter_RendersKnownAfterApply()
    {
        var row = Row(Render(Res(["update"], Items("a"), "{}", afterUnknown: "{\"items\":true}")), "items");

        row.Should().Contain("(known after apply)");
    }

    [Test]
    public void Update_PartialUnknownElement_RendersLabelWithoutMarkers()
    {
        var row = Row(
            Render(Res(["update"], Items("a", "b", "c"), "{\"items\":[\"a\",null,\"c\"]}", afterUnknown: "{\"items\":[false,true,false]}")),
            "items");

        row.Should().Contain("(known after apply)");
        row.Should().NotContain("➖").And.NotContain("➕").And.NotContain("🔀");
    }

    [Test]
    public void Update_SensitiveCollectionReorderAndDuplicates_LeakNothing()
    {
        var resource = Res(
            ["update"], Items("SECRETA", "SECRETB"), Items("SECRETB", "SECRETA", "SECRETA"),
            beforeSensitive: "{\"items\":true}", afterSensitive: "{\"items\":true}");

        var markdown = Render(resource);

        Row(markdown, "items").Should().Be("| items | `(sensitive)` | `(sensitive)` |");
        markdown.Should().NotContain("SECRET").And.NotContain("➖").And.NotContain("➕").And.NotContain("🔀");
    }

    [Test]
    public void Update_OneSensitiveElement_NeitherSiblingsNorCountAppear()
    {
        var resource = Res(
            ["update"], Items("PUBLICSIBLING", "SECRETB"), Items("PUBLICSIBLING", "SECRETB", "OTHER"),
            beforeSensitive: "{\"items\":[false,true]}", afterSensitive: "{\"items\":[false,true,false]}");

        var markdown = Render(resource);

        markdown.Should().NotContain("PUBLICSIBLING").And.NotContain("SECRETB").And.NotContain("OTHER");
    }

    [Test]
    public void Update_SensitiveCollectionWithShowSensitive_RendersNormalDiff()
    {
        var resource = Res(
            ["update"], Items("a"), Items("a", "b"),
            beforeSensitive: "{\"items\":true}", afterSensitive: "{\"items\":true}");

        Row(Render(resource, new ReportModelBuilderOptions(ShowSensitive: true)), "items").Should().Be("| items | `a` | `a`<br>➕\u00A0`b` |");
    }

    // ---- Task 9: large elements ----

    [Test]
    public void Update_LargeElement_RendersInLargeSectionOneOccurrencePerLine()
    {
        var large = new string('x', 150);
        var markdown = Render(Res(["update"], Items("a", large), Items("a", large, "b")));

        markdown.Should().Contain("Large values: items");
        markdown.Should().NotContain("| items |");
        markdown.Should().Contain("➕\u00A0b");
        markdown.Should().Contain(large);
    }

    [Test]
    public void Update_LargeElementReorder_ReportsOrderChangeNotRemoveAdd()
    {
        var large = new string('x', 150);
        var markdown = Render(Res(["update"], Items("a", large), Items(large, "a")));

        markdown.Should().Contain("🔀\u00A0order changed");
        markdown.Should().NotContain("➖\u00A0").And.NotContain("➕\u00A0");
    }

    [Test]
    public void Create_ElementWithNewline_RendersInLargeSectionWithEscapedNewline()
    {
        var markdown = Render(Res(["create"], null, Items("a", "line1\nline2")));

        markdown.Should().Contain("Large values: items");
        markdown.Should().Contain("line1\\nline2");
    }

    [Test]
    public void Create_LargeElement_RendersSingleBlockWithoutMarkers()
    {
        var large = new string('x', 150);
        var markdown = Render(Res(["create"], null, Items("a", large)));

        markdown.Should().Contain("Large values: items");
        markdown.Should().NotContain("➕\u00A0x");
        markdown.Should().NotContain("Before:");
    }

    [Test]
    public void Update_ShortCollection_IsNotLarge()
    {
        Render(Res(["update"], Items("a"), Items("b"))).Should().NotContain("Large values");
    }
}
