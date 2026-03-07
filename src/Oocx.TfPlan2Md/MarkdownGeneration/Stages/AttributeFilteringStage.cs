using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Default implementation of the attribute-filtering pipeline stage.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal sealed class AttributeFilteringStage : IAttributeFilteringStage
{
    /// <summary>
    /// Indicates whether Azure ID case-only rows should be suppressed.
    /// </summary>
    private readonly bool _ignoreAzureIdCaseChanges;

    /// <summary>
    /// Registry of attribute suppression filters.
    /// </summary>
    private readonly AttributeChangeFilterRegistry _attributeChangeFilterRegistry;

    /// <summary>
    /// Strategy used to rebuild default summaries after filtering.
    /// </summary>
    private readonly IResourceSummaryBuilder _summaryBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttributeFilteringStage"/> class.
    /// </summary>
    /// <param name="ignoreAzureIdCaseChanges">Whether Azure ID case-only rows should be suppressed.</param>
    /// <param name="attributeChangeFilterRegistry">Registry of attribute suppression filters.</param>
    /// <param name="summaryBuilder">Strategy used to rebuild default summaries after filtering.</param>
    internal AttributeFilteringStage(
        bool ignoreAzureIdCaseChanges,
        AttributeChangeFilterRegistry attributeChangeFilterRegistry,
        IResourceSummaryBuilder summaryBuilder)
    {
        _ignoreAzureIdCaseChanges = ignoreAzureIdCaseChanges;
        _attributeChangeFilterRegistry = attributeChangeFilterRegistry;
        _summaryBuilder = summaryBuilder;
    }

    /// <inheritdoc />
    public IReadOnlyList<ResourceChangeModel> Build(IReadOnlyList<ResourceChangeModel> resourceChanges)
    {
        if (!_ignoreAzureIdCaseChanges)
        {
            return resourceChanges;
        }

        foreach (var resourceChange in resourceChanges)
        {
            FilterAttributes(resourceChange);
        }

        return resourceChanges;
    }

    /// <summary>
    /// Filters suppressible attributes from a resource model and refreshes default summaries.
    /// </summary>
    /// <param name="resourceChange">The resource model to update.</param>
    private void FilterAttributes(ResourceChangeModel resourceChange)
    {
        if (resourceChange.AttributeChanges is not List<AttributeChangeModel> attributeChanges
            || attributeChanges.Count == 0)
        {
            return;
        }

        var defaultSummary = _summaryBuilder.BuildSummary(resourceChange);
        var defaultChangedAttributesSummary = ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary(resourceChange.AttributeChanges, resourceChange.Action);
        var defaultSummaryHtml = ResourceSummaryHtmlBuilder.BuildSummaryHtml(resourceChange);

        var usesDefaultSummary = string.IsNullOrWhiteSpace(resourceChange.Summary)
            || string.Equals(resourceChange.Summary, defaultSummary, StringComparison.Ordinal);
        var usesDefaultChangedAttributesSummary = string.IsNullOrWhiteSpace(resourceChange.ChangedAttributesSummary)
            || string.Equals(resourceChange.ChangedAttributesSummary, defaultChangedAttributesSummary, StringComparison.Ordinal);
        var usesDefaultSummaryHtml = string.IsNullOrWhiteSpace(resourceChange.SummaryHtml)
            || string.Equals(resourceChange.SummaryHtml, defaultSummaryHtml, StringComparison.Ordinal);

        var originalCount = attributeChanges.Count;
        attributeChanges.RemoveAll(attributeChange => _attributeChangeFilterRegistry.ShouldSuppress(
            new AttributeChangeFilterContext(
                resourceChange.ProviderName,
                attributeChange.Name,
                attributeChange.Before,
                attributeChange.After)));

        if (attributeChanges.Count == originalCount)
        {
            return;
        }

        if (usesDefaultSummary)
        {
            resourceChange.Summary = _summaryBuilder.BuildSummary(resourceChange);
        }

        if (usesDefaultChangedAttributesSummary)
        {
            resourceChange.ChangedAttributesSummary = ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary(resourceChange.AttributeChanges, resourceChange.Action);
        }

        if (usesDefaultSummaryHtml)
        {
            resourceChange.SummaryHtml = ResourceSummaryHtmlBuilder.BuildSummaryHtml(resourceChange);
        }
    }
}
