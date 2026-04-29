# Work Protocol: Terraform 1.14 / 1.15 Plan-JSON Support

**Work Item:** `docs/features/122-terraform-1-15-support/`
**Branch:** `copilot/analyze-terraform-release-features`
**Workflow Type:** Feature
**Created:** 2026-04-29

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst (analysis phase)
- **Date:** 2026-04-29
- **Summary:** Produced the upstream-changes analysis comparing Terraform 1.13 → 1.14 → 1.15 plan JSON against current tfplan2md parsing/rendering. Documented six prioritized enhancement suggestions (H1 action_invocations, H2 plan-context awareness, M1 relevant_attributes detail, M2 deprecations, M3 checks, plus low-priority items L1–L6). Confirmed that `format_version` remains `"1.2"` and that all 1.14/1.15 additions are additive. Raised six open questions for the maintainer. Per maintainer instruction, no `work-protocol.md`, ADR, or code was created at this stage — the artifact was scoped explicitly as analysis + suggestions.
- **Artifacts Produced:**
  - `docs/features/122-terraform-1-15-support/analysis.md`
- **Problems Encountered:** None. Source diffing of `internal/command/jsonplan/{plan,resource,action_invocations}.go` across `v1.13`, `v1.14`, `v1.15` branches plus changelog reading was sufficient to characterise the JSON delta.

### Requirements Engineer
- **Date:** 2026-04-29
- **Summary:** Took the maintainer's approved scope (bundle H1 + H2 + M2 into a single "Terraform 1.14/1.15 plan-JSON support" feature) and produced the formal Feature Specification. Locked decisions per maintainer instruction: inline action rendering via the existing parent-child registry pattern (no new top-level Actions section), single generic action renderer, deferred actions handled in the same inline location, plan-context fields surfaced in dedicated top-level sections, deprecations routed through the existing code-analysis warnings mechanism rather than a new warnings system, always-on rendering with no opt-in flag, and hand-crafted JSON test fixtures. Confirmed via source-code inspection that the parent-child registry exists at `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ParentChildRelationshipRegistry.cs` (with merging logic in `ReportModelBuilder.ParentChildMerging.cs`) and that warnings are surfaced through `CodeAnalysisWarningModel`, `BuildWarningModels` in `ReportModelBuilder.CodeAnalysis.cs`, and the "Code Analysis Warnings" heading rendered by `CodeAnalysisSectionRenderer.cs` — and cited those files in the spec. Three layout-only questions remain genuinely open for the Architect (H2 final layout, warnings heading wording, fallback location for actions whose triggering resource has no section).
- **Artifacts Produced:**
  - `docs/features/122-terraform-1-15-support/specification.md`
  - `docs/features/122-terraform-1-15-support/work-protocol.md`
- **Problems Encountered:** None.

### Architect
- **Date:** 2026-04-29
- **Summary:** Resolved the three open architecture questions from the spec (H2 layout, warnings heading wording, orphan-action fallback) and locked the supporting technical choices the Developer needs (plan-JSON model extensions, deferred indicator form, sensitivity reuse, deprecation routing). Produced four focused ADRs in the feature folder (one per coherent decision cluster, instead of one per bullet) following the project's ADR template (status / context / options / decision / consequences / implementation notes). Verified the existing report ordering by reading `ReportRenderer.Render`, the parent-child registry by reading `ParentChildRelationshipRegistry` and `ReportModelBuilder.ParentChildMerging`, the warnings pipeline by reading `CodeAnalysisWarningModel` / `CodeAnalysisSectionRenderer`, and the sensitivity helpers by reading `SensitivityHelper`. Decisions made: (ADR-001) extend `TerraformPlan` with five optional properties + four new records; reuse `ResourceChange` for `resource_drift`; carry deferred-ness in the model-builder, not on the wire record; read deprecations from the existing `JsonElement?` configuration via a new `ConfigurationDeprecationReader` helper. (ADR-002) plan-status banner emitted from `HeaderRenderer` between title and metadata; new H2 sections for `🌀 Drift Detected` and `Relevant Attributes` placed between Resource Changes and Refactoring as a contiguous "plan-context cluster"; no new section emitted on absence (NFR-1, AC-9). (ADR-003) actions attach to parents via a new `Actions` collection on `ResourceChangeModel` populated by a new `ReportModelBuilder.Actions.cs` partial (without polluting `ParentChildRelationshipRegistry`); deferred actions render in-place with a `⏳` prefix and a deferred callout; sensitivity reuses `JsonFlattener` + `SensitivityHelper`; orphan actions go to a new H2 `🎬 Other Actions` section between Resource Changes and the plan-context cluster, with two H3 sub-groups (invoke / lifecycle-orphan). (ADR-004) extend `CodeAnalysisWarningModel` with an optional `Source` discriminator + `SubjectName` / `SubjectKind` (additive, default preserves SARIF rendering); rename the H3 from "Code Analysis Warnings" to "Warnings"; emit one warning per *referenced* deprecated variable / output (not per declaration). All decisions respect the maintainer-locked scope (inline action UX, single generic renderer, always-on, hand-crafted fixtures, Stacks out of scope, deprecations through existing warnings).
- **Artifacts Produced:**
  - `docs/features/122-terraform-1-15-support/adr-001-plan-json-model-extensions.md`
  - `docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md`
  - `docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md`
  - `docs/features/122-terraform-1-15-support/adr-004-deprecation-warnings-via-existing-pipeline.md`
- **Problems Encountered:** None. One snapshot-test consequence is called out for QE/Developer in ADR-004: the heading rename from "Code Analysis Warnings" to "Warnings" will require updating any existing snapshot fixture that exercises a SARIF processing failure (spec AC-9 explicitly permits this carve-out). No genuinely unresolved questions remain for downstream agents.
