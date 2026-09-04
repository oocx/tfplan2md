# Tasks: Harness-Neutral Agent Workflow

Phases 1–5 are additive and reversible. Phase 6 is the point of no return.

| ID | Phase | Status | Notes |
|----|-------|--------|-------|
| 1 | Scaffolding | ✅ Done | `AGENTS.md`, `.agents/` layout, tier registry, sync generator, doctor + setup scripts, reference role |
| 2 | Role merge | ✅ Done | 26 → 13 roles (9,820 → 887 lines); `agent-runtime` skill absorbs the plumbing; `validate-agents.py` rewritten; `docs/workflow.md` written |
| 3 | Driver | ✅ Done | `.agents/workflow.json`, `workflow-lib.sh`, `workflow-next.sh`, `workflow-gate.sh`, `wp-append.sh`, `run-workflow` skill, 4 slash commands |
| 4 | Codex reviewer | ✅ Done | `codex-review.sh` + JSON output schema; verified against the real CLI on a live diff (4m34s, 199,652 tokens, 6 Blockers found) |
| 5 | Review rework | ✅ Done (2 rounds) | Fix the 6 Blockers and 1 Major from the Codex review: double stage advance, rejected-gate bypass, unopened UAT gate, bug-workflow Developer inputs, review requirement on workflow/website releases, driver test coverage |
| 6 | Skills | ✅ Done | 23 skills moved to `.agents/skills/`, 6 deleted, 4 new (`agent-runtime`, `run-workflow`, `authoring-roles`, `context-pack`, `retrospective-evidence`); `SKLL.md` renamed; Copilot-isms and retired git wrappers purged |
| 7 | Demolition | ✅ Done | Copilot corpus removed; `docs/agents.md` replaced by `docs/workflow.md` with a rewritten diagram; CI paths updated and agent validation wired in |
| 8 | Dry run | ⬜ Not started | Drive one real work item end to end in auto mode; record where it stopped |

## Open questions

| # | Question | Assumption in force |
|---|----------|--------------------|
| 1 | ~~Do Sol / Terra / Luna descend in capability?~~ | **Answered:** yes. Real slugs are `gpt-5.6-sol` / `-terra` / `-luna`; bare names are rejected by the API. |
| 2 | Should Issue Analyst move to deep tier? | Left at standard. It is the first stage of the bug workflow and its errors propagate the way a bad requirement does, so the reasoning behind task 7 applies to it unchanged. |
