using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Reads element lists, states and canonical text from collection values in plan JSON.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md (section 3).
/// </summary>
internal static class CollectionValues
{
    /// <summary>
    /// Builds the ordered element list of a collection side.
    /// </summary>
    /// <param name="side">The JSON value at the collection path, or <c>null</c> when absent.</param>
    /// <returns>The element texts and keys in order; empty when the side is not an array.</returns>
    internal static List<(string? Value, CollectionElementKey Key)> ReadElements(JsonElement? side)
    {
        var result = new List<(string?, CollectionElementKey)>();
        if (side is not { ValueKind: JsonValueKind.Array } array)
        {
            return result;
        }

        foreach (var element in array.EnumerateArray())
        {
            var value = ElementText(element);
            result.Add((value, new CollectionElementKey(element.ValueKind, value ?? "null")));
        }

        return result;
    }

    /// <summary>
    /// Returns the display text of a primitive element, or <c>null</c> for a JSON null.
    /// </summary>
    /// <param name="element">The primitive element.</param>
    /// <returns>The text, or <c>null</c> for JSON null.</returns>
    internal static string? ElementText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null
    };

    /// <summary>
    /// Describes the state of one side of a collection.
    /// </summary>
    /// <param name="side">The JSON value at the collection path, or <c>null</c> when absent.</param>
    /// <returns>The collection state.</returns>
    internal static CollectionState StateOf(JsonElement? side) => side switch
    {
        null => CollectionState.Absent,
        { ValueKind: JsonValueKind.Null } => CollectionState.Null,
        { ValueKind: JsonValueKind.Array } array => array.GetArrayLength() == 0 ? CollectionState.Empty : CollectionState.Elements,
        _ => CollectionState.Absent
    };

    /// <summary>
    /// Returns the compact JSON text of a collection side, <c>null</c> for JSON null and <c>-</c> when absent.
    /// </summary>
    /// <param name="side">The JSON value at the collection path, or <c>null</c> when absent.</param>
    /// <returns>The canonical text.</returns>
    internal static string CanonicalText(JsonElement? side) => side switch
    {
        null => "-",
        { ValueKind: JsonValueKind.Null } => "null",
        { } element => Compact(element)
    };

    /// <summary>
    /// Serialises a JSON element without insignificant whitespace.
    /// </summary>
    /// <param name="element">The element to serialise.</param>
    /// <returns>Compact JSON text.</returns>
    internal static string Compact(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            element.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Returns the text shown for an element. A string that looks like another JSON type (a number, <c>true</c>,
    /// <c>false</c> or <c>null</c>) is quoted so that <c>"1"</c> and <c>1</c> stay visibly different.
    /// An empty string is shown as <c>""</c> and a JSON null as <c>null</c>.
    /// </summary>
    /// <param name="element">The collection element.</param>
    /// <returns>The display text before any markdown formatting.</returns>
    internal static string ElementDisplayText(CollectionElementModel element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var value = element.Value;
        if (value is null)
        {
            return "null";
        }

        if (element.Kind != JsonValueKind.String)
        {
            return value;
        }

        if (value.Length == 0)
        {
            return "\"\"";
        }

        var looksLikeOtherType = value is "true" or "false" or "null"
            || double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        return looksLikeOtherType ? $"\"{value}\"" : value;
    }
}
