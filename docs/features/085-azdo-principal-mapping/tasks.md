# Tasks: Azure DevOps Principal Mapping

## Overview

This document breaks down the implementation of Azure DevOps principal mapping into actionable tasks. The feature extends the existing principal mapping infrastructure to support Azure DevOps entities (users, groups, projects), enabling human-readable names in place of cryptic GUIDs and descriptors.

**Related Documents:**
- Specification: `docs/features/085-azdo-principal-mapping/specification.md`
- Architecture: `docs/features/085-azdo-principal-mapping/architecture.md`
- Test Plan: `docs/features/085-azdo-principal-mapping/test-plan.md`
- Test Cases: `docs/features/085-azdo-principal-mapping/test-cases.md`

## Implementation Approach

**Test-First Development:** Each task follows the pattern:
1. Write unit tests first
2. Implement code to pass tests
3. Verify integration

**Provider Separation:** Respect the boundary between `Platforms/Azure/` (provider-agnostic) and `Providers/AzureDevOps/` (provider-specific) as defined in ADR-007.

---

## Tasks

### Task 1: Extend Data Model for Azdo Sections

**Priority:** High

**Description:**
Add three new properties to `PrincipalMappingFile` class to support Azure DevOps entity mappings. This is foundational work required by all other components.

**Acceptance Criteria:**
- [ ] `PrincipalMappingFile.cs` includes `AzdoUsers` property with `JsonPropertyName("azdoUsers")` attribute
- [ ] `PrincipalMappingFile.cs` includes `AzdoGroups` property with `JsonPropertyName("azdoGroups")` attribute
- [ ] `PrincipalMappingFile.cs` includes `AzdoProjects` property with `JsonPropertyName("azdoProjects")` attribute
- [ ] All three properties are of type `Dictionary<string, string>?` (nullable)
- [ ] XML documentation comments explain what each property maps (user GUIDs, group descriptors, project GUIDs)
- [ ] Test TC-01 passes: `PrincipalMappingFile_DeserializeAzdoUsers_PopulatesProperty`
- [ ] Test TC-02 passes: `PrincipalMappingFile_DeserializeAllAzdoSections_PopulatesAllProperties`

**Dependencies:** None

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMappingFile.cs`
- Follow the existing pattern for `Users`, `Groups`, `ServicePrincipals` properties
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/PrincipalMappingFileTests.cs`

**Implementation Details:**
```csharp
/// <summary>
/// Gets or sets the mapping of Azure DevOps user IDs (GUIDs) to display names.
/// </summary>
[JsonPropertyName("azdoUsers")]
public Dictionary<string, string>? AzdoUsers { get; set; }

/// <summary>
/// Gets or sets the mapping of Azure DevOps group descriptors to display names.
/// Group descriptors are base64-encoded strings (e.g., "vssgp.Uy0xLTktMTU1MTM...").
/// </summary>
[JsonPropertyName("azdoGroups")]
public Dictionary<string, string>? AzdoGroups { get; set; }

/// <summary>
/// Gets or sets the mapping of Azure DevOps project IDs (GUIDs) to display names.
/// </summary>
[JsonPropertyName("azdoProjects")]
public Dictionary<string, string>? AzdoProjects { get; set; }
```

---

### Task 2: Extend AzureMappingFileResult Record

**Priority:** High

**Description:**
Add three new properties to `AzureMappingFileResult` to return parsed Azure DevOps entity mappings. This is the contract for returning parsed data.

**Acceptance Criteria:**
- [ ] `AzureMappingFileResult` includes `AzdoUsers` property of type `FrozenDictionary<string, string>`
- [ ] `AzureMappingFileResult` includes `AzdoGroups` property of type `FrozenDictionary<string, string>`
- [ ] `AzureMappingFileResult` includes `AzdoProjects` property of type `FrozenDictionary<string, string>`
- [ ] Properties are added to the positional record constructor
- [ ] All properties are non-nullable (empty dictionary when no mappings)

**Dependencies:** Task 1

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileResult.cs` (or within `AzureMappingFileParser.cs` if internal record)
- Use `FrozenDictionary` for performance (follows existing pattern)
- Parser tests will validate this change

**Implementation Details:**
```csharp
internal sealed record AzureMappingFileResult(
    FrozenDictionary<string, string> Principals,
    FrozenDictionary<string, string> PrincipalTypes,
    IReadOnlyList<MappingEntry> Subscriptions,
    IReadOnlyList<MappingEntry> ManagementGroups,
    IReadOnlyList<MappingEntry> Tenants,
    IReadOnlyList<MappingEntry> Roles,
    FrozenDictionary<string, string> AzdoUsers,      // NEW
    FrozenDictionary<string, string> AzdoGroups,     // NEW
    FrozenDictionary<string, string> AzdoProjects    // NEW
);
```

---

### Task 3: Extend DiagnosticContext for Azdo Entity Counts

**Priority:** High

**Description:**
Add diagnostic properties to track Azure DevOps entity counts and extend `FailedResolutionType` enum to include azdo entity types.

**Acceptance Criteria:**
- [ ] `DiagnosticContext` includes `AzdoUserCount` property (int)
- [ ] `DiagnosticContext` includes `AzdoGroupCount` property (int)
- [ ] `DiagnosticContext` includes `AzdoProjectCount` property (int)
- [ ] `FailedResolutionType` enum includes `AzdoUser` value
- [ ] `FailedResolutionType` enum includes `AzdoGroup` value
- [ ] `FailedResolutionType` enum includes `AzdoProject` value
- [ ] XML documentation comments added for all new properties

**Dependencies:** None

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`
- File location: `src/Oocx.TfPlan2Md/Diagnostics/FailedResolutionType.cs` (or wherever the enum is defined)
- Required by mappers for diagnostic tracking
- Required by test TC-17 and TC-18

---

### Task 4: Update AzureMappingFileParser to Parse Azdo Sections

**Priority:** High

**Description:**
Extend the parser to read azdo sections from the mapping JSON file and return them in the result object. Includes updating diagnostic counts.

**Acceptance Criteria:**
- [ ] Parser reads `azdoUsers` section and populates `AzdoUsers` dictionary
- [ ] Parser reads `azdoGroups` section and populates `AzdoGroups` dictionary
- [ ] Parser reads `azdoProjects` section and populates `AzdoProjects` dictionary
- [ ] Null or missing azdo sections result in empty dictionaries (not null)
- [ ] Azdo entity counts are set in `DiagnosticContext` (if provided)
- [ ] Azdo mappings are kept separate from Azure AD principal mappings (no cross-contamination)
- [ ] Test TC-03 passes: Parser reads azdoUsers correctly
- [ ] Test TC-04 passes: Parser preserves long group descriptors
- [ ] Test TC-05 passes: Parser reads azdoProjects correctly
- [ ] Test TC-06 passes: Mixed Azure AD and azdo sections parse independently
- [ ] Test TC-07 passes: Null azdo sections handled gracefully
- [ ] Test TC-08 passes: Missing azdo sections maintain backwards compatibility

**Dependencies:** Task 1, Task 2, Task 3

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileParser.cs` (or `AzureMappingFileLoader.cs`)
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`
- Follow existing pattern for parsing `users`, `groups`, `servicePrincipals`
- Update `HasNestedSections()` method if it exists
- Set diagnostic counts: `diagnostics.AzdoUserCount = result.AzdoUsers.Count` (if diagnostics provided)

**Implementation Pattern:**
```csharp
var azdoUsers = mappingFile.AzdoUsers ?? new Dictionary<string, string>();
var azdoGroups = mappingFile.AzdoGroups ?? new Dictionary<string, string>();
var azdoProjects = mappingFile.AzdoProjects ?? new Dictionary<string, string>();

if (diagnostics != null)
{
    diagnostics.AzdoUserCount = azdoUsers.Count;
    diagnostics.AzdoGroupCount = azdoGroups.Count;
    diagnostics.AzdoProjectCount = azdoProjects.Count;
}

return new AzureMappingFileResult(
    ..., // existing properties
    azdoUsers.ToFrozenDictionary(),
    azdoGroups.ToFrozenDictionary(),
    azdoProjects.ToFrozenDictionary()
);
```

---

### Task 5: Create AzdoUserMapper Class

**Priority:** High

**Description:**
Create a mapper class for Azure DevOps users that resolves user GUIDs to display names in the format `DisplayName [ID]`.

**Acceptance Criteria:**
- [ ] `AzdoUserMapper` class created in `Providers/AzureDevOps/` namespace
- [ ] Constructor accepts `FrozenDictionary<string, string>` and optional `DiagnosticContext?`
- [ ] `GetName(userId, resourceAddress = null)` method returns display name or null if not found
- [ ] `GetEntityName(userId)` method returns formatted string: `DisplayName [ID]` or just `ID` if not mapped
- [ ] Failed resolutions are tracked in DiagnosticContext with type `FailedResolutionType.AzdoUser`
- [ ] Test TC-09 passes: Known user IDs return formatted names
- [ ] Test TC-12 passes: Unknown user IDs return raw IDs
- [ ] Test TC-13 passes: Failed resolutions are recorded in diagnostics

**Dependencies:** Task 2, Task 3

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoUserMapper.cs`
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoUserMapperTests.cs`
- Follow the pattern established by `PrincipalMapper` in `Platforms/Azure/PrincipalMapper.cs`
- Use nullable reference types appropriately

**Implementation Pattern:**
```csharp
public sealed class AzdoUserMapper
{
    private readonly FrozenDictionary<string, string> _userMappings;
    private readonly DiagnosticContext? _diagnostics;

    public AzdoUserMapper(FrozenDictionary<string, string> userMappings, DiagnosticContext? diagnostics)
    {
        _userMappings = userMappings;
        _diagnostics = diagnostics;
    }

    public string? GetName(string userId, string? resourceAddress = null)
    {
        if (_userMappings.TryGetValue(userId, out var displayName))
        {
            return displayName;
        }

        if (resourceAddress != null)
        {
            _diagnostics?.RecordFailedResolution(FailedResolutionType.AzdoUser, userId, resourceAddress);
        }

        return null;
    }

    public string GetEntityName(string userId)
    {
        var displayName = GetName(userId);
        return displayName != null ? $"{displayName} [{userId}]" : userId;
    }
}
```

---

### Task 6: Create AzdoGroupMapper Class

**Priority:** High

**Description:**
Create a mapper class for Azure DevOps groups that resolves group descriptors to display names. Must preserve full descriptors without truncation.

**Acceptance Criteria:**
- [ ] `AzdoGroupMapper` class created in `Providers/AzureDevOps/` namespace
- [ ] Constructor accepts `FrozenDictionary<string, string>` and optional `DiagnosticContext?`
- [ ] `GetName(groupDescriptor, resourceAddress = null)` method returns display name or null if not found
- [ ] `GetEntityName(groupDescriptor)` method returns formatted string with full descriptor (no truncation)
- [ ] Long descriptors (100+ characters) are preserved completely
- [ ] Failed resolutions are tracked with type `FailedResolutionType.AzdoGroup`
- [ ] Test TC-10 passes: Long descriptors are preserved in output
- [ ] Test TC-12 passes: Unknown descriptors return raw descriptors
- [ ] Test TC-13 passes: Failed resolutions are recorded

**Dependencies:** Task 2, Task 3

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoGroupMapper.cs`
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoGroupMapperTests.cs`
- Similar structure to `AzdoUserMapper` but semantically distinct
- Group descriptors can be very long (e.g., `vssgp.Uy0xLTktMTU1MTM3NDI0NS0yNzY5MzQwNjk3...`)

---

### Task 7: Create AzdoProjectMapper Class

**Priority:** High

**Description:**
Create a mapper class for Azure DevOps projects that resolves project GUIDs to display names.

**Acceptance Criteria:**
- [ ] `AzdoProjectMapper` class created in `Providers/AzureDevOps/` namespace
- [ ] Constructor accepts `FrozenDictionary<string, string>` and optional `DiagnosticContext?`
- [ ] `GetName(projectId, resourceAddress = null)` method returns display name or null if not found
- [ ] `GetEntityName(projectId)` method returns formatted string: `DisplayName [ID]`
- [ ] Failed resolutions are tracked with type `FailedResolutionType.AzdoProject`
- [ ] Test TC-11 passes: Known project IDs return formatted names
- [ ] Test TC-12 passes: Unknown project IDs return raw IDs
- [ ] Test TC-13 passes: Failed resolutions are recorded

**Dependencies:** Task 2, Task 3

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoProjectMapper.cs`
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoProjectMapperTests.cs`
- Similar structure to `AzdoUserMapper` and `AzdoGroupMapper`

---

### Task 8: Create Azure DevOps Scriban Helpers

**Priority:** Medium

**Description:**
Create Scriban helper functions that templates can use to resolve Azure DevOps entity names during rendering.

**Acceptance Criteria:**
- [ ] `ScribanHelpers.AzureDevOps.cs` created in `Providers/AzureDevOps/Helpers/` namespace
- [ ] `azdo_user_name(userId, resourceAddress = null)` helper function resolves user IDs
- [ ] `azdo_group_name(groupDescriptor, resourceAddress = null)` helper function resolves group descriptors
- [ ] `azdo_project_name(projectId, resourceAddress = null)` helper function resolves project IDs
- [ ] Helpers return formatted string: `DisplayName [ID]` or just `ID` if not mapped
- [ ] Helpers accept mappers via closure (registered in module)
- [ ] Test TC-14 passes: `azdo_user_name` helper works correctly
- [ ] Test TC-15 passes: `azdo_group_name` helper preserves long descriptors
- [ ] Test TC-16 passes: `azdo_project_name` helper works correctly

**Dependencies:** Task 5, Task 6, Task 7

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Helpers/ScribanHelpers.AzureDevOps.cs`
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersAzdoTests.cs`
- Follow the pattern from `ScribanHelpers.Azure.cs` for Azure AD principals
- Helpers will be registered in `AzureDevOpsModule` via closures

**Implementation Pattern:**
```csharp
public static class ScribanHelpersAzureDevOps
{
    public static Func<string, string?> CreateAzdoUserNameHelper(AzdoUserMapper mapper)
    {
        return userId => mapper.GetEntityName(userId);
    }

    public static Func<string, string?> CreateAzdoGroupNameHelper(AzdoGroupMapper mapper)
    {
        return groupDescriptor => mapper.GetEntityName(groupDescriptor);
    }

    public static Func<string, string?> CreateAzdoProjectNameHelper(AzdoProjectMapper mapper)
    {
        return projectId => mapper.GetEntityName(projectId);
    }
}
```

---

### Task 9: Register Azdo Mappers and Helpers in AzureDevOpsModule

**Priority:** Medium

**Description:**
Update `AzureDevOpsModule` to instantiate the azdo mappers from the mapping file result and register the Scriban helpers in the template context.

**Acceptance Criteria:**
- [ ] `AzureDevOpsModule.cs` retrieves azdo dictionaries from `AzureMappingFileResult`
- [ ] Three mapper instances created: `AzdoUserMapper`, `AzdoGroupMapper`, `AzdoProjectMapper`
- [ ] Mappers receive diagnostic context if available
- [ ] Scriban helpers registered in template context with names: `azdo_user_name`, `azdo_group_name`, `azdo_project_name`
- [ ] Helpers receive mappers via closures
- [ ] Module gracefully handles cases where mapping file is not provided (mappers use empty dictionaries)

**Dependencies:** Task 4, Task 5, Task 6, Task 7, Task 8

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs`
- Follow the pattern established by Azure platform module registration
- Mappers should be instantiated only once per rendering session

**Implementation Pattern:**
```csharp
// In module registration
var azdoUsers = mappingFileResult?.AzdoUsers ?? FrozenDictionary<string, string>.Empty;
var azdoGroups = mappingFileResult?.AzdoGroups ?? FrozenDictionary<string, string>.Empty;
var azdoProjects = mappingFileResult?.AzdoProjects ?? FrozenDictionary<string, string>.Empty;

var userMapper = new AzdoUserMapper(azdoUsers, diagnostics);
var groupMapper = new AzdoGroupMapper(azdoGroups, diagnostics);
var projectMapper = new AzdoProjectMapper(azdoProjects, diagnostics);

// Register helpers
templateContext.PushGlobal(new ScriptObject
{
    ["azdo_user_name"] = ScribanHelpersAzureDevOps.CreateAzdoUserNameHelper(userMapper),
    ["azdo_group_name"] = ScribanHelpersAzureDevOps.CreateAzdoGroupNameHelper(groupMapper),
    ["azdo_project_name"] = ScribanHelpersAzureDevOps.CreateAzdoProjectNameHelper(projectMapper)
});
```

---

### Task 10: Update Diagnostic Output to Include Azdo Entity Counts

**Priority:** Medium

**Description:**
Extend the diagnostic output generation to include Azure DevOps entity counts when `--debug` flag is used.

**Acceptance Criteria:**
- [ ] Diagnostic output includes azdo user count
- [ ] Diagnostic output includes azdo group count
- [ ] Diagnostic output includes azdo project count
- [ ] Format matches existing diagnostic patterns (e.g., "Found N azdo users, M azdo groups, P azdo projects")
- [ ] Counts appear in the "Principal Mapping" section
- [ ] Zero counts are displayed if mapping file has no azdo sections
- [ ] Test TC-18 passes: Diagnostic output includes azdo counts

**Dependencies:** Task 3, Task 4

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticOutputGenerator.cs` (or wherever diagnostic output is generated)
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/Diagnostics/DiagnosticOutputTests.cs`
- Follow existing pattern for displaying principal counts

**Implementation Pattern:**
```csharp
if (diagnostics.AzdoUserCount > 0 || diagnostics.AzdoGroupCount > 0 || diagnostics.AzdoProjectCount > 0)
{
    output.AppendLine($"  - Found {diagnostics.AzdoUserCount} azdo users, {diagnostics.AzdoGroupCount} azdo groups, {diagnostics.AzdoProjectCount} azdo projects");
}
```

---

### Task 11: Update Azure DevOps Group Membership Template

**Priority:** Medium

**Description:**
Update the `azuredevops_group_membership` template to use the new azdo Scriban helpers for resolving user and group names.

**Acceptance Criteria:**
- [ ] Template uses `azdo_user_name` helper for member IDs
- [ ] Template uses `azdo_group_name` helper for group descriptors
- [ ] Template gracefully handles cases where helpers are not registered (backwards compatibility)
- [ ] Rendered output shows `DisplayName [ID]` format when mappings are available
- [ ] Rendered output shows raw IDs when mappings are not available
- [ ] Test TC-20 passes: End-to-end rendering with group membership

**Dependencies:** Task 8, Task 9

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops_group_membership.sbn` (if exists) or create it
- Check if custom template exists or if default template is used
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzureDevOpsSnapshotTests.cs`

**Template Pattern:**
```scriban
Member: {{ azdo_user_name member_id resource_address }}
Group: {{ azdo_group_name group_descriptor resource_address }}
```

---

### Task 12: Update Azure DevOps Project Template (if needed)

**Priority:** Low

**Description:**
Update the `azuredevops_project` template (if it exists) to use the `azdo_project_name` helper for resolving project names.

**Acceptance Criteria:**
- [ ] Template uses `azdo_project_name` helper for project IDs (if applicable)
- [ ] Rendered output shows `DisplayName [ID]` format when mappings are available
- [ ] Test TC-21 passes: End-to-end rendering with project resources

**Dependencies:** Task 8, Task 9

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops_project.sbn` (if exists)
- May not be needed if projects don't display IDs that need mapping
- Investigate which Azure DevOps resource templates display project IDs
- Test file: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzureDevOpsSnapshotTests.cs`

---

### Task 13: Update Example Mapping File

**Priority:** Low

**Description:**
Update the comprehensive demo mapping file to include examples of all three azdo sections with realistic data.

**Acceptance Criteria:**
- [ ] `demo-principals-nested.json` includes `azdoUsers` section with 2-3 example mappings
- [ ] `demo-principals-nested.json` includes `azdoGroups` section with 2-3 example mappings (including long descriptors)
- [ ] `demo-principals-nested.json` includes `azdoProjects` section with 1-2 example mappings
- [ ] Example file remains valid JSON
- [ ] Example uses realistic Azure DevOps IDs and descriptors
- [ ] Test TC-19 passes: Example file parses successfully

**Dependencies:** Task 4

**Notes:**
- File location: `examples/comprehensive-demo/demo-principals-nested.json`
- Add comments in the JSON (if supported) or in adjacent documentation explaining the azdo sections
- Use realistic-looking data (GUIDs for users/projects, base64 descriptors for groups)

**Example Data:**
```json
{
  "users": { ... },
  "groups": { ... },
  "azdoUsers": {
    "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
    "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
  },
  "azdoGroups": {
    "vssgp.Uy0xLTktMTU1MTM3NDI0NS0yNzY5MzQwNjk3LTExMDE5ODM1NjMtMzU0Nzk5MjM2MS0zNzAyMTIxNjI4LTEtMTIzNDU2Nzg5MC0xMjM0NTY3ODkwLTEyMzQ1Njc4OTAtMTIzNDU2Nzg5MA": "Platform Team",
    "vssgp.Short": "Security Reviewers"
  },
  "azdoProjects": {
    "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project",
    "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": "Application Platform"
  },
  "subscriptions": [ ... ],
  "tenants": [ ... ]
}
```

---

### Task 14: Create Snapshot Test Data and Baselines

**Priority:** Low

**Description:**
Create test data files and snapshot baselines for integration testing of Azure DevOps resource rendering with azdo mapping.

**Acceptance Criteria:**
- [ ] Mapping file created for azuredevops-group-members test with azdo user and group mappings
- [ ] Expected snapshot created: `azuredevops-group-members-with-mapping.md`
- [ ] Snapshot shows `DisplayName [ID]` format for mapped entities
- [ ] Test plan JSON created for azuredevops-project test (if needed)
- [ ] Expected snapshot created: `azuredevops-projects-with-mapping.md` (if needed)
- [ ] All snapshot tests pass

**Dependencies:** Task 11, Task 12

**Notes:**
- File locations:
  - Test data: `TestData/` directory
  - Snapshots: `tests/TestData/Snapshots/` directory
- Use `scripts/update-snapshots.sh` to generate initial snapshots after verifying output manually
- Snapshots demonstrate the before/after improvement in readability

---

### Task 15: Update Documentation

**Priority:** Low

**Description:**
Update README and other documentation to explain the new Azure DevOps mapping feature.

**Acceptance Criteria:**
- [ ] README.md includes section on Azure DevOps principal mapping
- [ ] Documentation explains the three azdo sections: `azdoUsers`, `azdoGroups`, `azdoProjects`
- [ ] Example JSON snippet shows how to use azdo sections
- [ ] Documentation references the comprehensive demo mapping file
- [ ] Documentation explains the output format: `DisplayName [ID]`
- [ ] Scriban helpers documented (if there's template documentation)

**Dependencies:** Task 13

**Notes:**
- File locations:
  - `README.md`
  - Any provider-specific documentation files
- Keep documentation concise and practical
- Include at least one complete example

---

## Implementation Order

The recommended sequence for implementation (respects dependencies and risk):

1. **Phase 1: Foundation (Tasks 1-3)** - Data model and diagnostics
   - Task 1: Extend PrincipalMappingFile
   - Task 2: Extend AzureMappingFileResult
   - Task 3: Extend DiagnosticContext
   - **Milestone:** Data structures ready for parsing and mapping

2. **Phase 2: Parsing (Task 4)** - Read mapping file
   - Task 4: Update AzureMappingFileParser
   - **Milestone:** Mapping file can be loaded and parsed

3. **Phase 3: Mappers (Tasks 5-7)** - Core mapping logic
   - Task 5: Create AzdoUserMapper
   - Task 6: Create AzdoGroupMapper
   - Task 7: Create AzdoProjectMapper
   - **Milestone:** Entity resolution logic complete

4. **Phase 4: Integration (Tasks 8-10)** - Wire everything together
   - Task 8: Create Scriban helpers
   - Task 9: Register in AzureDevOpsModule
   - Task 10: Update diagnostic output
   - **Milestone:** Feature fully integrated and functional

5. **Phase 5: Templates (Tasks 11-12)** - Apply to resources
   - Task 11: Update azuredevops_group_membership template
   - Task 12: Update azuredevops_project template (if needed)
   - **Milestone:** Templates use new mapping functionality

6. **Phase 6: Polish (Tasks 13-15)** - Examples, tests, documentation
   - Task 13: Update example mapping file
   - Task 14: Create snapshot test data
   - Task 15: Update documentation
   - **Milestone:** Feature complete with examples and documentation

## Open Questions

1. **Template Investigation:** Which Azure DevOps resource templates currently exist and display entity IDs that would benefit from mapping?
   - Need to review `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/` directory
   - May need to create templates if they don't exist

2. **Diagnostic Output Format:** Should azdo entity counts be displayed on separate lines or combined?
   - Option A: "Found 2 azdo users, 3 azdo groups, 1 azdo project"
   - Option B: Three separate lines
   - Recommendation: Follow existing diagnostic output pattern

3. **Helper Registration Location:** Should helpers be registered in a separate `ScribanHelperRegistry` class or directly in `AzureDevOpsModule`?
   - Recommendation: Register directly in `AzureDevOpsModule` to keep it simple and follow existing patterns

4. **Test File Organization:** Should azdo mapper tests be in a new folder `Providers/AzureDevOps/` or in the existing `Platforms/Azure/` test folder?
   - Recommendation: Create new folder `Providers/AzureDevOps/` to mirror source structure

## Test Execution

Run all tests after each phase:

```bash
# Run all tests
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx

# Run specific test class
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/AzdoUserMapperTests/*

# Update snapshots (after verifying output manually)
scripts/update-snapshots.sh
```

## Definition of Done

The feature is complete when:

- [ ] All 15 tasks are completed with acceptance criteria met
- [ ] All 21 test cases (TC-01 through TC-21) pass
- [ ] No regressions in existing tests
- [ ] Code review completed (via `code_review` tool)
- [ ] Security scan completed (via `codeql_checker` tool)
- [ ] Documentation updated
- [ ] Example mapping file demonstrates feature
- [ ] Snapshot tests capture expected behavior
- [ ] Diagnostic output includes azdo entity information
- [ ] Feature works end-to-end: mapping file → parser → mappers → templates → rendered output

## Notes for Developer

- **Test-First:** Write unit tests before implementation for each task
- **Incremental:** Complete tasks in order; each builds on previous work
- **Pattern Matching:** Follow existing patterns for Azure AD principal mapping
- **Provider Separation:** Keep Azure DevOps logic in `Providers/AzureDevOps/`
- **Error Handling:** Gracefully handle null/empty azdo sections (all optional)
- **Backwards Compatibility:** Existing mapping files without azdo sections must continue to work
