# Terraform 1.14 / 1.15 Plan-JSON Support

tfplan2md now renders the new fields introduced by Terraform 1.14 and 1.15 — action invocations, plan-context status, drift detection, relevant attributes, and deprecation warnings — all without any change to your CLI invocation. Plans from Terraform 1.13 and earlier are unaffected.

## ✨ Features

### 🎬 Action invocations (Terraform 1.14)

Terraform 1.14 adds `action_invocations[]` and `deferred_action_invocations[]` to the plan JSON, produced by provider-shipped Actions (e.g. `aws_lambda_invoke`, `aws_cloudfront_create_invalidation`) and by `lifecycle { action_trigger { … } }` blocks. These are now rendered inline under the resource that triggers them, using an **`#### 🎬 Actions`** H4 section inside each resource's collapsible block. Deferred actions are prefixed `⏳` and include a "will run on a subsequent apply" callout. Actions with no matching resource section (orphan/invoke-mode) appear in a new top-level **`## 🎬 Other Actions`** section.

![Inline action invocations under a resource block](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/122-terraform-1-15-support/feature-122-actions.png)

### 🌀 Drift detection (Terraform 1.14)

When `resource_drift[]` is non-empty — meaning Terraform detected that real-world state diverged from what the state file recorded — a **`## 🌀 Drift Detected`** section now appears in the report, listing each drifted resource in the same collapsible before/after format used for resource changes.

![Drift Detected section](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/122-terraform-1-15-support/feature-122-drift.png)

### 🛑 Plan status banners (Terraform 1.14)

The plan booleans `errored`, `applyable`, and `complete` are now read. When any of them signals a non-ordinary state, a blockquote banner is injected immediately after the report title:

- 🛑 **Plan errored** — when `errored: true`
- ⛔ **Plan is not applyable** — when `applyable: false`
- ⚠️ **Plan is incomplete** — when `complete: false`

All three can appear together.

![Plan status banners for an errored, non-applyable, incomplete plan](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/122-terraform-1-15-support/feature-122-status-banner.png)

### 📋 Relevant attributes (Terraform 1.14)

When `relevant_attributes[]` is present, a **`## Relevant Attributes`** section is rendered with a two-column `Resource | Attribute path` table, making upstream dependencies that triggered cascading changes visible to reviewers.

![Relevant Attributes table](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/122-terraform-1-15-support/feature-122-relevant-attrs.png)

### ⚠️ Deprecation warnings (Terraform 1.15)

Terraform 1.15 adds a `deprecated` string to `configuration.root_module.variables[*]` and `configuration.root_module.outputs[*]`. Any variable or output that is both deprecated **and referenced** by the plan now produces a warning entry in the `### Warnings` section of the Code Analysis Summary, showing the name and the deprecation message from the Terraform configuration.

![Deprecation warning for a deprecated variable](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/122-terraform-1-15-support/feature-122-deprecation.png)

### Backwards compatibility

All five rendering additions are strictly additive. Plans produced by Terraform 1.13 and earlier (which omit these fields) render byte-for-byte identically to the previous version.

## 🐛 Bug fixes

- **Deprecation warnings silently dropped when no SARIF scan findings were present.** `CodeAnalysisSectionRenderer` returned early with "No code analysis findings were reported" whenever the SARIF total count was zero, even when deprecation warnings existed. Fix: the early-return now requires both SARIF count and warnings count to be zero; the SARIF severity table is only rendered when there are actual SARIF findings.

## 📸 Screenshots

See inline screenshots above for each feature area.

## 🔗 Commits

- [`60aee150`](https://github.com/oocx/tfplan2md/commit/60aee150) feat(parsing): add Terraform 1.14/1.15 action and relevant-attribute records
- [`d3a1765a`](https://github.com/oocx/tfplan2md/commit/d3a1765a) feat(parsing): extend TerraformPlan with optional Terraform 1.14/1.15 fields
- [`d0f4820d`](https://github.com/oocx/tfplan2md/commit/d0f4820d) feat(parsing): add ConfigurationDeprecationReader helper
- [`3f7e96ff`](https://github.com/oocx/tfplan2md/commit/3f7e96ff) feat(features/122): plan-status banner, drift, relevant attrs, actions, deprecations (Tasks 5-13)
- [`e81027fd`](https://github.com/oocx/tfplan2md/commit/e81027fd) fix(feature/122): fix deprecation warnings silently dropped when no SARIF findings; add sealed to PlanStatusModel/RelevantAttributeModel
