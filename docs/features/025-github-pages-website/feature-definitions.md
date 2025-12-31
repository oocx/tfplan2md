# Feature Definitions

## High Value: What Sets Us Apart

| Feature | Description |
| :--- | :--- |
| **Semantic Diffs** | Shows "Before" and "After" values side-by-side for changed attributes, highlighting exactly what changed. |
| **Firewall Rule Interpretation** | Renders complex Azure Firewall rule collections as readable tables with protocols, ports, and actions. |
| **NSG Rule Interpretation** | Renders Network Security Group rules as readable tables, making security changes easy to audit. |
| **Role Assignment Mapping** | Resolves cryptic Principal IDs (GUIDs) to human-readable names (e.g., "Jane Doe", "DevOps Team"), including mapping of scopes and role names. |
| **Large Value Formatting** | Shows computed diffs for large values like JSON policies or scripts. |
| **Optimized for PR Comments** | Designed and tested for rendering in markdown pull requests on Azure DevOps Services and Github. |
| **Friendly Names** | Friendly names for resources instead of complex resource id strings. |

## Medium Value: Built-In Capabilities

| Feature | Description |
| :--- | :--- |
| **Plan Summary** | High-level overview table showing counts of adds, changes, and destroys by resource type. |
| **Module Grouping** | Groups resources logically by their Terraform module hierarchy (e.g., module.network). |
| **Collapsible Details** | Hides verbose resource details inside `<details>` tags to keep the PR comment readable. |
| **Tag Visualization** | Renders resource tags with specific icons and formatting for easy scanning. |
| **Smart Iconography** | Adds context-aware icons for common attributes like Locations (🌍), IPs (🌐), and Ports (🔌). |
| **Custom Templates** | Allows users to completely customize the markdown output using Go templates. |
| **CI/CD Integration** | Native support and examples for GitHub Actions, Azure DevOps, and GitLab CI. |
| **Standard Features** | Works with any Terraform provider (AWS, GCP, etc.) using standard resource rendering. |
| **Local Resource Names** | In modules, just render the local name part instead of the full name that includes the module. |

## Low Value: Also Included

| Feature | Description |
| :--- | :--- |
| **Sensitive Value Masking** | Automatically detects and masks sensitive values (marked as sensitive in Terraform) to prevent leaks. Optionally, sensitive values can be included in the report. |
| **Docker Support** | Distributed as a lightweight Docker container for easy usage in any environment. |
| **Minimal Base Image** | Based on `mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled` for security and small footprint. |
| **Dark Mode / Light Mode** | Fully supports both dark and light themes to match your system preferences. |
