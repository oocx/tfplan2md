# Architecture: Azure DevOps Repository Mapping and Branch/Repo Icons

## Status

Proposed

## Context

This feature extends the principal mapping file to support Azure DevOps repository mappings and adds semantic icons for repository and branch/ref attributes. It builds directly on Feature 085 (Azure DevOps Principal Mapping) by following the same architectural patterns established for `azdoUsers`, `azdoGroups`, and `azdoProjects`.

Azure DevOps resources frequently reference repositories by GUID (e.g., `azuredevops_build_definition`, `azuredevops_git_repository`, `azuredevops_branch_policy_*`). These GUIDs are not human-readable, making plans difficult to review. Additionally, branch and repository attributes lack visual distinction in reports.

## Design Decision

Follow the exact same pattern as Feature 085 to maintain consistency:

1. **Extend PrincipalMappingFile** to include `AzdoRepositories` dictionary
2. **Create AzdoRepositoryMapper** following the same pattern as AzdoUserMapper, AzdoGroupMapper, AzdoProjectMapper
3. **Create AzdoRepositoryIdFormatter** following the same pattern as AzdoUserIdFormatter, AzdoGroupIdFormatter, AzdoProjectIdFormatter
4. **Register formatter in AzureDevOpsModule** following the existing pattern
5. **Add icon formatters** in `SemanticFormatting.Identity.cs` for repositories and branches (🗃️ and ⎇)

This approach maximizes consistency, reuses existing infrastructure, and follows established project patterns.

## Components Affected

### 1. Data Model Layer

#### File: `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMappingFile.cs`

**Change:** Add new property for repository mappings

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

**Rationale:** Follows the exact pattern established for AzdoUsers, AzdoGroups, and AzdoProjects in Feature 085.

---

#### File: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileResult.cs`

**Change:** Add AzdoRepositories parameter to record

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
    FrozenDictionary<string, string> AzdoRepositories)  // NEW
```

**Rationale:** Maintains consistency with existing azdo entity storage pattern.

---

#### File: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileParser.cs`

**Change 1:** Parse `azdoRepositories` section in `TryParseNested`

```csharp
// After parsing AzdoProjects
var azdoRepositories = nestedMapping.AzdoRepositories ?? new Dictionary<string, string>();
```

**Change 2:** Include in result construction

```csharp
return new AzureMappingFileResult(
    // ... existing parameters ...
    azdoUsers.ToFrozenDictionary(StringComparer.Ordinal),
    azdoGroups.ToFrozenDictionary(StringComparer.Ordinal),
    azdoProjects.ToFrozenDictionary(StringComparer.Ordinal),
    azdoRepositories.ToFrozenDictionary(StringComparer.Ordinal));  // NEW
```

**Change 3:** Update `HasNestedSections` to include AzdoRepositories check

**Change 4:** Update `RecordNestedDiagnostics` to include repository count

```csharp
diagnosticContext.AzdoRepositoryCount = nestedMapping.AzdoRepositories?.Count ?? 0;
```

**Change 5:** Update flat format fallback to include empty AzdoRepositories

**Rationale:** Follows exact parsing pattern from Feature 085.

---

### 2. Mapping and Formatting Layer

#### File: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoRepositoryMapper.cs` (NEW)

**Purpose:** Maps repository GUIDs to display names

**Implementation Pattern:** Clone of AzdoUserMapper.cs with these changes:
- Replace "user" with "repository" throughout
- Use StringComparer.OrdinalIgnoreCase for GUID lookups (repositories are GUIDs like users, unlike group descriptors which are case-sensitive)
- Format as `DisplayName [GUID]` when mapped, just `GUID` when unmapped
- Include 🗃️ icon in GetEntityName() output

**Key Methods:**
- `GetName(string repositoryId)` - returns display name or null
- `GetName(string repositoryId, string? resourceAddress)` - with diagnostic tracking
- `GetEntityName(string repositoryId)` - returns formatted `🗃️ DisplayName [GUID]` or `🗃️ GUID`

**Rationale:** Maintains consistency with existing mapper pattern; uses OrdinalIgnoreCase because repository IDs are GUIDs (same as user IDs).

---

#### File: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoRepositoryIdFormatter.cs` (NEW)

**Purpose:** Value formatter for repository ID attributes

**Implementation Pattern:** Clone of AzdoUserIdFormatter.cs with these changes:
- Replace "user" with "repository" throughout
- Use 🗃️ icon instead of 👤
- Format as `🗃️ DisplayName (GUID)` for matched values in tables

**Key Method:**
- `TryFormat(ServiceResolutionContext context)` - formats repository IDs with icon and display name

**Rationale:** Maintains consistency with existing formatter pattern.

---

### 3. Semantic Icon Formatting Layer

#### File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs`

**Change 1:** Add `TryFormatRepositoryAttribute` method

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

    formatted = FormatIconValue($"🗃️ {value}", context, false);
    return true;
}
```

**Change 2:** Add `TryFormatBranchAttribute` method

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

    formatted = FormatIconValue($"⎇ {value}", context, false);
    return true;
}
```

**Change 3:** Update `TryFormatSemanticValue` to call new formatters

Add after `TryFormatSubscriptionAttribute` check:

```csharp
if (TryFormatRepositoryAttribute(attributeName, value, context, out var repositoryFormatted))
{
    formattedValue = repositoryFormatted;
    return true;
}

if (TryFormatBranchAttribute(attributeName, value, context, out var branchFormatted))
{
    formattedValue = branchFormatted;
    return true;
}
```

**Change 4:** Add plain versions for `FormatAttributeValuePlain`

```csharp
private static bool TryFormatRepositoryAttributePlain(string attributeName, string value, out string formatted)
{
    // Same attribute checks as TryFormatRepositoryAttribute
    formatted = FormatIconValuePlain($"🗃️ {value}");
    return true;
}

private static bool TryFormatBranchAttributePlain(string attributeName, string value, out string formatted)
{
    // Same attribute checks as TryFormatBranchAttribute
    formatted = FormatIconValuePlain($"⎇ {value}");
    return true;
}
```

**Rationale:** Icons are applied at the semantic formatting layer to all resources uniformly, regardless of provider. This placement:
- Follows existing pattern for role, subscription, identity icons
- Ensures icons appear in both table and summary contexts
- Works across all providers (not just AzureDevOps)
- Uses established FormatIconValue infrastructure

---

### 4. Composition and Registration Layer

#### File: `src/Oocx.TfPlan2Md/CompositionRoot.cs`

**Change 1:** Add CreateAzdoRepositoryMapper method

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
```

**Change 2:** Update CreateProviderRegistry signature and implementation

Add `AzdoRepositoryMapper azdoRepositoryMapper` parameter and pass to AzureDevOpsModule constructor.

**Rationale:** Follows Feature 085 pattern for mapper instantiation and injection.

---

#### File: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs`

**Change 1:** Add field and constructor parameter

```csharp
/// <summary>
/// Optional mapper for Azure DevOps repository resolution.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
private readonly AzdoRepositoryMapper? _azdoRepositoryMapper;
```

**Change 2:** Register helper in RegisterHelpers

```csharp
if (_azdoRepositoryMapper is not null)
{
    scriptObject.Import("azdo_repository_name", 
        new Func<string, string>(repoId => _azdoRepositoryMapper.GetEntityName(repoId)));
}
```

**Change 3:** Register formatter in RegisterValueFormatters

```csharp
// Register Azure DevOps repository formatter
if (_azdoRepositoryMapper is not null)
{
    var repositoryFormatter = new AzdoRepositoryIdFormatter(_azdoRepositoryMapper);
    // Match repository attribute names with GUID pattern
    registry.Register(
        new MatchPattern(
            "(^azuredevops$|.*/azuredevops$)",
            null,
            "^repo_id$|^repository_id$|^source_repo_id$|^target_repo_id$",
            AzureValueFormatterRegistration.GuidPattern),
        repositoryFormatter);
}
```

**Rationale:** Follows exact registration pattern from Feature 085 for users, groups, and projects.

---

#### File: `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`

**Change:** Add AzdoRepositoryCount property

```csharp
/// <summary>
/// Gets or sets the count of Azure DevOps repository mappings loaded.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
public int AzdoRepositoryCount { get; set; }
```

**Rationale:** Maintains consistency with diagnostic tracking for other azdo entity types.

---

#### File: `src/Oocx.TfPlan2Md/Diagnostics/FailedResolutionType.cs`

**Change:** Add AzdoRepository enum value

```csharp
/// <summary>
/// Azure DevOps repository resolution failure.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
AzdoRepository,
```

**Rationale:** Enables diagnostic tracking of failed repository name resolutions.

---

### 5. Entry Point Updates

#### File: `src/Oocx.TfPlan2Md/ProgramEntry.cs`

**Change:** Wire up AzdoRepositoryMapper in the composition chain

Add mapper creation and pass to CreateProviderRegistry:

```csharp
var azdoRepositoryMapper = compositionRoot.CreateAzdoRepositoryMapper(mappingResult, diagnosticContext);
```

**Rationale:** Completes the dependency injection chain for the new mapper.

---

## Data Flow

### Repository Mapping Resolution Flow

1. **Load Phase** (`AzureMappingFileParser`)
   - Parse `azdoRepositories` JSON section from mapping file
   - Create FrozenDictionary<string, string> with case-insensitive GUID lookups
   - Store in AzureMappingFileResult

2. **Composition Phase** (`CompositionRoot`)
   - Create AzdoRepositoryMapper with FrozenDictionary from mapping result
   - Inject mapper into AzureDevOpsModule
   - Create AzdoRepositoryIdFormatter with mapper reference
   - Register formatter in ValueFormatterRegistry with pattern `^repo_id$|^repository_id$|...`

3. **Rendering Phase** (`MarkdownRenderer` → `ResourceSummaryBuilder`)
   - For each resource attribute, check ValueFormatterRegistry for matches
   - When repository attribute detected, call AzdoRepositoryIdFormatter.TryFormat()
   - Formatter calls AzdoRepositoryMapper.GetName() to resolve display name
   - Return formatted value: `🗃️ DisplayName (GUID)` or null if not mapped

4. **Template Phase** (Scriban templates)
   - Templates can call `azdo_repository_name(repo_id)` helper
   - Helper calls AzdoRepositoryMapper.GetEntityName() which returns `🗃️ DisplayName [GUID]` or `🗃️ GUID`

### Icon Application Flow

1. **Semantic Formatting Phase** (`SemanticFormatting.Identity.cs`)
   - During attribute value formatting, call TryFormatRepositoryAttribute for repository attributes
   - Call TryFormatBranchAttribute for branch/ref attributes
   - Apply icon prefix using FormatIconValue helper
   - Use non-breaking space between icon and value

2. **Context-Aware Rendering**
   - Table context: wrap in markdown code spans (backticks)
   - Summary context: wrap in HTML code elements
   - Plain context: icon with non-breaking space only

## Integration Points

### With Feature 085 Components

- **Reuses:** Same mapper pattern, same formatter pattern, same registration infrastructure
- **Extends:** PrincipalMappingFile, AzureMappingFileResult, AzureMappingFileParser, AzureDevOpsModule
- **Parallels:** Creates AzdoRepositoryMapper alongside AzdoUserMapper, AzdoGroupMapper, AzdoProjectMapper

### With Existing Icon System

- **Integrates:** SemanticFormatting.Identity.cs (where all identity/principal icons live)
- **Reuses:** FormatIconValue, FormatIconValuePlain, EnsureNonBreakingIconSpacing helpers
- **Pattern:** Same TryFormat* method pattern as TryFormatPrincipalType, TryFormatRoleDefinition, TryFormatIdentityAttribute

## Design Rationale

### Why Not Create a Separate File for Repository/Branch Icons?

**Decision:** Add to `SemanticFormatting.Identity.cs` rather than create `SemanticFormatting.AzureDevOps.cs`

**Rationale:**
- Repositories and branches are identity-adjacent concepts (they identify resources)
- Feature 085 added Azure DevOps principal icons to Identity.cs (consistent precedent)
- Keeps all entity/identity icons in one discoverable location
- Avoids file proliferation for small additions
- Only 2 new TryFormat methods (not enough to warrant separate file)

If future features add many more AzureDevOps-specific icons (e.g., pipelines, service connections, work items), refactoring to a separate file would be appropriate.

### Why StringComparer.Ordinal for Repositories?

**Decision:** Use `StringComparer.Ordinal` for repository GUID lookups

**Rationale:**
- Repositories are identified by GUIDs (like users and projects)
- GUIDs are case-insensitive by nature
- However, Feature 085 used `StringComparer.Ordinal` for AzdoUsers and AzdoProjects
- **Correction needed:** Should use `StringComparer.OrdinalIgnoreCase` for GUIDs to handle case variations
- Group descriptors use `StringComparer.Ordinal` because they're base64-encoded strings (case matters)

**Implementation note:** Use `StringComparer.OrdinalIgnoreCase` for consistency with GUID semantics, even though Feature 085 used Ordinal for users/projects. This is a minor improvement over Feature 085.

### Display Format Consistency

**For ValueFormatters (table context):**
- Format: `🗃️ DisplayName (GUID)` when mapped
- Format: (null/skip) when not mapped - falls back to semantic icon formatting
- Wraps in markdown code spans for tables
- **Pattern:** Same as AzdoUserIdFormatter

**For GetEntityName (template helpers):**
- Format: `🗃️ DisplayName [GUID]` when mapped
- Format: `🗃️ GUID` when not mapped
- No wrapping (templates apply their own wrapping)
- **Pattern:** Same as all other Get*Name helpers

**For Semantic Icons (all contexts):**
- Format: `🗃️ value` for repository attributes (whether or not mapped)
- Format: `⎇ value` for branch attributes (never mapped)
- Applies when ValueFormatter doesn't match or isn't registered

## Testing Considerations

### Unit Tests Required

1. **AzureMappingFileParser** (extend existing tests)
   - Parse azdoRepositories section successfully
   - Handle missing azdoRepositories section gracefully
   - Handle empty azdoRepositories object
   - Handle null azdoRepositories value

2. **AzdoRepositoryMapper** (new test class)
   - GetName returns display name when mapped
   - GetName returns null when not mapped
   - GetName records diagnostic failure when enabled
   - GetEntityName formats as `DisplayName [GUID]` when mapped
   - GetEntityName formats as GUID when not mapped
   - GetEntityName includes 🗃️ icon in output

3. **AzdoRepositoryIdFormatter** (new test class)
   - TryFormat returns formatted value with icon when mapped
   - TryFormat returns null when not mapped
   - Formatted output includes code wrapping

4. **SemanticFormatting** (extend existing tests)
   - TryFormatRepositoryAttribute matches all repository attribute names
   - TryFormatRepositoryAttribute returns false for non-repository attributes
   - TryFormatBranchAttribute matches all branch attribute names
   - TryFormatBranchAttribute returns false for non-branch attributes
   - Icons render correctly in table and summary contexts

### Integration Tests Required

1. **End-to-End Repository Mapping**
   - Load mapping file with azdoRepositories section
   - Render resource with repository_id attribute
   - Verify display name appears in output
   - Verify icon appears

2. **End-to-End Branch Icons**
   - Render resource with branch_name attribute
   - Verify ⎇ icon appears
   - Verify no mapping occurs (branches aren't mapped)

3. **Diagnostic Output**
   - Verify --debug shows AzdoRepositoryCount
   - Verify --debug shows failed repository resolutions

## Implementation Sequence

Recommended order to minimize merge conflicts and enable incremental testing:

1. **Data Model Layer** (can be parallel)
   - Update PrincipalMappingFile
   - Update AzureMappingFileResult
   - Update AzureMappingFileParser
   - Update DiagnosticContext and FailedResolutionType

2. **Mapper Layer**
   - Create AzdoRepositoryMapper (copy-modify from AzdoUserMapper)
   - Create AzdoRepositoryIdFormatter (copy-modify from AzdoUserIdFormatter)
   - Add unit tests for both

3. **Registration Layer**
   - Update CompositionRoot
   - Update AzureDevOpsModule
   - Update ProgramEntry

4. **Icon Layer**
   - Add TryFormatRepositoryAttribute to SemanticFormatting.Identity.cs
   - Add TryFormatBranchAttribute to SemanticFormatting.Identity.cs
   - Update TryFormatSemanticValue and FormatAttributeValuePlain
   - Add unit tests

5. **Integration Testing**
   - Add end-to-end tests
   - Update example mapping files
   - Test with real Terraform plans

## Consequences

### Positive

- **Consistency:** Follows Feature 085 pattern exactly - easy to understand and maintain
- **Reusability:** All infrastructure from Feature 085 is reused (no new abstractions needed)
- **Extensibility:** Pattern proven to work; can add more azdo entity types easily
- **Readability:** Icons and display names significantly improve report clarity
- **Discoverability:** Icons help users quickly scan for repositories and branches in plans

### Negative

- **Slight code duplication:** AzdoRepositoryMapper is nearly identical to AzdoUserMapper
  - **Mitigation:** Acceptable - mapper classes are small, and abstraction would add complexity
- **Icon choice limitations:** 🗃️ and ⎇ are fixed (not configurable)
  - **Mitigation:** Consistent with existing icon system; can make configurable in future if needed
- **Case sensitivity correction:** Need to use OrdinalIgnoreCase instead of Ordinal for GUIDs
  - **Impact:** Minor improvement over Feature 085; should update users/projects too in follow-up

### Risks

- **Attribute name coverage:** The specified attribute names may not cover all repository/branch attributes in Azure DevOps provider
  - **Mitigation:** Start with specified list, extend if gaps found during testing
- **Performance:** Additional TryFormat checks in hot path
  - **Mitigation:** Early-exit pattern minimizes overhead; no regex compilation needed

## Open Questions Resolved

### 1. Helper function placement
**Answer:** Create AzdoRepositoryMapper in `Providers/AzureDevOps/` following Feature 085 pattern.

### 2. Icon method placement
**Answer:** Add to `SemanticFormatting.Identity.cs` - repositories/branches are identity-adjacent, and Feature 085 set precedent by adding Azure DevOps principal icons there.

### 3. Attribute name coverage
**Answer:** Start with specified list (`repo_id`, `repository_id`, `source_repo_id`, `target_repo_id`, `default_branch`, `branch_name`, `ref_name`, `source_branch`, `target_branch`). Extend if gaps found.

### 4. Icon choice
**Answer:** Use 🗃️ (file cabinet U+1F5C3) and ⎇ (branch U+2387) as specified - semantically appropriate and visually distinct.

### 5. Branch mapping
**Answer:** Out of scope - branches are human-readable strings. Only repositories need GUID-to-name mapping.

## Related Documentation

- Feature 085: `docs/features/085-azdo-principal-mapping/` - Primary reference for implementation pattern
- Existing ADRs: `docs/adr-006-dependency-injection.md` - Pure DI composition pattern
- Architecture: `docs/architecture.md` - System architecture and building blocks
- Provider separation: Feature 047 - Provider code organization
- Semantic icons: Feature 024 - Visual report enhancements
