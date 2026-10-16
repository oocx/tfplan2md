using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
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
        attributeChanges.RemoveAll(attributeChange => ShouldSuppress(resourceChange.ProviderName, attributeChange));

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

    /// <summary>
    /// Decides whether a row is suppressed. A collection row is evaluated element-wise, by position: it is suppressed
    /// only when both sides have the same length, the order is unchanged, every position is either equal or
    /// suppressed by the registry, and at least one position was suppressed. This reproduces the outcome of the
    /// former per-index rows without teaching the core any provider rule.
    /// </summary>
    /// <remarks>
    /// A position holding a masked entry is neutral only when both entries are masked and both are unchanged; any
    /// other masked position keeps the row. Masked entries are never passed to the registry, so masked content can
    /// never be treated as a case-only difference
    /// (docs/features/148-compact-collection-diffs/architecture.md, section 6).
    /// </remarks>
    /// <param name="providerName">The Terraform provider name.</param>
    /// <param name="attributeChange">The row to evaluate.</param>
    /// <returns><c>true</c> when the row should be removed.</returns>
    private bool ShouldSuppress(string providerName, AttributeChangeModel attributeChange)
    {
        var collection = attributeChange.Collection;
        if (collection is null)
        {
            return _attributeChangeFilterRegistry.ShouldSuppress(
                new AttributeChangeFilterContext(providerName, attributeChange.Name, attributeChange.Before, attributeChange.After));
        }

        if (collection.IsUnresolved
            || collection.OrderChanged
            || collection.BeforeState != CollectionState.Elements
            || collection.AfterState != CollectionState.Elements
            || collection.Before.Count != collection.After.Count)
        {
            return false;
        }

        var differingPositions = 0;
        for (var index = 0; index < collection.Before.Count; index++)
        {
            var before = collection.Before[index];
            var after = collection.After[index];
            if (before.IsMasked || after.IsMasked)
            {
                if (IsUnchangedMaskedPair(before, after))
                {
                    continue;
                }

                return false;
            }

            if (before.Kind == after.Kind && string.Equals(before.Value, after.Value, StringComparison.Ordinal))
            {
                continue;
            }

            differingPositions++;
            var context = new AttributeChangeFilterContext(providerName, $"{attributeChange.Name}[{index}]", before.Value, after.Value);
            if (!_attributeChangeFilterRegistry.ShouldSuppress(context))
            {
                return false;
            }
        }

        return differingPositions > 0;
    }

    /// <summary>
    /// Determines whether a position holds two masked entries that the diff matched as unchanged.
    /// </summary>
    private static bool IsUnchangedMaskedPair(CollectionElementModel before, CollectionElementModel after)
        => before.IsMasked
            && after.IsMasked
            && before.Status == CollectionElementStatus.Unchanged
            && after.Status == CollectionElementStatus.Unchanged;
}
