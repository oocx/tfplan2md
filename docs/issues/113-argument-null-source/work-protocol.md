# Work Protocol: ArgumentNullException When Parsing Plan Without resource_changes

**Work Item:** `docs/issues/113-argument-null-source/`
**Branch:** `copilot/fix-argument-null-error`
**Workflow Type:** Bug Fix
**Created:** 2025-07-14

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2025-07-14
- **Summary:** Investigated the `ArgumentNull_Generic Arg_ParamName_Name, source` error reported in v1.36.0. Identified root cause as `plan.ResourceChanges` being null when the `resource_changes` field is absent from (or explicitly null in) the Terraform plan JSON. Also identified a secondary null risk on `Change.Actions` and `OutputChange.Actions`. Produced issue analysis document.
- **Artifacts Produced:** `docs/issues/113-argument-null-source/analysis.md`, `docs/issues/113-argument-null-source/work-protocol.md`
- **Problems Encountered:** None
