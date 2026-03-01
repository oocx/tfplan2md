using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Orchestrates full report rendering from <see cref="ReportModel"/> to markdown text.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
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

        _headerRenderer.Render(writer, model);
        SummaryRenderer.Render(writer, model.Summary, boldTotal: true);
        CodeAnalysisSectionRenderer.RenderSummary(writer, model.CodeAnalysis);
        RenderResourceChanges(writer, model, context);
        CodeAnalysisSectionRenderer.RenderOtherFindings(writer, model.CodeAnalysis);
        RenderRefactoring(writer, model.RefactoringOperations);
        RenderOutputs(writer, model.GlobalOutputs);
        RenderFilteredResourceInfo(writer, model);

        return writer.Build();
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
                : ScribanHelpers.FormatCodeTable(module.ModuleAddress);

            writer.Heading($"📦\u00A0Module: {moduleText}", 3);
            writer.BlankLine();

            foreach (var change in module.Changes)
            {
                var renderer = _resourceRendererRegistry.GetRenderer(change.Type) ?? _defaultResourceRenderer;
                renderer.Render(writer, change, context);
            }

            RenderModuleOutputs(writer, module.Outputs);
        }
    }

    /// <summary>
    /// Renders outputs section for a specific module.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="outputs">Output models.</param>
    private static void RenderModuleOutputs(MarkdownWriter writer, IReadOnlyList<OutputChangeModel> outputs)
    {
        if (outputs.Count == 0)
        {
            return;
        }

        writer.Heading("📤\u00A0Outputs", 4);
        writer.BlankLine();
        RenderOutputTable(writer, outputs);
    }

    /// <summary>
    /// Renders global outputs section.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="outputs">Output models.</param>
    private static void RenderOutputs(MarkdownWriter writer, IReadOnlyList<OutputChangeModel> outputs)
    {
        if (outputs.Count == 0)
        {
            return;
        }

        writer.Heading("📤\u00A0Outputs", 2);
        writer.BlankLine();
        RenderOutputTable(writer, outputs);
    }

    /// <summary>
    /// Renders a markdown outputs table.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="outputs">Output models.</param>
    private static void RenderOutputTable(MarkdownWriter writer, IReadOnlyList<OutputChangeModel> outputs)
    {
        writer.TableHeader("Change", "Name", "Description", "Sensitive", "Value");

        foreach (var output in outputs)
        {
            var value = ScribanHelpers.EscapeMarkdownTableCell(output.Value?.ToString());
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

            writer.TableRow([
                output.ActionSymbol,
                MarkdownWriter.InlineCode(ScribanHelpers.EscapeMarkdownTableCell(output.Name)),
                ScribanHelpers.EscapeMarkdownTableCell(output.Description),
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

            writer.Paragraph($"**{MarkdownWriter.InlineCode(ScribanHelpers.EscapeMarkdownTableCell(output.Name))}:**");
            writer.BlankLine();
            writer.Code(output.Value?.ToString() ?? string.Empty, "json");
            writer.BlankLine();
        }
    }

    /// <summary>
    /// Renders refactoring summary section.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="operations">Refactoring operations.</param>
    private static void RenderRefactoring(MarkdownWriter writer, IReadOnlyList<RefactoringOperationModel> operations)
    {
        if (operations.Count == 0)
        {
            return;
        }

        writer.Heading("Refactoring Summary", 2);
        writer.BlankLine();

        writer.TableHeader("Operation", "Resource", "Details", "Status");

        foreach (var operation in operations)
        {
            var operationText = operation.Operation == "Import" ? "📥\u00A0Import" : "🔀\u00A0Move";
            var resourceText = $"{ScribanHelpers.EscapeMarkdown(operation.ResourceType)} {ScribanHelpers.FormatCodeTable(operation.ResourceName)}";
            var detailsText = operation.Operation == "Import"
                ? $"ID: {ScribanHelpers.FormatImportIdDetails(operation.Details)}"
                : $"From: {ScribanHelpers.FormatCodeTable(operation.Details)}";

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
    private static void RenderFilteredResourceInfo(MarkdownWriter writer, ReportModel model)
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
