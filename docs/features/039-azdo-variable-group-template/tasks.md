# Tasks: Azure DevOps Variable Group Template

## Overview

This feature implements a custom Scriban template with ViewModel pattern for Azure DevOps Variable Groups (`azuredevops_variable_group`) to display variable changes semantically. The template merges regular and secret variables into a unified table, showing all metadata while protecting secret values.

**References:**
- Specification: [specification.md](specification.md)
- Architecture: [architecture.md](architecture.md)
- Test Plan: [test-plan.md](test-plan.md)

**Implementation Approach:** ViewModel Pattern (Factory + ViewModel + Template)

## Tasks

### Task 1: Create ViewModel Classes

**Priority:** High

**Description:**
Define the data structures that will hold precomputed variable data for template rendering. This includes the main `VariableGroupViewModel` and three row classes for different table layouts.

**Acceptance Criteria:**
- [ ] File created: `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/VariableGroupViewModel.cs`
- [ ] `VariableGroupViewModel` class defined with properties:
  - `string? Name` - variable group name
  - `string? Description` - variable group description
  - `IReadOnlyList<VariableChangeRowViewModel> VariableChanges` - for update operations
  - `IReadOnlyList<VariableRowViewModel> AfterVariables` - for create operations
  - `IReadOnlyList<VariableRowViewModel> BeforeVariables` - for delete operations
  - `IReadOnlyList<KeyVaultRowViewModel> KeyVaultBlocks` - for Key Vault integration
- [ ] `VariableChangeRowViewModel` class defined with properties:
  - `required string Change` - change indicator (➕, 🔄, ❌, ⏺️)
  - `required string Name` - formatted variable name
  - `required string Value` - formatted value or diff (with `(sensitive / hidden)` for secrets)
  - `required string Enabled` - formatted enabled status or diff
  - `required string ContentType` - formatted content_type or diff
  - `required string Expires` - formatted expires or diff
  - `bool IsLargeValue` - flag for large value handling
- [ ] `VariableRowViewModel` class defined with properties (for create/delete tables):
  - `required string Name`
  - `required string Value`
  - `required string Enabled`
  - `required string ContentType`
  - `required string Expires`
  - `bool IsLargeValue`
- [ ] `KeyVaultRowViewModel` class defined with properties:
  - `required string Name`
  - `required string ServiceEndpointId`
  - `required string SearchDepth`
- [ ] All classes are `public sealed`
- [ ] All classes have XML documentation comments (including summary, properties)
- [ ] Property types use `required` or nullable appropriately
- [ ] Collections initialized to `Array.Empty<T>()` as default
- [ ] Feature reference comment added: `Related feature: docs/features/039-azdo-variable-group-template/specification.md`

**Dependencies:** None

**Notes:**
- Follow the pattern from `NetworkSecurityGroupViewModel.cs` (separate classes for change rows vs simple rows)
- Use `required` for string properties that must always have values from the factory
- Keep classes minimal - no logic, just data containers

---

### Task 2: Create Factory with Unit Tests (TDD)

**Priority:** High

**Description:**
Implement the `VariableGroupViewModelFactory` class using Test-Driven Development. Start by writing tests for each scenario, then implement the factory logic to make tests pass. The factory performs semantic diffing of variables by name across both `variable` and `secret_variable` arrays.

**Acceptance Criteria:**
- [ ] File created: `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/VariableGroupViewModelFactory.cs`
- [ ] Test file created: `tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/VariableGroupViewModelFactoryTests.cs`
- [ ] Factory class is `internal static` with static `Build()` method:
  - Signature: `public static VariableGroupViewModel Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat)`
- [ ] Factory extracts variable group name and description from before/after JSON
- [ ] Factory merges `variable` and `secret_variable` arrays by name into unified dictionaries
- [ ] Factory performs semantic matching by variable name across before/after states
- [ ] Factory categorizes variables as: Added (➕), Modified (🔄), Removed (❌), Unchanged (⏺️)
- [ ] Factory detects secret variables (from `secret_variable` array) and formats value as `` `(sensitive / hidden)` ``
- [ ] Factory formats regular variable values as inline code (`` `value` ``)
- [ ] Factory handles modified variables with before/after diff formatting:
  - Changed attributes: `` - `old`<br>+ `new` `` (two lines with `<br>`)
  - Unchanged attributes: `` `value` `` (single value, no prefix)
- [ ] Factory detects large values (>100 chars or multi-line) for regular variables only
- [ ] Factory formats empty/null attributes as `-` (plain text, no code formatting)
- [ ] Factory parses `key_vault` blocks and formats all properties as inline code
- [ ] Factory handles edge cases: empty arrays, null values, unknown/computed values
- [ ] All factory methods have XML documentation comments
- [ ] Unit tests implemented covering test cases from test-plan.md:
  - TC-01: Create operation with regular variables
  - TC-02: Create operation with secret variables
  - TC-03: Create operation with mixed variables
  - TC-04: Update operation with secret metadata changes
  - TC-05: Update operation with secret value changes
  - TC-06: Update operation with added variables
  - TC-07: Update operation with removed variables
  - TC-08: Update operation with modified variables (before/after diff)
  - TC-09: Update operation with unchanged variables
  - TC-10: Large value detection for regular variables (>100 chars, multi-line)
  - TC-11: Large value detection excludes secret variables
  - TC-12: Key Vault integration with blocks
  - TC-13: Key Vault integration with empty array
  - TC-17: Edge case - empty variable arrays
  - TC-18: Edge case - replace action treated as update
  - TC-19: Edge case - unknown/computed values
  - TC-20: Edge case - null and empty string attributes
- [ ] All tests pass consistently
- [ ] Test naming follows convention: `Build_Scenario_ExpectedResult`

**Dependencies:** Task 1 (ViewModel classes)

**Notes:**
- Use TDD: Write test first, then implement factory logic to pass the test
- Study `NetworkSecurityGroupViewModelFactory.cs` for patterns (semantic matching, diff formatting)
- Use `JsonElement` for JSON parsing (no dynamic/reflection)
- The factory should be pure logic - no side effects, no I/O
- Test data can be created inline in tests using `JsonDocument.Parse()`

---

### Task 3: Register ViewModel in ResourceChangeModel

**Priority:** High

**Description:**
Add the `VariableGroup` property to `ResourceChangeModel` class so the ViewModel can be attached to resource changes.

**Acceptance Criteria:**
- [ ] File modified: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`
- [ ] New property added to `ResourceChangeModel` class:
  ```csharp
  /// <summary>
  /// Gets or sets the precomputed view model for azuredevops_variable_group resources.
  /// Related feature: docs/features/039-azdo-variable-group-template/specification.md
  /// </summary>
  public VariableGroupViewModel? VariableGroup { get; set; }
  ```
- [ ] Property placed after existing ViewModel properties (after `RoleAssignment`)
- [ ] XML documentation follows existing pattern
- [ ] Property is nullable (allows null for non-variable-group resources)

**Dependencies:** Task 1 (ViewModel classes)

**Notes:**
- Follow the existing pattern from `NetworkSecurityGroup`, `FirewallNetworkRuleCollection`, `RoleAssignment` properties
- Keep properties in alphabetical order by feature number (this is #039, after #026 features)

---

### Task 4: Attach ViewModel in ReportModelBuilder

**Priority:** High

**Description:**
Wire up the factory to be called when building resource change models for `azuredevops_variable_group` resources. This populates the `VariableGroup` property added in Task 3.

**Acceptance Criteria:**
- [ ] File modified: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`
- [ ] Type check added in `BuildResourceChangeModel()` method after existing ViewModel checks:
  ```csharp
  else if (string.Equals(rc.Type, "azuredevops_variable_group", StringComparison.OrdinalIgnoreCase))
  {
      model.VariableGroup = VariableGroupViewModelFactory.Build(rc, rc.ProviderName, _largeValueFormat);
  }
  ```
- [ ] Factory called with correct parameters: `ResourceChange`, `providerName`, `largeValueFormat`
- [ ] Follows existing pattern from NSG and role assignment checks

**Dependencies:** Task 2 (Factory implementation), Task 3 (ViewModel property)

**Notes:**
- The `BuildResourceChangeModel()` method already has similar type checks around line 376-383
- Add the new check in the same `if/else if` chain
- Ensure `StringComparison.OrdinalIgnoreCase` is used for type matching

---

### Task 5: Create Scriban Template

**Priority:** High

**Description:**
Create the Scriban template that renders variable group changes using the precomputed ViewModel. The template should handle create, update, and delete operations with appropriate table layouts.

**Acceptance Criteria:**
- [ ] Directory created: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azuredevops/`
- [ ] File created: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azuredevops/variable_group.sbn`
- [ ] Template includes collapsible `<details>` section with summary line using `change.summary_html`
- [ ] Template displays variable group name: `**Variable Group:** <code>{{ change.variable_group.name | escape_markdown }}</code>`
- [ ] Template displays description if present
- [ ] Template conditionally renders Key Vault Integration section when `change.variable_group.key_vault_blocks.size > 0`:
  - Section header: `#### Key Vault Integration`
  - Table columns: Name | Service Endpoint ID | Search Depth
  - Iterate `change.variable_group.key_vault_blocks`
- [ ] Template renders Variables section based on action:
  - **Update**: Table with columns: Change | Name | Value | Enabled | Content Type | Expires
    - Iterates `change.variable_group.variable_changes`
  - **Create**: Table with columns: Name | Value | Enabled | Content Type | Expires (no Change column)
    - Iterates `change.variable_group.after_variables`
  - **Delete**: Section header `#### Variables (being deleted)`, same columns as create
    - Iterates `change.variable_group.before_variables`
- [ ] Template uses `{{ value }}` directly (no escaping needed - values pre-formatted by factory)
- [ ] Template follows Report Style Guide:
  - Use `<code>` tags in `<summary>` for Azure DevOps compatibility
  - Use backticks for inline code in tables
  - Plain text for labels and headers
- [ ] Template whitespace control uses `{{~ ~}}` to avoid extra blank lines
- [ ] Template handles empty collections gracefully (no errors if arrays are empty)

**Dependencies:** Task 1 (ViewModel classes), Task 2 (Factory implementation)

**Notes:**
- Study existing templates: `Templates/azurerm/network_security_group.sbn`, `Templates/azurerm/role_assignment.sbn`
- Template resolver automatically finds templates by path: `azuredevops/variable_group.sbn` matches `azuredevops_variable_group` type
- Values are pre-formatted by factory - template just iterates and displays them
- No need to call `format_large_value()` helper - factory handles large value detection, template can reference a future extension point if needed

---

### Task 6: Create Template Tests

**Priority:** High

**Description:**
Write tests that validate the template rendering with mock ViewModels. These tests verify the template structure, table layouts, and proper handling of different operations.

**Acceptance Criteria:**
- [ ] Test file created or updated: `tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/VariableGroupTemplateTests.cs`
- [ ] Template tests implemented covering test cases from test-plan.md:
  - TC-14: Template renders create operation layout (no Change column)
  - TC-15: Template renders update operation layout with change indicators
  - TC-16: Template renders delete operation layout
  - TC-21: Template follows Report Style Guide (inline code for values, plain text for labels)
  - TC-22: Template renders Key Vault section when blocks present
  - TC-23: Template handles large values section (if implemented)
- [ ] Tests create mock `VariableGroupViewModel` objects with controlled data
- [ ] Tests render template and verify output structure:
  - Correct table headers
  - Correct number of rows
  - Change indicators present in update tables
  - Key Vault section appears before Variables section
  - HTML structure is valid (balanced tags)
- [ ] Tests verify template doesn't error on edge cases:
  - Empty variable arrays
  - Null description
  - Empty Key Vault blocks
- [ ] All tests pass consistently

**Dependencies:** Task 5 (Template implementation)

**Notes:**
- Template tests validate **structure**, not data formatting (that's tested in factory tests)
- Use `AwesomeAssertions` for fluent assertions
- Test framework is TUnit (not xUnit or NUnit)
- Tests should be fast - use minimal mock data

---

### Task 7: Integration Testing with Example Data

**Priority:** Medium

**Description:**
Validate the complete feature using actual Azure DevOps Terraform plan data from the examples directory. This ensures the factory, ViewModel, and template work correctly together.

**Acceptance Criteria:**
- [ ] Integration test created covering TC-24 from test-plan.md
- [ ] Test loads `examples/azuredevops/terraform_plan2.json` (or creates test fixture if needed)
- [ ] Test parses plan with `TerraformPlanParser`
- [ ] Test builds report with `ReportModelBuilder`
- [ ] Test extracts variable group section from rendered markdown
- [ ] Test verifies:
  - Variable group section exists in output
  - All variables from plan are displayed
  - Secret variables show `(sensitive / hidden)` for values
  - Metadata (enabled, content_type) is visible for secret variables
  - Markdown is valid (no parsing errors)
  - Output matches specification examples
- [ ] Test covering TC-25: Full report generation with mixed resource types
- [ ] Test covering TC-26: Markdown validation (no lint errors)
- [ ] All integration tests pass consistently

**Dependencies:** Task 6 (Template tests passing)

**Notes:**
- If `examples/azuredevops/terraform_plan2.json` doesn't have suitable variable group data, examine the file and either:
  - Use existing data if sufficient
  - Create a test fixture in `tests/Oocx.TfPlan2Md.TUnit/TestData/`
- Integration tests may take longer - that's acceptable
- Consider running markdown lint validation programmatically if feasible

---

### Task 8: Update Test Data Fixtures (If Needed)

**Priority:** Low

**Description:**
Create dedicated test data fixtures for edge cases and specific scenarios not covered by existing example data. This ensures comprehensive test coverage for all scenarios defined in the test plan.

**Acceptance Criteria:**
- [ ] Evaluate existing test data in `examples/azuredevops/terraform_plan2.json`
- [ ] If needed, create test fixtures in `tests/Oocx.TfPlan2Md.TUnit/TestData/`:
  - `variable-group-create.json` - create operation with mixed variables
  - `variable-group-update-mixed.json` - update with added/modified/removed/unchanged
  - `variable-group-delete.json` - delete operation
  - `variable-group-keyvault.json` - Key Vault integration
  - `variable-group-large-values.json` - large value handling
  - `variable-group-edge-cases.json` - empty arrays, null values, special characters
- [ ] All fixtures are valid Terraform plan JSON
- [ ] Fixtures cover scenarios from test-plan.md (TC test data requirements section)
- [ ] Fixtures used in unit and integration tests where appropriate

**Dependencies:** Task 7 (Integration testing to identify gaps)

**Notes:**
- Only create fixtures that are **actually needed** - don't over-engineer
- Many edge cases can be tested with inline JSON in unit tests
- Fixtures are valuable for complex, realistic scenarios
- Coordinate with existing example data to avoid duplication

---

## Implementation Order

Recommended sequence for test-first development:

1. **Task 1**: Create ViewModel classes (data structures first)
2. **Task 2**: Create Factory with tests (TDD - write tests, implement factory)
3. **Task 3**: Register ViewModel in ResourceChangeModel (quick wiring)
4. **Task 4**: Attach ViewModel in ReportModelBuilder (complete integration)
5. **Task 5**: Create Scriban template (frontend rendering)
6. **Task 6**: Add template tests (validate rendering)
7. **Task 7**: Integration testing with example data (end-to-end validation)
8. **Task 8**: Update test data fixtures if needed (fill gaps)

**Rationale:**
- Test-first approach: Write tests in Task 2, then implement factory logic
- Build from inside-out: Data structures → Logic → Integration → Rendering
- Factory tests provide confidence before touching templates
- Template tests validate structure before full integration
- Integration tests catch issues the other tests miss

**Commit Strategy:**
- Commit after each task completes and passes tests
- Use conventional commit messages: `feat(azdo-variable-group): <description>`
- Use `git commit --amend` for fixes to the same task (avoid "fix" commits)

## Test Execution Commands

Run all variable group tests:
```bash
scripts/test-with-timeout.sh -- dotnet test --project tests/Oocx.TfPlan2Md.TUnit/ -- --treenode-filter /*/*/VariableGroup*/*
```

Run factory unit tests only:
```bash
scripts/test-with-timeout.sh -- dotnet test --project tests/Oocx.TfPlan2Md.TUnit/ -- --treenode-filter /*/*/VariableGroupViewModelFactoryTests/*
```

Run template tests only:
```bash
scripts/test-with-timeout.sh -- dotnet test --project tests/Oocx.TfPlan2Md.TUnit/ -- --treenode-filter /*/*/VariableGroupTemplateTests/*
```

Run full test suite:
```bash
scripts/test-with-timeout.sh -- dotnet test
```

## Open Questions

None at this time. All implementation details are defined in the architecture document.

## Definition of Done

This feature is complete when:

- [ ] All 8 tasks completed with acceptance criteria met
- [ ] All unit tests pass (factory logic)
- [ ] All template tests pass (rendering structure)
- [ ] All integration tests pass (end-to-end)
- [ ] All existing tests still pass (no regressions)
- [ ] Test coverage >80% for ViewModel and Factory classes
- [ ] Markdown output validated (no lint errors)
- [ ] Example Azure DevOps plan renders correctly
- [ ] Changes committed to feature branch with conventional commit messages
- [ ] No fixup or "fix" commits (use `git commit --amend` for same-task fixes)
- [ ] Code follows project conventions:
  - XML documentation on all public/internal types and members
  - `InternalsVisibleTo` for test access
  - AOT-compatible (no reflection in user code paths)
  - Report Style Guide formatting applied
- [ ] Ready for handoff to Quality Engineer for UAT

## Notes for Developer

### Code Quality Expectations

- **XML Documentation**: All classes, methods, and properties must have XML doc comments
- **Nullability**: Use nullable reference types correctly (`?` for optional, `required` for mandatory)
- **Formatting**: Follow Report Style Guide - inline code for values (`` `value` ``), plain text for labels
- **Testing**: Write tests first (TDD) in Task 2 - let tests drive implementation
- **Pattern Following**: Study `NetworkSecurityGroupViewModel.cs` and `NetworkSecurityGroupViewModelFactory.cs` closely
- **AOT Compatibility**: No dynamic access, no reflection - use `JsonElement` for JSON parsing

### Helpful References

- Existing ViewModel pattern: `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/NetworkSecurityGroupViewModel*.cs`
- Existing template: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn`
- Report Style Guide: `docs/report-style-guide.md`
- Test framework patterns: Look at existing tests in `tests/Oocx.TfPlan2Md.TUnit/`

### Key Implementation Details

1. **Secret Detection**: Variables from `secret_variable` array → value formatted as `(sensitive / hidden)`
2. **Large Value Detection**: Regular variables only, check length >100 or contains `\n`
3. **Semantic Matching**: Match variables by `name` attribute across before/after states
4. **Diff Formatting**: Changed attributes use `` - `old`<br>+ `new` ``, unchanged use `` `value` ``
5. **Empty Values**: Format as `-` (plain text, no backticks)
6. **Replace Action**: Treat `["delete", "create"]` same as `"update"` for variable group rendering

### Common Pitfalls to Avoid

- ❌ Don't use reflection or dynamic access (AOT incompatible)
- ❌ Don't format values in template (factory handles all formatting)
- ❌ Don't forget to mark secret variables even if they're long (secrets never large-value)
- ❌ Don't create "fix" commits - use `git commit --amend` for same-task corrections
- ❌ Don't skip XML documentation (required for all members)
- ❌ Don't hardcode formatting strings - reference Report Style Guide patterns
