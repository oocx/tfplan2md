# Tasks: Azure DevOps Repository Mapping and Branch/Repo Icons

## Overview

This document breaks down the implementation of Azure DevOps repository mapping and semantic icons into actionable tasks. The feature extends the existing principal mapping infrastructure to support Azure DevOps repositories (mapping GUIDs to display names) and adds semantic icons (🗃️ for repositories, ⎇ for branches/refs) to improve report readability.

**Related Documents:**
- Specification: `docs/features/096-azdo-repo-mapping-and-icons/specification.md`
- Architecture: `docs/features/096-azdo-repo-mapping-and-icons/architecture.md`
- Test Plan: `docs/features/096-azdo-repo-mapping-and-icons/test-plan.md`
- Template Feature: `docs/features/085-azdo-principal-mapping/tasks.md` (reference implementation)

## Implementation Approach

**Test-First Development:** Each task follows the pattern:
1. Write unit tests first
2. Implement code to pass tests
3. Verify integration

**Provider Separation:** Respect the boundary between `Platforms/Azure/` (provider-agnostic) and `Providers/AzureDevOps/` (provider-specific) as defined in ADR-007.

**Pattern Consistency:** Follow Feature 085 patterns exactly for mapper and formatter implementation.

---

## Tasks

### Task 1: Extend Data Model for AzdoRepositories Section

**Priority:** High

**Description:**
Add `AzdoRepositories` property to `PrincipalMappingFile` class to support Azure DevOps repository GUID mappings. This is foundational work required by all other components.

**Acceptance Criteria:**
- [ ] `PrincipalMappingFile.cs` includes `AzdoRepositories` property with `JsonPropertyName("azdoRepositories")` attribute
- [ ] Property is of type `Dictionary<string, string>?` (nullable)
- [ ] XML documentation comments explain that this maps repository GUIDs to display names
- [ ] Documentation includes example mapping (e.g., `"a1b2c3d4-e5f6-...": "Infrastructure Repo"`)
- [ ] Feature reference comment added: `docs/features/096-azdo-repo-mapping-and-icons/specification.md`
- [ ] Test TC-01 passes: `PrincipalMappingFile_DeserializeAzdoRepositories_PopulatesProperty`
- [ ] Test TC-02 passes: `PrincipalMappingFile_DeserializeAllAzdoSections_IncludesRepositories`

**Dependencies:** None

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMappingFile.cs`
- Follow the exact pattern used for `AzdoUsers`, `AzdoGroups`, `AzdoProjects` (from Feature 085)
- Test file: `tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/PrincipalMappingFileTests.cs`

**Implementation Pattern:**
```csharp
/// <summary>
/// Gets or sets the mapping of Azure DevOps repository IDs (GUIDs) to display names.
/// </summary>
/// <remarks>
/// Azure DevOps repositories are identified by unique GUIDs. This mapping allows
/// displaying recognizable repository names in rendered Terraform plans.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </remarks>
/// <example>
/// "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo".
/// </example>
[JsonPropertyName("azdoRepositories")]
public Dictionary<string, string>? AzdoRepositories { get; set; }
```

---

### Task 2: Extend AzureMappingFileResult Record

**Priority:** High

**Description:**
Add `AzdoRepositories` property to `AzureMappingFileResult` record to return parsed Azure DevOps repository mappings. This is the contract for returning parsed data.

**Acceptance Criteria:**
- [ ] `AzureMappingFileResult` includes `AzdoRepositories` property of type `FrozenDictionary<string, string>`
- [ ] Property is added to the positional record constructor (after `AzdoProjects`)
- [ ] Property is non-nullable (empty dictionary when no mappings)
- [ ] All constructor call sites updated to include the new parameter

**Dependencies:** Task 1

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileResult.cs` (or within `AzureMappingFileParser.cs` if internal record)
- Use `FrozenDictionary` for performance (follows existing pattern)
- Parser tests will validate this change

**Implementation Pattern:**
```csharp
internal sealed record AzureMappingFileResult(
    FrozenDictionary<string, string> Principals,
    FrozenDictionary<string, string> PrincipalTypes,
    IReadOnlyList<MappingEntry> Subscriptions,
    IReadOnlyList<MappingEntry> ManagementGroups,
    IReadOnlyList<MappingEntry> Tenants,
    IReadOnlyList<MappingEntry> Roles,
    FrozenDictionary<string, string> AzdoUsers,
    FrozenDictionary<string, string> AzdoGroups,
    FrozenDictionary<string, string> AzdoProjects,
    FrozenDictionary<string, string> AzdoRepositories);  // NEW
```

---

### Task 3: Extend DiagnosticContext for Repository Tracking

**Priority:** High

**Description:**
Add diagnostic properties to track Azure DevOps repository counts and extend `FailedResolutionType` enum to include repository resolution failures.

**Acceptance Criteria:**
- [ ] `DiagnosticContext` includes `AzdoRepositoryCount` property (int)
- [ ] XML documentation comment added for the new property
- [ ] Feature reference comment added: `docs/features/096-azdo-repo-mapping-and-icons/specification.md`
- [ ] `FailedResolutionType` enum includes `AzdoRepository` value
- [ ] XML documentation comment added for the new enum value

**Dependencies:** None

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`
- File location: `src/Oocx.TfPlan2Md/Diagnostics/FailedResolutionType.cs` (or wherever the enum is defined)
- Required by mapper for diagnostic tracking
- Required by tests TC-21 and TC-22

**Implementation Pattern:**
```csharp
// In DiagnosticContext.cs
/// <summary>
/// Gets or sets the count of Azure DevOps repository mappings loaded.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
public int AzdoRepositoryCount { get; set; }

// In FailedResolutionType.cs
/// <summary>
/// Azure DevOps repository resolution failure.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
AzdoRepository,
```

---

### Task 4: Update AzureMappingFileParser to Parse AzdoRepositories Section

**Priority:** High

**Description:**
Extend the parser to read the `azdoRepositories` section from the mapping JSON file and return it in the result object. Includes updating diagnostic counts.

**Acceptance Criteria:**
- [ ] Parser reads `azdoRepositories` section from JSON and populates `AzdoRepositories` dictionary
- [ ] Null or missing `azdoRepositories` section results in empty dictionary (not null)
- [ ] Repository count is set in `DiagnosticContext` (if provided): `diagnosticContext.AzdoRepositoryCount = azdoRepositories.Count`
- [ ] `azdoRepositories` mappings are kept separate from other sections (no cross-contamination)
- [ ] `HasNestedSections()` method updated to include `AzdoRepositories` check (if method exists)
- [ ] `RecordNestedDiagnostics()` method updated to include repository count (if method exists)
- [ ] Flat format fallback includes empty `AzdoRepositories` parameter
- [ ] Test TC-03 passes: Parser reads azdoRepositories correctly
- [ ] Test TC-04 passes: Multiple repository mappings are preserved
- [ ] Test TC-05 passes: Mixed Azure AD and azdo sections parse independently
- [ ] Test TC-06 passes: Null azdoRepositories section handled gracefully
- [ ] Test TC-07 passes: Missing azdoRepositories maintains backwards compatibility

**Dependencies:** Task 1, Task 2, Task 3

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileParser.cs`
- Test file: `tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileParserTests.cs`
- Follow the exact parsing pattern from Feature 085 for `azdoUsers`, `azdoGroups`, `azdoProjects`
- Update `TryParseNested()` method (or equivalent)

**Implementation Pattern:**
```csharp
// In TryParseNested or similar method
var azdoRepositories = nestedMapping.AzdoRepositories ?? new Dictionary<string, string>();

if (diagnosticContext != null)
{
    diagnosticContext.AzdoRepositoryCount = azdoRepositories.Count;
}

return new AzureMappingFileResult(
    // ... existing parameters ...
    azdoUsers.ToFrozenDictionary(StringComparer.Ordinal),
    azdoGroups.ToFrozenDictionary(StringComparer.Ordinal),
    azdoProjects.ToFrozenDictionary(StringComparer.Ordinal),
    azdoRepositories.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));  // NEW - use OrdinalIgnoreCase for GUIDs
```

---

### Task 5: Create AzdoRepositoryMapper Class

**Priority:** High

**Description:**
Create a mapper class for Azure DevOps repositories that resolves repository GUIDs to display names in the format `🗃️ DisplayName [GUID]` (when mapped) or `🗃️ GUID` (when unmapped).

**Acceptance Criteria:**
- [ ] `AzdoRepositoryMapper` class created in `Providers/AzureDevOps/` namespace
- [ ] Constructor accepts `FrozenDictionary<string, string>` and optional `DiagnosticContext?`
- [ ] `GetName(repositoryId, resourceAddress = null)` method returns display name or null if not found
- [ ] `GetEntityName(repositoryId)` method returns formatted string with 🗃️ icon: `🗃️ DisplayName [GUID]` or `🗃️ GUID`
- [ ] Icon uses non-breaking space (U+00A0) between emoji and text
- [ ] Failed resolutions are tracked in DiagnosticContext with type `FailedResolutionType.AzdoRepository`
- [ ] GUID lookups use case-insensitive comparison (StringComparer.OrdinalIgnoreCase)
- [ ] Test TC-08 passes: Known repository IDs return formatted names with icon
- [ ] Test TC-09 passes: Unknown repository IDs return GUID with icon only
- [ ] Test TC-10 passes: GetName returns null for unmapped IDs
- [ ] Test TC-11 passes: Case-insensitive GUID matching works
- [ ] Test TC-12 passes: Failed resolutions are recorded in diagnostics

**Dependencies:** Task 2, Task 3

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoRepositoryMapper.cs`
- Test file: `tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoRepositoryMapperTests.cs`
- Clone implementation from `AzdoUserMapper.cs` (Feature 085) with these changes:
  - Replace "user" with "repository" throughout
  - Replace 👤 icon with 🗃️ icon
  - Use StringComparer.OrdinalIgnoreCase in dictionary lookups (repositories are GUIDs)
- Icon constant: `🗃️ ` (U+1F5C3 + U+00A0)

**Implementation Pattern:**
```csharp
public sealed class AzdoRepositoryMapper
{
    private const string RepositoryIcon = "🗃️\u00A0"; // file cabinet + non-breaking space
    private readonly FrozenDictionary<string, string> _repositoryMappings;
    private readonly DiagnosticContext? _diagnostics;

    public AzdoRepositoryMapper(FrozenDictionary<string, string> repositoryMappings, DiagnosticContext? diagnostics)
    {
        _repositoryMappings = repositoryMappings;
        _diagnostics = diagnostics;
    }

    public string? GetName(string repositoryId, string? resourceAddress = null)
    {
        if (_repositoryMappings.TryGetValue(repositoryId, out var displayName))
        {
            return displayName;
        }

        if (resourceAddress != null)
        {
            _diagnostics?.RecordFailedResolution(FailedResolutionType.AzdoRepository, repositoryId, resourceAddress);
        }

        return null;
    }

    public string GetEntityName(string repositoryId)
    {
        var displayName = GetName(repositoryId);
        return displayName != null 
            ? $"{RepositoryIcon}{displayName} [{repositoryId}]" 
            : $"{RepositoryIcon}{repositoryId}";
    }
}
```

---

### Task 6: Create AzdoRepositoryIdFormatter Class

**Priority:** High

**Description:**
Create a value formatter class for repository ID attributes that formats values as `🗃️ DisplayName (GUID)` in table contexts.

**Acceptance Criteria:**
- [ ] `AzdoRepositoryIdFormatter` class created in `Providers/AzureDevOps/` namespace
- [ ] Implements `IValueFormatter` interface
- [ ] Constructor accepts `AzdoRepositoryMapper` reference
- [ ] `TryFormat(ServiceResolutionContext context)` method formats repository IDs with icon and display name
- [ ] Returns formatted value `🗃️ DisplayName (GUID)` when mapped (with code wrapping for markdown)
- [ ] Returns null when repository ID is not mapped (falls back to semantic icon formatting)
- [ ] Icon uses non-breaking space between emoji and text
- [ ] Test TC-13 passes: Formatter returns formatted value with icon when mapped
- [ ] Test TC-14 passes: Formatter returns null when not mapped
- [ ] Test TC-14 passes: Formatted output includes markdown code wrapping

**Dependencies:** Task 5

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoRepositoryIdFormatter.cs`
- Test file: `tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoRepositoryIdFormatterTests.cs`
- Clone implementation from `AzdoUserIdFormatter.cs` (Feature 085) with these changes:
  - Replace "user" with "repository" throughout
  - Replace 👤 icon with 🗃️ icon
- Table format uses parentheses: `DisplayName (GUID)`, template format uses brackets: `DisplayName [GUID]`

**Implementation Pattern:**
```csharp
public sealed class AzdoRepositoryIdFormatter : IValueFormatter
{
    private const string RepositoryIcon = "🗃️\u00A0";
    private readonly AzdoRepositoryMapper _mapper;

    public AzdoRepositoryIdFormatter(AzdoRepositoryMapper mapper)
    {
        _mapper = mapper;
    }

    public string? TryFormat(ServiceResolutionContext context)
    {
        var repositoryId = context.Value;
        var displayName = _mapper.GetName(repositoryId, context.ResourceAddress);
        
        if (displayName == null)
        {
            return null; // Fall back to semantic icon formatting
        }

        // Table context: wrap in code spans with icon and display name
        return $"`{RepositoryIcon}{displayName} ({repositoryId})`";
    }
}
```

---

### Task 7: Add Repository Icon Semantic Formatting

**Priority:** High

**Description:**
Add `TryFormatRepositoryAttribute` method to `SemanticFormatting.Identity.cs` to apply the 🗃️ icon to repository-related attributes across all rendering contexts.

**Acceptance Criteria:**
- [ ] `TryFormatRepositoryAttribute` method added to `SemanticFormatting.Identity.cs`
- [ ] Method applies 🗃️ icon to these attributes (case-insensitive): `repo_id`, `repository_id`, `source_repo_id`, `target_repo_id`
- [ ] Icon uses non-breaking space between emoji and value
- [ ] Returns false for non-repository attributes
- [ ] Method follows existing pattern (uses `FormatIconValue` helper)
- [ ] `TryFormatSemanticValue` method updated to call `TryFormatRepositoryAttribute`
- [ ] `TryFormatRepositoryAttributePlain` method added for plain text formatting
- [ ] `FormatAttributeValuePlain` method updated to call `TryFormatRepositoryAttributePlain`
- [ ] XML documentation comments include feature reference
- [ ] Test TC-15 passes: Repository attributes are formatted with icon and non-breaking space
- [ ] Test TC-16 passes: Icons render correctly in table context (code spans)
- [ ] Test TC-16 passes: Icons render correctly in summary context (HTML code elements)
- [ ] Test TC-17 passes: Non-repository attributes are not affected

**Dependencies:** None

**Notes:**
- File location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs`
- Test file: `tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Helpers/SemanticFormattingTests.cs`
- Add after existing `TryFormatSubscriptionAttribute` check in `TryFormatSemanticValue`
- Icon constant: `🗃️\u00A0` (U+1F5C3 + U+00A0)

**Implementation Pattern:**
```csharp
/// <summary>
/// Determines whether an attribute represents a repository value and formats it with the repository icon.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
/// <param name="attributeName">The attribute name to evaluate.</param>
/// <param name="value">The raw attribute value.</param>
/// <param name="context">The rendering context.</param>
/// <param name="formatted">Formatted result when the attribute is a repository identifier.</param>
/// <returns>True when the attribute was formatted; otherwise false.</returns>
private static bool TryFormatRepositoryAttribute(string attributeName, string value, ValueFormatContext context, out string formatted)
{
    if (!attributeName.Equals("repo_id", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("repository_id", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("source_repo_id", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("target_repo_id", StringComparison.OrdinalIgnoreCase))
    {
        formatted = string.Empty;
        return false;
    }

    formatted = FormatIconValue($"🗃️\u00A0{value}", context, false);
    return true;
}

private static bool TryFormatRepositoryAttributePlain(string attributeName, string value, out string formatted)
{
    if (!attributeName.Equals("repo_id", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("repository_id", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("source_repo_id", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("target_repo_id", StringComparison.OrdinalIgnoreCase))
    {
        formatted = string.Empty;
        return false;
    }

    formatted = FormatIconValuePlain($"🗃️\u00A0{value}");
    return true;
}
```

---

### Task 8: Add Branch/Ref Icon Semantic Formatting

**Priority:** High

**Description:**
Add `TryFormatBranchAttribute` method to `SemanticFormatting.Identity.cs` to apply the ⎇ icon to branch/ref-related attributes across all rendering contexts.

**Acceptance Criteria:**
- [ ] `TryFormatBranchAttribute` method added to `SemanticFormatting.Identity.cs`
- [ ] Method applies ⎇ icon to these attributes (case-insensitive): `default_branch`, `branch_name`, `ref_name`, `source_branch`, `target_branch`
- [ ] Icon uses non-breaking space between emoji and value
- [ ] Returns false for non-branch attributes
- [ ] Method follows existing pattern (uses `FormatIconValue` helper)
- [ ] `TryFormatSemanticValue` method updated to call `TryFormatBranchAttribute` (after repository check)
- [ ] `TryFormatBranchAttributePlain` method added for plain text formatting
- [ ] `FormatAttributeValuePlain` method updated to call `TryFormatBranchAttributePlain`
- [ ] XML documentation comments include feature reference
- [ ] Test TC-18 passes: Branch attributes are formatted with icon and non-breaking space
- [ ] Test TC-19 passes: Icons render correctly in table context (code spans)
- [ ] Test TC-19 passes: Icons render correctly in summary context (HTML code elements)
- [ ] Test TC-20 passes: Non-branch attributes are not affected

**Dependencies:** None

**Notes:**
- File location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs`
- Test file: `tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Helpers/SemanticFormattingTests.cs`
- Add after `TryFormatRepositoryAttribute` check in `TryFormatSemanticValue`
- Icon constant: `⎇\u00A0` (U+2387 + U+00A0)
- Branch values are NOT mapped (they're already human-readable strings)

**Implementation Pattern:**
```csharp
/// <summary>
/// Determines whether an attribute represents a branch/ref value and formats it with the branch icon.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
/// <param name="attributeName">The attribute name to evaluate.</param>
/// <param name="value">The raw attribute value.</param>
/// <param name="context">The rendering context.</param>
/// <param name="formatted">Formatted result when the attribute is a branch/ref identifier.</param>
/// <returns>True when the attribute was formatted; otherwise false.</returns>
private static bool TryFormatBranchAttribute(string attributeName, string value, ValueFormatContext context, out string formatted)
{
    if (!attributeName.Equals("default_branch", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("branch_name", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("ref_name", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("source_branch", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("target_branch", StringComparison.OrdinalIgnoreCase))
    {
        formatted = string.Empty;
        return false;
    }

    formatted = FormatIconValue($"⎇\u00A0{value}", context, false);
    return true;
}

private static bool TryFormatBranchAttributePlain(string attributeName, string value, out string formatted)
{
    if (!attributeName.Equals("default_branch", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("branch_name", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("ref_name", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("source_branch", StringComparison.OrdinalIgnoreCase)
        && !attributeName.Equals("target_branch", StringComparison.OrdinalIgnoreCase))
    {
        formatted = string.Empty;
        return false;
    }

    formatted = FormatIconValuePlain($"⎇\u00A0{value}");
    return true;
}
```

---

### Task 9: Register Repository Mapper and Formatter in AzureDevOpsModule

**Priority:** Medium

**Description:**
Update `AzureDevOpsModule` to instantiate the repository mapper and formatter, and register them in the template context and value formatter registry.

**Acceptance Criteria:**
- [ ] `AzureDevOpsModule` constructor accepts `AzdoRepositoryMapper?` parameter
- [ ] Field added to store mapper reference: `private readonly AzdoRepositoryMapper? _azdoRepositoryMapper;`
- [ ] `RegisterHelpers` method registers `azdo_repository_name` Scriban helper (if mapper provided)
- [ ] Helper returns result of `mapper.GetEntityName(repoId)`
- [ ] `RegisterValueFormatters` method registers `AzdoRepositoryIdFormatter` (if mapper provided)
- [ ] Formatter registration uses MatchPattern: provider `azuredevops`, attribute pattern `^repo_id$|^repository_id$|^source_repo_id$|^target_repo_id$`, value pattern `GuidPattern`
- [ ] Module gracefully handles null mapper (when mapping file not provided or has no repositories)
- [ ] Test TC-23 passes: Scriban helper is registered and callable
- [ ] Test TC-13 passes: Value formatter is registered and applies to repository attributes

**Dependencies:** Task 5, Task 6

**Notes:**
- File location: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs`
- Follow the exact registration pattern from Feature 085 for users, groups, and projects
- GUID pattern should be reused from existing Azure value formatters

**Implementation Pattern:**
```csharp
// Constructor parameter
public AzureDevOpsModule(
    // ... existing parameters ...
    AzdoRepositoryMapper? azdoRepositoryMapper)
{
    // ... existing initialization ...
    _azdoRepositoryMapper = azdoRepositoryMapper;
}

// In RegisterHelpers
if (_azdoRepositoryMapper is not null)
{
    scriptObject.Import("azdo_repository_name", 
        new Func<string, string>(repoId => _azdoRepositoryMapper.GetEntityName(repoId)));
}

// In RegisterValueFormatters
if (_azdoRepositoryMapper is not null)
{
    var repositoryFormatter = new AzdoRepositoryIdFormatter(_azdoRepositoryMapper);
    registry.Register(
        new MatchPattern(
            "(^azuredevops$|.*/azuredevops$)",
            null,
            "^repo_id$|^repository_id$|^source_repo_id$|^target_repo_id$",
            AzureValueFormatterRegistration.GuidPattern),
        repositoryFormatter);
}
```

---

### Task 10: Update CompositionRoot to Create Repository Mapper

**Priority:** Medium

**Description:**
Update `CompositionRoot` to create the `AzdoRepositoryMapper` instance and pass it to the provider registry during composition.

**Acceptance Criteria:**
- [ ] `CreateAzdoRepositoryMapper` method added to `CompositionRoot`
- [ ] Method signature: `internal AzdoRepositoryMapper CreateAzdoRepositoryMapper(AzureMappingFileResult mappingResult, DiagnosticContext? diagnostics)`
- [ ] Method creates mapper with `mappingResult.AzdoRepositories` dictionary
- [ ] Method passes diagnostic context to mapper
- [ ] XML documentation comments added with feature reference
- [ ] `CreateProviderRegistry` method signature updated to accept `AzdoRepositoryMapper azdoRepositoryMapper` parameter
- [ ] Mapper is passed to `AzureDevOpsModule` constructor

**Dependencies:** Task 5, Task 9

**Notes:**
- File location: `src/Oocx.TfPlan2Md/CompositionRoot.cs`
- Follow the exact pattern from Feature 085 for creating user/group/project mappers
- Mapper instance should be created once and shared across the provider registry

**Implementation Pattern:**
```csharp
/// <summary>
/// Creates the Azure DevOps repository mapper for repository ID resolution.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
/// <param name="mappingResult">The Azure mapping data loaded from file.</param>
/// <param name="diagnostics">Optional diagnostic context for troubleshooting.</param>
/// <returns>A configured Azure DevOps repository mapper instance.</returns>
internal AzdoRepositoryMapper CreateAzdoRepositoryMapper(
    AzureMappingFileResult mappingResult,
    DiagnosticContext? diagnostics)
{
    return new AzdoRepositoryMapper(mappingResult.AzdoRepositories, diagnostics);
}

// In CreateProviderRegistry - add parameter and pass to module
internal ProviderRegistry CreateProviderRegistry(
    // ... existing parameters ...
    AzdoRepositoryMapper azdoRepositoryMapper)
{
    var azureDevOpsModule = new AzureDevOpsModule(
        // ... existing parameters ...
        azdoRepositoryMapper);
    
    // ... rest of method ...
}
```

---

### Task 11: Update ProgramEntry to Wire Up Repository Mapper

**Priority:** Medium

**Description:**
Update `ProgramEntry` to create the `AzdoRepositoryMapper` instance and pass it through the composition chain to the provider registry.

**Acceptance Criteria:**
- [ ] `ProgramEntry` creates `azdoRepositoryMapper` using `compositionRoot.CreateAzdoRepositoryMapper(mappingResult, diagnosticContext)`
- [ ] Mapper is passed to `CreateProviderRegistry` method call
- [ ] Integration completes the dependency injection chain
- [ ] Null mapping result is handled gracefully (empty dictionary passed to mapper)

**Dependencies:** Task 10

**Notes:**
- File location: `src/Oocx.TfPlan2Md/ProgramEntry.cs`
- Follow the exact pattern from Feature 085 for wiring up user/group/project mappers
- This completes the composition chain: ProgramEntry → CompositionRoot → AzureDevOpsModule

**Implementation Pattern:**
```csharp
// Where mapping file is loaded and other mappers are created
var azdoRepositoryMapper = compositionRoot.CreateAzdoRepositoryMapper(mappingResult, diagnosticContext);

var providerRegistry = compositionRoot.CreateProviderRegistry(
    // ... existing parameters ...
    azdoRepositoryMapper);
```

---

### Task 12: Update Diagnostic Output to Include Repository Count

**Priority:** Medium

**Description:**
Extend the diagnostic output generation to include Azure DevOps repository count when `--debug` flag is used.

**Acceptance Criteria:**
- [ ] Diagnostic output includes repository count in the principal mapping section
- [ ] Format matches existing pattern: displayed alongside other azdo entity counts
- [ ] Zero count is displayed if mapping file has no `azdoRepositories` section
- [ ] Output appears in the same section as other azdo counts
- [ ] Test TC-21 passes: Repository count appears in diagnostic output when repositories are mapped
- [ ] Test TC-22 passes: Zero count appears when no repositories are mapped

**Dependencies:** Task 3, Task 4

**Notes:**
- File location: Wherever diagnostic output is generated (likely `DiagnosticOutputGenerator.cs` or similar)
- Test file: `tests/Oocx.TfPlan2Md.TUnit/Diagnostics/DiagnosticOutputTests.cs`
- Follow existing pattern for displaying azdo user/group/project counts

**Implementation Pattern:**
```csharp
// In diagnostic output generation
if (diagnostics.AzdoUserCount > 0 || diagnostics.AzdoGroupCount > 0 
    || diagnostics.AzdoProjectCount > 0 || diagnostics.AzdoRepositoryCount > 0)
{
    output.AppendLine($"  - Found {diagnostics.AzdoUserCount} azdo users, " +
                     $"{diagnostics.AzdoGroupCount} azdo groups, " +
                     $"{diagnostics.AzdoProjectCount} azdo projects, " +
                     $"{diagnostics.AzdoRepositoryCount} azdo repositories");
}
```

---

### Task 13: Update Example Mapping File

**Priority:** Low

**Description:**
Update the comprehensive demo mapping file to include the `azdoRepositories` section with realistic example data.

**Acceptance Criteria:**
- [ ] `demo-principals-nested.json` includes `azdoRepositories` section
- [ ] Section contains 2-3 example repository mappings with realistic GUIDs
- [ ] Repository names are descriptive (e.g., "Infrastructure Repo", "Web Application Repo", "Shared Libraries")
- [ ] Example file remains valid JSON after update
- [ ] File demonstrates all four azdo sections together (users, groups, projects, repositories)
- [ ] Test TC-05 passes: Example file parses successfully with all azdo sections

**Dependencies:** Task 4

**Notes:**
- File location: `examples/comprehensive-demo/demo-principals-nested.json`
- Use realistic-looking GUIDs for repository IDs
- Consider adding a comment in adjacent documentation explaining the new section

**Example Data:**
```json
{
  "users": { ... },
  "groups": { ... },
  "azdoUsers": { ... },
  "azdoGroups": { ... },
  "azdoProjects": { ... },
  "azdoRepositories": {
    "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo",
    "f9e8d7c6-b5a4-3210-fedc-ba9876543210": "Web Application Repo",
    "01234567-89ab-cdef-0123-456789abcdef": "Shared Libraries"
  },
  "subscriptions": [ ... ],
  "tenants": [ ... ]
}
```

---

### Task 14: Create Integration Test Data and Snapshots

**Priority:** Low

**Description:**
Create test data files and snapshot baselines for integration testing of Azure DevOps resource rendering with repository mapping and icons.

**Acceptance Criteria:**
- [ ] Mapping file created for azuredevops-repo-mapping test with repository mappings
- [ ] Terraform plan JSON created with azuredevops resources containing repository and branch attributes
- [ ] Expected snapshot created showing repository mapping: `🗃️ DisplayName [GUID]` format
- [ ] Expected snapshot created showing repository icon on unmapped IDs: `🗃️ GUID` format
- [ ] Expected snapshot created showing branch icons: `⎇ branch-name` format
- [ ] Test TC-24 passes: End-to-end rendering with repository mapping
- [ ] Test TC-25 passes: Repository icons appear on unmapped repository IDs
- [ ] Test TC-26 passes: Branch icons appear on branch/ref attributes
- [ ] Test TC-27 passes: Backwards compatibility - resources render correctly without mapping file

**Dependencies:** Task 7, Task 8, Task 9, Task 11

**Notes:**
- File locations:
  - Test data: `TestData/` directory
  - Snapshots: `tests/TestData/Snapshots/` directory
- Use `scripts/update-snapshots.sh` to generate initial snapshots after manually verifying output
- Create test cases for:
  - `azuredevops_build_definition` (has repository_id)
  - `azuredevops_git_repository` (has default_branch)
  - `azuredevops_branch_policy_min_reviewers` (has repository_id and branch_name)

**Test Resources:**
- Repository with mapping: shows `🗃️ Infrastructure Repo [a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d]`
- Repository without mapping: shows `🗃️ unmapped-guid-here`
- Branch attribute: shows `⎇ refs/heads/main`

---

### Task 15: Update Documentation

**Priority:** Low

**Description:**
Update README and other documentation to explain the new Azure DevOps repository mapping and icon features.

**Acceptance Criteria:**
- [ ] README.md includes section on Azure DevOps repository mapping
- [ ] Documentation explains the `azdoRepositories` section format
- [ ] Example JSON snippet shows how to use `azdoRepositories`
- [ ] Documentation references the comprehensive demo mapping file
- [ ] Documentation explains the output formats:
  - `🗃️ DisplayName [GUID]` for mapped repositories
  - `🗃️ GUID` for unmapped repositories
  - `⎇ branch-name` for branch/ref attributes
- [ ] Documentation mentions the semantic icons and which attributes they apply to
- [ ] Scriban helper `azdo_repository_name` documented (if there's template documentation)

**Dependencies:** Task 13, Task 14

**Notes:**
- File locations:
  - `README.md`
  - Any provider-specific documentation files
  - Feature documentation in `docs/` (if applicable)
- Keep documentation concise and practical
- Include at least one complete example showing repositories and branches together
- Mention that this feature builds on Feature 085 (Azure DevOps Principal Mapping)

**Documentation Example:**
```markdown
### Azure DevOps Repository Mapping

Map Azure DevOps repository GUIDs to human-readable names:

```json
{
  "azdoRepositories": {
    "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo",
    "f9e8d7c6-b5a4-3210-fedc-ba9876543210": "Web Application Repo"
  }
}
```

Repositories display with the 🗃️ icon, branches with the ⎇ icon.
```

---

## Implementation Order

The recommended sequence for implementation (respects dependencies and minimizes risk):

1. **Phase 1: Foundation (Tasks 1-3)** - Data model and diagnostics
   - Task 1: Extend PrincipalMappingFile
   - Task 2: Extend AzureMappingFileResult
   - Task 3: Extend DiagnosticContext
   - **Milestone:** Data structures ready for parsing and mapping

2. **Phase 2: Parsing (Task 4)** - Read mapping file
   - Task 4: Update AzureMappingFileParser
   - **Milestone:** Mapping file can be loaded and parsed with repository section

3. **Phase 3: Mapper and Formatter (Tasks 5-6)** - Core mapping logic
   - Task 5: Create AzdoRepositoryMapper
   - Task 6: Create AzdoRepositoryIdFormatter
   - **Milestone:** Repository resolution logic complete

4. **Phase 4: Semantic Icons (Tasks 7-8)** - Icon formatting
   - Task 7: Add Repository Icon Semantic Formatting
   - Task 8: Add Branch/Ref Icon Semantic Formatting
   - **Milestone:** Icons apply to all resources uniformly

5. **Phase 5: Integration (Tasks 9-12)** - Wire everything together
   - Task 9: Register in AzureDevOpsModule
   - Task 10: Update CompositionRoot
   - Task 11: Update ProgramEntry
   - Task 12: Update Diagnostic Output
   - **Milestone:** Feature fully integrated and functional

6. **Phase 6: Polish (Tasks 13-15)** - Examples, tests, documentation
   - Task 13: Update Example Mapping File
   - Task 14: Create Integration Test Data
   - Task 15: Update Documentation
   - **Milestone:** Feature complete with examples and documentation

## Open Questions

1. **Attribute Name Coverage:** The specified attribute names (`repo_id`, `repository_id`, `source_repo_id`, `target_repo_id`, `default_branch`, `branch_name`, `ref_name`, `source_branch`, `target_branch`) are based on analysis of common Azure DevOps resources. Should we verify these cover all repository/branch attributes?
   - **Recommendation:** Start with the specified list and extend if gaps found during testing or user feedback

2. **Icon Unicode Consistency:** Should we verify that the chosen icons (🗃️ U+1F5C3, ⎇ U+2387) render correctly across all target platforms (GitHub, Azure DevOps, browsers)?
   - **Recommendation:** Test during UAT; icons are well-supported in modern platforms

3. **Diagnostic Output Format:** Should repository count be displayed on the same line as other azdo counts or on a separate line?
   - **Recommendation:** Same line for consistency: "Found X users, Y groups, Z projects, W repositories"

4. **Template Investigation:** Which Azure DevOps resource templates currently exist that would benefit from the `azdo_repository_name` helper?
   - **Action:** Investigate `Providers/AzureDevOps/Templates/` directory during implementation
   - **Candidates:** `azuredevops_build_definition`, `azuredevops_git_repository`, `azuredevops_branch_policy_*`

## Test Execution

Run tests after each phase:

```bash
# Run all tests
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx

# Run specific test class
scripts/test-with-timeout.sh -- dotnet test --project tests/Oocx.TfPlan2Md.TUnit/ --filter "FullyQualifiedName~AzdoRepositoryMapperTests"

# Update snapshots (after verifying output manually)
scripts/update-snapshots.sh
```

## Definition of Done

The feature is complete when:

- [ ] All 15 tasks are completed with acceptance criteria met
- [ ] All 27 test cases (TC-01 through TC-27) pass
- [ ] No regressions in existing tests
- [ ] Code review completed (via `code_review` tool)
- [ ] Security scan completed (via `codeql_checker` tool)
- [ ] Documentation updated (README and examples)
- [ ] Example mapping file demonstrates `azdoRepositories` section
- [ ] Snapshot tests capture expected icon rendering behavior
- [ ] Diagnostic output includes repository count
- [ ] Feature works end-to-end: mapping file → parser → mapper → formatter → semantic icons → rendered output
- [ ] Backwards compatibility verified: mapping files without `azdoRepositories` continue to work
- [ ] Icons verified: 🗃️ on repository attributes, ⎇ on branch/ref attributes

## Notes for Developer

- **Test-First:** Write unit tests before implementation for each task
- **Incremental:** Complete tasks in order; each builds on previous work
- **Pattern Matching:** Follow Feature 085 patterns exactly for mapper and formatter
- **Provider Separation:** Keep Azure DevOps logic in `Providers/AzureDevOps/`, semantic icons in `MarkdownGeneration/Helpers/ScribanHelpers/`
- **Error Handling:** Gracefully handle null/empty `azdoRepositories` section (all optional)
- **Backwards Compatibility:** Existing mapping files without `azdoRepositories` must continue to work
- **Icon Constants:** Use `\u00A0` (non-breaking space) after icons for consistent spacing
- **Case Sensitivity:** Use `StringComparer.OrdinalIgnoreCase` for GUID lookups (repositories are GUIDs)
- **Format Consistency:** 
  - Templates: `🗃️ DisplayName [GUID]`
  - Tables: `🗃️ DisplayName (GUID)`
  - Unmapped: `🗃️ GUID`
  - Branches: `⎇ branch-name` (no mapping)
