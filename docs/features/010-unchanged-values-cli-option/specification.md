# Feature: CLI Option to Control Unchanged Value Display

## Overview

Add a command-line option that allows users to control whether unchanged values are displayed in attribute change tables. This feature reduces noise in plan review by hiding rows where the before and after values are identical, making it easier to quickly identify actual changes.

## User Goals

- **Reduce noise**: Users want to quickly see what has changed in a Terraform plan to verify the plan contains expected changes
- **Improve review efficiency**: Filtering out unchanged values allows users to focus on actual changes without visual clutter
- **Maintain flexibility**: Some users may still want to see all values (changed and unchanged) for completeness or audit purposes

## Scope

### In Scope

- Add a CLI flag to control unchanged value filtering (e.g., `--show-unchanged-values`)
- **Default behavior**: Hide unchanged values (rows where `before == after` are filtered out)
- **Opt-in behavior**: When flag is present, show all rows including unchanged values
- Apply filtering consistently to ALL attribute change tables throughout the tool
- Filtering applies to all templates (default template and any custom templates)

### Out of Scope

- Special handling for specific attribute types (security-sensitive, etc.) - all tables use the same filtering logic
- Filtering based on attribute importance or criticality
- Different filtering rules for different resource types
- Filtering for other table types (not attribute change tables)

## User Experience

### Command-Line Interface

**Default usage (hide unchanged values)**:
```bash
tfplan2md plan.json > report.md
```
Result: Attribute change tables only show rows where the value actually changed.

**Show all values including unchanged**:
```bash
tfplan2md plan.json --show-unchanged-values > report.md
```
Result: Attribute change tables show all rows, even if before and after values are identical.

### Expected Behavior

**Example: Resource update with mixed changes**

Given a resource with these attribute changes:
- `name`: "old-name" → "new-name" (changed)
- `location`: "eastus" → "eastus" (unchanged)
- `sku`: "Standard" → "Premium" (changed)
- `enabled`: true → true (unchanged)

**Default output (unchanged values hidden)**:
| Attribute | Before | After |
|-----------|--------|-------|
| name | "old-name" | "new-name" |
| sku | "Standard" | "Premium" |

**With `--show-unchanged-values` flag**:
| Attribute | Before | After |
|-----------|--------|-------|
| name | "old-name" | "new-name" |
| location | "eastus" | "eastus" |
| sku | "Standard" | "Premium" |
| enabled | true | true |

### Edge Cases

- **All attributes unchanged**: Table should still be rendered with header, but with no data rows (or a message indicating no changes)
- **Nested attributes**: Filtering applies at the individual attribute level
- **Null/empty values**: If both before and after are null/empty, row should be hidden by default
- **Complex values (lists, objects)**: Use same comparison logic as currently used for display

## Success Criteria

- [ ] CLI flag `--show-unchanged-values` is implemented and documented
- [ ] Default behavior hides unchanged values in all attribute change tables
- [ ] Flag enables display of all values including unchanged
- [ ] Filtering logic is consistent across all templates
- [ ] Filtering works correctly for all data types (strings, numbers, booleans, nulls)
- [ ] Template authors can access the flag value to implement filtering in custom templates
- [ ] Help text documents the new flag
- [ ] Behavior is covered by automated tests
- [ ] Documentation (README, usage guides) is updated

## Open Questions

None - requirements are clear and unambiguous.
