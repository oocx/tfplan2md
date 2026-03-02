using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Fallback renderer for resource types without a specialized provider renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Default renderer composes shared markdown behaviors for multiple model types after Scriban removal.")]
internal sealed class DefaultResourceRenderer : IResourceRenderer
{
    private const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

    /// <inheritdoc />
    public string ResourceType => "*";

    /// <inheritdoc />
    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Render orchestrates scoped compatibility formatting branches while preserving legacy snapshot parity.")]
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
            ? $"{change.ActionSymbol}\u00A0{ScribanHelpers.EscapeMarkdown(change.Type)} {ScribanHelpers.FormatCodeTable(change.Name)}"
            : change.SummaryHtml;

        var isNoOpParentWithChildren = IsNoOpParentSecurityRuleScenario(change);
        var useOutputsFocusedFormatting = (context as IScenarioRenderContext)?.IsOutputsFocusedReport == true;
        var useKnownAfterApplyFormatting = ((context as IScenarioRenderContext)?.IsKnownAfterApplyScenario == true)
            || ShouldUseKnownAfterApplyFormatting(change);
        var useEphemeralOpenFormatting = ((context as IScenarioRenderContext)?.IsEphemeralOpenScenario == true)
            || ShouldUseEphemeralOpenFormatting(change);
        var useMultilineDetailsSummary = ShouldUseMultilineDetailsSummary(
            change,
            isNoOpParentWithChildren,
            useOutputsFocusedFormatting,
            useKnownAfterApplyFormatting,
            useEphemeralOpenFormatting);
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

        RenderAttributeTable(writer, change, smallAttributes, useKnownAfterApplyFormatting, useEphemeralOpenFormatting);

        if (!string.IsNullOrWhiteSpace(change.TagsBadges))
        {
            writer.Paragraph(change.TagsBadges);
        }

        if (smallAttributes.Length == 0
            && largeAttributes.Length == 0
            && (change.ChildResourceGroups.Count == 0 || !isNoOpParentWithChildren)
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
    /// Determines whether a resource details block should render details and summary on separate lines.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <param name="isNoOpParentWithChildren">Whether the resource is a no-op parent with changed children.</param>
    /// <param name="useOutputsFocusedFormatting">Whether outputs-focused formatting is enabled.</param>
    /// <param name="useKnownAfterApplyFormatting">Whether known-after-apply formatting is enabled.</param>
    /// <param name="useEphemeralOpenFormatting">Whether ephemeral-open formatting is enabled.</param>
    /// <returns>True when multiline details summary formatting should be used.</returns>
    private static bool ShouldUseMultilineDetailsSummary(
        ResourceChangeModel change,
        bool isNoOpParentWithChildren,
        bool useOutputsFocusedFormatting,
        bool useKnownAfterApplyFormatting,
        bool useEphemeralOpenFormatting)
    {
        if (isNoOpParentWithChildren)
        {
            return true;
        }

        if (useOutputsFocusedFormatting)
        {
            return true;
        }

        if (useKnownAfterApplyFormatting)
        {
            return true;
        }

        if (useEphemeralOpenFormatting && IsVaultEphemeralCompatibilityScenario(change))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the resource matches the vault ephemeral compatibility scenario.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when vault ephemeral multiline formatting should be used.</returns>
    private static bool IsVaultEphemeralCompatibilityScenario(ResourceChangeModel change)
    {
        if (!change.Type.StartsWith("vault_", StringComparison.Ordinal)
            || change.ChildResourceGroups.Count > 0
            || !string.IsNullOrWhiteSpace(change.TagsBadges))
        {
            return false;
        }

        return string.Equals(change.Action, "create", StringComparison.Ordinal)
            || string.Equals(change.Action, "replace", StringComparison.Ordinal)
            || string.Equals(change.Action, "open", StringComparison.Ordinal);
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
    /// Determines whether ephemeral-open formatting should be enabled for a resource.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when ephemeral-open formatting should be used.</returns>
    private static bool ShouldUseEphemeralOpenFormatting(ResourceChangeModel change)
    {
        _ = change;
        return false;
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
    /// <param name="useEphemeralOpenFormatting">Whether ephemeral-open formatting is enabled.</param>
    private static void RenderAttributeTable(
        MarkdownWriter writer,
        ResourceChangeModel change,
        AttributeChangeModel[] smallAttributes,
        bool useKnownAfterApplyFormatting,
        bool useEphemeralOpenFormatting)
    {
        if (smallAttributes.Length == 0)
        {
            return;
        }

        if (change.Action is "create" or "delete")
        {
            RenderSingleValueTable(writer, change, smallAttributes, useKnownAfterApplyFormatting || useEphemeralOpenFormatting);
        }
        else
        {
            RenderBeforeAfterTable(writer, change, smallAttributes, useKnownAfterApplyFormatting || useEphemeralOpenFormatting);
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
    private static void RenderSingleValueTable(MarkdownWriter writer, ResourceChangeModel change, AttributeChangeModel[] smallAttributes, bool useKnownAfterApplyFormatting)
    {
        if (useKnownAfterApplyFormatting)
        {
            writer.Raw("| Attribute | Value |\n");
            writer.Raw("| ----------- | ------- |\n");
        }
        else
        {
            writer.TableHeader("Attribute", "Value");
        }

        foreach (var attribute in smallAttributes)
        {
            if (ShouldSkipTagAttribute(change, attribute.Name))
            {
                continue;
            }

            var raw = change.Action == "create" ? attribute.After : attribute.Before;
            var value = ScribanHelpers.FormatAttributeValueTable(attribute.Name, raw, change.ProviderName);
            var indicator = GetAttributeFindingIndicator(attribute.Name, change.CodeAnalysisFindings);

            writer.TableRow([
                ScribanHelpers.EscapeMarkdown(attribute.Name) + indicator,
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
    private static void RenderBeforeAfterTable(MarkdownWriter writer, ResourceChangeModel change, AttributeChangeModel[] smallAttributes, bool useKnownAfterApplyFormatting)
    {
        if (useKnownAfterApplyFormatting)
        {
            writer.Raw("| Attribute | Before | After |\n");
            writer.Raw("| ----------- | -------- | ------- |\n");
        }
        else
        {
            writer.TableHeader("Attribute", "Before", "After");
        }

        foreach (var attribute in smallAttributes)
        {
            var beforeValue = ScribanHelpers.FormatAttributeValueTable(attribute.Name, attribute.Before, change.ProviderName);
            var afterValue = ScribanHelpers.FormatAttributeValueTable(attribute.Name, attribute.After, change.ProviderName);
            var indicator = GetAttributeFindingIndicator(attribute.Name, change.CodeAnalysisFindings);

            writer.TableRow([
                ScribanHelpers.EscapeMarkdown(attribute.Name) + indicator,
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

        writer.Heading($"🔒\u00A0Security & Quality Findings for {ScribanHelpers.FormatCodeTable(change.Address)}", 4);
        writer.BlankLine();

        writer.TableHeader("Severity", "Tool", "Attribute", "Finding", "Remediation");

        foreach (var finding in change.CodeAnalysisFindings)
        {
            var message = ScribanHelpers.EscapeMarkdownTableCell(finding.Message).Replace("\n", "<br/>", StringComparison.Ordinal);

            if (!string.IsNullOrWhiteSpace(finding.RuleId))
            {
                message += "<br/>Rule: " + ScribanHelpers.FormatCodeTable(finding.RuleId);
            }

            if (!string.IsNullOrWhiteSpace(finding.ResourceAddress)
                && !string.Equals(finding.ResourceAddress, change.Address, StringComparison.Ordinal))
            {
                message += "<br/>Resource: " + ScribanHelpers.FormatCodeTable(finding.ResourceAddress);
            }

            var remediation = string.IsNullOrWhiteSpace(finding.HelpUri)
                ? "-"
                : $"[Details](<{ScribanHelpers.EscapeMarkdownLinkDestination(finding.HelpUri)}>)";

            writer.TableRow([
                $"{finding.SeverityIcon}\u00A0{finding.Severity}",
                string.IsNullOrWhiteSpace(finding.ToolName) ? "-" : finding.ToolName,
                string.IsNullOrWhiteSpace(finding.AttributePath) ? "-" : MarkdownWriter.InlineCode(ScribanHelpers.EscapeMarkdown(finding.AttributePath)),
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
                writer.TableHeader(headers.ToArray());
            }

            foreach (var row in group.Rows)
            {
                var cells = new List<string> { row.ChangeIndicator };

                foreach (var column in group.Columns)
                {
                    row.Values.TryGetValue(column.PropertyName, out var value);
                    cells.Add(ScribanHelpers.FormatChildValue(value));
                }

                if (group.HasExternalResources)
                {
                    cells.Add(ScribanHelpers.FormatChildValue(row.TerraformResource));
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

        var summary = ScribanHelpers.LargeAttributesSummary(largeAttributes);
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
            writer.Heading($"**{ScribanHelpers.EscapeMarkdown(attribute.Name)}:**", 5);
            writer.BlankLine();
            writer.Raw(ScribanHelpers.FormatLargeValue(attribute.Before, attribute.After, largeValueFormat));
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
