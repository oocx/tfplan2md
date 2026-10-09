# Workflow

How work moves from a request to a release. [AGENTS.md](../AGENTS.md) covers project
rules; this file covers stages, gates and artifacts. Role definitions live in
`.agents/roles/`.

The workflow runs unattended between gates. Roles do not decide the sequence — it comes
from `state.json`, resolved by `scripts/workflow-next.sh`.

## Workflow diagram

Read the workflows from left to right, and each numbered sequence from top to bottom.
Workflow improvement is a separate process started only at the Maintainer's request.
This diagram is the source for the website's `ai-workflow.svg`; regenerate it with the
`update-workflow-diagram` skill after changing it.

```mermaid
%%{init: {'theme':'dark', 'flowchart': {'curve':'linear', 'nodeSpacing':30, 'rankSpacing':35}, 'themeVariables': { 'fontSize':'16px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart LR
    classDef role fill:#3b82f6,stroke:#60a5fa,stroke-width:2px,color:#ffffff;
    classDef meta fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff;
    classDef gate fill:#f59e0b,stroke:#fbbf24,stroke-width:2px,color:#ffffff;
    classDef external fill:#ec4899,stroke:#f472b6,stroke-width:2px,color:#ffffff;

    subgraph FEATURE["Feature"]
        direction TB
        F_RE["1. Requirements Engineer"] --> F_SPEC{{"Specification approval"}}
        F_SPEC --> F_AR["2. Architect"]
        F_AR --> F_ARCH{{"Architecture choice<br/>only when options compete"}}
        F_ARCH --> F_QE["3. Quality Engineer"]
        F_QE --> F_TP["4. Task Planner"]
        F_TP --> F_DEV["5. Developer"]
        F_DEV --> F_TW["6. Technical Writer"]
        F_TW --> F_CR["7. Code Reviewer<br/>runs in Codex"]
        F_CR --> F_UAT["8. UAT Tester<br/>only when output changes"]
        F_UAT --> F_UAT_GATE{{"UAT approval"}}
        F_UAT_GATE --> F_RM["9. Release Manager"]
        F_RM -->|"release verified"| F_RETRO["10. Retrospective"]
        F_CR -->|"UAT skipped"| F_RM
    end

    subgraph BUG["Bug fix"]
        direction TB
        B_IA["1. Issue Analyst"] --> B_DEV["2. Developer"]
        B_DEV --> B_TW["3. Technical Writer"]
        B_TW --> B_CR["4. Code Reviewer<br/>runs in Codex"]
        B_CR --> B_UAT["5. UAT Tester<br/>only when output changes"]
        B_UAT --> B_GATE{{"UAT approval"}}
        B_GATE --> B_RM["6. Release Manager"]
        B_RM -->|"release verified"| B_RETRO["7. Retrospective"]
        B_CR -->|"UAT skipped"| B_RM
    end

    subgraph WEBSITE["Website"]
        direction TB
        S_WD["1. Web Designer"] --> S_GATE{{"UAT approval"}}
        S_GATE --> S_RM["2. Release Manager"]
    end

    subgraph MAINTENANCE["Workflow improvement"]
        direction TB
        W_WE["1. Workflow Engineer<br/>Maintainer request only"] --> W_RM["2. Release Manager"]
    end

    FEATURE ~~~ BUG ~~~ WEBSITE ~~~ MAINTENANCE
    class F_RE,F_AR,F_QE,F_TP,F_DEV,F_TW,F_UAT,F_RM,F_RETRO,B_IA,B_DEV,B_TW,B_UAT,B_RM,B_RETRO,S_RM,W_RM role;
    class F_SPEC,F_ARCH,F_UAT_GATE,B_GATE,S_GATE gate;
    class F_CR,B_CR external;
    class S_WD,W_WE meta;
```

The Maintainer approves the gates. Architecture approval is needed only when options
compete; UAT and its approval are skipped when output does not change. Retrospective
follows the completed, verified feature or bug-fix release and reports findings to the
Maintainer. Rework and audit links are described below to keep the diagram focused on
the forward sequence.

## Stages

### Feature — `feature/NNN-<slug>` → `docs/features/NNN-<slug>/`

| # | Role | Produces |
|---|------|----------|
| 1 | Requirements Engineer | `specification.md`, `work-protocol.md`, `state.json` |
| — | **GATE: specification approval** | always human |
| 2 | Architect | `architecture.md`, `docs/adr-*.md` |
| — | **GATE: architecture choice** | only when options genuinely compete |
| 3 | Quality Engineer | `test-plan.md`, `uat-test-plan.md` |
| 4 | Task Planner | `tasks.md` |
| 5 | Developer | code, tests, `uat-plan.json`, `uat-plan.md` |
| 6 | Technical Writer | updated global documentation |
| 7 | Code Reviewer | `code-review.md` + verdict |
| 8 | UAT Tester | UAT PRs in GitHub and Azure DevOps, `uat-report.md` |
| — | **GATE: UAT** | only when user-visible output changed — decided after the PRs exist |
| 9 | Release Manager | PR, `release-notes.md`, the release |
| 10 | Retrospective | post-release analysis in `retrospective.md` |

### Bug fix — `fix/NNN-<slug>` → `docs/issues/NNN-<slug>/`

Issue Analyst (`analysis.md`) → Developer → Technical Writer → Code Reviewer →
UAT Tester (if applicable) → Release Manager → Retrospective.

### Website — `website/NNN-<slug>` → `docs/website/NNN-<slug>/`

Web Designer → **GATE: UAT** → Release Manager. A website change is user-visible by
definition, so the gate fires whenever the diff touches `website/`.

### Separate process: workflow improvement — `workflow/NNN-<slug>` → `docs/workflow/NNN-<slug>/`

The Maintainer starts this process when they want to improve the development workflow.
It is not an automatic entry point in the feature, bug-fix, or website process and is
not dispatched automatically from a retrospective.

Workflow Engineer → Release Manager. UAT does not apply.

## Gates

Three, and only three. Everything else runs unattended.

| Gate | Fires | Decided by |
|------|-------|-----------|
| Specification | Every feature | Always the Maintainer |
| Architecture | Two or more viable options with material trade-offs | Maintainer; otherwise the Architect decides and records the ADR |
| UAT | The diff touches user-visible output, **and** the work type has a UAT gate | Maintainer's explicit pass/fail, after the artifacts exist — the UAT PRs for a feature or fix, the deployed preview for a website change |

The UAT gate is a path rule, not a judgement:

```bash
git diff --name-only origin/main...HEAD | grep -qE \
  '^(src/Oocx\.TfPlan2Md/(MarkdownGeneration|RenderTargets)/|examples/|website/)'
```

A change to parsing, CLI wiring, tests or documentation never triggers UAT. A change to
rendering, render targets, bundled examples or the website does — for feature, fix and
website work items.

Whether a type has the gate at all is declared in `.agents/workflow.json` as
`uat_gate_after`, naming the stage after which it opens: the UAT Tester for a feature or
fix, the Web Designer for a website change, and nothing for a workflow item, whose
changes are internal tooling. Inferring this from the stage list instead would exempt
website work — the one type whose entire purpose is user-visible change.

### Away from a gate, nothing blocks

A role that hits ambiguity records the question **and the assumption it is proceeding
on** into `state.json` → `open_questions`, then continues. The list surfaces at the next
gate and in the PR description.

This is deliberate. The previous rule — ask one question at a time and wait — made
unattended runs impossible. Blocking is now correct only at a gate.

## State

Each work item carries `state.json`:

```json
{
  "type": "feature",
  "slug": "NNN-example",
  "stage": "code-reviewer",
  "status": "running",
  "gates": { "spec": "approved", "arch": "n/a", "uat": "n/a" },
  "attempts": { "developer": 1 },
  "open_questions": [ { "q": "...", "assumed": "...", "raised_by": "developer" } ]
}
```

`stage` and the keys of `attempts` are **stage ids** — the basename of a file in
`.agents/roles/`, so `code-reviewer`, never `review`. Do not hand-write this file;
the entry role creates it with:

```bash
scripts/wp-append.sh --init <type> <slug>
```

For features and bug fixes, the Retrospective runs only after the Release Manager has
merged the work and verified the published release. The retrospective is a post-release
follow-up; it is not part of the release PR and cannot delay or change that release.
Because the Release Manager deletes the source branch, resume the final stage from a
worktree on a separate documentation follow-up branch based on the merged branch, and
pass the retained work-item directory explicitly:

```bash
scripts/workflow-next.sh --work-item docs/features/NNN-<slug>
```

Use `docs/issues/NNN-<slug>` for a bug fix. The post-release report is kept as a
separate documentation follow-up, so it never ships in the PR whose process it reviews.

`stage` is the role that runs next. `attempts` counts rework loops and drives model
escalation — a role at attempt 2 or later runs one tier deeper. `status` is `running`,
`blocked` or `done`.

State on disk is what makes an unattended run resumable: a session that compacts or dies
loses nothing, because the next stage is a script call rather than a memory.

## Rework loops

| Failure | Returns to |
|---------|-----------|
| Code review verdict `REWORK` | Developer |
| UAT failed | Developer |
| PR Validation or release build failed | Developer |
| Rework reveals a specification gap | Requirements Engineer |

The Developer re-enters with the report, fixes the named findings, and re-runs the
verification that failed. `attempts` increments each time.

## Work protocol

`work-protocol.md` is the audit trail. The first role in the workflow creates it; every
role appends an entry on completion. `scripts/workflow-gate.sh work-protocol` blocks
release when a required role has no entry.

Required roles by workflow type:

| Workflow | Required entries |
|----------|-----------------|
| Feature | Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer, Technical Writer, Code Reviewer |
| Bug fix | Issue Analyst, Developer, Technical Writer, Code Reviewer |
| Workflow | Workflow Engineer |
| Website | Web Designer |

UAT Tester and Retrospective are required when they apply, and are not gate-blocking.
Neither is the Release Manager: it runs the completeness check *before* doing its work,
so it cannot require its own entry.

## Work item numbering

`NNN` is global and monotonic across features, issues and workflow items. Never reuse a
number across types.

On a parallel-work collision, the first PR to merge keeps the number; the later one
renumbers its folder and updates affected intra-doc links before merge.

## Code review runs in Codex

The Code Reviewer role executes through `scripts/codex-review.sh` in a different model
family from the author, so the review tests the code rather than ratifying the reasoning
that produced it. The review must end with `VERDICT: APPROVED` or `VERDICT: REWORK`; the
driver parses that line and treats anything else as `REWORK`.

If `codex` is unavailable the wrapper retries once, then falls back to a Claude reviewer
and records `reviewer: claude-fallback` in `work-protocol.md`.
