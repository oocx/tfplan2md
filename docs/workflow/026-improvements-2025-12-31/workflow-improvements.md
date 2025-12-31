# Workflow Improvements: 2025-12-31

This improvement set is based on:
- Feature 026 retrospective: [Template Rendering Simplification](../../features/026-template-rendering-simplification/retrospective.md)
- Carry-over items still marked **Open** from: [Workflow Improvements: 2025-12-29](../025-improvements-2025-12-29/workflow-improvements.md)

## Improvement Tasks (Consolidated)

| # | Task | Source | Value | Effort | Priority | Status | Definition of Done (DoD) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | **Snapshot integrity guardrail**: add a snapshot-change policy and automation | Feature 026 | High | Medium | **Critical** | Open | Snapshot updates are rejected unless explicitly justified (e.g., commit message token) and Code Reviewer checklist requires justification for snapshot diffs. |
| 2 | **Code Reviewer DoD for snapshots**: require justification + regression reasoning | Feature 026 | High | Low | **Critical** | Open | `.github/agents/code-reviewer.agent.md` explicitly calls out snapshot changes as a high-risk area and requires a review note explaining why they changed and why that’s correct. |
| 3 | **Developer correctness guardrail**: always run tests before “next steps” | Feature 026 | Medium | Low | High | Open | `.github/agents/developer.agent.md` requires running tests (or scoped tests) before declaring work complete, and forbids updating snapshots to “make tests pass” without diagnosing the mismatch. |
| 4 | **Test hang detection**: timeout wrapper for `dotnet test` | Feature 026 | High | Medium | **Critical** | Open | `scripts/test-with-timeout.sh` exists, returns non-zero on timeout, and is used by recommended workflow commands (docs/agents and/or CI guidance). |
| 5 | **Fix UAT polling hang**: detect closed/abandoned PRs in UAT scripts | Feature 026 | High | Low | **Critical** | Done | `scripts/uat-helpers.sh` (or `scripts/uat-run.sh`) exits cleanly with an error when PR is `CLOSED`/`ABANDONED` instead of polling indefinitely. |
| 6 | **Temp file policy**: standardize `.tmp/` workspace folder usage | Feature 026 | Medium | Low | Medium | Open | Workflow documentation forbids creating files outside the workspace; agents use `.tmp/` for scratch output; no scripts write to `/tmp` or `~/` by default. |
| 7 | **Option-selection robustness**: avoid numeric “option confusion” in multi-list chats | Feature 026 | Medium | Low | Medium | Open | Agent prompts require explicit “Option 1/2/3” (or the option label text) rather than numeric-only answers; prompts avoid presenting multiple option lists at once. |
| 8 | **Metrics plausibility checks**: flag anomalous session metrics to avoid hallucinations | Feature 026 | Medium | Medium | Medium | Open | `scripts/analyze-chat.py` flags suspicious mismatches (e.g., very low request count for long sessions) and emits a warning in its report output. |
| 9 | **Safe merge script**: verify file integrity post-merge | Workflow 025 (Open) | High | Medium | **Critical** | Open | `scripts/safe-merge.sh` exists and verifies a set of critical files (presence + size/hash) after merge; documented usage in release workflow instructions. |
| 10 | **Markdown syntax validator**: detect broken tables/headings before UAT | Workflow 025 (Open) | Medium | Low | Medium | Open | `scripts/validate-markdown.sh` exists and fails on common markdown structural errors (e.g., table column mismatch, malformed headings) for generated artifacts. |
| 11 | **UAT signal detection**: explicitly fail on reject/fail signals in UAT output | Workflow 025 (Open) | Medium | Medium | Medium | Open | `scripts/uat-run.sh` (or a wrapper) treats explicit “reject/fail” signals as a failing outcome with a clear exit code and summary. |
| 12 | **Model outage protocol**: define fallback behavior for missing primary models | Workflow 025 (Open) | Low | Low | Low | Open | Workflow docs include a short “primary model unavailable” fallback rule per critical agent (Developer, Code Reviewer), including how to proceed and what to note in the report. |

## Notes

- Feature 026 already recorded one workflow improvement as completed: handoff-prompt detection improvements in `scripts/analyze-chat.py`. This list focuses on items that remain actionable or require follow-up hardening.
