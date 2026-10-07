# Retrospective: 146-generic-resource-review-clarity

## Summary

Feature 146 reached approved code review and Maintainer UAT after one recorded
Developer rework. Review caught a generic-import classification gap and missing
coverage/lint evidence. Both were resolved before platform UAT. Publication succeeded
on GitHub and Azure DevOps, but required several command-scoped workarounds.

This report covers work through UAT evidence commit `d99bfd08`; release and main-repo
PR validation have not completed. The branch CI query using
`scripts/check-workflow-status.sh list --branch feature/146-generic-resource-review-clarity-implementation --limit 5`
on 2026-10-07 failed with `error connecting to api.github.com`. No CI pass or failure
is inferred from that connection error. The second Code Reviewer also recorded an
unreachable GitHub API in [code-review.md](code-review.md).

Evidence sources were read in role order: [work-protocol.md](work-protocol.md),
[state.json](state.json), `git log --oneline origin/main..HEAD`, both review revisions,
and the CI query. [uat-report.md](uat-report.md) supplies publication and approval
evidence. Existing validation was inspected; no tests were executed for this role.

## What Went Well

- Requirements, architecture, testing, and task planning supplied explicit acceptance
  mappings: 17 success criteria and T01–T18 in [test-plan.md](test-plan.md) and
  [tasks.md](tasks.md). The final review traces every criterion to implementation
  and automated coverage. This made the SC10 omission identifiable against a
  concrete requirement rather than an unstated preference.
- Review caused a reproducible correction. Initial review commit `167cc7bc` identified
  B1 for non-MsGraph housekeeping-only imports. Developer round 3 records a regression
  failing before the fix; `ba84aa61` applies shared exclusions. Final review commit
  `c3475f39` approves the correction. The final recorded Release result is 1450/1450,
  with CoverageEnforcer at 88.94% line and 80.19% branch coverage and lint passing six
  refreshed reports ([work-protocol.md](work-protocol.md), Developer round 3).
- Snapshot intent and documentation stayed reviewable. `6ca9b5c8` includes
  `SNAPSHOT_UPDATE_OK` and rationale; `02c388b1` documents behavior, and `c5e69da3`
  confirms accuracy after rework. The final reviewer confirmed commit policy and
  an untouched CHANGELOG ([code-review.md](code-review.md)).
- UAT exercised eight feature variants plus a regression report on each real
  platform. [GitHub #127](https://github.com/oocx/tfplan2md-uat/pull/127) and
  [Azure DevOps #113](https://dev.azure.com/oocx/test/_git/test/pullrequest/113)
  received matching-target reports; 24 feature reports were generated across three
  targets. Explicit visual approval was recorded separately from publication
  permission: “approved regarding the changes from this feature.” Gate commit
  `441e8ef0` and evidence commit `d99bfd08` retain that distinction
  ([uat-report.md](uat-report.md)).
- Sensitive-output follow-up was investigated against published comments. The
  synthetic marker appeared only in the authorized `--show-sensitive` variant,
  while default output masked it. The investigation and its scope are recorded in
  [uat-report.md](uat-report.md); the question did not establish a default secret leak.

## What Didn't

### Shared policy coverage stopped short of the generic case

T12 already required housekeeping-only imported state, yet initial tests and
implementation covered the fuller exclusion set in MsGraph while core excluded only
`id`. Initial review B1 (`167cc7bc`) gives a concrete unmapped `review_object` example
that omitted the required note. Passing 1449 tests did not prove the missing
provider-neutral case. Final review still suggests consuming the shared flag in
`MsGraphResourceViewModelFactory` to prevent duplicated policy drifting
([code-review.md](code-review.md)).

### Validation evidence arrived after the review handoff

Developer round 2 recorded a full suite and artifact generation, but first review M1
found no CoverageEnforcer or markdownlint result (`167cc7bc`). Heading/freshness checks
were insufficient evidence for those checks. Developer round 3 supplied the missing
results. Earlier NU1403 compiler/linker package hash failures also interrupted focused
validation (Developer rounds 1–2 in [work-protocol.md](work-protocol.md)); the later
successful hook/build does not establish the cause of the earlier failures.

### Environment and UAT tooling added avoidable recovery work

The protocol records missing jq, worktree write restrictions, and an initially rejected
Codex model slug. The first review used a disclosed subagent fallback; round 2 obtained
an actual CLI `gpt-6.1-sol` review. Availability must be verified in the environment
that runs the reviewer ([work-protocol.md](work-protocol.md), Code Reviewer entries).

UAT recorded argument-order rejection, absent gitlink checkouts, hardcoded `/tmp`
logs, Azure credential preflight differing from stored login, and one backslash lost
from approved bodies. Scoped scratch overrides, a tee shim, existing credentials in
child environment, and exact-body restoration recovered publication. These are
specific script-contract problems, not evidence that each platform render failed
([uat-report.md](uat-report.md), Problems and operational workarounds).

### Evidence has explicit limits

Bitbucket reports and automated target coverage exist, but real Bitbucket UI
validation is unavailable. The Azure DevOps app could not attach its PR URL, although
publication succeeded and the report preserves a direct link. Main-repo CI remains
unverified. These limits are recorded in [uat-report.md](uat-report.md) and
[code-review.md](code-review.md), and must remain visible at release handoff.

`state.json` records `attempts.developer = 1`; three Developer protocol entries include
initial work, continuation, and review rework. They do not establish three rejected
attempts. Its five open questions retain explicit assumptions about overrides,
reference bounds, labels, provider metadata, and output scope. No evidence shows those
assumptions were reversed; specification and UAT gates are approved. Their unresolved
list format should still be surfaced in the eventual PR description.

## Improvement Opportunities

These are proposed follow-up changes, not changes implemented by this retrospective.

| Problem and evidence | Change location | Verification method | Priority |
| --- | --- | --- | --- |
| Generic import case escaped planned T12 coverage; initial B1, `167cc7bc` | `.agents/roles/quality-engineer.md`: require mapped and unmapped examples when an acceptance rule is provider-neutral; `src/Oocx.TfPlan2Md/Providers/MsGraph/Models/MsGraphResourceViewModelFactory.cs`: assess consuming the shared import flag, as final review suggests | Future test plans name both categories; the existing generic housekeeping regression and nested substantive cases remain green; any Graph-specific exception is justified explicitly | High |
| First handoff lacked coverage/lint proof; initial M1, Developer round 3 | `.agents/roles/developer.md` and `.agents/skills/pre-pr-checklist/SKILL.md`: require a concise handoff matrix with command, source revision, exit status, and location for suite, CoverageEnforcer, lint, and artifact checks | A reviewer can find each required result at first handoff; heading checks cannot fill the lint result field | High |
| UAT options and escaped approved bodies were mishandled; UAT operational report | `scripts/uat-run.sh`, `scripts/uat-github.sh`, `scripts/uat-azdo.sh`: accept supported option ordering consistently and transfer multiline bodies through files or structured arguments | Exercise both option orders; compare a published body containing quoted keys and literal backslashes byte-for-byte to its approved body | High |
| UAT needed missing-checkout and scratch-log shims; UAT operational report | `scripts/uat-helpers.sh`, `scripts/uat-github.sh`, `scripts/uat-azdo.sh`: preflight checkout paths and use repository scratch for logs; `.agents/skills/run-uat/SKILL.md`: document supported credential preflight without printing secrets | A clean managed worktree gives actionable checkout diagnostics and completes publication with logs under `.tmp/`, without tee or credential shims | High |
| jq/model readiness discovered during roles; protocol Requirements Engineer and first Code Reviewer | `scripts/agent-doctor.sh`, `scripts/codex-review.sh`: report jq resolution and check the configured reviewer invocation in its actual CLI environment before the review starts | Missing jq or unsupported configured model produces an early actionable error; accepted model runs produce the actual CLI review and record model identity | Medium |
| Synthetic sensitivity comparison prompted a follow-up; UAT report and `d99bfd08` | `.agents/skills/run-uat/SKILL.md`: require the show-sensitive variant's heading/instructions to say it intentionally reveals synthetic fixture values and pair it with default masking evidence | Each synthetic sensitive marker appears only in explicitly labeled reveal variants; the manifest links the matching masked report | Medium |

## Automation Opportunities

- Add a handoff evidence manifest to existing validation tooling rather than a new
  parallel check implementation. Extend `.agents/skills/pre-pr-checklist/SKILL.md`
  to consume command outcomes and source revision. Verify it reports missing coverage
  and lint separately from a passing suite; this targets initial review M1.
- Add local wrapper contract checks for `scripts/uat-run.sh`, `scripts/uat-github.sh`,
  and `scripts/uat-azdo.sh` using mocked platform commands. Cover option permutations,
  scratch paths, and escaped body preservation before network publication. Verify
  the body from this cycle round-trips unchanged; this targets UAT workarounds.
- Extend `scripts/agent-doctor.sh` readiness reporting for managed worktrees, including
  required jq and UAT checkout paths. Verify a clean worktree identifies prerequisites
  before a role reaches its publication step; this targets recorded setup failures.

## Checklist

- [x] Read protocol, workflow state, branch history, review revisions, and UAT report.
- [x] Attempted branch CI inspection and recorded the connection limitation.
- [x] Distinguished one recorded rework from role continuations.
- [x] Cited evidence for findings and named change locations and verification methods.
- [x] Retained platform and release-validation limits without inferring unavailable results.
- [x] Kept proposals separate from implemented changes; modified only owned report and protocol.
- [x] Appended the Retrospective role entry using the repository helper.

Next action: the driver should hand off to Release Manager, carrying approved UAT,
the five recorded assumptions, and pending main-repo PR validation. The PR title and
body require the Maintainer's approval before creation; merge requires green PR
Validation. No further input is needed to complete this retrospective stage.
