using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Attribute table rendering helpers shared by default and provider-extended resource renderers.
/// </summary>
internal sealed partial class DefaultResourceRenderer
{
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
    /// Renders an optional collapsed single-value section using the default renderer's value and large-attribute formatting.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="title">Accessible summary text for the collapsed section.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="attributes">Display-ready secondary attributes.</param>
    /// <param name="context">Global render context.</param>
    internal static void RenderAdditionalAttributeSection(
        MarkdownWriter writer,
        string title,
        ResourceChangeModel change,
        IReadOnlyList<AttributeChangeModel> attributes,
        IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(context);

        if (attributes.Count == 0)
        {
            return;
        }

        writer.DetailsOpen(MarkdownHelpers.EscapeMarkdown(title));
        writer.BlankLine();

        var smallAttributes = attributes.Where(attribute => !attribute.IsLarge).ToArray();
        var largeAttributes = attributes.Where(attribute => attribute.IsLarge).ToArray();
        if (smallAttributes.Length > 0)
        {
            RenderSingleValueTable(
                writer,
                change,
                smallAttributes,
                useKnownAfterApplyFormatting: false,
                context.ValueFormatterRegistry,
                context.IconProviderRegistry,
                firstColumnHeader: "Setting",
                preferAfterValue: true);
            writer.BlankLine();
        }

        RenderLargeAttributes(writer, largeAttributes, smallAttributes.Length > 0, context);
        writer.DetailsClose();
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
    /// <param name="firstColumnHeader">Label for the attribute-name column.</param>
    /// <param name="preferAfterValue">When <c>true</c>, displays the desired value regardless of the resource action.</param>
    private static void RenderSingleValueTable(
        MarkdownWriter writer,
        ResourceChangeModel change,
        AttributeChangeModel[] smallAttributes,
        bool useKnownAfterApplyFormatting,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry,
        bool useResourceTypeForAttributeIcons = false,
        string firstColumnHeader = "Attribute",
        bool preferAfterValue = false)
    {
        // Use fixed-width separators to preserve baseline output for all cases.
        _ = useKnownAfterApplyFormatting;
        writer.Raw($"| {firstColumnHeader} | Value |\n");
        writer.Raw("| ----------- | ------- |\n");

        foreach (var attribute in smallAttributes)
        {
            if (ShouldSkipTagAttribute(change, attribute.Name))
            {
                continue;
            }

            var raw = preferAfterValue || change.Action == "create" ? attribute.After : attribute.Before;
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
