using Oocx.TfPlan2Md.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Value formatting helpers that handle provider-aware rendering and large value detection.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Determines whether a value should be treated as large based on newlines or length.
    /// Related features: docs/features/006-large-attribute-value-display/specification.md, docs/features/019-azure-resource-id-formatting/specification.md
    /// </summary>
    /// <param name="input">The raw value.</param>
    /// <param name="providerName">The Terraform provider name to allow azurerm-specific exemptions.</param>
    /// <returns>True when the value contains newlines or exceeds 100 characters (unless exempt); otherwise false.</returns>
    public static bool IsLargeValue(string? input, string? providerName = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        if (input.Contains('\n', StringComparison.Ordinal) || input.Contains('\r', StringComparison.Ordinal))
        {
            return true;
        }

        if (IsAzurermProvider(providerName) && AzureScopeParser.IsAzureResourceId(input))
        {
            return false;
        }

        return input.Length > 100;
    }

    /// <summary>
    /// Formats attribute values with provider-aware logic (Azure IDs are rendered readably; others as inline code).
    /// Related feature: docs/features/019-azure-resource-id-formatting/specification.md
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="providerName">The Terraform provider name.</param>
    /// <returns>Formatted markdown string for table rendering.</returns>
    public static string FormatValue(string? value, string? providerName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (IsAzurermProvider(providerName) && AzureScopeParser.IsAzureResourceId(value))
        {
            return AzureScopeParser.ParseScope(value);
        }

        return $"`{EscapeMarkdown(value)}`";
    }

    /// <summary>
    /// Determines whether the provided Terraform provider name represents the azurerm provider.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>True when the provider is azurerm; otherwise false.</returns>
    private static bool IsAzurermProvider(string? providerName)
    {
        return !string.IsNullOrWhiteSpace(providerName)
               && providerName.Contains("azurerm", StringComparison.OrdinalIgnoreCase);
    }
}
