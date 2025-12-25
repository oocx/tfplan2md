# Tasks: Consistent Value Formatting

## Overview

This document outlines the tasks required to implement the "Consistent Value Formatting" feature. The goal is to improve readability by code-formatting actual data values while keeping labels and attribute names as plain text, and providing enhanced diff formatting for small values.

Reference: [Specification](specification.md), [Architecture](architecture.md), [Test Plan](test-plan.md)

## Tasks

### Task 1: Enhance `format_diff` Helper and Registration

**Priority:** High

**Description:**
Update the `format_diff` helper to support styled diffs (inline and standard) and update the registration logic to capture the global configuration.

**Acceptance Criteria:**
- [x] `ScribanHelpers.FormatDiff` signature updated to `FormatDiff(string? before, string? after, string format)`.
- [x] `FormatDiff` implements table-compatible `inline-diff` (HTML with `<br>`).
- [x] `FormatDiff` implements table-compatible `standard-diff` (text with `<br>`).
- [x] `ScribanHelpers.RegisterHelpers` updated to accept `LargeValueFormat`.
- [x] `format_diff` registered as a closure: `(b, a) => FormatDiff(b, a, formatString)`.
- [x] Unit tests for `FormatDiff` (TC-05, TC-06) pass.

**Dependencies:** None

---

### Task 2: Update `MarkdownRenderer` to Thread Configuration

**Priority:** High

**Description:**
Update `MarkdownRenderer` to pass the `LargeValueFormat` from the `ReportModel` to the helper registration.

**Acceptance Criteria:**
- [x] `MarkdownRenderer.Render` passes `model.LargeValueFormat` to `RegisterHelpers`.
- [x] `RenderResourceChange` and `RenderResourceWithTemplate` updated to accept and pass `LargeValueFormat`.
- [x] Integration test for configuration propagation (TC-07) passes.

**Dependencies:** Task 1

---

### Task 3: Update Core Templates (`default.sbn`, `role_assignment.sbn`)

**Priority:** Medium

**Description:**
Reverse backtick formatting in attribute tables and refine role assignment summaries.

**Acceptance Criteria:**
- [x] `default.sbn`: Attribute names are plain text, values are code-formatted.
- [x] `role_assignment.sbn`: Attribute names are plain text, values are code-formatted.
- [x] `role_assignment.sbn`: Summary lines only code-format data values (TC-08).
- [x] Unit tests for templates (TC-01, TC-02) pass.

**Dependencies:** Task 2

---

### Task 4: Update Resource-Specific Templates (Firewall, NSG)

**Priority:** Medium

**Description:**
Update firewall and NSG templates to use consistent code formatting for data values in headers and tables.

**Acceptance Criteria:**
- [x] `firewall_network_rule_collection.sbn`: Headers and rule tables use code formatting for data (TC-03).
- [x] `network_security_group.sbn`: Headers use code formatting for names, rule tables use code formatting for data (TC-04).
- [x] `format_diff` calls in these templates now produce styled output automatically.
- [x] Unit tests for templates (TC-03, TC-04) pass.

**Dependencies:** Task 2

---

### Task 5: Final Verification and Documentation Update

**Priority:** Low

**Description:**
Perform final verification of the generated reports and update any relevant documentation.

**Acceptance Criteria:**
- [x] Manual verification of Scenario 1 (Default Report) and Scenario 2 (Firewall Diffs).
- [x] All existing tests pass (regression check).
- [x] Documentation examples in `docs/` reflect the new formatting.

**Dependencies:** Task 3, Task 4

## Implementation Order

1. **Task 1 & 2** - Foundational work to enable styled diffs and configuration propagation.
2. **Task 3** - Core formatting changes affecting most reports.
3. **Task 4** - Resource-specific formatting and diff verification.
4. **Task 5** - Final polish and verification.

## Open Questions

None.

