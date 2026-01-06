using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>Utility helpers for diff rendering. Related feature: docs/features/030-terraform-show-approximation/specification.md</summary>
internal sealed partial class DiffRenderer
{
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
        writer.WriteStyled(marker, style);
        writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
        writer.Write(" ");
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

    /// <summary>
    /// Writes a removed array scalar item with comma suffix (no -> null).
    /// Terraform uses commas for array items, not -> null annotation.
    /// </summary>
    /// <param name="writer">Target writer for output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="value">Scalar value to render.</param>
    private void WriteRemovedArrayScalar(AnsiTextWriter writer, string indent, JsonElement value)
    {
        writer.Write(indent);
        writer.WriteStyled("-", AnsiStyle.Red);
        writer.WriteReset();
        writer.Write(" ");
        writer.Write(_valueRenderer.Render(value));
        writer.WriteLine(",");
    }

    /// <summary>
    /// Writes an added array scalar item with comma suffix.
    /// Terraform uses commas for array items in creation/addition.
    /// </summary>
    /// <param name="writer">Target writer for output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="marker">Change marker to prefix the line.</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <param name="value">Scalar value to render.</param>
    private void WriteAddedArrayScalar(AnsiTextWriter writer, string indent, string marker, AnsiStyle style, JsonElement value)
    {
        writer.Write(indent);
        writer.WriteStyled(marker, style);
        writer.WriteReset();
        writer.Write(" ");
        writer.Write(_valueRenderer.Render(value));
        writer.WriteLine(",");
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
        writer.WriteStyled(marker, style);
        writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
        writer.Write(" ");
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
    /// <param name="appendNull">Whether to append -> null annotation.</param>
    /// <returns>Nothing.</returns>
    private static void WriteClosingBracket(AnsiTextWriter writer, string indent, bool appendNull = false)
    {
        writer.Write(indent);
        writer.Write("]");
        if (appendNull)
        {
            writer.Write(" ");
            writer.WriteStyled("-> null", AnsiStyle.Dim);
        }

        writer.WriteLine();
    }

    /// <summary>
    /// Writes the opening of a block-style collection entry (e.g., Terraform's nested blocks).
    /// Block names are never padded - they use a single space before the opening brace.
    /// </summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="indent">Indentation for the current depth.</param>
    /// <param name="marker">Change marker to prefix the line.</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <param name="name">Block name.</param>
    /// <param name="nameWidth">Unused - blocks don't align names.</param>
    /// <returns>Nothing.</returns>
    private static void WriteBlockOpening(AnsiTextWriter writer, string indent, string marker, AnsiStyle style, string name, int nameWidth = 0)
    {
        _ = nameWidth; // Unused - blocks don't align names
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
    /// <returns>Nothing.</returns>
    private static void WriteUnchangedComment(AnsiTextWriter writer, string indent, int count)
    {
        writer.Write(indent);
        writer.WriteStyled("#", AnsiStyle.Dim);
        writer.Write(" (");
        writer.Write(count.ToString(CultureInfo.InvariantCulture));
        writer.Write(" unchanged attributes hidden)");
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
    /// Determines whether a property represents a block (object or array of objects).
    /// Blocks are rendered after scalar attributes in Terraform output.
    /// </summary>
    /// <param name="value">JSON value to evaluate.</param>
    /// <returns><see langword="true"/> when the value is an object or array of objects.</returns>
    private static bool IsBlock(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Object || ContainsOnlyObjects(value);
    }

    /// <summary>
    /// Sorts properties for Terraform output: scalars first (in order), then blocks (in order).
    /// Preserves the original property order within each group (scalars vs blocks).
    /// </summary>
    /// <param name="properties">Properties to sort.</param>
    /// <returns>Sorted properties with scalars before blocks.</returns>
    private static List<(string Name, JsonElement Value)> SortPropertiesForOutput(IEnumerable<(string Name, JsonElement Value)> properties)
    {
        return properties
            .OrderBy(p => IsBlock(p.Value) ? 1 : 0) // Scalars first (0), then blocks (1)
            .ToList();
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
    /// </summary>
    /// <param name="properties">Properties to inspect.</param>
    /// <returns>Maximum name length or zero when no properties exist.</returns>
    private static int ComputeNameWidth(IEnumerable<(string Name, JsonElement Value)> properties)
    {
        var max = 0;
        foreach (var property in properties)
        {
            // Only count scalar properties for alignment, not blocks
            if (!IsBlock(property.Value) && property.Name.Length > max)
            {
                max = property.Name.Length;
            }
        }

        return max;
    }
}
