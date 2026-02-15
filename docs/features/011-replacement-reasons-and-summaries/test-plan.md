# Test Plan: Replacement Reasons and Resource Summaries

## Overview

This test plan covers the validation of the "Replacement Reasons and Resource Summaries" feature. The goal is to ensure that every resource change in the generated markdown report includes a concise summary line and that replacement reasons are accurately displayed when available.

**Reference:**
- [Specification](specification.md)
- [Architecture](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `replace_paths` field is parsed from Terraform plan JSON | TC-01 | Unit |
| `ReplacePaths` property is added to `ResourceChangeModel` | TC-02 | Integration |
| `Summary` property is added to `ResourceChangeModel` | TC-02 | Integration |
| Summary generated for CREATE (Resource-Specific Mapping) | TC-03 | Unit |
| Summary generated for CREATE (Provider Fallback) | TC-04 | Unit |
| Summary generated for CREATE (Generic Fallback) | TC-05 | Unit |
| Summary generated for UPDATE (< 3 changes) | TC-06 | Unit |
| Summary generated for UPDATE (> 3 changes) | TC-07 | Unit |
| Summary generated for REPLACE (with reason) | TC-08 | Unit |
| Summary generated for REPLACE (without reason) | TC-09 | Unit |
| Summary generated for DELETE | TC-10 | Unit |
| Specific mappings for `azurerm` resources | TC-03, TC-11 | Unit |
| Specific mappings for `msgraph` resources (URL handling) | TC-12 | Unit |
| Summary line appears in rendered Markdown | TC-13 | Snapshot |

## Test Cases

### TC-01: Parse Replace Paths

**Type:** Unit (Parser)

**Description:**
Verify that the `TerraformPlanParser` correctly deserializes the `replace_paths` field from the JSON input into the `Change` record.

**Preconditions:**
- Input JSON contains a resource change with `replace_paths`.

**Test Steps:**
1. Parse a JSON snippet containing `replace_paths: [["address_prefixes", 0]]`.
2. Inspect the resulting `TerraformPlan` object.

**Expected Result:**
- `Change.ReplacePaths` is not null.
- It contains one list with elements "address_prefixes" and 0 (as JsonElement or object).

---

### TC-02: Model Builder Integration

**Type:** Integration (ReportModelBuilder)

**Description:**
Verify that `ReportModelBuilder` correctly populates the `Summary` and `ReplacePaths` properties on the `ResourceChangeModel`.

**Preconditions:**
- A `TerraformPlan` object with valid changes.
- A mock or real `IResourceSummaryBuilder`.

**Test Steps:**
1. Call `ReportModelBuilder.Build(plan)`.
2. Inspect the returned `ReportModel`.

**Expected Result:**
- `ResourceChangeModel.Summary` contains the string returned by the builder.
- `ResourceChangeModel.ReplacePaths` matches the input plan data.

---

### TC-03: Summary - Create (Resource Specific)

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify that the summary for a CREATE action uses the specific attribute mapping for a known resource type.

**Preconditions:**
- Resource Type: `azurerm_storage_account`
- Action: `create`
- State (After): Contains `name`, `resource_group_name`, `location`, `account_tier`.

**Test Steps:**
1. Call `BuildSummary` with the resource change.

**Expected Result:**
- Summary string follows format: `` `name` in `rg` (location) | tier ``.
- Example: `` `st1` in `rg1` (eastus) | Standard ``.

---

### TC-04: Summary - Create (Provider Fallback)

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify that the summary falls back to provider defaults when the resource type is not explicitly mapped but the provider is known.

**Preconditions:**
- Resource Type: `azurerm_unknown_resource`
- Action: `create`
- State (After): Contains `name`, `resource_group_name`, `location`.

**Test Steps:**
1. Call `BuildSummary`.

**Expected Result:**
- Summary includes `name`, `resource_group_name`, and `location` (the azurerm fallback).

---

### TC-05: Summary - Create (Generic Fallback)

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify that the summary falls back to `name` or `display_name` for unknown providers.

**Preconditions:**
- Resource Type: `random_string`
- Action: `create`
- State (After): Contains `result` (ignored) and `id` (ignored), but we simulate a `name` or `display_name` if applicable, or fallback to address if neither exists. *Correction based on spec:* Spec says "name or display_name (first that exists)". If neither, we should probably fallback to the resource address or empty.

**Test Steps:**
1. Call `BuildSummary` with a resource having `display_name`.

**Expected Result:**
- Summary shows the `display_name` value.

---

### TC-06: Summary - Update (Few Changes)

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify UPDATE summary lists changed attributes when count <= 3.

**Preconditions:**
- Action: `update`
- Attribute Changes: `tags`, `sku`.

**Test Steps:**
1. Call `BuildSummary`.

**Expected Result:**
- Summary format: `` `name` | Changed: tags, sku ``.

---

### TC-07: Summary - Update (Many Changes)

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify UPDATE summary truncates changed attributes when count > 3.

**Preconditions:**
- Action: `update`
- Attribute Changes: `tags`, `sku`, `capacity`, `tier`.

**Test Steps:**
1. Call `BuildSummary`.

**Expected Result:**
- Summary format: `` `name` | Changed: tags, sku, capacity, +1 more ``.

---

### TC-08: Summary - Replace (With Reason)

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify REPLACE summary includes the replacement reason.

**Preconditions:**
- Action: `replace`
- ReplacePaths: `[["sku", "tier"], ["location"]]`

**Test Steps:**
1. Call `BuildSummary`.

**Expected Result:**
- Summary format: `` recreate `name` (sku.tier, location changed: force replacement) ``.

---

### TC-09: Summary - Replace (No Reason)

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify REPLACE summary handles missing replacement reasons (legacy plans).

**Preconditions:**
- Action: `replace`
- ReplacePaths: `null` or empty.
- Attribute Changes count: 2.

**Test Steps:**
1. Call `BuildSummary`.

**Expected Result:**
- Summary format: `` recreating `name` (2 changed) ``.

---

### TC-10: Summary - Delete

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify DELETE summary shows only the resource name.

**Preconditions:**
- Action: `delete`
- State (Before): Contains `name`.

**Test Steps:**
1. Call `BuildSummary`.

**Expected Result:**
- Summary format: `` `name` ``.

---

### TC-11: Mappings - Azurerm Coverage

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify a sample of the 49 mapped `azurerm` resources to ensure the registry is correctly populated.

**Test Steps:**
1. Test `azurerm_virtual_network` (expects `address_space`).
2. Test `azurerm_subnet` (expects `address_prefixes`).
3. Test `azurerm_key_vault` (expects `sku_name`).

**Expected Result:**
- Correct attributes are extracted for each type.

---

### TC-12: Mappings - MSGraph URL

**Type:** Unit (ResourceSummaryBuilder)

**Description:**
Verify that `msgraph` resources use the `url` attribute as the primary identifier.

**Preconditions:**
- Resource Type: `msgraph_resource`
- State: `url` = "applications", `body.displayName` = "MyApp"

**Test Steps:**
1. Call `BuildSummary`.

**Expected Result:**
- Summary includes "applications" and "MyApp".

---

### TC-13: Rendered Output

**Type:** Snapshot

**Description:**
Verify the final Markdown output contains the summary line above the details block.

**Preconditions:**
- A full `ReportModel` with summaries.

**Test Steps:**
1. Render the model using `default.sbn`.

**Expected Result:**
- Output contains `**Summary:** ...` followed by `<details>`.

## Test Data Requirements

- **`replacement-reasons.json`**: A Terraform plan JSON (v1.2+) containing `replace_paths`.
- **`resource-types.json`**: A plan containing various resource types (`azurerm`, `azuread`, `msgraph`) to verify mapping logic.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Resource has no `name` or `display_name` | Fallback to resource address or empty string | TC-05 (Variant) |
| `replace_paths` contains complex object | Convert to string representation or simplified path | TC-01 |
| `msgraph` body is missing `displayName` | Show only `url` | TC-12 (Variant) |
| Null values in state attributes | Handle gracefully (print empty or skip) | TC-03 (Variant) |

## Non-Functional Tests

- **Performance**: Summary generation should not significantly increase processing time for large plans (1000+ resources).
