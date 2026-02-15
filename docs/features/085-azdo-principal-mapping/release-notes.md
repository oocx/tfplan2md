# Release Notes: Azure DevOps Principal Mapping

## Overview

tfplan2md now supports mapping Azure DevOps user IDs, group descriptors, and project IDs to human-readable display names using the same principal mapping JSON file as Azure AD entities. This enhancement makes Terraform plans for Azure DevOps resources significantly more readable by replacing cryptic GUIDs and descriptors with recognizable names.

## What's New

### Azure DevOps Entity Mapping

You can now map three types of Azure DevOps entities to display names in your principal mapping file:

- **Users**: Map Azure DevOps user GUIDs to display names (e.g., "John Smith")
- **Groups**: Map Azure DevOps group descriptors to display names (e.g., "Platform Team")
- **Projects**: Map Azure DevOps project GUIDs to display names (e.g., "Infrastructure Project")

### Automatic Resolution in Reports

Azure DevOps entities are automatically resolved throughout your reports:

- **Group memberships**: Member and group names appear in membership tables
- **Team members**: Team member names appear in team resource tables
- **Project references**: Project names replace GUIDs wherever they appear

### Unified Mapping Experience

Azure DevOps mappings integrate seamlessly with the existing principal mapping infrastructure:

- **Same mapping file**: Add `azdoUsers`, `azdoGroups`, and `azdoProjects` sections to your existing principals.json
- **Same CLI option**: Use the existing `--principal-mapping` flag
- **Same display format**: Entities display as `DisplayName (ID)` for consistency

## Usage

### Mapping File Format

Add three new optional sections to your principal mapping JSON file:

```json
{
  "users": {
    "00000000-0000-0000-0000-000000000001": "Jane Doe"
  },
  "groups": {
    "00000000-0000-0000-0000-000000000002": "DevOps Team"
  },
  "azdoUsers": {
    "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
    "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
  },
  "azdoGroups": {
    "vssgp.Uy0xLTktMTU1MTM...": "Platform Team",
    "aadgp.Uy0.ReleaseManagers": "Release Managers"
  },
  "azdoProjects": {
    "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project",
    "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": "Application Platform"
  }
}
```

All sections are optional—include only what you need.

### CLI Usage

No changes to the CLI interface. Use the existing `--principal-mapping` option:

```bash
# Without mapping
tfplan2md plan.json

# With Azure DevOps mapping
tfplan2md --principal-mapping principals.json plan.json

# Docker with mapping
docker run -v $(pwd):/data oocx/tfplan2md \
  --principal-mapping /data/principals.json \
  /data/plan.json --output /data/plan.md

# With debug output to see entity counts
tfplan2md --debug --principal-mapping principals.json plan.json
```

### Rendered Output

**Before** (without mapping):
```markdown
#### Members

| Change | Member | Terraform Resource |
| -------- | -------- | -------------------- |
| ➕ | aadgp.Uy0.AliceUser | `azuredevops_group_membership.release_managers_membership_alice` |
| ➕ | aadgp.Uy0.BobUser | `azuredevops_group_membership.release_managers_membership_bob` |
```

**After** (with mapping):
```markdown
#### Members

| Change | Member | Terraform Resource |
| -------- | -------- | -------------------- |
| ➕ | Alice User (aadgp.Uy0.AliceUser) | `azuredevops_group_membership.release_managers_membership_alice` |
| ➕ | Bob Smith (aadgp.Uy0.BobUser) | `azuredevops_group_membership.release_managers_membership_bob` |
```

### Debug Output

When using the `--debug` flag, diagnostic output includes Azure DevOps entity counts:

```markdown
## Debug Information

### Principal Mapping

Principal Mapping: Loaded successfully from 'principals.json'
- Found 45 principals
- Found 3 azdo users, 2 azdo groups, 1 azdo project

Failed to resolve 1 azdo entity:
- User `unknown-guid` (referenced in `azuredevops_group_membership.example`)
```

## Benefits

### Improved Readability

- **Recognize team members**: See actual names instead of GUIDs in membership tables
- **Identify groups quickly**: Know which team or security group is being modified
- **Understand project context**: See project names instead of cryptic IDs

### Faster Reviews

- **Scan membership changes**: Quickly identify who was added or removed
- **Verify permissions**: Confirm correct groups are assigned
- **Audit trail**: Clear documentation of who has access

### Consistent Experience

- **Same pattern as Azure AD**: Azure DevOps mappings work just like Azure AD principal mappings
- **Unified mapping file**: One file for all your Azure AD and Azure DevOps entities
- **Predictable format**: All entities display as `DisplayName (ID)`

## Custom Templates

For users writing custom Scriban templates, three new helper functions are available:

- `azdo_user_name(userId)` → resolves Azure DevOps user ID
- `azdo_group_name(groupDescriptor)` → resolves Azure DevOps group descriptor
- `azdo_project_name(projectId)` → resolves Azure DevOps project ID

These helpers provide explicit control over entity resolution in custom templates, while default rendering automatically resolves entities via value formatters.

## Backwards Compatibility

- **Existing mapping files work unchanged**: Files without azdo sections continue to work
- **No breaking changes**: All existing functionality remains intact
- **Optional feature**: Only use azdo mappings if you need them

## Technical Details

- **Automatic application**: Value formatters automatically resolve Azure DevOps entities in default rendering
- **Provider-specific implementation**: Azure DevOps mapping logic is isolated in the azuredevops provider
- **Diagnostic support**: Failed resolutions are tracked and reported in debug output
- **Full descriptor display**: Group descriptors are displayed in full for copy-paste friendliness

## Migration Guide

If you already have a principal mapping file:

1. **Add azdo sections** to your existing principals.json file (see format above)
2. **No other changes needed** - the CLI usage remains the same
3. **Test with `--debug`** to verify entity counts and catch unmapped IDs

## Related Documentation

- **Feature specification**: [docs/features/085-azdo-principal-mapping/specification.md](specification.md)
- **Architecture**: [docs/features/085-azdo-principal-mapping/architecture.md](architecture.md)
- **Test plan**: [docs/features/085-azdo-principal-mapping/test-plan.md](test-plan.md)
- **User guide**: [README.md](../../../README.md) (principal mapping section)
- **Features list**: [docs/features.md](../../features.md)

## Acknowledgments

This feature follows the established pattern for Azure AD principal mapping and extends it consistently to Azure DevOps entities. It maintains the project's commitment to readable, user-friendly infrastructure reports.
