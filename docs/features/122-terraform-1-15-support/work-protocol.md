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
