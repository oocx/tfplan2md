using System.Net;
using System.Text.RegularExpressions;
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

public class MarkdownRendererNsgTemplateTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = CreateRenderer();

    private static MarkdownRenderer CreateRenderer()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));
        return new MarkdownRenderer(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);
    }

    private static ReportModelBuilder CreateBuilder()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));
        return new ReportModelBuilder(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);
    }

    private static string Normalize(string markdown)
    {
        var decoded = WebUtility.HtmlDecode(markdown);
        var withoutTags = Regex.Replace(decoded, "<.*?>", string.Empty, RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        var withoutBackticks = withoutTags.Replace("`", string.Empty, StringComparison.Ordinal);
        return Regex.Replace(withoutBackticks, "\\s+", " ", RegexOptions.Singleline, TimeSpan.FromSeconds(2)).Trim();
    }

    private string RenderNsgPlan()
    {
        var json = File.ReadAllText("TestData/nsg-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = CreateBuilder().Build(plan);

        return _renderer.Render(model);
    }

    [Test]
    public void Render_NsgCreate_ShowsRulesTable()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        normalized.Should().Contain("azurerm_network_security_group new");
        normalized.Should().Contain("| 🆔 allow-web-out | 200 | ⬆️ Outbound | ✅ Allow | 🔗 TCP | ✳️ | ✳️ | ✳️ | 🔌 443 | Allow outbound HTTPS |");
        normalized.Should().Contain("| 🆔 allow-health | 210 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | 🌐 10.0.20.0/24 | ✳️ | ✳️ | 🔌 15000 | Health probes |");
    }

    [Test]
    public void Render_NsgDelete_ShowsRulesBeingDeleted()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        normalized.Should().Contain("azurerm_network_security_group legacy");
        normalized.Should().Contain("Security Rules (being deleted)");
        normalized.Should().Contain("| 🆔 allow-ftp | 300 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | ✳️ | ✳️ | 🌐 10.10.5.0/24 | 🔌 21 | Deprecated FTP |");
    }

    [Test]
    public void Render_NsgUpdate_ShowsSemanticDiff()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        normalized.Should().Contain($"| {ActionIcons.Add} | 🆔 allow-https | 100 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | ✳️ | ✳️ | ✳️ | 🔌 443 | Allow HTTPS traffic |");
        normalized.Should().Contain("allow-http");
        normalized.Should().Contain("10.0.2.0/24");
        normalized.Should().Contain("alternate HTTP");
        normalized.Should().Contain($"| {ActionIcons.Delete} | 🆔 allow-ssh | 120");
        normalized.Should().Contain($"| {ActionIcons.Unchanged} | 🆔 allow-dns | 130 | ⬆️ Outbound | ✅ Allow | 📨 UDP | ✳️ | ✳️ | 🌐 168.63.129.16 | 🔌 53 | Azure DNS |");
    }

    [Test]
    public void Render_NsgUpdate_SortsRulesByPriority()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        // Parent-child framework format uses backticks around name values
        var addedIndex = normalized.IndexOf($"| {ActionIcons.Add} | `🆔 allow-https` | 100", StringComparison.Ordinal);
        var modifiedIndex = normalized.IndexOf($"| {ActionIcons.Add} | `🆔 allow-http` | 110", StringComparison.Ordinal); // Modified rule shows as ➕ in update
        var removedIndex = normalized.IndexOf($"| {ActionIcons.Delete} | `🆔 allow-ssh` | 120", StringComparison.Ordinal);
        var unchangedDnsIndex = normalized.IndexOf($"| {ActionIcons.Unchanged} | `🆔 allow-dns` | 130", StringComparison.Ordinal);
        var unchangedMonitoringIndex = normalized.IndexOf($"| {ActionIcons.Unchanged} | `🆔 allow-monitoring` | 140", StringComparison.Ordinal);

        addedIndex.Should().BeGreaterThanOrEqualTo(0);
        modifiedIndex.Should().BeGreaterThan(addedIndex);
        removedIndex.Should().BeGreaterThan(modifiedIndex);
        unchangedDnsIndex.Should().BeGreaterThan(removedIndex);
        unchangedMonitoringIndex.Should().BeGreaterThan(unchangedDnsIndex);
    }

    [Test]
    public void Render_NsgUpdate_HandlesSingularAndPluralFields()
    {
        var result = RenderNsgPlan();
        var normalized = Normalize(result);

        // Plural addresses take precedence when present (parent-child framework format with backticks)
        normalized.Should().Contain("allow-http").And.Contain("10.0.2.0/24");

        // Unchanged rule with plural addresses should render the joined list (condensed format)
        normalized.Should().Contain($"| {ActionIcons.Unchanged} | `🆔 allow-monitoring` | 140 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | `🌐 10.0.3.0/24`, `🌐 10.0.4.0/24` | `🌐 10.0.10.0/24` | 🔌 443 | security_rule attribute |");

        // Wildcards remain visible
        normalized.Should().Contain($"| {ActionIcons.Add} | `🆔 allow-https` | 100 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | ✳️ | ✳️ | 🔌 443 | security_rule attribute |");
    }
}
