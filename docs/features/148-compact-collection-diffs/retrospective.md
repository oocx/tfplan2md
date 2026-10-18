# Retrospective: 148-compact-collection-diffs

## Summary

Feature 148 (compact primitive collection diffs and resource-level collection drift)
shipped as v1.48.0 through PR #685 (merged as 4add0884). The feature itself was
delivered and the spec, architecture, tests and UAT gates all passed. The cost was in
the process around it: the Developer stage ran 6 times (`state.json` attempts:
developer 6, requirements-engineer 2), the Maintainer unblocked the run by hand twice,
CI failed twice after the Release Manager finished, and the published release body
omitted this feature's release notes.

Evidence base: `work-protocol.md`, `state.json`, `code-review.md`, the commit range
v1.47.0..v1.48.0 (ending 4add0884), `.github/workflows/release.yml`, and facts
supplied by the driver (CI failures, release body, stash incident, push permission).
CI run logs themselves were not re-read; those facts are the driver's account.

## What Went Well

- Open questions were recorded rather than blocking: eight entries in `state.json`,
  seven resolved by the Maintainer on 2026-10-16 in one pass, with
  `original_assumption` preserved. The gate design worked as intended.
- Maintainer reversals (per-entry masking instead of whole-collection masking, masked
  entries may carry change markers) were absorbed by a spec round (Requirements
  Engineer rounds 2 and 3), then Architect round 2, Quality Engineer round 2 and Task
  Planner round 2, in order, without role-boundary violations.
- Code review caught real defects: round 2 B1 (element-less equivalence), and the
  round 3 Developer entry shows the masked path used a separate `RawKey` the fix had
  missed. Rounds 3 to 6 ended with "Problems: None" from the reviewer.
- Pre-UAT corrections extended the UAT fixture (commit 1b7a60c2) to cover large
  collections and element-less states before the human gate.
- The workflow defect in code review was fixed within the cycle (8e38bf3e, per the
  driver, 36394b12 on the original branch).

## What Didn't

### 1. Reviewer demanded CI evidence that cannot exist yet

The Maintainer entry "code review accepted" states that `code-reviewer.md` step 2 and
`scripts/codex-review.sh` ask for PR CI status, but the PR is created later by the
Release Manager. Rounds 4, 5 and 6 each repeated the Major finding, and each routed
the run back to the Developer (Developer round 5 notes "needs the Maintainer to push";
round 6 notes coverage not re-measured). That is three of the six Developer attempts
spent on an unsatisfiable finding. Fixed in-cycle, but the class of defect (a role
checking for evidence produced by a later stage) was not found earlier.

### 2. Rework cap bypassed by hand, no unblock tool

`wp-append.sh` has `MAX_ATTEMPTS=3`; the Developer reached 6. Two Maintainer entries
("Maintainer decision (unblock)", "(unblock, round 5)") both say: "No script exists to
unblock a run; state.json was edited by hand." Hand edits of `state.json` bypass the
rule that only the driver writes it, and leave no structured trace except a free-text
work-protocol entry. Likewise the Requirements Engineer (round 2) had to write
question resolutions into `state.json` directly because `wp-append.sh` cannot resolve
questions.

### 3. Cross-role stale artifacts re-reported every round

Architecture section 7 (code review m3) and `uat-test-plan.md` step 4 (m5) were stale
after round 2. They were reported as "still open for other owners" by the Technical
Writer in rounds 2, 3, 4, 5 and 6, by the Developer in rounds 2 and 3, and fixed only
in the "pre-UAT corrections" entry. Ownership rules prevented the Developer and Writer
from fixing them, but nothing scheduled the Architect or Quality Engineer after the
review that raised them. Related: the Task Planner round 2 noted tasks 16-25 were
"checked by reading the code, not by running tests", and the UAT fixture lacked the
scenarios needed for the round 5 Blocker fix until the pre-UAT step.

### 4. Release notes dropped from the published release

The v1.48.0 GitHub release body shows only
`docs/features/151-contribution-guidelines/release-notes.md`. Three notes files changed
in v1.47.0..v1.48.0 (148, 149 workflow, 151); the "Find user-focused release notes"
step in `.github/workflows/release.yml` (lines 133 onward) selects only the file with
the highest work-item number. The Release Manager wrote 148's notes (f7e5d51a) and
logged no problem. User-visible feature notes were lost to a docs-only item with a
higher number. This is a release-pipeline defect, not a one-off.

### 5. CI failed twice after the Release Manager entry

- ba4a8426: NativeAOT lock files had to be refreshed for .NET 10.0.13 because CI's SDK
  moved; the local Homebrew SDK cannot generate correct locks, so they were rebuilt in
  the `mcr.microsoft.com/dotnet/sdk:10.0` container.
- 4add0884: the feature 148 UAT fixture was missing from the `.dockerignore`
  allowlist; the TUnit project links it, so the Docker test image failed with MSB3030.

The second is caused by this feature adding a linked file. The Developer ran tests
locally (non-Docker) and the Developer entries never mention the Docker test image.
Both surfaced only in PR CI, after the work protocol said done.

### 6. Environment and tooling friction

- UAT Tester: "stray git stash pop applied unrelated stash during cleanup, repaired by
  Maintainer reset". Worktrees share one stash list.
- UAT Tester: submodule gitlinks untracked on main; no AzDO token, so no AzDO PR.
- Push permission was denied until the Maintainer added a settings rule (driver).
- Requirements Engineer: host lacked jq; the Release Manager: Playwright rejected the
  OS (ubuntu26.04) and needed `PLAYWRIGHT_HOST_PLATFORM_OVERRIDE`.
- Developer round 1: `scripts/update-test-snapshots.sh` deletes snapshots it does not
  regenerate; regenerated by hand.
- Developer round 4: tests comparing two full renders flaked on the metadata
  timestamp (3 failures, did not reproduce).
- Technical Writer rounds 4 and 6: `scripts/markdownlint.sh` needs Docker; ran
  markdownlint-cli2 directly instead.

## Improvement Opportunities

| Problem | Change location | Verification | Priority |
|---------|-----------------|--------------|----------|
| Release body drops all but the highest-numbered work item's notes | `.github/workflows/release.yml`, "Find user-focused release notes" step: concatenate all changed `release-notes.md` files in range (ordered by number), or fail if more than one and not combined | Dry-run the step logic against v1.47.0..v1.48.0 and confirm output contains 148, 149 and 151; check next multi-item release body | High |
| No supported way to unblock a stalled run or resolve a question | `scripts/wp-append.sh`: add `--unblock --reason` (resets attempts, appends protocol entry) and `--resolve-question N --by --answer`; update `docs/workflow.md` | Bats/shell test for both options; next cycle's protocol has no "edited by hand" entries | High |
| Docker test context breaks when a test project links a new file | Add a check in `scripts/` (or pre-pr-checklist skill) that every file linked by `src/tests/**/*.csproj` is not excluded by `.dockerignore`; run from `workflow-gate.sh` or pre-PR | Reintroduce the missing allowlist entry in a scratch branch and confirm the check fails before CI | High |
| NativeAOT lock files drift when the CI SDK updates; local SDK cannot regenerate them | `.agents/roles/developer.md` and pre-pr-checklist: document the container-based regeneration command; consider a script `scripts/refresh-locks.sh` wrapping the `mcr.microsoft.com/dotnet/sdk:10.0` run | Script produces lock files identical to CI's on a clean tree; `git status` clean after CI restore | Medium |
| Roles request evidence produced by a later stage | `.agents/roles/code-reviewer.md`, `scripts/codex-review.sh` (already patched): add a lint in the sync script that flags role files mentioning PR CI before the Release Manager stage | Run the lint against all roles; grep shows no pre-PR role asking for PR CI | Medium |
| Stale artifacts owned by later-listed roles re-reported for five rounds | `docs/workflow.md` and the driver (`run-workflow` skill): after a code review whose findings name artifacts of other owners (m-level items), route them to the owning role in the same round instead of deferring to pre-UAT | Next cycle with such findings: count of rounds in which the same "still open for other owners" Problem appears drops to at most 1 | Medium |
| Stash list is shared across worktrees; a `git stash pop` hit another branch's stash | `.agents/roles/uat-tester.md`: forbid `git stash` and require `git worktree` isolation or `git stash pop <ref>` of a named ref created by the role | Role text diff; next UAT protocol has no stash incident | Medium |
| Push permission denied mid-cycle | Ship the needed Bash rule in checked-in `.claude/settings.json` (or document it in `agent-runtime`) | Fresh clone: Release Manager push succeeds without Maintainer action | Low |
| Release Manager did not log either the notes-drop or the CI fallout (it ended before CI finished) | `.agents/roles/release-manager.md`: require waiting for PR CI to finish and recording its outcome and any follow-up commits in the entry | Next Release Manager entry names the CI run result | Medium |
| Snapshot script deletes snapshots it does not regenerate; timestamp-flaky full-render tests | `scripts/update-test-snapshots.sh` (regenerate or skip, never delete); test base helper that hides metadata | Run script on clean tree: `git status` shows no deletions; repeat render-compare tests 20 times | Low |

## Automation Opportunities

- `scripts/refresh-locks.sh`: regenerate NativeAOT lock files in the official SDK
  container.
- `scripts/check-docker-context.sh`: verify linked test inputs survive `.dockerignore`.
- `scripts/wp-append.sh --unblock` and `--resolve-question` (see above).
- A release-notes aggregation script shared by `release.yml` and the Release Manager
  so the PR can preview the exact release body.

## Checklist

- [x] Release merged and verified before this report (PR #685, v1.48.0)
- [x] Evidence read: work protocol, state.json, code-review.md, git history, workflow file
- [x] Findings clustered by theme, each with evidence
- [x] Each improvement names a change location and verification
- [ ] Maintainer decision on which items go to Workflow Engineer (not started automatically)
