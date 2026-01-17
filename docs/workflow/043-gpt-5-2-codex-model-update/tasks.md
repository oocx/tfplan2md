## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Update GPT-5.2-Codex references + benchmarks | Maintainer request (2026-01-17) | ⬜ Not started | New model release requires fresh benchmarks + guidance to keep agent recommendations accurate. | High | Med | Low | Update `docs/ai-model-reference.md` and assess agent recommendations. |
| 2 | Formalize per-agent chat workflow docs | Workflow 028 carry-over | ⬜ Not started | The per-agent chat setup/handoff steps are not explicit, causing inconsistent usage. | Med | Low | Low | Likely update `docs/agents.md` with a short, explicit section. |
| 3 | Add markdown syntax validator for tables/headings | Workflow 028 carry-over | ⬜ Not started | Structured markdown validation is still missing and causes late CI failures. | Med | Med | Med | Would be `scripts/validate-markdown.sh` and used by UAT/pre-UAT checks. |
| 4 | Add merge command to `scripts/pr-github.sh` | 2025-12-28 workflow retrospective | ⬜ Not started | Prevents merge friction and out-of-date branch errors when `gh` fallback is required. | High | Med | Med | Keep scope tight: merge existing PRs; optionally auto `gh pr update-branch`. |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **1** — Required to keep model guidance accurate after a new release.
- **Option 2 (Quick win):** **2** — Low effort documentation improvement with immediate clarity benefits.
- **Option 3 (Highest impact):** **4** — Reduces merge friction and avoids out-of-date branch failures.

## Decision
Which item should I implement first? (Reply with the Option number, or reply with "work on task <task id>")
