# Test Plan: Tool Column in Findings Tables

## Overview

This test plan covers the addition of a "Tool" column to all code analysis findings tables in tfplan2md. The Tool column will be positioned between the "Severity" and "Attribute/Finding" columns and will display the tool name (e.g., "Checkov", "Trivy") or "-" when the tool name is null or empty.

**Feature Reference:**
- Specification: (to be created)
- Architecture: `docs/features/039-tool-column-findings-tables/architecture.md`
- Branch: `feature/059-tool-column-findings-tables`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type | Priority |
|---------------------|--------------|-----------|----------|
| Tool column appears between Severity and Attribute/Finding columns | TC-01, TC-02, TC-03 | Unit | P0 |
| Tool name displays correctly from SARIF | TC-01, TC-02, TC-03, TC-04 | Unit | P0 |
| Null/empty tool names render as "-" | TC-05, TC-06 | Unit | P0 |
| Table structure remains valid markdown | TC-01, TC-02, TC-03 | Unit | P0 |
| All three findings tables updated | TC-01, TC-02, TC-03 | Unit | P0 |
| Existing tests pass with updated snapshots | TC-10 | Snapshot | P0 |
| Tool names with special characters render correctly | TC-07 | Unit | P1 |
| Very long tool names don't break layout | TC-08 | Unit | P1 |
| Multiple different tools in one report | TC-04 | Unit | P1 |
| Tool column aligns properly in rendered markdown | TC-09 | Integration | P1 |

## Test Cases

### TC-01: Security & Quality Findings Table - Tool Column Display

**Type:** Unit

**Priority:** P0

**Description:**
Verify that the per-resource Security & Quality findings table (template `_code_analysis_findings.sbn`) includes a Tool column between Severity and Attribute columns, and displays the tool name correctly.

**Preconditions:**
- Template `_code_analysis_findings.sbn` has been modified to include Tool column
- Test uses a CodeAnalysisFinding with ToolName = "Checkov"

**Test Steps:**
1. Create a minimal terraform plan with a resource (e.g., `null_resource.test`)
2. Create a code analysis finding with:
   - `ToolName = "Checkov"`
   - Resource location matching the plan
   - Severity = Critical
   - Attribute path = "triggers.endpoint"
3. Build report model with the finding
4. Render markdown using MarkdownRenderer
5. Parse the rendered markdown to locate the findings table

**Expected Result:**
- Table header is: `| Severity | Tool | Attribute | Finding | Remediation |`
- Table separator line includes: `| -------- | ---- | --------- | ------- | ----------- |`
- First data row includes: `| 🚨 Critical | Checkov | ...`
- Tool name appears between severity and attribute columns
- Table structure is valid markdown

**Test Data:**
- Minimal plan: `TestData/minimal-plan.json`
- Tool name: "Checkov"
- Inline finding creation in test code

**Verification:**
```csharp
markdown.Should().Contain("| Severity | Tool | Attribute | Finding | Remediation |");
markdown.Should().Contain("| 🚨 Critical | Checkov | `triggers.endpoint` |");
```

---

### TC-02: Module Findings Table - Tool Column Display

**Type:** Unit

**Priority:** P0

**Description:**
Verify that the module-level findings table in `_code_analysis_other_findings.sbn` includes a Tool column between Severity and Finding columns.

**Preconditions:**
- Template `_code_analysis_other_findings.sbn` has been modified to include Tool column
- Test uses a CodeAnalysisFinding with module-level location and ToolName = "tfsec"

**Test Steps:**
1. Create a minimal terraform plan
2. Create a module-level finding with:
   - `ToolName = "tfsec"`
   - Location = "module.network"
   - Severity = High
3. Build report model with the finding
4. Render markdown using MarkdownRenderer
5. Locate the "Other Findings" section and module findings table

**Expected Result:**
- Section header `## Other Findings` is present
- Table header is: `| Severity | Tool | Finding | Remediation |`
- Table separator includes: `| -------- | ---- | ------- | ----------- |`
- First data row includes: `| ⚠️ High | tfsec |`
- Tool name appears between severity and finding columns

**Test Data:**
- Minimal plan: `TestData/minimal-plan.json`
- Module location: "module.network"
- Tool name: "tfsec"

**Verification:**
```csharp
markdown.Should().Contain("## Other Findings");
markdown.Should().Contain("| Severity | Tool | Finding | Remediation |");
markdown.Should().Contain("| ⚠️ High | tfsec |");
```

---

### TC-03: Unmatched Findings Table - Tool Column Display

**Type:** Unit

**Priority:** P0

**Description:**
Verify that the unmatched findings table in `_code_analysis_other_findings.sbn` includes a Tool column between Severity and Finding columns.

**Preconditions:**
- Template `_code_analysis_other_findings.sbn` has been modified to include Tool column
- Test uses an unmatched CodeAnalysisFinding with ToolName = "Trivy"

**Test Steps:**
1. Create a minimal terraform plan
2. Create an unmatched finding with:
   - `ToolName = "Trivy"`
   - Empty Locations array (no matching resource)
   - Severity = Medium
   - Message = "Orphaned security finding"
3. Build report model with the finding
4. Render markdown using MarkdownRenderer
5. Locate the "Unmatched Findings" section and table

**Expected Result:**
- Section header `### Unmatched Findings` is present
- Table header is: `| Severity | Tool | Finding | Remediation |`
- Table separator includes: `| -------- | ---- | ------- | ----------- |`
- First data row includes: `| ⚠️ Medium | Trivy | Orphaned security finding |`
- Tool name appears between severity and finding columns

**Test Data:**
- Minimal plan: `TestData/minimal-plan.json`
- Tool name: "Trivy"
- Unmatched finding (empty locations)

**Verification:**
```csharp
markdown.Should().Contain("### Unmatched Findings");
markdown.Should().Contain("| Severity | Tool | Finding | Remediation |");
markdown.Should().Contain("| ⚠️ Medium | Trivy | Orphaned security finding |");
```

---

### TC-04: Multiple Tools in Single Report

**Type:** Unit

**Priority:** P1

**Description:**
Verify that when a report contains findings from multiple different tools, each finding displays its correct tool name in the Tool column.

**Preconditions:**
- Templates have been modified to include Tool column
- Test creates findings from 3 different tools: Checkov, tfsec, Trivy

**Test Steps:**
1. Create a minimal terraform plan with multiple resources
2. Create findings from multiple tools:
   - Finding 1: ToolName = "Checkov", resource-level
   - Finding 2: ToolName = "tfsec", module-level
   - Finding 3: ToolName = "Trivy", unmatched
3. Build report model with all findings
4. Render markdown using MarkdownRenderer
5. Verify each finding shows the correct tool name

**Expected Result:**
- Security & Quality table row shows "Checkov" in Tool column
- Module findings table row shows "tfsec" in Tool column
- Unmatched findings table row shows "Trivy" in Tool column
- Each tool name appears only in rows for that tool's findings

**Test Data:**
- Minimal plan: `TestData/minimal-plan.json`
- Three findings with different tool names

**Verification:**
```csharp
markdown.Should().Contain("| 🚨 Critical | Checkov |");
markdown.Should().Contain("| ⚠️ High | tfsec |");
markdown.Should().Contain("| ⚠️ Medium | Trivy |");
```

---

### TC-05: Null Tool Name Handling

**Type:** Unit

**Priority:** P0

**Description:**
Verify that when a finding has a null ToolName property, the Tool column displays "-" instead of an empty cell or error.

**Preconditions:**
- Templates use null-handling pattern: `{{ if finding.tool_name }}{{ finding.tool_name }}{{ else }}-{{ end }}`
- Test creates a finding with ToolName = null

**Test Steps:**
1. Create a minimal terraform plan
2. Create a finding with:
   - `ToolName = null`
   - Valid location, severity, message
3. Build report model with the finding
4. Render markdown using MarkdownRenderer
5. Locate the findings table and verify Tool column content

**Expected Result:**
- Tool column displays: `| - |` (hyphen/dash)
- Table structure remains valid (no empty cells)
- No rendering errors or exceptions
- Markdown parses correctly in GitHub/Azure DevOps

**Test Data:**
- Minimal plan: `TestData/minimal-plan.json`
- Finding with ToolName = null

**Verification:**
```csharp
var finding = CreateFinding("null_resource.test", "http://rule", 9.5);
finding.ToolName.Should().BeNull();
markdown.Should().Contain("| 🚨 Critical | - | ");
```

---

### TC-06: Empty String Tool Name Handling

**Type:** Unit

**Priority:** P0

**Description:**
Verify that when a finding has an empty string ToolName property, the Tool column displays "-".

**Preconditions:**
- Templates use null-handling pattern that also catches empty strings
- Test creates a finding with ToolName = ""

**Test Steps:**
1. Create a minimal terraform plan
2. Create a finding with:
   - `ToolName = ""`
   - Valid location, severity, message
3. Build report model with the finding
4. Render markdown using MarkdownRenderer
5. Verify Tool column shows "-"

**Expected Result:**
- Tool column displays: `| - |` (hyphen/dash)
- Behavior identical to null ToolName case
- Table structure remains valid

**Test Data:**
- Minimal plan: `TestData/minimal-plan.json`
- Finding with ToolName = ""

**Verification:**
```csharp
markdown.Should().Contain("| 🚨 Critical | - | ");
```

---

### TC-07: Special Characters in Tool Name

**Type:** Unit

**Priority:** P1

**Description:**
Verify that tool names containing special characters (e.g., hyphens, underscores, dots) render correctly without breaking markdown table structure.

**Preconditions:**
- Templates have been modified to include Tool column
- Test uses tool names with various special characters

**Test Steps:**
1. Create findings with tool names containing special characters:
   - "tool-name" (hyphen)
   - "tool_name" (underscore)
   - "tool.name" (dot)
   - "Tool-Name 2.0" (mixed)
2. Render markdown for each case
3. Verify tool names display correctly
4. Verify markdown table structure remains valid

**Expected Result:**
- All special characters display correctly in Tool column
- No markdown escaping issues (e.g., no extra backslashes)
- Table pipes `|` remain unaffected
- Markdown table structure is valid

**Test Data:**
- Inline test data with various tool names
- Tool names: "tool-name", "tool_name", "tool.name", "Tool-Name 2.0"

**Verification:**
```csharp
markdown.Should().Contain("| tool-name |");
markdown.Should().Contain("| tool_name |");
markdown.Should().Contain("| tool.name |");
markdown.Should().Contain("| Tool-Name 2.0 |");
```

---

### TC-08: Long Tool Name Handling

**Type:** Unit

**Priority:** P1

**Description:**
Verify that very long tool names (e.g., 50+ characters) don't break the table layout or cause rendering issues.

**Preconditions:**
- Templates have been modified to include Tool column
- Test uses a tool name exceeding typical length

**Test Steps:**
1. Create a finding with:
   - `ToolName = "VeryLongSecurityScannerToolNameThatExceedsTypicalLength"`
   - Valid location, severity, message
2. Render markdown using MarkdownRenderer
3. Verify the tool name displays completely
4. Check markdown table validity
5. Visually verify rendering in GitHub/Azure DevOps (manual UAT)

**Expected Result:**
- Long tool name displays in full in the Tool column
- Table remains structurally valid markdown
- No truncation or overflow
- Markdown renderers (GitHub/Azure DevOps) wrap text appropriately

**Test Data:**
- Minimal plan: `TestData/minimal-plan.json`
- Tool name: "VeryLongSecurityScannerToolNameThatExceedsTypicalLength"

**Verification:**
```csharp
markdown.Should().Contain("| VeryLongSecurityScannerToolNameThatExceedsTypicalLength |");
```

**Note:** Visual verification during UAT phase is recommended to ensure acceptable wrapping behavior in GitHub and Azure DevOps PR views.

---

### TC-09: Table Column Alignment in Rendered Markdown

**Type:** Integration

**Priority:** P1

**Description:**
Verify that the Tool column aligns properly with other columns when rendered in GitHub and Azure DevOps markdown viewers.

**Preconditions:**
- Feature implementation complete
- Templates include Tool column
- UAT test plan exists

**Test Steps:**
1. Generate markdown report with code analysis findings
2. Create test PR in GitHub with markdown in PR description/comment
3. Create test PR in Azure DevOps with markdown in PR description
4. Visually inspect table rendering in both platforms
5. Verify column alignment and readability

**Expected Result:**
- Tool column aligns vertically with other columns
- Column headers align with data rows
- Table is readable and professional in appearance
- No visual artifacts or layout issues
- Column widths adjust reasonably for content

**Test Data:**
- Real SARIF files from Checkov, tfsec, Trivy
- Comprehensive demo markdown artifact

**Verification:**
- Manual visual inspection during UAT phase
- Screenshot comparison against baseline
- Maintainer approval of rendering quality

---

### TC-10: Snapshot Test Regeneration

**Type:** Snapshot

**Priority:** P0

**Description:**
Verify that all existing snapshot tests pass after regenerating snapshots to include the Tool column in findings tables.

**Preconditions:**
- All template modifications complete
- `update-test-snapshots` skill available

**Test Steps:**
1. Run full test suite: `dotnet test --solution src/tfplan2md.slnx`
2. Identify all failing snapshot tests
3. Use `update-test-snapshots` skill to regenerate snapshots
4. Re-run test suite to verify all tests pass
5. Manually review 2-3 regenerated snapshots for correctness

**Expected Result:**
- All tests pass after snapshot regeneration
- All snapshots containing findings tables now include Tool column
- Snapshot files show correct table structure:
  - `| Severity | Tool | Attribute | Finding | Remediation |` (per-resource)
  - `| Severity | Tool | Finding | Remediation |` (module/unmatched)
- Tool column populated correctly in snapshot data

**Test Data:**
- All existing test data in `TestData/` directory
- All existing snapshots in `TestData/Snapshots/`

**Verification:**
```bash
dotnet test --solution src/tfplan2md.slnx --output Detailed
# All tests should pass (exit code 0)
```

**Manual Verification:**
- Open 2-3 snapshot files
- Verify Tool column is present
- Verify tool names or "-" appear correctly

---

## Test Data Requirements

### Existing Test Data (Reuse)

The following existing test data will be used:
- `TestData/minimal-plan.json` - Minimal terraform plan for basic tests
- `TestData/code-analysis/checkov.sarif` - Real Checkov SARIF output
- `TestData/code-analysis/tfsec.sarif` - Real tfsec SARIF output
- `TestData/code-analysis/trivy.sarif` - Real Trivy SARIF output

### New Test Data (Create Inline)

The following test scenarios will use inline test data creation in test code:
- Findings with null ToolName
- Findings with empty string ToolName
- Findings with special characters in tool names
- Findings with very long tool names
- Mixed findings from multiple tools

No new SARIF files need to be created; test helpers like `CreateFinding()` will be used to create findings with specific ToolName values.

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Null ToolName | Display "-" | TC-05 |
| Empty string ToolName | Display "-" | TC-06 |
| Whitespace-only ToolName | Display as-is (template doesn't trim) | Covered by TC-06 |
| Tool name with hyphen | Display correctly | TC-07 |
| Tool name with underscore | Display correctly | TC-07 |
| Tool name with dot | Display correctly | TC-07 |
| Very long tool name (50+ chars) | Display in full, wrap if needed | TC-08 |
| Tool name with pipe character \| | Should be escaped by markdown escaping logic | TC-07 (add case) |
| Multiple tools in one report | Each shows correct name | TC-04 |
| No findings (empty table) | N/A - table not rendered | Existing test coverage |

---

## Non-Functional Tests

### Rendering Compatibility

**Objective:** Ensure Tool column renders correctly in GitHub and Azure DevOps markdown viewers.

**Approach:**
- UAT phase testing in real PR environments
- Test cases TC-09 covers this
- Visual inspection by Maintainer required

**Success Criteria:**
- Tables render with proper column alignment
- Tool names are readable
- No layout breaking or overflow issues
- Professional appearance maintained

### Performance Impact

**Objective:** Verify that adding the Tool column does not significantly impact markdown generation performance.

**Approach:**
- No specific test case required
- Template changes are minimal (no complex logic)
- Performance impact expected to be negligible

**Success Criteria:**
- Existing performance tests still pass
- No noticeable slowdown in markdown generation

---

## Test Execution Strategy

### Phase 1: Unit Tests (Developer)

1. **Create new test cases** in `MarkdownRendererCodeAnalysisTests.cs`:
   - `Render_SecurityFindingsTable_IncludesToolColumn()` (TC-01)
   - `Render_ModuleFindingsTable_IncludesToolColumn()` (TC-02)
   - `Render_UnmatchedFindingsTable_IncludesToolColumn()` (TC-03)
   - `Render_FindingsTable_HandlesNullToolName()` (TC-05)
   - `Render_FindingsTable_HandlesEmptyToolName()` (TC-06)
   - `Render_FindingsTable_HandlesMultipleTools()` (TC-04)
   - `Render_FindingsTable_HandlesSpecialCharsInToolName()` (TC-07)
   - `Render_FindingsTable_HandlesLongToolName()` (TC-08)

2. **Update existing test assertions** that check table structure:
   - Update line 74 assertion to include "Tool" in header
   - Update line 157 assertion to include "Tool" in header
   - Any other hardcoded header checks

3. **Run tests** to verify all pass:
   ```bash
   scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx --output Detailed
   ```

### Phase 2: Snapshot Regeneration (Developer)

1. **Run tests** to identify failing snapshots
2. **Use `update-test-snapshots` skill** to regenerate all snapshots
3. **Manually review** 2-3 snapshot files to verify Tool column appears correctly
4. **Re-run tests** to confirm all pass

### Phase 3: UAT Testing (UAT Tester)

1. **Generate UAT artifacts** using `generate-demo-artifacts` skill
2. **Create test PRs** in GitHub and Azure DevOps
3. **Verify rendering** of Tool column in both platforms (TC-09)
4. **Collect feedback** from Maintainer on visual appearance

---

## Test Automation

### Fully Automated Tests

The following tests are fully automated and run via `dotnet test`:
- TC-01 through TC-08 (unit tests)
- TC-10 (snapshot tests)

**Execution:**
```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

**No manual intervention required** for these tests.

### Manual Verification Required

The following tests require human judgment:
- TC-09: Visual inspection of table rendering in GitHub and Azure DevOps

**Execution:**
- UAT Tester agent creates test PRs
- Maintainer reviews rendering quality
- Approval/feedback provided via PR comments

---

## Regression Testing

### Scope

This feature modifies core template files that affect all code analysis findings. Regression testing must verify:
- No existing functionality is broken
- All table types still render correctly
- Code analysis summary section unaffected
- Tool names in summary still appear correctly
- Findings ordering (by severity) unchanged
- Remediation links still work

### Approach

- **Automated:** All existing tests must pass (covered by TC-10)
- **Manual:** Comprehensive demo artifact reviewed during UAT

### Exit Criteria

- All 370+ tests pass
- No visual regressions in UAT review
- Maintainer approval obtained

---

## Dependencies

### Test Infrastructure Dependencies

- TUnit 1.9.26 test framework
- AwesomeAssertions for fluent assertions
- `scripts/test-with-timeout.sh` for reliable test execution
- `update-test-snapshots` skill for snapshot regeneration

### Code Dependencies

- Templates: `_code_analysis_findings.sbn`, `_code_analysis_other_findings.sbn`
- Models: `CodeAnalysisFindingModel` (ToolName property already exists)
- Mapper: `AotScriptObjectMapper` (tool_name already mapped)

### No New Dependencies Required

This feature requires no new NuGet packages or external tools.

---

## Test Environment

### Local Development Environment

- .NET 9.0 SDK
- TUnit 1.9.26
- Test execution via `dotnet test` or `scripts/test-with-timeout.sh`

### CI/CD Environment

- GitHub Actions workflows
- Automated test execution on PR creation
- Snapshot validation

### UAT Environment

- GitHub repository (test PR)
- Azure DevOps repository (test PR)
- Real markdown rendering in PR comments/descriptions

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation | Test Coverage |
|------|--------|------------|------------|---------------|
| Table layout breaks in GitHub/Azure DevOps | High | Low | UAT testing in real PRs | TC-09 |
| Null tool names cause rendering errors | High | Low | Explicit null handling in templates | TC-05, TC-06 |
| Snapshot regeneration misses some files | Medium | Low | Manual review of 2-3 snapshots | TC-10 |
| Special characters break markdown | Medium | Low | Existing markdown escaping handles this | TC-07 |
| Long tool names cause overflow | Low | Low | Markdown viewers handle wrapping | TC-08 |
| Custom template users miss update | Low | High | Documentation and release notes (not tested) | N/A |

---

## Open Questions

None at this time. All testing requirements are clear from the architecture document.

---

## Success Criteria

The test plan is considered complete and successful when:

- ✅ All P0 test cases (TC-01 through TC-06, TC-10) pass
- ✅ All P1 test cases (TC-04, TC-07, TC-08, TC-09) pass
- ✅ All existing tests pass after snapshot regeneration
- ✅ UAT testing shows acceptable rendering in GitHub and Azure DevOps
- ✅ Maintainer approves visual appearance and functionality
- ✅ No regressions in existing code analysis features

---

## Next Steps

1. **Quality Engineer:** Deliver this test plan to Maintainer for review and approval
2. **Developer:** Implement template changes per architecture.md
3. **Developer:** Create new test cases per this test plan
4. **Developer:** Update existing test assertions
5. **Developer:** Regenerate snapshots using `update-test-snapshots` skill
6. **UAT Tester:** Execute UAT test plan (to be created) for visual verification
7. **Technical Writer:** Update documentation to show Tool column in examples
