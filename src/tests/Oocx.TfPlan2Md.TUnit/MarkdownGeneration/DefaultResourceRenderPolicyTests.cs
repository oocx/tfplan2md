using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for extracted default-renderer policy decisions.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
[Category("Unit")]
public class DefaultResourceRenderPolicyTests
{
    [Test]
    public async Task ResolvePolicy_NoOpParentSecurityRuleScenario_SetsCompatibilityFlags()
    {
        var change = CreateChange(
            resourceType: "azurerm_network_security_group",
            moduleAddress: null,
            attributes: []);
        change.ChildResourceGroups =
        [
            new ChildResourceGroup
            {
                Label = "Security Rules",
                Columns = [new ChildTableColumn("Name", "name")],
                Rows =
                [
                    new ChildResourceRow
                    {
                        ChangeIndicator = "➕",
                        Values = new Dictionary<string, string> { ["name"] = "rule-a" },
                        TerraformResource = "azurerm_network_security_rule.rule_a"
                    },
                    new ChildResourceRow
                    {
                        ChangeIndicator = "➕",
                        Values = new Dictionary<string, string> { ["name"] = "rule-b" },
                        TerraformResource = "azurerm_network_security_rule.rule_b"
                    }
                ]
            }
        ];

        var policy = DefaultResourceRenderPolicy.Resolve(change, CreateContext());

        policy.IsNoOpParentWithChildren.Should().BeTrue();
        policy.UseMultilineDetailsSummary.Should().BeTrue();
        await Task.CompletedTask;
    }

    [Test]
    public async Task ResolvePolicy_AzureAdResource_UsesExtraBlankLineBeforeSummary()
    {
        var change = CreateChange(
            resourceType: "azuread_group",
            moduleAddress: null,
            attributes:
            [
                new AttributeChangeModel { Name = "display_name", Before = null, After = "team-a" }
            ]);

        var policy = DefaultResourceRenderPolicy.Resolve(change, CreateContext());

        policy.UseExtraBlankLineBeforeSummary.Should().BeTrue();
        await Task.CompletedTask;
    }

    private static RenderContext CreateContext()
    {
        return new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: RenderTarget.AzureDevOps,
            detailsDisplayMode: DetailsDisplayMode.Auto);
    }

    private static ResourceChangeModel CreateChange(
        string resourceType,
        string? moduleAddress,
        IReadOnlyList<AttributeChangeModel> attributes,
        string action = "create")
    {
        return new ResourceChangeModel
        {
            Address = $"{resourceType}.main",
            ModuleAddress = moduleAddress,
            Type = resourceType,
            Name = "main",
            ProviderName = "registry.terraform.io/hashicorp/azurerm",
            Action = action,
            ActionSymbol = "➕",
            AttributeChanges = attributes,
            ConfigurationReferences = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        };
    }
}
