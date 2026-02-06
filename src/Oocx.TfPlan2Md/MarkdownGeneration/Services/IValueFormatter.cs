namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Formats attribute values using provider-aware logic.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal interface IValueFormatter
{
    /// <summary>
    /// Attempts to format the value for the given resolution context.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>A formatted string when handled; otherwise null to allow fallback.</returns>
    string? TryFormat(ServiceResolutionContext context);
}
