using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Renderers;

/// <summary>
/// Base class for AzureRM resource renderers that currently delegate to the default renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal abstract class AzureRmDelegatingRenderer(string resourceType) : IResourceRenderer
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
/// Renders <c>azurerm_role_assignment</c> resources.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Compatibility renderer intentionally combines provider-specific summary/model logic to preserve legacy snapshot output.")]
internal sealed class RoleAssignmentRenderer : AzureRmDelegatingRenderer
{
    private const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

    /// <summary>
    /// Mapper used for principal display name resolution.
    /// </summary>
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Optional scope formatter for enriched Azure scope display names.
    /// </summary>
    private readonly EnrichedAzureScopeFormatter? _scopeFormatter;

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
    public RoleAssignmentRenderer(IPrincipalMapper principalMapper, EnrichedAzureScopeFormatter? scopeFormatter = null)
        : base("azurerm_role_assignment")
    {
        _principalMapper = principalMapper;
        _scopeFormatter = scopeFormatter;
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        if (!ShouldUseCompatibilityRoleAssignmentRendering(change))
        {
            base.Render(writer, change, context);
            return;
        }

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change.ResourceChange!,
            change.Action,
            change.AttributeChanges,
            _principalMapper,
            _scopeFormatter);

        if (string.IsNullOrWhiteSpace(viewModel.SummaryText) || viewModel.SmallAttributes.Count == 0)
        {
            base.Render(writer, change, context);
            return;
        }

        var detailsTag = context.DetailsDisplayMode switch
        {
            RenderTargets.DetailsDisplayMode.Open => "<details open",
            RenderTargets.DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };

        var summary = $"{change.ActionSymbol}\u00A0{ScribanHelpers.EscapeMarkdown(change.Type)} <b>{ScribanHelpers.FormatCodeSummary(change.Name)}</b> — {viewModel.SummaryText}";

        writer.Raw(detailsTag + DetailsStyle + ">\n");
        writer.Raw("<summary>");
        writer.Raw(summary);
        writer.Raw("</summary>\n");
        writer.Raw("<br>\n\n");

        writer.Raw("| Attribute | Value |\n");
        writer.Raw("| ----------- | ------- |\n");

        var scopeAttributes = viewModel.SmallAttributes
            .Where(attribute => string.Equals(attribute.Name, "scope", StringComparison.Ordinal))
            .ToArray();

        if (scopeAttributes.Length == 0)
        {
            scopeAttributes = [new RoleAssignmentAttributeViewModel { Name = "scope", After = string.Empty, Before = string.Empty }];
        }

        foreach (var attribute in scopeAttributes)
        {
            var value = string.Equals(change.Action, "delete", StringComparison.Ordinal)
                ? attribute.Before
                : attribute.After;

            writer.TableRow([
                ScribanHelpers.EscapeMarkdown(attribute.Name),
                value ?? string.Empty
            ]);
        }

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Determines whether role assignment compatibility rendering should be used.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when the legacy role-assignment fallback shape is required.</returns>
    private static bool ShouldUseCompatibilityRoleAssignmentRendering(ResourceChangeModel change)
    {
        return change.ResourceChange is not null
            && string.Equals(change.Action, "create", StringComparison.Ordinal)
            && change.AttributeChanges.Count == 0
            && change.ChildResourceGroups.Count == 0
            && string.IsNullOrWhiteSpace(change.TagsBadges)
            && change.CodeAnalysisFindings.Count == 0;
    }
}

/// <summary>
/// Renders <c>azurerm_network_security_group</c> resources.
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
}

/// <summary>
/// Renders <c>azurerm_firewall_network_rule_collection</c> resources.
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
}

/// <summary>
/// Renders <c>azurerm_firewall_application_rule_collection</c> resources.
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
}
