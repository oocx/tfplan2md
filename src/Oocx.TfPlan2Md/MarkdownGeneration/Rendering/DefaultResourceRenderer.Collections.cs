using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Rendering of compact primitive collection rows in generic attribute tables and the large-value section.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md (section 7).
/// </summary>
internal sealed partial class DefaultResourceRenderer
{
    /// <summary>Marker prefixed to occurrences that exist only before.</summary>
    internal const string RemovedMarker = "➖";

    /// <summary>Marker prefixed to occurrences that exist only after.</summary>
    internal const string AddedMarker = "➕";

    /// <summary>Line appended to the after cell when retained occurrences changed their relative order.</summary>
    internal const string OrderChangedLine = "🔀\u00A0order changed";

    /// <summary>
    /// Formats one side of a collection row for a table cell.
    /// </summary>
    /// <param name="attribute">The collection row.</param>
    /// <param name="beforeSide"><c>true</c> to format the before side; otherwise the after side.</param>
    /// <param name="withMarkers">Whether removed, added and order-change markers are shown.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="resourceType">Resource type used for icon lookup, or <c>null</c>.</param>
    /// <param name="valueFormatterRegistry">Optional value formatter registry.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>The markdown for the cell.</returns>
    private static string FormatCollectionCell(
        AttributeChangeModel attribute,
        bool beforeSide,
        bool withMarkers,
        ResourceChangeModel change,
        string? resourceType,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        var collection = attribute.Collection!;
        var state = beforeSide ? collection.BeforeState : collection.AfterState;
        string Format(string name, string? text) => MarkdownHelpers.FormatAttributeValueTableWithRegistryResource(
            name, text, change.ProviderName, resourceType, valueFormatterRegistry, iconProviderRegistry);

        switch (state)
        {
            case CollectionState.Masked:
            case CollectionState.Unknown:
                return Format(attribute.Name, beforeSide ? attribute.Before : attribute.After);
            case CollectionState.Absent:
                return "-";
            case CollectionState.Null:
                return MarkdownHelpers.FormatCodeTable("null");
            case CollectionState.Empty:
                return MarkdownHelpers.FormatCodeTable("[]");
        }

        var elements = beforeSide ? collection.Before : collection.After;
        var quoteAmbiguous = collection.HasMixedKinds;
        var lines = elements
            .Select(element => FormatCollectionElement(attribute.Name, element, withMarkers, quoteAmbiguous, Format))
            .ToList();
        if (withMarkers && !beforeSide && collection.OrderChanged)
        {
            lines.Add(OrderChangedLine);
        }

        return string.Join("<br>", lines);
    }

    /// <summary>
    /// Formats one occurrence, prefixing the marker outside the code span so element text can never mimic a marker.
    /// </summary>
    private static string FormatCollectionElement(
        string path,
        CollectionElementModel element,
        bool withMarkers,
        bool quoteAmbiguous,
        Func<string, string?, string> format)
    {
        string text;
        if (element.IsMasked)
        {
            // Checked before every other branch: a masked element never reaches a value formatter, so no display
            // name or other annotation can be derived from it.
            text = MarkdownHelpers.FormatCodeTable(CollectionValues.SensitivePlaceholder);
        }
        else if (element.Status == CollectionElementStatus.Unknown)
        {
            text = format(path, element.Value);
        }
        else if (string.IsNullOrWhiteSpace(element.Value))
        {
            text = MarkdownHelpers.FormatCodeTable(CollectionValues.ElementDisplayText(element, quoteAmbiguous));
        }
        else
        {
            text = format($"{path}[{element.Index}]", CollectionValues.ElementDisplayText(element, quoteAmbiguous));
        }

        if (!withMarkers)
        {
            return text;
        }

        return element.Status switch
        {
            CollectionElementStatus.Removed => $"{RemovedMarker}\u00A0{text}",
            CollectionElementStatus.Added => $"{AddedMarker}\u00A0{text}",
            _ => text
        };
    }

    /// <summary>
    /// Renders the body of a large collection attribute: one occurrence per line, with the same markers as the table.
    /// A reorder is shown as an order change, not as a removed/added pair.
    /// </summary>
    /// <remarks>
    /// On an update both sides are always shown, labelled, so an element-less side stays visible as <c>-</c>
    /// (absent), <c>null</c> or <c>[]</c>, exactly as in the table cell. On a create or delete, where the table has a
    /// single value column, the absent side is omitted and the remaining side is shown without markers.
    /// </remarks>
    /// <param name="attribute">The large collection row.</param>
    /// <param name="twoSided"><c>true</c> when the resource shows before and after values (not a create or delete).</param>
    /// <returns>Markdown for the attribute body.</returns>
    private static string FormatLargeCollection(AttributeChangeModel attribute, bool twoSided)
    {
        var collection = attribute.Collection!;
        var showBefore = twoSided || collection.BeforeState != CollectionState.Absent;
        var showAfter = twoSided || collection.AfterState != CollectionState.Absent;
        var labelled = showBefore && showAfter;
        var builder = new StringBuilder();

        if (showBefore)
        {
            AppendLargeFence(builder, labelled ? "Before" : null, LargeSideLines(attribute, beforeSide: true, withMarkers: twoSided));
        }

        if (showAfter)
        {
            AppendLargeFence(builder, labelled ? "After" : null, LargeSideLines(attribute, beforeSide: false, withMarkers: twoSided));
        }

        return builder.ToString().TrimEnd('\n') + "\n";
    }

    /// <summary>
    /// Returns the lines of one side of a large collection. Element-less states use the table's placeholders, and a
    /// side masked or unknown as a whole uses the row's display text, which never holds a masked value.
    /// </summary>
    private static List<string> LargeSideLines(AttributeChangeModel attribute, bool beforeSide, bool withMarkers)
    {
        var collection = attribute.Collection!;
        var state = beforeSide ? collection.BeforeState : collection.AfterState;
        switch (state)
        {
            case CollectionState.Absent:
                return ["-"];
            case CollectionState.Null:
                return ["null"];
            case CollectionState.Empty:
                return ["[]"];
            case CollectionState.Masked:
            case CollectionState.Unknown:
                return [(beforeSide ? attribute.Before : attribute.After) ?? "-"];
        }

        var lines = LargeLines(beforeSide ? collection.Before : collection.After, withMarkers, collection.HasMixedKinds);
        if (withMarkers && !beforeSide && collection.OrderChanged)
        {
            lines.Add(OrderChangedLine);
        }

        return lines;
    }

    private static List<string> LargeLines(IEnumerable<CollectionElementModel> elements, bool withMarkers, bool quoteAmbiguous)
        => elements.Select(element =>
        {
            var text = CollectionValues.ElementDisplayText(element, quoteAmbiguous).Replace("\r\n", "\\n", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)
                .Replace("\r", "\\n", StringComparison.Ordinal);
            if (!withMarkers)
            {
                return text;
            }

            return element.Status switch
            {
                CollectionElementStatus.Removed => $"{RemovedMarker}\u00A0{text}",
                CollectionElementStatus.Added => $"{AddedMarker}\u00A0{text}",
                _ => text
            };
        }).ToList();

    private static void AppendLargeFence(StringBuilder builder, string? label, List<string> lines)
    {
        if (label is not null)
        {
            builder.Append(label).Append(":\n\n");
        }

        var longestRun = lines.Select(LongestBacktickRun).DefaultIfEmpty(0).Max();
        var fence = new string('`', Math.Max(3, longestRun + 1));
        builder.Append(fence).Append("text\n");
        foreach (var line in lines)
        {
            builder.Append(line).Append('\n');
        }

        builder.Append(fence).Append("\n\n");
    }

    private static int LongestBacktickRun(string text)
    {
        var longest = 0;
        var current = 0;
        foreach (var character in text)
        {
            current = character == '`' ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }

        return longest;
    }
}
