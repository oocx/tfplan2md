# Feature: Summary Resource Type Breakdown

## Overview

Enhance the summary table in Terraform plan reports to include a detailed breakdown of resource types for each action. This helps users quickly understand not just how many resources will change, but what types of resources are affected.

## User Goals

- Quickly identify which resource types are being added, changed, replaced, or destroyed without reading through the entire detailed changes section
- Understand the distribution of changes across different resource types at a glance
- Make faster decisions about plan approval based on resource type patterns

## Scope

### In Scope
- Add a new "Resource Types" column to the existing summary table
- Display resource type breakdown for each action row (Add, Change, Replace, Destroy)
- Show count and full resource type name (e.g., "3 azurerm_storage_account")
- Present each resource type on its own line within the table cell
- Sort resource types alphabetically within each action
- Handle empty cases gracefully (no resources for a particular action)

### Out of Scope
- Changing the format of the detailed changes section
- Adding filtering or grouping by resource type
- Showing resource type breakdown in the Total row
- Interactive features (collapsible sections, etc.)
- Resource type abbreviations or shortened names

## User Experience

The summary table will be enhanced with a third column:

```markdown
| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | 1 azurerm_resource_group<br/>3 azurerm_storage_account<br/>2 azurerm_virtual_network |
| 🔄 Change | 3 | 2 azurerm_app_service<br/>1 azurerm_sql_database |
| ♻️ Replace | 1 | 1 azurerm_kubernetes_cluster |
| ❌ Destroy | 0 | |
| **Total** | **10** | |
```

**Formatting rules:**
- Each resource type appears on its own line using HTML `<br/>` tags
- Format: `<count> <full_resource_type>`
- Resource types sorted alphabetically within each action
- Empty cell when count is 0
- Total row shows no resource type breakdown

**Behavior:**
- Works with existing templates (updates default template)
- No new command-line flags required
- No change to JSON parsing or data model beyond what's needed for rendering

## Success Criteria

- [ ] Summary table includes "Resource Types" column in default template
- [ ] Each action row displays correct count and type name for all resources
- [ ] Resource types are sorted alphabetically within each action
- [ ] Multi-line display works correctly in markdown renderers (GitHub, Azure DevOps, etc.)
- [ ] Empty cells display correctly when an action has 0 resources
- [ ] Total row's Resource Types column remains empty
- [ ] Existing tests pass without modification (no breaking changes)
- [ ] New behavior is covered by tests

## Open Questions

None - all requirements are clear and approved.
