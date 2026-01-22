using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Helper functions for summarizing large attribute value changes.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Builds the summary string for a set of large attributes.
    /// Related feature: docs/features/006-large-attribute-value-display/specification.md.
    /// </summary>
    /// <param name="attributes">Collection of attribute change objects (ScriptArray) with name/before/after.</param>
    /// <returns>Summary string like "Large values: policy (3 lines, 2 changed)" or empty when none.</returns>
    public static string LargeAttributesSummary(object? attributes)
    {
        var attrList = ToAttributeList(attributes);
        if (attrList.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var attr in attrList)
        {
            var before = attr.Before ?? string.Empty;
            var after = attr.After ?? string.Empty;

            var totalLines = CountTotalLines(before, after);
            var changedLines = CountChangedLines(before, after);

            var totalLabel = totalLines == 1 ? "line" : "lines";
            var changedLabel = changedLines == 1 ? "change" : "changes";
            parts.Add($"{attr.Name} ({totalLines} {totalLabel}, {changedLines} {changedLabel})");
        }

        return $"Large values: {string.Join(", ", parts)}";
    }

    /// <summary>
    /// Converts raw attribute change objects into strongly typed attribute change info entries.
    /// </summary>
    /// <param name="attributes">Attributes passed from Scriban context.</param>
    /// <returns>List of attribute change information.</returns>
    private static List<AttributeChangeInfo> ToAttributeList(object? attributes)
    {
        var list = new List<AttributeChangeInfo>();

        if (attributes is null)
        {
            return list;
        }

        if (attributes is IEnumerable enumerable && attributes is not string)
        {
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                list.Add(ToAttributeChangeInfo(item));
            }
        }

        return list;
    }

    /// <summary>
    /// Creates a typed attribute change entry from a dynamic object or ScriptObject.
    /// </summary>
    /// <param name="item">The raw item to map.</param>
    /// <returns>Attribute change information instance.</returns>
    private static AttributeChangeInfo ToAttributeChangeInfo(object item)
    {
        if (item is ScriptObject obj)
        {
            var name = obj.TryGetValue("name", out var n) ? n?.ToString() ?? string.Empty : string.Empty;
            var before = obj.TryGetValue("before", out var b) ? b?.ToString() : null;
            var after = obj.TryGetValue("after", out var a) ? a?.ToString() : null;

            return new AttributeChangeInfo(name, before, after);
        }

        if (item is AttributeChangeModel attrModel)
        {
            return new AttributeChangeInfo(attrModel.Name, attrModel.Before, attrModel.After);
        }

        if (item is Models.RoleAssignmentAttributeViewModel roleAttr)
        {
            return new AttributeChangeInfo(roleAttr.Name, roleAttr.Before, roleAttr.After);
        }

        if (item is AttributeChangeInfo info)
        {
            return info;
        }

        if (item is IReadOnlyDictionary<string, object?> readOnlyDictionary)
        {
            return FromDictionary(readOnlyDictionary);
        }

        if (item is IDictionary<string, object?> dictionary)
        {
            return FromDictionary(dictionary);
        }

        return new AttributeChangeInfo(string.Empty, item.ToString(), item.ToString());
    }

    private static AttributeChangeInfo FromDictionary(IReadOnlyDictionary<string, object?> data)
    {
        var name = data.TryGetValue("name", out var n) ? n?.ToString() ?? string.Empty : string.Empty;
        var before = data.TryGetValue("before", out var b) ? b?.ToString() : null;
        var after = data.TryGetValue("after", out var a) ? a?.ToString() : null;

        return new AttributeChangeInfo(name, before, after);
    }

    private static AttributeChangeInfo FromDictionary(IDictionary<string, object?> data)
    {
        var name = data.TryGetValue("name", out var n) ? n?.ToString() ?? string.Empty : string.Empty;
        var before = data.TryGetValue("before", out var b) ? b?.ToString() : null;
        var after = data.TryGetValue("after", out var a) ? a?.ToString() : null;

        return new AttributeChangeInfo(name, before, after);
    }

    /// <summary>
    /// Counts total unique lines across before and after values.
    /// </summary>
    /// <param name="before">The original value.</param>
    /// <param name="after">The updated value.</param>
    /// <returns>Total distinct line count.</returns>
    private static int CountTotalLines(string before, string after)
    {
        var beforeLines = SplitLines(before);
        var afterLines = SplitLines(after);
        var set = new HashSet<string>(beforeLines, StringComparer.Ordinal);
        foreach (var line in afterLines)
        {
            set.Add(line);
        }

        return set.Count;
    }

    /// <summary>
    /// Counts the number of changed lines between before and after values using diff tracking.
    /// </summary>
    /// <param name="before">Original value.</param>
    /// <param name="after">Updated value.</param>
    /// <returns>Number of lines marked as added or removed.</returns>
    private static int CountChangedLines(string before, string after)
    {
        var beforeLines = SplitLines(before);
        var afterLines = SplitLines(after);
        var diff = BuildLineDiff(beforeLines, afterLines);
        return diff.Count(d => d.Kind != DiffKind.Unchanged);
    }
}
