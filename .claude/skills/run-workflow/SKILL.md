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
# ... run that stage ...
scripts/wp-append.sh --role "<Role>" --summary "..." --artifacts "..." --problems "..."
# repeat
```

`workflow-next.sh` derives everything from the branch name and `state.json`. It costs
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

## Starting a new work item

The entry role creates the branch, the folder, `work-protocol.md` and `state.json`:

| Request | Entry role | Branch |
|---------|-----------|--------|
| New feature | Requirements Engineer | `feature/NNN-<slug>` |
| Bug | Issue Analyst | `fix/NNN-<slug>` |
| Workflow change | Workflow Engineer | `workflow/NNN-<slug>` |
| Website change | Web Designer | `website/<slug>` |

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

When the reviewer returns `VERDICT: REWORK`, UAT fails, or a build fails:

```bash
scripts/wp-append.sh --rework code-reviewer --reason "<what failed>"
```

This routes back to the Developer and increments `attempts`, which escalates the model
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
