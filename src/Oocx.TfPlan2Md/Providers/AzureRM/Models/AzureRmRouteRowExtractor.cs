using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Extracts Azure RM route row values for inline child tables.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/azure-rm-batch-specification.md.
/// Supports both inline routes (via azurerm_route_table.route attribute) and separate azurerm_route resources.
/// </remarks>
internal sealed class AzureRmRouteRowExtractor : IChildRowExtractor
{
    /// <summary>
    /// Extracts the route row values from Azure RM route state.
    /// </summary>
    /// <param name="childState">The child JSON state for the route.</param>
    /// <param name="providerName">The provider name for formatting context.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for formatting values.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <returns>The formatted row values with columns: name, address_prefix, next_hop_type, next_hop_in_ip_address.</returns>
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
        var addressPrefix = FormatAttribute(element, "address_prefix", providerName, valueFormatterRegistry, iconProviderRegistry);
        var nextHopType = JsonStateReader.GetStringProperty(element, "next_hop_type") ?? "-";
        var nextHopAddress = FormatNextHopAddress(element, providerName, valueFormatterRegistry, iconProviderRegistry);

        return new Dictionary<string, string>
        {
            ["name"] = name,
            ["address_prefix"] = addressPrefix,
            ["next_hop_type"] = nextHopType,
            ["next_hop_in_ip_address"] = nextHopAddress
        };
    }

    /// <summary>
    /// Formats a route attribute using the formatter registry.
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
    /// Formats next hop IP address, or returns "-" if not applicable.
    /// </summary>
    private static string FormatNextHopAddress(
        JsonElement element,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        var address = JsonStateReader.GetStringProperty(element, "next_hop_in_ip_address");
        if (string.IsNullOrEmpty(address))
        {
            return "-";
        }

        return MarkdownHelpers.FormatAttributeValueTableWithRegistry(
            "next_hop_in_ip_address",
            address,
            providerName,
            valueFormatterRegistry,
            iconProviderRegistry);
    }

    /// <summary>
    /// Extracts column values with inline diffs for a route that changed between before/after states.
    /// </summary>
    /// <param name="beforeState">The route state before the change.</param>
    /// <param name="afterState">The route state after the change.</param>
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

        var beforeAddressPrefix = ExtractRawAttribute(beforeElement, "address_prefix", providerName, iconProviderRegistry);
        var afterAddressPrefix = ExtractRawAttribute(afterElement, "address_prefix", providerName, iconProviderRegistry);
        var addressPrefixDiff = MarkdownHelpers.FormatDiff(beforeAddressPrefix, afterAddressPrefix, format);

        var beforeNextHopType = JsonStateReader.GetStringProperty(beforeElement, "next_hop_type") ?? "-";
        var afterNextHopType = JsonStateReader.GetStringProperty(afterElement, "next_hop_type") ?? "-";
        var nextHopTypeDiff = MarkdownHelpers.FormatDiff(beforeNextHopType, afterNextHopType, format);

        var beforeNextHopAddress = ExtractRawNextHopAddress(beforeElement, providerName, iconProviderRegistry);
        var afterNextHopAddress = ExtractRawNextHopAddress(afterElement, providerName, iconProviderRegistry);
        var nextHopAddressDiff = MarkdownHelpers.FormatDiff(beforeNextHopAddress, afterNextHopAddress, format);

        return new Dictionary<string, string>
        {
            ["name"] = nameDiff,
            ["address_prefix"] = addressPrefixDiff,
            ["next_hop_type"] = nextHopTypeDiff,
            ["next_hop_in_ip_address"] = nextHopAddressDiff
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
    /// Extracts raw next hop IP address with icons but without backtick wrapping (for diff generation).
    /// </summary>
    private static string ExtractRawNextHopAddress(
        JsonElement element,
        string providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        var address = JsonStateReader.GetStringProperty(element, "next_hop_in_ip_address");
        if (string.IsNullOrEmpty(address))
        {
            return "-";
        }

        return MarkdownHelpers.FormatAttributeValuePlainWithRegistry(
            "next_hop_in_ip_address",
            address,
            providerName,
            iconProviderRegistry);
    }
}
