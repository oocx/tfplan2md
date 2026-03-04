using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Extracts Azure RM subnet row values for inline child tables.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/azure-rm-batch-specification.md.
/// Supports both inline subnets (via azurerm_virtual_network.subnet attribute) and separate azurerm_subnet resources.
/// </remarks>
internal sealed class AzureRmSubnetRowExtractor : IChildRowExtractor
{
    /// <summary>
    /// Extracts the subnet row values from Azure RM subnet state.
    /// </summary>
    /// <param name="childState">The child JSON state for the subnet.</param>
    /// <param name="providerName">The provider name for formatting context.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for formatting values.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <returns>The formatted row values with columns: name, address_prefixes, nsg, delegation.</returns>
    public IReadOnlyDictionary<string, string> ExtractRow(
        object? childState,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (childState is not JsonElement element)
        {
            return new Dictionary<string, string>();
        }

        var name = FormatAttribute(element, "name", providerName, valueFormatterRegistry, iconProviderRegistry);
        var addressPrefixes = FormatAddressPrefixes(element, providerName, valueFormatterRegistry, iconProviderRegistry);
        var nsg = FormatNsg(element, providerName, valueFormatterRegistry, iconProviderRegistry);
        var delegation = ExtractDelegation(element);

        return new Dictionary<string, string>
        {
            ["name"] = name,
            ["address_prefixes"] = addressPrefixes,
            ["nsg"] = nsg,
            ["delegation"] = delegation
        };
    }

    /// <summary>
    /// Formats a subnet attribute using the formatter registry.
    /// </summary>
    private static string FormatAttribute(
        JsonElement element,
        string attributeName,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        var value = JsonStateReader.GetStringProperty(element, attributeName);
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        return MarkdownHelpers.FormatAttributeValueTableWithRegistry(
            attributeName,
            value,
            providerName,
            valueFormatterRegistry,
            iconProviderRegistry);
    }

    /// <summary>
    /// Formats address prefixes as a comma-separated list or count indicator.
    /// </summary>
    private static string FormatAddressPrefixes(
        JsonElement element,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (!element.TryGetProperty("address_prefixes", out var prefixesProperty))
        {
            // Fallback to singular address_prefix
            return FormatAttribute(element, "address_prefix", providerName, valueFormatterRegistry, iconProviderRegistry);
        }

        if (prefixesProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var prefixes = new List<string>();
        foreach (var prefix in prefixesProperty.EnumerateArray())
        {
            if (prefix.ValueKind == JsonValueKind.String)
            {
                var formatted = MarkdownHelpers.FormatAttributeValueTableWithRegistry(
                    "address_prefix",
                    prefix.GetString() ?? string.Empty,
                    providerName,
                    valueFormatterRegistry,
                    iconProviderRegistry);
                prefixes.Add(formatted);
            }
        }

        if (prefixes.Count == 0)
        {
            return "-";
        }

        if (prefixes.Count <= 2)
        {
            return string.Join(", ", prefixes);
        }

        return $"✳️ {prefixes.Count} items";
    }

    /// <summary>
    /// Formats network security group reference.
    /// </summary>
    private static string FormatNsg(
        JsonElement element,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        // Try security_group attribute first (reference or name)
        var nsgValue = JsonStateReader.GetStringProperty(element, "security_group");
        if (string.IsNullOrEmpty(nsgValue))
        {
            return "-";
        }

        return MarkdownHelpers.FormatAttributeValueTableWithRegistry(
            "security_group",
            nsgValue,
            providerName,
            valueFormatterRegistry,
            iconProviderRegistry);
    }

    /// <summary>
    /// Extracts column values with inline diffs for a subnet that changed between before/after states.
    /// </summary>
    /// <param name="beforeState">The subnet state before the change.</param>
    /// <param name="afterState">The subnet state after the change.</param>
    /// <param name="providerName">The provider name for formatting context.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for formatting values.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <param name="largeValueFormat">The preferred format for rendering large value diffs.</param>
    /// <returns>The formatted row values with inline diffs where values changed.</returns>
    public IReadOnlyDictionary<string, string> ExtractDiffRow(
        object? beforeState,
        object? afterState,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry,
        LargeValueFormat largeValueFormat)
    {
        if (beforeState is not JsonElement beforeElement || afterState is not JsonElement afterElement)
        {
            return new Dictionary<string, string>();
        }

        var format = largeValueFormat.ToString();

        // Extract RAW values without formatting, then format the diff
        // FormatDiff will add HTML styling, so we must NOT pre-format with backticks
        var beforeName = ExtractRawAttribute(beforeElement, "name", providerName, iconProviderRegistry);
        var afterName = ExtractRawAttribute(afterElement, "name", providerName, iconProviderRegistry);
        var nameDiff = MarkdownHelpers.FormatDiff(beforeName, afterName, format);

        var beforeAddressPrefixes = ExtractRawAddressPrefixes(beforeElement, providerName, iconProviderRegistry);
        var afterAddressPrefixes = ExtractRawAddressPrefixes(afterElement, providerName, iconProviderRegistry);
        var addressPrefixesDiff = MarkdownHelpers.FormatDiff(beforeAddressPrefixes, afterAddressPrefixes, format);

        var beforeNsg = ExtractRawNsg(beforeElement, providerName, iconProviderRegistry);
        var afterNsg = ExtractRawNsg(afterElement, providerName, iconProviderRegistry);
        var nsgDiff = MarkdownHelpers.FormatDiff(beforeNsg, afterNsg, format);

        var beforeDelegation = ExtractDelegation(beforeElement);
        var afterDelegation = ExtractDelegation(afterElement);
        var delegationDiff = MarkdownHelpers.FormatDiff(beforeDelegation, afterDelegation, format);

        return new Dictionary<string, string>
        {
            ["name"] = nameDiff,
            ["address_prefixes"] = addressPrefixesDiff,
            ["nsg"] = nsgDiff,
            ["delegation"] = delegationDiff
        };
    }

    /// <summary>
    /// Extracts raw attribute value with icons but without backtick wrapping (for diff generation).
    /// </summary>
    private static string ExtractRawAttribute(
        JsonElement element,
        string attributeName,
        string providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        var value = JsonStateReader.GetStringProperty(element, attributeName);
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        // Use FormatAttributeValuePlainWithRegistry which adds icons but NOT backticks
        return MarkdownHelpers.FormatAttributeValuePlainWithRegistry(
            attributeName,
            value,
            providerName,
            iconProviderRegistry);
    }

    /// <summary>
    /// Extracts raw address prefixes with icons but without backtick wrapping (for diff generation).
    /// </summary>
    private static string ExtractRawAddressPrefixes(
        JsonElement element,
        string providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (!element.TryGetProperty("address_prefixes", out var prefixesProperty))
        {
            // Fallback to singular address_prefix
            return ExtractRawAttribute(element, "address_prefix", providerName, iconProviderRegistry);
        }

        if (prefixesProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var prefixes = new List<string>();
        foreach (var prefix in prefixesProperty.EnumerateArray())
        {
            if (prefix.ValueKind == JsonValueKind.String)
            {
                var formatted = MarkdownHelpers.FormatAttributeValuePlainWithRegistry(
                    "address_prefix",
                    prefix.GetString() ?? string.Empty,
                    providerName,
                    iconProviderRegistry);
                prefixes.Add(formatted);
            }
        }

        if (prefixes.Count == 0)
        {
            return "-";
        }

        if (prefixes.Count <= 2)
        {
            return string.Join(", ", prefixes);
        }

        return $"✳️ {prefixes.Count} items";
    }

    /// <summary>
    /// Extracts raw NSG value with icons but without backtick wrapping (for diff generation).
    /// </summary>
    private static string ExtractRawNsg(
        JsonElement element,
        string providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        var nsgValue = JsonStateReader.GetStringProperty(element, "security_group");
        if (string.IsNullOrEmpty(nsgValue))
        {
            return "-";
        }

        return MarkdownHelpers.FormatAttributeValuePlainWithRegistry(
            "security_group",
            nsgValue,
            providerName,
            iconProviderRegistry);
    }

    /// <summary>
    /// Extracts service delegation name from nested delegation structure.
    /// </summary>
    private static string ExtractDelegation(JsonElement element)
    {
        if (!element.TryGetProperty("delegation", out var delegationProperty))
        {
            return "-";
        }

        if (delegationProperty.ValueKind != JsonValueKind.Array || delegationProperty.GetArrayLength() == 0)
        {
            return "-";
        }

        var firstDelegation = delegationProperty[0];
        if (!firstDelegation.TryGetProperty("service_delegation", out var serviceDelegationProperty))
        {
            return "-";
        }

        if (serviceDelegationProperty.ValueKind != JsonValueKind.Array || serviceDelegationProperty.GetArrayLength() == 0)
        {
            return "-";
        }

        var firstServiceDelegation = serviceDelegationProperty[0];
        var delegationName = JsonStateReader.GetStringProperty(firstServiceDelegation, "name");

        return string.IsNullOrEmpty(delegationName) ? "-" : delegationName;
    }
}
