# Feature Definitions (Website)

This document is the **source of truth** for how each tfplan2md feature is represented on the website.

## Rules

- The feature list and categorization (Group/Value) must stay consistent with the agreed feature definitions.
- The `Icon/Image` assignment must be **unique per feature** (different features may not use the same icon/image).
- If a feature is reclassified or its icon/image changes, update this file in the same PR.

## Current Icon Usage

The website uses inline emoji icons on the Features page. Each feature must have a unique icon.

### Icon Inventory

| Icon | Assigned To | Status |
|------|-------------|--------|
| 🔍 | Semantic Diffs | ✅ Unique |
| 🔥 | Firewall Rule Interpretation | ✅ Unique |
| 🛡️ | NSG Rule Interpretation | ✅ Unique |
| 👥 | Role Assignment Mapping | ✅ Unique |
| 📄 | Large Value Formatting | ✅ Unique |
| 🔄 | CI/CD Integration | ✅ Unique |
| 📋 | PR Platform Compatibility | ✅ Unique |
| � | Friendly Resource Names | ✅ Unique |
| 📊 | Plan Summary | ✅ Unique |
| 📁 | Module Grouping | ✅ Unique |
| 📂 | Collapsible Details | ✅ Unique |
| 🏷️ | Tag Visualization | ✅ Unique |
| 🎨 | Smart Iconography | ✅ Unique |
| 🛠️ | Custom Templates | ✅ Unique |
| 🌐 | Provider Agnostic Core | ✅ Unique |
| 📝 | Local Resource Names | ✅ Unique |
| � | Container Support | ✅ Unique |
| 🔒 | Sensitive Value Masking | ✅ Unique |
| 📦 | Minimal Container Image | ✅ Unique |
| 🌓 | Dark/Light Mode | ✅ Unique |

## Feature Table

| Feature | Description | Group | Value | Icon/Image |
|---------|-------------|-------|-------|-----------|
| Semantic Diffs | Shows "Before" and "After" values side-by-side for changed attributes, highlighting exactly what changed. | What Sets Us Apart | High | 🔍 |
| Firewall Rule Interpretation | Renders complex Azure Firewall rule collections as readable tables with protocols, ports, and actions. | What Sets Us Apart | High | 🔥 |
| NSG Rule Interpretation | Renders Network Security Group rules as readable tables, making security changes easy to audit. | What Sets Us Apart | High | 🛡️ |
| Role Assignment Mapping | Resolves cryptic Principal IDs, Scopes, and Role Names (GUIDs) to human-readable names. | What Sets Us Apart | High | 👥 |
| Large Value Formatting | Handles large text blocks (like JSON policies or scripts) by showing a computed diff instead of the full text. | What Sets Us Apart | High | 📄 |
| CI/CD Integration | Native support and examples for GitHub Actions, Azure DevOps, and GitLab CI. | Built-In Capabilities | Medium | 🔄 |
| PR Platform Compatibility | Designed and tested for rendering in markdown pull requests on Azure DevOps Services and GitHub. | What Sets Us Apart | High | 📋 |
| Friendly Resource Names | Displays friendly names for resources instead of complex resource ID strings. | What Sets Us Apart | High | 🆔 |
| Plan Summary | High-level overview table showing counts of adds, changes, and destroys by resource type. | Built-In Capabilities | Medium | 📊 |
| Module Grouping | Groups resources logically by their Terraform module hierarchy (e.g., module.network). | Built-In Capabilities | Medium | 📁 |
| Collapsible Details | Hides verbose resource details inside `<details>` tags to keep the PR comment readable. | Built-In Capabilities | Medium | 📂 |
| Tag Visualization | Renders resource tags with specific icons and formatting for easy scanning. | Built-In Capabilities | Medium | 🏷️ |
| Smart Iconography | Adds context-aware icons for common attributes like Locations (🌍), IPs (🌐), and Ports (🔌). | Built-In Capabilities | Medium | 🎨 |
| Custom Templates | Allows users to completely customize the markdown output using Scriban templates. | Built-In Capabilities | Medium | 🛠️ |
| Provider Agnostic Core | Works with any Terraform provider (AWS, GCP, etc.) using standard resource rendering. | Built-In Capabilities | Medium | 🌐 |
| Local Resource Names | In modules, renders the local name part instead of the full name that includes the module path. | Built-In Capabilities | Medium | 📝 |
| Container Support | Distributed as a lightweight container image for easy usage in any environment. | Also Included | Low | 📦 |
| Sensitive Value Masking | Automatically detects and masks sensitive values (marked as sensitive in Terraform) to prevent leaks. Optionally, sensitive values can be included in the report. | Also Included | Low | 🔒 |
| Minimal Container Image | Uses mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled as base for minimal attack surface. | Also Included | Low | 📦 |
| Dark/Light Mode | Website supports dark and light theme toggle. | Also Included | Low | 🌓 |

## Feature Notes

- **Role Assignment Mapping**: Must mention mapping of scopes and role names (not just principals) - all three are important.
- **Sensitive Value Masking**: Include note that sensitive values can optionally be included.
- **Minimal Container Image**: Base image is `mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled`.
- **CI/CD Integration**: Moved to "Built-In Capabilities" per maintainer decision (was initially High, changed to Medium).

## Decision Log

- 2026-01-03: Initial feature definitions created.
- 2026-01-03: CI/CD Integration moved to Medium (Built-In Capabilities) per maintainer instruction.
- 2026-01-03: Tag Visualization keeps 🏷️ icon (matches how tags are visualized in product).
- 2026-01-03: Friendly Resource Names changed to 🆔 icon to resolve duplicate.
- 2026-01-03: Dark/Light Mode added as Low (Also Included).
- 2026-01-04: Custom Templates icon finalized (Grid Layout).
- 2026-01-04: Firewall Rules icon finalized (Cards Below / v6). Brick wall with 3 rows and separate allow/deny cards below.
