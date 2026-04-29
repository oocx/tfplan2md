using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Orchestrates full report rendering from <see cref="ReportModel"/> to markdown text.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Renderer composes multiple model/rendering abstractions by design.")]
internal sealed class ReportRenderer
{
    /// <summary>
    /// Renders report header.
    /// </summary>
    private readonly HeaderRenderer _headerRenderer;

    /// <summary>
    /// Registry for resource-specific renderers.
    /// </summary>
    private readonly ResourceRendererRegistry _resourceRendererRegistry;

    /// <summary>
    /// Default fallback resource renderer.
    /// </summary>
    private readonly IResourceRenderer _defaultResourceRenderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportRenderer"/> class.
    /// </summary>
    /// <param name="headerRenderer">Header renderer.</param>
    /// <param name="resourceRendererRegistry">Resource renderer registry.</param>
    /// <param name="defaultResourceRenderer">Default fallback resource renderer.</param>
    public ReportRenderer(
        HeaderRenderer? headerRenderer = null,
        ResourceRendererRegistry? resourceRendererRegistry = null,
        IResourceRenderer? defaultResourceRenderer = null)
    {
        _headerRenderer = headerRenderer ?? new HeaderRenderer();
        _resourceRendererRegistry = resourceRendererRegistry ?? new ResourceRendererRegistry();
        _defaultResourceRenderer = defaultResourceRenderer ?? new DefaultResourceRenderer();
    }

    /// <summary>
    /// Renders a report model to markdown.
    /// </summary>
    /// <param name="model">The report model.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>Rendered markdown document.</returns>
    public string Render(ReportModel model, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        var writer = new MarkdownWriter();
        var hasOutputs = model.GlobalOutputs.Count > 0 || model.ModuleChanges.Any(module => module.Outputs.Count > 0);
        var isOutputsFocusedReport = hasOutputs && model.ModuleChanges.Count <= 1 && model.ModuleChanges.All(module => module.Changes.Count <= 4);
        var effectiveContext = new ScenarioRenderContext(
            context,
            isOutputsFocusedReport,
            false);

        _headerRenderer.Render(writer, model);
        SummaryRenderer.Render(writer, model.Summary, boldTotal: true);
        CodeAnalysisSectionRenderer.RenderSummary(writer, model.CodeAnalysis);
        RenderResourceChanges(writer, model, effectiveContext);
        CodeAnalysisSectionRenderer.RenderOtherFindings(writer, model.CodeAnalysis);
        RenderOtherActions(writer, model, effectiveContext);
        RenderDriftSection(writer, model, effectiveContext);
        RenderRelevantAttributes(writer, model.RelevantAttributes);
        RenderRefactoring(writer, model.RefactoringOperations);
        RenderOutputs(writer, model.GlobalOutputs, effectiveContext);
        RenderFilteredResourceInfo(writer, model);

        return writer.Build();
    }

    /// <summary>
    /// Renders the "🌀 Drift Detected" H2 section listing every entry from
    /// <see cref="ReportModel.Drift"/>. Section is omitted when drift is empty.
    /// Reuses the resource renderer registry so each drift entry is rendered
    /// with the same provider-specific styling as in the main "Resource Changes"
    /// section.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="model">Report model.</param>
    /// <param name="context">Render context.</param>
    private void RenderDriftSection(MarkdownWriter writer, ReportModel model, IRenderContext context)
    {
        if (model.Drift.Count == 0)
        {
            return;
        }

        writer.Heading("🌀\u00A0Drift Detected", 2);
        writer.BlankLine();

        foreach (var change in model.Drift)
        {
            var renderer = _resourceRendererRegistry.GetRenderer(change.Type) ?? _defaultResourceRenderer;
            renderer.Render(writer, change, context);
        }
    }

    /// <summary>
    /// Renders the "Relevant Attributes" H2 section. Each entry is rendered as a
    /// row in a two-column table (Resource, Attribute path). Section is omitted
    /// when the collection is empty so plans without this Terraform 1.14+ field
    /// produce no diff against pre-feature snapshots.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="attributes">Relevant attribute models.</param>
    private static void RenderRelevantAttributes(
        MarkdownWriter writer,
        IReadOnlyList<RelevantAttributeModel> attributes)
    {
        if (attributes.Count == 0)
        {
            return;
        }

        writer.Heading("Relevant Attributes", 2);
        writer.BlankLine();
        writer.Raw("| Resource | Attribute path |\n");
        writer.Raw("| -------- | -------------- |\n");

        foreach (var attr in attributes)
        {
            writer.TableRow([
                MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdownTableCell(attr.Resource)),
                MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdownTableCell(attr.AttributePath))
            ]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders the "🎬 Other Actions" H2 section for action invocations that do
    /// not attach to any rendered resource (invoke-mode actions and lifecycle
    /// orphans). Section is omitted when both groups are empty.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="model">Report model.</param>
    /// <param name="context">Render context.</param>
    private static void RenderOtherActions(MarkdownWriter writer, ReportModel model, IRenderContext context)
    {
        var other = model.OtherActions;
        if (other is null)
        {
            return;
        }

        if (other.InvokeActions.Count == 0 && other.LifecycleOrphanActions.Count == 0)
        {
            return;
        }

        writer.Heading("🎬\u00A0Other Actions", 2);
        writer.BlankLine();

        if (other.InvokeActions.Count > 0)
        {
            writer.Heading("Invoke actions", 3);
            writer.BlankLine();
            foreach (var action in other.InvokeActions)
            {
                ActionInvocationSectionRenderer.Render(writer, action, context);
            }
        }

        if (other.LifecycleOrphanActions.Count > 0)
        {
            writer.Heading("Lifecycle actions without a matching resource change", 3);
            writer.BlankLine();
            foreach (var action in other.LifecycleOrphanActions)
            {
                ActionInvocationSectionRenderer.Render(writer, action, context);
            }
        }
    }

    /// <summary>
    /// Renders the resource changes section grouped by module.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="model">Report model.</param>
    /// <param name="context">Render context.</param>
    private void RenderResourceChanges(MarkdownWriter writer, ReportModel model, IRenderContext context)
    {
        if (model.ModuleChanges.Count == 0)
        {
            return;
        }

        writer.Heading("Resource Changes", 2);
        writer.BlankLine();

        for (var moduleIndex = 0; moduleIndex < model.ModuleChanges.Count; moduleIndex++)
        {
            var module = model.ModuleChanges[moduleIndex];

            if (moduleIndex > 0)
            {
                writer.Paragraph("---");
                writer.BlankLine();
            }

            var moduleText = string.IsNullOrWhiteSpace(module.ModuleAddress)
                ? "root"
                : MarkdownHelpers.FormatCodeTable(module.ModuleAddress);

            writer.Heading($"📦\u00A0Module: {moduleText}", 3);
            writer.BlankLine();

            foreach (var change in module.Changes)
            {
                var renderer = _resourceRendererRegistry.GetRenderer(change.Type) ?? _defaultResourceRenderer;
                renderer.Render(writer, change, context);
            }

            RenderModuleOutputs(writer, module.Outputs, context);
        }
    }

    /// <summary>
    /// Renders outputs section for a specific module.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="outputs">Output models.</param>
    /// <param name="context">Render context.</param>
    private static void RenderModuleOutputs(MarkdownWriter writer, IReadOnlyList<OutputChangeModel> outputs, IRenderContext context)
    {
        if (outputs.Count == 0)
        {
            return;
        }

        writer.Heading("📤\u00A0Outputs", 4);
        writer.BlankLine();
        RenderOutputTable(writer, outputs, context);
    }

    /// <summary>
    /// Renders global outputs section.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="outputs">Output models.</param>
    /// <param name="context">Render context.</param>
    private static void RenderOutputs(MarkdownWriter writer, IReadOnlyList<OutputChangeModel> outputs, IRenderContext context)
    {
        if (outputs.Count == 0)
        {
            return;
        }

        writer.Heading("📤\u00A0Outputs", 2);
        writer.BlankLine();
        RenderOutputTable(writer, outputs, context);
    }

    /// <summary>
    /// Renders a markdown outputs table.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="outputs">Output models.</param>
    /// <param name="context">Render context.</param>
    private static void RenderOutputTable(MarkdownWriter writer, IReadOnlyList<OutputChangeModel> outputs, IRenderContext context)
    {
        writer.Raw("| Change | Name | Description | Sensitive | Value |\n");
        writer.Raw("| ------ | ---- | ----------- | --------- | ----- |\n");

        foreach (var output in outputs)
        {
            string value;
            // IsMasked must be checked before IsLargeOutputValue: a value that is both
            // sensitive and large must show "(sensitive value)" in the table cell, never
            // "_(see below)_". Reversing this order leaks secrets (Bug 1A).
            // Related issue: docs/issues/fix-sensitive-large-value-rendering/analysis.md
            if (output.IsMasked)
            {
                value = "(sensitive value)";
            }
            else if (output.IsLargeOutputValue)
            {
                value = "_(see below)_";
            }
            else if (output.IsComputed)
            {
                value = "(known after apply)";
            }
            else
            {
                var rawValue = output.Value?.ToString();
                var formatAttributeName = string.IsNullOrWhiteSpace(output.ReferencedAttributeName)
                    ? output.Name
                    : output.ReferencedAttributeName;

                if (TryFormatJsonOutputValue(rawValue, out var formattedJsonValue))
                {
                    value = formattedJsonValue;
                }
                else
                {
                    value = MarkdownHelpers.FormatAttributeValueTableWithRegistry(
                        formatAttributeName,
                        rawValue,
                        output.ProviderName,
                        context.ValueFormatterRegistry,
                        context.IconProviderRegistry);
                }
            }

            writer.TableRow([
                output.ActionSymbol,
                MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdownTableCell(output.Name)),
                MarkdownHelpers.EscapeMarkdownTableCell(output.Description),
                output.IsSensitive ? "🔒\u00A0Yes" : "No",
                value
            ]);
        }

        writer.BlankLine();

        foreach (var output in outputs)
        {
            // Guard: masked/sensitive values must never be rendered verbatim below the table,
            // regardless of their size. Omitting this guard leaks secrets (Bug 1B).
            // Related issue: docs/issues/fix-sensitive-large-value-rendering/analysis.md
            if (!output.IsLargeOutputValue || output.IsMasked)
            {
                continue;
            }

            writer.Paragraph($"**{MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdownTableCell(output.Name))}:**");
            writer.BlankLine();
            writer.Code(FormatLargeOutputValueContent(output.Value), "json");
            writer.BlankLine();
        }
    }

    /// <summary>
    /// Wraps the base context with report-level scenario hints.
    /// </summary>
    /// <param name="baseContext">Base render context.</param>
    /// <param name="isOutputsFocusedReport">Whether the report is outputs-focused.</param>
    /// <param name="isKnownAfterApplyScenario">Whether the report is the known-after-apply scenario.</param>
    private sealed class ScenarioRenderContext(
        IRenderContext baseContext,
        bool isOutputsFocusedReport,
        bool isKnownAfterApplyScenario) : IRenderContext, IScenarioRenderContext
    {
        /// <inheritdoc />
        public bool ShowSensitive => baseContext.ShowSensitive;

        /// <inheritdoc />
        public bool ShowUnchangedValues => baseContext.ShowUnchangedValues;

        /// <inheritdoc />
        public bool IgnoreAzureIdCaseChanges => baseContext.IgnoreAzureIdCaseChanges;

        /// <inheritdoc />
        public RenderTargets.RenderTarget RenderTarget => baseContext.RenderTarget;

        /// <inheritdoc />
        public RenderTargets.DetailsDisplayMode DetailsDisplayMode => baseContext.DetailsDisplayMode;

        /// <inheritdoc />
        public Oocx.TfPlan2Md.MarkdownGeneration.Services.ValueFormatterRegistry? ValueFormatterRegistry => baseContext.ValueFormatterRegistry;

        /// <inheritdoc />
        public Oocx.TfPlan2Md.MarkdownGeneration.Services.IconProviderRegistry? IconProviderRegistry => baseContext.IconProviderRegistry;

        /// <inheritdoc />
        public bool IsKnownAfterApplyScenario { get; } = isKnownAfterApplyScenario;

        /// <inheritdoc />
        public bool IsOutputsFocusedReport { get; } = isOutputsFocusedReport;
    }

    /// <summary>
    /// Formats a large output value for the below-table code block.
    /// When the value is a <see cref="JsonElement"/> of kind <see cref="JsonValueKind.Object"/>
    /// or <see cref="JsonValueKind.Array"/>, returns indented (pretty-printed) JSON.
    /// When the value is a JSON string that itself contains a JSON object or array, parses and
    /// pretty-prints the embedded JSON.
    /// For all other values, falls back to <see cref="object.ToString"/>.
    /// </summary>
    /// <param name="value">The output value from the plan model.</param>
    /// <returns>A formatted string suitable for display in a fenced code block.</returns>
    /// <remarks>
    /// Uses <see cref="Utf8JsonWriter"/> with <see cref="JsonWriterOptions.Indented"/> = <c>true</c>,
    /// matching the approach already used in <see cref="TryFormatJsonOutputValue"/>.
    /// No new library dependencies are introduced.
    /// Related issue: docs/issues/fix-sensitive-large-value-rendering/analysis.md (Bug 2).
    /// </remarks>
    private static string FormatLargeOutputValueContent(object? value)
    {
        if (value is JsonElement element)
        {
            if (element.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                using var stream = new MemoryStream();
                using (var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    element.WriteTo(jsonWriter);
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }

            // A string-kind element may contain embedded JSON that IsLargeOutputValue detected.
            if (element.ValueKind == JsonValueKind.String)
            {
                var str = element.GetString();
                if (str is not null)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(str);
                        if (doc.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        {
                            using var stream = new MemoryStream();
                            using (var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                            {
                                doc.RootElement.WriteTo(jsonWriter);
                            }

                            return Encoding.UTF8.GetString(stream.ToArray());
                        }
                    }
                    catch (JsonException)
                    {
                        // Not valid JSON — fall through to plain string return.
                    }

                    return str;
                }
            }
        }

        return value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Attempts to format JSON output values as HTML code spans with line breaks for table rendering.
    /// </summary>
    /// <param name="rawValue">Raw output value string.</param>
    /// <param name="formatted">Formatted output value when JSON object/array parsing succeeds.</param>
    /// <returns>True when the value was parsed and formatted as JSON object or array; otherwise false.</returns>
    private static bool TryFormatJsonOutputValue(string? rawValue, out string formatted)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            formatted = string.Empty;
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawValue);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                formatted = string.Empty;
                return false;
            }

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                document.RootElement.WriteTo(writer);
            }

            var pretty = Encoding.UTF8.GetString(stream.ToArray());
            var encoded = System.Net.WebUtility.HtmlEncode(pretty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("  ", "&nbsp;&nbsp;", StringComparison.Ordinal)
                .Replace("\n", "<br>", StringComparison.Ordinal);

            formatted = $"<code>{encoded}</code>";
            return true;
        }
        catch (JsonException)
        {
            formatted = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Renders refactoring summary section.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="operations">Refactoring operations.</param>
    internal static void RenderRefactoring(MarkdownWriter writer, IReadOnlyList<RefactoringOperationModel> operations)
    {
        if (operations.Count == 0)
        {
            return;
        }

        writer.Heading("Refactoring Summary", 2);
        writer.BlankLine();

        writer.Raw("| Operation | Resource | Details | Status |\n");
        writer.Raw("| --------- | -------- | ------- | ------ |\n");

        foreach (var operation in operations)
        {
            var operationText = operation.Operation == "Import" ? "📥\u00A0Import" : "🔀\u00A0Move";
            var resourceText = $"{MarkdownHelpers.EscapeMarkdown(operation.ResourceType)} {MarkdownHelpers.FormatCodeTable(operation.ResourceName)}";
            var detailsText = operation.Operation == "Import"
                ? $"ID: {MarkdownHelpers.FormatImportIdDetails(operation.Details)}"
                : $"From: {MarkdownHelpers.FormatCodeTable(operation.Details)}";

            var statusText = "✅\u00A0Ready";
            if (operation.IsAlreadyApplied)
            {
                statusText = operation.Operation == "Import"
                    ? "⚠️\u00A0Already imported"
                    : "⚠️\u00A0Already moved";
            }

            writer.TableRow([operationText, resourceText, detailsText, statusText]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders filtered-resource informational note.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="model">Report model.</param>
    internal static void RenderFilteredResourceInfo(MarkdownWriter writer, ReportModel model)
    {
        if (!model.IgnoreAzureIdCaseChanges || model.FilteredResourceCount <= 0)
        {
            return;
        }

        var plural = model.FilteredResourceCount > 1;
        var count = model.FilteredResourceCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

        writer.Paragraph(
            $"> ℹ️ {count} resource{(plural ? "s" : string.Empty)} with only filtered changes (e.g. Azure resource ID casing differences) {(plural ? "are" : "is")} not shown. Use `--no-ignore-azure-id-case-changes` to see all changes.");
    }
}
