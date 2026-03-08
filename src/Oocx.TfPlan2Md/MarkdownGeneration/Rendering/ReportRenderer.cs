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
        var isNoOpParentChildScenario = model.ModuleChanges.Count == 1
            && model.Summary.Total == 2
            && model.ModuleChanges[0].Changes.Any(change =>
                string.Equals(change.Action, "no-op", StringComparison.Ordinal)
                && change.ChildResourceGroups.Count > 0);
        var useWideSummarySeparators = isOutputsFocusedReport
            || isNoOpParentChildScenario;
        var effectiveContext = new ScenarioRenderContext(
            context,
            isOutputsFocusedReport,
            false);

        _headerRenderer.Render(writer, model);
        RenderSummary(writer, model.Summary, useWideSummarySeparators);
        CodeAnalysisSectionRenderer.RenderSummary(writer, model.CodeAnalysis);
        RenderResourceChanges(writer, model, effectiveContext);
        CodeAnalysisSectionRenderer.RenderOtherFindings(writer, model.CodeAnalysis);
        RenderRefactoring(writer, model.RefactoringOperations);
        RenderOutputs(writer, model.GlobalOutputs, effectiveContext);
        RenderFilteredResourceInfo(writer, model);

        return writer.Build();
    }

    /// <summary>
    /// Renders the summary section using the canonical style for the current report shape.
    /// Delegates to <see cref="SummaryRenderer.Render"/> for all report shapes.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="summary">Summary model.</param>
    /// <param name="useWideSeparators">Parameter retained for API compatibility; the canonical renderer handles all shapes.</param>
    private static void RenderSummary(MarkdownWriter writer, SummaryModel summary, bool useWideSeparators)
    {
        _ = useWideSeparators;
        SummaryRenderer.Render(writer, summary, boldTotal: true);
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
            if (output.IsLargeOutputValue)
            {
                value = "_(see below)_";
            }
            else if (output.IsMasked)
            {
                value = "(sensitive value)";
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
            if (!output.IsLargeOutputValue)
            {
                continue;
            }

            writer.Paragraph($"**{MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdownTableCell(output.Name))}:**");
            writer.BlankLine();
            writer.Code(output.Value?.ToString() ?? string.Empty, "json");
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
