# Test Plan: Resource Details Display Mode

## Overview

This test plan defines comprehensive test coverage for the `--details` CLI feature, which allows users to control whether resource details blocks (`<details>` HTML elements) are rendered as open or closed in the generated markdown report.

**Specification:** `docs/features/092-details-display-mode/specification.md`  
**Architecture:** `docs/features/092-details-display-mode/architecture.md`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| CLI accepts `--details` with valid values (closed, open, auto) | TC-01, TC-02, TC-03 | Unit |
| Invalid `--details` values show error and exit | TC-04, TC-05 | Unit |
| Closed mode renders all resources without `open` attribute | TC-06, TC-14 | Unit, Integration |
| Open mode renders all resources with `open` attribute | TC-07, TC-15 | Unit, Integration |
| Auto mode opens resources with findings only | TC-08, TC-16 | Unit, Integration |
| Auto mode handles merged child resources with findings | TC-09, TC-17 | Unit, Integration |
| Debug block always collapsed regardless of mode | TC-10, TC-18 | Unit, Integration |
| Default behavior equals `--details auto` | TC-11 | Unit |
| Helper function determines `open` attribute correctly | TC-06, TC-07, TC-08, TC-09, TC-10 | Unit |
| Template uses helper function (not hardcoded logic) | TC-19 | Integration |
| Integration tests verify HTML output per mode | TC-14, TC-15, TC-16, TC-17, TC-18 | Integration |

## User Acceptance Scenarios

> **Purpose**: For user-facing rendering changes, define scenarios for manual Maintainer review via Test PRs in GitHub and Azure DevOps. These help catch rendering bugs and validate real-world usage before merge.

### Scenario 1: Collapsed View for Large Plans

**User Goal**: DevOps engineer reviewing a large Terraform plan with 50+ resources wants all details collapsed by default to reduce cognitive load and manually expand only resources of interest.

**Test PR Context**:
- **GitHub**: Verify all resource `<details>` blocks are collapsed (no `open` attribute) in PR comments
- **Azure DevOps**: Verify all resource `<details>` blocks are collapsed in PR description

**Expected Output**:
- All resources show only their summary line (collapsed)
- User clicks to expand individual resources
- Debug block (if present) is collapsed
- No resources have the `open` attribute in HTML

**Success Criteria**:
- [x] All resources collapsed in GitHub markdown rendering
- [x] All resources collapsed in Azure DevOps markdown rendering
- [x] Clicking a resource expands it properly
- [x] Large reports are easier to navigate

**Feedback Opportunities**:
- Is the collapsed view easier to navigate for large plans?
- Does the summary line provide enough context before expanding?
- Are there any resources that should be expanded by default?

---

### Scenario 2: Expanded View for Quick Review

**User Goal**: Developer reviewing a small infrastructure change wants all resources expanded by default to see full details without clicking through each resource.

**Test PR Context**:
- **GitHub**: Verify all resource `<details>` blocks are expanded (have `open` attribute)
- **Azure DevOps**: Verify all resource `<details>` blocks are expanded

**Expected Output**:
- All resources show full details immediately (no clicking required)
- Full attribute tables, diffs, and metadata visible
- Debug block (if present) is still collapsed

**Success Criteria**:
- [x] All resources expanded in GitHub markdown
- [x] All resources expanded in Azure DevOps markdown
- [x] Debug block remains collapsed
- [x] Full details visible without interaction

**Feedback Opportunities**:
- Is the expanded view overwhelming for larger plans?
- Does this mode match expectations for "show everything" behavior?

---

### Scenario 3: Auto Mode - Focus on Security Findings

**User Goal**: Security engineer reviewing infrastructure changes with static code analysis results wants to immediately see resources with security/quality findings while keeping clean resources collapsed.

**Test PR Context**:
- **GitHub**: Verify resources with code analysis findings are expanded, others collapsed
- **Azure DevOps**: Verify selective expansion based on findings

**Expected Output**:
- Resources WITH findings: expanded with findings table visible
- Resources WITHOUT findings: collapsed
- Debug block: collapsed
- Merged parent resources with child findings: expanded

**Success Criteria**:
- [x] Resources with findings are expanded in GitHub
- [x] Resources with findings are expanded in Azure DevOps
- [x] Clean resources are collapsed
- [x] Findings are immediately visible without scrolling
- [x] Merged parent resources with child findings are expanded

**Feedback Opportunities**:
- Does auto mode correctly highlight security issues?
- Are there false positives (expanded resources without meaningful findings)?
- Should severity levels affect expansion behavior (future enhancement)?

---

### Scenario 4: Auto Mode Without Code Analysis

**User Goal**: User runs tfplan2md with `--details auto` but does not provide any code analysis files. The tool should behave gracefully (equivalent to `closed` mode).

**Test PR Context**:
- **GitHub**: Verify all resources collapsed when no SARIF files provided
- **Azure DevOps**: Verify same behavior

**Expected Output**:
- All resources collapsed (no findings, so nothing to highlight)
- No error messages about missing code analysis
- Debug block collapsed

**Success Criteria**:
- [x] All resources collapsed in both platforms
- [x] No errors or warnings about missing SARIF
- [x] Behavior equivalent to `--details closed`

**Feedback Opportunities**:
- Is the behavior clear when no code analysis is provided?
- Should there be a message indicating auto mode is active?

---

### Scenario 5: Debug Block Always Collapsed

**User Goal**: Developer uses `--debug` flag with different `--details` modes and expects debug information to always be collapsed (never auto-expanded).

**Test PR Context**:
- **GitHub**: Verify debug block collapsed regardless of `--details open`
- **Azure DevOps**: Verify same behavior

**Expected Output**:
- Debug `<details>` block has NO `open` attribute
- Debug info visible only after clicking
- Other resources respect `--details` mode

**Success Criteria**:
- [x] Debug block collapsed with `--details closed`
- [x] Debug block collapsed with `--details open`
- [x] Debug block collapsed with `--details auto`
- [x] User must click to see debug info

**Feedback Opportunities**:
- Is it clear that debug info requires manual expansion?
- Should debug info ever be auto-expanded?

## Test Cases

### TC-01: CLI Parses `--details closed`

**Type:** Unit

**Description:**
Verify that the CLI parser correctly accepts `--details closed` and sets `DetailsDisplayMode` to `Closed`.

**Preconditions:**
- None

**Test Steps:**
1. Create CLI arguments: `["--details", "closed", "plan.json"]`
2. Call `CliParser.Parse(args)`
3. Inspect `options.DetailsDisplayMode`

**Expected Result:**
- `options.DetailsDisplayMode` should be `DetailsDisplayMode.Closed`

**Test Data:**
- Inline arguments

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

**Test Method Name:**
`Parse_DetailsClosedFlag_SetsDetailsDisplayModeTolosed`

---

### TC-02: CLI Parses `--details open`

**Type:** Unit

**Description:**
Verify that the CLI parser correctly accepts `--details open` and sets `DetailsDisplayMode` to `Open`.

**Preconditions:**
- None

**Test Steps:**
1. Create CLI arguments: `["--details", "open", "plan.json"]`
2. Call `CliParser.Parse(args)`
3. Inspect `options.DetailsDisplayMode`

**Expected Result:**
- `options.DetailsDisplayMode` should be `DetailsDisplayMode.Open`

**Test Data:**
- Inline arguments

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

**Test Method Name:**
`Parse_DetailsOpenFlag_SetsDetailsDisplayModeToOpen`

---

### TC-03: CLI Parses `--details auto`

**Type:** Unit

**Description:**
Verify that the CLI parser correctly accepts `--details auto` and sets `DetailsDisplayMode` to `Auto`.

**Preconditions:**
- None

**Test Steps:**
1. Create CLI arguments: `["--details", "auto", "plan.json"]`
2. Call `CliParser.Parse(args)`
3. Inspect `options.DetailsDisplayMode`

**Expected Result:**
- `options.DetailsDisplayMode` should be `DetailsDisplayMode.Auto`

**Test Data:**
- Inline arguments

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

**Test Method Name:**
`Parse_DetailsAutoFlag_SetsDetailsDisplayModeToAuto`

---

### TC-04: CLI Rejects Invalid `--details` Value

**Type:** Unit

**Description:**
Verify that the CLI parser throws `CliParseException` with a clear error message when an invalid value is provided to `--details`.

**Preconditions:**
- None

**Test Steps:**
1. Create CLI arguments: `["--details", "invalid", "plan.json"]`
2. Call `CliParser.Parse(args)`
3. Expect exception to be thrown

**Expected Result:**
- `CliParseException` is thrown
- Exception message contains: "Invalid value for --details. Allowed values: closed, open, auto"

**Test Data:**
- Inline arguments

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

**Test Method Name:**
`Parse_DetailsInvalidValue_ThrowsCliParseException`

---

### TC-05: CLI Rejects `--details` Without Value

**Type:** Unit

**Description:**
Verify that the CLI parser throws `CliParseException` when `--details` is provided without a value argument.

**Preconditions:**
- None

**Test Steps:**
1. Create CLI arguments: `["--details"]`
2. Call `CliParser.Parse(args)`
3. Expect exception to be thrown

**Expected Result:**
- `CliParseException` is thrown
- Exception message contains: "--details requires a mode argument"

**Test Data:**
- Inline arguments

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

**Test Method Name:**
`Parse_DetailsWithoutValue_ThrowsCliParseException`

---

### TC-06: Helper Returns Empty String for Closed Mode

**Type:** Unit

**Description:**
Verify that `details_open_attr` helper function returns an empty string when mode is "closed", regardless of whether resource has findings.

**Preconditions:**
- Helper function `GetDetailsOpenAttribute` is registered in Scriban context

**Test Steps:**
1. Create a `ScriptObject` representing a resource change with code analysis findings
2. Call `details_open_attr(change)` with mode = "closed"
3. Verify result is empty string
4. Create a `ScriptObject` representing a resource change WITHOUT findings
5. Call `details_open_attr(change)` with mode = "closed"
6. Verify result is empty string

**Expected Result:**
- Returns `""` (empty string) for resources with findings
- Returns `""` (empty string) for resources without findings

**Test Data:**
- ScriptObject with `code_analysis_findings` array containing 1 finding
- ScriptObject with empty or missing `code_analysis_findings` array

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersDetailsDisplayTests.cs` (new file)

**Test Method Name:**
`DetailsOpenAttr_ClosedMode_ReturnsEmptyString`

---

### TC-07: Helper Returns " open" for Open Mode

**Type:** Unit

**Description:**
Verify that `details_open_attr` helper function returns " open" (with leading space) when mode is "open", regardless of whether resource has findings.

**Preconditions:**
- Helper function is registered in Scriban context

**Test Steps:**
1. Create a `ScriptObject` representing a resource change with code analysis findings
2. Call `details_open_attr(change)` with mode = "open"
3. Verify result is " open"
4. Create a `ScriptObject` representing a resource change WITHOUT findings
5. Call `details_open_attr(change)` with mode = "open"
6. Verify result is " open"

**Expected Result:**
- Returns `" open"` for resources with findings
- Returns `" open"` for resources without findings

**Test Data:**
- ScriptObject with `code_analysis_findings` array containing 1 finding
- ScriptObject with empty `code_analysis_findings` array

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersDetailsDisplayTests.cs`

**Test Method Name:**
`DetailsOpenAttr_OpenMode_ReturnsOpenAttribute`

---

### TC-08: Helper Returns " open" for Auto Mode with Findings

**Type:** Unit

**Description:**
Verify that `details_open_attr` helper function returns " open" when mode is "auto" and the resource has code analysis findings.

**Preconditions:**
- Helper function is registered in Scriban context

**Test Steps:**
1. Create a `ScriptObject` representing a resource change with `code_analysis_findings` array containing at least one finding
2. Call `details_open_attr(change)` with mode = "auto"
3. Verify result is " open"

**Expected Result:**
- Returns `" open"` for resources with findings in auto mode

**Test Data:**
- ScriptObject with `code_analysis_findings` array: `[{ "severity": "critical", "message": "Test finding" }]`

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersDetailsDisplayTests.cs`

**Test Method Name:**
`DetailsOpenAttr_AutoModeWithFindings_ReturnsOpenAttribute`

---

### TC-09: Helper Returns Empty String for Auto Mode Without Findings

**Type:** Unit

**Description:**
Verify that `details_open_attr` helper function returns an empty string when mode is "auto" and the resource has no code analysis findings.

**Preconditions:**
- Helper function is registered in Scriban context

**Test Steps:**
1. Create a `ScriptObject` representing a resource change with empty `code_analysis_findings` array
2. Call `details_open_attr(change)` with mode = "auto"
3. Verify result is empty string

**Expected Result:**
- Returns `""` for resources without findings in auto mode

**Test Data:**
- ScriptObject with empty `code_analysis_findings` array or missing field

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersDetailsDisplayTests.cs`

**Test Method Name:**
`DetailsOpenAttr_AutoModeWithoutFindings_ReturnsEmptyString`

---

### TC-10: Helper Returns Empty String for Unknown Mode

**Type:** Unit

**Description:**
Verify that `details_open_attr` helper function returns an empty string (defaults to closed) when an unknown mode is provided.

**Preconditions:**
- Helper function is registered in Scriban context

**Test Steps:**
1. Create a `ScriptObject` representing a resource change
2. Call `details_open_attr(change)` with mode = "unknown"
3. Verify result is empty string

**Expected Result:**
- Returns `""` for unknown modes (safe default)

**Test Data:**
- Any ScriptObject

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersDetailsDisplayTests.cs`

**Test Method Name:**
`DetailsOpenAttr_UnknownMode_ReturnsEmptyString`

---

### TC-11: Default CLI Behavior Uses Auto Mode

**Type:** Unit

**Description:**
Verify that when `--details` is NOT specified in CLI arguments, the default value is `DetailsDisplayMode.Auto` (preserving current behavior).

**Preconditions:**
- None

**Test Steps:**
1. Create CLI arguments without `--details` flag: `["plan.json"]`
2. Call `CliParser.Parse(args)`
3. Inspect `options.DetailsDisplayMode`

**Expected Result:**
- `options.DetailsDisplayMode` should be `DetailsDisplayMode.Auto` (default)

**Test Data:**
- Inline arguments

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

**Test Method Name:**
`Parse_NoDetailsFlag_DefaultsToAuto`

---

### TC-12: Helper Handles Merged Child Resources with Findings

**Type:** Unit

**Description:**
Verify that `details_open_attr` helper correctly identifies findings in merged child resources. When a parent resource has children merged into it (parent-child grouping) and any child has findings, the parent should be opened in auto mode.

**Preconditions:**
- Helper function is registered in Scriban context
- Understanding of how child findings are attached during parent-child merging

**Test Steps:**
1. Create a `ScriptObject` representing a parent resource with merged children
2. Ensure the parent's `code_analysis_findings` array includes findings from child resources (as per existing parent-child merging logic)
3. Call `details_open_attr(change)` with mode = "auto"
4. Verify result is " open"

**Expected Result:**
- Returns `" open"` when parent has child findings rolled up

**Test Data:**
- ScriptObject with `code_analysis_findings` array containing findings from merged child
- ScriptObject structure matching `ReportModelBuilder.ParentChildMerging.cs` output

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersDetailsDisplayTests.cs`

**Test Method Name:**
`DetailsOpenAttr_AutoModeWithChildFindings_ReturnsOpenAttribute`

**Note:**
According to the architecture document, findings are already rolled up to parent resources during the parent-child merging process in `ReportModelBuilder.ParentChildMerging.cs`. The helper function only needs to check the `code_analysis_findings` array on the resource, which already includes findings from merged children. This test verifies that behavior.

---

### TC-13: Case-Insensitive CLI Parsing

**Type:** Unit

**Description:**
Verify that `--details` values are case-insensitive (e.g., "Closed", "OPEN", "AuTo" all work correctly).

**Preconditions:**
- None

**Test Steps:**
1. Test with `["--details", "Closed"]` → expect `DetailsDisplayMode.Closed`
2. Test with `["--details", "OPEN"]` → expect `DetailsDisplayMode.Open`
3. Test with `["--details", "AuTo"]` → expect `DetailsDisplayMode.Auto`

**Expected Result:**
- All case variations parse correctly to their respective enum values

**Test Data:**
- Inline arguments with various cases

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

**Test Method Name:**
`Parse_DetailsCaseInsensitive_ParsesCorrectly`

---

### TC-14: Integration Test - Closed Mode Renders All Resources Collapsed

**Type:** Integration (Snapshot Test)

**Description:**
End-to-end test verifying that when `DetailsDisplayMode.Closed` is used, all resource `<details>` blocks in the generated markdown do NOT have the `open` attribute.

**Preconditions:**
- Test plan JSON file with multiple resources (at least 3)
- No code analysis files

**Test Steps:**
1. Parse test plan JSON using `TerraformPlanParser`
2. Build `ReportModel` with `detailsDisplayMode = DetailsDisplayMode.Closed`
3. Render markdown using `MarkdownRenderer`
4. Assert all `<details>` tags do NOT contain `open` attribute
5. Compare against snapshot file

**Expected Result:**
- All resource `<details>` blocks: `<details>` (no `open` attribute)
- Snapshot matches expected output

**Test Data:**
- `TestData/details-display-test-plan.json` (new file with 3+ resources)

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/DetailsDisplayModeSnapshotTests.cs` (new file)

**Test Method Name:**
`Render_DetailsClosedMode_AllResourcesCollapsed`

---

### TC-15: Integration Test - Open Mode Renders All Resources Expanded

**Type:** Integration (Snapshot Test)

**Description:**
End-to-end test verifying that when `DetailsDisplayMode.Open` is used, all resource `<details>` blocks in the generated markdown HAVE the `open` attribute.

**Preconditions:**
- Test plan JSON file with multiple resources

**Test Steps:**
1. Parse test plan JSON using `TerraformPlanParser`
2. Build `ReportModel` with `detailsDisplayMode = DetailsDisplayMode.Open`
3. Render markdown using `MarkdownRenderer`
4. Assert all resource `<details>` tags contain `open` attribute
5. Compare against snapshot file

**Expected Result:**
- All resource `<details>` blocks: `<details open>` (with `open` attribute)
- Snapshot matches expected output

**Test Data:**
- `TestData/details-display-test-plan.json`

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/DetailsDisplayModeSnapshotTests.cs`

**Test Method Name:**
`Render_DetailsOpenMode_AllResourcesExpanded`

---

### TC-16: Integration Test - Auto Mode Selectively Expands Resources

**Type:** Integration (Snapshot Test)

**Description:**
End-to-end test verifying that when `DetailsDisplayMode.Auto` is used with code analysis results, only resources with findings have the `open` attribute.

**Preconditions:**
- Test plan JSON file with at least 3 resources
- SARIF file with findings for 1-2 of those resources

**Test Steps:**
1. Parse test plan JSON using `TerraformPlanParser`
2. Load code analysis results from SARIF file
3. Build `ReportModel` with `detailsDisplayMode = DetailsDisplayMode.Auto` and `codeAnalysisInput`
4. Render markdown using `MarkdownRenderer`
5. Assert resources WITH findings have `<details open>`
6. Assert resources WITHOUT findings have `<details>` (no `open`)
7. Compare against snapshot file

**Expected Result:**
- Resources with findings: `<details open>`
- Resources without findings: `<details>`
- Snapshot matches expected output

**Test Data:**
- `TestData/details-display-test-plan.json`
- `TestData/details-display-findings.sarif` (new file with selective findings)

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/DetailsDisplayModeSnapshotTests.cs`

**Test Method Name:**
`Render_DetailsAutoModeWithFindings_SelectiveExpansion`

---

### TC-17: Integration Test - Auto Mode Without SARIF Behaves Like Closed

**Type:** Integration (Snapshot Test)

**Description:**
End-to-end test verifying that when `DetailsDisplayMode.Auto` is used WITHOUT code analysis results, all resources are collapsed (behaves like closed mode).

**Preconditions:**
- Test plan JSON file with multiple resources
- No SARIF files

**Test Steps:**
1. Parse test plan JSON using `TerraformPlanParser`
2. Build `ReportModel` with `detailsDisplayMode = DetailsDisplayMode.Auto` and `codeAnalysisInput = null`
3. Render markdown using `MarkdownRenderer`
4. Assert all `<details>` tags do NOT contain `open` attribute

**Expected Result:**
- All resource `<details>` blocks: `<details>` (no `open` attribute)
- Behavior identical to closed mode

**Test Data:**
- `TestData/details-display-test-plan.json`

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/DetailsDisplayModeSnapshotTests.cs`

**Test Method Name:**
`Render_DetailsAutoModeWithoutSarif_AllResourcesCollapsed`

---

### TC-18: Integration Test - Debug Block Always Collapsed

**Type:** Integration

**Description:**
End-to-end test verifying that the debug details block is ALWAYS rendered without the `open` attribute, regardless of `DetailsDisplayMode` setting.

**Preconditions:**
- Test plan JSON file
- Debug mode enabled (diagnostics context populated)

**Test Steps:**
1. For each mode (Closed, Open, Auto):
   - Parse test plan JSON
   - Build `ReportModel` with the specific `detailsDisplayMode`
   - Enable debug/diagnostic context
   - Render markdown
   - Assert debug `<details>` block does NOT have `open` attribute

**Expected Result:**
- Debug block always renders as `<details>` (no `open`) regardless of mode
- Other resource blocks respect the mode setting

**Test Data:**
- `TestData/details-display-test-plan.json`
- Diagnostic context with debug information

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/DetailsDisplayModeSnapshotTests.cs`

**Test Method Name:**
`Render_DebugBlock_AlwaysCollapsedRegardlessOfMode`

---

### TC-19: Template Uses Helper Function (Not Hardcoded Logic)

**Type:** Integration

**Description:**
Verify that the `_resource.sbn` template uses the `details_open_attr()` helper function instead of hardcoded conditional logic like `{{ if change.code_analysis_findings.size > 0 }} open{{ end }}`.

**Preconditions:**
- Template file `_resource.sbn` exists

**Test Steps:**
1. Read `_resource.sbn` template content
2. Assert it does NOT contain the pattern `{{ if change.code_analysis_findings`
3. Assert it DOES contain the pattern `{{ details_open_attr`
4. Verify the pattern is used in the `<details>` tag opening

**Expected Result:**
- Template contains: `<details{{ details_open_attr(change) }}`
- Template does NOT contain hardcoded `{{ if change.code_analysis_findings.size > 0 }} open{{ end }}`

**Test Data:**
- Template file: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn`

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/TemplateArchitectureTests.cs` (add to existing file)

**Test Method Name:**
`ResourceTemplate_UsesDetailsOpenAttrHelper`

---

### TC-20: Merged Parent Resource with Child Findings Opens in Auto Mode

**Type:** Integration

**Description:**
End-to-end test verifying that when using parent-child grouping, if a parent resource has merged children and any child has code analysis findings, the parent resource is expanded in auto mode.

**Preconditions:**
- Test plan JSON with parent-child relationship (e.g., `azurerm_key_vault` parent with `azurerm_key_vault_secret` children)
- SARIF file with findings on a child resource only
- Parent-child merging enabled

**Test Steps:**
1. Parse test plan JSON with parent-child relationships
2. Load SARIF with findings for a child resource (not parent)
3. Build `ReportModel` with parent-child merging and `detailsDisplayMode = DetailsDisplayMode.Auto`
4. Render markdown
5. Assert parent resource `<details>` block has `open` attribute (because child has findings)

**Expected Result:**
- Parent resource has `<details open>` due to child findings
- Child findings are visible in the merged parent block

**Test Data:**
- Test plan JSON with parent-child structure
- SARIF with findings for child resource address

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/DetailsDisplayModeSnapshotTests.cs`

**Test Method Name:**
`Render_AutoModeWithChildFindings_ParentExpanded`

---

## Test Data Requirements

### New Test Data Files

1. **`TestData/details-display-test-plan.json`**
   - Terraform plan JSON with 3-5 resources
   - Mix of create, update, and delete operations
   - At least one parent-child relationship (e.g., Key Vault + Secret)
   - Purpose: Test different display modes

2. **`TestData/details-display-findings.sarif`**
   - SARIF file with code analysis findings for 1-2 specific resources from the test plan
   - Mix of severity levels (critical, high, medium)
   - Purpose: Test auto mode selective expansion

### Existing Test Data (Reuse)

- `TestData/azurerm-azuredevops-plan.json` - Can be used for basic rendering tests
- Existing SARIF files if available for code analysis integration

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| `--details` with empty string value | Throw CliParseException | TC-04 |
| `--details` at end of args without value | Throw CliParseException | TC-05 |
| Unknown mode value | Throw CliParseException with clear message | TC-04 |
| Case variations (CLOSED, Open, AuTo) | Parse correctly (case-insensitive) | TC-13 |
| Auto mode without SARIF | Behave like closed mode (all collapsed) | TC-17 |
| Resource with empty findings array | Treat as no findings (collapsed in auto) | TC-09 |
| Resource with null findings field | Treat as no findings (collapsed in auto) | TC-09 |
| Parent resource with child findings | Expand parent in auto mode | TC-12, TC-20 |
| Debug block in open mode | Always collapsed | TC-18 |
| Debug block in auto mode | Always collapsed | TC-18 |

## Non-Functional Tests

### Performance

**Scenario:** Rendering a large plan (100+ resources) with different details modes should not significantly impact performance.

**Acceptance Criteria:**
- Helper function is called once per resource (O(n) complexity)
- No performance regression compared to current implementation
- Rendering time increase < 5% for large plans

**Test Approach:**
- Use existing performance benchmarks if available
- Monitor test execution time for snapshot tests
- No dedicated performance test needed unless regression detected

### Compatibility

**Scenario:** The feature must work correctly with existing features (parent-child grouping, code analysis integration, Azure/GitHub rendering targets).

**Acceptance Criteria:**
- Works with both RenderTarget.GitHub and RenderTarget.AzureDevOps
- Works with parent-child merged resources
- Works with code analysis SARIF integration
- Works with large attribute inline diffs

**Test Approach:**
- Integration tests cover these scenarios (TC-16, TC-20)
- UAT scenarios validate rendering in real GitHub/Azure DevOps PRs

### Error Handling

**Scenario:** Invalid CLI arguments should provide clear, actionable error messages.

**Acceptance Criteria:**
- Error message lists valid values: "closed, open, auto"
- Error message indicates the flag that has an issue: "--details"
- Exception type is consistent with existing CLI errors: `CliParseException`

**Test Approach:**
- Unit tests verify exception messages (TC-04, TC-05)

## Open Questions

**Q1:** Should the feature support a short flag (e.g., `-d` for `--details`)?

**Answer:** Not in initial implementation. The specification does not mention a short flag, and it's unclear which letter would be appropriate without conflicting with existing flags. Can be added in a future enhancement if users request it.

**Q2:** Should the UAT test plan include all three modes or focus on one specific mode?

**Answer:** Focus UAT on **auto mode with code analysis**, as it's the most complex scenario and provides the most value for security-focused reviewers. The comprehensive demo will exercise other modes in automated tests.

**Q3:** How should the test data JSON be structured for parent-child relationships?

**Answer:** Use the existing parent-child merging test patterns from `ReportModelBuilderParentChildTests.cs` as a reference. The Developer will create appropriate test data during implementation based on these patterns.

## Test Implementation Notes

### Test File Organization

Based on existing patterns:

1. **CLI Tests:** Add to existing `CliParserTests.cs`
   - TC-01 through TC-05, TC-11, TC-13

2. **Helper Tests:** Create new file `ScribanHelpersDetailsDisplayTests.cs`
   - TC-06 through TC-10, TC-12
   - Follow pattern from existing `ScribanHelpersTests.cs`

3. **Integration/Snapshot Tests:** Create new file `DetailsDisplayModeSnapshotTests.cs`
   - TC-14 through TC-18, TC-20
   - Follow pattern from existing `MarkdownSnapshotTests.cs`
   - Use `SnapshotTestAssertions.AssertMatchesSnapshot()` helper

4. **Template Tests:** Add to existing `TemplateArchitectureTests.cs`
   - TC-19

### Test Naming Convention

Follow existing pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `Parse_DetailsClosedFlag_SetsDetailsDisplayModeToClosed`
- `DetailsOpenAttr_AutoModeWithFindings_ReturnsOpenAttribute`
- `Render_DetailsClosedMode_AllResourcesCollapsed`

### Assertion Library

Use **AwesomeAssertions** fluent style (project standard):

```csharp
options.DetailsDisplayMode.Should().Be(DetailsDisplayMode.Closed);
markdown.Should().Contain("<details open>");
markdown.Should().NotContain("<details open>");
act.Should().Throw<CliParseException>();
```

### Snapshot Testing

Use existing `SnapshotTestAssertions` helper:

```csharp
SnapshotTestAssertions.AssertMatchesSnapshot("details-closed-mode.md", markdown);
```

Snapshots are stored in: `src/tests/Oocx.TfPlan2Md.TUnit/Snapshots/`

## UAT Test Plan

A separate UAT test plan will be created in: `docs/features/092-details-display-mode/uat-test-plan.md`

The UAT plan will focus on:
1. **Auto mode with code analysis** - Primary validation scenario
2. **Visual rendering in GitHub PR comments**
3. **Visual rendering in Azure DevOps PR description**
4. **Regression testing** - Comprehensive demo artifact

The UAT Tester agent will use the comprehensive demo to ensure no unintended side effects, and a feature-specific test artifact to validate the auto mode behavior with selective expansion.

## Definition of Done

All test cases are documented with:
- [x] Clear test steps and expected results
- [x] Test type identified (Unit/Integration)
- [x] Test data requirements specified
- [x] Test location and naming conventions defined
- [x] Edge cases and error scenarios covered
- [x] Non-functional requirements addressed
- [x] UAT scenarios defined for visual validation
- [x] Acceptance criteria mapped to test cases
- [x] Test plan approved by Maintainer
- [x] Test plan committed to feature branch

## Next Steps

After Maintainer approval:
1. **Developer** agent will implement the feature and write all test code
2. **Developer** will create the required test data files
3. **Developer** will run tests via `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`
4. **Developer** will generate snapshots for snapshot tests
5. **UAT Tester** agent will validate rendering in real PR environments
