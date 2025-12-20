# Feature: Built-in Templates

## Status

✅ **Implemented** - All requirements completed and tests passing.

## Overview

Add support for multiple built-in templates that users can easily select via the `--template` option. This feature introduces a "summary" built-in template optimized for brief notifications (e.g., Teams messages for drift detection alerts) and establishes a pattern for adding more built-in templates in the future.

## User Goals

- **Concise notifications**: Users running automated drift detection pipelines need a compact summary format suitable for sending to Teams channels or other notification systems, where the full plan would be too verbose
- **Easy selection**: Users want to select built-in templates by name without needing to know file paths or manage template files
- **Template discovery**: Users need a way to discover which built-in templates are available

## Scope

### In Scope

1. **New "summary" built-in template** containing:
   - Terraform version
   - Plan timestamp (when the plan was generated)
   - Summary table with action counts and resource type breakdown

2. **Enhanced template resolution** for `--template` option:
   - Check for known built-in template names first
   - If not a built-in, attempt to load as a file path
   - If neither found, display error listing available built-in templates

3. **Timestamp parsing**: Add support for parsing the `timestamp` field from Terraform plan JSON and making it available to templates

4. **Help text updates**: Include information about available built-in templates in `--help` output

5. **Extensibility**: Design to accommodate additional built-in templates in the future

### Out of Scope

- Modifying the existing "default" template behavior when no `--template` is specified
- Adding other new built-in templates beyond "summary" (though the infrastructure will support them)
- Allowing users to override built-in templates with custom files
- Template validation or linting

## User Experience

### Selecting Built-in Templates

Users select the summary template by name:

```bash
# Using Docker
terraform show -json plan.tfplan | docker run -i oocx/tfplan2md --template summary

# Local execution
tfplan2md plan.json --template summary
```

### Using Custom Templates (existing behavior, unchanged)

Custom template files still work as before:

```bash
tfplan2md plan.json --template /path/to/my-template.sbn
```

### Template Resolution Behavior

1. **Built-in name**: `--template summary` → uses built-in summary template
2. **Built-in name**: `--template default` → uses built-in default template  
3. **File path**: `--template ./custom.sbn` → loads custom.sbn from filesystem
4. **Unknown**: `--template unknown` → tries to load file "unknown", fails, shows error with available built-ins

### Error Handling

When template cannot be resolved:

```
Error: Template 'unknown' not found.

Available built-in templates:
  - default: Full report with all resource changes
  - summary: Compact summary with action counts only

Use --template <name> to select a built-in template, or provide a path to a custom template file.
```

### Help Output

The `--help` text should include information about built-in templates:

```
--template, -t <name|file>
    Select a built-in template by name, or use a custom Scriban template file.
    
    Built-in templates:
      default  - Full report with all resource changes (used when not specified)
      summary  - Compact summary with action counts only
    
    Example: --template summary
    Example: --template /path/to/custom.sbn
```

### Summary Template Output Example

```markdown
# Terraform Plan Summary

**Terraform Version:** 1.14.0  
**Generated:** 2025-12-20T10:30:45Z

## Summary

| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 3 | 1 azurerm_resource_group<br/>2 azurerm_storage_account |
| 🔄 Change | 1 | 1 azurerm_key_vault |
| ♻️ Replace | 0 | |
| ❌ Destroy | 0 | |
| **Total** | **4** | |
```

## Success Criteria

- [ ] Users can select the summary template with `--template summary`
- [ ] Summary template outputs only: Terraform version, timestamp, and summary table
- [ ] Timestamp is parsed from Terraform plan JSON and displayed in the summary template
- [ ] Template resolution tries built-in names first, then file paths
- [ ] When template not found, error message lists available built-in templates
- [ ] `--help` output documents available built-in templates
- [ ] Existing custom template file functionality continues to work
- [ ] Default template behavior (when no `--template` specified) is unchanged
- [ ] Code structure supports easily adding more built-in templates in the future

## Open Questions

None - all requirements clarified.
