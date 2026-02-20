# Test Plan: Azure DevOps Build Definition Nested Block Tables

## Overview

This test plan verifies the implementation of specialized table rendering for `azuredevops_build_definition` Terraform resource nested blocks, following the pattern established by `azuredevops_variable_group` (Feature 027/039).

**Reference Documents:**
- Feature Specification: [specification.md](specification.md)
- Architecture Document: [architecture.md](architecture.md)
- Testing Strategy: [docs/testing-strategy.md](../../testing-strategy.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Template created for `azuredevops_build_definition` | TC-01, TC-14, TC-15, TC-16 | Integration/Template |
| ViewModel and Mapper classes created | TC-02 through TC-13 | Unit |
| Variables displayed in table format | TC-01, TC-03, TC-14, TC-15 | Unit/Integration |
| Secret variables show metadata but mask values | TC-04, TC-05, TC-17 | Unit/Integration |
| Variables categorized (Added/Modified/Removed/Unchanged) | TC-06, TC-07, TC-08, TC-09, TC-15 | Unit |
| Large variable values handled correctly | TC-10 | Unit |
| Modified variables show before/after with prefixes | TC-07, TC-15 | Unit/Integration |
| Unchanged attributes show single value | TC-07 | Unit |
| CI Trigger block displayed as table | TC-11, TC-18 | Unit/Integration |
| Pull Request Trigger block displayed as table | TC-11, TC-19 | Unit/Integration |
| Schedules block displayed as table | TC-11, TC-20 | Unit/Integration |
| Repository block displayed as table | TC-11, TC-18, TC-19, TC-20 | Unit/Integration |
| Jobs block displayed if populated | TC-11 | Unit/Integration |
| Empty/null attributes displayed as `-` | TC-12 | Unit |
| Conditional rendering (no empty tables) | TC-13, TC-21 | Integration |
| Create/Update/Delete operations have appropriate layouts | TC-14, TC-15, TC-16 | Integration |
| Build definition metadata displayed prominently | TC-14, TC-15, TC-16 | Integration |
| Template follows Report Style Guide | TC-22 | Integration |
| Mapper registered in dependency injection | TC-23 | Integration |

## Test Cases

### Unit Tests - BuildDefinitionViewModelFactory

#### TC-01: Create Operation - Regular Variables

**Type:** Unit

**Description:**
Verifies that regular (non-secret) variables are correctly formatted for create operations, including proper formatting of name, value, is_secret, and allow_override attributes.

**Preconditions:**
- BuildDefinitionViewModelFactory class exists
- Test data with create action and regular variables

**Test Steps:**
1. Create ResourceChange with `actions: ["create"]` and `after` state containing:
   - Variable 1: `BUILD_CONFIGURATION` = `"Release"`, `is_secret: false`, `allow_override: true`
   - Variable 2: `BUILD_PLATFORM` = `"Any CPU"`, `is_secret: false`, `allow_override: false`
   - Variable 3: `TIMEOUT` = `"30"`, `is_secret: false`, `allow_override: null`
2. Call `BuildDefinitionViewModelFactory.Build(change, "azuredevops", LargeValueFormat.InlineDiff)`
3. Assert the view model properties

**Expected Result:**
- `viewModel.AfterVariables` has count 3
- `viewModel.VariableChanges` is empty (no changes for create)
- `viewModel.BeforeVariables` is empty
- Variable 1: Name=`` `BUILD_CONFIGURATION` ``, Value=`` `Release` ``, IsSecret=`` `false` ``, AllowOverride=`` `true` ``, IsLargeValue=false
- Variable 2: Name=`` `BUILD_PLATFORM` ``, Value=`` `Any CPU` ``, IsSecret=`` `false` ``, AllowOverride=`` `false` ``
- Variable 3: Name=`` `TIMEOUT` ``, Value=`` `30` ``, IsSecret=`` `false` ``, AllowOverride=`-` (null displayed as dash)

**Test Data:**
Inline JSON (similar to `VariableGroupViewModelFactoryTests.Build_CreateWithRegularVariables_FormatsCorrectly`)

---

#### TC-02: Create Operation - Secret Variables

**Type:** Unit

**Description:**
Verifies that secret variables (`is_secret: true`) mask their values while preserving all metadata (name, is_secret flag, allow_override).

**Preconditions:**
- BuildDefinitionViewModelFactory class exists
- Secret masking logic implemented in BuildDefinitionFormatters

**Test Steps:**
1. Create ResourceChange with `actions: ["create"]` and `after` state containing:
   - Variable 1: `API_KEY` with `value: ""`, `secret_value: "super-secret-123"`, `is_secret: true`, `allow_override: true`
   - Variable 2: `DB_PASSWORD` with `value: ""`, `secret_value: "p@ssw0rd"`, `is_secret: true`, `allow_override: false`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Assert values are masked

**Expected Result:**
- `viewModel.AfterVariables` has count 2
- Variable 1: Name=`` `API_KEY` ``, Value=`` `(sensitive / hidden)` ``, IsSecret=`` `true` ``, AllowOverride=`` `true` ``
- Variable 2: Name=`` `DB_PASSWORD` ``, Value=`` `(sensitive / hidden)` ``, IsSecret=`` `true` ``, AllowOverride=`` `false` ``
- **Security Critical:** Never display actual `secret_value` in any output field

**Test Data:**
Inline JSON

---

#### TC-03: Create Operation - Mixed Variables

**Type:** Unit

**Description:**
Verifies that build definitions with both regular and secret variables correctly format and merge variables into a unified collection, maintaining proper ordering.

**Preconditions:**
- BuildDefinitionViewModelFactory supports both regular and secret variables

**Test Steps:**
1. Create ResourceChange with `actions: ["create"]` and `after` state containing:
   - 2 regular variables (non-secret)
   - 2 secret variables
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Assert variables are merged and ordered correctly

**Expected Result:**
- `viewModel.AfterVariables` has count 4 (all variables merged)
- Regular variables show actual values
- Secret variables show `(sensitive / hidden)`
- All variables maintain correct metadata

**Test Data:**
Inline JSON

---

#### TC-04: Delete Operation - Secret Variables Remain Masked

**Type:** Unit

**Description:**
Verifies that even in delete operations, secret variable values remain masked (security requirement).

**Preconditions:**
- BuildDefinitionViewModelFactory handles delete operations
- Secret masking applies to all operation types

**Test Steps:**
1. Create ResourceChange with `actions: ["delete"]` and `before` state containing:
   - Variable 1: `SECRET_TOKEN` with `is_secret: true`
   - Variable 2: `BUILD_NUMBER` with `is_secret: false`, `value: "42"`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Assert secret value is masked in before state

**Expected Result:**
- `viewModel.BeforeVariables` has count 2
- `SECRET_TOKEN` shows Value=`` `(sensitive / hidden)` ``
- `BUILD_NUMBER` shows Value=`` `42` ``

**Test Data:**
Inline JSON

---

#### TC-05: Update - Secret Variable Changes from Non-Secret to Secret

**Type:** Unit

**Description:**
Verifies that when `is_secret` changes from `false` to `true`, the value column always shows `(sensitive / hidden)`, never revealing the before value.

**Preconditions:**
- BuildDefinitionChangeBuilders supports semantic diffing
- Secret masking logic handles transitional states

**Test Steps:**
1. Create ResourceChange with `actions: ["update"]` and:
   - Before: Variable `API_KEY` with `is_secret: false`, `value: "old-value"`
   - After: Variable `API_KEY` with `is_secret: true`, `secret_value: "new-secret"`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Find the modified variable row for `API_KEY`

**Expected Result:**
- `viewModel.VariableChanges` contains a modified row for `API_KEY`
- Value column shows: `` `(sensitive / hidden)` `` (NOT the before value)
- IsSecret column shows: `- \`false\`<br>+ \`true\`` (diff format)
- **Security Critical:** Never expose the before value even though it was non-secret previously

**Test Data:**
Inline JSON

---

#### TC-06: Update - Added Variables

**Type:** Unit

**Description:**
Verifies semantic matching correctly identifies variables that exist in `after` but not in `before` as "Added" (➕).

**Preconditions:**
- BuildDefinitionChangeBuilders.BuildAdded() implemented
- Semantic matching by variable `name` attribute

**Test Steps:**
1. Create ResourceChange with `actions: ["update"]` and:
   - Before: Variables `VAR_A`, `VAR_B`
   - After: Variables `VAR_A`, `VAR_B`, `NEW_VAR`, `ANOTHER_NEW`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Filter `viewModel.VariableChanges` for added variables

**Expected Result:**
- 2 variables with `Change = "add"` and `ChangeIcon = "➕"`
- Variable names: `NEW_VAR`, `ANOTHER_NEW`
- All metadata formatted correctly for added variables

**Test Data:**
Inline JSON

---

#### TC-07: Update - Modified Variables with Before/After Diffs

**Type:** Unit

**Description:**
Verifies that modified variables show before/after values with `-` and `+` prefixes for changed attributes, and single values without prefix for unchanged attributes.

**Preconditions:**
- BuildDefinitionChangeBuilders.BuildModified() implemented
- BuildDefinitionFormatters.CreateModifiedVariableRow() handles diff formatting

**Test Steps:**
1. Create ResourceChange with `actions: ["update"]` and:
   - Before: Variable `BUILD_CONFIG` with `value: "Debug"`, `is_secret: false`, `allow_override: true`
   - After: Variable `BUILD_CONFIG` with `value: "Release"`, `is_secret: false`, `allow_override: false`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Find the modified variable row for `BUILD_CONFIG`

**Expected Result:**
- `viewModel.VariableChanges` contains a modified row with `Change = "update"`, `ChangeIcon = "🔄"`
- Name: `` `BUILD_CONFIG` `` (no prefix, unchanged)
- Value: `- \`Debug\`<br>+ \`Release\`` (before/after with prefixes)
- IsSecret: `` `false` `` (no prefix, unchanged)
- AllowOverride: `- \`true\`<br>+ \`false\`` (before/after with prefixes)

**Test Data:**
Inline JSON

---

#### TC-08: Update - Removed Variables

**Type:** Unit

**Description:**
Verifies semantic matching correctly identifies variables that exist in `before` but not in `after` as "Removed" (❌).

**Preconditions:**
- BuildDefinitionChangeBuilders.BuildRemoved() implemented

**Test Steps:**
1. Create ResourceChange with `actions: ["update"]` and:
   - Before: Variables `VAR_A`, `VAR_B`, `OLD_VAR`
   - After: Variables `VAR_A`, `VAR_B`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Filter `viewModel.VariableChanges` for removed variables

**Expected Result:**
- 1 variable with `Change = "remove"` and `ChangeIcon = "❌"`
- Variable name: `OLD_VAR`
- Metadata from `before` state displayed correctly

**Test Data:**
Inline JSON

---

#### TC-09: Update - Unchanged Variables

**Type:** Unit

**Description:**
Verifies that variables present in both `before` and `after` with identical values are marked as "Unchanged" (⏺️).

**Preconditions:**
- BuildDefinitionChangeBuilders.BuildUnchanged() implemented

**Test Steps:**
1. Create ResourceChange with `actions: ["update"]` and:
   - Before: Variable `STABLE_VAR` with `value: "same-value"`, `is_secret: false`, `allow_override: true`
   - After: Variable `STABLE_VAR` with `value: "same-value"`, `is_secret: false`, `allow_override: true`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Filter `viewModel.VariableChanges` for unchanged variables

**Expected Result:**
- 1 variable with `Change = "unchanged"` and `ChangeIcon = "⏺️"`
- Variable name: `STABLE_VAR`
- Single values displayed (no before/after diffs)

**Test Data:**
Inline JSON

---

#### TC-10: Large Variable Values - Regular Variables Only

**Type:** Unit

**Description:**
Verifies that variable values exceeding 100 characters or containing line breaks are flagged with `IsLargeValue = true`, and that this applies only to regular (non-secret) variables.

**Preconditions:**
- BuildDefinitionFormatters implements large value detection
- Existing large value display mechanism integrated

**Test Steps:**
1. Create ResourceChange with `actions: ["create"]` and `after` state containing:
   - Variable 1: `SMALL_VALUE` with `value: "short"`, `is_secret: false`
   - Variable 2: `LONG_VALUE` with `value: "x" * 150`, `is_secret: false` (exceeds 100 chars)
   - Variable 3: `MULTILINE_VALUE` with `value: "line1\nline2"`, `is_secret: false`
   - Variable 4: `SECRET_LONG` with `secret_value: "y" * 150`, `is_secret: true`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Check `IsLargeValue` flag for each variable

**Expected Result:**
- `SMALL_VALUE`: IsLargeValue = false
- `LONG_VALUE`: IsLargeValue = true (exceeds 100 chars)
- `MULTILINE_VALUE`: IsLargeValue = true (contains newline)
- `SECRET_LONG`: IsLargeValue = false (secret variables never have large values displayed, always show `(sensitive / hidden)`)

**Test Data:**
Inline JSON

---

#### TC-11: Nested Blocks - CI Trigger, Repository, PR Trigger, Schedules, Jobs

**Type:** Unit

**Description:**
Verifies that non-variable nested blocks (ci_trigger, repository, pull_request_trigger, schedules, jobs) are correctly extracted and formatted as row view models.

**Preconditions:**
- BuildDefinitionExtractors extracts all nested blocks
- BuildDefinitionFormatters formats nested block rows

**Test Steps:**
1. Create ResourceChange with `actions: ["create"]` and `after` state containing:
   - `ci_trigger`: `[{ use_yaml: true, override: ["refs/heads/main", "refs/heads/develop"] }]`
   - `repository`: `[{ repo_type: "TfsGit", repo_id: "repo-123", branch_name: "refs/heads/master", yml_path: "azure-pipelines.yml", report_build_status: true }]`
   - `pull_request_trigger`: `[{ use_yaml: false, override: ["refs/heads/*"], forks: { enabled: true, share_secrets: false } }]`
   - `schedules`: `[{ days_to_build: ["Monday", "Friday"], schedule_only_with_changes: true, start_hours: 9, start_minutes: 0, time_zone: "UTC" }]`
   - `jobs`: `[{ name: "Build", timeout_in_minutes: 60, condition: "succeeded()" }]`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Assert nested block collections are populated

**Expected Result:**
- `viewModel.AfterCiTriggers` has count 1 with UseYaml=`` `true` ``, Override=`` `refs/heads/main`, `refs/heads/develop` ``
- `viewModel.AfterRepositories` has count 1 with RepoType=`` `TfsGit` ``, RepoId=`` `repo-123` ``, BranchName=`` `refs/heads/master` ``, YmlPath=`` `azure-pipelines.yml` ``, ReportBuildStatus=`` `true` ``
- `viewModel.AfterPullRequestTriggers` has count 1 with proper formatting
- `viewModel.AfterSchedules` has count 1 with proper formatting
- Jobs are formatted if present (typically empty for YAML pipelines)

**Test Data:**
Inline JSON

---

#### TC-12: Empty/Null Attribute Values

**Type:** Unit

**Description:**
Verifies that empty strings, null values, and missing attributes are displayed as `-` (dash) for consistency.

**Preconditions:**
- BuildDefinitionFormatters.FormatOptionalString() handles null/empty values
- BuildDefinitionFormatters.FormatBoolean() handles null values

**Test Steps:**
1. Create ResourceChange with `actions: ["create"]` and `after` state containing:
   - Variable with `name: "VAR1"`, `value: ""`, `is_secret: null`, `allow_override: null`
   - Repository with `repo_type: "TfsGit"`, `github_enterprise_url: ""`, `service_connection_id: null`
   - CI Trigger with `use_yaml: true`, `override: []`
2. Call `BuildDefinitionViewModelFactory.Build()`
3. Assert null/empty values are formatted as dashes

**Expected Result:**
- Variable: Value=`-` or `` `(empty)` ``, IsSecret=`` `false` `` (default), AllowOverride=`-`
- Repository: GithubEnterpriseUrl=`-`, ServiceConnectionId=`-`
- CI Trigger: Override=`-` (empty array)

**Test Data:**
Inline JSON

---

#### TC-13: Conditional Rendering - Empty Collections

**Type:** Unit

**Description:**
Verifies that view model correctly represents empty collections, enabling the template to conditionally render sections (no empty tables).

**Preconditions:**
- BuildDefinitionViewModelFactory returns empty collections when blocks are absent

**Test Steps:**
1. Create ResourceChange with `actions: ["create"]` and `after` state containing:
   - `variable: []` (empty)
   - `ci_trigger: []` (empty)
   - `pull_request_trigger: []` (empty)
   - `schedules: []` (empty)
   - `repository: [{ repo_type: "TfsGit", repo_id: "repo-123", branch_name: "refs/heads/main", yml_path: "pipeline.yml" }]` (only repository present)
2. Call `BuildDefinitionViewModelFactory.Build()`

**Expected Result:**
- `viewModel.AfterVariables` is empty
- `viewModel.AfterCiTriggers` is empty
- `viewModel.AfterPullRequestTriggers` is empty
- `viewModel.AfterSchedules` is empty
- `viewModel.AfterRepositories` has count 1
- Template can check `.size > 0` to conditionally render sections

**Test Data:**
Inline JSON

---

### Integration Tests - BuildDefinitionMapper

#### TC-23: Mapper Registration and Integration

**Type:** Integration

**Description:**
Verifies that BuildDefinitionMapper is correctly registered in the dependency injection container and enriches ScriptObject for template rendering.

**Preconditions:**
- BuildDefinitionMapper implements IResourceModelMapper
- AzureDevOpsModule.RegisterResourceModelMappers() registers BuildDefinitionMapper
- AzureDevOpsModule.RegisterFactories() registers BuildDefinitionFactory

**Test Steps:**
1. Create a ProviderRegistry
2. Register AzureDevOpsModule with registry
3. Create a ResourceChange with type `azuredevops_build_definition`
4. Create a ResourceChangeModel wrapper
5. Call mapper's `CanMap()` method
6. Create an empty ScriptObject
7. Call mapper's `EnrichScriptObject()` method
8. Assert ScriptObject contains `build_definition` key with expected properties

**Expected Result:**
- `CanMap()` returns true for `azuredevops_build_definition` type
- `EnrichScriptObject()` adds `build_definition` to ScriptObject
- `scriptObject["build_definition"]` contains: `name`, `path`, `agent_pool_name`, `variable_changes`, `after_variables`, `after_ci_triggers`, `after_repositories`, etc.

**Test Data:**
Minimal ResourceChange JSON

---

### Integration Tests - Template Rendering

#### TC-14: Template Renders Create Operation Layout

**Type:** Integration

**Description:**
Verifies that the Scriban template correctly renders a create operation with proper summary, metadata, and variables table (no Change column).

**Preconditions:**
- `build_definition.sbn` template exists
- Template registered for `azuredevops_build_definition` resource type
- Test data file with create operation

**Test Steps:**
1. Create a test plan JSON with `azuredevops_build_definition` create operation including:
   - Build definition metadata (name, path, agent_pool_name)
   - 3 variables (2 regular, 1 secret)
   - CI trigger block
   - Repository block
2. Parse plan and build report model
3. Render markdown using MarkdownRenderer
4. Extract the build definition section from markdown
5. Assert expected content

**Expected Result:**
- Summary line: `<summary>➕ azuredevops_build_definition <b><code>example</code></b>`
- Metadata: `**Pipeline Name:** <code>example-pipeline</code>`
- Metadata: `**Path:** <code>\\Pipelines</code>`
- Metadata: `**Agent Pool:** <code>Azure Pipelines</code>` (if present)
- Variables section header: `#### Variables`
- Table header (NO Change column): `| Name | Value | Is Secret | Allow Override |`
- Regular variable row: `| \`BUILD_CONFIGURATION\` | \`Release\` | \`false\` | \`true\` |`
- Secret variable row: `| \`API_TOKEN\` | \`(sensitive / hidden)\` | \`true\` | \`true\` |`
- CI Trigger section: `#### CI Trigger`
- Repository section: `#### Repository`

**Test Data:**
`src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuredevops-build-definitions.json` (to be created)

---

#### TC-15: Template Renders Update Operation with Change Indicators

**Type:** Integration

**Description:**
Verifies that the template correctly renders update operations with change indicators (➕, 🔄, ❌, ⏺️) and before/after diffs for modified variables.

**Preconditions:**
- Template supports update operation layout
- Test data with variable changes

**Test Steps:**
1. Create a test plan JSON with `azuredevops_build_definition` update operation including:
   - 1 added variable (➕)
   - 1 modified variable (🔄) with changed value
   - 1 removed variable (❌)
   - 1 unchanged variable (⏺️)
2. Parse plan and render markdown
3. Extract the build definition section
4. Assert change indicators and diffs are present

**Expected Result:**
- Summary line: `<summary>🔄 azuredevops_build_definition <b><code>example</code></b>`
- Table header (WITH Change column): `| Change | Name | Value | Is Secret | Allow Override |`
- Added variable row: `| ➕ | \`NEW_VAR\` | \`new-value\` | \`false\` | \`true\` |`
- Modified variable row: `| 🔄 | \`BUILD_CONFIG\` | - \`Debug\`<br>+ \`Release\` | \`false\` | \`true\` |`
- Removed variable row: `| ❌ | \`OLD_VAR\` | \`old-value\` | \`false\` | \`true\` |`
- Unchanged variable (if shown): `| ⏺️ | \`STABLE\` | \`same\` | \`false\` | \`true\` |`
- HTML code block formatting for diffs (rich diff format)

**Test Data:**
`azuredevops-build-definitions.json`

---

#### TC-16: Template Renders Delete Operation Layout

**Type:** Integration

**Description:**
Verifies that the template correctly renders delete operations with "being deleted" notation.

**Preconditions:**
- Template supports delete operation layout
- Test data with delete operation

**Test Steps:**
1. Create a test plan JSON with `azuredevops_build_definition` delete operation including:
   - Build definition metadata
   - 2 variables (1 regular, 1 secret)
   - Repository block
2. Parse plan and render markdown
3. Extract the build definition section
4. Assert delete layout

**Expected Result:**
- Summary line: `<summary>❌ azuredevops_build_definition <b><code>example</code></b>`
- Metadata displayed
- Variables section header: `#### Variables (being deleted)`
- Table header (NO Change column): `| Name | Value | Is Secret | Allow Override |`
- Regular variable: `| \`BUILD_CONFIG\` | \`Release\` | \`false\` | \`true\` |`
- Secret variable: `| \`SECRET_KEY\` | \`(sensitive / hidden)\` | \`true\` | \`false\` |`
- Repository section: `#### Repository (being deleted)` (if present)

**Test Data:**
`azuredevops-build-definitions.json`

---

#### TC-17: Secret Variables - Value Always Masked

**Type:** Integration

**Description:**
End-to-end verification that secret variable values are never exposed in rendered markdown output, regardless of operation type.

**Preconditions:**
- Template integrated with factory secret masking logic
- Test data with secret variables in create/update/delete

**Test Steps:**
1. Create test plan JSON with secret variables in:
   - Create operation
   - Update operation (secret value changed)
   - Delete operation
2. Render markdown for all operations
3. Search for any occurrence of actual secret values in output

**Expected Result:**
- Rendered markdown contains `(sensitive / hidden)` for all secret variables
- Rendered markdown does NOT contain actual secret values (e.g., "super-secret-123", "p@ssw0rd")
- Metadata (name, is_secret flag, allow_override) is displayed correctly
- **Security Critical:** No secret value leakage

**Test Data:**
`azuredevops-build-definitions.json`

---

#### TC-18: CI Trigger and Repository Blocks Displayed

**Type:** Integration

**Description:**
Verifies that CI trigger and repository configuration blocks are rendered as tables when present.

**Preconditions:**
- Template includes CI trigger and repository sections
- Test data with these blocks populated

**Test Steps:**
1. Create test plan JSON with build definition containing:
   - `ci_trigger: [{ use_yaml: true, override: [] }]`
   - `repository: [{ repo_type: "TfsGit", repo_id: "repo-guid", branch_name: "refs/heads/master", yml_path: "azure-pipelines.yml", report_build_status: true }]`
2. Render markdown
3. Extract build definition section

**Expected Result:**
- CI Trigger section header: `#### CI Trigger`
- CI Trigger table: `| Use YAML | Override (Branch Filters) |`
- CI Trigger row: `| \`true\` | - |` (empty override displayed as dash)
- Repository section header: `#### Repository`
- Repository table: `| Type | Repo ID | Branch | YAML Path | Report Build Status |`
- Repository row: `| \`TfsGit\` | \`repo-guid\` | \`refs/heads/master\` | \`azure-pipelines.yml\` | \`true\` |`

**Test Data:**
`azuredevops-build-definitions.json`

---

#### TC-19: Pull Request Trigger Block Displayed

**Type:** Integration

**Description:**
Verifies that pull request trigger configuration is rendered as a table when present.

**Preconditions:**
- Template includes pull request trigger section
- Test data with pull request trigger block

**Test Steps:**
1. Create test plan JSON with build definition containing:
   - `pull_request_trigger: [{ use_yaml: false, override: ["refs/heads/feature/*"], forks: { enabled: true, share_secrets: false } }]`
2. Render markdown
3. Extract build definition section

**Expected Result:**
- Pull Request Trigger section header: `#### Pull Request Trigger`
- Pull Request Trigger table with columns for: Use YAML, Override, Forks Enabled, Share Secrets
- PR Trigger row with formatted values

**Test Data:**
`azuredevops-build-definitions.json`

---

#### TC-20: Schedules Block Displayed

**Type:** Integration

**Description:**
Verifies that schedules configuration is rendered as a table when present.

**Preconditions:**
- Template includes schedules section
- Test data with schedules block

**Test Steps:**
1. Create test plan JSON with build definition containing:
   - `schedules: [{ branch_filters: ["refs/heads/main"], days_to_build: ["Monday", "Friday"], schedule_only_with_changes: true, start_hours: 9, start_minutes: 0, time_zone: "UTC" }]`
2. Render markdown
3. Extract build definition section

**Expected Result:**
- Schedules section header: `#### Schedules`
- Schedules table with columns for: Branch Filters, Days to Build, Schedule Only with Changes, Start Time, Time Zone
- Schedule row with formatted values (days as comma-separated list, time formatted as HH:MM)

**Test Data:**
`azuredevops-build-definitions.json`

---

#### TC-21: Conditional Rendering - No Empty Tables

**Type:** Integration

**Description:**
Verifies that the template only renders sections when the corresponding blocks contain data (no empty tables displayed).

**Preconditions:**
- Template uses conditional rendering (`if collection.size > 0`)
- Test data with minimal build definition (only repository block)

**Test Steps:**
1. Create test plan JSON with build definition containing:
   - `variable: []` (empty)
   - `ci_trigger: []` (empty)
   - `pull_request_trigger: []` (empty)
   - `schedules: []` (empty)
   - `repository: [{ repo_type: "TfsGit", repo_id: "repo-123", branch_name: "refs/heads/main", yml_path: "pipeline.yml" }]`
2. Render markdown
3. Extract build definition section

**Expected Result:**
- Pipeline metadata displayed
- Repository section displayed with table
- NO Variables section (empty array)
- NO CI Trigger section (empty array)
- NO Pull Request Trigger section (empty array)
- NO Schedules section (empty array)
- NO Jobs section (empty array)
- HTML comments indicating sections were skipped (optional, for clarity)

**Test Data:**
`azuredevops-build-definitions.json`

---

#### TC-22: Template Follows Report Style Guide

**Type:** Integration

**Description:**
Verifies that the template adheres to the Report Style Guide formatting standards (code formatting for values, plain text for labels).

**Preconditions:**
- Template follows style guide conventions
- Test data with various values

**Test Steps:**
1. Render markdown from test plan JSON
2. Extract build definition section
3. Assert formatting conventions

**Expected Result:**
- `<code>` tags used in summary line: `<code>example</code>`, `<code>example-pipeline</code>` (Azure DevOps compatibility)
- Backticks used for inline code in table cells: `` `BUILD_CONFIG` ``, `` `Release` ``, `` `true` ``
- Plain text for labels (no backticks): `**Pipeline Name:**`, `**Path:**`, `**Agent Pool:**`
- Table headers are plain text: `| Name | Value | Is Secret |`
- Empty values displayed as `-` (dash) with backticks: `` `-` ``

**Test Data:**
`azuredevops-build-definitions.json`

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty variable array | No Variables section displayed | TC-13, TC-21 |
| Null description | No Description line displayed | (Same as variable group pattern) |
| Variable with null `is_secret` | Displayed as `` `false` `` (default) | TC-12 |
| Variable with null `allow_override` | Displayed as `-` | TC-12 |
| Variable with empty string value | Displayed as `-` or `` `(empty)` `` | TC-12 |
| Secret variable with >100 char value | Always shows `(sensitive / hidden)`, IsLargeValue = false | TC-10 |
| Regular variable with >100 char value | IsLargeValue = true, value moved to large values section | TC-10 |
| CI trigger with empty override array | Override column shows `-` | TC-12, TC-18 |
| Repository with empty `github_enterprise_url` | Column shows `-` | TC-12 |
| Multiple repositories (array) | Each repository shown as table row | TC-11 |
| Jobs array populated (rare) | Jobs section displayed with basic metadata | TC-11 |
| Variable name case sensitivity | Semantic matching is case-insensitive | TC-06, TC-07, TC-08 |
| Secret variable changes to non-secret | Before/after diff for `is_secret`, value shown after change | (Add to TC-07 variation) |
| All variables unchanged | All rows show ⏺️ icon, single values (no diffs) | TC-09 |

## Test Data Requirements

### New Test Data File

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuredevops-build-definitions.json`

**Contents:**
A Terraform plan JSON containing multiple `azuredevops_build_definition` resource changes:

1. **Create Operation - `azuredevops_build_definition.create_basic`**
   - Name: `basic-pipeline`
   - Path: `\\Pipelines`
   - Agent Pool: `Azure Pipelines`
   - Variables:
     - Regular: `BUILD_CONFIGURATION` = `"Release"`, `BUILD_PLATFORM` = `"Any CPU"`
     - Secret: `API_TOKEN` (is_secret: true)
   - CI Trigger: `use_yaml: true`, `override: []`
   - Repository: `repo_type: "TfsGit"`, with all required fields
   - Empty: `pull_request_trigger`, `schedules`, `jobs`

2. **Update Operation - `azuredevops_build_definition.update_variables`**
   - Before and After states with variable changes:
     - Added: `NEW_VAR` = `"new-value"`
     - Modified: `BUILD_CONFIGURATION` changes from `"Debug"` to `"Release"`
     - Removed: `OLD_VAR` = `"old-value"`
     - Unchanged: `STABLE_VAR` = `"same"`

3. **Delete Operation - `azuredevops_build_definition.delete_basic`**
   - Before state with:
     - Variables (regular and secret)
     - Repository configuration

4. **Edge Case - `azuredevops_build_definition.minimal`**
   - Minimal configuration with only repository block
   - All other blocks empty (tests conditional rendering)

5. **Edge Case - `azuredevops_build_definition.with_all_blocks`**
   - Contains all nested blocks populated:
     - Variables (regular and secret)
     - CI trigger with override branch filters
     - Pull request trigger with forks configuration
     - Schedules with branch filters and time configuration
     - Repository configuration
     - Jobs array (if testing this edge case)

6. **Security Test - `azuredevops_build_definition.secret_transitions`**
   - Update operation with:
     - Variable changing from `is_secret: false` to `is_secret: true`
     - Secret variable with metadata changes (e.g., `allow_override` changed)

### DemoPaths Entry

Add to `src/tests/Oocx.TfPlan2Md.TUnit/TestData/DemoPaths.cs`:

```csharp
/// <summary>
/// Gets the path to the Azure DevOps build definitions test plan JSON file.
/// </summary>
public static string AzureDevOpsBuildDefinitionPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "azuredevops-build-definitions.json");
```

## Non-Functional Tests

### Performance

**Requirement:** Template rendering should have negligible performance impact (<10ms per resource)

**Test:**
- Create a test plan with 10 build definition resources (mix of create/update/delete)
- Measure total rendering time
- Assert: Total time < 100ms (10ms per resource)

**Test Case:** Add to performance test suite

### Security

**Requirement:** Secret variable values must never be exposed

**Test:**
- Render markdown for build definition with secret variables
- Search entire markdown output for actual secret values
- Assert: No secret values found, only `(sensitive / hidden)` displayed

**Test Case:** TC-17 (already defined)

### Backwards Compatibility

**Requirement:** Must not break rendering of other Azure DevOps resources

**Test:**
- Render a plan containing:
  - `azuredevops_project`
  - `azuredevops_variable_group`
  - `azuredevops_build_definition`
  - `azuredevops_git_repository`
- Assert: All resources render correctly without errors

**Test Case:** Integration test using existing `examples/azuredevops/terraform_plan2.json`

## UAT Test Plan

For user-facing markdown rendering changes, a separate **UAT Test Plan** document is required.

**Location:** `docs/features/094-build-definition-tables/uat-test-plan.md`

**Note to Quality Engineer:** This UAT test plan should be created as a separate deliverable and should define:
1. The test artifact (plan.json) that exercises all build definition features
2. Validation instructions for Maintainer review in GitHub and Azure DevOps PRs
3. Expected rendering outcomes (what to verify visually)

The UAT plan will be used by the UAT Tester agent to create test PRs and validate rendering in real platforms.

## Open Questions

1. **Q: Should we include the build definition ID in the metadata display?**
   - A: Defer to Developer - likely yes if available in the Terraform state, displayed as `**Definition ID:** <code>123</code>`

2. **Q: How should we handle very long branch filter lists in CI/Pull Request triggers?**
   - A: Display as comma-separated list. If exceeds reasonable length (e.g., >200 chars), consider moving to large values section (similar to variable values).

3. **Q: Should schedules display time in 24-hour format or include AM/PM?**
   - A: Use 24-hour format for consistency (e.g., `09:00`, `14:30`). Follow existing time formatting patterns in the codebase if any.

4. **Q: For update operations, should unchanged variables be displayed or hidden?**
   - A: Display all variables (added, modified, removed, unchanged) for complete transparency. This matches the variable group pattern.

5. **Q: Should we display `variable_groups` array in the build definition template?**
   - A: Yes, display as standard attribute (array of IDs) in metadata section, similar to `features` array. Do not render as table - variable group details are shown when the `azuredevops_variable_group` resource itself is in the plan.

## Traceability

| Specification Section | Test Cases | Notes |
|----------------------|------------|-------|
| Specialized template for `azuredevops_build_definition` | TC-14, TC-15, TC-16, TC-22 | Template structure and rendering |
| Display variables in table format | TC-01, TC-03, TC-14, TC-15, TC-16 | Variables table rendering |
| Secret variables show metadata but mask values | TC-02, TC-04, TC-05, TC-17 | Security requirement |
| Semantic diffing (Added/Modified/Removed/Unchanged) | TC-06, TC-07, TC-08, TC-09, TC-15 | Variable matching by name |
| Large variable values (>100 chars or multi-line) | TC-10 | Regular variables only |
| Modified variables show before/after with prefixes | TC-07, TC-15 | Diff formatting |
| Unchanged attributes show single value | TC-07 | No unnecessary diffs |
| CI Trigger block displayed | TC-11, TC-18 | CI trigger table |
| Pull Request Trigger block displayed | TC-11, TC-19 | PR trigger table |
| Schedules block displayed | TC-11, TC-20 | Schedules table |
| Repository block displayed | TC-11, TC-18, TC-19, TC-20 | Repository table |
| Jobs block displayed if populated | TC-11 | Jobs table (edge case) |
| Empty/null attributes displayed as `-` | TC-12 | Consistent null handling |
| Conditional rendering (no empty tables) | TC-13, TC-21 | Template conditional logic |
| Create/Update/Delete operations | TC-14, TC-15, TC-16 | Different table layouts |
| Build definition metadata displayed | TC-14, TC-15, TC-16 | Metadata section |
| Template follows Report Style Guide | TC-22 | Formatting conventions |
| Mapper registered in DI | TC-23 | Integration with provider module |
