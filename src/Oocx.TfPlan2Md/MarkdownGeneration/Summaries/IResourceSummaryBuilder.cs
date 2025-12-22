using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

/// <summary>
/// Builds concise, human-readable summaries for resource changes.
/// Related feature: docs/features/replacement-reasons-and-summaries/specification.md
/// </summary>
public interface IResourceSummaryBuilder
{
    /// <summary>
    /// Builds a summary string for the specified resource change.
    /// </summary>
    /// <param name="change">The change to summarize.</param>
    /// <returns>A summary string or null when no summary is available.</returns>
    string? BuildSummary(ResourceChangeModel change);
}
