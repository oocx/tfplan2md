using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Extracts Azure AD group member values for inline child tables.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
internal sealed class AzureAdGroupMemberRowExtractor : IChildRowExtractor
{
    /// <summary>
    /// Extracts the member row values from Azure AD group member state.
    /// </summary>
    /// <param name="childState">The child JSON state for the member.</param>
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
        var rawMember = ResolveMemberId(childState);
        var formatted = MarkdownHelpers.FormatAttributeValueTableWithRegistry(
            "member_object_id",
            rawMember,
            providerName,
            valueFormatterRegistry,
            iconProviderRegistry);

        return new Dictionary<string, string> { ["member"] = formatted };
    }

    /// <summary>
    /// Resolves the member identifier from either inline or separate child state.
    /// </summary>
    /// <param name="state">The child state to inspect.</param>
    /// <returns>The resolved member identifier, or an empty string.</returns>
    private static string ResolveMemberId(object? state)
    {
        if (state is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var memberId = JsonStateReader.GetStringProperty(element, "member_object_id")
                    ?? JsonStateReader.GetStringProperty(element, "member");

                return memberId ?? string.Empty;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString() ?? string.Empty;
            }

            return element.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts column values with inline diffs for a member that changed between before/after states.
    /// </summary>
    /// <param name="beforeState">The member state before the change.</param>
    /// <param name="afterState">The member state after the change.</param>
    /// <param name="providerName">The provider name for formatting context.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for formatting values.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for semantic icons.</param>
    /// <param name="largeValueFormat">The preferred format for rendering large value diffs.</param>
    /// <returns>A mapping from column property names to formatted display values with inline diffs.</returns>
    /// <remarks>
    /// Group members are typically added or removed, not modified. This method falls back to showing after state.
    /// </remarks>
    public IReadOnlyDictionary<string, string> ExtractDiffRow(
        object? beforeState,
        object? afterState,
        string providerName,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry,
        LargeValueFormat largeValueFormat)
    {
        // Group members don't typically change values, they're added or removed
        // Fall back to showing the after state
        return ExtractRow(afterState, providerName, valueFormatterRegistry, iconProviderRegistry);
    }
}
