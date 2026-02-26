using System.Globalization;
using System.Text.Json;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Provides helpers for detecting Terraform <c>after_unknown</c> values.
/// </summary>
/// <remarks>
/// The Terraform plan JSON can represent unknown post-apply values either as a root boolean
/// (<c>after_unknown: true</c>) or as a nested object/array tree where leaf nodes are
/// <see langword="true"/> for unknown attributes.
/// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
/// </remarks>
internal static class AfterUnknownHelper
{
    /// <summary>
    /// Determines whether the whole resource is marked unknown after apply.
    /// </summary>
    /// <param name="afterUnknown">The raw <c>after_unknown</c> value from Terraform plan JSON.</param>
    /// <returns><see langword="true"/> when <paramref name="afterUnknown"/> is a root boolean true; otherwise <see langword="false"/>.</returns>
    internal static bool IsWholeResourceUnknownAfterApply(object? afterUnknown)
    {
        if (!TryGetJsonElement(afterUnknown, out var root))
        {
            return false;
        }

        return root.ValueKind == JsonValueKind.True;
    }

    /// <summary>
    /// Determines whether a flattened attribute key is marked unknown after apply.
    /// </summary>
    /// <param name="afterUnknown">The raw <c>after_unknown</c> value from Terraform plan JSON.</param>
    /// <param name="flattenedKey">Flattened key, e.g. <c>tags.env</c> or <c>rules[0].priority</c>.</param>
    /// <returns><see langword="true"/> if the path or one of its parent subtrees is marked unknown; otherwise <see langword="false"/>.</returns>
    internal static bool IsAttributeUnknownAfterApply(object? afterUnknown, string flattenedKey)
    {
        if (!TryGetJsonElement(afterUnknown, out var current))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(flattenedKey))
        {
            return false;
        }

        if (!TryParseFlattenedKey(flattenedKey, out var pathSegments))
        {
            return false;
        }

        foreach (var segment in pathSegments)
        {
            if (current.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (!TryMoveNext(current, segment, out current))
            {
                return false;
            }
        }

        return current.ValueKind == JsonValueKind.True;
    }

    /// <summary>
    /// Tries to move to the next path segment in an <c>after_unknown</c> tree.
    /// </summary>
    /// <param name="current">Current node in the JSON tree.</param>
    /// <param name="segment">Next path segment.</param>
    /// <param name="next">Resolved next node when successful.</param>
    /// <returns><see langword="true"/> when segment resolution succeeds; otherwise <see langword="false"/>.</returns>
    private static bool TryMoveNext(JsonElement current, PathSegment segment, out JsonElement next)
    {
        next = default;

        if (segment.IsArrayIndex)
        {
            if (current.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            if (segment.ArrayIndex < 0 || segment.ArrayIndex >= current.GetArrayLength())
            {
                return false;
            }

            next = current[segment.ArrayIndex];
            return true;
        }

        if (current.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return current.TryGetProperty(segment.PropertyName, out next);
    }

    /// <summary>
    /// Tries to parse a flattened attribute key into navigation segments.
    /// </summary>
    /// <param name="flattenedKey">Flattened key to parse.</param>
    /// <param name="segments">Parsed path segments.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    private static bool TryParseFlattenedKey(string flattenedKey, out List<PathSegment> segments)
    {
        segments = [];

        var dottedSegments = flattenedKey.Split('.');
        if (dottedSegments.Length == 0)
        {
            return false;
        }

        foreach (var dottedSegment in dottedSegments)
        {
            if (string.IsNullOrWhiteSpace(dottedSegment))
            {
                segments = [];
                return false;
            }

            if (!TryParseDottedSegment(dottedSegment, segments))
            {
                segments = [];
                return false;
            }
        }

        return segments.Count > 0;
    }

    /// <summary>
    /// Parses one dotted segment into property and array-index segments.
    /// </summary>
    /// <param name="dottedSegment">Segment between dots, e.g. <c>rules[0]</c>.</param>
    /// <param name="segments">Destination collection for parsed segments.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    private static bool TryParseDottedSegment(string dottedSegment, List<PathSegment> segments)
    {
        if (!TryParseLeadingProperty(dottedSegment, segments, out var position))
        {
            return false;
        }

        return TryParseArrayIndices(dottedSegment, position, segments);
    }

    /// <summary>
    /// Parses an optional leading property name from a dotted segment.
    /// </summary>
    /// <param name="dottedSegment">Segment between dots.</param>
    /// <param name="segments">Destination collection for parsed segments.</param>
    /// <param name="position">Start position for parsing any following array indexes.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    private static bool TryParseLeadingProperty(string dottedSegment, List<PathSegment> segments, out int position)
    {
        position = 0;
        if (dottedSegment[0] == '[')
        {
            return true;
        }

        var bracketPosition = dottedSegment.IndexOf('[');
        var propertyName = bracketPosition >= 0
            ? dottedSegment[..bracketPosition]
            : dottedSegment;

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        segments.Add(PathSegment.ForProperty(propertyName));
        position = propertyName.Length;
        return true;
    }

    /// <summary>
    /// Parses zero or more trailing array indexes from a dotted segment.
    /// </summary>
    /// <param name="dottedSegment">Segment between dots.</param>
    /// <param name="startPosition">Position to start parsing indexes from.</param>
    /// <param name="segments">Destination collection for parsed segments.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    private static bool TryParseArrayIndices(string dottedSegment, int startPosition, List<PathSegment> segments)
    {
        var position = startPosition;
        while (position < dottedSegment.Length)
        {
            if (!TryParseSingleArrayIndex(dottedSegment, ref position, out var index))
            {
                return false;
            }

            segments.Add(PathSegment.ForArrayIndex(index));
        }

        return true;
    }

    /// <summary>
    /// Parses a single array index token (e.g. <c>[0]</c>) from a segment.
    /// </summary>
    /// <param name="dottedSegment">Segment being parsed.</param>
    /// <param name="position">Current parsing position; advanced on success.</param>
    /// <param name="index">Parsed array index.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    private static bool TryParseSingleArrayIndex(string dottedSegment, ref int position, out int index)
    {
        index = -1;
        if (position >= dottedSegment.Length || dottedSegment[position] != '[')
        {
            return false;
        }

        position++;
        var indexStart = position;

        while (position < dottedSegment.Length && char.IsDigit(dottedSegment[position]))
        {
            position++;
        }

        if (indexStart == position)
        {
            return false;
        }

        if (position >= dottedSegment.Length || dottedSegment[position] != ']')
        {
            return false;
        }

        var indexText = dottedSegment[indexStart..position];
        if (!int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out index))
        {
            return false;
        }

        position++;
        return true;
    }

    /// <summary>
    /// Tries to normalize the incoming unknown object into a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="afterUnknown">Raw unknown object.</param>
    /// <param name="element">Parsed JSON element when successful.</param>
    /// <returns><see langword="true"/> when the input is a <see cref="JsonElement"/>; otherwise <see langword="false"/>.</returns>
    private static bool TryGetJsonElement(object? afterUnknown, out JsonElement element)
    {
        if (afterUnknown is JsonElement jsonElement)
        {
            element = jsonElement;
            return true;
        }

        if (afterUnknown is bool boolValue)
        {
            using var document = JsonDocument.Parse(boolValue ? "true" : "false");
            element = document.RootElement.Clone();
            return true;
        }

        element = default;
        return false;
    }

    /// <summary>
    /// Represents one path segment for unknown-tree navigation.
    /// </summary>
    /// <param name="PropertyName">Property name when the segment targets an object member.</param>
    /// <param name="ArrayIndex">Array index when the segment targets an array element.</param>
    /// <param name="IsArrayIndex">Indicates whether the segment is an array index.</param>
    private readonly record struct PathSegment(string PropertyName, int ArrayIndex, bool IsArrayIndex)
    {
        /// <summary>
        /// Creates a property segment.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <returns>A property path segment.</returns>
        internal static PathSegment ForProperty(string propertyName)
            => new(propertyName, -1, false);

        /// <summary>
        /// Creates an array-index segment.
        /// </summary>
        /// <param name="arrayIndex">Array index.</param>
        /// <returns>An array-index path segment.</returns>
        internal static PathSegment ForArrayIndex(int arrayIndex)
            => new(string.Empty, arrayIndex, true);
    }
}
