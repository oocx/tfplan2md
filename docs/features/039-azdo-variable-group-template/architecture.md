# Architecture: Azure DevOps Variable Group Template

## Status

Proposed

## Context

Feature specification: [specification.md](specification.md)

This feature creates a custom Scriban template for `azuredevops_variable_group` resources to provide semantic diffing of variables by name. Currently, all variables show as "(sensitive)" in Terraform plans even when they're not secrets, making it impossible to review variable changes.

### Requirements Summary

1. **Unified variable display**: Merge regular (`variable` array) and secret (`secret_variable` array) variables into a single table
2. **Semantic diffing**: Match variables by `name` across before/after states to show Added/Modified/Removed/Unchanged
3. **Secret value protection**: Display all metadata (name, enabled, content_type, expires) but show "(sensitive / hidden)" for secret values
4. **Large value handling**: Move values >100 chars or multi-line to collapsible large values section
5. **Key Vault integration**: Display `key_vault` blocks in separate table when present
6. **Operation support**: Create, update (replace), and delete operations with appropriate layouts

### Data Structure

Based on `examples/azuredevops/terraform_plan2.json`:

```json
{
  "type": "azuredevops_variable_group",
  "change": {
    "before": {
      "name": "example-variables",
      "description": "Variable group for example pipeline",
      "variable": [
        {"name": "APP_VERSION", "value": "1.0.0", "enabled": false, "content_type": "", "expires": ""}
      ],
      "secret_variable": [
        {"name": "SECRET_KEY", "value": "supersecret", "enabled": false, "content_type": "", "expires": ""}
      ],
      "key_vault": []
    },
    "after": { /* similar structure */ },
    "before_sensitive": {
      "secret_variable": true,  // entire array marked sensitive
      "variable": [{}, {}]      // individual entries NOT sensitive
    }
  }
}
```

**Key observations:**
- Regular variables have `value` attribute; secret variables have `value` attribute in the same location (not `secret_value`)
- The `secret_variable` array is marked as sensitive (`true`), but `variable` array is not
- Both arrays have identical attributes: name, value, enabled, content_type, expires
- Values may be empty strings, null, or missing (especially for enabled, content_type, expires)

### Existing Patterns

The codebase has two patterns for resource-specific templates:

**Pattern 1: Direct template logic** (`azurerm_network_security_group.sbn`)
- Template contains all semantic diffing logic in Scriban
- Iterates arrays, matches by name, builds change rows
- No ViewModel or Factory classes
- Pros: Self-contained, easy to understand flow
- Cons: Complex Scriban logic, harder to test, limited by Scriban capabilities

**Pattern 2: ViewModel pattern** (`azurerm_role_assignment.sbn`)
- C# ViewModel + Factory precompute all display logic
- Template is minimal, just iterates preformatted rows
- ViewModel attached to `ResourceChangeModel` via type matching in `ReportModel.cs`
- Pros: Testable, powerful C# for complex logic, clean templates
- Cons: More files, additional complexity

Feature 026 (Template Rendering Simplification) established the current template system where templates receive a `change` variable with access to both raw data (`BeforeJson`, `AfterJson`) and precomputed ViewModels.

## Options Considered

### Option 1: ViewModel Pattern (Recommended)

Create a `VariableGroupViewModel` and `VariableGroupViewModelFactory` following the pattern from `azurerm_role_assignment` and `azurerm_network_security_group`.

**Structure:**
```
VariableGroupViewModel {
  string? Name
  string? Description
  IReadOnlyList<VariableChangeRowViewModel> VariableChanges  // For update
  IReadOnlyList<VariableRowViewModel> AfterVariables         // For create
  IReadOnlyList<VariableRowViewModel> BeforeVariables        // For delete
  IReadOnlyList<KeyVaultRowViewModel> KeyVaultBlocks
}

VariableChangeRowViewModel {
  string Change              // ➕, 🔄, ❌, ⏺️
  string Name                // `var_name`
  string Value               // `value` or `(sensitive / hidden)` or `-`/`+` diff
  string Enabled             // `true`/`false` or `-`/`+` diff
  string ContentType         // formatted value or `-`/`+` diff
  string Expires             // formatted value or `-`/`+` diff
  bool IsLargeValue          // true if value moved to large values section
}

VariableRowViewModel {
  string Name
  string Value
  string Enabled
  string ContentType
  string Expires
  bool IsLargeValue
}

KeyVaultRowViewModel {
  string Name
  string ServiceEndpointId
  string SearchDepth
}
```

**Factory responsibilities:**
1. Extract `variable` and `secret_variable` arrays from before/after states
2. Merge into unified lists indexed by variable name
3. Categorize as Added/Modified/Removed/Unchanged
4. For each variable:
   - Detect if from `secret_variable` array → format value as "(sensitive / hidden)"
   - Format attributes with `-`/`+` diff prefixes for changes
   - Show single value without prefix for unchanged attributes
   - Detect large values (>100 chars or multi-line) and set flag
5. Format Key Vault blocks if present
6. Apply Report Style Guide formatting (inline code for values, dashes for empty)

**Template responsibilities:**
- Simple iteration over precomputed rows
- Different table layouts for create/update/delete
- Conditionally show Key Vault table
- Leverage existing `format_large_value()` helper for large values section

**Pros:**
- **Testable**: Factory logic can be unit tested with various scenarios
- **Powerful**: C# handles complex merging, matching, and formatting logic
- **Clean template**: Minimal Scriban logic, easy to read
- **Consistent**: Follows established pattern from features 026, 024
- **AOT-compatible**: All reflection eliminated via explicit mapping (no runtime reflection needed)
- **Easier debugging**: Can set breakpoints in factory, inspect intermediate state

**Cons:**
- More files to create (3 new C# files: ViewModel, Factory, tests)
- Requires wiring in `ReportModel.cs` to attach ViewModel

### Option 2: Direct Template Logic

Implement all semantic diffing and formatting in the Scriban template, similar to the NSG template before it was refactored.

**Structure:**
- Single `.sbn` template file
- Scriban logic to:
  - Extract and merge `variable` and `secret_variable` arrays
  - Match variables by name across before/after
  - Format values with sensitivity detection
  - Build diff strings inline

**Pros:**
- Fewer files (just template)
- Self-contained logic
- No C# changes except registering template

**Cons:**
- **Complex Scriban logic**: Merging two arrays by name, detecting changes, formatting diffs
- **Limited debugging**: Can't set breakpoints or inspect intermediate state
- **Harder to test**: Would need template rendering tests, can't test logic in isolation
- **Error-prone**: Scriban has limited data structures (no dictionaries for O(1) lookup)
- **Difficult to maintain**: Logic spread across many `{{ for }}` loops and conditionals
- **Large value detection**: Would need to implement in Scriban (string length check, line counting)
- **Inconsistent**: Goes against the direction established in feature 026

### Option 3: Hybrid Approach

Use a ViewModel for complex parts (variable merging, semantic diffing) but keep simple formatting in template.

**Structure:**
- Lightweight ViewModel that merges arrays and provides matched pairs
- Template handles formatting and diff generation

**Pros:**
- Balances concerns between C# and template
- Less code in Factory than Option 1

**Cons:**
- **Split responsibilities**: Unclear boundary between Factory and template
- **Still complex template**: Formatting logic in template harder to test
- **Inconsistent**: Doesn't follow either established pattern fully
- **No clear advantage**: Complexity reduced only marginally vs Option 1

## Decision

**Choose Option 1: ViewModel Pattern**

## Rationale

1. **Complexity justifies ViewModel**: This feature requires:
   - Merging two arrays (variable, secret_variable) into unified view
   - Semantic matching by variable name across before/after states
   - Different handling for secret vs regular variables
   - Large value detection and formatting
   - Key Vault block handling
   
   This level of complexity is best handled in testable C# code rather than Scriban templates.

2. **Follows established pattern**: Features 026, 024 established ViewModels as the preferred approach for complex resource-specific rendering. The NSG and role assignment templates both use ViewModels successfully.

3. **Testability**: We can write focused unit tests for:
   - Variable merging logic
   - Semantic matching by name
   - Secret vs regular variable detection
   - Large value detection
   - Diff formatting
   
   Template tests become simple: "does it iterate the rows correctly?"

4. **Maintainability**: Future enhancements (e.g., better diff formatting, sorting options) can be added to the Factory without touching the template.

5. **Debugging**: Developers can set breakpoints in the Factory, inspect intermediate state, and understand exactly what's being rendered.

6. **AOT compatibility**: Explicit property mapping in ViewModel avoids reflection, consistent with the project's AOT direction (feature 037).

## Implementation Design

### 1. File Structure

**New files to create:**
```
src/Oocx.TfPlan2Md/MarkdownGeneration/
  Models/
    VariableGroupViewModel.cs          (ViewModel classes)
    VariableGroupViewModelFactory.cs   (Factory to build ViewModels)
  Templates/
    azuredevops/
      variable_group.sbn                (Scriban template)
```

**Files to modify:**
```
src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs
  - Add VariableGroupViewModel? property to ResourceChangeModel
  - Add type check and Factory call in BuildResourceChangeModel()
```

**Test files to create:**
```
tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/
  VariableGroupViewModelFactoryTests.cs
  VariableGroupTemplateTests.cs
```

### 2. ViewModel Design

**Data flow:**
1. `ReportModelBuilder.BuildResourceChangeModel()` detects `azuredevops_variable_group` type
2. Calls `VariableGroupViewModelFactory.Build(change, largeValueFormat)`
3. Factory:
   - Extracts before/after JSON
   - Parses `variable` and `secret_variable` arrays
   - Merges into dictionaries by variable name
   - Matches before/after variables semantically
   - Categorizes as Added/Modified/Removed/Unchanged
   - Formats values with secret detection and large value detection
   - Builds `VariableChangeRowViewModel` for each variable
   - Parses `key_vault` array if present
4. Returns `VariableGroupViewModel` with preformatted rows
5. `ResourceChangeModel.VariableGroup` property set to ViewModel
6. Template iterates preformatted rows

**Semantic matching algorithm:**
```csharp
// Pseudocode
var beforeVars = MergeVariableArrays(beforeState);  // regular + secret
var afterVars = MergeVariableArrays(afterState);

var allNames = beforeVars.Keys.Union(afterVars.Keys);

foreach (var name in allNames) {
    var before = beforeVars.TryGetValue(name);
    var after = afterVars.TryGetValue(name);
    
    if (before == null) -> Added
    else if (after == null) -> Removed
    else if (DiffersInAnyAttribute(before, after)) -> Modified
    else -> Unchanged
}
```

**Secret detection:**
```csharp
// Check which array the variable came from
bool isSecret = (sourceArray == "secret_variable");
string formattedValue = isSecret 
    ? "(sensitive / hidden)" 
    : FormatValue(variable.Value);
```

**Large value detection:**
```csharp
bool isLarge = !isSecret && (
    value.Length > 100 || 
    value.Contains('\n')
);

// If large, store reference in ViewModel for template to display in collapsible section
```

### 3. Template Structure

The template will follow the pattern from `azurerm_network_security_group.sbn`:

```scriban
<details open ...>
<summary>{{ change.summary_html }}</summary>
<br>

### {{ change.action_symbol }} {{ change.address | escape_markdown }}

{{~ if change.variable_group && change.variable_group.name ~}}
**Variable Group:** `{{ change.variable_group.name | escape_markdown }}`
{{~ end ~}}

{{~ if change.variable_group.description ~}}
**Description:** `{{ change.variable_group.description | escape_markdown }}`
{{~ end ~}}

{{~ if change.variable_group.key_vault_blocks.size > 0 ~}}
#### Key Vault Integration

| Name | Service Endpoint ID | Search Depth |
| ---- | ------------------- | ------------ |
{{~ for kv in change.variable_group.key_vault_blocks ~}}
| {{ kv.name }} | {{ kv.service_endpoint_id }} | {{ kv.search_depth }} |
{{~ end ~}}
{{~ end ~}}

{{~ if change.action == "update" && change.variable_group.variable_changes.size > 0 ~}}
#### Variables

| Change | Name | Value | Enabled | Content Type | Expires |
| ------ | ---- | ----- | ------- | ------------ | ------- |
{{~ for var in change.variable_group.variable_changes ~}}
| {{ var.change }} | {{ var.name }} | {{ var.value }} | {{ var.enabled }} | {{ var.content_type }} | {{ var.expires }} |
{{~ end ~}}

{{~ else if change.action == "create" && change.variable_group.after_variables.size > 0 ~}}
#### Variables

| Name | Value | Enabled | Content Type | Expires |
| ---- | ----- | ------- | ------------ | ------- |
{{~ for var in change.variable_group.after_variables ~}}
| {{ var.name }} | {{ var.value }} | {{ var.enabled }} | {{ var.content_type }} | {{ var.expires }} |
{{~ end ~}}

{{~ else if change.action == "delete" && change.variable_group.before_variables.size > 0 ~}}
#### Variables (being deleted)

| Name | Value | Enabled | Content Type | Expires |
| ---- | ----- | ------- | ------------ | ------- |
{{~ for var in change.variable_group.before_variables ~}}
| {{ var.name }} | {{ var.value }} | {{ var.enabled }} | {{ var.content_type }} | {{ var.expires }} |
{{~ end ~}}
{{~ end ~}}

{{~ if change.variable_group.has_large_values ~}}
<details>
<summary>Large values: {{ change.variable_group.large_value_names }}</summary>

{{~ for var in change.variable_group.large_value_variables ~}}
### `{{ var.name }}`

{{ format_large_value(var.before_value, var.after_value, large_value_format) }}

{{~ end ~}}
</details>
{{~ end ~}}

</details>
```

### 4. Attribute Formatting

Follow Report Style Guide and existing patterns:

**Regular values:**
```
value: "Production" → `Production`
value: "" → `-`
value: null → `-`
```

**Secret values:**
```
From secret_variable array → `(sensitive / hidden)`
```

**Boolean values:**
```
true → `true`
false → `false`
null/missing → `-`
```

**Modified attributes (update operation):**
```
unchanged: `value` (single line)
changed:   - `old`<br>+ `new` (two lines with br)
```

**Large values:**
```
Move to separate section using existing format_large_value() helper
Reference by variable name in summary
```

### 5. Handling Edge Cases

**Empty arrays:**
- No variables: Show "No variables defined" message
- No key_vault blocks: Don't show Key Vault section

**Replace vs Update:**
- Azure DevOps variable groups often show as "replace" (delete+create) in Terraform
- Template should handle `action == "replace"` same as `action == "update"`
- Or check for `["delete", "create"]` in actions array

**Unknown/computed values:**
- Show as `(known after apply)` consistent with Terraform terminology

**Missing attributes:**
- Format as `-` (dash) per Report Style Guide

**Variable ordering:**
- Preserve order from Terraform plan (no custom sorting)
- In update tables: Added → Modified → Removed → Unchanged

### 6. Integration Points

**ReportModel.cs modification:**
```csharp
public class ResourceChangeModel
{
    // ... existing properties ...
    
    /// <summary>
    /// Gets or sets the precomputed view model for azuredevops_variable_group resources.
    /// Related feature: docs/features/039-azdo-variable-group-template/specification.md
    /// </summary>
    public VariableGroupViewModel? VariableGroup { get; set; }
}

// In ReportModelBuilder.BuildResourceChangeModel():
if (string.Equals(rc.Type, "azuredevops_variable_group", StringComparison.OrdinalIgnoreCase))
{
    model.VariableGroup = VariableGroupViewModelFactory.Build(rc, _largeValueFormat);
}
```

**Template registration:**
- File location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azuredevops/variable_group.sbn`
- Embedded resource via csproj (automatic via wildcard)
- Template resolver will match by type: `azuredevops_variable_group` → `azuredevops/variable_group.sbn`

**AOT compatibility:**
- All ViewModels use explicit properties (no dynamic access)
- No reflection needed in Factory (uses JsonElement for parsing)
- Consistent with AotScriptObjectMapper pattern from feature 037

## Consequences

### Positive

- **Clear variable changes**: Users can see exactly which variables are added, modified, or removed
- **Secret protection maintained**: Secret values hidden but metadata visible
- **Testable design**: Factory logic can be thoroughly tested in isolation
- **Clean templates**: Minimal Scriban logic, easy to understand and maintain
- **Extensible**: Future enhancements (sorting, filtering) can be added to Factory
- **Consistent pattern**: Follows established ViewModel approach from features 026, 024
- **Large value support**: Handles long connection strings and multi-line values cleanly
- **Key Vault awareness**: Shows Key Vault integration when present

### Negative

- **More files**: Requires 3 new C# files (ViewModel, Factory, tests) vs 1 template-only file
- **Build time**: Minimal increase (compiling 2 small classes)
- **Learning curve**: Contributors need to understand Factory pattern (but it's already used elsewhere)

### Risks and Mitigations

**Risk: Variable matching logic errors**
- Mitigation: Comprehensive unit tests with various scenarios (added, removed, modified, renamed)
- Mitigation: Test with actual Azure DevOps plan JSON from examples/

**Risk: Secret detection false positives/negatives**
- Mitigation: Test both `variable` and `secret_variable` arrays explicitly
- Mitigation: Validate against actual Terraform plan sensitive markers

**Risk: Large value detection edge cases**
- Mitigation: Test boundary cases (exactly 100 chars, single newline, etc.)
- Mitigation: Use existing large value format helpers

**Risk: Template rendering errors**
- Mitigation: Template tests validate output structure
- Mitigation: Integration tests with real plan JSON

## Testing Strategy

### Unit Tests (Factory)

Test `VariableGroupViewModelFactory.Build()`:
1. **Create operation**: Variables correctly formatted
2. **Delete operation**: Before variables shown
3. **Update operation**: 
   - Added variables (➕)
   - Removed variables (❌)
   - Modified variables (🔄) with before/after diffs
   - Unchanged variables (⏺️) shown as single value
4. **Secret variables**: Value shows "(sensitive / hidden)", metadata visible
5. **Large values**: Detected correctly, flagged for separate section
6. **Empty attributes**: Formatted as `-`
7. **Key Vault blocks**: Parsed and formatted
8. **Edge cases**: Empty arrays, all secrets, all regular, mixed changes

### Template Tests

Test template rendering with mock ViewModels:
1. Create operation layout (no Change column)
2. Update operation layout (with Change column)
3. Delete operation layout
4. Key Vault section shown when present
5. Large values section shown when present
6. Empty states handled gracefully

### Integration Tests

Test with actual `examples/azuredevops/terraform_plan2.json`:
1. Render full report with variable group change
2. Validate markdown quality (no lint errors)
3. Verify GitHub/Azure DevOps rendering compatibility
4. Compare output with expected format from specification

## Implementation Notes

### For the Developer Agent

1. **Start with ViewModel classes**: Define data structures first
2. **Implement Factory**: Focus on variable merging and semantic matching
3. **Write Factory tests**: Cover all scenarios before touching template
4. **Create template**: Should be straightforward once ViewModel is tested
5. **Wire up ReportModel.cs**: Add property and type check
6. **Write template tests**: Validate output structure
7. **Run integration tests**: Test with real JSON

### Code Quality Expectations

- All classes must have XML documentation comments (including private members)
- Follow Report Style Guide for value formatting
- Use `InternalsVisibleTo` for test access (don't make everything public)
- Follow existing ViewModel patterns (NSG, role assignment) closely
- Ensure AOT compatibility (no reflection in user code paths)

### Directory Structure

Create the `azuredevops` directory under Templates:
```
src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/
  azuredevops/
    variable_group.sbn
```

This follows the pattern from `azurerm/` provider-specific templates.

## Definition of Done

- [ ] `VariableGroupViewModel.cs` created with row classes
- [ ] `VariableGroupViewModelFactory.cs` created with Build() method
- [ ] Factory unit tests cover all scenarios (added/modified/removed/unchanged, secrets, large values)
- [ ] `Templates/azuredevops/variable_group.sbn` created
- [ ] Template handles create/update/delete operations
- [ ] Key Vault blocks displayed when present
- [ ] Large values moved to collapsible section
- [ ] `ReportModel.cs` modified to attach ViewModel
- [ ] Template tests validate output structure
- [ ] Integration test with `examples/azuredevops/terraform_plan2.json` passes
- [ ] All existing tests still pass
- [ ] Markdown output validated (no lint errors)
- [ ] Documentation updated in `docs/features.md`

## References

- Feature specification: [specification.md](specification.md)
- Related ADR: [ADR-001: Scriban Templating](../../adr-001-scriban-templating.md)
- Related feature: [Feature 026: Template Rendering Simplification](../026-template-rendering-simplification/architecture.md)
- Report Style Guide: [docs/report-style-guide.md](../../report-style-guide.md)
- Example data: `examples/azuredevops/terraform_plan2.json`
