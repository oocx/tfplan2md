---
name: run-workflow
description: Drive a work item through the workflow in auto mode - resolve the next stage, run it, advance state, and stop only at gates.
---

# Run Workflow

The driver. Runs a work item from its current stage to completion, stopping only at
the three gates. Read [docs/workflow.md](../../../docs/workflow.md) for the stages and
gate rules, and the `agent-runtime` skill for how roles operate.

## The loop

```bash
scripts/workflow-next.sh          # what runs next, and with which model
# ... run that stage; the role appends its own work-protocol entry ...
scripts/workflow-next.sh          # repeat
```

**You do not call `wp-append.sh --role`.** The role that ran the stage appends its own
entry before returning, and that append is what advances the stage. A second append
from the driver would advance past whichever role is now current, silently skipping it.
`wp-append.sh` refuses a `--role` that does not match the current stage, so a duplicate
call fails loudly rather than corrupting the run — but the rule is that completion has
exactly one owner.

The driver does use `wp-append.sh` for `--gate` and `--rework`, which are its own
decisions rather than a role's.

`workflow-next.sh` derives everything from the branch name and `state.json`. Note that
it is not purely a query: it advances past a skipped UAT stage and records that UAT is
required, so calling it is part of driving the run rather than inspecting it. Use
`scripts/workflow-gate.sh status` when you only want to look. It costs
almost nothing and needs no memory of previous turns, so a session that compacted or
died resumes by calling it again.

Exit codes: `0` a stage is ready, `2` blocked at a gate.

## Running a stage

Spawn the role as a **subagent** with the model `workflow-next.sh` reports — that
isolation is why this workflow is affordable. Give it:

> Act as the `<Role>` role. Read `.agents/roles/<stage>.md` and the `agent-runtime`
> skill, then do your stage for the work item in `<work-item-dir>`.

Do not paste the role file into the prompt; the subagent reads it. Do not add your own
instructions about how to do the role's job — if they were needed, they belong in the
role file.

**One exception:** the `code-reviewer` stage runs in Codex, not as a subagent:

```bash
scripts/codex-review.sh <work-item-dir>
```

Its exit code decides what happens next, and it advances the stage itself:

| Exit | Meaning | What you do |
|------|---------|-------------|
| 0 | APPROVED | Continue the loop |
| 1 | REWORK | Continue the loop — it has already routed back to the Developer |
| 2 | codex unavailable or failed twice | Spawn the `Code Reviewer` subagent instead, and pass `--problems "reviewer: claude-fallback"` on its work-protocol entry so the retrospective can see the review was single-family |

Exit 2 is **not** a rework signal. Treating it as one sends the Developer back for a
review that never happened.

## Starting a new work item

The entry role creates the branch, the folder, `work-protocol.md` and `state.json`:

| Request | Entry role | Branch |
|---------|-----------|--------|
| New feature | Requirements Engineer | `feature/NNN-<slug>` |
| Bug | Issue Analyst | `fix/NNN-<slug>` |
| Workflow change | Workflow Engineer | `workflow/NNN-<slug>` |
| Website change | Web Designer | `website/NNN-<slug>` |

Reserve `NNN` with the `next-issue-number` skill. Do not ask which role to start with —
determine it from the request and delegate; the entry role asks the clarifying
questions, because that is its job and not yours.

## Gates

When `workflow-next.sh` exits 2, stop and put the question to the Maintainer, along with
any `open_questions` accumulated since the last gate. Then record the answer:

```bash
scripts/wp-append.sh --gate spec --decision approved
```

`arch` is only pending when the Architect wrote `contested` into it. `uat` is skipped
automatically when the diff does not touch user-visible output — the driver reports the
skip and moves on.

## Rework

**The code reviewer routes its own rework.** `codex-review.sh` calls `--rework` itself
before exiting 1, so do not call it again — a second call counts the same rejection
twice and can block the run after two failed reviews instead of three.

For a UAT failure, use the gate; for a build failure, call rework yourself:

```bash
scripts/wp-append.sh --gate uat --decision rejected      # UAT failed
scripts/wp-append.sh --rework release-manager --reason "PR validation failed"
```

Both route back to the Developer and increment `attempts`, which escalates the model
one tier. Escalating forever is not a strategy: **after three attempts at the same
stage, stop and involve the Maintainer.** Repeated failure at one stage is usually a
specification problem wearing an implementation costume.

## Before release

```bash
scripts/workflow-gate.sh all
```

Non-zero means a required role never logged its work, and the release does not proceed.

## What not to do

- Do not do a role's work yourself. If the Developer's stage is next, spawn the
  Developer — writing the code yourself defeats the isolation the workflow is built on.
- Do not skip a stage because it seems unnecessary. The work-protocol gate will refuse
  the release, and you will have lost the intervening work.
- Do not block on a question away from a gate. Record it with an assumption
  (`wp-append.sh --question ... --assumed ...`) and continue.
