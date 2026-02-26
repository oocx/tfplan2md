namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Defines a filter that can suppress an attribute change row from the report.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
/// <remarks>
/// Implementations are responsible for self-selecting based on their own criteria
/// (e.g., provider name, value pattern). When <see cref="ShouldSuppress"/> returns
/// <c>true</c>, the attribute change row is excluded from the generated report.
/// </remarks>
internal interface IAttributeChangeFilter
{
    /// <summary>
    /// Determines whether the given attribute change row should be suppressed.
    /// </summary>
    /// <param name="context">The context containing provider name, attribute name, and before/after values.</param>
    /// <returns><c>true</c> to suppress (hide) the row; <c>false</c> to keep it.</returns>
    bool ShouldSuppress(AttributeChangeFilterContext context);
}
