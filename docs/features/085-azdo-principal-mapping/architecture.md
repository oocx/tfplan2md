# Architecture: Azure DevOps Principal Mapping

## Status

Proposed

## Overview

This feature extends the existing Azure principal mapping infrastructure to support Azure DevOps entities (users, groups, projects). The design leverages the established patterns for Azure AD principal mapping while respecting provider separation boundaries.

## Architectural Context

### Existing Infrastructure

The codebase currently supports principal mapping for Azure AD entities through:

- **`PrincipalMappingFile`** (`src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMappingFile.cs`): Data model for the mapping JSON file with sections for users, groups, service principals, subscriptions, management groups, tenants, and roles
- **`AzureMappingFileParser`** (`src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileParser.cs`): Parses JSON and builds `AzureMappingFileResult` with principal name/type dictionaries
- **`PrincipalMapper`** (`src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMapper.cs`): Resolves principal IDs to display names, tracks failed resolutions
- **`ScribanHelpers.Azure`** (`src/Oocx.TfPlan2Md/Platforms/Azure/ScribanHelpers.Azure.cs`): Provides Scriban helpers like `azure_principal_name`, `azure_principal_info`
- **`DiagnosticContext`** (`src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`): Tracks mapping load status, counts, and failed resolutions

### Provider Separation Pattern

Per ADR-007 (Architecture Boundary Enforcement) and the docs/architecture.md structure:

- **Provider-specific code MUST be isolated in `Providers/<ProviderName>/`**
- **Platform utilities (provider-agnostic) live in `Platforms/`**
- Azure AD principal mapping is in `Platforms/Azure/` because it's used across multiple Azure providers (AzureRM, AzApi, AzureAD)
- Azure DevOps entities are provider-specific and belong in `Providers/AzureDevOps/`

## Design Decisions

### Decision 1: Naming Convention for JSON Properties

**Options Considered:**
1. `azdoUsers`, `azdoGroups`, `azdoProjects` (abbreviated)
2. `azureDevOpsUsers`, `azureDevOpsGroups`, `azureDevOpsProjects` (full names)

**Decision:** Use `azdoUsers`, `azdoGroups`, `azdoProjects`.

**Rationale:**
- Consistent with common abbreviations in the Azure DevOps ecosystem
- Keeps JSON files more concise and readable
- Follows the pattern of existing properties (e.g., `servicePrincipals` is abbreviated to `servicePrincipals` not `azureActiveDirectoryServicePrincipals`)
- Users already understand "azdo" as the standard abbreviation for Azure DevOps

**Impact:**
- `PrincipalMappingFile` will have three new properties: `AzdoUsers`, `AzdoGroups`, `AzdoProjects` (C# naming conventions)
- JSON properties will be `azdoUsers`, `azdoGroups`, `azdoProjects` (camelCase via `JsonPropertyName` attributes)

### Decision 2: Helper Placement

**Options Considered:**
1. Add to existing `ScribanHelpers.Azure` in `Platforms/Azure/`
2. Create new `ScribanHelpers.AzureDevOps` in `Providers/AzureDevOps/`
3. Create a shared entity resolution service

**Decision:** Create provider-specific Scriban helpers in `Providers/AzureDevOps/`.

**Rationale:**
- **Respects provider separation**: Azure DevOps entity resolution is provider-specific logic
- **Follows existing pattern**: `Providers/AzureRM/`, `Providers/AzApi/`, and `Providers/AzureDevOps/` all have provider-specific helpers
- **Clear ownership**: Makes it obvious where Azure DevOps-specific logic lives
- **Avoids coupling**: `Platforms/Azure/` should remain provider-agnostic; mixing Azure AD and Azure DevOps logic would violate separation of concerns

**Implementation:**
- Create `Providers/AzureDevOps/Helpers/ScribanHelpers.AzureDevOps.cs` with helpers like:
  - `azdo_user_name(userId)` → resolves Azure DevOps user ID
  - `azdo_group_name(groupDescriptor)` → resolves Azure DevOps group descriptor
  - `azdo_project_name(projectId)` → resolves Azure DevOps project ID
- Register these helpers in `AzureDevOpsModule.cs` during provider initialization

### Decision 3: Mapper Reuse vs. New Mapper

**Options Considered:**
1. Reuse existing `PrincipalMapper` for Azure DevOps entities
2. Create separate `AzdoEntityMapper` class

**Decision:** Create separate mapper classes for Azure DevOps entities.

**Rationale:**
- **Semantic clarity**: `PrincipalMapper` is conceptually for Azure AD/Entra principals (users, groups, service principals). Azure DevOps entities are different identity types.
- **Separation of concerns**: Azure DevOps entities have different characteristics:
  - Group descriptors are very long base64-encoded strings (not GUIDs)
  - Projects are a different entity type (not principals)
  - Different diagnostic tracking needs (separate counts for azdo entities)
- **Independent evolution**: Azure DevOps mapping may need different features in the future (e.g., type inference from descriptor format)
- **Avoids coupling**: Keeps Azure AD and Azure DevOps mapping logic independent

**Implementation:**
- Create three focused mapper classes in `Providers/AzureDevOps/`:
  - `AzdoUserMapper` → maps user GUIDs to display names
  - `AzdoGroupMapper` → maps group descriptors to display names
  - `AzdoProjectMapper` → maps project GUIDs to display names
- All three follow the same pattern as `PrincipalMapper` but are specific to their entity type
- Helper methods will receive the appropriate mapper via dependency injection

### Decision 4: Type Tracking for Azure DevOps Entities

**Decision:** Do not implement type tracking in the initial version.

**Rationale:**
- **Marked as out of scope** in the specification
- **YAGNI principle**: No current use case for distinguishing entity types in the same way Azure AD principals need type metadata
- **Can be added later**: If future features need type metadata (e.g., different formatting for users vs. groups), it can be added without breaking changes
- **Keeps implementation simple**: Focus on core functionality first

**Future Consideration:**
If type tracking is needed later, it can be added by:
- Extending the mappers with a `GetEntityType()` method
- Adding type dictionaries similar to `PrincipalMapper._principalTypes`
- Updating diagnostic output to show type counts

### Decision 5: Group Descriptor Display Format

**Options Considered:**
1. Display full descriptor: `Platform Team [vssgp.Uy0xLTktMTU1MTM...]`
2. Truncate descriptor: `Platform Team [vssgp.Uy0x...MTM]`

**Decision:** Display full descriptor without truncation.

**Rationale:**
- **Consistency**: Matches existing behavior for all other ID types (GUIDs, role definition IDs, etc.)
- **Avoids ambiguity**: Truncation could make different descriptors appear identical
- **Copy-paste friendly**: Users may need to copy the full descriptor for other tools or scripts
- **User control**: Users already control verbosity through template customization

**Impact:**
- Group descriptors will appear in full in rendered output
- This may make some lines longer, but maintains consistency and utility
- Users who want shorter output can customize templates

## High-Level Design

### Components to Modify

#### 1. `PrincipalMappingFile.cs` (Platforms/Azure/)

Add three new properties:

```csharp
/// <summary>
/// Gets or sets the mapping of Azure DevOps user IDs (GUIDs) to display names.
/// </summary>
[JsonPropertyName("azdoUsers")]
public Dictionary<string, string>? AzdoUsers { get; set; }

/// <summary>
/// Gets or sets the mapping of Azure DevOps group descriptors to display names.
/// </summary>
[JsonPropertyName("azdoGroups")]
public Dictionary<string, string>? AzdoGroups { get; set; }

/// <summary>
/// Gets or sets the mapping of Azure DevOps project IDs (GUIDs) to display names.
/// </summary>
[JsonPropertyName("azdoProjects")]
public Dictionary<string, string>? AzdoProjects { get; set; }
```

**Why here?** This file is the canonical definition of the mapping file format. It remains in `Platforms/Azure/` as it's shared across multiple providers.

#### 2. `AzureMappingFileParser.cs` (Platforms/Azure/)

Update parsing logic:
- Check for azdo sections in `HasNestedSections()`
- Parse azdo sections into separate dictionaries (not merged with principals)
- Return azdo mappings in `AzureMappingFileResult`

**Why here?** This is the central parser for the mapping file format. It needs to understand all sections.

#### 3. `AzureMappingFileResult.cs` (Platforms/Azure/)

Add three new properties to return azdo mappings:

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

**Why here?** This is the parse result contract. All parsed mapping data flows through this record.

#### 4. Create `AzdoUserMapper.cs`, `AzdoGroupMapper.cs`, `AzdoProjectMapper.cs` (Providers/AzureDevOps/)

Three new mapper classes following the pattern established by `PrincipalMapper`:
- Constructor takes dictionary and optional `DiagnosticContext`
- `GetName(id)` method returns display name or null
- `GetEntityName(id)` method returns formatted string: `DisplayName [ID]`
- Track failed resolutions in diagnostic context

**Why here?** Provider-specific mapping logic belongs in the provider folder.

#### 5. Create `ScribanHelpers.AzureDevOps.cs` (Providers/AzureDevOps/Helpers/)

New Scriban helper functions:
- `azdo_user_name(userId, resourceAddress)` → resolves user ID
- `azdo_group_name(groupDescriptor, resourceAddress)` → resolves group descriptor
- `azdo_project_name(projectId, resourceAddress)` → resolves project ID

Each helper:
- Receives the appropriate mapper via closure
- Returns formatted string: `DisplayName [ID]` or just `ID` if not mapped
- Records failed resolutions for diagnostics

**Why here?** Provider-specific Scriban helpers belong with the provider.

#### 6. `AzureDevOpsModule.cs` (Providers/AzureDevOps/)

Update module registration:
- Instantiate the three mappers from `AzureMappingFileResult`
- Register azdo Scriban helpers in the template context
- Pass mappers to helpers via closures

**Why here?** Provider registration happens in the module class.

#### 7. `DiagnosticContext.cs` (Diagnostics/)

Add three new properties for azdo entity counts:

```csharp
/// <summary>
/// Gets or sets the number of Azure DevOps user mappings loaded.
/// </summary>
public int AzdoUserCount { get; set; }

/// <summary>
/// Gets or sets the number of Azure DevOps group mappings loaded.
/// </summary>
public int AzdoGroupCount { get; set; }

/// <summary>
/// Gets or sets the number of Azure DevOps project mappings loaded.
/// </summary>
public int AzdoProjectCount { get; set; }
```

Update diagnostic output generation:
- Add azdo entity counts to the "Principal Mapping" section
- Format as: `- Found N azdo users, M azdo groups, P azdo projects`
- Include azdo failed resolutions (already supported via `FailedResolutionType`)

**Why here?** Centralized diagnostic tracking for debug output.

#### 8. Update Templates (Providers/AzureDevOps/Templates/)

Update affected resource templates to use new helpers:
- `azuredevops_group_membership.sbn` → use `azdo_user_name` and `azdo_group_name`
- `azuredevops_project.sbn` → use `azdo_project_name`
- Other templates as needed

**Why here?** Provider-specific templates use provider-specific helpers.

### Data Flow

```
User provides mapping file
    ↓
AzureMappingFileParser parses JSON
    ↓
AzureMappingFileResult contains azdo dictionaries
    ↓
AzureDevOpsModule creates mappers
    ↓
Mappers registered with Scriban helpers
    ↓
Templates use helpers to resolve names
    ↓
Mappers track failed resolutions
    ↓
DiagnosticContext generates debug output
```

## Consequences

### Positive

- **Consistency**: Follows established patterns for Azure AD principal mapping
- **Separation of concerns**: Azure DevOps logic isolated in provider folder
- **Extensibility**: Easy to add more Azure DevOps entity types in the future
- **Diagnostics**: Full debug output for troubleshooting mapping issues
- **Type safety**: Separate mappers prevent mixing Azure AD and Azure DevOps entities

### Negative

- **Code duplication**: Three mapper classes instead of reusing `PrincipalMapper`
  - **Mitigation**: Each mapper is simple (~100 lines) and semantically distinct
- **Additional classes**: More files to maintain
  - **Mitigation**: Clear separation makes understanding and testing easier
- **Mapping file complexity**: More sections in the JSON file
  - **Mitigation**: All sections remain optional; users only add what they need

### Risks

1. **Long group descriptors**: Group descriptors can be very long (100+ characters)
   - **Mitigation**: Documented in specification; users can customize templates if needed
2. **Diagnostic output verbosity**: Adding three new count types to debug output
   - **Mitigation**: Only shown when `--debug` flag is used; users opt in

## Implementation Guidance

### For the Developer Agent

1. **Start with data model changes**: Update `PrincipalMappingFile`, `AzureMappingFileResult`
2. **Update parser**: Extend `AzureMappingFileParser` to handle azdo sections
3. **Create mappers**: Build three mapper classes in `Providers/AzureDevOps/`
4. **Create helpers**: Implement Scriban helpers in `Providers/AzureDevOps/Helpers/`
5. **Register in module**: Wire up mappers and helpers in `AzureDevOpsModule`
6. **Update diagnostics**: Add azdo counts to `DiagnosticContext`
7. **Update templates**: Use new helpers in Azure DevOps resource templates

### Key Patterns to Follow

- **Naming**: Use `Azdo` prefix for all Azure DevOps-specific types
- **Comments**: Add XML doc comments explaining the azdo entity types (user, group, project)
- **Tests**: Follow existing test patterns for `PrincipalMapper` and `AzureMappingFileParser`
- **Error handling**: Gracefully handle null/empty azdo sections (all optional)
- **Diagnostics**: Track counts and failed resolutions like Azure AD principals

### Integration Points

- **Existing integration**: CLI argument `--principal-mapping` already loads the file; no changes needed
- **Backward compatibility**: Existing mapping files without azdo sections continue to work
- **Template compatibility**: Existing templates work unchanged; new templates use new helpers

## Testing Strategy

1. **Unit tests** for mappers (similar to `PrincipalMapperTests`)
2. **Unit tests** for parser extension (add azdo sections to test cases)
3. **Unit tests** for Scriban helpers (similar to `ScribanHelpersPrincipalInfoTests`)
4. **Integration tests** with example mapping files showing azdo sections
5. **Snapshot tests** for rendered output with azdo entity resolution

## Documentation Updates

- Update example mapping file (`examples/comprehensive-demo/demo-principals-nested.json`) to include azdo sections
- Add section to README.md explaining Azure DevOps mapping
- Document the new Scriban helpers in template documentation

## Related Decisions

- **ADR-007**: Architecture Boundary Enforcement - Providers must be isolated
- **Feature 063**: Azure Display Enhancements - Established mapping file patterns
- **Feature 006**: Role Assignment Readable Display - Original principal mapping feature
