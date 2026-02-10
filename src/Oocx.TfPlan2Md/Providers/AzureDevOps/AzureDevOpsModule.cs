using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Provider module for Azure DevOps (azuredevops) resources.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </summary>
internal sealed class AzureDevOpsModule : IProviderModule
{
    private readonly LargeValueFormat _largeValueFormat;

    /// <summary>
    /// Optional mapper for tenant display name resolution.
    /// </summary>
    private readonly AzureEntityMapper? _entityMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureDevOpsModule"/> class.
    /// </summary>
    /// <param name="largeValueFormat">Format for rendering large values (inline-diff or simple-diff).</param>
    /// <param name="entityMapper">Optional mapper for tenant display names.</param>
    public AzureDevOpsModule(LargeValueFormat largeValueFormat, AzureEntityMapper? entityMapper = null)
    {
        _largeValueFormat = largeValueFormat;
        _entityMapper = entityMapper;
    }

    /// <summary>
    /// Gets the unique name of this Terraform provider.
    /// </summary>
    public string ProviderName => "azuredevops";

    /// <summary>
    /// Gets the embedded resource prefix for this provider's templates.
    /// </summary>
    public string TemplateResourcePrefix => "Oocx.TfPlan2Md.Providers.AzureDevOps.Templates.";

    /// <summary>
    /// Registers AzureDevOps-specific Scriban helper functions.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register helpers with.</param>
    public void RegisterHelpers(ScriptObject scriptObject)
    {
        // AzureDevOps provider currently uses only core helpers
        // No provider-specific Scriban helpers needed
    }

    /// <summary>
    /// Registers AzureDevOps-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        registry.RegisterFactory("azuredevops_variable_group", new VariableGroupFactory(_largeValueFormat));
    }

    /// <summary>
    /// Registers Azure DevOps-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    public void RegisterValueFormatters(ValueFormatterRegistry registry)
    {
        if (_entityMapper is null)
        {
            return;
        }

        AzureValueFormatterRegistration.RegisterTenantAndManagementGroup(
            registry,
            "(^azuredevops$|.*/azuredevops$)",
            _entityMapper);
    }

    /// <summary>
    /// Registers Azure DevOps-specific icon providers.
    /// </summary>
    /// <param name="registry">The icon provider registry to register with.</param>
    public void RegisterIconProviders(IconProviderRegistry registry)
    {
        AzureDevOpsIconProviderRegistration.Register(registry);
    }

    /// <summary>
    /// Registers Azure DevOps parent-child relationships for inline rendering.
    /// </summary>
    /// <param name="registry">The parent-child relationship registry to register with.</param>
    public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
    {
        registry.Register(new ParentChildRelationship
        {
            ParentResourceType = "azuredevops_group",
            ChildResourceType = "azuredevops_group_membership",
            InlineAttributeName = "members",
            ChildReferenceAttribute = "group",
            ParentIdAttribute = "descriptor",
            ChildGroupLabel = "Members",
            TableColumns = [new ChildTableColumn { Header = "Member", PropertyName = "member" }],
            RowExtractor = new AzureDevOpsDescriptorRowExtractor(
                columnKey: "member",
                attributeName: "member",
                propertyNames: ["member", "descriptor"])
        });

        registry.Register(new ParentChildRelationship
        {
            ParentResourceType = "azuredevops_team",
            ChildResourceType = "azuredevops_team_members",
            InlineAttributeName = "members",
            ChildReferenceAttribute = "team_id",
            ChildGroupLabel = "Members",
            TableColumns = [new ChildTableColumn { Header = "Member", PropertyName = "member" }],
            RowExtractor = new AzureDevOpsDescriptorRowExtractor(
                columnKey: "member",
                attributeName: "member",
                propertyNames: ["members", "member", "descriptor"])
        });

        registry.Register(new ParentChildRelationship
        {
            ParentResourceType = "azuredevops_team",
            ChildResourceType = "azuredevops_team_administrators",
            InlineAttributeName = "administrators",
            ChildReferenceAttribute = "team_id",
            ChildGroupLabel = "Administrators",
            TableColumns = [new ChildTableColumn { Header = "Administrator", PropertyName = "administrator" }],
            RowExtractor = new AzureDevOpsDescriptorRowExtractor(
                columnKey: "administrator",
                attributeName: "administrator",
                propertyNames: ["administrators", "administrator", "descriptor"])
        });
    }
}
