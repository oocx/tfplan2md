using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Render-time model for a single Terraform 1.14+ action invocation
/// (lifecycle-triggered or invoke-triggered, immediate or deferred).
/// Wraps the parsed <see cref="ActionInvocation"/> so the renderer can stay
/// generic across providers and action types.
/// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md.
/// </summary>
internal sealed class ActionInvocationModel
{
    /// <summary>
    /// Gets the parsed action invocation entry from the plan JSON.
    /// </summary>
    public required ActionInvocation Invocation { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry comes from
    /// <c>deferred_action_invocations[]</c>. Drives the deferred badge.
    /// </summary>
    public bool IsDeferred { get; init; }
}
