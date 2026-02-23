# Terraform Outputs Support

This release adds rendering of Terraform output changes to markdown reports, making it easy to see which outputs are being created, updated, or deleted as part of a plan — with sensitive values protected by default.

## ✨ Features

- **Outputs table**: Outputs render as a 5-column table showing Change, Name, Description, Sensitive, and Value
- **Change type column**: Each output shows its action icon (➕ create, 🔄 update, ❌ delete, or blank for no-op), consistent with resource change rendering
- **Sensitive value masking**: Sensitive outputs display `(sensitive value)` by default; use `--show-sensitive` to reveal them
- **Computed value handling**: Outputs with unknown values (known after apply) display `(known after apply)`
- **Module outputs**: Outputs scoped to a module appear at the end of that module's section under a `#### 📤 Outputs` heading
- **Global outputs**: Root-level outputs appear after all resource sections under a `## 📤 Outputs` heading
- **Display name mappings**: Output values automatically benefit from existing display name mappings (Azure resource IDs, principal names, role names, etc.)
- **Alphabetical ordering**: Outputs within each section are sorted alphabetically by name

## 🔒 Security

Sensitive output values are **masked by default** to prevent accidental exposure of secrets (tokens, passwords, keys) in PR comments and review reports.

To reveal sensitive values when needed (e.g., in a secure environment):

```bash
tfplan2md --show-sensitive plan.json
```

## 📋 Output Table Format

```markdown
## 📤 Outputs

| Change | Name | Description | Sensitive | Value |
| ------ | ---- | ----------- | --------- | ----- |
| ➕ | `new_repository_id` | ID of the new repository | No | `abc-123` |
| 🔄 | `endpoint_url` | Service endpoint URL | No | `https://...` |
| ➕ | `deploy_token` | Deployment token | 🔒 Yes | (sensitive value) |
```

## 💡 Use Cases

- **Infrastructure visibility**: See which output values are exposed as part of a plan
- **Security review**: Sensitive outputs are clearly marked without revealing their values
- **ID-to-name resolution**: Output values referencing Azure resource IDs are automatically resolved to human-readable names where possible (e.g., output referencing a repository ID shows the repository name)

## ▶️ Example: Repository ID Output

When an output references a resource ID that tfplan2md can resolve to a display name, the value column shows the readable name:

```hcl
resource "azuredevops_git_repository" "new_repo" {
  ...
}

output "new_repository_id" {
  value = azuredevops_git_repository.new_repo.id
}
```

Renders as:

| Change | Name | Description | Sensitive | Value |
| ------ | ---- | ----------- | --------- | ----- |
| ➕ | `new_repository_id` | ID of the new repository | No | `🗃️ my-repo [a1b2c3d4-...]` |

## 🔧 Usage

No changes to the CLI interface are required to use output rendering. Outputs are rendered automatically.

To suppress sensitive output values (default behavior — nothing to configure):

```bash
tfplan2md plan.json
```

To show sensitive output values:

```bash
tfplan2md --show-sensitive plan.json
```

## ✅ Backwards Compatibility

- **Existing plans without outputs**: Reports without `output_changes` in the plan JSON are unaffected; no outputs section is rendered
- **No breaking changes**: All existing functionality remains intact
- **CLI interface unchanged**: No new required flags

## 🔍 Technical Details

- **Data source**: Reads `output_changes` from the Terraform plan JSON (top-level key alongside `resource_changes`)
- **Metadata correlation**: Description and sensitivity flags are read from `configuration.root_module.outputs` (and module equivalents)
- **Sensitivity detection**: Precedence order — `after_sensitive` marker > `before_sensitive` marker > configuration `sensitive` flag
- **Value formatting**: Reuses the existing `ValueFormatterRegistry` pipeline, so all provider-specific display name mappings apply automatically

## 📚 Related Documentation

- **Feature specification**: [docs/features/097-terraform-outputs/specification.md](specification.md)
- **Architecture**: [docs/features/097-terraform-outputs/architecture.md](architecture.md)
- **Test plan**: [docs/features/097-terraform-outputs/test-plan.md](test-plan.md)
