using System;
using EmbeddedJsonResources;

namespace Oocx.TfPlan2Md;

/// <summary>
/// Provides access to generated embedded JSON payloads.
/// Related feature: docs/features/107-remove-scriban/reflection-removal-analysis.md.
/// </summary>
internal static class EmbeddedJsonPayloads
{
    /// <summary>
    /// Gets Azure role definitions JSON bytes.
    /// </summary>
    internal static ReadOnlySpan<byte> AzureRoleDefinitions => global::EmbeddedJsonResources.AzureRoleDefinitions.GetBytes();

    /// <summary>
    /// Gets Microsoft Graph app roles JSON bytes.
    /// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
    /// </summary>
    internal static ReadOnlySpan<byte> MicrosoftGraphAppRoles => global::EmbeddedJsonResources.MicrosoftGraphAppRoles.GetBytes();

    /// <summary>
    /// Gets AzAPI documentation mapping JSON bytes.
    /// </summary>
    internal static ReadOnlySpan<byte> AzureApiDocumentationMappings => global::EmbeddedJsonResources.AzureApiDocumentationMappings.GetBytes();

    /// <summary>
    /// Resolves icon rules JSON payload by legacy resource name.
    /// </summary>
    /// <param name="resourceName">Legacy manifest resource name.</param>
    /// <returns>Payload bytes when mapped; otherwise null.</returns>
    internal static byte[]? GetIconRulesPayload(string resourceName)
    {
        return resourceName switch
        {
            "Oocx.TfPlan2Md.Providers.Shared.Icons.azure-common-icons.json" => Azure_common_icons.GetBytes(),
            "Oocx.TfPlan2Md.Providers.AzureAD.Icons.azuread-icons.json" => Azuread_icons.GetBytes(),
            "Oocx.TfPlan2Md.Providers.AzureDevOps.Icons.azuredevops-icons.json" => Azuredevops_icons.GetBytes(),
            _ => null,
        };
    }
}
