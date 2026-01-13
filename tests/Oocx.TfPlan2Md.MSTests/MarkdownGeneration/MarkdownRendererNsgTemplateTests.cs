using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

[TestClass]
public class MarkdownRendererNsgTemplateTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    private static string Normalize(string markdown)
    {
        var decoded = WebUtility.HtmlDecode(markdown);
        var withoutTags = Regex.Replace(decoded, "<.*?>", string.Empty, RegexOptions.Singleline);
        var withoutBackticks = withoutTags.Replace("`", string.Empty, StringComparison.Ordinal);
        return Regex.Replace(withoutBackticks, "\\s+", " ", RegexOptions.Singleline).Trim();
    }

    private string RenderNsgPlan()
    {
        var json = File.ReadAllText("TestData/nsg-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);

        return _renderer.Render(model);
    }

    [TestMethod]
    public void Render_NsgCreate_ShowsRulesTable()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        normalized.Should().Contain("azurerm_network_security_group.new");
        normalized.Should().Contain("| 🆔 allow-web-out | 200 | ⬆️ Outbound | ✅ Allow | 🔗 TCP | ✳️ | ✳️ | ✳️ | 🔌 443 | Allow outbound HTTPS |");
        normalized.Should().Contain("| 🆔 allow-health | 210 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | 🌐 10.0.20.0/24 | ✳️ | ✳️ | 🔌 15000 | Health probes |");
    }

    [TestMethod]
    public void Render_NsgDelete_ShowsRulesBeingDeleted()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        normalized.Should().Contain("azurerm_network_security_group.legacy");
        normalized.Should().Contain("Security Rules (being deleted)");
        normalized.Should().Contain("| 🆔 allow-ftp | 300 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | ✳️ | ✳️ | 🌐 10.10.5.0/24 | 🔌 21 | Deprecated FTP |");
    }

    [TestMethod]
    public void Render_NsgUpdate_ShowsSemanticDiff()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        normalized.Should().Contain("| ➕ | 🆔 allow-https | 100 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | ✳️ | ✳️ | ✳️ | 🔌 443 | Allow HTTPS traffic |");
        normalized.Should().Contain("allow-http");
        normalized.Should().Contain("10.0.2.0/24");
        normalized.Should().Contain("alternate HTTP");
        normalized.Should().Contain("| ❌ | 🆔 allow-ssh | 120");
        normalized.Should().Contain("| ⏺️ | 🆔 allow-dns | 130 | ⬆️ Outbound | ✅ Allow | 📨 UDP | ✳️ | ✳️ | 🌐 168.63.129.16 | 🔌 53 | Azure DNS |");
    }

    [TestMethod]
    public void Render_NsgUpdate_SortsRulesByPriority()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        var addedIndex = normalized.IndexOf("| ➕ | 🆔 allow-https | 100", StringComparison.Ordinal);
        var modifiedIndex = normalized.IndexOf("| 🔄 | 🆔 allow-http | 110", StringComparison.Ordinal);
        var removedIndex = normalized.IndexOf("| ❌ | 🆔 allow-ssh | 120", StringComparison.Ordinal);
        var unchangedDnsIndex = normalized.IndexOf("| ⏺️ | 🆔 allow-dns | 130", StringComparison.Ordinal);
        var unchangedMonitoringIndex = normalized.IndexOf("| ⏺️ | 🆔 allow-monitoring | 140", StringComparison.Ordinal);

        addedIndex.Should().BeGreaterThanOrEqualTo(0);
        modifiedIndex.Should().BeGreaterThan(addedIndex);
        removedIndex.Should().BeGreaterThan(modifiedIndex);
        unchangedDnsIndex.Should().BeGreaterThan(removedIndex);
        unchangedMonitoringIndex.Should().BeGreaterThan(unchangedDnsIndex);
    }

    [TestMethod]
    public void Render_NsgUpdate_HandlesSingularAndPluralFields()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        // Plural addresses take precedence when present
        normalized.Should().Contain("allow-http").And.Contain("10.0.2.0/24");

        // Unchanged rule with plural addresses should render the joined list
        normalized.Should().Contain("| ⏺️ | 🆔 allow-monitoring | 140 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | 10.0.3.0/24, 10.0.4.0/24 | ✳️ | 🌐 10.0.10.0/24 | 🔌 443 | Monitoring agents |");

        // Wildcards remain visible
        normalized.Should().Contain("| ➕ | 🆔 allow-https | 100 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | ✳️ | ✳️ | ✳️ | 🔌 443 | Allow HTTPS traffic |");
    }
}
