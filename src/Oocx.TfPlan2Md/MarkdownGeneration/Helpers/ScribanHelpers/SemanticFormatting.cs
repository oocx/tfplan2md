using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Semantic formatting helpers for attribute values.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Non-breaking space used to keep semantic icons attached to their labels for reliable markdown rendering.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    internal const string NonBreakingSpace = "\u00A0";

    /// <summary>
    /// Rendering context for semantic formatting.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
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
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <returns>Formatted value suitable for use inside &lt;summary&gt; tags.</returns>
    public static string FormatAttributeValueSummary(string? attributeName, string? value, string? providerName)
    {
        return FormatAttributeValue(attributeName, value, providerName, ValueFormatContext.Summary, null);
    }

    /// <summary>
    /// Formats attribute values for table context using semantic icons and markdown code spans.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <returns>Formatted value suitable for markdown tables.</returns>
    public static string FormatAttributeValueTable(string? attributeName, string? value, string? providerName)
    {
        return FormatAttributeValue(attributeName, value, providerName, ValueFormatContext.Table, null);
    }

    /// <summary>
    /// Formats attribute values without wrapping so callers can apply their own wrappers.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <returns>Plain text value with semantic icons, no markdown or HTML wrapping.</returns>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.")]
    public static string FormatAttributeValuePlain(string? attributeName, string? value, string? providerName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedValue = value.Trim();
        var normalizedName = attributeName ?? string.Empty;

        var registryIcon = TryGetRegistryIcon(providerName, attributeName, value, null);
        if (!string.IsNullOrWhiteSpace(registryIcon))
        {
            return $"{registryIcon}{NonBreakingSpace}{normalizedValue}";
        }

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

        if (TryFormatIdentityAttributePlain(normalizedName, normalizedValue, out var identityFormatted))
        {
            return identityFormatted;
        }

        if (TryFormatSubscriptionAttributePlain(normalizedName, normalizedValue, out var subscriptionFormatted))
        {
            return subscriptionFormatted;
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
    /// Attempts to apply semantic formatting rules for known attribute patterns.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="context">The rendering context (table or summary).</param>
    /// <param name="formattedValue">The formatted value when a semantic match is found.</param>
    /// <returns><c>true</c> when a semantic formatter applied; otherwise <c>false</c>.</returns>
    private static bool TryFormatSemanticValue(
        string attributeName,
        string value,
        ValueFormatContext context,
        [NotNullWhen(true)] out string? formattedValue)
    {
        if (TryFormatBoolean(value, context, out var booleanFormatted))
        {
            formattedValue = booleanFormatted;
            return true;
        }

        if (TryFormatAccess(attributeName, value, context, out var accessFormatted))
        {
            formattedValue = accessFormatted;
            return true;
        }

        if (TryFormatDirection(attributeName, value, context, out var directionFormatted))
        {
            formattedValue = directionFormatted;
            return true;
        }

        if (TryFormatProtocol(attributeName, value, context, out var protocolFormatted))
        {
            formattedValue = protocolFormatted;
            return true;
        }

        if (TryFormatPort(attributeName, value, context, out var portFormatted))
        {
            formattedValue = portFormatted;
            return true;
        }

        if (TryFormatPrincipalType(attributeName, value, context, out var principalTypeFormatted))
        {
            formattedValue = principalTypeFormatted;
            return true;
        }

        if (TryFormatRoleDefinition(attributeName, value, context, out var roleFormatted))
        {
            formattedValue = roleFormatted;
            return true;
        }

        if (TryFormatIdentityAttribute(attributeName, value, context, out var identityFormatted))
        {
            formattedValue = identityFormatted;
            return true;
        }

        if (TryFormatSubscriptionAttribute(attributeName, value, context, out var subscriptionFormatted))
        {
            formattedValue = subscriptionFormatted;
            return true;
        }

        if (TryFormatNameAttribute(attributeName, value, context, out var nameFormatted))
        {
            formattedValue = nameFormatted;
            return true;
        }

        formattedValue = null;
        return false;
    }

    /// <summary>
    /// Formats attribute values with semantic icons for the requested rendering context.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="context">The rendering context (table or summary).</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted value respecting semantic icon rules and context-specific code wrapping.</returns>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.")]
    private static string FormatAttributeValue(
        string? attributeName,
        string? value,
        string? providerName,
        ValueFormatContext context,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedValue = value.Trim();
        var normalizedName = attributeName ?? string.Empty;

        var registryIcon = TryGetRegistryIcon(providerName, attributeName, value, iconProviderRegistry);
        if (!string.IsNullOrWhiteSpace(registryIcon))
        {
            var iconText = $"{registryIcon}{NonBreakingSpace}{normalizedValue}";
            return context == ValueFormatContext.Table ? FormatCodeTable(iconText) : FormatCodeSummary(iconText);
        }

        if (TryFormatSemanticValue(normalizedName, normalizedValue, context, out var semanticFormatted))
        {
            return semanticFormatted;
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

    /// <summary>
    /// Formats attribute values for summary context using icon registry overrides when supplied.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted value suitable for use inside &lt;summary&gt; tags.</returns>
    private static string FormatAttributeValueSummaryWithRegistry(
        string? attributeName,
        string? value,
        string? providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        return FormatAttributeValue(attributeName, value, providerName, ValueFormatContext.Summary, iconProviderRegistry);
    }

    /// <summary>
    /// Formats attribute values for table context using icon registry overrides when supplied.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted value suitable for markdown tables.</returns>
    private static string FormatAttributeValueTableWithRegistry(
        string? attributeName,
        string? value,
        string? providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        return FormatAttributeValue(attributeName, value, providerName, ValueFormatContext.Table, iconProviderRegistry);
    }

    /// <summary>
    /// Formats attribute values without wrapping using icon registry overrides when supplied.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Plain text value with semantic icons, no markdown or HTML wrapping.</returns>
    private static string FormatAttributeValuePlainWithRegistry(
        string? attributeName,
        string? value,
        string? providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedValue = value.Trim();
        var registryIcon = TryGetRegistryIcon(providerName, attributeName, value, iconProviderRegistry);
        if (!string.IsNullOrWhiteSpace(registryIcon))
        {
            return $"{registryIcon}{NonBreakingSpace}{normalizedValue}";
        }

        return FormatAttributeValuePlain(attributeName, value, providerName);
    }

    /// <summary>
    /// Resolves an icon from the registry for the given context.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <param name="value">The raw value.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>The icon string or an empty string if none.</returns>
    private static string GetIconWithRegistry(
        string? providerName,
        string? resourceType,
        string? attributeName,
        string? value,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (iconProviderRegistry is null)
        {
            return string.Empty;
        }

        var context = new ServiceResolutionContext(providerName, resourceType, attributeName, value);
        return iconProviderRegistry.TryGetIcon(context) ?? string.Empty;
    }

    /// <summary>
    /// Attempts to resolve an icon from the registry for attribute formatting.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <param name="value">The raw value.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>The icon string when available; otherwise null.</returns>
    private static string? TryGetRegistryIcon(
        string? providerName,
        string? attributeName,
        string? value,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (iconProviderRegistry is null)
        {
            return null;
        }

        var context = new ServiceResolutionContext(providerName, null, attributeName, value);
        return iconProviderRegistry.TryGetIcon(context);
    }

    /// <summary>
    /// Resolves a registry icon for use in templates.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <param name="value">The raw value.</param>
    /// <returns>The icon string or an empty string if none.</returns>
    public static string GetIcon(string? providerName, string? resourceType, string? attributeName, string? value)
    {
        return GetIconWithRegistry(providerName, resourceType, attributeName, value, null);
    }
}
