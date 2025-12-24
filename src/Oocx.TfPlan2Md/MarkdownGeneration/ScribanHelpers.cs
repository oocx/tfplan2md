using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Oocx.TfPlan2Md.Azure;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Custom Scriban helper functions for template rendering.
/// </summary>
public static class ScribanHelpers
{
    /// <summary>
    /// Registers all custom helper functions with the given ScriptObject.
    /// </summary>
    public static void RegisterHelpers(ScriptObject scriptObject, IPrincipalMapper principalMapper)
    {
        scriptObject.Import("format_diff", new Func<string?, string?, string>(FormatDiff));
        scriptObject.Import("diff_array", new Func<object?, object?, string, ScriptObject>(DiffArray));
        scriptObject.Import("escape_markdown", new Func<string?, string>(EscapeMarkdown));
        scriptObject.Import("format_large_value", new Func<string?, string?, string, string>(FormatLargeValue));
        scriptObject.Import("large_attributes_summary", new Func<object?, string>(LargeAttributesSummary));
        scriptObject.Import("is_large_value", new Func<string?, bool>(IsLargeValue));
        scriptObject.Import("azure_role_name", new Func<string?, string>(AzureRoleDefinitionMapper.GetRoleName));
        scriptObject.Import("azure_scope", new Func<string?, string>(AzureScopeParser.ParseScope));
        scriptObject.Import("azure_principal_name", new Func<string?, string>(p => ResolvePrincipalName(p, principalMapper)));
        scriptObject.Import("azure_scope_info", new Func<string?, ScriptObject>(GetScopeInfo));
        scriptObject.Import("azure_role_info", new Func<string?, string?, ScriptObject>(GetRoleInfo));
        scriptObject.Import("azure_principal_info", new Func<string?, string?, ScriptObject>((id, type) => GetPrincipalInfo(id, type, principalMapper)));
        scriptObject.Import("collect_attributes", new Func<object?, object?, ScriptArray>(CollectAttributes));
    }

    /// <summary>
    /// Escapes only markdown-breaking characters to keep generated tables and headings valid while preserving readability.
    /// Related feature: docs/features/markdown-quality-validation/specification.md
    /// </summary>
    /// <param name="input">The raw value to escape.</param>
    /// <returns>A markdown-safe string with newlines replaced by &lt;br/&gt;.</returns>
    public static string EscapeMarkdown(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var value = input;

        value = value.Replace("\\", "\\\\");
        value = value.Replace("|", "\\|");
        value = value.Replace("`", "\\`");
        value = value.Replace("<", "\\<");
        value = value.Replace(">", "\\>");
        value = value.Replace("&", "&amp;");

        value = value.Replace("\r\n", "<br/>");
        value = value.Replace("\n", "<br/>");
        value = value.Replace("\r", "<br/>");

        return value;
    }

    /// <summary>
    /// Determines whether a value should be treated as large based on newlines or length.
    /// Related feature: docs/features/large-attribute-value-display/specification.md
    /// </summary>
    /// <param name="input">The raw value.</param>
    /// <returns>True when the value contains newlines or exceeds 100 characters; otherwise false.</returns>
    public static bool IsLargeValue(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        if (input.Contains('\n', StringComparison.Ordinal) || input.Contains('\r', StringComparison.Ordinal))
        {
            return true;
        }

        return input.Length > 100;
    }

    /// <summary>
    /// Formats large attribute values according to the requested rendering format.
    /// Related feature: docs/features/large-attribute-value-display/specification.md
    /// </summary>
    /// <param name="before">The previous value; null indicates creation.</param>
    /// <param name="after">The new value; null indicates deletion.</param>
    /// <param name="format">The rendering format ("inline-diff" or "standard-diff").</param>
    /// <returns>Markdown string containing the formatted value.</returns>
    public static string FormatLargeValue(string? before, string? after, string format)
    {
        var parsedFormat = ParseLargeValueFormat(format);

        if (after is null && before is null)
        {
            return string.Empty;
        }

        if (after is null)
        {
            // Delete
            return CodeFence(before ?? string.Empty);
        }

        if (before is null)
        {
            // Create
            return CodeFence(after);
        }

        return parsedFormat switch
        {
            LargeValueFormat.StandardDiff => BuildStandardDiff(before, after),
            _ => BuildInlineDiff(before, after)
        };
    }

    /// <summary>
    /// Builds the summary string for a set of large attributes.
    /// Related feature: docs/features/large-attribute-value-display/specification.md
    /// </summary>
    /// <param name="attributes">Collection of attribute change objects (ScriptArray) with name/before/after.</param>
    /// <returns>Summary string like "Large values: policy (3 lines, 2 changed)" or empty when none.</returns>
    public static string LargeAttributesSummary(object? attributes)
    {
        var attrList = ToAttributeList(attributes);
        if (attrList.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var attr in attrList)
        {
            var before = attr.Before ?? string.Empty;
            var after = attr.After ?? string.Empty;

            var totalLines = CountTotalLines(before, after);
            var changedLines = CountChangedLines(before, after);

            var totalLabel = totalLines == 1 ? "line" : "lines";
            var changedLabel = changedLines == 1 ? "changed" : "changed"; // label text stays the same, but kept for clarity
            parts.Add($"{attr.Name} ({totalLines} {totalLabel}, {changedLines} {changedLabel})");
        }

        return $"Large values: {string.Join(", ", parts)}";
    }

    private static LargeValueFormat ParseLargeValueFormat(string format)
    {
        var normalized = (format ?? string.Empty).Trim().ToLowerInvariant();
        var compact = normalized
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);

        return compact switch
        {
            "" => LargeValueFormat.InlineDiff,
            "inlinediff" => LargeValueFormat.InlineDiff,
            "standarddiff" => LargeValueFormat.StandardDiff,
            _ => throw new ScribanHelperException("Unsupported large value format. Use 'inline-diff' or 'standard-diff'.")
        };
    }

    private static string CodeFence(string content, string? language = null)
    {
        var fenceLang = string.IsNullOrWhiteSpace(language) ? string.Empty : language;
        var sb = new StringBuilder();
        sb.Append("```");
        sb.AppendLine(fenceLang);
        sb.AppendLine(content);
        sb.Append("```");
        return sb.ToString();
    }

    private static string BuildStandardDiff(string before, string after)
    {
        var sb = new StringBuilder();
        sb.AppendLine("```diff");

        foreach (var line in SplitLines(before))
        {
            sb.Append("- ");
            sb.AppendLine(line);
        }

        foreach (var line in SplitLines(after))
        {
            sb.Append("+ ");
            sb.AppendLine(line);
        }

        sb.Append("```");
        return sb.ToString();
    }

    private static string[] SplitLines(string value)
    {
        return value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);
    }

    private static string BuildInlineDiff(string before, string after)
    {
        var beforeLines = SplitLines(before);
        var afterLines = SplitLines(after);

        var commonLength = ComputeLcsLength(beforeLines, afterLines);
        if (commonLength == 0)
        {
            // Complete replacement fallback
            var sbNoCommon = new StringBuilder();
            sbNoCommon.AppendLine("**Before:**");
            sbNoCommon.AppendLine(CodeFence(before));
            sbNoCommon.AppendLine();
            sbNoCommon.AppendLine("**After:**");
            sbNoCommon.Append(CodeFence(after));
            return sbNoCommon.ToString();
        }

        var diff = BuildLineDiff(beforeLines, afterLines);
        var sb = new StringBuilder();
        sb.Append("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>");

        var index = 0;
        while (index < diff.Count)
        {
            var entry = diff[index];
            if (entry.Kind == DiffKind.Unchanged)
            {
                sb.Append(HtmlEncode(entry.Text));
                sb.Append('\n');
                index++;
                continue;
            }

            if (entry.Kind == DiffKind.Removed && index + 1 < diff.Count && diff[index + 1].Kind == DiffKind.Added)
            {
                var addEntry = diff[index + 1];
                AppendStyledLineWithCharDiff(sb, entry.Text, addEntry.Text, removed: true);
                AppendStyledLineWithCharDiff(sb, addEntry.Text, entry.Text, removed: false);
                index += 2;
                continue;
            }

            if (entry.Kind == DiffKind.Removed)
            {
                AppendStyledLine(sb, entry.Text, removed: true);
                index++;
                continue;
            }

            if (entry.Kind == DiffKind.Added)
            {
                AppendStyledLine(sb, entry.Text, removed: false);
                index++;
                continue;
            }
        }

        sb.Append("</code></pre>");
        return sb.ToString();
    }

    private static int CountTotalLines(string before, string after)
    {
        var beforeLines = SplitLines(before);
        var afterLines = SplitLines(after);
        var set = new HashSet<string>(beforeLines, StringComparer.Ordinal);
        foreach (var line in afterLines)
        {
            set.Add(line);
        }

        return set.Count;
    }

    private static int CountChangedLines(string before, string after)
    {
        var beforeLines = SplitLines(before);
        var afterLines = SplitLines(after);
        var diff = BuildLineDiff(beforeLines, afterLines);
        return diff.Count(d => d.Kind != DiffKind.Unchanged);
    }

    private static void AppendStyledLine(StringBuilder sb, string line, bool removed)
    {
        var lineStyle = removed
            ? "background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;"
            : "background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;";

        sb.Append("<span style=\"");
        sb.Append(lineStyle);
        sb.Append('\"');
        sb.Append('>');
        sb.Append(HtmlEncode(line));
        sb.AppendLine("</span>");
    }

    private static void AppendStyledLineWithCharDiff(StringBuilder sb, string line, string otherLine, bool removed)
    {
        var pairs = ComputeLcsPairs(line, otherLine);
        var commonMask = BuildCommonMask(line.Length, pairs.Select(p => p.BeforeIndex));
        var highlightColor = removed ? "#ffc0c0" : "#acf2bd";
        var lineStyle = removed
            ? "background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;"
            : "background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;";

        sb.Append("<span style=\"");
        sb.Append(lineStyle);
        sb.Append('\"');
        sb.Append('>');
        sb.Append(ApplyCharHighlights(line, commonMask, highlightColor));
        sb.AppendLine("</span>");
    }

    private static string ApplyCharHighlights(string line, bool[] commonMask, string highlightColor)
    {
        var sb = new StringBuilder();
        var inHighlight = false;

        for (var i = 0; i < line.Length; i++)
        {
            var isCommon = i < commonMask.Length && commonMask[i];
            if (!isCommon && !inHighlight)
            {
                sb.Append("<span style=\"background-color: ");
                sb.Append(highlightColor);
                sb.Append("; color: #24292e;\">");
                inHighlight = true;
            }
            else if (isCommon && inHighlight)
            {
                sb.Append("</span>");
                inHighlight = false;
            }

            sb.Append(HtmlEncode(line[i].ToString()));
        }

        if (inHighlight)
        {
            sb.Append("</span>");
        }

        return sb.ToString();
    }

    private static List<DiffEntry> BuildLineDiff(string[] before, string[] after)
    {
        var pairs = ComputeLcsPairs(before, after);
        var result = new List<DiffEntry>();

        var beforeIndex = 0;
        var afterIndex = 0;

        foreach (var pair in pairs)
        {
            while (beforeIndex < pair.BeforeIndex)
            {
                result.Add(new DiffEntry(DiffKind.Removed, before[beforeIndex]));
                beforeIndex++;
            }

            while (afterIndex < pair.AfterIndex)
            {
                result.Add(new DiffEntry(DiffKind.Added, after[afterIndex]));
                afterIndex++;
            }

            result.Add(new DiffEntry(DiffKind.Unchanged, before[pair.BeforeIndex]));
            beforeIndex++;
            afterIndex++;
        }

        while (beforeIndex < before.Length)
        {
            result.Add(new DiffEntry(DiffKind.Removed, before[beforeIndex]));
            beforeIndex++;
        }

        while (afterIndex < after.Length)
        {
            result.Add(new DiffEntry(DiffKind.Added, after[afterIndex]));
            afterIndex++;
        }

        return result;
    }

    private static List<AttributeChangeInfo> ToAttributeList(object? attributes)
    {
        var list = new List<AttributeChangeInfo>();

        if (attributes is null)
        {
            return list;
        }

        if (attributes is IEnumerable enumerable && attributes is not string)
        {
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                list.Add(ToAttributeChangeInfo(item));
            }
        }

        return list;
    }

    private static AttributeChangeInfo ToAttributeChangeInfo(object item)
    {
        if (item is ScriptObject obj)
        {
            var name = obj.TryGetValue("name", out var n) ? n?.ToString() ?? string.Empty : string.Empty;
            var before = obj.TryGetValue("before", out var b) ? b?.ToString() : null;
            var after = obj.TryGetValue("after", out var a) ? a?.ToString() : null;

            return new AttributeChangeInfo(name, before, after);
        }

        var type = item.GetType();
        var nameProp = type.GetProperty("Name");
        var beforeProp = type.GetProperty("Before");
        var afterProp = type.GetProperty("After");

        var resolvedName = nameProp?.GetValue(item)?.ToString() ?? string.Empty;
        var resolvedBefore = beforeProp?.GetValue(item)?.ToString();
        var resolvedAfter = afterProp?.GetValue(item)?.ToString();

        return new AttributeChangeInfo(resolvedName, resolvedBefore, resolvedAfter);
    }

    private static List<LcsPair> ComputeLcsPairs(string[] before, string[] after)
    {
        var m = before.Length;
        var n = after.Length;
        var lengths = new int[m + 1, n + 1];

        for (var i = m - 1; i >= 0; i--)
        {
            for (var j = n - 1; j >= 0; j--)
            {
                if (string.Equals(before[i], after[j], StringComparison.Ordinal))
                {
                    lengths[i, j] = lengths[i + 1, j + 1] + 1;
                }
                else
                {
                    lengths[i, j] = Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
                }
            }
        }

        var pairs = new List<LcsPair>();
        var x = 0;
        var y = 0;
        while (x < m && y < n)
        {
            if (string.Equals(before[x], after[y], StringComparison.Ordinal))
            {
                pairs.Add(new LcsPair(x, y));
                x++;
                y++;
            }
            else if (lengths[x + 1, y] >= lengths[x, y + 1])
            {
                x++;
            }
            else
            {
                y++;
            }
        }

        return pairs;
    }

    private static int ComputeLcsLength(string[] before, string[] after)
    {
        return ComputeLcsPairs(before, after).Count;
    }

    private static List<LcsPair> ComputeLcsPairs(string before, string after)
    {
        var m = before.Length;
        var n = after.Length;
        var lengths = new int[m + 1, n + 1];

        for (var i = m - 1; i >= 0; i--)
        {
            for (var j = n - 1; j >= 0; j--)
            {
                if (before[i] == after[j])
                {
                    lengths[i, j] = lengths[i + 1, j + 1] + 1;
                }
                else
                {
                    lengths[i, j] = Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
                }
            }
        }

        var pairs = new List<LcsPair>();
        var x = 0;
        var y = 0;
        while (x < m && y < n)
        {
            if (before[x] == after[y])
            {
                pairs.Add(new LcsPair(x, y));
                x++;
                y++;
            }
            else if (lengths[x + 1, y] >= lengths[x, y + 1])
            {
                x++;
            }
            else
            {
                y++;
            }
        }

        return pairs;
    }

    private static bool[] BuildCommonMask(int length, IEnumerable<int> indices)
    {
        var mask = new bool[length];
        foreach (var index in indices)
        {
            if (index >= 0 && index < length)
            {
                mask[index] = true;
            }
        }

        return mask;
    }

    private static string HtmlEncode(string value)
    {
        return HtmlEncoder.Default.Encode(value);
    }

    private readonly record struct DiffEntry(DiffKind Kind, string Text);

    private readonly record struct AttributeChangeInfo(string Name, string? Before, string? After);

    private enum DiffKind
    {
        Unchanged,
        Removed,
        Added
    }

    private readonly record struct LcsPair(int BeforeIndex, int AfterIndex);

    private static ScriptObject GetScopeInfo(string? scope)
    {
        var info = AzureScopeParser.Parse(scope);

        return new ScriptObject
        {
            ["name"] = info.Name,
            ["type"] = info.Type,
            ["subscription_id"] = info.SubscriptionId ?? string.Empty,
            ["resource_group"] = info.ResourceGroup ?? string.Empty,
            ["level"] = info.Level.ToString(),
            ["summary"] = info.Summary,
            ["summary_label"] = info.SummaryLabel,
            ["summary_name"] = info.SummaryName,
            ["details"] = info.Details
        };
    }

    private static ScriptObject GetRoleInfo(string? roleDefinitionId, string? roleDefinitionName)
    {
        var info = AzureRoleDefinitionMapper.GetRoleDefinition(roleDefinitionId, roleDefinitionName);

        return new ScriptObject
        {
            ["name"] = info.Name,
            ["id"] = info.Id,
            ["full_name"] = info.FullName
        };
    }

    private static ScriptObject GetPrincipalInfo(string? principalId, string? principalType, IPrincipalMapper principalMapper)
    {
        var id = principalId ?? string.Empty;
        var type = principalType ?? string.Empty;
        var name = principalMapper.GetName(id, type) ?? id;
        var fullName = BuildPrincipalFullName(name, id, type);

        return new ScriptObject
        {
            ["name"] = name,
            ["id"] = id,
            ["type"] = type,
            ["full_name"] = fullName
        };
    }

    private static ScriptArray CollectAttributes(object? before, object? after)
    {
        var beforeDict = ToDictionary(before);
        var afterDict = ToDictionary(after);
        var keys = beforeDict.Keys.Union(afterDict.Keys).ToList();

        var attributes = new ScriptArray();
        foreach (var key in keys)
        {
            beforeDict.TryGetValue(key, out var beforeValue);
            afterDict.TryGetValue(key, out var afterValue);

            if (IsNullValue(beforeValue) && IsNullValue(afterValue))
            {
                continue;
            }

            attributes.Add(key);
        }

        return attributes;
    }

    /// <summary>
    /// Formats a before/after pair into a diff-style string while preserving intended line breaks.
    /// </summary>
    /// <param name="before">The original value.</param>
    /// <param name="after">The updated value.</param>
    /// <returns>
    /// The escaped updated value when the inputs are equal; otherwise "- escapedBefore&lt;br&gt;+ escapedAfter" with values escaped but the line break tag preserved.
    /// </returns>
    /// <remarks>Related feature: docs/features/firewall-rule-before-after-display/specification.md</remarks>
    /// <example>
    /// <code>
    /// FormatDiff("TCP", "UDP"); // returns "- TCP<br>+ UDP"
    /// FormatDiff("|before|", "|after|"); // returns "- \\|before\\|<br>+ \\|after\\|"
    /// </code>
    /// </example>
    public static string FormatDiff(string? before, string? after)
    {
        var beforeValue = before ?? string.Empty;
        var afterValue = after ?? string.Empty;

        var escapedBefore = EscapeMarkdown(beforeValue);
        var escapedAfter = EscapeMarkdown(afterValue);

        if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
        {
            return escapedAfter;
        }

        return $"- {escapedBefore}<br>+ {escapedAfter}";
    }

    /// <summary>
    /// Computes semantic differences between two arrays using a key property.
    /// </summary>
    /// <param name="beforeArray">Array from current state.</param>
    /// <param name="afterArray">Array from planned state.</param>
    /// <param name="keyProperty">Property name to identify items (e.g., "name").</param>
    /// <returns>Object with added, removed, modified, and unchanged collections.</returns>
    /// <exception cref="ScribanHelperException">Thrown when an item lacks the key property.</exception>
    public static ScriptObject DiffArray(object? beforeArray, object? afterArray, string keyProperty)
    {
        var beforeItems = ExtractArrayItems(beforeArray, keyProperty, "before");
        var afterItems = ExtractArrayItems(afterArray, keyProperty, "after");

        var beforeKeys = beforeItems.Keys.ToHashSet();
        var afterKeys = afterItems.Keys.ToHashSet();

        var addedKeys = afterKeys.Except(beforeKeys);
        var removedKeys = beforeKeys.Except(afterKeys);
        var commonKeys = beforeKeys.Intersect(afterKeys);

        var added = new ScriptArray();
        var removed = new ScriptArray();
        var modified = new ScriptArray();
        var unchanged = new ScriptArray();

        foreach (var key in addedKeys)
        {
            added.Add(afterItems[key]);
        }

        foreach (var key in removedKeys)
        {
            removed.Add(beforeItems[key]);
        }

        foreach (var key in commonKeys)
        {
            var beforeItem = beforeItems[key];
            var afterItem = afterItems[key];

            if (AreItemsEqual(beforeItem, afterItem))
            {
                unchanged.Add(afterItem);
            }
            else
            {
                var modifiedItem = new ScriptObject
                {
                    ["before"] = beforeItem,
                    ["after"] = afterItem
                };
                modified.Add(modifiedItem);
            }
        }

        var result = new ScriptObject
        {
            ["added"] = added,
            ["removed"] = removed,
            ["modified"] = modified,
            ["unchanged"] = unchanged
        };

        return result;
    }

    private static Dictionary<string, ScriptObject> ExtractArrayItems(
        object? array,
        string keyProperty,
        string arrayName)
    {
        var result = new Dictionary<string, ScriptObject>();

        if (array is null)
        {
            return result;
        }

        if (array is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            var index = 0;
            foreach (var element in jsonElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    index++;
                    continue;
                }

                if (!element.TryGetProperty(keyProperty, out var keyElement))
                {
                    throw new ScribanHelperException(
                        $"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
                }

                var key = keyElement.ToString();
                var scriptObj = JsonElementToScriptObject(element);
                result[key] = scriptObj;
                index++;
            }
        }
        else if (array is ScriptArray scriptArray)
        {
            var index = 0;
            foreach (var item in scriptArray)
            {
                if (item is ScriptObject scriptObj)
                {
                    if (!scriptObj.TryGetValue(keyProperty, out var keyValue) || keyValue is null)
                    {
                        throw new ScribanHelperException(
                            $"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
                    }

                    var key = keyValue.ToString()!;
                    result[key] = scriptObj;
                }
                else if (item is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    if (!element.TryGetProperty(keyProperty, out var keyElement))
                    {
                        throw new ScribanHelperException(
                            $"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
                    }

                    var key = keyElement.ToString();
                    var converted = JsonElementToScriptObject(element);
                    result[key] = converted;
                }

                index++;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a JsonElement to a Scriban-compatible object (ScriptObject or ScriptArray).
    /// </summary>
    /// <param name="element">The JsonElement to convert.</param>
    /// <returns>A ScriptObject, ScriptArray, or primitive value.</returns>
    public static object? ConvertToScriptObject(JsonElement element)
    {
        return ConvertJsonValue(element);
    }

    private static ScriptObject JsonElementToScriptObject(JsonElement element)
    {
        var scriptObj = new ScriptObject();

        if (element.ValueKind != JsonValueKind.Object)
        {
            return scriptObj;
        }

        foreach (var property in element.EnumerateObject())
        {
            scriptObj[property.Name] = ConvertJsonValue(property.Value);
        }

        return scriptObj;
    }

    private static string ResolvePrincipalName(string? principalId, IPrincipalMapper principalMapper)
    {
        if (principalId is null)
        {
            return string.Empty;
        }

        return principalMapper.GetPrincipalName(principalId);
    }

    private static string BuildPrincipalFullName(string name, string? principalId, string? principalType)
    {
        var typePart = string.IsNullOrWhiteSpace(principalType) ? string.Empty : $" ({principalType})";
        var idPart = string.IsNullOrWhiteSpace(principalId) ? string.Empty : $" [{principalId}]";
        return $"{name}{typePart}{idPart}".Trim();
    }

    private static Dictionary<string, object?> ToDictionary(object? obj)
    {
        var result = new Dictionary<string, object?>();

        if (obj is null)
        {
            return result;
        }

        if (obj is JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ConvertJsonValue(property.Value);
            }

            return result;
        }

        if (obj is ScriptObject scriptObject)
        {
            foreach (var key in scriptObject.Keys)
            {
                result[key] = scriptObject[key];
            }
        }

        return result;
    }

    private static bool IsNullValue(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Null;
        }

        return false;
    }

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => JsonElementToScriptObject(element),
            JsonValueKind.Array => ConvertJsonArray(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static ScriptArray ConvertJsonArray(JsonElement element)
    {
        var array = new ScriptArray();
        foreach (var item in element.EnumerateArray())
        {
            array.Add(ConvertJsonValue(item));
        }
        return array;
    }

    private static bool AreItemsEqual(ScriptObject before, ScriptObject after)
    {
        var beforeKeys = before.Keys.ToHashSet();
        var afterKeys = after.Keys.ToHashSet();

        if (!beforeKeys.SetEquals(afterKeys))
        {
            return false;
        }

        foreach (var key in beforeKeys)
        {
            var beforeValue = before[key];
            var afterValue = after[key];

            if (!AreValuesEqual(beforeValue, afterValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreValuesEqual(object? before, object? after)
    {
        if (before is null && after is null)
        {
            return true;
        }

        if (before is null || after is null)
        {
            return false;
        }

        if (before is ScriptObject beforeObj && after is ScriptObject afterObj)
        {
            return AreItemsEqual(beforeObj, afterObj);
        }

        if (before is ScriptArray beforeArr && after is ScriptArray afterArr)
        {
            if (beforeArr.Count != afterArr.Count)
            {
                return false;
            }

            for (var i = 0; i < beforeArr.Count; i++)
            {
                if (!AreValuesEqual(beforeArr[i], afterArr[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return before.Equals(after);
    }
}

/// <summary>
/// Exception thrown when a Scriban helper function encounters an error.
/// </summary>
public class ScribanHelperException : Exception
{
    public ScribanHelperException(string message) : base(message)
    {
    }

    public ScribanHelperException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
