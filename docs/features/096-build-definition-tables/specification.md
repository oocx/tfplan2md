# Feature: Azure DevOps Build Definition Nested Block Tables

## Overview

Create specialized table display for Azure DevOps Build Definition (`azuredevops_build_definition`) nested blocks including variables, CI triggers, pull request triggers, schedules, repository configuration, and jobs. This feature follows the same pattern established by `azuredevops_variable_group` (Feature 027), transforming opaque "sensitive block" messages into clear, structured tables that show metadata while protecting secret values.

Currently, when build definition resources are included in Terraform plans, nested blocks (especially `variable` blocks containing secret values) display as "At least one attribute in this block is (or was) sensitive, so its contents will not be displayed." This makes it impossible to understand what pipeline configuration changes will be applied, forcing users to either apply blindly or manually inspect the Terraform state.

## User Goals

- Users need to see which pipeline variables are being added, modified, or removed in a build definition
- Users want to understand what specific values are changing for regular variables during updates
- Users need to verify pipeline configuration changes (triggers, repository settings, schedules) in CI/CD workflows before applying Terraform plans
- Users want secret variable values to remain hidden for security while still seeing variable metadata (name, is_secret flag, allow_override, etc.)
- Users need to see CI trigger, pull request trigger, schedule, and repository configuration details when these blocks are present
- Users need this information presented clearly in Azure DevOps pipeline comments and GitHub PR reviews

## Scope

### In Scope

- Specialized template for `azuredevops_build_definition` resource type
- Display build definition metadata (name, path, project_id, queue_status, agent_pool_name)
- Show all **variables** in a table format with columns: Name, Value, Is Secret, Allow Override
  - For secret variables (`is_secret: true`): display all metadata but show `(sensitive / hidden)` in the Value column instead of the actual value or secret_value
  - Categorize variables in update scenarios as: Added (➕), Modified (🔄), Removed (❌), or Unchanged (⏺️)
  - Match variables by `name` attribute for semantic diffing
  - For modified variables: show before/after values with `-` and `+` prefixes for changed attributes
  - For modified variables: show single value without prefix for unchanged attributes
- Display **CI trigger** configuration (`ci_trigger` block) as a table showing: use_yaml, override (list of branch filters)
- Display **Pull Request trigger** configuration (`pull_request_trigger` block) as a table showing: use_yaml, override, forks (enabled/comment)
- Display **Schedules** (`schedules` block) as a table showing: branch filters, days to build, schedule only with changes, start hours, start minutes, time zone
- Display **Repository** configuration (`repository` block) as a table showing: repo_type, repo_id, branch_name, yml_path, report_build_status
- Display **Jobs** (`jobs` block) as a table if present (though typically empty when using YAML pipelines)
- Support create, update, and delete operations with appropriate display formats
- Handle large variable values using the existing large value display mechanism (values over 100 characters or multi-line) - regular variables only
- Only display tables for blocks that actually have data (conditional rendering)

### Out of Scope

- Displaying the actual values of secret variables (security requirement - show `(sensitive / hidden)` instead)
- Build definition permissions or access control details
- Detailed job configuration (if jobs array is populated) - just show basic metadata
- Custom sorting options beyond the natural order from Terraform
- Expandable/collapsible sections per variable (maintain single-table format)
- Build completion triggers (`build_completion_trigger` block) - defer to future enhancement
- Features array - display as standard attribute
- Variable groups array - display as standard attribute (IDs only)

## User Experience

### Current Behavior (Default Template)

When a build definition is modified, users see completely opaque messages for nested blocks:

```markdown
### 🔄 azuredevops_build_definition.example

| Attribute | Before | After |
|-----------|--------|-------|
| name | `example-pipeline` | `example-pipeline` |
| path | `\\Pipelines` | `\\Pipelines` |

- variable {
    # At least one attribute in this block is (or was) sensitive,
    # so its contents will not be displayed.
  }
- variable {
    # At least one attribute in this block is (or was) sensitive,
    # so its contents will not be displayed.
  }
+ variable {
    # At least one attribute in this block is (or was) sensitive,
    # so its contents will not be displayed.
  }
- ci_trigger {
    # At least one attribute in this block is (or was) sensitive,
    # so its contents will not be displayed.
  }
```

This makes it **impossible** to understand what pipeline configuration is changing.

### New Behavior (Build Definition Template)

With the specialized template, the same changes are rendered with full transparency for all metadata:

```markdown
<details open>
<summary>🔄 azuredevops_build_definition <b><code>example</code></b> — <code>example-pipeline</code> | 3 🔧 variables</summary>
<br>

**Pipeline Name:** `example-pipeline`

**Path:** `\\Pipelines`

**Agent Pool:** `Azure Pipelines`

#### Variables

| Change | Name | Value | Is Secret | Allow Override |
| ------ | ---- | ----- | --------- | -------------- |
| ➕ | `NEW_VARIABLE` | `new-value` | `false` | `true` |
| 🔄 | `BUILD_CONFIGURATION` | - `Debug`<br>+ `Release` | `false` | `true` |
| 🔄 | `API_KEY` | `(sensitive / hidden)` | - `false`<br>+ `true` | `true` |
| ❌ | `OLD_VARIABLE` | `old-value` | `false` | `true` |

#### CI Trigger

| Use YAML | Override (Branch Filters) |
| -------- | ------------------------- |
| `true` | - |

#### Repository

| Type | Repo ID | Branch | YAML Path | Report Build Status |
| ---- | ------- | ------ | --------- | ------------------- |
| `TfsGit` | `80128bc2-17ff-45f8-ad59-d7609a605c75` | `refs/heads/master` | `azure-pipelines.yml` | `true` |

</details>
```

Note how the `API_KEY` variable shows its metadata (name, is_secret flag, allow_override) but displays `(sensitive / hidden)` in the Value column instead of the actual secret value.

### Handling Large Variable Values

If a variable value exceeds 100 characters or contains line breaks, it will be moved to the large values section:

```markdown
<details open>
<summary>🔄 azuredevops_build_definition <b><code>example</code></b> — <code>example-pipeline</code> | 1 🔧 variables</summary>
<br>

**Pipeline Name:** `example-pipeline`

#### Variables

| Change | Name | Is Secret | Allow Override |
| ------ | ---- | --------- | -------------- |
| 🔄 | `CONNECTION_STRING` | `false` | `true` |

<details>
<summary>Large values: CONNECTION_STRING (2 lines, 1 changed)</summary>

### `CONNECTION_STRING`

**Before:**
```
Server=tcp:db-old.database.windows.net,1433;Database=mydb;
```

**After:**
```
Server=tcp:db-new.database.windows.net,1433;Database=mydb;
```

</details>

</details>
```

### Create Operation

For create operations, show simpler tables without the Change column:

```markdown
<details open>
<summary>➕ azuredevops_build_definition <b><code>example</code></b> — <code>example-pipeline</code></summary>
<br>

**Pipeline Name:** `example-pipeline`

**Path:** `\\Pipelines`

**Agent Pool:** `Azure Pipelines`

#### Variables

| Name | Value | Is Secret | Allow Override |
| ---- | ----- | --------- | -------------- |
| `BUILD_CONFIGURATION` | `Release` | `false` | `true` |
| `BUILD_PLATFORM` | `Any CPU` | `false` | `true` |
| `API_TOKEN` | `(sensitive / hidden)` | `true` | `true` |

#### CI Trigger

| Use YAML | Override (Branch Filters) |
| -------- | ------------------------- |
| `true` | - |

#### Repository

| Type | Repo ID | Branch | YAML Path | Report Build Status |
| ---- | ------- | ------ | --------- | ------------------- |
| `TfsGit` | `80128bc2-17ff-45f8-ad59-d7609a605c75` | `refs/heads/master` | `azure-pipelines.yml` | `true` |

</details>
```

### Delete Operation

For delete operations, show the configuration being deleted:

```markdown
<details open>
<summary>❌ azuredevops_build_definition <b><code>example</code></b> — <code>example-pipeline</code></summary>
<br>

**Pipeline Name:** `example-pipeline`

**Path:** `\\Pipelines`

#### Variables (being deleted)

| Name | Value | Is Secret | Allow Override |
| ---- | ----- | --------- | -------------- |
| `BUILD_CONFIGURATION` | `Release` | `false` | `true` |
| `BUILD_PLATFORM` | `Any CPU` | `false` | `true` |
| `SECRET_KEY` | `(sensitive / hidden)` | `true` | `false` |

#### Repository (being deleted)

| Type | Repo ID | Branch | YAML Path | Report Build Status |
| ---- | ------- | ------ | --------- | ------------------- |
| `TfsGit` | `80128bc2-17ff-45f8-ad59-d7609a605c75` | `refs/heads/master` | `azure-pipelines.yml` | `true` |

</details>
```

### Conditional Rendering

Tables are only shown when the corresponding blocks contain data:

```markdown
<details open>
<summary>➕ azuredevops_build_definition <b><code>minimal</code></b> — <code>minimal-pipeline</code></summary>
<br>

**Pipeline Name:** `minimal-pipeline`

**Path:** `\\`

#### Repository

| Type | Repo ID | Branch | YAML Path |
| ---- | ------- | ------ | --------- |
| `TfsGit` | `repo-id-here` | `refs/heads/main` | `pipeline.yml` |

<!-- No Variables table shown - no variables defined -->
<!-- No CI Trigger table shown - empty array -->
<!-- No Pull Request Trigger table shown - empty array -->
<!-- No Schedules table shown - empty array -->

</details>
```

## Technical Details

### Data Structure

Based on `examples/azuredevops/terraform_plan2.json`, the build definition structure includes:

```json
{
  "type": "azuredevops_build_definition",
  "change": {
    "before": {
      "name": "example-pipeline",
      "path": "\\Pipelines",
      "project_id": "0f0b93a6-f450-49b2-ad52-fe3303c2f9aa",
      "agent_pool_name": "Azure Pipelines",
      "queue_status": "enabled",
      "variable": [
        {
          "name": "BUILD_CONFIGURATION",
          "value": "Release",
          "is_secret": false,
          "allow_override": true,
          "secret_value": ""
        }
      ],
      "ci_trigger": [
        {
          "use_yaml": true,
          "override": []
        }
      ],
      "pull_request_trigger": [],
      "schedules": [],
      "repository": [
        {
          "repo_type": "TfsGit",
          "repo_id": "80128bc2-17ff-45f8-ad59-d7609a605c75",
          "branch_name": "refs/heads/master",
          "yml_path": "azure-pipelines.yml",
          "report_build_status": true,
          "github_enterprise_url": "",
          "service_connection_id": ""
        }
      ],
      "jobs": [],
      "features": [],
      "variable_groups": [1]
    },
    "after": { /* similar structure */ },
    "before_sensitive": {
      "variable": true,
      "ci_trigger": [{"override": []}],
      "pull_request_trigger": [],
      "repository": [{}],
      "schedules": [],
      "jobs": [],
      "features": [],
      "variable_groups": [false]
    },
    "after_sensitive": { /* similar structure */ }
  }
}
```

### Key Observations

1. **Variables** are in the `variable` array with attributes: `name`, `value`, `is_secret`, `allow_override`, `secret_value`
   - When `is_secret: false`, the value is in `value` attribute
   - When `is_secret: true`, the value is in `secret_value` attribute (but should be displayed as "(sensitive / hidden)")
2. The `variable` array is marked as `true` in `before_sensitive` and `after_sensitive` (entire array is sensitive)
3. **CI Trigger** block has `use_yaml` (boolean) and `override` (array of branch filter strings)
4. **Pull Request Trigger** block structure is similar to CI trigger with additional fields
5. **Repository** block is an array (typically single element) with repo connection details
6. **Schedules** block is an array of schedule configurations
7. **Jobs** block is typically empty for YAML-based pipelines
8. Not all attributes are always present (e.g., `github_enterprise_url` may be empty string or null)

### Security Consideration

The template must **never display** the actual value when `is_secret: true`. Instead:
- Display `(sensitive / hidden)` in the Value column
- Show all other metadata (name, is_secret flag, allow_override)

For regular variables (`is_secret: false`), display the `value` attribute normally.

### Empty Value Handling

- `is_secret`: If null or missing, display as `false`
- `allow_override`: If null or missing, display as `false` or `-`
- `value`: If empty string or null for non-secret variables, display as `(empty)` or `-`
- `secret_value`: Always display as `(sensitive / hidden)` regardless of actual value
- `override` (in triggers): If empty array, display as `-`
- `github_enterprise_url`, `service_connection_id`: If empty string, display as `-`

### Semantic Diffing

Variables should be matched by their `name` attribute (similar to variable group variables). This ensures that:
- Adding a new variable is clearly marked as ➕
- Removing a variable is clearly marked as ❌
- Modifying a variable shows before/after values as 🔄
- Reordering variables doesn't create false change indicators

For blocks like `ci_trigger`, `repository`, etc., which are typically single-element arrays or have specific structure, display before/after differences inline.

## Success Criteria

- [ ] Template created for `azuredevops_build_definition` resource type at `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/build_definition.sbn`
- [ ] ViewModel and Mapper classes created following the pattern from `azuredevops_variable_group`:
  - [ ] `BuildDefinitionViewModel.cs` with nested models for variables, triggers, repository, etc.
  - [ ] `BuildDefinitionMapper.cs` implementing `IResourceModelMapper`
  - [ ] `BuildDefinitionFactory.cs` for creating view models from resource changes
- [ ] All **variables** displayed in table format with columns: Name, Value, Is Secret, Allow Override
- [ ] Secret variables (`is_secret: true`) show all metadata but display `(sensitive / hidden)` in Value column
- [ ] Variables categorized correctly as Added, Modified, Removed, or Unchanged using semantic matching by name
- [ ] Large variable values (>100 chars or multi-line) handled using existing large value display mechanism (regular variables only)
- [ ] Modified variables show before/after values with `-` and `+` prefixes for changed attributes
- [ ] Unchanged attributes in modified variables show single value without prefix
- [ ] **CI Trigger** block displayed as table when present (use_yaml, override)
- [ ] **Pull Request Trigger** block displayed as table when present
- [ ] **Schedules** block displayed as table when present
- [ ] **Repository** block displayed as table showing repo connection details
- [ ] **Jobs** block displayed as table if populated
- [ ] Empty/null attribute values displayed as `-` (dash)
- [ ] Tables only shown when blocks contain data (conditional rendering - no empty tables)
- [ ] Create, update, and delete operations each have appropriate table layouts
- [ ] Build definition metadata (name, path, agent_pool_name) displayed prominently
- [ ] Summary line includes pipeline name and variable change count for updates
- [ ] Template follows Report Style Guide formatting standards (code formatting for values, plain text for labels)
- [ ] Mapper registered in `AzureDevOpsModule.cs` dependency injection
- [ ] All existing tests pass
- [ ] New tests verify build definition rendering for:
  - [ ] Create operation with variables (both regular and secret)
  - [ ] Update operation with added/modified/removed variables
  - [ ] Delete operation with variables
  - [ ] Secret variables display metadata but show `(sensitive / hidden)` for values
  - [ ] CI trigger display
  - [ ] Repository configuration display
  - [ ] Pull request trigger display (if test data available)
  - [ ] Schedules display (if test data available)
  - [ ] Large variable values (regular variables only)
  - [ ] Empty/null attribute values
  - [ ] Conditional rendering (tables only shown when blocks have data)
- [ ] Documentation updated in `docs/features.md`
- [ ] Example output included in feature documentation

## Non-Functional Requirements

- **Backwards Compatibility**: Must not break rendering of other Azure DevOps resources
- **No Breaking Changes**: Existing build definition resources should render with the new template without requiring configuration changes
- **Performance**: Template rendering should have negligible performance impact (<10ms per resource)
- **Consistency**: Follow the exact same pattern as `azuredevops_variable_group` for maintainability

## Open Questions

**Q: How should we handle the `override` array in CI triggers and Pull Request triggers?**

**A:** Display as a comma-separated list of branch filters in the table. If empty array, display as `-`. For create/delete, show in a single table row. For updates, show before/after if changed.

**Q: The `repository` block is an array but typically has only one element. Should we handle multiple repositories?**

**A:** Yes, display as a table with one row per repository element. This handles the general case and matches Terraform's schema.

**Q: Should we display the `jobs` array if it's populated?**

**A:** Yes, display basic job metadata (name, condition, timeout_in_minutes) in a table if the array is not empty. However, for YAML-based pipelines, this is typically empty.

**Q: How detailed should the schedules table be?**

**A:** Show all relevant attributes: branch filters, days to build (as comma-separated list), schedule only with changes (boolean), start hours/minutes (formatted as time), time zone. Follow the Report Style Guide for value formatting.

**Q: Should we show `variable_groups` as a table?**

**A:** No, display as a standard attribute (array of IDs). Variable group content is shown when the `azuredevops_variable_group` resource itself is in the plan.

**Q: How should we display boolean values in tables (e.g., `is_secret`, `allow_override`)?**

**A:** Display as `true` or `false` strings wrapped in code formatting (backticks), following the Report Style Guide principle that data values are code-formatted. Do not use checkmarks or other symbols.

**Q: Should pull request trigger forks settings be in a separate column or merged?**

**A:** Display in separate columns for clarity: "Forks Enabled" (boolean) and "Forks Comment Requirement" (boolean or text based on schema).

**Q: For the summary line, should we count all configuration changes or just variables?**

**A:** For build definitions, the most important changes are typically variables. Show variable count in summary (e.g., "3 🔧 variables"). Other block changes (triggers, repository) are visible in the expanded details.
