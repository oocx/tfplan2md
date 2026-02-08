# Architecture: Tenant Display Name Mapping

## Status

Proposed

## Context

Feature 065 extends the Azure display enhancements from Feature 063 by ensuring Entra ID tenant IDs and Azure management group IDs are rendered consistently and recognizably across the generated Markdown report:

- **Tenants**: Render as 🏢 `Display Name (tenant_id)` everywhere tenant IDs appear in attribute tables and summaries.
- **Management Groups**: Render as 🗂️ `Display Name` in attribute tables and summaries; tenant root management group should render as 🗂️ Tenant `Display Name` root.

Key constraints and existing building blocks:

- No runtime calls to Azure APIs (mapping file remains user-provided).
- Mapping file sections already exist in the current architecture (Feature 063): `subscriptions`, `managementGroups`, `tenants`, `roles`.
- Cross-provider requirement: applies to Azure-related Terraform providers (azurerm, azapi, azuread, azuredevops).
- Architectural boundary: provider-specific behavior must remain under `src/Oocx.TfPlan2Md/Providers/<ProviderName>/`; shared Azure logic belongs under `src/Oocx.TfPlan2Md/Platforms/Azure/`.
- Existing Azure enrichment primitives:
  - `AzureEntityMapper` resolves subscription / management group / tenant display names.
  - `EnrichedAzureScopeFormatter` formats Azure scopes and already detects tenant-root management group scopes.
  - `ValueFormatterRegistry` + provider modules allow provider-aware value formatting for attribute tables via `format_value` Scriban helper.

## Options Considered

### Option 1: Provider-registered value formatters + enhanced scope formatter (explicit attribute matching)

Implement dedicated value formatters for tenant IDs and management group IDs and register them in each Azure provider module using attribute-name matching (e.g., `tenant_id`, `management_group_id`). Enhance `EnrichedAzureScopeFormatter` to include 🗂️ icon output for management group scopes and tenant-root strings.

- Pros
  - Predictable and low risk of false positives.
  - Uses the existing `ValueFormatterRegistry` pipeline that already formats Azure IDs and role/principal values.
  - Keeps shared Azure logic in `Platforms/Azure` and only registers it from providers.
- Cons
  - Requires maintaining a small list of attribute names per provider.
  - Might miss tenant IDs if they appear under unexpected attribute names.

### Option 2: Provider-registered value formatters + enhanced scope formatter (mapping-based tenant detection)

In addition to explicit attribute matching, add a tenant formatter that can run on **any GUID value** for Azure providers and only applies if the GUID exists in the `tenants` mapping section. Keep management group formatting explicit (attribute names + scope parsing) because management group IDs are often non-GUID strings.

- Pros
  - Best coverage for “everywhere they appear” without growing attribute-name lists.
  - Low overhead (O(1) mapping lookup) and low practical collision risk.
  - Mapping remains selective: only tenants the user chooses to include can be “recognized”.
- Cons
  - A mapped tenant GUID could be formatted in an unexpected context if the same GUID appears in a non-tenant attribute (unlikely, but possible).
  - Requires careful ordering vs. other GUID-based formatters (e.g., role definitions) to avoid confusing output.

### Option 3: Template-only solution (explicit Scriban helpers)

Expose helpers like `azure_tenant(id)` / `azure_management_group(id)` and rely on templates to call them.

- Pros
  - Minimal automatic behavior; maximum author control.
- Cons
  - Violates the “everywhere they appear” goal unless all templates are updated.
  - Higher maintenance cost and greater risk of inconsistency across templates and providers.

## Decision

Choose **Option 2**.

- Implement Azure-agnostic formatting logic in `Platforms/Azure` (tenant and management group label formatting with icons).
- Apply formatting automatically in two places:
  1. **ValueFormatterRegistry**: provider modules register tenant/mgmt-group formatters so `format_value` renders icons in attribute tables and summary fragments that flow through value formatting.
  2. **EnrichedAzureScopeFormatter**: add management-group icon output and prefix tenant-root output with the management-group icon.

Detection rules:

- **Tenant IDs**
  - Primary: format attributes named `tenant_id` (and common casing variants like `tenantId`) across Azure providers.
  - Fallback: for Azure providers, attempt tenant formatting for any GUID-shaped value **only when it exists in the tenant mapping**.

- **Management Group IDs**
  - Format attributes named `management_group_id` (and common casing variants like `managementGroupId`) using the management group mapping.
  - For Azure resource IDs and scopes, apply icon formatting via `EnrichedAzureScopeFormatter` (because scope parsing already identifies management group contexts, including tenant-root detection).

Formatting rules (must match the spec):

- Tenant: 🏢 `Display Name (tenant_id)`
- Management group: 🗂️ `Display Name`
- Tenant root management group: 🗂️ Tenant `Display Name` root

## Rationale

- This approach reuses the existing Azure mapping and enrichment pipeline from Feature 063 rather than introducing new mapping concepts.
- It provides strong coverage (“everywhere they appear”) while keeping false positives unlikely and bounded by the user-controlled mapping file.
- It respects the provider-separation constraint: providers only register formatters; the rules live in shared Azure platform code.

## Consequences

### Positive

- Consistent tenant and management-group rendering across Azure providers and templates.
- No changes to CLI flags and no runtime Azure calls.
- Debug diagnostics for unmapped values can reuse existing `DiagnosticContext` recording in `AzureEntityMapper` when resource context is available.

### Negative

- A tenant GUID that appears in an unexpected place could still be formatted if it is mapped (rare, but possible).
- Requires coordination of formatter precedence to ensure role GUID formatting and tenant GUID formatting do not fight (implementation should prefer role-definition formatting when the attribute context indicates a role).

## Implementation Notes

High-level guidance for the Developer agent:

- **Shared Azure formatting**
  - Add an Azure-specific label formatter in `src/Oocx.TfPlan2Md/Platforms/Azure/` (or a small pair of formatters in `MarkdownGeneration/Services`) that:
    - Resolves display names via `AzureEntityMapper`
    - Produces the exact Markdown strings required (icon outside backticks)
    - Uses non-breaking space between icon and label to keep them together in GitHub/AzDO rendering

- **Scope output**
  - Update `EnrichedAzureScopeFormatter` to:
    - Prefix management group labels with 🗂️ where management group contexts are rendered
    - Prefix tenant-root output with 🗂️

- **Value formatting**
  - Add value formatters (implementing `IValueFormatter`) for:
    - Tenant ID values (attribute-name match + mapping-based GUID fallback)
    - Management group ID values (attribute-name match)
  - Register them from the provider modules for azurerm, azapi, azuread, azuredevops.

- **Diagnostics**
  - When a value cannot be resolved and `--debug` is enabled, ensure the existing diagnostic recording gets the resource address context (via the `ServiceResolutionContext` passed through the registry).

- **Tests & snapshots**
  - Add/adjust tests that cover tenant ID formatting in attribute tables for each Azure provider.
  - Update snapshots as needed (intentional, with the existing snapshot-update workflow).
