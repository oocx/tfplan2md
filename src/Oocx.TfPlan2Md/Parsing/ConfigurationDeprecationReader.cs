using System.Text.Json;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Reads deprecated variable and output declarations from Terraform 1.15+ configuration blocks.
/// Related feature: docs/features/122-terraform-1-15-support/adr-001-plan-json-model-extensions.md.
/// </summary>
public static class ConfigurationDeprecationReader
{
    /// <summary>
    /// Yields tuples of (name, kind, deprecationMessage, optionalCtyType) for each deprecated
    /// variable or output declared in the root module configuration.
    /// </summary>
    /// <param name="configuration">The raw configuration JsonElement from TerraformPlan.</param>
    /// <returns>An enumerable of deprecation records.</returns>
    public static IEnumerable<(string Name, string Kind, string DeprecationMessage, string? CtyType)> ReadDeprecations(JsonElement? configuration)
    {
        if (!TryGetRootModule(configuration, out var rootModule))
        {
            yield break;
        }

        // Read deprecated variables
        foreach (var deprecation in ReadDeprecatedVariables(rootModule))
        {
            yield return deprecation;
        }

        // Read deprecated outputs
        foreach (var deprecation in ReadDeprecatedOutputs(rootModule))
        {
            yield return deprecation;
        }
    }

    private static bool TryGetRootModule(JsonElement? configuration, out JsonElement rootModule)
    {
        rootModule = default;

        if (configuration is null || configuration.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!configuration.Value.TryGetProperty("root_module", out rootModule) ||
            rootModule.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return true;
    }

    private static IEnumerable<(string Name, string Kind, string DeprecationMessage, string? CtyType)> ReadDeprecatedVariables(JsonElement rootModule)
    {
        if (!rootModule.TryGetProperty("variables", out var variables) ||
            variables.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var variable in variables.EnumerateObject())
        {
            if (variable.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (variable.Value.TryGetProperty("deprecated", out var deprecated) &&
                deprecated.ValueKind == JsonValueKind.String)
            {
                yield return (variable.Name, "variable", deprecated.GetString() ?? string.Empty, null);
            }
        }
    }

    private static IEnumerable<(string Name, string Kind, string DeprecationMessage, string? CtyType)> ReadDeprecatedOutputs(JsonElement rootModule)
    {
        if (!rootModule.TryGetProperty("outputs", out var outputs) ||
            outputs.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var output in outputs.EnumerateObject())
        {
            if (output.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!output.Value.TryGetProperty("deprecated", out var deprecated) ||
                deprecated.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? ctyType = null;
            if (output.Value.TryGetProperty("type", out var typeProperty) &&
                typeProperty.ValueKind == JsonValueKind.String)
            {
                ctyType = typeProperty.GetString();
            }

            yield return (output.Name, "output", deprecated.GetString() ?? string.Empty, ctyType);
        }
    }
}
