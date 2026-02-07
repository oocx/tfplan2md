using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Registers AzureRM resource view model factories.
/// </summary>
/// <remarks>
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
internal static class AzureRmFactoryRegistration
{
    /// <summary>
    /// API Management subresource types that include api_management_name in their summaries.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    private static readonly string[] ApimSubresourceTypes =
    [
        "azurerm_api_management_api",
        "azurerm_api_management_api_policy",
        "azurerm_api_management_api_operation_policy",
        "azurerm_api_management_api_schema",
        "azurerm_api_management_api_version_set",
        "azurerm_api_management_api_release",
        "azurerm_api_management_api_diagnostic",
        "azurerm_api_management_backend",
        "azurerm_api_management_product",
        "azurerm_api_management_product_api",
        "azurerm_api_management_product_group",
        "azurerm_api_management_subscription",
        "azurerm_api_management_user",
        "azurerm_api_management_group",
        "azurerm_api_management_tag",
        "azurerm_api_management_tag_api",
        "azurerm_api_management_tag_operation",
        "azurerm_api_management_tag_product",
        "azurerm_api_management_gateway",
        "azurerm_api_management_gateway_api",
        "azurerm_api_management_gateway_host",
        "azurerm_api_management_logger",
        "azurerm_api_management_identity_provider",
        "azurerm_api_management_openid_connect_provider",
        "azurerm_api_management_policy"
    ];

    /// <summary>
    /// Registers AzureRM resource view model factories in the provided registry.
    /// </summary>
    /// <param name="registry">The registry to populate.</param>
    /// <param name="largeValueFormat">The preferred large value format.</param>
    /// <param name="principalMapper">The mapper used for resolving principal names.</param>
    /// <param name="scopeFormatter">Optional formatter for enriched scope display.</param>
    public static void Register(
        IResourceViewModelFactoryRegistry registry,
        LargeValueFormat largeValueFormat,
        IPrincipalMapper principalMapper,
        EnrichedAzureScopeFormatter? scopeFormatter)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(principalMapper);

        registry.RegisterFactory("azurerm_network_security_group", new NetworkSecurityGroupFactory(largeValueFormat));
        registry.RegisterFactory("azurerm_firewall_network_rule_collection", new FirewallNetworkRuleCollectionFactory(largeValueFormat));
        registry.RegisterFactory("azurerm_firewall_application_rule_collection", new FirewallApplicationRuleCollectionFactory(largeValueFormat));
        registry.RegisterFactory("azurerm_role_assignment", new RoleAssignmentFactory(principalMapper));
        registry.RegisterFactory("azurerm_private_dns_a_record", new AzureRMPrivateDnsARecordFactory());
        registry.RegisterFactory("azurerm_pim_eligible_role_assignment", new PimEligibleRoleAssignmentFactory(principalMapper));
        registry.RegisterFactory("azurerm_role_management_policy", new RoleManagementPolicyFactory(scopeFormatter));
        registry.RegisterFactory("azurerm_api_management_api_operation", new AzureRMApimApiOperationFactory());
        registry.RegisterFactory("azurerm_api_management_named_value", new AzureRMApimNamedValueFactory());

        var apimSubresourceFactory = new AzureRMApimSubresourceFactory(ApimSubresourceTypes);
        foreach (var resourceType in ApimSubresourceTypes)
        {
            registry.RegisterFactory(resourceType, apimSubresourceFactory);
        }
    }
}
