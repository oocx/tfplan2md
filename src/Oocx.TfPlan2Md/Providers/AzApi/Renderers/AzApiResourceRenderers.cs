using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

namespace Oocx.TfPlan2Md.Providers.AzApi.Renderers;

/// <summary>
/// Base class for AzApi resource renderers.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal abstract class AzApiRendererBase(string resourceType) : IResourceRenderer
{
    private const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

    /// <summary>
    /// Provider used for type formatting in AzAPI metadata.
    /// </summary>
    private const string AzApiProviderName = "azapi";
    private const string OutputPropertyName = "output";
    private const string OutputValuesHeading = "Output Values";
    private const string ReplaceAction = "replace";

    /// <summary>
    /// Provider used for Azure ID-aware value formatting.
    /// </summary>
    private const string AzureRmProviderName = "azurerm";

    /// <inheritdoc />
    public string ResourceType { get; } = resourceType;

    /// <inheritdoc />
    public abstract void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context);

    /// <summary>
    /// Opens the details/summary block for a resource.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="change">Resource change.</param>
    /// <param name="context">Render context.</param>
    protected static void RenderDetailsOpen(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        var detailsTag = context.DetailsDisplayMode switch
        {
            RenderTargets.DetailsDisplayMode.Open => "<details open",
            RenderTargets.DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };

        var summary = string.IsNullOrWhiteSpace(change.SummaryHtml)
            ? $"{change.ActionSymbol}\u00A0{ScribanHelpers.EscapeMarkdown(change.Type)} {ScribanHelpers.FormatCodeTable(change.Name)}"
            : change.SummaryHtml;

        writer.Raw(detailsTag + DetailsStyle + ">\n");
        writer.Raw("<summary>");
        writer.Raw(summary);
        writer.Raw("</summary>\n");
        writer.Raw("<br>\n\n");
    }

    /// <summary>
    /// Renders common metadata section.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="metadata">AzAPI metadata.</param>
    /// <param name="context">Render context.</param>
    protected static void RenderTypeAndDocs(MarkdownWriter writer, AzApiMetadata metadata, IRenderContext context)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Type))
        {
            var typeText = ScribanHelpers.FormatAttributeValueTableWithRegistryResource(
                "type",
                metadata.Type,
                AzApiProviderName,
                null,
                context.ValueFormatterRegistry,
                context.IconProviderRegistry);
            writer.Paragraph($"**Type:** {typeText}");
        }

        var docUrl = AzureApiDocumentationMapper.GetDocumentationUrl(metadata.Type);
        if (!string.IsNullOrWhiteSpace(docUrl))
        {
            writer.BlankLine();
            writer.Paragraph($"📚 [View API Documentation]({ScribanHelpers.EscapeMarkdown(docUrl)})");
        }
    }

    /// <summary>
    /// Renders metadata table for azapi_resource.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="metadata">Extracted metadata.</param>
    /// <param name="context">Render context.</param>
    protected static void RenderResourceMetadataTable(MarkdownWriter writer, AzApiMetadata metadata, IRenderContext context)
    {
        if (string.IsNullOrWhiteSpace(metadata.Name)
            && string.IsNullOrWhiteSpace(metadata.ParentId)
            && string.IsNullOrWhiteSpace(metadata.Location))
        {
            return;
        }

        writer.BlankLine();
        writer.Raw("| Attribute | Value |\n");
        writer.Raw("|-----------|-------|\n");

        if (!string.IsNullOrWhiteSpace(metadata.Name))
        {
            var formatted = ScribanHelpers.FormatAttributeValueTableWithRegistryResource(
                "name",
                metadata.Name,
                AzApiProviderName,
                null,
                context.ValueFormatterRegistry,
                context.IconProviderRegistry);
            writer.Raw($"| name | {formatted} |\n");
        }

        if (!string.IsNullOrWhiteSpace(metadata.ParentId))
        {
            var formatted = ScribanHelpers.FormatAttributeValueTableWithRegistryResource(
                "parent_id",
                metadata.ParentId,
                AzureRmProviderName,
                null,
                context.ValueFormatterRegistry,
                context.IconProviderRegistry);
            writer.Raw($"| parent_id | {formatted} |\n");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Location))
        {
            var formatted = ScribanHelpers.FormatAttributeValueTableWithRegistryResource(
                "location",
                metadata.Location,
                AzApiProviderName,
                null,
                context.ValueFormatterRegistry,
                context.IconProviderRegistry);
            writer.Raw($"| location | {formatted} |\n");
        }
    }

    /// <summary>
    /// Renders resource_id table for azapi_update_resource.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="metadata">Extracted metadata.</param>
    /// <param name="context">Render context.</param>
    protected static void RenderResourceIdTable(MarkdownWriter writer, AzApiMetadata metadata, IRenderContext context)
    {
        if (string.IsNullOrWhiteSpace(metadata.ResourceId))
        {
            return;
        }

        writer.BlankLine();
        writer.Raw("| Attribute | Value |\n");
        writer.Raw("|-----------|-------|\n");

        var formatted = ScribanHelpers.FormatAttributeValueTableWithRegistryResource(
            "resource_id",
            metadata.ResourceId,
            AzureRmProviderName,
            null,
            context.ValueFormatterRegistry,
            context.IconProviderRegistry);
        writer.Raw($"| resource_id | {formatted} |\n");
    }

    /// <summary>
    /// Renders tags badge section in multiline format.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="metadata">Extracted metadata.</param>
    protected static void RenderTags(MarkdownWriter writer, AzApiMetadata metadata)
    {
        if (metadata.Tags is null || metadata.Tags.Count == 0)
        {
            return;
        }

        writer.BlankLine();
        writer.Paragraph("**🏷️\u00A0Tags:**");
        foreach (var (key, value) in metadata.Tags)
        {
            writer.Paragraph($" `{ScribanHelpers.EscapeMarkdown(key)}: {ScribanHelpers.EscapeMarkdown(value)}`");
        }
    }

    /// <summary>
    /// Renders AzAPI body section according to action.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="change">Resource change.</param>
    /// <param name="context">Render context.</param>
    protected static void RenderBody(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        AzApiMetadataExtractor.TryGetProperty(change.AfterJson, "body", out var afterBody);
        AzApiMetadataExtractor.TryGetProperty(change.BeforeJson, "body", out var beforeBody);

        AzApiMetadataExtractor.TryGetProperty(change.AfterSensitive, "body", out var afterSensitiveBody);
        AzApiMetadataExtractor.TryGetProperty(change.BeforeSensitive, "body", out var beforeSensitiveBody);

        if (change.Action is "create" or ReplaceAction)
        {
            if (afterBody is null)
            {
                writer.Paragraph("*Body: (empty)*");
                return;
            }

            var heading = change.Action == ReplaceAction ? "Body (replacing existing resource)" : "Body";
            AzApiBodyRenderer.RenderCreateDeleteBody(writer, heading, afterBody, afterSensitiveBody, context);
            return;
        }

        if (change.Action == "update")
        {
            if (beforeBody is null || afterBody is null)
            {
                writer.Paragraph("*Body: (no changes or missing data)*");
                return;
            }

            AzApiBodyRenderer.RenderUpdateBody(writer, "Body Changes", beforeBody, afterBody, beforeSensitiveBody, afterSensitiveBody, context);
            return;
        }

        if (change.Action == "delete")
        {
            if (beforeBody is null)
            {
                writer.Paragraph("*Body: (empty)*");
                return;
            }

            AzApiBodyRenderer.RenderCreateDeleteBody(writer, "Body (being deleted)", beforeBody, beforeSensitiveBody, context);
        }
    }

    /// <summary>
    /// Renders output-values section.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="change">Resource change.</param>
    /// <param name="context">Render context.</param>
    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Action-specific output rendering mirrors historical azapi template behavior.")]
    [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Preserves template-compatible branch behavior for create/update/delete output-values rendering.")]
    protected static void RenderOutputValues(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        AzApiMetadataExtractor.TryGetProperty(change.BeforeJson, OutputPropertyName, out var beforeOutput);
        AzApiMetadataExtractor.TryGetProperty(change.AfterJson, OutputPropertyName, out var afterOutput);

        AzApiMetadataExtractor.TryGetProperty(change.BeforeSensitive, OutputPropertyName, out var beforeSensitiveOutput);
        AzApiMetadataExtractor.TryGetProperty(change.AfterSensitive, OutputPropertyName, out var afterSensitiveOutput);

        AzApiMetadataExtractor.TryGetProperty(change.AfterUnknown, OutputPropertyName, out var outputUnknown);

        var hasBeforeOutput = beforeOutput is not null;
        var hasAfterOutput = afterOutput is not null;

        if (!hasBeforeOutput && !hasAfterOutput)
        {
            return;
        }

        if (change.Action is "create" or ReplaceAction)
        {
            if (IsUnknown(outputUnknown))
            {
                if (hasBeforeOutput && change.Action == ReplaceAction)
                {
                    AzApiBodyRenderer.RenderCreateDeleteBody(writer, OutputValuesHeading, beforeOutput!, beforeSensitiveOutput, context);
                    writer.Paragraph("_Output values are not known until after apply._");
                }

                return;
            }

            if (hasBeforeOutput && hasAfterOutput)
            {
                AzApiBodyRenderer.RenderUpdateBody(writer, OutputValuesHeading, beforeOutput!, afterOutput!, beforeSensitiveOutput, afterSensitiveOutput, context);
                return;
            }

            if (hasAfterOutput)
            {
                AzApiBodyRenderer.RenderCreateDeleteBody(writer, OutputValuesHeading, afterOutput!, afterSensitiveOutput, context);
            }

            return;
        }

        if (change.Action == "update")
        {
            if (hasBeforeOutput && hasAfterOutput)
            {
                AzApiBodyRenderer.RenderUpdateBody(writer, OutputValuesHeading, beforeOutput!, afterOutput!, beforeSensitiveOutput, afterSensitiveOutput, context);
            }
            else if (hasAfterOutput)
            {
                AzApiBodyRenderer.RenderCreateDeleteBody(writer, OutputValuesHeading, afterOutput!, afterSensitiveOutput, context);
            }
            else
            {
                AzApiBodyRenderer.RenderCreateDeleteBody(writer, OutputValuesHeading, beforeOutput!, beforeSensitiveOutput, context);
            }

            return;
        }

        if (change.Action == "delete" && hasBeforeOutput)
        {
            AzApiBodyRenderer.RenderCreateDeleteBody(writer, OutputValuesHeading, beforeOutput!, beforeSensitiveOutput, context);
        }
    }

    /// <summary>
    /// Checks whether an output-unknown marker is present.
    /// </summary>
    /// <param name="outputUnknown">Output unknown marker.</param>
    /// <returns><c>true</c> when unknown.</returns>
    private static bool IsUnknown(object? outputUnknown)
    {
        return outputUnknown switch
        {
            true => true,
            bool value => value,
            _ => false
        };
    }
}

/// <summary>
/// Renders <c>azapi_resource</c> resources.
/// </summary>
internal sealed class AzApiResourceRenderer : AzApiRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzApiResourceRenderer"/> class.
    /// </summary>
    public AzApiResourceRenderer()
        : base("azapi_resource")
    {
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        RenderDetailsOpen(writer, change, context);

        var metadata = AzApiMetadataExtractor.Extract(change);
        RenderTypeAndDocs(writer, metadata, context);
        RenderResourceMetadataTable(writer, metadata, context);
        RenderTags(writer, metadata);
        RenderBody(writer, change, context);
        RenderOutputValues(writer, change, context);

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }
}

/// <summary>
/// Renders <c>azapi_update_resource</c> resources.
/// </summary>
internal sealed class AzApiUpdateResourceRenderer : AzApiRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzApiUpdateResourceRenderer"/> class.
    /// </summary>
    public AzApiUpdateResourceRenderer()
        : base("azapi_update_resource")
    {
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        RenderDetailsOpen(writer, change, context);

        var metadata = AzApiMetadataExtractor.Extract(change);
        RenderTypeAndDocs(writer, metadata, context);
        RenderResourceIdTable(writer, metadata, context);
        RenderBody(writer, change, context);
        RenderOutputValues(writer, change, context);

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }
}

/// <summary>
/// Renders pseudo output-values rows represented by <c>azapi_output_values</c>.
/// </summary>
internal sealed class AzApiOutputValuesRenderer : AzApiRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzApiOutputValuesRenderer"/> class.
    /// </summary>
    public AzApiOutputValuesRenderer()
        : base("azapi_output_values")
    {
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        RenderDetailsOpen(writer, change, context);
        RenderOutputValues(writer, change, context);

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }
}
