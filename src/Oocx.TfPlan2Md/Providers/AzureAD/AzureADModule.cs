using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;
using Oocx.TfPlan2Md.Providers.AzureAD.Renderers;

namespace Oocx.TfPlan2Md.Providers.AzureAD;

/// <summary>
/// Provider module for Azure AD (Entra) resources.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Provider registration module aggregates provider-specific factories, formatters, and renderers.")]
internal sealed class AzureADModule : IProvider, IValueFormatterProvider, IIconRegistrationProvider, IParentChildRelationshipProvider, IPostMergeCallbackProvider, IResourceRendererProvider
{
    /// <summary>
    /// Optional mapper for tenant display name resolution.
    /// </summary>
    private readonly AzureEntityMapper? _entityMapper;

    /// <summary>
    /// Optional mapper for resolving principal GUIDs to display names.
    /// </summary>
    private readonly IPrincipalMapper? _principalMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureADModule"/> class.
    /// </summary>
    /// <param name="entityMapper">Optional mapper for tenant display names.</param>
    /// <param name="principalMapper">Optional mapper for principal display names.</param>
    public AzureADModule(AzureEntityMapper? entityMapper = null, IPrincipalMapper? principalMapper = null)
    {
        _entityMapper = entityMapper;
        _principalMapper = principalMapper;
    }

    /// <summary>
    /// Gets the unique name of this Terraform provider.
    /// </summary>
    public string ProviderName => "azuread";

    /// <summary>
    /// Gets the embedded resource prefix for this provider's templates.
    /// </summary>
    public string TemplateResourcePrefix => "Oocx.TfPlan2Md.Providers.AzureAD.Templates.";

    /// <summary>
    /// Registers Azure AD-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        var summaryFactory = new AzureAdSummaryFactory();

        registry.RegisterFactory("azuread_user", summaryFactory);
        registry.RegisterFactory("azuread_group", summaryFactory);
        registry.RegisterFactory("azuread_group_without_members", summaryFactory);
        registry.RegisterFactory("azuread_group_member", summaryFactory);
        registry.RegisterFactory("azuread_service_principal", summaryFactory);
        registry.RegisterFactory("azuread_invitation", summaryFactory);
        registry.RegisterFactory("azuread_app_role_assignment", summaryFactory);
        registry.RegisterFactory("azuread_directory_role_assignment", summaryFactory);
        registry.RegisterFactory("azuread_service_principal_delegated_permission_grant", summaryFactory);
    }

    /// <summary>
    /// Registers Azure AD-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    public void RegisterValueFormatters(ValueFormatterRegistry registry)
    {
        if (_entityMapper is not null)
        {
            AzureValueFormatterRegistration.RegisterTenantAndManagementGroup(
                registry,
                "(^azuread$|.*/azuread$)",
                _entityMapper);
        }

        if (_principalMapper is not null)
        {
            var principalFormatter = new Providers.AzureRM.PrincipalIdFormatter(_principalMapper);
            registry.Register(
                new MatchPattern("(^azuread$|.*/azuread$)", null, "^(principal_object_id|resource_object_id)$", null),
                principalFormatter);
        }

        registry.Register(
            new MatchPattern(
                "(^azuread$|.*/azuread$)",
                null,
                "^app_role_id$",
                null),
            new AppRoleIdFormatter());
    }

    /// <summary>
    /// Registers Azure AD-specific icon providers.
    /// </summary>
    /// <param name="registry">The icon provider registry to register with.</param>
    public void RegisterIconProviders(IconProviderRegistry registry)
    {
        AzureAdIconProviderRegistration.Register(registry);
    }

    /// <summary>
    /// Registers Azure AD parent-child relationships for inline rendering.
    /// </summary>
    /// <param name="registry">The parent-child relationship registry to register with.</param>
    public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
    {
        registry.Register(new ParentChildRelationship
        {
            ParentResourceType = "azuread_group",
            ChildResourceType = "azuread_group_member",
            InlineAttributeName = "members",
            ChildReferenceAttribute = "group_object_id",
            ChildGroupLabel = "Members",
            TableColumns = [new ChildTableColumn("Member", "member")],
            RowExtractor = new AzureAdGroupMemberRowExtractor()
        });
    }

    /// <summary>
    /// Registers Azure AD-specific post-merge callbacks for updating group summaries.
    /// </summary>
    /// <param name="builder">The report model builder to register callbacks with.</param>
    public void RegisterPostMergeCallbacks(MarkdownGeneration.ReportModelBuilder builder)
    {
        builder.RegisterPostMergeCallback(AzureAdGroupSummaryRebuilder.UpdateGroupSummaries);
    }

    /// <summary>
    /// Registers Azure AD-specific C# resource renderers.
    /// </summary>
    /// <param name="registry">The resource renderer registry to register with.</param>
    public void RegisterResourceRenderers(ResourceRendererRegistry registry)
    {
        registry.Register(new UserRenderer());
        registry.Register(new GroupRenderer());
        registry.Register(new GroupWithoutMembersRenderer());
        registry.Register(new GroupMemberRenderer());
        registry.Register(new InvitationRenderer());
        registry.Register(new ServicePrincipalRenderer());
        registry.Register(new AppRoleAssignmentRenderer());
        registry.Register(new DirectoryRoleAssignmentRenderer());
        registry.Register(new DelegatedPermissionGrantRenderer());
    }
}
