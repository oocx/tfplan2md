# Test Plan: Improved AzAPI Attribute Grouping and Array Rendering

## Overview

This test plan validates the improved rendering of AzAPI JSON body attributes, focusing on prefix-based grouping and enhanced array rendering. The goal is to ensure complex JSON structures are rendered in a readable, structured format in GitHub and Azure DevOps markdown while preserving all information.

**Related Documents:**
- Feature Specification: [specification.md](specification.md)
- Architecture Design: [architecture.md](architecture.md)
- Rendering Options: [rendering-options.md](rendering-options.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Attributes with ≥3 common prefix components are grouped | TC-01, TC-02 | Unit/Integration |
| Array-indexed attributes are grouped into dedicated sections | TC-03, TC-04 | Unit/Integration |
| Non-array prefix groups are rendered as separate sections | TC-05 | Unit/Integration |
| "Longest common prefix wins" rule prevents nested sections | TC-06 | Unit |
| Array rendering uses matrix table for simple arrays (≤8 props) | TC-07 | Unit/Integration |
| Array rendering falls back to per-item tables for complex arrays (>8 props) | TC-08 | Unit/Integration |
| Update operations show full groups if any item changed | TC-09 | Integration |
| Changes within groups are correctly highlighted | TC-10 | Integration |
| Sensitive value masking is preserved in groups | TC-11 | Integration |
| Grouping works for Create, Update, Delete, Replace | TC-12 | Integration |
| Ordering of attributes and sections is preserved | TC-13 | Unit |
| Threshold of 3 attributes is strictly enforced | TC-14 | Unit |
| Heterogeneous array schema triggers per-item fallback | TC-15 | Unit/Integration |
| Array property count boundary (8 vs 9) | TC-16 | Unit/Integration |
| Array item addition/removal in updates | TC-17, TC-18 | Integration |
| Null/Empty body handling | TC-19 | Unit |
| Deeply nested arrays (MVP boundary) | TC-20 | Unit/Integration |

## User Acceptance Scenarios

> **Purpose**: These scenarios validate the visual rendering of complex AzAPI resources in PR comments.

### Scenario 1: Complex App Service Config (Array Grouping)

**User Goal**: Review an App Service with many connection strings and app settings without scanning 50+ rows of flat paths.

**Test Artifact**: `artifacts/azapi-complex-demo.md`

**Expected Output**:
- `connectionStrings` is rendered as a separate section with an H6 heading: `###### connectionStrings Array`
- Since `connectionStrings` items typically have few properties (name, connectionString, type), they should render as a matrix table.
- `appSettings` is rendered as a separate section.

**Success Criteria**:
- [ ] No repetitive prefixes like `connectionStrings[0].` in the table rows.
- [ ] Clear separation between main configuration and arrays.

---

### Scenario 2: Nested Object Grouping (CORS Rules)

**User Goal**: Quickly see CORS configuration grouped together.

**Test Artifact**: `artifacts/azapi-nested-grouping-demo.md`

**Expected Output**:
- A section `###### cors` or `###### 🧩 cors` if it has an icon.
- Local property names like `allowedOrigins` and `supportCredentials` rendered without the `cors.` prefix.

**Success Criteria**:
- [ ] Grouping triggered only if ≥3 attributes share the prefix.

## Test Cases

### TC-01: Prefix Detection Logic

**Type:** Unit

**Description:**
Verify that the logic correctly identifies prefixes shared by at least 3 attributes.

**Preconditions:**
- Mocked leaf attributes list.

**Test Steps:**
1. Input list with 2 attributes sharing `foo.`.
2. Input list with 3 attributes sharing `foo.`.
3. Input list with 3 attributes sharing `foo.bar.` and `foo.`.

**Expected Result:**
1. No group identified for `foo.` (below threshold).
2. Group identified for `foo.`.
3. Group identified for `foo.bar.` (longest prefix wins).

---

### TC-02: Threshold Enforcement (Fixed at 3)

**Type:** Unit

**Description:**
Verify that the threshold for grouping is exactly 3 attributes.

**Test Steps:**
1. Input 2 attributes: `a.b`, `a.c`.
2. Input 3 attributes: `a.b`, `a.c`, `a.d`.

**Expected Result:**
1. Rendered in a single flat table.
2. Rendered with a grouping section for `a`.

---

### TC-03: Array Grouping into Dedicated Sections

**Type:** Integration

**Description:**
Verify that arrays meeting the threshold trigger an H6 section.

**Test Steps:**
1. Render a resource with `tags[0]`, `tags[1]`, `tags[2]`.
2. Verify H6 heading `###### tags Array` exists.

---

### TC-04: Non-Array Prefix Grouping

**Type:** Integration

**Description:**
Verify that non-array nested objects meeting the threshold trigger their own section.

**Test Steps:**
1. Render a resource with `properties.networkProfile.vnetId`, `properties.networkProfile.subnetId`, `properties.networkProfile.publicIpId`.
2. Verify H6 heading `###### properties.networkProfile` (or abbreviated path if applicable) exists.

---

### TC-05: Matrix Table for Simple Arrays

**Type:** Integration

**Description:**
Verify that arrays with few properties per item render in a compact matrix format.

**Test Steps:**
1. Render an array where each item has 4 properties.
2. Verify output is a single table with properties as columns and indices as rows.

---

### TC-06: Per-item Table Fallback for Complex Arrays

**Type:** Integration

**Description:**
Verify that arrays with >8 properties per item or heterogeneous schemas fall back to separate tables.

**Test Steps:**
1. Render an array where each item has 10 properties.
2. Verify output includes multiple tables, one per item.

---

### TC-07: Update Operation Group Visibility

**Type:** Integration

**Description:**
Verify that an entire group (array or object) is rendered if at least one attribute within it changed.

**Test Steps:**
1. Render an update to `connectionStrings[1].value`.
2. Verify the entire `connectionStrings` section is rendered, including unchanged item `[0]`.

---

### TC-08: Change Highlighting within Groups

**Type:** Integration

**Description:**
Verify that specific changes are highlighted using the `Before | After` pattern within grouped tables.

**Test Steps:**
1. Render an update to `cors.maxAge`.
2. Verify the row for `maxAge` in the `cors` section shows the diff.

---

### TC-09: Sensitive Value Masking in Groups

**Type:** Integration

**Description:**
Verify that sensitive values remain masked when part of a group.

**Test Steps:**
1. Render a plan with a sensitive attribute `password` inside a group.
2. Verify value is rendered as `(sensitive value)`.

---

### TC-10: Resource Operation Support

**Type:** Integration

**Description:**
Verify that grouping works correctly for all Terraform resource actions (Create, Update, Delete, Replace).

**Test Steps:**
1. Render resources with Create, Update, Delete, and Replace actions.
2. Verify grouping logic applies consistently.

---

### TC-11: Ordering Persistence

**Type:** Unit

**Description:**
Verify that sections and rows follow the original plan order.

**Test Steps:**
1. Input attributes in order: `Z.1`, `Z.2`, `Z.3`, `A.1`, `A.2`, `A.3`.
2. Verify group `Z` appears before group `A`.

---

### TC-12: Threshold Boundary Enforcement

**Type:** Unit

**Description:**
Verify that exactly 3 attributes are required for grouping.

**Test Steps:**
1. Test with 2 attributes sharing a prefix.
2. Test with 3 attributes sharing a prefix.

**Expected Result:**
1. No group created (rendered in main table).
2. Group section created.

---

### TC-13: Heterogeneous Array Schema Fallback

**Type:** Unit/Integration

**Description:**
Verify that arrays where items have different property sets trigger the per-item table fallback.

**Test Steps:**
1. Create an array where item 0 has `propA` and item 1 has `propB`.
2. Verify rendering uses separate tables for each item instead of a matrix.

---

### TC-14: Array Property Count Boundary (8 vs 9)

**Type:** Unit/Integration

**Description:**
Verify the threshold where a compact matrix table switches to per-item tables.

**Test Steps:**
1. Render array item with 8 properties -> Matrix table.
2. Render array item with 9 properties -> Per-item tables.

---

### TC-15: Array Item Addition in Updates

**Type:** Integration

**Description:**
Verify rendering behavior when a new item is added to an existing array.

**Test Steps:**
1. Provide a plan where `connectionStrings[0]` exists in state and `connectionStrings[1]` is added.
2. Verify the section shows the added item.

---

### TC-16: Array Item Removal in Updates

**Type:** Integration

**Description:**
Verify rendering behavior when an item is removed from an array.

**Test Steps:**
1. Provide a plan where `connectionStrings[1]` is deleted in the plan.
2. Verify the section correctly represents the removal.

---

### TC-17: Null or Empty Body Handling

**Type:** Unit

**Description:**
Verify that null or empty `body` attributes do not cause errors in the grouping logic.

**Test Steps:**
1. Provide a resource with no `body` or empty body JSON.
2. Verify no groups are generated and no exception is thrown.

---

### TC-18: Deeply Nested Arrays (Outermost Grouping)

**Type:** Unit/Integration

**Description:**
Verify that only the outermost array triggers a section, while nested arrays remain flattened within.

**Test Steps:**
1. Input attribute `parent[0].child[0].prop`.
2. Verify section `###### parent Array` exists, but NOT `###### child Array`.

---

### TC-19: Special Characters in Paths

**Type:** Unit

**Description:**
Verify that property paths containing dots or brackets in keys (not just delimiters) are handled correctly.

**Test Steps:**
1. Source JSON: `{"foo.bar": 1, "foo.baz": 2, "foo.qux": 3}` (escaped keys).
2. Verify grouping logic correctly handles these escaped paths.

---

### TC-20: Performance with Large Bodies

**Type:** Performance

**Description:**
Verify that bodies with 500+ attributes are grouped and rendered in a reasonable time.

**Test Steps:**
1. Generate a large JSON body.
2. Measure time from plan parsing to markdown output.

**Expected Result:**
- Performance satisfies project constraints (< 1s per resource).

## Test Data Requirements

- `azapi-complex-body.json`: A large JSON body with multiple arrays and nested objects.
- `azapi-update-grouped.json`: An update plan with changes inside and outside groups.
- `azapi-heterogeneous-array.json`: Array items with different property sets for fallback testing.
- `azapi-matrix-boundary.json`: Arrays with exactly 8 and 9 properties per item.
- `azapi-nested-arrays.json`: Data with nested arrays to verify single-level grouping.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Array with 2 items | No grouping (threshold 3) | TC-12 |
| Nested Groups | Longest prefix wins | TC-01 |
| Mixed types in array | Fallback to per-item tables | TC-13 |
| Deeply nested arrays | Group at outermost array boundary | TC-18 |
| Exactly 8 properties | Matrix table format | TC-14 |
| Exactly 9 properties | Per-item table format | TC-14 |
| Escaped keys in JSON | Correct grouping/key extraction | TC-19 |
| Empty body | Graceful no-op | TC-17 |

## Non-Functional Tests

- **Performance**: Verify rendering of a plan with 100+ grouped attributes takes < 500ms (TC-20).
- **Markdownlint**: Run `markdownlint` on generated artifacts to ensure no violations.

## Definition of Done

- [ ] Unit tests for grouping logic pass.
- [ ] Integration tests with Scriban templates pass.
- [ ] Snapshot tests updated and verified for visual correctness.
- [ ] UAT scenarios validated in GitHub and Azure DevOps.
- [ ] No regression in sensitive value rendering.
- [ ] Performance within acceptable limits.
- [ ] Maintainer approval of test plan.
