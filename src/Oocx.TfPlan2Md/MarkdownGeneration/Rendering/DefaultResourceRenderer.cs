using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Fallback renderer for resource types without a specialized provider renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Default renderer composes shared markdown behaviors for multiple model types in the pure C# rendering pipeline.")]
internal sealed partial class DefaultResourceRenderer : IResourceRenderer
{
    private const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

    private readonly bool _useResourceTypeForAttributeIcons;
    private readonly bool _suppressNoAttributeChangesForNoOpParents;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResourceRenderer"/> class.
    /// </summary>
    /// <param name="useResourceTypeForAttributeIcons">
    /// When <c>true</c>, resource-type-specific icon lookup is used for attribute tables.
    /// </param>
    /// <param name="suppressNoAttributeChangesForNoOpParents">
    /// When <c>true</c>, the "_No attribute changes._" message is suppressed when child resource groups are present.
    /// </param>
    public DefaultResourceRenderer(bool useResourceTypeForAttributeIcons = false, bool suppressNoAttributeChangesForNoOpParents = false)
    {
        _useResourceTypeForAttributeIcons = useResourceTypeForAttributeIcons;
        _suppressNoAttributeChangesForNoOpParents = suppressNoAttributeChangesForNoOpParents;
    }

    /// <inheritdoc />
    public string ResourceType => "*";

    /// <inheritdoc />
    public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(context);

        var detailsTag = ResolveDetailsTag(change, context);
        var summary = ResolveSummary(change);
        var policy = DefaultResourceRenderPolicy.Resolve(change, context);

        WriteDetailsHeader(writer, detailsTag, summary, policy);
        RenderCodeAnalysisMetadata(writer, change.CodeAnalysisFindings);
        RenderInlineRelevantAttributeAnnotations(writer, change);

        var smallAttributes = change.AttributeChanges.Where(attribute => !attribute.IsLarge).ToArray();
        var largeAttributes = change.AttributeChanges.Where(attribute => attribute.IsLarge).ToArray();

        RenderAttributeTable(writer, change, smallAttributes, policy.UseKnownAfterApplyFormatting, context.ValueFormatterRegistry, context.IconProviderRegistry, _useResourceTypeForAttributeIcons);
        WriteTagsBadgesSection(writer, change);
        WriteNoChangesMessage(writer, change, smallAttributes, largeAttributes, policy);

        RenderChildResources(writer, change.ChildResourceGroups);
        RenderCodeAnalysisFindings(writer, change);
        RenderLargeAttributes(writer, largeAttributes, smallAttributes.Length > 0 || !string.IsNullOrWhiteSpace(change.TagsBadges), context);

        RenderInlineActions(writer, change, context);

        if (policy.UseMultilineDetailsSummary)
        {
            writer.BlankLine();
        }

        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Renders the inline "🎬 Actions" H4 sub-section listing every Terraform 1.14+
    /// action invocation attached to this resource via <c>lifecycle_action_trigger</c>.
    /// Section is silent when the resource has no attached actions, preserving
    /// pre-feature snapshots for plans without action_invocations.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="context">Render context.</param>
    private static void RenderInlineActions(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        if (change.Actions.Count == 0)
        {
            return;
        }

        writer.Heading("🎬\u00A0Actions", 4);
        writer.BlankLine();
        foreach (var action in change.Actions)
        {
            ActionInvocationSectionRenderer.Render(writer, action, context);
        }
    }

    /// <summary>Determines the appropriate details opening tag.</summary>
    private static string ResolveDetailsTag(ResourceChangeModel change, IRenderContext context)
    {
        return context.DetailsDisplayMode switch
        {
            RenderTargets.DetailsDisplayMode.Open => "<details open",
            RenderTargets.DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };
    }

    /// <summary>Resolves the HTML summary content for the details block.</summary>
    private static string ResolveSummary(ResourceChangeModel change)
    {
        return string.IsNullOrWhiteSpace(change.SummaryHtml)
            ? $"{change.ActionSymbol}\u00A0{MarkdownHelpers.EscapeMarkdown(change.Type)} {MarkdownHelpers.FormatCodeTable(change.Name)}"
            : change.SummaryHtml;
    }

    /// <summary>Writes the opening details block including the summary line.</summary>
    private static void WriteDetailsHeader(MarkdownWriter writer, string detailsTag, string summary, DefaultResourceRenderPolicyResult policy)
    {
        writer.Raw(detailsTag + DetailsStyle + (policy.UseMultilineDetailsSummary ? ">\n" : ">"));
        if (policy.UseExtraBlankLineBeforeSummary)
        {
            writer.BlankLine();
        }

        writer.Raw("<summary>");
        writer.Raw(summary);
        writer.Raw("</summary>\n");
        writer.Raw(policy.UseMultilineDetailsSummary ? "<br>\n\n" : "<br>\n");
    }

    /// <summary>Writes the tags badges paragraph when present.</summary>
    private static void WriteTagsBadgesSection(MarkdownWriter writer, ResourceChangeModel change)
    {
        if (!string.IsNullOrWhiteSpace(change.TagsBadges))
        {
            writer.Paragraph(change.TagsBadges);
            writer.BlankLine();
        }
    }

    /// <summary>Writes the "No attribute changes" or "all values known after apply" message when applicable.</summary>
    private void WriteNoChangesMessage(
        MarkdownWriter writer,
        ResourceChangeModel change,
        AttributeChangeModel[] smallAttributes,
        AttributeChangeModel[] largeAttributes,
        DefaultResourceRenderPolicyResult policy)
    {
        if (smallAttributes.Length == 0
            && largeAttributes.Length == 0
            && (change.ChildResourceGroups.Count == 0 || (!policy.IsNoOpParentWithChildren && !_suppressNoAttributeChangesForNoOpParents))
            && string.IsNullOrWhiteSpace(change.TagsBadges))
        {
            writer.Paragraph(change.HasWholeResourceUnknownAfterApply
                ? "_(all values known after apply)_"
                : "_No attribute changes._");
        }
    }

    /// <summary>
    /// Resolves report-scenario formatting flags from context overrides and resource heuristics.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <param name="context">Current render context.</param>
    /// <returns>
    /// A tuple containing, in order, outputs-focused formatting and known-after-apply formatting flags.
    /// </returns>
    internal static (bool UseOutputsFocusedFormatting, bool UseKnownAfterApplyFormatting)
        ResolveScenarioFormatting(ResourceChangeModel change, IRenderContext context)
    {
        var policy = DefaultResourceRenderPolicy.Resolve(change, context);
        return (policy.UseOutputsFocusedFormatting, policy.UseKnownAfterApplyFormatting);
    }

    /// <summary>
    /// Renders attribute changes table according to action semantics.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="smallAttributes">Non-large attribute changes.</param>
    /// <param name="useKnownAfterApplyFormatting">Whether known-after-apply formatting is enabled.</param>
    /// <param name="valueFormatterRegistry">Optional value formatter registry for attribute value enrichment.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry for resource-type-aware icon resolution.</param>
    /// <param name="useResourceTypeForAttributeIcons">When <c>true</c>, passes the resource type for icon lookup.</param>
    private static void RenderAttributeTable(
        MarkdownWriter writer,
        ResourceChangeModel change,
        AttributeChangeModel[] smallAttributes,
        bool useKnownAfterApplyFormatting,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry,
        bool useResourceTypeForAttributeIcons = false)
    {
        if (smallAttributes.Length == 0)
        {
            return;
        }

        if (change.Action is "create" or "delete")
        {
            RenderSingleValueTable(writer, change, smallAttributes, useKnownAfterApplyFormatting, valueFormatterRegistry, iconProviderRegistry, useResourceTypeForAttributeIcons);
        }
        else
        {
            RenderBeforeAfterTable(writer, change, smallAttributes, useKnownAfterApplyFormatting, valueFormatterRegistry, iconProviderRegistry, useResourceTypeForAttributeIcons);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders a two-column attribute table for create/delete actions.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="smallAttributes">Non-large attribute changes.</param>
    /// <param name="useKnownAfterApplyFormatting">Whether known-after-apply formatting is enabled.</param>
    /// <param name="valueFormatterRegistry">Optional value formatter registry for attribute value enrichment.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry for resource-type-aware icon resolution.</param>
    /// <param name="useResourceTypeForAttributeIcons">When <c>true</c>, passes the resource type for icon lookup.</param>
    private static void RenderSingleValueTable(MarkdownWriter writer, ResourceChangeModel change, AttributeChangeModel[] smallAttributes, bool useKnownAfterApplyFormatting, ValueFormatterRegistry? valueFormatterRegistry, IconProviderRegistry? iconProviderRegistry, bool useResourceTypeForAttributeIcons = false)
    {
        // Use fixed-width separators to preserve baseline output for all cases.
        _ = useKnownAfterApplyFormatting;
        writer.Raw("| Attribute | Value |\n");
        writer.Raw("| ----------- | ------- |\n");

        foreach (var attribute in smallAttributes)
        {
            if (ShouldSkipTagAttribute(change, attribute.Name))
            {
                continue;
            }

            var raw = change.Action == "create" ? attribute.After : attribute.Before;
            var resourceType = useResourceTypeForAttributeIcons ? change.Type : null;
            var value = MarkdownHelpers.FormatAttributeValueTableWithRegistryResource(
                attribute.Name, raw, change.ProviderName, resourceType, valueFormatterRegistry, iconProviderRegistry);
            var indicator = GetAttributeFindingIndicator(attribute.Name, change.CodeAnalysisFindings);

            writer.TableRow([
                MarkdownHelpers.EscapeMarkdown(attribute.Name) + indicator,
                value
            ]);
        }
    }

    /// <summary>
    /// Renders a three-column before/after attribute table for update-like actions.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="smallAttributes">Non-large attribute changes.</param>
    /// <param name="useKnownAfterApplyFormatting">Whether known-after-apply formatting is enabled.</param>
    /// <param name="valueFormatterRegistry">Optional value formatter registry for attribute value enrichment.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry for resource-type-aware icon resolution.</param>
    /// <param name="useResourceTypeForAttributeIcons">When <c>true</c>, passes the resource type for icon lookup.</param>
    private static void RenderBeforeAfterTable(MarkdownWriter writer, ResourceChangeModel change, AttributeChangeModel[] smallAttributes, bool useKnownAfterApplyFormatting, ValueFormatterRegistry? valueFormatterRegistry, IconProviderRegistry? iconProviderRegistry, bool useResourceTypeForAttributeIcons = false)
    {
        // Use fixed-width separators to preserve baseline output for all cases.
        _ = useKnownAfterApplyFormatting;
        writer.Raw("| Attribute | Before | After |\n");
        writer.Raw("| ----------- | -------- | ------- |\n");

        foreach (var attribute in smallAttributes)
        {
            var resourceType = useResourceTypeForAttributeIcons ? change.Type : null;
            var beforeValue = MarkdownHelpers.FormatAttributeValueTableWithRegistryResource(
                attribute.Name, attribute.Before, change.ProviderName, resourceType, valueFormatterRegistry, iconProviderRegistry);
            var afterValue = MarkdownHelpers.FormatAttributeValueTableWithRegistryResource(
                attribute.Name, attribute.After, change.ProviderName, resourceType, valueFormatterRegistry, iconProviderRegistry);
            var indicator = GetAttributeFindingIndicator(attribute.Name, change.CodeAnalysisFindings);

            writer.TableRow([
                MarkdownHelpers.EscapeMarkdown(attribute.Name) + indicator,
                string.IsNullOrEmpty(beforeValue) ? "-" : beforeValue,
                string.IsNullOrEmpty(afterValue) ? "-" : afterValue
            ]);
        }
    }

}
