# Feature: Azure DevOps Repository Mapping and Branch/Repo Icons

## Overview

Extend the principal mapping file to support Azure DevOps repository mappings and add semantic icons for repository and branch/ref attributes. This builds upon feature 085 (Azure DevOps Principal Mapping) by adding an `azdoRepositories` section to map repository GUIDs to human-readable names, and introduces two new semantic icons (🗃️ for repositories, ⎇ for branches/refs) to improve readability of Terraform plans containing Azure DevOps resources.

## User Goals

- **Map repository IDs to display names**: Users want to see recognizable repository names (e.g., "Infrastructure Repo") instead of cryptic GUIDs in their rendered reports
- **Unified mapping experience**: Users want to use the same mapping file for Azure AD principals, Azure DevOps principals (users, groups, projects), and now Azure DevOps repositories without learning a different format
- **Visual distinction for repositories and branches**: Users want repositories and branches/refs to be visually distinguishable with semantic icons, making it easier to scan plans at a glance
- **Improved report readability**: Users want Azure DevOps build definitions, git repositories, and other resources referencing repositories or branches to be as readable as other mapped entities

## Background

The codebase currently supports principal mapping for:
- **Azure AD entities**: users, groups, service principals (in `azuread` section)
- **Azure platform entities**: subscriptions, management groups, tenants, custom role definitions
- **Azure DevOps entities** (feature 085): users, groups, projects (in `azdoUsers`, `azdoGroups`, `azdoProjects` sections)

Azure DevOps resources frequently reference:
- **Repositories**: Identified by GUIDs (e.g., `8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f`)
- **Branches and refs**: String values like `refs/heads/main`, `main`, or `develop`

When these appear in Terraform plans (e.g., `azuredevops_build_definition`, `azuredevops_git_repository`, `azuredevops_branch_policy_*`), repository IDs display as GUIDs and branch/ref attributes lack visual distinction, making plans harder to review.

Additionally, semantic icons are used throughout the codebase to improve visual parsing (e.g., 👤 for users, 👥 for groups, 🛡️ for roles). Repositories and branches/refs currently lack semantic icons.

## Scope

### In Scope

- Extend `PrincipalMappingFile` class to include a new `azdoRepositories` section
- Map Azure DevOps repository GUIDs to display names
- Update `AzureMappingFileParser` to parse the `azdoRepositories` section
- Display mapped repository names in the format: `🗃️ RepoName [GUID]` (icon + DisplayName + [ID])
- Display unmapped repository IDs with icon only: `🗃️ <GUID>`
- Add semantic icon 🗃️ (file cabinet) for repository-related attributes:
  - `repo_id`
  - `repository_id`
  - `source_repo_id`
  - `target_repo_id`
- Add semantic icon ⎇ (branch symbol) for branch/ref-related attributes:
  - `default_branch`
  - `branch_name`
  - `ref_name`
  - `source_branch`
  - `target_branch`
- Apply icons to all Azure DevOps resources that use these attributes
- Update documentation and examples to show the new `azdoRepositories` section and icon usage

### Out of Scope

- Mapping other Azure DevOps entity types not yet supported (teams, service connections, pipelines, etc.)
- Automatic discovery/generation of mapping files from Azure DevOps APIs
- Changes to the CLI interface (continues to use the existing `--principal-mapping` option)
- Icon customization or configuration (icons are fixed for their respective attribute types)
- Mapping branch names to display names (branches are descriptive strings, not GUIDs)
- Conditional icon application based on resource type (icons apply uniformly to matching attribute names)

## User Experience

### Mapping File Format

Users will add a new optional `azdoRepositories` section to their principal mapping JSON file:

```json
{
  "users": {
    "00000000-0000-0000-0000-000000000001": "Jane Doe"
  },
  "groups": {
    "00000000-0000-0000-0000-000000000002": "DevOps Team"
  },
  "servicePrincipals": {
    "00000000-0000-0000-0000-000000000003": "Deployment Pipeline"
  },
  "azdoUsers": {
    "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
    "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
  },
  "azdoGroups": {
    "vssgp.Uy0xLTktMTU1MTM...": "Platform Team",
    "vssgp.Uy0yLTktMTY2MjQ...": "Security Reviewers"
  },
  "azdoProjects": {
    "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project",
    "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": "Application Platform"
  },
  "azdoRepositories": {
    "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo",
    "f9e8d7c6-b5a4-3210-fedc-ba9876543210": "Web Application Repo",
    "01234567-89ab-cdef-0123-456789abcdef": "Shared Libraries"
  },
  "subscriptions": [
    { "id": "sub-123", "displayName": "Production" }
  ],
  "tenants": [
    { "id": "mg-root", "displayName": "Contoso Corp" }
  ]
}
```

All sections remain optional - users can include only the sections they need.

### Rendered Output Examples

#### Example 1: Repository Mapping

**Before** (without mapping):
```
azuredevops_build_definition ci-pipeline
  Repository ID: a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d
```

**After** (with mapping):
```
azuredevops_build_definition ci-pipeline
  Repository ID: 🗃️ Infrastructure Repo [a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d]
```

**After** (with icon, no mapping):
```
azuredevops_build_definition ci-pipeline
  Repository ID: 🗃️ a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d
```

#### Example 2: Branch Icons

**Before** (without icons):
```
azuredevops_git_repository main-repo
  Default Branch: refs/heads/main
```

**After** (with icons):
```
azuredevops_git_repository main-repo
  Default Branch: ⎇ refs/heads/main
```

#### Example 3: Combined Repository and Branch

**Before**:
```
azuredevops_branch_policy_min_reviewers main-policy
  Repository ID: a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d
  Branch Name: refs/heads/main
```

**After** (with mapping and icons):
```
azuredevops_branch_policy_min_reviewers main-policy
  Repository ID: 🗃️ Infrastructure Repo [a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d]
  Branch Name: ⎇ refs/heads/main
```

### CLI Usage

No changes to CLI - users continue using the existing option:

```bash
tfplan2md plan.json --principal-mapping mappings.json
```

### Diagnostic Output

When `--debug` is enabled, diagnostic output should show:
- Count of mapped azdoRepositories
- Failed resolution attempts for repository IDs
- (Existing diagnostic output for azdoUsers, azdoGroups, azdoProjects remains unchanged)

## Functional Requirements

### FR-1: Repository Mapping Data Model

- The `PrincipalMappingFile` class SHALL include an `AzdoRepositories` property of type `Dictionary<string, string>`
- The dictionary key SHALL be the repository GUID
- The dictionary value SHALL be the human-readable repository display name
- The property SHALL be nullable/optional to maintain backwards compatibility

### FR-2: Repository Mapping Parsing

- The `AzureMappingFileParser` SHALL parse an `azdoRepositories` JSON property if present
- Parsing SHALL handle empty or missing `azdoRepositories` sections gracefully
- Parsing errors in the `azdoRepositories` section SHALL be reported consistently with other mapping sections

### FR-3: Repository Name Resolution

- A helper function SHALL resolve repository GUIDs to display names using the `azdoRepositories` mapping
- When a repository ID is mapped, the output SHALL be formatted as: `🗃️ DisplayName [GUID]`
- When a repository ID is NOT mapped, the output SHALL be formatted as: `🗃️ GUID` (icon with original GUID)
- The helper SHALL be callable from Scriban templates during rendering

### FR-4: Repository Icon Application

- The semantic formatting system SHALL apply the 🗃️ icon to the following attribute names (exact match, case-insensitive):
  - `repo_id`
  - `repository_id`
  - `source_repo_id`
  - `target_repo_id`
- Icon application SHALL occur in the `TryFormat*` methods within the semantic formatting helpers
- Icons SHALL be applied consistently in both table and summary rendering contexts

### FR-5: Branch/Ref Icon Application

- The semantic formatting system SHALL apply the ⎇ icon to the following attribute names (exact match, case-insensitive):
  - `default_branch`
  - `branch_name`
  - `ref_name`
  - `source_branch`
  - `target_branch`
- Icon application SHALL occur in the `TryFormat*` methods within the semantic formatting helpers
- Icons SHALL be applied consistently in both table and summary rendering contexts
- Branch/ref values SHALL NOT be mapped (they are already human-readable strings)

### FR-6: Integration with Existing Icon System

- Repository and branch icons SHALL follow the same formatting patterns as existing semantic icons
- Non-breaking spaces SHALL be used between icons and values (consistent with existing icon formatting)
- Icons SHALL be rendered in code spans for table contexts and HTML code elements for summary contexts

## Non-Functional Requirements

### NFR-1: Backwards Compatibility

- Existing mapping files without `azdoRepositories` SHALL continue to work without errors
- The absence of the `azdoRepositories` section SHALL NOT affect other mapping sections
- Existing tests and rendering behavior for non-repository attributes SHALL remain unchanged

### NFR-2: Consistency

- Repository mapping SHALL follow the same pattern as feature 085's `azdoUsers`, `azdoGroups`, and `azdoProjects` mappings
- Display format SHALL be consistent: `Icon DisplayName [ID]` for mapped values, `Icon ID` for unmapped values
- Icon spacing and formatting SHALL match existing semantic icon conventions

### NFR-3: Performance

- Repository name resolution SHALL NOT introduce noticeable performance degradation
- Icon application SHALL reuse existing formatting infrastructure without duplication

### NFR-4: Testability

- Repository mapping parsing SHALL be testable via unit tests
- Repository name resolution SHALL be testable via unit tests
- Icon application SHALL be verifiable via integration tests or snapshot tests

## User Stories

### US-1: Map Repository IDs
**As a** DevOps engineer  
**I want to** map Azure DevOps repository GUIDs to repository names in my mapping file  
**So that** I can recognize which repositories are referenced in my Terraform plans

**Acceptance Criteria:**
- I can add an `azdoRepositories` section to my mapping JSON file
- I can map repository GUIDs to display names (e.g., `"a1b2c3d4-...": "Infrastructure Repo"`)
- Mapped repository IDs appear as `🗃️ RepoName [GUID]` in rendered output

### US-2: Visual Repository Identification
**As a** report reviewer  
**I want to** see a 🗃️ icon next to repository attributes  
**So that** I can quickly identify repository references when scanning plans

**Acceptance Criteria:**
- Repository-related attributes (`repo_id`, `repository_id`, etc.) display with a 🗃️ icon
- The icon appears whether or not the repository ID is mapped
- The icon is consistently positioned before the value

### US-3: Visual Branch/Ref Identification
**As a** report reviewer  
**I want to** see a ⎇ icon next to branch and ref attributes  
**So that** I can quickly identify branch references when scanning plans

**Acceptance Criteria:**
- Branch/ref attributes (`default_branch`, `branch_name`, etc.) display with a ⎇ icon
- The icon appears consistently in all rendering contexts
- Branch names remain unchanged (no mapping, just icon addition)

### US-4: Backwards Compatibility
**As a** existing user  
**I want to** continue using my mapping file without adding `azdoRepositories`  
**So that** I don't have to update my configuration immediately

**Acceptance Criteria:**
- Mapping files without `azdoRepositories` continue to work
- No errors or warnings are generated for missing `azdoRepositories` section
- Other mapping sections function normally

## Success Criteria

- [ ] `PrincipalMappingFile` class includes `AzdoRepositories` property
- [ ] `AzureMappingFileParser` correctly parses `azdoRepositories` section from JSON
- [ ] Repository GUIDs are resolved to display names using the mapping
- [ ] Display format for mapped repositories matches pattern: `🗃️ DisplayName [GUID]`
- [ ] Display format for unmapped repositories matches pattern: `🗃️ GUID`
- [ ] 🗃️ icon is applied to: `repo_id`, `repository_id`, `source_repo_id`, `target_repo_id`
- [ ] ⎇ icon is applied to: `default_branch`, `branch_name`, `ref_name`, `source_branch`, `target_branch`
- [ ] Icons use non-breaking spaces consistent with existing icon formatting
- [ ] Icons render correctly in both table and summary contexts
- [ ] Empty/null `azdoRepositories` section is handled gracefully
- [ ] Diagnostic output includes repository mapping counts when `--debug` is used
- [ ] Example mapping file demonstrates `azdoRepositories` section
- [ ] Documentation updated to describe repository mapping and new icons
- [ ] Tests verify repository mapping parsing, resolution, and icon application
- [ ] Backwards compatibility maintained - existing mapping files without `azdoRepositories` continue to work

## Open Questions

1. **Helper function placement**: Should repository resolution helpers be:
   - Added to the existing Azure ScribanHelpers class?
   - Created as AzureDevOps-specific ScribanHelpers?
   - Integrated into a shared entity resolution service?
   - **Recommendation**: Follow the same approach used in feature 085 for `azdoUsers`, `azdoGroups`, and `azdoProjects` to maintain consistency

2. **Icon method placement**: Should the new `TryFormatRepository` and `TryFormatBranchRef` methods be:
   - Added to `SemanticFormatting.Identity.cs` (alongside other identity/principal formatters)?
   - Added to a new `SemanticFormatting.AzureDevOps.cs` file?
   - Added to `SemanticFormatting.cs` main file?
   - **Recommendation**: Add to `SemanticFormatting.Identity.cs` since repositories and branches are identity-adjacent concepts, or create `SemanticFormatting.AzureDevOps.cs` if significant additional Azure DevOps formatting is expected

3. **Attribute name coverage**: The specified attribute names (`repo_id`, `repository_id`, `source_repo_id`, `target_repo_id`, `default_branch`, `branch_name`, `ref_name`, `source_branch`, `target_branch`) are based on analysis of common Azure DevOps resources. Should we:
   - Verify these cover all repository/branch attributes in Azure DevOps provider?
   - Add additional attribute names discovered during implementation?
   - **Recommendation**: Start with the specified list (based on current analysis) and extend if needed during implementation or future features

4. **Icon choice**: Are 🗃️ (file cabinet) and ⎇ (branch) the appropriate icons?
   - Alternatives for repository: 📦 (package), 📚 (books), 🗄️ (file cabinet variant)
   - Alternatives for branch: 🌿 (herb), 🎋 (tanabata tree), 🌳 (tree)
   - **Recommendation**: Use 🗃️ and ⎇ as specified; they are semantically appropriate and visually distinct from existing icons

5. **Branch mapping**: Should we support mapping branch names to display names (e.g., `"refs/heads/main": "Main Branch"`)?
   - **Recommendation**: Out of scope for this feature; branch names are already human-readable strings unlike GUIDs. Can be added in a future feature if user demand exists.

## Related Documentation

- Feature 085: `docs/features/085-azdo-principal-mapping/specification.md` - Template for this feature
- Existing principal mapping: `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMappingFile.cs`
- Mapping file parser: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileParser.cs`
- Principal mapper: `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMapper.cs`
- Semantic formatting (identity): `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs`
- Semantic formatting (main): `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.cs`
- Example mapping: `examples/comprehensive-demo/demo-principals-nested.json`
