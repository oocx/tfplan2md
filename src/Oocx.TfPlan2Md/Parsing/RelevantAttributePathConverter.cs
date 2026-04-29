using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Converts Terraform relevant_attributes[].attribute arrays into simple CLR objects.
/// Uses the same heterogeneous-path parsing pattern as ReplacePathsConverter.
/// Related feature: docs/features/122-terraform-1-15-support/specification.md.
/// </summary>
public class RelevantAttributePathConverter : JsonConverter<IReadOnlyList<object>>
{
    /// <inheritdoc />
    public override IReadOnlyList<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Array.Empty<object>();
        }

        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("relevant_attributes[].attribute must be an array.");
        }

        var result = new List<object>();
        foreach (var segment in document.RootElement.EnumerateArray())
        {
            result.Add(ConvertSegment(segment));
        }

        return result;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<object> value, JsonSerializerOptions options)
    {
        if (value is null || value.Count == 0)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var segment in value)
        {
            WriteSegment(writer, segment);
        }

        writer.WriteEndArray();
    }

    private static void WriteSegment(Utf8JsonWriter writer, object segment)
    {
        switch (segment)
        {
            case string text:
                writer.WriteStringValue(text);
                break;
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case long longValue:
                writer.WriteNumberValue(longValue);
                break;
            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;
            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                break;
            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;
            case null:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteStringValue(segment.ToString());
                break;
        }
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
