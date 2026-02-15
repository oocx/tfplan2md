## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Document PR-coding-agent branch rules | user-report | ✅ Done | Cloud coding agents must not switch branches; doing so can lead to “no files in PR” even if work was done. | High | Low | Low | Updated docs/agents.md and the execution-context-detection skill with explicit PR-coding-agent branch constraints and PR-comment clarification guidance. |
| 2 | Update Workflow Engineer for cloud PR branch safety | user-report | ✅ Done | Workflow Engineer currently instructs switching/creating branches; in PR-coding-agent context this is harmful. | High | Low | Med | Add explicit guardrails: never `git switch` / create new branch in GitHub coding agent runs; work on the provided branch. |
| 3 | Fix Requirements Engineer “made-up answers” via PR comments | user-report | ✅ Done | In GitHub coding agent runs, requirements clarifications must be asked via PR comments (or issue comments), not guessed and written into specs. | High | Med | Med | Add “no assumptions” rule: ask via PR comments; keep unknowns as Open Questions until answered. |
| 4 | Align all agents on cloud branch constraints | prior-work | ✅ Done | Multiple agents contain branch-creation steps that are correct locally but wrong in cloud PR runs. | High | Med | High | Added consistent PR-coding-agent branch safety guidance across all agents; adjusted “Do NOT push” guidance to be VS Code-local only where it conflicted with PR-coding-agent updates. |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **2 + 3** — Fix the two concrete failures you observed (branch switching + hallucinated clarifications) with minimal blast radius.
- **Option 2 (Quick win):** **1** — Improves correctness immediately with near-zero risk.
- **Option 3 (Highest impact):** **4** — Consistent behavior across all agents in GitHub PR-coding-agent runs.

## Decision
Which item should I implement first? (Reply with the Option number, or reply with "work on task <task id>")
