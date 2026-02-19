# Tasks: Resource Details Display Mode

## Overview

This document breaks down the implementation of the `--details` CLI feature, which allows users to control whether resource details blocks (`<details>` HTML elements) are rendered as open or closed in the generated markdown report.

**Feature Specification:** `docs/features/092-details-display-mode/specification.md`  
**Architecture:** `docs/features/092-details-display-mode/architecture.md`  
**Test Plan:** `docs/features/092-details-display-mode/test-plan.md`

## Tasks

### Task 1: Create `DetailsDisplayMode` Enum

**Priority:** High

**Description:**
Create the `DetailsDisplayMode` enum in `RenderTargets` namespace to define the three display modes: `Closed`, `Open`, and `Auto`.

**Acceptance Criteria:**
- [ ] File `src/Oocx.TfPlan2Md/RenderTargets/DetailsDisplayMode.cs` is created
- [ ] Enum has three values: `Closed`, `Open`, `Auto`
- [ ] Enum is `internal` with appropriate XML documentation
- [ ] Each enum value has XML doc comments explaining its behavior
- [ ] Documentation references the feature specification (docs/features/092-details-display-mode/specification.md)
- [ ] Code follows project conventions (namespace, formatting, etc.)

**Dependencies:** None

**Notes:**
- Follow the pattern used in `RenderTarget.cs` for consistency
- Location: `src/Oocx.TfPlan2Md/RenderTargets/DetailsDisplayMode.cs`
- See architecture document section "1. Enum Location and Definition" for full specification

---

### Task 2: Add `--details` CLI Argument and Update `CliOptions`

**Priority:** High

**Description:**
Add CLI argument parsing for `--details <mode>` in `CliParser.cs` and add the `DetailsDisplayMode` property to `CliOptions.cs`.

**Acceptance Criteria:**
- [ ] `CliOptions.cs` has `DetailsDisplayMode` property with default value `DetailsDisplayMode.Auto`
- [ ] Property has XML doc comment referencing the feature specification
- [ ] `CliParser.cs` parses `--details` argument in the `Parse` method
- [ ] Helper method `ParseDetailsDisplayMode(string value)` is created to parse mode values
- [ ] Parser accepts three case-insensitive values: "closed", "open", "auto"
- [ ] Parser throws `CliParseException` with clear message for invalid values
- [ ] Parser throws `CliParseException` if `--details` is provided without a mode value
- [ ] Parsed mode is assigned to `CliOptions.DetailsDisplayMode`
- [ ] Code follows existing CLI parsing patterns

**Dependencies:** Task 1 (DetailsDisplayMode enum must exist)

**Notes:**
- See architecture document section "2. Data Flow Threading → Step 1: CLI Parsing" for implementation details
- Error message: "Invalid value for --details. Allowed values: closed, open, auto"
- Error message for missing value: "--details requires a mode argument (closed, open, or auto)."
- Follow the pattern used for other CLI arguments like `--render-target`

---

### Task 3: Update `ReportModel` with `DetailsDisplayMode` Property

**Priority:** High

**Description:**
Add the `DetailsDisplayMode` property to `ReportModel.cs` to flow the mode through the rendering pipeline.

**Acceptance Criteria:**
- [ ] `ReportModel.cs` has `DetailsDisplayMode` property
- [ ] Property is marked as `required` (following existing pattern for model properties)
- [ ] Property has XML doc comment referencing the feature specification
- [ ] Property is included in the model's initialization

**Dependencies:** Task 1 (DetailsDisplayMode enum must exist)

**Notes:**
- See architecture document section "2. Data Flow Threading → Step 2: ReportModel"
- Location: Add property around line 94 in `ReportModel.cs`
- Follow pattern used for other rendering-related properties like `RenderTarget`

---

### Task 4: Update `ReportModelBuilder` to Accept and Pass Through `DetailsDisplayMode`

**Priority:** High

**Description:**
Update `ReportModelBuilder` constructor to accept `DetailsDisplayMode` parameter, store it as a field, and set it in the `Build()` method.

**Acceptance Criteria:**
- [ ] `ReportModelBuilder.cs` constructor has `detailsDisplayMode` parameter with default value `DetailsDisplayMode.Auto`
- [ ] Constructor parameter has XML doc comment
- [ ] Private field `_detailsDisplayMode` is created to store the value
- [ ] `ReportModelBuilder.Build.cs` sets `DetailsDisplayMode = _detailsDisplayMode` in the built `ReportModel`
- [ ] Code follows existing pattern for other constructor parameters (showSensitive, renderTarget, etc.)

**Dependencies:** Task 1, Task 3 (enum and ReportModel property must exist)

**Notes:**
- See architecture document section "2. Data Flow Threading → Step 3: ReportModelBuilder"
- Follow the pattern used for `renderTarget`, `showSensitive`, etc.
- Update both `ReportModelBuilder.cs` (constructor) and `ReportModelBuilder.Build.cs` (Build method)

---

### Task 5: Update `CompositionRoot` to Pass `options.DetailsDisplayMode`

**Priority:** High

**Description:**
Update `CompositionRoot.CreateReportModelBuilder()` to pass the `DetailsDisplayMode` from `CliOptions` to `ReportModelBuilder` constructor.

**Acceptance Criteria:**
- [ ] `CompositionRoot.cs` method `CreateReportModelBuilder()` passes `detailsDisplayMode: options.DetailsDisplayMode` to `ReportModelBuilder` constructor
- [ ] Parameter is added in the correct position (after existing parameters, maintaining readability)
- [ ] Code compiles successfully
- [ ] Follows existing parameter passing patterns

**Dependencies:** Task 2, Task 4 (CliOptions and ReportModelBuilder updates must exist)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/CompositionRoot.cs` around line 210
- Add the parameter after `iconProviderRegistry` parameter
- See architecture document section "2. Data Flow Threading"

---

### Task 6: Create the `details_open_attr` Scriban Helper (C# Implementation)

**Priority:** High

**Description:**
Create a new file `DetailsDisplay.cs` in the `ScribanHelpers` directory with the helper function implementation that determines whether a resource details block should have the `open` attribute.

**Acceptance Criteria:**
- [ ] File `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/DetailsDisplay.cs` is created
- [ ] File contains partial class `ScribanHelpers` in correct namespace
- [ ] Method `GetDetailsOpenAttribute(ScriptObject change, string mode)` is implemented
- [ ] Method returns " open" (with leading space) for resources that should be expanded
- [ ] Method returns empty string for resources that should be collapsed
- [ ] Closed mode always returns empty string
- [ ] Open mode always returns " open"
- [ ] Auto mode checks for code analysis findings via `HasCodeAnalysisFindings()` helper
- [ ] `HasCodeAnalysisFindings(ScriptObject change)` helper method is implemented
- [ ] Helper checks `change.code_analysis_findings` array for findings count > 0
- [ ] Helper handles merged child resources (findings are already rolled up to parent)
- [ ] Methods have XML doc comments explaining behavior
- [ ] Code follows existing Scriban helper patterns
- [ ] Unknown modes default to empty string (closed behavior)

**Dependencies:** Task 1 (DetailsDisplayMode enum for type reference in docs)

**Notes:**
- See architecture document section "3. Scriban Helper Function Design"
- Follow pattern in other ScribanHelpers files (e.g., `CodeAnalysis.cs`, `DiffFormatting.cs`)
- The findings rollup for parent-child merging is already handled by `ReportModelBuilder.ParentChildMerging.cs`, so the helper only needs to check the `code_analysis_findings` property
- Return " open" with leading space (not "open") to match template usage pattern

---

### Task 7: Register the Helper in `ScribanHelpers.RegisterHelpers()`

**Priority:** High

**Description:**
Register the `details_open_attr` helper function in the `Registry.cs` file, passing the `DetailsDisplayMode` as a closure parameter.

**Acceptance Criteria:**
- [ ] `Registry.cs` `RegisterHelpers()` method signature includes `DetailsDisplayMode detailsDisplayMode` parameter with default value `DetailsDisplayMode.Auto`
- [ ] Method converts `detailsDisplayMode` to lowercase string for use in helper
- [ ] Method registers `details_open_attr` function using `scriptObject.Import()`
- [ ] Registration uses closure to capture mode: `new Func<ScriptObject, string>(change => GetDetailsOpenAttribute(change, detailsMode))`
- [ ] Registration follows pattern used for other helpers (e.g., `format_diff`)
- [ ] XML doc comment for parameter is added

**Dependencies:** Task 6 (helper implementation must exist)

**Notes:**
- See architecture document section "3. Scriban Helper Function Design" and "7. Registering the Helper"
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Registry.cs`
- Follow the pattern used for `format_diff` and other registered helpers

---

### Task 8: Update `MarkdownRenderer` to Accept and Use `DetailsDisplayMode`

**Priority:** High

**Description:**
Update `MarkdownRenderer.cs` to pass `DetailsDisplayMode` from `ReportModel` to the `RegisterHelpers()` method when rendering templates.

**Acceptance Criteria:**
- [ ] `MarkdownRenderer.RenderWithTemplate()` method passes `model.DetailsDisplayMode` to `RegisterHelpers()` call
- [ ] `MarkdownRenderer.RenderResourceWithTemplate()` method passes appropriate `DetailsDisplayMode` to `RegisterHelpers()` call
- [ ] Both call sites are updated (around lines 330 and 368)
- [ ] Code compiles successfully
- [ ] Follows existing parameter passing patterns

**Dependencies:** Task 3, Task 7 (ReportModel property and RegisterHelpers signature must be updated)

**Notes:**
- See architecture document section "7. Registering the Helper"
- Update both locations where `RegisterHelpers()` is called in `MarkdownRenderer.cs`
- For `RenderResourceWithTemplate()`, extract mode from the appropriate context (follow pattern from main render method)

---

### Task 9: Update `_resource.sbn` Template to Use Helper

**Priority:** High

**Description:**
Update the `_resource.sbn` template to replace the hardcoded `open` attribute logic with a call to the `details_open_attr` helper function.

**Acceptance Criteria:**
- [ ] Line 6 of `_resource.sbn` is updated to use `{{ details_open_attr(change) }}`
- [ ] Old logic `{{ if change.code_analysis_findings.size > 0 }} open{{ end }}` is removed
- [ ] Template syntax is correct (no template compilation errors)
- [ ] Template remains readable and maintainable
- [ ] Change is minimal (only affects the details tag, no other modifications)

**Dependencies:** Task 6, Task 7 (helper must be implemented and registered)

**Notes:**
- See architecture document section "6. Template Updates"
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn`
- Current line 6: `<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }} style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">`
- New line 6: `<details{{ details_open_attr(change) }} style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">`

---

### Task 10: Write CLI Parsing Tests

**Priority:** High

**Description:**
Add unit tests to `CliParserTests.cs` covering the `--details` argument parsing logic for all valid and invalid cases.

**Acceptance Criteria:**
- [ ] Test validates `--details closed` parses to `DetailsDisplayMode.Closed`
- [ ] Test validates `--details open` parses to `DetailsDisplayMode.Open`
- [ ] Test validates `--details auto` parses to `DetailsDisplayMode.Auto`
- [ ] Test validates case-insensitive parsing ("CLOSED", "Open", "AuTo" all work)
- [ ] Test validates missing mode value throws `CliParseException` with expected message
- [ ] Test validates invalid mode value throws `CliParseException` with expected message
- [ ] Test validates default value is `DetailsDisplayMode.Auto` when `--details` is not specified
- [ ] Tests follow existing TUnit + AwesomeAssertions patterns
- [ ] Tests have clear naming following project conventions
- [ ] All tests pass

**Dependencies:** Task 2 (CLI parsing implementation must exist)

**Notes:**
- See test plan document section "Unit Tests - TC-01 through TC-07"
- Location: `src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`
- Follow existing test patterns in `CliParserTests.cs` for argument parsing
- Use AwesomeAssertions for fluent assertions

---

### Task 11: Write Helper Unit Tests

**Priority:** High

**Description:**
Create a new test file `DetailsDisplayTests.cs` with comprehensive unit tests for the `GetDetailsOpenAttribute()` and `HasCodeAnalysisFindings()` helper methods.

**Acceptance Criteria:**
- [ ] File `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Helpers/ScribanHelpers/DetailsDisplayTests.cs` is created
- [ ] Test verifies closed mode returns empty string for all resources
- [ ] Test verifies open mode returns " open" for all resources
- [ ] Test verifies auto mode with findings returns " open"
- [ ] Test verifies auto mode without findings returns empty string
- [ ] Test verifies auto mode with merged child findings returns " open" (parent scenario)
- [ ] Test verifies unknown mode defaults to closed (empty string)
- [ ] Test verifies edge case: empty findings array returns false for HasCodeAnalysisFindings
- [ ] Tests use ScriptObject to simulate resource change objects
- [ ] Tests follow TUnit + AwesomeAssertions patterns
- [ ] All tests pass

**Dependencies:** Task 6 (helper implementation must exist)

**Notes:**
- See test plan document section "Unit Tests - TC-08 through TC-13"
- Create new test file in: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Helpers/ScribanHelpers/DetailsDisplayTests.cs`
- Follow patterns in other ScribanHelper test files
- Create mock ScriptObject instances with `code_analysis_findings` arrays to test various scenarios

---

### Task 12: Write Integration/Snapshot Tests

**Priority:** High

**Description:**
Create integration tests that validate end-to-end rendering with each display mode and verify the generated HTML contains correct `open` attributes.

**Acceptance Criteria:**
- [ ] File `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/DetailsDisplayModeSnapshotTests.cs` is created (or tests added to existing snapshot test file)
- [ ] Test validates `--details closed` produces no `open` attributes in resource details blocks
- [ ] Test validates `--details open` produces `open` attributes in all resource details blocks
- [ ] Test validates `--details auto` with SARIF produces `open` only for resources with findings
- [ ] Test validates `--details auto` without SARIF produces no `open` attributes (all closed)
- [ ] Test validates debug block is always collapsed regardless of mode
- [ ] Test validates merged parent resources with child findings are opened in auto mode
- [ ] Tests use snapshot comparison for HTML output validation
- [ ] Test data files are created (Terraform plan JSON and SARIF files as needed)
- [ ] Tests follow existing snapshot test patterns
- [ ] All tests pass

**Dependencies:** Task 1-9 (entire implementation must be complete)

**Notes:**
- See test plan document section "Integration Tests - TC-14 through TC-18"
- Follow patterns in `MarkdownSnapshotTests.cs`, `AzapiSnapshotTests.cs`, etc.
- Create test data files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`
- Test data files suggested: `details-display-test-plan.json` (Terraform plan), `details-display-findings.sarif` (code analysis)
- Use `SnapshotTestAssertions` for snapshot comparison
- Verify HTML output contains correct `<details>` vs `<details open>` patterns

---

## Implementation Order

Recommended sequence for implementation:

1. **Task 1** - Create `DetailsDisplayMode` enum (foundation type)
2. **Task 2** - Add `--details` CLI argument and update `CliOptions` (input layer)
3. **Task 3** - Update `ReportModel` with property (model layer)
4. **Task 4** - Update `ReportModelBuilder` to accept parameter (model builder)
5. **Task 5** - Update `CompositionRoot` to wire dependencies (composition)
6. **Task 6** - Create `details_open_attr` helper implementation (business logic)
7. **Task 7** - Register helper in `RegisterHelpers()` (helper registration)
8. **Task 8** - Update `MarkdownRenderer` to pass mode (rendering pipeline)
9. **Task 9** - Update `_resource.sbn` template (template layer)
10. **Task 10** - Write CLI parsing tests (validate input layer)
11. **Task 11** - Write helper unit tests (validate business logic)
12. **Task 12** - Write integration/snapshot tests (validate end-to-end behavior)

**Rationale:**
- Tasks 1-5 establish the data flow from CLI to model
- Tasks 6-9 implement the rendering logic
- Tasks 10-12 validate all layers work correctly
- Tests are written after implementation to validate completed code
- Dependencies flow naturally: each task builds on previous tasks

---

## Open Questions

None. All requirements are clear from the specification, architecture, and test plan documents.

---

## Testing Notes

### Test Execution
- Run all tests with: `dotnet test` (or use repository's test wrapper script if available)
- Ensure all existing tests still pass (regression check)
- New snapshot tests may require snapshot updates on first run

### Test Data Requirements
- Create minimal Terraform plan JSON with a few resources (some with findings, some without)
- Create SARIF file with findings targeting specific resources in the test plan
- Test data should be minimal but representative of real-world usage

### Coverage Goals
- CLI parsing: 100% coverage of all branches (valid values, invalid values, missing values, defaults)
- Helper logic: 100% coverage of all modes and edge cases
- Integration: At least one test per mode, plus edge cases (debug block, merged children)

---

## Documentation Notes

After implementation and tests pass, the following documentation updates will be needed (handled by Technical Writer agent):

- [ ] `README.md` - Add `--details` option to CLI usage section
- [ ] `docs/features.md` - Add feature description and examples
- [ ] `HelpTextProvider.cs` - Add `--details` option to help text

These documentation tasks are outside the scope of this implementation work and will be handled separately.
