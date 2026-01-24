using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>Handles array-level diff rendering. Related feature: docs/features/030-terraform-show-approximation/specification.md.</summary>
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
    private void RenderAddedArrayItem(AnsiTextWriter writer, JsonElement element, string indent, string marker, AnsiStyle style, JsonElement? unknown, JsonElement? sensitive, List<string> path)
    {
        if (IsSensitivePath(sensitive, path))
        {
            WriteSensitivePlaceholder(writer, indent, null);
            return;
        }

        if (IsUnknownPath(unknown, path))
        {
            writer.Write(indent);
            writer.WriteStyled(marker, style);
            writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
            writer.Write(" ");
            writer.Write("(known after apply)");
            writer.WriteLine(",");
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.Write(indent);
                writer.WriteStyled(marker, style);
                writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
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
                writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
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
                writer.Write(indent);
                writer.WriteStyled(marker, style);
                writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                writer.Write(" ");
                writer.Write(_valueRenderer.Render(element));
                writer.WriteLine(",");
                break;
        }
    }

    /// <summary>Renders removed array items while preserving Terraform ordering.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="element">Array element to render.</param>
    /// <param name="indent">Indentation for current depth.</param>
    /// <param name="path">Current array path for lookups.</param>
    private void RenderRemovedArrayItem(AnsiTextWriter writer, JsonElement element, string indent, JsonElement? sensitive, List<string> path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.Write(indent);
                writer.WriteStyled("-", AnsiStyle.Red);
                writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                writer.WriteLine(" {");
                foreach (var property in element.EnumerateObject())
                {
                    var childPath = new List<string>(path) { property.Name };
                    RenderRemovedValue(writer, property.Value, property.Name, indent + Indent, sensitive, childPath);
                }

                WriteClosingBrace(writer, indent + Indent);
                break;
            case JsonValueKind.Array:
                writer.Write(indent);
                writer.WriteStyled("-", AnsiStyle.Red);
                writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
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
                writer.Write(indent);
                writer.WriteStyled("-", AnsiStyle.Red);
                writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                writer.Write(" ");
                writer.Write(_valueRenderer.Render(element));
                writer.WriteLine(",");
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
            writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
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
                RenderRemovedValue(writer, beforeDict[removedName], removedName, indent + Indent + Indent, sensitive, childPath);
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
    private void RenderAddedObjectBlock(AnsiTextWriter writer, JsonElement element, string name, string indent, string marker, AnsiStyle style, JsonElement? unknown, JsonElement? sensitive, List<string> path)
    {
        if (!ShouldRenderValue(element, isUnknown: false, isSensitive: IsSensitivePath(sensitive, path)))
        {
            return;
        }

        var childUnknown = GetChildElement(unknown, path);
        var childProperties = EnumerateProperties(element, childUnknown).ToList();
        var sortedChildProperties = SortPropertiesByType(childProperties);
        var childWidth = ComputeNameWidth(sortedChildProperties, childUnknown);
        WriteBlockOpening(writer, indent, marker, style, name);
        foreach (var property in sortedChildProperties)
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
    private void RenderRemovedObjectBlock(AnsiTextWriter writer, JsonElement element, string name, string indent, JsonElement? sensitive, List<string> path)
    {
        if (!ShouldRenderValue(element, isUnknown: false, isSensitive: false))
        {
            return;
        }

        WriteBlockOpening(writer, indent, "-", AnsiStyle.Red, name);
        var childProperties = element.EnumerateObject().Select(p => (p.Name, p.Value)).ToList();
        var sortedChildProperties = SortPropertiesByType(childProperties);
        var childWidth = ComputeNameWidth(sortedChildProperties, unknown: null);
        var hiddenCount = 0;

        foreach (var property in sortedChildProperties)
        {
            var childPath = new List<string>(path) { property.Name };
            var isSensitive = IsSensitivePath(sensitive, childPath);

            if (!ShouldRenderValue(property.Value, isUnknown: false, isSensitive: isSensitive))
            {
                // Only count non-empty strings and non-null scalars as hidden unchanged attributes
                // Empty arrays and empty objects are not counted
                if (property.Value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(property.Value.GetString()))
                {
                    hiddenCount++;
                }

                continue;
            }

            RenderRemovedValue(writer, property.Value, property.Name, indent + Indent + Indent, sensitive, childPath, childWidth);
        }

        if (hiddenCount > 0)
        {
            WriteUnchangedComment(writer, indent + Indent + Indent + Indent, hiddenCount, "attributes");
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
                RenderRemovedObjectBlock(writer, element, name, indent, sensitive, childPath);
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
                RenderAddedObjectBlock(writer, element, name, indent, marker, style, unknown, sensitive, childPath);
                index++;
            }
        }
    }
}
