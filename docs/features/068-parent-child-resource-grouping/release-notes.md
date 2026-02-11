# Parent-Child Resource Grouping (Inline Tables)

This release introduces an improved rendering pattern for resources with parent-child relationships, such as groups with members or network collections with rules. Instead of separate collapsible sections, child resources are now rendered as compact inline tables within the parent resource section.

## ✨ Features

- **Inline Child Resource Tables**: Automatically groups functionally related resources (e.g., `azuread_group_member` or `azuredevops_team_members`) into tables inside their parent resource section.
- **Enhanced Grouping Support**: Initially supports Azure AD Groups, Azure DevOps Groups, and Azure DevOps Teams.
- **Mixed Management Detection**: Added a warning indicator when a resource has children managed both as inline attributes and as separate Terraform resources, helping to identify potential configuration conflicts.
- **Improved Resource Summaries**: Parent resource summary lines now include aggregate counts of child changes (e.g., "👤 5 members | ➕ 2 members").
- **Preserved Code Analysis Findings**: Security and quality findings for child resources are now displayed directly within the parent section, preserving the original resource address for clear attribution.
- **Configuration Reference Matching**: Sophisticated matching logic for `(known after apply)` scenarios using Terraform configuration expressions to ensure children are correctly associated with parents even before they are created.

## 🐛 Bug fixes

- **Azure AD Group Formatting**: Improved the display of Azure AD group names and descriptions in summaries with a clearer separator.
- **Target Table Consistency**: Fixed an issue where some inline child attributes were duplicated in both the main attribute table and the specialized child table.

## 📚 Documentation

- Added a comprehensive catalog of planned parent-child patterns in `docs/features.md`.
- Updated `README.md` to reflect the new inline rendering capabilities.

## 🔗 Commits

- [50972f49](https://github.com/oocx/tfplan2md/commit/50972f49) feat: add configuration reference matching
- 45a61f74 feat: remove inline child attributes from parent tables
- fe0df543 feat: add azuredevops group/team inline rendering
- 7b6c757b feat: add azuread group member inline rendering
- 230c181c feat: add child resource rendering pipeline
- 50aaf5ed feat: merge parent-child resources in report model
- c1434cc5 feat: add provider hook for parent-child relationships
- 71b8c9ea feat: add parent-child relationship registry

## ▶️ Getting started

This feature is enabled by default for all supported resource types. No additional configuration or flags are required.

