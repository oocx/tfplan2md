using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Formats variable values and change rows for Azure DevOps variable group rendering.
/// </summary>
/// <remarks>
/// Extracted from VariableGroupViewModelFactory to improve maintainability.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </remarks>
internal static class VariableGroupFormatters
{
    /// <summary>
    /// Formats variable values for create/delete tables.
    /// </summary>
    /// <param name="variables">Raw variable values.</param>
    /// <returns>Formatted variable rows.</returns>
    public static List<VariableRowViewModel> FormatVariableRows(
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
    public static VariableChangeRowViewModel CreateAddedRow(VariableValues variable)
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
    public static VariableChangeRowViewModel CreateRemovedRow(VariableValues variable)
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
    public static VariableChangeRowViewModel CreateUnchangedRow(VariableValues variable)
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
    public static VariableChangeRowViewModel CreateDiffRow(
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
        // Use "-" placeholder for null values in diffs (consistent with initial null rendering)
        var beforeStr = ConvertBoolToString(before);
        var afterStr = ConvertBoolToString(after);

        if (beforeStr == afterStr)
        {
            return FormatEnabled(after);
        }

        return ScribanHelpers.FormatDiff(beforeStr, afterStr, format);
    }

    /// <summary>
    /// Converts a nullable boolean to its string representation for display.
    /// </summary>
    /// <param name="value">Boolean value to convert.</param>
    /// <returns>"true", "false", or "-" for null.</returns>
    private static string ConvertBoolToString(bool? value)
    {
        if (value == null)
        {
            return "-";
        }

        return value.Value ? "true" : "false";
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
        // Use "-" placeholder for null/empty values in diffs (consistent with initial null rendering)
        var beforeStr = string.IsNullOrEmpty(before) ? "-" : before;
        var afterStr = string.IsNullOrEmpty(after) ? "-" : after;

        if (beforeStr == afterStr)
        {
            return FormatOptionalString(after);
        }

        return ScribanHelpers.FormatDiff(beforeStr, afterStr, format);
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
}
