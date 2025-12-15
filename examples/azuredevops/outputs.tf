output "project_id" {
  description = "The ID of the created Azure DevOps project"
  value       = azuredevops_project.example.id
}

output "project_name" {
  description = "The name of the created Azure DevOps project"
  value       = azuredevops_project.example.name
}

output "repository_id" {
  description = "The ID of the created Git repository"
  value       = azuredevops_git_repository.example.id
}

output "repository_url" {
  description = "The HTTPS URL of the created Git repository"
  value       = azuredevops_git_repository.example.remote_url
}

output "variable_group_id" {
  description = "The ID of the created variable group"
  value       = azuredevops_variable_group.example.id
}

output "pipeline_id" {
  description = "The ID of the created build pipeline"
  value       = azuredevops_build_definition.example.id
}
