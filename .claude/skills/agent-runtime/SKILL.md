---
name: agent-runtime
description: Operating conventions shared by every role - delegating, committing, asking, running tests, model tiers, and artifact ownership. Load this before acting as any role.
---

# Agent Runtime

Conventions every role obeys. Role files under `.agents/roles/` state only what is
specific to their role and defer everything here. [AGENTS.md](../../../AGENTS.md)
carries the project-wide coding, commit and terminal rules; this skill covers how a
role *operates*.

This is also the only file that needs to change to support a second harness.

## Running as a role

Prefer **spawning the role as a subagent** over adopting it in-session. The isolated
context is the main reason this workflow is affordable: a stage's file reads, test
output and search results never enter the parent thread.

Adopt a role in-session only when you need its output in your working context to
continue immediately.

Read the role file before acting as one. Do not improvise a role's boundaries — they
exist to keep artifact ownership clean, and the work-protocol gate checks that the
right roles ran.

## Model tiers

`.agents/tiers.json` is the single source of truth. Role files declare `tier:`, never
a model name.

| Tier | Claude | Codex |
|------|--------|-------|
| deep | opus | `gpt-5.6-sol` |
| standard | sonnet | `gpt-5.6-terra` |
| cheap | haiku | `gpt-5.6-luna` |

Codex needs the full model slug. Bare `sol` / `terra` / `luna` are rejected by the API
("not supported when using Codex with a ChatGPT account").

**Escalation:** on rework attempt 2 or later, run the role one tier deeper. Cheap
models get first crack; expensive ones only see the cases that failed once.

Delegate to the **cheap** tier for work whose output you do not need verbatim:
codebase search, "does this file exist", build and test runs where only pass/fail
matters. Do not delegate single file reads or one grep — the spawn costs more than
the call.

## Committing

Commit with `git` directly. Conventional Commits, types per AGENTS.md.

- **Commit before handing off.** The next role must see complete work.
- **Amend, don't stack fixups.** To correct work you just committed, use
  `git commit --amend`. Never create a "fix the previous commit" commit.
- Commit only files your role owns (see below).

## Asking

The workflow runs unattended. There are exactly three gates — specification approval,
a genuinely contested architecture choice, and UAT for user-visible output changes.
They are defined in [docs/workflow.md](../../../docs/workflow.md).

**Away from a gate, do not block.** Record the question *and the assumption you are
proceeding on* in `state.json` → `open_questions`, then continue:

```bash
scripts/wp-append.sh --question "Should X do Y?" --assumed "Yes, matching Z"
```

The accumulated list surfaces at the next gate and in the PR description. This is a
deliberate inversion of the old "ask one question at a time and wait" rule, which made
unattended runs impossible.

**At a gate**, ask one question at a time and wait. If you offer alternatives, give
pros and cons and a recommendation with its reason.

When genuinely blocked: say you are blocked in one sentence, summarise what is done,
state what remains, and ask one question.

## Running tests

Use the `run-dotnet-tests` skill. Never call `dotnet test` directly — .NET 10 has two
test runners and a direct call from the repo root fails with `MSB1001`.

Test naming: `MethodName_Scenario_ExpectedResult`. TUnit is the only test framework in
this project.

## Work protocol

Every role appends an entry to `<work-item>/work-protocol.md` after completing its work
and before handing off:

```bash
scripts/wp-append.sh --role "Developer" \
  --summary "..." --artifacts "..." --problems "None"
```

Record problems honestly — the Retrospective reads them, and a problem that was never
logged cannot be fixed. `scripts/workflow-gate.sh` blocks release when a required role
has no entry.

## Artifact ownership

Each artifact has exactly one owning role. Do not edit another role's artifacts; hand
back instead.

| Artifact | Owner |
|----------|-------|
| `src/`, `src/tests/` | Developer |
| `docs/`, `README.md` (global docs) | Technical Writer |
| `docs/workflow.md`, `AGENTS.md` | Workflow Engineer |
| `specification.md` | Requirements Engineer |
| `analysis.md` | Issue Analyst |
| `architecture.md`, `docs/adr-*.md` | Architect |
| `test-plan.md`, `uat-test-plan.md` | Quality Engineer |
| `tasks.md` | Task Planner |
| `code-review.md` | Code Reviewer |
| `uat-plan.json`, `uat-plan.md` | Developer (to the Quality Engineer's specification) |
| `uat-report.md` | UAT Tester |
| UAT PRs and their comments | UAT Tester |
| `release-notes.md`, the PR, the release | Release Manager |
| `retrospective.md` | Retrospective |
| `.agents/`, `AGENTS.md`, `scripts/` workflow tooling | Workflow Engineer |
| `website/src/` | Web Designer |

`work-protocol.md` is append-only, and only through `scripts/wp-append.sh`.

`state.json` belongs to the driver. Roles never write `stage`, `status` or `gates.*`
directly — a role that could write a gate field could clear a rejection the Maintainer
had already made. The one field a role sets is `arch_contested`, and only the Architect
sets it.

## Reporting

End a turn with what changed, what is next, and what you need — or "Nothing" if
unblocked. Keep it short; the work-protocol entry is the durable record, not the chat.

When handing off, state what you completed, which files you touched, and the specific
next action for the receiving role.
