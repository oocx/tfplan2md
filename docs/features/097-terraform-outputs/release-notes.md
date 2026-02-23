# Terraform Outputs Support

tfplan2md now renders `output_changes` from your Terraform plan in the markdown report. Outputs appear at the end of the root module section, displayed as a table with name, description, sensitivity, and value columns.

## Features

- **Output Changes Table**: Outputs are rendered in a `📤 Outputs` table showing name, description, action, sensitivity, and value
- **Sensitive Value Masking**: Sensitive outputs are automatically masked (`***`) by default. Use `--show-sensitive` to reveal values
- **Unknown Values**: Outputs with unknown values (computed after apply) show `(known after apply)`
- **Action Icons**: Each output shows the planned action (➕ create, 🔄 update, ❌ delete, ⏺️ no-op)
- **Descriptions**: Output descriptions from your Terraform configuration are included in the table

## Example

For a Terraform configuration with outputs like:

```hcl
output "repository_id" {
  value       = azuredevops_git_repository.new_repo.id
  description = "The ID of the created Git repository"
}

output "secret_token" {
  value     = var.api_token
  sensitive = true
}
```

The report renders:

```markdown
#### 📤 Outputs

| Name | Description | Action | Sensitive | Value |
| ---- | ----------- | ------ | --------- | ----- |
| `repository_id` | The ID of the created Git repository | ➕ create | No | (known after apply) |
| `secret_token` |  | ➕ create | 🔒 Yes | *** |
```

## Commits

- feat: add terraform outputs support
