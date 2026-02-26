using System;
using System.Text.RegularExpressions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Suppresses attribute change rows where both before and after values are Azure resource IDs
/// that differ only in letter casing.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
/// <remarks>
/// Azure ARM API occasionally returns resource IDs with different capitalization on successive
/// reads (e.g., <c>/subscriptions/ABC123/…</c> vs <c>/subscriptions/abc123/…</c>).
/// Terraform detects these as changes; this filter suppresses such noise rows.
///
/// Only rows where the provider is <c>azurerm</c> (or fully-qualified
/// <c>registry.terraform.io/hashicorp/azurerm</c>) AND at least one value is an Azure resource
/// ID (per <see cref="AzureScopeParser.IsAzureResourceId"/>) AND the two values are ordinally
/// equal under case-insensitive comparison are suppressed.
/// </remarks>
internal sealed class AzureResourceIdCaseChangeFilter : IAttributeChangeFilter
{
    /// <summary>
    /// Matches both the short provider name "azurerm" and fully-qualified registry paths
    /// such as "registry.terraform.io/hashicorp/azurerm".
    /// Consistent with other regex usage in the codebase (1-second timeout).
    /// </summary>
    private static readonly Regex AzureRmProviderPattern =
        new(@"(^azurerm$|.*/azurerm$)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Determines whether the attribute change row should be suppressed.
    /// </summary>
    /// <param name="context">The filter context carrying provider name, attribute name, and before/after values.</param>
    /// <returns>
    /// <c>true</c> when the provider is azurerm, at least one value is an Azure resource ID,
    /// and the before/after values are equal under ordinal case-insensitive comparison;
    /// <c>false</c> otherwise.
    /// </returns>
    public bool ShouldSuppress(AttributeChangeFilterContext context)
    {
        // Guard 1: Do not suppress when either value is null.
        if (context.BeforeValue is null || context.AfterValue is null)
        {
            return false;
        }

        // Guard 2: Only suppress for azurerm resources.
        if (!AzureRmProviderPattern.IsMatch(context.ProviderName ?? string.Empty))
        {
            return false;
        }

        // Guard 3: Only suppress when at least one value looks like an Azure resource ID.
        if (!AzureScopeParser.IsAzureResourceId(context.BeforeValue)
            && !AzureScopeParser.IsAzureResourceId(context.AfterValue))
        {
            return false;
        }

        // Suppress when the sole difference between before and after is letter casing.
        // Note: the caller already established that ordinal equality is false, so this
        // specifically catches casing-only differences.
        return string.Equals(context.BeforeValue, context.AfterValue, StringComparison.OrdinalIgnoreCase);
    }
}
