## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Web Designer: enforce VS Code preview + DevTools | prior-work | ✅ Done | Prevent redundant local servers and standardize rendering validation. | High | Low | Low | Use VS Code built-in server at `http://127.0.0.1:3000/website/` and require Chrome DevTools MCP for validation. |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **1** — Small instruction update prevents recurring workflow friction.
- **Option 2 (Quick win):** **1** — Single-agent change, immediate value.
- **Option 3 (Highest impact):** **1** — Standardizes website validation.

## Decision
Selected: **Task 1** (Maintainer request in VS Code chat, 2026-01-10).
