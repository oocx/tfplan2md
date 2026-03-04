using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.RenderTargets;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ComprehensiveDemoTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer;
    private readonly IPrincipalMapper _principalMapper;

    public ComprehensiveDemoTests()
    {
        _principalMapper = PrincipalMapperFactory.Create(DemoPaths.DemoPrincipalsPath);
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: _principalMapper));
        _renderer = new MarkdownRenderer(
            principalMapper: _principalMapper,
            providerRegistry: providerRegistry);
    }

    private ReportModelBuilder CreateBuilder(bool showSensitive = false)
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: _principalMapper));
        return new ReportModelBuilder(
            showSensitive: showSensitive,
            principalMapper: _principalMapper,
            providerRegistry: providerRegistry);
    }

    [Test]
    public void DefaultTemplate_RendersAllKeyFeatures()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = CreateBuilder().Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("Module: root")
            .And.Contain("Module: `module.network`")
            .And.Contain("Module: `module.security`")
            .And.Contain("Module: `module.network.module.monitoring`");

        markdown.Should().Contain(ActionIcons.Add)
            .And.Contain(ActionIcons.Update)
            .And.Contain(ActionIcons.Replace)
            .And.Contain(ActionIcons.Delete);

        markdown.Should().Contain("azurerm_firewall_network_rule_collection")
            .And.Contain("azurerm_role_assignment");
    }

    [Test]
    public void Render_WithShowSensitive_RevealsSecretValues()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var builder = CreateBuilder(showSensitive: true);
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("super-secret-value");
        markdown.Should().NotContain("(sensitive)");
    }

    [Test]
    public void SummaryTemplate_ShowsExpectedCounts()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = CreateBuilder().Build(plan);

        var summary = _renderer.Render(model, "summary");

        summary.Should().Contain("Terraform Plan Summary")
            .And.Contain($"{ActionIcons.Add}\u00A0Add")
            .And.Contain($"{ActionIcons.Update}\u00A0Change")
            .And.Contain($"{ActionIcons.Replace}\u00A0Replace")
            .And.Contain($"{ActionIcons.Delete}\u00A0Destroy")
            // The summary template uses boldTotal: false so Total row is not bold (matching baseline output).
            .And.Contain("| Total |");
    }

    [Test]
    public void DefaultTemplate_AddsBlankLineAfterDetailsSections()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = CreateBuilder().Build(plan);

        var markdown = _renderer.Render(model);

        // Verify that resource sections are separated properly (</details> followed by whitespace and new <details> or <div>)
        // The pattern looks for </details> followed by whitespace and then another block start
        var pattern = @"</details>\s+(?:<details[^>]*>|<div[^>]*>)";

        markdown.Should().MatchRegex(pattern);
    }

    /// <summary>
    /// TC-19: Comprehensive demo mapping file with azdo sections loads successfully.
    /// Related feature: docs/features/085-azdo-principal-mapping/test-cases.md.
    /// </summary>
    [Test]
    public void ComprehensiveDemoMappingFile_WithAzdoSections_LoadsSuccessfully()
    {
        // Arrange
        var diagnostics = new DiagnosticContext();

        // Act
        var result = AzureMappingFileLoader.Load(DemoPaths.DemoPrincipalsPath, diagnostics);

        // Assert - Should have Azure AD mappings
        result.Principals.Should().NotBeEmpty();

        // Assert - Should have azdo mappings
        result.AzdoUsers.Should().NotBeEmpty();
        result.AzdoGroups.Should().NotBeEmpty();
        result.AzdoProjects.Should().NotBeEmpty();

        diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
        diagnostics.AzdoUserCount.Should().BeGreaterThan(0);
        diagnostics.AzdoGroupCount.Should().BeGreaterThan(0);
        diagnostics.AzdoProjectCount.Should().BeGreaterThan(0);
    }
}
