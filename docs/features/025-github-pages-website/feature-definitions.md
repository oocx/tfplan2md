# Feature Definitions

## High Value: What Sets Us Apart

| Feature | Description | Group |
| :--- | :--- | :--- |
| **Semantic Diffs** | Shows "Before" and "After" values side-by-side for changed attributes, highlighting exactly what changed. | Smart Visualization |
| **Firewall Rule Interpretation** | Renders complex Azure Firewall rule collections as readable tables with protocols, ports, and actions. | Smart Visualization |
| **NSG Rule Interpretation** | Renders Network Security Group rules as readable tables, making security changes easy to audit. | Smart Visualization |
| **Role Assignment Mapping** | Resolves cryptic Principal IDs (GUIDs), role names, and scopes to human-readable names (e.g., "Jane Doe", "DevOps Team", "Reader", "Resource Group X"). | Smart Visualization |
| **Large Value Formatting** | Handles large text blocks (like JSON policies or scripts) by showing a computed diff instead of the full text. | Smart Visualization |
| **Optimized for PR Comments** | Designed and tested for rendering in markdown pull requests on Azure DevOps Services and Github. | Integration |
| **Friendly Resource Names** | Displays friendly names for resources instead of complex resource ID strings. | Smart Visualization |

## Medium Value: Built-In Capabilities

| Feature | Description | Group |
| :--- | :--- | :--- |
| **Plan Summary** | High-level overview table showing counts of adds, changes, and destroys by resource type. | Core Reporting |
| **Module Grouping** | Groups resources logically by their Terraform module hierarchy (e.g., module.network). | Core Reporting |
| **Collapsible Details** | Hides verbose resource details inside `<details>` tags to keep the PR comment readable. | Core Reporting |
| **Tag Visualization** | Renders resource tags with specific icons and formatting for easy scanning. | Smart Visualization |
| **Smart Iconography** | Adds context-aware icons for common attributes like Locations (🌍), IPs (🌐), and Ports (🔌). | Smart Visualization |
| **Custom Templates** | Allows users to completely customize the markdown output using Go templates. | Extensibility |
| **Standard Features** | Works with any Terraform provider (AWS, GCP, etc.) using standard resource rendering. | Core Reporting |
| **Local Resource Names** | In modules, just render the local name part instead of the full name that includes the module. | Smart Visualization |
| **CI/CD Integration** | Native support and examples for GitHub Actions, Azure DevOps, and GitLab CI. | Integration |

## Low Value: Also Included

| Feature | Description | Group |
| :--- | :--- | :--- |
| **Sensitive Value Masking** | Automatically detects and masks sensitive values. Optionally, sensitive values can be included in the report. | Security & Compliance |
| **Docker Support** | Distributed as a lightweight Docker container for easy usage in any environment. | Integration |
| **Minimal Base Image** | Built on the secure and minimal `mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled` image. | Security & Compliance |
| **Dark/Light Mode** | Fully supports both dark and light appearance modes. | UX |
