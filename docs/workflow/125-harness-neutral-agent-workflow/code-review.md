# Code Review: 125-harness-neutral-agent-workflow

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-09-03

## Summary

The branch is not release-ready. Static review found multiple state-machine and role-ordering defects that prevent code review, UAT, website work, and release completion; the migration is also explicitly unfinished.

## Verification Results

The read-only sandbox prevented test execution. work-protocol.md records 31 workflow-driver assertions passing after the phase-5 fixes and 9 failures against the prior scripts, but provides no raw output. HEAD is not present on a remote branch, GitHub was unreachable, and no CI result or coverage report exists. Static checks found git diff --check clean, compliant workflow: commits, and no CHANGELOG.md or snapshot changes.

## What I Tried To Break

Checked stage advancement, review approval/rework, UAT ordering, premature gate decisions, release preflight and post-merge state, website initialization, architecture-gate signaling, adapter drift enforcement, report generation, incomplete tasks, commit hygiene, snapshots, and CI integration. These probes exposed several paths that deadlock or bypass required human gates.

## Issues Found

### Blockers

- **The migration is explicitly incomplete** — `docs/workflow/125-harness-neutral-agent-workflow/tasks.md:12`
  Tasks 6–8 remain partial or not started. Required canonical skills are still absent, the old Copilot instruction/agent corpus remains alongside the new implementation, validation/CI paths have not been migrated, and no end-to-end dry run exists. This contradicts the design decisions to drop Copilot and maintain one canonical implementation.
- **An approved Codex review never completes its workflow stage** — `scripts/codex-review.sh:155`
  The wrapper writes code-review.md and exits but never appends the Code Reviewer work-protocol entry or advances state. Because run-workflow assigns completion exclusively to the role, APPROVED leaves .stage at code-reviewer and reruns forever; REWORK reroutes without the required reviewer audit entry.
- **The UAT gate opens before the artifacts it asks the Maintainer to review exist** — `.agents/workflow.json:85`
  workflow-next blocks before uat-tester, while the gate prompt asks the Maintainer to review both UAT PRs. Those PRs are only created by the UAT Tester after the gate. Valid approval is therefore impossible, and the UAT Tester subsequently waits for another approval inside its own stage.
- **Release preflight requires the Release Manager's own future log entry** — `.agents/workflow.json:29`
  release-manager is included in every gate_blocking_stages list. The Release Manager runs workflow-gate.sh work-protocol before doing its work, but appends its entry only after release, so preflight necessarily fails on the current role.
- **Release notes are checked before their owning role has any step to create them** — `.agents/roles/release-manager.md:51`
  release-notes.md is owned by the Release Manager, no earlier role may create it, and the Release Manager's first step is to run preflight. Requiring the file during that preflight without first creating it blocks every new work item.
- **The release stage merges and deletes the branch before recording completion** — `.agents/roles/release-manager.md:63`
  The role merges/deletes the branch and waits for main CI before appending its protocol entry. That entry and state advancement cannot be included in the already-merged work, and feature/fix workflows cannot then execute the Retrospective stage that follows Release Manager.
- **Website workflows cannot initialize the state required by the driver** — `.agents/roles/web-designer.md:31`
  workflow.json expects docs/workflow/`<website-slug>`/state.json, while AGENTS.md declares no website work-item folder and Web Designer only creates the branch. workflow-next and wp-append therefore cannot locate state or work-protocol files for website work.
- **Gate decisions can be recorded when no gate is pending** — `scripts/wp-append.sh:65`
  wp-append validates the decision text but not the gate's current state. A caller can pre-approve UAT while it is n/a; workflow-next later sees approved and skips opening the mandatory human gate. The same flaw permits out-of-sequence specification and architecture decisions.
- **The advertised all-gates release check does not enforce gate states** — `scripts/workflow-gate.sh:100`
  The all check only prints state, checks protocol headings, and reports whether paths trigger UAT. It never fails for pending or rework gate values and explicitly swallows check_uat's nonzero result, so it can return success with unresolved required approvals.
- **The promised CI adapter-drift check is not wired into CI** — `scripts/sync-agent-config.sh:17`
  The design requires generated .claude content to be protected by a CI drift check, but no workflow invokes sync-agent-config.sh --check or validate-agents.py. The script advertises CI enforcement that does not exist, allowing canonical and generated instructions to diverge unnoticed.

### Majors

- **The architecture gate relies on an undocumented sentinel value** — `.agents/roles/architect.md:39`
  wp-append opens the architecture gate only when gates.arch equals exactly "contested", but the Architect role tells the agent to record the option count without specifying allowed values. Recording 2 or another natural representation silently bypasses the mandatory architecture-choice gate.
- **Workflow tests and agent validation are not executed by PR validation** — `.github/workflows/pr-validation.yml:120`
  The new driver test and rewritten agent validator are standalone scripts only. PR validation runs other shell tests but invokes neither, leaving the state machine and generated-agent invariants without regression protection. This also leaves part of the previous review's CI recommendation unresolved.
- **Generated review reports cannot contain the required specification-compliance matrix** — `.agents/codex-review-schema.json:6`
  The Code Reviewer role requires a Specification Compliance section mapping each criterion to implementation and tests. The JSON schema has no field for that evidence, and codex-review.sh never renders the section, so every wrapper-generated report omits a mandatory part of the review.

## Decision

`VERDICT: REWORK`
