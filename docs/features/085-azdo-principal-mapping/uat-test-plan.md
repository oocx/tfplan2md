# UAT Test Plan: Azure DevOps Principal Mapping

## Overview

This UAT validates the Azure DevOps principal mapping feature by demonstrating how Azure DevOps user IDs, group descriptors, and project IDs are resolved to human-readable display names in rendered Terraform plan reports.

## Test Artifacts

### Generated Artifacts (Real tfplan2md Output)

- **`uat-plan.md`** - Real output from tfplan2md CLI tool showing principal mapping in action
- **`uat-plan.json`** - Terraform plan JSON with Azure DevOps resources (test data)
- **`uat-mapping.json`** - Principal mapping file with azdo sections

### How the Artifact Was Generated

**CRITICAL**: This artifact was generated using the **actual tfplan2md CLI tool**, not handwritten markdown.

```bash
# Build the tool
cd src/Oocx.TfPlan2Md
dotnet build --configuration Release

# Generate real output
dotnet bin/Release/net10.0/tfplan2md.dll \
  uat-plan.json \
  --principal-mapping uat-mapping.json \
  --output uat-plan.md
```

## Test Data

### Terraform Plan (`uat-plan.json`)

The test plan includes:
- 2 **azuredevops_project** resources with project IDs
- 2 **azuredevops_group** resources with group descriptors
- 3 **azuredevops_group_membership** resources with user IDs
- 1 **azuredevops_team** resource with project reference
- 1 **azuredevops_team_members** resource with user IDs array
- 1 **azuredevops_team_administrators** resource with user IDs array

### Principal Mapping File (`uat-mapping.json`)

```json
{
  "azdoUsers": {
    "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "Alice Smith",
    "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Bob Johnson",
    "9d8e7f6c-5b4a-3c2d-1e0f-9a8b7c6d5e4f": "Charlie Williams"
  },
  "azdoGroups": {
    "vssgp.Uy0xLTktMTU1MTM3MjIxNy0zNzYzMTkyMTY3LTI3NDgzNjU3MTYtMjE3MDg3MzYzMy0yNzA1Nzk5MDA2LTEtMjgyODQxODkxNS0zMzY4NDc0NzY0LTIxNDc0ODM2NDgtMTI1MTM1ODA3OQ": "Platform Engineering Team",
    "vssgp.Uy0yLTktMTY2MjQ4MzIyOC00ODc2MjkzMzg4LTM4NTk0NzYyOTctMzI4MTk2NDc0NC0zODE2OTAwMTE3LTItMzkzOTUwODAyNi00NDk1ODU4ODc1LTMyNTk1OTczNzktMjM2MTQ2OTA4OA": "Security Review Team"
  },
  "azdoProjects": {
    "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project",
    "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": "Application Platform"
  }
}
```

## Validation Steps

When reviewing the UAT artifact (`uat-plan.md`), verify:

### 1. Azure DevOps Users Are Resolved

**Expected**: User GUIDs should show as `DisplayName (ID)`

**Example from line 58**:
```
| ➕ | `Alice Smith (4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b)` | ...
```

✅ **Pass Criteria**: All user IDs in group membership tables show resolved display names

### 2. Azure DevOps Groups Are Resolved

**Expected**: Group descriptors (even very long ones) should be preserved

**Example from line 68**:
```
vssgp.Uy0xLTktMTU1MTM3MjIxNy0zNzYzMTkyMTY3LTI3NDgzNjU3MTYtMjE3MDg3MzYzMy0yNzA1Nzk5MDA2LTEtMjgyODQxODkxNS0zMzY4NDc0NzY0LTIxNDc0ODM2NDgtMTI1MTM1ODA3OQ
```

✅ **Pass Criteria**: Group descriptors are shown in full (not truncated)

### 3. Azure DevOps Projects Are Resolved

**Expected**: Project IDs should show as `DisplayName (ID)`

**Example from line 113**:
```
| project_id | `Infrastructure Project (8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f)` |
```

✅ **Pass Criteria**: Project references show resolved display names with IDs in parentheses

### 4. Output Format Is Consistent

**Expected**: All resolved entities use the format `DisplayName (ID)`

✅ **Pass Criteria**: 
- Users: `Alice Smith (4a2c5e2b-...)`
- Groups: Full descriptor preserved
- Projects: `Infrastructure Project (8f7e6d5c-...)`

### 5. Rendering Is Real tfplan2md Output

**Expected**: The artifact must be authentic tfplan2md CLI output

✅ **Pass Criteria**:
- Contains standard tfplan2md header with version and timestamp
- Has proper summary table with resource counts
- Uses collapsible `<details>` sections
- Shows resource changes in standard format
- NOT handwritten or synthetic markdown

## Known Limitations

1. **Team members/administrators arrays**: The `azuredevops_team_members` and `azuredevops_team_administrators` resources show arrays instead of resolved individual names (lines 119, 125). This is expected because these are arrays at the Terraform resource level, not individual parent-child relationships like group memberships.

2. **Future enhancement**: Individual member resolution in team arrays could be added through custom templates or enhanced value formatters.

## UAT Execution

### Create UAT PRs

```bash
scripts/uat-run.sh \
  docs/features/085-azdo-principal-mapping/uat-plan.md \
  "Validate Azure DevOps principal mapping: users, groups, and projects are resolved to display names" \
  --create-only
```

### Add Comprehensive Demo as Regression Test

After creating the UAT PRs, add the comprehensive demo as a second comment:

```bash
# Extract PR numbers from state
gh_pr=$(jq -r '.github.pr // ""' .tmp/uat-run/last-run.json)
azdo_pr=$(jq -r '.azdo.pr // ""' .tmp/uat-run/last-run.json)

# Post comprehensive demo to GitHub
scripts/uat-github.sh comment "$gh_pr" artifacts/comprehensive-demo-simple-diff.md

# Post comprehensive demo to Azure DevOps
scripts/uat-azdo.sh comment "$azdo_pr" artifacts/comprehensive-demo.md
```

### Approve and Clean Up

After Maintainer approval:

```bash
scripts/uat-run.sh --cleanup-last
```

## Success Criteria

- ✅ UAT artifact is **real tfplan2md output** (not synthetic)
- ✅ Users are resolved: `Alice Smith (4a2c5e2b-...)`
- ✅ Groups are resolved with full descriptors
- ✅ Projects are resolved: `Infrastructure Project (8f7e6d5c-...)`
- ✅ Format is consistent: `DisplayName (ID)`
- ✅ GitHub and Azure DevOps render the markdown correctly
- ✅ Feature improves readability compared to raw IDs
