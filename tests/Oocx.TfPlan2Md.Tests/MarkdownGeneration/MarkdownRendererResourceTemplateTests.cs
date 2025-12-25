using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererResourceTemplateTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    private string RenderFirewallPlan()
    {
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        return _renderer.Render(model);
    }

    [Fact]
    public void Render_FirewallRuleCollection_UsesResourceSpecificTemplate()
    {
        // Act
        var result = RenderFirewallPlan();

        // Assert - full render should use resource-specific template for the firewall collection
        result.Should().NotBeNull();
        result.Should().Contain("Rule Changes").And.Contain("allow-dns").And.Contain("allow-ssh-old");
    }

    [Fact]
    public void Render_FirewallModifiedRules_ShowsDiffForChangedAttributes()
    {
        // Act
        var result = RenderFirewallPlan();

        // Assert
        result.Should().Contain("allow-http");
        result.Should().Contain("background-color:");
        result.Should().Contain("10.0.3.0/24");
        result.Should().Contain("from web and API tiers");
    }

    [Fact]
    public void Render_FirewallModifiedRules_ShowsSingleValueForUnchangedAttributes()
    {
        // Act
        var result = RenderFirewallPlan();

        // Assert
        result.Should().Contain("| 🔄 | `allow-http` | <code>TCP</code> |");
        result.Should().NotContain("- TCP");
        result.Should().NotContain("+ TCP");
        result.Should().NotContain("- *<br>");
        result.Should().NotContain("+ *");
        result.Should().NotContain("- 80<br>");
        result.Should().NotContain("+ 80");
    }

    [Fact]
    public void Render_FirewallNonModifiedRules_DisplayAsExpected()
    {
        // Act
        var result = RenderFirewallPlan();

        // Assert
        result.Should().Contain("| ➕ | `allow-dns` | `UDP` | `10.0.1.0/24, 10.0.2.0/24` | `168.63.129.16` | `53` | `Allow DNS queries to Azure DNS` |");
        result.Should().Contain("| ❌ | `allow-ssh-old` | `TCP` | `10.0.0.0/8` | `10.0.2.0/24` | `22` | `Legacy SSH access - to be removed` |");
        result.Should().Contain("| ⏺️ | `allow-https` | `TCP` | `10.0.1.0/24` | `*` | `443` | `Allow HTTPS traffic to internet` |");
        result.Should().NotContain("- allow-dns");
        result.Should().NotContain("+ allow-dns");
    }
}
