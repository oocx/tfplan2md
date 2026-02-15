# Tasks: Debug Output

## Overview

Add comprehensive debug output capabilities to tfplan2md to help users troubleshoot principal mapping issues and understand template resolution. This feature introduces a `--debug` CLI flag that appends diagnostic information to the markdown report as a new section.

Reference: [specification.md](specification.md) | [architecture.md](architecture.md) | [test-plan.md](test-plan.md)

## User Stories

### US-01: Troubleshoot Principal Mapping Failures
**As a** tfplan2md user  
**I want** to see which principal IDs failed to resolve and where they are referenced  
**So that** I can update my principal mapping file with the missing entries

**Acceptance Criteria:**
- Debug output shows failed principal IDs with resource context
- Each failed ID indicates which resource (e.g., `azurerm_role_assignment.example`) referenced it
- Load status of principal mapping file is clearly indicated

### US-02: Understand Template Selection
**As a** tfplan2md user  
**I want** to see which templates were used for each resource type  
**So that** I can verify my custom templates are being applied correctly

**Acceptance Criteria:**
- Debug output lists template resolution for each resource type
- Distinguishes between built-in, custom, and default templates
- Shows file path for custom templates

### US-03: Enable Debug Mode Easily
**As a** tfplan2md user  
**I want** to enable debug output with a simple flag  
**So that** I can quickly diagnose issues without changing my workflow

**Acceptance Criteria:**
- Single `--debug` flag enables all diagnostics
- Debug output appended to markdown report (default behavior)
- No impact when debug flag is not used
- Help text documents the debug flag

## Tasks

### Task 1: Create Diagnostic Context Infrastructure

**Priority:** High

**Description:**
Create the `DiagnosticContext` class and supporting record types to collect diagnostic information throughout the processing pipeline. This is the foundation for all debug output functionality.

**Acceptance Criteria:**
- [ ] Create `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs` with:
  - Properties for principal mapping diagnostics (file path, load status, type counts, failed resolutions)
  - Properties for template resolution diagnostics (list of template resolutions)
  - Method `GenerateMarkdownSection()` that produces formatted markdown output
- [ ] Create `src/Oocx.TfPlan2Md/Diagnostics/FailedPrincipalResolution.cs` record with:
  - `string PrincipalId` - The principal ID that failed to resolve
  - `string ResourceAddress` - The resource that referenced this ID
- [ ] Create `src/Oocx.TfPlan2Md/Diagnostics/TemplateResolution.cs` record with:
  - `string ResourceType` - The Terraform resource type
  - `string TemplateSource` - Description of which template was used
- [ ] All classes follow existing code style and conventions
- [ ] XML documentation comments added for public APIs

**Dependencies:** None

**Notes:**
- `DiagnosticContext` should have mutable collections (List, Dictionary) since it accumulates data
- `GenerateMarkdownSection()` should handle empty diagnostics gracefully (return "## Debug Information\n\nNo diagnostics collected." or similar)
- Follow the markdown format specified in architecture.md (use code formatting for IDs and resource addresses)

---

### Task 2: Add Debug Flag to CLI

**Priority:** High

**Description:**
Add support for the `--debug` flag in the CLI parser and options, and update help text to document the new flag.

**Acceptance Criteria:**
- [ ] Add `bool Debug { get; init; }` property to `CliOptions` record in `src/Oocx.TfPlan2Md/CLI/CliParser.cs`
- [ ] Add XML documentation comment for the `Debug` property explaining its purpose
- [ ] Parse `--debug` flag in `CliParser.Parse()` method (add case statement)
- [ ] Update `HelpTextProvider.GetHelpText()` in `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs` to document `--debug` flag:
  - Include description: "Append diagnostic information to the report"
  - Position appropriately with other flags
- [ ] Default value for `Debug` is `false` (no behavior change by default)
- [ ] `--debug` flag can appear anywhere in argument list (before or after other arguments)

**Dependencies:** None

**Notes:**
- Follow the pattern of existing boolean flags like `--show-sensitive`
- Flag should not require a value (it's a switch, not an option)

---

### Task 3: Integrate DiagnosticContext with PrincipalMapper

**Priority:** High

**Description:**
Update `PrincipalMapper` to accept an optional `DiagnosticContext` parameter and record principal mapping diagnostics during loading and resolution.

**Acceptance Criteria:**
- [ ] Add optional `DiagnosticContext?` parameter to `PrincipalMapper` constructor
- [ ] In `LoadMappings()` method, record diagnostics when context is provided:
  - Set `PrincipalMappingFileProvided = true` if file path is not null/empty
  - Set `PrincipalMappingFilePath` to the provided file path
  - Set `PrincipalMappingLoadedSuccessfully = true` on successful load
  - Set `PrincipalMappingLoadedSuccessfully = false` if exception occurs during load
  - Calculate and record `PrincipalTypeCount` by analyzing the loaded principals
- [ ] Update `GetName()` or `GetPrincipalName()` to record failed resolutions:
  - When a principal ID is not found in the mapping, add a `FailedPrincipalResolution` entry
  - Include the resource address as context (this will need to be passed from caller)
- [ ] `PrincipalMapper` works normally when `DiagnosticContext` is null (backward compatibility)
- [ ] No changes to public API signature (context is optional parameter)

**Dependencies:** Task 1

**Notes:**
- Recording failed resolutions requires knowing which resource is requesting the mapping. This may require updating call sites in `ReportModelBuilder` to pass resource context.
- Type counting: analyze the loaded dictionary to determine if principals follow naming conventions (e.g., "user-", "group-", "sp-" prefixes) or use a separate metadata structure if available.
- For initial implementation, if type distinction is complex, start with just total count and refine later.

---

### Task 4: Integrate DiagnosticContext with MarkdownRenderer

**Priority:** High

**Description:**
Update `MarkdownRenderer` to accept an optional `DiagnosticContext` parameter and record template resolution decisions during rendering.

**Acceptance Criteria:**
- [ ] Add optional `DiagnosticContext?` parameter to `Render()` methods in `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs`:
  - `Render(ReportModel model)` → `Render(ReportModel model, DiagnosticContext? context = null)`
  - `Render(ReportModel model, string templateNameOrPath)` → `Render(ReportModel model, string templateNameOrPath, DiagnosticContext? context = null)`
  - `RenderAsync(ReportModel model, string templateNameOrPath)` → `RenderAsync(ReportModel model, string templateNameOrPath, DiagnosticContext? context = null)`
- [ ] In `RenderWithTemplate()` or similar internal method, record template resolution:
  - Add `TemplateResolution` entry for main template (custom vs built-in)
  - Add entries for resource-specific templates resolved via `TemplateResolver`
- [ ] Template source descriptions should be clear:
  - "Built-in resource-specific template" for embedded resource templates
  - "Default template" for the default fallback
  - "Custom template: {path}" for custom template files
- [ ] `MarkdownRenderer` works normally when `DiagnosticContext` is null (backward compatibility)

**Dependencies:** Task 1

**Notes:**
- May need to intercept `TemplateResolver` calls or extend it to report resolution decisions
- Consider recording template resolutions in the template loading/resolution phase rather than during rendering
- Avoid duplicate entries (track which resource types have been logged)

---

### Task 5: Wire Up DiagnosticContext in Program.cs

**Priority:** High

**Description:**
Update `Program.cs` to create a `DiagnosticContext` when `--debug` is enabled, pass it through the processing pipeline, and append the debug section to the final output.

**Acceptance Criteria:**
- [ ] In `RunAsync()` method, after parsing options:
  - Create `DiagnosticContext?` variable: `var diagnosticContext = options.Debug ? new DiagnosticContext() : null;`
- [ ] Pass `diagnosticContext` to `PrincipalMapper` constructor
- [ ] Pass `diagnosticContext` to `MarkdownRenderer` constructor or `Render()` method
- [ ] After rendering markdown, append debug section if context exists:
  - `if (diagnosticContext is not null) { markdown += "\n\n" + diagnosticContext.GenerateMarkdownSection(); }`
- [ ] Output includes debug section only when `--debug` flag is used
- [ ] No debug section appears when `--debug` flag is not used

**Dependencies:** Task 1, Task 2, Task 3, Task 4

**Notes:**
- Ensure proper newline handling when appending debug section
- Consider edge case: empty markdown output (shouldn't happen in practice, but handle gracefully)
- The debug section should be clearly separated from main content

---

### Task 6: Implement Unit Tests for DiagnosticContext

**Priority:** High

**Description:**
Write comprehensive unit tests for the `DiagnosticContext` class to verify markdown generation with various diagnostic scenarios.

**Acceptance Criteria:**
- [ ] Test TC-03: Empty diagnostic context generates appropriate output
- [ ] Test TC-04: Full diagnostic context (principal mapping + template resolution) generates correctly formatted markdown
- [ ] Test TC-15: Failed principal resolutions are formatted correctly with code blocks
- [ ] Test TC-16: Template resolutions are formatted correctly as a list
- [ ] Test TC-20: No principal mapping file provided case is handled
- [ ] Test that markdown structure includes proper headings ("## Debug Information", subsections)
- [ ] Test edge cases: very long resource addresses, special characters in paths
- [ ] All tests use TUnit framework and follow existing test conventions

**Dependencies:** Task 1

**Notes:**
- Tests should be in `src/tests/Oocx.TfPlan2Md.TUnit/Diagnostics/` directory
- Use inline test data (no external files needed for these unit tests)
- Verify markdown formatting (code blocks, bullet lists, headings)

---

### Task 7: Implement Unit Tests for CLI Changes

**Priority:** High

**Description:**
Write unit tests to verify the CLI parser correctly handles the `--debug` flag and help text includes documentation.

**Acceptance Criteria:**
- [ ] Test TC-01: `--debug` flag in various positions is parsed correctly
  - `["--debug", "plan.json"]` → `Debug = true`
  - `["plan.json", "--debug"]` → `Debug = true`
  - `["plan.json"]` → `Debug = false` (default)
- [ ] Test TC-02: Help text includes `--debug` flag documentation
  - Verify flag name appears in help output
  - Verify description explains diagnostic output
- [ ] Tests follow existing CLI test patterns
- [ ] All tests use TUnit framework

**Dependencies:** Task 2

**Notes:**
- Locate tests in `src/tests/Oocx.TfPlan2Md.TUnit/CLI/` directory
- Follow existing test naming conventions

---

### Task 8: Implement Integration Tests for PrincipalMapper Diagnostics

**Priority:** High

**Description:**
Write integration tests to verify `PrincipalMapper` correctly collects and records principal mapping diagnostics.

**Acceptance Criteria:**
- [ ] Test TC-05: Successful principal mapping file load records diagnostics
  - File path is recorded
  - Load status is true
  - Type counts are accurate
- [ ] Test TC-06: Failed principal mapping file load records diagnostics
  - File path is recorded
  - Load status is false
  - Type counts are empty
- [ ] Test TC-07: Type counts match actual principals in mapping file
- [ ] Test TC-12: Failed principal resolutions record resource context
  - Each failed ID includes which resource referenced it
  - Multiple references to same ID are all recorded
- [ ] Test TC-13: `PrincipalMapper` works normally with null context (backward compatibility)
- [ ] Test TC-19: Same failed ID referenced by multiple resources records all references

**Dependencies:** Task 3

**Notes:**
- Use existing test data files where possible (`TestData/principal-mapping.json`)
- May need to create `TestData/partial-principal-mapping.json` for testing failed resolutions
- Tests should verify both the diagnostics collection and normal mapper operation

---

### Task 9: Implement Integration Tests for MarkdownRenderer Diagnostics

**Priority:** High

**Description:**
Write integration tests to verify `MarkdownRenderer` correctly collects and records template resolution diagnostics.

**Acceptance Criteria:**
- [ ] Test TC-08: Multiple template types are recorded correctly
  - Custom template shows file path
  - Built-in resource-specific template indicated
  - Default template indicated
- [ ] Test TC-09: Built-in template usage recorded when no custom template provided
  - Resources with built-in templates show "built-in resource-specific"
  - Other resources show "default template"
- [ ] Test TC-14: `MarkdownRenderer` works normally with null context (backward compatibility)
- [ ] Tests verify template resolution diagnostics don't interfere with rendering

**Dependencies:** Task 4

**Notes:**
- Use `TestData/firewall-rule-changes.json` for testing built-in templates
- May need to create or use existing multi-resource test plans
- Focus on verifying diagnostic collection, not full rendering functionality

---

### Task 10: Implement End-to-End Integration Tests

**Priority:** High

**Description:**
Write end-to-end tests that verify the complete debug output feature from CLI flag through to final markdown output.

**Acceptance Criteria:**
- [ ] Test TC-10: Without `--debug` flag, no debug section appears
  - Parse arguments without flag
  - Generate report
  - Verify output does NOT contain "## Debug Information"
- [ ] Test TC-11: With `--debug` flag, debug section is appended
  - Parse arguments with `--debug`
  - Generate report with principal mapping
  - Verify output contains "## Debug Information" section
  - Verify section includes principal mapping diagnostics
  - Verify section includes template resolution diagnostics
  - Verify main report content is unchanged
- [ ] Test TC-17: Docker container respects `--debug` flag
  - Run container with `--debug`
  - Verify debug section appears in output
  - Verify exit code is 0
- [ ] Test TC-18: All existing tests still pass (regression verification)

**Dependencies:** Task 5

**Notes:**
- Use representative test data that exercises both principal mapping and template resolution
- Verify the debug section is properly separated from main content
- Docker test may be manual or automated depending on test infrastructure

---

### Task 11: Create Test Data Files

**Priority:** Medium

**Description:**
Create or identify test data files needed for testing the debug output feature.

**Acceptance Criteria:**
- [ ] Verify `TestData/principal-mapping.json` exists and is adequate
- [ ] Create `TestData/partial-principal-mapping.json` with intentionally missing principal IDs
  - Include some principals but omit others that exist in test plans
  - Document which IDs are omitted
- [ ] Create or identify `TestData/role-assignments-plan.json` with role assignment resources
  - Multiple `azurerm_role_assignment` resources
  - References to principal IDs (some present in partial mapping, some not)
- [ ] Create or identify `TestData/multi-resource-plan.json` with diverse resource types
  - Resources that use default template
  - Resources with built-in templates (e.g., firewall rules)
  - Mix suitable for testing template resolution tracking
- [ ] Document test data files in test plan or README

**Dependencies:** None (can be done in parallel with other tasks)

**Notes:**
- Some test data may already exist; verify before creating new files
- Keep test data minimal but representative
- Ensure test data is committed to version control

---

### Task 12: Update Documentation

**Priority:** Low

**Description:**
Update project documentation to reflect the new debug output feature.

**Acceptance Criteria:**
- [ ] Verify help text is clear and accurate (covered by Task 2)
- [ ] Update README.md if it includes CLI flag documentation
- [ ] Verify specification.md, architecture.md, and test-plan.md are accurate
- [ ] Add examples of debug output to specification if helpful
- [ ] No additional documentation needed beyond what's already in feature docs

**Dependencies:** Task 5 (after implementation is complete)

**Notes:**
- Most documentation is already in place (specification, architecture, test plan)
- Focus on user-facing documentation (README, help text)
- Consider adding a troubleshooting section with debug flag usage examples

---

### Task 13: Performance and Edge Case Validation

**Priority:** Low

**Description:**
Verify that debug output collection has minimal performance impact and handles edge cases gracefully.

**Acceptance Criteria:**
- [ ] Test with large plan (100+ resources) to verify diagnostic collection scales
- [ ] Verify debug output collection adds < 5% overhead to processing time
- [ ] Test with very long resource addresses (>1000 characters)
- [ ] Test with special characters in file paths (spaces, quotes, unicode)
- [ ] Test with empty principal mapping file (0 principals)
- [ ] Verify diagnostic generation doesn't fail if principal mapper or renderer encounter errors
- [ ] All edge cases produce valid markdown (no broken formatting)

**Dependencies:** Task 10 (after main implementation is complete)

**Notes:**
- Performance overhead should be negligible since diagnostics are optional and lightweight
- Focus on ensuring edge cases don't break the debug output generation
- If performance issues are found, optimize diagnostic collection

---

## Implementation Order

The recommended implementation sequence, ordered by dependencies and risk:

### Phase 1: Foundation (Tasks 1-2)
1. **Task 1**: Create Diagnostic Context Infrastructure - Core data structures
2. **Task 2**: Add Debug Flag to CLI - User interface for enabling feature
3. **Task 11**: Create Test Data Files - Prepare for testing (can overlap with Phase 1)

### Phase 2: Integration (Tasks 3-5)
4. **Task 3**: Integrate DiagnosticContext with PrincipalMapper - First integration point
5. **Task 4**: Integrate DiagnosticContext with MarkdownRenderer - Second integration point
6. **Task 5**: Wire Up DiagnosticContext in Program.cs - Complete the pipeline

### Phase 3: Testing (Tasks 6-10)
7. **Task 6**: Unit Tests for DiagnosticContext - Test core functionality
8. **Task 7**: Unit Tests for CLI Changes - Test CLI parsing
9. **Task 8**: Integration Tests for PrincipalMapper - Test mapper integration
10. **Task 9**: Integration Tests for MarkdownRenderer - Test renderer integration
11. **Task 10**: End-to-End Integration Tests - Test complete feature

### Phase 4: Finalization (Tasks 12-13)
12. **Task 12**: Update Documentation - User-facing docs
13. **Task 13**: Performance and Edge Case Validation - Final validation

**Rationale for ordering:**
- Foundation tasks establish the core infrastructure needed by all other tasks
- Integration tasks build on the foundation and can be done in parallel (with some coordination)
- Testing tasks verify each layer as it's implemented
- Finalization tasks ensure quality and completeness

**Critical path:** Tasks 1 → 3 → 5 → 10 (minimum viable implementation)

## Open Questions

None - all design decisions have been finalized in the architecture document.

## Notes for Developer

### Key Design Principles
- **Non-intrusive**: The diagnostic context is optional; components work normally without it
- **Backward compatible**: No breaking changes to existing APIs
- **Separation of concerns**: Diagnostics are separate from business logic
- **Testable**: Each component can be tested independently

### Implementation Tips
- Start with Task 1 to establish the data structures
- Use nullable `DiagnosticContext?` parameters throughout to maintain backward compatibility
- Check for null before recording diagnostics: `context?.RecordSomething()`
- Follow existing code style (especially for records and XML comments)
- Test incrementally - verify each integration point before moving to the next

### Potential Challenges
- **Principal type detection**: The current `PrincipalMapper` uses a flat dictionary. Type counting may require inferring types from naming conventions or adding metadata to the mapping file format. Start simple (total count) and enhance later if needed.
- **Resource context for failed resolutions**: Failed principal lookups need to know which resource requested them. This may require threading resource addresses through the call stack or refactoring how mapper is called.
- **Template resolution tracking**: Need to intercept or extend `TemplateResolver` to record decisions. May require refactoring or adding a callback mechanism.

### Testing Strategy
- Unit tests for core logic (DiagnosticContext, CLI parsing)
- Integration tests for each integration point (PrincipalMapper, MarkdownRenderer)
- End-to-end tests for the complete feature
- Regression tests to ensure existing functionality is unaffected

## Definition of Done

Feature 038 (Debug Output) is complete when:

- [ ] All 13 tasks are completed with acceptance criteria met
- [ ] All test cases from test-plan.md (TC-01 through TC-20) are implemented and passing
- [ ] `--debug` flag is documented in help text
- [ ] Debug output is appended to markdown report when flag is used
- [ ] Debug output includes principal mapping diagnostics (load status, type counts, failed resolutions with context)
- [ ] Debug output includes template resolution diagnostics (template sources for each resource type)
- [ ] Debug output is disabled by default (no change to existing behavior)
- [ ] All existing tests pass (no regressions)
- [ ] Code follows project conventions and includes XML documentation
- [ ] Feature is tested with Docker container
- [ ] Documentation is updated (help text, README if applicable)
- [ ] Edge cases are handled gracefully
- [ ] Performance impact is negligible (< 5% overhead)
