using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

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
    /// Related feature: docs/features/011-replacement-reasons-and-summaries/specification.md.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<object>>? ReplacePaths { get; set; }

    /// <summary>
    /// Gets or sets the human-readable summary of the resource change for quick scanning in templates.
    /// Related feature: docs/features/011-replacement-reasons-and-summaries/specification.md.
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
    /// Gets or sets the child resource groups rendered inline within this parent.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </summary>
    public IReadOnlyList<ChildResourceGroup> ChildResourceGroups { get; set; } = [];

    /// <summary>
    /// Gets or sets the code analysis findings associated with this resource.
    /// Related feature: docs/features/056-static-analysis-integration/specification.md.
    /// </summary>
    public IReadOnlyList<CodeAnalysisFindingModel> CodeAnalysisFindings { get; set; } = [];

    /// <summary>
    /// Gets the import identifier when this resource is managed via an import block.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public string? ImportId { get; init; }

    /// <summary>
    /// Gets the previous address when this resource is moved by a refactoring block.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public string? MovedFromAddress { get; init; }

    /// <summary>
    /// Gets a value indicating whether the refactoring operation has already been applied.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public bool IsRefactoringAlreadyApplied { get; init; }

    /// <summary>
    /// Gets the sensitivity map for the resource state before the change.
    /// Contains the <c>before_sensitive</c> structure from the Terraform plan,
    /// used by provider templates to mask sensitive values in rendered output.
    /// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </summary>
    public object? BeforeSensitive { get; init; }

    /// <summary>
    /// Gets the sensitivity map for the resource state after the change.
    /// Contains the <c>after_sensitive</c> structure from the Terraform plan,
    /// used by provider templates to mask sensitive values in rendered output.
    /// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </summary>
    public object? AfterSensitive { get; init; }

    /// <summary>
    /// Gets or sets the original resource change from parsing.
    /// Used by resource model mappers to create provider-specific view models.
    /// Internal only - not exposed to templates.
    /// </summary>
    internal Parsing.ResourceChange? ResourceChange { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Terraform marked the whole resource as unknown after apply.
    /// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
    /// </summary>
    public bool HasWholeResourceUnknownAfterApply { get; set; }

    /// <summary>
    /// Gets or sets configuration references grouped by top-level attribute name.
    /// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
    /// </summary>
    internal IReadOnlyDictionary<string, IReadOnlyList<string>> ConfigurationReferences { get; set; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
}
