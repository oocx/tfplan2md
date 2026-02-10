using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests the parent-child merging logic in the report model builder.
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </summary>
public class ReportModelBuilderParentChildTests
{
    /// <summary>
    /// Non-breaking space used in summary assertions.
    /// </summary>
    private const string Nbsp = "\u00A0";
    /// <summary>
    /// Sample parent resource type for test data.
    /// </summary>
    private const string ParentType = "custom_parent";
    /// <summary>
    /// Sample child resource type for test data.
    /// </summary>
    private const string ChildType = "custom_child";

    /// <summary>
    /// Ensures separate child resources are merged into the parent group and removed from the list.
    /// </summary>
    [Test]
    public void Build_MergesSeparateChildren_IntoParentGroup()
    {
        var plan = BuildPlanWithSeparateChildren();
        var model = BuildModel(plan);

        model.Changes.Should().ContainSingle(change => change.Type == ParentType);
        model.Changes.Should().NotContain(change => change.Type == ChildType);

        var parent = model.Changes.Single();
        parent.ChildResourceGroups.Should().ContainSingle();
        parent.ChildResourceGroups[0].Rows.Should().HaveCount(2);
        parent.ChildResourceGroups[0].Rows.Should().OnlyContain(row => row.TerraformResource.StartsWith("custom_child"));
    }

    /// <summary>
    /// Ensures inline rows use the inline attribute name for the Terraform Resource column.
    /// </summary>
    [Test]
    public void Build_InlineChildren_LabelUsesAttributeName()
    {
        var plan = BuildPlanWithInlineMembers();
        var model = BuildModel(plan);

        var parent = model.Changes.Single(change => change.Type == ParentType);
        parent.ChildResourceGroups.Should().ContainSingle();

        parent.ChildResourceGroups[0].Rows
            .Should()
            .OnlyContain(row => row.TerraformResource == "members attribute");
    }

    /// <summary>
    /// Ensures mixed inline and separate children set the mixed sources flag.
    /// </summary>
    [Test]
    public void Build_MixedInlineAndSeparateChildren_SetsMixedSourceFlag()
    {
        var plan = BuildPlanWithMixedChildren();
        var model = BuildModel(plan);

        var parent = model.Changes.Single(change => change.Type == ParentType);
        parent.ChildResourceGroups.Should().ContainSingle();
        parent.ChildResourceGroups[0].HasMixedSources.Should().BeTrue();
    }

    /// <summary>
    /// Ensures code analysis findings on child resources are reattributed to the parent.
    /// </summary>
    [Test]
    public void Build_ReattributesChildFindings_ToParent()
    {
        var plan = BuildPlanWithSeparateChildren();
        var finding = CreateFinding("custom_child.member1");
        var codeAnalysisInput = new CodeAnalysisInput
        {
            Model = new CodeAnalysisModel
            {
                Tools = [],
                Findings = [finding]
            },
            Warnings = [],
            MinimumLevel = null,
            FailOnLevel = null
        };

        var model = BuildModel(plan, codeAnalysisInput);

        model.Changes.Should().ContainSingle(change => change.Type == ParentType);
        var parent = model.Changes.Single();
        parent.CodeAnalysisFindings.Should().ContainSingle();
        parent.CodeAnalysisFindings[0].ResourceAddress.Should().Be("custom_child.member1");
    }

    /// <summary>
    /// Ensures parent summaries include aggregated child change counts.
    /// </summary>
    [Test]
    public void Build_ParentSummary_IncludesChildCounts()
    {
        var plan = BuildPlanWithInlineMembers();
        var model = BuildModel(plan);

        var parent = model.Changes.Single(change => change.Type == ParentType);
        parent.SummaryHtml.Should().Contain($"{ActionIcons.Add}{Nbsp}1 members");
    }

    /// <summary>
    /// Builds a plan with a parent and two separate child resources.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithSeparateChildren()
    {
        var parentAfter = JsonDocument.Parse("{\"id\":\"parent-1\"}").RootElement;
        var childAfterOne = JsonDocument.Parse("{\"parent_id\":\"parent-1\",\"member\":\"alice\"}").RootElement;
        var childAfterTwo = JsonDocument.Parse("{\"parent_id\":\"parent-1\",\"member\":\"bob\"}").RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "custom_parent.team",
                    null,
                    "managed",
                    ParentType,
                    "team",
                    "custom",
                    new Change(["create"], null, parentAfter, null, null, null)),
                new ResourceChange(
                    "custom_child.member1",
                    null,
                    "managed",
                    ChildType,
                    "member1",
                    "custom",
                    new Change(["create"], null, childAfterOne, null, null, null)),
                new ResourceChange(
                    "custom_child.member2",
                    null,
                    "managed",
                    ChildType,
                    "member2",
                    "custom",
                    new Change(["create"], null, childAfterTwo, null, null, null))
            });
    }

    /// <summary>
    /// Builds a plan with inline members on the parent resource.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithInlineMembers()
    {
        var before = JsonDocument.Parse("{\"id\":\"parent-1\",\"members\":[\"alice\"]}").RootElement;
        var after = JsonDocument.Parse("{\"id\":\"parent-1\",\"members\":[\"alice\",\"bob\"]}").RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "custom_parent.team",
                    null,
                    "managed",
                    ParentType,
                    "team",
                    "custom",
                    new Change(["update"], before, after, null, null, null))
            });
    }

    /// <summary>
    /// Builds a plan with both inline and separate children for the same parent.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithMixedChildren()
    {
        var parentBefore = JsonDocument.Parse("{\"id\":\"parent-1\",\"members\":[\"alice\"]}").RootElement;
        var parentAfter = JsonDocument.Parse("{\"id\":\"parent-1\",\"members\":[\"alice\"]}").RootElement;
        var childAfter = JsonDocument.Parse("{\"parent_id\":\"parent-1\",\"member\":\"bob\"}").RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "custom_parent.team",
                    null,
                    "managed",
                    ParentType,
                    "team",
                    "custom",
                    new Change(["update"], parentBefore, parentAfter, null, null, null)),
                new ResourceChange(
                    "custom_child.member2",
                    null,
                    "managed",
                    ChildType,
                    "member2",
                    "custom",
                    new Change(["create"], null, childAfter, null, null, null))
            });
    }

    /// <summary>
    /// Builds a report model using the test provider registry.
    /// </summary>
    /// <param name="plan">The Terraform plan to render.</param>
    /// <param name="codeAnalysisInput">Optional code analysis input.</param>
    /// <returns>The built report model.</returns>
    private static ReportModel BuildModel(TerraformPlan plan, CodeAnalysisInput? codeAnalysisInput = null)
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new ParentChildTestModule());

        var builder = new ReportModelBuilder(providerRegistry: providerRegistry, codeAnalysisInput: codeAnalysisInput);
        return builder.Build(plan);
    }

    /// <summary>
    /// Creates a code analysis finding mapped to a resource address.
    /// </summary>
    /// <param name="address">The resource address to map.</param>
    /// <returns>The constructed code analysis finding.</returns>
    private static CodeAnalysisFinding CreateFinding(string address)
    {
        return new CodeAnalysisFinding
        {
            Message = "Finding message",
            SecuritySeverity = 9.1,
            Locations =
            [
                new CodeAnalysisLocation { FullyQualifiedName = address }
            ]
        };
    }

    /// <summary>
    /// Test provider module that registers a parent-child relationship.
    /// </summary>
    private sealed class ParentChildTestModule : IProviderModule
    {
        /// <summary>
        /// Gets the unique provider name for the test module.
        /// </summary>
        public string ProviderName => "custom";

        /// <summary>
        /// Gets the template resource prefix for the test module.
        /// </summary>
        public string TemplateResourcePrefix => string.Empty;

        /// <summary>
        /// Registers provider-specific helpers (none for the test module).
        /// </summary>
        /// <param name="scriptObject">The script object to register helpers with.</param>
        public void RegisterHelpers(ScriptObject scriptObject)
        {
        }

        /// <summary>
        /// Registers view model factories (none for the test module).
        /// </summary>
        /// <param name="registry">The factory registry to register with.</param>
        public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
        {
        }

        /// <summary>
        /// Registers the parent-child relationship for test resources.
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
                TableColumns = [new ChildTableColumn { Header = "Member", PropertyName = "member" }],
                RowExtractor = new MemberRowExtractor()
            });
        }
    }

    /// <summary>
    /// Extracts member values for child rows in tests.
    /// </summary>
    private sealed class MemberRowExtractor : IChildRowExtractor
    {
        /// <summary>
        /// Extracts a row with a single member value.
        /// </summary>
        /// <param name="childState">The child state to inspect.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="valueFormatterRegistry">The value formatter registry.</param>
        /// <param name="iconProviderRegistry">The icon provider registry.</param>
        /// <returns>A dictionary with the member value.</returns>
        public IReadOnlyDictionary<string, string> ExtractRow(
            object? childState,
            string providerName,
            ValueFormatterRegistry? valueFormatterRegistry,
            IconProviderRegistry? iconProviderRegistry)
        {
            var value = ResolveValue(childState, "member") ?? ResolveInlineValue(childState);
            return new Dictionary<string, string> { ["member"] = value ?? string.Empty };
        }

        /// <summary>
        /// Resolves a named property from a JSON state object.
        /// </summary>
        /// <param name="state">The JSON state object.</param>
        /// <param name="propertyName">The property to resolve.</param>
        /// <returns>The resolved value or null.</returns>
        private static string? ResolveValue(object? state, string propertyName)
        {
            if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
        }

        /// <summary>
        /// Resolves an inline primitive value from the child state.
        /// </summary>
        /// <param name="state">The inline child state.</param>
        /// <returns>The resolved value or null.</returns>
        private static string? ResolveInlineValue(object? state)
        {
            if (state is not JsonElement element)
            {
                return null;
            }

            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
        }
    }
}
