# Fix: False positive "Already imported" warning for resources with read actions

This release fixes a bug where Terraform import operations with `actions: ["read"]` incorrectly displayed the "⚠️ Already imported" warning, even though the import had not yet been executed.

## 🐛 Bug fixes

- **Fixed false positive "⚠️ Already imported" warning** for Terraform import operations that use the `"read"` action. The tool now correctly recognizes `"read"` as a distinct action type and displays "✅ Ready" status instead of incorrectly marking the import as already applied.

## 📋 Impact

**Who was affected:** Users performing Terraform import operations where Terraform reports `actions: ["read"]` for the resource being imported (common when importing resources that need to be read from the provider).

**When it occurred:** When running `tfplan2md` on a plan containing import blocks that Terraform marked with `actions: ["read"]`.

**Symptom:** The Refactoring Summary table incorrectly showed "⚠️ Already imported" status for resources that were actively being imported, causing confusion about whether the import block was necessary.

## ✅ What now works correctly

- Import operations with `actions: ["read"]` now show **✅ Ready** status in the Refactoring Summary
- The "⚠️ Already imported" warning is now only displayed for true no-op imports where `actions: ["no-op"]`
- Resource summary lines correctly show "📥 Imported" without the false warning annotation
- The tool properly distinguishes between:
  - `["read"]` - Import will be executed ✅ Ready
  - `["create"]` - Import will be executed ✅ Ready
  - `["update"]` - Import will be executed ✅ Ready
  - `["no-op"]` - Import already applied ⚠️ Already imported

## 🔗 Commits

- [`1c95d3b`](https://github.com/oocx/tfplan2md/commit/1c95d3b) fix: handle Terraform 'read' action to prevent false 'Already imported' warnings
