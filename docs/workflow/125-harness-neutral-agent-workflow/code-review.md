# Code Review: 125-harness-neutral-agent-workflow

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-09-03

## Summary

Reviewed the committed origin/main...HEAD diff for workflow item 125. The branch is not release-ready: the workflow has several state-machine defects that can skip roles or bypass rejected gates, and tasks.md explicitly shows phases 4–7 unfinished. Uncommitted phase-4 files currently in the working tree were outside the specified diff and were not credited as implementation.

## Verification Results

The read-only sandbox prevented test execution. The work protocol records only three manual happy-path checks, with no commands, outputs, test plan, automated driver tests, coverage, or lint results. GitHub CI status could not be retrieved because network access failed. Static checks found git diff --check clean, compliant workflow: commit types, and no CHANGELOG.md or snapshot changes. Rendering validation was not applicable.

## What I Tried To Break

Checked first-run and rework stage advancement, accepted and rejected gate decisions, required-UAT handling and cold resume, feature/fix/workflow/website stage compatibility, referenced scripts and skills at HEAD, CI adapter-drift wiring, commit hygiene, snapshots, and dirty-worktree scope. These probes exposed role skipping, rejection bypass, an unopened UAT gate, missing fix inputs, and release paths requiring artifacts they never schedule.

## Issues Found

### Blockers

- **Stage completion is performed twice and can skip the next role** — `.agents/skills/run-workflow/SKILL.md:17`
  The driver loop tells its caller to invoke wp-append.sh after a role finishes, while agent-runtime and every role definition require the spawned role to append its own entry before returning. Because wp-append.sh advances whatever stage is currently recorded without validating the supplied role, the caller's second append advances past the next role. Assign completion to exactly one actor and reject a --role value that does not match the current stage.
- **A rejected gate is treated as permission to continue** — `scripts/wp-append.sh:69`
  The gate command stores any non-empty decision verbatim, including rejected. workflow-next.sh blocks only when the value equals pending, so recording a rejection immediately unblocks the next stage. Validate gate decisions and route rejection to a blocked/rework state; the release gate must also fail unless required gates are explicitly approved.
- **The declared UAT gate is never opened** — `scripts/workflow-next.sh:61`
  workflow.json declares UAT as a before_stage gate, but wp-append.sh only processes after_stage gates. When UAT is required, workflow-next.sh proceeds directly to uat-tester without setting gates.uat to pending. The state therefore cannot persist the wait for Maintainer approval, and a cold resume can start UAT again instead of reporting the open gate.
- **Bug workflows send the Developer to nonexistent feature artifacts** — `.agents/roles/developer.md:31`
  The fix sequence supplies analysis.md and then invokes Developer, but the Developer role unconditionally reads tasks.md, specification.md, architecture.md, and test-plan.md and never reads analysis.md. Those files are not produced by the fix workflow, so the role cannot follow the diagnosed bug or its proposed failing test. Add workflow-type-specific inputs.
- **Workflow and website releases require a review they never schedule** — `.agents/roles/release-manager.md:33`
  Release Manager always requires code-review.md, while the workflow and website sequences contain only their entry role followed by Release Manager. Both workflows therefore reach a pre-flight requirement that no scheduled role can produce. Either schedule Code Reviewer for these workflow types or make the requirement conditional and document the alternative review gate.
- **The committed branch is an incomplete, non-runnable implementation of the design** — `scripts/workflow-next.sh:84`
  tasks.md marks the Codex reviewer not started, skills partial, and demolition and dry run not started. At committed HEAD, workflow-next.sh points to scripts/codex-review.sh even though that file does not exist, and canonical .agents/skills contains only agent-runtime and run-workflow despite mandatory references such as run-dotnet-tests. Uncommitted local files do not satisfy the reviewed diff.

### Majors

- **The new workflow state machine has no recorded automated verification** — `docs/workflow/125-harness-neutral-agent-workflow/work-protocol.md:60`
  No test plan or workflow-driver tests were added. The work protocol records only three happy-path assertions and provides no reproducible output; it does not cover rejected gates, duplicate completion, required UAT, malformed state, missing artifacts, or each workflow type. Add shell-level state-machine coverage and wire the agent/adapter validation into PR CI before relying on this driver.

## Decision

`VERDICT: REWORK`
