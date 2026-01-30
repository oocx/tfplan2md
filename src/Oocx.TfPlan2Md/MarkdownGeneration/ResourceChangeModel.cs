using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Represents a single resource change for template rendering.
/// </summary>
public class ResourceChangeModel
{
    /// <summary>
    /// Gets the full Terraform address of the resource.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets or sets the module address containing this resource.
    /// </summary>
    public string? ModuleAddress { get; set; }

    /// <summary>
    /// Gets the resource type (e.g., "aws_s3_bucket").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the resource name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the provider name (e.g., "aws", "azurerm").
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Gets the action being performed (e.g., "create", "update", "delete").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the symbol representing the action (e.g., "+", "~", "-").
    /// </summary>
    public required string ActionSymbol { get; init; }

    /// <summary>
    /// Gets the list of attribute changes for this resource.
    /// </summary>
    public required IReadOnlyList<AttributeChangeModel> AttributeChanges { get; init; }

    /// <summary>
    /// Gets the raw JSON representation of the resource state before the change.
    /// Used by resource-specific templates for semantic diffing.
    /// </summary>
    public object? BeforeJson { get; init; }

    /// <summary>
    /// Gets the raw JSON representation of the resource state after the change.
    /// Used by resource-specific templates for semantic diffing.
    /// </summary>
    public object? AfterJson { get; init; }

    /// <summary>
    /// Gets or sets the paths to attributes that triggered replacement (from Terraform plan replace_paths).
    /// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<object>>? ReplacePaths { get; set; }

    /// <summary>
    /// Gets or sets the human-readable summary of the resource change for quick scanning in templates.
    /// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the precomputed HTML summary line content for rich summary rendering (includes action, type, name, and context values with HTML code spans).
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    public string? SummaryHtml { get; set; }

    /// <summary>
    /// Gets or sets the precomputed changed-attributes summary for update operations (e.g., "2 🔧 attr1, attr2"). Empty for non-update actions.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    public string? ChangedAttributesSummary { get; set; }

    /// <summary>
    /// Gets or sets the precomputed tags badge string for create/delete actions (e.g., "**🏷️ Tags:** `env: prod` `owner: ops`"). Null when no tags or on updates.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    public string? TagsBadges { get; set; }

    /// <summary>
    /// Gets or sets the precomputed view model for azurerm_network_security_group resources.
    /// Related feature: docs/features/026-template-rendering-simplification/specification.md.
    /// </summary>
    public NetworkSecurityGroupViewModel? NetworkSecurityGroup { get; set; }

    /// <summary>
    /// Gets or sets the precomputed view model for azurerm_firewall_network_rule_collection resources.
    /// Related feature: docs/features/026-template-rendering-simplification/specification.md.
    /// </summary>
    public FirewallNetworkRuleCollectionViewModel? FirewallNetworkRuleCollection { get; set; }

    /// <summary>
    /// Gets or sets the precomputed view model for azurerm_role_assignment resources.
    /// Related feature: docs/features/026-template-rendering-simplification/specification.md.
    /// </summary>
    public RoleAssignmentViewModel? RoleAssignment { get; set; }

    /// <summary>
    /// Gets or sets the precomputed view model for azuredevops_variable_group resources.
    /// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
    /// </summary>
    public VariableGroupViewModel? VariableGroup { get; set; }

    /// <summary>
    /// Gets or sets the code analysis findings associated with this resource.
    /// Related feature: docs/features/056-static-analysis-integration/specification.md.
    /// </summary>
    public IReadOnlyList<CodeAnalysisFindingModel> CodeAnalysisFindings { get; set; } = [];
}
