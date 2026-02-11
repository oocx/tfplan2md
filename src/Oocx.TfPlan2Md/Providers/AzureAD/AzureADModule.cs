using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureAD;

/// <summary>
/// Provider module for Azure AD (Entra) resources.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal sealed class AzureADModule : IProviderModule
{
    /// <summary>
    /// Optional mapper for tenant display name resolution.
    /// </summary>
    private readonly AzureEntityMapper? _entityMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureADModule"/> class.
    /// </summary>
    /// <param name="entityMapper">Optional mapper for tenant display names.</param>
    public AzureADModule(AzureEntityMapper? entityMapper = null)
    {
        _entityMapper = entityMapper;
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
    /// Registers Azure AD-specific Scriban helper functions.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register helpers with.</param>
    public void RegisterHelpers(ScriptObject scriptObject)
    {
        // Azure AD provider currently uses only core helpers
        // Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
    }

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
    }

    /// <summary>
    /// Registers Azure AD-specific value formatters.
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
}
