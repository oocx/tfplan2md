# Test Plan: Debug Output

## Overview

This test plan covers the debug output feature (038) which adds comprehensive diagnostic capabilities to tfplan2md. The feature introduces a `--debug` CLI flag that enables collection and display of diagnostic information about principal mapping and template resolution, appended to the markdown report.

Reference: [specification.md](specification.md) and [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Command-line option `--debug` is available and documented | TC-01, TC-02 | Unit |
| Debug output appended to markdown when `--debug` enabled | TC-03, TC-04 | Integration |
| Principal mapping diagnostics: load status | TC-05, TC-06 | Integration |
| Principal mapping diagnostics: type counts | TC-07 | Integration |
| Principal mapping diagnostics: failed IDs with context | TC-08, TC-09 | Integration |
| Template resolution logging (custom vs built-in) | TC-10, TC-11, TC-12 | Integration |
| Debug output disabled by default | TC-13 | Integration |
| Debug functionality does not break existing features | TC-14, TC-15 | Regression |
| Help text updated with debug option | TC-02 | Unit |
| DiagnosticContext generates valid markdown | TC-16, TC-17, TC-18 | Unit |

## User Acceptance Scenarios

### Scenario 1: View Principal Mapping Diagnostics

**User Goal**: Troubleshoot principal mapping failures by viewing which principals were loaded, type counts, and which IDs failed to resolve with resource context.

**Test PR Context**:
- **GitHub**: Verify debug section renders correctly with principal mapping diagnostics.
- **Azure DevOps**: Verify debug section renders correctly with principal mapping diagnostics.

**Expected Output**:
- A markdown report with a "Debug Information" section at the end
- Principal mapping section showing:
  - Load status and file path
  - Count of principals by type (users, groups, service principals)
  - List of failed principal IDs with resource addresses that referenced them

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] Information is accurate and complete
- [ ] Failed principal IDs are clearly linked to resources

**Feedback Opportunities**:
- Is the diagnostic information clear and actionable?
- Does the formatting make it easy to identify issues?
- Are there any edge cases that need better handling?

---

### Scenario 2: View Template Resolution Information

**User Goal**: Understand which templates are being used for each resource type (custom, built-in resource-specific, or default).

**Test PR Context**:
- **GitHub**: Verify template resolution information displays correctly.
- **Azure DevOps**: Verify template resolution information displays correctly.

**Expected Output**:
- A markdown report with a "Debug Information" section
- Template resolution section showing:
  - List of resource types processed
  - Template source for each (built-in, custom path, or default)

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] Template sources are clearly identified
- [ ] Information helps users verify template behavior

**Feedback Opportunities**:
- Does the template information help understand template resolution?
- Is the distinction between custom and built-in templates clear?

## Test Cases

### TC-01: CliParser_ParseDebugFlag_RecognizesFlag

**Type:** Unit

**Description:**
Verifies that `CliParser` correctly parses the `--debug` flag and sets the `Debug` property in `CliOptions`.

**Preconditions:**
- None

**Test Steps:**
1. Call `CliParser.Parse` with arguments `["plan.json", "--debug"]`
2. Verify `CliOptions.Debug` is `true`
3. Call `CliParser.Parse` with arguments `["plan.json"]` (no debug flag)
4. Verify `CliOptions.Debug` is `false`

**Expected Result:**
- `--debug` flag sets `Debug` property to `true`
- Without flag, `Debug` property is `false`

**Test Data:**
- Inline arguments

---

### TC-02: HelpText_IncludesDebugOption

**Type:** Unit

**Description:**
Verifies that the help text includes documentation for the `--debug` option.

**Preconditions:**
- None

**Test Steps:**
1. Call `HelpTextProvider.GetHelpText()` or similar
2. Verify the output contains `--debug` option
3. Verify the description explains the debug output behavior

**Expected Result:**
Help text documents the `--debug` flag and explains it appends diagnostic information to the markdown report.

**Test Data:**
- None

---

### TC-03: DiagnosticContext_GenerateMarkdownSection_EmptyContext_ReturnsEmptyString

**Type:** Unit

**Description:**
Verifies that `DiagnosticContext.GenerateMarkdownSection()` returns an empty string or minimal content when no diagnostics have been collected.

**Preconditions:**
- Create a new `DiagnosticContext` with no data

**Test Steps:**
1. Create a new `DiagnosticContext`
2. Call `GenerateMarkdownSection()`
3. Verify the result is empty or contains only a minimal header

**Expected Result:**
No diagnostic content is generated when context is empty.

**Test Data:**
- None

---

### TC-04: Render_WithDebugFlag_AppendsDebugSection

**Type:** Integration

**Description:**
Verifies that when the `--debug` flag is used, the markdown output includes a debug section at the end.

**Preconditions:**
- Valid Terraform plan JSON
- Principal mapping file (optional)

**Test Steps:**
1. Create a `DiagnosticContext`
2. Process a plan with `PrincipalMapper` and `MarkdownRenderer`, passing the context
3. Call `context.GenerateMarkdownSection()` and append to output
4. Verify the output contains "## Debug Information" heading
5. Verify the debug section appears after the main report content

**Expected Result:**
Debug section is appended to the markdown output with proper formatting.

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-05: PrincipalMapper_WithContext_RecordsSuccessfulLoad

**Type:** Integration

**Description:**
Verifies that when a principal mapping file is loaded successfully, `PrincipalMapper` records the success in the diagnostic context.

**Preconditions:**
- Valid principal mapping JSON file
- `DiagnosticContext` instance

**Test Steps:**
1. Create a `DiagnosticContext`
2. Create a `PrincipalMapper` with a valid mapping file and the context
3. Verify `context.PrincipalMappingLoadedSuccessfully` is `true`
4. Verify `context.PrincipalMappingFilePath` contains the file path

**Expected Result:**
Successful load is recorded in the diagnostic context.

**Test Data:**
- Test principal mapping file with users, groups, and service principals

---

### TC-06: PrincipalMapper_WithContext_RecordsFailedLoad

**Type:** Integration

**Description:**
Verifies that when a principal mapping file cannot be loaded, `PrincipalMapper` records the failure in the diagnostic context.

**Preconditions:**
- Invalid or missing principal mapping file
- `DiagnosticContext` instance

**Test Steps:**
1. Create a `DiagnosticContext`
2. Create a `PrincipalMapper` with an invalid mapping file and the context
3. Verify `context.PrincipalMappingLoadedSuccessfully` is `false`
4. Verify the failure is recorded

**Expected Result:**
Failed load is recorded in the diagnostic context.

**Test Data:**
- Invalid JSON or non-existent file path

---

### TC-07: PrincipalMapper_WithContext_RecordsTypeCounts

**Type:** Integration

**Description:**
Verifies that `PrincipalMapper` records the count of principals by type (users, groups, service principals) in the diagnostic context.

**Preconditions:**
- Valid principal mapping file with known counts
- `DiagnosticContext` instance

**Test Steps:**
1. Create a `DiagnosticContext`
2. Create a `PrincipalMapper` with a mapping file containing:
   - 3 users
   - 2 groups
   - 1 service principal
3. Verify `context.PrincipalTypeCount["users"]` equals 3
4. Verify `context.PrincipalTypeCount["groups"]` equals 2
5. Verify `context.PrincipalTypeCount["service principals"]` equals 1

**Expected Result:**
Type counts are accurately recorded in the diagnostic context.

**Test Data:**
- Test principal mapping file with known type counts

---

### TC-08: PrincipalMapper_WithContext_RecordsFailedResolutions

**Type:** Integration

**Description:**
Verifies that when principal IDs cannot be resolved, `PrincipalMapper` records the failed IDs along with the resource address that referenced them.

**Preconditions:**
- Principal mapping file missing certain IDs
- Plan with role assignments referencing those IDs
- `DiagnosticContext` instance

**Test Steps:**
1. Create a `DiagnosticContext`
2. Create a `PrincipalMapper` with a mapping file that does NOT contain certain principal IDs
3. Process a plan with role assignments referencing the missing IDs
4. Verify `context.FailedResolutions` contains entries for each missing ID
5. Verify each entry includes the resource address (e.g., "azurerm_role_assignment.example")

**Expected Result:**
Failed resolutions are recorded with resource context.

**Test Data:**
- Plan with role assignments: `TestData/azurerm-azuredevops-plan.json`
- Principal mapping file with missing IDs

---

### TC-09: DiagnosticContext_GenerateMarkdownSection_FailedResolutions_FormatsCorrectly

**Type:** Unit

**Description:**
Verifies that failed principal resolutions are formatted correctly in the markdown output.

**Preconditions:**
- `DiagnosticContext` with failed resolutions

**Test Steps:**
1. Create a `DiagnosticContext`
2. Add failed resolutions:
   - ID: "12345678-1234-1234-1234-123456789012", Resource: "azurerm_role_assignment.example"
   - ID: "87654321-4321-4321-4321-210987654321", Resource: "azurerm_role_assignment.reader"
3. Call `GenerateMarkdownSection()`
4. Verify the output contains:
   - A list of failed IDs
   - Each ID formatted as code (backticks)
   - Resource addresses shown in parentheses with backticks

**Expected Result:**
Output format matches specification:
```
Failed to resolve 2 principal IDs:
- `12345678-1234-1234-1234-123456789012` (referenced in `azurerm_role_assignment.example`)
- `87654321-4321-4321-4321-210987654321` (referenced in `azurerm_role_assignment.reader`)
```

**Test Data:**
- Inline test data

---

### TC-10: MarkdownRenderer_WithContext_RecordsBuiltInTemplate

**Type:** Integration

**Description:**
Verifies that `MarkdownRenderer` records when a built-in resource-specific template is used.

**Preconditions:**
- Plan with a resource that has a built-in template (e.g., `azurerm_firewall_network_rule_collection`)
- `DiagnosticContext` instance

**Test Steps:**
1. Create a `DiagnosticContext`
2. Render a plan containing a firewall resource using `MarkdownRenderer` with the context
3. Verify `context.TemplateResolutions` contains an entry for the firewall resource type
4. Verify the source indicates "Built-in resource-specific template"

**Expected Result:**
Built-in template usage is recorded.

**Test Data:**
- `TestData/firewall-rule-changes.json`

---

### TC-11: MarkdownRenderer_WithContext_RecordsCustomTemplate

**Type:** Integration

**Description:**
Verifies that `MarkdownRenderer` records when a custom template is used.

**Preconditions:**
- Custom template file
- `DiagnosticContext` instance

**Test Steps:**
1. Create a `DiagnosticContext`
2. Render a plan with a custom template path using `MarkdownRenderer` with the context
3. Verify `context.TemplateResolutions` indicates custom template was used
4. Verify the source includes the custom template path

**Expected Result:**
Custom template usage is recorded with the file path.

**Test Data:**
- Any test plan with custom template

---

### TC-12: MarkdownRenderer_WithContext_RecordsDefaultTemplate

**Type:** Integration

**Description:**
Verifies that `MarkdownRenderer` records when the default template is used (no custom template, no resource-specific template).

**Preconditions:**
- Plan with a resource that has no resource-specific template
- No custom template specified
- `DiagnosticContext` instance

**Test Steps:**
1. Create a `DiagnosticContext`
2. Render a plan with a generic Azure resource (e.g., `azurerm_resource_group`)
3. Verify `context.TemplateResolutions` indicates default template was used

**Expected Result:**
Default template usage is recorded.

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-13: Render_WithoutDebugFlag_NoDebugSection

**Type:** Integration

**Description:**
Verifies that when the `--debug` flag is NOT used, no debug section appears in the output (existing behavior is preserved).

**Preconditions:**
- Valid Terraform plan JSON

**Test Steps:**
1. Process a plan without creating a `DiagnosticContext` (debug flag not set)
2. Render the markdown output
3. Verify the output does NOT contain "## Debug Information" heading

**Expected Result:**
No debug section is present when debug is disabled.

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-14: PrincipalMapper_WithNullContext_FunctionsNormally

**Type:** Regression

**Description:**
Verifies that `PrincipalMapper` functions correctly when no diagnostic context is provided (null).

**Preconditions:**
- Valid principal mapping file
- No `DiagnosticContext`

**Test Steps:**
1. Create a `PrincipalMapper` with a mapping file and `null` context
2. Use the mapper to resolve principal IDs
3. Verify normal functionality (principals are resolved correctly)
4. Verify no exceptions are thrown

**Expected Result:**
`PrincipalMapper` works normally without a diagnostic context.

**Test Data:**
- Test principal mapping file
- Plan with role assignments

---

### TC-15: MarkdownRenderer_WithNullContext_FunctionsNormally

**Type:** Regression

**Description:**
Verifies that `MarkdownRenderer` functions correctly when no diagnostic context is provided (null).

**Preconditions:**
- Valid plan and model
- No `DiagnosticContext`

**Test Steps:**
1. Render a plan with `MarkdownRenderer` passing `null` for the context parameter
2. Verify the output is valid markdown
3. Verify no exceptions are thrown

**Expected Result:**
`MarkdownRenderer` works normally without a diagnostic context.

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-16: DiagnosticContext_GenerateMarkdownSection_PrincipalMapping_FormatsCorrectly

**Type:** Unit

**Description:**
Verifies that principal mapping diagnostics are formatted correctly in the markdown output.

**Preconditions:**
- `DiagnosticContext` with principal mapping data

**Test Steps:**
1. Create a `DiagnosticContext`
2. Set `PrincipalMappingLoadedSuccessfully = true`
3. Set `PrincipalMappingFilePath = "principals.json"`
4. Add type counts: 45 users, 12 groups, 8 service principals
5. Call `GenerateMarkdownSection()`
6. Verify the output contains:
   - "Principal Mapping: Loaded successfully from 'principals.json'"
   - "Found 45 users, 12 groups, 8 service principals"

**Expected Result:**
Output format matches specification:
```
### Principal Mapping

Principal Mapping: Loaded successfully from 'principals.json'
- Found 45 users, 12 groups, 8 service principals
```

**Test Data:**
- Inline test data

---

### TC-17: DiagnosticContext_GenerateMarkdownSection_TemplateResolution_FormatsCorrectly

**Type:** Unit

**Description:**
Verifies that template resolution diagnostics are formatted correctly in the markdown output.

**Preconditions:**
- `DiagnosticContext` with template resolution data

**Test Steps:**
1. Create a `DiagnosticContext`
2. Add template resolutions:
   - `azurerm_firewall_network_rule_collection`: Built-in resource-specific template
   - `azurerm_virtual_network`: Default template
   - `azurerm_custom_resource`: Custom template from '/templates/azurerm/custom_resource.sbn'
3. Call `GenerateMarkdownSection()`
4. Verify the output contains a formatted list with resource types and template sources

**Expected Result:**
Output format matches specification:
```
### Template Resolution

- `azurerm_firewall_network_rule_collection`: Built-in resource-specific template
- `azurerm_virtual_network`: Default template
- `azurerm_custom_resource`: Custom template from '/templates/azurerm/custom_resource.sbn'
```

**Test Data:**
- Inline test data

---

### TC-18: DiagnosticContext_GenerateMarkdownSection_Complete_ValidMarkdown

**Type:** Unit

**Description:**
Verifies that a complete debug section with all diagnostic categories generates valid markdown.

**Preconditions:**
- `DiagnosticContext` with all diagnostic data

**Test Steps:**
1. Create a `DiagnosticContext`
2. Add principal mapping data (load status, type counts, failed resolutions)
3. Add template resolution data
4. Call `GenerateMarkdownSection()`
5. Verify the output:
   - Starts with "## Debug Information" heading
   - Contains "### Principal Mapping" subsection
   - Contains "### Template Resolution" subsection
   - Is valid markdown (no syntax errors)

**Expected Result:**
Complete debug section is well-formatted, valid markdown.

**Test Data:**
- Inline test data

---

### TC-19: EndToEnd_DebugFlag_CLI_ProducesDebugSection

**Type:** End-to-End

**Description:**
Verifies the complete flow from CLI argument parsing through to debug section in output.

**Preconditions:**
- Terraform plan JSON file
- Principal mapping file (optional)

**Test Steps:**
1. Run the CLI with: `tfplan2md plan.json --debug -o output.md`
2. Read the output file
3. Verify it contains "## Debug Information" section
4. Verify principal mapping diagnostics are present (if mapping file provided)
5. Verify template resolution diagnostics are present

**Expected Result:**
End-to-end flow produces complete debug output.

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`
- Test principal mapping file

---

### TC-20: EndToEnd_NoDebugFlag_CLI_NoDebugSection

**Type:** End-to-End

**Description:**
Verifies that without the `--debug` flag, no debug section is produced.

**Preconditions:**
- Terraform plan JSON file

**Test Steps:**
1. Run the CLI with: `tfplan2md plan.json -o output.md` (no --debug flag)
2. Read the output file
3. Verify it does NOT contain "## Debug Information" section
4. Verify the output is identical to normal operation

**Expected Result:**
Without debug flag, no debug section is produced (backward compatible).

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

## Test Data Requirements

### New Test Data Files

1. **`principals-with-types.json`** - Principal mapping file with known counts:
   - 3 users with display names
   - 2 groups with display names
   - 1 service principal with display name

2. **`principals-incomplete.json`** - Principal mapping file missing certain IDs:
   - Contains some valid principals
   - Missing IDs that will be referenced in test plans

3. **`plan-with-role-assignments.json`** (may use existing test data):
   - Plan with multiple role assignments
   - Role assignments reference principal IDs (some present, some missing in mapping file)

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| No principal mapping file provided | Debug section shows "No principal mapping file provided" | TC-05, TC-16 |
| Principal mapping file load fails | Debug section shows load failure | TC-06 |
| No failed principal resolutions | Debug section omits failed resolutions list | TC-16 |
| No template resolutions recorded | Debug section omits or shows minimal template info | TC-17 |
| Empty diagnostic context | No debug section or minimal header only | TC-03 |
| Null diagnostic context | Components function normally without errors | TC-14, TC-15 |

## Non-Functional Tests

### Performance

- **Test:** Verify that collecting diagnostics has negligible performance impact
- **Acceptance Criteria:** Processing time with `--debug` is within 5% of without debug
- **Method:** Time execution with and without `--debug` on a large plan

### Backward Compatibility

- **Test:** Verify all existing tests continue to pass
- **Acceptance Criteria:** No regression in existing test suite (393+ tests)
- **Method:** Run full test suite with new changes

### Markdown Validity

- **Test:** Verify debug section produces valid markdown
- **Acceptance Criteria:** Debug output passes markdownlint validation
- **Method:** Run markdownlint on reports with debug section (TC-18)

## Open Questions

None - all design decisions finalized in the architecture document.

## Testing Strategy Notes

Based on the architecture document, the testing approach follows these principles:

1. **Unit Tests** focus on `DiagnosticContext` markdown generation in isolation
2. **Integration Tests** verify that `PrincipalMapper` and `MarkdownRenderer` correctly collect diagnostics
3. **End-to-End Tests** verify the complete CLI flow with the `--debug` flag
4. **Regression Tests** ensure backward compatibility (null context handling)
5. **UAT Tests** validate markdown rendering in real GitHub and Azure DevOps PR environments

All tests follow the project's conventions:
- Test framework: TUnit (primary), xUnit (legacy)
- Assertion library: AwesomeAssertions
- Test naming: `MethodName_Scenario_ExpectedResult`
- Test execution: `scripts/test-with-timeout.sh -- dotnet test`
