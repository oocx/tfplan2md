# Parent-Child Resource Grouping (Inline Tables)

This release introduces an improved rendering pattern for resources with parent-child relationships, such as groups with members or network collections with rules. Instead of separate collapsible sections, child resources are now rendered as compact inline tables within the parent resource section.

## ✨ Features

- **Inline Child Resource Tables**: Automatically groups functionally related resources (e.g., `azuread_group_member` or `azuredevops_group_membership`) into tables inside their parent resource section.
- **Enhanced Grouping Support**: Initially supports Azure AD Groups, Azure DevOps Groups, and Azure DevOps Teams.
- **Mixed Management Detection**: Added a warning indicator when a resource has children managed both as inline attributes and as separate Terraform resources, helping to identify potential configuration conflicts.
- **Improved Resource Summaries**: Parent resource summary lines now include aggregate counts of child changes (e.g., "👤 5 members | ➕ 2 members").
- **Preserved Code Analysis Findings**: Security and quality findings for child resources are now displayed directly within the parent section, preserving the original resource address for clear attribution.
- **Configuration Reference Matching**: Sophisticated matching logic for `(known after apply)` scenarios using Terraform configuration expressions to ensure children are correctly associated with parents even before they are created.

## 🐛 Bug fixes

- **Azure AD Group Formatting**: Improved the display of Azure AD group names and descriptions in summaries with a clearer separator.
- **Target Table Consistency**: Fixed an issue where some inline child attributes were duplicated in both the main attribute table and the specialized child table.
- **CI Reliability**: Resolved flaky architecture boundary tests that occurred stochastically in high-load environments.

## 📚 Documentation

- Added a comprehensive catalog of planned parent-child patterns in `docs/features.md`.
- Updated `README.md` to reflect the new inline rendering capabilities.

## 🔗 Commits

- [6259fba4](https://github.com/oocx/tfplan2md/commit/6259fba4) feat: add configuration reference matching
- [fda5f5d1](https://github.com/oocx/tfplan2md/commit/fda5f5d1) feat: remove inline child attributes from parent tables
- [b11ac8ac](https://github.com/oocx/tfplan2md/commit/b11ac8ac) refactor: move ProviderRegistry to MarkdownGeneration.Services
- [990fe38d](https://github.com/oocx/tfplan2md/commit/990fe38d) fix: resolve flaky architecture tests in CI

## ▶️ Getting started

This feature is enabled by default for all supported resource types. No additional configuration or flags are required.
