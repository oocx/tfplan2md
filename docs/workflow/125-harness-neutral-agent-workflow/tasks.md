# Tasks: Harness-Neutral Agent Workflow

Phases 1–5 are additive and reversible. Phase 6 is the point of no return.

| ID | Phase | Status | Notes |
|----|-------|--------|-------|
| 1 | Scaffolding | ✅ Done | `AGENTS.md`, `.agents/` layout, tier registry, sync generator, doctor + setup scripts, reference role |
| 2 | Role merge | ⬜ Not started | 26 → 13 roles; `agent-runtime` skill absorbs all plumbing |
| 3 | Driver | ⬜ Not started | `state.json` schema, `workflow-next`, `workflow-gate`, `wp-append`, `run-workflow` skill |
| 4 | Codex reviewer | ⬜ Not started | Install `@openai/codex`, verify `codex exec` flags against the real CLI, wire `codex-review.sh` and the verdict contract |
| 5 | Skills | ⬜ Not started | 4 new, 4 deleted, 3 rewritten; rename `SKLL.md`; fix the `workflow:` commit-msg regex |
| 6 | Demolition | ⬜ Not started | 49 deletions; split `docs/agents.md`; update validation and CI paths |
| 7 | Dry run | ⬜ Not started | Drive one real work item end to end in auto mode; record where it stopped |

## Open questions

| # | Question | Assumption in force |
|---|----------|--------------------|
| 1 | Do Sol / Terra / Luna descend in capability? | Yes — Sol is the deep tier and therefore the reviewer model. Load-bearing; asked, not yet answered. |
| 2 | Should Issue Analyst move to deep tier? | Left at standard. It is the first stage of the bug workflow and its errors propagate the way a bad requirement does, so the reasoning behind task 7 applies to it unchanged. |
