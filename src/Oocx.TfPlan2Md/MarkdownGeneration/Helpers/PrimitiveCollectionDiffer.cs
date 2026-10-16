using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// A path at which both plan sides hold a collection of primitive values (or one side is null or absent).
/// </summary>
/// <param name="Path">The flattened attribute path, for example <c>rule[0].prefixes</c>.</param>
/// <param name="Before">The before-side value, or <c>null</c> when the path is absent.</param>
/// <param name="After">The after-side value, or <c>null</c> when the path is absent.</param>
internal sealed record DetectedCollection(string Path, JsonElement? Before, JsonElement? After);

/// <summary>
/// Detects primitive collections in plan state and builds element lists for them.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md (section 2).
/// </summary>
internal static class PrimitiveCollectionDiffer
{
    /// <summary>
    /// Walks <paramref name="before"/> and <paramref name="after"/> in parallel and records each path where a collection
    /// of primitives (possibly empty) sits on at least one side and the other side is an array, null or absent.
    /// A single object or array element on either side disqualifies the path, which then keeps indexed rows.
    /// </summary>
    /// <param name="before">The raw before state, usually a <see cref="JsonElement"/>.</param>
    /// <param name="after">The raw after state, usually a <see cref="JsonElement"/>.</param>
    /// <returns>The detected collections keyed by their flattened path.</returns>
    internal static Dictionary<string, DetectedCollection> Detect(object? before, object? after)
    {
        var detected = new Dictionary<string, DetectedCollection>(StringComparer.Ordinal);
        Visit(string.Empty, AsElement(before), AsElement(after), detected);
        return detected;
    }

    private static JsonElement? AsElement(object? value) => value is JsonElement element ? element : null;

    private static void Visit(string path, JsonElement? before, JsonElement? after, Dictionary<string, DetectedCollection> detected)
    {
        var anyArray = before is { ValueKind: JsonValueKind.Array } || after is { ValueKind: JsonValueKind.Array };
        if (anyArray)
        {
            VisitArrays(path, before, after, detected);
            return;
        }

        if (IsObjectOrAbsent(before) && IsObjectOrAbsent(after))
        {
            var names = new HashSet<string>(PropertyNames(before), StringComparer.Ordinal);
            names.UnionWith(PropertyNames(after));
            foreach (var name in names)
            {
                var childPath = path.Length == 0 ? name : $"{path}.{name}";
                Visit(childPath, Property(before, name), Property(after, name), detected);
            }
        }
    }

    private static void VisitArrays(string path, JsonElement? before, JsonElement? after, Dictionary<string, DetectedCollection> detected)
    {
        if (path.Length == 0 || !OtherSideAllowsArray(before) || !OtherSideAllowsArray(after))
        {
            return;
        }

        var beforeItems = Items(before);
        var afterItems = Items(after);
        if (beforeItems.Concat(afterItems).All(IsPrimitive))
        {
            detected[path] = new DetectedCollection(path, before, after);
            return;
        }

        // Not a primitive collection: indexed rows stay, but primitive lists nested in its elements still compact.
        for (var index = 0; index < Math.Max(beforeItems.Count, afterItems.Count); index++)
        {
            // A list of lists keeps indexed rows, so only object elements are searched for nested collections.
            JsonElement? beforeChild = index < beforeItems.Count ? beforeItems[index] : null;
            JsonElement? afterChild = index < afterItems.Count ? afterItems[index] : null;
            if (beforeChild is { ValueKind: JsonValueKind.Array } || afterChild is { ValueKind: JsonValueKind.Array })
            {
                continue;
            }

            Visit(string.Create(CultureInfo.InvariantCulture, $"{path}[{index}]"), beforeChild, afterChild, detected);
        }
    }

    private static bool OtherSideAllowsArray(JsonElement? side)
        => side is null or { ValueKind: JsonValueKind.Array or JsonValueKind.Null };

    private static bool IsObjectOrAbsent(JsonElement? side)
        => side is null or { ValueKind: JsonValueKind.Object or JsonValueKind.Null };

    private static bool IsPrimitive(JsonElement element)
        => element.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array);

    private static List<JsonElement> Items(JsonElement? side)
        => side is { ValueKind: JsonValueKind.Array } array ? array.EnumerateArray().ToList() : [];

    private static IEnumerable<string> PropertyNames(JsonElement? side)
        => side is { ValueKind: JsonValueKind.Object } obj ? obj.EnumerateObject().Select(p => p.Name) : [];

    private static JsonElement? Property(JsonElement? side, string name)
        => side is { ValueKind: JsonValueKind.Object } obj && obj.TryGetProperty(name, out var value) ? value : null;
}
