namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
internal partial class ReportModelBuilder
{
    private const string CreateAction = TerraformActions.Create;
    private const string DeleteAction = TerraformActions.Delete;
    private const string UpdateAction = TerraformActions.Update;
    private const string ReplaceAction = TerraformActions.Replace;

    /// <summary>
    /// Maps action type to the display symbol used across report models.
    /// </summary>
    /// <param name="action">The normalized action string.</param>
    /// <returns>The symbol representing the action.</returns>
    private static string GetActionSymbol(string action)
    {
        return TerraformActions.GetSymbol(action);
    }
}
