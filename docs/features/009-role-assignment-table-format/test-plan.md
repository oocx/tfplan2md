# Test Plan: Role Assignment Table Format

## Overview

This test plan covers the "Role Assignment Table Format" feature, which changes the rendering of `azurerm_role_assignment` resources from a bullet list to a table format with a summary line. The goal is to ensure visual consistency with other resources and improve readability.

Reference: [Specification](specification.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Role assignments use table format with collapsible `<details>` wrapper | TC-01, TC-02, TC-03, TC-04 | Integration |
| Summary line appears above the details section for all actions | TC-01, TC-02, TC-03, TC-04 | Integration |
| CREATE operations show summary: Principal → Role on Scope | TC-01 | Integration |
| UPDATE operations show only new/after values in summary | TC-02 | Integration |
| REPLACE operations show summary: recreate as Principal → Role on Scope | TC-03 | Integration |
| DELETE operations show summary: remove Role on Scope from Type Principal | TC-04 | Integration |
| Summary uses backticks for principals, roles, and resource names | TC-01, TC-02, TC-03, TC-04 | Integration |
| Description field is displayed on a new line below summary if present | TC-05 | Integration |
| Details table uses plain text (no bold, italic, or backticks in values) | TC-01, TC-02, TC-03, TC-04 | Integration |
| Attribute names use backticks in table headers | TC-01, TC-02, TC-03, TC-04 | Integration |
| Details table includes all non-null attributes | TC-01, TC-02, TC-03, TC-04 | Integration |
| Attributes with null values in both before and after states are omitted | TC-06 | Integration |
| Existing Azure enhancements are preserved (role names, parsed scopes) | TC-07, TC-08 | Unit |
| Role assignments without principal mapping render correctly | TC-09 | Integration |
| Long resource names are not truncated | TC-10 | Integration |

## Test Cases

### TC-01: Render Create Operation

**Type:** Integration

**Description:**
Verifies that a `create` operation renders with the correct summary format and details table.

**Preconditions:**
- Test data with `azurerm_role_assignment` having `change.actions = ["create"]`.
- Resource does NOT have a `description`.

**Test Steps:**
1. Parse the test plan containing a create operation.
2. Render the markdown report.
3. Inspect the output for the specific resource.

**Expected Result:**
- Summary line: `**Summary:** \`Principal Name\` (Type) → \`Role Name\` on \`Scope Name\``
- No description line is rendered below the summary.
- Details block is wrapped in `<details>`.
- Table contains columns "Attribute" and "Value".
- Table rows include `scope`, `role_definition_id`, `principal_id`.
- Values are plain text.

**Test Data:**
`role-assignments.json` (Scenario: Create)

---

### TC-02: Render Update Operation

**Type:** Integration

**Description:**
Verifies that an `update` operation renders with the correct summary format (showing new values) and a 3-column details table.

**Preconditions:**
- Test data with `azurerm_role_assignment` having `change.actions = ["update"]`.

**Test Steps:**
1. Parse the test plan containing an update operation.
2. Render the markdown report.

**Expected Result:**
- Summary line: `**Summary:** \`Principal Name\` (Type) → \`Role Name\` on \`Scope Name\`` (using "after" values).
- Table contains columns "Attribute", "Before", "After".
- Changed attributes show both values.

**Test Data:**
`role-assignments.json` (Scenario: Update)

---

### TC-03: Render Replace Operation

**Type:** Integration

**Description:**
Verifies that a `replace` operation renders with the "recreate as" summary prefix.

**Preconditions:**
- Test data with `azurerm_role_assignment` having `change.actions = ["create", "delete"]` (replace).

**Test Steps:**
1. Parse the test plan containing a replace operation.
2. Render the markdown report.

**Expected Result:**
- Summary line: `**Summary:** recreate as \`Principal Name\` (Type) → \`Role Name\` on \`Scope Name\``
- Table contains columns "Attribute", "Before", "After".

**Test Data:**
`role-assignments.json` (Scenario: Replace)

---

### TC-04: Render Delete Operation

**Type:** Integration

**Description:**
Verifies that a `delete` operation renders with the "remove..." summary format.

**Preconditions:**
- Test data with `azurerm_role_assignment` having `change.actions = ["delete"]`.

**Test Steps:**
1. Parse the test plan containing a delete operation.
2. Render the markdown report.

**Expected Result:**
- Summary line: `**Summary:** remove \`Role Name\` on \`Scope Name\` from Type \`Principal Name\``
- Table contains columns "Attribute", "Value".

**Test Data:**
`role-assignments.json` (Scenario: Delete)

---

### TC-05: Render Description Field

**Type:** Integration

**Description:**
Verifies that the `description` field appears below the summary line when present.

**Preconditions:**
- Test data includes `description` attribute.

**Test Steps:**
1. Parse the test plan.
2. Render the markdown report.

**Expected Result:**
- The description text appears on the line immediately following the summary line.
- It is plain text (no formatting).

**Test Data:**
`role-assignments.json` (Scenario: With Description)

---

### TC-06: Optional Attributes Handling

**Type:** Integration

**Description:**
Verifies that optional attributes are included when present and omitted when null.

**Preconditions:**
- Test data includes a resource with an optional attribute (e.g. `condition`) set to a value.
- Test data includes a resource with an optional attribute (e.g. `delegated_managed_identity_resource_id`) set to null.

**Test Steps:**
1. Parse the test plan.
2. Render the markdown report.

**Expected Result:**
- The details table includes rows for non-null optional attributes (e.g. `condition`).
- The details table does NOT contain rows for the null attributes.

**Test Data:**
`role-assignments.json` (Scenario: Optional Attributes)

---

### TC-07: Helper - AzureScopeParser Structured Data

**Type:** Unit

**Description:**
Verifies that the new `AzureScopeParser.Parse` method returns a correct `ScopeInfo` object.

**Test Steps:**
1. Call `AzureScopeParser.Parse` with a subscription scope string.
2. Call `AzureScopeParser.Parse` with a resource group scope string.
3. Call `AzureScopeParser.Parse` with a resource scope string.

**Expected Result:**
- Returns object with correct `Name`, `Type`, `SubscriptionId`, etc.
- Does NOT return markdown formatted strings.

---

### TC-08: Helper - AzureRoleDefinitionMapper Structured Data

**Type:** Unit

**Description:**
Verifies that `AzureRoleDefinitionMapper.GetRoleDefinition` returns the correct name and ID tuple.

**Test Steps:**
1. Call with a known role ID (e.g., Reader).
2. Call with an unknown role ID.

**Expected Result:**
- Known ID: Returns `("Reader", "guid")`.
- Unknown ID: Returns `("guid", "guid")` or fallback.

---

### TC-09: Missing Principal Mapping

**Type:** Integration

**Description:**
Verifies rendering when no principal mapping file is provided or principal ID is not found.

**Preconditions:**
- Run rendering without a principal mapping file.

**Test Steps:**
1. Render report for a plan with a principal ID.

**Expected Result:**
- Summary shows the Principal ID (or whatever fallback is defined) in backticks.
- No crash or error.

**Test Data:**
`role-assignments.json`

---

### TC-10: Long Names

**Type:** Integration

**Description:**
Verifies that very long resource names or principal names are not truncated.

**Preconditions:**
- Test data with 100+ character names.

**Test Steps:**
1. Render report.

**Expected Result:**
- Full name is displayed in the summary and table.

**Test Data:**
`role-assignments.json` (Scenario: Long Names)

## Test Data Requirements

New file: `src/tests/Oocx.TfPlan2Md.Tests/TestData/role-assignments.json`
- Contains a `resource_changes` array with at least 5 items:
    1.  Create operation (standard)
    2.  Update operation (standard)
    3.  Replace operation (standard)
    4.  Delete operation (standard)
    5.  Create operation with `description` and long names.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Missing `role_definition_id` | Fallback to `role_definition_name` | TC-01 (Variant) |
| Empty `description` | No description line rendered | TC-01 |
| Unknown Scope format | Render raw scope string | TC-07 |
| Null Principal ID | Handle gracefully (empty string or "Unknown") | TC-09 |

## Open Questions

None.
