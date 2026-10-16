using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Renders per-resource collection drift entries inside the drift section.
/// Lines show deltas only, because each line is labelled and there are no Before/After columns to stay truthful to.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md (section 8).
/// </summary>
internal static class CollectionDriftRenderer
{
    /// <summary>
    /// Writes one collapsed details block for a drifted resource.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="entry">The collection drift entry.</param>
    internal static void Render(MarkdownWriter writer, CollectionDriftModel entry)
    {
        var count = entry.Paths.Count;
        var noun = count == 1 ? "collection" : "collections";
        var paths = string.Join(", ", entry.Paths.Select(path => ReportRenderer.FormatDriftCode(path.Path)));
        var summary = $"🌀 {ReportRenderer.EncodeDriftText(entry.ResourceType)} {ReportRenderer.FormatDriftCode(entry.Address)} — " +
            $"{count} changed {noun}: {paths}";

        writer.DetailsOpen(summary);
        writer.BlankLine();
        foreach (var path in entry.Paths)
        {
            writer.Paragraph($"- {ReportRenderer.FormatDriftCode(path.Path)}");
            foreach (var line in BuildLines(path.Collection))
            {
                writer.Paragraph($"  - {line}");
            }
        }

        writer.BlankLine();
        writer.DetailsClose();
    }

    /// <summary>
    /// Builds the delta lines of one collection.
    /// </summary>
    private static List<string> BuildLines(CollectionChangeModel collection)
    {
        if (collection.BeforeState == CollectionState.Masked || collection.AfterState == CollectionState.Masked)
        {
            return [ReportRenderer.FormatDriftCode("(sensitive)")];
        }

        if (collection.IsUnresolved || collection.AfterState == CollectionState.Unknown)
        {
            return ["unresolved: some values are known after apply"];
        }

        var quoteAmbiguous = collection.HasMixedKinds;
        // A masked entry is written as the placeholder; its value is not in the model and never reaches FormatDriftCode.
        string FormatElement(CollectionElementModel element)
            => ReportRenderer.FormatDriftCode(element.IsMasked
                ? CollectionValues.SensitivePlaceholder
                : CollectionValues.ElementDisplayText(element, quoteAmbiguous));

        var lines = new List<string>();
        lines.AddRange(collection.Before
            .Where(element => element.Status == CollectionElementStatus.Removed)
            .Select(element => $"{DefaultResourceRenderer.RemovedMarker} {FormatElement(element)}"));
        lines.AddRange(collection.After
            .Where(element => element.Status == CollectionElementStatus.Added)
            .Select(element => $"{DefaultResourceRenderer.AddedMarker} {FormatElement(element)}"));

        if (collection.OrderChanged)
        {
            var before = FormatRetained(collection.Before, FormatElement);
            var after = FormatRetained(collection.After, FormatElement);
            lines.Add($"{DefaultResourceRenderer.OrderChangedLine}: {before} → {after}");
        }

        if (lines.Count == 0)
        {
            lines.Add($"{DescribeState(collection.BeforeState)} → {DescribeState(collection.AfterState)}");
        }

        return lines;
    }

    private static string FormatRetained(IReadOnlyList<CollectionElementModel> elements, Func<CollectionElementModel, string> formatElement)
        => string.Join(", ", elements
            .Where(element => element.Status is CollectionElementStatus.Unchanged or CollectionElementStatus.Moved)
            .Select(formatElement));

    private static string DescribeState(CollectionState state) => state switch
    {
        CollectionState.Absent => "absent",
        CollectionState.Null => "null",
        CollectionState.Empty => "empty",
        _ => "present"
    };
}
