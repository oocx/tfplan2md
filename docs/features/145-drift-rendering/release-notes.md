# Configurable grouped drift rendering

tfplan2md now turns Terraform drift into a concise, grouped review section instead of
repeating the same transition for every affected resource.

## ✨ Features

- Drift entries with the same resource type, attribute path, before value, and after
  value are grouped in a collapsible entry with their resource addresses listed below.
- The new `--drift` option controls visibility: `all` (the default), `relevant` (only
  resources with visible planned changes), or `none`.
- `relevant` excludes Terraform no-op changes, and drift entries with different value
  transitions remain separate rather than being combined.

## 📸 Screenshot

<!-- release-screenshot: selector="h2 + details"; focus="Shows one expanded grouped drift transition with both affected resource addresses." -->
![Grouped drift transition](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/145-drift-rendering/drift-rendering.png)

## ▶️ Getting started

```bash
# Show all grouped drift entries (the default)
tfplan2md plan.json --drift all

# Keep only drift for resources with displayed planned changes
tfplan2md plan.json --drift relevant

# Omit the drift section
tfplan2md plan.json --drift none
```

## 🔗 Commits

- [`3ed79bf7`](https://github.com/oocx/tfplan2md/commit/3ed79bf7) feat: add configurable grouped drift rendering
- [`6b95e9b5`](https://github.com/oocx/tfplan2md/commit/6b95e9b5) fix: escape line breaks in grouped drift details
- [`ccf59c04`](https://github.com/oocx/tfplan2md/commit/ccf59c04) fix: exclude unchanged attributes from drift groups
