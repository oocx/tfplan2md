# Summary table now shows imports/moves and hides empty rows

The Summary table at the top of every report now surfaces Terraform `import` and `moved` blocks as their own rows, hides any action row whose count is `0`, and falls back to a clean "No changes" message when no row has any content. Reports stay focused on what actually changes.

## ✨ Enhancements

### Imports and moves are now first-class summary rows

`📥 Import` and `🔀 Move` rows are added to the Summary table whenever the plan contains resources brought in via `import` blocks or relocated via `moved` blocks. Each row uses the same `Count` and `Resource Types` breakdown as the existing action rows, so refactoring activity is visible at a glance without scrolling to the per-resource details.

```markdown
## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 2 | 1 azurerm_resource_group<br/>1 azurerm_virtual_network |
| 📥 Import | 2 | 1 azurerm_resource_group<br/>1 azurerm_storage_account |
| 🔀 Move | 1 | 1 azurerm_virtual_network |
| **Total** | **2** | |
```

Imports and moves are reported alongside the action buckets (a single change can appear in both an action row and an `Import`/`Move` row). The `Total` still reflects only resource changes (add + change + replace + destroy), so the existing Azure DevOps `tfplan2md_haschanges` pipeline variable behavior is unchanged.

### Zero-count rows are hidden

Rows whose count is `0` are no longer rendered, so a create-only plan now shows just the `Add` row instead of three empty `Change`, `Replace`, and `Destroy` rows. This keeps the summary tight and matches the new behavior for `Import`/`Move` rows (they only appear when there is something to report).

**Before:**

```markdown
| ➕ Add | 2 | 1 azurerm_resource_group<br/>1 azurerm_storage_account |
| 🔄 Change | 0 |  |
| ♻️ Replace | 0 |  |
| ❌ Destroy | 0 |  |
| **Total** | **2** | |
```

**After:**

```markdown
| ➕ Add | 2 | 1 azurerm_resource_group<br/>1 azurerm_storage_account |
| **Total** | **2** | |
```

### "No changes" replaces the empty table

When every row would be hidden — including the new `Import`/`Move` rows — the renderer keeps the existing "No changes" paragraph in place of an empty table.

## 📚 Documentation

- `docs/features.md` replaces the old "No-Changes Summary" section with a new "Concise Summary Table" section that documents both the import/move rows and the zero-count filtering.

## 🔗 Commits

- [`ed654f1`](https://github.com/oocx/tfplan2md/commit/ed654f1) feat: add imports/moves to summary table and hide zero-count rows
- [`64a34c4`](https://github.com/oocx/tfplan2md/commit/64a34c4) docs: clarify imports/moves are orthogonal to action buckets
