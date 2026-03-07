using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Renderers;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Provider module for Azure DevOps (azuredevops) resources.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </summary>
#pragma warning disable CA1506 // Suppress class coupling - module integrates multiple mapper types
internal sealed class AzureDevOpsModule : IProvider, IValueFormatterProvider, IIconRegistrationProvider, IParentChildRelationshipProvider, IResourceRendererProvider
{
    /// <summary>
    /// Optional mapper for tenant display name resolution.
    /// </summary>
    private readonly AzureEntityMapper? _entityMapper;

    /// <summary>
    /// Optional mapper for Azure DevOps user resolution.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </summary>
    private readonly AzdoUserMapper? _azdoUserMapper;

    /// <summary>
    /// Optional mapper for Azure DevOps group resolution.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </summary>
    private readonly AzdoGroupMapper? _azdoGroupMapper;

    /// <summary>
    /// Optional mapper for Azure DevOps project resolution.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </summary>
    private readonly AzdoProjectMapper? _azdoProjectMapper;

    /// <summary>
    /// Optional mapper for Azure DevOps repository resolution.
    /// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
    /// </summary>
    private readonly AzdoRepositoryMapper? _azdoRepositoryMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureDevOpsModule"/> class.
    /// </summary>
    /// <param name="entityMapper">Optional mapper for Azure entity display names.</param>
    /// <param name="azdoUserMapper">Optional mapper for Azure DevOps user display names.</param>
    /// <param name="azdoGroupMapper">Optional mapper for Azure DevOps group display names.</param>
    /// <param name="azdoProjectMapper">Optional mapper for Azure DevOps project display names.</param>
    /// <param name="azdoRepositoryMapper">Optional mapper for Azure DevOps repository display names.</param>
    public AzureDevOpsModule(
        AzureEntityMapper? entityMapper = null,
        AzdoUserMapper? azdoUserMapper = null,
        AzdoGroupMapper? azdoGroupMapper = null,
        AzdoProjectMapper? azdoProjectMapper = null,
        AzdoRepositoryMapper? azdoRepositoryMapper = null)
    {
        _entityMapper = entityMapper;
        _azdoUserMapper = azdoUserMapper;
        _azdoGroupMapper = azdoGroupMapper;
        _azdoProjectMapper = azdoProjectMapper;
        _azdoRepositoryMapper = azdoRepositoryMapper;
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
    /// Registers AzureDevOps-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        // No AzureDevOps factories are needed — rendering is handled by dedicated renderer classes.
    }

    /// <summary>
    /// Registers Azure DevOps-specific value formatters.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    public void RegisterValueFormatters(ValueFormatterRegistry registry)
    {
        // Register Azure entity formatters (tenant, management group)
        if (_entityMapper is not null)
        {
            AzureValueFormatterRegistration.RegisterTenantAndManagementGroup(
                registry,
                "(^azuredevops$|.*/azuredevops$)",
                _entityMapper);
        }

        // Register Azure DevOps user formatter
        // Note: Users can appear as either GUIDs or descriptors in member/administrator fields
        if (_azdoUserMapper is not null)
        {
            var userFormatter = new AzdoUserIdFormatter(_azdoUserMapper);
            // Match common user attribute names (both singular and plural forms)
            registry.Register(
                new MatchPattern(
                    "(^azuredevops$|.*/azuredevops$)",
                    null,
                    "^members?$|^administrators?$|^user$",
                    null),
                userFormatter);
        }

        // Register Azure DevOps group formatter
        if (_azdoGroupMapper is not null)
        {
            var groupFormatter = new AzdoGroupDescriptorFormatter(_azdoGroupMapper);
            // Match group/descriptor attributes
            registry.Register(
                new MatchPattern(
                    "(^azuredevops$|.*/azuredevops$)",
                    null,
                    "^group$|^descriptor$",
                    null),
                groupFormatter);
        }

        // Register Azure DevOps project formatter
        if (_azdoProjectMapper is not null)
        {
            var projectFormatter = new AzdoProjectIdFormatter(_azdoProjectMapper);
            // Match project attribute names with GUID pattern
            registry.Register(
                new MatchPattern(
                    "(^azuredevops$|.*/azuredevops$)",
                    null,
                    "^project_id$|^project$",
                    AzureValueFormatterRegistration.GuidPattern),
                projectFormatter);
        }

        // Register Azure DevOps repository formatter
        // Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md
        if (_azdoRepositoryMapper is not null)
        {
            var repositoryFormatter = new AzdoRepositoryIdFormatter(_azdoRepositoryMapper);
            // Match repository attribute names with GUID pattern
            registry.Register(
                new MatchPattern(
                    "(^azuredevops$|.*/azuredevops$)",
                    null,
                    "^repo_id$|^repository_id$|^source_repo_id$|^target_repo_id$",
                    AzureValueFormatterRegistration.GuidPattern),
                repositoryFormatter);
        }
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
            TableColumns = [new ChildTableColumn("Member", "member")],
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
            TableColumns = [new ChildTableColumn("Member", "member")],
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
            TableColumns = [new ChildTableColumn("Administrator", "administrator")],
            RowExtractor = new AzureDevOpsDescriptorRowExtractor(
                columnKey: "administrator",
                attributeName: "administrator",
                propertyNames: ["administrators", "administrator", "descriptor"])
        });
    }

    /// <summary>
    /// Registers Azure DevOps-specific C# resource renderers.
    /// </summary>
    /// <param name="registry">The resource renderer registry to register with.</param>
    public void RegisterResourceRenderers(ResourceRendererRegistry registry)
    {
        registry.Register(new VariableGroupRenderer());
        registry.Register(new AzureDevOpsDelegatingRenderer("azuredevops_build_definition"));
    }
}
#pragma warning restore CA1506
