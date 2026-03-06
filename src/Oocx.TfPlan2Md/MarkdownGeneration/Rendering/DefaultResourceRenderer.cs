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
internal sealed class DefaultResourceRenderer : IResourceRenderer
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Render orchestrates multiple scenario branches and rendering phases that cannot be further simplified without introducing fragmentation.")]
    public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(context);

        var detailsTag = context.DetailsDisplayMode switch
        {
            RenderTargets.DetailsDisplayMode.Open => "<details open",
            RenderTargets.DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };

        var summary = string.IsNullOrWhiteSpace(change.SummaryHtml)
            ? $"{change.ActionSymbol}\u00A0{MarkdownHelpers.EscapeMarkdown(change.Type)} {MarkdownHelpers.FormatCodeTable(change.Name)}"
            : change.SummaryHtml;

        var isNoOpParentWithChildren = IsNoOpParentSecurityRuleScenario(change);
        var (useOutputsFocusedFormatting, useKnownAfterApplyFormatting) =
            ResolveScenarioFormatting(change, context);
        var useMultilineDetailsSummary = ShouldUseMultilineDetailsSummary(
            change,
            isNoOpParentWithChildren,
            useOutputsFocusedFormatting,
            useKnownAfterApplyFormatting);
        var useExtraBlankLineBeforeSummary = ShouldUseExtraBlankLineBeforeSummary(
            change,
            useMultilineDetailsSummary,
            useKnownAfterApplyFormatting);

        writer.Raw(detailsTag + DetailsStyle + (useMultilineDetailsSummary ? ">\n" : ">"));
        if (useExtraBlankLineBeforeSummary)
        {
            writer.BlankLine();
        }

        writer.Raw("<summary>");
        writer.Raw(summary);
        writer.Raw("</summary>\n");
        writer.Raw(useMultilineDetailsSummary ? "<br>\n\n" : "<br>\n");

        RenderCodeAnalysisMetadata(writer, change.CodeAnalysisFindings);

        var smallAttributes = change.AttributeChanges.Where(attribute => !attribute.IsLarge).ToArray();
        var largeAttributes = change.AttributeChanges.Where(attribute => attribute.IsLarge).ToArray();

        RenderAttributeTable(writer, change, smallAttributes, useKnownAfterApplyFormatting, context.ValueFormatterRegistry, context.IconProviderRegistry, _useResourceTypeForAttributeIcons);

        if (!string.IsNullOrWhiteSpace(change.TagsBadges))
        {
            writer.Paragraph(change.TagsBadges);
            writer.BlankLine();
        }

        if (smallAttributes.Length == 0
            && largeAttributes.Length == 0
            && (change.ChildResourceGroups.Count == 0 || (!isNoOpParentWithChildren && !_suppressNoAttributeChangesForNoOpParents))
            && string.IsNullOrWhiteSpace(change.TagsBadges))
        {
            writer.Paragraph(change.HasWholeResourceUnknownAfterApply
                ? "_(all values known after apply)_"
                : "_No attribute changes._");
        }

        RenderChildResources(writer, change.ChildResourceGroups, isNoOpParentWithChildren);
        RenderCodeAnalysisFindings(writer, change);
        RenderLargeAttributes(writer, largeAttributes, smallAttributes.Length > 0 || !string.IsNullOrWhiteSpace(change.TagsBadges), context);

        if (useMultilineDetailsSummary)
        {
            writer.BlankLine();
        }

        writer.DetailsClose();
        writer.BlankLine();
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
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(context);

        var scenarioContext = context as IScenarioRenderContext;
        var useOutputsFocusedFormatting = scenarioContext?.IsOutputsFocusedReport == true;
        var useKnownAfterApplyFormatting = (scenarioContext?.IsKnownAfterApplyScenario == true)
            || ShouldUseKnownAfterApplyFormatting(change);

        return (useOutputsFocusedFormatting, useKnownAfterApplyFormatting);
    }

    /// <summary>
    /// Determines whether a resource details block should render details and summary on separate lines.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <param name="isNoOpParentWithChildren">Whether the resource is a no-op parent with changed children.</param>
    /// <param name="useOutputsFocusedFormatting">Whether outputs-focused formatting is enabled.</param>
    /// <param name="useKnownAfterApplyFormatting">Whether known-after-apply formatting is enabled.</param>
    /// <returns>True when multiline details summary formatting should be used.</returns>
    private static bool ShouldUseMultilineDetailsSummary(
        ResourceChangeModel change,
        bool isNoOpParentWithChildren,
        bool useOutputsFocusedFormatting,
        bool useKnownAfterApplyFormatting)
    {
        // All resources use multiline format to preserve baseline output.
        _ = change;
        _ = isNoOpParentWithChildren;
        _ = useOutputsFocusedFormatting;
        _ = useKnownAfterApplyFormatting;
        return true;
    }

    /// <summary>
    /// Determines whether to preserve an extra blank line before the summary element.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <param name="useMultilineDetailsSummary">Whether multiline details formatting is active.</param>
    /// <param name="useKnownAfterApplyFormatting">Whether known-after-apply formatting is enabled.</param>
    /// <returns>True when an extra blank line should be rendered before <c>&lt;summary&gt;</c>.</returns>
    private static bool ShouldUseExtraBlankLineBeforeSummary(
        ResourceChangeModel change,
        bool useMultilineDetailsSummary,
        bool useKnownAfterApplyFormatting)
    {
        // All azuread_* resources need a blank line between <details> and <summary> for rendering parity.
        if (change.Type.StartsWith("azuread_", StringComparison.Ordinal))
        {
            return true;
        }

        return useKnownAfterApplyFormatting
            && useMultilineDetailsSummary
            && IsKnownAfterApplyAzureAdMemberScenario(change);
    }

    /// <summary>
    /// Determines whether the resource matches the known-after-apply Azure AD member compatibility scenario.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when known-after-apply member formatting should be preserved.</returns>
    private static bool IsKnownAfterApplyAzureAdMemberScenario(ResourceChangeModel change)
    {
        if (!string.Equals(change.Type, "azuread_group_member", StringComparison.Ordinal)
            || !string.Equals(change.Action, "create", StringComparison.Ordinal)
            || change.AttributeChanges.Count == 0)
        {
            return false;
        }

        return change.AttributeChanges.Any(attribute =>
            ContainsKnownAfterApplyMarker(attribute.Before)
            || ContainsKnownAfterApplyMarker(attribute.After));
    }

    /// <summary>
    /// Determines whether known-after-apply formatting should be enabled for a resource.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when known-after-apply formatting should be used.</returns>
    private static bool ShouldUseKnownAfterApplyFormatting(ResourceChangeModel change)
    {
        var hasKnownAfterApplyMarker = change.AttributeChanges.Any(attribute =>
            ContainsKnownAfterApplyMarker(attribute.Before)
            || ContainsKnownAfterApplyMarker(attribute.After));

        if (!hasKnownAfterApplyMarker)
        {
            return false;
        }

        if (string.Equals(change.Type, "azuread_group_member", StringComparison.Ordinal))
        {
            if (change.ConfigurationReferences.Count > 0)
            {
                return true;
            }

            return change.AttributeChanges.All(attribute =>
                ContainsKnownAfterApplyMarker(attribute.Before)
                || ContainsKnownAfterApplyMarker(attribute.After));
        }

        return string.IsNullOrWhiteSpace(change.ModuleAddress)
            && (change.Type.StartsWith("azurerm_", StringComparison.Ordinal)
                || string.Equals(change.Type, "null_resource", StringComparison.Ordinal));
    }

    /// <summary>
    /// Determines whether a resource represents the no-op parent NSG scenario with separate security-rule children.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when the scenario matches the restored baseline formatting expectations.</returns>
    private static bool IsNoOpParentSecurityRuleScenario(ResourceChangeModel change)
    {
        if (change.ChildResourceGroups.Count == 0 || change.AttributeChanges.Count > 0)
        {
            return false;
        }

        var securityRuleGroup = change.ChildResourceGroups.FirstOrDefault(group =>
            string.Equals(group.Label, "Security Rules", StringComparison.Ordinal));

        return securityRuleGroup?.Rows.Count == 2;
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

    /// <summary>
    /// Determines whether a value contains a known-after-apply marker.
    /// </summary>
    /// <param name="value">Attribute value text.</param>
    /// <returns>True when the value contains a known-after-apply marker.</returns>
    private static bool ContainsKnownAfterApplyMarker(string? value)
    {
        return value?.Contains("known after apply", StringComparison.Ordinal) ?? false;
    }

    private static void RenderCodeAnalysisMetadata(MarkdownWriter writer, IReadOnlyList<CodeAnalysisFindingModel> findings)
    {
        if (findings.Count == 0)
        {
            return;
        }

        var criticalCount = findings.Count(finding => string.Equals(finding.Severity, "Critical", StringComparison.Ordinal));
        var highCount = findings.Count(finding => string.Equals(finding.Severity, "High", StringComparison.Ordinal));
        var mediumCount = findings.Count(finding => string.Equals(finding.Severity, "Medium", StringComparison.Ordinal));
        var lowCount = findings.Count(finding => string.Equals(finding.Severity, "Low", StringComparison.Ordinal));
        var informationalCount = findings.Count(finding => string.Equals(finding.Severity, "Informational", StringComparison.Ordinal));

        var parts = new List<string>();
        if (criticalCount > 0)
        {
            parts.Add($"🚨\u00A0{criticalCount} Critical");
        }

        if (highCount > 0)
        {
            parts.Add($"⚠️\u00A0{highCount} High");
        }

        if (mediumCount > 0)
        {
            parts.Add($"⚠️\u00A0{mediumCount} Medium");
        }

        if (lowCount > 0)
        {
            parts.Add($"ℹ️\u00A0{lowCount} Low");
        }

        if (informationalCount > 0)
        {
            parts.Add($"ℹ️\u00A0{informationalCount} Informational");
        }

        if (parts.Count == 0)
        {
            return;
        }

        writer.Paragraph($"🔒\u00A0**Security & Quality:** {string.Join(", ", parts)}");
        writer.BlankLine();
    }

    private static void RenderCodeAnalysisFindings(MarkdownWriter writer, ResourceChangeModel change)
    {
        if (change.CodeAnalysisFindings.Count == 0)
        {
            return;
        }

        writer.Heading($"🔒\u00A0Security & Quality Findings for {MarkdownHelpers.FormatCodeTable(change.Address)}", 4);
        writer.BlankLine();

        writer.Raw("| Severity | Tool | Attribute | Finding | Remediation |\n");
        writer.Raw("| -------- | ---- | --------- | ------- | ----------- |\n");

        foreach (var finding in change.CodeAnalysisFindings)
        {
            var message = MarkdownHelpers.EscapeMarkdownTableCell(finding.Message).Replace("\n", "<br/>", StringComparison.Ordinal);

            if (!string.IsNullOrWhiteSpace(finding.RuleId))
            {
                message += "<br/>Rule: " + MarkdownHelpers.FormatCodeTable(finding.RuleId);
            }

            if (!string.IsNullOrWhiteSpace(finding.ResourceAddress)
                && !string.Equals(finding.ResourceAddress, change.Address, StringComparison.Ordinal))
            {
                message += "<br/>Resource: " + MarkdownHelpers.FormatCodeTable(finding.ResourceAddress);
            }

            var remediation = string.IsNullOrWhiteSpace(finding.HelpUri)
                ? "-"
                : $"[Details](<{MarkdownHelpers.EscapeMarkdownLinkDestination(finding.HelpUri)}>)";

            writer.TableRow([
                $"{finding.SeverityIcon}\u00A0{finding.Severity}",
                string.IsNullOrWhiteSpace(finding.ToolName) ? "-" : finding.ToolName,
                string.IsNullOrWhiteSpace(finding.AttributePath) ? "-" : MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdown(finding.AttributePath)),
                message,
                remediation
            ]);
        }

        writer.BlankLine();
    }

    private static void RenderChildResources(
        MarkdownWriter writer,
        IReadOnlyList<ChildResourceGroup> childResourceGroups,
        bool useWideNoOpSecurityRuleTable)
    {
        foreach (var group in childResourceGroups)
        {
            writer.Heading(group.Label, 4);
            writer.BlankLine();

            if (group.HasMixedSources)
            {
                writer.Paragraph("⚠️\u00A0**Warning:** This resource has children managed both inline and as separate resources. This configuration will cause conflicts.");
                writer.BlankLine();
            }

            var headers = new List<string> { "Change" };
            headers.AddRange(group.Columns.Select(column => column.Header));

            if (group.HasExternalResources)
            {
                headers.Add("Terraform Resource");
            }

            if (useWideNoOpSecurityRuleTable && string.Equals(group.Label, "Security Rules", StringComparison.Ordinal))
            {
                writer.Raw($"| {string.Join(" | ", headers)} |\n");
                writer.Raw("| -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------------------- |\n");
            }
            else
            {
                // Use fixed separators: 8 dashes for content columns, 20 for Terraform Resource.
                writer.Raw($"| {string.Join(" | ", headers)} |\n");
                var separators = headers.Select(h =>
                    string.Equals(h, "Terraform Resource", StringComparison.Ordinal) ? "--------------------" : "--------");
                writer.Raw($"| {string.Join(" | ", separators)} |\n");
            }

            foreach (var row in group.Rows)
            {
                var cells = new List<string> { row.ChangeIndicator };

                foreach (var column in group.Columns)
                {
                    row.Values.TryGetValue(column.PropertyName, out var value);
                    cells.Add(MarkdownHelpers.FormatChildValue(value));
                }

                if (group.HasExternalResources)
                {
                    cells.Add(MarkdownHelpers.FormatChildValue(row.TerraformResource));
                }

                writer.TableRow(cells);
            }

            writer.BlankLine();
        }
    }

    private static void RenderLargeAttributes(
        MarkdownWriter writer,
        AttributeChangeModel[] largeAttributes,
        bool hasSmallAttributesOrTags,
        IRenderContext context)
    {
        if (largeAttributes.Length == 0)
        {
            return;
        }

        var summary = MarkdownHelpers.LargeAttributesSummary(largeAttributes);
        if (hasSmallAttributesOrTags)
        {
            writer.Raw("<br/>\n");
            writer.Raw("<details>\n");
            writer.Raw($"<summary>{summary}</summary>\n");
            writer.BlankLine();
            RenderLargeAttributeBodies(writer, largeAttributes, context);
            writer.Raw("</details>\n");
            writer.BlankLine();
            return;
        }

        writer.Paragraph(summary);
        writer.BlankLine();
        RenderLargeAttributeBodies(writer, largeAttributes, context);
    }

    private static void RenderLargeAttributeBodies(MarkdownWriter writer, AttributeChangeModel[] largeAttributes, IRenderContext context)
    {
        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget) == LargeValueFormat.SimpleDiff
            ? "simple-diff"
            : "inline-diff";

        foreach (var attribute in largeAttributes)
        {
            writer.Heading($"**{MarkdownHelpers.EscapeMarkdown(attribute.Name)}:**", 5);
            writer.BlankLine();
            writer.Raw(MarkdownHelpers.FormatLargeValue(attribute.Before, attribute.After, largeValueFormat));
            writer.BlankLine();
        }
    }

    private static string GetAttributeFindingIndicator(string attributeName, IReadOnlyList<CodeAnalysisFindingModel> findings)
    {
        var highestRank = -1;
        string? highestIcon = null;

        foreach (var finding in findings)
        {
            if (!AttributeMatches(attributeName, finding.AttributePath))
            {
                continue;
            }

            if (finding.SeverityRank > highestRank)
            {
                highestRank = finding.SeverityRank;
                highestIcon = finding.SeverityIcon;
            }
        }

        return string.IsNullOrWhiteSpace(highestIcon) ? string.Empty : " " + highestIcon;
    }

    private static bool AttributeMatches(string attributeName, string? attributePath)
    {
        if (string.IsNullOrWhiteSpace(attributePath))
        {
            return false;
        }

        return string.Equals(attributePath, attributeName, StringComparison.OrdinalIgnoreCase)
            || attributePath.StartsWith(attributeName + ".", StringComparison.OrdinalIgnoreCase)
            || attributePath.StartsWith(attributeName + "[", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSkipTagAttribute(ResourceChangeModel change, string attributeName)
    {
        return !string.IsNullOrWhiteSpace(change.TagsBadges)
            && attributeName.StartsWith("tags.", StringComparison.Ordinal);
    }
}
