using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Verifies effective no-op outputs can be filtered before report grouping.
/// Related feature: docs/features/146-generic-resource-review-clarity/specification.md.
/// </summary>
public class ReportModelBuilderHideUnchangedOutputsTests
{
    /// <summary>
    /// Verifies no-op and absent-action outputs are removed while CRUD values and masking remain unchanged.
    /// </summary>
    [Test]
    public void Build_HideUnchangedOutputs_FiltersOnlyNoOpsAndPreservesRetainedOutputState()
    {
        var outputs = new Dictionary<string, OutputChange>
        {
            ["created"] = new(["create"], before: "old-create", after: "created-value"),
            ["updated"] = new(["update"], before: "old-update", after: "private-update", afterUnknown: true, afterSensitive: true),
            ["computed"] = new(["update"], before: "old-computed", after: "placeholder", afterUnknown: true),
            ["deleted"] = new(["delete"], before: "private-delete", after: "ignored-delete", beforeSensitive: true),
            ["no_op"] = new(["no-op"], before: "before-no-op", after: "after-no-op"),
            ["missing_action"] = new([], before: "before-missing", after: "after-missing")
        };

        var model = BuildModel(outputs, hideUnchangedOutputs: true);
        var byName = model.GlobalOutputs.ToDictionary(output => output.Name);

        byName.Keys.Should().BeEquivalentTo("created", "updated", "computed", "deleted");
        byName["created"].Value.Should().Be("created-value");
        byName["created"].Action.Should().Be("create");
        byName["created"].ActionSymbol.Should().NotBeNullOrWhiteSpace();
        byName["updated"].Value.Should().Be("private-update");
        byName["updated"].Action.Should().Be("update");
        byName["updated"].IsComputed.Should().BeTrue();
        byName["updated"].IsMasked.Should().BeTrue();
        byName["computed"].Action.Should().Be("update");
        byName["computed"].Value.Should().Be("placeholder");
        byName["computed"].IsComputed.Should().BeTrue();
        byName["deleted"].Value.Should().Be("private-delete");
        byName["deleted"].Action.Should().Be("delete");
        byName["deleted"].IsMasked.Should().BeTrue();

        var markdown = new MarkdownRenderer().Render(model);
        markdown.Should().NotContain("private-update");
        markdown.Should().NotContain("private-delete");
        markdown.Should().Contain("(sensitive value)");
        markdown.Should().Contain("(known after apply)");
    }

    /// <summary>
    /// Verifies unchanged outputs remain selected by default and absent actions keep no-op value semantics.
    /// </summary>
    [Test]
    public void Build_DefaultOutputOptions_RetainsNoOpsAndUsesAfterValue()
    {
        var outputs = new Dictionary<string, OutputChange>
        {
            ["no_op"] = new(["no-op"], before: "before", after: "after"),
            ["missing_action"] = new([], before: "before-missing", after: "after-missing")
        };

        var model = BuildModel(outputs, hideUnchangedOutputs: false);

        model.GlobalOutputs.Should().HaveCount(2);
        model.GlobalOutputs.Single(output => output.Name == "no_op").Action.Should().Be("no-op");
        model.GlobalOutputs.Single(output => output.Name == "no_op").Value.Should().Be("after");
        model.GlobalOutputs.Single(output => output.Name == "missing_action").Action.Should().Be("no-op");
        model.GlobalOutputs.Single(output => output.Name == "missing_action").Value.Should().Be("after-missing");
    }

    /// <summary>
    /// Verifies filtering removes empty output-only modules but keeps modules with resources.
    /// </summary>
    [Test]
    public void Build_HideUnchangedOutputs_RemovesEmptyOutputOnlyModuleGroups()
    {
        const string configuration = """
            {
              "root_module": {
                "outputs": { "root_noop": {} },
                "modules": [
                  { "address": "module.output_only", "outputs": { "module_noop": {} } },
                  { "address": "module.with_resource", "outputs": { "resource_module_noop": {}, "resource_module_update": {} } }
                ]
              }
            }
            """;
        var resource = new ResourceChange(
            "module.with_resource.example_resource.sample",
            "module.with_resource",
            "managed",
            "example_resource",
            "sample",
            "registry.terraform.io/hashicorp/example",
            new Change(["create"], before: null, after: """{"name":"sample"}""", afterUnknown: null, beforeSensitive: null, afterSensitive: null));
        var outputs = new Dictionary<string, OutputChange>
        {
            ["root_noop"] = new(["no-op"], after: "root"),
            ["module_noop"] = new(["no-op"], after: "module-only"),
            ["resource_module_noop"] = new(["no-op"], after: "resource-module"),
            ["resource_module_update"] = new(["update"], before: "before", after: "after")
        };

        var model = BuildModel(
            outputs,
            hideUnchangedOutputs: true,
            configuration: configuration,
            resources: [resource]);
        var module = model.ModuleChanges.Single();

        model.GlobalOutputs.Should().BeEmpty();
        module.ModuleAddress.Should().Be("module.with_resource");
        module.Changes.Should().ContainSingle();
        module.Outputs.Should().ContainSingle(output => output.Name == "resource_module_update");

        var markdown = new MarkdownRenderer().Render(model);
        var moduleHeadingIndex = markdown.IndexOf("📦\u00A0Module:", StringComparison.Ordinal);
        var outputHeadingIndex = markdown.IndexOf("📤\u00A0Outputs", StringComparison.Ordinal);
        moduleHeadingIndex.Should().BeGreaterThanOrEqualTo(0);
        outputHeadingIndex.Should().BeGreaterThan(moduleHeadingIndex);
    }

    /// <summary>
    /// Verifies filtering all outputs creates no empty sections or output-only module headings.
    /// </summary>
    [Test]
    public void Render_HideUnchangedOutputsWithNoRemainingOutputs_OmitsOutputSections()
    {
        const string configuration = """
            {
              "root_module": {
                "outputs": { "root_noop": {} },
                "modules": [
                  { "address": "module.output_only", "outputs": { "module_noop": {} } }
                ]
              }
            }
            """;
        var outputs = new Dictionary<string, OutputChange>
        {
            ["root_noop"] = new(["no-op"], after: "root"),
            ["module_noop"] = new(["no-op"], after: "module")
        };
        var model = BuildModel(outputs, hideUnchangedOutputs: true, configuration: configuration);

        var markdown = new MarkdownRenderer().Render(model);

        model.GlobalOutputs.Should().BeEmpty();
        model.ModuleChanges.Should().BeEmpty();
        markdown.Should().NotContain("📤");
        markdown.Should().NotContain("module.output_only");
    }

    /// <summary>
    /// Verifies a plan without outputs remains valid when unchanged-output filtering is enabled.
    /// </summary>
    [Test]
    public void Build_HideUnchangedOutputsWithNoOutputs_ReturnsEmptyOutputCollections()
    {
        var model = BuildModel(outputs: null, hideUnchangedOutputs: true);

        model.GlobalOutputs.Should().BeEmpty();
        model.ModuleChanges.Should().BeEmpty();
    }

    /// <summary>
    /// Builds a report model from synthetic outputs and optional resource/configuration context.
    /// </summary>
    /// <param name="outputs">Output changes or null.</param>
    /// <param name="hideUnchangedOutputs">Whether effective no-op outputs are hidden.</param>
    /// <param name="configuration">Configuration JSON or null.</param>
    /// <param name="resources">Resource changes to retain in module groups.</param>
    /// <returns>The assembled report model.</returns>
    private static ReportModel BuildModel(
        IReadOnlyDictionary<string, OutputChange>? outputs,
        bool hideUnchangedOutputs,
        object? configuration = null,
        IReadOnlyList<ResourceChange>? resources = null)
    {
        return new ReportModelBuilder(
            new ReportModelBuilderOptions(HideUnchangedOutputs: hideUnchangedOutputs))
            .Build(new TerraformPlan(
                "1.0",
                "1.0",
                resources ?? [],
                Configuration: ParseState(configuration),
                OutputChanges: outputs));
    }

    /// <summary>
    /// Converts optional configuration JSON to an owned element for plan construction.
    /// </summary>
    /// <param name="json">JSON text or null.</param>
    /// <returns>A cloned JSON element or null.</returns>
    private static JsonElement? ParseState(object? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = json is string text
            ? JsonDocument.Parse(text)
            : JsonDocument.Parse(JsonSerializer.Serialize(json));
        return document.RootElement.Clone();
    }
}
