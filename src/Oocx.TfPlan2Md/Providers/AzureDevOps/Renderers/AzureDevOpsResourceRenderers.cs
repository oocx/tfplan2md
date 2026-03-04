using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Renderers;

/// <summary>
/// Base class for Azure DevOps resource renderers that currently delegate to the default renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal abstract class AzureDevOpsDelegatingRenderer(string resourceType) : IResourceRenderer
{
    /// <summary>
    /// Default fallback renderer.
    /// </summary>
    private readonly DefaultResourceRenderer _defaultRenderer = new();

    /// <inheritdoc />
    public string ResourceType { get; } = resourceType;

    /// <inheritdoc />
    public virtual void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        _defaultRenderer.Render(writer, change, context);
    }
}

/// <summary>
/// Renders <c>azuredevops_variable_group</c> resources using structured tables.
/// Masks secret variable values to prevent sensitive data exposure in reports.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class VariableGroupRenderer : AzureDevOpsDelegatingRenderer
{
    /// <summary>
    /// Shared details block style matching all other structured renderers.
    /// </summary>
    private const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableGroupRenderer"/> class.
    /// </summary>
    public VariableGroupRenderer()
        : base("azuredevops_variable_group")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableGroupRenderer"/> class.
    /// The <paramref name="largeValueFormat"/> parameter is accepted for API compatibility but
    /// is not stored — the format is derived from the render context at render time.
    /// </summary>
    /// <param name="largeValueFormat">Accepted for API compatibility; derived from context at render time.</param>
    public VariableGroupRenderer(LargeValueFormat largeValueFormat)
        : base("azuredevops_variable_group")
    {
        _ = largeValueFormat;
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        // Fall back to default renderer when raw parsing data is unavailable.
        if (change.ResourceChange is null)
        {
            base.Render(writer, change, context);
            return;
        }

        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget);
        var viewModel = VariableGroupViewModelFactory.Build(change.ResourceChange, change.ProviderName, largeValueFormat);

        var detailsTag = context.DetailsDisplayMode switch
        {
            DetailsDisplayMode.Open => "<details open",
            DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };

        var summary = change.SummaryHtml
            ?? $"{change.ActionSymbol}\u00A0{MarkdownHelpers.EscapeMarkdown(change.Type)} <b>{MarkdownHelpers.FormatCodeSummary(change.Name)}</b>";

        writer.Raw(detailsTag + DetailsStyle + ">\n");
        writer.Raw("<summary>");
        writer.Raw(summary);
        writer.Raw("</summary>\n");
        writer.Raw("<br>\n\n");

        if (!string.IsNullOrWhiteSpace(viewModel.Name))
        {
            writer.Paragraph($"**Variable Group:** <code>{MarkdownHelpers.EscapeMarkdown(viewModel.Name)}</code>");
        }

        writer.BlankLine();

        if (!string.IsNullOrWhiteSpace(viewModel.Description))
        {
            writer.Paragraph($"**Description:** <code>{MarkdownHelpers.EscapeMarkdown(viewModel.Description)}</code>");
        }

        if (viewModel.KeyVaultBlocks.Count > 0)
        {
            writer.Heading("Key Vault Integration", 4);
            writer.BlankLine();
            writer.TableHeader("Name", "Service Endpoint ID", "Search Depth");
            foreach (var kv in viewModel.KeyVaultBlocks)
            {
                writer.TableRow([kv.Name, kv.ServiceEndpointId, kv.SearchDepth]);
            }

            writer.BlankLine();
        }

        // Render variables using the appropriate table format based on the action.
        if (viewModel.VariableChanges.Count > 0)
        {
            RenderVariableChangesTable(writer, viewModel.VariableChanges);
        }
        else if (viewModel.AfterVariables.Count > 0)
        {
            RenderVariablesTable(writer, viewModel.AfterVariables, heading: "Variables");
        }
        else if (viewModel.BeforeVariables.Count > 0)
        {
            RenderVariablesTable(writer, viewModel.BeforeVariables, heading: "Variables (being deleted)");
        }

        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Renders a create or delete variable table with no change column.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="variables">Variable rows to render.</param>
    /// <param name="heading">Section heading text.</param>
    private static void RenderVariablesTable(MarkdownWriter writer, IReadOnlyList<VariableRowViewModel> variables, string heading)
    {
        writer.Heading(heading, 4);
        writer.BlankLine();
        writer.Raw("| Name | Value | Enabled | Content Type | Expires |\n");
        writer.Raw("| ---- | ----- | ------- | ------------ | ------- |\n");
        foreach (var v in variables)
        {
            writer.TableRow([v.Name, v.Value, v.Enabled, v.ContentType, v.Expires]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders an update/replace variable change table with a change indicator column.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="changes">Variable change rows to render.</param>
    private static void RenderVariableChangesTable(MarkdownWriter writer, IReadOnlyList<VariableChangeRowViewModel> changes)
    {
        writer.Heading("Variables", 4);
        writer.BlankLine();
        writer.Raw("| Change | Name | Value | Enabled | Content Type | Expires |\n");
        writer.Raw("| ------ | ---- | ----- | ------- | ------------ | ------- |\n");
        foreach (var vc in changes)
        {
            writer.TableRow([vc.ChangeIcon, vc.Name, vc.Value, vc.Enabled, vc.ContentType, vc.Expires]);
        }

        writer.BlankLine();
    }
}

/// <summary>
/// Renders <c>azuredevops_build_definition</c> resources.
/// </summary>
internal sealed class BuildDefinitionRenderer : AzureDevOpsDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuildDefinitionRenderer"/> class.
    /// </summary>
    public BuildDefinitionRenderer()
        : base("azuredevops_build_definition")
    {
    }
}
