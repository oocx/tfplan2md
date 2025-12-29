# Feature: Firewall Rule Before/After Attributes Display

## Overview

Enhance the Azure firewall rule collection template to show before and after values for modified rule attributes. Currently, modified rules (indicated with 🔄) only display the "after" values in the main table, making it difficult for users to understand what specifically changed within each rule. This feature will display both before and after values for changed attributes directly in the main rule changes table, maintaining the concise representation while providing full visibility into changes.

## User Goals

- Users need to see what specific attributes changed within a modified firewall rule during Terraform plan review
- Users want to quickly identify which rule attributes were added, removed, or changed without navigating away from the main table
- Users need this information to be visually clear and easy to compare, especially when reviewing plans in CI/CD pipeline comments or reports

## Scope

### In Scope

- Display before and after values for changed attributes in modified firewall rules
- Show before and after values in the same table cell, separated by a newline for easy visual comparison
- Use diff-style symbols (`-` prefix for before value, `+` prefix for after value) to clearly indicate which is which
- Only show before/after for attributes that actually changed (unchanged attributes display single value without prefix)
- Maintain the current concise single-table layout with all rules (added, modified, removed, unchanged) in one table
- Apply this enhancement to the `azurerm_firewall_network_rule_collection` resource template

### Out of Scope

- Changes to how added, removed, or unchanged rules are displayed (they remain as-is)
- Changes to the overall table structure or column layout
- Display of before/after for collection-level attributes (name, priority, action) - only rule-level attributes
- Implementation for other resource types beyond firewall rule collections
- Expandable/collapsible sections or separate detail tables

## User Experience

### Current Behavior

When a firewall rule is modified, the main table shows:
```markdown
| 🔄 | allow-http | TCP | 10.0.1.0/24, 10.0.3.0/24 | * | 80 | Allow HTTP traffic from web and API tiers |
```

This only shows the "after" values, so users cannot see what changed.

### New Behavior

When a firewall rule is modified, changed attributes will show both before and after values in the same cell with diff-style prefixes:

```markdown
| 🔄 | `allow-http` | `TCP` | - 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24 | `*` | `80` | - Allow HTTP traffic<br>+ Allow HTTP traffic from web and API tiers |
```

Where:
- The first line shows `- <before_value>` (the value being removed/changed from)
- The second line shows `+ <after_value>` (the value being added/changed to)
- Unchanged attributes (like `Protocols` and `Destination Ports` in this example) show only a single value without any prefix

### Visual Formatting

The template will use HTML line breaks (`<br>`) or markdown line breaks to separate before/after values within the same table cell. The `-` and `+` prefixes provide clear visual indication similar to standard diff notation, making it immediately recognizable which value is being replaced and what the new value will be.

## Success Criteria

- [ ] Modified firewall rules show before and after values for all changed attributes with `-` and `+` prefixes
- [ ] Unchanged attributes in modified rules show only a single value without prefix (not duplicated)
- [ ] Before values are prefixed with `-` and after values are prefixed with `+`
- [ ] Before and after values are visually separated (on separate lines within the cell)
- [ ] The main rule changes table remains a single table (no additional detail sections)
- [ ] Added, removed, and unchanged rules continue to display as they currently do
- [ ] All existing tests for firewall rule rendering pass
- [ ] New tests verify that changed attributes show before/after with proper prefixes and unchanged attributes show single values
- [ ] Documentation ([docs/features.md](docs/features.md) and [docs/features/001-resource-specific-templates/specification.md](docs/features/001-resource-specific-templates/specification.md)) is updated to reflect the new behavior with example output

## Open Questions

None - all requirements are clearly defined.
