using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>Handles array-level diff rendering. Related feature: docs/features/030-terraform-show-approximation/specification.md</summary>
internal sealed partial class DiffRenderer
{
    /// <summary>Renders added array items while preserving Terraform ordering.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="element">Array element to render.</param>
    /// <param name="indent">Indentation for current depth.</param>
    /// <param name="marker">Change marker to prefix lines.</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <param name="unknown">Unknown value map from <c>after_unknown</c>.</param>
    /// <param name="sensitive">Sensitive value map from <c>after_sensitive</c>.</param>
    /// <param name="path">Current array path for lookups.</param>
    /// <returns>Nothing.</returns>
    private void RenderAddedArrayItem(AnsiTextWriter writer, JsonElement element, string indent, string marker, AnsiStyle style, JsonElement? unknown, JsonElement? sensitive, List<string> path)
    {
        if (IsSensitivePath(sensitive, path))
        {
            // Scalar items need extra Indent to match Terraform output formatting
            WriteSensitivePlaceholder(writer, indent + Indent, null);
            return;
        }

        if (IsUnknownPath(unknown, path))
        {
            // Scalar items need extra Indent to match Terraform output formatting
            WriteScalarLine(writer, indent + Indent, marker, style, string.Empty, "(known after apply)");
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.Write(indent);
                writer.WriteStyled(marker, style);
                writer.WriteLine(" {");
                foreach (var property in element.EnumerateObject())
                {
                    var childPath = new List<string>(path) { property.Name };
                    RenderAddedValue(writer, property.Value, property.Name, indent + Indent + Indent, marker, style, unknown, sensitive, childPath, 0);
                }

                WriteClosingBrace(writer, indent + Indent);
                break;
            case JsonValueKind.Array:
                writer.Write(indent);
                writer.WriteStyled(marker, style);
                writer.WriteLine(" [");
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var childPath = new List<string>(path) { index.ToString(CultureInfo.InvariantCulture) };
                    RenderAddedArrayItem(writer, item, indent + Indent, marker, style, unknown, sensitive, childPath);
                    index++;
                }

                WriteClosingBracket(writer, indent + Indent);
                break;
            default:
                // Scalar items need extra Indent to match Terraform output formatting and trailing comma
                WriteAddedArrayScalar(writer, indent + Indent, marker, style, element);
                break;
        }
    }

    /// <summary>Renders removed array items while preserving Terraform ordering.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="element">Array element to render.</param>
    /// <param name="indent">Indentation for current depth.</param>
    /// <param name="path">Current array path for lookups.</param>
    /// <returns>Nothing.</returns>
    private void RenderRemovedArrayItem(AnsiTextWriter writer, JsonElement element, string indent, JsonElement? sensitive, List<string> path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.Write(indent);
                writer.WriteStyled("-", AnsiStyle.Red);
                writer.WriteLine(" {");
                foreach (var property in element.EnumerateObject())
                {
                    var childPath = new List<string>(path) { property.Name };
                    RenderRemovedValue(writer, property.Value, property.Name, indent + Indent, sensitive, childPath, 0);
                }

                WriteClosingBrace(writer, indent + Indent);
                break;
            case JsonValueKind.Array:
                writer.Write(indent);
                writer.WriteStyled("-", AnsiStyle.Red);
                writer.WriteLine(" [");
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var childPath = new List<string>(path) { index.ToString(CultureInfo.InvariantCulture) };
                    RenderRemovedArrayItem(writer, item, indent + Indent, sensitive, childPath);
                    index++;
                }

                WriteClosingBracket(writer, indent + Indent);
                break;
            default:
                // Array items use comma suffix instead of -> null
                // Scalar items need extra Indent to match Terraform output formatting
                WriteRemovedArrayScalar(writer, indent + Indent, element);
                break;
        }
    }

    /// <summary>Renders updated array items to mirror Terraform in-place and replacement formatting.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="before">Array element before the change.</param>
    /// <param name="after">Array element after the change.</param>
    /// <param name="indent">Indentation for current depth.</param>
    /// <param name="path">Current array path for lookups.</param>
    /// <param name="unknown">Unknown value map from <c>after_unknown</c>.</param>
    /// <param name="sensitive">Sensitive value map from <c>after_sensitive</c>.</param>
    /// <param name="replacePaths">Paths that force replacement.</param>
    /// <returns>Nothing.</returns>
    private void RenderUpdatedArrayItem(AnsiTextWriter writer, JsonElement before, JsonElement after, string indent, List<string> path, JsonElement? unknown, JsonElement? sensitive, HashSet<string> replacePaths)
    {
        var replacement = replacePaths.Contains(FormatPath(path));
        if (IsSensitivePath(sensitive, path))
        {
            WriteSensitivePlaceholder(writer, indent, null);
            return;
        }

        if (IsUnknownPath(unknown, path))
        {
            WriteScalarLine(writer, indent, "~", AnsiStyle.Yellow, string.Empty, "(known after apply)", false, replacement);
            return;
        }

        if (before.ValueKind == JsonValueKind.Object && after.ValueKind == JsonValueKind.Object)
        {
            writer.Write(indent);
            writer.WriteStyled("~", AnsiStyle.Yellow);
            writer.WriteLine(" {");
            var beforeDict = before.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            var afterProps = after.EnumerateObject().ToList();
            foreach (var prop in afterProps)
            {
                var childPath = new List<string>(path) { prop.Name };
                if (beforeDict.TryGetValue(prop.Name, out var beforeChild))
                {
                    if (!AreEqual(beforeChild, prop.Value))
                    {
                        RenderUpdatedValue(writer, beforeChild, prop.Value, prop.Name, indent + Indent + Indent, childPath, unknown, sensitive, replacePaths);
                    }
                }
                else
                {
                    RenderAddedValue(writer, prop.Value, prop.Name, indent + Indent + Indent, "+", AnsiStyle.Green, unknown, sensitive, childPath, 0);
                }
            }

            foreach (var removedName in beforeDict.Keys.Except(afterProps.Select(p => p.Name)))
            {
                var childPath = new List<string>(path) { removedName };
                RenderRemovedValue(writer, beforeDict[removedName], removedName, indent + Indent + Indent, sensitive, childPath, 0);
            }

            WriteClosingBrace(writer, indent + Indent);
            return;
        }

        WriteArrowLine(writer, indent, string.Empty, before, after, replacement);
    }

    /// <summary>Renders added object blocks for array elements.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="element">Object element to render.</param>
    /// <param name="name">Block name.</param>
    /// <param name="indent">Current indentation.</param>
    /// <param name="marker">Marker used for the block.</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <param name="unknown">Unknown value map.</param>
    /// <param name="sensitive">Sensitive value map.</param>
    /// <param name="path">Path for unknown/sensitive lookups.</param>
    private void RenderAddedObjectBlock(AnsiTextWriter writer, JsonElement element, string name, string indent, string marker, AnsiStyle style, JsonElement? unknown, JsonElement? sensitive, List<string> path, int nameWidth)
    {
        if (!ShouldRenderValue(element, isUnknown: false, isSensitive: IsSensitivePath(sensitive, path)))
        {
            return;
        }

        var childUnknown = GetChildElement(unknown, path);
        var allChildProperties = EnumerateProperties(element, childUnknown).ToList();

        // Filter properties that will actually be rendered (for nested blocks, only renderable properties affect width)
        var renderableProperties = new List<(string Name, JsonElement Value)>();
        foreach (var property in allChildProperties)
        {
            var childPath = new List<string>(path) { property.Name };
            var isUnknown = IsUnknownPath(unknown, childPath);
            var isSensitive = IsSensitivePath(sensitive, childPath);
            if (ShouldRenderValue(property.Value, isUnknown, isSensitive))
            {
                renderableProperties.Add(property);
            }
        }

        // Sort properties: scalars first (alphabetically), then blocks (alphabetically)
        var sortedProperties = SortPropertiesForOutput(renderableProperties);

        // Compute width from renderable properties only
        var childWidth = ComputeNameWidth(sortedProperties);

        WriteBlockOpening(writer, indent, marker, style, name, nameWidth);
        foreach (var property in sortedProperties)
        {
            var childPath = new List<string>(path) { property.Name };
            RenderAddedValue(writer, property.Value, property.Name, indent + Indent + Indent, marker, style, unknown, sensitive, childPath, childWidth);
        }

        WriteClosingBrace(writer, indent + Indent);
    }

    /// <summary>Renders removed object blocks for array elements.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="element">Object element to render.</param>
    /// <param name="name">Block name.</param>
    /// <param name="indent">Current indentation.</param>
    /// <param name="path">Path for lookups.</param>
    /// <param name="nameWidth">Width for name padding to align block names.</param>
    private void RenderRemovedObjectBlock(AnsiTextWriter writer, JsonElement element, string name, string indent, JsonElement? sensitive, List<string> path, int nameWidth = 0)
    {
        if (!ShouldRenderValue(element, isUnknown: false, isSensitive: false))
        {
            return;
        }

        var properties = element.EnumerateObject().Select(p => (p.Name, p.Value)).ToList();

        // Compute width from ALL properties (Terraform includes hidden properties in width calculation)
        var childWidth = ComputeNameWidth(properties);

        // Sort properties: scalars first (alphabetically), then blocks (alphabetically)
        var sortedProperties = SortPropertiesForOutput(properties);

        WriteBlockOpening(writer, indent, "-", AnsiStyle.Red, name, nameWidth);
        var renderedCount = 0;
        var hiddenButCountedCount = 0;
        foreach (var property in sortedProperties)
        {
            var childPath = new List<string>(path) { property.Name };
            var isSensitive = IsSensitivePath(sensitive, childPath);
            // Render if sensitive or if the value should be rendered
            if (isSensitive || ShouldRenderValue(property.Value, false, false))
            {
                RenderRemovedValue(writer, property.Value, property.Name, indent + Indent + Indent, sensitive, childPath, childWidth);
                renderedCount++;
            }
            else
            {
                // Count hidden attributes, but only if they are not empty arrays (empty arrays are completely ignored)
                if (property.Value.ValueKind != JsonValueKind.Array || property.Value.GetArrayLength() > 0)
                {
                    hiddenButCountedCount++;
                }
            }
        }

        if (hiddenButCountedCount > 0)
        {
            // Align comment with property names (add 2 spaces for "- " marker)
            WriteUnchangedComment(writer, indent + Indent + Indent + "  ", hiddenButCountedCount);
        }

        WriteClosingBrace(writer, indent + Indent);
    }

    /// <summary>Renders updates for arrays containing objects by removing and adding elements.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="before">Array before the change.</param>
    /// <param name="after">Array after the change.</param>
    /// <param name="name">Block name.</param>
    /// <param name="indent">Current indentation.</param>
    /// <param name="path">Path for lookups.</param>
    /// <param name="unknown">Unknown value map.</param>
    /// <param name="sensitive">Sensitive value map.</param>
    /// <param name="replacePaths">Replacement path set.</param>
    private void RenderUpdatedObjectArray(AnsiTextWriter writer, JsonElement before, JsonElement after, string name, string indent, List<string> path, JsonElement? unknown, JsonElement? sensitive, HashSet<string> replacePaths)
    {
        if (before.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var element in before.EnumerateArray())
            {
                var childPath = new List<string>(path) { index.ToString(CultureInfo.InvariantCulture) };
                RenderRemovedObjectBlock(writer, element, name, indent, sensitive, childPath, 0);
                index++;
            }
        }

        if (after.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var element in after.EnumerateArray())
            {
                var childPath = new List<string>(path) { index.ToString(CultureInfo.InvariantCulture) };
                var marker = replacePaths.Contains(FormatPath(childPath)) ? "-" : "+";
                var style = marker == "+" ? AnsiStyle.Green : AnsiStyle.Red;
                RenderAddedObjectBlock(writer, element, name, indent, marker, style, unknown, sensitive, childPath, 0);
                index++;
            }
        }
    }
}
