using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Extracts display values for a child resource table row.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
internal interface IChildRowExtractor
{
    /// <summary>
    /// Extracts column values for a single child entry.
    /// </summary>
    /// <param name="childState">The child state object from Terraform JSON.</param>
    /// <param name="providerName">The provider name used for formatting decisions.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <returns>A mapping from column property names to formatted display values.</returns>
    IReadOnlyDictionary<string, string> ExtractRow(
        object? childState,
        string providerName,
        IconProviderRegistry? iconProviderRegistry);
}
