# Test Plan: Universal Azure Resource ID Formatting

## Overview

This test plan covers the extension of human-readable Azure resource ID formatting to all `azurerm` resources. It ensures that Azure resource IDs are correctly detected, formatted, and displayed in the main change table instead of being treated as large values.

Reference: [specification.md](specification.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Automatic detection of Azure resource IDs by pattern | TC-01, TC-02 | Unit |
| Formatting using `AzureScopeParser` logic | TC-03 | Unit |
| Formatted IDs appear in change tables (exempt from large value) | TC-04, TC-05 | Unit, Integration |
| No false positives for non-Azure ID values | TC-06 | Unit |
| Existing role assignment tests continue to pass | TC-07 | Regression |
| Verify formatting for common ID attributes | TC-08 | Integration |

## User Acceptance Scenarios

### Scenario 1: Long Azure Resource IDs in Change Table

**User Goal**: View long Azure resource IDs (like `key_vault_id`) in a readable format directly in the change table.

**Test PR Context**:
- **GitHub**: Verify that long IDs are formatted and stay in the table.
- **Azure DevOps**: Verify that long IDs are formatted and stay in the table.

**Expected Output**:
- A resource like `azurerm_key_vault_secret` should show its `key_vault_id` in the change table.
- The value should be formatted as `Key Vault <name> in resource group <rg> of subscription <sub-id>`.
- The value should NOT be in the "Large attributes" section.

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] Information is accurate and complete
- [ ] Long IDs do not trigger the "Large attributes" section

## Test Cases

### TC-01: AzureScopeParser_IsAzureResourceId_ValidIds_ReturnsTrue

**Type:** Unit

**Description:**
Verifies that `IsAzureResourceId` correctly identifies various valid Azure resource ID patterns.

**Preconditions:**
- None

**Test Steps:**
1. Call `AzureScopeParser.IsAzureResourceId` with:
   - `/subscriptions/12345678-1234-1234-1234-123456789012`
   - `/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/my-rg`
   - `/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.KeyVault/vaults/my-kv`
   - `/providers/Microsoft.Management/managementGroups/my-mg`

**Expected Result:**
All calls return `true`.

---

### TC-02: AzureScopeParser_IsAzureResourceId_InvalidIds_ReturnsFalse

**Type:** Unit

**Description:**
Verifies that `IsAzureResourceId` returns `false` for strings that are not Azure resource IDs.

**Preconditions:**
- None

**Test Steps:**
1. Call `AzureScopeParser.IsAzureResourceId` with:
   - `not-an-id`
   - `https://portal.azure.com`
   - `/subscriptions/` (too short)
   - `12345678-1234-1234-1234-123456789012` (just a GUID)

**Expected Result:**
All calls return `false`.

---

### TC-03: ScribanHelpers_FormatValue_AzureId_ReturnsFormattedString

**Type:** Unit

**Description:**
Verifies that `FormatValue` uses `AzureScopeParser.ParseScope` for Azure IDs when the provider is `azurerm`.

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.FormatValue` with:
   - `value`: `/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.KeyVault/vaults/my-kv`
   - `providerName`: `registry.terraform.io/hashicorp/azurerm`

**Expected Result:**
Returns: Key Vault `my-kv` in resource group `my-rg` of subscription `sub-id`.

---

### TC-04: ScribanHelpers_IsLargeValue_AzureId_ReturnsFalse

**Type:** Unit

**Description:**
Verifies that long Azure resource IDs are NOT treated as large values when the provider is `azurerm`.

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.IsLargeValue` with:
   - `input`: `/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/very-long-resource-group-name-that-exceeds-one-hundred-characters-threshold/providers/Microsoft.KeyVault/vaults/my-kv`
   - `providerName`: `registry.terraform.io/hashicorp/azurerm`

**Expected Result:**
Returns `false`.

---

### TC-05: MarkdownRenderer_AzureResourceId_StaysInTable

**Type:** Integration

**Description:**
Verifies that a resource with a long Azure ID renders it in the change table using the default template.

**Preconditions:**
- A Terraform plan JSON containing an `azurerm_key_vault_secret` with a long `key_vault_id`.

**Test Steps:**
1. Render the plan using `MarkdownRenderer`.
2. Inspect the generated markdown.

**Expected Result:**
- The `key_vault_id` attribute is present in the `<details>` table of the resource.
- The value is formatted with inline code for values.
- There is no "Large attributes" section for this resource.

---

### TC-06: ScribanHelpers_FormatValue_NonAzureId_ReturnsBacktickedString

**Type:** Unit

**Description:**
Verifies that non-Azure ID values are still formatted with backticks.

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.FormatValue` with:
   - `value`: `standard-value`
   - `providerName`: `registry.terraform.io/hashicorp/azurerm`

**Expected Result:**
Returns `` `standard-value` ``.

---

### TC-07: Regression_RoleAssignmentFormatting

**Type:** Regression

**Description:**
Ensures that existing role assignment formatting still works correctly.

**Preconditions:**
- Existing role assignment test data.

**Test Steps:**
1. Run `MarkdownRendererRoleAssignmentTests`.

**Expected Result:**
All tests pass.

---

### TC-08: Integration_CommonAzureIdAttributes

**Type:** Integration

**Description:**
Verifies formatting for a variety of common Azure ID attributes.

**Preconditions:**
- Test data with `subnet_id`, `workspace_id`, `storage_account_id`.

**Test Steps:**
1. Render the plan.
2. Verify each attribute is formatted correctly in the output.

**Expected Result:**
All attributes are formatted according to `AzureScopeParser` logic.

## Test Data Requirements

New test data files needed:
- `azure-resource-ids.json` - A plan containing various `azurerm` resources with long ID attributes:
    - `azurerm_key_vault_secret` (`key_vault_id`)
    - `azurerm_monitor_diagnostic_setting` (`log_analytics_workspace_id`)
    - `azurerm_subnet` (`virtual_network_id`)
    - `azurerm_network_interface` (`subnet_id`)

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| ID exactly 100 chars | Treated as Azure ID (not large) | TC-04 |
| ID with newlines | Treated as large value (newlines take precedence) | TC-04 |
| Non-azurerm provider with Azure-like ID | Treated as large value (no exemption) | TC-04 |
| Invalid Azure ID format | Treated as normal value (backticked) | TC-03 |

## Non-Functional Tests

- **Performance**: Ensure that pattern matching for every attribute doesn't significantly slow down report generation.

## Open Questions

None.
