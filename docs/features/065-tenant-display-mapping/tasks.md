# Tasks: Tenant Display Name Mapping

## Overview

This feature adds display name mapping for Entra ID tenants with visual icons (🏢) and enhances management group display with icons (🗂️). It ensures consistent, human-readable formatting for these Azure entities across all relevant providers (azurerm, azapi, azuread, azdevops).

Reference: [specification.md](specification.md), [architecture.md](architecture.md).

## Tasks

### Task 1: Update Azure Entity Mapper

**Priority:** High

**Description:**
Update `AzureEntityMapper` to follow the new formatting rule for tenants: `DisplayName (Id)`.

**Acceptance Criteria:**
- [x] `GetTenantDisplayName` returns `DisplayName (Id)` when a mapping exists.
- [x] `GetTenantDisplayName` returns raw `Id` when no mapping exists.
- [x] Unit tests for `AzureEntityMapper` cover these cases (mapped/unmapped).
- [x] Diagnostic recording for unmapped tenants is verified.

**Dependencies:** None

---

### Task 2: Shared Azure Formatting Logic

**Priority:** Medium

**Description:**
Add shared formatting constants and/or logic for Azure icons and labels to ensure consistency between scope formatting and attribute value formatting.

**Acceptance Criteria:**
- [x] `🏢` icon and `🗂️` icon are defined as constants (using `\u00A0` for non-breaking space).
- [x] Logic for formatting tenant labels and management group labels is reusable.

**Dependencies:** Task 1

---

### Task 3: Update Enriched Azure Scope Formatter

**Priority:** High

**Description:**
Update `EnrichedAzureScopeFormatter` to include icons for management groups and tenant root management groups.

**Acceptance Criteria:**
- [x] Management group scopes are prefixed with 🗂️.
- [x] Tenant root management group scopes are prefixed with 🗂️ and follow the format: `🗂️ Tenant Display Name root`.
- [x] Unit tests for `EnrichedAzureScopeFormatter` are updated/added (TC-03, TC-04).

**Dependencies:** Task 2

---

### Task 4: Implement Value Formatters for Tenant and Management Group IDs

**Priority:** High

**Description:**
Create `IValueFormatter` implementations for Tenant IDs and Management Group IDs.

**Acceptance Criteria:**
- [x] `TenantIdFormatter`:
    - [x] Match by attribute name (`tenant_id`, `tenantId`).
    - [x] GUID-based fallback detection for Azure providers (if value matches a mapped tenant).
    - [x] Formats with 🏢 icon.
- [x] `ManagementGroupIdFormatter`:
    - [x] Match by attribute name (`management_group_id`, `managementGroupId`).
    - [x] Formats with 🗂️ icon.
- [x] Precedence logic ensures role IDs are not misidentified as tenant IDs when the attribute name suggests a role (TC-09).
- [x] Unit tests for both formatters (TC-01, TC-02, TC-03, TC-07, TC-08, TC-09).

**Dependencies:** Task 2

---

### Task 5: Register Value Formatters in Provider Modules

**Priority:** High

**Description:**
Register the new formatters in `azurerm`, `azapi`, `azuread`, and `azdevops` providers.

**Acceptance Criteria:**
- [x] `AzureRMModule` (via `AzureRmValueFormatterRegistration`) registers both formatters.
- [x] `AzApiModule` registers both formatters.
- [x] `AzureADModule` implements `RegisterValueFormatters` and registers `TenantIdFormatter`.
- [x] `AzureDevOpsModule` implements `RegisterValueFormatters` and registers `TenantIdFormatter`.
- [x] Integration tests verify that formatters are active for these providers.

**Dependencies:** Task 4

---

### Task 6: Update Examples and Test Data

**Priority:** Medium

**Description:**
Update mapping files in `examples/` and test snapshots to include tenant mappings.

**Acceptance Criteria:**
- [x] `examples/` mapping files include a `tenants` section.
- [x] `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azure-mappings-extended.json` is created/updated.
- [x] Test snapshots for all Azure providers are updated and verified (TC-10).

**Dependencies:** Task 5

---

### Task 7: Documentation Updates

**Priority:** Low

**Description:**
Update documentation with help on how to populate the `tenants` section and filter mappings by tenant.

**Acceptance Criteria:**
- [x] Documentation includes Azure CLI commands for retrieving specific tenants, users, etc. (as specified in the specification).
- [x] Examples show the benefit of multi-tenant mapping.

**Dependencies:** None

---

### Task 8: Final Verification & UAT

**Priority:** High

**Description:**
Perform a final run of all tests and execute UAT.

**Acceptance Criteria:**
- [x] All unit tests pass.
- [x] All snapshot tests pass.
- [ ] UAT artifacts are generated and verified on GitHub and Azure DevOps.

**Dependencies:** Task 6

## Implementation Order

1. **Task 1 & 2** - Foundational mapping and formatting logic.
2. **Task 3 & 4** - Core implementation in scope and value formatters.
3. **Task 5** - Wiring up the formatters to providers.
4. **Task 6 & 7** - Data updates and documentation.
5. **Task 8** - Final validation.

## Open Questions

- None at this stage. Logic for precedence (Role vs Tenant) will be handled in the `MatchPattern` or within the formatter logic as discussed in architecture.
