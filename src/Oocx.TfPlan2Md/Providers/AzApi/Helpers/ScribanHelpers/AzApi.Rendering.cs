using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Scriban helper functions for azapi_resource template rendering.
/// Related feature: docs/features/040-azapi-resource-template/specification.md.
/// </summary>
/// <remarks>
/// These helpers transform JSON body content from azapi_resource resources into human-readable
/// markdown tables using dot-notation property paths. This makes Azure REST API resource
/// configurations easy to review in pull requests.
/// </remarks>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Renders azapi_resource body content as formatted markdown with proper handling of large values.
    /// </summary>
    /// <param name="bodyJson">The JSON body object to render.</param>
    /// <param name="heading">The heading text (e.g., "Body", "Body Changes").</param>
    /// <param name="mode">The rendering mode: "create", "update", or "delete".</param>
    /// <param name="beforeJson">The before state JSON (for update mode).</param>
    /// <param name="beforeSensitive">The before_sensitive structure (for update mode).</param>
    /// <param name="afterSensitive">The after_sensitive structure (for update mode).</param>
    /// <param name="showUnchanged">Whether to show unchanged properties (for update mode).</param>
    /// <param name="largeValueFormat">Format for rendering large values ("inline-diff" or "simple-diff").</param>
    /// <returns>Formatted markdown string for the body section.</returns>
    /// <remarks>
    /// This helper consolidates body rendering logic to keep the template concise.
    /// For create/delete modes, it flattens the body and separates small vs. large properties.
    /// For update mode, it compares before/after and shows only changed properties.
    /// Large properties are rendered outside of tables to avoid markdown parsing issues with newlines.
    /// Related feature: docs/features/040-azapi-resource-template/specification.md.
    /// </remarks>
    [SuppressMessage(
        "Major Code Smell",
        "S107:Methods should not have too many parameters",
        Justification = "Scriban entrypoint; optional parameters keep template usage concise.")]
    public static string RenderAzapiBody(
        object? bodyJson,
        string heading,
        string mode,
        object? beforeJson = null,
        object? beforeSensitive = null,
        object? afterSensitive = null,
        bool showUnchanged = false,
        string largeValueFormat = "inline-diff")
    {
        if (bodyJson is null)
        {
            return $"*{heading}: (empty)*\n";
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"#### {heading}");
        sb.AppendLine();

        if (mode == "update" && beforeJson is not null)
        {
            var updateInput = new UpdateBodyRenderInput(
                bodyJson,
                beforeJson,
                beforeSensitive,
                afterSensitive,
                showUnchanged,
                largeValueFormat);

            RenderUpdateBody(sb, updateInput);
        }
        else
        {
            RenderCreateDeleteBody(sb, heading, bodyJson, largeValueFormat);
        }

        return sb.ToString();
    }
}
