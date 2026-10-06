using System;
using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Verifies import context for resources whose prior state has no reviewable values.
/// </summary>
public class ImportedPriorStateContextTests
{
    private const string ImportNote = "> 📥 Prior state from import is empty; the values below show the full desired state.";

    [Test]
    public void Render_ImportedResourceWithNullPriorState_ExplainsDesiredStateBeforeAttributes()
    {
        var markdown = Render(BuildModel("example_resource", ["create"], beforeJson: null, afterJson: "{\"name\":\"legacy\"}"));

        AssertNotePrecedesAttributes(markdown);
        markdown.Should().NotContain("newly created");
    }

    [Test]
    public void Render_ImportedResourcesWithOnlyEmptyOrHousekeepingState_ShowNote()
    {
        string?[] emptyStates =
        [
            null,
            "{}",
            "{\"id\":\"existing-id\",\"body\":{}}",
            "{\"body\":{},\"nested\":{\"empty\":\"\",\"missing\":null,\"items\":[]}}"
        ];

        foreach (var beforeJson in emptyStates)
        {
            AssertNotePrecedesAttributes(Render(BuildModel(
                "example_resource",
                ["create"],
                beforeJson,
                afterJson: "{\"desired\":\"value\"}")));
        }
    }

    [Test]
    public void Render_ImportedResourcesWithSubstantivePriorState_OmitNote()
    {
        string[] meaningfulStates =
        [
            "{\"name\":\"existing\"}",
            "{\"zero\":0}",
            "{\"disabled\":false}",
            "{\"items\":[\"value\"]}",
            "{\"body\":{\"id\":\"nested-id\"}}"
        ];

        foreach (var beforeJson in meaningfulStates)
        {
            Render(BuildModel("example_resource", ["update"], beforeJson, afterJson: "{\"desired\":\"value\"}"))
                .Should()
                .NotContain(ImportNote, beforeJson);
        }
    }

    [Test]
    public void Render_OrdinaryCreate_DoesNotShowImportNote()
    {
        Render(BuildModel("example_resource", ["create"], beforeJson: null, afterJson: "{\"name\":\"new\"}", isImport: false))
            .Should()
            .NotContain(ImportNote);
    }

    [Test]
    public void Render_ImportedResourceWithSensitiveDesiredValue_KeepsMasking()
    {
        var markdown = Render(BuildModel(
            "example_resource",
            ["create"],
            beforeJson: null,
            afterJson: "{\"token\":\"do-not-leak\"}",
            afterSensitiveJson: "{\"token\":true}"));

        AssertNotePrecedesAttributes(markdown);
        markdown.Should().Contain("(sensitive)");
        markdown.Should().NotContain("do-not-leak");
    }

    [Test]
    public void Render_AzApiImportedResource_ConsumesSharedImportContextFlag()
    {
        var markdown = Render(BuildModel(
            "azapi_resource",
            ["create"],
            beforeJson: "{\"id\":\"existing-id\",\"body\":{}}",
            afterJson: "{\"id\":\"planned-id\",\"body\":{\"name\":\"legacy\"}}",
            providerName: "registry.terraform.io/azure/azapi"));

        markdown.Should().Contain(ImportNote);
        markdown.IndexOf(ImportNote, StringComparison.Ordinal)
            .Should()
            .BeLessThan(markdown.IndexOf("| Attribute | Value |", StringComparison.Ordinal));
        markdown.Should().Contain("body.name");
    }

    private static void AssertNotePrecedesAttributes(string markdown)
    {
        var notePosition = markdown.IndexOf(ImportNote, StringComparison.Ordinal);
        var attributesPosition = markdown.IndexOf("| Attribute | Value |", StringComparison.Ordinal);

        notePosition.Should().BeGreaterThanOrEqualTo(0);
        attributesPosition.Should().BeGreaterThan(notePosition);
    }

    private static string Render(ReportModel model)
    {
        return new MarkdownRenderer().Render(model);
    }

    private static ReportModel BuildModel(
        string type,
        IReadOnlyList<string> actions,
        string? beforeJson,
        string? afterJson,
        string? afterSensitiveJson = null,
        bool isImport = true,
        string? providerName = null)
    {
        var change = new Change(
            actions,
            before: ParseJson(beforeJson),
            after: ParseJson(afterJson),
            afterUnknown: null,
            beforeSensitive: null,
            afterSensitive: ParseJson(afterSensitiveJson),
            importing: isImport ? new Importing { Id = "import-id" } : null);
        var resourceChange = new ResourceChange(
            $"{type}.legacy",
            null,
            "managed",
            type,
            "legacy",
            providerName ?? "registry.terraform.io/example/provider",
            change);

        return new ReportModelBuilder().Build(new TerraformPlan("1.0", "1.0", [resourceChange]));
    }

    private static JsonElement? ParseJson(string? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
