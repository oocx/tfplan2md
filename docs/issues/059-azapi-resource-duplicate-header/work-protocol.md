# Work Protocol: Duplicate Header in azapi_resource and azuredevops_variable_group Templates

**Work Item:** `docs/issues/059-azapi-resource-duplicate-header/`
**Branch:** `copilot/fix-duplicate-header-azapi-resource` (non-standard; should be `fix/059-azapi-resource-duplicate-header`)
**Workflow Type:** Bug Fix
**Created:** 2025-02-09

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2025-02-09
- **Summary:** Investigated duplicate header rendering in azapi_resource template. Found that both azapi_resource.sbn and azuredevops_variable_group.sbn templates include an explicit `### {{ change.action_symbol }} {{ change.address }}` header at lines 11 and 10 respectively, which duplicates the information already present in the `<summary>{{ change.summary_html }}</summary>` tag. This pattern is not present in any other custom templates (azuread, azurerm templates do not have this explicit header).
- **Artifacts Produced:** 
  - `docs/issues/059-azapi-resource-duplicate-header/work-protocol.md`
  - `docs/issues/059-azapi-resource-duplicate-header/analysis.md`
- **Problems Encountered:** None
