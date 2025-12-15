# Azure DevOps Example
# This example creates an Azure DevOps project with a repository, build pipeline, and variable group.
# It can be used to generate Terraform plans for testing tfplan2md.

terraform {
  required_version = ">= 1.0"

  required_providers {
    azuredevops = {
      source  = "microsoft/azuredevops"
      version = "~> 1.0"
    }
  }
}

provider "azuredevops" {
  # Configuration is expected via environment variables:
  # - AZDO_ORG_SERVICE_URL: The Azure DevOps organization URL (e.g., https://dev.azure.com/myorg)
  # - AZDO_PERSONAL_ACCESS_TOKEN: A PAT with appropriate permissions
}

# Azure DevOps Project
resource "azuredevops_project" "example" {
  name               = var.project_name
  description        = var.project_description
  visibility         = "private"
  version_control    = "Git"
  work_item_template = "Agile"

  features = {
    "boards"       = "enabled"
    "repositories" = "enabled"
    "pipelines"    = "enabled"
    "testplans"    = "disabled"
    "artifacts"    = "disabled"
  }
}

# Git Repository
resource "azuredevops_git_repository" "example" {
  project_id = azuredevops_project.example.id
  name       = var.repository_name

  initialization {
    init_type = "Clean"
  }
}

# Variable Group
resource "azuredevops_variable_group" "example" {
  project_id   = azuredevops_project.example.id
  name         = var.variable_group_name
  description  = "Variable group for example pipeline"
  allow_access = true

  variable {
    name  = "ENVIRONMENT"
    value = "development"
  }

  variable {
    name  = "APP_VERSION"
    value = "1.0.0"
  }

  secret_variable {
    name  = "SECRET_KEY"
    value = "supersecret"
  }
}

# Build Definition (Pipeline)
resource "azuredevops_build_definition" "example" {
  project_id = azuredevops_project.example.id
  name       = var.pipeline_name
  path       = "\\Pipelines"

  ci_trigger {
    use_yaml = true
  }

  repository {
    repo_type   = "TfsGit"
    repo_id     = azuredevops_git_repository.example.id
    branch_name = azuredevops_git_repository.example.default_branch
    yml_path    = "azure-pipelines.yml"
  }

  variable_groups = [
    azuredevops_variable_group.example.id
  ]

  variable {
    name  = "BUILD_CONFIGURATION"
    value = "Release"
  }

  variable {
    name  = "BUILD_PLATFORM"
    value = "Any CPU"
  }
}
