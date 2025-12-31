# Feature Definitions for tfplan2md Website

This document contains the agreed-upon feature categorization for the website content.

## Feature Categories

- **What Sets Us Apart** - High impact/high value features (shown in carousel, get dedicated pages)
- **Built-In Capabilities** - Medium value features (get dedicated pages)
- **Also Included** - Low value features (shown on a single page together)

## Complete Feature Table

| Feature | Description | Group | Value |
|---------|-------------|-------|-------|
| Semantic Diffs | Shows "Before" and "After" values side-by-side for changed attributes, highlighting exactly what changed. | What Sets Us Apart | High |
| Firewall Rule Interpretation | Renders complex Azure Firewall rule collections as readable tables with protocols, ports, and actions. | What Sets Us Apart | High |
| NSG Rule Interpretation | Renders Network Security Group rules as readable tables, making security changes easy to audit. | What Sets Us Apart | High |
| Role Assignment Mapping | Resolves cryptic Principal IDs, Scopes, and Role Names (GUIDs) to human-readable names (e.g., "Jane Doe", "DevOps Team", "Reader on rg-prod"). | What Sets Us Apart | High |
| Large Value Formatting | Handles large text blocks (like JSON policies or scripts) by showing a computed diff instead of the full text. Works with inline diffs for maximum clarity. | What Sets Us Apart | High |
| CI/CD Integration | Native support and examples for GitHub Actions, Azure DevOps, and GitLab CI. | What Sets Us Apart | High |
| PR Platform Compatibility | Designed and tested for rendering in markdown pull requests on Azure DevOps Services and GitHub. | What Sets Us Apart | High |
| Friendly Resource Names | Displays friendly names for resources instead of complex resource ID strings. | What Sets Us Apart | High |
| Plan Summary | High-level overview table showing counts of adds, changes, and destroys by resource type. | Built-In Capabilities | Medium |
| Module Grouping | Groups resources logically by their Terraform module hierarchy (e.g., module.network). | Built-In Capabilities | Medium |
| Collapsible Details | Hides verbose resource details inside \<details\> tags to keep the PR comment readable. | Built-In Capabilities | Medium |
| Tag Visualization | Renders resource tags with specific icons and formatting for easy scanning. | Built-In Capabilities | Medium |
| Smart Iconography | Adds context-aware icons for common attributes like Locations (🌍), IPs (🌐), and Ports (🔌). | Built-In Capabilities | Medium |
| Custom Templates | Allows users to completely customize the markdown output using Go templates. | Built-In Capabilities | Medium |
| Provider Agnostic Core | Works with any Terraform provider (AWS, GCP, etc.) using standard resource rendering. | Built-In Capabilities | Medium |
| Local Resource Names | In modules, renders the local name part instead of the full name that includes the module path. | Built-In Capabilities | Medium |
| Docker Support | Distributed as a lightweight Docker container for easy usage in any environment. | Also Included | Low |
| Sensitive Value Masking | Automatically detects and masks sensitive values (marked as sensitive in Terraform) to prevent leaks. | Also Included | Low |
| Minimal Container Image | Uses mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled as base for minimal attack surface. | Also Included | Low |

## Website Implementation Notes

### Carousel (Homepage)
Shows high-value features from "What Sets Us Apart" category with rotating visual examples.

### Feature Pages
- Each feature in "What Sets Us Apart" gets its own dedicated page with screenshots and examples
- Each feature in "Built-In Capabilities" gets its own dedicated page
- All features in "Also Included" are displayed together on a single page

### Feature Index Page
Groups features by category with the agreed-upon headlines:
- What Sets Us Apart
- Built-In Capabilities
- Also Included
