# Ignore Azure Resource ID Casing-Only Changes

The Azure Resource Manager API occasionally returns resource IDs with inconsistent casing
(e.g., `/subscriptions/ABC123` vs `/subscriptions/abc123`). This causes Terraform to report
false attribute changes that are not real infrastructure changes. This release suppresses these
noise changes by default.

## ✨ Features

- **`--ignore-azure-id-case-changes` (enabled by default).** Attribute changes where both the before
  and after values are Azure resource IDs (paths starting with `/subscriptions/`,
  `/providers/`, `/tenants/`, or `/managementGroups/`) and differ only in casing are silently
  suppressed from the report. Use `--no-ignore-azure-id-case-changes` to see all changes.

## ▶️ Getting started

```bash
# Azure resource ID casing-only changes are suppressed by default
tfplan2md plan.json > plan.md

# Opt out to see all changes including Azure resource ID casing differences
tfplan2md plan.json --no-ignore-azure-id-case-changes > plan.md
```

## 🔗 Commits

- [`409c463`](https://github.com/oocx/tfplan2md/commit/409c463) feat: add --ignore-azure-id-case-changes flag to suppress Azure resource ID casing noise
