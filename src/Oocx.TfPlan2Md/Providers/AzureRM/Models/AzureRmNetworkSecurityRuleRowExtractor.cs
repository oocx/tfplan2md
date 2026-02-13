using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Extracts Azure RM network security rule row values for inline child tables.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/azure-rm-batch-specification.md.
/// Supports both inline rules (via azurerm_network_security_group.security_rule attribute) and separate azurerm_network_security_rule resources.
/// </remarks>
internal sealed class AzureRmNetworkSecurityRuleRowExtractor : IChildRowExtractor
{
    /// <summary>
    /// Extracts the security rule row values from Azure RM network security rule state.
    /// </summary>
    /// <param name="childState">The child JSON state for the security rule.</param>
    /// <param name="providerName">The provider name for formatting context.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for formatting values.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <returns>The formatted row values with columns: name, priority, direction, access, protocol, source_addresses, source_ports, destination_addresses, destination_ports, description.</returns>
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
        var priority = JsonStateReader.GetStringProperty(element, "priority") ?? "-";
        var direction = FormatDirection(element);
        var access = FormatAccess(element);
        var protocol = FormatProtocol(element);

        return new Dictionary<string, string>
        {
            ["name"] = name,
            ["priority"] = priority,
            ["direction"] = direction,
            ["access"] = access,
            ["protocol"] = protocol,
            ["source_addresses"] = FormatAddresses(element, "source", providerName, valueFormatterRegistry, iconProviderRegistry),
            ["source_ports"] = FormatPorts(element, "source"),
            ["destination_addresses"] = FormatAddresses(element, "destination", providerName, valueFormatterRegistry, iconProviderRegistry),
            ["destination_ports"] = FormatPorts(element, "destination"),
            ["description"] = FormatDescription(element)
        };
    }

    /// <summary>
    /// Formats a security rule attribute using the formatter registry.
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

        return ScribanHelpers.FormatAttributeValueTableWithRegistry(
            attributeName,
            value,
            providerName,
            valueFormatterRegistry,
            iconProviderRegistry);
    }

    /// <summary>
    /// Formats direction with icon (⬇️ Inbound / ⬆️ Outbound).
    /// </summary>
    private static string FormatDirection(JsonElement element)
    {
        var direction = JsonStateReader.GetStringProperty(element, "direction");
        return direction?.ToLowerInvariant() switch
        {
            "inbound" => "⬇️ Inbound",
            "outbound" => "⬆️ Outbound",
            _ => direction ?? "-"
        };
    }

    /// <summary>
    /// Formats access with icon (✅ Allow / ⛔ Deny).
    /// </summary>
    private static string FormatAccess(JsonElement element)
    {
        var access = JsonStateReader.GetStringProperty(element, "access");
        return access?.ToLowerInvariant() switch
        {
            "allow" => "✅ Allow",
            "deny" => "⛔ Deny",
            _ => access ?? "-"
        };
    }

    /// <summary>
    /// Formats protocol with icon (🔗 TCP / 🔗 UDP / ✳️ for Any).
    /// </summary>
    private static string FormatProtocol(JsonElement element)
    {
        var protocol = JsonStateReader.GetStringProperty(element, "protocol");
        return protocol?.ToUpperInvariant() switch
        {
            "TCP" => "🔗 TCP",
            "UDP" => "🔗 UDP",
            "ICMP" => "🔗 ICMP",
            "*" => "✳️",
            _ => protocol ?? "-"
        };
    }

    /// <summary>
    /// Formats source or destination addresses with support for wildcards and service tags.
    /// </summary>
    /// <remarks>
    /// Handles both address_prefix (singular) and address_prefixes (array) properties.
    /// Array takes precedence when both are present.
    /// </remarks>
    private static string FormatAddresses(
        JsonElement element,
        string prefix,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        // Try address_prefixes array FIRST (takes precedence over singular)
        if (element.TryGetProperty($"{prefix}_address_prefixes", out var prefixesProperty) &&
            prefixesProperty.ValueKind == JsonValueKind.Array)
        {
            var prefixes = prefixesProperty.EnumerateArray()
                .Where(p => p.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(p.GetString()))
                .Select(p => p.GetString() ?? string.Empty)
                .ToList();

            if (prefixes.Count > 0)
            {
                if (prefixes.Count == 1 && prefixes[0] == "*")
                {
                    return "✳️";
                }

                var formatted = new List<string>(prefixes.Count);
                foreach (var p in prefixes)
                {
                    if (p == "*")
                    {
                        formatted.Add("✳️");
                    }
                    else if (IsServiceTag(p))
                    {
                        formatted.Add($"`{p}`");
                    }
                    else
                    {
                        formatted.Add(ScribanHelpers.FormatAttributeValueTableWithRegistry(
                            $"{prefix}_address_prefix",
                            p,
                            providerName,
                            valueFormatterRegistry,
                            iconProviderRegistry));
                    }
                }

                if (formatted.Count <= 2)
                {
                    return string.Join(", ", formatted);
                }

                return $"✳️ {formatted.Count} items";
            }
        }

        // Fallback to address_prefix (singular)
        var addressPrefix = JsonStateReader.GetStringProperty(element, $"{prefix}_address_prefix");
        if (!string.IsNullOrEmpty(addressPrefix))
        {
            if (addressPrefix == "*")
            {
                return "✳️";
            }

            // Check if it's a service tag (starts with capital letter, no dots/slashes)
            if (IsServiceTag(addressPrefix))
            {
                return $"`{addressPrefix}`";
            }

            return ScribanHelpers.FormatAttributeValueTableWithRegistry(
                $"{prefix}_address_prefix",
                addressPrefix,
                providerName,
                valueFormatterRegistry,
                iconProviderRegistry);
        }

        return "-";
    }

    /// <summary>
    /// Checks if a value is a service tag (e.g., "Internet", "VirtualNetwork").
    /// </summary>
    private static bool IsServiceTag(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "*")
        {
            return false;
        }

        // Service tags start with capital letter and don't contain IP-like patterns
        return char.IsUpper(value[0]) && !value.Contains('/') && !value.Contains('.');
    }

    /// <summary>
    /// Formats source or destination port ranges with 🔌 icon.
    /// </summary>
    /// <param name="element">The JSON element containing port range data.</param>
    /// <param name="prefix">The prefix for port properties ("source" or "destination").</param>
    /// <returns>Formatted port range string with icon, or ✳️ for wildcard.</returns>
    private static string FormatPorts(JsonElement element, string prefix)
    {
        // Try single port first
        var portRange = JsonStateReader.GetStringProperty(element, $"{prefix}_port_range");
        if (!string.IsNullOrEmpty(portRange))
        {
            if (portRange == "*")
            {
                return "✳️";
            }
            return $"🔌\u00A0{portRange}";
        }

        // Try port ranges array
        if (element.TryGetProperty($"{prefix}_port_ranges", out var rangesProperty) &&
            rangesProperty.ValueKind == JsonValueKind.Array)
        {
            var ranges = rangesProperty.EnumerateArray()
                .Where(r => r.ValueKind == JsonValueKind.String)
                .Select(r => r.GetString() ?? string.Empty)
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList();

            if (ranges.Count > 0)
            {
                if (ranges.Count == 1 && ranges[0] == "*")
                {
                    return "✳️";
                }

                if (ranges.Count <= 2)
                {
                    return $"🔌\u00A0{string.Join(",", ranges)}";
                }

                return $"✳️ {ranges.Count} ranges";
            }
        }

        return "✳️";  // Default for wildcard
    }

    /// <summary>
    /// Formats rule description text.
    /// </summary>
    /// <param name="element">The JSON element containing description property.</param>
    /// <returns>The description text, or "-" if empty.</returns>
    private static string FormatDescription(JsonElement element)
    {
        var description = JsonStateReader.GetStringProperty(element, "description");
        if (string.IsNullOrEmpty(description))
        {
            return "-";
        }
        return description;
    }

    /// <summary>
    /// Extracts column values with inline diffs for a security rule that changed between before/after states.
    /// </summary>
    /// <param name="beforeState">The security rule state before the change.</param>
    /// <param name="afterState">The security rule state after the change.</param>
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
        var nameDiff = ScribanHelpers.FormatDiff(beforeName, afterName, format);

        var beforePriority = JsonStateReader.GetStringProperty(beforeElement, "priority") ?? "-";
        var afterPriority = JsonStateReader.GetStringProperty(afterElement, "priority") ?? "-";
        var priorityDiff = ScribanHelpers.FormatDiff(beforePriority, afterPriority, format);

        var beforeDirection = FormatDirection(beforeElement);
        var afterDirection = FormatDirection(afterElement);
        var directionDiff = ScribanHelpers.FormatDiff(beforeDirection, afterDirection, format);

        var beforeAccess = FormatAccess(beforeElement);
        var afterAccess = FormatAccess(afterElement);
        var accessDiff = ScribanHelpers.FormatDiff(beforeAccess, afterAccess, format);

        var beforeProtocol = FormatProtocol(beforeElement);
        var afterProtocol = FormatProtocol(afterElement);
        var protocolDiff = ScribanHelpers.FormatDiff(beforeProtocol, afterProtocol, format);

        var beforeSourceAddresses = ExtractRawAddresses(beforeElement, "source", providerName, iconProviderRegistry);
        var afterSourceAddresses = ExtractRawAddresses(afterElement, "source", providerName, iconProviderRegistry);
        var sourceAddressesDiff = ScribanHelpers.FormatDiff(beforeSourceAddresses, afterSourceAddresses, format);

        var beforeSourcePorts = FormatPorts(beforeElement, "source");
        var afterSourcePorts = FormatPorts(afterElement, "source");
        var sourcePortsDiff = ScribanHelpers.FormatDiff(beforeSourcePorts, afterSourcePorts, format);

        var beforeDestinationAddresses = ExtractRawAddresses(beforeElement, "destination", providerName, iconProviderRegistry);
        var afterDestinationAddresses = ExtractRawAddresses(afterElement, "destination", providerName, iconProviderRegistry);
        var destinationAddressesDiff = ScribanHelpers.FormatDiff(beforeDestinationAddresses, afterDestinationAddresses, format);

        var beforeDestinationPorts = FormatPorts(beforeElement, "destination");
        var afterDestinationPorts = FormatPorts(afterElement, "destination");
        var destinationPortsDiff = ScribanHelpers.FormatDiff(beforeDestinationPorts, afterDestinationPorts, format);

        var beforeDescription = FormatDescription(beforeElement);
        var afterDescription = FormatDescription(afterElement);
        var descriptionDiff = ScribanHelpers.FormatDiff(beforeDescription, afterDescription, format);

        return new Dictionary<string, string>
        {
            ["name"] = nameDiff,
            ["priority"] = priorityDiff,
            ["direction"] = directionDiff,
            ["access"] = accessDiff,
            ["protocol"] = protocolDiff,
            ["source_addresses"] = sourceAddressesDiff,
            ["source_ports"] = sourcePortsDiff,
            ["destination_addresses"] = destinationAddressesDiff,
            ["destination_ports"] = destinationPortsDiff,
            ["description"] = descriptionDiff
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
        return ScribanHelpers.FormatAttributeValuePlainWithRegistry(
            attributeName,
            value,
            providerName,
            iconProviderRegistry);
    }

    /// <summary>
    /// Extracts raw source or destination addresses with icons but without backtick wrapping (for diff generation).
    /// </summary>
    /// <remarks>
    /// Handles both address_prefix (singular) and address_prefixes (array) properties.
    /// Array takes precedence when both are present.
    /// </remarks>
    private static string ExtractRawAddresses(
        JsonElement element,
        string prefix,
        string providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        // Try address_prefixes array FIRST (takes precedence over singular)
        if (element.TryGetProperty($"{prefix}_address_prefixes", out var prefixesProperty) &&
            prefixesProperty.ValueKind == JsonValueKind.Array)
        {
            var prefixes = prefixesProperty.EnumerateArray()
                .Where(p => p.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(p.GetString()))
                .Select(p => p.GetString() ?? string.Empty)
                .ToList();

            if (prefixes.Count > 0)
            {
                if (prefixes.Count == 1 && prefixes[0] == "*")
                {
                    return "✳️";
                }

                var formatted = new List<string>(prefixes.Count);
                foreach (var p in prefixes)
                {
                    if (p == "*")
                    {
                        formatted.Add("✳️");
                    }
                    else if (IsServiceTag(p))
                    {
                        formatted.Add(p);  // No backticks for service tags in diffs
                    }
                    else
                    {
                        formatted.Add(ScribanHelpers.FormatAttributeValuePlainWithRegistry(
                            $"{prefix}_address_prefix",
                            p,
                            providerName,
                            iconProviderRegistry));
                    }
                }

                if (formatted.Count <= 2)
                {
                    return string.Join(", ", formatted);
                }

                return $"✳️ {formatted.Count} items";
            }
        }

        // Fallback to address_prefix (singular)
        var addressPrefix = JsonStateReader.GetStringProperty(element, $"{prefix}_address_prefix");
        if (!string.IsNullOrEmpty(addressPrefix))
        {
            if (addressPrefix == "*")
            {
                return "✳️";
            }

            // Check if it's a service tag (starts with capital letter, no dots/slashes)
            if (IsServiceTag(addressPrefix))
            {
                return addressPrefix;  // No backticks for service tags in diffs
            }

            return ScribanHelpers.FormatAttributeValuePlainWithRegistry(
                $"{prefix}_address_prefix",
                addressPrefix,
                providerName,
                iconProviderRegistry);
        }

        return "-";
    }
}
