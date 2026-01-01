## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Reduce manual handoff friction (prompt generator / prompt templates) | Feature 027 retrospective | ⬜ Not started | Per-agent chat workflow adds manual prompt assembly overhead. | High | Med | Low | Prefer simplest approach first (e.g., prompt templates) before adding scripts/skills. |
| 2 | Improve developer progress visibility | Feature 027 retrospective | ⬜ Not started | Maintainer feedback: progress and next steps were unclear during long dev sessions. | High | Low | Low | Likely update to `.github/agents/developer.agent.md` guidance and `todo` cadence. |
| 3 | Formalize per-agent chat workflow docs | Feature 027 retrospective | ⬜ Not started | The “how” of per-agent chats (naming, storage, handoffs) isn’t explicit. | Med | Low | Low | Likely update `docs/agents.md` with a short, explicit section. |
| 4 | Add markdown syntax validator for tables/headings | Workflow 025/026 carry-over | ⬜ Not started | Still open item from prior workflow improvements. | Med | Low | Med | Would be `scripts/validate-markdown.sh` and used by UAT or pre-UAT checks. |
| 5 | Add merge command to `scripts/pr-github.sh` (avoid gh fallback) | 2025-12-28 workflow retrospective | ⬜ Not started | Prevents merge friction and out-of-date branch errors when using `gh` directly. | Med | Med | Med | Keep scope tight: merge existing PRs; optionally auto `gh pr update-branch`. |
| 6 | Always present 3 options in chat before selection | Workflow Engineer meta-agent gap | 🟡 In progress | I asked for an ID without repeating the 3 recommended options in chat, forcing the Maintainer to open `tasks.md`. | Med | Low | Low | Update WE agent + `/we` prompt to require presenting Option 1/2/3 in the selection message. |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **6** — Low effort, prevents recurring instruction-following misses.
- **Option 2 (Quick win):** **3** — Low effort; makes per-agent workflow repeatable for new contributors.
- **Option 3 (Highest impact):** **1** — Eliminates the biggest friction from per-agent chats, but needs careful scoping.

## Decision
Selected: **6** (2026-01-01)
