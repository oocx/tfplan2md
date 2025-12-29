using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererAnchorTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    [Fact]
    public void Render_DefaultTemplate_EmitsResourceAnchors()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/azurerm-azuredevops-plan.json"));
        var model = new ReportModelBuilder().Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("<!-- tfplan2md:resource-start address=azurerm_resource_group.main -->")
            .And.Contain("<!-- tfplan2md:resource-end address=azurerm_resource_group.main -->");
    }

    [Fact]
    public void Render_WithResourceSpecificTemplate_UsesAnchorsForReplacement()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/firewall-rule-changes.json"));
        var model = new ReportModelBuilder().Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("<!-- tfplan2md:resource-start address=azurerm_firewall_network_rule_collection.web_tier -->")
            .And.Contain("<!-- tfplan2md:resource-end address=azurerm_firewall_network_rule_collection.web_tier -->")
            .And.Contain("| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |");
    }
}
