using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Contains logic for building output change models from Terraform plan.
/// Related feature: docs/features/097-terraform-outputs/specification.md.
/// </summary>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Builds a list of output change models from the plan's output_changes section.
    /// </summary>
    /// <param name="plan">The Terraform plan containing output changes.</param>
    /// <returns>A list of output change models for the markdown report.</returns>
    private List<OutputChangeModel> BuildOutputChangeModels(TerraformPlan plan)
    {
        if (plan.OutputChanges is null || plan.OutputChanges.Count == 0)
        {
            return [];
        }

        // Extract output descriptions from configuration
        var outputDescriptions = ExtractOutputDescriptions(plan.Configuration);

        var outputs = new List<OutputChangeModel>();

        foreach (var (name, change) in plan.OutputChanges)
        {
            var action = DetermineAction(change.Actions);
            var actionIcon = GetActionSymbol(action);
            var isSensitive = IsSensitiveOutput(change);
            var isUnknown = IsUnknownOutput(change);

            // Format values for display
            var before = FormatOutputValue(change.Before);
            var after = FormatOutputValue(change.After);

            // Get description from configuration
            outputDescriptions.TryGetValue(name, out var description);

            outputs.Add(new OutputChangeModel
            {
                Name = name,
                Description = description,
                Action = action,
                ActionIcon = actionIcon,
                IsSensitive = isSensitive,
                Before = before,
                After = after,
                IsUnknown = isUnknown
            });
        }

        // Sort outputs by name for consistent rendering
        return outputs.OrderBy(o => o.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Extracts output descriptions from the plan configuration.
    /// </summary>
    /// <param name="configuration">The configuration JSON element from the plan.</param>
    /// <returns>A dictionary mapping output names to their descriptions.</returns>
    private static Dictionary<string, string> ExtractOutputDescriptions(JsonElement? configuration)
    {
        var descriptions = new Dictionary<string, string>();

        if (configuration is null || configuration.Value.ValueKind == JsonValueKind.Undefined)
        {
            return descriptions;
        }

        try
        {
            // Navigate to configuration.root_module.outputs
            if (configuration.Value.TryGetProperty("root_module", out var rootModule)
                && rootModule.TryGetProperty("outputs", out var outputs)
                && outputs.ValueKind == JsonValueKind.Object)
            {
                foreach (var outputProp in outputs.EnumerateObject())
                {
                    var outputName = outputProp.Name;
                    var outputConfig = outputProp.Value;

                    if (outputConfig.TryGetProperty("description", out var descriptionElement)
                        && descriptionElement.ValueKind == JsonValueKind.String)
                    {
                        var description = descriptionElement.GetString();
                        if (!string.IsNullOrWhiteSpace(description))
                        {
                            descriptions[outputName] = description;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // If we can't parse the configuration, just return empty descriptions
            // This is not critical - outputs will still be rendered without descriptions
        }

        return descriptions;
    }

    /// <summary>
    /// Determines if an output is marked as sensitive.
    /// Returns true if before_sensitive or after_sensitive is true.
    /// </summary>
    /// <param name="change">The output change to check.</param>
    /// <returns>True if the output is sensitive; otherwise, false.</returns>
    private static bool IsSensitiveOutput(OutputChange change)
    {
        // Check if before_sensitive is true (boolean value)
        if (change.BeforeSensitive is bool beforeBool && beforeBool)
        {
            return true;
        }

        // Check if after_sensitive is true (boolean value)
        if (change.AfterSensitive is bool afterBool && afterBool)
        {
            return true;
        }

        // Handle JsonElement boolean values for before_sensitive
        if (change.BeforeSensitive is JsonElement beforeJson
            && beforeJson.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        // Handle JsonElement boolean values for after_sensitive
        if (change.AfterSensitive is JsonElement afterJson
            && afterJson.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        // Check if before_sensitive is a non-empty object (complex sensitivity)
        if (change.BeforeSensitive is JsonElement beforeObjJson
            && beforeObjJson.ValueKind == JsonValueKind.Object
            && beforeObjJson.EnumerateObject().Any())
        {
            return true;
        }

        // Check if after_sensitive is a non-empty object (complex sensitivity)
        if (change.AfterSensitive is JsonElement afterObjJson
            && afterObjJson.ValueKind == JsonValueKind.Object
            && afterObjJson.EnumerateObject().Any())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if an output's value is unknown (will be known after apply).
    /// </summary>
    /// <param name="change">The output change to check.</param>
    /// <returns>True if the output value is unknown; otherwise, false.</returns>
    private static bool IsUnknownOutput(OutputChange change)
    {
        // after_unknown can be true (boolean) or an object with unknown attributes
        if (change.AfterUnknown is bool unknownBool && unknownBool)
        {
            return true;
        }

        // Handle JsonElement boolean values
        if (change.AfterUnknown is JsonElement unknownJson)
        {
            if (unknownJson.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            // If after_unknown is a non-empty object, the output has unknown attributes
            if (unknownJson.ValueKind == JsonValueKind.Object
                && unknownJson.EnumerateObject().Any())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Formats an output value for display in the markdown table.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A string representation of the value, or null if the value is null.</returns>
    private static string? FormatOutputValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        // Handle JsonElement values
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                JsonValueKind.Object or JsonValueKind.Array => jsonElement.ToString(),
                _ => value.ToString()
            };
        }

        // Handle primitive values
        return value.ToString();
    }
}
