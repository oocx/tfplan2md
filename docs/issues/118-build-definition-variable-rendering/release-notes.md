# Fix: Build Definition Variable Rendering

This release fixes a bug where secret `azuredevops_build_definition` variables caused **all** variable attributes (name, is_secret, allow_override) to be shown as `(sensitive)`. It also connects the fully-implemented-but-unregistered tabular renderer for `azuredevops_build_definition`, giving pipeline variables, triggers, repository settings, and schedules the same structured table display that `azuredevops_variable_group` has had since Feature 027.

## 🐛 Bug Fixes

### Secret variable attributes no longer bleed `(sensitive)`

**Problem:** When a build definition variable had `is_secret = true`, the entire variable block was treated as sensitive by the default renderer. Non-sensitive attributes — `name`, `allow_override`, and `is_secret` itself — were all shown as `(sensitive)`.

**Before (broken output):**

```
variable[0].allow_override    (sensitive)    (sensitive)
variable[0].is_secret         (sensitive)    (sensitive)
variable[0].name              (sensitive)    (sensitive)
variable[0].value             (sensitive)    (sensitive)
```

**After (correct output):**

| Name | Value | Secret | Allow Override |
| ---- | ----- | ------ | -------------- |
| `🆔 API_KEY` | `(sensitive / hidden)` | `✅ true` | `✅ true` |
| `🆔 ENV` | `production` | `❌ false` | `✅ true` |

**Root cause:** Terraform's AzureDevOps provider marks the **entire `variable[N]` object** as sensitive in `after_sensitive` / `before_sensitive` when any attribute within it is a secret. The default renderer's `SensitivityHelper.IsSensitiveAttribute` checked sensitivity hierarchically — when `variable[0]` was marked sensitive as a whole block, all its child attributes inherited that sensitivity flag.

**Fix:** A dedicated `BuildDefinitionRenderer` now reads variable data directly from the `before`/`after` JSON via `BuildDefinitionViewModelFactory`, completely bypassing the hierarchical sensitivity check. Only the `value`/`secret_value` field is explicitly masked for secret variables; `name`, `is_secret`, and `allow_override` always display their actual values.

## ✨ Features

### Tabular rendering for `azuredevops_build_definition`

`azuredevops_build_definition` resources now render in structured tables — variables, CI triggers, pull request triggers, schedules, repository settings, and jobs — matching the rendering style of `azuredevops_variable_group`.

> **Background:** Feature 094 implemented all the underlying infrastructure (`BuildDefinitionViewModelFactory`, `BuildDefinitionFormatters`, view models) but stopped short of registering a dedicated renderer. The `AzureDevOpsDelegatingRenderer` (generic fallback) was left in place. This issue completes Feature 094 by creating and registering `BuildDefinitionRenderer`.

**Table columns — create/delete operations:**

| Name | Value | Secret | Allow Override |
| ---- | ----- | ------ | -------------- |
| `🆔 BUILD_CONFIGURATION` | `Release` | `❌ false` | `✅ true` |
| `🆔 API_KEY` | `(sensitive / hidden)` | `✅ true` | `❌ false` |

**Table columns — update/replace operations:**

| Change | Name | Value | Secret | Allow Override |
| ------ | ---- | ----- | ------ | -------------- |
| ➕ | `🆔 NEW_VAR` | `new-value` | `❌ false` | `❌ false` |
| 🔄 | `🆔 CONFIG` | `- debug`<br>`+ release` | `❌ false` | `- true`<br>`+ false` |
| ❌ | `🆔 OLD_VAR` | `old-value` | `❌ false` | `❌ false` |

**Additional sections (rendered when data is present):**

- **CI Triggers** — Use YAML flag and branch filter overrides
- **Pull Request Triggers** — Use YAML, branch filters, fork support, comment requirement
- **Schedules** — Branch filters, days to build, start time, time zone, changes-only flag
- **Repository** — Type, repo ID, branch, YAML path, build status reporting, service connection
- **Jobs** — Job name, condition, timeout

## 💡 Use Cases

- **Pipeline security review**: Confirm secret variables (`is_secret: true`) are correctly configured without exposing their values
- **Change detection**: See exactly which variables were added, modified, or removed when updating a build definition — previously impossible because all content was masked as `(sensitive)`
- **Trigger configuration review**: Understand which branches and schedules will trigger builds before applying changes
- **Repository configuration review**: Verify the source repository, branch, and YAML file path

## 🔗 Commits

See git log for commits on branch `copilot/add-azuredevops-variable-rendering`.

## 🧪 Test Coverage

- `AzureDevOpsSnapshotTests.Snapshot_AzureDevOps_BuildDefinitions_MatchesBaseline` — verifies full rendering output including secret masking, semantic variable diffing, CI triggers, PR triggers, schedules, and repository tables
- `ProviderResourceRenderersTests.ProviderRenderers_ExposeExpectedResourceTypes` — updated to reference `BuildDefinitionRenderer` directly
- All existing `BuildDefinitionViewModelFactoryTests.*` continue to pass unchanged

## 📚 Related Documentation

- [Feature 094: Azure DevOps Build Definition Tables](../../features/094-build-definition-tables/specification.md) — original infrastructure implementation
- [Issue 093: Sensitive Attribute Disclosure](../093-sensitive-attribute-disclosure/analysis.md) — hierarchical sensitivity check that caused the bleed
- [docs/features.md § Azure DevOps Build Definitions](../../features.md) — updated to reflect correct column names and example output
