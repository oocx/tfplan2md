# Test Plan: Tenant Display Name Mapping

## Overview

This test plan covers the implementation of tenant display name mapping and enhanced management group icons for Azure-related providers. It ensures that tenant IDs and management group IDs are rendered with human-readable names and distinctive icons throughout the reports.

Reference: [docs/features/043-tenant-display-mapping/specification.md](specification.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|----------------------|--------------|-----------|
| Tenant IDs display as `🏢 display_name (tenant_id)` | TC-01, TC-02 | Unit |
| Management groups display with 🗂️ icon | TC-03 | Unit |
| Root management group rendering | TC-04 | Unit |
| Mapping file JSON extension (`tenants` section) | TC-05 | Unit |
| Backward compatibility for mapping files | TC-06 | Unit |
| Fallback to raw ID for unmapped values | TC-07 | Unit |
| Detection by attribute name (`tenant_id`, `tenantId`) | TC-08 | Unit |
| GUID-based fallback detection for tenants | TC-09 | Unit |
| Provider coverage (azurerm, azapi, azuread, azdevops) | TC-10 | Integration (Snapshot) |
| Debug output for unmapped values | TC-11 | Integration |

## User Acceptance Scenarios

> **Purpose**: Verify rendering in real GitHub and Azure DevOps environments to ensure icons and formatting are visually correct and consistent.

### Scenario 1: Multi-tenant Resource Review

**User Goal**: Review a plan containing resources across multiple tenants and easily distinguish them.

**Test PR Context**:
- **GitHub**: Verify that the 🏢 icon is displayed outside the backticks and the tenant name is followed by the GUID in backticks.
- **Azure DevOps**: Verify the same rendering and ensure the non-breaking space between icon and label prevents awkward wrapping.

**Expected Output**:
- Resource attributes like `tenant_id` should show: 🏢 `Contoso Corp (12345678-....)`
- Management groups should show: 🗂️ `Production Workloads`
- Root management groups should show: 🗂️ Tenant `Contoso Corp` root

**Success Criteria**:
- [ ] 🏢 and 🗂️ icons are clearly visible.
- [ ] Formatting follows the `Icon `backticked display name (id)`` pattern.
- [ ] Multi-tenant visibility is improved compared to raw GUIDs.

---

### Scenario 2: Unmapped Tenant Behavior

**User Goal**: Verify that the application still works correctly when a mapping is missing.

**Test PR Context**:
- **GitHub/Azure DevOps**: Plan containing a tenant ID NOT present in the mapping file.

**Expected Output**:
- The attribute should show the raw GUID in backticks: `12345678-....`
- No 🏢 icon should be present for unmapped values (unless it's an attribute explicitly named `tenant_id` - to be confirmed by implementation).

**Success Criteria**:
- [ ] Application does not crash.
- [ ] Raw ID is displayed as fallback.
- [ ] (If --debug used) Debug output mentions the unmapped tenant.

## Test Cases

### TC-01: Tenant ID Formatter - Mapped Value

**Type:** Unit

**Description:**
Verify that `TenantIDFormatter` (or equivalent) correctly formats a mapped tenant ID.

**Preconditions:**
- `AzureEntityMapper` has a mapping for tenant `1234-5678`.

**Test Steps:**
1. Call the formatter with value `1234-5678`.
2. Inspect the result.

**Expected Result:**
Result should be `🏢 `Contoso (1234-5678)``.

---

### TC-02: Tenant ID Formatter - Unmapped Value

**Type:** Unit

**Description:**
Verify fallback behavior when no mapping is available.

**Preconditions:**
- `AzureEntityMapper` has no mapping for `8765-4321`.

**Test Steps:**
1. Call the formatter with value `8765-4321`.
2. Inspect the result.

**Expected Result:**
Result should be ``8765-4321``.

---

### TC-03: Management Group Formatter - Mapped Value

**Type:** Unit

**Description:**
Verify that `ManagementGroupIDFormatter` correctly formats a mapped management group ID.

**Preconditions:**
- `AzureEntityMapper` has mapping for `mg-prod` -> `Production`.

**Test Steps:**
1. Call formatter with `mg-prod`.

**Expected Result:**
Result should be `🗂️ `Production``.

---

### TC-04: Tenant Root Management Group Rendering

**Type:** Unit

**Description:**
Verify `EnrichedAzureScopeFormatter` rendering for tenant root.

**Preconditions:**
- Tenant mapping exists for `tenant-1` -> `Contoso`.

**Test Steps:**
1. Format scope `/providers/Microsoft.Management/managementGroups/tenant-1`.

**Expected Result:**
Result should be `🗂️ Tenant `Contoso` root`.

---

### TC-05: Mapping File Loader - New Section

**Type:** Unit

**Description:**
Verify `AzureMappingFileLoader` correctly parses the `tenants` section.

**Test Data:**
`{ "tenants": [{ "id": "t1", "displayName": "Tenant 1" }] }`

**Expected Result:**
Mapper should contain one tenant entry with ID `t1` and name `Tenant 1`.

---

### TC-06: Mapping File Loader - Backward Compatibility

**Type:** Unit

**Description:**
Verify loader handles files without `tenants` section.

**Test Data:**
`{ "subscriptions": [...] }` (no tenants section)

**Expected Result:**
Loader succeeds, `tenants` collection is empty.

---

### TC-07: Attribute Name Detection

**Type:** Unit

**Description:**
Verify that common variations of tenant and MG attribute names are detected.

**Test Steps:**
1. Check `tenant_id`, `tenantId`, `management_group_id`, `managementGroupId`.

**Expected Result:**
All should trigger their respective formatters.

---

### TC-08: GUID Fallback Detection (Azure Providers)

**Type:** Unit

**Description:**
Verify that any GUID matching a mapped tenant is formatted as a tenant in Azure provider context.

**Preconditions:**
- Mapped tenant `abcd-1234`.
- Provider is `azurerm`.
- Attribute name is `some_random_id`.

**Test Steps:**
1. Call value formatting for value `abcd-1234` under attribute `some_random_id`.

**Expected Result:**
Should be formatted with 🏢 icon because it matches a mapped tenant.

---

### TC-09: Precedence - Role ID vs Tenant ID

**Type:** Unit

**Description:**
Verify that if a GUID matches both a role and a tenant (unlikely but possible in tests), role formatting takes precedence if the attribute suggests a role.

**Test Steps:**
1. Attribute `role_definition_id` with GUID that is also in `tenants` mapping.

**Expected Result:**
Formatted as a role (using 🔑 icon or role-specific formatting).

---

### TC-10: Provider Snapshots

**Type:** Integration (Snapshot)

**Description:**
Verify rendering for all 4 providers using realistic plans.

**Test Data:**
- `azurerm-plan.json`
- `azapi-plan.json`
- `azuread-plan.json`
- `azdevops-plan.json`

**Expected Result:**
Snapshots should show the new icons and formatted tenant/MG names.

---

### TC-11: Debug Diagnostics

**Type:** Integration

**Description:**
Verify that unmapped tenant IDs are logged when `--debug` is enabled.

**Test Steps:**
1. Run with `--debug` and a plan containing unmapped tenants.
2. Check standard error or diagnostic output.

**Expected Result:**
Diagnostics should contain "Unmapped tenant ID: <id>".

## Test Data Requirements

- `azure-mappings-extended.json`: Mapping file with `tenants` section.
- `multi-tenant-plan.json`: A plan containing resources in at least two different tenants.
- Updated `azurerm-azuredevops-plan.json` (or similar) to include tenant IDs in more places.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Invalid GUID in mapping | Load error or skip invalid entry | TC-05 |
| Duplicate IDs in mapping | Last one wins or error | TC-05 |
| Null display name | Fallback to ID | TC-02 |
| Case sensitivity (IDs) | IDs should be matched case-insensitively | TC-01 |

## Open Questions

- **Should unmapped `tenant_id` attributes still get the 🏢 icon?** (Decision: Follow existing pattern for subscriptions - if it's clearly a tenant_id attribute, maybe show the icon even without a name, but usually icons are for *enriched* values).
- **Precedence between different mappings**: If a value matches a Subscription ID AND a Tenant ID (impossible in reality but possible in config), what wins?
