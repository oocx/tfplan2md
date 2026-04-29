# ADR-002 (feature 122): H2 report layout — plan-status banner, drift section, relevant-attributes section

## Status

Proposed

## Context

The feature spec (FR-H2.4 – FR-H2.7) requires three new pieces of plan-context information to be surfaced:

- A **plan-status banner** that visibly signals when `applyable` is false, `complete` is false, or `errored` is true.
- A **"Drift detected"** rendering of `resource_drift[]`.
- A **`relevant_attributes[]`** rendering, with placement explicitly delegated to the Architect.

The spec does not lock the precise placement; this ADR makes that decision so the Quality Engineer can write snapshot tests against a stable layout and the Developer has an unambiguous target.

The current report ordering, established by `ReportRenderer.Render` in `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`, is:

1. `HeaderRenderer.Render` — title (H1) + optional metadata line
2. `SummaryRenderer.Render` — totals
3. `CodeAnalysisSectionRenderer.RenderSummary` — Code Analysis Summary (H2)
4. `RenderResourceChanges` — "Resource Changes" (H2) grouped by `📦 Module` (H3)
5. `CodeAnalysisSectionRenderer.RenderOtherFindings` — module-level / unmatched findings
6. `RenderRefactoring` — "Refactoring Summary" (H2)
7. `RenderOutputs` — "Outputs" (H2, global)
8. `RenderFilteredResourceInfo` — informational note

Reviewers' attention budget is highest at the top of the report; primary changes (step 4) must remain above the fold.

## Options considered

### (a) Plan-status banner

**Option A1 (chosen): Render in `HeaderRenderer`, immediately after the title and before the metadata line / `SummaryRenderer`.** Banner is emitted only when at least one signal is non-default (`applyable == false`, `complete == false`, `errored == true`). Style: a Markdown blockquote callout, e.g. `> 🚨 **Errored** — this plan failed to compute fully.` / `> ⛔ **Not applyable** — Terraform refused to apply this plan.` / `> ⚠️ **Incomplete** — this plan does not represent the full intended state.` Multiple signals stack as separate blockquote lines in a single block. For an ordinary plan (or any plan where all three fields are absent), nothing is rendered — no whitespace, no banner.

- Pros: maximum visibility (top of the report); reuses the existing `HeaderRenderer` so we don't add another renderer to `ReportRenderer.Render`; keeps the rule "absence of fields ⇒ no visible change" trivially true (NFR-1, AC-9).
- Cons: `HeaderRenderer` grows a dependency on three new `ReportModel` properties.

**Option A2 (rejected): Banner as a quiet-confirmation pill on every plan.** Rejected because it changes the rendered output for every Terraform 1.13 plan (violates AC-9) unless we suppress it on absence anyway, in which case it's identical to A1.

**Option A3 (rejected): Banner inside `SummaryRenderer`.** Rejected because the summary already carries totals; mixing plan-validity status with counts dilutes both.

### (b) `resource_drift[]` placement

**Option B1 (chosen): New "Drift Detected" H2 section, rendered between `RenderResourceChanges` and `RenderRefactoring`** (i.e. after Code Analysis "Other Findings", before Refactoring/Outputs). Reuses `DefaultResourceRenderer` to render each drift entry, but the H2 heading is `🌀 Drift Detected` with a leading paragraph: `> Detected outside Terraform — these changes were observed in real infrastructure but were not requested by configuration.` Each drifted resource is rendered as an H3 with a `🌀 ` prefix on the resource address to visually distinguish it from planned changes (which use the existing action emoji). Section omitted entirely when `resource_drift` is null or empty.

- Pros: keeps primary planned changes above the fold; reuses `DefaultResourceRenderer` so attribute diff rendering, sensitivity, and replace-paths work for free (drift entries share the `ResourceChange` shape per ADR-001); the `🌀` prefix gives reviewers a clear visual cue distinct from plan actions.
- Cons: a second resource-render path means snapshot tests for plans-with-drift are independent of plans-without-drift.

**Option B2 (rejected): Inline drift entries as a banner near the top.** Rejected because drift entries have full attribute changes (not summaries) and would push primary changes far below the fold for any moderately-sized drift set.

**Option B3 (rejected): Fold drift into the main "Resource Changes" section.** Rejected because reviewers must be able to distinguish "Terraform will do this" from "Terraform observed this happened outside its control"; mixing them is unsafe.

### (c) `relevant_attributes[]` placement

**Option C1 (chosen): New "Relevant Attributes" H2 section, rendered immediately after "Drift Detected" and before Refactoring.** Single concise table with columns `Resource | Attribute path` (where the attribute path is rendered using the same path-string formatter used for `replace_paths`). Section omitted when the array is null or empty.

- Pros: contiguous "plan-context cluster" with drift; one place to look; rendering is cheap (no per-resource state); easy to snapshot-test.
- Cons: reviewers don't see the upstream attribute next to the resource it influenced — they must cross-reference. Acceptable: M1 (a richer per-resource "why" column) is explicitly out of scope (spec § Out of Scope), and the spec only requires that relevant attributes be surfaced "somewhere reviewers will see them" (FR-H2.6, AC-7).

**Option C2 (rejected): Per-resource footnotes on each `ResourceChangeModel`.** Rejected because it requires correlating each `relevant_attributes[]` entry to the affected downstream resource (Terraform does not emit this correlation explicitly), it forces every renderer (default and provider-specific) to know about relevant-attributes, and it crosses an architecture boundary by injecting plan-context concerns into resource renderers.

**Option C3 (rejected): An extra column on the existing summary table.** Rejected because the summary table is per-resource-type, not per-resource, so the column has nowhere to land.

## Decision

Final report ordering for plans that contain the new fields:

1. `HeaderRenderer` — title (H1)
2. **NEW**: plan-status banner (blockquote, only when any of `applyable=false` / `complete=false` / `errored=true`)
3. metadata line (existing)
4. `SummaryRenderer`
5. `CodeAnalysisSectionRenderer.RenderSummary`
6. `RenderResourceChanges` (existing, unchanged)
7. `CodeAnalysisSectionRenderer.RenderOtherFindings`
8. **NEW**: `RenderOtherActions` (see ADR-003) — H2 "Other Actions"
9. **NEW**: `RenderDriftSection` — H2 "🌀 Drift Detected" (only when `resource_drift` is non-empty)
10. **NEW**: `RenderRelevantAttributes` — H2 "Relevant Attributes" (only when `relevant_attributes` is non-empty)
11. `RenderRefactoring`
12. `RenderOutputs`
13. `RenderFilteredResourceInfo`

For plans that lack every new field, the rendered output is byte-identical to today (NFR-1, AC-9).

## Consequences

### Positive

- Primary changes remain above the fold; new sections cluster together as "plan context" so a reviewer scanning the report sees `Resource Changes → Other Actions → Drift → Relevant Attributes → Refactoring → Outputs` as a coherent flow.
- The banner gives errored / non-applyable plans an immediately visible signal at the top.
- Every new section is omitted on absence, so existing snapshot tests pass without modification.

### Negative

- `ReportRenderer.Render` grows from 7 to 10 ordered render calls. Acceptable: each new method is a focused renderer with a clear precondition (`if (model.X == null || model.X.Count == 0) return;`).
- `ReportModel` gains four new optional properties: `PlanStatus` (`bool? Applyable, Complete, Errored` — likely a small value record), `Drift` (`IReadOnlyList<ResourceChangeModel>`), `RelevantAttributes` (`IReadOnlyList<RelevantAttributeModel>`), and the per-resource action attachments specified in ADR-003.

## Implementation notes

For the Developer:

- Add a small `PlanStatusModel` (or three nullable bools on `ReportModel`) so the banner renderer is unit-testable without a full plan.
- The drift renderer can call `DefaultResourceRenderer.Render` directly for each `ResourceChangeModel`. The visual differentiator (`🌀` prefix on the heading) is added by overriding the heading text passed in via the existing renderer hook OR by wrapping the resource renderer in a small "drift framing" helper — pick whichever is least invasive. Do **not** create a parallel `DriftResourceRenderer` class.
- The relevant-attributes table reuses the path formatter from `MarkdownHelpers` already used for `replace_paths`.
- Snapshot fixtures (per AC-11) must include: a plan with all three banner signals true individually, a plan with non-empty drift, a plan with non-empty relevant_attributes, and the negative case (1.13 plan with none of the fields → identical output).

## References

- Specification: [docs/features/122-terraform-1-15-support/specification.md](specification.md) §§ FR-H2.1–FR-H2.7, AC-5, AC-6, AC-7, AC-9
- Existing render order: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`
- Existing header rendering: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/HeaderRenderer.cs`
