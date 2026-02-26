namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Carries the full context needed for an attribute change filter decision.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
/// <param name="ProviderName">The fully-qualified or short Terraform provider name (e.g., "registry.terraform.io/hashicorp/azurerm").</param>
/// <param name="AttributeName">The flattened attribute key (e.g., "scope", "role_definition_id").</param>
/// <param name="BeforeValue">The raw attribute value from the plan's "before" state.</param>
/// <param name="AfterValue">The raw attribute value from the plan's "after" state.</param>
internal sealed record AttributeChangeFilterContext(
    string? ProviderName,
    string? AttributeName,
    string? BeforeValue,
    string? AfterValue);
