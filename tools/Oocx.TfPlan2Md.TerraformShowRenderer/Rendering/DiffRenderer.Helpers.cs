using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>Handles object-level diff rendering. Related feature: docs/features/030-terraform-show-approximation/specification.md</summary>
internal sealed partial class DiffRenderer
{
    /// <summary>Renders added values to mirror Terraform create/read formatting.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="value">Added JSON value.</param>
    /// <param name="name">Attribute name being rendered.</param>
    /// <param name="indent">Indentation for current depth.</param>
    /// <param name="marker">Change marker to prefix lines.</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <param name="unknown">Unknown value map from <c>after_unknown</c>.</param>
    /// <param name="sensitive">Sensitive value map from <c>after_sensitive</c>.</param>
    /// <param name="path">Current attribute path for lookups.</param>
    /// <returns>Nothing.</returns>
    private void RenderAddedValue(AnsiTextWriter writer, JsonElement value, string name, string indent, string marker, AnsiStyle style, JsonElement? unknown, JsonElement? sensitive, List<string> path, int nameWidth)
    {
        var isUnknown = IsUnknownPath(unknown, path);
        var isSensitive = IsSensitivePath(sensitive, path);

        if (!ShouldRenderValue(value, isUnknown, isSensitive))
        {
            return;
        }

        if (isSensitive)
        {
            if (value.ValueKind == JsonValueKind.Array && ContainsOnlyObjects(value))
            {
                var index = 0;
                foreach (var _ in value.EnumerateArray())
                {
                    WriteSensitiveBlock(writer, indent, marker, style, name);
                    index++;
                }

                return;
            }

            if (value.ValueKind == JsonValueKind.Object)
            {
                WriteSensitiveBlock(writer, indent, marker, style, name);
                return;
            }

            WriteSensitivePlaceholder(writer, indent, name);
            return;
        }

        if (isUnknown)
        {
            WriteScalarLine(writer, indent, marker, style, name, "(known after apply)", nameWidth: nameWidth);
            return;
        }

        if (value.ValueKind == JsonValueKind.Array && ContainsOnlyObjects(value))
        {
            var index = 0;
            foreach (var element in value.EnumerateArray())
            {
                var childPath = new List<string>(path) { index.ToString(CultureInfo.InvariantCulture) };
                RenderAddedObjectBlock(writer, element, name, indent, marker, style, unknown, sensitive, childPath);
                index++;
            }

            return;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                var childUnknown = GetChildElement(unknown, path);
                var childProperties = EnumerateProperties(value, childUnknown).ToList();
                // Sort nested object properties by type (scalars, arrays, objects), then alphabetically
                var sortedChildProperties = SortPropertiesByType(childProperties);
                var childWidth = ComputeNameWidth(sortedChildProperties, childUnknown);
                var isMap = IsMap(value);
                if (isMap && childWidth > 0)
                {
                    childWidth += 2; // Add 2 for quotes around map keys
                }
                WriteContainerOpening(writer, indent, marker, style, name, "{", false, nameWidth);
                foreach (var property in sortedChildProperties)
                {
                    var childPath = new List<string>(path) { property.Name };
                    var propName = isMap ? $"\"{property.Name}\"" : property.Name;
                    RenderAddedValue(writer, property.Value, propName, indent + Indent + Indent, marker, style, unknown, sensitive, childPath, childWidth);
                }

                WriteClosingBrace(writer, indent + Indent);
                break;
            case JsonValueKind.Array:
                WriteContainerOpening(writer, indent, marker, style, name, "[", false, nameWidth);
                var index = 0;
                foreach (var element in value.EnumerateArray())
                {
                    var childPath = new List<string>(path) { index.ToString(CultureInfo.InvariantCulture) };
                    RenderAddedArrayItem(writer, element, indent + Indent + Indent, marker, style, unknown, sensitive, childPath);
                    index++;
                }

                WriteClosingBracket(writer, indent + Indent);
                break;
            default:
                WriteScalarLine(writer, indent, marker, style, name, _valueRenderer.Render(value), nameWidth: nameWidth);
                break;
        }
    }

    /// <summary>Renders removed values to mirror Terraform destroy formatting.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="value">Removed JSON value.</param>
    /// <param name="name">Attribute name being rendered.</param>
    /// <param name="indent">Indentation for current depth.</param>
    /// <param name="path">Current attribute path for lookups.</param>
    /// <param name="nameWidth">Width for name padding to align equals signs.</param>
    /// <returns>Nothing.</returns>
    private void RenderRemovedValue(AnsiTextWriter writer, JsonElement value, string name, string indent, JsonElement? sensitive, List<string> path, int nameWidth = 0)
    {
        var isSensitive = IsSensitivePath(sensitive, path);

        if (isSensitive)
        {
            if (value.ValueKind == JsonValueKind.Array && ContainsOnlyObjects(value))
            {
                var index = 0;
                foreach (var _ in value.EnumerateArray())
                {
                    WriteSensitiveBlock(writer, indent, "-", AnsiStyle.Red, name);
                    index++;
                }

                return;
            }

            if (value.ValueKind == JsonValueKind.Object)
            {
                WriteSensitiveBlock(writer, indent, "-", AnsiStyle.Red, name);
                return;
            }

            WriteSensitivePlaceholder(writer, indent, name);
            return;
        }

        if (!ShouldRenderValue(value, isUnknown: false, isSensitive: false))
        {
            return;
        }

        if (value.ValueKind == JsonValueKind.Array && ContainsOnlyObjects(value))
        {
            var index = 0;
            foreach (var element in value.EnumerateArray())
            {
                var childPath = new List<string>(path) { index.ToString(CultureInfo.InvariantCulture) };
                RenderRemovedObjectBlock(writer, element, name, indent, sensitive, childPath);
                index++;
            }

            return;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                WriteContainerOpening(writer, indent, "-", AnsiStyle.Red, name, "{");
                var removedProperties = value.EnumerateObject().Select(p => (p.Name, Value: p.Value)).ToList();
                var sortedRemovedProperties = SortPropertiesByType(removedProperties);
                var removedWidth = ComputeNameWidth(sortedRemovedProperties, unknown: null);
                foreach (var (propName, propValue) in sortedRemovedProperties)
                {
                    var childPath = new List<string>(path) { propName };
                    RenderRemovedValue(writer, propValue, propName, indent + Indent + Indent, sensitive, childPath, removedWidth);
                }

                WriteClosingBrace(writer, indent + Indent);
                break;
            case JsonValueKind.Array:
                // For arrays of primitives, use multi-line format with " -> null" suffix at closing bracket
                if (!ContainsOnlyObjects(value))
                {
                    WriteContainerOpening(writer, indent, "-", AnsiStyle.Red, name, "[", false, nameWidth);
                    foreach (var element in value.EnumerateArray())
                    {
                        writer.Write(indent + Indent + Indent);
                        writer.WriteStyled("-", AnsiStyle.Red);
                        writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                        writer.Write(" ");
                        writer.Write(_valueRenderer.Render(element));
                        writer.WriteLine(",");
                    }

                    writer.Write(indent + Indent);
                    writer.Write("] ");
                    writer.WriteStyled("-> null", AnsiStyle.Dim);
                    writer.WriteReset(); // Extra reset after styled content at line end
                    writer.WriteLine();
                }
                else
                {
                    WriteContainerOpening(writer, indent, "-", AnsiStyle.Red, name, "[");
                    var index = 0;
                    foreach (var element in value.EnumerateArray())
                    {
                        var childPath = new List<string>(path) { index.ToString(CultureInfo.InvariantCulture) };
                        RenderRemovedArrayItem(writer, element, indent + Indent, sensitive, childPath);
                        index++;
                    }

                    WriteClosingBracket(writer, indent + Indent);
                }
                break;
            default:
                WriteScalarLine(writer, indent, "-", AnsiStyle.Red, name, _valueRenderer.Render(value), appendNull: true, nameWidth: nameWidth);
                break;
        }
    }

    /// <summary>Renders updated values to mirror Terraform in-place and replacement formatting.</summary>
    /// <param name="writer">Target writer for diff output.</param>
    /// <param name="before">Value before the change.</param>
    /// <param name="after">Value after the change.</param>
    /// <param name="name">Attribute name being rendered.</param>
    /// <param name="indent">Indentation for current depth.</param>
    /// <param name="path">Current attribute path for lookups.</param>
    /// <param name="unknown">Unknown value map from <c>after_unknown</c>.</param>
    /// <param name="sensitive">Sensitive value map from <c>after_sensitive</c>.</param>
    /// <param name="replacePaths">Paths that force replacement.</param>
    /// <param name="nameWidth">Width for aligning attribute names.</param>
    /// <returns>Nothing.</returns>
    private void RenderUpdatedValue(AnsiTextWriter writer, JsonElement before, JsonElement after, string name, string indent, List<string> path, JsonElement? unknown, JsonElement? sensitive, HashSet<string> replacePaths, int nameWidth = 0)
    {
        var replacement = replacePaths.Contains(FormatPath(path));
        var isSensitive = IsSensitivePath(sensitive, path);
        var isUnknown = IsUnknownPath(unknown, path);

        if (!ShouldRenderValue(after, isUnknown, isSensitive) && !ShouldRenderValue(before, isUnknown, isSensitive))
        {
            return;
        }

        if (isSensitive)
        {
            if (after.ValueKind == JsonValueKind.Array && ContainsOnlyObjects(after))
            {
                var index = 0;
                foreach (var _ in after.EnumerateArray())
                {
                    WriteSensitiveBlock(writer, indent, "~", AnsiStyle.Yellow, name);
                    index++;
                }

                return;
            }

            if (after.ValueKind == JsonValueKind.Object)
            {
                WriteSensitiveBlock(writer, indent, "~", AnsiStyle.Yellow, name);
                return;
            }

            WriteSensitivePlaceholder(writer, indent, name);
            return;
        }

        if (isUnknown)
        {
            WriteScalarLine(writer, indent, "~", AnsiStyle.Yellow, name, "(known after apply)", false, replacement);
            return;
        }

        if (after.ValueKind == JsonValueKind.Array && ContainsOnlyObjects(after))
        {
            RenderUpdatedObjectArray(writer, before, after, name, indent, path, unknown, sensitive, replacePaths);
            return;
        }

        if (before.ValueKind == JsonValueKind.Object && after.ValueKind == JsonValueKind.Object)
        {
            var isMap = IsMap(after);
            WriteContainerOpening(writer, indent, "~", AnsiStyle.Yellow, name, "{", replacement, nameWidth);
            var childUnknown = GetChildElement(unknown, path);
            var beforeDict = before.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            var afterProps = EnumerateProperties(after, childUnknown).ToList();
            var sortedAfterProps = SortPropertiesByType(afterProps);

            // Compute width for child properties
            var childWidth = ComputeNameWidth(sortedAfterProps, childUnknown);
            if (isMap && childWidth > 0)
            {
                // For maps: ComputeNameWidth returns max+implicit_space, add 2 for quotes
                childWidth += 2;
            }

            var unchanged = 0;
            foreach (var prop in sortedAfterProps)
            {
                var childPath = new List<string>(path) { prop.Name };
                if (beforeDict.TryGetValue(prop.Name, out var beforeChild))
                {
                    if (AreEqual(beforeChild, prop.Value))
                    {
                        unchanged++;
                    }
                    else
                    {
                        var propName = isMap ? $"\"{prop.Name}\"" : prop.Name;
                        RenderUpdatedValue(writer, beforeChild, prop.Value, propName, indent + Indent + Indent, childPath, unknown, sensitive, replacePaths, childWidth);
                    }
                }
                else
                {
                    var propName = isMap ? $"\"{prop.Name}\"" : prop.Name;
                    RenderAddedValue(writer, prop.Value, propName, indent + Indent + Indent, "+", AnsiStyle.Green, unknown, sensitive, childPath, childWidth);
                }
            }

            foreach (var removedName in beforeDict.Keys.Except(afterProps.Select(p => p.Name)))
            {
                var childPath = new List<string>(path) { removedName };
                var propName = isMap ? $"\"{removedName}\"" : removedName;
                RenderRemovedValue(writer, beforeDict[removedName], propName, indent + Indent + Indent, sensitive, childPath, childWidth);
            }

            if (unchanged > 0)
            {
                var itemType = isMap ? "elements" : "attributes";
                // Comment should be at content level (indent + Indent + Indent) plus marker offset (+ Indent)
                WriteUnchangedComment(writer, indent + Indent + Indent + Indent, unchanged, itemType);
            }

            WriteClosingBrace(writer, indent + Indent);
            return;
        }

        if (before.ValueKind == JsonValueKind.Array && after.ValueKind == JsonValueKind.Array)
        {
            WriteContainerOpening(writer, indent, "~", AnsiStyle.Yellow, name, "[", replacement);
            var beforeItems = before.EnumerateArray().ToList();
            var afterItems = after.EnumerateArray().ToList();
            var max = Math.Max(beforeItems.Count, afterItems.Count);
            for (var i = 0; i < max; i++)
            {
                var childPath = new List<string>(path) { i.ToString(CultureInfo.InvariantCulture) };
                if (i < beforeItems.Count && i < afterItems.Count)
                {
                    if (!AreEqual(beforeItems[i], afterItems[i]))
                    {
                        RenderUpdatedArrayItem(writer, beforeItems[i], afterItems[i], indent + Indent + Indent, childPath, unknown, sensitive, replacePaths);
                    }
                }
                else if (i < afterItems.Count)
                {
                    RenderAddedArrayItem(writer, afterItems[i], indent + Indent + Indent, "+", AnsiStyle.Green, unknown, sensitive, childPath);
                }
                else
                {
                    RenderRemovedArrayItem(writer, beforeItems[i], indent + Indent + Indent, sensitive, childPath);
                }
            }

            WriteClosingBracket(writer, indent + Indent);
            return;
        }

        WriteArrowLine(writer, indent, name, before, after, replacement, nameWidth);
    }
}
