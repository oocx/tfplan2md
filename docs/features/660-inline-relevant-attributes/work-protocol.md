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
