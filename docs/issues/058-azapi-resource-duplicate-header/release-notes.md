# Duplicate Header Bug Fix

This release fixes a bug that caused duplicate headers in `azapi_resource` and `azuredevops_variable_group` rendered output.

## 🐛 Bug fixes

- **Duplicate headers removed from azapi_resource and azuredevops_variable_group templates**
  
  Previously, these two resource types would display their resource name twice: once in the collapsible summary and again as a Markdown heading immediately below. This created unnecessary visual redundancy and differed from the pattern used by all other templates.
  
  **Before:**
  ```markdown
  <summary>➕ azapi_resource <b><code>container_app</code></b> — <code>🆔 ca-tfplan2md-demo</code></summary>
  <br>
  
  ### ➕ azapi_resource.container_app
  ```
  
  **After:**
  ```markdown
  <summary>➕ azapi_resource <b><code>container_app</code></b> — <code>🆔 ca-tfplan2md-demo</code></summary>
  <br>
  
  [resource details continue here]
  ```
  
  Both templates now follow the same pattern as all other custom templates: summary tag only, no duplicate heading line.

## 🔗 Commits

- [`bbfca0e`](https://github.com/oocx/tfplan2md/commit/bbfca0e) fix: remove duplicate headers in azapi_resource and azuredevops_variable_group templates
- [`f0057ad`](https://github.com/oocx/tfplan2md/commit/f0057ad) test: update snapshots for duplicate header fix
- [`0be9e3c`](https://github.com/oocx/tfplan2md/commit/0be9e3c) docs: update examples in features.md to remove duplicate headers

## Impact

This fix improves the visual consistency of generated markdown reports. All 21 custom templates now follow the same header pattern. No breaking changes or migration steps required.
