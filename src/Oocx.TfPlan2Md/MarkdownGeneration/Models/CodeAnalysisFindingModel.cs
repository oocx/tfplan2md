namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a code analysis finding prepared for report rendering.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
public sealed class CodeAnalysisFindingModel
{
    /// <summary>
    /// Gets the normalized severity label.
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Gets the severity icon used in the report.
    /// </summary>
    public required string SeverityIcon { get; init; }

    /// <summary>
    /// Gets the severity rank used for sorting (higher means more severe).
    /// </summary>
    public required int SeverityRank { get; init; }

    /// <summary>
    /// Gets the finding message text.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the rule identifier for the finding.
    /// </summary>
    public string? RuleId { get; init; }

    /// <summary>
    /// Gets the remediation link for the finding.
    /// </summary>
    public string? HelpUri { get; init; }

    /// <summary>
    /// Gets the tool name that produced the finding.
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Gets the Terraform resource address when available.
    /// </summary>
    public string? ResourceAddress { get; init; }

    /// <summary>
    /// Gets the Terraform module address when available.
    /// </summary>
    public string? ModuleAddress { get; init; }

    /// <summary>
    /// Gets the attribute path when available.
    /// </summary>
    public string? AttributePath { get; init; }
}
