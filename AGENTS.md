# AGENTS.md

Canonical instructions for AI agents working in this repository. Harness-neutral;
`CLAUDE.md` points here. Keep this file short — it loads before every session.
Detail belongs in the files it links to, loaded only when needed.

## Project

`tfplan2md` converts Terraform plan JSON into human-readable markdown reports.
.NET 10 / C# 13, root namespace `Oocx.TfPlan2Md`, single CLI project plus test projects.
Full spec: [docs/spec.md](docs/spec.md).

## How work is organised

Work happens in **work items**, one folder and one branch each:

| Type | Branch | Folder |
|---|---|---|
| Feature | `feature/NNN-<slug>` | `docs/features/NNN-<slug>/` |
| Bug fix | `fix/NNN-<slug>` | `docs/issues/NNN-<slug>/` |
| Workflow | `workflow/NNN-<slug>` | `docs/workflow/NNN-<slug>/` |
| Website | `website/NNN-<slug>` | `docs/website/NNN-<slug>/` |

`NNN` is global and monotonic across all four types — never reuse a number for a
different type. On a collision at merge time, first PR to merge keeps the number;
the later one renumbers, including intra-doc links.

Each work item carries a `work-protocol.md` (audit log, every role appends) and a
`state.json` (machine-readable stage + gates). See [docs/workflow.md](docs/workflow.md).

## Roles

Work is done by **roles** — definitions in `.agents/roles/*.md`. A role is loaded
either as a subagent (preferred: isolated context) or adopted in-session.
Read the role file before acting as one; do not improvise a role's boundaries.

Sequencing is not in the role files. It is in `state.json`, driven by
`scripts/workflow-next.sh`. To start or continue a work item, use the
`run-workflow` skill.

## Skills

`.agents/skills/<name>/SKILL.md` is the **source of truth** for recurring procedures.
Before any non-trivial workflow task — PR creation or merge, rebase, merge conflicts,
UAT, releases, demo artifacts, screenshots, architecture docs, test runs — search
`.agents/skills/` and follow the skill instead of improvising. If no skill fits and
the task recurs, write one (`create-agent-skill`).

Operating conventions that apply to every role — committing, delegating, asking,
running tests, model tiers — live in `.agents/skills/agent-runtime/SKILL.md`.

## Coding rules

- Simple over clever. No features that were not asked for. No speculative abstraction.
- Check for existing code that does the job before adding more. Remove the old path
  when you replace it — never leave two implementations of one thing.
- Files over ~300 lines get refactored.
- Most restrictive access modifier that works. This is a CLI, not a library: `public`
  needs justification. Tests reach `internal` via `InternalsVisibleTo`.
- XML doc comments on all class members, including private. Explain *why*, not *what*.
  See [docs/commenting-guidelines.md](docs/commenting-guidelines.md).
- `_camelCase` private fields. Prefer immutable collection types (`IReadOnlyList<T>`)
  where mutation is not required. Prefer collection expressions, primary constructors,
  pattern matching, target-typed `new()`, expression-bodied members.
- **Bug fixes start with a failing test.** Do not begin a fix until a test reproduces it.
- Update documentation in the same change as the code.
- **Never edit `CHANGELOG.md`** — Versionize generates it in CI.

## Commits and branches

Conventional Commits, enforced by a git hook. Types: `feat`, `fix`, `docs`, `style`,
`refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`, `workflow`.

**Version-bump guardrail:** `feat:` and `fix:` cause Versionize to bump the released
version. A change touching only `.github/`, `.agents/`, `scripts/`, `docs/` or
`website/` must use `workflow:`, `docs:`, `chore:` or `ci:` instead.

Snapshot changes under `src/tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/` must be
intentional: regenerate with `scripts/update-test-snapshots.sh` and put
`SNAPSHOT_UPDATE_OK` in a commit message explaining why they are correct.

Commit before handing off to the next role, so the next role sees complete work.

## Terminal

- **Never call `dotnet test` directly.** Use the `run-dotnet-tests` skill — .NET 10 has
  two test runners and a direct call from the repo root fails with `MSB1001`.
- Run repo scripts directly (`scripts/uat-run.sh`), not through an interpreter
  (`bash scripts/uat-run.sh`). If a script is not directly invokable, fix it: add a
  shebang and the executable bit.
- Suppress pagers: `GH_PAGER=cat GH_FORCE_TTY=false` for `gh`, `AZURE_CORE_PAGER=cat`
  for `az`, `PAGER=cat` generally.
- Scratch files go in `.tmp/` inside the repo (`scripts/setup-tmp.sh`), never `/tmp`
  or `~`.
- Explain a command, then run it in the same turn. Do not ask for permission in prose —
  the harness prompts.
- When a command fails, diagnose and explain before retrying.

## GitHub

Prefer the repo wrappers, which encode policy the raw commands do not:
`scripts/pr-github.sh` (rebase-merge for linear history), `scripts/check-workflow-status.sh`,
`scripts/gh-release-view.sh`, `scripts/uat-run.sh`. Plain `gh` is fine for read-only
inspection the wrappers do not cover.

**Before creating any PR**, post the exact title and body for approval. Body template:

```markdown
## Problem
<why is this change needed?>

## Change
<what changed?>

## Verification
<how was it validated?>
```

Merging requires PR Validation to be green — branch protection cannot enforce this on
a private repo, so it is checked by hand.

## Asking the user

The workflow runs unattended. Stop only at the gates defined in
[docs/workflow.md](docs/workflow.md): specification approval, an architecture choice
with genuinely competing options, and UAT for user-visible output changes.

Away from a gate, do not block. Record the question **and the assumption you are
proceeding on** in `state.json` → `open_questions`, continue, and surface the list at
the next gate or in the PR description.

When you are genuinely blocked: say you are blocked in one sentence, summarise what is
done, state what remains, and ask one question.

## Tooling

`scripts/agent-doctor.sh` verifies the required toolchain; `scripts/setup-agent-tools.sh`
installs it. Required on dev machines, never in CI:

| Tool | Purpose |
|---|---|
| `ast-grep` | Structural C# search — matched nodes instead of whole files |
| `rtk` | Compresses command output before it reaches context |
| `codex` | Runs the Code Reviewer role in a different model family |

`sg` on Linux is util-linux's setgid binary, **not** ast-grep. Always invoke `ast-grep`
by full name.
