using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Scriban helpers for controlling details block display state.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Determines the 'open' attribute for a resource details block based on display mode.
    /// Returns either " open" (with leading space) or an empty string.
    /// </summary>
    /// <param name="change">The resource change ScriptObject containing code analysis findings.</param>
    /// <param name="mode">The details display mode from the CLI.</param>
    /// <returns>" open" (with leading space) if the resource should be expanded, empty string otherwise.</returns>
    internal static string GetDetailsOpenAttr(ScriptObject? change, RenderTargets.DetailsDisplayMode mode)
    {
        return mode switch
        {
            RenderTargets.DetailsDisplayMode.Open => " open",
            RenderTargets.DetailsDisplayMode.Closed => string.Empty,
            RenderTargets.DetailsDisplayMode.Auto => HasCodeAnalysisFindings(change) ? " open" : string.Empty,
            _ => string.Empty // Default to closed for unknown modes
        };
    }

    /// <summary>
    /// Checks if a resource change has code analysis findings.
    /// </summary>
    /// <param name="change">The resource change ScriptObject.</param>
    /// <returns>True if the resource has findings, false otherwise.</returns>
    private static bool HasCodeAnalysisFindings(ScriptObject? change)
    {
        if (change is null)
        {
            return false;
        }

        // Check direct findings on this resource
        if (change.TryGetValue("code_analysis_findings", out var findingsValue)
            && findingsValue is ScriptArray findings
            && findings.Count > 0)
        {
            return true;
        }

        return false;
    }
}
