using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

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
internal sealed class RoleAssignmentRenderer : AzureRmDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleAssignmentRenderer"/> class.
    /// </summary>
    public RoleAssignmentRenderer()
        : base("azurerm_role_assignment")
    {
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
