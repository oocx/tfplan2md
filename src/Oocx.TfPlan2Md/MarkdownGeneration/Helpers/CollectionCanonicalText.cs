using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Builds the canonical text of a collection side from model elements, so that masked values are never serialised.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md (section 3).
/// </summary>
internal static class CollectionCanonicalText
{
    /// <summary>
    /// Builds the canonical text of a collection side from its model elements, writing each masked element as the
    /// bare token <c>(sensitive)</c>, for example <c>["a",(sensitive),"b"]</c>. Used whenever entries may be masked,
    /// because the raw JSON array still contains the masked values.
    /// </summary>
    /// <param name="state">The state of the side.</param>
    /// <param name="elements">The model elements of the side.</param>
    /// <returns>The canonical text.</returns>
    internal static string FromModel(CollectionState state, IReadOnlyList<CollectionElementModel> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        switch (state)
        {
            case CollectionState.Absent:
                return "-";
            case CollectionState.Null:
                return "null";
            case CollectionState.Empty:
                return "[]";
        }

        var builder = new StringBuilder("[");
        for (var index = 0; index < elements.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(CanonicalElementText(elements[index]));
        }

        return builder.Append(']').ToString();
    }

    /// <summary>
    /// Returns the JSON text of one model element for canonical text; masked elements become <c>(sensitive)</c>.
    /// </summary>
    private static string CanonicalElementText(CollectionElementModel element)
    {
        if (element.IsMasked)
        {
            return CollectionValues.SensitivePlaceholder;
        }

        if (element.Status == CollectionElementStatus.Unknown || element.Value is null)
        {
            return "null";
        }

        if (element.Kind != JsonValueKind.String)
        {
            return element.Value;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStringValue(element.Value);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

}
