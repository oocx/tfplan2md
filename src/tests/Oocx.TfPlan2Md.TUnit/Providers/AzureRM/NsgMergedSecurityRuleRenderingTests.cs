using System;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Regression coverage for merged NSG child rules rendered through the AzureRM specialized renderer.
/// Related issue: docs/issues/112-missing-nsg-rule-report/analysis.md.
/// </summary>
public class NsgMergedSecurityRuleRenderingTests
{
    /// <summary>
    /// Shared Terraform action name for create operations in the issue-scoped test plans.
    /// </summary>
    private const string CreateAction = "create";

    /// <summary>
    /// Shared Terraform action name for update operations in the issue-scoped test plans.
    /// </summary>
    private const string UpdateAction = "update";

    /// <summary>
    /// Shared Terraform action name for delete operations in the issue-scoped test plans.
    /// </summary>
    private const string DeleteAction = "delete";

    /// <summary>
    /// Shared Terraform action name for unchanged parent resources that remain visible due to merged children.
    /// </summary>
    private const string NoOpAction = "no-op";

    /// <summary>
    /// Resource type name for AzureRM network security groups used throughout this regression suite.
    /// </summary>
    private const string NetworkSecurityGroupType = "azurerm_network_security_group";

    /// <summary>
    /// Resource type name for AzureRM network security rules used throughout this regression suite.
    /// </summary>
    private const string NetworkSecurityRuleType = "azurerm_network_security_rule";

    /// <summary>
    /// Module path used by the focused NSG regression scenarios.
    /// </summary>
    private const string MonitoringModulePath = "module.monitoring_vnet[0]";

    /// <summary>
    /// Terraform mode used by the AzureRM managed resources in this regression suite.
    /// </summary>
    private const string ManagedMode = "managed";

    /// <summary>
    /// Provider identifier used by the AzureRM regression scenarios.
    /// </summary>
    private const string AzureRmProviderName = "registry.terraform.io/hashicorp/azurerm";

    /// <summary>
    /// Canonical parent NSG address for the issue-scoped test plans.
    /// </summary>
    private const string ParentNsgAddress = "module.monitoring_vnet[0].azurerm_network_security_group.this[\"pep\"]";

    /// <summary>
    /// Canonical parent NSG local name for the issue-scoped test plans.
    /// </summary>
    private const string ParentNsgName = "this[\"pep\"]";

    /// <summary>
    /// Verifies the report model uses a merged security-rules child group and removes separate child resources.
    /// </summary>
    [Test]
    public void Build_NoOpNsgParentWithMergedSecurityRules_PopulatesChildResourceGroupsAndSummary()
    {
        var model = BuildModel(BuildNoOpParentWithSeparateRulePlan(CreateAction));

        model.Changes.Should().ContainSingle(change => change.Type == NetworkSecurityGroupType);
        model.Changes.Should().NotContain(change => change.Type == NetworkSecurityRuleType);

        var parent = model.Changes.Single(change => change.Type == NetworkSecurityGroupType);
        var securityRules = parent.ChildResourceGroups.Should().ContainSingle(group => group.Label == "Security Rules").Subject;

        securityRules.Rows.Should().HaveCount(3, "the merged group should include unchanged inline rows and the separate child rule");
        securityRules.HasExternalResources.Should().BeTrue();
        parent.SummaryHtml.Should().Contain($"{ActionIcons.Add}\u00A01 security rules");
    }

    /// <summary>
    /// Verifies a separate created rule remains visible under a no-op NSG parent.
    /// </summary>
    [Test]
    public void Render_NoOpParentWithSeparateCreatedRule_ShowsCreatedRowInSecurityRulesTable()
    {
        var markdown = Render(BuildNoOpParentWithSeparateRulePlan(CreateAction));
        var normalized = Normalize(markdown);

        normalized.Should().Contain($"| {ActionIcons.Add} | 🆔 MyRuleName | 120 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | 🌐 127.1.0.0/22 | ✳️ | ✳️ | 🔌 5050-5051 | Allow PRTG HTTP push |");
        markdown.Should().NotContain("azurerm_network_security_rule <b><code>rule_create</code></b>");
    }

    /// <summary>
    /// Verifies a separate updated rule remains visible under a no-op NSG parent.
    /// </summary>
    [Test]
    public void Render_NoOpParentWithSeparateUpdatedRule_ShowsUpdatedRowInSecurityRulesTable()
    {
        var markdown = Render(BuildNoOpParentWithSeparateRulePlan(UpdateAction));
        var normalized = Normalize(markdown);

        normalized.Should().Contain($"| {ActionIcons.Update} | 🆔 MyRuleName | 120 | ⬇️ Inbound | ✅ Allow | 🔗 TCP |");
        normalized.Should().Contain("10.98.0.0/22");
        normalized.Should().Contain("5050-5051");
        markdown.Should().NotContain("azurerm_network_security_rule <b><code>rule_update</code></b>");
    }

    /// <summary>
    /// Verifies a separate deleted rule remains visible under a no-op NSG parent.
    /// </summary>
    [Test]
    public void Render_NoOpParentWithSeparateDeletedRule_ShowsDeletedRowInSecurityRulesTable()
    {
        var markdown = Render(BuildNoOpParentWithSeparateRulePlan(DeleteAction));
        var normalized = Normalize(markdown);

        normalized.Should().Contain($"| {ActionIcons.Delete} | 🆔 AllowLegacyPushInbound | 130 | ⬇️ Inbound | ✅ Allow | 🔗 TCP | 🌐 10.99.0.0/24 | ✳️ | ✳️ | 🔌 5050 | Legacy HTTP push |");
        markdown.Should().NotContain("azurerm_network_security_rule <b><code>rule_delete</code></b>");
    }

    /// <summary>
    /// Verifies mixed inline and separate NSG rules both remain visible in one table.
    /// </summary>
    [Test]
    public void Render_MixedInlineAndSeparateRules_ShowsAllRows()
    {
        var markdown = Render(BuildMixedInlineAndSeparateRulePlan());
        var normalized = Normalize(markdown);

        normalized.Should().Contain("AllowHttpsInbound");
        normalized.Should().Contain("AllowDnsOutbound");
        normalized.Should().Contain("MyRuleName");
        normalized.Should().Contain("This resource has children managed both inline and as separate resources");
        markdown.Should().NotContain("azurerm_network_security_rule <b><code>mixed_rule_create</code></b>");
    }

    /// <summary>
    /// Renders markdown for the provided Terraform plan.
    /// </summary>
    /// <param name="plan">The plan to render.</param>
    /// <returns>The rendered markdown output.</returns>
    private static string Render(TerraformPlan plan)
    {
        var renderer = new MarkdownRenderer(providerRegistry: CreateProviderRegistry());
        return renderer.Render(BuildModel(plan));
    }

    /// <summary>
    /// Builds the report model for the provided Terraform plan.
    /// </summary>
    /// <param name="plan">The plan to convert.</param>
    /// <returns>The built report model.</returns>
    private static ReportModel BuildModel(TerraformPlan plan)
    {
        var providerRegistry = CreateProviderRegistry();
        var builder = new ReportModelBuilder(
            services: new ReportModelBuilderServices(ProviderRegistry: providerRegistry));
        return builder.Build(plan);
    }

    /// <summary>
    /// Creates a provider registry with the AzureRM provider registered.
    /// </summary>
    /// <returns>The configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));
        return providerRegistry;
    }

    /// <summary>
    /// Normalizes markdown for stable assertions.
    /// </summary>
    /// <param name="markdown">The markdown to normalize.</param>
    /// <returns>The normalized markdown text.</returns>
    private static string Normalize(string markdown)
    {
        var decoded = WebUtility.HtmlDecode(markdown);
        var withoutTags = Regex.Replace(decoded, "<.*?>", string.Empty, RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        var withoutBackticks = withoutTags.Replace("`", string.Empty, StringComparison.Ordinal);
        return Regex.Replace(withoutBackticks, "\\s+", " ", RegexOptions.Singleline, TimeSpan.FromSeconds(2)).Trim();
    }

    /// <summary>
    /// Builds a plan with a no-op NSG parent and a separate child rule action.
    /// </summary>
    /// <param name="childAction">The child action to model: create, update, or delete.</param>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildNoOpParentWithSeparateRulePlan(string childAction)
    {
        var parentState = JsonDocument.Parse(
            """
            {
              "name": "my-nsg",
              "location": "westeurope",
              "resource_group_name": "rg-monitoring",
              "security_rule": [
                {
                  "name": "AllowHttpsInbound",
                  "priority": 100,
                  "direction": "Inbound",
                  "access": "Allow",
                  "protocol": "Tcp",
                  "source_address_prefix": "127.0.0.0/24",
                  "source_port_range": "*",
                  "destination_address_prefix": "*",
                  "destination_port_range": "443",
                  "description": "Existing HTTPS ingress"
                },
                {
                  "name": "AllowMonitoringInbound",
                  "priority": 110,
                  "direction": "Inbound",
                  "access": "Allow",
                  "protocol": "Tcp",
                  "source_address_prefix": "127.0.1.0/24",
                  "source_port_range": "*",
                  "destination_address_prefix": "*",
                  "destination_port_range": "8443",
                  "description": "Existing monitoring ingress"
                }
              ]
            }
            """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            [
                CreateResourceChange(
                    ParentNsgAddress,
                    NetworkSecurityGroupType,
                    ParentNsgName,
                    new Change([NoOpAction], parentState, parentState, null, null, null)),
                CreateSeparateRuleChange(childAction)
            ]);
    }

    /// <summary>
    /// Builds a plan with both inline and separate NSG rule changes.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildMixedInlineAndSeparateRulePlan()
    {
        var parentBefore = JsonDocument.Parse(
            """
            {
              "name": "my-nsg",
              "location": "westeurope",
              "resource_group_name": "rg-monitoring",
              "security_rule": [
                {
                  "name": "AllowHttpsInbound",
                  "priority": 100,
                  "direction": "Inbound",
                  "access": "Allow",
                  "protocol": "Tcp",
                  "source_address_prefix": "127.0.0.0/24",
                  "source_port_range": "*",
                  "destination_address_prefix": "*",
                  "destination_port_range": "443",
                  "description": "Existing HTTPS ingress"
                }
              ]
            }
            """).RootElement;

        var parentAfter = JsonDocument.Parse(
            """
            {
              "name": "my-nsg",
              "location": "westeurope",
              "resource_group_name": "rg-monitoring",
              "security_rule": [
                {
                  "name": "AllowHttpsInbound",
                  "priority": 100,
                  "direction": "Inbound",
                  "access": "Allow",
                  "protocol": "Tcp",
                  "source_address_prefix": "127.0.0.0/24",
                  "source_port_range": "*",
                  "destination_address_prefix": "*",
                  "destination_port_range": "443",
                  "description": "Existing HTTPS ingress"
                },
                {
                  "name": "AllowDnsOutbound",
                  "priority": 115,
                  "direction": "Outbound",
                  "access": "Allow",
                  "protocol": "Udp",
                  "source_address_prefix": "*",
                  "source_port_range": "*",
                  "destination_address_prefix": "168.63.129.16",
                  "destination_port_range": "53",
                  "description": "DNS resolution"
                }
              ]
            }
            """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            [
                CreateResourceChange(
                    ParentNsgAddress,
                    NetworkSecurityGroupType,
                    ParentNsgName,
                    new Change([UpdateAction], parentBefore, parentAfter, null, null, null)),
                CreateResourceChange(
                    $"{MonitoringModulePath}.{NetworkSecurityRuleType}.mixed_rule_create",
                    NetworkSecurityRuleType,
                    "mixed_rule_create",
                    new Change([CreateAction], null, JsonDocument.Parse(
                        """
                        {
                          "name": "MyRuleName",
                          "resource_group_name": "rg-monitoring",
                          "network_security_group_name": "my-nsg",
                          "priority": 120,
                          "direction": "Inbound",
                          "access": "Allow",
                          "protocol": "Tcp",
                          "source_address_prefix": "127.1.0.0/22",
                          "source_port_range": "*",
                          "destination_address_prefix": "*",
                          "destination_port_range": "5050-5051",
                          "description": "Allow PRTG HTTP push"
                        }
                        """).RootElement, null, null, null))
            ]);
    }

    /// <summary>
    /// Creates a managed AzureRM resource change using the shared module and provider metadata.
    /// </summary>
    /// <param name="address">Terraform address for the resource.</param>
    /// <param name="resourceType">Terraform resource type.</param>
    /// <param name="name">Terraform local resource name.</param>
    /// <param name="change">Planned change details.</param>
    /// <returns>The configured resource change.</returns>
    private static ResourceChange CreateResourceChange(string address, string resourceType, string name, Change change)
    {
        return new ResourceChange(
            address,
            MonitoringModulePath,
            ManagedMode,
            resourceType,
            name,
            AzureRmProviderName,
            change);
    }

    /// <summary>
    /// Creates the separate NSG rule resource change for the requested action.
    /// </summary>
    /// <param name="childAction">The action to model.</param>
    /// <returns>The resource change.</returns>
    private static ResourceChange CreateSeparateRuleChange(string childAction)
    {
        return childAction switch
        {
            CreateAction => CreateResourceChange(
                $"{MonitoringModulePath}.{NetworkSecurityRuleType}.rule_create",
                NetworkSecurityRuleType,
                "rule_create",
                new Change([CreateAction], null, JsonDocument.Parse(
                    """
                    {
                      "name": "MyRuleName",
                      "resource_group_name": "rg-monitoring",
                      "network_security_group_name": "my-nsg",
                      "priority": 120,
                      "direction": "Inbound",
                      "access": "Allow",
                      "protocol": "Tcp",
                      "source_address_prefix": "127.1.0.0/22",
                      "source_port_range": "*",
                      "destination_address_prefix": "*",
                      "destination_port_range": "5050-5051",
                      "description": "Allow PRTG HTTP push"
                    }
                    """).RootElement, null, null, null)),
            UpdateAction => CreateResourceChange(
                $"{MonitoringModulePath}.{NetworkSecurityRuleType}.rule_update",
                NetworkSecurityRuleType,
                "rule_update",
                new Change([UpdateAction], JsonDocument.Parse(
                    """
                    {
                      "name": "MyRuleName",
                      "resource_group_name": "rg-monitoring",
                      "network_security_group_name": "my-nsg",
                      "priority": 120,
                      "direction": "Inbound",
                      "access": "Allow",
                      "protocol": "Tcp",
                      "source_address_prefix": "127.1.0.0/22",
                      "source_port_range": "*",
                      "destination_address_prefix": "*",
                      "destination_port_range": "5050",
                      "description": "Legacy PRTG push"
                    }
                    """).RootElement, JsonDocument.Parse(
                    """
                    {
                      "name": "MyRuleName",
                      "resource_group_name": "rg-monitoring",
                      "network_security_group_name": "my-nsg",
                      "priority": 120,
                      "direction": "Inbound",
                      "access": "Allow",
                      "protocol": "Tcp",
                      "source_address_prefix": "10.98.0.0/22",
                      "source_port_range": "*",
                      "destination_address_prefix": "*",
                      "destination_port_range": "5050-5051",
                      "description": "Allow PRTG HTTP push"
                    }
                    """).RootElement, null, null, null)),
            DeleteAction => CreateResourceChange(
                $"{MonitoringModulePath}.{NetworkSecurityRuleType}.rule_delete",
                NetworkSecurityRuleType,
                "rule_delete",
                new Change([DeleteAction], JsonDocument.Parse(
                    """
                    {
                      "name": "AllowLegacyPushInbound",
                      "resource_group_name": "rg-monitoring",
                      "network_security_group_name": "my-nsg",
                      "priority": 130,
                      "direction": "Inbound",
                      "access": "Allow",
                      "protocol": "Tcp",
                      "source_address_prefix": "10.99.0.0/24",
                      "source_port_range": "*",
                      "destination_address_prefix": "*",
                      "destination_port_range": "5050",
                      "description": "Legacy HTTP push"
                    }
                    """).RootElement, null, null, null, null)),
            _ => throw new ArgumentOutOfRangeException(nameof(childAction), childAction, "Unsupported child action")
        };
    }
}
