namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Semantic formatting helpers for attribute values.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Non-breaking space used to keep semantic icons attached to their labels for reliable markdown rendering.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md
    /// </summary>
    internal const string NonBreakingSpace = "\u00A0";

    /// <summary>
    /// Rendering context for semantic formatting.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md
    /// </summary>
    private enum ValueFormatContext
    {
        /// <summary>
        /// Table rendering where markdown backticks are required.
        /// </summary>
        Table,

        /// <summary>
        /// Summary rendering where HTML code spans are required.
        /// </summary>
        Summary
    }

    /// <summary>
    /// Formats attribute values for summary context using semantic icons and HTML code spans.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <returns>Formatted value suitable for use inside &lt;summary&gt; tags.</returns>
    public static string FormatAttributeValueSummary(string? attributeName, string? value, string? providerName)
    {
        return FormatAttributeValue(attributeName, value, providerName, ValueFormatContext.Summary);
    }

    /// <summary>
    /// Formats attribute values for table context using semantic icons and markdown code spans.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <returns>Formatted value suitable for markdown tables.</returns>
    public static string FormatAttributeValueTable(string? attributeName, string? value, string? providerName)
    {
        return FormatAttributeValue(attributeName, value, providerName, ValueFormatContext.Table);
    }

    /// <summary>
    /// Formats attribute values without wrapping so callers can apply their own wrappers.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <returns>Plain text value with semantic icons, no markdown or HTML wrapping.</returns>
    public static string FormatAttributeValuePlain(string? attributeName, string? value, string? providerName)
    {
        _ = providerName;

        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedValue = value.Trim();
        var normalizedName = attributeName ?? string.Empty;

        if (TryFormatBoolean(normalizedValue, ValueFormatContext.Table, out var booleanFormatted))
        {
            return booleanFormatted.Trim('`');
        }

        if (TryFormatAccess(normalizedName, normalizedValue, ValueFormatContext.Table, out var accessFormatted))
        {
            return accessFormatted.Trim('`');
        }

        if (TryFormatDirection(normalizedName, normalizedValue, ValueFormatContext.Table, out var directionFormatted))
        {
            return directionFormatted.Trim('`');
        }

        if (TryFormatProtocol(normalizedName, normalizedValue, ValueFormatContext.Table, out var protocolFormatted))
        {
            return protocolFormatted.Trim('`');
        }

        if (TryFormatPort(normalizedName, normalizedValue, ValueFormatContext.Table, out var portFormatted))
        {
            return portFormatted.Trim('`');
        }

        if (TryFormatPrincipalType(normalizedName, normalizedValue, ValueFormatContext.Table, out var principalTypeFormatted))
        {
            return principalTypeFormatted.Trim('`');
        }

        if (TryFormatRoleDefinition(normalizedName, normalizedValue, ValueFormatContext.Table, out var roleFormatted))
        {
            return roleFormatted.Trim('`');
        }

        if (TryFormatNameAttributePlain(normalizedName, normalizedValue, out var nameFormatted))
        {
            return nameFormatted;
        }

        if (value.Equals("*", StringComparison.OrdinalIgnoreCase))
        {
            return "✳️";
        }

        if (IsIpAddressOrCidr(normalizedValue))
        {
            return $"🌐{NonBreakingSpace}{normalizedValue}";
        }

        if (IsLocationAttribute(normalizedName))
        {
            return $"🌍{NonBreakingSpace}{normalizedValue}";
        }

        return normalizedValue;
    }

    /// <summary>
    /// Formats attribute values with semantic icons for the requested rendering context.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="context">The rendering context (table or summary).</param>
    /// <returns>Formatted value respecting semantic icon rules and context-specific code wrapping.</returns>
    private static string FormatAttributeValue(string? attributeName, string? value, string? providerName, ValueFormatContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedValue = value.Trim();
        var normalizedName = attributeName ?? string.Empty;

        if (TryFormatBoolean(normalizedValue, context, out var booleanFormatted))
        {
            return booleanFormatted;
        }

        if (TryFormatAccess(normalizedName, normalizedValue, context, out var accessFormatted))
        {
            return accessFormatted;
        }

        if (TryFormatDirection(normalizedName, normalizedValue, context, out var directionFormatted))
        {
            return directionFormatted;
        }

        if (TryFormatProtocol(normalizedName, normalizedValue, context, out var protocolFormatted))
        {
            return protocolFormatted;
        }

        if (TryFormatPort(normalizedName, normalizedValue, context, out var portFormatted))
        {
            return portFormatted;
        }

        if (TryFormatPrincipalType(normalizedName, normalizedValue, context, out var principalTypeFormatted))
        {
            return principalTypeFormatted;
        }

        if (TryFormatRoleDefinition(normalizedName, normalizedValue, context, out var roleFormatted))
        {
            return roleFormatted;
        }

        if (TryFormatNameAttribute(normalizedName, normalizedValue, context, out var nameFormatted))
        {
            return nameFormatted;
        }

        if (value.Equals("*", StringComparison.OrdinalIgnoreCase))
        {
            return context == ValueFormatContext.Table ? FormatCodeTable("✳️") : "✳️";
        }

        if (IsIpAddressOrCidr(normalizedValue))
        {
            return FormatIconValue($"🌐 {normalizedValue}", context, false);
        }

        if (IsLocationAttribute(normalizedName))
        {
            return FormatIconValue($"🌍 {normalizedValue}", context, false);
        }

        return context == ValueFormatContext.Table
            ? FormatValue(normalizedValue, providerName)
            : FormatCodeSummary(normalizedValue);
    }
}
