using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Regression tests for AzureRM resource-specific templates.
/// Related issue: docs/issues/058-nsg-rendering-issues/analysis.md.
/// </summary>
public class MarkdownRendererAzureRmTemplateRegressionTests
{
    /// <summary>
    /// Parses Terraform plan JSON for regression scenarios.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Renders markdown using AzureRM templates.
    /// </summary>
    private readonly MarkdownRenderer _renderer = CreateRenderer();

    /// <summary>
    /// Ensures NSG summaries do not repeat the name inside the details body.
    /// </summary>
    [Test]
    public void Render_NsgTemplate_DoesNotRepeatHeaderLine()
    {
        var result = RenderNsgPlan();

        result.Should().NotContain("Network Security Group:");
    }

    /// <summary>
    /// Ensures create-only NSG fallbacks render a single value column.
    /// </summary>
    [Test]
    public void Render_NsgCreateFallback_UsesSingleValueColumn()
    {
        var resourceChange = new ResourceChange(
            Address: "azurerm_network_security_group.empty",
            ModuleAddress: null,
            Mode: "managed",
            Type: "azurerm_network_security_group",
            Name: "empty",
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            Change: new Change(
                actions: ["create"],
                before: null,
                after: new System.Collections.Generic.Dictionary<string, object?>
                {
                    ["name"] = "nsg-empty",
                    ["security_rule"] = System.Array.Empty<object>()
                }));

        var change = new ResourceChangeModel
        {
            Address = "azurerm_network_security_group.empty",
            Name = "empty",
            Type = "azurerm_network_security_group",
            ProviderName = "registry.terraform.io/hashicorp/azurerm",
            Action = "create",
            ActionSymbol = ActionIcons.Add,
            SummaryHtml = $"{ActionIcons.Add} azurerm_network_security_group <b><code>empty</code></b>",
            AttributeChanges =
            [
                new AttributeChangeModel
                {
                    Name = "name",
                    After = "nsg-empty"
                }
            ],
            ResourceChange = resourceChange
        };

        var result = _renderer.RenderResourceChange(change);

        result.Should().Contain("| Attribute | Value |").And.NotContain("| Attribute | Before | After |");
    }

    /// <summary>
    /// Ensures create-only firewall collection fallbacks render a single value column.
    /// </summary>
    [Test]
    public void Render_FirewallCreateFallback_UsesSingleValueColumn()
    {
        var resourceChange = new ResourceChange(
            Address: "azurerm_firewall_network_rule_collection.empty",
            ModuleAddress: null,
            Mode: "managed",
            Type: "azurerm_firewall_network_rule_collection",
            Name: "empty",
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            Change: new Change(
                actions: ["create"],
                before: null,
                after: new System.Collections.Generic.Dictionary<string, object?>
                {
                    ["name"] = "collection-empty",
                    ["priority"] = 100,
                    ["action"] = "Allow",
                    ["rule"] = System.Array.Empty<object>()
                }));

        var change = new ResourceChangeModel
        {
            Address = "azurerm_firewall_network_rule_collection.empty",
            Name = "empty",
            Type = "azurerm_firewall_network_rule_collection",
            ProviderName = "registry.terraform.io/hashicorp/azurerm",
            Action = "create",
            ActionSymbol = ActionIcons.Add,
            SummaryHtml = $"{ActionIcons.Add} azurerm_firewall_network_rule_collection <b><code>empty</code></b>",
            AttributeChanges =
            [
                new AttributeChangeModel
                {
                    Name = "name",
                    After = "collection-empty"
                }
            ],
            ResourceChange = resourceChange
        };

        var result = _renderer.RenderResourceChange(change);

        result.Should().Contain("| Attribute | Value |").And.NotContain("| Attribute | Before | After |");
    }

    /// <summary>
    /// Creates a markdown renderer configured for AzureRM provider templates.
    /// </summary>
    /// <returns>Configured renderer instance.</returns>
    private static MarkdownRenderer CreateRenderer()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));
        return new MarkdownRenderer(
            providerRegistry: providerRegistry);
    }

    /// <summary>
    /// Creates a report model builder with AzureRM provider registration.
    /// </summary>
    /// <returns>Configured report model builder.</returns>
    private static ReportModelBuilder CreateBuilder()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));
        return new ReportModelBuilder(
            services: new ReportModelBuilderServices(ProviderRegistry: providerRegistry));
    }

    /// <summary>
    /// Renders the existing NSG rule changes plan used for regression checks.
    /// </summary>
    /// <returns>Markdown output for the NSG plan.</returns>
    private string RenderNsgPlan()
    {
        var json = File.ReadAllText("TestData/nsg-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = CreateBuilder().Build(plan);

        return _renderer.Render(model);
    }
}
