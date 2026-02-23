using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Builds output change models from the Terraform plan.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="plan">The Terraform plan containing output changes and configuration.</param>
    /// <returns>A list of output change models with metadata and masking applied.</returns>
    private List<OutputChangeModel> BuildOutputModels(TerraformPlan plan)
    {
        if (plan.OutputChanges is null || plan.OutputChanges.Count == 0)
        {
            return new List<OutputChangeModel>();
        }

        var outputs = new List<OutputChangeModel>();

        foreach (var (outputName, outputChange) in plan.OutputChanges)
        {
            // Determine module address for this output
            var moduleAddress = DetermineOutputModuleAddress(plan.Configuration, outputName);

            // Extract metadata (description, sensitivity from configuration)
            var (description, configSensitive) = ExtractOutputMetadata(plan.Configuration, outputName, moduleAddress);

            // Determine primary action (first action in the list)
            var action = outputChange.Actions.Count > 0 ? outputChange.Actions[0] : "no-op";

            // Select value based on action (after for create/update/no-op, before for delete)
            var value = action == "delete" ? outputChange.Before : outputChange.After;

            // Check if value is computed
            var isComputed = outputChange.AfterUnknown;

            // Detect sensitivity from multiple sources with precedence:
            // 1. after_sensitive (for create/update/no-op)
            // 2. before_sensitive (for delete)
            // 3. configuration.sensitive (fallback)
            var isSensitive = action == "delete"
                ? IsSensitiveValue(outputChange.BeforeSensitive) || configSensitive
                : IsSensitiveValue(outputChange.AfterSensitive) || configSensitive;

            // Determine if value should be masked (sensitive AND --show-sensitive is NOT set)
            var isMasked = isSensitive && !_showSensitive;

            outputs.Add(new OutputChangeModel
            {
                Name = outputName,
                Description = description,
                IsSensitive = isSensitive,
                Action = action,
                ActionSymbol = GetActionSymbol(action),
                Value = value,
                IsComputed = isComputed,
                IsMasked = isMasked,
                ModuleAddress = moduleAddress
            });
        }

        return outputs;
    }

    /// <summary>
    /// Determines the module address for an output by searching the configuration structure.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="configuration">The configuration JSON element.</param>
    /// <param name="outputName">The output name to locate.</param>
    /// <returns>The module address (empty string for root module).</returns>
    private static string DetermineOutputModuleAddress(JsonElement? configuration, string outputName)
    {
        if (configuration is null || configuration.Value.ValueKind == JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        // Check root module outputs first
        if (configuration.Value.TryGetProperty("root_module", out var rootModule))
        {
            if (rootModule.TryGetProperty("outputs", out var rootOutputs) &&
                rootOutputs.TryGetProperty(outputName, out _))
            {
                return string.Empty; // Root module
            }

            // Check module outputs
            if (rootModule.TryGetProperty("modules", out var modules) &&
                modules.ValueKind == JsonValueKind.Array)
            {
                foreach (var module in modules.EnumerateArray())
                {
                    if (module.TryGetProperty("address", out var address) &&
                        module.TryGetProperty("outputs", out var moduleOutputs) &&
                        moduleOutputs.TryGetProperty(outputName, out _))
                    {
                        return address.GetString() ?? string.Empty;
                    }
                }
            }
        }

        // Default to root if not found
        return string.Empty;
    }

    /// <summary>
    /// Extracts output metadata (description and sensitivity) from the configuration.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="configuration">The configuration JSON element.</param>
    /// <param name="outputName">The output name to extract metadata for.</param>
    /// <param name="moduleAddress">The module address (empty for root).</param>
    /// <returns>A tuple containing the optional description and sensitivity flag.</returns>
    private static (string? Description, bool Sensitive) ExtractOutputMetadata(
        JsonElement? configuration,
        string outputName,
        string moduleAddress)
    {
        if (configuration is null || configuration.Value.ValueKind == JsonValueKind.Undefined)
        {
            return (null, false);
        }

        if (!configuration.Value.TryGetProperty("root_module", out var rootModule))
        {
            return (null, false);
        }

        var outputConfig = FindOutputConfig(rootModule, outputName, moduleAddress);
        if (!outputConfig.HasValue)
        {
            return (null, false);
        }

        return ParseOutputConfig(outputConfig.Value);
    }

    /// <summary>
    /// Finds the output configuration element for the specified output.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="rootModule">The root module JSON element.</param>
    /// <param name="outputName">The output name.</param>
    /// <param name="moduleAddress">The module address (empty for root).</param>
    /// <returns>The output configuration element if found, null otherwise.</returns>
    private static JsonElement? FindOutputConfig(JsonElement rootModule, string outputName, string moduleAddress)
    {
        // Root module outputs
        if (string.IsNullOrEmpty(moduleAddress))
        {
            if (rootModule.TryGetProperty("outputs", out var rootOutputs) &&
                rootOutputs.TryGetProperty(outputName, out var outputConfig))
            {
                return outputConfig;
            }
        }
        else
        {
            // Module outputs
            if (rootModule.TryGetProperty("modules", out var modules) &&
                modules.ValueKind == JsonValueKind.Array)
            {
                foreach (var module in modules.EnumerateArray())
                {
                    if (module.TryGetProperty("address", out var address) &&
                        address.GetString() == moduleAddress &&
                        module.TryGetProperty("outputs", out var moduleOutputs) &&
                        moduleOutputs.TryGetProperty(outputName, out var outputConfig))
                    {
                        return outputConfig;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Parses the output configuration element to extract description and sensitivity.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="outputConfig">The output configuration element.</param>
    /// <returns>A tuple containing the optional description and sensitivity flag.</returns>
    private static (string? Description, bool Sensitive) ParseOutputConfig(JsonElement outputConfig)
    {
        // Extract description
        var description = outputConfig.TryGetProperty("description", out var descElement) &&
                         descElement.ValueKind == JsonValueKind.String
            ? descElement.GetString()
            : null;

        // Extract sensitive flag (defaults to false)
        var sensitive = outputConfig.TryGetProperty("sensitive", out var sensElement) &&
                       sensElement.ValueKind == JsonValueKind.True;

        return (description, sensitive);
    }

    /// <summary>
    /// Determines if a sensitivity marker indicates a sensitive value.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="sensitivityMarker">The sensitivity marker (boolean or nested object).</param>
    /// <returns>True if the value is sensitive, false otherwise.</returns>
    private static bool IsSensitiveValue(object? sensitivityMarker)
    {
        if (sensitivityMarker is null)
        {
            return false;
        }

        // Handle JsonElement
        if (sensitivityMarker is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => CheckNestedSensitivity(element),
                _ => false
            };
        }

        // Handle boolean directly
        if (sensitivityMarker is bool boolValue)
        {
            return boolValue;
        }

        return false;
    }

    /// <summary>
    /// Recursively checks for sensitivity markers in nested objects.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="element">The JSON element to check.</param>
    /// <returns>True if any nested value is sensitive, false otherwise.</returns>
    private static bool CheckNestedSensitivity(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return element.ValueKind == JsonValueKind.True;
        }

        // SonarAnalyzer S3267: Cannot simplify with LINQ - this loop uses early return for efficiency
        // Justification: Early return pattern avoids checking all properties when sensitivity is found
#pragma warning disable S3267
        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.Value.ValueKind == JsonValueKind.Object &&
                CheckNestedSensitivity(property.Value))
            {
                return true;
            }
        }
#pragma warning restore S3267

        return false;
    }
}
