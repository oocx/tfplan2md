using System;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Registers AzureRM value formatters.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal static class AzureRmValueFormatterRegistration
{
    /// <summary>
    /// Registers AzureRM value formatters in the provided registry.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    /// <param name="scopeFormatter">Optional formatter for enriched scope display.</param>
    /// <param name="principalMapper">Optional mapper for enriching principal identifiers.</param>
    /// <param name="entityMapper">Optional mapper for tenant and management group formatting.</param>
    public static void Register(
        ValueFormatterRegistry registry,
        EnrichedAzureScopeFormatter? scopeFormatter = null,
        IPrincipalMapper? principalMapper = null,
        AzureEntityMapper? entityMapper = null)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(
            new MatchPattern("(^azurerm$|.*/azurerm$)", null, null, null),
            new AzureResourceIdFormatter(scopeFormatter));

        registry.Register(
            new MatchPattern(
                "(^azurerm$|.*/azurerm$)",
                null,
                "^role_definition_id$|^role_definition_resource_id$",
                "(?i)^(?:/subscriptions/[^/]+/providers/Microsoft.Authorization/roleDefinitions/[^/]+|/providers/Microsoft.Authorization/roleDefinitions/[^/]+|[0-9a-f-]{36})$"),
            new RoleDefinitionFormatter());

        if (principalMapper is not null)
        {
            registry.Register(
                new MatchPattern("(^azurerm$|.*/azurerm$)", null, "^principal_id$", null),
                new PrincipalIdFormatter(principalMapper));
        }

        if (entityMapper is not null)
        {
            AzureValueFormatterRegistration.RegisterTenantAndManagementGroup(
                registry,
                "(^azurerm$|.*/azurerm$)",
                entityMapper);
        }
    }
}
