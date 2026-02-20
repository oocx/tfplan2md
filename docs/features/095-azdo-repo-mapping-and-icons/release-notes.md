# Azure DevOps Repository Mapping and Branch/Repo Icons

This release adds repository mapping support and semantic icons for Azure DevOps repositories and branches/refs, making Terraform plans for Azure DevOps resources more readable.

## ✨ Features

- **Repository mapping**: Map Azure DevOps repository GUIDs to human-readable display names using a new `azdoRepositories` section in the principal mapping file
- **Repository icon (🗃️)**: Automatically applied to repository-related attributes (`repo_id`, `repository_id`, `source_repo_id`, `target_repo_id`)
- **Branch icon (⎇)**: Automatically applied to branch/ref attributes (`default_branch`, `branch_name`, `ref_name`, `source_branch`, `target_branch`)
- **Consistent display format**: Mapped repositories display as `🗃️ DisplayName [GUID]`, unmapped as `🗃️ GUID`
- **Unified mapping file**: Uses the same principals.json file as Azure AD and Azure DevOps principal mappings
- **Diagnostic support**: Debug output shows repository mapping counts and failed resolutions

## 💡 Use Cases

- **Repository identification**: See repository names instead of cryptic GUIDs in build definitions, branch policies, and git repository resources
- **Visual scanning**: Quickly identify repository and branch references with semantic icons when reviewing Terraform plans
- **Consistent mapping experience**: Manage all Azure DevOps entities (users, groups, projects, repositories) in a single mapping file

## 📋 Mapping File Format

Add a new optional `azdoRepositories` section to your principal mapping JSON file:

```json
{
  "users": {
    "00000000-0000-0000-0000-000000000001": "Jane Doe"
  },
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
    "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo",
    "f9e8d7c6-b5a4-3210-fedc-ba9876543210": "Web Application Repo",
    "01234567-89ab-cdef-0123-456789abcdef": "Shared Libraries"
  }
}
```

All sections are optional—include only what you need.

## ▶️ Example Output

Repository and branch attributes in Azure DevOps resources now display with semantic icons:

![Azure DevOps build definition repository table showing 🗃️ icon on Repo ID and ⎇ icon on Branch](https://raw.githubusercontent.com/oocx/tfplan2md/v1.25.0/docs/features/095-azdo-repo-mapping-and-icons/repo-icons.png)

### Repository Mapping

**Before** (without mapping):
```markdown
azuredevops_build_definition ci-pipeline
  Repository ID: a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d
```

**After** (with mapping):
```markdown
azuredevops_build_definition ci-pipeline
  Repository ID: 🗃️ Infrastructure Repo [a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d]
```

**After** (with icon, no mapping):
```markdown
azuredevops_build_definition ci-pipeline
  Repository ID: 🗃️ a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d
```

### Branch Icons

**Before** (without icons):
```markdown
azuredevops_git_repository main-repo
  Default Branch: refs/heads/main
```

**After** (with icons):
```markdown
azuredevops_git_repository main-repo
  Default Branch: ⎇ refs/heads/main
```

### Combined Example

**Before**:
```markdown
azuredevops_branch_policy_min_reviewers main-policy
  Repository ID: a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d
  Branch Name: refs/heads/main
```

**After** (with mapping and icons):
```markdown
azuredevops_branch_policy_min_reviewers main-policy
  Repository ID: 🗃️ Infrastructure Repo [a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d]
  Branch Name: ⎇ refs/heads/main
```

## 🔧 Usage

No changes to the CLI interface. Use the existing `--principal-mapping` option:

```bash
# With repository mapping
tfplan2md --principal-mapping principals.json plan.json

# Docker with mapping
docker run -v $(pwd):/data oocx/tfplan2md \
  --principal-mapping /data/principals.json \
  /data/plan.json --output /data/plan.md

# With debug output to see repository counts
tfplan2md --debug --principal-mapping principals.json plan.json
```

## 🐛 Debug Output

When using the `--debug` flag, diagnostic output includes repository mapping information:

```markdown
### Principal Mapping

Principal Mapping: Loaded successfully from 'principals.json'
- Found 45 principals
- Found 3 azdo users, 2 azdo groups, 1 azdo project, 3 azdo repositories

Failed to resolve 1 azdo repository:
- Repository `unknown-repo-guid` (referenced in `azuredevops_build_definition.example`)
```

## 🎯 Custom Templates

For users writing custom Scriban templates, a new helper function is available:

- `azdo_repository_name(repositoryId)` → resolves Azure DevOps repository ID to display name

This helper provides explicit control over repository resolution in custom templates. For default rendering, repositories are automatically resolved via value formatters and semantic formatting.

## ✅ Backwards Compatibility

- **Existing mapping files work unchanged**: Files without `azdoRepositories` continue to work
- **No breaking changes**: All existing functionality remains intact
- **Optional feature**: Only use repository mappings if you need them
- **Icons applied uniformly**: Repository and branch icons appear for all matching attribute names across all providers (not limited to Azure DevOps)

## 🔍 Technical Details

- **Semantic icon application**: Repository (🗃️) and branch (⎇) icons are applied via semantic formatting layer based on attribute name matching
- **Case-insensitive matching**: Attribute names are matched case-insensitively (e.g., `repo_id`, `Repo_ID`, and `REPO_ID` all get the icon)
- **Consistent with Feature 085**: Repository mapping follows the same architecture as Azure DevOps principal mapping (users, groups, projects)
- **Non-breaking spaces**: Icons use non-breaking spaces for proper formatting in markdown tables
- **Provider-agnostic icons**: While designed for Azure DevOps, the semantic icons apply to any provider using the same attribute names

## 📚 Related Documentation

- **Feature specification**: [docs/features/095-azdo-repo-mapping-and-icons/specification.md](specification.md)
- **Architecture**: [docs/features/095-azdo-repo-mapping-and-icons/architecture.md](architecture.md)
- **Test plan**: [docs/features/095-azdo-repo-mapping-and-icons/test-plan.md](test-plan.md)
- **User guide**: [README.md](../../../README.md) (principal mapping section)
- **Features list**: [docs/features.md](../../features.md)

## 🙏 Acknowledgments

This feature extends the principal mapping pattern established in Feature 085 (Azure DevOps Principal Mapping) to include repository entities, and adds semantic icons to improve visual scanning of Azure DevOps resources in Terraform plans.
