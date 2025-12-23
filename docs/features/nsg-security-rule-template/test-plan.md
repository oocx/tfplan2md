# Test Plan: Network Security Group Security Rules Template

## Overview

This test plan covers the verification of the specialized Scriban template for `azurerm_network_security_group`. The goal is to ensure that security rules are rendered in a semantic, easy-to-read table that highlights changes (added, modified, removed, unchanged) and respects priority ordering.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Template created for `azurerm_network_security_group` | TC-01, TC-02, TC-03 | Unit |
| Rules categorized correctly (Added, Modified, Removed, Unchanged) | TC-03 | Unit |
| Rules sorted by ascending priority | TC-03, TC-04 | Unit |
| All specified columns displayed | TC-01, TC-03 | Unit |
| Modified rules show before/after values with `-`/`+` prefixes | TC-03 | Unit |
| Unchanged attributes in modified rules show single value | TC-03 | Unit |
| Multi-value fields (address prefixes, port ranges) rendered correctly | TC-05 | Unit |
| Singular vs Plural field precedence handled correctly | TC-05 | Unit |

## Test Cases

### TC-01: Render NSG Creation

**Type:** Unit

**Description:**
Verify that creating a new NSG renders a simple table with all rules listed.

**Preconditions:**
- Terraform plan JSON containing a `create` action for `azurerm_network_security_group`.

**Test Steps:**
1. Parse the plan JSON.
2. Render the resource change using the `MarkdownRenderer`.
3. Inspect the output markdown.

**Expected Result:**
- Header shows "Action: Create" (or similar).
- Table lists all rules.
- No diff icons (or all implied as new).
- Columns match the specification.

**Test Data:**
- `nsg-create.json` (or section within `nsg-rule-changes.json`)

---

### TC-02: Render NSG Deletion

**Type:** Unit

**Description:**
Verify that deleting an NSG renders a table showing the rules that are being removed.

**Preconditions:**
- Terraform plan JSON containing a `delete` action for `azurerm_network_security_group`.

**Test Steps:**
1. Parse the plan JSON.
2. Render the resource change.

**Expected Result:**
- Header shows "Action: Delete".
- Table lists all rules being deleted.

---

### TC-03: Render NSG Update - Mixed Changes

**Type:** Unit

**Description:**
Verify that an update to an NSG correctly categorizes rules as Added, Modified, Removed, and Unchanged, and displays diffs for modified rules.

**Preconditions:**
- Terraform plan JSON containing an `update` action for `azurerm_network_security_group` with:
    - One new rule added.
    - One rule removed.
    - One rule modified (e.g., port changed).
    - One rule unchanged.

**Test Steps:**
1. Parse the plan JSON.
2. Render the resource change.

**Expected Result:**
- Table contains rows with icons: ➕, ❌, 🔄, ⏺️.
- Modified rule shows `- <old_value><br>+ <new_value>` for the changed attribute.
- Modified rule shows single value for unchanged attributes.
- Rules are matched by `name`.

**Test Data:**
- `nsg-rule-changes.json`

---

### TC-04: Render NSG Update - Priority Sorting

**Type:** Unit

**Description:**
Verify that rules are sorted by priority in ascending order within the table, regardless of their order in the JSON array.

**Preconditions:**
- Terraform plan JSON where rules are not sorted by priority in the `security_rule` array.

**Test Steps:**
1. Parse the plan JSON.
2. Render the resource change.

**Expected Result:**
- The rendered table rows are ordered by the `priority` column (ascending).
- For modified rules, the `after` priority is used for sorting.

---

### TC-05: Render NSG Update - Address/Port Logic

**Type:** Unit

**Description:**
Verify the logic for handling singular vs. plural address and port fields.

**Preconditions:**
- Terraform plan JSON with rules using:
    - `source_address_prefix` (singular)
    - `source_address_prefixes` (plural)
    - Both (plural should take precedence if populated)
    - `*` (wildcard)

**Test Steps:**
1. Parse the plan JSON.
2. Render the resource change.

**Expected Result:**
- Singular fields are displayed directly.
- Plural fields are joined by commas.
- If both exist, plural is used (or as per Terraform provider behavior/template logic).
- Wildcards are displayed as `*`.

## Test Data Requirements

Create a new test data file `tests/Oocx.TfPlan2Md.Tests/TestData/nsg-rule-changes.json` containing:
- An `azurerm_network_security_group` resource change.
- `before` state with a set of rules.
- `after` state with:
    - A rule with a new name (Added).
    - A rule removed (missing in after).
    - A rule with same name but different properties (Modified).
    - A rule with same name and properties (Unchanged).
    - Usage of `source_address_prefix` vs `source_address_prefixes`.

## Open Questions

None.
