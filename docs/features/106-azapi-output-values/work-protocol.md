# Work Protocol: Separate Table for azapi Output Values

**Work Item:** `docs/features/106-azapi-output-values/`
**Branch:** `copilot/add-separate-table-for-azapi-output`
**Workflow Type:** Feature
**Created:** 2025-07-14

## Required Agents

| Agent | Required | Status |
|-------|----------|--------|
| Requirements Engineer | ✅ Required | ✅ Done |
| Architect | ✅ Required | ⏳ Pending |
| Quality Engineer | ✅ Required | ⏳ Pending |
| Task Planner | ✅ Required | ⏳ Pending |
| Developer | ✅ Required | ⏳ Pending |
| Technical Writer | ✅ Required | ⏳ Pending |
| Code Reviewer | ✅ Required | ⏳ Pending |
| UAT Tester | ⚠️ If user-facing | ⏳ Pending |
| Release Manager | ✅ Required | ⏳ Pending |
| Retrospective | ✅ Required | ⏳ Pending |

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2025-07-14
- **Summary:** Gathered requirements and wrote the feature specification for rendering azapi
  `output` attribute values in a separate table after body (input) attributes. Covered all
  change actions, grouping, sensitivity, large-value handling, and "known after apply" behaviour.
  Based on existing issue analyst findings confirming `output` is a sibling of `body` in the
  plan JSON and that `RenderAzapiBody` can be reused.
- **Artifacts Produced:**
  - `docs/features/106-azapi-output-values/specification.md`
  - `docs/features/106-azapi-output-values/work-protocol.md`
- **Problems Encountered:** None.
