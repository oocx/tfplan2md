# Azure DevOps Build Definition Tables

This release adds structured table display for `azuredevops_build_definition` resources, showing pipeline variables, triggers, repository configuration, schedules, and jobs as tables instead of raw attribute lists.

## ✨ Features

- **Variables table**: Displays pipeline variables with Name, Value, Is Secret, and Allow Override columns
  - Secret variables (`is_secret: true`) show `(sensitive / hidden)` for the value — actual secret values are never exposed
  - Semantic diffing for update operations: variables matched by name to show Added/Modified/Removed/Unchanged status
- **CI Trigger table**: Shows continuous integration trigger configuration (Use YAML, branch filter overrides)
- **Pull Request Trigger table**: Shows pull request trigger settings including fork support configuration
- **Schedules table**: Displays scheduled build configuration (branch filters, days to build, start time, time zone)
- **Repository table**: Shows source repository details (type, repo ID, branch, YAML path, build status reporting)
- **Jobs table**: Displays job definitions when present (name, condition, timeout)
- **Conditional rendering**: Tables only appear when the corresponding blocks are present — no empty tables shown

## 💡 Use Cases

- **Pipeline reviews**: Quickly see all pipeline variables and whether they are secrets before applying changes
- **Trigger configuration reviews**: Understand which branches trigger builds and pull request builds
- **Security reviews**: Verify that secret variables are properly configured (`is_secret: true`) without exposing their values
- **Change detection**: See which variables were added, modified, or removed when updating a build definition

## ▶️ Example Output

For a `create` operation with variables:

**Variables**

| Name | Value | Is Secret | Allow Override |
| ---- | ----- | --------- | -------------- |
| `BUILD_CONFIGURATION` | `Release` | `false` | `true` |
| `API_KEY` | `(sensitive / hidden)` | `true` | `false` |

For an `update` operation showing variable changes:

**Variables**

| Change | Name | Value | Is Secret | Allow Override |
| ------ | ---- | ----- | --------- | -------------- |
| ➕ | `NEW_VAR` | `value` | `false` | `true` |
| 🔄 | `EXISTING_VAR` | `` `old` → `new` `` | `false` | `false` |
| ❌ | `REMOVED_VAR` | `old-value` | `false` | `true` |
| ⏺️ | `UNCHANGED_VAR` | `same-value` | `false` | `false` |

## 🔍 How It Works

The feature uses the same architecture pattern as `azuredevops_variable_group`. A dedicated `BuildDefinitionViewModelFactory` extracts and formats the nested block data, and a Scriban template renders the tables conditionally based on whether each block type has data.

Secret variable values are masked at the formatting layer — the `is_secret` flag is checked and `(sensitive / hidden)` is substituted before the value ever reaches the template.

## 📸 Screenshots

Example output showing variables table with secret masking, CI trigger table, and repository table:

![Build Definition Tables](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/094-build-definition-tables/build-definition-tables.png)

## 🔗 Commits

- [`156701a4`](https://github.com/oocx/tfplan2md/commit/156701a427957a93f5e8216a03b090d0a9b168ba) fix: split build_definition.sbn into partial templates to pass line count test
