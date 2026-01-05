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
        var indent = baseIndent + baseIndent;
        var before = ToElement(change.Change.Before);
        var after = ToElement(change.Change.After);
        var afterUnknown = ToElement(change.Change.AfterUnknown);
        var afterSensitive = ToElement(change.Change.AfterSensitive);
        var replacePaths = CollectReplacePaths(change.Change.ReplacePaths);

        switch (action)
        {
            case ResourceAction.Create:
            case ResourceAction.Read:
                RenderAdd(writer, after, afterUnknown, afterSensitive, indent, action == ResourceAction.Read ? "<=" : "+", action == ResourceAction.Read ? AnsiStyle.Cyan : AnsiStyle.Green);
                break;
            case ResourceAction.Delete:
                RenderRemove(writer, before, indent);
                break;
            case ResourceAction.Update:
            case ResourceAction.Replace:
                RenderUpdate(writer, before, after, afterUnknown, afterSensitive, indent, replacePaths);
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

        foreach (var property in EnumerateProperties(after.Value, unknown))
        {
            RenderAddedValue(writer, property.Value, property.Name, indent, marker, style, unknown, sensitive, new List<string> { property.Name });
        }
    }

    /// <summary>Renders deletions for destroy operations.</summary>
    /// <param name="writer">Target writer that applies ANSI styling.</param>
    /// <param name="before">State before the change.</param>
    /// <param name="indent">Indentation to use for nested attributes.</param>
    /// <returns>Nothing.</returns>
    private void RenderRemove(AnsiTextWriter writer, JsonElement? before, string indent)
    {
        if (before is not { ValueKind: JsonValueKind.Object })
        {
            return;
        }

        foreach (var property in before.Value.EnumerateObject())
        {
            RenderRemovedValue(writer, property.Value, property.Name, indent, new List<string> { property.Name });
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
                RenderAddedValue(writer, prop.Value, prop.Name, indent, "+", AnsiStyle.Green, unknown, sensitive, path);
            }
        }

        foreach (var removedName in beforeDict.Keys.Except(afterProps.Select(p => p.Name)))
        {
            RenderRemovedValue(writer, beforeDict[removedName], removedName, indent, new List<string> { removedName });
        }

        if (unchanged > 0)
        {
            WriteUnchangedComment(writer, indent, unchanged);
        }
    }
}
