# Feature: Case-Insensitive Attribute Change Filter

## Overview

The Azure Resource Manager (azurerm) provider occasionally reports resource attribute changes where the before and after values are identical except for letter casing. This is a known quirk of the Azure ARM API, which sometimes returns resource IDs (and occasionally other string attributes) with different capitalization on successive reads. Terraform detects these as changes, and tfplan2md faithfully reports them in the generated report — causing noise for reviewers who need to focus on real infrastructure changes.

This feature introduces a new CLI flag (`--ignore-case-changes`) that, when enabled, suppresses attribute change rows where the before and after values are equal under case-insensitive comparison. The filter is disabled by default to preserve the existing behavior for all users.

## User Goals

- **Eliminate casing noise from Azure plans**: Users working with `azurerm` resources want to review only genuine changes and not be distracted by spurious ID casing differences (e.g., `/subscriptions/ABC…` vs `/subscriptions/abc…`) that have no real infrastructure impact.
- **Opt-in control**: Users who need to audit every string difference, including casing, should not be affected unless they choose otherwise.
- **Consistent application**: The filter should apply uniformly across all attribute change tables in the report, not just for specific resource types.

## Scope

### In Scope

- A new CLI flag `--ignore-case-changes` that enables the case-insensitive attribute change filter.
- When the flag is active, attribute change rows where `before` and `after` are equal under case-insensitive string comparison are suppressed (treated as unchanged and hidden from the table).
- The flag interacts correctly with the existing `--show-unchanged-values` flag: rows suppressed by `--ignore-case-changes` are considered "effectively unchanged" and are **not** shown even when `--show-unchanged-values` is active.
- The filter applies to all attribute change tables produced by any template (built-in and custom).
- Help text documents the new flag.

### Out of Scope

- Locale-aware or Unicode-normalizing case comparison (simple ASCII/ordinal case-insensitive comparison is sufficient).
- Filtering of non-string attribute types (numbers, booleans, objects); only string values are compared case-insensitively.
- Automatic detection of the azurerm provider or Azure resource IDs — the filter is a blanket opt-in applied to all attributes across all providers.
- Suppressing the resource entry itself when all attribute changes are filtered out; the resource still appears in the report, but its attribute change table will have fewer (or no) rows.
- Filtering of values in the plan summary counts (the resource-level summary lines and counts remain unaffected by this flag).

## User Experience

### Command-Line Interface

**Default usage (no change from current behavior)**:
```bash
tfplan2md plan.json > report.md
```
Result: All attribute change rows are included, including rows where before/after differ only in casing.

**Enable case-insensitive filter**:
```bash
tfplan2md plan.json --ignore-case-changes > report.md
```
Result: Attribute change rows where before and after values are equal under case-insensitive comparison are suppressed.

### Expected Behavior

**Example: Azure resource with ID casing difference**

Given a Terraform plan that reports the following changes on an `azurerm_role_assignment`:

| Attribute | Before | After |
|-----------|--------|-------|
| scope | `/subscriptions/ABC123/resourceGroups/my-rg` | `/subscriptions/abc123/resourceGroups/my-rg` |
| role_definition_id | `/providers/Microsoft.Authorization/roleDefinitions/XYZ` | `/providers/Microsoft.Authorization/roleDefinitions/xyz` |
| display_name | `My App` | `My Application` |

**Default output (flag absent — all rows shown)**:

| Attribute | Before | After |
|-----------|--------|-------|
| scope | `/subscriptions/ABC123/resourceGroups/my-rg` | `/subscriptions/abc123/resourceGroups/my-rg` |
| role_definition_id | `/providers/Microsoft.Authorization/roleDefinitions/XYZ` | `/providers/Microsoft.Authorization/roleDefinitions/xyz` |
| display_name | `My App` | `My Application` |

**With `--ignore-case-changes` (casing-only rows suppressed)**:

| Attribute | Before | After |
|-----------|--------|-------|
| display_name | `My App` | `My Application` |

The `scope` and `role_definition_id` rows are suppressed because their before/after values differ only in casing. The `display_name` row is kept because the values differ by more than casing.

### Interaction with `--show-unchanged-values`

When `--ignore-case-changes` is active, rows suppressed by casing are treated as "effectively unchanged" and remain hidden even if `--show-unchanged-values` is also passed. This means `--ignore-case-changes` takes precedence over `--show-unchanged-values` for casing-only rows.

### Edge Cases

- **Non-string values**: Numbers and booleans are not subject to the case-insensitive filter; they are compared as-is.
- **Null values**: If before or after is null, no case-insensitive comparison is performed and the row is shown normally.
- **All rows suppressed**: If all attribute changes in a resource's table are suppressed by this filter, the attribute table shows no rows (consistent with how `--show-unchanged-values` off behaves when all values are unchanged).
- **Mixed resources (some casing-only, some real)**: Only casing-only rows are suppressed; genuine changes remain visible.

## Success Criteria

- [ ] CLI flag `--ignore-case-changes` is implemented and appears in help text.
- [ ] When the flag is absent, report output is identical to current behavior (no regression).
- [ ] When the flag is present, attribute change rows where before and after values are equal under case-insensitive string comparison are suppressed.
- [ ] Non-string attribute values (numbers, booleans, nulls) are not affected by the filter.
- [ ] Rows suppressed by `--ignore-case-changes` remain hidden even when `--show-unchanged-values` is also passed.
- [ ] The filter is applied consistently across all attribute change tables in the report (all resource types, all providers, built-in and custom templates).
- [ ] Template authors can access the flag value to implement the filter in custom templates.
- [ ] Behavior is covered by automated tests, including:
  - A test with only casing-only changes (all rows suppressed).
  - A test with mixed changes (some casing-only, some real).
  - A test confirming no regression when the flag is absent.
  - A test confirming casing-only rows are still hidden when both `--ignore-case-changes` and `--show-unchanged-values` are active.
- [ ] README and usage documentation are updated to describe the new flag.

## Open Questions

None — requirements are clear and unambiguous.
