namespace Oocx.TfPlan2Md.RenderTargets;

/// <summary>
/// Defines the interface for platform-specific diff formatting strategies.
/// </summary>
/// <remarks>
/// Different rendering platforms (GitHub, Azure DevOps) require different diff formatting approaches.
/// This interface enables platform-specific diff rendering while keeping the core logic platform-agnostic.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
public interface IDiffFormatter
{
    /// <summary>
    /// Formats a before/after value pair as a diff suitable for the target platform.
    /// </summary>
    /// <param name="before">The original value.</param>
    /// <param name="after">The updated value.</param>
    /// <returns>
    /// Platform-specific formatted diff string. Returns empty when both values are null or empty.
    /// Returns the unchanged value when both values are identical.
    /// </returns>
    string FormatDiff(string? before, string? after);
}
