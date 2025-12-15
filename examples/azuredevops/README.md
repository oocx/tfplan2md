# Azure DevOps Example

This example demonstrates how to create Azure DevOps resources using Terraform. It creates:

- An Azure DevOps project
- A Git repository
- A variable group with sample variables
- A build pipeline (definition)

## Purpose

This example is designed to generate Terraform plans that can be used to test `tfplan2md`.

## Prerequisites

- [Terraform](https://www.terraform.io/downloads) >= 1.0
- An Azure DevOps organization
- A Personal Access Token (PAT) with the following permissions:
  - Project and Team: Read, write, & manage
  - Code: Full
  - Build: Read & execute
  - Variable Groups: Read, create, & manage

## Configuration

Set the following environment variables:

```bash
export AZDO_ORG_SERVICE_URL="https://dev.azure.com/your-organization"
export AZDO_PERSONAL_ACCESS_TOKEN="your-pat-token"
```

## Usage

### Initialize Terraform

```bash
terraform init
```

### Generate a Plan

To generate a plan file for testing with `tfplan2md`:

```bash
# Generate a binary plan file
terraform plan -out=tfplan

# Convert to JSON format
terraform show -json tfplan > tfplan.json
```

### Convert Plan to Markdown

```bash
# Using tfplan2md
tfplan2md tfplan.json
```

### Customize Variables

You can customize the resources by creating a `terraform.tfvars` file:

```hcl
project_name        = "my-custom-project"
project_description = "My custom Azure DevOps project"
repository_name     = "my-repo"
variable_group_name = "my-variables"
pipeline_name       = "my-pipeline"
```

## Resources Created

| Resource | Type | Description |
|----------|------|-------------|
| `azuredevops_project.example` | Project | The main Azure DevOps project |
| `azuredevops_git_repository.example` | Repository | Git repository within the project |
| `azuredevops_variable_group.example` | Variable Group | Shared variables for pipelines |
| `azuredevops_build_definition.example` | Pipeline | Build pipeline using YAML |

## Cleaning Up

To destroy the created resources:

```bash
terraform destroy
```

## Notes

- The pipeline expects an `azure-pipelines.yml` file in the repository root
- Secret variables are marked as sensitive and won't appear in plan outputs
- The project is created with private visibility by default
