using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.TestData;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ComprehensiveDemoTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer;

    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    public ComprehensiveDemoTests()
    {
        _renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));
    }

    [Fact]
    public void DefaultTemplate_RendersAllKeyFeatures()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder().Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("Module: root")
            .And.Contain("Module: `module.network`")
            .And.Contain("Module: `module.security`")
            .And.Contain("Module: `module.network.module.monitoring`");

        markdown.Should().Contain("➕")
            .And.Contain("🔄")
            .And.Contain("♻️")
            .And.Contain("❌");

        markdown.Should().Contain(Escape("azurerm_firewall_network_rule_collection.network_rules"))
            .And.Contain("Rule Changes")
            .And.Contain(Escape("azurerm_role_assignment.rg_reader"))
            .And.Contain("Jane Doe (User)");
    }

    [Fact]
    public void Render_WithShowSensitive_RevealsSecretValues()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var builder = new ReportModelBuilder(showSensitive: true);
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("super-secret-value");
        markdown.Should().NotContain("(sensitive)");
    }

    [Fact]
    public void SummaryTemplate_ShowsExpectedCounts()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder().Build(plan);

        var summary = _renderer.Render(model, "summary");

        summary.Should().Contain("Terraform Plan Summary")
            .And.Contain("➕ Add | 12")
            .And.Contain("🔄 Change | 5")
            .And.Contain("♻️ Replace | 2")
            .And.Contain("❌ Destroy | 3")
            .And.Contain("Total | 42");
    }

    [Fact]
    public void DefaultTemplate_AddsBlankLineAfterDetailsSections()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder().Build(plan);

        var markdown = _renderer.Render(model);

        // Verify that headings following </details> have at least one blank line between them.
        // This ensures proper Markdown rendering (content not running into heading).
        // The regex checks for </details> followed by at least 2 newlines before a heading.
        var heading = $"### ➕ {Escape("module.network.azurerm_firewall_network_rule_collection.new_public")}";
        var pattern = $"</details>\\n\\n+{Regex.Escape(heading)}";

        markdown.Should().MatchRegex(pattern);
    }
}
