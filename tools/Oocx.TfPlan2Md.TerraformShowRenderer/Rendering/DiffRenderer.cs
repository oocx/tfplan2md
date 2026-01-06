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
                RenderAdd(writer, after, afterUnknown, afterSensitive, indent, action == ResourceAction.Read ? "<=" : "+", action == ResourceAction.Read ? AnsiStyle.Cyan : AnsiStyle.Green);
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
        // Sort properties: scalars first (alphabetically), then blocks (alphabetically)
        var sorted = SortPropertiesForOutput(properties);
        var width = ComputeNameWidth(sorted);

        var previousWasScalar = false;
        var previousBlockName = (string?)null;
        foreach (var property in sorted)
        {
            var isBlock = IsBlock(property.Value);
            var path = new List<string> { property.Name };
            var isUnknown = IsUnknownPath(unknown, path);
            var isSensitive = IsSensitivePath(sensitive, path);

            // Skip properties that won't be rendered (e.g., empty arrays)
            if (!ShouldRenderValue(property.Value, isUnknown, isSensitive))
            {
                continue;
            }

            // Add blank line when transitioning from scalars to blocks, or between different block types
            if (isBlock && (previousWasScalar || (previousBlockName != null && previousBlockName != property.Name)))
            {
                writer.WriteLine();
            }

            RenderAddedValue(writer, property.Value, property.Name, indent, marker, style, unknown, sensitive, path, width);
            previousWasScalar = !isBlock;
            previousBlockName = isBlock ? property.Name : null;
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

        var properties = before.Value.EnumerateObject().Select(p => (p.Name, p.Value)).ToList();
        // Sort properties: scalars first (alphabetically), then blocks (alphabetically)
        var sorted = SortPropertiesForOutput(properties);
        var width = ComputeNameWidth(sorted);

        var previousWasScalar = false;
        var previousBlockName = (string?)null;
        foreach (var property in sorted)
        {
            var isBlock = IsBlock(property.Value);
            var path = new List<string> { property.Name };
            var isSensitive = IsSensitivePath(sensitive, path);

            // Skip properties that won't be rendered (e.g., empty arrays)
            if (!ShouldRenderValue(property.Value, false, isSensitive))
            {
                continue;
            }

            // Add blank line when transitioning from scalars to blocks, or between different block types
            if (isBlock && (previousWasScalar || (previousBlockName != null && previousBlockName != property.Name)))
            {
                writer.WriteLine();
            }

            RenderRemovedValue(writer, property.Value, property.Name, indent, sensitive, path, width);
            previousWasScalar = !isBlock;
            previousBlockName = isBlock ? property.Name : null;
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
        var afterProps = afterObj?.EnumerateObject().ToList() ?? new List<JsonProperty>();
        var unchanged = 0;

        foreach (var prop in afterProps)
        {
            var path = new List<string> { prop.Name };
            if (beforeDict.TryGetValue(prop.Name, out var beforeValue))
            {
                if (AreEqual(beforeValue, prop.Value))
                {
                    unchanged++;
                }
                else
                {
                    RenderUpdatedValue(writer, beforeValue, prop.Value, prop.Name, indent, path, unknown, sensitive, replacePaths);
                }
            }
            else
            {
                RenderAddedValue(writer, prop.Value, prop.Name, indent, "+", AnsiStyle.Green, unknown, sensitive, path, 0);
            }
        }

        foreach (var removedName in beforeDict.Keys.Except(afterProps.Select(p => p.Name)))
        {
            RenderRemovedValue(writer, beforeDict[removedName], removedName, indent, sensitive, new List<string> { removedName }, 0);
        }

        if (unchanged > 0)
        {
            WriteUnchangedComment(writer, indent, unchanged);
        }
    }
}
