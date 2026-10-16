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
        var lines = elements
            .Select(element => FormatCollectionElement(attribute.Name, element, withMarkers, Format))
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
        Func<string, string?, string> format)
    {
        string text;
        if (element.Status == CollectionElementStatus.Unknown)
        {
            text = format(path, element.Value);
        }
        else if (string.IsNullOrWhiteSpace(element.Value))
        {
            text = MarkdownHelpers.FormatCodeTable(CollectionValues.ElementDisplayText(element));
        }
        else
        {
            text = format($"{path}[{element.Index}]", CollectionValues.ElementDisplayText(element));
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
    /// <param name="attribute">The large collection row.</param>
    /// <returns>Markdown for the attribute body.</returns>
    private static string FormatLargeCollection(AttributeChangeModel attribute)
    {
        var collection = attribute.Collection!;
        var comparing = collection.BeforeState is CollectionState.Elements or CollectionState.Empty
            && collection.AfterState is CollectionState.Elements or CollectionState.Empty;
        var builder = new StringBuilder();

        var showBefore = collection.BeforeState is CollectionState.Elements or CollectionState.Empty;
        var showAfter = collection.AfterState is CollectionState.Elements or CollectionState.Empty or CollectionState.Unknown or CollectionState.Masked;
        if (showBefore)
        {
            AppendLargeBlock(builder, comparing && showAfter ? "Before" : null, collection.Before, comparing, attribute.Before);
        }

        if (showAfter)
        {
            var lines = collection.AfterState is CollectionState.Unknown or CollectionState.Masked
                ? [attribute.After ?? "-"]
                : LargeLines(collection.After, comparing);
            if (comparing && collection.OrderChanged)
            {
                lines.Add(OrderChangedLine);
            }

            AppendLargeFence(builder, showBefore && comparing ? "After" : null, lines);
        }

        return builder.ToString().TrimEnd('\n') + "\n";
    }

    private static void AppendLargeBlock(
        StringBuilder builder,
        string? label,
        IReadOnlyList<CollectionElementModel> elements,
        bool withMarkers,
        string? fallback)
    {
        var lines = elements.Count == 0 ? [fallback ?? "[]"] : LargeLines(elements, withMarkers);
        AppendLargeFence(builder, label, lines);
    }

    private static List<string> LargeLines(IEnumerable<CollectionElementModel> elements, bool withMarkers)
        => elements.Select(element =>
        {
            var text = CollectionValues.ElementDisplayText(element).Replace("\r\n", "\\n", StringComparison.Ordinal)
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
