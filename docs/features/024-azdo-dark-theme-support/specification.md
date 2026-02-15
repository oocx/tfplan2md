# Feature: Azure DevOps Dark Theme Support

## Overview

Users viewing Terraform plan reports in Azure DevOps Services with dark theme enabled encounter visually jarring borders around resource details. The current near-white borders create excessive contrast against dark backgrounds. This feature implements theme-adaptive border styling that automatically adjusts to both light and dark Azure DevOps themes.

## User Goals

- View Terraform plan reports comfortably in Azure DevOps dark mode
- Experience subtle, appropriate borders regardless of theme choice
- Avoid visual strain from high-contrast borders in dark environments

## Scope

### In Scope
- Update `<details>` element border styling in Azure DevOps HTML reports
- Use Azure DevOps theme-aware CSS custom property `--palette-neutral-10`
- Provide neutral gray fallback color for non-Azure DevOps environments
- Ensure borders remain subtle in both light and dark themes

### Out of Scope
- GitHub report styling (not tested in GitHub dark mode)
- Border styling for elements other than `<details>` (resource containers)
- Changes to border width, radius, or other styling properties
- Markdown report output (HTML only)

## User Experience

Users will see:
- In Azure DevOps light theme: Borders automatically use light-appropriate colors from `--palette-neutral-10`
- In Azure DevOps dark theme: Borders automatically use dark-appropriate colors from `--palette-neutral-10`
- In non-Azure DevOps contexts: Borders use neutral gray fallback color (`#999`)

The visual change will be automatic - no user configuration or action required.

## Success Criteria

- [ ] Azure DevOps HTML reports use `border-color: rgb(var(--palette-neutral-10, 153, 153, 153));` for `<details>` elements
- [ ] Borders appear subtle in Azure DevOps dark theme (no excessive contrast)
- [ ] Borders remain appropriate in Azure DevOps light theme
- [ ] Fallback gray color works in non-Azure DevOps environments
- [ ] Existing demo artifacts updated to show new styling

## Open Questions

None - requirements are clear.
