# Tasks: Improved AzAPI Attribute Grouping and Array Rendering

## Overview

This feature enhances the readability of AzAPI resource JSON body attributes in markdown reports. It implements intelligent grouping for attributes sharing common prefixes (≥3) and improves array rendering by introducing a hybrid approach (compact matrix tables vs. per-item tables).

Refer to:
- [specification.md](specification.md)
- [architecture.md](architecture.md)
- [test-plan.md](test-plan.md)

## Tasks

### Task 1: Advanced Prefix Grouping Logic

**Priority:** High

**Description:**
Implement the core logic for detecting qualifying groups in the flattened JSON body.

**Acceptance Criteria:**
- [ ] Implement a grouping algorithm that identifies prefixes shared by at least 3 leaf attributes.
- [ ] Implement the "longest common prefix wins" rule to avoid redundant nested sections.
- [ ] Distinguish between array groups (path ending in `[n]`) and non-array prefix groups.
- [ ] Ensure that groups are detected consistently for both simple (create/delete) and complex (update/replace) attribute lists.
- [ ] Preserve the original plan ordering when identifying groups (sections should be ordered by their first appearing attribute).
- [ ] Unit tests for the grouping logic covering prefix detection, threshold, and "longest common prefix wins".

---

### Task 2: Array Rendering Strategy Selection Logic

**Priority:** High

**Description:**
Implement the logic to select the most appropriate rendering format for arrays based on their complexity and homogeneity.

**Acceptance Criteria:**
- [ ] Implement a check for array homogeneity (items should have similar sets of properties).
- [ ] Implement the complexity threshold: use matrix table if each item has ≤8 properties; otherwise, fall back to per-item tables.
- [ ] Handle heterogeneous arrays by falling back to per-item tables.
- [ ] Matrix table logic must correctly handle missing properties in some items (fill with null/empty indicator).
- [ ] Unit tests for array rendering strategy selection (matrix vs. per-item).

---

### Task 3: Body Render Model & Scriban Helper Integration

**Priority:** Medium

**Description:**
Define a structured view model for the AzAPI body and update Scriban helpers to transform the flat attribute list into this model.

**Acceptance Criteria:**
- [ ] Define `BodyRenderModel`, `SectionModel` (Array or Object group), and `RowModel`.
- [ ] Update `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Grouping.cs` to return the new model.
- [ ] Ensure sensitive values are correctly handled within the new model structure.
- [ ] Support both create/delete (simple row) and update (before/after row) formats in the model.

---

### Task 4: Scriban Template Implementation for Grouping

**Priority:** Medium

**Description:**
Update the Scriban templates to render the `BodyRenderModel`.

**Acceptance Criteria:**
- [ ] Update `azapi_resource.scriban` to iterate over sections in the `BodyRenderModel`.
- [ ] Implement rendering for H6 headers: `###### <path> Array` for arrays and `###### <path>` for object groups.
- [ ] Implement Matrix Table rendering (rows as indices, columns as properties).
- [ ] Implement Per-Item Table rendering (one table per index).
- [ ] Implement Standard Table rendering (for ungrouped attributes).
- [ ] Ensure output is markdownlint-friendly.

---

### Task 5: Update and Diff Support

**Priority:** Medium

**Description:**
Refine the rendering logic specifically for update and replace operations to ensure diffs are clear within groups.

**Acceptance Criteria:**
- [ ] Implement "show full group if any item changed" logic for update operations.
- [ ] Ensure the matrix table clearly displays before/after values for changed cells.
- [ ] Verify that changed rows in per-item tables use the `Property | Before | After` format.
- [ ] Integration tests for update operations with changed attributes inside groups.

---

### Task 6: Test Automation and Snapshots

**Priority:** High

**Description:**
Exhaustively test the new rendering logic and update snapshots to reflect the improved output.

**Acceptance Criteria:**
- [ ] Implement all test cases defined in `test-plan.md`.
- [ ] Implement UAT scenarios defined in `uat-test-plan.md`.
- [ ] Verify that existing AzAPI snapshots are either preserved (if below threshold) or updated intentionally.
- [ ] Verify that sensitive values are never leaked in the new grouped output.
- [ ] Run `scripts/update-test-snapshots.sh` and verify changes.

## Implementation Order

1. **Task 1 & 2** (Foundational Logic): Implement the algorithms for grouping and strategy selection.
2. **Task 3** (Data Model): Connect the logic to a structured model that Scriban can use.
3. **Task 4** (Template): Implement the visual rendering in Scriban.
4. **Task 5** (Update Operations): Fine-tune the logic for diffs.
5. **Task 6** (Verification): Final testing and snapshot updates.

## Open Questions

- None at this stage. Logic decisions for MVP are resolved in the specification.
