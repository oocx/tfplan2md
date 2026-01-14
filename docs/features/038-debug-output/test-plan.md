# Test Plan: Debug Output

## Overview

This test plan covers the validation of the debug output feature, which provides diagnostic information to help users troubleshoot principal mapping issues and understand template resolution. The feature introduces a `--debug` CLI flag that appends diagnostic information to the markdown report as a new section.

Reference: [specification.md](specification.md) | [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `--debug` flag is available and documented | TC-01, TC-02 | Unit |
| Debug output appended to markdown report | TC-03, TC-04 | Integration |
| Principal mapping diagnostics displayed | TC-05, TC-06, TC-07 | Integration |
| Template resolution diagnostics displayed | TC-08, TC-09 | Integration |
| Debug output disabled by default | TC-10 | Integration |
| Debug does not break existing functionality | TC-11 | Regression |
| Failed principal IDs show resource context | TC-12 | Integration |
| Components work without diagnostic context | TC-13, TC-14 | Unit |

## User Acceptance Scenarios

> **Note**: This feature produces diagnostic output in markdown format appended to the report. No UAT Test Plan is required as the output format is text-based and does not involve visual rendering differences that would benefit from platform-specific review.

## Test Cases

### TC-01: CliParser_DebugFlag_ParsedCorrectly

**Type:** Unit

**Description:**
Verifies that the CLI parser correctly recognizes and parses the `--debug` flag.

**Preconditions:**
- None

**Test Steps:**
1. Call `CliParser.Parse` with `["--debug", "plan.json"]`
2. Call `CliParser.Parse` with `["plan.json", "--debug"]`
3. Call `CliParser.Parse` with `["plan.json"]` (no debug flag)

**Expected Result:**
- First two calls return `CliOptions` with `Debug = true`
- Third call returns `CliOptions` with `Debug = false` (default)

**Test Data:**
Inline test data (argument arrays)

---

### TC-02: HelpTextProvider_IncludesDebugFlag

**Type:** Unit

**Description:**
Verifies that the help text includes documentation for the `--debug` flag.

**Preconditions:**
- None

**Test Steps:**
1. Call `HelpTextProvider.GetHelpText()`
2. Search output for `--debug` documentation

**Expected Result:**
- Help text contains `--debug` flag
- Help text describes the flag's purpose (diagnostic output)

**Test Data:**
None

---

### TC-03: DiagnosticContext_GenerateMarkdownSection_EmptyDiagnostics

**Type:** Unit

**Description:**
Verifies that generating a markdown section from an empty diagnostic context returns an appropriate message or empty section.

**Preconditions:**
- None

**Test Steps:**
1. Create a new `DiagnosticContext` instance
2. Call `GenerateMarkdownSection()`

**Expected Result:**
- Returns a valid markdown section (e.g., "## Debug Information") with indication that no diagnostics were collected
- OR returns empty string if no diagnostics present

**Test Data:**
None

---

### TC-04: DiagnosticContext_GenerateMarkdownSection_WithDiagnostics

**Type:** Unit

**Description:**
Verifies that generating a markdown section with diagnostic data produces correctly formatted markdown.

**Preconditions:**
- None

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Add principal mapping diagnostics:
   - Set `PrincipalMappingFileProvided = true`
   - Set `PrincipalMappingLoadedSuccessfully = true`
   - Set `PrincipalMappingFilePath = "principals.json"`
   - Add type counts: 45 users, 12 groups, 8 service principals
   - Add 2 failed resolutions
3. Add template resolution diagnostics:
   - Add 3 template resolutions (built-in, custom, default)
4. Call `GenerateMarkdownSection()`

**Expected Result:**
- Returns markdown section with:
  - Heading: "## Debug Information"
  - "### Principal Mapping" subsection
  - Load success message with file path
  - Type counts formatted as list or prose
  - Failed resolutions with resource addresses
  - "### Template Resolution" subsection
  - Template resolutions listed with resource types

**Test Data:**
Inline test data (diagnostic properties)

---

### TC-05: PrincipalMapper_WithDiagnosticContext_RecordsLoadSuccess

**Type:** Integration

**Description:**
Verifies that PrincipalMapper records successful file load in the diagnostic context.

**Preconditions:**
- Valid principal mapping JSON file exists

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Create `PrincipalMapper` with mapping file path and diagnostic context
3. Inspect diagnostic context properties

**Expected Result:**
- `PrincipalMappingFileProvided = true`
- `PrincipalMappingLoadedSuccessfully = true`
- `PrincipalMappingFilePath` matches provided path
- `PrincipalTypeCount` contains correct counts for each type

**Test Data:**
- `TestData/principal-mapping.json` (existing test data)

---

### TC-06: PrincipalMapper_WithDiagnosticContext_RecordsLoadFailure

**Type:** Integration

**Description:**
Verifies that PrincipalMapper records failed file load in the diagnostic context.

**Preconditions:**
- None (using non-existent file path)

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Attempt to create `PrincipalMapper` with invalid file path and diagnostic context
3. Inspect diagnostic context properties

**Expected Result:**
- `PrincipalMappingFileProvided = true`
- `PrincipalMappingLoadedSuccessfully = false`
- `PrincipalMappingFilePath` matches provided path
- `PrincipalTypeCount` is empty

**Test Data:**
None (invalid file path)

---

### TC-07: PrincipalMapper_WithDiagnosticContext_RecordsTypeCounts

**Type:** Integration

**Description:**
Verifies that PrincipalMapper correctly counts and records principals by type.

**Preconditions:**
- Principal mapping file with known counts exists

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Create `PrincipalMapper` with mapping file and diagnostic context
3. Inspect `PrincipalTypeCount` in diagnostic context

**Expected Result:**
- Type counts match the actual counts in the mapping file
- All principal types present in file are counted
- Count dictionary keys match expected type names

**Test Data:**
- `TestData/principal-mapping.json` with known type distribution

---

### TC-08: MarkdownRenderer_WithDiagnosticContext_RecordsTemplateResolution

**Type:** Integration

**Description:**
Verifies that MarkdownRenderer records template resolution decisions in the diagnostic context.

**Preconditions:**
- Test plan with multiple resource types
- Custom template file exists

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Create `MarkdownRenderer` instance
3. Render a report model with:
   - A resource using a custom template
   - A resource using a built-in resource-specific template
   - A resource using the default template
4. Pass diagnostic context to render method
5. Inspect `TemplateResolutions` in diagnostic context

**Expected Result:**
- Contains 3 template resolutions
- Each resolution shows correct resource type and template source
- Custom template shows file path
- Built-in template indicates "built-in resource-specific"
- Default template indicates "default template"

**Test Data:**
- `TestData/multi-resource-plan.json` (plan with varied resources)
- Custom template file

---

### TC-09: MarkdownRenderer_WithoutCustomTemplate_RecordsBuiltInUsage

**Type:** Integration

**Description:**
Verifies that template resolution is recorded correctly when using only built-in templates.

**Preconditions:**
- Test plan with firewall rule resources (known to have built-in template)

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Render plan without specifying custom template
3. Inspect `TemplateResolutions` in diagnostic context

**Expected Result:**
- Firewall resources show "built-in resource-specific template"
- Other resources show "default template"

**Test Data:**
- `TestData/firewall-rule-changes.json`

---

### TC-10: EndToEnd_WithoutDebugFlag_NoDebugSection

**Type:** Integration

**Description:**
Verifies that debug output is NOT appended when `--debug` flag is absent (default behavior).

**Preconditions:**
- Valid Terraform plan file

**Test Steps:**
1. Parse CLI arguments without `--debug` flag
2. Process plan and generate markdown
3. Inspect final markdown output

**Expected Result:**
- Output does NOT contain "## Debug Information" section
- No diagnostic information in output
- Report renders normally

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-11: EndToEnd_WithDebugFlag_AppendedToReport

**Type:** Integration

**Description:**
Verifies that debug output is appended to the markdown report when `--debug` flag is present.

**Preconditions:**
- Valid Terraform plan file
- Principal mapping file

**Test Steps:**
1. Parse CLI arguments with `--debug` flag
2. Process plan with principal mapping and generate markdown
3. Inspect final markdown output

**Expected Result:**
- Output contains "## Debug Information" section at the end
- Debug section includes principal mapping diagnostics
- Debug section includes template resolution diagnostics
- Main report content is unchanged
- Debug section is properly formatted markdown

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`
- `TestData/principal-mapping.json`

---

### TC-12: PrincipalMapper_FailedResolution_RecordsResourceContext

**Type:** Integration

**Description:**
Verifies that failed principal ID resolutions record which resource referenced each ID.

**Preconditions:**
- Plan with role assignments referencing principal IDs
- Principal mapping file that doesn't contain some of those IDs

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Create `PrincipalMapper` with incomplete mapping file
3. Build report model (which attempts to resolve principals)
4. Inspect `FailedResolutions` in diagnostic context

**Expected Result:**
- Each failed resolution includes:
  - Principal ID that failed to resolve
  - Resource address that referenced the ID
- Multiple resources referencing same ID show multiple entries
- Resource addresses are correct and complete

**Test Data:**
- `TestData/role-assignments-plan.json` (plan with role assignments)
- `TestData/partial-principal-mapping.json` (mapping missing some IDs)

---

### TC-13: PrincipalMapper_WithNullContext_WorksNormally

**Type:** Unit

**Description:**
Verifies that PrincipalMapper functions normally when diagnostic context is null (backward compatibility).

**Preconditions:**
- Valid principal mapping file

**Test Steps:**
1. Create `PrincipalMapper` with `null` diagnostic context
2. Use mapper to resolve principal IDs
3. Verify resolution works

**Expected Result:**
- No errors or exceptions
- Principal resolution works as expected
- No diagnostic information collected (as context is null)

**Test Data:**
- `TestData/principal-mapping.json`

---

### TC-14: MarkdownRenderer_WithNullContext_WorksNormally

**Type:** Integration

**Description:**
Verifies that MarkdownRenderer functions normally when diagnostic context is null (backward compatibility).

**Preconditions:**
- Valid test plan

**Test Steps:**
1. Create `MarkdownRenderer` instance
2. Render report with `null` diagnostic context
3. Verify rendering works

**Expected Result:**
- No errors or exceptions
- Markdown is rendered correctly
- No diagnostic information collected (as context is null)

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-15: DiagnosticContext_FailedPrincipalResolution_FormatsCorrectly

**Type:** Unit

**Description:**
Verifies that failed principal resolutions are formatted correctly in the markdown section.

**Preconditions:**
- None

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Add failed resolutions:
   - `FailedPrincipalResolution("12345678-1234-1234-1234-123456789012", "azurerm_role_assignment.example")`
   - `FailedPrincipalResolution("87654321-4321-4321-4321-210987654321", "azurerm_role_assignment.reader")`
3. Call `GenerateMarkdownSection()`

**Expected Result:**
- Failed resolutions appear as markdown list
- Each entry shows:
  - Principal ID in code format (e.g., `` `12345678-...` ``)
  - Resource address in code format (e.g., `referenced in azurerm_role_assignment.example`)
- Format matches specification example

**Test Data:**
Inline test data (failed resolution records)

---

### TC-16: DiagnosticContext_TemplateResolution_FormatsCorrectly

**Type:** Unit

**Description:**
Verifies that template resolutions are formatted correctly in the markdown section.

**Preconditions:**
- None

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Add template resolutions:
   - `TemplateResolution("azurerm_firewall_network_rule_collection", "Built-in resource-specific template")`
   - `TemplateResolution("azurerm_virtual_network", "Default template")`
   - `TemplateResolution("azurerm_custom_resource", "Custom template: /templates/azurerm/custom_resource.sbn")`
3. Call `GenerateMarkdownSection()`

**Expected Result:**
- Template resolutions appear as markdown list
- Each entry shows:
  - Resource type in code format
  - Template source description
- Format matches specification example

**Test Data:**
Inline test data (template resolution records)

---

### TC-17: EndToEnd_Docker_WithDebugFlag

**Type:** Integration (Docker)

**Description:**
Verifies that the `--debug` flag works correctly in the Docker container environment.

**Preconditions:**
- Docker container built and available
- Valid Terraform plan file

**Test Steps:**
1. Run container with `--debug` flag:
   ```bash
   docker run -v $(pwd)/test-plan.json:/plan.json tfplan2md --debug /plan.json
   ```
2. Inspect container output

**Expected Result:**
- Container runs successfully
- Output contains markdown report with debug section
- Debug section includes diagnostic information
- Exit code is 0

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-18: Regression_AllExistingTests_PassWithDebugCode

**Type:** Regression

**Description:**
Verifies that all existing tests continue to pass after adding debug output feature.

**Preconditions:**
- All existing tests in test suite

**Test Steps:**
1. Run full test suite: `dotnet test tests/Oocx.TfPlan2Md.TUnit/`

**Expected Result:**
- All existing tests pass
- No regressions introduced by diagnostic context changes
- Test execution completes within expected timeframe

**Test Data:**
All existing test data

---

### TC-19: PrincipalMapper_MultipleSameFailedId_RecordsAllReferences

**Type:** Integration

**Description:**
Verifies that when the same principal ID fails to resolve in multiple resources, all resource contexts are recorded.

**Preconditions:**
- Plan with multiple resources referencing the same principal ID
- Principal mapping file missing that ID

**Test Steps:**
1. Create a `DiagnosticContext` instance
2. Create `PrincipalMapper` with incomplete mapping
3. Process multiple resources referencing same missing principal ID
4. Inspect `FailedResolutions` in diagnostic context

**Expected Result:**
- Multiple entries for the same principal ID
- Each entry shows different resource address
- All resources that referenced the ID are captured

**Test Data:**
- Custom test plan with multiple role assignments using same principal ID
- Principal mapping file without that ID

---

### TC-20: DiagnosticContext_NoPrincipalMappingFile_ShowsNotProvided

**Type:** Integration

**Description:**
Verifies that diagnostic output correctly indicates when no principal mapping file was provided.

**Preconditions:**
- Valid test plan without principal mapping

**Test Steps:**
1. Create a `DiagnosticContext` instance (context not updated with mapping info)
2. Call `GenerateMarkdownSection()`

**Expected Result:**
- Debug section indicates no principal mapping file was provided
- OR Principal Mapping subsection is omitted entirely
- No errors or null reference exceptions

**Test Data:**
None

---

## Test Data Requirements

### Existing Test Data Files
- `TestData/azurerm-azuredevops-plan.json` - Primary test plan
- `TestData/principal-mapping.json` - Valid principal mapping file
- `TestData/firewall-rule-changes.json` - Plan with built-in template resources

### New Test Data Files Needed

1. **`TestData/partial-principal-mapping.json`**
   - Description: Principal mapping file with intentionally missing IDs for testing failed resolution
   - Contents: Valid JSON with subset of principals from full mapping file

2. **`TestData/role-assignments-plan.json`**
   - Description: Terraform plan focused on role assignments with principal IDs
   - Contents: Multiple `azurerm_role_assignment` resources with various principal IDs

3. **`TestData/multi-resource-plan.json`**
   - Description: Plan with diverse resource types for testing template resolution tracking
   - Contents: Mix of resources that use default template, built-in templates, and custom templates

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Principal mapping file doesn't exist | Record load failure in diagnostics | TC-06 |
| No principal mapping file provided | No principal mapping section OR "not provided" message | TC-20 |
| Empty principal mapping file | Load success with zero type counts | TC-07 |
| Diagnostic context is null | Components work normally without diagnostics | TC-13, TC-14 |
| Multiple resources reference same failed ID | All references recorded | TC-19 |
| Debug section generation fails | Should not break report generation | TC-04 |
| Plan with only default template | Shows all as default template | TC-08 |
| Very long resource address | Formatted correctly without breaking markdown | TC-15 |

## Non-Functional Tests

### Performance
- Debug information collection should not add more than 5% overhead to processing time
- Test with large plan (100+ resources) to ensure diagnostic collection scales

### Compatibility
- Verify backward compatibility: existing tests pass without changes
- Verify Docker integration: debug flag works in containerized environment

## Test Execution Strategy

### Unit Tests
Execute with:
```bash
dotnet test tests/Oocx.TfPlan2Md.TUnit/ -- --treenode-filter /**[Category=Unit]
```

### Integration Tests
Execute with:
```bash
scripts/test-with-timeout.sh -- dotnet test tests/Oocx.TfPlan2Md.TUnit/ -- --treenode-filter /**[Category=Integration]
```

### Regression Tests
Execute full suite:
```bash
scripts/test-with-timeout.sh -- dotnet test tests/Oocx.TfPlan2Md.TUnit/
```

## Open Questions

None - all design decisions have been finalized in the architecture document.

## Definition of Done

- [ ] All test cases (TC-01 through TC-20) are implemented
- [ ] All test data files are created
- [ ] All tests pass when executed via `scripts/test-with-timeout.sh -- dotnet test`
- [ ] Edge cases are covered
- [ ] Regression tests confirm no existing functionality is broken
- [ ] Docker integration test verifies containerized behavior
- [ ] Test execution completes within expected timeframe (< 60 seconds for unit tests)
