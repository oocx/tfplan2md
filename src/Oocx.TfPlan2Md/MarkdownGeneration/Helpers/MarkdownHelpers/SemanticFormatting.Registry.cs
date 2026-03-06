using System;
using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Semantic formatting helpers for attribute values.
/// </summary>
internal static partial class MarkdownHelpers
{
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
        return FormatAttributeValueCore(attributeName, value, providerName, null, context, iconProviderRegistry);
    }

    /// <summary>
    /// Formats attribute values with semantic icons, including resource-specific icon rules when available.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="resourceType">The resource type for resource-scoped icon resolution.</param>
    /// <param name="context">The rendering context (table or summary).</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted value respecting semantic icon rules and context-specific code wrapping.</returns>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.")]
    private static string FormatAttributeValueWithResource(
        string? attributeName,
        string? value,
        string? providerName,
        string? resourceType,
        ValueFormatContext context,
        IconProviderRegistry? iconProviderRegistry)
    {
        return FormatAttributeValueCore(attributeName, value, providerName, resourceType, context, iconProviderRegistry);
    }

    /// <summary>
    /// Formats attribute values for table context using icon registry overrides when supplied.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="valueFormatterRegistry">Optional value formatter registry.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted value suitable for markdown tables.</returns>
    internal static string FormatAttributeValueTableWithRegistry(
        string? attributeName,
        string? value,
        string? providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (valueFormatterRegistry is not null)
        {
            var context = new ServiceResolutionContext(providerName, null, attributeName, value);
            var formatted = valueFormatterRegistry.TryFormat(context);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                return formatted;
            }
        }

        return FormatAttributeValue(attributeName, value, providerName, ValueFormatContext.Table, iconProviderRegistry);
    }

    /// <summary>
    /// Formats attribute values for table context using resource-aware icon registry overrides when supplied.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="resourceType">The resource type for icon resolution.</param>
    /// <param name="valueFormatterRegistry">Optional value formatter registry.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted value suitable for markdown tables.</returns>
    internal static string FormatAttributeValueTableWithRegistryResource(
        string? attributeName,
        string? value,
        string? providerName,
        string? resourceType,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (valueFormatterRegistry is not null)
        {
            var context = new ServiceResolutionContext(providerName, resourceType, attributeName, value);
            var formatted = valueFormatterRegistry.TryFormat(context);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                return formatted;
            }
        }

        return FormatAttributeValueWithResource(
            attributeName,
            value,
            providerName,
            resourceType,
            ValueFormatContext.Table,
            iconProviderRegistry);
    }

    /// <summary>
    /// Formats attribute values without wrapping using icon registry overrides when supplied.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Plain text value with semantic icons, no markdown or HTML wrapping.</returns>
    internal static string FormatAttributeValuePlainWithRegistry(
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
    /// Formats attribute values with shared semantic rules, optionally scoped by resource type.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic formatting.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="providerName">The Terraform provider name for provider-aware fallbacks.</param>
    /// <param name="resourceType">Optional resource type for resource-scoped icon resolution.</param>
    /// <param name="context">The rendering context (table or summary).</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted value respecting semantic icon rules and context-specific code wrapping.</returns>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.")]
    private static string FormatAttributeValueCore(
        string? attributeName,
        string? value,
        string? providerName,
        string? resourceType,
        ValueFormatContext context,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedValue = value.Trim();
        var normalizedName = attributeName ?? string.Empty;

        var registryIcon = resourceType is null
            ? TryGetRegistryIcon(providerName, attributeName, value, iconProviderRegistry)
            : GetIconWithRegistry(providerName, resourceType, attributeName, value, iconProviderRegistry);

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
}
