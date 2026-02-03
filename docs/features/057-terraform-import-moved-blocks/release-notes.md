# Terraform Import and Moved Blocks Visibility

## Overview

Refactoring Terraform configurations often involves importing existing resources or moving them to new addresses without destroying and recreating them. Previously, tfplan2md didn't surface these operations clearly in reports, making it hard to understand the scope of refactoring work at a glance.

Now, tfplan2md provides full visibility into Terraform `import` and `moved` blocks with a dedicated Refactoring Summary section and inline annotations. You'll immediately see which resources are being imported or moved, and get warnings about unnecessary blocks that can be removed from your configuration.

## What's New

### Refactoring Summary Section

When your plan includes import or moved operations, a new "Refactoring Summary" table appears at the end of the report:

```markdown
## Refactoring Summary

| Operation | Resource | Details | Status |
|-----------|----------|---------|--------|
| Import | azurerm_storage_account.example | Importing existing resource | ✅ Ready |
| Moved | azurerm_resource_group.old → azurerm_resource_group.new | Renaming resource | ✅ Ready |
| Import | azurerm_virtual_network.legacy | Already imported | ⚠️ Unnecessary block |
```

This summary shows:
- **Operation type**: Import or Moved
- **Resource address**: The resource being affected
- **Details**: What's happening (import source, move destination, or warning)
- **Status**: Whether the operation is needed or can be removed

### Inline Resource Annotations

Each imported or moved resource now shows its refactoring status directly in the resource summary line:

**Imported resource:**
```markdown
<summary>azurerm_storage_account.data (imported)</summary>
```

**Moved resource:**
```markdown
<summary>azurerm_resource_group.main (moved from module.old.azurerm_resource_group.main)</summary>
```

These annotations appear alongside other resource context, making it easy to spot refactoring operations while reviewing individual resources.

### Unnecessary Block Warnings

tfplan2md now detects when import or moved blocks reference resources that have already been imported or moved (no-op actions). The Refactoring Summary flags these with a warning status, helping you clean up your configuration:

```markdown
| Import | azurerm_key_vault.existing | Already imported | ⚠️ Unnecessary block |
```

These warnings indicate blocks that can be safely removed from your Terraform configuration since they've already been applied.

## Why This Matters

**Better Refactoring Visibility**: Understand exactly which resources are being imported or reorganized without digging through plan JSON.

**Catch Configuration Cleanup**: Warnings help you identify and remove import/moved blocks that are no longer needed, keeping your configuration clean.

**Inline Context**: See refactoring annotations right where you need them - next to each affected resource.

## Getting Started

No changes needed to your workflow - just run tfplan2md as usual:

```bash
terraform plan -out=plan.tfplan
terraform show -json plan.tfplan | tfplan2md > plan.md
```

If your plan includes any import or moved blocks, you'll automatically see the new Refactoring Summary section and inline annotations.

## Example

See the [demo artifacts](demo/) for complete examples showing:
- Import operations for existing infrastructure
- Moved operations for resource refactoring
- Warnings for unnecessary blocks

## Learn More

- [Terraform Import Documentation](https://www.terraform.io/docs/cli/import/index.html)
- [Terraform Moved Blocks](https://www.terraform.io/language/modules/develop/refactoring)
