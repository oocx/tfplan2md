using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Applies Azure AD summary overrides for templates that rely on <see cref="ResourceChangeModel.SummaryHtml" />.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal sealed class AzureAdSummaryFactory : IResourceViewModelFactory
{
    /// <summary>
    /// Optional resolver for Microsoft Graph app role GUIDs.
    /// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
    /// </summary>
    private readonly IAppRoleResolver? _appRoleResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdSummaryFactory"/> class.
    /// </summary>
    /// <param name="appRoleResolver">Optional resolver for app role GUIDs.</param>
    public AzureAdSummaryFactory(IAppRoleResolver? appRoleResolver = null)
    {
        _appRoleResolver = appRoleResolver;
    }

    /// <inheritdoc />
    public void ApplyViewModel(ApplyViewModelContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Model.SummaryHtml = AzureAdSummaryBuilder.BuildSummaryHtml(
            context.Model,
            context.ResourceChange,
            context.Action,
            context.PrincipalMapper,
            context.IconProviderRegistry,
            _appRoleResolver);
    }
}
