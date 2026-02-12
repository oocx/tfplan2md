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
    /// <returns>The formatted row values with columns: name, priority, direction, access, protocol, source, destination, ports.</returns>
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
        var source = FormatSourceOrDestination(element, "source", providerName, valueFormatterRegistry, iconProviderRegistry);
        var destination = FormatSourceOrDestination(element, "destination", providerName, valueFormatterRegistry, iconProviderRegistry);
        var ports = FormatPorts(element);

        return new Dictionary<string, string>
        {
            ["name"] = name,
            ["priority"] = priority,
            ["direction"] = direction,
            ["access"] = access,
            ["protocol"] = protocol,
            ["source"] = source,
            ["destination"] = destination,
            ["ports"] = ports
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
            "inbound" => "⬇️ Inbound",
            "outbound" => "⬆️ Outbound",
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
            "allow" => "✅ Allow",
            "deny" => "⛔ Deny",
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
            "TCP" => "🔗 TCP",
            "UDP" => "🔗 UDP",
            "ICMP" => "🔗 ICMP",
            "*" => "✳️",
            _ => protocol ?? "-"
        };
    }

    /// <summary>
    /// Formats source or destination address/prefix with support for wildcards and service tags.
    /// </summary>
    private static string FormatSourceOrDestination(
        JsonElement element,
        string prefix,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        // Try address_prefix first
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

        // Try address_prefixes array
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
    /// Formats destination port ranges with 🔌 icon.
    /// </summary>
    private static string FormatPorts(JsonElement element)
    {
        // Try single port first
        var portRange = JsonStateReader.GetStringProperty(element, "destination_port_range");
        if (!string.IsNullOrEmpty(portRange))
        {
            if (portRange == "*")
            {
                return "✳️";
            }
            return $"🔌 {portRange}";
        }

        // Try port ranges array
        if (element.TryGetProperty("destination_port_ranges", out var rangesProperty) &&
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
                    return $"🔌 {string.Join(",", ranges)}";
                }

                return $"✳️ {ranges.Count} ranges";
            }
        }

        return "-";
    }
}
