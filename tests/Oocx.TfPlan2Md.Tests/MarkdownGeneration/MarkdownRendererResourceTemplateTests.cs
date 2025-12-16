using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererResourceTemplateTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    [Fact]
    public void Render_FirewallRuleCollection_UsesResourceSpecificTemplate()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var result = _renderer.Render(model);

        // Assert - full render should use resource-specific template for the firewall collection
        result.Should().NotBeNull();
        result.Should().Contain("Rule Changes").And.Contain("allow-dns").And.Contain("allow-ssh-old");
    }
}
