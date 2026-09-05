# Retrospective: Drift Rendering (Feature 145)

**Date:** 2026-09-05
**Evidence reviewed:** `work-protocol.md`, `state.json`, `code-review.md`, `uat-report.md`, branch commits, and workflow-status check.

## Summary

Feature 145 reached approved UAT with 1,375 passing tests and coverage above the
enforced thresholds. The workflow caught several correctness and rendering defects
before UAT, but required four Developer rework attempts. GitHub UAT passed; the
Maintainer explicitly waived Azure DevOps UAT because credentials were unavailable.

## What Went Well

- The final independent review approved the implementation after confirming 1,375
  passing tests, 88.80% line coverage, and 79.95% branch coverage
  (`code-review.md`, **Verification Results**).
- Review found the correctness gaps before release: unsafe CR/LF rendering,
  unchanged drift attributes under `--show-unchanged-values`, and the missing
  Markdown boundary in the generated UAT report. The final review records no
  remaining findings (`work-protocol.md`, Code Reviewer rounds 1–5;
  `code-review.md`, **Issues Found**).
- The generated UAT report was rendered and reviewed on GitHub UAT PR #126. The
  Maintainer confirmed it passed, covering the feature-specific and regression
  reports (`uat-report.md`, **GitHub**).
- The UAT decision is unambiguous despite unavailable Azure DevOps credentials:
  `uat-report.md` records the Maintainer's explicit waiver, and `state.json` marks
  the UAT gate approved.

## What Didn't

- `state.json` records four Developer attempts. The associated review rounds found
  a blocker and three majors, then four majors, then a blocker and a major, then a
  stale generated artifact. This is a concentrated rework loop rather than isolated
  test failures (`work-protocol.md`, Code Reviewer rounds 1–4).
- The initial implementation and its first corrections did not exercise the full
  interaction matrix: grouping keys, unsafe text, ordinal matching, suppressed and
  unchanged attributes, CLI positional input, and generated Markdown structure were
  added only after review (`work-protocol.md`, Developer rounds 2–5;
  commits `6b95e9b5`, `0855d2ab`, `ccf59c04`, `ec3076f3`).
- Three focused test runs did not run tests because their selector syntax or wrapper
  directory was wrong. Those mistakes were corrected, but they delayed feedback
  (`work-protocol.md`, Developer round 3).
- The normal workflow-status CI check could not reach GitHub, so there is no remote
  CI result for the reviewed revision. The final approval therefore relied on
  independent local validation and UAT evidence (`code-review.md`, **Verification
  Results**; retrospective evidence collection on 2026-09-05).

## Improvement Opportunities

| Priority | Problem and evidence | Change location | Verification |
|---|---|---|---|
| High | Four Developer attempts show that the review-sensitive edge cases were not all proved before the first review. | `.agents/skills/pre-pr-checklist/SKILL.md` — require a feature-to-test matrix review for every specification acceptance criterion, including rendering boundaries and option interactions. | First Code Reviewer pass has no blocker or major caused by a missing test case; record this in the next comparable work item. |
| High | A regenerated UAT artifact was stale and omitted the blank line required to terminate an HTML block. | `.agents/skills/generate-demo-artifacts/SKILL.md` and `.agents/skills/pre-pr-checklist/SKILL.md` — add a final regeneration plus structural Markdown assertion for UAT artifacts. | A deliberately stale artifact is detected before code review; generated UAT artifacts pass the assertion in CI or the pre-PR checklist. |
| Medium | Two focused test commands reported success without running tests due to unsupported selectors or a wrong directory. | `.agents/skills/run-dotnet-tests/SKILL.md` — document supported focused-test invocation and require checking a nonzero executed-test count. | A documented focused command runs the intended class, and an invalid selector fails clearly rather than yielding zero tests. |
| Medium | The reviewer wrapper was blocked by an unrelated invalid template and Markdown linting could not run without Docker. | `.agents/skills/pre-pr-checklist/SKILL.md` — add early tool-availability checks and specify the approved fallback evidence for unavailable container tooling. | The checklist reports unavailable dependencies before review and the fallback produces a complete, reproducible validation record. |

## Automation Opportunities

- Add a lightweight UAT-artifact validation script that checks generated Markdown
  headings following HTML blocks; wire it into the pre-PR checklist.
- Extend the test-run wrapper guidance with a focused-test helper that validates the
  selector and reports the executed-test total.

## Checklist

- [x] Reviewed work protocol, workflow state, review report, UAT report, branch history, and CI-status evidence.
- [x] Recorded the Maintainer-approved Azure DevOps UAT waiver as part of the UAT evidence.
- [x] Clustered evidence by validation, rework, and tooling themes.
- [x] Provided a change location and verification method for every improvement opportunity.
- [x] Did not modify artifacts owned by other roles.
