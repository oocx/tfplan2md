# Code Review: Generic Resource Review Clarity

## Summary

Reviewed `origin/main...HEAD` through `02c388b1`, the approved specification,
architecture, test plan, task list, work protocol, implementation, tests, and rendered
artifacts. Rework is required for imported-state classification and missing validation
evidence. Source code was not modified and tests were not run during this review.

## Verification Results

- Developer records full Release TUnit: **1,449 passed, 0 skipped**; the normal
  commit hook subsequently built Release successfully after earlier NU1403 failures.
- Seven intentional snapshots were regenerated and verified. Commit `6ca9b5c8`
  includes `SNAPSHOT_UPDATE_OK` and explains identity, refactoring-label, and import-note
  baseline changes. Those changes agree with the feature intent.
- Demo artifacts were regenerated; their wrapper verifies presence, non-empty output,
  and headings. This is not markdownlint evidence.
- **No CoverageEnforcer output, markdownlint result, or PR CI result is available.**
  No PR exists yet. Platform legibility remains for the UAT stage.
- README, feature documentation, and report-style guidance were updated. CHANGELOG
  is untouched. Commit types follow the version-bump guardrail. Task 10 checkbox
  bookkeeping remains unchecked despite the Technical Writer entry and completed docs.
- The required `scripts/codex-review.sh` attempt exited 2 because the installed
  Codex CLI rejects `gpt-6.1-sol` for this ChatGPT account. This report is the requested
  GPT-Sol 6.1 subagent fallback review, not a Claude review.

## Specification Compliance

| Criteria | Implementing code | Proving tests / status |
| --- | --- | --- |
| SC1–SC3: ordered, usable, policy-safe identity | ResourceDisplayIdentityResolver; shared model fields | GenericResourceDisplayIdentityTests covers all six paths, scalar types, nested/literal distinction, action preference, sensitivity and unknown ancestors |
| SC4: override and CLI errors | CliParser; ReportModelBuilderOptions; CompositionRoot | CliParserTests and GenericResourceDisplayIdentityTests cover replacement, empty entries and nonmatching override |
| SC5: mapped identity compatibility | ResourceSummaryMappings.ResolveIdentityCandidatePaths; UsesGenericDisplayIdentityPolicy | GenericResourceDisplayIdentityTests mapped override cases; complete acceptance fixture includes mapped azapi |
| SC6: exact instances | TerraformResourceAddressFormatter; default/provider summary headers | ResourceInstanceSuffixTests covers all actions, numeric indices, indexed modules, dots/brackets and escaped quoted keys |
| SC7–SC8: pseudo/useful hints | ReferenceSelector | ReferenceSelectorTests and ReportModelBuilderComputedAttributeTests cover qualified pseudo references, useful hints and computed masking |
| SC9: stable refactoring address | ReportAssemblyStage; ReportRenderer | ResourceInstanceSuffixTests full-address/visible-identity assertions; intentional refactoring snapshots |
| SC10: imported prior-state context | MeaningfulStateWalker; ResourceChangeStage; MsGraphResourceViewModelFactory; note renderer | ImportedPriorStateContextTests and MsGraph tests cover null/id-only/substantive state, but non-MsGraph provider housekeeping paths are missing; **fails B1** |
| SC11–SC13: accessible settings, changes and counts | MsGraph factory/renderer; default renderer section hook | MsGraphProviderSettingsModelTests and RenderingTests cover create/import/update, added/removed/changed settings, both unchanged modes, masking and large values |
| SC14: output filtering | BuildOutputModels before assembly | ReportModelBuilderHideUnchangedOutputsTests covers effective no-ops, retained state/masking, empty module/root sections and absent outputs |
| SC15–SC16: defaults and integrated breadth | Existing rendering paths plus shared identity/settings changes | GenericResourceReviewClarityAcceptanceTests and reviewed snapshots; SC10 gap remains |
| SC17: supported targets | Existing target formatting and shared renderer | Acceptance/rendering tests parameterize GitHub, Azure DevOps and Bitbucket; actual platform UAT remains pending |

## What I Tried To Break

- Checked sensitivity union, root/ancestor markers, desired unknown values and prior
  fallback. Generic identities use the existing sensitivity helper and unknown walker;
  the tests exercise hidden and explicitly authorized values.
- Checked dotted keys versus nested properties, null/object/array candidates, false/zero,
  custom-order exhaustion and replacement actions. The resolver follows the documented
  candidate and state order without treating dotted literal keys as nested values.
- Traced exact suffix extraction with indexed modules, quoted dots/brackets and escaped
  quotes/backslashes. The scanner takes the original suffix instead of reconstructing it.
- Traced module-qualified pseudo references through classification after module context;
  useful each-value and variable/local references remain selectable.
- Traced setting additions/removals/unknowns and classification before changed counts.
  Changed settings remain primary; unchanged/create/empty-import desired settings remain
  available through the shared rendering hook.
- Checked root/module filtering before grouping, including output-only modules and
  sensitive retained values. The existing value-formatting path remains in use.
- Checked a non-MsGraph import with only root `url`, `api_version`, or
  `ignore_missing_property`: each incorrectly establishes meaningful prior state (B1).

## Issues Found

### Blockers

**B1 — Apply the specified housekeeping exclusions to every imported resource.**
`src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.cs:78` excludes
only `id`; line 230 uses that set for the shared flag. The MsGraph factory supplies
all four exclusions, but unmapped and other provider resources retain the incomplete
set. For an imported `review_object` with prior state
`{"url":"/existing","api_version":"v1.0","ignore_missing_property":false,"body":{}}`,
the walker returns meaningful state and the report omits the required empty-import
note. The specification's empty-import section and architecture both exclude these
four root paths generally. Extend the classification to satisfy that rule without
adding provider-type decisions to core. Add a failing generic-import regression for
these housekeeping-only states, retain nested substantive `body.url`/`body.id`
coverage, and verify provider renderers consume the resulting flag.

### Major

**M1 — Supply coverage and Markdown lint evidence.**
`docs/features/146-generic-resource-review-clarity/work-protocol.md:51` records
precise test totals but no coverage enforcement or markdownlint result. The artifact
wrapper's heading checks cannot establish Markdown lint cleanliness. Record the real
CoverageEnforcer line/branch results and exit status against the repository's required
thresholds, and markdownlint results for the generated review/demo Markdown. Resolve
any failures before returning for review; do not substitute test pass counts for
coverage. PR validation remains mandatory before eventual merge.

### Minor

None.

### Suggestions

Mark Task 10 acceptance checkboxes complete when its owning role confirms the
Technical Writer's delivered documentation, so the task list matches the protocol.

## Decision

**REWORK.** Resolve B1 and M1, then request another review. Actual platform acceptance
and PR validation remain later workflow responsibilities.
