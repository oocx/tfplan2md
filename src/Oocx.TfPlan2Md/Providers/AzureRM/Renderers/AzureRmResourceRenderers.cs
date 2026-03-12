using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Renderers;

/// <summary>
/// Base class for AzureRM resource renderers that delegate to <see cref="DefaultResourceRenderer"/> when
/// specialized rendering is not available.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal abstract class AzureRmDelegatingRenderer(string resourceType) : IResourceRenderer
{
    /// <summary>Shared details block style applied to all resource renderers.</summary>
    protected const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

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

    /// <summary>
    /// Writes the opening details tag and summary line for a resource change block.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="change">The resource change model providing summary and finding data.</param>
    /// <param name="context">The render context providing display mode settings.</param>
    internal static void WriteDetailsOpen(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
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
    }
}

/// <summary>
/// Renders <c>azurerm_role_assignment</c> resources with enriched principal, role, and scope display.
/// Supports create, update, delete, and replace scenarios via <see cref="RoleAssignmentViewModelFactory"/>.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class RoleAssignmentRenderer : AzureRmDelegatingRenderer
{
    /// <summary>Mapper used for principal display name resolution.</summary>
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>Optional scope formatter for enriched Azure scope display names.</summary>
    private readonly EnrichedAzureScopeFormatter? _scopeFormatter;

    /// <summary>Resolver used to format Azure role definition names for the current run.</summary>
    private readonly IRoleDefinitionResolver _roleDefinitionResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleAssignmentRenderer"/> class.
    /// </summary>
    public RoleAssignmentRenderer()
        : this(new NullPrincipalMapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleAssignmentRenderer"/> class.
    /// </summary>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="scopeFormatter">Optional formatter for scope display enrichment.</param>
    /// <param name="roleDefinitionResolver">Optional run-scoped resolver for role definition names.</param>
    public RoleAssignmentRenderer(
        IPrincipalMapper principalMapper,
        EnrichedAzureScopeFormatter? scopeFormatter = null,
        IRoleDefinitionResolver? roleDefinitionResolver = null)
        : base("azurerm_role_assignment")
    {
        _principalMapper = principalMapper;
        _scopeFormatter = scopeFormatter;
        _roleDefinitionResolver = roleDefinitionResolver ?? AzureRoleDefinitionResolver.CreateBuiltIn();
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        if (change.ResourceChange is null)
        {
            base.Render(writer, change, context);
            return;
        }

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change.ResourceChange,
            change.Action,
            change.AttributeChanges,
            _principalMapper,
            _scopeFormatter,
            _roleDefinitionResolver);

        // Fall back to default renderer when the view model cannot produce meaningful content.
        if (viewModel.SmallAttributes.Count == 0 && viewModel.LargeAttributes.Count == 0)
        {
            base.Render(writer, change, context);
            return;
        }

        WriteEnrichedDetailsOpen(writer, change, context, viewModel.SummaryText);

        // Render description as a plain paragraph before the attribute table, if present.
        if (!string.IsNullOrWhiteSpace(viewModel.Description))
        {
            writer.Raw(viewModel.Description + "\n\n");
        }

        var isDelete = string.Equals(change.Action, "delete", StringComparison.OrdinalIgnoreCase);
        var isUpdateOrReplace = !string.Equals(change.Action, "create", StringComparison.OrdinalIgnoreCase) && !isDelete;

        if (isUpdateOrReplace)
        {
            writer.Raw("| Attribute | Before | After |\n");
            writer.Raw("| ----------- | -------- | ------- |\n");
            foreach (var attr in viewModel.SmallAttributes)
            {
                writer.TableRow([MarkdownHelpers.EscapeMarkdown(attr.Name), string.IsNullOrWhiteSpace(attr.Before) ? "-" : attr.Before, attr.After ?? string.Empty]);
            }
        }
        else
        {
            writer.Raw("| Attribute | Value |\n");
            writer.Raw("| ----------- | ------- |\n");
            foreach (var attr in viewModel.SmallAttributes)
            {
                var value = isDelete ? attr.Before : attr.After;
                writer.TableRow([MarkdownHelpers.EscapeMarkdown(attr.Name), value ?? string.Empty]);
            }
        }

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Writes the opening details tag with an enriched summary line that appends role-assignment-specific
    /// "principal → role on scope" text to the generic action/type/name summary.
    /// Related feature: docs/features/107-remove-scriban/specification.md.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="change">The resource change model providing generic summary and finding data.</param>
    /// <param name="context">The render context providing display mode settings.</param>
    /// <param name="summaryText">Role-assignment-specific enriched summary fragment produced by the view model factory.</param>
    private static void WriteEnrichedDetailsOpen(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context, string? summaryText)
    {
        // Build the base prefix from scratch rather than using change.SummaryHtml, because
        // SummaryHtml for role assignments may already include provider-specific content
        // (e.g. Azure resource name badge, change-count badges) that is not part of the
        // plain action/type/name prefix the snapshot expects.
        var baseSummary = $"{change.ActionSymbol}\u00A0{MarkdownHelpers.EscapeMarkdown(change.Type)} <b>{MarkdownHelpers.FormatCodeSummary(change.Name)}</b>";
        var enrichedSummary = !string.IsNullOrWhiteSpace(summaryText)
            ? $"{baseSummary} — {summaryText}"
            : baseSummary;

        var detailsTag = context.DetailsDisplayMode switch
        {
            DetailsDisplayMode.Open => "<details open",
            DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };

        writer.Raw(detailsTag + DetailsStyle + ">\n");
        writer.Raw("<summary>");
        writer.Raw(enrichedSummary);
        writer.Raw("</summary>\n");
        writer.Raw("<br>\n\n");
    }
}

/// <summary>
/// Renders <c>azurerm_network_security_group</c> resources using a structured security rule table.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class NsgRenderer : AzureRmDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NsgRenderer"/> class.
    /// </summary>
    public NsgRenderer()
        : base("azurerm_network_security_group")
    {
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        if (change.ResourceChange is null)
        {
            base.Render(writer, change, context);
            return;
        }

        var mergedSecurityRules = NsgMergedSecurityRulesRenderer.GetMergedSecurityRulesGroup(change);
        if (mergedSecurityRules is not null)
        {
            NsgMergedSecurityRulesRenderer.Render(writer, change, context, mergedSecurityRules);
            return;
        }

        var viewModel = NetworkSecurityGroupViewModelFactory.Build(change.ResourceChange, change.ProviderName);

        var isCreate = string.Equals(change.Action, "create", StringComparison.OrdinalIgnoreCase);
        var isDelete = string.Equals(change.Action, "delete", StringComparison.OrdinalIgnoreCase);

        // Fall back to default renderer when no rule data is available (e.g. all values are computed).
        if (viewModel.RuleChanges.Count == 0 && viewModel.AfterRules.Count == 0 && viewModel.BeforeRules.Count == 0)
        {
            base.Render(writer, change, context);
            return;
        }

        WriteDetailsOpen(writer, change, context);

        // Heading is always "Security Rules" regardless of action type to preserve established output.
        writer.Heading("Security Rules", 4);
        writer.BlankLine();
        writer.TableHeader("Change", "Name", "Priority", "Direction", "Access", "Protocol", "Source Addresses", "Source Ports", "Destination Addresses", "Destination Ports", "Description");

        if (isCreate)
        {
            foreach (var rule in viewModel.AfterRules)
            {
                writer.TableRow([ActionIcons.Add, rule.Name, rule.Priority, rule.Direction, rule.Access, rule.Protocol, rule.SourceAddresses, rule.SourcePorts, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
            }
        }
        else if (isDelete)
        {
            foreach (var rule in viewModel.BeforeRules)
            {
                writer.TableRow([ActionIcons.Delete, rule.Name, rule.Priority, rule.Direction, rule.Access, rule.Protocol, rule.SourceAddresses, rule.SourcePorts, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
            }
        }
        else
        {
            foreach (var rule in viewModel.RuleChanges)
            {
                writer.TableRow([rule.Change, rule.Name, rule.Priority, rule.Direction, rule.Access, rule.Protocol, rule.SourceAddresses, rule.SourcePorts, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
            }
        }

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }
}

/// <summary>
/// Renders <c>azurerm_firewall_network_rule_collection</c> resources using a structured rule table.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class FirewallNetworkRuleRenderer : AzureRmDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallNetworkRuleRenderer"/> class.
    /// </summary>
    public FirewallNetworkRuleRenderer()
        : base("azurerm_firewall_network_rule_collection")
    {
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        if (change.ResourceChange is null)
        {
            base.Render(writer, change, context);
            return;
        }

        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget);
        var viewModel = FirewallNetworkRuleCollectionViewModelFactory.Build(change.ResourceChange, change.ProviderName, largeValueFormat);

        var isCreate = string.Equals(change.Action, "create", StringComparison.OrdinalIgnoreCase);
        var isDelete = string.Equals(change.Action, "delete", StringComparison.OrdinalIgnoreCase);

        // Fall back to default renderer when no rule data is available (e.g. all values are computed).
        if (viewModel.RuleChanges.Count == 0 && viewModel.AfterRules.Count == 0 && viewModel.BeforeRules.Count == 0)
        {
            base.Render(writer, change, context);
            return;
        }

        WriteDetailsOpen(writer, change, context);
        WriteFirewallCollectionHeader(writer, viewModel.Name, viewModel.Priority, viewModel.Action);

        if (isCreate)
        {
            writer.Heading("Network Rules", 4);
            writer.BlankLine();
            writer.Raw("| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |\n");
            writer.Raw("| ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |\n");
            foreach (var rule in viewModel.AfterRules)
            {
                writer.TableRow([rule.Name, rule.Protocols, rule.SourceAddresses, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
            }
        }
        else if (isDelete)
        {
            writer.Heading("Rules (being deleted)", 4);
            writer.BlankLine();
            writer.Raw("| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |\n");
            writer.Raw("| ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |\n");
            foreach (var rule in viewModel.BeforeRules)
            {
                writer.TableRow([rule.Name, rule.Protocols, rule.SourceAddresses, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
            }
        }
        else
        {
            writer.Heading("Rule Changes", 4);
            writer.BlankLine();
            writer.Raw("| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |\n");
            writer.Raw("| -------- | ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |\n");
            foreach (var rule in viewModel.RuleChanges)
            {
                writer.TableRow([rule.Change, rule.Name, rule.Protocols, rule.SourceAddresses, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
            }
        }

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Writes the collection header line with name, priority, and action.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="name">Collection name.</param>
    /// <param name="priority">Formatted priority value.</param>
    /// <param name="action">Formatted action value (Allow/Deny with icons).</param>
    private static void WriteFirewallCollectionHeader(MarkdownWriter writer, string? name, string? priority, string? action)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(name))
        {
            parts.Add($"**Collection:** `{MarkdownHelpers.EscapeMarkdown(name)}`");
        }

        if (!string.IsNullOrEmpty(priority))
        {
            parts.Add($"**Priority:** `{MarkdownHelpers.EscapeMarkdown(priority)}`");
        }

        if (!string.IsNullOrEmpty(action))
        {
            parts.Add($"**Action:** {action}");
        }

        if (parts.Count > 0)
        {
            writer.Paragraph(string.Join(" | ", parts));
        }
    }
}

/// <summary>
/// Renders <c>azurerm_firewall_application_rule_collection</c> resources using a structured rule table.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class FirewallAppRuleRenderer : AzureRmDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallAppRuleRenderer"/> class.
    /// </summary>
    public FirewallAppRuleRenderer()
        : base("azurerm_firewall_application_rule_collection")
    {
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        if (change.ResourceChange is null)
        {
            base.Render(writer, change, context);
            return;
        }

        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget);
        var viewModel = FirewallApplicationRuleCollectionViewModelFactory.Build(change.ResourceChange, change.ProviderName, largeValueFormat);

        var isCreate = string.Equals(change.Action, "create", StringComparison.OrdinalIgnoreCase);
        var isDelete = string.Equals(change.Action, "delete", StringComparison.OrdinalIgnoreCase);

        // Fall back to default renderer when no rule data is available (e.g. all values are computed).
        if (viewModel.RuleChanges.Count == 0 && viewModel.AfterRules.Count == 0 && viewModel.BeforeRules.Count == 0)
        {
            base.Render(writer, change, context);
            return;
        }

        WriteDetailsOpen(writer, change, context);
        WriteAppCollectionHeader(writer, viewModel.Name, viewModel.Priority, viewModel.Action);

        if (isCreate)
        {
            writer.Heading("Application Rules", 4);
            writer.BlankLine();
            writer.TableHeader("Rule Name", "Protocols", "Source Addresses", "Source IP Groups", "Target FQDNs", "FQDN Tags", "Description");
            foreach (var rule in viewModel.AfterRules)
            {
                writer.TableRow([rule.Name, rule.Protocols, rule.SourceAddresses, rule.SourceIpGroups, rule.TargetFqdns, rule.FqdnTags, rule.Description]);
            }
        }
        else if (isDelete)
        {
            writer.Heading("Rules (being deleted)", 4);
            writer.BlankLine();
            writer.TableHeader("Rule Name", "Protocols", "Source Addresses", "Source IP Groups", "Target FQDNs", "FQDN Tags", "Description");
            foreach (var rule in viewModel.BeforeRules)
            {
                writer.TableRow([rule.Name, rule.Protocols, rule.SourceAddresses, rule.SourceIpGroups, rule.TargetFqdns, rule.FqdnTags, rule.Description]);
            }
        }
        else
        {
            writer.Heading("Rule Changes", 4);
            writer.BlankLine();
            writer.TableHeader("Change", "Rule Name", "Protocols", "Source Addresses", "Source IP Groups", "Target FQDNs", "FQDN Tags", "Description");
            foreach (var rule in viewModel.RuleChanges)
            {
                writer.TableRow([rule.Change, rule.Name, rule.Protocols, rule.SourceAddresses, rule.SourceIpGroups, rule.TargetFqdns, rule.FqdnTags, rule.Description]);
            }
        }

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Writes the application rule collection header line with name, priority, and action.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="name">Collection name.</param>
    /// <param name="priority">Formatted priority value.</param>
    /// <param name="action">Formatted action value (Allow/Deny with icons).</param>
    private static void WriteAppCollectionHeader(MarkdownWriter writer, string? name, string? priority, string? action)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(name))
        {
            parts.Add($"**Collection:** `{MarkdownHelpers.EscapeMarkdown(name)}`");
        }

        if (!string.IsNullOrEmpty(priority))
        {
            parts.Add($"**Priority:** `{MarkdownHelpers.EscapeMarkdown(priority)}`");
        }

        if (!string.IsNullOrEmpty(action))
        {
            parts.Add($"**Action:** {action}");
        }

        if (parts.Count > 0)
        {
            writer.Paragraph(string.Join(" | ", parts));
        }
    }
}

