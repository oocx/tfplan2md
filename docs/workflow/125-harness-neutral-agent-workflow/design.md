# Design: Harness-Neutral Agent Workflow

**Work Item:** `docs/workflow/125-harness-neutral-agent-workflow/`
**Branch:** `workflow/125-harness-neutral-agent-workflow`
**Created:** 2026-09-03

## Problem

The agent workflow was built for GitHub Copilot under premium-request billing, where
token count did not affect cost. Three consequences:

1. **Every agent exists twice** — 26 files, 9,820 lines, for 13 roles. Diffing
   `developer.agent.md` against `developer-coding-agent.agent.md` shows identical role
   content; every delta is plumbing (`report_progress` vs `git commit`, `askQuestions`
   vs PR comment, VS Code tool-ID lists).
2. **The preamble is enormous** — `.github/copilot-instructions.md` (247 lines) plus
   `docs/agents.md` (896) load before any work starts: ~14k tokens per session.
3. **It only runs in Copilot** — Copilot frontmatter (`target:`, `tools:`), Copilot
   model names, Copilot hook schema, Copilot prompt files.

## Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | Drop Copilot support entirely | Removes `report_progress`, the coding-agent machinery, and 13 duplicate role files. |
| 2 | Claude runs the full workflow | Single target; subagents give per-role context isolation. |
| 3 | Codex runs the Code Reviewer role | A reviewer sharing the author's model and context ratifies the author's reasoning. Cross-family review tests it instead. |
| 4 | Canonical `.agents/`, generated `.claude/` | Harness-neutral source, one adapter, CI drift check. Keeps a future port cheap. |
| 5 | Auto mode with three gates | Spec approval (always), architecture choice (when options genuinely compete), UAT (when user-visible output changed). |
| 6 | `ast-grep` + `rtk` + `codex` required on dev machines | Different token axes; none required in CI. |
| 7 | Requirements Engineer at deep tier | Requirement errors propagate through every later stage, so they cost the most to unwind. |

## Architecture

### Roles

`.agents/roles/*.md` — 13 harness-neutral definitions, ≤130 lines each. A role file
declares `name`, `description` and `tier`; it never names a model. It states only what
is specific to that role, deferring shared rules to `AGENTS.md`.

Sequencing is deliberately **not** in role files. There is no `handoffs:` block. The
next stage comes from `state.json`.

### Tiers

`.agents/tiers.json` is the single source of truth. Roles declare a tier; the sync
script resolves it to a Claude model and `codex-review.sh` resolves it to a Codex model.

| Tier | Claude | Codex | Roles |
|------|--------|-------|-------|
| deep | opus | sol | requirements-engineer, architect, code-reviewer, workflow-engineer |
| standard | sonnet | terra | issue-analyst, quality-engineer, developer, release-manager, retrospective, web-designer, uat-tester |
| cheap | haiku | luna | task-planner, technical-writer, search and test-run subagents |

A role escalates one tier on rework attempt 2, so cheap models get first crack and
expensive ones only see hard cases.

### Driver and state

The workflow lives in a driver skill plus `state.json` per work item, not in an
orchestrator agent. With Claude-only execution a Task-tool orchestrator would work, so
this needs its own justification: a long unattended run accumulates context in whatever
agent holds the thread, and when that session compacts or dies the run is
unrecoverable. State on disk makes stage resolution deterministic, near-free in tokens,
and resumable from a cold session.

```
scripts/workflow-next.sh   state -> next stage + the exact prompt for it
scripts/workflow-gate.sh   gate evaluation + work-protocol completeness
scripts/wp-append.sh       append a work-protocol entry, advance state
```

### Gates

Away from a gate, roles do not block. A role that hits ambiguity records the question
**and the assumption it is proceeding on** into `state.open_questions`, continues, and
the list surfaces at the next gate or in the PR description.

The UAT gate is a path rule, not a judgement:

```bash
git diff --name-only origin/main...HEAD | grep -qE \
  '^(src/Oocx\.TfPlan2Md/(MarkdownGeneration|RenderTargets)/|examples/|website/)'
```

### Code review via Codex

`scripts/codex-review.sh` is the only place Codex CLI flags exist. It passes the role
file and a diff range; Codex reads `AGENTS.md` natively for project conventions. The
review must end with `VERDICT: APPROVED` or `VERDICT: REWORK` — the driver parses that
line, never prose, and treats a missing verdict as `REWORK`.

If `codex` is absent or fails, the wrapper retries once, then falls back to a Claude
reviewer and records `reviewer: claude-fallback` in `work-protocol.md`, so an
unattended run never stalls and the retrospective can see the review was single-family.

## Token strategy

Cost accrues on four independent axes. The first two are architecture, not purchases,
and are worth more than the second two combined.

| Axis | Instrument | Effect |
|------|-----------|--------|
| Corpus size | Role merge, progressive disclosure | ~14k → ~2k tokens per session |
| Working context | Subagent isolation per role | A stage's reads never enter the parent thread |
| Reads | `ast-grep` | Matched AST nodes instead of whole C# files |
| Command output | `rtk` | Compresses git, test, lint, `gh`, docker output |

**RTK caveat.** Its compression is lossy by design (dedup, truncation, grouping). That
is free money on `git status` and dangerous for the Codex reviewer reading a diff and
for a developer diagnosing a test failure. `tee` mode stays on so full output survives
on failure, and the reviewer's diff path is excluded in `~/.config/rtk/config.toml`.
A truncated diff produces a confident review of code the reviewer never saw.

**RTK collision.** RTK's auto-rewrite hook matches `git status`; this repo's
conventions send agents to `scripts/git-status.sh`, a script invocation the hook does
not match. Those wrappers existed to reduce VS Code approval friction, which dies with
Copilot. They are retired so RTK can match the underlying commands; a Claude permission
allowlist replaces what they bought. Wrappers enforcing real policy (`pr-github.sh` and
its rebase-merge rule) stay.

**Rejected:** `tokei` counts lines by language — `wc -l` already does the one job it
had here. `code2prompt` is optional; roles need known paths, not glob discovery.

## Phases

Phases 1–5 are additive and reversible; phase 6 is the point of no return. See
[tasks.md](tasks.md) for status.

## Consequences

- Copilot can no longer run this workflow. `AGENTS.md` remains readable by any agent,
  but the cloud coding-agent path is gone.
- Dev machines need a Rust toolchain and the Codex CLI. CI does not.
- `docs/ai-model-reference.md` (644 lines of Copilot benchmark data) is deleted; tier
  selection is now a four-row table.
- Losing per-role `tools:` lists means roles run with the harness default toolset.
  This trades fine-grained restriction for the elimination of the single largest source
  of drift in the old corpus. Role boundaries are enforced by instruction and by the
  work-protocol gate rather than by tool availability.
