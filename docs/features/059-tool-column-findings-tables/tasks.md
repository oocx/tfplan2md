# Tasks: Tool Column in Findings Tables

## Overview

This feature adds a "Tool" column to all code analysis findings tables to display the name of the security/quality tool that produced each finding (e.g., "Checkov", "tfsec", "Trivy"). This is a template-only change - no C# code modifications are required since the `ToolName` property already exists in `CodeAnalysisFindingModel` and is populated from SARIF files.

**Feature Reference:**
- Architecture: `docs/features/059-tool-column-findings-tables/architecture.md`
- Test Plan: `docs/features/059-tool-column-findings-tables/test-plan.md`
- UAT Test Plan: `docs/features/059-tool-column-findings-tables/uat-test-plan.md`
- Branch: `feature/059-tool-column-findings-tables`

## Tasks

### Task 1: Add Unit Tests for Tool Column

**Priority:** High

**Description:**
Create comprehensive unit tests in `MarkdownRendererCodeAnalysisTests.cs` to verify that the Tool column displays correctly in all three findings table types (per-resource, module, and unmatched), and handles null/empty tool names gracefully.

**Acceptance Criteria:**
- [ ] Test `Render_SecurityFindingsTable_IncludesToolColumn()` verifies Tool column appears between Severity and Attribute columns in per-resource findings table
- [ ] Test verifies table header is `| Severity | Tool | Attribute | Finding | Remediation |`
- [ ] Test `Render_ModuleFindingsTable_IncludesToolColumn()` verifies Tool column in module findings table with header `| Severity | Tool | Finding | Remediation |`
- [ ] Test `Render_UnmatchedFindingsTable_IncludesToolColumn()` verifies Tool column in unmatched findings table
- [ ] Test `Render_FindingsTable_HandlesNullToolName()` creates finding with `ToolName = null` and verifies Tool column displays "-"
- [ ] Test `Render_FindingsTable_HandlesEmptyToolName()` creates finding with `ToolName = ""` and verifies Tool column displays "-"
- [ ] Test `Render_FindingsTable_HandlesMultipleTools()` creates findings from different tools (Checkov, tfsec, Trivy) and verifies each shows correct tool name
- [ ] Test `Render_FindingsTable_HandlesSpecialCharsInToolName()` verifies tool names with hyphens, underscores, dots render correctly
- [ ] All new tests pass (tests will fail initially until templates are updated)

**Dependencies:** None (tests are created first following test-first approach)

**Notes:**
- Add tests to `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererCodeAnalysisTests.cs`
- Use existing helper methods: `CreateFinding()`, `BuildInput()`
- Set `ToolName` property directly on `CodeAnalysisFinding` objects
- Tests will initially fail - this is expected (templates not yet updated)
- Reference test plan TC-01 through TC-07 for detailed test case specifications

**Estimated Complexity:** Medium

---

### Task 2: Update Existing Test Assertions

**Priority:** High

**Description:**
Update existing test assertions in `MarkdownRendererCodeAnalysisTests.cs` that check for table headers to include the new "Tool" column. This prevents false negatives when snapshots are regenerated.

**Acceptance Criteria:**
- [ ] Update line 74 assertion from `"| Severity | Attribute | Finding | Remediation |"` to `"| Severity | Tool | Attribute | Finding | Remediation |"`
- [ ] Update line 157 assertion from `"| Severity | Finding | Remediation |"` to `"| Severity | Tool | Finding | Remediation |"`
- [ ] Search for all hardcoded header strings in the test file and update to include "Tool" column
- [ ] Verify separator line checks include the Tool column separator `| -------- |`
- [ ] All existing tests compile without errors (may fail until templates updated)

**Dependencies:** None (can be done in parallel with Task 1)

**Notes:**
- File: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererCodeAnalysisTests.cs`
- Line numbers are approximate - use search to locate exact assertions
- Tests checking table structure need column count updates
- Tests will fail until templates are updated (expected)

**Estimated Complexity:** Simple

---

### Task 3: Modify Per-Resource Findings Table Template

**Priority:** High

**Description:**
Update the `_code_analysis_findings.sbn` template to add the Tool column between Severity and Attribute columns in the per-resource Security & Quality findings table.

**Acceptance Criteria:**
- [ ] Table header line changed to: `| Severity | Tool | Attribute | Finding | Remediation |`
- [ ] Separator line updated to: `| -------- | ---- | --------- | ------- | ----------- |`
- [ ] Tool column added to data row after severity column: `| {{ finding.severity_icon }} {{ finding.severity }} | {{ if finding.tool_name }}{{ finding.tool_name }}{{ else }}-{{ end }} | ...`
- [ ] Tool column positioned immediately after Severity, before Attribute
- [ ] Template compiles without errors
- [ ] Null/empty tool names render as "-"

**Dependencies:** Task 1 (tests should exist to validate changes)

**Notes:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_findings.sbn`
- Use Scriban null-handling pattern: `{{ if finding.tool_name }}{{ finding.tool_name }}{{ else }}-{{ end }}`
- The variable `finding.tool_name` is already mapped by `AotScriptObjectMapper.cs` (line 342)
- Current table header is on line 9, data row on line 16 (approximate)
- Follow the architecture.md Implementation Guidance section

**Estimated Complexity:** Simple

---

### Task 4: Modify Other Findings Table Template

**Priority:** High

**Description:**
Update the `_code_analysis_other_findings.sbn` template to add the Tool column to both the module findings table and unmatched findings table.

**Acceptance Criteria:**
- [ ] Module findings table header (line ~7) changed to: `| Severity | Tool | Finding | Remediation |`
- [ ] Module findings separator line updated to: `| -------- | ---- | ------- | ----------- |`
- [ ] Module findings data row (line ~14) includes Tool column after severity
- [ ] Unmatched findings table header (line ~21) changed to: `| Severity | Tool | Finding | Remediation |`
- [ ] Unmatched findings separator line updated to: `| -------- | ---- | ------- | ----------- |`
- [ ] Unmatched findings data row (line ~28) includes Tool column after severity
- [ ] Template compiles without errors
- [ ] Both tables use same null-handling pattern: `{{ if finding.tool_name }}{{ finding.tool_name }}{{ else }}-{{ end }}`

**Dependencies:** Task 1 (tests should exist to validate changes)

**Notes:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_other_findings.sbn`
- This template has TWO tables that need updating (module findings and unmatched findings)
- Line numbers are approximate - verify by viewing the file
- Follow same pattern as Task 3 for consistency

**Estimated Complexity:** Simple

---

### Task 5: Run Tests and Verify Initial Failures

**Priority:** High

**Description:**
Run the full test suite to verify that new tests fail as expected (templates not yet updated) and identify all snapshot tests that need regeneration.

**Acceptance Criteria:**
- [ ] Command `dotnet test --solution src/tfplan2md.slnx` executes successfully
- [ ] New tests from Task 1 fail with expected error messages (table header mismatch)
- [ ] Existing snapshot tests fail due to missing Tool column
- [ ] List of failing snapshot tests is captured for reference
- [ ] No compilation errors or unexpected test failures
- [ ] Test execution completes within timeout limits

**Dependencies:** Task 1, Task 2 (tests must be written)

**Notes:**
- Use command: `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx --output Detailed`
- Expected failures are OK at this stage - we're validating test coverage
- Capture output to identify which snapshots need regeneration
- This validates that tests correctly detect the missing Tool column

**Estimated Complexity:** Simple

---

### Task 6: Regenerate Snapshot Tests

**Priority:** High

**Description:**
Use the `update-test-snapshots` skill to regenerate all snapshot files after template modifications, ensuring they reflect the new Tool column in findings tables.

**Acceptance Criteria:**
- [ ] `update-test-snapshots` skill executed successfully
- [ ] All snapshot files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/` updated with Tool column
- [ ] Manually reviewed 2-3 snapshot files to verify Tool column appears correctly
- [ ] Verified snapshot table headers show: `| Severity | Tool | Attribute/Finding | ...`
- [ ] Verified snapshot data rows show tool names or "-" in Tool column
- [ ] Verified table structure remains valid markdown
- [ ] No snapshots show empty cells or malformed tables

**Dependencies:** Task 3, Task 4 (templates must be updated first)

**Notes:**
- Use the project's `update-test-snapshots` skill
- Manual spot-check is important to catch any rendering issues
- Focus review on snapshots containing code analysis findings tables
- Look for snapshots with filenames containing "CodeAnalysis" or "Sarif"
- Verify both table types (per-resource and other findings) are updated

**Estimated Complexity:** Simple

---

### Task 7: Run Full Test Suite and Verify All Pass

**Priority:** High

**Description:**
Run the complete test suite to verify that all tests (new, updated, and snapshot) pass with the new Tool column implementation.

**Acceptance Criteria:**
- [ ] Command `dotnet test --solution src/tfplan2md.slnx` completes with exit code 0
- [ ] All 8 new unit tests from Task 1 pass
- [ ] All updated test assertions from Task 2 pass
- [ ] All snapshot tests pass with regenerated snapshots
- [ ] No test failures, compilation errors, or warnings
- [ ] Test execution completes within timeout limits
- [ ] Test output shows expected test count (370+ tests)

**Dependencies:** Task 6 (snapshots must be regenerated)

**Notes:**
- Use command: `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx --output Detailed`
- This is the validation that implementation is complete and correct
- All tests must pass before proceeding to UAT
- If any tests fail, investigate and fix before continuing

**Estimated Complexity:** Simple

---

### Task 8: Generate Demo Artifacts for UAT

**Priority:** Medium

**Description:**
Generate comprehensive demo markdown artifacts that demonstrate the Tool column in action with real SARIF files from multiple tools, preparing for UAT validation.

**Acceptance Criteria:**
- [ ] `generate-demo-artifacts` skill executed successfully
- [ ] Artifacts generated in `artifacts/` directory
- [ ] Artifacts include code analysis findings from Checkov, tfsec, and Trivy
- [ ] Tool column visible in all findings tables in generated artifacts
- [ ] Tool names display correctly (Checkov, tfsec, Trivy)
- [ ] Tables render with proper markdown structure
- [ ] Both per-resource and Other Findings sections show Tool column

**Dependencies:** Task 7 (all tests must pass)

**Notes:**
- Use the project's `generate-demo-artifacts` skill
- This generates standardized demo files used for UAT
- Artifacts will be used by UAT Tester to create test PRs
- Verify artifacts visually before handing off to UAT

**Estimated Complexity:** Simple

---

### Task 9: Create Feature-Specific UAT Artifact

**Priority:** Medium

**Description:**
Generate a focused UAT artifact specifically for testing the Tool column feature, using multiple SARIF files to demonstrate tool name differentiation.

**Acceptance Criteria:**
- [ ] Artifact created at `artifacts/tool-column-uat.md`
- [ ] Artifact generated using `examples/comprehensive-demo.tfplan.json`
- [ ] Multiple SARIF files included: Checkov, tfsec, and Trivy
- [ ] Command used: `tfplan2md examples/comprehensive-demo.tfplan.json --sarif <paths> > artifacts/tool-column-uat.md`
- [ ] Artifact contains Security & Quality findings table with Tool column
- [ ] Artifact contains Other Findings section with Tool column
- [ ] Tool names (Checkov, tfsec, Trivy) clearly visible in Tool column
- [ ] Table structure is valid markdown

**Dependencies:** Task 7 (all tests must pass)

**Notes:**
- This is a focused artifact for UAT testing, separate from comprehensive demos
- Reference UAT test plan for exact command and paths
- Use existing SARIF files from `src/tests/Oocx.TfPlan2Md.TUnit/TestData/code-analysis/`
- This artifact demonstrates the value of the Tool column when multiple scanners are used

**Estimated Complexity:** Simple

---

## Implementation Order

The tasks are designed to follow a test-first approach and minimize rework:

1. **Task 1** - Write unit tests first (defines expected behavior)
2. **Task 2** - Update existing test assertions (prepares for validation)
3. **Task 5** - Run tests to verify they fail appropriately (confirms test coverage)
4. **Task 3** - Implement template changes for per-resource findings table
5. **Task 4** - Implement template changes for other findings tables
6. **Task 6** - Regenerate snapshots (updates expectations)
7. **Task 7** - Verify all tests pass (confirms implementation correctness)
8. **Task 8** - Generate comprehensive demo artifacts (prepares for UAT)
9. **Task 9** - Generate feature-specific UAT artifact (focused UAT testing)

**Rationale:**
- Tests first (Tasks 1-2) ensures we know what "done" looks like
- Running tests before implementation (Task 5) validates test coverage
- Templates next (Tasks 3-4) implements the feature
- Snapshots after templates (Task 6) updates expectations
- Full validation (Task 7) confirms everything works
- UAT preparation last (Tasks 8-9) readies artifacts for final validation

## Open Questions

None - all requirements are clearly defined in the architecture and test plan documents.

## Definition of Done

Implementation is complete when:

- [ ] All 8 new unit tests pass (Task 1)
- [ ] All existing test assertions updated (Task 2)
- [ ] Both template files modified correctly (Tasks 3-4)
- [ ] All snapshot tests pass with regenerated snapshots (Task 6)
- [ ] Full test suite passes (370+ tests) (Task 7)
- [ ] Demo artifacts generated successfully (Task 8)
- [ ] Feature-specific UAT artifact created (Task 9)
- [ ] No compilation errors, warnings, or test failures
- [ ] Code committed to feature branch with descriptive commit message

**Next Steps After Implementation:**
1. Hand off to UAT Tester for visual validation in GitHub and Azure DevOps
2. Technical Writer updates documentation to show Tool column in examples
3. Release Manager includes feature in next release with release notes noting breaking change for custom template users
