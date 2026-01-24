using System;
using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Extracts variable data from Terraform JSON state for Azure DevOps variable groups.
/// </summary>
/// <remarks>
/// Extracted from VariableGroupViewModelFactory to improve maintainability.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </remarks>
internal static class VariableGroupExtractors
{
    /// <summary>
    /// Extracts the variable group name from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Name value when present; otherwise null.</returns>
    public static string? ExtractName(object? state)
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
    public static string? ExtractDescription(object? state)
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
    public static IReadOnlyList<VariableValues> ExtractVariables(object? state)
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
    public static IReadOnlyList<KeyVaultRowViewModel> ExtractKeyVaultBlocks(object? state)
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
