# Azure DevOps Pipeline Variable: `tfplan2md_haschanges`

When you run tfplan2md in an Azure DevOps pipeline and write the report to a file, you now
automatically get a pipeline variable `tfplan2md_haschanges` set to `true` or `false` — so
downstream steps can gate on whether the Terraform plan actually contains changes.

## ✨ Features

- **New Azure DevOps pipeline variable** — When `--render-target azuredevops` (the default)
  and `--output <file>` are both active, tfplan2md writes
  `##vso[task.setvariable variable=tfplan2md_haschanges]true` or `…false` to stdout after the
  report is written.

- **Filter-aware** — The `haschanges` value reflects the effective change count after any
  filters (e.g. `--ignore-azure-id-case-changes`). A plan where all resources were filtered
  out correctly emits `false`, not `true`.

- **GitHub render target unaffected** — Running with `--render-target github` suppresses the
  variable emission entirely, so GitHub Actions pipelines see no extra stdout noise.

## ▶️ Getting started

```bash
# In an Azure DevOps pipeline step:
tfplan2md plan.json --output plan.md

# The variable is now set; use it in a downstream step:
# condition: eq(variables['tfplan2md_haschanges'], 'true')
```

Full Azure DevOps YAML example:

```yaml
- script: tfplan2md plan.json --output plan.md
  name: tfplan2md

- task: SomeApprovalTask@1
  condition: eq(variables['tfplan2md_haschanges'], 'true')
  displayName: 'Approve Terraform changes'
```

## 🔗 Commits

- [`c991e28`](https://github.com/oocx/tfplan2md/commit/c991e2839fc927b6a9802102df0924f79ea1b5a9) feat: emit Azure DevOps pipeline variable tfplan2md_haschanges
- [`93c8ec7`](https://github.com/oocx/tfplan2md/commit/93c8ec7bb50e2bdd48d8185780a9912a31f3e5ae) feat: suppress haschanges variable when no output file specified
- [`12df0e3`](https://github.com/oocx/tfplan2md/commit/12df0e354b5912e6dff825057d32ef0a0c4c49c4) feat: add casing-filter test, feature comment, and help text update
