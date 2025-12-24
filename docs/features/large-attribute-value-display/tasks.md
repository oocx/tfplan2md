# Tasks: Large Attribute Value Display

## Overview

This document outlines the tasks required to implement the "Large Attribute Value Display" feature, which provides improved rendering for multi-line and long attribute values in Terraform plan reports.

Reference:
- Specification: [specification.md](specification.md)
- Architecture: [architecture.md](architecture.md)
- Test Plan: [test-plan.md](test-plan.md)

## Tasks

### Task 1: CLI and Configuration Updates

**Priority:** High

**Description:**
Add the new `--large-value-format` CLI option and update the internal models to carry this preference.

**Status:** Done

**Acceptance Criteria:**
- [x] `CliOptions` includes `LargeValueFormat` enum (values: `InlineDiff`, `StandardDiff`).
- [x] `CliParser` correctly parses `--large-value-format` (case-insensitive).
- [x] Default value for `LargeValueFormat` is `InlineDiff`.
- [x] `ReportModel` includes `LargeValueFormat` property.
- [x] `ReportModelBuilder` populates `LargeValueFormat` from CLI options.

**Dependencies:** None

---

### Task 2: Large Value Detection Logic

**Priority:** High

**Description:**
Implement the logic to identify which attributes should be treated as "large".

**Acceptance Criteria:**
- [ ] New helper `is_large_value(value)` in `ScribanHelpers.cs`.
- [ ] Returns `true` if value contains `\n`, `\r`, or `\r\n`.
- [ ] Returns `true` if value length > 100 characters.
- [ ] Returns `false` for short single-line values and null/empty values.
- [ ] Unit tests (TC-01, TC-02, TC-03) pass.

**Dependencies:** Task 1

---

### Task 3: Standard Diff Implementation

**Priority:** Medium

**Description:**
Implement the cross-platform compatible `standard-diff` formatting.

**Acceptance Criteria:**
- [ ] New helper `format_large_value(before, after, format)` in `ScribanHelpers.cs`.
- [ ] For `standard-diff`, generates a markdown code block with `diff` syntax highlighting.
- [ ] Correctly handles Create (before is null) and Delete (after is null) operations.
- [ ] Unit tests (TC-04, TC-05, TC-06) pass.

**Dependencies:** Task 2

---

### Task 4: Inline Diff Implementation (Line Level)

**Priority:** Medium

**Description:**
Implement the Azure DevOps-optimized `inline-diff` formatting at the line level, including the "Complete Replacement" logic.

**Acceptance Criteria:**
- [ ] `format_large_value` supports `inline-diff`.
- [ ] Generates HTML `<pre>` block with specified inline styles (background colors, left borders).
- [ ] Includes `color: #24292e` for dark mode compatibility.
- [ ] **Complete Replacement**: If no common lines exist, switches to separate "Before:" and "After:" code blocks.
- [ ] Unit tests (TC-07, TC-08) pass for line-level changes.

**Dependencies:** Task 3

---

### Task 5: Inline Diff Implementation (Character Level)

**Priority:** Medium

**Description:**
Enhance the `inline-diff` with character-level highlighting for changed lines.

**Acceptance Criteria:**
- [ ] Within changed lines in `inline-diff` mode, specific changed characters are wrapped in `<span>` with darker background colors (`#ffc0c0` for deletions, `#acf2bd` for additions).
- [ ] Correctly handles escaping of HTML special characters (`<`, `>`, `&`) within the diff.
- [ ] Character-level highlighting is precise (uses LCS or similar algorithm).

**Dependencies:** Task 4

---

### Task 6: Summary Line Logic

**Priority:** Low

**Description:**
Implement the logic to generate the summary string for the collapsible section.

**Acceptance Criteria:**
- [ ] New helper `large_attributes_summary(attributes_list, before_obj, after_obj)` in `ScribanHelpers.cs`.
- [ ] Correctly calculates total lines and changed lines for each attribute.
- [ ] Format matches: `Large values: attr1 (X lines, Y changed), ...`
- [ ] Unit test (TC-09) passes.

**Dependencies:** Task 2

---

### Task 7: Template Integration

**Priority:** High

**Description:**
Update the `default.sbn` template to use the new helpers and implement the hybrid layout.

**Acceptance Criteria:**
- [ ] `default.sbn` separates attributes into "small" and "large" groups.
- [ ] Small attributes are rendered in the existing table.
- [ ] Large attributes are rendered in a `<details>` section below the table.
- [ ] The `<summary>` tag uses the output from `large_attributes_summary`.
- [ ] Integration test (TC-10) passes.

**Dependencies:** Task 3, Task 4, Task 6

---

### Task 8: Documentation and Examples

**Priority:** Low

**Description:**
Update the project documentation and examples to reflect the new feature.

**Acceptance Criteria:**
- [ ] `README.md` updated with the new `--large-value-format` option.
- [ ] A new example in `examples/` demonstrating large value display.
- [ ] All documentation markdown files pass linting.

**Dependencies:** Task 7

## Implementation Order

1. **Task 1 (CLI)**: Foundation for the feature.
2. **Task 2 (Detection)**: Required to decide which attributes go where.
3. **Task 7 (Template Integration)**: Can be started early with a placeholder for the diff logic to verify the layout.
4. **Task 3 (Standard Diff)**: Easiest diff format to implement.
5. **Task 6 (Summary)**: Simple logic to enhance the template.
6. **Task 4 & 5 (Inline Diff)**: Most complex part, can be done iteratively.
7. **Task 8 (Docs)**: Final polish.

## Open Questions

None.
