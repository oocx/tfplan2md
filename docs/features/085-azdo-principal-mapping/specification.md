# Feature: Azure DevOps Principal Mapping

## Overview

Extend the principal mapping file to support Azure DevOps (azdo) entities, allowing users to map Azure DevOps user IDs, group descriptors, and project IDs to human-readable display names. This improves readability of rendered Terraform plans that include Azure DevOps resources, similar to how Azure AD principals are currently mapped.

## User Goals

- **Map Azure DevOps identities to display names**: Users want to see recognizable names (e.g., "John Smith", "Platform Team") instead of cryptic GUIDs and descriptors in their rendered reports
- **Unified mapping experience**: Users want to use the same mapping file for both Azure AD and Azure DevOps entities without learning a different format
- **Improved report readability**: Users want Azure DevOps resources (permissions, group memberships, project assignments) to be as readable as Azure role assignments

## Background

The codebase currently supports principal mapping for:
- **Azure AD entities**: users, groups, service principals (in `azuread` section)
- **Azure platform entities**: subscriptions, management groups, tenants, custom role definitions

Azure DevOps has its own identity model with:
- **Users**: Identified by unique GUIDs (e.g., `4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b`)
- **Groups**: Identified by descriptors (e.g., `vssgp.Uy0xLTktMTU1MTM...`)
- **Projects**: Identified by GUIDs (e.g., `8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f`)

When these entities appear in Terraform plans (e.g., `azuredevops_group_membership`, `azuredevops_project`), they display as IDs, making plans difficult to review.

## Scope

### In Scope

- Extend `PrincipalMappingFile` class to include three new sections:
  - `azdoUsers`: Map Azure DevOps user GUIDs to display names
  - `azdoGroups`: Map Azure DevOps group descriptors to display names  
  - `azdoProjects`: Map Azure DevOps project GUIDs to display names

- Update `AzureMappingFileParser` to parse the new azdo sections

- Create helper functions or Scriban helpers to resolve Azure DevOps entity names during template rendering

- Display mapped names in the format: `DisplayName [ID]` (consistent with existing Azure AD mapping behavior)

- Update documentation and examples to show the new azdo mapping sections

### Out of Scope

- Mapping Azure DevOps teams, service connections, or other entity types (can be added in future features)
- Automatic discovery/generation of mapping files from Azure DevOps APIs
- Type metadata tracking for azdo entities (no equivalent to the `principalTypes` tracking used for Azure AD)
- Changes to the CLI interface (continues to use the existing `--principal-mapping` option)
- Separate mapper classes for Azure DevOps (reuse existing infrastructure where possible)

## User Experience

### Mapping File Format

Users will add three new optional sections to their principal mapping JSON file:

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
  "subscriptions": [
    { "id": "sub-123", "displayName": "Production" }
  ],
  "tenants": [
    { "id": "mg-root", "displayName": "Contoso Corp" }
  ]
}
```

All sections remain optional - users can include only the sections they need.

### Rendered Output

**Before** (without mapping):
```
azuredevops_group_membership main-member
  Group: vssgp.Uy0xLTktMTU1MTM...
  Member: 4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b
```

**After** (with mapping):
```
azuredevops_group_membership main-member
  Group: Platform Team [vssgp.Uy0xLTktMTU1MTM...]
  Member: John Smith [4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b]
```

### CLI Usage

No changes to CLI - users continue using the existing option:

```bash
tfplan2md plan.json --principal-mapping mappings.json
```

### Diagnostic Output

When `--debug` is enabled, diagnostic output should show:
- Count of mapped azdoUsers
- Count of mapped azdoGroups  
- Count of mapped azdoProjects
- Failed resolution attempts for azdo entities

## Success Criteria

- [ ] `PrincipalMappingFile` class includes `AzdoUsers`, `AzdoGroups`, and `AzdoProjects` properties
- [ ] `AzureMappingFileParser` correctly parses azdo sections from JSON
- [ ] Azdo entity names are resolved and displayed in rendered output
- [ ] Display format matches existing pattern: `DisplayName [ID]`
- [ ] Empty/null azdo sections are handled gracefully
- [ ] Diagnostic output includes azdo entity counts when `--debug` is used
- [ ] Example mapping file demonstrates azdo sections
- [ ] Documentation updated to describe azdo mapping sections
- [ ] Tests verify azdo mapping parsing and rendering
- [ ] Backwards compatibility maintained - existing mapping files without azdo sections continue to work

## Open Questions

1. **Naming convention**: Should we use `azdoUsers/azdoGroups/azdoProjects` or `azureDevOpsUsers/azureDevOpsGroups/azureDevOpsProjects`?
   - Recommendation: Use `azdoUsers/azdoGroups/azdoProjects` for brevity and consistency with common abbreviations

2. **Resolution helper placement**: Should azdo resolution helpers be:
   - Added to the existing Azure ScribanHelpers class?
   - Created as a new AzureDevOps-specific ScribanHelpers class?
   - Integrated into a shared entity resolution service?
   - Recommendation: Start with the existing Azure ScribanHelpers (or create azdo-specific helpers if Azure helpers are tightly coupled to Azure AD)

3. **Mapper reuse**: Should we:
   - Reuse the existing `PrincipalMapper` class for azdo entities?
   - Create a separate `AzdoEntityMapper` class?
   - Recommendation: Assess coupling in implementation phase - if `PrincipalMapper` is Azure AD-specific, create a parallel mapper; if it's generic, reuse it

4. **Type tracking**: Azure AD mappings track principal type metadata (User, Group, ServicePrincipal). Should azdo mappings track entity types?
   - Recommendation: Not in initial implementation (marked as out of scope) - can be added later if needed

5. **Group descriptor format**: Azure DevOps group descriptors can be very long. Should we:
   - Display full descriptor in `[...]` brackets as with other IDs?
   - Truncate or abbreviate the descriptor?
   - Recommendation: Display full descriptor for consistency and to avoid ambiguity

## Related Documentation

- Existing principal mapping: `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMappingFile.cs`
- Mapping file parser: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileParser.cs`
- Principal mapper: `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMapper.cs`
- Azure Scriban helpers: `src/Oocx.TfPlan2Md/Platforms/Azure/ScribanHelpers.Azure.cs`
- Example mapping: `examples/comprehensive-demo/demo-principals-nested.json`
