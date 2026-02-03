using System.Globalization;
using System.Text.Json;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Reads primitive values from SARIF JSON elements with defensive parsing.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class SarifJsonReader
{
    /// <summary>
    /// Gets a string property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property value when present; otherwise <c>null</c>.</returns>
    internal static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    /// <summary>
    /// Reads an optional numeric property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The numeric value when present and valid; otherwise <c>null</c>.</returns>
    internal static double? GetOptionalDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return GetOptionalDouble(property);
    }

    /// <summary>
    /// Reads an optional numeric property from a nested SARIF properties object.
    /// </summary>
    /// <param name="result">The SARIF result element.</param>
    /// <param name="propertyName">The property name within the properties object.</param>
    /// <returns>The numeric value when present and valid; otherwise <c>null</c>.</returns>
    internal static double? GetOptionalDoubleFromProperties(JsonElement result, string propertyName)
    {
        if (!result.TryGetProperty("properties", out var propertiesElement) || propertiesElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!propertiesElement.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return GetOptionalDouble(property);
    }

    /// <summary>
    /// Reads an optional integer property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The integer value when present and valid; otherwise <c>null</c>.</returns>
    internal static int? GetOptionalInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numeric))
        {
            return numeric;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    /// <summary>
    /// Reads an optional numeric value from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <returns>The numeric value when present and valid; otherwise <c>null</c>.</returns>
    private static double? GetOptionalDouble(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var numeric))
        {
            return numeric;
        }

        if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
