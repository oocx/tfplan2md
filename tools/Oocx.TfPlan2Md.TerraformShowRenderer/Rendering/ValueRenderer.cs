using System.Text.Json;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>
/// Formats JSON values in a Terraform show-like style.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
/// </summary>
internal sealed class ValueRenderer
{
    /// <summary>
    /// Renders a JSON value to a Terraform-like string.
    /// </summary>
    /// <param name="value">JSON value to render.</param>
    /// <returns>Terraform-style representation.</returns>
    public string Render(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => FormattableString.Invariant($"\"{value.GetString()}\""),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => value.GetRawText()
        };
    }
}
