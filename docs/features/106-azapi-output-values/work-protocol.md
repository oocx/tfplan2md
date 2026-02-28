# Work Protocol: Separate Table for azapi Output Values

**Work Item:** `docs/features/106-azapi-output-values/`
**Branch:** `copilot/add-separate-table-for-azapi-output`
**Workflow Type:** Feature
**Created:** 2025-07-14

## Required Agents

| Agent | Required | Status |
|-------|----------|--------|
| Requirements Engineer | ✅ Required | ✅ Done |
| Architect | ✅ Required | ✅ Done |
| Quality Engineer | ✅ Required | ✅ Done |
| Task Planner | ✅ Required | ✅ Done |
| Developer | ✅ Required | ⏳ Pending |
| Technical Writer | ✅ Required | ⏳ Pending |
| Code Reviewer | ✅ Required | ⏳ Pending |
| UAT Tester | ⚠️ If user-facing | ⏳ Pending |
| Release Manager | ✅ Required | ⏳ Pending |
| Retrospective | ✅ Required | ⏳ Pending |

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Quality Engineer
- **Date:** 2025-07-14
- **Summary:** Reviewed the feature specification and architecture, explored existing azapi
  snapshot test patterns, and produced a comprehensive test plan covering all 10 scenarios
  identified by the Architect: create-unknown, create-present, update-changed,
  update-unchanged, delete, replace-unknown, no-output (regression), sensitive output,
  grouped output, and large output value. Also created a UAT test plan defining a
  three-resource feature plan covering the "known after apply" notice, grouped update
  output, and sensitive delete output. Mapped all 11 acceptance criteria to test cases.
- **Artifacts Produced:**
  - `docs/features/106-azapi-output-values/test-plan.md`
  - `docs/features/106-azapi-output-values/uat-test-plan.md`
- **Problems Encountered:** None.

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

### Architect
- **Date:** 2025-07-14
- **Summary:** Analysed the feature specification against the existing codebase. Confirmed that
  `RenderAzapiBody` (registered as `render_azapi_body` in Scriban) is fully reusable for
  rendering the `output` attribute — it accepts any JSON sub-object and already handles
  grouping, sensitivity masking, large-value handling, and all rendering modes. Designed a
  template-only implementation requiring additions to `resource.sbn` and
  `update_resource.sbn` only. No C# changes are required. Documented complete Scriban
  snippets for both templates covering all actions (create, update, delete, replace), the
  `after_unknown.output` known-after-apply guard, and sensitivity pass-through. Noted that
  grouped sub-section headings will be `` `prefix` `` (consistent with existing body
  sub-sections) rather than the aspirational `` Output Values - `prefix` `` format from the
  spec, because modifying the C# grouping renderer is out of scope.
- **Artifacts Produced:**
  - `docs/features/106-azapi-output-values/architecture.md`
- **Problems Encountered:** None.

### Task Planner
- **Date:** 2025-07-14
- **Summary:** Reviewed the feature specification, architecture, and test plan. Explored the
  existing azapi template files (`resource.sbn`, `update_resource.sbn`), test data JSON
  structure, and `AzapiSnapshotTests.cs` snapshot test pattern. Produced a five-task plan
  covering: (1) output rendering block in `resource.sbn`, (2) output rendering block in
  `update_resource.sbn`, (3) six new test data JSON files (TC-01, TC-02, TC-03, TC-08,
  TC-09, TC-11), (4) six new snapshot test methods in `AzapiSnapshotTests.cs`, and
  (5) snapshot regeneration and full test suite verification.
- **Artifacts Produced:**
  - `docs/features/106-azapi-output-values/tasks.md`
- **Problems Encountered:** None.
