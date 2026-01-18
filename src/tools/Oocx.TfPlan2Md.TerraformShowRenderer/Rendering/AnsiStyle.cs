namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>
/// Represents ANSI styling options used by the Terraform show renderer.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
/// </summary>
internal enum AnsiStyle
{
    /// <summary>
    /// Applies bold text styling.
    /// </summary>
    Bold,

    /// <summary>
    /// Applies green foreground coloring (create/add).
    /// </summary>
    Green,

    /// <summary>
    /// Applies yellow foreground coloring (update/change).
    /// </summary>
    Yellow,

    /// <summary>
    /// Applies red foreground coloring (delete/destroy).
    /// </summary>
    Red,

    /// <summary>
    /// Applies cyan foreground coloring (read operations).
    /// </summary>
    Cyan,

    /// <summary>
    /// Applies dim/gray styling (comments, hidden markers).
    /// </summary>
    Dim,

    /// <summary>
    /// Resets styling to defaults.
    /// </summary>
    Reset
}
