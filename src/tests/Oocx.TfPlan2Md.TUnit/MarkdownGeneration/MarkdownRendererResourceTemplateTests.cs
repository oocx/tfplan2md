using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererResourceTemplateTests
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

    private string RenderFirewallPlan()
    {
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = CreateBuilder().Build(plan);

        return _renderer.Render(model);
    }

    [Test]
    public void Render_FirewallRuleCollection_UsesResourceSpecificTemplate()
    {
        // Act
        var result = RenderFirewallPlan();

        // Assert - full render should use resource-specific template for the firewall collection
        result.Should().NotBeNull();
        result.Should().Contain("Rule Changes").And.Contain("allow-dns").And.Contain("allow-ssh-old");
    }

    [Test]
    public void Render_FirewallModifiedRules_ShowsDiffForChangedAttributes()
    {
        // Act
        var result = RenderFirewallPlan();
        var normalized = Normalize(result);

        // Assert
        normalized.Should().Contain("allow-http");
        normalized.Should().Contain("10.0.3.0/24");
        normalized.Should().Contain("from web and API tiers");
    }

    [Test]
    public void Render_FirewallModifiedRules_ShowsSingleValueForUnchangedAttributes()
    {
        // Act
        var result = RenderFirewallPlan();
        var normalized = Normalize(result);

        // Assert - modified rule row should appear with 🔄
        normalized.Should().Contain("| 🔄 | 🆔 allow-http |");
        // After normalization, unchanged attributes like protocol, destination ports show icons
        normalized.Should().Contain("🔗 TCP");
        normalized.Should().Contain("🔌 80");
    }

    [Test]
    public void Render_FirewallNonModifiedRules_DisplayAsExpected()
    {
        // Act
        var result = RenderFirewallPlan();
        var normalized = Normalize(result);

        // Assert
        normalized.Should().Contain("| ➕ | 🆔 allow-dns | 📨 UDP | 🌐 10.0.1.0/24, 🌐 10.0.2.0/24 | 🌐 168.63.129.16 | 🔌 53 | Allow DNS queries to Azure DNS |");
        normalized.Should().Contain("| ❌ | 🆔 allow-ssh-old | 🔗 TCP | 🌐 10.0.0.0/8 | 🌐 10.0.2.0/24 | 🔌 22 | Legacy SSH access - to be removed |");
        normalized.Should().Contain("| ⏺️ | 🆔 allow-https | 🔗 TCP | 🌐 10.0.1.0/24 | ✳️ | 🔌 443 | Allow HTTPS traffic to internet |");
        normalized.Should().NotContain("- allow-dns");
        normalized.Should().NotContain("+ allow-dns");
    }
}
