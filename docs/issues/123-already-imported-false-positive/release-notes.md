# Pending import no-op status fix (Issue 123)

This bug-fix release resolves a false-positive status in Terraform refactoring output. Scope is limited to import-status classification and related rendered report output.

## 🐛 Bug fixes

- Fixed pending `importing.id + no-op` resources being mislabeled as already imported.
- Pending imports now render as `✅ Ready` while keeping existing moved-resource behavior unchanged.

## 📸 Screenshots

### After
![Pending imports now render as Ready](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/issues/123-already-imported-false-positive/issue-123-import-ready.png)

## 🔗 Commits

- [`a93d7ea0`](https://github.com/oocx/tfplan2md/commit/a93d7ea0) Fix false “already imported” status for pending Terraform imports
