using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Provider module for Azure Resource Manager (azurerm) resources.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </summary>
internal sealed class AzureRMModule : IProviderModule
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

    private readonly LargeValueFormat _largeValueFormat;
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureRMModule"/> class.
    /// </summary>
    /// <param name="largeValueFormat">Format for rendering large values (inline-diff or simple-diff).</param>
    /// <param name="principalMapper">Mapper for resolving principal names in role assignments.</param>
    public AzureRMModule(LargeValueFormat largeValueFormat, IPrincipalMapper principalMapper)
    {
        _largeValueFormat = largeValueFormat;
        _principalMapper = principalMapper;
    }

    /// <summary>
    /// Gets the unique name of this Terraform provider.
    /// </summary>
    public string ProviderName => "azurerm";

    /// <summary>
    /// Gets the embedded resource prefix for this provider's templates.
    /// </summary>
    public string TemplateResourcePrefix => "Oocx.TfPlan2Md.Providers.AzureRM.Templates.";

    /// <summary>
    /// Registers AzureRM-specific Scriban helper functions.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register helpers with.</param>
    public void RegisterHelpers(ScriptObject scriptObject)
    {
        // AzureRM provider currently uses only core helpers
        // No provider-specific Scriban helpers needed
    }

    /// <summary>
    /// Registers AzureRM-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        registry.RegisterFactory("azurerm_network_security_group", new NetworkSecurityGroupFactory(_largeValueFormat));
        registry.RegisterFactory("azurerm_firewall_network_rule_collection", new FirewallNetworkRuleCollectionFactory(_largeValueFormat));
        registry.RegisterFactory("azurerm_role_assignment", new RoleAssignmentFactory(_principalMapper));
        registry.RegisterFactory("azurerm_api_management_api_operation", new AzureRMApimApiOperationFactory());
        registry.RegisterFactory("azurerm_api_management_named_value", new AzureRMApimNamedValueFactory());

        var apimSubresourceFactory = new AzureRMApimSubresourceFactory(ApimSubresourceTypes);
        foreach (var resourceType in ApimSubresourceTypes)
        {
            registry.RegisterFactory(resourceType, apimSubresourceFactory);
        }
    }
}
