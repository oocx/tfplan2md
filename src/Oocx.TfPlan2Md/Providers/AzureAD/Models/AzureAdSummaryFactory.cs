using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Applies Azure AD summary overrides for templates that rely on <see cref="ResourceChangeModel.SummaryHtml" />.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal sealed class AzureAdSummaryFactory : IResourceViewModelFactory
{
    /// <inheritdoc />
    public void ApplyViewModel(ApplyViewModelContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Model.SummaryHtml = AzureAdSummaryBuilder.BuildSummaryHtml(
            context.Model,
            context.ResourceChange,
            context.Action,
            context.PrincipalMapper,
            context.IconProviderRegistry);
    }
}
