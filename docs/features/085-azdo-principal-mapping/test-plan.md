# Test Plan: Azure DevOps Principal Mapping

## Overview

This test plan verifies the Azure DevOps principal mapping feature that extends the existing principal mapping infrastructure to support Azure DevOps entities (users, groups, projects). The feature enables users to map Azure DevOps identifiers to human-readable display names in rendered Terraform plan reports.

**Related Documents:**
- Specification: `docs/features/085-azdo-principal-mapping/specification.md`
- Architecture: `docs/features/085-azdo-principal-mapping/architecture.md`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `PrincipalMappingFile` includes azdo properties | TC-01, TC-02 | Unit |
| `AzureMappingFileParser` parses azdo sections | TC-03, TC-04, TC-05, TC-06 | Unit |
| Azdo entity names resolved in rendered output | TC-14, TC-15, TC-16 | Integration |
| Display format: `DisplayName [ID]` | TC-09, TC-10, TC-11 | Unit |
| Empty/null azdo sections handled gracefully | TC-07, TC-08 | Unit |
| Diagnostic output includes azdo counts | TC-17, TC-18 | Unit |
| Example mapping file demonstrates azdo sections | TC-19 | Integration |
| Backwards compatibility maintained | TC-08, TC-20 | Integration |
| Tests verify parsing and rendering | All tests | All types |

## Test Cases

### Data Model Tests

#### TC-01: PrincipalMappingFile Deserializes AzdoUsers Section

**Type:** Unit

**Description:**
Verifies that the `PrincipalMappingFile` class correctly deserializes the `azdoUsers` JSON section into the `AzdoUsers` property.

**Preconditions:**
- Test mapping file with `azdoUsers` section

**Test Steps:**
1. Create JSON with `azdoUsers` section containing multiple user mappings
2. Deserialize JSON to `PrincipalMappingFile` object
3. Verify `AzdoUsers` property is not null
4. Verify each user mapping is present in the dictionary

**Expected Result:**
- `AzdoUsers` property contains all user mappings from JSON
- User IDs (GUIDs) map to correct display names

**Test Data:**
```json
{
  "azdoUsers": {
    "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
    "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
  }
}
```

---

#### TC-02: PrincipalMappingFile Deserializes All Azdo Sections

**Type:** Unit

**Description:**
Verifies that the `PrincipalMappingFile` class correctly deserializes all three azdo sections (`azdoUsers`, `azdoGroups`, `azdoProjects`) simultaneously.

**Preconditions:**
- Test mapping file with all three azdo sections

**Test Steps:**
1. Create JSON with all three azdo sections containing mappings
2. Deserialize JSON to `PrincipalMappingFile` object
3. Verify all three properties (`AzdoUsers`, `AzdoGroups`, `AzdoProjects`) are not null
4. Verify mappings in each section are correct

**Expected Result:**
- All three azdo properties contain correct mappings
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
  }
}
```

---

### Parser Tests

#### TC-03: AzureMappingFileParser Parses AzdoUsers Section

**Type:** Unit

**Description:**
Verifies that the parser correctly extracts azdo user mappings and returns them in `AzureMappingFileResult`.

**Preconditions:**
- Test mapping file with `azdoUsers` section

**Test Steps:**
1. Create test JSON file with `azdoUsers` section
2. Parse file using `AzureMappingFileParser`
3. Verify `AzureMappingFileResult.AzdoUsers` contains expected mappings
4. Verify the dictionary is a `FrozenDictionary<string, string>`

**Expected Result:**
- Parser returns azdo user mappings in result object
- Mappings are correct and complete

**Test Data:**
Inline test file with 2-3 azdo user mappings

---

#### TC-04: AzureMappingFileParser Parses AzdoGroups Section

**Type:** Unit

**Description:**
Verifies that the parser correctly extracts azdo group mappings including long descriptors.

**Preconditions:**
- Test mapping file with `azdoGroups` section

**Test Steps:**
1. Create test JSON file with `azdoGroups` section including long descriptors
2. Parse file using `AzureMappingFileParser`
3. Verify `AzureMappingFileResult.AzdoGroups` contains expected mappings
4. Verify long descriptors are preserved completely (not truncated)

**Expected Result:**
- Parser returns azdo group mappings with full descriptors
- No truncation or corruption of descriptor strings

**Test Data:**
Inline test file with realistic group descriptors (e.g., `vssgp.Uy0xLTktMTU1MTM...`)

---

#### TC-05: AzureMappingFileParser Parses AzdoProjects Section

**Type:** Unit

**Description:**
Verifies that the parser correctly extracts azdo project mappings.

**Preconditions:**
- Test mapping file with `azdoProjects` section

**Test Steps:**
1. Create test JSON file with `azdoProjects` section
2. Parse file using `AzureMappingFileParser`
3. Verify `AzureMappingFileResult.AzdoProjects` contains expected mappings

**Expected Result:**
- Parser returns azdo project mappings in result object
- Project IDs (GUIDs) map to correct display names

**Test Data:**
Inline test file with 2-3 project mappings

---

#### TC-06: AzureMappingFileParser Handles Mixed Azure and Azdo Sections

**Type:** Unit

**Description:**
Verifies that the parser correctly handles mapping files containing both traditional Azure AD sections and new azdo sections.

**Preconditions:**
- Test mapping file with both Azure AD and azdo sections

**Test Steps:**
1. Create test JSON with users, groups, servicePrincipals, AND all three azdo sections
2. Parse file using `AzureMappingFileParser`
3. Verify Azure AD mappings are in `Principals` dictionary
4. Verify azdo mappings are in separate `AzdoUsers`, `AzdoGroups`, `AzdoProjects` dictionaries
5. Verify no cross-contamination between Azure AD and azdo mappings

**Expected Result:**
- Azure AD principals remain in their existing dictionary
- Azdo entities are in separate dictionaries
- Both types of mappings work correctly

**Test Data:**
Comprehensive test file similar to `examples/comprehensive-demo/demo-principals-nested.json` but with azdo sections added

---

#### TC-07: AzureMappingFileParser Handles Null Azdo Sections

**Type:** Unit

**Description:**
Verifies that the parser gracefully handles mapping files where azdo sections are explicitly null.

**Preconditions:**
- Test mapping file with azdo sections set to null

**Test Steps:**
1. Create test JSON with `"azdoUsers": null`, `"azdoGroups": null`, `"azdoProjects": null`
2. Parse file using `AzureMappingFileParser`
3. Verify parsing succeeds without errors
4. Verify azdo dictionaries in result are empty (not null)

**Expected Result:**
- Parser does not throw exceptions
- Result contains empty dictionaries for azdo entities

**Test Data:**
```json
{
  "users": { "user-1": "Test User" },
  "azdoUsers": null,
  "azdoGroups": null,
  "azdoProjects": null
}
```

---

#### TC-08: AzureMappingFileParser Handles Missing Azdo Sections (Backwards Compatibility)

**Type:** Unit

**Description:**
Verifies that existing mapping files without any azdo sections continue to work (backwards compatibility).

**Preconditions:**
- Test mapping file without azdo sections (existing format)

**Test Steps:**
1. Use existing test file with only Azure AD sections (no azdo sections)
2. Parse file using updated `AzureMappingFileParser`
3. Verify parsing succeeds
4. Verify Azure AD mappings work correctly
5. Verify azdo dictionaries in result are empty

**Expected Result:**
- Existing mapping files work without modification
- No breaking changes to existing functionality

**Test Data:**
Existing test file: `examples/comprehensive-demo/demo-principals-nested.json`

---

### Mapper Tests

#### TC-09: AzdoUserMapper Resolves Known User IDs

**Type:** Unit

**Description:**
Verifies that `AzdoUserMapper` correctly resolves user IDs to display names in the format `DisplayName [ID]`.

**Preconditions:**
- Mapper initialized with user mappings

**Test Steps:**
1. Create `AzdoUserMapper` with test user mappings
2. Call `GetEntityName("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b")`
3. Verify result is in format `DisplayName [ID]`

**Expected Result:**
- Method returns `"John Smith [4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b]"`

**Test Data:**
Dictionary with one user mapping: `"4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b" => "John Smith"`

---

#### TC-10: AzdoGroupMapper Resolves Known Group Descriptors with Full Descriptor

**Type:** Unit

**Description:**
Verifies that `AzdoGroupMapper` correctly resolves group descriptors including long descriptors without truncation.

**Preconditions:**
- Mapper initialized with group mappings

**Test Steps:**
1. Create `AzdoGroupMapper` with test group mappings including a long descriptor
2. Call `GetEntityName("vssgp.Uy0xLTktMTU1MTM...")`
3. Verify result includes full descriptor (not truncated)

**Expected Result:**
- Method returns `"Platform Team [vssgp.Uy0xLTktMTU1MTM...]"` with complete descriptor

**Test Data:**
Dictionary with long group descriptor mapping

---

#### TC-11: AzdoProjectMapper Resolves Known Project IDs

**Type:** Unit

**Description:**
Verifies that `AzdoProjectMapper` correctly resolves project IDs to display names.

**Preconditions:**
- Mapper initialized with project mappings

**Test Steps:**
1. Create `AzdoProjectMapper` with test project mappings
2. Call `GetEntityName("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f")`
3. Verify result is in format `DisplayName [ID]`

**Expected Result:**
- Method returns `"Infrastructure Project [8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f]"`

**Test Data:**
Dictionary with one project mapping

---

#### TC-12: Azdo Mappers Return Raw ID for Unknown Entities

**Type:** Unit

**Description:**
Verifies that all three azdo mappers return the raw ID when a mapping is not found.

**Preconditions:**
- Mappers initialized with limited mappings

**Test Steps:**
1. Create each mapper with test mappings
2. Call `GetEntityName()` with unmapped ID for each mapper
3. Verify each returns just the raw ID (no null, no exception)

**Expected Result:**
- `AzdoUserMapper.GetEntityName("unknown")` returns `"unknown"`
- `AzdoGroupMapper.GetEntityName("unknown")` returns `"unknown"`
- `AzdoProjectMapper.GetEntityName("unknown")` returns `"unknown"`

**Test Data:**
Mappers with limited dictionaries

---

#### TC-13: Azdo Mappers Track Failed Resolutions in Diagnostics

**Type:** Unit

**Description:**
Verifies that azdo mappers record failed resolutions in the diagnostic context when a resource address is provided.

**Preconditions:**
- Mappers initialized with diagnostic context
- Mappers have limited mappings

**Test Steps:**
1. Create mappers with diagnostic context
2. Call `GetName("unknown-id", "azuredevops_group_membership.example")` on each mapper
3. Verify diagnostic context contains failed resolution entries
4. Verify failed resolution has correct type and ID

**Expected Result:**
- DiagnosticContext.FailedResolutions contains entries
- Each entry has appropriate FailedResolutionType (e.g., `AzdoUser`, `AzdoGroup`, `AzdoProject`)
- Entry includes the unmapped ID and resource address

**Test Data:**
Mappers with empty dictionaries and diagnostic context

---

### Scriban Helper Tests

#### TC-14: azdo_user_name Helper Resolves Known User IDs

**Type:** Unit

**Description:**
Verifies that the `azdo_user_name` Scriban helper correctly resolves user IDs.

**Preconditions:**
- Helper registered with `AzdoUserMapper`

**Test Steps:**
1. Set up Scriban context with `azdo_user_name` helper
2. Render template: `{{ azdo_user_name "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b" }}`
3. Verify output matches expected format

**Expected Result:**
- Rendered output: `"John Smith [4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b]"`

**Test Data:**
Mapper with test user mappings

---

#### TC-15: azdo_group_name Helper Resolves Known Group Descriptors

**Type:** Unit

**Description:**
Verifies that the `azdo_group_name` Scriban helper correctly resolves group descriptors.

**Preconditions:**
- Helper registered with `AzdoGroupMapper`

**Test Steps:**
1. Set up Scriban context with `azdo_group_name` helper
2. Render template: `{{ azdo_group_name "vssgp.Uy0xLTktMTU1MTM..." }}`
3. Verify output matches expected format with full descriptor

**Expected Result:**
- Rendered output: `"Platform Team [vssgp.Uy0xLTktMTU1MTM...]"`

**Test Data:**
Mapper with test group mappings

---

#### TC-16: azdo_project_name Helper Resolves Known Project IDs

**Type:** Unit

**Description:**
Verifies that the `azdo_project_name` Scriban helper correctly resolves project IDs.

**Preconditions:**
- Helper registered with `AzdoProjectMapper`

**Test Steps:**
1. Set up Scriban context with `azdo_project_name` helper
2. Render template: `{{ azdo_project_name "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f" }}`
3. Verify output matches expected format

**Expected Result:**
- Rendered output: `"Infrastructure Project [8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f]"`

**Test Data:**
Mapper with test project mappings

---

### Diagnostic Tests

#### TC-17: DiagnosticContext Tracks Azdo Entity Counts

**Type:** Unit

**Description:**
Verifies that the `DiagnosticContext` correctly tracks counts for all three azdo entity types.

**Preconditions:**
- Diagnostic context initialized
- Mapping file loaded with azdo sections

**Test Steps:**
1. Create diagnostic context
2. Load mapping file with azdo sections
3. Verify `AzdoUserCount` equals number of user mappings
4. Verify `AzdoGroupCount` equals number of group mappings
5. Verify `AzdoProjectCount` equals number of project mappings

**Expected Result:**
- All three count properties reflect correct numbers
- Counts are independent (not summed together)

**Test Data:**
Mapping file with known counts: 2 users, 3 groups, 1 project

---

#### TC-18: Diagnostic Output Includes Azdo Entity Counts

**Type:** Integration

**Description:**
Verifies that the diagnostic output (when `--debug` is used) includes azdo entity counts in the "Principal Mapping" section.

**Preconditions:**
- Diagnostic context with azdo entity counts
- Debug output generation enabled

**Test Steps:**
1. Load mapping file with azdo sections
2. Generate diagnostic output
3. Verify output includes azdo entity counts
4. Verify format matches: "Found N azdo users, M azdo groups, P azdo projects"

**Expected Result:**
- Diagnostic output shows azdo counts
- Format is consistent with existing diagnostic patterns

**Test Data:**
Mapping file with 2 users, 2 groups, 1 project

---

### Integration Tests

#### TC-19: Example Mapping File Demonstrates Azdo Sections

**Type:** Integration

**Description:**
Verifies that the example mapping file includes azdo sections with realistic data.

**Preconditions:**
- Example mapping file at `examples/comprehensive-demo/demo-principals-nested.json`

**Test Steps:**
1. Load the example mapping file
2. Verify it contains all three azdo sections
3. Verify azdo sections have realistic IDs and display names
4. Verify file remains valid JSON

**Expected Result:**
- Example file parses successfully
- Azdo sections demonstrate proper usage
- Documentation references the example file

**Test Data:**
Updated `examples/comprehensive-demo/demo-principals-nested.json`

---

#### TC-20: End-to-End Rendering with Azdo Group Membership

**Type:** Integration

**Description:**
Verifies that azdo entity names appear correctly in rendered output for `azuredevops_group_membership` resources.

**Preconditions:**
- Test plan JSON with `azuredevops_group_membership` resources
- Mapping file with corresponding azdo user and group mappings

**Test Steps:**
1. Create test Terraform plan with `azuredevops_group_membership` resources
2. Create mapping file with user and group mappings
3. Render markdown with mapping file
4. Verify group names appear as `DisplayName [descriptor]`
5. Verify member names appear as `DisplayName [ID]`
6. Compare against snapshot baseline

**Expected Result:**
- Rendered output shows display names instead of raw IDs
- Format matches specification: `DisplayName [ID]`
- Output is more readable than without mapping

**Test Data:**
- Test plan: `azuredevops-group-members-plan.json` (existing)
- New mapping file with azdo sections
- Expected snapshot: updated version of `azuredevops-group-members.md`

---

#### TC-21: End-to-End Rendering with Azdo Project Resources

**Type:** Integration

**Description:**
Verifies that azdo project names appear correctly in rendered output for `azuredevops_project` resources.

**Preconditions:**
- Test plan JSON with `azuredevops_project` resources
- Mapping file with azdo project mappings

**Test Steps:**
1. Create test Terraform plan with `azuredevops_project` resources
2. Create mapping file with project mappings
3. Render markdown with mapping file
4. Verify project names appear as `DisplayName [ID]`

**Expected Result:**
- Rendered output shows project display names
- Format is consistent with other entity types

**Test Data:**
- New test plan with `azuredevops_project` resources
- Mapping file with project mappings

---

## Test Data Requirements

### New Test Data Files

1. **`azdo-principals-mapping.json`** - Comprehensive mapping file with all three azdo sections
   - 3 azdo users
   - 3 azdo groups (including long descriptors)
   - 2 azdo projects
   - Also includes traditional Azure AD sections for mixed testing

2. **`azdo-users-only.json`** - Mapping file with only azdoUsers section
   - Used for isolated user mapping tests

3. **`azdo-empty-sections.json`** - Mapping file with empty azdo sections
   - Tests null/empty handling

4. **`azuredevops-projects-plan.json`** - Terraform plan with azuredevops_project resources
   - For end-to-end project mapping tests

### Updated Test Data Files

1. **`examples/comprehensive-demo/demo-principals-nested.json`**
   - Add all three azdo sections with example mappings

### Test Snapshots

1. **`azuredevops-group-members-with-mapping.md`** - Expected output with azdo mapping applied
2. **`azuredevops-projects-with-mapping.md`** - Expected output for project resources with mapping

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty azdo sections | Parser returns empty dictionaries; no errors | TC-07 |
| Missing azdo sections | Backwards compatible; existing behavior unchanged | TC-08 |
| Null azdo sections | Treated as empty; no exceptions | TC-07 |
| Unmapped azdo entities | Return raw ID; track as failed resolution | TC-12, TC-13 |
| Very long group descriptors | Full descriptor preserved in output | TC-04, TC-10, TC-15 |
| Mixed Azure AD and azdo mappings | Both types work independently | TC-06 |
| Azdo sections without Azure AD sections | Valid; azdo mappings work standalone | TC-03, TC-04, TC-05 |
| Duplicate IDs across entity types | Each mapper is independent; no conflicts | TC-06 |
| Invalid JSON in azdo sections | Standard JSON parse error handling | Existing error tests |
| Case sensitivity in azdo IDs | Case-sensitive matching (GUIDs and descriptors) | TC-09, TC-10, TC-11 |

## Non-Functional Tests

### Performance
- **Loading large mapping files**: Verify azdo sections don't significantly impact parse time
  - Test file with 100+ mappings in each azdo section
  - Expected: Parse time remains under 100ms

### Compatibility
- **Existing mapping files**: All existing test files continue to work
  - Run full test suite with existing mapping files
  - Expected: No regressions; all existing tests pass

### Diagnostics
- **Debug output completeness**: All azdo entity types appear in diagnostic output
  - Verify counts for all three types
  - Verify failed resolutions are tracked by type

## Test Execution Strategy

### Phase 1: Unit Tests (Developer implements first)
1. TC-01, TC-02: Data model deserialization
2. TC-03, TC-04, TC-05, TC-06, TC-07, TC-08: Parser tests
3. TC-09, TC-10, TC-11, TC-12, TC-13: Mapper tests
4. TC-14, TC-15, TC-16: Scriban helper tests
5. TC-17, TC-18: Diagnostic tests

### Phase 2: Integration Tests (After unit tests pass)
1. TC-19: Example file update
2. TC-20, TC-21: End-to-end rendering tests

### Test Execution Command
```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

### Snapshot Update Command (when output changes are intentional)
```bash
# After verifying changes are correct
scripts/update-snapshots.sh
```

## Open Questions

1. **FailedResolutionType enum**: Should we add specific enum values for azdo entity types (e.g., `AzdoUser`, `AzdoGroup`, `AzdoProject`) or reuse existing values?
   - Recommendation: Add specific enum values for better diagnostic granularity

2. **Snapshot test updates**: Should we create entirely new snapshot tests or update existing `azuredevops-group-members.md` snapshot to include mapping?
   - Recommendation: Create separate test with mapping to show before/after comparison

3. **Template updates**: Which Azure DevOps resource templates need to use the new helpers?
   - From specification: `azuredevops_group_membership`, `azuredevops_project`
   - Need to review: Are there other resources that display user/group/project IDs?

4. **Test coverage for template rendering**: Should we add template-specific unit tests or rely on snapshot tests?
   - Recommendation: Rely on snapshot tests for template rendering; unit tests for helpers only

## Definition of Done

- [ ] All 21 test cases implemented and passing
- [ ] Test data files created
- [ ] Example mapping file updated with azdo sections
- [ ] Snapshot baselines updated/created
- [ ] Edge cases covered
- [ ] No regressions in existing tests
- [ ] Test execution completes in under 30 seconds
- [ ] Code coverage maintained (no drop from current levels)
