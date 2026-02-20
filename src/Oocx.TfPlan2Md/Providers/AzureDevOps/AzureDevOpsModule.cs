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
#pragma warning disable CA1506 // Suppress class coupling - module integrates multiple mapper types
internal sealed class AzureDevOpsModule : IProviderModule
{
    private readonly LargeValueFormat _largeValueFormat;

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
    /// <param name="largeValueFormat">Format for rendering large values (inline-diff or simple-diff).</param>
    /// <param name="entityMapper">Optional mapper for tenant display names.</param>
    /// <param name="azdoUserMapper">Optional mapper for Azure DevOps user display names.</param>
    /// <param name="azdoGroupMapper">Optional mapper for Azure DevOps group display names.</param>
    /// <param name="azdoProjectMapper">Optional mapper for Azure DevOps project display names.</param>
    /// <param name="azdoRepositoryMapper">Optional mapper for Azure DevOps repository display names.</param>
    public AzureDevOpsModule(
        LargeValueFormat largeValueFormat,
        AzureEntityMapper? entityMapper = null,
        AzdoUserMapper? azdoUserMapper = null,
        AzdoGroupMapper? azdoGroupMapper = null,
        AzdoProjectMapper? azdoProjectMapper = null,
        AzdoRepositoryMapper? azdoRepositoryMapper = null)
    {
        _largeValueFormat = largeValueFormat;
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
    /// Registers AzureDevOps-specific Scriban helper functions.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register helpers with.</param>
    public void RegisterHelpers(ScriptObject scriptObject)
    {
        // Register Azure DevOps entity name helpers if mappers are available
        if (_azdoUserMapper is not null)
        {
            scriptObject.Import("azdo_user_name", new Func<string, string>(userId => _azdoUserMapper.GetEntityName(userId)));
        }

        if (_azdoGroupMapper is not null)
        {
            scriptObject.Import("azdo_group_name", new Func<string, string>(groupDescriptor => _azdoGroupMapper.GetEntityName(groupDescriptor)));
        }

        if (_azdoProjectMapper is not null)
        {
            scriptObject.Import("azdo_project_name", new Func<string, string>(projectId => _azdoProjectMapper.GetEntityName(projectId)));
        }

        if (_azdoRepositoryMapper is not null)
        {
            scriptObject.Import("azdo_repository_name", new Func<string, string>(repoId => _azdoRepositoryMapper.GetEntityName(repoId)));
        }
    }

    /// <summary>
    /// Registers AzureDevOps-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        registry.RegisterFactory("azuredevops_variable_group", new VariableGroupFactory(_largeValueFormat));
        registry.RegisterFactory("azuredevops_build_definition", new BuildDefinitionFactory(_largeValueFormat));
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
    /// Registers Azure DevOps-specific resource model mappers for ScriptObject enrichment.
    /// </summary>
    /// <param name="registry">The resource model mapper registry to register with.</param>
    public void RegisterResourceModelMappers(ResourceModelMapperRegistry registry)
    {
        var variableGroupFactory = new VariableGroupFactory(_largeValueFormat);
        registry.Register(new Mappers.VariableGroupMapper(variableGroupFactory));

        var buildDefinitionFactory = new BuildDefinitionFactory(_largeValueFormat);
        registry.Register(new Mappers.BuildDefinitionMapper(buildDefinitionFactory));
    }
}
#pragma warning restore CA1506
