variable "project_name" {
  description = "The name of the Azure DevOps project"
  type        = string
  default     = "example-project"
}

variable "project_description" {
  description = "The description of the Azure DevOps project"
  type        = string
  default     = "An example project created by Terraform for testing tfplan2md"
}

variable "repository_name" {
  description = "The name of the Git repository"
  type        = string
  default     = "example-repo"
}

variable "variable_group_name" {
  description = "The name of the variable group"
  type        = string
  default     = "example-variables"
}

variable "pipeline_name" {
  description = "The name of the build pipeline"
  type        = string
  default     = "example-pipeline"
}
