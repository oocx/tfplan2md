using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererNsgTemplateTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    private string RenderNsgPlan()
    {
        var json = File.ReadAllText("TestData/nsg-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);

        return _renderer.Render(model);
    }

    [Fact]
    public void Render_NsgCreate_ShowsRulesTable()
    {
        var result = RenderNsgPlan();

        result.Should().Contain("azurerm_network_security_group.new");
        result.Should().Contain("| `allow-web-out` | `200` | `Outbound` | `Allow` | `Tcp` | `*` | `*` | `*` | `443` | `Allow outbound HTTPS` |");
        result.Should().Contain("| `allow-health` | `210` | `Inbound` | `Allow` | `Tcp` | `10.0.20.0/24` | `*` | `*` | `15000` | `Health probes` |");
    }

    [Fact]
    public void Render_NsgDelete_ShowsRulesBeingDeleted()
    {
        var result = RenderNsgPlan();

        result.Should().Contain("azurerm_network_security_group.legacy");
        result.Should().Contain("Security Rules (being deleted)");
        result.Should().Contain("| `allow-ftp` | `300` | `Inbound` | `Allow` | `Tcp` | `*` | `*` | `10.10.5.0/24` | `21` | `Deprecated FTP` |");
    }

    [Fact]
    public void Render_NsgUpdate_ShowsSemanticDiff()
    {
        var result = RenderNsgPlan();

        result.Should().Contain("| ➕ | `allow-https` | `100` | `Inbound` | `Allow` | `Tcp` | `*` | `*` | `*` | `443` | `Allow HTTPS traffic` |");
        result.Should().Contain("allow-http").And.Contain("background-color:");
        result.Should().Contain("10.0.2.0/24");
        result.Should().Contain("Allow <span").And.Contain("alternate </span>HTTP");
        result.Should().Contain("| ❌ | `allow-ssh` | `120`");
        result.Should().Contain("| ⏺️ | `allow-dns` | `130` | `Outbound` | `Allow` | `Udp` | `*` | `*` | `168.63.129.16` | `53` | `Azure DNS` |");
    }

    [Fact]
    public void Render_NsgUpdate_SortsRulesByPriority()
    {
        var result = RenderNsgPlan();

        var addedIndex = result.IndexOf("| ➕ | `allow-https` | `100`", StringComparison.Ordinal);
        var modifiedIndex = result.IndexOf("| 🔄 | `allow-http` | <code>110</code>", StringComparison.Ordinal);
        var removedIndex = result.IndexOf("| ❌ | `allow-ssh` | `120`", StringComparison.Ordinal);
        var unchangedDnsIndex = result.IndexOf("| ⏺️ | `allow-dns` | `130`", StringComparison.Ordinal);
        var unchangedMonitoringIndex = result.IndexOf("| ⏺️ | `allow-monitoring` | `140`", StringComparison.Ordinal);

        addedIndex.Should().BeGreaterThanOrEqualTo(0);
        modifiedIndex.Should().BeGreaterThan(addedIndex);
        removedIndex.Should().BeGreaterThan(modifiedIndex);
        unchangedDnsIndex.Should().BeGreaterThan(removedIndex);
        unchangedMonitoringIndex.Should().BeGreaterThan(unchangedDnsIndex);
    }

    [Fact]
    public void Render_NsgUpdate_HandlesSingularAndPluralFields()
    {
        var result = RenderNsgPlan();

        // Plural addresses take precedence when present
        result.Should().Contain("allow-http").And.Contain("10.0.2.0/24");

        // Unchanged rule with plural addresses should render the joined list
        result.Should().Contain("| ⏺️ | `allow-monitoring` | `140` | `Inbound` | `Allow` | `Tcp` | `10.0.3.0/24, 10.0.4.0/24` | `*` | `10.0.10.0/24` | `443` | `Monitoring agents` |");

        // Wildcards remain visible
        result.Should().Contain("| ➕ | `allow-https` | `100` | `Inbound` | `Allow` | `Tcp` | `*` | `*` | `*` | `443` | `Allow HTTPS traffic` |");
    }
}
