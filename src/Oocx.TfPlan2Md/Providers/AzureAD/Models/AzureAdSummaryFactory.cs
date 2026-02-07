using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Applies Azure AD summary overrides for templates that rely on <see cref="ResourceChangeModel.SummaryHtml" />.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal sealed class AzureAdSummaryFactory : IResourceViewModelFactory
{
    /// <inheritdoc />
    public void ApplyViewModel(
        ResourceChangeModel model,
        ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        _ = attributeChanges;

        model.SummaryHtml = AzureAdSummaryBuilder.BuildSummaryHtml(
            model,
            resourceChange,
            action,
            principalMapper,
            iconProviderRegistry);
    }
}
