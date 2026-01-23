using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>Renders before/after differences in a Terraform show-like format. Related feature: docs/features/030-terraform-show-approximation/specification.md.</summary>
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
                // SonarAnalyzer S3923: Both branches return "+" by design
                // Justification: Ternary kept for future extension where Read might use different symbol
#pragma warning disable S3923 // All branches return same value
                RenderAdd(writer, after, afterUnknown, afterSensitive, indent, action == ResourceAction.Read ? "+" : "+", AnsiStyle.Green);
#pragma warning restore S3923
                break;
            case ResourceAction.Delete:
                RenderRemove(writer, before, indent, beforeSensitive);
                break;
            case ResourceAction.Update:
            case ResourceAction.Replace:
                var effectiveSensitive = afterSensitive ?? beforeSensitive;
                RenderUpdate(writer, before, after, afterUnknown, effectiveSensitive, indent, replacePaths, action);
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
    private void RenderAdd(AnsiTextWriter writer, JsonElement? after, JsonElement? unknown, JsonElement? sensitive, string indent, string marker, AnsiStyle style)
    {
        if (after is not { ValueKind: JsonValueKind.Object })
        {
            return;
        }

        var properties = EnumerateProperties(after.Value, unknown).ToList();
        // Sort properties by type (scalars first, then nested blocks), then alphabetically
        var sorted = SortPropertiesByType(properties);
        var width = ComputeNameWidth(sorted, unknown);

        var previousWasNestedBlock = false;
        string? previousNestedBlockName = null;

        foreach (var (name, value) in sorted)
        {
            // Only arrays of objects are nested blocks; inline objects/maps are NOT nested blocks
            var isNestedBlock = value.ValueKind == JsonValueKind.Array && !ContainsOnlyPrimitives(value);

            if (ShouldInsertBlankLineBeforeNestedBlock(isNestedBlock, previousWasNestedBlock, previousNestedBlockName, name))
            {
                writer.WriteLineIfNotBlank();
            }

            RenderAddedValue(writer, value, name, indent, marker, style, unknown, sensitive, new List<string> { name }, width);

            if (isNestedBlock)
            {
                previousWasNestedBlock = true;
                previousNestedBlockName = name;
            }
            else
            {
                previousWasNestedBlock = false;
                previousNestedBlockName = null;
            }
        }
    }

    /// <summary>Renders deletions for destroy operations.</summary>
    /// <param name="writer">Target writer that applies ANSI styling.</param>
    /// <param name="before">State before the change.</param>
    /// <param name="indent">Indentation to use for nested attributes.</param>
    private void RenderRemove(AnsiTextWriter writer, JsonElement? before, string indent, JsonElement? sensitive)
    {
        if (before is not { ValueKind: JsonValueKind.Object })
        {
            return;
        }

        var properties = before.Value.EnumerateObject().Select(p => (p.Name, Value: p.Value)).ToList();
        // Sort properties by type (scalars first, then arrays, then nested blocks), then alphabetically
        var sorted = SortPropertiesByType(properties);
        var width = ComputeNameWidth(sorted, unknown: null);

        var previousWasNestedBlock = false;
        string? previousNestedBlockName = null;

        foreach (var (name, value) in sorted)
        {
            // Only arrays of objects are nested blocks; inline objects/maps are NOT nested blocks
            var isNestedBlock = value.ValueKind == JsonValueKind.Array && !ContainsOnlyPrimitives(value);

            if (ShouldInsertBlankLineBeforeNestedBlock(isNestedBlock, previousWasNestedBlock, previousNestedBlockName, name))
            {
                writer.WriteLineIfNotBlank();
            }

            RenderRemovedValue(writer, value, name, indent, sensitive, new List<string> { name }, width);

            if (isNestedBlock)
            {
                previousWasNestedBlock = true;
                previousNestedBlockName = name;
            }
            else
            {
                previousWasNestedBlock = false;
                previousNestedBlockName = null;
            }
        }
    }

    /// <summary>
    /// Determines whether a blank line should be inserted before rendering a nested block.
    /// Terraform show outputs a blank line when transitioning from scalar attributes to blocks,
    /// and also between different block types, but not between repeated blocks of the same name.
    /// </summary>
    /// <param name="isNestedBlock">Whether the current value is a nested block.</param>
    /// <param name="previousWasNestedBlock">Whether the previously rendered value was a nested block.</param>
    /// <param name="previousNestedBlockName">The previous block name when <paramref name="previousWasNestedBlock"/> is true.</param>
    /// <param name="currentName">Current property name.</param>
    /// <returns>True if a blank line should be inserted.</returns>
    private static bool ShouldInsertBlankLineBeforeNestedBlock(
        bool isNestedBlock,
        bool previousWasNestedBlock,
        string? previousNestedBlockName,
        string currentName)
    {
        if (!isNestedBlock)
        {
            return false;
        }

        if (!previousWasNestedBlock)
        {
            return true;
        }

        return !string.Equals(previousNestedBlockName, currentName, StringComparison.Ordinal);
    }

    /// <summary>Renders updates including replacement scenarios.</summary>
    /// <param name="writer">Target writer that applies ANSI styling.</param>
    /// <param name="before">State before the change.</param>
    /// <param name="after">State after the change.</param>
    /// <param name="unknown">Unknown value map from the plan.</param>
    /// <param name="sensitive">Sensitive value map from the plan.</param>
    /// <param name="indent">Indentation to use for nested attributes.</param>
    /// <param name="replacePaths">Paths that force replacement.</param>
    /// <param name="action">Resource action (Update or Replace).</param>
    private void RenderUpdate(AnsiTextWriter writer, JsonElement? before, JsonElement? after, JsonElement? unknown, JsonElement? sensitive, string indent, HashSet<string> replacePaths, ResourceAction action)
    {
        var beforeObj = before is { ValueKind: JsonValueKind.Object } ? before : null;
        var afterObj = after is { ValueKind: JsonValueKind.Object } ? after : null;
        if (afterObj is null && beforeObj is null)
        {
            return;
        }

        var beforeDict = beforeObj?.EnumerateObject().ToDictionary(p => p.Name, p => p.Value) ?? new Dictionary<string, JsonElement>();
        var afterProps = afterObj?.EnumerateObject().Select(p => (p.Name, Value: p.Value)).ToList() ?? new List<(string Name, JsonElement Value)>();

        // For replace operations, preserve before JSON order; for updates, sort by type
        List<(string Name, JsonElement Value)> sortedAfterProps;
        List<string> propertyOrder;
        if (action == ResourceAction.Replace)
        {
            // Use before JSON order for replace operations
            propertyOrder = beforeDict.Keys.ToList();
            sortedAfterProps = afterProps.OrderBy(p =>
            {
                var idx = propertyOrder.IndexOf(p.Name);
                return idx == -1 ? int.MaxValue : idx;
            }).ToList();
        }
        else
        {
            // Sort properties by type (scalars first, then nested blocks), then alphabetically
            sortedAfterProps = SortPropertiesByType(afterProps);
            propertyOrder = new List<string>(); // Not used for non-replace
        }

        var unchangedAttributes = 0;
        var unchangedBlocks = 0;
        var unchangedIdName = new List<(string Name, JsonElement Value, List<string> Path)>();

        // Compute width based on ALL properties in the update block for consistent alignment
        var allProperties = afterProps.Select(p => (p.Name, p.Value)).Order().ToList();
        var width = ComputeNameWidth(allProperties, unknown);

        // Identify removed properties that are actually unknown (for replace operations)
        var removedProps = beforeDict.Keys.Except(sortedAfterProps.Select(p => p.Name))
            .Select(name => (Name: name, Value: beforeDict[name]))
            .ToList();
        List<(string Name, JsonElement Value)> sortedRemovedProps;
        if (action == ResourceAction.Replace)
        {
            // Use before JSON order for replace operations
            sortedRemovedProps = removedProps.OrderBy(p =>
            {
                var idx = propertyOrder.IndexOf(p.Name);
                return idx == -1 ? int.MaxValue : idx;
            }).ToList();
        }
        else
        {
            sortedRemovedProps = SortPropertiesByType(removedProps);
        }

        var unknownObj = unknown is { ValueKind: JsonValueKind.Object } ? unknown : null;
        var removedButUnknown = new List<(string Name, JsonElement Value)>();
        var truelyRemoved = new List<(string Name, JsonElement Value)>();

        foreach (var (name, value) in sortedRemovedProps)
        {
            // Check if this property is in after_unknown (indicates it will exist but value is unknown)
            var isInAfterUnknown = unknownObj?.TryGetProperty(name, out _) == true;

            // If it's a scalar and in after_unknown, it's actually an update to (known after apply)
            var isScalar = value.ValueKind != JsonValueKind.Array && value.ValueKind != JsonValueKind.Object;
            if (isScalar && isInAfterUnknown)
            {
                removedButUnknown.Add((name, value));
            }
            else
            {
                truelyRemoved.Add((name, value));
            }
        }

        // First pass: Process scalars and single objects (not block arrays)
        // For replace operations, also interleave removed-but-unknown scalars and unchanged id/name in correct order
        var processedRemovedButUnknown = new HashSet<string>();
        var processedUnchangedIdName = new HashSet<string>();
        foreach (var (name, value) in sortedAfterProps)
        {
            var isBlockArray = value.ValueKind == JsonValueKind.Array && !ContainsOnlyPrimitives(value);

            // Skip block arrays in first pass
            if (isBlockArray)
            {
                continue;
            }

            // For replace operations, render any removed-but-unknown scalars and unchanged id/name that come before this property
            if (action == ResourceAction.Replace)
            {
                var currentIdx = propertyOrder.IndexOf(name);

                // Render removed-but-unknown scalars in their correct position
                foreach (var (rName, rValue) in removedButUnknown)
                {
                    if (processedRemovedButUnknown.Contains(rName))
                    {
                        continue;
                    }

                    var rIdx = propertyOrder.IndexOf(rName);
                    if (rIdx < currentIdx)
                    {
                        // Render this removed-but-unknown scalar before the current property
                        var rPath = new List<string> { rName };
                        var replacement = replacePaths.Contains(FormatPath(rPath));
                        writer.Write(indent);
                        writer.WriteStyled("~", AnsiStyle.Yellow);
                        writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                        writer.Write(" ");
                        var paddedName = width > 0 ? rName.PadRight(width, ' ') : rName;
                        writer.Write(paddedName);
                        writer.Write(" = ");
                        writer.Write(InlineValue(rValue));
                        writer.Write(" -> ");
                        writer.Write("(known after apply)");
                        if (replacement)
                        {
                            writer.Write(" ");
                            writer.WriteStyled("# forces replacement", AnsiStyle.Red);
                            writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                        }

                        writer.WriteLine();
                        processedRemovedButUnknown.Add(rName);
                    }
                }

                // Render unchanged id/name in their correct position
                foreach (var (uName, uValue, uPath) in unchangedIdName)
                {
                    if (processedUnchangedIdName.Contains(uName))
                    {
                        continue;
                    }

                    var uIdx = propertyOrder.IndexOf(uName);
                    if (uIdx < currentIdx)
                    {
                        // Render this unchanged id/name before the current property
                        RenderAddedValue(writer, uValue, uName, indent + Indent, marker: string.Empty, style: AnsiStyle.Reset, unknown, sensitive, uPath, nameWidth: width);
                        processedUnchangedIdName.Add(uName);
                    }
                }
            }

            var path = new List<string> { name };
            if (beforeDict.TryGetValue(name, out var beforeValue))
            {
                if (AreEqual(beforeValue, value))
                {
                    // Skip null values and empty arrays/objects
                    var isNullOrEmpty = value.ValueKind == JsonValueKind.Null ||
                                       (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0) ||
                                       (value.ValueKind == JsonValueKind.Object && !value.EnumerateObject().Any());

                    if (!isNullOrEmpty)
                    {
                        var isObject = value.ValueKind == JsonValueKind.Object;
                        if (isObject)
                        {
                            unchangedBlocks++;
                        }
                        else
                        {
                            // For replace operations, render unchanged id/name immediately in their correct position
                            // For other operations, collect them for rendering after changed items
                            if (name == "id" || name == "name")
                            {
                                if (action == ResourceAction.Replace)
                                {
                                    // Render immediately
                                    RenderAddedValue(writer, value, name, indent + Indent, marker: string.Empty, style: AnsiStyle.Reset, unknown, sensitive, path, nameWidth: width);
                                    processedUnchangedIdName.Add(name);
                                }
                                else
                                {
                                    unchangedIdName.Add((name, value, path));
                                }
                            }
                            else
                            {
                                unchangedAttributes++;
                            }
                        }
                    }
                }
                else
                {
                    RenderUpdatedValue(writer, beforeValue, value, name, indent, path, unknown, sensitive, replacePaths, width);
                }
            }
            else
            {
                RenderAddedValue(writer, value, name, indent, "+", AnsiStyle.Green, unknown, sensitive, path, 0);
            }
        }

        // For replace operations, render any remaining removed-but-unknown scalars that come after all after props
        if (action == ResourceAction.Replace)
        {
            foreach (var (rName, rValue) in removedButUnknown)
            {
                if (processedRemovedButUnknown.Contains(rName))
                {
                    continue;
                }

                var rPath = new List<string> { rName };
                var replacement = replacePaths.Contains(FormatPath(rPath));
                writer.Write(indent);
                writer.WriteStyled("~", AnsiStyle.Yellow);
                writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                writer.Write(" ");
                var paddedName = width > 0 ? rName.PadRight(width, ' ') : rName;
                writer.Write(paddedName);
                writer.Write(" = ");
                writer.Write(InlineValue(rValue));
                writer.Write(" -> ");
                writer.Write("(known after apply)");
                if (replacement)
                {
                    writer.Write(" ");
                    writer.WriteStyled("# forces replacement", AnsiStyle.Red);
                    writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                }

                writer.WriteLine();
            }
        }

        // Render unchanged id/name and write unchanged comment BEFORE block arrays
        // For replace operations, skip those already processed
        foreach (var (name, value, path) in unchangedIdName)
        {
            if (action == ResourceAction.Replace && processedUnchangedIdName.Contains(name))
            {
                continue;
            }

            // Unchanged attributes are indented an extra level to compensate for no marker
            RenderAddedValue(writer, value, name, indent + Indent, marker: string.Empty, style: AnsiStyle.Reset, unknown, sensitive, path, nameWidth: width);
        }

        if (unchangedAttributes > 0)
        {
            WriteUnchangedComment(writer, indent + Indent, unchangedAttributes, "attributes");
        }

        // Check if there are any block arrays to process in second pass
        // SonarAnalyzer S6605: Use Exists instead of Any
        // Justification: This is a false positive. sortedAfterProps is List<JsonProperty>, where
        // JsonProperty is a struct from System.Text.Json. The List<JsonProperty> type does not
        // have an Exists method - only List<T> for reference types does. Any() is the correct
        // method for this collection type.
#pragma warning disable S6605
        var hasBlockArrays = sortedAfterProps.Any(p => p.Value.ValueKind == JsonValueKind.Array && !ContainsOnlyPrimitives(p.Value));
#pragma warning restore S6605

        // Insert blank line before block arrays if there were scalar attributes AND there are block arrays to show
        // For replace operations, count only unprocessed unchangedIdName
        var unprocessedUnchangedIdName = action == ResourceAction.Replace
            ? unchangedIdName.Count(item => !processedUnchangedIdName.Contains(item.Name))
            : unchangedIdName.Count;
        var hasScalarContent = unprocessedUnchangedIdName > 0 || unchangedAttributes > 0;
        if (hasScalarContent && hasBlockArrays)
        {
            writer.WriteLine();
        }

        // Second pass: Process block arrays and removed items
        var blockArraysInAfter = sortedAfterProps.Where(p => p.Value.ValueKind == JsonValueKind.Array && !ContainsOnlyPrimitives(p.Value)).ToList();
        var blockArrayIndex = 0;
        foreach (var (name, value) in sortedAfterProps)
        {
            var isBlockArray = value.ValueKind == JsonValueKind.Array && !ContainsOnlyPrimitives(value);

            // Only process block arrays in second pass
            if (!isBlockArray)
            {
                continue;
            }

            var path = new List<string> { name };
            if (beforeDict.TryGetValue(name, out var beforeValue))
            {
                // Check if this block array is in after_unknown (for replace operations with sensitive blocks)
                var isInAfterUnknown = unknownObj?.TryGetProperty(name, out _) == true;

                // In replace operations, if block is in after_unknown, render as separate - and + blocks
                if (action == ResourceAction.Replace && isInAfterUnknown)
                {
                    // Render as removal then addition
                    RenderRemovedValue(writer, beforeValue, name, indent, sensitive, path);
                    RenderAddedValue(writer, value, name, indent, "+", AnsiStyle.Green, unknown, sensitive, path, 0);
                }
                else if (AreEqual(beforeValue, value))
                {
                    var isNullOrEmpty = value.GetArrayLength() == 0;
                    if (!isNullOrEmpty)
                    {
                        unchangedBlocks++;
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

            blockArrayIndex++;
            // Add blank line after this block if there are more blocks to render (in sortedAfterProps or truelyRemoved)
            // This applies to replace operations where blocks are rendered as separate - and + 
            // Skip the blank line after the FIRST block (it's already added before the blocks section)
            if (action == ResourceAction.Replace && blockArrayIndex > 1 && (blockArrayIndex < blockArraysInAfter.Count || truelyRemoved.Count > 0))
            {
                writer.WriteLine();
            }
        }

        // Render truly removed properties (block arrays that don't exist in after)
        foreach (var (name, value) in truelyRemoved)
        {
            RenderRemovedValue(writer, value, name, indent, sensitive, new List<string> { name });
        }

        if (unchangedBlocks > 0)
        {
            // Add blank line before unchanged blocks comment if there were block arrays or removed properties
            if (hasBlockArrays || truelyRemoved.Count > 0)
            {
                writer.WriteLine();
            }
            var blockWord = unchangedBlocks == 1 ? "block" : "blocks";
            WriteUnchangedComment(writer, indent + Indent, unchangedBlocks, blockWord);
        }
    }

    /// <summary>
    /// Sorts properties to match Terraform's grouping: scalars (including inline objects/maps), then block arrays.
    /// Within each group, properties are sorted alphabetically.
    /// </summary>
    /// <param name="properties">Properties to sort.</param>
    /// <returns>Sorted properties.</returns>
    private static List<(string Name, JsonElement Value)> SortPropertiesByType(IEnumerable<(string Name, JsonElement Value)> properties)
    {
        return properties
            .OrderBy(p =>
            {
                // Group 0: Scalars (string, number, bool, null), inline objects/maps, and primitive arrays
                // Group 1: Object arrays (nested blocks represented as arrays)
                return p.Value.ValueKind switch
                {
                    JsonValueKind.Array => ContainsOnlyPrimitives(p.Value) ? 0 : 1,
                    JsonValueKind.Object => 0, // Inline objects/maps are sorted with scalars
                    _ => 0
                };
            })
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Checks if an array contains only primitive values (not objects or nested arrays).
    /// Empty arrays are treated as non-primitive since their intended type is unknown.
    /// </summary>
    /// <param name="array">Array to check.</param>
    /// <returns>True if array contains only scalars, false if it contains objects/arrays or is empty.</returns>
    private static bool ContainsOnlyPrimitives(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        // SonarAnalyzer S3267: Loop simplification
        // Justification: This is an early-exit check with state mutation (hasAnyElement tracking),
        // not a pure Select operation. Converting to LINQ would require either two separate
        // Any() calls (double enumeration) or forcing full enumeration when early exit is needed.
        // The explicit loop is more efficient and clearer for this "check-while-tracking" pattern.
#pragma warning disable S3267
        var hasAnyElement = false;
        foreach (var item in array.EnumerateArray())
        {
            hasAnyElement = true;
            if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
            {
                return false;
            }
        }
#pragma warning restore S3267

        // Empty arrays are treated as non-primitive (likely object arrays in schema)
        return hasAnyElement;
    }
}
