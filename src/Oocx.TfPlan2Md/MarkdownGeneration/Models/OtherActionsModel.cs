namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Holds action invocations that do not attach to any rendered resource: invoke-mode
/// actions and lifecycle actions whose triggering resource is not present in
/// <c>resource_changes[]</c>. Rendered as the "🎬 Other Actions" H2 section.
/// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md.
/// </summary>
internal sealed class OtherActionsModel
{
    /// <summary>
    /// Gets the actions invoked via <c>terraform plan -invoke=…</c>
    /// (carry an <c>invoke_action_trigger</c>).
    /// </summary>
    public IReadOnlyList<ActionInvocationModel> InvokeActions { get; init; } = [];

    /// <summary>
    /// Gets lifecycle-triggered actions whose triggering resource has no
    /// matching entry in the rendered resource changes.
    /// </summary>
    public IReadOnlyList<ActionInvocationModel> LifecycleOrphanActions { get; init; } = [];
}
