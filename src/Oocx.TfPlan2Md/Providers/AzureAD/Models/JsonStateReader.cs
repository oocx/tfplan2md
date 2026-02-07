using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Reads commonly used string values from Terraform JSON state objects.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal static class JsonStateReader
{
    /// <summary>
    /// Gets a string property from a JSON state object.
    /// </summary>
    /// <param name="state">The JSON state object.</param>
    /// <param name="propertyName">The property to retrieve.</param>
    /// <returns>The property value or null.</returns>
    internal static string? GetStringProperty(object? state, string propertyName)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => property.ToString()
        };
    }

    /// <summary>
    /// Gets string values from an array property on the JSON state object.
    /// </summary>
    /// <param name="state">The JSON state object.</param>
    /// <param name="propertyName">The array property to retrieve.</param>
    /// <returns>List of string values.</returns>
    internal static IReadOnlyList<string> GetStringArray(object? state, string propertyName)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<string>();
        }

        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var results = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    results.Add(value);
                }
            }
            else
            {
                var raw = item.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    results.Add(raw);
                }
            }
        }

        return results;
    }
}
