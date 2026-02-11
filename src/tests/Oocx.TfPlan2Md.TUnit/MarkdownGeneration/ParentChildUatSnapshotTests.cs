using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot tests for the parent-child UAT rendering examples.
/// </summary>
public class ParentChildUatSnapshotTests
{
    /// <summary>
    /// Ensures the UAT artifact rendering matches the baseline and key invariants.
    /// </summary>
    [Test]
    public void Snapshot_ParentChildUat_MatchesBaseline()
    {
        var markdown = RenderUatPlan();

        markdown.Should().Contain("| Member | Terraform Resource |");
        markdown.Should().Contain("| Change | Member | Terraform Resource |");
        markdown.Should().Contain("Security & Quality");
        markdown.Should().Contain("members attribute");
        markdown.Should().Contain("azuread_group_member.contractor_high_risk");

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "parent-child-resource-grouping-uat.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("parent-child-resource-grouping-uat.md", markdown);
    }

    /// <summary>
    /// Renders the UAT plan with code analysis results.
    /// </summary>
    /// <returns>The rendered markdown output.</returns>
    private static string RenderUatPlan()
    {
        var json = File.ReadAllText("TestData/parent-child-resource-grouping-uat-plan.json");
        var plan = new TerraformPlanParser().Parse(json);

        var sarifLoader = new CodeAnalysisLoader(new SarifParser());
        var sarifResult = sarifLoader.Load(["TestData/code-analysis/parent-child-resource-grouping-uat.sarif"]);
        var codeAnalysisInput = new CodeAnalysisInput
        {
            Model = sarifResult.Model,
            Warnings = sarifResult.Warnings,
            MinimumLevel = null,
            FailOnLevel = null
        };

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule());
        providerRegistry.RegisterProvider(new AzureDevOpsModule(LargeValueFormat.SimpleDiff));

        var valueFormatterRegistry = new ValueFormatterRegistry();
        providerRegistry.RegisterAllValueFormatters(valueFormatterRegistry);

        var iconProviderRegistry = new IconProviderRegistry();
        providerRegistry.RegisterAllIconProviders(iconProviderRegistry);

        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry,
            codeAnalysisInput: codeAnalysisInput,
            iconProviderRegistry: iconProviderRegistry).Build(plan);

        var renderer = new MarkdownRenderer(
            providerRegistry: providerRegistry,
            valueFormatterRegistry: valueFormatterRegistry,
            iconProviderRegistry: iconProviderRegistry);

        return renderer.Render(model);
    }
}
