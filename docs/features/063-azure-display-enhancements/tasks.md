# Tasks: Azure Display Enhancements

## Overview

Implement enhancements to Azure resource display across multiple providers (azurerm, azapi, azuread, azdevops). This includes universal resource ID detection, enrichment of IDs with display names (subscriptions, management groups, tenants, roles), and resource-specific summary improvements.

Reference: [specification.md](specification.md), [architecture.md](architecture.md), [test-plan.md](test-plan.md)

## Tasks

### Task 1: Foundation - Extend Mapping Models and Loading logic

**Priority:** High

**Description:**
Update the mapping file models to support the new sections (subscriptions, managementGroups, tenants, roles) using the array-of-objects format. Implement `AzureMappingFileLoader` and refactor `PrincipalMapper` to accept pre-parsed data.

**Acceptance Criteria:**
- [x] `MappingEntry` record created for `id`/`displayName` pairs.
- [x] `PrincipalMappingFile` model updated with new sections (backward compatible).
- [x] `AzureMappingFileLoader` implemented to load and parse the unified JSON file.
- [x] `PrincipalMapper` refactored to take `IReadOnlyDictionary<string, string>` principals and `IReadOnlyDictionary<string, string>` types.
- [x] TC-15 and TC-16 (Unit tests for backward compatibility and new section parsing) pass.
- [x] `DiagnosticContext` updated with new counters for mapping statistics.

**Dependencies:** None

---

### Task 2: Implement AzureEntityMapper and EnrichedAzureScopeFormatter

**Priority:** High

**Description:**
Create the `AzureEntityMapper` to resolve subscription, management group, and tenant IDs. Create the `EnrichedAzureScopeFormatter` to inject these names into resource scope strings.

**Acceptance Criteria:**
- [ ] `AzureEntityMapper` implemented and tested with TC-02 and TC-04.
- [ ] `EnrichedAzureScopeFormatter` implemented to post-process `ScopeInfo` from `AzureScopeParser`.
- [ ] Root management group formatted as "Tenant `<name>` root" when IDs match (TC-05).
- [ ] Subscription names injected as `DisplayName (ID)` (TC-03).
- [ ] Integration: `AzureResourceIdFormatter` uses `EnrichedAzureScopeFormatter`.

**Dependencies:** Task 1

---

### Task 3: Implement Role Definition Resolution

**Priority:** High

**Description:**
Extend `AzureRoleDefinitionMapper` to support custom role overrides and implement `RoleDefinitionFormatter` to display role names for role attributes.

**Acceptance Criteria:**
- [ ] `AzureRoleDefinitionMapper.MergeCustomRoles` implemented to merge mappings from the JSON file.
- [ ] Custom roles can override built-in roles (TC-08).
- [ ] `RoleDefinitionFormatter` implemented to match `role_definition_id` and `role_definition_resource_id` attributes.
- [ ] `AzureRMModule` registers `RoleDefinitionFormatter` in `RegisterValueFormatters`.
- [ ] TC-06, TC-07, and TC-12 pass.

**Dependencies:** Task 1

---

### Task 4: Resource-Specific Summaries (Simple)

**Priority:** Medium

**Description:**
Implement the enhanced summary for `azurerm_private_dns_a_record`.

**Acceptance Criteria:**
- [ ] `ResourceSummaryMappings` updated with `azurerm_private_dns_a_record` keys (`name`, `zone_name`).
- [ ] `ResourceSummaryBuilder` updated with special-case logic to join names with `.` (TC-09).

**Dependencies:** None

---

### Task 5: Resource-Specific Summaries (Complex)

**Priority:** Medium

**Description:**
Implement `ViewModelFactory` classes for PIM assignments and role management policies.

**Acceptance Criteria:**
- [ ] `PimEligibleRoleAssignmentFactory` implemented for `azurerm_pim_eligible_role_assignment` (TC-10).
- [ ] `RoleManagementPolicyFactory` implemented for `azurerm_role_management_policy` (TC-11).
- [ ] `AzureRMModule` registers the new factories.
- [ ] Summaries correctly resolve principal names, role names, and enriched scopes.

**Dependencies:** Tasks 1, 2, 3

---

### Task 6: Debugging and Fallback Improvements

**Priority:** Medium

**Description:**
Improve failure tracking in `DiagnosticContext` and ensure raw ID fallback works correctly.

**Acceptance Criteria:**
- [ ] `DiagnosticContext` tracks failed resolutions with context (referencing resource) (TC-14).
- [ ] All mappers correctly fall back to raw IDs when no mapping is found (TC-13).
- [ ] Debug output reflects the new mapping stats and failure details.

**Dependencies:** Tasks 1, 2, 3

---

### Task 7: Universal Resource ID Detection

**Priority:** Low

**Description:**
Ensure any attribute containing an Azure resource ID gets formatted.

**Acceptance Criteria:**
- [ ] `AzureRMModule` (and other Azure modules) registers the `AzureResourceIdFormatter` with a value pattern match instead of just attribute name match if necessary, or verify current regex-based detector handles any attribute value.
- [ ] TC-01 (Integration test with unknown attribute name) passes.

**Dependencies:** Task 2

---

### Task 8: Documentation and Utility Scripts

**Priority:** Low

**Description:**
Update project documentation with the new mapping format and provide Azure CLI commands for population.

**Acceptance Criteria:**
- [ ] `README.md` or `docs/` updated with the extended mapping file schema.
- [ ] Azure CLI snippets provided for exporting subscriptions, management groups, tenants, and roles.
- [ ] `scripts/validate-azure-cli-commands.sh` created and TC-17 passes.

**Dependencies:** None

---

### Task 9: Final Integration and UAT Artifacts

**Priority:** Medium

**Description:**
Create snapshots and demo artifacts for the new rendering.

**Acceptance Criteria:**
- [ ] New test data `azure-display-enhancements.json` and `azure-mappings-extended.json` created.
- [ ] Snapshots updated to reflect new formatting.
- [ ] `uat-test-plan.md` scenarios verified via `scripts/uat-run.sh` (or manual PR review if scripts not yet available for these resources).

**Dependencies:** All implementation tasks

## Implementation Order

1. **Task 1: Foundation** - Must be first as it changes core mapping models and `PrincipalMapper` constructor.
2. **Task 2: Entity Mapper & Scope Formatter** - Enables the core "display name" feature.
3. **Task 3: Role Resolution** - Complements the display name feature with role names.
4. **Task 4: DNS Summaries** - Quick win, independent.
5. **Task 5: Complex Summaries** - Depends on foundation mappers.
6. **Task 6 & 7: Polish** - Refine failure tracking and universal detection.
7. **Task 8: Documentation** - Can be done anytime but best after implementation is stable.
8. **Task 9: UAT** - Final verification.

## Open Questions

- None. Requirements and architecture have been clarified.
