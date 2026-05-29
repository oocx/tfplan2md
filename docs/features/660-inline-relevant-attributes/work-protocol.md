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
