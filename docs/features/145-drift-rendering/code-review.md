# Code Review: 145-drift-rendering

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-09-05

## Summary

The line-break escaping blocker from the prior review is fixed, but the branch remains unready. Required acceptance-test coverage is still incomplete, no authoritative test or coverage result is recorded or available from CI, a drift demo artifact is stale, and a documentation-only commit still violates the version-bump guardrail.

## Verification Results

The sandbox was read-only, so tests and CoverageEnforcer were not run. The Developer recorded no test command, pass/fail totals, coverage percentage, or enforcer result; round 2 explicitly says coverage expansion remains in progress. GitHub CI could not be inspected because api.github.com was unreachable. The three snapshot changes are authorized by commit d6da4ed4 with SNAPSHOT_UPDATE_OK and a relevant justification. CHANGELOG.md is untouched.

## Specification Compliance

1: GroupDrift (ReportModelBuilder.PlanContext.cs:95) groups matching tuples; Build_DriftModes...:120 partially tests it. 2: the key includes type/path/before/after at :106, but only differing values are tested; type/path and normalized-equivalence cases are missing. 3: DriftGroupModel and ReportRenderer.cs:104 implement summary fields; ReportRendererTests.cs:35 tests ordinary values. 4: ReportRenderer.cs:107 renders collapsed address lists; test :35 covers two addresses. 5: CliParser.cs:151 and :290 plus CliParserTests.cs:21 implement/test default and explicit modes; builder default/all equivalence is only partial. 6: SelectRelevantDrift at ReportModelBuilder.PlanContext.cs:80 uses display changes and ordinal equality; no-op is tested at :141, but fully suppressed and case-distinct membership are not. 7: None short-circuits at :52 and renderer emptiness at ReportRenderer.cs:90; model emptiness is tested, but rendered omission for None is not. 8: selection precedes grouping at :70-74 and is tested at :120. 9: filtering is applied at :63-68; no-op is tested at :110, but fully suppressed drift is not. 10: invalid/missing CLI values are implemented at CliParser.cs:290 and :382 and tested at CliParserTests.cs:30. 11: empty guards and existing empty/no-op snapshots cover ordinary absence, but no-heading output across all three modes is not proved. 12: not satisfied because the test-plan edge cases and suppression scenarios remain absent.

## What I Tried To Break

Checked empty and no-op drift, all/relevant/none selection, filtering-before-grouping, ordinal address correlation and ordering, grouping-key dimensions, duplicate addresses, masking/suppression coverage, unsafe HTML/backticks/CR/LF, collapsed markup, snapshots, demo artifacts, documentation, role entries, commit types, and CI evidence. The CR/LF implementation now normalizes line breaks, but several required branches remain untested and the checked drift demo still shows the obsolete resource-card layout.

## Issues Found

### Majors

- **Required drift acceptance coverage remains incomplete** — `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderPlanContextTests.cs:94`
  The implemented tests do not prove differences in every grouping-key component, normalized-equivalent raw values, multiple changed paths, duplicate-address removal, complete group ordering, fully suppressed planned and drift changes, sensitive masking, ordinal case-distinct relevance, single-member rendering, or heading absence across every mode. Several acceptance criteria therefore have code but no proving automated test, contrary to the specification and test plan.
- **No verifiable automated-test or coverage evidence** — `docs/features/145-drift-rendering/work-protocol.md:46`
  The Developer entries provide no executed command, pass/fail totals, coverage result, or CoverageEnforcer output. CI was also unavailable for inspection. The round-2 entry explicitly states that full coverage expansion remains in progress, so correctness cannot be established from the available evidence.
- **Drift demo artifact still uses the obsolete rendering** — `artifacts/drift-single-entry-plan.md:28`
  The tracked drift demo still renders a provider-specific resource card and attribute table instead of the new grouped collapsed summary. Rendering changes require regenerated demo artifacts; this stale artifact also means there is no reviewed demo evidence for the new output.
- **Documentation-only commit uses a version-bumping commit type** — `docs/features/145-drift-rendering/specification.md:1`
  Commit 1a6ace64 changes only documentation under the work-item folder but is titled `feat: specify configurable drift rendering`. AGENTS.md requires docs/workflow/chore/ci for documentation-only changes because `feat:` triggers an unintended release bump. Rewrite the commit type.

## Decision

`VERDICT: REWORK`
