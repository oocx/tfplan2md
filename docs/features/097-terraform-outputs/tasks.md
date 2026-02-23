# Tasks: Terraform Outputs Support

## Overview

This document breaks down the implementation of Feature 097: Terraform Outputs Support into discrete, actionable tasks. The feature adds rendering of Terraform outputs in tfplan2md reports, with support for module outputs (positioned within module sections) and global outputs (positioned at the end), sensitive value masking, computed value handling, and automatic display name mapping integration.

**References:**
- Specification: `docs/features/097-terraform-outputs/specification.md`
- Architecture: `docs/features/097-terraform-outputs/architecture.md`
- Test Plan: `docs/features/097-terraform-outputs/test-plan.md`

**Implementation Approach:** Test-Driven Development (TDD)
- Write tests first (unit tests, then integration/snapshot tests)
- Implement code to make tests pass
- Refactor and verify

---

## Tasks

### Task 1: Add Test Data Files for Output Scenarios

**Priority:** High

**Description:**
Create JSON test plan files covering various output scenarios needed for unit and integration tests. These files will serve as the foundation for all output-related tests.

**Acceptance Criteria:**
- [ ] `TestData/outputs-basic-plan.json` - Plan with simple global outputs (create, update, delete, no-op actions)
- [ ] `TestData/outputs-module-plan.json` - Plan with module outputs (at least 2 modules with outputs)
- [ ] `TestData/outputs-mixed-plan.json` - Plan with both module and global outputs
- [ ] `TestData/outputs-sensitive-plan.json` - Plan with mix of sensitive and non-sensitive outputs
- [ ] `TestData/outputs-computed-plan.json` - Plan with computed outputs (`after_unknown: true`)
- [ ] `TestData/outputs-no-description-plan.json` - Plan with outputs missing description field
- [ ] `TestData/outputs-sensitivity-sources-plan.json` - Plan with sensitivity markers in different locations (`after_sensitive`, `before_sensitive`, `configuration.sensitive`)
- [ ] `TestData/outputs-with-azure-ids-plan.json` - Plan with outputs containing Azure resource IDs, principal IDs, subscription IDs
- [ ] `TestData/outputs-diverse-actions-plan.json` - Plan with various output actions and edge cases
- [ ] `TestData/outputs-no-outputs-plan.json` - Plan with no outputs at all
- [ ] `TestData/outputs-complex-values-plan.json` - Plan with complex output values (arrays, objects, nested structures)
- [ ] `TestData/outputs-nested-sensitivity-plan.json` - Plan with nested sensitivity objects
- [ ] `TestData/outputs-module-only-plan.json` - Plan with a module that has outputs but no resource changes
- [ ] All test data files are valid Terraform plan JSON format
- [ ] Each file includes both `output_changes` (value data) and `configuration` sections (metadata)
- [ ] Test data files are placed in `TestData/` directory

**Dependencies:** None

**Notes:**
- Use existing test data files as templates for structure
- Ensure outputs include realistic Terraform data (e.g., Azure resource IDs match format `/subscriptions/.../resourceGroups/.../providers/...`)
- For sensitivity testing, include various combinations of `after_sensitive`, `before_sensitive`, and `configuration.sensitive`
- Include both root module outputs and nested module outputs with proper addressing (e.g., `module.database`, `module.network.module.subnet`)

---

### Task 2: Extend TerraformPlan Parsing Layer with OutputChange

**Priority:** High

**Description:**
Extend the `TerraformPlan` record and create a new `OutputChange` record to parse `output_changes` from the Terraform plan JSON. This provides the foundation for the entire feature.

**Acceptance Criteria:**
- [ ] `OutputChange` record created in `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs` with properties:
  - [ ] `Actions` (IReadOnlyList<string>)
  - [ ] `Before` (object?)
  - [ ] `After` (object?)
  - [ ] `AfterUnknown` (bool)
  - [ ] `BeforeSensitive` (object?)
  - [ ] `AfterSensitive` (object?)
- [ ] All properties have `[JsonPropertyName]` attributes matching Terraform plan JSON format
- [ ] XML documentation comments reference the feature spec
- [ ] `TerraformPlan` record extended with `OutputChanges` property (`IReadOnlyDictionary<string, OutputChange>?`)
- [ ] Property is nullable (plans may not have outputs)
- [ ] Property uses `[JsonPropertyName("output_changes")]`
- [ ] Unit test `TC-01` passes: Parse `OutputChange` from JSON
- [ ] Unit test `TC-02` passes: Parse `output_changes` dictionary into `TerraformPlan.OutputChanges`
- [ ] Existing parsing tests still pass (no regression)

**Dependencies:** Task 1 (test data files)

**Notes:**
- Follow the existing pattern from `ResourceChange` record for consistency
- `BeforeSensitive` and `AfterSensitive` use `object?` because they can be boolean or nested objects
- The parsing is automatic via System.Text.Json serialization
- Location: `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`

---

### Task 3: Create OutputChangeModel Class

**Priority:** High

**Description:**
Create the `OutputChangeModel` class in the model layer. This represents a processed output ready for rendering, with all masking and formatting decisions pre-computed.

**Acceptance Criteria:**
- [ ] File created: `src/Oocx.TfPlan2Md/MarkdownGeneration/OutputChangeModel.cs`
- [ ] Class has `internal` visibility
- [ ] Properties implemented:
  - [ ] `Name` (required string) - Output name
  - [ ] `Description` (string?) - Optional description from configuration
  - [ ] `IsSensitive` (bool) - Whether marked as sensitive in configuration
  - [ ] `Action` (required string) - Primary action (create, update, delete, no-op)
  - [ ] `Value` (object?) - The output value (before or after depending on action)
  - [ ] `IsComputed` (bool) - Whether value is computed (known after apply)
  - [ ] `IsMasked` (bool) - Whether value should be masked (pre-computed based on sensitivity + --show-sensitive flag)
  - [ ] `ModuleAddress` (required string) - Module address (empty string for root)
- [ ] XML documentation comments explain each property
- [ ] XML documentation references feature specification
- [ ] Class follows existing model conventions (required properties use `required` keyword)

**Dependencies:** None (pure model class)

**Notes:**
- This class is in the MarkdownGeneration layer, not Parsing layer
- `IsMasked` is pre-computed during model building (defense in depth, per ADR-009)
- `Value` is raw value; templates will format it via helpers
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/OutputChangeModel.cs`

---

### Task 4: Extend ModuleChangeGroup with Outputs Property

**Priority:** High

**Description:**
Add an `Outputs` property to `ModuleChangeGroup` to associate outputs with their containing modules.

**Acceptance Criteria:**
- [ ] `ModuleChangeGroup` class extended with `Outputs` property
- [ ] Property type: `IReadOnlyList<OutputChangeModel>`
- [ ] Property has default value: `Array.Empty<OutputChangeModel>()`
- [ ] Property has XML documentation comment referencing feature spec
- [ ] Property is initialized correctly in existing usages
- [ ] Existing tests still pass (no regression)

**Dependencies:** Task 3 (OutputChangeModel)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/ModuleChangeGroup.cs`
- The default empty array allows existing code to work without changes
- Module outputs are logically grouped with their module's resource changes

---

### Task 5: Extend ReportModel with GlobalOutputs Property

**Priority:** High

**Description:**
Add a `GlobalOutputs` property to `ReportModel` to hold root-level outputs that appear in a dedicated section at the end of the report.

**Acceptance Criteria:**
- [ ] `ReportModel` class extended with `GlobalOutputs` property
- [ ] Property type: `IReadOnlyList<OutputChangeModel>`
- [ ] Property has default value: `Array.Empty<OutputChangeModel>()`
- [ ] Property has XML documentation comment referencing feature spec
- [ ] Outputs are ordered alphabetically by name
- [ ] Existing tests still pass (no regression)

**Dependencies:** Task 3 (OutputChangeModel)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`
- Global outputs are separate from module outputs to support different rendering positions
- Alphabetical ordering is specified in the model, not template

---

### Task 6: Implement Output Metadata Extraction Logic

**Priority:** High

**Description:**
Create helper methods to extract output metadata (description, sensitivity) from the Terraform plan's `configuration` JSON structure. This correlates `output_changes` with `configuration.*.outputs`.

**Acceptance Criteria:**
- [ ] Create partial file: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs`
- [ ] Method `ExtractOutputMetadata(JsonElement? configuration, string outputName, string moduleAddress)` implemented
  - [ ] Returns tuple: `(string? description, bool sensitive)`
  - [ ] For root outputs: navigates to `configuration.root_module.outputs[outputName]`
  - [ ] For module outputs: navigates to `configuration.root_module.modules[...].outputs[outputName]`
  - [ ] Extracts `description` field (null if not present)
  - [ ] Extracts `sensitive` field (defaults to false if not present)
  - [ ] Handles missing configuration gracefully (returns null description, false sensitive)
  - [ ] Handles missing outputs section gracefully
- [ ] Method `IsSensitiveValue(object? sensitivityMarker)` implemented
  - [ ] Returns `true` for boolean `true`
  - [ ] Returns `false` for boolean `false` or `null`
  - [ ] Recursively checks nested objects for sensitivity markers
  - [ ] Handles JsonElement types correctly
- [ ] Unit test `TC-04` passes: Handle missing output descriptions
- [ ] Unit test `TC-05` passes: Detect sensitive outputs from multiple sources

**Dependencies:** Task 1 (test data), Task 3 (OutputChangeModel)

**Notes:**
- Location: New file `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs`
- Use partial class pattern like existing `ReportModelBuilder.*.cs` files
- Sensitivity detection precedence: `after_sensitive` > `before_sensitive` > `configuration.sensitive` > `false`
- The configuration structure is nested JSON, so use JsonElement navigation

---

### Task 7: Implement BuildOutputModels Method

**Priority:** High

**Description:**
Create the core method that builds `OutputChangeModel` instances from the parsed plan data. This method orchestrates metadata extraction, sensitivity detection, masking logic, and value selection.

**Acceptance Criteria:**
- [ ] Method `BuildOutputModels(TerraformPlan plan)` implemented in `ReportModelBuilder.Outputs.cs`
  - [ ] Returns `List<OutputChangeModel>`
  - [ ] Iterates through `plan.OutputChanges` (returns empty list if null)
  - [ ] For each output:
    - [ ] Determines module address from configuration structure
    - [ ] Extracts metadata using `ExtractOutputMetadata`
    - [ ] Determines primary action from `actions` array (first action)
    - [ ] Selects value: `after` for create/update/no-op, `before` for delete
    - [ ] Checks if computed: `after_unknown == true`
    - [ ] Detects sensitivity using precedence rules
    - [ ] Computes `IsMasked`: sensitive AND NOT `_showSensitive` flag
    - [ ] Creates `OutputChangeModel` with all properties set
  - [ ] Returns list ordered by module address, then name
- [ ] Unit test `TC-03` passes: Build `OutputChangeModel` from parsed data
- [ ] Unit test `TC-06` passes: Mask sensitive values by default
- [ ] Unit test `TC-07` passes: Detect computed values
- [ ] Unit test `TC-08` passes: Reveal sensitive values with `--show-sensitive` flag
- [ ] Unit test `TC-10` passes: Order outputs alphabetically
- [ ] Unit tests `TC-11` to `TC-14` pass: All output actions (create, update, delete, no-op)

**Dependencies:** Task 6 (metadata extraction), Task 1 (test data)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs`
- Access `_showSensitive` field from `ReportModelBuilder` (already exists from Issue 098)
- Handle edge case: computed AND sensitive → masked (sensitivity takes precedence)
- Module address for root outputs is empty string `""`

---

### Task 8: Integrate Output Building into ReportModelBuilder.Build

**Priority:** High

**Description:**
Integrate the output building logic into the main `Build()` method of `ReportModelBuilder`. This populates the `GlobalOutputs` property and enhances `ModuleChangeGroup` instances with outputs.

**Acceptance Criteria:**
- [ ] `ReportModelBuilder.Build()` method calls `BuildOutputModels(plan)`
- [ ] Global outputs separated: outputs with `ModuleAddress == string.Empty`
- [ ] Global outputs ordered alphabetically by name
- [ ] Module outputs grouped by module address
- [ ] Module outputs ordered alphabetically within each module
- [ ] Module outputs associated with existing `ModuleChangeGroup` instances
- [ ] Edge case handled: modules with ONLY outputs (no resource changes) get a `ModuleChangeGroup` created
- [ ] `ReportModel.GlobalOutputs` populated with global outputs
- [ ] Unit test `TC-15` passes: Plans with no outputs (empty lists, no exceptions)
- [ ] Unit test `TC-16` passes: Modules with only outputs
- [ ] Integration test `TC-17` passes: Module outputs positioned after module's resource changes
- [ ] Integration test `TC-18` passes: Multiple modules with outputs
- [ ] Integration test `TC-19` passes: Global outputs positioned after all modules
- [ ] Integration test `TC-20` passes: Mixed module and global outputs

**Dependencies:** Task 7 (BuildOutputModels), Task 4 (ModuleChangeGroup.Outputs), Task 5 (ReportModel.GlobalOutputs)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`
- Use LINQ to separate, group, and sort outputs
- Handle case where a module has outputs but no resource changes (rare but possible)
- The ordering ensures deterministic, reviewable output

---

### Task 9: Create format_output_value Scriban Helper

**Priority:** High

**Description:**
Create a Scriban helper function that formats output values for display in the outputs table. This handles masking, computed values, and applies display name mappings.

**Acceptance Criteria:**
- [ ] Method `FormatOutputValue(OutputChangeModel output)` added to `ValueFormatting.cs`
- [ ] Returns string (markdown-formatted value)
- [ ] Logic implemented:
  - [ ] If `output.IsMasked`: return `"(sensitive value)"` (plain text, not code-formatted)
  - [ ] If `output.IsComputed`: return `"(known after apply)"` (plain text, not code-formatted)
  - [ ] Otherwise: format value through existing value formatting pipeline
  - [ ] Apply display name mappings via existing `ValueFormatterRegistry`
  - [ ] Return code-formatted value (backticks)
- [ ] Helper registered in Scriban context as `format_output_value`
- [ ] Unit test created: verify masking produces plain text `"(sensitive value)"`
- [ ] Unit test created: verify computed produces plain text `"(known after apply)"`
- [ ] Unit test created: verify normal values are code-formatted
- [ ] Unit test `TC-09` passes: Display name mappings apply to output values

**Dependencies:** Task 3 (OutputChangeModel)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers/ValueFormatting.cs`
- Reuse existing `ValueFormatterRegistry` to automatically get Azure resource ID formatting, principal mappings, etc.
- Masking and computed values use plain text (not code-formatted) per spec
- The helper will be called from the `_outputs.sbn` template

---

### Task 10: Create _outputs.sbn Partial Template

**Priority:** High

**Description:**
Create a reusable Scriban partial template for rendering output tables. This template receives a list of outputs and a header level, and renders the 4-column table.

**Acceptance Criteria:**
- [ ] File created: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_outputs.sbn`
- [ ] Template accepts parameters:
  - [ ] `outputs` (IReadOnlyList<OutputChangeModel>)
  - [ ] `header_level` (string, e.g., "##" or "####")
- [ ] Template renders nothing if `outputs.size == 0`
- [ ] Template renders header: `{{ header_level }} Outputs`
- [ ] Template renders table header:
  ```
  | Name | Description | Sensitive | Value |
  |------|-------------|-----------|-------|
  ```
- [ ] For each output, renders row:
  - [ ] Name: code-formatted using `format_code_inline`
  - [ ] Description: plain text with markdown escaping, or `-` if null
  - [ ] Sensitive: `Yes` if `is_sensitive`, otherwise `-`
  - [ ] Value: formatted using `format_output_value` helper
- [ ] Template follows existing Scriban conventions (whitespace control with `~`)
- [ ] Template has comments explaining parameters

**Dependencies:** Task 9 (format_output_value helper)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_outputs.sbn`
- This is a reusable partial, called from both module sections and global section
- The header level differs: `####` for module outputs, `##` for global outputs
- Follow existing template patterns from `_resource_change.sbn` and other partials

---

### Task 11: Integrate Outputs into default.sbn Template (Module Outputs)

**Priority:** High

**Description:**
Update the `default.sbn` template to render module outputs immediately after each module's resource changes.

**Acceptance Criteria:**
- [ ] `default.sbn` modified in the module loop section
- [ ] After rendering all resource changes for a module, include `_outputs.sbn` partial
- [ ] Template call: `{{ include "_outputs.sbn" header_level:"####" outputs:module.outputs }}`
- [ ] Module outputs appear after resource changes, before the next module separator (`---`)
- [ ] If module has no outputs, nothing is rendered (template handles empty list)
- [ ] Snapshot test `TC-21` passes: Basic outputs rendering (verify module header is `####`)
- [ ] Snapshot test `TC-18` confirms: Module outputs positioned after module resources

**Dependencies:** Task 10 (_outputs.sbn template)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`
- Module outputs use 4th-level header (`####`) per spec
- The module outputs appear within the module's section, maintaining hierarchy
- Test by generating markdown and verifying position/format

---

### Task 12: Integrate Outputs into default.sbn Template (Global Outputs)

**Priority:** High

**Description:**
Update the `default.sbn` template to render global outputs in a dedicated section at the end of the report (after all resource changes, before debug info).

**Acceptance Criteria:**
- [ ] `default.sbn` modified at the end (after resource changes section)
- [ ] Before debug section, include `_outputs.sbn` partial for global outputs
- [ ] Template call: `{{ include "_outputs.sbn" header_level:"##" outputs:global_outputs }}`
- [ ] Global outputs appear after all modules and resource changes
- [ ] If there are no global outputs, nothing is rendered (template handles empty list)
- [ ] Snapshot test `TC-21` passes: Basic outputs rendering (verify global header is `##`)
- [ ] Snapshot test `TC-19` confirms: Global outputs positioned after all modules

**Dependencies:** Task 10 (_outputs.sbn template)

**Notes:**
- Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`
- Global outputs use 2nd-level header (`##`) per spec (same level as "Summary", "Resource Changes")
- This section appears only if there are global outputs
- Position: after resource changes, before debug information (if present)

---

### Task 13: Create Snapshot Test Baselines for Output Rendering

**Priority:** Medium

**Description:**
Generate baseline markdown files for snapshot tests that validate complete output rendering scenarios. This ensures the entire rendering pipeline produces correct markdown.

**Acceptance Criteria:**
- [ ] Snapshot test `TC-21` baseline created: `outputs-basic-expected.md`
  - [ ] Shows global outputs section with 4-column table
  - [ ] Names are code-formatted (backticks)
  - [ ] Descriptions are plain text or `-`
  - [ ] Sensitive column shows `Yes` or `-`
  - [ ] Values are code-formatted
  - [ ] Alphabetical ordering verified
- [ ] Snapshot test `TC-22` baseline created: `outputs-sensitive-revealed-expected.md`
  - [ ] Sensitive values shown (code-formatted) when `--show-sensitive` used
  - [ ] "Sensitive" column still shows `Yes`
- [ ] Snapshot test `TC-23` baseline created: `outputs-computed-expected.md`
  - [ ] Computed values show `(known after apply)` (plain text, not code-formatted)
- [ ] Snapshot test `TC-24` baseline created: `outputs-with-azure-ids-expected.md`
  - [ ] Output values with Azure resource IDs show display name formatting
  - [ ] Principal IDs show resolved names
  - [ ] Subscription IDs show display names
- [ ] Snapshot test `TC-25` baseline created: `outputs-all-actions-expected.md`
  - [ ] Create action shows `after` value
  - [ ] Update action shows `after` value
  - [ ] Delete action shows `before` value
  - [ ] No-op action shows current value
- [ ] Snapshot test `TC-26` baseline created: `outputs-no-outputs-expected.md`
  - [ ] No "Outputs" section appears when there are no outputs
  - [ ] Existing sections (Summary, Resource Changes) render normally
- [ ] All snapshot tests pass

**Dependencies:** Tasks 1-12 (full implementation chain)

**Notes:**
- Use existing snapshot testing infrastructure
- Baseline files are expected markdown output for comparison
- Tests verify entire rendering pipeline (parsing → model building → template rendering)
- Location: Test baselines typically in `tests/Oocx.TfPlan2Md.TUnit/TestData/` or similar

---

### Task 14: Add Integration Tests for Complex Scenarios

**Priority:** Medium

**Description:**
Create integration tests that verify outputs work correctly in complex scenarios involving nested modules, sensitivity detection, and complex values.

**Acceptance Criteria:**
- [ ] Integration test `TC-27` implemented: Nested sensitivity detection
  - [ ] Test plan with `after_sensitive` as nested object
  - [ ] Verify recursive sensitivity detection works
  - [ ] Verify nested paths are correctly identified as sensitive
- [ ] Integration test `TC-28` implemented: Complex output values (arrays, objects)
  - [ ] Test plan with output values as arrays
  - [ ] Test plan with output values as nested objects
  - [ ] Verify values are formatted as JSON
  - [ ] Verify complex values are code-formatted correctly
- [ ] All integration tests pass

**Dependencies:** Task 1 (test data), Tasks 2-12 (implementation)

**Notes:**
- These tests verify edge cases and complex scenarios
- Integration tests run the full pipeline: parse → build → render
- Focus on realistic scenarios that users might encounter
- Location: `tests/Oocx.TfPlan2Md.TUnit/` (follow existing test organization)

---

### Task 15: Add Architecture Layer Boundary Tests

**Priority:** Low

**Description:**
Verify that the outputs feature doesn't introduce circular dependencies or violate layer boundaries. This ensures the architecture remains clean.

**Acceptance Criteria:**
- [ ] Architecture test `TC-29` implemented: No circular dependencies
  - [ ] Test verifies no circular references in assembly
  - [ ] Test verifies layer boundaries respected (Parsing → Model → Rendering)
  - [ ] OutputChangeModel in correct namespace (MarkdownGeneration, not Parsing)
  - [ ] OutputChange record in correct namespace (Parsing)
- [ ] Test passes

**Dependencies:** Tasks 2-3 (parsing and model classes)

**Notes:**
- Use existing architecture testing infrastructure (if present)
- This is a defensive test to prevent future architectural issues
- Location: Follow existing architecture test patterns

---

### Task 16: Add End-to-End Docker Tests

**Priority:** Medium

**Description:**
Create end-to-end tests that run tfplan2md in a Docker container with various CLI flag combinations, verifying the complete user experience.

**Acceptance Criteria:**
- [ ] End-to-end test `TC-30` implemented: Docker container validation
  - [ ] Test runs tfplan2md in Docker with plan containing outputs
  - [ ] Test verifies default behavior (sensitive values masked)
  - [ ] Test runs with `--show-sensitive` flag
  - [ ] Test verifies sensitive values revealed
  - [ ] Test verifies exit codes and error handling
  - [ ] Test verifies markdown is written to file correctly
- [ ] Test passes

**Dependencies:** Tasks 1-12 (full implementation)

**Notes:**
- Use existing Docker testing infrastructure
- This validates the complete user workflow
- Tests both default behavior and `--show-sensitive` flag
- Location: `tests/Oocx.TfPlan2Md.TUnit/Docker/` (follow existing patterns)

---

### Task 17: Update Documentation and Style Guide

**Priority:** Low

**Description:**
Update the report style guide and other documentation to reflect the new outputs feature and table format.

**Acceptance Criteria:**
- [ ] Report style guide updated (if exists) with outputs table format
  - [ ] 4-column table structure documented
  - [ ] Sensitive value masking behavior documented
  - [ ] Computed value display documented
  - [ ] Module vs global positioning documented
- [ ] CLI help text verified for `--show-sensitive` flag (already exists from Issue 098)
- [ ] Feature specification marked as implemented (status update)
- [ ] CHANGELOG.md updated with feature addition (for next release)

**Dependencies:** All implementation tasks complete

**Notes:**
- The `--show-sensitive` flag already exists, so CLI help is already updated
- Focus on documenting the output table format and positioning rules
- Update any existing documentation that mentions outputs or report structure
- This is the final polish task before handoff

---

### Task 18: Verify All Acceptance Criteria and Tests Pass

**Priority:** High

**Description:**
Final verification that all acceptance criteria from the specification are met and all tests pass. This is the definition of done for the feature.

**Acceptance Criteria:**
- [ ] All unit tests pass (TC-01 through TC-16)
- [ ] All integration tests pass (TC-17 through TC-20, TC-27 through TC-28)
- [ ] All snapshot tests pass (TC-21 through TC-26)
- [ ] Architecture test passes (TC-29)
- [ ] End-to-end Docker test passes (TC-30)
- [ ] All 23 acceptance criteria from specification are verified:
  - [ ] Outputs parsed from plan JSON
  - [ ] Module outputs positioned after module resources
  - [ ] Global outputs positioned after all resources
  - [ ] 4-column table format
  - [ ] Names code-formatted
  - [ ] Descriptions plain text or `-`
  - [ ] Sensitive column shows `Yes` or `-`
  - [ ] Values code-formatted when displayed
  - [ ] Sensitive values masked by default
  - [ ] Computed values show `(known after apply)`
  - [ ] `--show-sensitive` reveals sensitive values
  - [ ] Display name mappings apply automatically
  - [ ] Alphabetical ordering
  - [ ] Create action shows `after` value
  - [ ] Update action shows `after` value
  - [ ] Delete action shows `before` value
  - [ ] No-op action shows current value
  - [ ] Plans with no outputs omit section
  - [ ] Module outputs use `####` header
  - [ ] Global outputs use `##` header
  - [ ] Report style guide updated
  - [ ] CLI help includes `--show-sensitive`
  - [ ] Existing tests pass (no regression)
  - [ ] New tests cover output scenarios
- [ ] No test failures
- [ ] No compiler warnings
- [ ] Code review feedback addressed (if any)

**Dependencies:** All previous tasks

**Notes:**
- This is a comprehensive verification task before handoff
- Run full test suite to ensure no regressions
- Verify manually that all specification requirements are met
- This task serves as the gate before committing the feature as complete

---

## Implementation Order

Recommended sequence for implementation (follows TDD approach):

1. **Task 1** - Add test data files (foundation for all tests)
2. **Task 2** - Extend parsing layer (TerraformPlan + OutputChange)
3. **Task 3** - Create OutputChangeModel (model class)
4. **Task 4** - Extend ModuleChangeGroup with Outputs
5. **Task 5** - Extend ReportModel with GlobalOutputs
6. **Task 6** - Implement output metadata extraction logic
7. **Task 7** - Implement BuildOutputModels method (core logic)
8. **Task 8** - Integrate into ReportModelBuilder.Build (wire everything together)
9. **Task 9** - Create format_output_value Scriban helper
10. **Task 10** - Create _outputs.sbn partial template
11. **Task 11** - Integrate module outputs into default.sbn
12. **Task 12** - Integrate global outputs into default.sbn
13. **Task 13** - Create snapshot test baselines (full rendering validation)
14. **Task 14** - Add integration tests for complex scenarios
15. **Task 15** - Add architecture layer boundary tests
16. **Task 16** - Add end-to-end Docker tests
17. **Task 17** - Update documentation and style guide
18. **Task 18** - Final verification (all tests pass, all criteria met)

**Rationale for ordering:**
- Test data first (enables TDD from the start)
- Parsing layer next (foundation for everything)
- Model layer next (data structures)
- Model building logic (transforms parsed data to models)
- Rendering layer (templates and helpers)
- Comprehensive testing (snapshot, integration, E2E)
- Documentation and final verification

---

## Open Questions

None at this time. All architectural questions were resolved in the architecture document:
- Data model extension → Parse `output_changes` eagerly
- Metadata correlation → Navigate configuration structure during model building
- Module output parsing → Build complete list, group by module address
- Value rendering → Reuse existing `ValueFormatterRegistry`
- Sensitivity detection → Precedence: `after_sensitive` > `before_sensitive` > `configuration.sensitive` > `false`
- Update actions → Show only `after` value (before→after diff is future enhancement)

---

## Notes for Developer

**Key Implementation Patterns:**
- **Follow TDD**: Write tests first for each task, then implement to make tests pass
- **Partial classes**: Use `ReportModelBuilder.Outputs.cs` for output-related logic (follows existing pattern)
- **Defense in depth**: Pre-compute `IsMasked` flag during model building (ADR-009 security pattern)
- **Value formatting reuse**: Leverage existing `ValueFormatterRegistry` for automatic display name mappings
- **Template conventions**: Follow existing Scriban template patterns (whitespace control, helper usage)
- **Alphabetical ordering**: Apply at model layer, not template layer (deterministic, testable)

**Edge Cases to Handle:**
- No outputs (return empty lists, render nothing)
- Modules with only outputs, no resource changes (create ModuleChangeGroup)
- Missing descriptions (null → `-` in template)
- Nested sensitivity objects (recursive detection)
- Complex values (arrays, objects → JSON formatting)
- Computed AND sensitive (masking takes precedence over computed)
- Missing configuration metadata (graceful degradation)

**Testing Strategy:**
- Unit tests validate individual methods and logic paths
- Integration tests validate end-to-end scenarios
- Snapshot tests validate complete markdown output
- Docker tests validate user experience
- Architecture tests ensure no structural issues

**Performance Considerations:**
- Outputs list is typically small (<50 outputs)
- Single-pass processing (no multiple iterations)
- Pre-compute masking flags (avoid template logic)
- Reuse existing formatters (no duplication)

**Security Considerations:**
- Sensitive values masked by default
- `IsMasked` computed at model boundary (defense in depth)
- Template receives pre-computed flag (cannot accidentally leak)
- Follows same masking pattern as resource attributes (consistency)
