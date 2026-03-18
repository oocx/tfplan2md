# Work Protocol: Build Definition Variable Rendering

**Work Item:** `docs/issues/118-build-definition-variable-rendering/`
**Branch:** `copilot/add-azuredevops-variable-rendering`
**Workflow Type:** Bug Fix
**Created:** 2026-03-18

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2026-03-18
- **Summary:** Investigated the bug where `azuredevops_build_definition` variables with `is_secret = true` show all variable attributes as `(sensitive)` instead of only the value attribute. Identified the root cause (missing dedicated renderer) and the scope of the fix. Also confirmed that all the infrastructure for a proper tabular renderer already exists (BuildDefinitionViewModelFactory, BuildDefinitionFormatters, etc.) but is not yet connected.
- **Artifacts Produced:** `docs/issues/118-build-definition-variable-rendering/analysis.md`, `docs/issues/118-build-definition-variable-rendering/work-protocol.md`
- **Problems Encountered:** None
