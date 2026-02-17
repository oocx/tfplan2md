# Tasks: Output Display Enhancements

## Overview

This document breaks down Feature 086: Output Display Enhancements into actionable implementation tasks. The feature introduces two improvements to tfplan2md markdown output:

1. **Collapsible Debug Section**: Wrap debug information in a collapsed `<details>` block to reduce visual clutter
2. **No-Changes Summary**: Display "No changes" text instead of empty summary tables when plans have zero resource changes

**References:**
- Specification: `docs/features/086-output-display-enhancements/specification.md`
- Architecture: `docs/features/086-output-display-enhancements/architecture.md`
- Test Plan: `docs/features/086-output-display-enhancements/test-plan.md`

## Tasks

### Task 1: Implement Collapsible Debug Section

**Priority:** P0 (Critical)

**Description:**
Modify `DiagnosticContext.GenerateMarkdownSection()` to wrap the entire debug section in a `<details>` block with a `<summary>` tag containing the bug emoji and "Debug Information" text. The section should be collapsed by default (no `open` attribute).

**Acceptance Criteria:**
- [x] Debug section output starts with `<details>` tag (no `open` attribute)
- [x] Summary line is `<summary>🐛\u00A0Debug Information</summary>` (using non-breaking space U+00A0)
- [x] `<br>` tag appears immediately after `</summary>` for proper spacing
- [x] `## Debug Information` H2 heading is removed (replaced by summary tag)
- [x] All subsection headings (`### Principal Mapping`, `### Template Resolution`) are preserved
- [x] All diagnostic content is preserved exactly as before (principal mapping, template resolution, failed resolutions)
- [x] "No diagnostics collected." message appears inside `<details>` block when context is empty
- [x] Debug section output ends with `</details>` tag
- [x] Changes follow the style guide pattern for collapsible sections

**Dependencies:** None

**Implementation Notes:**
- File to modify: `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`
- Method: `GenerateMarkdownSection()` (currently around line 194)
- Reference the report style guide (`docs/report-style-guide.md`) for collapsible section patterns
- Use `StringBuilder` to construct the wrapped output
- Ensure proper newline handling around tags

**Testing:**
- Unit tests: TC-01 (empty diagnostics), TC-02 (with diagnostics), TC-03 (no `open` attribute), TC-04 (content preserved), TC-14 (non-breaking space)
- Integration test: TC-10 (end-to-end rendering with debug)

---

### Task 2: Implement No-Changes Summary Logic

**Priority:** P0 (Critical)

**Description:**
Modify the `_summary.sbn` template to conditionally render either "No changes" text or the full summary table based on whether `summary.total == 0`. When there are zero changes, display a simple "No changes" message instead of an empty table.

**Acceptance Criteria:**
- [x] Template checks `if summary.total == 0` condition
- [x] When `summary.total == 0`, output is plain text: `No changes`
- [x] When `summary.total > 0`, output is the existing summary table with all columns and rows
- [x] `## Summary` heading remains in both cases
- [x] No regression: plans with changes continue to show full summary table
- [x] Template logic is clean and readable

**Dependencies:** None

**Implementation Notes:**
- File to modify: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_summary.sbn`
- Use Scriban conditional syntax: `{{ if summary.total == 0 }}`
- The `summary.total` property already exists in `SummaryModel` and contains the sum of all action counts
- Preserve existing table structure for non-zero cases
- Ensure proper indentation and spacing

**Testing:**
- Unit tests: TC-05 (no-changes summary format), TC-07 (with changes - regression)
- Integration test: TC-06 (no-changes plan), TC-11 (summary-only template)

---

### Task 3: Conditionally Render Resource Changes Section

**Priority:** P0 (Critical)

**Description:**
Modify the `default.sbn` template to wrap the Resource Changes section in a conditional block that only renders when `module_changes.size > 0`. This prevents displaying a redundant "No changes" message in the Resource Changes section when the Summary already shows "No changes".

**Acceptance Criteria:**
- [x] Resource Changes section wrapped in `{{ if module_changes.size > 0 }}...{{ end }}`
- [x] `## Resource Changes` heading only appears when there are changes
- [x] When `module_changes.size == 0`, Resource Changes section is completely omitted
- [x] When `module_changes.size > 0`, Resource Changes section renders exactly as before
- [x] No regression: existing Resource Changes rendering remains unchanged for plans with changes
- [x] Code Analysis Summary section (if present) still renders independently of Resource Changes

**Dependencies:** Task 2 (for consistent no-changes detection)

**Implementation Notes:**
- File to modify: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`
- Wrap the entire `## Resource Changes` section and its content in the conditional
- `module_changes.size == 0` aligns with `summary.total == 0` for no-changes plans
- Ensure other sections (Code Analysis Summary, etc.) remain unaffected
- Verify conditional placement doesn't introduce extra blank lines

**Testing:**
- Integration tests: TC-06 (no-changes plan omits section), TC-07 (with changes - regression)
- Edge cases: TC-12 (no debug, no changes), TC-13 (debug enabled, no changes)

---

### Task 4: Create Unit Tests for Debug Section

**Priority:** P1 (High)

**Description:**
Implement comprehensive unit tests for the collapsible debug section feature, covering empty diagnostics, diagnostics with content, structure validation, and content preservation.

**Acceptance Criteria:**
- [ ] Test TC-01: Empty DiagnosticContext renders "No diagnostics collected." inside `<details>` block
- [ ] Test TC-02: DiagnosticContext with content renders all sections inside `<details>` block
- [ ] Test TC-03: `<details>` tag does NOT contain `open` attribute
- [ ] Test TC-04: All diagnostic content (principal mapping, template resolution, failed resolutions) is preserved
- [ ] Test TC-14: Summary tag uses non-breaking space (U+00A0) between emoji and text
- [ ] All tests pass with 100% coverage of `GenerateMarkdownSection()` method
- [ ] Tests are added to existing test file: `src/tests/Oocx.TfPlan2Md.TUnit/Diagnostics/DiagnosticContextTests.cs`

**Dependencies:** Task 1 (implementation must be complete)

**Implementation Notes:**
- Use TUnit testing framework (existing in project)
- Test file location: `src/tests/Oocx.TfPlan2Md.TUnit/Diagnostics/DiagnosticContextTests.cs`
- Create in-memory `DiagnosticContext` instances with controlled state
- Use `Contains()` and `StartsWith()`/`EndsWith()` assertions for tag validation
- Verify Unicode character U+00A0 explicitly (non-breaking space)
- Test both empty and populated diagnostic contexts

**Testing:**
- Run tests with: `scripts/test-with-timeout.sh -- dotnet test --treenode-filter /*/*/DiagnosticContextTests/*`
- Verify all new tests pass
- Confirm no regression in existing DiagnosticContext tests

---

### Task 5: Create Unit Tests for No-Changes Summary

**Priority:** P1 (High)

**Description:**
Implement unit tests for the no-changes summary feature, verifying that "No changes" text appears instead of empty tables when `summary.total == 0`, and full tables render for plans with changes.

**Acceptance Criteria:**
- [ ] Test TC-05: Plan with zero changes shows "No changes" in Summary section (not table)
- [ ] Test TC-06: Plan with zero changes does NOT render `## Resource Changes` section
- [ ] Test TC-07: Plan with changes shows full summary table with all columns/rows
- [ ] Test TC-11: Summary-only template shows "No changes" for no-changes plan
- [ ] Tests verify both Summary section content and absence/presence of Resource Changes section
- [ ] All tests pass and use existing test data (`no-op-plan.json`, `azurerm-azuredevops-plan.json`)

**Dependencies:** Task 2, Task 3 (implementations must be complete)

**Implementation Notes:**
- Test file location: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererTests.cs` (or new file if needed)
- Use existing test data: `TestData/no-op-plan.json`, `TestData/azurerm-azuredevops-plan.json`
- Parse Terraform plan JSON, build ReportModel, render with templates
- Assert on presence/absence of strings: "No changes", "## Resource Changes", summary table headers
- Test both `default.sbn` and `summary.sbn` templates

**Testing:**
- Run tests with: `scripts/test-with-timeout.sh -- dotnet test --treenode-filter /*/*/MarkdownRendererTests/*`
- Verify all new tests pass
- Confirm no regression in existing renderer tests

---

### Task 6: Create Integration Tests

**Priority:** P1 (High)

**Description:**
Implement end-to-end integration tests that verify the complete rendering pipeline with both features enabled, covering combinations of debug/no-debug and changes/no-changes scenarios.

**Acceptance Criteria:**
- [ ] Test TC-10: Debug section appended to full report renders as collapsible
- [ ] Test TC-12: No debug + no changes shows minimal output (Summary with "No changes" only)
- [ ] Test TC-13: Debug enabled + no changes shows "No changes" in Summary and collapsible debug section
- [ ] Integration tests exercise full pipeline: parse → build model → render → append debug
- [ ] Tests verify proper section separation and ordering
- [ ] All tests pass with existing test data

**Dependencies:** Task 1, Task 2, Task 3 (all implementations complete)

**Implementation Notes:**
- Test file location: `src/tests/Oocx.TfPlan2Md.TUnit/EndToEnd/DebugOutputIntegrationTests.cs` (or new file)
- Use existing test data: `TestData/no-op-plan.json`, `TestData/azurerm-azuredevops-plan.json`, `TestData/principal-mapping.json`
- Simulate full rendering flow as done in `ProgramEntry.cs`
- Verify section ordering: Summary → Code Analysis → Resource Changes (if any) → Debug (if enabled)
- Test edge cases: no changes + no debug, no changes + debug, changes + debug

**Testing:**
- Run tests with: `scripts/test-with-timeout.sh -- dotnet test --treenode-filter /*/*/DebugOutputIntegrationTests/*`
- Verify all integration tests pass
- Confirm end-to-end rendering works correctly

---

### Task 7: Update Existing Tests (Regression)

**Priority:** P1 (High)

**Description:**
Update existing tests for debug output and no-changes scenarios to expect the new formats (collapsible debug section, "No changes" text), ensuring backward compatibility for plans with changes and maintaining test coverage.

**Acceptance Criteria:**
- [ ] Test TC-08: All existing debug output tests updated to expect `<details>` wrapper
- [ ] Test TC-09: All existing no-changes tests updated to expect "No changes" text and no Resource Changes section
- [ ] Snapshot tests updated if they exist (use `update-test-snapshots` skill if needed)
- [ ] All updated tests pass
- [ ] Test coverage remains at or above current levels
- [ ] No regression: tests for plans with changes remain unchanged (except debug wrapper)

**Dependencies:** Task 1, Task 2, Task 3, Task 4, Task 5 (all implementations and new tests complete)

**Implementation Notes:**
- Review existing test files:
  - `src/tests/Oocx.TfPlan2Md.TUnit/Diagnostics/DiagnosticContextTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/EndToEnd/` (any existing tests)
- Update string assertions to look for `<summary>🐛 Debug Information</summary>` instead of `## Debug Information`
- Update assertions to NOT expect `## Resource Changes` for no-changes plans
- Update assertions to expect "No changes" text in Summary for zero-changes plans
- If snapshot tests exist, use the `update-test-snapshots` skill to regenerate them

**Testing:**
- Run all tests: `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`
- Verify 100% pass rate
- Confirm coverage metrics are maintained

---

### Task 8: Update Report Style Guide

**Priority:** P2 (Medium)

**Description:**
Update the `docs/report-style-guide.md` to document the new collapsible debug section structure and no-changes summary format, providing guidance for future template development and maintenance.

**Acceptance Criteria:**
- [ ] Style guide documents collapsible debug section pattern (structure, tags, spacing)
- [ ] Style guide documents no-changes summary format ("No changes" text vs table)
- [ ] Style guide explains when Resource Changes section is omitted (zero changes)
- [ ] Examples provided for both debug section and no-changes summary
- [ ] Documentation is consistent with existing style guide structure and formatting
- [ ] References to these features are added to appropriate sections (collapsible sections, summary formatting)

**Dependencies:** Task 1, Task 2, Task 3 (implementations complete to accurately document)

**Implementation Notes:**
- File to modify: `docs/report-style-guide.md`
- Add section for collapsible debug section under existing collapsible sections guidance
- Add section for no-changes summary format under summary formatting guidance
- Include markdown examples showing `<details>` structure for debug
- Include examples of "No changes" vs summary table
- Explain relationship between Summary and Resource Changes sections in no-changes scenarios
- Cross-reference architecture document for technical details

**Testing:**
- Review for clarity and completeness
- Verify examples are valid markdown and render correctly
- Confirm alignment with existing style guide patterns

---

### Task 9: Update Changelog and Documentation

**Priority:** P3 (Low)

**Description:**
Update the project changelog to document the new features and any user-facing changes. Ensure all user-impacting changes are clearly communicated.

**Acceptance Criteria:**
- [ ] `CHANGELOG.md` updated with new features under appropriate version section
- [ ] Collapsible debug section feature documented
- [ ] No-changes summary format documented
- [ ] User benefits clearly explained (cleaner output, less clutter)
- [ ] Breaking changes noted if any (e.g., test output format changes)
- [ ] Changelog follows existing format and style

**Dependencies:** Task 1, Task 2, Task 3 (implementations complete)

**Implementation Notes:**
- File to modify: `CHANGELOG.md`
- Add entries under "## [Unreleased]" or next version section
- Format: `### Added` or `### Changed` sections
- Mention both features: collapsible debug section and no-changes summary
- Note that debug output visual format changed (for users who parse output)
- Reference GitHub issue or PR if applicable

**Testing:**
- Review for clarity
- Verify changelog follows existing conventions
- Confirm all user-visible changes are documented

---

## Implementation Order

Recommended sequence for implementation:

1. **Task 1** (Collapsible Debug Section) - Foundational change, self-contained in `DiagnosticContext.cs`
2. **Task 2** (No-Changes Summary Logic) - Template change for `_summary.sbn`
3. **Task 3** (Conditionally Render Resource Changes) - Template change for `default.sbn`, depends on Task 2 for consistency
4. **Task 4** (Unit Tests for Debug Section) - Test implementation for Task 1
5. **Task 5** (Unit Tests for No-Changes Summary) - Test implementation for Tasks 2 & 3
6. **Task 6** (Integration Tests) - Tests require all implementations (Tasks 1-3) to be complete
7. **Task 7** (Update Existing Tests) - Regression testing, requires all new implementations and tests
8. **Task 8** (Update Report Style Guide) - Documentation, can be done after implementations are verified
9. **Task 9** (Update Changelog) - Final step, documents all completed work

**Rationale:**
- Core implementations first (Tasks 1-3): Enable the features
- Unit tests follow immediately (Tasks 4-5): Verify individual components
- Integration tests (Task 6): Verify combined behavior
- Regression testing (Task 7): Ensure no breaks in existing functionality
- Documentation last (Tasks 8-9): Document verified, working implementations

**Parallel Work Opportunities:**
- Task 1 and Task 2 can be implemented in parallel (different files, no dependencies)
- Task 4 and Task 5 can be implemented in parallel (different test scopes)

---

## Testing Strategy

### Unit Tests (Tasks 4, 5)
- Focus: Individual components (`DiagnosticContext.GenerateMarkdownSection()`, template rendering)
- Test data: In-memory contexts, existing JSON test data
- Coverage: All code paths, edge cases (empty diagnostics, zero changes)

### Integration Tests (Task 6)
- Focus: Full rendering pipeline with both features
- Test data: Existing test plans (`no-op-plan.json`, `azurerm-azuredevops-plan.json`, `principal-mapping.json`)
- Coverage: Combinations of debug/no-debug and changes/no-changes

### Regression Tests (Task 7)
- Focus: Existing functionality not broken
- Test data: All existing test data
- Coverage: Ensure plans with changes render correctly, debug content preserved

### Test Execution
- Command: `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`
- Expected: 100% pass rate across all test categories
- Snapshot updates: Use `update-test-snapshots` skill if needed (after intentional output changes)

---

## UAT Preparation

After all tasks are complete and tests pass:

1. **UAT Test Plan**: `docs/features/086-output-display-enhancements/uat-test-plan.md` (already created by Quality Engineer)
2. **UAT Agent**: Use `@uat-tester-coding-agent` to execute UAT plan
3. **UAT Focus**:
   - Visual verification of collapsible debug section in GitHub PR
   - Visual verification of collapsible debug section in Azure DevOps PR
   - Visual verification of "No changes" summary in both platforms
   - Confirm `<details>` tags render correctly (expand/collapse functionality)

---

## Open Questions

None. All implementation details are clearly defined in the architecture and specification documents. Any questions during implementation should be raised with the Maintainer for clarification.

---

## Risk Mitigation

### Risk: Breaking Existing Tests
- **Mitigation**: Task 7 explicitly updates regression tests; comprehensive test coverage before and after
- **Validation**: Run full test suite (`dotnet test`) after each task

### Risk: Markdown Compatibility Issues
- **Mitigation**: Both GitHub and Azure DevOps support `<details>` tags (already used for resource sections)
- **Validation**: UAT visual verification in both platforms

### Risk: Template Conditional Logic Errors
- **Mitigation**: Thorough unit tests (Tasks 5), simple conditional logic (`summary.total == 0`)
- **Validation**: Integration tests (Task 6) verify template composition

### Risk: Debug Section Structure Changes Breaking Tools
- **Mitigation**: Content is preserved exactly; only presentation wrapper changes
- **Validation**: Content preservation tests (TC-04) verify diagnostic output unchanged

---

## Success Metrics

Implementation is successful when:

- [ ] All 9 tasks completed and acceptance criteria met
- [ ] All unit tests pass (Tasks 4, 5)
- [ ] All integration tests pass (Task 6)
- [ ] All regression tests pass (Task 7)
- [ ] Full test suite passes: `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`
- [ ] Documentation updated (Tasks 8, 9)
- [ ] UAT plan ready for execution
- [ ] No regressions: plans with changes render identically (except debug wrapper)
- [ ] Feature meets all acceptance criteria in specification

---

## Definition of Done

For this planning phase:
- [x] Tasks document created with clear breakdown
- [x] All tasks have defined acceptance criteria
- [x] Tasks are prioritized (P0, P1, P2, P3)
- [x] Implementation order is logical and accounts for dependencies
- [x] Testing strategy is comprehensive
- [x] Risk mitigation identified
- [ ] Maintainer has reviewed and approved the tasks
- [ ] Tasks document committed to feature branch (after approval)
- [ ] Developer agent recommended as next step (after approval)
