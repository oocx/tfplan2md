using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Verifies that markdown rendering respects the configured diff format for small values.
/// Related feature: docs/features/003-consistent-value-formatting/specification.md.
/// </summary>
public class MarkdownRendererFormatDiffConfigTests
{
    /// <summary>
    /// Parses fixture plans for rendering tests.
    /// Related feature: docs/features/003-consistent-value-formatting/specification.md.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    private static (ReportModelBuilder Builder, MarkdownRenderer Renderer) CreateTestInfrastructure(RenderTarget renderTarget)
    {
        var largeValueFormat = renderTarget == RenderTarget.GitHub
            ? LargeValueFormat.SimpleDiff
            : LargeValueFormat.InlineDiff;

        // Create a single shared ProviderRegistry with the correct format for this renderTarget
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: largeValueFormat,
            principalMapper: new NullPrincipalMapper()));

        var builder = new ReportModelBuilder(
            renderTarget: renderTarget,
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);

        var renderer = new MarkdownRenderer(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);

        return (builder, renderer);
    }

    /// <summary>
    /// Ensures standard diff formatting is used when the model requests standard diff (TC-07).
    /// Related feature: docs/features/003-consistent-value-formatting/specification.md.
    /// </summary>
    [Test]
    public void Render_UsesSimpleDiff_WhenModelConfigIsSimple()
    {
        // Arrange
        var plan = _parser.Parse(File.ReadAllText("TestData/firewall-rule-changes.json"));
        var (builder, renderer) = CreateTestInfrastructure(RenderTarget.GitHub);
        var model = builder.Build(plan);
        var change = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var markdown = renderer.RenderResourceChange(change, RenderTarget.GitHub)!;

        // Assert - simple diff uses -/+ prefix with newline separator WITHOUT backticks (backticks prevent proper markdown rendering)
        markdown.Should().Contain("- 🌐 10.0.1.0/24<br>+ 🌐 10.0.1.0/24, 🌐 10.0.3.0/24")
            .And.NotContain("background-color:");
    }

    /// <summary>
    /// Ensures inline diff formatting is used when the model requests inline diff (TC-07).
    /// Related feature: docs/features/003-consistent-value-formatting/specification.md.
    /// </summary>
    [Test]
    public void Render_UsesInlineDiff_WhenModelConfigIsInline()
    {
        // Arrange
        var plan = _parser.Parse(File.ReadAllText("TestData/firewall-rule-changes.json"));
        var (builder, renderer) = CreateTestInfrastructure(RenderTarget.AzureDevOps);
        var model = builder.Build(plan);
        var change = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var markdown = renderer.RenderResourceChange(change, RenderTarget.AzureDevOps)!;

        // Assert - inline diff uses HTML with character-level highlighting and background-color styling
        markdown.Should().Contain("10.0.1.0/24")
            .And.Contain("10.0.3.0/24")
            .And.Contain("background-color:")
            .And.NotContain("```diff");
    }
}
