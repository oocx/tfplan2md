using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Extracts Azure RM DNS record row values for inline child tables.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/azure-rm-batch-specification.md.
/// Supports all DNS record types (A, AAAA, CNAME, MX, NS, PTR, SRV, TXT, CAA) for both public and private zones.
/// </remarks>
internal sealed class AzureRmDnsRecordRowExtractor : IChildRowExtractor
{
    /// <summary>
    /// Extracts the DNS record row values from Azure RM DNS record state.
    /// </summary>
    /// <param name="childState">The child JSON state for the DNS record.</param>
    /// <param name="providerName">The provider name for formatting context.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for formatting values.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <returns>The formatted row values with columns: name, type, ttl, value.</returns>
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

        var name = JsonStateReader.GetStringProperty(element, "name") ?? "-";
        var recordType = InferRecordType(element);
        var ttl = JsonStateReader.GetStringProperty(element, "ttl") ?? "-";
        var value = FormatRecordValue(element, recordType, providerName, valueFormatterRegistry, iconProviderRegistry);

        return new Dictionary<string, string>
        {
            ["name"] = name,
            ["type"] = recordType,
            ["ttl"] = ttl,
            ["value"] = value
        };
    }

    /// <summary>
    /// Infers the DNS record type from the resource type name.
    /// </summary>
    private static string InferRecordType(JsonElement element)
    {
        // Try to get from metadata if available
        if (element.TryGetProperty("_tfplan2md_metadata", out var metadata))
        {
            var resourceType = JsonStateReader.GetStringProperty(metadata, "resource_type");
            if (!string.IsNullOrEmpty(resourceType))
            {
                return ExtractRecordTypeFromResourceType(resourceType);
            }
        }

        // Fallback: check for specific attributes to infer type
        if (element.TryGetProperty("record", out var recordProp) &&
            recordProp.ValueKind == JsonValueKind.String)
        {
            return "CNAME";
        }

        if (element.TryGetProperty("records", out _))
        {
            return "A"; // Default for records array
        }

        return "Unknown";
    }

    /// <summary>
    /// Extracts record type from resource type name (e.g., "azurerm_dns_a_record" → "A").
    /// </summary>
    private static string ExtractRecordTypeFromResourceType(string resourceType)
    {
        // Remove prefixes and suffix
        var cleaned = resourceType;

        if (cleaned.StartsWith("azurerm_private_dns_", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring("azurerm_private_dns_".Length);
        }
        else if (cleaned.StartsWith("azurerm_dns_", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring("azurerm_dns_".Length);
        }

        if (cleaned.EndsWith("_record", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - "_record".Length);
        }

        return cleaned.ToUpperInvariant();
    }

    /// <summary>
    /// Formats record value based on record type.
    /// </summary>
    private static string FormatRecordValue(
        JsonElement element,
        string recordType,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        return recordType.ToUpperInvariant() switch
        {
            "A" or "AAAA" => FormatIpRecords(element, providerName, valueFormatterRegistry, iconProviderRegistry),
            "CNAME" => FormatCnameRecord(element),
            "MX" => FormatMxRecords(element),
            "NS" => FormatNsRecords(element),
            "PTR" => FormatPtrRecords(element),
            "SRV" => FormatSrvRecords(element),
            "TXT" => FormatTxtRecords(element),
            "CAA" => FormatCaaRecords(element),
            _ => "-"
        };
    }

    /// <summary>
    /// Formats A/AAAA record IP addresses.
    /// </summary>
    private static string FormatIpRecords(
        JsonElement element,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (!element.TryGetProperty("records", out var recordsProperty) || recordsProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var ips = new List<string>();
        foreach (var record in recordsProperty.EnumerateArray())
        {
            if (record.ValueKind == JsonValueKind.String)
            {
                var ip = record.GetString();
                if (!string.IsNullOrEmpty(ip))
                {
                    var formatted = ScribanHelpers.FormatAttributeValueTableWithRegistry(
                        "ip_address",
                        ip,
                        providerName,
                        valueFormatterRegistry,
                        iconProviderRegistry);
                    ips.Add(formatted);
                }
            }
        }

        return ips.Count > 0 ? string.Join(", ", ips) : "-";
    }

    /// <summary>
    /// Formats CNAME record target.
    /// </summary>
    private static string FormatCnameRecord(JsonElement element)
    {
        var record = JsonStateReader.GetStringProperty(element, "record");
        return string.IsNullOrEmpty(record) ? "-" : record;
    }

    /// <summary>
    /// Formats MX records (priority + mail server).
    /// </summary>
    private static string FormatMxRecords(JsonElement element)
    {
        if (!element.TryGetProperty("record", out var recordProperty) || recordProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var records = new List<string>();
        foreach (var record in recordProperty.EnumerateArray())
        {
            var preference = JsonStateReader.GetStringProperty(record, "preference") ?? "0";
            var exchange = JsonStateReader.GetStringProperty(record, "exchange") ?? string.Empty;
            if (!string.IsNullOrEmpty(exchange))
            {
                records.Add($"{preference} {exchange}");
            }
        }

        return records.Count > 0 ? string.Join(", ", records) : "-";
    }

    /// <summary>
    /// Formats NS records (nameservers).
    /// </summary>
    private static string FormatNsRecords(JsonElement element)
    {
        if (!element.TryGetProperty("records", out var recordsProperty) || recordsProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var nameservers = recordsProperty.EnumerateArray()
            .Where(r => r.ValueKind == JsonValueKind.String)
            .Select(r => r.GetString() ?? string.Empty)
            .Where(ns => !string.IsNullOrEmpty(ns))
            .ToList();

        return nameservers.Count > 0 ? string.Join(", ", nameservers) : "-";
    }

    /// <summary>
    /// Formats PTR records (reverse DNS targets).
    /// </summary>
    private static string FormatPtrRecords(JsonElement element)
    {
        if (!element.TryGetProperty("records", out var recordsProperty) || recordsProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var targets = recordsProperty.EnumerateArray()
            .Where(r => r.ValueKind == JsonValueKind.String)
            .Select(r => r.GetString() ?? string.Empty)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        return targets.Count > 0 ? string.Join(", ", targets) : "-";
    }

    /// <summary>
    /// Formats SRV records (priority weight:port target).
    /// </summary>
    private static string FormatSrvRecords(JsonElement element)
    {
        if (!element.TryGetProperty("record", out var recordProperty) || recordProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var records = new List<string>();
        foreach (var record in recordProperty.EnumerateArray())
        {
            var priority = JsonStateReader.GetStringProperty(record, "priority") ?? "0";
            var weight = JsonStateReader.GetStringProperty(record, "weight") ?? "0";
            var port = JsonStateReader.GetStringProperty(record, "port") ?? "0";
            var target = JsonStateReader.GetStringProperty(record, "target") ?? string.Empty;

            if (!string.IsNullOrEmpty(target))
            {
                records.Add($"{priority} {weight}:{port} {target}");
            }
        }

        return records.Count > 0 ? string.Join(", ", records) : "-";
    }

    /// <summary>
    /// Formats TXT records (truncated to 50 characters).
    /// </summary>
    private static string FormatTxtRecords(JsonElement element)
    {
        if (!element.TryGetProperty("record", out var recordProperty) || recordProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var records = new List<string>();
        foreach (var record in recordProperty.EnumerateArray())
        {
            var value = JsonStateReader.GetStringProperty(record, "value");
            if (!string.IsNullOrEmpty(value))
            {
                var truncated = value.Length > 50 ? value[..50] + "..." : value;
                records.Add($"\"{truncated}\"");
            }
        }

        return records.Count > 0 ? string.Join(", ", records) : "-";
    }

    /// <summary>
    /// Formats CAA records (flag tag value).
    /// </summary>
    private static string FormatCaaRecords(JsonElement element)
    {
        if (!element.TryGetProperty("record", out var recordProperty) || recordProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var records = new List<string>();
        foreach (var record in recordProperty.EnumerateArray())
        {
            var flags = JsonStateReader.GetStringProperty(record, "flags") ?? "0";
            var tag = JsonStateReader.GetStringProperty(record, "tag") ?? string.Empty;
            var value = JsonStateReader.GetStringProperty(record, "value") ?? string.Empty;

            if (!string.IsNullOrEmpty(tag))
            {
                records.Add($"{flags} {tag} \"{value}\"");
            }
        }

        return records.Count > 0 ? string.Join(", ", records) : "-";
    }

    /// <summary>
    /// Extracts column values with inline diffs for a DNS record that changed between before/after states.
    /// </summary>
    /// <param name="beforeState">The DNS record state before the change.</param>
    /// <param name="afterState">The DNS record state after the change.</param>
    /// <param name="providerName">The provider name for formatting context.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for formatting values.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <param name="largeValueFormat">The preferred format for rendering large value diffs.</param>
    /// <returns>A mapping from column property names to formatted display values with inline diffs.</returns>
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

        var beforeName = JsonStateReader.GetStringProperty(beforeElement, "name") ?? "-";
        var afterName = JsonStateReader.GetStringProperty(afterElement, "name") ?? "-";
        var nameDiff = ScribanHelpers.FormatDiff(beforeName, afterName, format);

        var beforeRecordType = InferRecordType(beforeElement);
        var afterRecordType = InferRecordType(afterElement);
        var typeDiff = ScribanHelpers.FormatDiff(beforeRecordType, afterRecordType, format);

        var beforeTtl = JsonStateReader.GetStringProperty(beforeElement, "ttl") ?? "-";
        var afterTtl = JsonStateReader.GetStringProperty(afterElement, "ttl") ?? "-";
        var ttlDiff = ScribanHelpers.FormatDiff(beforeTtl, afterTtl, format);

        // Extract RAW values without formatting, then format the diff
        // FormatDiff will add HTML styling, so we must NOT pre-format with backticks
        var beforeValue = ExtractRawRecordValue(beforeElement, beforeRecordType, providerName, iconProviderRegistry);
        var afterValue = ExtractRawRecordValue(afterElement, afterRecordType, providerName, iconProviderRegistry);
        var valueDiff = ScribanHelpers.FormatDiff(beforeValue, afterValue, format);

        return new Dictionary<string, string>
        {
            ["name"] = nameDiff,
            ["type"] = typeDiff,
            ["ttl"] = ttlDiff,
            ["value"] = valueDiff
        };
    }

    /// <summary>
    /// Extracts raw record value with icons but without backtick wrapping (for diff generation).
    /// </summary>
    private static string ExtractRawRecordValue(
        JsonElement element,
        string recordType,
        string providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        return recordType.ToUpperInvariant() switch
        {
            "A" or "AAAA" => ExtractRawIpRecords(element, providerName, iconProviderRegistry),
            "CNAME" => FormatCnameRecord(element),  // CNAME doesn't need backticks
            "MX" => FormatMxRecords(element),  // MX format doesn't need backticks
            "NS" => FormatNsRecords(element),  // NS doesn't need backticks
            "PTR" => FormatPtrRecords(element),  // PTR doesn't need backticks
            "SRV" => FormatSrvRecords(element),  // SRV format doesn't need backticks
            "TXT" => FormatTxtRecords(element),  // TXT already has quotes
            "CAA" => FormatCaaRecords(element),  // CAA format doesn't need backticks
            _ => "-"
        };
    }

    /// <summary>
    /// Extracts raw A/AAAA record IP addresses with icons but without backtick wrapping (for diff generation).
    /// </summary>
    private static string ExtractRawIpRecords(
        JsonElement element,
        string providerName,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (!element.TryGetProperty("records", out var recordsProperty) || recordsProperty.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var ips = new List<string>();
        foreach (var record in recordsProperty.EnumerateArray())
        {
            if (record.ValueKind == JsonValueKind.String)
            {
                var ip = record.GetString();
                if (!string.IsNullOrEmpty(ip))
                {
                    var formatted = ScribanHelpers.FormatAttributeValuePlainWithRegistry(
                        "ip_address",
                        ip,
                        providerName,
                        iconProviderRegistry);
                    ips.Add(formatted);
                }
            }
        }

        return ips.Count > 0 ? string.Join(", ", ips) : "-";
    }
}
