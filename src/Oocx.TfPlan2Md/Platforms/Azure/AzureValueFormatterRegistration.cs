using System;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Registers Azure entity value formatters for provider-specific contexts.
/// </summary>
/// <remarks>
/// Centralizes tenant and management group formatter registrations across providers.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </remarks>
internal static class AzureValueFormatterRegistration
{
    /// <summary>
    /// Attribute pattern for tenant identifiers.
    /// </summary>
    internal const string TenantAttributePattern = "^tenant_id$|^tenantId$";

    /// <summary>
    /// Attribute pattern for management group identifiers.
    /// </summary>
    internal const string ManagementGroupAttributePattern = "^management_group_id$|^managementGroupId$";

    /// <summary>
    /// Regex pattern for GUID values used by tenant fallback matching.
    /// </summary>
    internal const string GuidPattern = "(?i)^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    /// <summary>
    /// Registers tenant and management group formatters for a provider pattern.
    /// </summary>
    /// <param name="registry">The value formatter registry to register into.</param>
    /// <param name="providerPattern">Regex that identifies the provider context.</param>
    /// <param name="entityMapper">Mapper used to resolve tenant and management group names.</param>
    internal static void RegisterTenantAndManagementGroup(
        ValueFormatterRegistry registry,
        string providerPattern,
        AzureEntityMapper entityMapper)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(providerPattern);
        ArgumentNullException.ThrowIfNull(entityMapper);

        var tenantFormatter = new TenantIdFormatter(entityMapper);
        registry.Register(
            new MatchPattern(providerPattern, null, TenantAttributePattern, null),
            tenantFormatter);
        registry.Register(
            new MatchPattern(providerPattern, null, null, GuidPattern),
            tenantFormatter);

        var managementGroupFormatter = new ManagementGroupIdFormatter(entityMapper);
        registry.Register(
            new MatchPattern(providerPattern, null, ManagementGroupAttributePattern, null),
            managementGroupFormatter);
    }
}
