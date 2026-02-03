# Improved Network Security Group Rendering and Markdown Escaping

## Overview
This release improves the clarity and readability of Terraform plan reports, specifically for Azure Network Security Groups (NSG) and Firewall Rule Collections. We've polished the visual layout and fixed an escaping issue that made some configuration values hard to read.

## What's New
### Polished NSG and Firewall Reports
We've streamlined the reports for Azure Network Security Groups and Firewall Network Rule Collections by removing redundant header information. This makes the generated markdown more concise and focuses your attention on the actual security rules being changed.

### Smarter Column Layouts for New Resources
When creating new NSGs or Firewall rules, the report now correctly displays a single "Value" column instead of an empty "Before" column. This aligns specialized security templates with the clean layout used by other resources in `tfplan2md`.

## Improvements
- **Cleaner Markdown Escaping**: We've refined how special characters are handled in markdown output. Values containing sequences like `->` (common in NSG rule descriptions) now render cleanly without unnecessary backslashes when viewed in tools that support inline code spans.
- **Reduced Redundancy**: Removed duplicate resource identification lines in NSG semantic templates.

## Bug Fixes
- Fixed an issue where `>` was being over-escaped to `\>`, which was visible inside inline code blocks.
- Fixed a layout bug where create/add actions in NSG templates still showed a the "Before" column.

## Getting Started
No configuration changes are required. Simply run `tfplan2md` as usual to see the improved rendering for your AzureRM plans:

```bash
tfplan2md plan.json > plan.md
```
