using System.Diagnostics;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Covers edge cases for parent-child resource merging.
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </summary>
public class ReportModelBuilderParentChildEdgeCaseTests
{
    private const string ParentType = "edge_parent";
    private const string ChildType = "edge_child";

    /// <summary>
    /// Ensures children referencing a non-existent parent remain separate.
    /// </summary>
    [Test]
    public void Build_ChildWithoutParent_RemainsSeparate()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_child.orphan",
                    null,
                    "managed",
                    ChildType,
                    "orphan",
                    "custom",
                    new Change(["create"], null, JsonDocument.Parse("{\"parent_id\":\"missing\",\"member\":\"value\"}").RootElement, null, null, null))
            });

        var model = BuildModel(plan);

        model.Changes.Should().ContainSingle(change => change.Type == ChildType);
    }

    /// <summary>
    /// Ensures parents without children do not get child groups.
    /// </summary>
    [Test]
    public void Build_ParentWithoutChildren_HasNoChildGroups()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_parent.empty",
                    null,
                    "managed",
                    ParentType,
                    "empty",
                    "custom",
                    new Change(["create"], null, JsonDocument.Parse("{\"id\":\"parent-1\"}").RootElement, null, null, null))
            });

        var model = BuildModel(plan);

        var parent = model.Changes.Should().ContainSingle().Subject;
        parent.ChildResourceGroups.Should().BeEmpty();
        parent.SummaryHtml.Should().NotContain("members");
    }

    /// <summary>
    /// Ensures empty inline attributes do not produce child groups.
    /// </summary>
    [Test]
    public void Build_EmptyInlineAttribute_DoesNotRenderChildGroups()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_parent.empty_members",
                    null,
                    "managed",
                    ParentType,
                    "empty_members",
                    "custom",
                    new Change(["update"], null, JsonDocument.Parse("{\"id\":\"parent-1\",\"members\":[]}").RootElement, null, null, null))
            });

        var model = BuildModel(plan);

        var parent = model.Changes.Should().ContainSingle().Subject;
        parent.ChildResourceGroups.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures missing child attributes do not cause failures.
    /// </summary>
    [Test]
    public void Build_ChildWithMissingAttributes_HandlesGracefully()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_parent.parent",
                    null,
                    "managed",
                    ParentType,
                    "parent",
                    "custom",
                    new Change(["create"], null, JsonDocument.Parse("{\"id\":\"parent-1\"}").RootElement, null, null, null)),
                new ResourceChange(
                    "edge_child.missing_member",
                    null,
                    "managed",
                    ChildType,
                    "missing_member",
                    "custom",
                    new Change(["create"], null, JsonDocument.Parse("{\"parent_id\":\"parent-1\"}").RootElement, null, null, null))
            });

        var model = BuildModel(plan);

        var parent = model.Changes.Should().ContainSingle().Subject;
        parent.ChildResourceGroups.Should().ContainSingle();
        parent.ChildResourceGroups[0].Rows.Should().ContainSingle();
        parent.ChildResourceGroups[0].Rows[0].Values["member"].Should().BeEmpty();
    }

    /// <summary>
    /// Ensures extractor exceptions do not crash merging and keep children standalone.
    /// </summary>
    [Test]
    public void Build_ExtractorThrows_ChildRemainsStandalone()
    {
        var plan = BuildPlanWithSingleChild();

        var model = BuildModel(plan, new ThrowingExtractorProviderModule());

        model.Changes.Should().Contain(change => change.Type == ParentType);
        model.Changes.Should().Contain(change => change.Type == ChildType);

        var parent = model.Changes.Single(change => change.Type == ParentType);
        parent.ChildResourceGroups.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures invalid child JSON payloads do not crash merging.
    /// </summary>
    [Test]
    public void Build_InvalidChildJson_ChildRemainsStandalone()
    {
        var plan = BuildPlanWithInvalidChildPayload();

        var model = BuildModel(plan, new InvalidJsonExtractorProviderModule());

        model.Changes.Should().Contain(change => change.Type == ParentType);
        model.Changes.Should().Contain(change => change.Type == ChildType);

        var parent = model.Changes.Single(change => change.Type == ParentType);
        parent.ChildResourceGroups.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures merging scales without excessive overhead for larger plans.
    /// </summary>
    [Test]
    public void Build_LargeChildSet_CompletesQuickly()
    {
        const int childCount = 500;
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            CreateLargePlan(childCount));

        var builder = CreateBuilder();
        var stopwatch = Stopwatch.StartNew();
        builder.Build(plan);
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Ensures self-referential relationships do not cause crashes or infinite loops.
    /// </summary>
    [Test]
    public void Build_SelfReferencingRelationship_DoesNotCrash()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "self_type.example",
                    null,
                    "managed",
                    "self_type",
                    "example",
                    "custom",
                    new Change(["create"], null, JsonDocument.Parse("{\"id\":\"self-1\",\"parent_id\":\"self-1\"}").RootElement, null, null, null))
            });

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new SelfReferencingProviderModule());
        var model = new ReportModelBuilder(providerRegistry: providerRegistry).Build(plan);

        model.Changes.Should().ContainSingle();
    }

    /// <summary>
    /// Builds a test plan with many child resources.
    /// </summary>
    /// <param name="childCount">The number of child resources to include.</param>
    /// <returns>The resource change list for the plan.</returns>
    private static List<ResourceChange> CreateLargePlan(int childCount)
    {
        var changes = new List<ResourceChange>
        {
            new(
                "edge_parent.large",
                null,
                "managed",
                ParentType,
                "large",
                "custom",
                new Change(["create"], null, JsonDocument.Parse("{\"id\":\"parent-1\"}").RootElement, null, null, null))
        };

        for (var index = 0; index < childCount; index++)
        {
            changes.Add(new ResourceChange(
                $"edge_child.child_{index}",
                null,
                "managed",
                ChildType,
                $"child_{index}",
                "custom",
                new Change(["create"], null, JsonDocument.Parse($"{{\"parent_id\":\"parent-1\",\"member\":\"member-{index}\"}}").RootElement, null, null, null)));
        }

        return changes;
    }

    /// <summary>
    /// Builds a report model using the test provider registry.
    /// </summary>
    /// <param name="plan">The Terraform plan to render.</param>
    /// <returns>The built report model.</returns>
    private static ReportModel BuildModel(TerraformPlan plan)
    {
        var builder = CreateBuilder();
        return builder.Build(plan);
    }

    /// <summary>
    /// Builds a report model using a custom provider module.
    /// </summary>
    /// <param name="plan">The Terraform plan to render.</param>
    /// <param name="providerModule">The provider module to register.</param>
    /// <returns>The built report model.</returns>
    private static ReportModel BuildModel(TerraformPlan plan, IProviderModule providerModule)
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(providerModule);
        return new ReportModelBuilder(providerRegistry: providerRegistry).Build(plan);
    }

    /// <summary>
    /// Creates a report builder configured with the test provider module.
    /// </summary>
    /// <returns>The configured report model builder.</returns>
    private static ReportModelBuilder CreateBuilder()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new EdgeCaseProviderModule());
        return new ReportModelBuilder(providerRegistry: providerRegistry);
    }

    /// <summary>
    /// Builds a plan with a single parent and child for extractor tests.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithSingleChild()
    {
        var parentAfter = JsonDocument.Parse("{\"id\":\"parent-1\"}").RootElement;
        var childAfter = JsonDocument.Parse("{\"parent_id\":\"parent-1\",\"member\":\"value\"}").RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_parent.parent",
                    null,
                    "managed",
                    ParentType,
                    "parent",
                    "custom",
                    new Change(["create"], null, parentAfter, null, null, null)),
                new ResourceChange(
                    "edge_child.child",
                    null,
                    "managed",
                    ChildType,
                    "child",
                    "custom",
                    new Change(["create"], null, childAfter, null, null, null))
            });
    }

    /// <summary>
    /// Builds a plan with an invalid JSON payload for child extraction.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithInvalidChildPayload()
    {
        var parentAfter = JsonDocument.Parse("{\"id\":\"parent-1\"}").RootElement;
        var childAfter = JsonDocument.Parse("{\"parent_id\":\"parent-1\",\"payload\":\"{ invalid json }\"}").RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_parent.parent",
                    null,
                    "managed",
                    ParentType,
                    "parent",
                    "custom",
                    new Change(["create"], null, parentAfter, null, null, null)),
                new ResourceChange(
                    "edge_child.child",
                    null,
                    "managed",
                    ChildType,
                    "child",
                    "custom",
                    new Change(["create"], null, childAfter, null, null, null))
            });
    }

    /// <summary>
    /// Provider module that registers a self-referencing relationship.
    /// </summary>
    private sealed class SelfReferencingProviderModule : IProviderModule
    {
        /// <summary>
        /// Gets the provider name for the self-referencing module.
        /// </summary>
        public string ProviderName => "custom";

        /// <summary>
        /// Gets the template resource prefix for the self-referencing module.
        /// </summary>
        public string TemplateResourcePrefix => string.Empty;

        /// <summary>
        /// Registers helper functions (none for this test module).
        /// </summary>
        /// <param name="scriptObject">The script object to register helpers with.</param>
        public void RegisterHelpers(ScriptObject scriptObject)
        {
        }

        /// <summary>
        /// Registers resource factories (none for this test module).
        /// </summary>
        /// <param name="registry">The factory registry.</param>
        public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
        {
        }

        /// <summary>
        /// Registers the self-referencing relationship.
        /// </summary>
        /// <param name="registry">The parent-child relationship registry.</param>
        public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
        {
            registry.Register(new ParentChildRelationship
            {
                ParentResourceType = "self_type",
                ChildResourceType = "self_type",
                InlineAttributeName = null,
                ChildReferenceAttribute = "parent_id",
                ParentIdAttribute = "id",
                ChildGroupLabel = "Children",
                TableColumns = [new ChildTableColumn("Child", "child")],
                RowExtractor = new EdgeCaseRowExtractor()
            });
        }
    }

    /// <summary>
    /// Provider module used for parent-child edge case tests.
    /// </summary>
    private sealed class EdgeCaseProviderModule : IProviderModule
    {
        /// <summary>
        /// Gets the provider name for the edge case test module.
        /// </summary>
        public string ProviderName => "custom";

        /// <summary>
        /// Gets the template resource prefix for the edge case test module.
        /// </summary>
        public string TemplateResourcePrefix => string.Empty;

        /// <summary>
        /// Registers helper functions (none for this test module).
        /// </summary>
        /// <param name="scriptObject">The script object to register helpers with.</param>
        public void RegisterHelpers(ScriptObject scriptObject)
        {
        }

        /// <summary>
        /// Registers resource factories (none for this test module).
        /// </summary>
        /// <param name="registry">The factory registry.</param>
        public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
        {
        }

        /// <summary>
        /// Registers the edge case parent-child relationship.
        /// </summary>
        /// <param name="registry">The parent-child relationship registry.</param>
        public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
        {
            registry.Register(new ParentChildRelationship
            {
                ParentResourceType = ParentType,
                ChildResourceType = ChildType,
                InlineAttributeName = "members",
                ChildReferenceAttribute = "parent_id",
                ChildGroupLabel = "Members",
                TableColumns = [new ChildTableColumn("Member", "member")],
                RowExtractor = new EdgeCaseRowExtractor()
            });
        }
    }

    /// <summary>
    /// Row extractor used for edge case tests.
    /// </summary>
    private sealed class EdgeCaseRowExtractor : IChildRowExtractor
    {
        public IReadOnlyDictionary<string, string> ExtractDiffRow(object? beforeState, object? afterState, string providerName, ValueFormatterRegistry? valueFormatterRegistry, IconProviderRegistry? iconProviderRegistry, LargeValueFormat largeValueFormat) => ExtractRow(afterState, providerName, valueFormatterRegistry, iconProviderRegistry);

        /// <summary>
        /// Extracts a member value for edge case tests.
        /// </summary>
        /// <param name="childState">The child state to inspect.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="valueFormatterRegistry">The value formatter registry.</param>
        /// <param name="iconProviderRegistry">The icon provider registry.</param>
        /// <returns>The extracted row values.</returns>
        public IReadOnlyDictionary<string, string> ExtractRow(
            object? childState,
            string providerName,
            ValueFormatterRegistry? valueFormatterRegistry,
            IconProviderRegistry? iconProviderRegistry)
        {
            if (childState is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string> { ["member"] = string.Empty };
            }

            if (!element.TryGetProperty("member", out var member))
            {
                return new Dictionary<string, string> { ["member"] = string.Empty };
            }

            var value = member.ValueKind == JsonValueKind.String ? member.GetString() : member.ToString();
            return new Dictionary<string, string> { ["member"] = value ?? string.Empty };
        }
    }

    /// <summary>
    /// Provider module that uses a row extractor which always throws.
    /// </summary>
    private sealed class ThrowingExtractorProviderModule : IProviderModule
    {
        /// <summary>
        /// Gets the provider name for the throwing extractor module.
        /// </summary>
        public string ProviderName => "custom";

        /// <summary>
        /// Gets the template resource prefix for the throwing extractor module.
        /// </summary>
        public string TemplateResourcePrefix => string.Empty;

        /// <summary>
        /// Registers helper functions (none for this module).
        /// </summary>
        /// <param name="scriptObject">The script object to register helpers with.</param>
        public void RegisterHelpers(ScriptObject scriptObject)
        {
        }

        /// <summary>
        /// Registers resource factories (none for this module).
        /// </summary>
        /// <param name="registry">The factory registry.</param>
        public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
        {
        }

        /// <summary>
        /// Registers the edge case relationship with a throwing extractor.
        /// </summary>
        /// <param name="registry">The parent-child relationship registry.</param>
        public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
        {
            registry.Register(new ParentChildRelationship
            {
                ParentResourceType = ParentType,
                ChildResourceType = ChildType,
                InlineAttributeName = null,
                ChildReferenceAttribute = "parent_id",
                ChildGroupLabel = "Members",
                TableColumns = [new ChildTableColumn("Member", "member")],
                RowExtractor = new ThrowingRowExtractor()
            });
        }
    }

    /// <summary>
    /// Row extractor that always throws to simulate extractor failures.
    /// </summary>
    private sealed class ThrowingRowExtractor : IChildRowExtractor
    {
        public IReadOnlyDictionary<string, string> ExtractDiffRow(object? beforeState, object? afterState, string providerName, ValueFormatterRegistry? valueFormatterRegistry, IconProviderRegistry? iconProviderRegistry, LargeValueFormat largeValueFormat) => ExtractRow(afterState, providerName, valueFormatterRegistry, iconProviderRegistry);

        /// <summary>
        /// Throws an exception to simulate a failure.
        /// </summary>
        /// <param name="childState">The child state to inspect.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="valueFormatterRegistry">The value formatter registry.</param>
        /// <param name="iconProviderRegistry">The icon provider registry.</param>
        /// <returns>Never returns a value.</returns>
        public IReadOnlyDictionary<string, string> ExtractRow(
            object? childState,
            string providerName,
            ValueFormatterRegistry? valueFormatterRegistry,
            IconProviderRegistry? iconProviderRegistry)
        {
            throw new InvalidOperationException("Extractor failure");
        }
    }

    /// <summary>
    /// Provider module that simulates invalid JSON in child payloads.
    /// </summary>
    private sealed class InvalidJsonExtractorProviderModule : IProviderModule
    {
        /// <summary>
        /// Gets the provider name for the invalid JSON module.
        /// </summary>
        public string ProviderName => "custom";

        /// <summary>
        /// Gets the template resource prefix for the invalid JSON module.
        /// </summary>
        public string TemplateResourcePrefix => string.Empty;

        /// <summary>
        /// Registers helper functions (none for this module).
        /// </summary>
        /// <param name="scriptObject">The script object to register helpers with.</param>
        public void RegisterHelpers(ScriptObject scriptObject)
        {
        }

        /// <summary>
        /// Registers resource factories (none for this module).
        /// </summary>
        /// <param name="registry">The factory registry.</param>
        public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
        {
        }

        /// <summary>
        /// Registers the edge case relationship with an invalid JSON extractor.
        /// </summary>
        /// <param name="registry">The parent-child relationship registry.</param>
        public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
        {
            registry.Register(new ParentChildRelationship
            {
                ParentResourceType = ParentType,
                ChildResourceType = ChildType,
                InlineAttributeName = null,
                ChildReferenceAttribute = "parent_id",
                ChildGroupLabel = "Members",
                TableColumns = [new ChildTableColumn("Member", "member")],
                RowExtractor = new InvalidJsonRowExtractor()
            });
        }
    }

    /// <summary>
    /// Row extractor that attempts to parse invalid JSON payloads.
    /// </summary>
    private sealed class InvalidJsonRowExtractor : IChildRowExtractor
    {
        public IReadOnlyDictionary<string, string> ExtractDiffRow(object? beforeState, object? afterState, string providerName, ValueFormatterRegistry? valueFormatterRegistry, IconProviderRegistry? iconProviderRegistry, LargeValueFormat largeValueFormat) => ExtractRow(afterState, providerName, valueFormatterRegistry, iconProviderRegistry);

        /// <summary>
        /// Parses the payload property and throws if JSON is invalid.
        /// </summary>
        /// <param name="childState">The child state to inspect.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="valueFormatterRegistry">The value formatter registry.</param>
        /// <param name="iconProviderRegistry">The icon provider registry.</param>
        /// <returns>Never returns a value.</returns>
        public IReadOnlyDictionary<string, string> ExtractRow(
            object? childState,
            string providerName,
            ValueFormatterRegistry? valueFormatterRegistry,
            IconProviderRegistry? iconProviderRegistry)
        {
            if (childState is JsonElement element && element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty("payload", out var payload))
            {
                var raw = payload.GetString() ?? string.Empty;
                JsonDocument.Parse(raw);
            }

            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Ensures inline attribute as Object (not Array) does not crash.
    /// Related issue: 071-json-parsing-error-azurerm-resources
    /// </summary>
    [Test]
    public void Build_InlineAttributeAsObject_DoesNotCrash()
    {
        // Arrange - parent with inline attribute as Object instead of Array
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_parent.example",
                    null,
                    "managed",
                    ParentType,
                    "example",
                    "custom",
                    new Change(
                        ["create"],
                        null,
                        JsonDocument.Parse("""{"name":"parent","members":{"type":"object","value":"data"}}""").RootElement,
                        null,
                        null,
                        null))
            });

        // Act - should not throw
        var model = BuildModel(plan);

        // Assert - parent should exist without child group
        model.Changes.Should().ContainSingle();
        model.Changes[0].Type.Should().Be(ParentType);
    }

    /// <summary>
    /// Ensures inline attribute as null does not crash.
    /// Related issue: 071-json-parsing-error-azurerm-resources
    /// </summary>
    [Test]
    public void Build_InlineAttributeAsNull_DoesNotCrash()
    {
        // Arrange - parent with null inline attribute
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_parent.example",
                    null,
                    "managed",
                    ParentType,
                    "example",
                    "custom",
                    new Change(
                        ["create"],
                        null,
                        JsonDocument.Parse("""{"name":"parent","members":null}""").RootElement,
                        null,
                        null,
                        null))
            });

        // Act - should not throw
        var model = BuildModel(plan);

        // Assert - parent should exist without child group
        model.Changes.Should().ContainSingle();
        model.Changes[0].Type.Should().Be(ParentType);
    }

    /// <summary>
    /// Ensures inline attribute as string (primitive) does not crash.
    /// Related issue: 071-json-parsing-error-azurerm-resources
    /// </summary>
    [Test]
    public void Build_InlineAttributeAsString_DoesNotCrash()
    {
        // Arrange - parent with string inline attribute
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "edge_parent.example",
                    null,
                    "managed",
                    ParentType,
                    "example",
                    "custom",
                    new Change(
                        ["create"],
                        null,
                        JsonDocument.Parse("""{"name":"parent","members":"string_value"}""").RootElement,
                        null,
                        null,
                        null))
            });

        // Act - should not throw
        var model = BuildModel(plan);

        // Assert - parent should exist without child group
        model.Changes.Should().ContainSingle();
        model.Changes[0].Type.Should().Be(ParentType);
    }
}
