using System;
using System.Text.RegularExpressions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Suppresses attribute change rows for <c>azapi</c> resources where both before and after values
/// are Azure resource IDs that differ only in letter casing.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// Related issue: docs/issues/108-azapi-body-casing-filter/analysis.md.
/// </summary>
/// <remarks>
/// The Azure ARM API occasionally returns resource IDs with different capitalisation on
/// successive reads (e.g., <c>APP-RG-GWC</c> vs <c>app-rg-gwc</c> in a resource group
/// segment). Terraform surfaces these as changes even though they are semantically identical.
/// This filter suppresses such noise rows for top-level attribute columns of
/// <c>azapi_resource</c> and <c>azapi_update_resource</c> resources.
///
/// Only rows where the provider is <c>azapi</c> (or fully-qualified
/// <c>registry.terraform.io/azure/azapi</c>) AND at least one value is an Azure resource ID
/// (per <see cref="AzureScopeParser.IsAzureResourceId"/>) AND the two values are ordinally
/// equal under case-insensitive comparison are suppressed.
///
/// For body-level property changes (inside the JSON body), case-insensitive filtering is
/// handled separately by <see cref="Helpers.AzApiBodyRenderer"/> when
/// <c>IgnoreAzureIdCaseChanges</c> is enabled on the render context.
/// </remarks>
internal sealed class AzApiResourceIdCaseChangeFilter : IAttributeChangeFilter
{
    /// <summary>
    /// Matches both the short provider name "azapi" and fully-qualified registry paths
    /// such as "registry.terraform.io/azure/azapi" or "registry.terraform.io/hashicorp/azapi".
    /// Consistent with other regex usage in the codebase (1-second timeout).
    /// </summary>
    private static readonly Regex AzApiProviderPattern =
        new(@"(^azapi$|.*/azapi$)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Determines whether the attribute change row should be suppressed.
    /// </summary>
    /// <param name="context">The filter context carrying provider name, attribute name, and before/after values.</param>
    /// <returns>
    /// <c>true</c> when the provider is azapi, at least one value is an Azure resource ID,
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

        // Guard 2: Only suppress for azapi resources.
        if (!AzApiProviderPattern.IsMatch(context.ProviderName ?? string.Empty))
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
