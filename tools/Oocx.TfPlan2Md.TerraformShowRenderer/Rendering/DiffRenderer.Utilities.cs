using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>Utility helpers for diff rendering. Related feature: docs/features/030-terraform-show-approximation/specification.md</summary>
internal sealed partial class DiffRenderer
{
    /// <summary>
    /// Determines if a JSON object is a map (all scalar values) vs a block (nested structures).
    /// Maps in Terraform render with quoted keys, blocks with unquoted keys.
    /// </summary>
    /// <param name="obj">Object to check.</param>
    /// <returns>True if object is a map (all values are scalars), false if it's a block.</returns>
    private static bool IsMap(JsonElement obj)
    {
        if (obj.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // A map has all scalar values (string, number, bool, null)
        // A block has nested objects, arrays, or mixed types
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Writes scalar lines matching Terraform marker and value formatting.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="marker">Change marker to prefix the line.</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <param name="name">Attribute name to render.</param>
    /// <param name="value">Rendered attribute value.</param>
    /// <param name="appendNull">Whether to append the Terraform <c>-> null</c> suffix.</param>
    /// <param name="appendReplacement">Whether to append the replacement comment.</param>
    /// <returns>Nothing.</returns>
    private static void WriteScalarLine(AnsiTextWriter writer, string indent, string marker, AnsiStyle style, string name, string value, bool appendNull = false, bool appendReplacement = false, int nameWidth = 0)
    {
        writer.Write(indent);
        if (!string.IsNullOrEmpty(marker))
        {
            writer.WriteStyled(marker, style);
            writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
            writer.Write(" ");
        }
        var paddedName = string.IsNullOrWhiteSpace(name) ? string.Empty : (nameWidth > 0 ? name.PadRight(nameWidth, ' ') : name);
        if (!string.IsNullOrWhiteSpace(paddedName))
        {
            writer.Write(paddedName);
            writer.Write(" = ");
        }

        writer.Write(value);
        if (appendNull)
        {
            writer.Write(" ");
            writer.WriteStyled("-> null", AnsiStyle.Dim);
        }

        if (appendReplacement)
        {
            writer.Write(" ");
            writer.WriteStyled("# forces replacement", AnsiStyle.Red);
        }

        writer.WriteLine();
    }

    /// <summary>Writes arrow update lines to mirror Terraform change notation.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="name">Attribute name to render.</param>
    /// <param name="before">Value before the change.</param>
    /// <param name="after">Value after the change.</param>
    /// <param name="appendReplacement">Whether to append the replacement comment.</param>
    /// <returns>Nothing.</returns>
    private void WriteArrowLine(AnsiTextWriter writer, string indent, string name, JsonElement before, JsonElement after, bool appendReplacement)
    {
        writer.Write(indent);
        writer.WriteStyled("~", AnsiStyle.Yellow);
        writer.Write(" ");
        writer.Write(name);
        writer.Write(" = ");
        writer.Write(InlineValue(before));
        writer.Write(" ");
        writer.WriteStyled("->", AnsiStyle.Yellow);
        writer.Write(" ");
        writer.Write(InlineValue(after));
        if (appendReplacement)
        {
            writer.Write(" ");
            writer.WriteStyled("# forces replacement", AnsiStyle.Red);
        }

        writer.WriteLine();
    }

    /// <summary>Writes container opening lines for objects and arrays.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="marker">Change marker to prefix the line.</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <param name="name">Attribute or collection name.</param>
    /// <param name="symbol">Container symbol, such as <c>{</c> or <c>[</c>.</param>
    /// <param name="appendReplacement">Whether to append the replacement comment.</param>
    /// <returns>Nothing.</returns>
    private static void WriteContainerOpening(AnsiTextWriter writer, string indent, string marker, AnsiStyle style, string name, string symbol, bool appendReplacement = false, int nameWidth = 0)
    {
        writer.Write(indent);
        if (!string.IsNullOrEmpty(marker))
        {
            writer.WriteStyled(marker, style);
            writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
            writer.Write(" ");
        }
        var paddedName = nameWidth > 0 ? name.PadRight(nameWidth, ' ') : name;
        writer.Write(paddedName);
        writer.Write(" = ");
        writer.Write(symbol);
        if (appendReplacement)
        {
            writer.Write(" ");
            writer.WriteStyled("# forces replacement", AnsiStyle.Red);
        }

        writer.WriteLine();
    }

    /// <summary>Writes closing braces for object containers.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <returns>Nothing.</returns>
    private static void WriteClosingBrace(AnsiTextWriter writer, string indent)
    {
        writer.Write(indent);
        writer.WriteLine("}");
    }

    /// <summary>Writes closing brackets for array containers.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <returns>Nothing.</returns>
    private static void WriteClosingBracket(AnsiTextWriter writer, string indent)
    {
        writer.Write(indent);
        writer.WriteLine("]");
    }

    /// <summary>
    /// Writes the opening of a block-style collection entry (e.g., Terraform's nested blocks).
    /// </summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="marker">Change marker to prefix the line.</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <param name="name">Block name.</param>
    /// <returns>Nothing.</returns>
    private static void WriteBlockOpening(AnsiTextWriter writer, string indent, string marker, AnsiStyle style, string name)
    {
        writer.Write(indent);
        writer.WriteStyled(marker, style);
        writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
        writer.Write(" ");
        writer.Write(name);
        writer.WriteLine(" {");
    }

    /// <summary>Writes Terraform's hidden unchanged attribute comment.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="count">Number of unchanged attributes hidden.</param>
    /// <param name="itemType">Type of items ("attributes", "elements", or "blocks").</param>
    /// <returns>Nothing.</returns>
    private static void WriteUnchangedComment(AnsiTextWriter writer, string indent, int count, string itemType = "attributes")
    {
        writer.Write(indent);
        writer.WriteStyled("#", AnsiStyle.Dim);
        writer.Write(" (");
        writer.Write(count.ToString(CultureInfo.InvariantCulture));
        writer.Write($" unchanged {itemType} hidden)");
        writer.WriteLine();
    }

    /// <summary>Writes Terraform's sensitive attribute placeholder lines.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="name">Optional attribute name for the placeholder.</param>
    /// <returns>Nothing.</returns>
    private static void WriteSensitivePlaceholder(AnsiTextWriter writer, string indent, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            writer.Write(indent);
            writer.WriteStyled("~", AnsiStyle.Yellow);
            writer.Write(" ");
            writer.Write(name);
            writer.WriteLine(" = (sensitive value)");
        }

        writer.Write(indent);
        writer.WriteLineStyled("# At least one attribute in this block is (or was) sensitive,", AnsiStyle.Dim);
        writer.Write(indent);
        writer.WriteLineStyled("# so its contents will not be displayed.", AnsiStyle.Dim);
    }

    /// <summary>
    /// Renders a Terraform-style sensitive block with a marker and placeholder comment.
    /// </summary>
    /// <param name="writer">Destination writer.</param>
    /// <param name="indent">Indentation for the block.</param>
    /// <param name="marker">Marker to display (<c>+</c>, <c>-</c>, or <c>~</c>).</param>
    /// <param name="style">ANSI style for the marker.</param>
    /// <param name="name">Block name.</param>
    /// <returns>Nothing.</returns>
    private static void WriteSensitiveBlock(AnsiTextWriter writer, string indent, string marker, AnsiStyle style, string name)
    {
        writer.Write(indent);
        writer.WriteStyled(marker, style);
        writer.Write(" ");
        writer.Write(name);
        writer.WriteLine(" {");
        WriteSensitivePlaceholder(writer, indent + Indent + Indent, null);
        WriteClosingBrace(writer, indent + Indent);
    }

    /// <summary>
    /// Determines whether an array contains only object elements.
    /// </summary>
    /// <param name="element">Array element to inspect.</param>
    /// <returns><see langword="true"/> when all elements are objects.</returns>
    private static bool ContainsOnlyObjects(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Array && element.EnumerateArray().All(item => item.ValueKind == JsonValueKind.Object);
    }

    /// <summary>
    /// Determines whether a value should render based on emptiness and flags.
    /// </summary>
    /// <param name="value">JSON value to evaluate.</param>
    /// <param name="isUnknown">Indicates whether the value is marked unknown.</param>
    /// <param name="isSensitive">Indicates whether the value is sensitive.</param>
    /// <returns><see langword="true"/> when the value should be rendered.</returns>
    private static bool ShouldRenderValue(JsonElement value, bool isUnknown, bool isSensitive)
    {
        if (isUnknown || isSensitive)
        {
            return true;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String when string.IsNullOrEmpty(value.GetString()) => false,
            JsonValueKind.Null => false,
            JsonValueKind.Object => value.EnumerateObject().Any(),
            JsonValueKind.Array => value.EnumerateArray().Any(),
            _ => true
        };
    }

    /// <summary>
    /// Creates a reusable null element for placeholder rendering.
    /// </summary>
    /// <returns>JSON element representing null.</returns>
    private static JsonElement CreateNullElement()
    {
        using var doc = JsonDocument.Parse("null");
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Retrieves a child element for the specified path, if present.
    /// </summary>
    /// <param name="root">Root element to traverse.</param>
    /// <param name="path">Ordered path segments.</param>
    /// <returns>Child element when found; otherwise null.</returns>
    private static JsonElement? GetChildElement(JsonElement? root, IReadOnlyList<string> path)
    {
        if (root is null)
        {
            return null;
        }

        var current = root.Value;
        foreach (var segment in path)
        {
            if (current.ValueKind == JsonValueKind.Object)
            {
                if (!current.TryGetProperty(segment, out current))
                {
                    return null;
                }
            }
            else if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index < current.GetArrayLength())
            {
                current = current.EnumerateArray().ElementAt(index);
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Enumerates properties from a value, including unknown-only properties.
    /// </summary>
    /// <param name="value">Primary value element.</param>
    /// <param name="unknown">Unknown subtree aligned with the primary element.</param>
    /// <returns>Ordered property entries.</returns>
    private static IEnumerable<(string Name, JsonElement Value)> EnumerateProperties(JsonElement? value, JsonElement? unknown)
    {
        if (value is { ValueKind: JsonValueKind.Object } objectValue)
        {
            foreach (var property in objectValue.EnumerateObject())
            {
                yield return (property.Name, property.Value);
            }
        }

        if (unknown is not { ValueKind: JsonValueKind.Object })
        {
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (value is { ValueKind: JsonValueKind.Object } objectValueSeen)
        {
            foreach (var property in objectValueSeen.EnumerateObject())
            {
                seen.Add(property.Name);
            }
        }

        foreach (var property in unknown.Value.EnumerateObject())
        {
            if (seen.Add(property.Name))
            {
                yield return (property.Name, CreateNullElement());
            }
        }
    }


    /// <summary>
    /// Computes the maximum property name width for alignment.
    /// Only considers scalar properties and primitive arrays that render inline,
    /// excludes nested blocks, object arrays, and null values (unless they're unknown).
    /// </summary>
    /// <param name="properties">Properties to inspect.</param>
    /// <param name="unknown">Unknown subtree for checking if null properties are unknown.</param>
    /// <returns>Maximum name length for alignment, or zero when no properties exist.</returns>
    private static int ComputeNameWidth(IEnumerable<(string Name, JsonElement Value)> properties, JsonElement? unknown)
    {
        var max = 0;
        foreach (var property in properties)
        {
            // Skip null values UNLESS they're unknown (from after_unknown)
            if (property.Value.ValueKind == JsonValueKind.Null)
            {
                // Check if this property is in the unknown map (meaning it's an unknown value, not a JSON null)
                var isUnknown = unknown is { ValueKind: JsonValueKind.Object } &&
                    unknown.Value.TryGetProperty(property.Name, out _);

                if (!isUnknown)
                {
                    continue; // Skip actual JSON nulls
                }
            }

            // Only include scalars and primitive arrays in width calculation
            // Nested blocks and object arrays don't contribute to alignment
            var isInlineProperty = property.Value.ValueKind switch
            {
                JsonValueKind.Array => ContainsOnlyPrimitives(property.Value),
                JsonValueKind.Object => false, // Nested objects render as blocks, not inline
                _ => true // Scalars (including empty strings and unknown nulls) contribute to width
            };

            if (isInlineProperty && property.Name.Length > max)
            {
                max = property.Name.Length;
            }
        }

        // Terraform pads to max_name_length for alignment (the +1 is implicit in the " = " format)
        return max;
    }
}
