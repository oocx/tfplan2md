# Feature Definitions for tfplan2md Website

This document defines the agreed-upon features, their categorization, and priority for display on the website.

## Category Headlines

| Category | Website Headline | Contains |
|----------|------------------|----------|
| High Impact | What Sets Us Apart | High-value, differentiating features |
| Standard Features | Built-In Capabilities | Medium-value, expected features |
| Additional | Also Included | Low-value, nice-to-have features |

## Complete Feature List

| Feature | Description | Category | Value |
|---------|-------------|----------|-------|
| Semantic Diffs | Shows "Before" and "After" values side-by-side for changed attributes, highlighting exactly what changed. | What Sets Us Apart | High |
| Large Value Formatting with Inline Diffs | Handles large text blocks (like JSON policies or scripts) by showing a computed diff with inline highlighting instead of the full text. | What Sets Us Apart | High |
| Firewall Rule Interpretation | Renders complex Azure Firewall rule collections as readable tables with protocols, ports, and actions. | What Sets Us Apart | High |
| NSG Rule Interpretation | Renders Network Security Group rules as readable tables, making security changes easy to audit. | What Sets Us Apart | High |
| Role Assignment Mapping | Resolves cryptic Principal IDs (GUIDs) to human-readable names (e.g., "Jane Doe", "DevOps Team"). Also maps scope resource IDs and role definition IDs to friendly names. | What Sets Us Apart | High |
| CI/CD Integration | Native support and examples for GitHub Actions, Azure DevOps, and GitLab CI. | Built-In Capabilities | Medium |
| PR Platform Compatibility | Designed and tested for rendering in markdown pull requests on Azure DevOps Services and GitHub. | What Sets Us Apart | High |
| Friendly Resource Names | Displays friendly names for resources instead of complex resource ID strings. | What Sets Us Apart | High |
| Provider Agnostic Core | Works with any Terraform provider (AWS, GCP, etc.) using standard resource rendering. | Built-In Capabilities | Medium |
| Plan Summary | High-level overview table showing counts of adds, changes, and destroys by resource type. | Built-In Capabilities | Medium |
| Module Grouping | Groups resources logically by their Terraform module hierarchy (e.g., module.network). | Built-In Capabilities | Medium |
| Collapsible Details | Hides verbose resource details inside `<details>` tags to keep the PR comment readable. | Built-In Capabilities | Medium |
| Local Resource Names | In modules, renders just the local name part instead of the full qualified name that includes the module path. | Built-In Capabilities | Medium |
| Tag Visualization | Renders resource tags with specific icons and formatting for easy scanning. | Built-In Capabilities | Medium |
| Smart Iconography | Adds context-aware icons for common attributes like Locations (🌍), IPs (🌐), and Ports (🔌). | Built-In Capabilities | Medium |
| Custom Templates | Allows users to completely customize the markdown output using Go templates. | Built-In Capabilities | Medium |
| Docker Support | Distributed as a lightweight Docker container for easy usage in any environment. | Also Included | Low |
| Sensitive Value Masking | Automatically detects and masks sensitive values (marked as sensitive in Terraform) to prevent leaks. | Also Included | Low |
| Minimal Container Image | Uses a minimal base image (mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled) for reduced attack surface and faster pulls. | Also Included | Low |

## Feature Groups for Website Display

### What Sets Us Apart (High Impact)
Features that differentiate tfplan2md from other solutions:
- Semantic Diffs
- Large Value Formatting with Inline Diffs
- Firewall Rule Interpretation
- NSG Rule Interpretation
- Role Assignment Mapping
- PR Platform Compatibility
- Friendly Resource Names

### Built-In Capabilities (Standard Features)
Expected functionality that provides value:
- Provider Agnostic Core
- Plan Summary
- Module Grouping
- Collapsible Details
- Local Resource Names
- Tag Visualization
- Smart Iconography
- Custom Templates
- CI/CD Integration

### Also Included (Additional Features)
Nice-to-have features mentioned together:
- Docker Support
- Sensitive Value Masking
- Minimal Container Image

## Carousel Features (Homepage)

The homepage carousel should showcase the High Impact features, rotating through:
1. Semantic Diffs (before/after visualization)
2. Large Value Formatting with Inline Diffs
3. Firewall Rule Interpretation
4. NSG Rule Interpretation
5. Role Assignment Mapping
6. PR Platform Compatibility

## Feature Page Structure

- **High Impact features**: Each gets its own dedicated feature page with examples
- **Standard Features**: Each gets its own dedicated feature page
- **Additional Features**: All combined on a single "Also Included" page
