using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Shared constants and helper methods used by both <c>VariableGroupFormatters</c>
/// and <c>BuildDefinitionFormatters</c>.
/// </summary>
internal static class AzureDevOpsFormatterHelpers
{
    /// <summary>Change label used for added items.</summary>
    internal const string AddedChange = "add";

    /// <summary>Change label used for removed items.</summary>
    internal const string RemovedChange = "remove";

    /// <summary>Change label used for unchanged items.</summary>
    internal const string UnchangedChange = "unchanged";

    /// <summary>Change label used for modified items.</summary>
    internal const string ModifiedChange = "update";

    /// <summary>
    /// Converts a nullable boolean to its string representation for display.
    /// Returns <c>"true"</c>, <c>"false"</c>, or <c>"-"</c> for <c>null</c>.
    /// </summary>
    /// <param name="value">Boolean value to convert.</param>
    /// <returns>"true", "false", or "-" for null.</returns>
    internal static string ConvertBoolToString(bool? value)
    {
        if (value == null)
        {
            return "-";
        }

        return value.Value ? "true" : "false";
    }

    /// <summary>
    /// Formats an optional string value, returning a dash when the value is null or empty.
    /// Uses <see cref="EscapeMarkdown"/> from <c>MarkdownHelpers</c> for the inline code span.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>Inline code span, or "-" when the value is absent.</returns>
    internal static string FormatOptionalString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        return $"`{EscapeMarkdown(value)}`";
    }
}
