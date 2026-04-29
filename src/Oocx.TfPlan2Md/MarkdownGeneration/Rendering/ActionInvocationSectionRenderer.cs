using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Generic renderer for a single Terraform 1.14+ action invocation. Stays
/// provider-agnostic by deferring all action-specific knowledge to the JSON
/// payload (see AC-3 and the architecture rule in
/// <c>Architecture_NoProviderSpecificActionRenderer_Exists</c>).
/// Renders: bold address paragraph + horizontal rule, optional deferred
/// callout, properties table, config values block (sensitivity-aware), and an
/// optional status/diagnostics JSON block.
/// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Renderer composes multiple model/helper abstractions by design.")]
internal static class ActionInvocationSectionRenderer
{
    /// <summary>
    /// Renders one action invocation to markdown.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="model">Action invocation model.</param>
    /// <param name="context">Render context (for sensitivity policy).</param>
    public static void Render(MarkdownWriter writer, ActionInvocationModel model, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        var inv = model.Invocation;
        var prefix = model.IsDeferred ? "⏳\u00A0" : string.Empty;
        writer.Paragraph(
            $"**{prefix}{MarkdownHelpers.EscapeMarkdown(inv.Address)}** — {MarkdownHelpers.EscapeMarkdown(inv.Type)} ({MarkdownHelpers.EscapeMarkdown(inv.ProviderName)})");
        writer.BlankLine();

        if (model.IsDeferred)
        {
            writer.Paragraph("> ⏳\u00A0**Deferred** — will run on a subsequent apply.");
            writer.BlankLine();
        }

        RenderPropertiesTable(writer, model);
        RenderConfigValues(writer, inv, context);
        RenderStatusAndDiagnostics(writer, inv);

        writer.Paragraph("---");
        writer.BlankLine();
    }

    /// <summary>
    /// Renders the action's two-column properties table (Trigger / list indices /
    /// Deferred). Optional rows are omitted gracefully when their source field
    /// is absent.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="model">Action invocation model.</param>
    private static void RenderPropertiesTable(MarkdownWriter writer, ActionInvocationModel model)
    {
        var inv = model.Invocation;
        writer.Raw("| Property | Value |\n");
        writer.Raw("| -------- | ----- |\n");

        if (inv.LifecycleActionTrigger is { } trig)
        {
            writer.TableRow(["Trigger", MarkdownHelpers.EscapeMarkdownTableCell(trig.ActionTriggerEvent ?? "(lifecycle)")]);
            if (trig.TriggeringResourceAddress is { Length: > 0 } addr)
            {
                writer.TableRow(["Triggered by", MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdownTableCell(addr))]);
            }

            if (trig.ActionTriggerBlockIndex is { } abi)
            {
                writer.TableRow(["Action block index", abi.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            }

            if (trig.ActionsListIndex is { } ali)
            {
                writer.TableRow(["Actions list index", ali.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            }
        }
        else if (inv.InvokeActionTrigger is not null)
        {
            writer.TableRow(["Trigger", "invoke"]);
        }

        if (model.IsDeferred)
        {
            writer.TableRow(["Deferred", "Yes"]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders the action's config values, masking sensitive entries via
    /// <see cref="SensitivityHelper"/> and replacing unknown entries with the
    /// "(known after apply)" sentinel. Honors <see cref="IRenderContext.ShowSensitive"/>
    /// for SARIF-aware unmasking.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="inv">Parsed action invocation.</param>
    /// <param name="context">Render context.</param>
    private static void RenderConfigValues(MarkdownWriter writer, Parsing.ActionInvocation inv, IRenderContext context)
    {
        if (inv.ConfigValues is not { } configElement)
        {
            return;
        }

        var values = JsonFlattener.ConvertToFlatDictionary(configElement);
        if (values.Count == 0)
        {
            return;
        }

        var sensitive = inv.ConfigSensitive is { } cs
            ? JsonFlattener.ConvertToFlatDictionary(cs)
            : new Dictionary<string, string?>();
        var unknown = inv.ConfigUnknown is { } cu
            ? JsonFlattener.ConvertToFlatDictionary(cu)
            : new Dictionary<string, string?>();
        var emptySensitive = new Dictionary<string, string?>();

        writer.Paragraph("**Config**");
        writer.BlankLine();
        writer.Raw("| Key | Value |\n");
        writer.Raw("| --- | ----- |\n");

        foreach (var kvp in values)
        {
            string display;
            if (unknown.TryGetValue(kvp.Key, out var u) && u == "true")
            {
                display = "(known after apply)";
            }
            else if (!context.ShowSensitive && SensitivityHelper.IsSensitiveAttribute(kvp.Key, sensitive, emptySensitive))
            {
                display = "(sensitive)";
            }
            else
            {
                display = MarkdownHelpers.EscapeMarkdownTableCell(kvp.Value ?? string.Empty);
            }

            writer.TableRow([
                MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdownTableCell(kvp.Key)),
                display
            ]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Emits a fenced JSON code block containing status and diagnostics fields,
    /// only when at least one is present. Absent / empty fields produce no output.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="inv">Parsed action invocation.</param>
    private static void RenderStatusAndDiagnostics(MarkdownWriter writer, Parsing.ActionInvocation inv)
    {
        if (inv.Status is null && inv.Diagnostics is null)
        {
            return;
        }

        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            if (inv.Status is { } status)
            {
                w.WritePropertyName("status");
                status.WriteTo(w);
            }

            if (inv.Diagnostics is { } diag)
            {
                w.WritePropertyName("diagnostics");
                diag.WriteTo(w);
            }

            w.WriteEndObject();
        }

        writer.Paragraph("**Status**");
        writer.BlankLine();
        writer.Code(Encoding.UTF8.GetString(stream.ToArray()), "json");
        writer.BlankLine();
    }
}
