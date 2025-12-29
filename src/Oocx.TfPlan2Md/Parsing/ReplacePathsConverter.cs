using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Converts Terraform replace_paths arrays into simple CLR objects (strings, numbers, booleans, nulls).
/// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md
/// </summary>
public class ReplacePathsConverter : JsonConverter<IReadOnlyList<IReadOnlyList<object>>?>
{
    /// <inheritdoc />
    public override IReadOnlyList<IReadOnlyList<object>>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("replace_paths must be an array of arrays.");
        }

        var outer = new List<IReadOnlyList<object>>();
        foreach (var pathElement in document.RootElement.EnumerateArray())
        {
            if (pathElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var inner = new List<object>();
            foreach (var segment in pathElement.EnumerateArray())
            {
                inner.Add(ConvertSegment(segment));
            }

            outer.Add(inner);
        }

        return outer;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<IReadOnlyList<object>>? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }

    private static object ConvertSegment(JsonElement segment)
    {
        return segment.ValueKind switch
        {
            JsonValueKind.String => segment.GetString() ?? string.Empty,
            JsonValueKind.Number when segment.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when segment.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number => segment.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,
            _ => segment.ToString()
        };
    }
}
