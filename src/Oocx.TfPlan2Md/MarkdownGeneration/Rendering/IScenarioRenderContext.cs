namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Exposes report-level rendering scenario hints derived from the current model.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal interface IScenarioRenderContext
{
    /// <summary>
    /// Gets a value indicating whether the report matches the known-after-apply scenario.
    /// </summary>
    bool IsKnownAfterApplyScenario { get; }

    /// <summary>
    /// Gets a value indicating whether the report matches the ephemeral-open scenario.
    /// </summary>
    bool IsEphemeralOpenScenario { get; }

    /// <summary>
    /// Gets a value indicating whether the report is outputs-focused.
    /// </summary>
    bool IsOutputsFocusedReport { get; }
}
