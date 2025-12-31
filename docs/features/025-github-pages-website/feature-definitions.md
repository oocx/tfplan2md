# Feature Definitions

| Feature | Description | Group | Value |
| :--- | :--- | :--- | :--- |
| **Semantic Diffs** | Shows "Before" and "After" values side-by-side for changed attributes, highlighting exactly what changed. | What Sets Us Apart | High |
| **Firewall Rule Interpretation** | Renders complex Azure Firewall rule collections as readable tables with protocols, ports, and actions. | What Sets Us Apart | High |
| **NSG Rule Interpretation** | Renders Network Security Group rules as readable tables, making security changes easy to audit. | What Sets Us Apart | High |
| **Role Assignment Mapping** | Resolves cryptic Principal IDs (GUIDs), role names, and scopes to human-readable names. | What Sets Us Apart | High |
| **Large Value Formatting** | Handles large text blocks (like JSON policies or scripts) by showing a computed diff instead of the full text. | What Sets Us Apart | High |
| **Optimized for PR Comments** | Designed and tested for rendering in markdown pull requests on Azure DevOps Services and Github. | What Sets Us Apart | High |
| **Friendly Resource Names** | Friendly names for resources instead of complex resource id strings. | What Sets Us Apart | High |
| **Plan Summary** | High-level overview table showing counts of adds, changes, and destroys by resource type. | Built-In Capabilities | Medium |
| **Module Grouping** | Groups resources logically by their Terraform module hierarchy (e.g., module.network). | Built-In Capabilities | Medium |
| **Collapsible Details** | Hides verbose resource details inside `<details>` tags to keep the PR comment readable. | Built-In Capabilities | Medium |
| **Tag Visualization** | Renders resource tags with specific icons and formatting for easy scanning. | Built-In Capabilities | Medium |
| **Smart Iconography** | Adds context-aware icons for common attributes like Locations (🌍), IPs (🌐), and Ports (🔌). | Built-In Capabilities | Medium |
| **Custom Templates** | Allows users to completely customize the markdown output using Go templates. | Built-In Capabilities | Medium |
| **Standard Features** | Works with any Terraform provider (AWS, GCP, etc.) using standard resource rendering. | Built-In Capabilities | Medium |
| **CI/CD Integration** | Native support and examples for GitHub Actions, Azure DevOps, and GitLab CI. | Built-In Capabilities | Medium |
| **Local Resource Names** | In modules, just render the local name part instead of the full name that includes the module. | Built-In Capabilities | Medium |
| **Sensitive Value Masking** | Automatically detects and masks sensitive values (marked as sensitive in Terraform) to prevent leaks. Optionally, sensitive values can be included in the report. | Also Included | Low |
| **Docker Support** | Distributed as a lightweight Docker container for easy usage in any environment. | Also Included | Low |
