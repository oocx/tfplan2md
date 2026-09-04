## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Add Sub-Agent Strategy section to agents.md | Issue exploration | ✅ Done | Agents lack guidance on when/how to use subagents (`explore`, `task`, `general-purpose`) to reduce context rot and improve quality | High | Low | Low | New section in docs/agents.md with decision matrix |
| 2 | Add billing/cost guidance for subagents to ai-model-reference.md | Issue exploration | ✅ Done | No documentation exists explaining how subagent calls affect premium request billing, making cost-conscious decisions impossible | Med | Low | Low | Covers coding agent (per-session) and VS Code chat (per-message) billing models |
| 3 | Add subagent best practices to copilot-instructions.md | Issue exploration | ✅ Done | Global agent instructions lack any mention of subagent delegation patterns; agents don't know when to offload work to subagents | High | Low | Low | Concise guidance added to Coding Workflow Preferences |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **1 + 2 + 3** — All three are low-effort documentation changes that together provide comprehensive subagent guidance across the three main reference documents.
- **Option 2 (Quick win):** **3** — Adding a few lines to copilot-instructions.md immediately helps all agents.
- **Option 3 (Highest impact):** **1** — The strategy section in agents.md provides the most detailed guidance.

## Decision

Implementing all three items as they are complementary low-effort documentation changes.
