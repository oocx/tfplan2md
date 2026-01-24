using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
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
        _principalMapper = new PrincipalMapper(DemoPaths.DemoPrincipalsPath);
        _renderer = new MarkdownRenderer(_principalMapper);
    }

    [Test]
    public void DefaultTemplate_RendersAllKeyFeatures()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder(principalMapper: _principalMapper).Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("Module: root")
            .And.Contain("Module: `module.network`")
            .And.Contain("Module: `module.security`")
            .And.Contain("Module: `module.network.module.monitoring`");

        markdown.Should().Contain("➕")
            .And.Contain("🔄")
            .And.Contain("♻️")
            .And.Contain("❌");

        // Verify firewall rules are rendered with semantic diffing
        markdown.Should().Contain("azurerm_firewall_network_rule_collection")
            .And.Contain("Rule Changes");

        // Verify role assignments show principal names from mapping
        markdown.Should().Contain("azurerm_role_assignment")
            .And.Contain("<b><code>rg_reader</code></b>")
            .And.Contain("Jane Doe (User)");
    }

    [Test]
    public void Render_WithShowSensitive_RevealsSecretValues()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var builder = new ReportModelBuilder(showSensitive: true, principalMapper: _principalMapper);
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("super-secret-value");
        markdown.Should().NotContain("(sensitive)");
    }

    [Test]
    public void SummaryTemplate_ShowsExpectedCounts()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder().Build(plan);

        var summary = _renderer.Render(model, "summary");

        summary.Should().Contain("Terraform Plan Summary")
            .And.Contain("➕\u00A0Add | 12")
            .And.Contain("🔄\u00A0Change | 6")
            .And.Contain("♻️\u00A0Replace | 2")
            .And.Contain("❌\u00A0Destroy | 3")
            .And.Contain("Total | 23");
    }

    [Test]
    public void DefaultTemplate_AddsBlankLineAfterDetailsSections()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder().Build(plan);

        var markdown = _renderer.Render(model);

        // Verify that resource sections are separated properly (</details> followed by whitespace and new <details> or <div>)
        // The pattern looks for </details> followed by whitespace and then another block start
        var pattern = @"</details>\s+(?:<details[^>]*>|<div[^>]*>)";

        markdown.Should().MatchRegex(pattern);
    }
}
