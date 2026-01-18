# Test Plan: Azure DevOps Variable Group Template

## Overview

This test plan defines comprehensive test coverage for Feature #039: Custom template for Azure DevOps Variable Groups. The feature creates a specialized Scriban template with a ViewModel pattern to display variable changes semantically, merging regular and secret variables into a unified table with proper secret value masking.

**Related Documents:**
- Specification: [specification.md](specification.md)
- Architecture: [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Unified table displays both regular and secret variables | TC-01, TC-02, TC-03 | Unit |
| Secret variables show metadata but "(sensitive / hidden)" in value | TC-04, TC-05 | Unit |
| Semantic diffing by variable name (Added/Modified/Removed/Unchanged) | TC-06, TC-07, TC-08, TC-09 | Unit |
| Large value handling (>100 chars or multi-line) | TC-10, TC-11 | Unit |
| Key vault blocks displayed in separate table | TC-12, TC-13 | Unit |
| Create/Update/Delete operations | TC-14, TC-15, TC-16 | Template |
| Edge cases (empty arrays, replace vs update, unknown values) | TC-17, TC-18, TC-19, TC-20 | Unit |
| Template follows Report Style Guide | TC-21, TC-22, TC-23 | Template |
| Integration with existing example data | TC-24, TC-25 | Integration |
| Markdown validity | TC-26 | Integration |

## User Acceptance Scenarios

> **Purpose**: Define scenarios for manual Maintainer review via Test PRs in GitHub and Azure DevOps to validate rendering and real-world usage.

### Scenario 1: Variable Group Update with Mixed Changes

**User Goal**: Review variable group changes in a Terraform plan before applying, understanding which variables are added, modified, or removed.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments
- **Azure DevOps**: Verify rendering in Azure DevOps PR comments

**Expected Output**:
- Summary line shows variable group name and change count (e.g., `3 🔧 variables`)
- Unified table displays both regular and secret variables together
- Change indicators (➕, 🔄, ❌, ⏺️) clearly identify variable state
- Secret variable metadata visible but value shows "(sensitive / hidden)"
- Modified variables show before/after values with `-` and `+` prefixes
- Empty/null attributes displayed as `-`

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] All variable changes are clearly visible
- [ ] Secret values are properly masked
- [ ] Table structure is readable and properly formatted
- [ ] Change indicators are visually distinct

**Feedback Opportunities**:
- Is the unified table format clear and readable?
- Are secret variables sufficiently distinguished?
- Is the before/after diff format easy to understand?

---

### Scenario 2: Key Vault-Linked Variable Group

**User Goal**: Understand Key Vault integration when reviewing variable groups linked to Azure Key Vault.

**Test PR Context**:
- **GitHub**: Verify Key Vault table rendering
- **Azure DevOps**: Verify Key Vault table rendering

**Expected Output**:
- Key Vault Integration section appears before Variables section
- Table shows: Name, Service Endpoint ID, Search Depth
- Local variables displayed in Variables table
- Clear separation between Key Vault metadata and local variables

**Success Criteria**:
- [ ] Key Vault section renders correctly
- [ ] Table columns are properly aligned
- [ ] Service endpoint IDs are formatted as code
- [ ] Section ordering is logical (metadata → Key Vault → Variables)

**Feedback Opportunities**:
- Is the Key Vault section placement appropriate?
- Is the distinction between Key Vault-sourced and local variables clear?

---

### Scenario 3: Large Variable Values

**User Goal**: Review variable groups containing connection strings or other large values without cluttering the main table.

**Test PR Context**:
- **GitHub**: Verify large values section rendering
- **Azure DevOps**: Verify large values section rendering

**Expected Output**:
- Large values moved to collapsible "Large values" section
- Main table shows variable metadata without value column for large variables
- Large values section shows before/after comparison with proper formatting
- Values formatted in code blocks

**Success Criteria**:
- [ ] Large values don't break main table layout
- [ ] Collapsible section works correctly
- [ ] Before/after comparison is readable
- [ ] Value content is properly escaped

**Feedback Opportunities**:
- Is the 100-character threshold appropriate?
- Is the large values section easily discoverable?

## Test Cases

### Unit Tests: VariableGroupViewModelFactory

#### TC-01: Create Operation - Regular Variables

**Type:** Unit

**Description:**
Verifies that the factory correctly formats regular variables for create operations.

**Preconditions:**
- Valid `ResourceChange` with `action == "create"`
- `variable` array with multiple entries
- No `secret_variable` array

**Test Steps:**
1. Create ResourceChange JSON with 3 regular variables
2. Set various attributes: name, value, enabled, content_type, expires
3. Call `VariableGroupViewModelFactory.Build()`
4. Inspect `AfterVariables` collection

**Expected Result:**
- `AfterVariables` contains 3 entries
- Each entry has `Name`, `Value`, `Enabled`, `ContentType`, `Expires` properties
- All values formatted as inline code (e.g., `` `value` ``)
- Empty/null attributes formatted as `-`
- Boolean enabled values formatted as `` `true` `` or `` `false` ``
- `IsLargeValue` is false for all entries

**Test Data:**
```json
{
  "variable": [
    {"name": "APP_VERSION", "value": "1.0.0", "enabled": false},
    {"name": "ENVIRONMENT", "value": "production", "enabled": true, "content_type": "text/plain"},
    {"name": "TIMEOUT", "value": "30", "expires": "2024-12-31"}
  ]
}
```

---

#### TC-02: Create Operation - Secret Variables

**Type:** Unit

**Description:**
Verifies that the factory correctly masks secret variable values while preserving metadata.

**Preconditions:**
- Valid `ResourceChange` with `action == "create"`
- `secret_variable` array with entries

**Test Steps:**
1. Create ResourceChange JSON with 2 secret variables
2. Set secret_value, enabled, content_type attributes
3. Call `VariableGroupViewModelFactory.Build()`
4. Inspect `AfterVariables` collection

**Expected Result:**
- `AfterVariables` contains 2 entries
- `Value` property shows `` `(sensitive / hidden)` `` for all secrets
- Metadata (enabled, content_type, expires) is visible and formatted
- `IsLargeValue` is false (secrets never marked as large)

**Test Data:**
```json
{
  "secret_variable": [
    {"name": "API_KEY", "value": "super-secret-key", "enabled": true},
    {"name": "DB_PASSWORD", "value": "p@ssw0rd123", "content_type": "password"}
  ]
}
```

---

#### TC-03: Create Operation - Mixed Variables

**Type:** Unit

**Description:**
Verifies that the factory correctly merges regular and secret variables into a unified collection.

**Preconditions:**
- Valid `ResourceChange` with `action == "create"`
- Both `variable` and `secret_variable` arrays present

**Test Steps:**
1. Create ResourceChange JSON with 2 regular + 2 secret variables
2. Call `VariableGroupViewModelFactory.Build()`
3. Inspect `AfterVariables` collection

**Expected Result:**
- `AfterVariables` contains 4 entries (merged from both arrays)
- Regular variables show actual values
- Secret variables show "(sensitive / hidden)"
- Variables preserve original array order (regular first, then secret)

**Test Data:**
```json
{
  "variable": [
    {"name": "ENV", "value": "prod"},
    {"name": "REGION", "value": "eastus"}
  ],
  "secret_variable": [
    {"name": "API_KEY", "value": "secret1"},
    {"name": "TOKEN", "value": "secret2"}
  ]
}
```

---

#### TC-04: Update Operation - Secret Metadata Changes

**Type:** Unit

**Description:**
Verifies that secret variable metadata changes (enabled, content_type) are displayed correctly while value remains masked.

**Preconditions:**
- Valid `ResourceChange` with `action == "update"`
- Same secret variable in before and after with changed metadata

**Test Steps:**
1. Create ResourceChange JSON with secret variable
2. Before: `enabled: false, content_type: ""`
3. After: `enabled: true, content_type: "password"`
4. Call `VariableGroupViewModelFactory.Build()`
5. Inspect `VariableChanges` collection

**Expected Result:**
- Variable categorized as Modified (🔄)
- `Value` column shows `` `(sensitive / hidden)` `` (no before/after diff)
- `Enabled` column shows `` - `false`<br>+ `true` ``
- `ContentType` column shows `` - `-`<br>+ `password` ``

**Test Data:**
```json
{
  "before": {
    "secret_variable": [{"name": "SECRET_KEY", "value": "old-secret", "enabled": false, "content_type": ""}]
  },
  "after": {
    "secret_variable": [{"name": "SECRET_KEY", "value": "new-secret", "enabled": true, "content_type": "password"}]
  }
}
```

---

#### TC-05: Update Operation - Secret Value Changes

**Type:** Unit

**Description:**
Verifies that when secret values change, the value column remains masked with no diff shown.

**Preconditions:**
- Valid `ResourceChange` with `action == "update"`
- Same secret variable with different values in before/after

**Test Steps:**
1. Create ResourceChange JSON with secret variable
2. Before: `value: "old-secret"`
3. After: `value: "new-secret"`
4. All metadata unchanged
5. Call `VariableGroupViewModelFactory.Build()`
6. Inspect `VariableChanges` collection

**Expected Result:**
- Variable categorized as Modified (🔄) due to value change
- `Value` column shows `` `(sensitive / hidden)` `` (single line, no diff)
- Other columns show single values (no diff formatting)

---

#### TC-06: Update Operation - Added Variables

**Type:** Unit

**Description:**
Verifies that variables present only in the after state are categorized as Added.

**Preconditions:**
- Valid `ResourceChange` with `action == "update"`
- Variable exists in after but not in before

**Test Steps:**
1. Create ResourceChange JSON
2. Before: 1 variable (`VAR_A`)
3. After: 2 variables (`VAR_A`, `VAR_B`)
4. Call `VariableGroupViewModelFactory.Build()`
5. Inspect `VariableChanges` collection

**Expected Result:**
- `VariableChanges` contains 2 entries
- `VAR_A` categorized as Unchanged (⏺️) or Modified if changed
- `VAR_B` categorized as Added (➕)
- Added variable shows single values (no before/after diff)

---

#### TC-07: Update Operation - Removed Variables

**Type:** Unit

**Description:**
Verifies that variables present only in the before state are categorized as Removed.

**Preconditions:**
- Valid `ResourceChange` with `action == "update"`
- Variable exists in before but not in after

**Test Steps:**
1. Create ResourceChange JSON
2. Before: 2 variables (`VAR_A`, `VAR_B`)
3. After: 1 variable (`VAR_A`)
4. Call `VariableGroupViewModelFactory.Build()`
5. Inspect `VariableChanges` collection

**Expected Result:**
- `VariableChanges` contains 2 entries
- `VAR_A` categorized as Unchanged (⏺️) or Modified if changed
- `VAR_B` categorized as Removed (❌)
- Removed variable shows before values

---

#### TC-08: Update Operation - Modified Variables

**Type:** Unit

**Description:**
Verifies that variables with changed attributes are categorized as Modified and show before/after diffs.

**Preconditions:**
- Valid `ResourceChange` with `action == "update"`
- Same variable in before/after with changed attributes

**Test Steps:**
1. Create ResourceChange JSON with same variable name in both states
2. Before: `value: "1.0.0", enabled: false`
3. After: `value: "2.0.0", enabled: true`
4. Call `VariableGroupViewModelFactory.Build()`
5. Inspect `VariableChanges` collection

**Expected Result:**
- Variable categorized as Modified (🔄)
- `Value` column shows `` - `1.0.0`<br>+ `2.0.0` ``
- `Enabled` column shows `` - `false`<br>+ `true` ``
- Other unchanged columns show single value (no diff)

---

#### TC-09: Update Operation - Unchanged Variables

**Type:** Unit

**Description:**
Verifies that variables with identical attributes in before/after are categorized as Unchanged.

**Preconditions:**
- Valid `ResourceChange` with `action == "update"`
- Same variable with identical attributes in before/after

**Test Steps:**
1. Create ResourceChange JSON with identical variable in both states
2. Before: `name: "ENV", value: "prod", enabled: true`
3. After: `name: "ENV", value: "prod", enabled: true`
4. Call `VariableGroupViewModelFactory.Build()`
5. Inspect `VariableChanges` collection

**Expected Result:**
- Variable categorized as Unchanged (⏺️)
- All columns show single values (no diff formatting)
- No before/after prefixes

---

#### TC-10: Large Value Detection - Regular Variables

**Type:** Unit

**Description:**
Verifies that regular variables with values >100 characters or multi-line are flagged as large.

**Preconditions:**
- Valid `ResourceChange` with regular variable
- Variable value exceeds 100 characters OR contains newline

**Test Steps:**
1. Test Case A: 101-character value
2. Test Case B: Multi-line value (2 lines, <100 chars total)
3. Test Case C: Multi-line value with >100 chars
4. Call `VariableGroupViewModelFactory.Build()` for each
5. Inspect `IsLargeValue` flag

**Expected Result:**
- All test cases have `IsLargeValue = true`
- Large values added to `LargeValueVariables` collection
- Main table value column omitted or shows reference

**Test Data:**
```json
// Case A
{"name": "LONG_VAR", "value": "a".repeat(101)}

// Case B
{"name": "MULTI_VAR", "value": "line1\nline2"}

// Case C
{"name": "BOTH_VAR", "value": "a".repeat(60) + "\n" + "b".repeat(60)}
```

---

#### TC-11: Large Value Detection - Secret Variables

**Type:** Unit

**Description:**
Verifies that secret variables are NEVER flagged as large, regardless of value length.

**Preconditions:**
- Valid `ResourceChange` with secret variable
- Secret value exceeds 100 characters

**Test Steps:**
1. Create ResourceChange with secret_variable
2. Value is 200 characters
3. Call `VariableGroupViewModelFactory.Build()`
4. Inspect `IsLargeValue` flag

**Expected Result:**
- `IsLargeValue = false` for secret variable
- Value displayed as "(sensitive / hidden)" in main table
- Variable NOT added to `LargeValueVariables` collection

---

#### TC-12: Key Vault Integration - Single Block

**Type:** Unit

**Description:**
Verifies that Key Vault block is parsed and formatted correctly.

**Preconditions:**
- Valid `ResourceChange` with `key_vault` array containing 1 entry

**Test Steps:**
1. Create ResourceChange JSON with key_vault block
2. Block has: name, service_endpoint_id, search_depth
3. Call `VariableGroupViewModelFactory.Build()`
4. Inspect `KeyVaultBlocks` collection

**Expected Result:**
- `KeyVaultBlocks` contains 1 entry
- Entry has formatted properties:
  - `Name`: `` `my-keyvault` ``
  - `ServiceEndpointId`: `` `a1b2c3d4-e5f6-7890-abcd-ef1234567890` ``
  - `SearchDepth`: `` `1` ``

**Test Data:**
```json
{
  "key_vault": [
    {
      "name": "my-keyvault",
      "service_endpoint_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "search_depth": 1
    }
  ]
}
```

---

#### TC-13: Key Vault Integration - Empty Array

**Type:** Unit

**Description:**
Verifies that empty key_vault array results in no Key Vault section.

**Preconditions:**
- Valid `ResourceChange` with empty `key_vault` array

**Test Steps:**
1. Create ResourceChange JSON with `key_vault: []`
2. Call `VariableGroupViewModelFactory.Build()`
3. Inspect `KeyVaultBlocks` collection

**Expected Result:**
- `KeyVaultBlocks` is empty
- Template should not render Key Vault section

---

#### TC-17: Edge Case - Empty Variable Arrays

**Type:** Unit

**Description:**
Verifies that empty variable and secret_variable arrays are handled gracefully.

**Preconditions:**
- Valid `ResourceChange` with empty arrays

**Test Steps:**
1. Create ResourceChange JSON with `variable: []` and `secret_variable: []`
2. Call `VariableGroupViewModelFactory.Build()` for create action
3. Inspect `AfterVariables` collection

**Expected Result:**
- `AfterVariables` is empty
- ViewModel should indicate "No variables defined"
- Template should display appropriate message

---

#### TC-18: Edge Case - Replace vs Update Action

**Type:** Unit

**Description:**
Verifies that replace actions (delete+create) are handled like update operations.

**Preconditions:**
- Valid `ResourceChange` with `actions: ["delete", "create"]`

**Test Steps:**
1. Create ResourceChange JSON with replace action
2. Before and after states with different variables
3. Call `VariableGroupViewModelFactory.Build()`
4. Verify change categorization

**Expected Result:**
- Factory treats replace as update
- Variables categorized as Added/Modified/Removed appropriately
- Uses `VariableChanges` collection (not AfterVariables/BeforeVariables)

---

#### TC-19: Edge Case - Unknown Values

**Type:** Unit

**Description:**
Verifies that unknown/computed values are displayed correctly.

**Preconditions:**
- Valid `ResourceChange` with unknown after values

**Test Steps:**
1. Create ResourceChange JSON with after value marked as unknown
2. Variable has `after_unknown: true` or similar marker
3. Call `VariableGroupViewModelFactory.Build()`
4. Inspect formatted value

**Expected Result:**
- Value displayed as `` `(known after apply)` ``
- Matches Terraform terminology

---

#### TC-20: Edge Case - Null and Empty String Attributes

**Type:** Unit

**Description:**
Verifies that null and empty string attributes are formatted consistently.

**Preconditions:**
- Valid `ResourceChange` with various null/empty attributes

**Test Steps:**
1. Test Case A: `enabled: null`
2. Test Case B: `content_type: ""`
3. Test Case C: `expires: null`
4. Call `VariableGroupViewModelFactory.Build()` for each
5. Inspect formatted attributes

**Expected Result:**
- All null and empty string attributes displayed as `-`
- Consistent formatting across all attribute types

---

### Template Tests

#### TC-14: Template - Create Operation Layout

**Type:** Template Integration

**Description:**
Verifies that the template renders create operations with correct layout and formatting.

**Preconditions:**
- Valid `VariableGroupViewModel` for create action
- Mock data with 3 variables (2 regular, 1 secret)

**Test Steps:**
1. Create mock ViewModel for create action
2. Render template with ViewModel
3. Inspect rendered markdown

**Expected Result:**
- Summary line: `➕ azuredevops_variable_group <b><code>example</code></b> — <code>example-variables</code>`
- Variable Group name displayed: `**Variable Group:** `example-variables``
- Description displayed if present
- Variables table has columns: Name | Value | Enabled | Content Type | Expires (NO Change column)
- All 3 variables displayed with correct formatting
- Secret variable value shows "(sensitive / hidden)"

---

#### TC-15: Template - Update Operation Layout

**Type:** Template Integration

**Description:**
Verifies that the template renders update operations with change indicators and diff formatting.

**Preconditions:**
- Valid `VariableGroupViewModel` for update action
- Mock data with Added, Modified, Removed, Unchanged variables

**Test Steps:**
1. Create mock ViewModel for update action
2. Include 1 added, 1 modified, 1 removed, 1 unchanged variable
3. Render template with ViewModel
4. Inspect rendered markdown

**Expected Result:**
- Summary line includes change count: `3 🔧 variables` (or similar)
- Variables table has columns: Change | Name | Value | Enabled | Content Type | Expires
- Change indicators displayed: ➕, 🔄, ❌, ⏺️
- Modified variables show before/after diff with `-` and `+` prefixes
- Unchanged variables show single values

---

#### TC-16: Template - Delete Operation Layout

**Type:** Template Integration

**Description:**
Verifies that the template renders delete operations with variables being deleted.

**Preconditions:**
- Valid `VariableGroupViewModel` for delete action
- Mock data with 2 variables

**Test Steps:**
1. Create mock ViewModel for delete action
2. Render template with ViewModel
3. Inspect rendered markdown

**Expected Result:**
- Summary line: `❌ azuredevops_variable_group <b><code>example</code></b> — <code>example-variables</code>`
- Section header: `#### Variables (being deleted)`
- Variables table has columns: Name | Value | Enabled | Content Type | Expires (NO Change column)
- All variables from before state displayed

---

#### TC-21: Template - Report Style Guide Compliance

**Type:** Template Integration

**Description:**
Verifies that the template follows Report Style Guide formatting rules.

**Preconditions:**
- Valid `VariableGroupViewModel`
- Mock data with various value types

**Test Steps:**
1. Create mock ViewModel with:
   - String values
   - Boolean values
   - Empty/null values
2. Render template
3. Inspect markdown formatting

**Expected Result:**
- **Data values** formatted as inline code (backticks)
- **Labels** (table headers, attribute names) as plain text
- Empty/null values displayed as `-` (plain text, no code formatting)
- Resource name in summary: `<b><code>name</code></b>`
- Non-breaking space (U+00A0) after action icons
- HTML `<code>` tags used in `<summary>` (Azure DevOps compatibility)

---

#### TC-22: Template - Key Vault Section Rendering

**Type:** Template Integration

**Description:**
Verifies that Key Vault integration section renders correctly when present.

**Preconditions:**
- Valid `VariableGroupViewModel` with Key Vault blocks

**Test Steps:**
1. Create mock ViewModel with 1 Key Vault block
2. Render template
3. Inspect Key Vault section

**Expected Result:**
- Section header: `#### Key Vault Integration`
- Table with columns: Name | Service Endpoint ID | Search Depth
- Section appears BEFORE Variables section
- All values formatted as inline code

---

#### TC-23: Template - Large Values Section

**Type:** Template Integration

**Description:**
Verifies that large values are moved to a collapsible section with proper formatting.

**Preconditions:**
- Valid `VariableGroupViewModel` with large values
- Mock data includes 1 large value variable

**Test Steps:**
1. Create mock ViewModel with large value variable
2. Set IsLargeValue flag and populate LargeValueVariables
3. Render template
4. Inspect large values section

**Expected Result:**
- Collapsible section: `<details><summary>Large values: VAR_NAME (2 lines, 1 changed)</summary>`
- Variable name as heading: `### `VAR_NAME``
- Before/After sections with code blocks
- Section appears AFTER main variables table

---

### Integration Tests

#### TC-24: Integration - Existing Example Data

**Type:** End-to-End Integration

**Description:**
Verifies that the template correctly renders the existing Azure DevOps variable group from `examples/azuredevops/terraform_plan2.json`.

**Preconditions:**
- Example file exists at `examples/azuredevops/terraform_plan2.json`
- File contains `azuredevops_variable_group.example` resource

**Test Steps:**
1. Load `terraform_plan2.json`
2. Parse with `TerraformPlanParser`
3. Build `ReportModel` (triggers ViewModel factory)
4. Render markdown
5. Extract variable group section

**Expected Result:**
- Variable group section exists in output
- All variables from example data are displayed
- Secret variables have masked values
- Markdown is valid (no parsing errors)
- Format matches specification examples

---

#### TC-25: Integration - Full Report Generation

**Type:** End-to-End Integration

**Description:**
Verifies that variable group resources integrate correctly with full report generation.

**Preconditions:**
- Test plan JSON with mixed resource types including variable groups

**Test Steps:**
1. Create test plan with:
   - 1 variable group (update)
   - 1 Azure resource (for context)
2. Generate full report
3. Verify variable group section

**Expected Result:**
- Variable group section rendered correctly
- Module grouping works (if variable group in module)
- No interference with other resource templates
- Overall report structure maintained

---

#### TC-26: Integration - Markdown Validation

**Type:** Linting Integration

**Description:**
Verifies that generated markdown passes all markdownlint rules.

**Preconditions:**
- Rendered markdown from variable group template

**Test Steps:**
1. Render variable group with various scenarios:
   - Create with mixed variables
   - Update with all change types
   - Delete with variables
   - Key Vault integration
2. Run markdownlint-cli2 on output
3. Check for lint errors

**Expected Result:**
- Zero lint errors
- No table formatting issues (MD012)
- Proper heading hierarchy
- Balanced HTML tags (<details>, <summary>)
- No consecutive blank lines

---

## Test Data Requirements

### Existing Test Data

Reuse existing test data from `examples/azuredevops/terraform_plan2.json`:
- Contains `azuredevops_variable_group.example` with replace action
- Has both regular and secret variables
- Provides real-world structure

### New Test Data Needed

Create the following test fixtures in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`:

#### 1. `variable-group-create.json`

Variable group create operation with:
- 2 regular variables (with all attributes populated)
- 1 secret variable
- Description present
- No key_vault blocks

#### 2. `variable-group-update-mixed.json`

Variable group update with:
- 1 added regular variable
- 1 modified regular variable (value + enabled changed)
- 1 removed secret variable
- 1 unchanged variable
- 1 modified secret variable (metadata only)

#### 3. `variable-group-delete.json`

Variable group delete operation with:
- 3 variables (2 regular, 1 secret)
- All attributes present

#### 4. `variable-group-keyvault.json`

Variable group with Key Vault integration:
- 1 key_vault block with all attributes
- 2 local regular variables
- No secret_variable array

#### 5. `variable-group-large-values.json`

Variable group with large values:
- 1 regular variable with 150-character value
- 1 regular variable with multi-line value (3 lines)
- 1 regular variable with normal value
- 1 secret variable (should not trigger large value handling)

#### 6. `variable-group-edge-cases.json`

Variable group with edge cases:
- Empty variable arrays
- Variables with all null/empty attributes
- Variable with unknown/computed value
- Variables with special characters in names/values

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty variable arrays | Display "No variables defined" message | TC-17 |
| Empty secret_variable array | Display only regular variables | TC-01 |
| Empty variable array | Display only secret variables | TC-02 |
| All variables unchanged | Display with ⏺️ indicator | TC-09 |
| Replace action (delete+create) | Handle as update with change indicators | TC-18 |
| Unknown/computed values | Show "(known after apply)" | TC-19 |
| Null enabled attribute | Display as `-` | TC-20 |
| Empty string content_type | Display as `-` | TC-20 |
| Secret variable value change | Show "(sensitive / hidden)" with no diff | TC-05 |
| Large secret variable | Do NOT move to large values section | TC-11 |
| Empty key_vault array | Do not render Key Vault section | TC-13 |
| Variable name with special chars | Escape properly for markdown | TC-26 |
| Value with pipes `\|` | Escape to avoid breaking table | TC-26 |
| Multi-line value (regular) | Convert to large value display | TC-10 |

## Non-Functional Tests

### Performance

**Requirement:** Factory should process 100 variables in <100ms

**Test Approach:**
- Create mock ResourceChange with 100 variables (mix of regular/secret)
- Call factory in loop (1000 iterations)
- Measure average execution time
- Assert p95 < 100ms

### Error Handling

**Requirement:** Factory should handle malformed JSON gracefully

**Test Cases:**
1. Missing `variable` property → return empty collection
2. Null `secret_variable` → treat as empty array
3. Invalid JSON structure → throw descriptive exception
4. Missing required attributes → use default/empty values

### Compatibility

**Requirement:** Template should render correctly in GitHub and Azure DevOps

**Test Approach:**
- Generate markdown with variable group
- Post as PR comment in test repositories (manual UAT)
- Verify rendering in both platforms
- Check for platform-specific issues (e.g., HTML tag support)

## Testing Tools and Infrastructure

### Test Framework

- **TUnit 1.9.26**: Primary test framework
- **AwesomeAssertions**: Fluent assertion library
- Test location: `src/tests/Oocx.TfPlan2Md.TUnit/`

### Test Naming Convention

Follow existing pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `Build_CreateOperation_FormatsRegularVariables`
- `Build_SecretVariable_MasksValue`
- `Build_UpdateOperation_ShowsBeforeAfterDiff`

### Test Execution

```bash
# Run all variable group tests
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/VariableGroup*/*

# Run factory unit tests
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/VariableGroupViewModelFactoryTests/*

# Run template tests
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/VariableGroupTemplateTests/*
```

### Mock Data Helpers

Create test helper class: `VariableGroupTestData.cs`

Provides factory methods:
- `CreateVariableGroupChange(action, variables, secretVariables, keyVault)`
- `CreateVariable(name, value, enabled, contentType, expires)`
- `CreateSecretVariable(name, value, enabled)`
- `CreateKeyVaultBlock(name, endpointId, searchDepth)`

## Open Questions

None at this time. All testing requirements are clear based on specification and architecture.

## Definition of Done

Testing is complete when:
- [ ] All unit tests implemented and passing (TC-01 through TC-13, TC-17 through TC-20)
- [ ] All template tests implemented and passing (TC-14 through TC-16, TC-21 through TC-23)
- [ ] All integration tests implemented and passing (TC-24 through TC-26)
- [ ] All edge cases covered with test cases
- [ ] Test data fixtures created
- [ ] All tests executable via `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` without human intervention
- [ ] Test coverage >80% for ViewModel and Factory classes
- [ ] All tests follow TUnit and AwesomeAssertions patterns
- [ ] Test naming follows convention
- [ ] No hanging or flaky tests
- [ ] Documentation updated (if needed)
