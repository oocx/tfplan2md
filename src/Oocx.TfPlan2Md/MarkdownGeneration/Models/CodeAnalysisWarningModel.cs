namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Source of a warning surfaced through the unified Warnings rendering pipeline.
/// Related feature: docs/features/122-terraform-1-15-support/adr-004-deprecation-warnings-via-existing-pipeline.md.
/// </summary>
public enum CodeAnalysisWarningSource
{
    /// <summary>
    /// Warning emitted when SARIF code-analysis input could not be processed.
    /// </summary>
    SarifProcessingFailure = 0,

    /// <summary>
    /// Warning emitted for a Terraform 1.15+ deprecated variable or output that
    /// is referenced by the plan.
    /// </summary>
    PlanDeprecation
}

/// <summary>
/// Represents a warning generated while processing code analysis inputs or plan
/// configuration metadata. Both SARIF processing failures and plan-level
/// deprecation notices flow through this single model so the renderer never has
/// to know about the source-specific origin.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// Related feature: docs/features/122-terraform-1-15-support/adr-004-deprecation-warnings-via-existing-pipeline.md.
/// </summary>
public sealed class CodeAnalysisWarningModel
{
    /// <summary>
    /// Gets the SARIF file path that produced the warning. Null for non-SARIF
    /// sources (such as plan-level deprecations).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the originating warning source. Defaults to
    /// <see cref="CodeAnalysisWarningSource.SarifProcessingFailure"/> so existing
    /// SARIF call sites need no behavioural change.
    /// </summary>
    public CodeAnalysisWarningSource Source { get; init; } = CodeAnalysisWarningSource.SarifProcessingFailure;

    /// <summary>
    /// Gets the kind of subject this warning relates to (e.g. <c>"variable"</c> or
    /// <c>"output"</c>) for <see cref="CodeAnalysisWarningSource.PlanDeprecation"/>;
    /// null for SARIF-sourced warnings.
    /// </summary>
    public string? SubjectKind { get; init; }

    /// <summary>
    /// Gets the name of the subject (e.g. variable or output name) for
    /// <see cref="CodeAnalysisWarningSource.PlanDeprecation"/>; null for SARIF-sourced warnings.
    /// </summary>
    public string? SubjectName { get; init; }
}
