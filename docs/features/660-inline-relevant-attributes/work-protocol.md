# Work Protocol: Inline Relevant Attributes

**Work Item:** `docs/features/660-inline-relevant-attributes/`
**Branch:** `feature/660-inline-relevant-attributes`
**Workflow Type:** Feature
**Created:** 2026-05-29

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2026-05-29
- **Summary:** Gathered requirements for redesigning "Relevant Attributes" from a standalone bottom-of-report table into contextual inline annotations on each resource card. The design combines Option 3 (inline "Depends on" line) and Option 5 (forced-replacement causal chain). Clarified all open questions with the Maintainer.
- **Artifacts Produced:** `docs/features/660-inline-relevant-attributes/specification.md`, `docs/features/660-inline-relevant-attributes/work-protocol.md`
- **Problems Encountered:** None

### Architect
- **Date:** 2026-05-29
- **Summary:** Analyzed the existing relevant-attributes rendering pipeline (parsing → `RelevantAttribute` record → `RelevantAttributeModel` → `ReportModel.RelevantAttributes` → `ReportRenderer.RenderRelevantAttributes` H2 table). Designed the correlation algorithm between `relevant_attributes[]`, `replace_paths`, and `ConfigurationReferences`. Evaluated three options for where to compute correlations (renderer, dedicated stage, `ReportModelBuilder` partial class) and selected Option 3 (model-build-time computation in `ReportModelBuilder.PlanContext.cs`). Documented new model types (`ForcedReplacementAnnotation`, `DependsOnAnnotation`), `ResourceChangeModel` property additions, rendering changes to `DefaultResourceRenderer` and `ReportRenderer`, and snapshot test implications.
- **Artifacts Produced:** `docs/features/660-inline-relevant-attributes/architecture.md`
- **Problems Encountered:** None — all design questions were already answered in the specification. The reference-address normalization (stripping `.attribute` suffix from configuration reference strings to match against upstream resource addresses) is the main implementation nuance flagged for the Developer.

### Quality Engineer
- **Date:** 2026-05-29
- **Summary:** Created a comprehensive test plan mapping all 11 success criteria to unit tests and snapshot tests. Identified 19 unit tests covering correlation logic (`BuildInlineRelevantAttributeAnnotations`): forced-replacement annotation population, `IsChangingInThisPlan` flag semantics, `DependsOnAnnotations` vs `ForcedReplacementAnnotations` routing, in-place update exclusion, drift exclusion, empty-input fast path, reference address normalisation (managed vs data source), case-insensitive matching, multiple replace_paths, and delete-as-replace equivalence. Defined 8 new snapshot test scenarios with required fixture structures (JSON plan + configuration block). Identified 1 existing snapshot requiring an update (`relevant-attributes-present.md`: H2 table → `<details>` fallback), 1 snapshot that must NOT change (`relevant-attributes-absent.md`), and the emoji-spacing constraint for the new ⚠️/🔗 renderer lines. Created a UAT test plan specifying a combined fixture that exercises all five annotation paths in a single PR comment.
- **Artifacts Produced:** `docs/features/660-inline-relevant-attributes/test-plan.md`, `docs/features/660-inline-relevant-attributes/uat-test-plan.md`
- **Problems Encountered:** None

### Task Planner
- **Date:** 2026-05-29
- **Summary:** Read specification, architecture, and test plan for feature 660. Produced `tasks.md` with 11 prioritized tasks in dependency order. Tasks cover: new model types (`ForcedReplacementAnnotation`, `DependsOnAnnotation`), `ResourceChangeModel` property additions, `BuildInlineRelevantAttributeAnnotations` algorithm in `ReportModelBuilder.PlanContext.cs`, build pipeline wiring, inline renderer helper in `DefaultResourceRenderer.Helpers.cs`, renderer call-site in `DefaultResourceRenderer.cs`, fallback `<details>` section replacing the H2 table in `ReportRenderer.cs`, 19 unit tests, 7 new JSON snapshot fixtures, new `InlineRelevantAttributeSnapshotTests` class, and the existing `relevant-attributes-present.md` snapshot update.
- **Artifacts Produced:** `docs/features/660-inline-relevant-attributes/tasks.md`
- **Problems Encountered:** None — specification, architecture, and test plan were all complete and consistent with no open questions.

### Developer
- **Date:** 2026-06-01
- **Summary:** Completed the inline relevant-attributes implementation and closed the remaining test gaps for feature 660. In production code, updated `ReportModelBuilder.PlanContext.cs` so relevant-attribute correlation resolves module-prefixed references by progressively trimming suffix segments until a matching upstream resource key is found, and sorted depends-on annotations deterministically for stable rendering/snapshots. Added 19 focused unit tests for forced-replacement correlation, changing-upstream semantics, depends-on routing, in-place update/drift exclusion, managed/data-source matching, case-insensitive matching, module-prefixed references, delete-as-replace behaviour, and fallback cases. Added 7 snapshot fixtures/tests covering forced replacement, combined cards, depends-on-only, fallback-only, all-correlated, and drift scenarios; updated the existing `relevant-attributes-present.md` snapshot to the new fallback-details rendering. Created feature-specific UAT artifacts (`uat-plan.json` + rendered `uat-plan.md`) to exercise the full user-visible flow in a single report. Full verification passes: `dotnet build src/tfplan2md.slnx` and `scripts/test-with-timeout.sh --timeout-seconds 300 -- dotnet test --solution src/tfplan2md.slnx` (1354/1354 passed).
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.PlanContext.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderInlineRelevantAttributeTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/InlineRelevantAttributeSnapshotTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/tf114/relevant-attrs-*.json`
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/relevant-attrs-*.md`
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/relevant-attributes-present.md`
  - `docs/features/660-inline-relevant-attributes/uat-plan.json`
  - `docs/features/660-inline-relevant-attributes/uat-plan.md`
- **Problems Encountered:** Two implementation issues were found and fixed during validation: the original address-normalisation heuristic did not handle module-prefixed configuration references, and depends-on annotation ordering from a `HashSet` was unstable for snapshot testing.

### Technical Writer
- **Date:** 2026-06-01
- **Summary:** Updated user-facing documentation for feature 660 to reflect the new inline rendering model. `docs/features.md` now explains that `relevant_attributes[]` render as inline forced-replacement callouts, inline dependency context, changing-upstream emphasis, and a fallback `🔗 Other plan inputs` details block instead of a standalone bottom-of-report section.
- **Artifacts Produced:**
  - `docs/features.md`
  - `docs/features/660-inline-relevant-attributes/work-protocol.md` (this entry)
- **Problems Encountered:** None.

### Code Reviewer
- **Date:** 2026-06-01
- **Summary:** Reviewed the full feature 660 change set after implementation, docs, and release-note assets were in place. Verified the production change against the specification and test plan, then ran repository validation on the PR diff. Result: no review issues found and no security findings. Independent verification also already passed locally with `dotnet build src/tfplan2md.slnx` and `scripts/test-with-timeout.sh --timeout-seconds 300 -- dotnet test --solution src/tfplan2md.slnx` (1354/1354 passed).
- **Decision:** ✅ **Approved** — no issues found.
- **Artifacts Produced:**
  - `docs/features/660-inline-relevant-attributes/work-protocol.md` (this entry)
- **Problems Encountered:** None.

### Release Manager
- **Date:** 2026-06-01
- **Summary:** Created release notes and release-note screenshots for feature 660. Added `release-notes.md` using the repository template, documenting the new inline forced-replacement/dependency annotations and fallback inputs section. Generated two 580×400 PNG screenshots (`feature-660-inline-annotations.png`, `feature-660-fallback-inputs.png`) from the feature-specific UAT plan and referenced them with absolute `raw.githubusercontent.com` URLs plus required `release-screenshot` metadata comments. Release-note validation requirements are satisfied for this work item (`release-notes.md`, `work-protocol.md`, screenshots, metadata).
- **Artifacts Produced:**
  - `docs/features/660-inline-relevant-attributes/release-notes.md`
  - `docs/features/660-inline-relevant-attributes/feature-660-inline-annotations.png`
  - `docs/features/660-inline-relevant-attributes/feature-660-fallback-inputs.png`
  - `docs/features/660-inline-relevant-attributes/work-protocol.md` (this entry)
- **Problems Encountered:** None.
