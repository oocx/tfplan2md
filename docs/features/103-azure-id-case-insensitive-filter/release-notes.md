# Ignore Azure Resource ID Casing-Only Changes

The Azure Resource Manager API occasionally returns resource IDs with inconsistent casing
(e.g., `/subscriptions/ABC123` vs `/subscriptions/abc123`). This causes Terraform to report
false attribute changes that are not real infrastructure changes. This release adds an opt-in
flag to suppress these noise changes.

## ✨ Features

- **New `--ignore-azure-id-case-changes` flag.** When enabled, attribute changes where both the before
  and after values are Azure resource IDs (paths starting with `/subscriptions/`,
  `/providers/`, `/tenants/`, or `/managementGroups/`) and differ only in casing are silently
  suppressed from the report. The flag is disabled by default to avoid any change in existing
  behaviour.

## ▶️ Getting started

```bash
# Suppress Azure resource ID casing-only changes
tfplan2md plan.json --ignore-azure-id-case-changes > plan.md
```

## 🔗 Commits

- [`409c463`](https://github.com/oocx/tfplan2md/commit/409c463) feat: add --ignore-azure-id-case-changes flag to suppress Azure resource ID casing noise
