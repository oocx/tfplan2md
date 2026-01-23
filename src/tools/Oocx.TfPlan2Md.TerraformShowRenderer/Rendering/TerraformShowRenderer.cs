using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>
/// Produces Terraform show-like text output from parsed plan data.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
internal sealed class TerraformShowRenderer
{
    private const string Indent = "  ";
    private readonly DiffRenderer _diffRenderer = new();
    private readonly ValueRenderer _valueRenderer = new();

    /// <summary>
    /// Renders the supplied plan into Terraform show-like text.
    /// Related acceptance: Task 2 legend and header.
    /// </summary>
    /// <param name="plan">Parsed Terraform plan.</param>
    /// <param name="suppressColor">Determines whether ANSI sequences are omitted.</param>
    /// <returns>Rendered text approximating terraform show output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="plan"/> is null.</exception>
    public string Render(TerraformPlan plan, bool suppressColor, JsonElement? outputChanges = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var builder = new StringBuilder();
        using var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture);
        using var ansiWriter = new AnsiTextWriter(stringWriter, !suppressColor);

        ansiWriter.WriteLine();

        var hasReplace = plan.ResourceChanges.Any(c => MapAction(c.Change.Actions) == ResourceAction.Replace);
        var hasRead = plan.ResourceChanges.Any(c => MapAction(c.Change.Actions) == ResourceAction.Read);

        WriteLegend(ansiWriter, hasReplace, hasRead);
        ansiWriter.WriteLine("Terraform will perform the following actions:");
        ansiWriter.WriteLine();

        foreach (var change in plan.ResourceChanges)
        {
            var action = MapAction(change.Change.Actions);
            if (action == ResourceAction.NoOp)
            {
                continue;
            }

            WriteResourceHeader(ansiWriter, change, action);
            WriteResourceBlock(ansiWriter, change, action);
            ansiWriter.WriteLine();
        }

        WritePlanSummary(ansiWriter, plan.ResourceChanges);

        var parsedOutputChanges = ParseOutputChanges(outputChanges);
        if (parsedOutputChanges.Count > 0)
        {
            ansiWriter.WriteLine();
            // Compute width from ALL outputs (not just changed ones) for consistent alignment
            var allOutputWidth = outputChanges is { ValueKind: JsonValueKind.Object }
                ? outputChanges.Value.EnumerateObject().Max(p => p.Name.Length)
                : 0;
            WriteOutputChanges(ansiWriter, parsedOutputChanges, allOutputWidth);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes the static legend section that precedes resource details.
    /// </summary>
    /// <param name="writer">ANSI-aware writer used for output.</param>
    private static void WriteLegend(AnsiTextWriter writer, bool includeReplace, bool includeRead)
    {
        writer.WriteLine("Terraform used the selected providers to generate the following execution");
        writer.WriteLine("plan. Resource actions are indicated with the following symbols:");

        WriteLegendEntry(writer, "+", "create", AnsiStyle.Green, "  ");
        WriteLegendEntry(writer, "~", "update in-place", AnsiStyle.Yellow, "  ");
        WriteLegendEntry(writer, "-", "destroy", AnsiStyle.Red, "  ");

        if (includeReplace)
        {
            writer.Write(string.Empty);
            writer.WriteStyled("-", AnsiStyle.Red);
            writer.Write("/");
            writer.WriteStyled("+", AnsiStyle.Green);
            writer.Write(" destroy and then create replacement");
            writer.WriteReset();
            writer.WriteLine();
        }

        if (includeRead)
        {
            WriteLegendEntry(writer, "<=", "read (data resources)", AnsiStyle.Cyan, " ");
        }

        writer.WriteLine();
    }

    /// <summary>
    /// Writes a single legend entry with a styled marker.
    /// </summary>
    /// <param name="writer">Destination writer.</param>
    /// <param name="marker">Symbol indicating the action.</param>
    /// <param name="description">Description of the action.</param>
    /// <param name="style">ANSI style applied to the marker.</param>
    private static void WriteLegendEntry(AnsiTextWriter writer, string marker, string description, AnsiStyle style, string indent)
    {
        writer.Write(indent);
        writer.WriteStyled(marker, style);
        writer.Write(" ");
        writer.Write(description);
        writer.WriteReset();
        writer.WriteLine();
    }

    /// <summary>
    /// Maps Terraform actions to a high-level action kind.
    /// </summary>
    /// <param name="actions">Ordered list of Terraform actions.</param>
    /// <returns>Mapped action kind.</returns>
    private static ResourceAction MapAction(IReadOnlyList<string> actions)
    {
        if (actions.Count == 0)
        {
            return ResourceAction.Unknown;
        }

        if (actions.Count == 1)
        {
            return actions[0] switch
            {
                "create" => ResourceAction.Create,
                "update" => ResourceAction.Update,
                "delete" => ResourceAction.Delete,
                "read" => ResourceAction.Read,
                "no-op" => ResourceAction.NoOp,
                _ => ResourceAction.Unknown
            };
        }

        if (actions.Count == 2)
        {
            var first = actions[0];
            var second = actions[1];
            if ((first == "delete" && second == "create") || (first == "create" && second == "delete"))
            {
                return ResourceAction.Replace;
            }
        }

        return ResourceAction.Unknown;
    }

    /// <summary>
    /// Writes the plan summary line with resource counts.
    /// </summary>
    /// <param name="writer">Destination writer.</param>
    /// <param name="changes">All resource changes from the plan.</param>
    private static void WritePlanSummary(AnsiTextWriter writer, IReadOnlyList<ResourceChange> changes)
    {
        var add = 0;
        var update = 0;
        var destroy = 0;

        foreach (var change in changes)
        {
            var action = MapAction(change.Change.Actions);
            switch (action)
            {
                case ResourceAction.Create:
                    add++;
                    break;
                case ResourceAction.Update:
                    update++;
                    break;
                case ResourceAction.Delete:
                    destroy++;
                    break;
                case ResourceAction.Replace:
                    add++;
                    destroy++;
                    break;
            }
        }

        writer.WriteStyled("Plan:", AnsiStyle.Bold);
        writer.Write(" ");
        writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
        writer.Write(add.ToString(CultureInfo.InvariantCulture));
        writer.Write(" to add, ");
        writer.Write(update.ToString(CultureInfo.InvariantCulture));
        writer.Write(" to change, ");
        writer.Write(destroy.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine(" to destroy.");
    }

    /// <summary>
    /// Parses output changes from a raw plan JSON fragment.
    /// </summary>
    /// <param name="outputChanges">Root JSON element for output changes.</param>
    /// <returns>Normalized output change entries.</returns>
    private static IReadOnlyList<OutputChange> ParseOutputChanges(JsonElement? outputChanges)
    {
        if (outputChanges is not { ValueKind: JsonValueKind.Object } root)
        {
            return Array.Empty<OutputChange>();
        }

        var results = new List<OutputChange>();
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!property.Value.TryGetProperty("actions", out var actionsElement) || actionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var actions = actionsElement.EnumerateArray().Select(a => a.GetString() ?? string.Empty).ToList();
            var action = MapAction(actions);
            if (action == ResourceAction.NoOp || action == ResourceAction.Unknown)
            {
                continue;
            }

            property.Value.TryGetProperty("before", out var before);
            property.Value.TryGetProperty("after", out var after);
            property.Value.TryGetProperty("after_unknown", out var afterUnknown);
            property.Value.TryGetProperty("after_sensitive", out var afterSensitive);

            results.Add(new OutputChange(property.Name, action, before, after, afterUnknown, afterSensitive));
        }

        return results;
    }

    /// <summary>
    /// Writes the output changes section following Terraform conventions.
    /// </summary>
    /// <param name="writer">Destination writer.</param>
    /// <param name="outputs">Parsed output change entries.</param>
    /// <param name="width">Width for aligning output names (computed from all outputs).</param>
    private void WriteOutputChanges(AnsiTextWriter writer, IReadOnlyList<OutputChange> outputs, int width)
    {
        if (outputs.Count == 0)
        {
            return;
        }

        writer.WriteLine("Changes to Outputs:");

        foreach (var output in outputs)
        {
            WriteOutputChange(writer, output, width);
        }
    }

    /// <summary>
    /// Writes a single output change line with appropriate styling.
    /// </summary>
    /// <param name="writer">Destination writer.</param>
    /// <param name="output">Output change data.</param>
    /// <param name="width">Width used to align output names.</param>
    private void WriteOutputChange(AnsiTextWriter writer, OutputChange output, int width)
    {
        writer.Write(Indent);
        switch (output.Action)
        {
            case ResourceAction.Create:
                writer.WriteStyled("+", AnsiStyle.Green);
                writer.Write(" ");
                writer.Write(output.Name.PadRight(width, ' '));
                writer.Write(" = ");
                writer.Write(RenderOutputValue(output.After, output.AfterUnknown, output.AfterSensitive));
                writer.WriteLine();
                return;
            case ResourceAction.Delete:
                writer.WriteStyled("-", AnsiStyle.Red);
                writer.Write(" ");
                writer.Write(output.Name.PadRight(width, ' '));
                writer.Write(" = ");
                writer.Write(RenderOutputValue(output.Before, null, output.AfterSensitive));
                writer.Write(" ");
                writer.WriteStyled("-> null", AnsiStyle.Dim);
                writer.WriteLine();
                return;
            default:
                writer.WriteStyled("~", AnsiStyle.Yellow);
                writer.WriteReset(); // Extra reset to match Terraform's double-reset pattern
                writer.Write(" ");
                writer.Write(output.Name.PadRight(width, ' '));
                writer.Write(" = ");
                writer.Write(RenderOutputValue(output.Before, null, output.AfterSensitive));
                writer.Write(" -> ");
                writer.Write(RenderOutputValue(output.After, output.AfterUnknown, output.AfterSensitive));
                writer.WriteLine();
                return;
        }
    }

    /// <summary>
    /// Formats output change values with unknown and sensitive handling.
    /// </summary>
    /// <param name="value">Value to render.</param>
    /// <param name="unknown">Unknown indicator element.</param>
    /// <param name="sensitive">Sensitive indicator element.</param>
    /// <returns>Rendered output value.</returns>
    private string RenderOutputValue(JsonElement? value, JsonElement? unknown, JsonElement? sensitive)
    {
        if (sensitive is { ValueKind: JsonValueKind.True })
        {
            return "(sensitive value)";
        }

        if (unknown is { ValueKind: JsonValueKind.True })
        {
            return "(known after apply)";
        }

        if (value is null)
        {
            return string.Empty;
        }

        return _valueRenderer.Render(value.Value);
    }

    /// <summary>
    /// Represents a normalized output change entry.
    /// </summary>
    /// <param name="Name">Output name.</param>
    /// <param name="Action">Change action.</param>
    /// <param name="Before">Value before the change.</param>
    /// <param name="After">Value after the change.</param>
    /// <param name="AfterUnknown">Unknown indicator.</param>
    /// <param name="AfterSensitive">Sensitive indicator.</param>
    private sealed record OutputChange(string Name, ResourceAction Action, JsonElement? Before, JsonElement? After, JsonElement? AfterUnknown, JsonElement? AfterSensitive);

    /// <summary>
    /// Writes the resource header comment lines for a change.
    /// </summary>
    /// <param name="writer">Destination writer.</param>
    /// <param name="change">Resource change metadata.</param>
    /// <param name="action">Mapped action kind.</param>
    private static void WriteResourceHeader(AnsiTextWriter writer, ResourceChange change, ResourceAction action)
    {
        // ANSI escape comes before indent in Terraform output
        writer.WriteStyled(Indent + "# " + change.Address, AnsiStyle.Bold);
        writer.Write(" ");

        switch (action)
        {
            case ResourceAction.Create:
                writer.WriteLine("will be created");
                break;
            case ResourceAction.Update:
                writer.WriteLine("will be updated in-place");
                break;
            case ResourceAction.Delete:
                writer.Write("will be ");
                writer.WriteLineStyled("destroyed", AnsiStyle.Bold, AnsiStyle.Red);
                break;
            case ResourceAction.Replace:
                writer.Write("must be ");
                writer.WriteLineStyled("replaced", AnsiStyle.Bold, AnsiStyle.Red);
                break;
            case ResourceAction.Read:
                writer.WriteLine("will be read during apply");
                break;
            default:
                writer.WriteLine("will be changed");
                break;
        }

        var reason = FormatActionReason(change);
        if (!string.IsNullOrWhiteSpace(reason))
        {
            writer.Write(Indent);
            writer.WriteLine(reason!);
        }
    }

    /// <summary>
    /// Writes the resource block line and braces with appropriate markers.
    /// </summary>
    /// <param name="writer">Destination writer.</param>
    /// <param name="change">Resource change metadata.</param>
    /// <param name="action">Mapped action kind.</param>
    private void WriteResourceBlock(AnsiTextWriter writer, ResourceChange change, ResourceAction action)
    {
        // Reset before indent, then write marker and block line
        writer.WriteReset();
        // Replace operations have no indent, Read operations use single space, others use Indent (2 spaces)
        // SonarAnalyzer S3358: Nested ternary operation
        // Justification: This 3-way indent selection (Replace→no indent, Read→1 space, others→2 spaces)
        // is highly readable in the rendering context and matches Terraform's output formatting convention
        // where precise visual layout is critical. Extracting to separate statements or method would
        // obscure the simple mapping between actions and their indent widths.
#pragma warning disable S3358
        var indent = action == ResourceAction.Replace ? "" : (action == ResourceAction.Read ? " " : Indent);
#pragma warning restore S3358
        writer.Write(indent);
        WriteMarker(writer, action);
        writer.WriteReset(); // Extra reset after marker to match Terraform output
        writer.Write(" ");

        var blockKeyword = string.Equals(change.Mode, "data", StringComparison.OrdinalIgnoreCase) ? "data" : "resource";
        writer.Write(blockKeyword);
        writer.Write(" \"");
        writer.Write(change.Type);
        writer.Write("\" \"");
        writer.Write(change.Name);
        writer.WriteLine("\" {");

        _diffRenderer.RenderAttributes(writer, change, action, Indent);

        // Closing brace aligns with attributes (Indent + Indent)
        writer.Write(Indent);
        writer.Write(Indent);
        writer.WriteLine("}");
    }

    /// <summary>
    /// Writes the marker for a resource action with styling.
    /// </summary>
    /// <param name="writer">Destination writer.</param>
    /// <param name="action">Action kind.</param>
    private static void WriteMarker(AnsiTextWriter writer, ResourceAction action)
    {
        switch (action)
        {
            case ResourceAction.Create:
                writer.WriteStyled("+", AnsiStyle.Green);
                break;
            case ResourceAction.Update:
                writer.WriteStyled("~", AnsiStyle.Yellow);
                break;
            case ResourceAction.Delete:
                writer.WriteStyled("-", AnsiStyle.Red);
                break;
            case ResourceAction.Read:
                writer.WriteStyled("<=", AnsiStyle.Cyan);
                break;
            case ResourceAction.Replace:
                writer.WriteStyled("-", AnsiStyle.Red);
                writer.Write("/");
                writer.WriteStyled("+", AnsiStyle.Green);
                break;
            default:
                writer.WriteStyled("~", AnsiStyle.Yellow);
                break;
        }
    }

    /// <summary>
    /// Formats a user-readable action reason, if provided.
    /// </summary>
    /// <param name="change">Resource change metadata.</param>
    /// <returns>Formatted reason or null.</returns>
    private static string? FormatActionReason(ResourceChange change)
    {
        if (string.IsNullOrWhiteSpace(change.ActionReason))
        {
            return null;
        }

        return change.ActionReason switch
        {
            "read_because_dependency_pending" => "# (depends on a resource or a module with changes pending)",
            "delete_because_no_resource_config" => FormattableString.Invariant($"# (because {change.Address} is not in configuration)"),
            "replace_because_cannot_update" => null, // Redundant with "must be replaced" message
            _ => FormattableString.Invariant($"# ({change.ActionReason})")
        };
    }
}

/// <summary>
/// Represents the normalized action applied to a resource change.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
internal enum ResourceAction
{
    /// <summary>
    /// Resource will be created.
    /// </summary>
    Create,

    /// <summary>
    /// Resource will be updated in-place.
    /// </summary>
    Update,

    /// <summary>
    /// Resource will be destroyed.
    /// </summary>
    Delete,

    /// <summary>
    /// Resource will be destroyed and recreated.
    /// </summary>
    Replace,

    /// <summary>
    /// Data resource will be read.
    /// </summary>
    Read,

    /// <summary>
    /// No changes will be applied.
    /// </summary>
    NoOp,

    /// <summary>
    /// Action could not be determined.
    /// </summary>
    Unknown
}
