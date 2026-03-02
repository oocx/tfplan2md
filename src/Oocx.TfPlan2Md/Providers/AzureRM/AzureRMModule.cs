using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using Oocx.TfPlan2Md.Providers.AzureRM.Renderers;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Provider module for Azure Resource Manager (azurerm) resources.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Provider registration module naturally references all provider-specific types (mappers, factories, formatters, icon providers). Coupling is marginally over threshold (22 vs 21) after mapper registry refactoring.")]
internal sealed class AzureRMModule : IProviderModule
{
    private readonly LargeValueFormat _largeValueFormat;
    private readonly IPrincipalMapper _principalMapper;
    private readonly EnrichedAzureScopeFormatter? _scopeFormatter;

    /// <summary>
    /// Optional mapper for tenant and management group display names.
    /// </summary>
    private readonly AzureEntityMapper? _entityMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureRMModule"/> class.
    /// </summary>
    /// <param name="largeValueFormat">Format for rendering large values (inline-diff or simple-diff).</param>
    /// <param name="principalMapper">Mapper for resolving principal names in role assignments.</param>
    /// <param name="scopeFormatter">Optional formatter for enriched Azure scope display.</param>
    /// <param name="entityMapper">Optional mapper for tenant and management group display names.</param>
    public AzureRMModule(
        LargeValueFormat largeValueFormat,
        IPrincipalMapper principalMapper,
        EnrichedAzureScopeFormatter? scopeFormatter = null,
        AzureEntityMapper? entityMapper = null)
    {
        _largeValueFormat = largeValueFormat;
        _principalMapper = principalMapper;
        _scopeFormatter = scopeFormatter;
        _entityMapper = entityMapper;
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
    /// Registers AzureRM-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        AzureRmFactoryRegistration.Register(registry, _largeValueFormat, _principalMapper, _scopeFormatter);
    }

    /// <summary>
    /// Registers AzureRM-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    public void RegisterValueFormatters(ValueFormatterRegistry registry)
    {
        AzureRmValueFormatterRegistration.Register(registry, _scopeFormatter, _principalMapper, _entityMapper);
    }

    /// <summary>
    /// Registers AzureRM-specific icon providers.
    /// </summary>
    /// <param name="registry">The icon provider registry to register with.</param>
    public void RegisterIconProviders(IconProviderRegistry registry)
    {
        AzureRmIconProviderRegistration.Register(registry);
    }

    /// <summary>
    /// Registers AzureRM-specific C# resource renderers.
    /// </summary>
    /// <param name="registry">The resource renderer registry to register with.</param>
    public void RegisterResourceRenderers(ResourceRendererRegistry registry)
    {
        registry.Register(new RoleAssignmentRenderer(_principalMapper, _scopeFormatter));
        registry.Register(new NsgRenderer());
        registry.Register(new FirewallNetworkRuleRenderer());
        registry.Register(new FirewallAppRuleRenderer());
    }

    /// <summary>
    /// Registers the AzureRM attribute change filter that suppresses Azure resource ID
    /// casing-only differences.
    /// </summary>
    /// <param name="registry">The attribute change filter registry to register with.</param>
    /// <remarks>
    /// The <see cref="AzureResourceIdCaseChangeFilter"/> is always registered here; the filter
    /// is only consulted by the core pipeline when <c>--ignore-case-changes</c> is active.
    /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
    /// </remarks>
    public void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry)
    {
        registry.Register(new AzureResourceIdCaseChangeFilter());
    }

    /// <summary>
    /// Registers AzureRM parent-child relationships for inline rendering.
    /// </summary>
    /// <param name="registry">The parent-child relationship registry to register with.</param>
    public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
    {
        // Virtual Network → Subnets
        registry.Register(new ParentChildRelationship
        {
            ParentResourceType = "azurerm_virtual_network",
            ChildResourceType = "azurerm_subnet",
            InlineAttributeName = "subnet",
            ChildReferenceAttribute = "virtual_network_name",
            ParentIdAttribute = "name",
            ChildGroupLabel = "Subnets",
            TableColumns =
            [
                new ChildTableColumn("Name", "name"),
                new ChildTableColumn("Address Prefixes", "address_prefixes"),
                new ChildTableColumn("NSG", "nsg"),
                new ChildTableColumn("Delegation", "delegation")
            ],
            RowExtractor = new AzureRmSubnetRowExtractor()
        });

        // Route Table → Routes
        registry.Register(new ParentChildRelationship
        {
            ParentResourceType = "azurerm_route_table",
            ChildResourceType = "azurerm_route",
            InlineAttributeName = "route",
            ChildReferenceAttribute = "route_table_name",
            ParentIdAttribute = "name",
            ChildGroupLabel = "Routes",
            TableColumns =
            [
                new ChildTableColumn("Name", "name"),
                new ChildTableColumn("Address Prefix", "address_prefix"),
                new ChildTableColumn("Next Hop Type", "next_hop_type"),
                new ChildTableColumn("Next Hop Address", "next_hop_in_ip_address")
            ],
            RowExtractor = new AzureRmRouteRowExtractor()
        });

        // Network Security Group → Security Rules
        registry.Register(new ParentChildRelationship
        {
            ParentResourceType = "azurerm_network_security_group",
            ChildResourceType = "azurerm_network_security_rule",
            InlineAttributeName = "security_rule",
            ChildReferenceAttribute = "network_security_group_name",
            ParentIdAttribute = "name",
            ChildGroupLabel = "Security Rules",
            TableColumns =
            [
                new ChildTableColumn("Name", "name"),
                new ChildTableColumn("Priority", "priority"),
                new ChildTableColumn("Direction", "direction"),
                new ChildTableColumn("Access", "access"),
                new ChildTableColumn("Protocol", "protocol"),
                new ChildTableColumn("Source Addresses", "source_addresses"),
                new ChildTableColumn("Source Ports", "source_ports"),
                new ChildTableColumn("Destination Addresses", "destination_addresses"),
                new ChildTableColumn("Destination Ports", "destination_ports"),
                new ChildTableColumn("Description", "description")
            ],
            RowExtractor = new AzureRmNetworkSecurityRuleRowExtractor()
        });

        // DNS Zone → DNS Records (public zone types)
        var dnsRecordTypes = new[]
        {
            "azurerm_dns_a_record",
            "azurerm_dns_aaaa_record",
            "azurerm_dns_cname_record",
            "azurerm_dns_mx_record",
            "azurerm_dns_ns_record",
            "azurerm_dns_ptr_record",
            "azurerm_dns_srv_record",
            "azurerm_dns_txt_record",
            "azurerm_dns_caa_record"
        };

        var dnsRecordExtractor = new AzureRmDnsRecordRowExtractor();
        var dnsColumns = new[]
        {
            new ChildTableColumn("Name", "name"),
            new ChildTableColumn("Type", "type"),
            new ChildTableColumn("TTL", "ttl"),
            new ChildTableColumn("Value", "value")
        };

        foreach (var recordType in dnsRecordTypes)
        {
            registry.Register(new ParentChildRelationship
            {
                ParentResourceType = "azurerm_dns_zone",
                ChildResourceType = recordType,
                InlineAttributeName = null, // DNS records are always separate resources
                ChildReferenceAttribute = "zone_name",
                ParentIdAttribute = "name",
                ChildGroupLabel = "DNS Records",
                TableColumns = dnsColumns,
                RowExtractor = dnsRecordExtractor
            });
        }

        // Private DNS Zone → Private DNS Records
        var privateDnsRecordTypes = new[]
        {
            "azurerm_private_dns_a_record",
            "azurerm_private_dns_aaaa_record",
            "azurerm_private_dns_cname_record",
            "azurerm_private_dns_mx_record",
            "azurerm_private_dns_ptr_record",
            "azurerm_private_dns_srv_record",
            "azurerm_private_dns_txt_record"
        };

        foreach (var recordType in privateDnsRecordTypes)
        {
            registry.Register(new ParentChildRelationship
            {
                ParentResourceType = "azurerm_private_dns_zone",
                ChildResourceType = recordType,
                InlineAttributeName = null, // DNS records are always separate resources
                ChildReferenceAttribute = "zone_name",
                ParentIdAttribute = "name",
                ChildGroupLabel = "DNS Records",
                TableColumns = dnsColumns,
                RowExtractor = dnsRecordExtractor
            });
        }
    }
}
