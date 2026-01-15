using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds <see cref="VariableGroupViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md
/// </summary>
internal static class VariableGroupViewModelFactory
{
    /// <summary>
    /// Creates a view model for the provided variable group change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="providerName">The provider name for semantic formatting.</param>
    /// <param name="largeValueFormat">Preferred large value format for diff rendering.</param>
    /// <returns>Populated <see cref="VariableGroupViewModel"/>.</returns>
    public static VariableGroupViewModel Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat)
    {
        _ = providerName; // Not used for Azure DevOps variables

        var name = ExtractName(change.Change.After) ?? ExtractName(change.Change.Before);
        var description = ExtractDescription(change.Change.After) ?? ExtractDescription(change.Change.Before);

        var beforeVariables = ExtractVariables(change.Change.Before);
        var afterVariables = ExtractVariables(change.Change.After);

        var keyVaultBlocks = ExtractKeyVaultBlocks(change.Change.After) ?? ExtractKeyVaultBlocks(change.Change.Before);

        // Determine action type (create, update, delete)
        var actions = change.Change.Actions ?? Array.Empty<string>();
        var isCreate = actions.Contains("create") && !actions.Contains("delete");
        var isDelete = actions.Contains("delete") && !actions.Contains("create");

        if (isCreate)
        {
            return new VariableGroupViewModel
            {
                Name = name,
                Description = description,
                AfterVariables = FormatVariableRows(afterVariables),
                KeyVaultBlocks = keyVaultBlocks
            };
        }
        else if (isDelete)
        {
            return new VariableGroupViewModel
            {
                Name = name,
                Description = description,
                BeforeVariables = FormatVariableRows(beforeVariables),
                KeyVaultBlocks = keyVaultBlocks
            };
        }
        else // update or replace
        {
            var added = BuildAdded(afterVariables, beforeVariables);
            var removed = BuildRemoved(beforeVariables, afterVariables);
            var modified = BuildModified(beforeVariables, afterVariables, largeValueFormat);
            var unchanged = BuildUnchanged(beforeVariables, afterVariables);

            var changeRows = new List<VariableChangeRowViewModel>();
            changeRows.AddRange(added);
            changeRows.AddRange(modified);
            changeRows.AddRange(removed);
            changeRows.AddRange(unchanged);

            return new VariableGroupViewModel
            {
                Name = name,
                Description = description,
                VariableChanges = changeRows,
                AfterVariables = FormatVariableRows(afterVariables),
                BeforeVariables = FormatVariableRows(beforeVariables),
                KeyVaultBlocks = keyVaultBlocks
            };
        }
    }

    /// <summary>
    /// Extracts the variable group name from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Name value when present; otherwise null.</returns>
    private static string? ExtractName(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty("name", out var nameProperty) && nameProperty.ValueKind == JsonValueKind.String
            ? nameProperty.GetString()
            : null;
    }

    /// <summary>
    /// Extracts the variable group description from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Description value when present; otherwise null.</returns>
    private static string? ExtractDescription(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty("description", out var descProperty) && descProperty.ValueKind == JsonValueKind.String
            ? descProperty.GetString()
            : null;
    }

    /// <summary>
    /// Extracts and merges variables from both variable and secret_variable arrays.
    /// </summary>
    /// <param name="state">Terraform state object containing variable arrays.</param>
    /// <returns>Collection of extracted variable values.</returns>
    private static IReadOnlyList<VariableValues> ExtractVariables(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<VariableValues>();
        }

        var variables = new List<VariableValues>();

        // Extract regular variables
        if (element.TryGetProperty("variable", out var varsElement) && varsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var varElement in varsElement.EnumerateArray())
            {
                if (varElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = GetString(varElement, "name");
                var value = GetString(varElement, "value");
                var enabled = GetNullableBool(varElement, "enabled");
                var contentType = GetString(varElement, "content_type");
                var expires = GetString(varElement, "expires");

                variables.Add(new VariableValues(name, value, enabled, contentType, expires, false));
            }
        }

        // Extract secret variables
        if (element.TryGetProperty("secret_variable", out var secretVarsElement) && secretVarsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var varElement in secretVarsElement.EnumerateArray())
            {
                if (varElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = GetString(varElement, "name");
                var value = GetString(varElement, "value");
                var enabled = GetNullableBool(varElement, "enabled");
                var contentType = GetString(varElement, "content_type");
                var expires = GetString(varElement, "expires");

                variables.Add(new VariableValues(name, value, enabled, contentType, expires, true));
            }
        }

        return variables;
    }

    /// <summary>
    /// Extracts Key Vault blocks from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Collection of Key Vault block ViewModels.</returns>
    private static IReadOnlyList<KeyVaultRowViewModel> ExtractKeyVaultBlocks(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<KeyVaultRowViewModel>();
        }

        if (!element.TryGetProperty("key_vault", out var kvElement) || kvElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<KeyVaultRowViewModel>();
        }

        var blocks = new List<KeyVaultRowViewModel>();
        foreach (var blockElement in kvElement.EnumerateArray())
        {
            if (blockElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = GetString(blockElement, "name");
            var serviceEndpointId = GetString(blockElement, "service_endpoint_id");
            var searchDepth = GetString(blockElement, "search_depth");

            blocks.Add(new KeyVaultRowViewModel
            {
                Name = ScribanHelpers.FormatAttributeValueTable("name", name, null),
                ServiceEndpointId = ScribanHelpers.FormatAttributeValueTable("service_endpoint_id", serviceEndpointId, null),
                SearchDepth = ScribanHelpers.FormatAttributeValueTable("search_depth", searchDepth, null)
            });
        }

        return blocks;
    }

    /// <summary>
    /// Builds change rows for variables that only exist in the after state.
    /// </summary>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <returns>Ordered added variable rows.</returns>
    private static List<VariableChangeRowViewModel> BuildAdded(
        IReadOnlyList<VariableValues> afterVariables,
        IReadOnlyList<VariableValues> beforeVariables)
    {
        var beforeNames = new HashSet<string>(beforeVariables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        return afterVariables
            .Where(variable => !beforeNames.Contains(variable.Name))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(CreateAddedRow)
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that only exist in the before state.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <returns>Ordered removed variable rows.</returns>
    private static List<VariableChangeRowViewModel> BuildRemoved(
        IReadOnlyList<VariableValues> beforeVariables,
        IReadOnlyList<VariableValues> afterVariables)
    {
        var afterNames = new HashSet<string>(afterVariables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        return beforeVariables
            .Where(variable => !afterNames.Contains(variable.Name))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(CreateRemovedRow)
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that exist in both states but differ.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <param name="largeValueFormat">Preferred diff format.</param>
    /// <returns>Ordered modified variable rows.</returns>
    private static List<VariableChangeRowViewModel> BuildModified(
        IReadOnlyList<VariableValues> beforeVariables,
        IReadOnlyList<VariableValues> afterVariables,
        LargeValueFormat largeValueFormat)
    {
        var beforeLookup = beforeVariables.ToDictionary(v => v.Name, StringComparer.OrdinalIgnoreCase);

        return afterVariables
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && !VariablesEqual(before!, after))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(after => CreateDiffRow(beforeLookup[after.Name], after, largeValueFormat))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that remain unchanged between states.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <returns>Ordered unchanged variable rows.</returns>
    private static List<VariableChangeRowViewModel> BuildUnchanged(
        IReadOnlyList<VariableValues> beforeVariables,
        IReadOnlyList<VariableValues> afterVariables)
    {
        var beforeLookup = beforeVariables.ToDictionary(v => v.Name, StringComparer.OrdinalIgnoreCase);

        return afterVariables
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && VariablesEqual(before!, after))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(CreateUnchangedRow)
            .ToList();
    }

    /// <summary>
    /// Formats variable values for create/delete tables.
    /// </summary>
    /// <param name="variables">Raw variable values.</param>
    /// <returns>Formatted variable rows.</returns>
    private static List<VariableRowViewModel> FormatVariableRows(
        IReadOnlyList<VariableValues> variables)
    {
        return variables
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(variable => new VariableRowViewModel
            {
                Name = $"`{ScribanHelpers.EscapeMarkdown(variable.Name)}`",
                Value = FormatVariableValue(variable),
                Enabled = FormatEnabled(variable.Enabled),
                ContentType = FormatOptionalString(variable.ContentType),
                Expires = FormatOptionalString(variable.Expires),
                IsLargeValue = IsLargeValue(variable)
            })
            .ToList();
    }

    /// <summary>
    /// Creates a formatted row for an added variable.
    /// </summary>
    /// <param name="variable">Variable values from the after state.</param>
    /// <returns>Formatted change row.</returns>
    private static VariableChangeRowViewModel CreateAddedRow(VariableValues variable)
    {
        return new VariableChangeRowViewModel
        {
            Change = "➕",
            Name = $"`{ScribanHelpers.EscapeMarkdown(variable.Name)}`",
            Value = FormatVariableValue(variable),
            Enabled = FormatEnabled(variable.Enabled),
            ContentType = FormatOptionalString(variable.ContentType),
            Expires = FormatOptionalString(variable.Expires),
            IsLargeValue = IsLargeValue(variable)
        };
    }

    /// <summary>
    /// Creates a formatted row for a removed variable.
    /// </summary>
    /// <param name="variable">Variable values from the before state.</param>
    /// <returns>Formatted change row.</returns>
    private static VariableChangeRowViewModel CreateRemovedRow(VariableValues variable)
    {
        return new VariableChangeRowViewModel
        {
            Change = "❌",
            Name = $"`{ScribanHelpers.EscapeMarkdown(variable.Name)}`",
            Value = FormatVariableValue(variable),
            Enabled = FormatEnabled(variable.Enabled),
            ContentType = FormatOptionalString(variable.ContentType),
            Expires = FormatOptionalString(variable.Expires),
            IsLargeValue = IsLargeValue(variable)
        };
    }

    /// <summary>
    /// Creates a formatted row for an unchanged variable.
    /// </summary>
    /// <param name="variable">Variable values.</param>
    /// <returns>Formatted change row.</returns>
    private static VariableChangeRowViewModel CreateUnchangedRow(VariableValues variable)
    {
        return new VariableChangeRowViewModel
        {
            Change = "⏺️",
            Name = $"`{ScribanHelpers.EscapeMarkdown(variable.Name)}`",
            Value = FormatVariableValue(variable),
            Enabled = FormatEnabled(variable.Enabled),
            ContentType = FormatOptionalString(variable.ContentType),
            Expires = FormatOptionalString(variable.Expires),
            IsLargeValue = IsLargeValue(variable)
        };
    }

    /// <summary>
    /// Creates a formatted diff row for a modified variable.
    /// </summary>
    /// <param name="before">Variable values before the change.</param>
    /// <param name="after">Variable values after the change.</param>
    /// <param name="largeValueFormat">Preferred diff format.</param>
    /// <returns>Formatted diff row.</returns>
    private static VariableChangeRowViewModel CreateDiffRow(
        VariableValues before,
        VariableValues after,
        LargeValueFormat largeValueFormat)
    {
        var format = largeValueFormat.ToString();

        // For secret variables, always show masked value (no diff)
        var valueDisplay = after.IsSecret
            ? "`(sensitive / hidden)`"
            : ScribanHelpers.FormatDiff(before.Value, after.Value, format);

        return new VariableChangeRowViewModel
        {
            Change = "🔄",
            Name = $"`{ScribanHelpers.EscapeMarkdown(after.Name)}`",
            Value = valueDisplay,
            Enabled = FormatEnabledDiff(before.Enabled, after.Enabled, format),
            ContentType = FormatOptionalDiff(before.ContentType, after.ContentType, format),
            Expires = FormatOptionalDiff(before.Expires, after.Expires, format),
            IsLargeValue = IsLargeValue(after)
        };
    }

    /// <summary>
    /// Formats a variable value, showing "(sensitive / hidden)" for secrets.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>Formatted value.</returns>
    private static string FormatVariableValue(VariableValues variable)
    {
        if (variable.IsSecret)
        {
            return "`(sensitive / hidden)`";
        }

        if (string.IsNullOrEmpty(variable.Value))
        {
            return "-";
        }

        return $"`{ScribanHelpers.EscapeMarkdown(variable.Value)}`";
    }

    /// <summary>
    /// Formats an enabled boolean value or displays dash for null.
    /// </summary>
    /// <param name="enabled">The enabled value.</param>
    /// <returns>Formatted string.</returns>
    private static string FormatEnabled(bool? enabled)
    {
        if (enabled == null)
        {
            return "-";
        }

        return enabled.Value ? "`true`" : "`false`";
    }

    /// <summary>
    /// Formats an enabled boolean diff.
    /// </summary>
    /// <param name="before">Before value.</param>
    /// <param name="after">After value.</param>
    /// <param name="format">Diff format.</param>
    /// <returns>Formatted diff or single value.</returns>
    private static string FormatEnabledDiff(bool? before, bool? after, string format)
    {
        var beforeStr = before == null ? "" : (before.Value ? "true" : "false");
        var afterStr = after == null ? "" : (after.Value ? "true" : "false");

        if (beforeStr == afterStr)
        {
            return FormatEnabled(after);
        }

        return ScribanHelpers.FormatDiff(beforeStr, afterStr, format);
    }

    /// <summary>
    /// Formats an optional string value (content_type, expires).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Formatted string or dash.</returns>
    private static string FormatOptionalString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        return $"`{ScribanHelpers.EscapeMarkdown(value)}`";
    }

    /// <summary>
    /// Formats an optional string diff.
    /// </summary>
    /// <param name="before">Before value.</param>
    /// <param name="after">After value.</param>
    /// <param name="format">Diff format.</param>
    /// <returns>Formatted diff or single value.</returns>
    private static string FormatOptionalDiff(string? before, string? after, string format)
    {
        var beforeStr = before ?? "";
        var afterStr = after ?? "";

        if (beforeStr == afterStr)
        {
            return FormatOptionalString(after);
        }

        return ScribanHelpers.FormatDiff(beforeStr, afterStr, format);
    }

    /// <summary>
    /// Determines if two variables are equal.
    /// </summary>
    /// <param name="before">Before variable.</param>
    /// <param name="after">After variable.</param>
    /// <returns>True if equal; otherwise false.</returns>
    private static bool VariablesEqual(VariableValues before, VariableValues after)
    {
        return before.Value == after.Value
            && before.Enabled == after.Enabled
            && before.ContentType == after.ContentType
            && before.Expires == after.Expires
            && before.IsSecret == after.IsSecret;
    }

    /// <summary>
    /// Determines if a variable value should be treated as large.
    /// Secret variables are never large (value is masked).
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>True if large; otherwise false.</returns>
    private static bool IsLargeValue(VariableValues variable)
    {
        if (variable.IsSecret)
        {
            return false;
        }

        return ScribanHelpers.IsLargeValue(variable.Value, null);
    }

    /// <summary>
    /// Gets a string property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The string value or empty string.</returns>
    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Gets a nullable boolean property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The boolean value or null.</returns>
    private static bool? GetNullableBool(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return null;
    }
}

/// <summary>
/// Represents raw variable values extracted from Terraform plan.
/// </summary>
/// <param name="Name">The variable name.</param>
/// <param name="Value">The variable value.</param>
/// <param name="Enabled">Whether the variable is enabled.</param>
/// <param name="ContentType">The content type.</param>
/// <param name="Expires">The expiration value.</param>
/// <param name="IsSecret">Whether this is a secret variable.</param>
internal record VariableValues(string Name, string? Value, bool? Enabled, string? ContentType, string? Expires, bool IsSecret);
