using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Value formatting helpers that handle provider-aware rendering and large value detection.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Determines whether a value should be treated as large based on newlines or length.
    /// Related features: docs/features/006-large-attribute-value-display/specification.md, docs/features/019-azure-resource-id-formatting/specification.md.
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
    /// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="providerName">The Terraform provider name.</param>
    /// <returns>Formatted markdown string for table rendering.</returns>
    public static string FormatValue(string? value, string? providerName)
    {
        return FormatValueWithRegistry(value, providerName, null);
    }

    /// <summary>
    /// Formats attribute values using registry-provided formatters before default logic.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="providerName">The Terraform provider name.</param>
    /// <param name="valueFormatterRegistry">Optional value formatter registry.</param>
    /// <returns>Formatted markdown string for table rendering.</returns>
    private static string FormatValueWithRegistry(
        string? value,
        string? providerName,
        ValueFormatterRegistry? valueFormatterRegistry)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (valueFormatterRegistry is not null)
        {
            var context = new ServiceResolutionContext(providerName, null, null, value);
            var formatted = valueFormatterRegistry.TryFormat(context);
            if (!string.IsNullOrEmpty(formatted))
            {
                return formatted;
            }
        }

        if (IsAzurermProvider(providerName) && AzureScopeParser.IsAzureResourceId(value))
        {
            return AzureScopeParser.ParseScope(value);
        }

        return $"`{EscapeMarkdown(value)}`";
    }

    /// <summary>
    /// Formats Terraform import IDs for the refactoring summary table.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    /// <param name="importId">The raw import ID from the plan.</param>
    /// <returns>A markdown string suitable for rendering inside a table cell.</returns>
    public static string FormatImportIdDetails(string? importId)
    {
        if (string.IsNullOrWhiteSpace(importId))
        {
            return string.Empty;
        }

        if (AzureScopeParser.IsAzureResourceId(importId))
        {
            return AzureScopeParser.ParseScope(importId);
        }

        return $"`{EscapeMarkdown(importId)}`";
    }

    /// <summary>
    /// Formats an output value for rendering in the outputs table.
    /// Handles masking (sensitive values), computed values (known after apply), and display name mappings.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="isMasked">Whether the value should be masked (sensitive and not --show-sensitive).</param>
    /// <param name="isComputed">Whether the value is computed (known after apply).</param>
    /// <param name="value">The raw value (may be JsonElement or string).</param>
    /// <param name="providerName">The Terraform provider name of the referenced resource.</param>
    /// <param name="attributeName">The output name used as attribute name for semantic formatting rules.</param>
    /// <param name="valueFormatterRegistry">Optional value formatter registry.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry for semantic icons.</param>
    /// <returns>Formatted markdown string for table rendering.</returns>
    private static string FormatOutputValue(
        bool isMasked,
        bool isComputed,
        object? value,
        string? providerName,
        string? attributeName,
        ValueFormatterRegistry? valueFormatterRegistry,
        Services.IconProviderRegistry? iconProviderRegistry = null)
    {
        if (isMasked)
        {
            return "(sensitive value)";
        }

        if (isComputed)
        {
            return "(known after apply)";
        }

        // For JSON objects and arrays, render as compact single-line JSON in backticks.
        // Code fences cannot be used inside table cells (they introduce newlines which break table formatting).
        if (value is System.Text.Json.JsonElement jsonElement &&
            (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object ||
             jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array))
        {
            var compact = CompactJson(jsonElement);
            return $"`{EscapeMarkdownTableCell(compact)}`";
        }

        // Convert scalar value to string (handles JsonElement strings/numbers/bools)
        var stringValue = ConvertValueToString(value);

        if (string.IsNullOrEmpty(stringValue))
        {
            return string.Empty;
        }

        // If the string value looks like a JSON object or array (e.g., stored via jsonencode()),
        // compact it to a single line to prevent <br> tags in table cells from embedded newlines.
        if (TryCompactJsonString(stringValue, out var compacted))
        {
            return $"`{EscapeMarkdownTableCell(compacted)}`";
        }

        // Use the same formatting pipeline as attribute values:
        // tries value formatter registry (display names, principal mapping) then
        // semantic formatting (icons, Azure ID parsing) based on attribute name + provider.
        return FormatAttributeValueTableWithRegistry(attributeName, stringValue, providerName, valueFormatterRegistry, iconProviderRegistry);
    }

    /// <summary>
    /// Converts an output value to a string for rendering.
    /// Handles JsonElement and other types.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A string representation of the value.</returns>
    private static string? ConvertValueToString(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => jsonElement.GetString(),
                System.Text.Json.JsonValueKind.Number => jsonElement.ToString(),
                System.Text.Json.JsonValueKind.True => "true",
                System.Text.Json.JsonValueKind.False => "false",
                System.Text.Json.JsonValueKind.Null => null,
                _ => jsonElement.GetRawText()
            };
        }

        return value.ToString();
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

    /// <summary>
    /// Serializes a JSON element to a compact (single-line) string without indentation.
    /// Uses a raw writer to avoid trimming issues with JsonSerializer.
    /// </summary>
    /// <param name="element">The JSON element to serialize.</param>
    /// <returns>A compact JSON string.</returns>
    private static string CompactJson(System.Text.Json.JsonElement element)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions { Indented = false });
        element.WriteTo(writer);
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Attempts to compact a string value that contains a JSON object or array.
    /// Used to prevent newlines in JSON strings (e.g., from jsonencode()) from producing
    /// &lt;br&gt; tags in table cells.
    /// </summary>
    /// <param name="value">The string value to inspect.</param>
    /// <param name="compacted">The compact JSON string when the method returns true.</param>
    /// <returns>True when the value was a JSON object or array and was successfully compacted.</returns>
    private static bool TryCompactJsonString(string value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? compacted)
    {
        var trimmed = value.TrimStart();
        if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
        {
            compacted = null;
            return false;
        }

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind is System.Text.Json.JsonValueKind.Object or System.Text.Json.JsonValueKind.Array)
            {
                compacted = CompactJson(doc.RootElement);
                return true;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not valid JSON — fall through
        }

        compacted = null;
        return false;
    }
}
