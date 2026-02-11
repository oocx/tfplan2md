using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Builds reference lookups from the Terraform configuration block.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/architecture.md.
/// </remarks>
internal sealed class ConfigurationReferenceResolver
{
    /// <summary>
    /// Builds a reference index keyed by resource address and attribute name.
    /// </summary>
    /// <param name="configuration">The optional configuration block from the plan.</param>
    /// <returns>
    /// A lookup mapping <c>(resource_address, attribute_name)</c> to referenced addresses.
    /// Returns an empty index when configuration is missing or malformed.
    /// </returns>
    internal static IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>> BuildReferenceIndex(
        JsonElement? configuration)
    {
        if (configuration is null || configuration.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return CreateEmptyIndex();
        }

        var root = configuration.Value;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return CreateEmptyIndex();
        }

        if (!root.TryGetProperty("root_module", out var rootModule) || rootModule.ValueKind != JsonValueKind.Object)
        {
            return CreateEmptyIndex();
        }

        var index = new Dictionary<(string Address, string Attribute), IReadOnlyList<string>>(
            new ConfigurationReferenceKeyComparer());

        BuildModuleIndex(rootModule, string.Empty, index);

        return index;
    }

    /// <summary>
    /// Creates an empty reference index with the correct comparer.
    /// </summary>
    /// <returns>An empty reference index.</returns>
    private static Dictionary<(string Address, string Attribute), IReadOnlyList<string>> CreateEmptyIndex()
    {
        return new Dictionary<(string Address, string Attribute), IReadOnlyList<string>>(
            new ConfigurationReferenceKeyComparer());
    }

    /// <summary>
    /// Walks a module block and records any expression references.
    /// </summary>
    /// <param name="moduleElement">The module JSON element.</param>
    /// <param name="modulePrefix">The current module address prefix.</param>
    /// <param name="index">The reference index to populate.</param>
    private static void BuildModuleIndex(
        JsonElement moduleElement,
        string modulePrefix,
        Dictionary<(string Address, string Attribute), IReadOnlyList<string>> index)
    {
        if (moduleElement.TryGetProperty("resources", out var resources) && resources.ValueKind == JsonValueKind.Array)
        {
            foreach (var resourceElement in resources.EnumerateArray())
            {
                AddResourceReferences(resourceElement, modulePrefix, index);
            }
        }

        if (!moduleElement.TryGetProperty("module_calls", out var moduleCalls) || moduleCalls.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var moduleCall in moduleCalls.EnumerateObject())
        {
            if (!moduleCall.Value.TryGetProperty("module", out var nestedModule) || nestedModule.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var nestedPrefix = string.Concat(modulePrefix, "module.", moduleCall.Name, ".");
            BuildModuleIndex(nestedModule, nestedPrefix, index);
        }
    }

    /// <summary>
    /// Adds expression references for a single resource entry.
    /// </summary>
    /// <param name="resourceElement">The resource element to inspect.</param>
    /// <param name="modulePrefix">The module prefix used to build full addresses.</param>
    /// <param name="index">The reference index to populate.</param>
    private static void AddResourceReferences(
        JsonElement resourceElement,
        string modulePrefix,
        Dictionary<(string Address, string Attribute), IReadOnlyList<string>> index)
    {
        if (!TryGetString(resourceElement, "address", out var address))
        {
            return;
        }

        var resourceAddress = string.Concat(modulePrefix, address);
        if (string.IsNullOrWhiteSpace(resourceAddress))
        {
            return;
        }

        if (!resourceElement.TryGetProperty("expressions", out var expressions) || expressions.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var expressionProperty in expressions.EnumerateObject())
        {
            if (!expressionProperty.Value.TryGetProperty("references", out var referencesElement) || referencesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var referenceElement in referencesElement.EnumerateArray())
            {
                if (!TryGetString(referenceElement, out var referenceValue))
                {
                    continue;
                }

                var normalizedReference = NormalizeReferenceAddress(referenceValue, modulePrefix);
                if (string.IsNullOrWhiteSpace(normalizedReference))
                {
                    continue;
                }

                references.Add(normalizedReference);
            }

            if (references.Count == 0)
            {
                continue;
            }

            var key = (resourceAddress, expressionProperty.Name);
            if (index.TryGetValue(key, out var existingReferences))
            {
                references.UnionWith(existingReferences);
            }

            index[key] = references.ToList();
        }
    }

    /// <summary>
    /// Normalizes a reference address into the current module scope.
    /// </summary>
    /// <param name="reference">The raw reference value from the configuration.</param>
    /// <param name="modulePrefix">The current module prefix.</param>
    /// <returns>The normalized reference address.</returns>
    private static string NormalizeReferenceAddress(string reference, string modulePrefix)
    {
        if (string.IsNullOrWhiteSpace(modulePrefix))
        {
            return reference;
        }

        return reference.StartsWith("module.", StringComparison.OrdinalIgnoreCase)
            ? reference
            : string.Concat(modulePrefix, reference);
    }

    /// <summary>
    /// Attempts to read a string property from a JSON object.
    /// </summary>
    /// <param name="element">The JSON element to read.</param>
    /// <param name="propertyName">The property name to extract.</param>
    /// <param name="value">The extracted value.</param>
    /// <returns><c>true</c> when the property is present and non-empty.</returns>
    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;

        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        return TryGetString(property, out value);
    }

    /// <summary>
    /// Attempts to read a string value from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element to inspect.</param>
    /// <param name="value">The extracted value.</param>
    /// <returns><c>true</c> when the element is a non-empty string.</returns>
    private static bool TryGetString(JsonElement element, out string value)
    {
        value = element.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Provides case-insensitive equality for reference index keys.
    /// </summary>
    private sealed class ConfigurationReferenceKeyComparer : IEqualityComparer<(string Address, string Attribute)>
    {
        /// <summary>
        /// Determines whether two keys are equal using ordinal ignore-case matching.
        /// </summary>
        /// <param name="x">The first key.</param>
        /// <param name="y">The second key.</param>
        /// <returns><c>true</c> when both address and attribute match.</returns>
        public bool Equals((string Address, string Attribute) x, (string Address, string Attribute) y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x.Address, y.Address)
                && StringComparer.OrdinalIgnoreCase.Equals(x.Attribute, y.Attribute);
        }

        /// <summary>
        /// Computes a hash code using ordinal ignore-case rules.
        /// </summary>
        /// <param name="obj">The key to hash.</param>
        /// <returns>The computed hash code.</returns>
        public int GetHashCode((string Address, string Attribute) obj)
        {
            var addressHash = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Address ?? string.Empty);
            var attributeHash = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Attribute ?? string.Empty);
            return HashCode.Combine(addressHash, attributeHash);
        }
    }
}
