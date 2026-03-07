using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Providers.AzApi.Renderers;
using Oocx.TfPlan2Md.Providers.AzureAD.Renderers;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Renderers;
using Oocx.TfPlan2Md.Providers.AzureRM.Renderers;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Verifies provider-specific resource renderer type mappings and render-path safety.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// Related test plan: docs/features/107-remove-scriban/test-plan.md (TC-ARM, TC-API, TC-AD, TC-ADO).
/// </summary>
public class ProviderResourceRenderersTests
{
    /// <summary>
    /// Expected provider-specific renderer resource types.
    /// </summary>
    private static readonly string[] ExpectedResourceTypes =
    [
        "azurerm_role_assignment",
        "azurerm_network_security_group",
        "azurerm_firewall_network_rule_collection",
        "azurerm_firewall_application_rule_collection",
        "azapi_resource",
        "azapi_update_resource",
        "azapi_output_values",
        "azuread_user",
        "azuread_group",
        "azuread_group_without_members",
        "azuread_group_member",
        "azuread_service_principal",
        "azuread_invitation",
        "azuredevops_variable_group",
        "azuredevops_build_definition"
    ];

    /// <summary>
    /// Verifies all provider-specific renderer classes expose expected resource type identifiers.
    /// </summary>
    [Test]
    public void ProviderRenderers_ExposeExpectedResourceTypes()
    {
        var renderers = new IResourceRenderer[]
        {
            new RoleAssignmentRenderer(),
            new NsgRenderer(),
            new FirewallNetworkRuleRenderer(),
            new FirewallAppRuleRenderer(),
            new AzApiResourceRenderer(),
            new AzApiUpdateResourceRenderer(),
            new AzApiOutputValuesRenderer(),
            new UserRenderer(),
            new GroupRenderer(),
            new GroupWithoutMembersRenderer(),
            new GroupMemberRenderer(),
            new ServicePrincipalRenderer(),
            new InvitationRenderer(),
            new VariableGroupRenderer(LargeValueFormat.InlineDiff),
            new AzureDevOpsDelegatingRenderer("azuredevops_build_definition")
        };

        renderers.Select(renderer => renderer.ResourceType).Should().BeEquivalentTo(ExpectedResourceTypes);
    }

    /// <summary>
    /// Verifies provider-specific renderers can render via their default delegation path.
    /// </summary>
    [Test]
    public void ProviderRenderers_Render_DelegatesWithoutThrowing()
    {
        var change = new ResourceChangeModel
        {
            Address = "azurerm_role_assignment.test",
            Type = "azurerm_role_assignment",
            Name = "test",
            ProviderName = "registry.terraform.io/hashicorp/azurerm",
            Action = "create",
            ActionSymbol = "➕",
            AttributeChanges =
            [
                new AttributeChangeModel
                {
                    Name = "name",
                    After = "test"
                }
            ],
            SummaryHtml = "➕\u00A0azurerm_role_assignment `test`"
        };

        var context = new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: RenderTarget.AzureDevOps,
            detailsDisplayMode: DetailsDisplayMode.Auto);

        var writer = new MarkdownWriter();
        var renderer = new RoleAssignmentRenderer();

        var action = () => renderer.Render(writer, change, context);

        action.Should().NotThrow();
        writer.Build().Should().Contain("<details");
    }
}
