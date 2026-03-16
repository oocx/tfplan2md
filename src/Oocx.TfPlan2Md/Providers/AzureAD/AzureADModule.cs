using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;
using Oocx.TfPlan2Md.Providers.AzureAD.Renderers;
using Oocx.TfPlan2Md.Providers.AzureRM;

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
    /// Optional resolver for Microsoft Graph app role GUIDs.
    /// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
    /// </summary>
    private readonly IAppRoleResolver? _appRoleResolver;

    /// <summary>
    /// Optional mapper for principal name resolution.
    /// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
    /// </summary>
    private readonly IPrincipalMapper? _principalMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureADModule"/> class.
    /// </summary>
    /// <param name="entityMapper">Optional mapper for tenant display names.</param>
    /// <param name="principalMapper">Optional mapper for principal name resolution.</param>
    /// <param name="appRoleResolver">Optional resolver for app role GUIDs.</param>
    public AzureADModule(AzureEntityMapper? entityMapper = null, IPrincipalMapper? principalMapper = null, IAppRoleResolver? appRoleResolver = null)
    {
        _entityMapper = entityMapper;
        _principalMapper = principalMapper;
        _appRoleResolver = appRoleResolver;
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
        var summaryFactory = new AzureAdSummaryFactory(_appRoleResolver);

        registry.RegisterFactory("azuread_user", summaryFactory);
        registry.RegisterFactory("azuread_group", summaryFactory);
        registry.RegisterFactory("azuread_group_without_members", summaryFactory);
        registry.RegisterFactory("azuread_group_member", summaryFactory);
        registry.RegisterFactory("azuread_service_principal", summaryFactory);
        registry.RegisterFactory("azuread_invitation", summaryFactory);
        registry.RegisterFactory("azuread_app_role_assignment", summaryFactory);
    }

    /// <summary>
    /// Registers Azure AD-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    public void RegisterValueFormatters(ValueFormatterRegistry registry)
    {
        // Register app role value formatters (independent of entity mapper)
        if (_appRoleResolver is not null)
        {
            registry.Register(
                new MatchPattern("(^azuread$|.*/azuread$)", "^azuread_app_role_assignment$", "^app_role_id$", null),
                new AppRoleIdFormatter(_appRoleResolver));
        }

        if (_principalMapper is not null)
        {
            registry.Register(
                new MatchPattern("(^azuread$|.*/azuread$)", "^azuread_app_role_assignment$", "^(principal_object_id|resource_object_id)$", null),
                new PrincipalIdFormatter(_principalMapper));
        }

        if (_entityMapper is null)
        {
            return;
        }

        AzureValueFormatterRegistration.RegisterTenantAndManagementGroup(
            registry,
            "(^azuread$|.*/azuread$)",
            _entityMapper);
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
    }
}
