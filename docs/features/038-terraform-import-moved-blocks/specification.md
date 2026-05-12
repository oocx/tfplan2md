# Feature: Terraform Import and Moved Blocks

## Overview

Add visibility for Terraform `import` and `moved` blocks in generated reports. These blocks are used for refactoring infrastructure code without destroying and recreating resources. Users need to see which resources are being imported from existing infrastructure or moved within their configuration, and the report must avoid false-positive "already imported" warnings for pending imports.

## User Goals

- Quickly identify which resources are being imported from existing infrastructure or moved to different addresses
- Understand the scope of refactoring operations in a plan at a glance
- Be warned when moved blocks remain in the configuration after they've been applied, without incorrectly marking pending imports as already imported
- See import/moved annotations inline with each affected resource for context

## Scope

### In Scope

1. **Refactoring Summary Table**
   - Display a new "Refactoring Summary" section at the end of the report (after all resource changes)
   - Only appears when the plan contains import or moved operations
   - Lists all imported and moved resources
   - Shows warnings for already-applied moves and keeps pending imports marked as ready

2. **Resource-Level Annotations**
   - Add "imported" or "moved from `<previous_address>`" annotations to the summary line of each affected resource
   - Annotations appear in the `<summary>` element alongside other resource context

3. **Unnecessary Block Detection**
   - Detect already-applied moved blocks from plan metadata
   - Keep pending imports with `importing.id` visible without inferring `already imported` from `no-op` alone

### Out of Scope

- Generating or recommending import/moved blocks
- Validating import IDs or moved addresses
- Showing import/moved block syntax or HCL code
- Filtering or grouping resources by import/moved status
- Historical tracking of imports/moves across multiple plans

## User Experience

### 1. Refactoring Summary Table

When import or moved operations exist, a new section appears at the end of the report:

```markdown
## Refactoring Summary

| Operation | Resource | Details | Status |
|-----------|----------|---------|--------|
| 📥 Import | azurerm_resource_group `existing-rg` | ID: `i-1234567890abcdef0` | ✅ Ready |
| 🔀 Move | azurerm_virtual_network `hub` | From: `module.old.azurerm_virtual_network.hub` | ✅ Ready |
| 🔀 Move | azurerm_subnet `legacy-subnet` | From: `module.old.azurerm_subnet.legacy-subnet` | ⚠️ Already moved |
```

**Column Definitions:**
- **Operation**: Icon and type (Import or Move)
- **Resource**: Resource type and name (code-formatted)
- **Details**: Import ID or previous address (code-formatted)
- **Status**: 
  - ✅ Ready - will be applied in this plan
  - ⚠️ Already moved - the move has already been applied and the block can be removed

### 2. Resource-Level Annotations

Each resource's summary line includes import/moved context:

**Imported Resource:**
```html
<summary>➕ azurerm_resource_group <b><code>existing-rg</code></b> — 📥 <i>Imported</i> | <code>🆔 rg-existing</code> <code>🌍 eastus</code></summary>
```

**Moved Resource:**
```html
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — 🔀 <i>Moved from</i> <code>module.old.azurerm_virtual_network.hub</code> | <code>🆔 vnet-hub</code> <code>🌍 eastus</code></summary>
```

**Already-applied Move (no-op):**
```html
<summary>⚪ azurerm_subnet <b><code>legacy-subnet</code></b> — 🔀 <i>Moved from</i> <code>module.old.azurerm_subnet.legacy-subnet</code> (⚠️ <i>already moved</i>)</summary>
```

### 3. Refactoring Summary Section Behavior

- **Appears only when**: At least one import or moved operation exists in the plan
- **Location**: After all resource changes, before any footer/metadata
- **Sorting**: 
  - Imports first, then moves
  - Within each group, sort alphabetically by resource address
  - Warnings (unnecessary blocks) highlighted at the top of each group

## Success Criteria

- [ ] Refactoring Summary table appears when import or moved operations exist
- [ ] Refactoring Summary does not appear when no import/moved operations exist
- [ ] Each imported resource shows "📥 Imported" annotation in its summary line
- [ ] Each moved resource shows "🔀 Moved from `<previous_address>`" in its summary line
- [ ] Import operations show the import ID in the Refactoring Summary
- [ ] Move operations show the previous address in the Refactoring Summary
- [ ] Already-applied moved resources show warning status, while pending imports remain `✅ Ready`
- [ ] Warnings clearly indicate blocks can be removed from configuration
- [ ] All code values (resource names, IDs, addresses) follow report style guide formatting
- [ ] Icons use non-breaking spaces to prevent line wrapping
- [ ] Resource summary lines without import/moved annotations render exactly as before this feature

## Open Questions

None - requirements are clear.

## Data Source

Based on Terraform plan JSON format (format_version 1.0+):

**Import detection:**
- Check `resource_changes[].change.importing` field
- Contains `{ "id": "<import-id>" }` for imported resources

**Moved detection:**
- Check `resource_changes[].previous_address` field
- Contains previous resource address string for moved resources

**Already-applied detection:**
- Moves use `resource_changes[].previous_address` together with `resource_changes[].change.actions = ["no-op"]`
- Imports use `resource_changes[].change.importing.id` for visibility, but `["no-op"]` alone is not treated as proof that the import was already applied
