using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Extracts descriptor values for Azure DevOps child resources.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
internal sealed class AzureDevOpsDescriptorRowExtractor : IChildRowExtractor
{
    /// <summary>
    /// The column key for the extracted value.
    /// </summary>
    private readonly string _columnKey;

    /// <summary>
    /// The attribute name used for formatting rules.
    /// </summary>
    private readonly string _attributeName;

    /// <summary>
    /// Candidate property names to resolve from object states.
    /// </summary>
    private readonly IReadOnlyList<string> _propertyNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureDevOpsDescriptorRowExtractor"/> class.
    /// </summary>
    /// <param name="columnKey">The column key used in row values.</param>
    /// <param name="attributeName">The attribute name for formatting.</param>
    /// <param name="propertyNames">The property names to probe for descriptor values.</param>
    public AzureDevOpsDescriptorRowExtractor(
        string columnKey,
        string attributeName,
        IReadOnlyList<string> propertyNames)
    {
        _columnKey = columnKey;
        _attributeName = attributeName;
        _propertyNames = propertyNames;
    }

    /// <summary>
    /// Extracts the descriptor value for the child resource.
    /// </summary>
    /// <param name="childState">The child JSON state.</param>
    /// <param name="providerName">The provider name for formatting context.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for formatting values.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <returns>The formatted row values.</returns>
    public IReadOnlyDictionary<string, string> ExtractRow(
        object? childState,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        var rawValue = ResolveDescriptor(childState);
        var formatted = ScribanHelpers.FormatAttributeValueTableWithRegistry(
            _attributeName,
            rawValue,
            providerName,
            valueFormatterRegistry,
            iconProviderRegistry);

        return new Dictionary<string, string> { [_columnKey] = formatted };
    }

    /// <summary>
    /// Resolves a descriptor value from inline or separate resource state.
    /// </summary>
    /// <param name="state">The JSON state to inspect.</param>
    /// <returns>The resolved descriptor value, or empty string.</returns>
    private string ResolveDescriptor(object? state)
    {
        if (state is not JsonElement element)
        {
            return string.Empty;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return element.ToString();
        }

        foreach (var propertyName in _propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            return property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : property.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts column values with inline diffs for a descriptor that changed between before/after states.
    /// </summary>
    /// <remarks>
    /// Descriptors are typically added or removed, not modified. This method falls back to showing after state.
    /// </remarks>
    public IReadOnlyDictionary<string, string> ExtractDiffRow(
        object? beforeState,
        object? afterState,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry,
        LargeValueFormat largeValueFormat)
    {
        // Descriptors don't typically change values, they're added or removed
        // Fall back to showing the after state
        return ExtractRow(afterState, providerName, valueFormatterRegistry, iconProviderRegistry);
    }
}
