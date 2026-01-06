using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>Renders before/after differences in a Terraform show-like format. Related feature: docs/features/030-terraform-show-approximation/specification.md</summary>
internal sealed partial class DiffRenderer
{
    /// <summary>Standard Terraform indentation spacing.</summary>
    private const string Indent = "  ";

    /// <summary>Formats scalar JSON values consistently with Terraform output.</summary>
    private readonly ValueRenderer _valueRenderer = new();

    /// <summary>Renders attribute differences for a resource change.</summary>
    /// <param name="writer">Target writer that applies ANSI styling.</param>
    /// <param name="change">Resource change data from the plan.</param>
    /// <param name="action">Effective resource action.</param>
    /// <param name="baseIndent">Indentation used for the resource block.</param>
    /// <returns>Nothing.</returns>
    public void RenderAttributes(AnsiTextWriter writer, ResourceChange change, ResourceAction action, string baseIndent)
    {
        var indent = baseIndent + Indent + Indent;
        var before = ToElement(change.Change.Before);
        var after = ToElement(change.Change.After);
        var afterUnknown = ToElement(change.Change.AfterUnknown);
        var afterSensitive = ToElement(change.Change.AfterSensitive);
        var beforeSensitive = ToElement(change.Change.BeforeSensitive);
        var replacePaths = CollectReplacePaths(change.Change.ReplacePaths);

        switch (action)
        {
            case ResourceAction.Create:
            case ResourceAction.Read:
                RenderAdd(writer, after, afterUnknown, afterSensitive, indent, action == ResourceAction.Read ? "+" : "+", AnsiStyle.Green);
                break;
            case ResourceAction.Delete:
                RenderRemove(writer, before, indent, beforeSensitive);
                break;
            case ResourceAction.Update:
            case ResourceAction.Replace:
                var effectiveSensitive = afterSensitive ?? beforeSensitive;
                RenderUpdate(writer, before, after, afterUnknown, effectiveSensitive, indent, replacePaths);
                break;
        }
    }

    /// <summary>Renders additions for create or read operations.</summary>
    /// <param name="writer">Target writer that applies ANSI styling.</param>
    /// <param name="after">State after the change.</param>
    /// <param name="unknown">Unknown value map from the plan.</param>
    /// <param name="sensitive">Sensitive value map from the plan.</param>
    /// <param name="indent">Indentation to use for nested attributes.</param>
    /// <param name="marker">Change marker to display (e.g., <c>+</c> or <c>&lt;=</c>).</param>
    /// <param name="style">ANSI style associated with the marker.</param>
    /// <returns>Nothing.</returns>
    private void RenderAdd(AnsiTextWriter writer, JsonElement? after, JsonElement? unknown, JsonElement? sensitive, string indent, string marker, AnsiStyle style)
    {
        if (after is not { ValueKind: JsonValueKind.Object })
        {
            return;
        }

        var properties = EnumerateProperties(after.Value, unknown).ToList();
        // Sort properties by type (scalars first, then nested blocks), then alphabetically
        var sorted = SortPropertiesByType(properties);
        var width = ComputeNameWidth(sorted);

        foreach (var (name, value) in sorted)
        {
            RenderAddedValue(writer, value, name, indent, marker, style, unknown, sensitive, new List<string> { name }, width);
        }
    }

    /// <summary>Renders deletions for destroy operations.</summary>
    /// <param name="writer">Target writer that applies ANSI styling.</param>
    /// <param name="before">State before the change.</param>
    /// <param name="indent">Indentation to use for nested attributes.</param>
    /// <returns>Nothing.</returns>
    private void RenderRemove(AnsiTextWriter writer, JsonElement? before, string indent, JsonElement? sensitive)
    {
        if (before is not { ValueKind: JsonValueKind.Object })
        {
            return;
        }

        var properties = before.Value.EnumerateObject().Select(p => (p.Name, Value: p.Value)).ToList();
        // Sort properties by type (scalars first, then arrays, then nested blocks), then alphabetically
        var sorted = SortPropertiesByType(properties);
        var width = ComputeNameWidth(sorted);

        foreach (var (name, value) in sorted)
        {
            RenderRemovedValue(writer, value, name, indent, sensitive, new List<string> { name }, width);
        }
    }

    /// <summary>Renders updates including replacement scenarios.</summary>
    /// <param name="writer">Target writer that applies ANSI styling.</param>
    /// <param name="before">State before the change.</param>
    /// <param name="after">State after the change.</param>
    /// <param name="unknown">Unknown value map from the plan.</param>
    /// <param name="sensitive">Sensitive value map from the plan.</param>
    /// <param name="indent">Indentation to use for nested attributes.</param>
    /// <param name="replacePaths">Paths that force replacement.</param>
    /// <returns>Nothing.</returns>
    private void RenderUpdate(AnsiTextWriter writer, JsonElement? before, JsonElement? after, JsonElement? unknown, JsonElement? sensitive, string indent, HashSet<string> replacePaths)
    {
        var beforeObj = before is { ValueKind: JsonValueKind.Object } ? before : null;
        var afterObj = after is { ValueKind: JsonValueKind.Object } ? after : null;
        if (afterObj is null && beforeObj is null)
        {
            return;
        }

        var beforeDict = beforeObj?.EnumerateObject().ToDictionary(p => p.Name, p => p.Value) ?? new Dictionary<string, JsonElement>();
        var afterProps = afterObj?.EnumerateObject().Select(p => (p.Name, Value: p.Value)).ToList() ?? new List<(string Name, JsonElement Value)>();
        // Sort properties by type (scalars first, then nested blocks), then alphabetically
        var sortedAfterProps = SortPropertiesByType(afterProps);
        var unchangedAttributes = 0;
        var unchangedBlocks = 0;

        foreach (var (name, value) in sortedAfterProps)
        {
            var path = new List<string> { name };
            if (beforeDict.TryGetValue(name, out var beforeValue))
            {
                if (AreEqual(beforeValue, value))
                {
                    // Count blocks (arrays/objects) vs attributes (scalars)
                    if (value.ValueKind == JsonValueKind.Array || value.ValueKind == JsonValueKind.Object)
                    {
                        unchangedBlocks++;
                    }
                    else
                    {
                        unchangedAttributes++;
                    }
                }
                else
                {
                    RenderUpdatedValue(writer, beforeValue, value, name, indent, path, unknown, sensitive, replacePaths);
                }
            }
            else
            {
                RenderAddedValue(writer, value, name, indent, "+", AnsiStyle.Green, unknown, sensitive, path, 0);
            }
        }

        // Handle removed properties (sorted)
        var removedProps = beforeDict.Keys.Except(sortedAfterProps.Select(p => p.Name))
            .Select(name => (Name: name, Value: beforeDict[name]))
            .ToList();
        var sortedRemovedProps = SortPropertiesByType(removedProps);

        foreach (var (name, value) in sortedRemovedProps)
        {
            RenderRemovedValue(writer, value, name, indent, sensitive, new List<string> { name });
        }

        if (unchangedAttributes > 0)
        {
            WriteUnchangedComment(writer, indent, unchangedAttributes, "attributes");
        }

        if (unchangedBlocks > 0)
        {
            var blockWord = unchangedBlocks == 1 ? "block" : "blocks";
            WriteUnchangedComment(writer, indent, unchangedBlocks, blockWord);
        }
    }

    /// <summary>
    /// Sorts properties to match Terraform's grouping: scalars, then arrays, then objects (nested blocks).
    /// Within each group, properties are sorted alphabetically.
    /// </summary>
    /// <param name="properties">Properties to sort.</param>
    /// <returns>Sorted properties.</returns>
    private static List<(string Name, JsonElement Value)> SortPropertiesByType(IEnumerable<(string Name, JsonElement Value)> properties)
    {
        return properties
            .OrderBy(p =>
            {
                // Group 0: Scalars (string, number, bool, null) and primitive arrays
                // Group 1: Object arrays (nested blocks represented as arrays)
                // Group 2: Objects (nested blocks)
                return p.Value.ValueKind switch
                {
                    JsonValueKind.Array => ContainsOnlyPrimitives(p.Value) ? 0 : 1,
                    JsonValueKind.Object => 2,
                    _ => 0
                };
            })
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Checks if an array contains only primitive values (not objects or nested arrays).
    /// </summary>
    /// <param name="array">Array to check.</param>
    /// <returns>True if array contains only scalars, false if it contains objects/arrays.</returns>
    private static bool ContainsOnlyPrimitives(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
            {
                return false;
            }
        }

        return true;
    }
}