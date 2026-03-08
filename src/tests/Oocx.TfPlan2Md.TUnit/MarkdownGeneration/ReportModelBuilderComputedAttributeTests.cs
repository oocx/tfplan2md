using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Integration tests for computed known-after-apply rendering behavior.
/// </summary>
/// <remarks>
/// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
/// Related test plan: docs/features/102-known-after-apply-rendering/test-plan.md (TC-17 to TC-26).
/// </remarks>
public class ReportModelBuilderComputedAttributeTests
{
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Scenario6a_GenericResourceWithComputedIdPresent_ShowsKnownAfterApply()
    {
        var model = BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azurerm_resource_group.demo",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azurerm_resource_group",
                  "name": "demo",
                  "provider_name": "registry.terraform.io/hashicorp/azurerm",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "id": null, "location": "eastus", "name": "rg-demo" },
                    "after_unknown": { "id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.AttributeChanges.Should().Contain(a => a.Name == "id" && a.After == "(known after apply)");
        change.AttributeChanges.Should().Contain(a => a.Name == "location" && a.After == "eastus");
        change.AttributeChanges.Should().Contain(a => a.Name == "name" && a.After == "rg-demo");
    }

    [Test]
    public void Scenario6b_AttributeAbsentFromAfter_NotAddedToTable()
    {
        var model = BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azurerm_resource_group.demo",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azurerm_resource_group",
                  "name": "demo",
                  "provider_name": "registry.terraform.io/hashicorp/azurerm",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "location": "eastus", "name": "rg-demo" },
                    "after_unknown": { "id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.AttributeChanges.Should().NotContain(a => a.Name == "id");
    }

    [Test]
    public void Scenario7a_SensitiveComputed_ShowsLockAndSensitiveMask()
    {
        var model = BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azurerm_storage_account.main",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azurerm_storage_account",
                  "name": "main",
                  "provider_name": "registry.terraform.io/hashicorp/azurerm",
                  "change": {
                    "actions": ["update"],
                    "before": { "primary_access_key": "abc123", "account_replication_type": "LRS" },
                    "after": { "primary_access_key": null, "account_replication_type": "GRS" },
                    "after_unknown": { "primary_access_key": true },
                    "before_sensitive": { "primary_access_key": true },
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """);

        var attr = model.Changes.Single().AttributeChanges.Single(a => a.Name == "primary_access_key");
        attr.Before.Should().Be("(sensitive)");
        attr.After.Should().Be("🔒(known after apply)");
    }

    [Test]
    public void Scenario7b_ShowSensitiveTrue_StillDoesNotExposeBeforeValue()
    {
        var model = BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azurerm_storage_account.main",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azurerm_storage_account",
                  "name": "main",
                  "provider_name": "registry.terraform.io/hashicorp/azurerm",
                  "change": {
                    "actions": ["update"],
                    "before": { "primary_access_key": "abc123" },
                    "after": { "primary_access_key": null },
                    "after_unknown": { "primary_access_key": true },
                    "before_sensitive": { "primary_access_key": true },
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """, showSensitive: true);

        var attr = model.Changes.Single().AttributeChanges.Single(a => a.Name == "primary_access_key");
        attr.Before.Should().Be("(sensitive)");
        attr.After.Should().Be("🔒(known after apply)");
    }

    [Test]
    public void Scenario7c_ComputedAttributeCountedInUpdateSummary()
    {
        var model = BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azurerm_storage_account.main",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azurerm_storage_account",
                  "name": "main",
                  "provider_name": "registry.terraform.io/hashicorp/azurerm",
                  "change": {
                    "actions": ["update"],
                    "before": { "primary_access_key": "abc123", "account_replication_type": "LRS" },
                    "after": { "primary_access_key": null, "account_replication_type": "GRS" },
                    "after_unknown": { "primary_access_key": true },
                    "before_sensitive": { "primary_access_key": true },
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """);

        var summary = model.Changes.Single().ChangedAttributesSummary;
        summary.Should().Contain("2");
        summary.Should().Contain("account_replication_type");
        summary.Should().Contain("primary_access_key");
    }

    [Test]
    public void Scenario8_WholeResourceUnknown_ShowsSpecificNote()
    {
        var markdown = RenderMarkdown(BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "null_resource.wait",
                  "module_address": null,
                  "mode": "managed",
                  "type": "null_resource",
                  "name": "wait",
                  "provider_name": "registry.terraform.io/hashicorp/null",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": null,
                    "after_unknown": true,
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """));

        markdown.Should().Contain("_(all values known after apply)_");
        markdown.Should().NotContain("_No attribute changes._");
    }

    [Test]
    public void Scenario9_ChildWithComputedReference_RendersStandalone()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
          largeValueFormat: LargeValueFormat.InlineDiff,
          principalMapper: new NullPrincipalMapper()));

        var model = BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azurerm_virtual_network.hub",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azurerm_virtual_network",
                  "name": "hub",
                  "provider_name": "registry.terraform.io/hashicorp/azurerm",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "name": "hub-vnet" },
                    "after_unknown": {},
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                },
                {
                  "address": "azurerm_subnet.app",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azurerm_subnet",
                  "name": "app",
                  "provider_name": "registry.terraform.io/hashicorp/azurerm",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "name": "app", "virtual_network_name": null },
                    "after_unknown": { "virtual_network_name": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """, providerRegistry: providerRegistry);

        model.Changes.Should().Contain(c => c.Address == "azurerm_subnet.app");
        model.Changes.Single(c => c.Address == "azurerm_subnet.app")
            .AttributeChanges.Should().Contain(a => a.Name == "virtual_network_name" && a.After == "(known after apply)");
    }

    [Test]
    public void ComputedOnCreate_DoesNotSetChangedAttributesSummary()
    {
        var model = BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azuread_group_member.platform_admin_member",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azuread_group_member",
                  "name": "platform_admin_member",
                  "provider_name": "registry.terraform.io/hashicorp/azuread",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "group_object_id": null, "member_object_id": null, "id": null },
                    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """);

        var change = model.Changes.Single();
        change.ChangedAttributesSummary.Should().BeNullOrEmpty();
    }

    [Test]
    public void Regression_KnownValuesOnly_NoKnownAfterApplyMarkers()
    {
        var markdown = RenderMarkdown(BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azurerm_resource_group.demo",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azurerm_resource_group",
                  "name": "demo",
                  "provider_name": "registry.terraform.io/hashicorp/azurerm",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "location": "eastus", "name": "rg-demo" },
                    "after_unknown": {},
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """));

        markdown.Should().NotContain("known after apply");
    }

    [Test]
    public void Invariant_ReferenceLabelsUseExpressionReferencesNotSensitiveValues()
    {
        var model = BuildModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azuread_group_member.member",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azuread_group_member",
                  "name": "member",
                  "provider_name": "registry.terraform.io/hashicorp/azuread",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "member_object_id": null },
                    "after_unknown": { "member_object_id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ],
              "configuration": {
                "root_module": {
                  "resources": [
                    {
                      "address": "azuread_group_member.member",
                      "mode": "managed",
                      "type": "azuread_group_member",
                      "name": "member",
                      "expressions": {
                        "member_object_id": {
                          "references": ["var.users"]
                        }
                      }
                    }
                  ]
                }
              }
            }
            """);

        var memberAttr = model.Changes.Single().AttributeChanges.Single(a => a.Name == "member_object_id");
        memberAttr.After.Should().Be("(known after apply: var.users)");
    }

    private ReportModel BuildModel(string json, bool showSensitive = false, ProviderRegistry? providerRegistry = null)
    {
        var plan = _parser.Parse(json);
        return new ReportModelBuilder(options: new ReportModelBuilderOptions(ShowSensitive: showSensitive), services: new ReportModelBuilderServices(ProviderRegistry: providerRegistry)).Build(plan);
    }

    private static string RenderMarkdown(ReportModel model)
    {
        var renderer = new MarkdownRenderer();
        return renderer.Render(model);
    }
}
