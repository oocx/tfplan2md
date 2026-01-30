using System.Collections.Generic;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Represents aggregated code analysis data parsed from SARIF inputs.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisModel
{
    /// <summary>
    /// Gets the tools that produced the findings.
    /// </summary>
    /// <value>
    /// The ordered list of tool metadata, or an empty list when no tool data is available.
    /// </value>
    public required IReadOnlyList<CodeAnalysisTool> Tools { get; init; }

    /// <summary>
    /// Gets the findings extracted from the SARIF results.
    /// </summary>
    /// <value>
    /// The ordered list of findings, or an empty list when no findings are present.
    /// </value>
    public required IReadOnlyList<CodeAnalysisFinding> Findings { get; init; }
}

/// <summary>
/// Describes a static analysis tool and its version metadata.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisTool
{
    /// <summary>
    /// Gets the tool display name.
    /// </summary>
    /// <value>The tool name as reported by SARIF.</value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the tool version when available.
    /// </summary>
    /// <value>The tool version string, or <c>null</c> when unavailable.</value>
    public string? Version { get; init; }
}

/// <summary>
/// Represents a single code analysis finding derived from SARIF results.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisFinding
{
    /// <summary>
    /// Gets the rule identifier for the finding.
    /// </summary>
    /// <value>The rule identifier, or <c>null</c> when none is provided.</value>
    public string? RuleId { get; init; }

    /// <summary>
    /// Gets the human-readable finding message.
    /// </summary>
    /// <value>The finding message text, or an empty string when unavailable.</value>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the remediation link for the finding.
    /// </summary>
    /// <value>The remediation URL, or <c>null</c> when not provided.</value>
    public string? HelpUri { get; init; }

    /// <summary>
    /// Gets the SARIF severity level for the finding.
    /// </summary>
    /// <value>The raw SARIF level value, or <c>null</c> when not provided.</value>
    public string? Level { get; init; }

    /// <summary>
    /// Gets the numeric security severity when provided by the tool.
    /// </summary>
    /// <value>The security severity score, or <c>null</c> when not provided.</value>
    public double? SecuritySeverity { get; init; }

    /// <summary>
    /// Gets the SARIF rank value when provided by the tool.
    /// </summary>
    /// <value>The SARIF rank value, or <c>null</c> when not provided.</value>
    public double? Rank { get; init; }

    /// <summary>
    /// Gets the logical locations associated with the finding.
    /// </summary>
    /// <value>The list of logical locations, or an empty list when none are provided.</value>
    public required IReadOnlyList<CodeAnalysisLocation> Locations { get; init; }

    /// <summary>
    /// Gets the tool name that produced the finding.
    /// </summary>
    /// <value>The tool name, or <c>null</c> when not available.</value>
    public string? ToolName { get; init; }
}

/// <summary>
/// Represents a logical location extracted from a SARIF result.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisLocation
{
    /// <summary>
    /// Gets the fully qualified name for the logical location.
    /// </summary>
    /// <value>The fully qualified name, or <c>null</c> when unavailable.</value>
    public string? FullyQualifiedName { get; init; }
}
