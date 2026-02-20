# Tasks: Azure DevOps Build Definition Nested Block Tables

## Overview

Implement specialized table rendering for `azuredevops_build_definition` Terraform resource nested blocks (variables, CI triggers, repository, etc.) following the exact pattern established by `azuredevops_variable_group` (Feature 027/039).

**Reference Documents:**
- [Feature Specification](specification.md)
- [Architecture Document](architecture.md)
- [Test Plan](test-plan.md)

**Pattern Reference:** `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroup*.cs` and related files

## Tasks

### Task 1: Create BuildDefinitionViewModel

**Priority:** High

**Description:**
Create the view model classes that define the typed data structures for the Scriban template. This includes the main `BuildDefinitionViewModel` and all nested row view models for variables, CI triggers, repositories, etc.

**Acceptance Criteria:**
- [ ] Create `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionViewModel.cs`
- [ ] Define `BuildDefinitionViewModel` class with properties for:
  - Metadata: `Name`, `Path`, `AgentPoolName`, `QueueStatus`
  - Variables: `VariableChanges`, `AfterVariables`, `BeforeVariables`
  - Other blocks: `AfterCiTriggers`, `BeforeCiTriggers`, `AfterPullRequestTriggers`, `BeforePullRequestTriggers`, `AfterSchedules`, `BeforeSchedules`, `AfterRepositories`, `BeforeRepositories`, `AfterJobs`, `BeforeJobs`
- [ ] Define `BuildDefinitionVariableChangeRowViewModel` with properties:
  - `Change`, `ChangeIcon`, `Name`, `Value`, `IsSecret`, `AllowOverride`, `IsLargeValue`
- [ ] Define `BuildDefinitionVariableRowViewModel` with properties:
  - `Name`, `Value`, `IsSecret`, `AllowOverride`, `IsLargeValue`
- [ ] Define `CiTriggerRowViewModel` with properties:
  - `UseYaml`, `Override` (formatted branch filters)
- [ ] Define `PullRequestTriggerRowViewModel` with properties:
  - `UseYaml`, `Override`, `ForksEnabled`, `ForksCommentRequirement`
- [ ] Define `ScheduleRowViewModel` with properties:
  - `BranchFilters`, `DaysToBuild`, `ScheduleOnlyWithChanges`, `StartTime`, `TimeZone`
- [ ] Define `RepositoryRowViewModel` with properties:
  - `RepoType`, `RepoId`, `BranchName`, `YmlPath`, `ReportBuildStatus`, `ServiceConnectionId`, `GithubEnterpriseUrl`
- [ ] Define `JobRowViewModel` with properties:
  - `Name`, `Condition`, `TimeoutInMinutes`
- [ ] All properties use `required` or `init` appropriately
- [ ] All classes include XML documentation comments
- [ ] All lists use `IReadOnlyList<T>` with default empty arrays
- [ ] Follow the exact pattern from `VariableGroupViewModel.cs`

**Dependencies:** None

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupViewModel.cs`
- Use nullable reference types for optional metadata (`string?`)
- The view model is purely data transfer - no logic

**Test Requirements:**
- No unit tests for view models (they are simple data structures)
- Will be tested through factory tests in Task 3

---

### Task 2: Create BuildDefinitionExtractors

**Priority:** High

**Description:**
Create extractor functions that parse `JsonElement` data from Terraform JSON to extract build definition attributes and nested blocks. This includes extracting variables, CI triggers, repositories, and other nested blocks.

**Acceptance Criteria:**
- [ ] Create `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionExtractors.cs`
- [ ] Implement metadata extractors:
  - `ExtractName(object? state)` → `string?`
  - `ExtractPath(object? state)` → `string?`
  - `ExtractAgentPoolName(object? state)` → `string?`
  - `ExtractQueueStatus(object? state)` → `string?`
- [ ] Implement variable extractor:
  - `ExtractVariables(object? state)` → `IReadOnlyList<BuildDefinitionVariableValues>`
  - Parse `variable` array from JSON
  - Extract: `name`, `value`, `is_secret`, `allow_override`, `secret_value`
- [ ] Implement nested block extractors:
  - `ExtractCiTriggers(object? state)` → `IReadOnlyList<CiTriggerValues>`
  - `ExtractPullRequestTriggers(object? state)` → `IReadOnlyList<PullRequestTriggerValues>`
  - `ExtractSchedules(object? state)` → `IReadOnlyList<ScheduleValues>`
  - `ExtractRepositories(object? state)` → `IReadOnlyList<RepositoryValues>`
  - `ExtractJobs(object? state)` → `IReadOnlyList<JobValues>` (optional - typically empty)
- [ ] Define internal value classes (e.g., `BuildDefinitionVariableValues`, `CiTriggerValues`, etc.)
- [ ] Handle null/missing values gracefully (return empty arrays or null as appropriate)
- [ ] Use `JsonElement` parsing with proper error handling
- [ ] Follow the exact pattern from `VariableGroupExtractors.cs`

**Dependencies:** Task 1 (for value class definitions)

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupExtractors.cs`
- Use `JsonElementExtensions` helper methods if available
- For `variable` array: each element has `name`, `value`, `is_secret`, `allow_override`, `secret_value`
- For `ci_trigger` array: each element has `use_yaml`, `override` (array of strings)
- For `repository` array: each element has `repo_type`, `repo_id`, `branch_name`, `yml_path`, `report_build_status`, etc.

**Test Requirements:**
- Will be tested indirectly through factory tests in Task 3
- Focus on null safety and edge cases (empty arrays, missing fields)

---

### Task 3: Create BuildDefinitionFormatters

**Priority:** High

**Description:**
Create formatter functions that convert extracted values into display-ready strings with proper Markdown escaping and code formatting. **Critical:** Implement secret masking logic to ensure `is_secret: true` variables always display `(sensitive / hidden)` instead of actual values.

**Acceptance Criteria:**
- [ ] Create `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionFormatters.cs`
- [ ] Implement variable value formatter with **SECRET MASKING**:
  - `FormatVariableValue(BuildDefinitionVariableValues variable)` → `string`
  - If `is_secret: true`, return `"(sensitive / hidden)"` - **NEVER show actual value**
  - If `is_secret: false`, format and escape the `value` attribute
  - Handle empty/null values: return `` `-` ``
- [ ] Implement attribute formatters:
  - `FormatBoolean(bool? value)` → format as `` `true` `` or `` `false` ``
  - `FormatOptionalString(string? value)` → format with backticks or return `` `-` ``
  - `FormatBranchFilters(IReadOnlyList<string>? filters)` → comma-separated list with backticks
  - `FormatTime(int? hours, int? minutes)` → format as `HH:MM` or `` `-` ``
  - `FormatDaysList(IReadOnlyList<string>? days)` → comma-separated list
- [ ] Implement row creators for variables:
  - `CreateVariableRow(BuildDefinitionVariableValues variable)` → `BuildDefinitionVariableRowViewModel`
  - `CreateAddedVariableRow(BuildDefinitionVariableValues variable)` → with `Change = "add"`, `ChangeIcon = "➕"`
  - `CreateRemovedVariableRow(BuildDefinitionVariableValues variable)` → with `Change = "remove"`, `ChangeIcon = "❌"`
  - `CreateModifiedVariableRow(BuildDefinitionVariableValues before, BuildDefinitionVariableValues after, LargeValueFormat format)` → with `Change = "update"`, `ChangeIcon = "🔄"`
  - `CreateUnchangedVariableRow(BuildDefinitionVariableValues variable)` → with `Change = "unchanged"`, `ChangeIcon = "⏺️"`
- [ ] Implement row creators for other blocks:
  - `CreateCiTriggerRow(CiTriggerValues trigger)` → `CiTriggerRowViewModel`
  - `CreatePullRequestTriggerRow(PullRequestTriggerValues trigger)` → `PullRequestTriggerRowViewModel`
  - `CreateScheduleRow(ScheduleValues schedule)` → `ScheduleRowViewModel`
  - `CreateRepositoryRow(RepositoryValues repo)` → `RepositoryRowViewModel`
  - `CreateJobRow(JobValues job)` → `JobRowViewModel`
- [ ] For modified variable rows, show before/after values with `-` and `+` prefixes for changed attributes
- [ ] For modified variable rows, show single value without prefix for unchanged attributes
- [ ] Handle large values: set `IsLargeValue = true` if value length > 100 chars or contains newlines (non-secret variables only)
- [ ] Properly escape Markdown in all string values
- [ ] Follow Report Style Guide: data values in code formatting (backticks), labels in plain text
- [ ] Follow the exact pattern from `VariableGroupFormatters.cs`

**Dependencies:** Task 2 (extractors)

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupFormatters.cs`
- **Security Critical:** The secret masking logic is the most important part - verify multiple times
- For modified variables where `is_secret` changes from false to true: always show `(sensitive / hidden)` in Value column
- Large value handling: only for non-secret variables; secret variables are never "large"
- Use existing `EscapeMarkdown` helper if available

**Test Requirements:**
- TC-02: Secret variables mask values (unit test)
- TC-04: Secret variable metadata displayed but values masked
- TC-17: Security - secret values never leaked in any scenario
- Will be tested through factory tests in Task 4

---

### Task 4: Create BuildDefinitionChangeBuilders

**Priority:** High

**Description:**
Create change builder functions that perform semantic diffing for variables by matching them by `name` attribute (not array index). This ensures that reordering variables doesn't create false change indicators.

**Acceptance Criteria:**
- [ ] Create `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionChangeBuilders.cs`
- [ ] Implement `BuildAdded(IReadOnlyList<BuildDefinitionVariableValues> afterVariables, IReadOnlyList<BuildDefinitionVariableValues> beforeVariables)`:
  - Find variables in `after` but not in `before` (match by `name`, case-insensitive)
  - Sort by name (case-ordinal)
  - Map to `BuildDefinitionVariableChangeRowViewModel` using `CreateAddedVariableRow`
- [ ] Implement `BuildRemoved(IReadOnlyList<BuildDefinitionVariableValues> afterVariables, IReadOnlyList<BuildDefinitionVariableValues> beforeVariables)`:
  - Find variables in `before` but not in `after` (match by `name`, case-insensitive)
  - Sort by name
  - Map to `BuildDefinitionVariableChangeRowViewModel` using `CreateRemovedVariableRow`
- [ ] Implement `BuildModified(IReadOnlyList<BuildDefinitionVariableValues> afterVariables, IReadOnlyList<BuildDefinitionVariableValues> beforeVariables, LargeValueFormat largeValueFormat)`:
  - Find variables in both `before` and `after` (match by `name`, case-insensitive)
  - Compare all attributes (`value`, `is_secret`, `allow_override`)
  - Only include if at least one attribute changed
  - Sort by name
  - Map to `BuildDefinitionVariableChangeRowViewModel` using `CreateModifiedVariableRow`
- [ ] Implement `BuildUnchanged(IReadOnlyList<BuildDefinitionVariableValues> afterVariables, IReadOnlyList<BuildDefinitionVariableValues> beforeVariables)`:
  - Find variables in both `before` and `after` with no changes
  - Sort by name
  - Map to `BuildDefinitionVariableChangeRowViewModel` using `CreateUnchangedVariableRow`
- [ ] Use `HashSet<string>` with `StringComparer.OrdinalIgnoreCase` for name matching
- [ ] Sort results using `StringComparer.Ordinal` (case-sensitive sort for display)
- [ ] Follow the exact pattern from `VariableGroupChangeBuilders.cs`

**Dependencies:** Task 3 (formatters)

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupChangeBuilders.cs`
- Semantic diffing is critical for accurate change detection
- Match variables case-insensitively, but sort case-sensitively for consistent display
- For other blocks (CI trigger, repository), semantic diffing is not needed - just show before/after

**Test Requirements:**
- TC-06: Variables categorized as Added (unit test)
- TC-07: Variables categorized as Modified (unit test)
- TC-08: Variables categorized as Removed (unit test)
- TC-09: Variables categorized as Unchanged (unit test)
- TC-15: Update operation with added/modified/removed variables (integration test)

---

### Task 5: Create BuildDefinitionViewModelFactory

**Priority:** High

**Description:**
Create the factory class that orchestrates extractors, formatters, and change builders to produce a complete `BuildDefinitionViewModel` from a `ResourceChange`. This is the main orchestration point that determines the operation type (create/update/delete) and builds the appropriate view model.

**Acceptance Criteria:**
- [ ] Create `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionViewModelFactory.cs`
- [ ] Implement `Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat)` → `BuildDefinitionViewModel`
- [ ] Extract metadata from `change.Change.After` or `change.Change.Before` (fallback)
- [ ] Extract all nested blocks using extractors (variables, CI triggers, repositories, etc.)
- [ ] Determine operation type from `change.Change.Actions`:
  - `actions.Contains("create") && !actions.Contains("delete")` → create operation
  - `actions.Contains("delete") && !actions.Contains("create")` → delete operation
  - Otherwise → update operation
- [ ] For **create operation**:
  - Populate `AfterVariables`, `AfterCiTriggers`, `AfterRepositories`, etc. using row creators
  - Leave `BeforeVariables`, `VariableChanges` empty
- [ ] For **delete operation**:
  - Populate `BeforeVariables`, `BeforeCiTriggers`, `BeforeRepositories`, etc. using row creators
  - Leave `AfterVariables`, `VariableChanges` empty
- [ ] For **update operation**:
  - Populate `VariableChanges` using change builders (added, removed, modified, unchanged - concatenated)
  - Populate `AfterCiTriggers`, `BeforeCiTriggers`, etc. for simple before/after display
  - Leave `AfterVariables`, `BeforeVariables` empty
- [ ] Handle null states gracefully (e.g., `change.Change.After` may be null for delete)
- [ ] Follow the exact pattern from `VariableGroupViewModelFactory.cs`

**Dependencies:** Task 1, 2, 3, 4 (all previous components)

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupViewModelFactory.cs`
- This is the central orchestration point - all other classes feed into this
- For variables: use semantic diffing (change builders) in update operations
- For other blocks: simple before/after arrays (no semantic diffing)

**Test Requirements:**
- TC-01: Create operation with regular variables (unit test)
- TC-02: Create operation with secret variables (unit test)
- TC-03: Delete operation (unit test)
- TC-05: Update operation - variable changes (unit test)
- TC-06-09: Semantic diffing tests (unit test)
- TC-10: Large variable values (unit test)
- TC-11: CI Trigger, Repository, and other blocks (unit test)
- TC-12: Empty/null attribute values (unit test)
- TC-13: Conditional rendering data (unit test)

---

### Task 6: Update Factories.cs with BuildDefinitionFactory

**Priority:** High

**Description:**
Add the `BuildDefinitionFactory` adapter class to `Factories.cs` following the pattern of `VariableGroupFactory`. This adapter implements `IResourceViewModelFactory` and provides the `CreateViewModel` method for use by the mapper.

**Acceptance Criteria:**
- [ ] Open `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/Factories.cs`
- [ ] Add `BuildDefinitionFactory` class after `VariableGroupFactory`
- [ ] Implement `IResourceViewModelFactory` interface
- [ ] Add constructor accepting `LargeValueFormat largeValueFormat`
- [ ] Implement `ApplyViewModel` method (leave empty - view model created on-demand)
- [ ] Implement `CreateViewModel(ResourceChange resourceChange)` method:
  - Call `BuildDefinitionViewModelFactory.Build(resourceChange, resourceChange.ProviderName, _largeValueFormat)`
  - Return the created view model
- [ ] Add XML documentation comments
- [ ] Mark class as `internal sealed`
- [ ] Follow the exact pattern from `VariableGroupFactory`

**Dependencies:** Task 5 (factory)

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/Factories.cs` (lines 15-58)
- This is a simple adapter - minimal logic
- The factory adapter pattern allows lazy view model creation

**Test Requirements:**
- No direct tests - tested through mapper tests (Task 7)

---

### Task 7: Create BuildDefinitionMapper

**Priority:** High

**Description:**
Create the mapper class that implements `IResourceModelMapper` to enrich the ScriptObject with build definition view model data. This makes the view model available to the Scriban template as `change.build_definition`.

**Acceptance Criteria:**
- [ ] Create `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Mappers/BuildDefinitionMapper.cs`
- [ ] Implement `IResourceModelMapper` interface
- [ ] Add constructor accepting `BuildDefinitionFactory factory` (with null check)
- [ ] Implement `CanMap(ResourceChangeModel resource)`:
  - Return `resource.Type == "azuredevops_build_definition"`
- [ ] Implement `EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject)`:
  - Return early if `resource.ResourceChange == null`
  - Call `_factory.CreateViewModel(resource.ResourceChange)` to get view model
  - Call `MapBuildDefinition(viewModel)` to convert to ScriptObject
  - Set `scriptObject["build_definition"] = result`
- [ ] Implement private `MapBuildDefinition(BuildDefinitionViewModel bd)` → `ScriptObject`:
  - Create new `ScriptObject` with metadata properties
  - Map `VariableChanges` to `ScriptArray` of `ScriptObject` items
  - Map `AfterVariables` to `ScriptArray` of `ScriptObject` items
  - Map `BeforeVariables` to `ScriptArray` of `ScriptObject` items
  - Map all nested block arrays (CI triggers, repositories, etc.) to `ScriptArray` items
  - Each row view model becomes a `ScriptObject` with snake_case property names
- [ ] Mark class as `internal sealed`
- [ ] Add XML documentation comments
- [ ] Follow the exact pattern from `VariableGroupMapper.cs`

**Dependencies:** Task 6 (factory adapter)

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Mappers/VariableGroupMapper.cs`
- Property names in ScriptObject use snake_case (e.g., `change_icon`, `is_secret`)
- The mapper is invoked by the template rendering engine before template execution

**Test Requirements:**
- TC-14: Template integration for create operation (integration test)
- TC-15: Template integration for update operation (integration test)
- TC-16: Template integration for delete operation (integration test)
- Tested through end-to-end template rendering tests

---

### Task 8: Create build_definition.sbn Template

**Priority:** High

**Description:**
Create the Scriban template that renders the build definition as collapsible HTML details with structured tables for variables and other nested blocks. The template must implement conditional rendering (only show tables when data exists).

**Acceptance Criteria:**
- [ ] Create `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/build_definition.sbn`
- [ ] Add template header comment explaining purpose and view model usage
- [ ] Render details summary with `{{ change.summary_html }}`
- [ ] Include code analysis metadata: `{{~ include "/_code_analysis_metadata.sbn" ~}}`
- [ ] Display metadata prominently:
  - **Pipeline Name:** `{{ format_code_summary(change.build_definition.name) }}`
  - **Path:** `{{ format_code_summary(change.build_definition.path) }}`
  - **Agent Pool:** (if present)
- [ ] **Variables section** - conditional rendering based on action:
  - **Create:** Show `after_variables` table with columns: Name, Value, Is Secret, Allow Override
  - **Delete:** Show `before_variables` table with "(being deleted)" label
  - **Update:** Show `variable_changes` table with columns: Change, Name, Value, Is Secret, Allow Override
  - Only render section if array size > 0
- [ ] **CI Trigger section** - conditional rendering:
  - Show `after_ci_triggers` table with columns: Use YAML, Override (Branch Filters)
  - Only render if `after_ci_triggers.size > 0`
- [ ] **Pull Request Trigger section** - conditional rendering:
  - Show `after_pull_request_triggers` table
  - Only render if array size > 0
- [ ] **Schedules section** - conditional rendering:
  - Show `after_schedules` table
  - Only render if array size > 0
- [ ] **Repository section** - conditional rendering:
  - Show `after_repositories` table with columns: Type, Repo ID, Branch, YAML Path, Report Build Status
  - Only render if array size > 0
- [ ] **Jobs section** - conditional rendering:
  - Show `after_jobs` table if array size > 0
- [ ] Use `details_open_attr(change)` for collapsible details
- [ ] Use proper table markdown formatting with aligned columns
- [ ] Follow Report Style Guide: no inline styles, clean table formatting
- [ ] Follow the exact pattern from `variable_group.sbn`

**Dependencies:** Task 7 (mapper)

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/variable_group.sbn`
- Template location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/build_definition.sbn`
- The template file must be set as an embedded resource in the `.csproj` file
- Conditional rendering is critical: `{{ if change.build_definition && change.build_definition.after_variables.size > 0 }}`
- For update operations, show before/after for non-variable blocks (not semantic diffing)

**Test Requirements:**
- TC-14: Create operation rendering (integration test)
- TC-15: Update operation rendering (integration test)
- TC-16: Delete operation rendering (integration test)
- TC-18: CI Trigger display (integration test)
- TC-19: Pull Request Trigger display (integration test)
- TC-20: Schedules and Repository display (integration test)
- TC-21: Conditional rendering - no empty tables (integration test)
- TC-22: Report Style Guide compliance (integration test)

---

### Task 9: Update AzureDevOpsModule.cs Registration

**Priority:** High

**Description:**
Register the `BuildDefinitionFactory` and `BuildDefinitionMapper` in the `AzureDevOpsModule` dependency injection configuration so they are used during template rendering.

**Acceptance Criteria:**
- [ ] Open `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs`
- [ ] In `RegisterFactories` method, add:
  ```csharp
  registry.RegisterFactory("azuredevops_build_definition", new BuildDefinitionFactory(_largeValueFormat));
  ```
- [ ] In `RegisterResourceModelMappers` method, add:
  ```csharp
  var buildDefinitionFactory = new BuildDefinitionFactory(_largeValueFormat);
  registry.Register(new Mappers.BuildDefinitionMapper(buildDefinitionFactory));
  ```
- [ ] Ensure proper using statements are added at the top
- [ ] Follow the exact pattern used for `VariableGroupFactory` and `VariableGroupMapper`

**Dependencies:** Task 7 (mapper), Task 6 (factory)

**Notes:**
- Reference: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs` (lines 102-104, 230-233)
- This is the final integration point that wires everything together
- Without this registration, the template won't be found and view model won't be populated

**Test Requirements:**
- TC-23: Mapper registered in dependency injection (integration test)
- All integration tests verify this registration

---

### Task 10: Add Template to .csproj Embedded Resources

**Priority:** High

**Description:**
Ensure the `build_definition.sbn` template file is included as an embedded resource in the project file so it can be loaded at runtime.

**Acceptance Criteria:**
- [ ] Open `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj`
- [ ] Verify or add `<ItemGroup>` section with embedded resources
- [ ] Add entry for the template:
  ```xml
  <EmbeddedResource Include="Providers/AzureDevOps/Templates/azuredevops/build_definition.sbn" />
  ```
- [ ] Ensure the path matches the actual file location
- [ ] Follow the pattern used for `variable_group.sbn`

**Dependencies:** Task 8 (template)

**Notes:**
- Check existing `.csproj` file to see if there's already a pattern for embedded resources
- The embedded resource path must match the `TemplateResourcePrefix` in `AzureDevOpsModule`
- Template files are loaded via reflection at runtime

**Test Requirements:**
- All integration tests verify template loading
- TC-14, TC-15, TC-16 will fail if template is not embedded

---

### Task 11: Create Unit Tests for BuildDefinitionViewModelFactory

**Priority:** High

**Description:**
Implement comprehensive unit tests for the `BuildDefinitionViewModelFactory` covering all operation types (create, update, delete), variable types (regular, secret), semantic diffing, and edge cases.

**Acceptance Criteria:**
- [ ] Create test file: `tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/BuildDefinitionViewModelFactoryTests.cs`
- [ ] Implement TC-01: Create operation with regular variables
- [ ] Implement TC-02: Create operation with secret variables (verify `(sensitive / hidden)`)
- [ ] Implement TC-03: Delete operation
- [ ] Implement TC-04: Secret variable metadata displayed but values masked
- [ ] Implement TC-05: Update operation with variable changes
- [ ] Implement TC-06: Variables categorized as Added
- [ ] Implement TC-07: Variables categorized as Modified (with before/after diffs)
- [ ] Implement TC-08: Variables categorized as Removed
- [ ] Implement TC-09: Variables categorized as Unchanged
- [ ] Implement TC-10: Large variable values flagged with `IsLargeValue = true`
- [ ] Implement TC-11: CI Trigger, Repository, and other nested blocks extraction
- [ ] Implement TC-12: Empty/null attribute values formatted as `-`
- [ ] Implement TC-13: Conditional rendering data (empty arrays result in empty view model lists)
- [ ] All tests use inline JSON test data or builder pattern
- [ ] All tests verify secret masking (no actual secret values in output)
- [ ] Follow the test structure pattern from existing tests

**Dependencies:** Task 5 (factory), Task 1-4 (supporting classes)

**Notes:**
- Reference test plan section starting at TC-01
- Use TUnit framework (not xUnit or NUnit)
- Test data should be inline JSON or use a builder pattern for clarity
- Focus on edge cases: null values, empty arrays, missing fields
- Security tests are critical: verify secret values never appear in output

**Test Requirements:**
- Covers TC-01 through TC-13 from test plan

---

### Task 12: Create Integration Tests for Template Rendering

**Priority:** High

**Description:**
Implement integration tests that verify end-to-end template rendering for build definitions, including all operation types and conditional rendering scenarios.

**Acceptance Criteria:**
- [ ] Create test file: `tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/BuildDefinitionTemplateTests.cs`
- [ ] Implement TC-14: Create operation template rendering
  - Verify markdown structure
  - Verify variables table with proper columns
  - Verify CI Trigger and Repository sections
- [ ] Implement TC-15: Update operation template rendering
  - Verify variables table includes Change column
  - Verify change icons (➕, 🔄, ❌, ⏺️) appear correctly
  - Verify before/after diffs for modified variables
- [ ] Implement TC-16: Delete operation template rendering
  - Verify "(being deleted)" label
  - Verify before_variables table
- [ ] Implement TC-17: Security - secret values never leaked (integration test)
  - Create build definition with secret variables
  - Verify rendered output contains `(sensitive / hidden)`
  - Verify rendered output does NOT contain actual secret values
- [ ] Implement TC-18: CI Trigger display
- [ ] Implement TC-19: Pull Request Trigger display
- [ ] Implement TC-20: Schedules and Repository display
- [ ] Implement TC-21: Conditional rendering - no empty tables
  - Create build definition with only variables (no triggers)
  - Verify only Variables section is rendered
  - Verify CI Trigger, Repository sections are NOT rendered
- [ ] Implement TC-22: Report Style Guide compliance
  - Verify code formatting (backticks) for values
  - Verify plain text for labels
  - Verify table structure
- [ ] Implement TC-23: Mapper registered in dependency injection
- [ ] All tests use full rendering pipeline (factory → mapper → template)
- [ ] All tests verify markdown output matches expected structure

**Dependencies:** Task 8 (template), Task 9 (registration), Task 10 (embedded resource)

**Notes:**
- Reference test plan sections TC-14 through TC-23
- Integration tests should use the full rendering pipeline
- Verify HTML details structure, table headers, and conditional rendering
- Use snapshot testing if available, otherwise string assertions
- Security test (TC-17) is critical: must verify no secret leakage

**Test Requirements:**
- Covers TC-14 through TC-23 from test plan

---

### Task 13: Create Test Data File

**Priority:** Medium

**Description:**
Create test data JSON file with realistic build definition scenarios to support unit and integration tests. This file should include examples of create, update, and delete operations with various nested blocks.

**Acceptance Criteria:**
- [ ] Create `tests/Oocx.TfPlan2Md.TUnit/TestData/azuredevops-build-definitions.json`
- [ ] Include test scenario 1: Create with regular variables
- [ ] Include test scenario 2: Create with secret variables
- [ ] Include test scenario 3: Update with added/modified/removed variables
- [ ] Include test scenario 4: Delete with variables
- [ ] Include test scenario 5: Build definition with CI trigger and repository
- [ ] Include test scenario 6: Build definition with all nested blocks (PR trigger, schedules, jobs)
- [ ] Each scenario includes proper `before`, `after`, `before_sensitive`, `after_sensitive` structure
- [ ] Follow the JSON structure from `examples/azuredevops/terraform_plan2.json`
- [ ] Include realistic data (GUIDs, branch names, etc.)

**Dependencies:** None (can be done in parallel)

**Notes:**
- Reference: `examples/azuredevops/terraform_plan2.json` for structure
- Test plan specifies 6 test scenarios
- This file can be used by both unit and integration tests
- Include edge cases: empty arrays, null values, missing fields

**Test Requirements:**
- Supports test data requirements for TC-01 through TC-23

---

### Task 14: Update DemoPaths.cs

**Priority:** Low

**Description:**
Add a new demo path entry for the Azure DevOps build definition test data file to support demo and testing workflows.

**Acceptance Criteria:**
- [ ] Open `tests/Oocx.TfPlan2Md.TUnit/TestData/DemoPaths.cs`
- [ ] Add property: `public static string AzureDevOpsBuildDefinitionPlanPath => ...;`
- [ ] Path should point to the test data file created in Task 13
- [ ] Follow the pattern of existing demo path entries
- [ ] Add XML documentation comment

**Dependencies:** Task 13 (test data file)

**Notes:**
- Reference: `tests/Oocx.TfPlan2Md.TUnit/TestData/DemoPaths.cs`
- This is a simple addition for test infrastructure
- Low priority - can be done after tests are working

**Test Requirements:**
- No specific test requirements - infrastructure only

---

### Task 15: Create UAT Artifacts

**Priority:** Medium

**Description:**
Create the UAT test artifacts (JSON and rendered Markdown) that demonstrate the build definition table rendering for Maintainer review in GitHub/Azure DevOps PR comments.

**Acceptance Criteria:**
- [ ] Create `docs/features/094-build-definition-tables/uat-plan.json`
  - Include realistic build definition with variables (regular and secret)
  - Include CI trigger configuration
  - Include repository configuration
  - Include at least one variable change scenario (added/modified/removed)
- [ ] Generate `docs/features/094-build-definition-tables/uat-plan.md` using tfplan2md
  - Run: `tfplan2md --input uat-plan.json --output uat-plan.md`
  - Verify variables are displayed in table format
  - Verify secret variables show `(sensitive / hidden)`
  - Verify CI trigger and repository sections appear
  - Verify conditional rendering (no empty tables)
- [ ] Follow the UAT test plan guidelines from `uat-test-plan.md`
- [ ] Ensure rendered output demonstrates all key features

**Dependencies:** Task 8, 9, 10 (template and registration must be complete)

**Notes:**
- Reference: `docs/features/094-build-definition-tables/uat-test-plan.md`
- UAT artifacts are for visual review by Maintainer in PR comments
- Should demonstrate the feature's value proposition clearly
- Medium priority - can be done after core implementation

**Test Requirements:**
- No automated tests - manual validation by Maintainer

---

### Task 16: Run All Tests and Verify

**Priority:** High

**Description:**
Run the complete test suite to verify all tests pass, including existing tests (regression check) and new tests for build definition rendering.

**Acceptance Criteria:**
- [ ] Run: `dotnet test` (or equivalent test command)
- [ ] All existing tests continue to pass (no regressions)
- [ ] All new build definition tests pass (TC-01 through TC-23)
- [ ] No test failures or warnings
- [ ] Code coverage includes all new components (extractors, formatters, builders, factory, mapper)
- [ ] Fix any test failures before proceeding

**Dependencies:** Task 11, 12, 13 (all tests created)

**Notes:**
- This is a verification step before committing
- If tests fail, debug and fix before moving forward
- Check for any warnings or skipped tests
- Verify code coverage of new code

**Test Requirements:**
- All tests from test plan (TC-01 through TC-23)

---

## Implementation Order

Recommended sequence for implementation:

1. **Task 1** - Create BuildDefinitionViewModel (foundation for all other code)
2. **Task 2** - Create BuildDefinitionExtractors (parse Terraform JSON)
3. **Task 3** - Create BuildDefinitionFormatters (format values, secret masking)
4. **Task 4** - Create BuildDefinitionChangeBuilders (semantic diffing)
5. **Task 5** - Create BuildDefinitionViewModelFactory (orchestrate all components)
6. **Task 6** - Update Factories.cs with BuildDefinitionFactory (adapter)
7. **Task 7** - Create BuildDefinitionMapper (ScriptObject enrichment)
8. **Task 8** - Create build_definition.sbn Template (visual rendering)
9. **Task 10** - Add Template to .csproj Embedded Resources (enable template loading)
10. **Task 9** - Update AzureDevOpsModule.cs Registration (wire everything together)
11. **Task 13** - Create Test Data File (can be done in parallel with Tasks 1-10)
12. **Task 11** - Create Unit Tests for BuildDefinitionViewModelFactory
13. **Task 12** - Create Integration Tests for Template Rendering
14. **Task 16** - Run All Tests and Verify
15. **Task 15** - Create UAT Artifacts (after tests pass)
16. **Task 14** - Update DemoPaths.cs (cleanup task)

**Rationale:**
- Tasks 1-5 build the core data processing pipeline in dependency order
- Tasks 6-10 integrate with the framework (registration and template)
- Task 13 (test data) can be done in parallel with implementation
- Tasks 11-12 (tests) verify the implementation
- Task 16 (verification) is a checkpoint before UAT
- Tasks 15, 14 are final cleanup/documentation tasks

## Open Questions

None at this time. All questions from the specification have been answered in the architecture document.

## Notes for Developer

- **Follow the Pattern:** The `azuredevops_variable_group` implementation is your complete reference. When in doubt, check how variable group does it.
- **Security First:** Secret masking is critical - verify multiple times that `is_secret: true` variables never leak actual values.
- **Test as You Go:** Don't wait until the end to test - verify each component as you build it.
- **Incremental Commits:** Commit after completing each task (or logical group of tasks) to preserve progress.
- **UAT Last:** Create UAT artifacts after all tests pass - this ensures you're demonstrating working functionality.

## Definition of Done

Implementation is complete when:
- [ ] All 16 tasks are completed
- [ ] All unit tests pass (TC-01 through TC-13)
- [ ] All integration tests pass (TC-14 through TC-23)
- [ ] Existing tests continue to pass (no regressions)
- [ ] Secret masking verified (no actual secret values in output)
- [ ] Conditional rendering verified (no empty tables)
- [ ] UAT artifacts created and demonstrate feature value
- [ ] Code follows existing patterns (variable group reference)
- [ ] All files committed to feature branch
