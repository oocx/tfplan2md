namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Canonical Terraform action names and their display symbols.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal static class TerraformActions
{
    /// <summary>
    /// Terraform action name for create operations.
    /// </summary>
    internal const string Create = "create";

    /// <summary>
    /// Terraform action name for delete operations.
    /// </summary>
    internal const string Delete = "delete";

    /// <summary>
    /// Terraform action name for update operations.
    /// </summary>
    internal const string Update = "update";

    /// <summary>
    /// Terraform action name for read operations.
    /// </summary>
    internal const string Read = "read";

    /// <summary>
    /// Terraform action name for forget operations.
    /// </summary>
    internal const string Forget = "forget";

    /// <summary>
    /// Terraform action name for ephemeral open operations.
    /// </summary>
    internal const string Open = "open";

    /// <summary>
    /// Terraform action name for replace operations.
    /// </summary>
    internal const string Replace = "replace";

    /// <summary>
    /// Terraform action name used when the input action set is unknown.
    /// </summary>
    internal const string Unknown = "unknown";

    /// <summary>
    /// Terraform action name for no-op operations.
    /// </summary>
    internal const string NoOp = "no-op";

    /// <summary>
    /// Maps a normalized Terraform action to the shared display symbol.
    /// </summary>
    /// <param name="action">The normalized Terraform action string.</param>
    /// <returns>The icon used in the report model.</returns>
    internal static string GetSymbol(string action)
    {
        return action switch
        {
            Create => ActionIcons.Add,
            Delete => ActionIcons.Delete,
            Update => ActionIcons.Update,
            Read => ActionIcons.Add,
            Open => ActionIcons.Add,
            Forget => ActionIcons.Delete,
            Replace => ActionIcons.Replace,
            Unknown => "⚠️",
            _ => ActionIcons.NoOp
        };
    }
}
