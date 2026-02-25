# Work Protocol: Known-After-Apply Rendering

**Work Item:** `docs/features/102-known-after-apply-rendering/`
**Branch:** `feature/102-known-after-apply-rendering`
**Workflow Type:** Feature
**Created:** 2026-02-25

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2026-02-25
- **Summary:** Analysed two existing Copilot branches (`copilot/fix-empty-summary-details` and `copilot/fix-empty-summary-details-azuread-group-member`) that each independently address the same underlying bug. Synthesised their approaches into a single feature specification covering all use-case scenarios (five group-member scenarios + generic resource scenarios + update scenario) and three explicit decision points (A/B/C) with rendered output examples for each option. Created the feature branch and specification document.
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/specification.md`, `docs/features/102-known-after-apply-rendering/work-protocol.md`
- **Problems Encountered:** The two copilot branches conflict on three design decisions (which attributes to show in tables, whether to show config references in table values, and sensitivity vs. computed priority). These are presented as Decision A, B, and C in the specification with rendered examples for Maintainer review before the Architect begins implementation planning.

### Architect
- **Date:** 2026-02-25
- **Summary:** Produced the technical design for surfacing computed (known-after-apply) values in attribute tables and for fixing `azuread_group_member` summaries when IDs are computed. The design keeps unknown-value semantics in `ReportModelBuilder` (so computed attributes participate in update summaries) and confines AzureAD-specific summary rules to the AzureAD provider. Also defined a minimal way to suppress the default `_No attribute changes._` placeholder for the whole-resource `after_unknown: true` plan shape.
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/architecture.md`
- **Problems Encountered:** None. One known variability risk is Terraform plan-shape differences for `after_unknown`; the design recommends conservative unknown-path parsing and snapshot coverage for the spec scenarios.

### Architect (revision)
- **Date:** 2026-02-25
- **Summary:** Enriched the architecture document with comprehensive codebase background so that a developer can work from this document and the specification alone, without needing the global `docs/architecture.md`. Added detailed explanations of: the full rendering pipeline (parsing → model building → template rendering), `JsonFlattener` behaviour with null values, the exact logic and root cause in `BuildAttributeChanges`, the `FormatAttributeValueTableWithRegistry` null-value gate, the `AzureAdSummaryBuilder.Groups.cs` empty-summary root cause, the `ConfigurationReferenceResolver` index structure, the provider module registration system, and the reference implementation in `DiffRenderer.Paths.cs`.
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/architecture.md` (updated)
- **Problems Encountered:** None.

### Quality Engineer
- **Date:** 2026-02-25
- **Summary:** Produced comprehensive test plan and UAT test plan from the specification and architecture. Mapped all 16 success criteria and 9 scenarios to 27 test cases across three new test files. Defined unit tests for `AfterUnknownHelper` (TC-01–TC-04) and `ReferenceSelector` (TC-05–TC-11), integration/snapshot tests for all 9 specification scenarios (TC-12–TC-23), and regression guards (TC-25–TC-27). Created UAT plan JSON covering 6 representative scenarios and a UAT validation checklist. Flagged one open question (OQ-01: whether to show any text for the Scenario 8 whole-resource-unknown case when there are zero attribute rows).
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/test-plan.md`, `docs/features/102-known-after-apply-rendering/uat-test-plan.md`, `docs/features/102-known-after-apply-rendering/uat-plan.json`
- **Problems Encountered:** Minor gap in specification — Scenario 8 states `_No attribute changes._` must not appear, but does not specify whether any alternative text should be shown. Flagged as OQ-01; a Maintainer decision is needed before snapshot assertions for TC-22 can be finalised.

### Task Planner
- **Date:** 2026-02-25
- **Summary:** Created a detailed task plan for implementing the 'Known-After-Apply' rendering feature. The plan consists of 6 tasks covering foundational helpers, core model-building updates, provider-specific enhancements for AzureAD, template updates for whole-resource unknown scenarios, and a full suite of integration tests. Ensured that all 9 scenarios from the test plan are covered by the tasks.
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/tasks.md`
- **Problems Encountered:** None. The task breakdown followed logically from the architecture and test plan.

### Developer
- **Date:** 2026-02-25
- **Summary:** Implemented Task 1 (Unknown-After-Apply Detection Helper) using test-first flow. Added `AfterUnknownHelper` to parse flattened keys and navigate `after_unknown` trees across objects and arrays, including subtree/root boolean unknown handling. Added unit tests for TC-01 to TC-04 covering whole-resource, nested, array, subtree, null, and malformed cases.
- **Artifacts Produced:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/AfterUnknownHelper.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AfterUnknownHelperTests.cs`, `docs/features/102-known-after-apply-rendering/tasks.md`, `docs/features/102-known-after-apply-rendering/work-protocol.md`
- **Problems Encountered:** Initial static analysis reported method cognitive complexity in key parsing; resolved by refactoring parsing into smaller private helper methods without behavior changes.

### Developer
- **Date:** 2026-02-25
- **Summary:** Implemented Tasks 2-6 end-to-end without pause: added `ReferenceSelector`; enhanced `ResourceChangeModel` and `ReportModelBuilder` for computed `after_unknown` rendering and reference labels; updated AzureAD group-member summaries for computed IDs; updated `_resource.sbn` whole-resource unknown note behavior; and added integration tests for computed scenarios across markdown generation and AzureAD provider output.
- **Artifacts Produced:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ReferenceSelector.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`, `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`, `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`, `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs`, `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn`, `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupMemberComputedTests.cs`, `docs/features/102-known-after-apply-rendering/tasks.md`, `docs/features/102-known-after-apply-rendering/work-protocol.md`
- **Problems Encountered:** Encountered analyzer and AOT/trimming constraints during helper/model test integration and one failing whole-resource unknown scenario. Resolved by refactoring helper parsing, tightening serialization fallback behavior, and wiring `has_whole_resource_unknown_after_apply` into `AotScriptObjectMapper` so the template condition is visible during rendering.

### Developer
- **Date:** 2026-02-25
- **Summary:** Added a dedicated combined snapshot test that covers all 9 scenarios from the feature specification in one rendered report. Created a new fixture plan containing Scenario 1-9 inputs (including configuration references, sensitive+computed update, whole-resource unknown boolean, and parent-child standalone behavior for computed child references), added a snapshot test using AzureAD + AzureRM provider registries, and generated the new baseline snapshot.
- **Artifacts Produced:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/known-after-apply-all-scenarios-plan.json`, `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/KnownAfterApplySnapshotTests.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/known-after-apply-all-scenarios.md`, `docs/features/102-known-after-apply-rendering/work-protocol.md`
- **Problems Encountered:** None. Initial snapshot generation failed as expected on first run due to missing baseline; resolved by syncing the generated snapshot file into the tracked snapshots directory and re-running tests.

### Technical Writer
- **Date:** 2026-02-25
- **Summary:** Reviewed the completed implementation against existing documentation. Added a new `## Known-After-Apply Rendering` section to `docs/features.md` covering: computed attributes in tables with `(known after apply)` placeholders, reference labels sourced from configuration expressions, the `🔒(known after apply)` format for sensitive+computed attributes, whole-resource unknown (`_(all values known after apply)_`), and fixed AzureAD group member summary lines. Corrected a now-stale sentence in the `### Attribute Tables` subsection that stated unknown attributes were omitted. Added a `🔮 Known-after-apply visibility` bullet to `README.md` features list.
- **Artifacts Produced:** `docs/features.md` (updated), `README.md` (updated), `docs/features/102-known-after-apply-rendering/work-protocol.md` (updated)
- **Problems Encountered:** None.

### Code Reviewer
- **Date:** 2026-02-25
- **Summary:** Reviewed all implementation tasks (T1–T6), spec compliance across all 9 scenarios, test coverage, snapshot diffs, and documentation. Found three Blockers: (1) `SNAPSHOT_UPDATE_OK` token absent from all feature branch commits despite 3 snapshot files changed; (2) required UAT artifact `uat-plan.md` missing; (3) Technical Writer's documentation edits (`docs/features.md`, `README.md`, `work-protocol.md`) are uncommitted. Also found one Minor issue: garbled emoji (`U+FFFD`) on two lines in `README.md`. Core implementation is correct and well-tested; all 1270 tests pass; coverage thresholds met.
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/code-review.md`
- **Problems Encountered:** Docker NativeAOT build takes >5 minutes; build was not fully confirmed. All test-based verification passed.

