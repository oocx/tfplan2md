# Test Plan: Azure DevOps Repository Mapping and Branch/Repo Icons

## Overview

This test plan verifies the Azure DevOps repository mapping and icon feature that extends the existing mapping infrastructure to support Azure DevOps repository GUIDs and adds semantic icons for repository and branch/ref attributes. The feature enables users to map repository GUIDs to human-readable names and provides visual distinction for repositories (🗃️) and branches (⎇) in rendered Terraform plan reports.

**Related Documents:**
- Specification: `docs/features/096-azdo-repo-mapping-and-icons/specification.md`
- Architecture: `docs/features/096-azdo-repo-mapping-and-icons/architecture.md`
- Template Feature: `docs/features/085-azdo-principal-mapping/` (mapping pattern reference)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `PrincipalMappingFile` includes `AzdoRepositories` property | TC-01, TC-02 | Unit |
| `AzureMappingFileParser` parses `azdoRepositories` section | TC-03, TC-04, TC-05 | Unit |
| Repository GUIDs resolved to display names | TC-08, TC-09, TC-12 | Unit |
| Display format: `🗃️ DisplayName [GUID]` when mapped | TC-08, TC-13 | Unit |
| Display format: `🗃️ GUID` when unmapped | TC-09, TC-14 | Unit |
| 🗃️ icon applied to repository attributes | TC-15, TC-16, TC-17 | Unit |
| ⎇ icon applied to branch/ref attributes | TC-18, TC-19, TC-20 | Unit |
| Icons use non-breaking spaces | TC-15, TC-16, TC-18, TC-19 | Unit |
| Icons render in table and summary contexts | TC-16, TC-19 | Unit |
| Empty/null `azdoRepositories` section handled gracefully | TC-06, TC-07 | Unit |
| Diagnostic output includes repository mapping counts | TC-21, TC-22 | Unit |
| Backwards compatibility maintained | TC-07, TC-23 | Integration |
| Tests verify repository mapping and icon rendering | All tests | All types |

## Test Cases

### Data Model Tests

#### TC-01: PrincipalMappingFile Deserializes AzdoRepositories Section

**Type:** Unit

**Description:**
Verifies that the `PrincipalMappingFile` class correctly deserializes the `azdoRepositories` JSON section into the `AzdoRepositories` property.

**Preconditions:**
- Test mapping file with `azdoRepositories` section

**Test Steps:**
1. Create JSON with `azdoRepositories` section containing multiple repository mappings
2. Deserialize JSON to `PrincipalMappingFile` object
3. Verify `AzdoRepositories` property is not null
4. Verify each repository mapping is present in the dictionary

**Expected Result:**
- `AzdoRepositories` property contains all repository mappings from JSON
- Repository GUIDs map to correct display names

**Test Data:**
```json
{
  "azdoRepositories": {
    "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo",
    "f9e8d7c6-b5a4-3210-fedc-ba9876543210": "Web Application Repo"
  }
}
```

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/PrincipalMappingFileTests.cs`

---

#### TC-02: PrincipalMappingFile Deserializes All Azdo Sections Including Repositories

**Type:** Unit

**Description:**
Verifies that the `PrincipalMappingFile` class correctly deserializes all azdo sections (`azdoUsers`, `azdoGroups`, `azdoProjects`, `azdoRepositories`) simultaneously.

**Preconditions:**
- Test mapping file with all four azdo sections

**Test Steps:**
1. Create JSON with all four azdo sections containing mappings
2. Deserialize JSON to `PrincipalMappingFile` object
3. Verify all four properties are not null
4. Verify mappings in each section are correct

**Expected Result:**
- All four azdo properties contain correct mappings
- Sections are independent (no cross-contamination)

**Test Data:**
```json
{
  "azdoUsers": {
    "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith"
  },
  "azdoGroups": {
    "vssgp.Uy0xLTktMTU1MTM...": "Platform Team"
  },
  "azdoProjects": {
    "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project"
  },
  "azdoRepositories": {
    "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo"
  }
}
```

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/PrincipalMappingFileTests.cs`

---

### Parser Tests

#### TC-03: AzureMappingFileParser Parses AzdoRepositories Section

**Type:** Unit

**Description:**
Verifies that the parser correctly extracts azdo repository mappings and returns them in `AzureMappingFileResult`.

**Preconditions:**
- Test mapping file with `azdoRepositories` section

**Test Steps:**
1. Create test JSON file with `azdoRepositories` section
2. Parse file using `AzureMappingFileParser`
3. Verify `AzureMappingFileResult.AzdoRepositories` contains expected mappings
4. Verify the dictionary is a `FrozenDictionary<string, string>`

**Expected Result:**
- Parser returns azdo repository mappings in result object
- Mappings are correct and complete

**Test Data:**
Inline test file with 2-3 repository mappings (GUIDs to display names)

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

---

#### TC-04: AzureMappingFileParser Handles Mixed Azure and All Azdo Sections

**Type:** Unit

**Description:**
Verifies that the parser correctly handles mapping files containing Azure AD sections and all four azdo sections including repositories.

**Preconditions:**
- Test mapping file with both Azure AD and all azdo sections

**Test Steps:**
1. Create test JSON with users, groups, servicePrincipals, AND all four azdo sections
2. Parse file using `AzureMappingFileParser`
3. Verify Azure AD mappings are in `Principals` dictionary
4. Verify azdo mappings are in separate dictionaries (`AzdoUsers`, `AzdoGroups`, `AzdoProjects`, `AzdoRepositories`)
5. Verify no cross-contamination between sections

**Expected Result:**
- Azure AD principals remain in their existing dictionary
- Azdo entities are in separate dictionaries
- All mapping types work correctly

**Test Data:**
Comprehensive test file similar to `examples/comprehensive-demo/demo-principals-nested.json` with `azdoRepositories` added

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

---

#### TC-05: AzureMappingFileParser Handles Empty AzdoRepositories Section

**Type:** Unit

**Description:**
Verifies that the parser gracefully handles mapping files with an empty `azdoRepositories` object.

**Preconditions:**
- Test mapping file with empty `azdoRepositories` object

**Test Steps:**
1. Create test JSON with `"azdoRepositories": {}`
2. Parse file using `AzureMappingFileParser`
3. Verify parsing succeeds without errors
4. Verify `AzdoRepositories` dictionary in result is empty (not null)

**Expected Result:**
- Parser does not throw exceptions
- Result contains empty dictionary for repositories

**Test Data:**
```json
{
  "azdoUsers": { "user-1": "Test User" },
  "azdoRepositories": {}
}
```

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

---

#### TC-06: AzureMappingFileParser Handles Null AzdoRepositories Section

**Type:** Unit

**Description:**
Verifies that the parser gracefully handles mapping files where `azdoRepositories` is explicitly null.

**Preconditions:**
- Test mapping file with `azdoRepositories` set to null

**Test Steps:**
1. Create test JSON with `"azdoRepositories": null`
2. Parse file using `AzureMappingFileParser`
3. Verify parsing succeeds without errors
4. Verify `AzdoRepositories` dictionary in result is empty (not null)

**Expected Result:**
- Parser does not throw exceptions
- Result contains empty dictionary for repositories

**Test Data:**
```json
{
  "azdoUsers": { "user-1": "Test User" },
  "azdoRepositories": null
}
```

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

---

#### TC-07: AzureMappingFileParser Handles Missing AzdoRepositories Section (Backwards Compatibility)

**Type:** Unit

**Description:**
Verifies that existing mapping files without `azdoRepositories` section continue to work (backwards compatibility).

**Preconditions:**
- Test mapping file without `azdoRepositories` section (existing format from Feature 085)

**Test Steps:**
1. Use existing test file with only Azure AD and azdo user/group/project sections (no repositories)
2. Parse file using updated `AzureMappingFileParser`
3. Verify parsing succeeds
4. Verify other mappings work correctly
5. Verify `AzdoRepositories` dictionary in result is empty

**Expected Result:**
- Existing mapping files work without modification
- No breaking changes to existing functionality

**Test Data:**
Existing test file: `examples/comprehensive-demo/demo-principals-nested.json` (before adding repositories)

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

---

### Mapper Tests

#### TC-08: AzdoRepositoryMapper Resolves Known Repository IDs

**Type:** Unit

**Description:**
Verifies that `AzdoRepositoryMapper` correctly resolves repository GUIDs to display names in the format `🗃️ DisplayName [GUID]`.

**Preconditions:**
- Mapper initialized with repository mappings

**Test Steps:**
1. Create `AzdoRepositoryMapper` with test repository mappings
2. Call `GetEntityName("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d")`
3. Verify result is in format `🗃️ DisplayName [GUID]`
4. Verify icon uses non-breaking space

**Expected Result:**
- Method returns `"🗃️\u00A0Infrastructure Repo [a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d]"`
- Format includes 🗃️ icon, display name, and GUID in brackets

**Test Data:**
Dictionary with one repository mapping: `"a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d" => "Infrastructure Repo"`

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoRepositoryMapperTests.cs` (NEW)

---

#### TC-09: AzdoRepositoryMapper Returns Icon Plus GUID for Unknown Repository IDs

**Type:** Unit

**Description:**
Verifies that `AzdoRepositoryMapper` returns the repository GUID with icon when a mapping is not found.

**Preconditions:**
- Mapper initialized with limited mappings

**Test Steps:**
1. Create `AzdoRepositoryMapper` with test mappings
2. Call `GetEntityName("unknown-repo-guid")`
3. Verify result is just the GUID with icon (no display name, no exception)

**Expected Result:**
- Method returns `"🗃️\u00A0unknown-repo-guid"`
- Format includes only icon and raw GUID

**Test Data:**
Mapper with empty dictionary

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoRepositoryMapperTests.cs` (NEW)

---

#### TC-10: AzdoRepositoryMapper GetName Returns Display Name for Mapped Repositories

**Type:** Unit

**Description:**
Verifies that `GetName()` method returns just the display name (without icon or GUID) for mapped repositories.

**Preconditions:**
- Mapper initialized with repository mappings

**Test Steps:**
1. Create `AzdoRepositoryMapper` with test repository mappings
2. Call `GetName("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d")`
3. Verify result is just the display name

**Expected Result:**
- Method returns `"Infrastructure Repo"` (no icon, no GUID)

**Test Data:**
Dictionary with repository mapping

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoRepositoryMapperTests.cs` (NEW)

---

#### TC-11: AzdoRepositoryMapper GetName Returns Null for Unknown Repositories

**Type:** Unit

**Description:**
Verifies that `GetName()` returns null when a repository ID is not mapped.

**Preconditions:**
- Mapper initialized with limited mappings

**Test Steps:**
1. Create `AzdoRepositoryMapper` with limited mappings
2. Call `GetName("unknown-repo-guid")`
3. Verify result is null

**Expected Result:**
- Method returns `null`

**Test Data:**
Mapper with empty or limited dictionary

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoRepositoryMapperTests.cs` (NEW)

---

#### TC-12: AzdoRepositoryMapper Tracks Failed Resolutions in Diagnostics

**Type:** Unit

**Description:**
Verifies that the mapper records failed resolutions in the diagnostic context when a resource address is provided.

**Preconditions:**
- Mapper initialized with diagnostic context
- Mapper has limited mappings

**Test Steps:**
1. Create mapper with diagnostic context
2. Call `GetName("unknown-repo-guid", "azuredevops_build_definition.example")`
3. Verify diagnostic context contains failed resolution entry
4. Verify failed resolution has correct type and ID

**Expected Result:**
- DiagnosticContext.FailedResolutions contains entry
- Entry has FailedResolutionType.AzdoRepository
- Entry includes the unmapped GUID and resource address

**Test Data:**
Mapper with empty dictionary and diagnostic context

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoRepositoryMapperTests.cs` (NEW)

---

### Value Formatter Tests

#### TC-13: AzdoRepositoryIdFormatter Formats Mapped Repository IDs

**Type:** Unit

**Description:**
Verifies that `AzdoRepositoryIdFormatter` correctly formats repository IDs with display names and wraps in code format.

**Preconditions:**
- Formatter initialized with mapper containing mappings

**Test Steps:**
1. Create `AzdoRepositoryIdFormatter` with test mapper
2. Create ServiceResolutionContext with repository_id attribute and GUID value
3. Call `TryFormat(context)`
4. Verify formatted output includes icon, display name, and GUID in parentheses
5. Verify output is wrapped in code format (backticks for table context)

**Expected Result:**
- Method returns formatted value: `` `🗃️\u00A0Infrastructure Repo (a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d)` ``
- Format includes icon, display name, GUID in parentheses, and code wrapping

**Test Data:**
Mapper with repository mapping, context with repository attribute

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoValueFormatterTests.cs` (extend existing)

---

#### TC-14: AzdoRepositoryIdFormatter Returns Null for Unmapped Repository IDs

**Type:** Unit

**Description:**
Verifies that the formatter returns null for unmapped repository IDs (allowing semantic formatting to apply icon only).

**Preconditions:**
- Formatter initialized with mapper without mappings

**Test Steps:**
1. Create `AzdoRepositoryIdFormatter` with empty mapper
2. Create ServiceResolutionContext with repository_id attribute and unmapped GUID
3. Call `TryFormat(context)`
4. Verify method returns null

**Expected Result:**
- Method returns `null` (semantic formatting will apply icon instead)

**Test Data:**
Empty mapper, context with unmapped repository GUID

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoValueFormatterTests.cs` (extend existing)

---

### Semantic Icon Formatting Tests

#### TC-15: FormatAttributeValueTable Applies Repository Icon to Repository Attributes

**Type:** Unit

**Description:**
Verifies that the semantic formatting system applies 🗃️ icon to all repository-related attribute names.

**Preconditions:**
- None (tests static helper methods)

**Test Steps:**
1. Test each repository attribute name: `repo_id`, `repository_id`, `source_repo_id`, `target_repo_id`
2. Call `FormatAttributeValueTable(attributeName, "test-guid", null)`
3. Verify result includes 🗃️ icon
4. Verify result uses non-breaking space
5. Verify result is wrapped in code backticks

**Expected Result:**
- For `repository_id` attribute: `` `🗃️\u00A0test-guid` ``
- Same pattern for all repository attributes
- Icon uses non-breaking space (\u00A0)

**Test Data:**
Test cases for: repo_id, repository_id, source_repo_id, target_repo_id

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

---

#### TC-16: FormatAttributeValueSummary Applies Repository Icon Without Code Wrapping

**Type:** Unit

**Description:**
Verifies that repository icons render correctly in summary context (without code wrapping).

**Preconditions:**
- None (tests static helper methods)

**Test Steps:**
1. Call `FormatAttributeValueSummary("repository_id", "test-guid", null)`
2. Verify result includes 🗃️ icon
3. Verify result uses non-breaking space
4. Verify result is NOT wrapped in code tags

**Expected Result:**
- Result: `🗃️\u00A0test-guid` (no code tags)

**Test Data:**
repository_id attribute with test value

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

---

#### TC-17: FormatAttributeValuePlain Applies Repository Icon

**Type:** Unit

**Description:**
Verifies that repository icons render correctly in plain context.

**Preconditions:**
- None (tests static helper methods)

**Test Steps:**
1. Call `FormatAttributeValuePlain("repository_id", "test-guid", null)`
2. Verify result includes 🗃️ icon with non-breaking space

**Expected Result:**
- Result: `🗃️\u00A0test-guid`

**Test Data:**
repository_id attribute with test value

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

---

#### TC-18: FormatAttributeValueTable Applies Branch Icon to Branch Attributes

**Type:** Unit

**Description:**
Verifies that the semantic formatting system applies ⎇ icon to all branch-related attribute names.

**Preconditions:**
- None (tests static helper methods)

**Test Steps:**
1. Test each branch attribute name: `default_branch`, `branch_name`, `ref_name`, `source_branch`, `target_branch`
2. Call `FormatAttributeValueTable(attributeName, "refs/heads/main", null)`
3. Verify result includes ⎇ icon
4. Verify result uses non-breaking space
5. Verify result is wrapped in code backticks

**Expected Result:**
- For `default_branch` attribute: `` `⎇\u00A0refs/heads/main` ``
- Same pattern for all branch attributes
- Icon uses non-breaking space (\u00A0)

**Test Data:**
Test cases for: default_branch, branch_name, ref_name, source_branch, target_branch

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

---

#### TC-19: FormatAttributeValueSummary Applies Branch Icon Without Code Wrapping

**Type:** Unit

**Description:**
Verifies that branch icons render correctly in summary context (without code wrapping).

**Preconditions:**
- None (tests static helper methods)

**Test Steps:**
1. Call `FormatAttributeValueSummary("default_branch", "refs/heads/main", null)`
2. Verify result includes ⎇ icon
3. Verify result uses non-breaking space
4. Verify result is NOT wrapped in code tags

**Expected Result:**
- Result: `⎇\u00A0refs/heads/main` (no code tags)

**Test Data:**
default_branch attribute with test value

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

---

#### TC-20: FormatAttributeValueTable Does Not Apply Branch Icon to Non-Branch Attributes

**Type:** Unit

**Description:**
Verifies that the branch icon is NOT applied to attributes that don't match branch attribute names.

**Preconditions:**
- None (tests static helper methods)

**Test Steps:**
1. Call `FormatAttributeValueTable("branch", "value", null)` (partial match should not trigger)
2. Call `FormatAttributeValueTable("description", "branch", null)` (value match should not trigger)
3. Verify results do NOT include ⎇ icon

**Expected Result:**
- Results should be plain formatted values without branch icon

**Test Data:**
Non-branch attribute names with branch-like values

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

---

### Diagnostic Tests

#### TC-21: DiagnosticContext Tracks AzdoRepository Count

**Type:** Unit

**Description:**
Verifies that the `DiagnosticContext` correctly tracks the count of repository mappings.

**Preconditions:**
- Diagnostic context initialized
- Mapping file loaded with `azdoRepositories` section

**Test Steps:**
1. Create diagnostic context
2. Load mapping file with `azdoRepositories` section containing 3 mappings
3. Verify `AzdoRepositoryCount` equals 3

**Expected Result:**
- `AzdoRepositoryCount` property reflects correct number

**Test Data:**
Mapping file with 3 repository mappings

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

---

#### TC-22: Diagnostic Output Includes AzdoRepository Count

**Type:** Integration

**Description:**
Verifies that the diagnostic output (when `--debug` is used) includes repository count in the mapping section.

**Preconditions:**
- Diagnostic context with repository count
- Debug output generation enabled

**Test Steps:**
1. Load mapping file with `azdoRepositories` section
2. Generate diagnostic output
3. Verify output includes repository count
4. Verify format is consistent with other azdo entity counts

**Expected Result:**
- Diagnostic output shows repository count
- Format matches: "Found N azdo repositories"

**Test Data:**
Mapping file with 2 repository mappings

**Test File Location:**
Integration test or manual validation with `--debug` flag

---

### Scriban Helper Tests

#### TC-23: azdo_repository_name Helper Resolves Known Repository IDs

**Type:** Unit

**Description:**
Verifies that the `azdo_repository_name` Scriban helper correctly resolves repository GUIDs.

**Preconditions:**
- Helper registered with `AzdoRepositoryMapper`

**Test Steps:**
1. Set up Scriban context with `azdo_repository_name` helper
2. Render template: `{{ azdo_repository_name "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d" }}`
3. Verify output matches expected format with icon

**Expected Result:**
- Rendered output: `🗃️\u00A0Infrastructure Repo [a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d]`

**Test Data:**
Mapper with test repository mapping

**Test File Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoValueFormatterTests.cs` or new Scriban helper test file

---

### Integration Tests

#### TC-24: End-to-End Rendering with Azure DevOps Build Definition

**Type:** Integration

**Description:**
Verifies that repository names and branch icons appear correctly in rendered output for `azuredevops_build_definition` resources.

**Preconditions:**
- Test plan JSON with `azuredevops_build_definition` resources
- Mapping file with repository mappings

**Test Steps:**
1. Create test Terraform plan with `azuredevops_build_definition` resources containing `repository_id` and `default_branch`
2. Create mapping file with repository mappings
3. Render markdown with mapping file
4. Verify repository IDs appear as `🗃️ DisplayName (GUID)` in table context
5. Verify branch names appear with ⎇ icon
6. Compare against snapshot baseline

**Expected Result:**
- Repository IDs show display names with icon
- Branch names show with ⎇ icon
- Format matches specification

**Test Data:**
- Test plan: `azuredevops-build-definition-repo-mapping.json` (NEW)
- Mapping file with repository mappings
- Expected snapshot: `azuredevops-build-definition-repo-mapping.md`

**Test File Location:**
Snapshot test in `src/tests/Oocx.TfPlan2Md.TUnit/` or `tests/` directory

---

#### TC-25: End-to-End Rendering with Azure DevOps Git Repository

**Type:** Integration

**Description:**
Verifies that repository and branch icons appear correctly in rendered output for `azuredevops_git_repository` resources.

**Preconditions:**
- Test plan JSON with `azuredevops_git_repository` resources
- Mapping file with repository mappings

**Test Steps:**
1. Create test Terraform plan with `azuredevops_git_repository` resources
2. Create mapping file with repository mappings
3. Render markdown with mapping file
4. Verify `default_branch` displays with ⎇ icon
5. Verify mapped repository fields show display names

**Expected Result:**
- Branch references show ⎇ icon
- Repository fields use 🗃️ icon
- Output is more readable than without icons

**Test Data:**
- Test plan: `azuredevops-git-repository-icons.json` (NEW)
- Mapping file with repository mappings
- Expected snapshot: `azuredevops-git-repository-icons.md`

**Test File Location:**
Snapshot test in `src/tests/Oocx.TfPlan2Md.TUnit/` or `tests/` directory

---

#### TC-26: End-to-End Rendering with Branch Policy Resources

**Type:** Integration

**Description:**
Verifies that both repository mappings and branch icons work together in `azuredevops_branch_policy_*` resources.

**Preconditions:**
- Test plan JSON with `azuredevops_branch_policy_min_reviewers` resources
- Mapping file with repository mappings

**Test Steps:**
1. Create test Terraform plan with branch policy resources containing `repository_id` and branch attributes
2. Create mapping file with repository mappings
3. Render markdown with mapping file
4. Verify `repository_id` appears with 🗃️ icon and display name
5. Verify branch references appear with ⎇ icon

**Expected Result:**
- Repository attributes formatted: `🗃️ DisplayName (GUID)`
- Branch attributes formatted: `⎇ branch-value`
- Both icons render correctly in same resource

**Test Data:**
- Test plan: `azuredevops-branch-policy-icons.json` (NEW)
- Mapping file with repository mappings
- Expected snapshot: `azuredevops-branch-policy-icons.md`

**Test File Location:**
Snapshot test in `src/tests/Oocx.TfPlan2Md.TUnit/` or `tests/` directory

---

#### TC-27: Example Mapping File Demonstrates AzdoRepositories Section

**Type:** Integration

**Description:**
Verifies that the example mapping file includes `azdoRepositories` section with realistic data.

**Preconditions:**
- Example mapping file at `examples/comprehensive-demo/demo-principals-nested.json`

**Test Steps:**
1. Load the example mapping file
2. Verify it contains `azdoRepositories` section
3. Verify section has realistic repository GUIDs and display names
4. Verify file remains valid JSON
5. Verify file parses successfully

**Expected Result:**
- Example file parses successfully
- `azdoRepositories` section demonstrates proper usage
- Documentation references the example file

**Test Data:**
Updated `examples/comprehensive-demo/demo-principals-nested.json`

**Test File Location:**
Manual verification + integration test that loads example files

---

## Test Data Requirements

### New Test Data Files

1. **`azdo-repositories-mapping.json`** - Comprehensive mapping file with `azdoRepositories` section
   - 3 repository mappings with realistic GUIDs
   - Also includes other azdo sections for mixed testing
   - Located in: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`

2. **`azdo-repositories-only.json`** - Mapping file with only `azdoRepositories` section
   - Used for isolated repository mapping tests
   - Located in: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`

3. **`azdo-empty-repositories.json`** - Mapping file with empty/null `azdoRepositories` section
   - Tests null/empty handling
   - Located in: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`

4. **`azuredevops-build-definition-repo-mapping.json`** - Terraform plan with build definitions
   - For end-to-end repository mapping tests
   - Includes `repository_id` and `default_branch` attributes
   - Located in: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`

5. **`azuredevops-git-repository-icons.json`** - Terraform plan with git repository resources
   - For end-to-end icon rendering tests
   - Includes various repository and branch attributes
   - Located in: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`

6. **`azuredevops-branch-policy-icons.json`** - Terraform plan with branch policy resources
   - Tests combined repository mapping and branch icons
   - Located in: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`

### Updated Test Data Files

1. **`examples/comprehensive-demo/demo-principals-nested.json`**
   - Add `azdoRepositories` section with 2-3 example mappings
   - Include realistic repository GUIDs and display names

### Test Snapshots

1. **`azuredevops-build-definition-repo-mapping.md`** - Expected output with repository mapping
2. **`azuredevops-git-repository-icons.md`** - Expected output with repository and branch icons
3. **`azuredevops-branch-policy-icons.md`** - Expected output for branch policies with icons

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty `azdoRepositories` section | Parser returns empty dictionary; no errors | TC-05 |
| Missing `azdoRepositories` section | Backwards compatible; existing behavior unchanged | TC-07 |
| Null `azdoRepositories` section | Treated as empty; no exceptions | TC-06 |
| Unmapped repository GUIDs | Return icon + raw GUID; track as failed resolution | TC-09, TC-12 |
| Mixed Azure AD and all azdo mappings | All types work independently | TC-04 |
| Repository attributes with non-GUID values | Icons still apply (semantic formatting doesn't validate GUID format) | TC-15, TC-16 |
| Branch attributes with various ref formats | Icon applies to all: `refs/heads/main`, `main`, `develop` | TC-18, TC-19 |
| Case variations in repository GUIDs | Case-insensitive matching (OrdinalIgnoreCase) | TC-08 |
| Attribute name case sensitivity | Case-insensitive matching for icon application | TC-15, TC-18 |
| Very long repository display names | Full name preserved in output | TC-08, TC-13 |
| Repository icon without mapping | Shows `🗃️ GUID` format | TC-09 |
| Branch icon (never mapped) | Always shows `⎇ value` format | TC-18, TC-19 |
| Non-branch attributes with "branch" in name | Icon NOT applied (exact name match required) | TC-20 |

## Non-Functional Tests

### Performance
- **Loading mapping files with repositories**: Verify `azdoRepositories` section doesn't significantly impact parse time
  - Test file with 50+ repository mappings
  - Expected: Parse time remains under 100ms

### Compatibility
- **Existing mapping files**: All existing test files continue to work without `azdoRepositories`
  - Run full test suite with existing mapping files
  - Expected: No regressions; all existing tests pass

### Diagnostics
- **Debug output completeness**: Repository mappings appear in diagnostic output
  - Verify count is tracked
  - Verify failed resolutions are tracked with AzdoRepository type

## Test Execution Strategy

### Phase 1: Unit Tests (Developer implements first)
1. TC-01, TC-02: Data model deserialization
2. TC-03, TC-04, TC-05, TC-06, TC-07: Parser tests
3. TC-08, TC-09, TC-10, TC-11, TC-12: Mapper tests
4. TC-13, TC-14: Value formatter tests
5. TC-15, TC-16, TC-17, TC-18, TC-19, TC-20: Semantic icon tests
6. TC-21, TC-22: Diagnostic tests
7. TC-23: Scriban helper tests

### Phase 2: Integration Tests (After unit tests pass)
1. TC-24, TC-25, TC-26: End-to-end rendering tests
2. TC-27: Example file update

### Test Execution Command
```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

### Snapshot Update Command (when output changes are intentional)
```bash
# After verifying changes are correct
scripts/update-snapshots.sh
```

## UAT Test Plan Reference

For user acceptance testing of this feature, see:
- `docs/features/096-azdo-repo-mapping-and-icons/uat-test-plan.md` - Defines UAT artifacts and validation steps
- UAT focuses on visual verification in real GitHub and Azure DevOps PR rendering
- Test plan includes feature-specific artifact (`uat-plan.md`) and comprehensive regression test

## Open Questions

1. **Value formatter registration pattern**: Should repository formatter use exact attribute name matches or pattern-based matching?
   - Recommendation: Use pattern-based matching (regex) consistent with Feature 085 for other azdo formatters

2. **Icon positioning in combined scenarios**: When both value formatter AND semantic formatting apply, which takes precedence?
   - Answer: Value formatter takes precedence (returns formatted value); semantic formatting is fallback when no formatter matches

3. **Template updates**: Which Azure DevOps resource templates need to explicitly use the new `azdo_repository_name` helper?
   - Review: `azuredevops_build_definition`, `azuredevops_git_repository`, branch policy templates
   - Recommendation: Most rendering should work via value formatters; helpers are for explicit template usage

4. **Snapshot test organization**: Should we create separate test categories for icon tests vs mapping tests?
   - Recommendation: Combine in same tests since icons and mapping often appear together in real resources

5. **Case sensitivity for repository lookups**: Confirm OrdinalIgnoreCase is correct for repository GUID matching
   - Decision: Yes, use OrdinalIgnoreCase (consistent with GUID semantics, minor improvement over Feature 085)

## Definition of Done

- [ ] All 27 test cases implemented and passing
- [ ] Test data files created (6 new files)
- [ ] Example mapping file updated with `azdoRepositories` section
- [ ] Snapshot baselines created (3 new snapshots)
- [ ] Edge cases covered
- [ ] No regressions in existing tests
- [ ] Test execution completes in under 30 seconds
- [ ] Code coverage maintained (no drop from current levels)
- [ ] UAT test plan references this test plan
- [ ] Repository mapper follows Feature 085 pattern
- [ ] Semantic icon tests verify non-breaking spaces
- [ ] Diagnostic tests verify repository count tracking
