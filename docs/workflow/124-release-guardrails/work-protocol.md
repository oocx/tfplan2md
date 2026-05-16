# Work Protocol: Release Guardrails

**Work Item:** `docs/workflow/124-release-guardrails/`
**Branch:** `copilot/fix-workflow-issues`
**Workflow Type:** Workflow
**Created:** 2026-05-16

## Agent Work Log

### Workflow Engineer
- **Date:** 2026-05-16
- **Summary:** Added workflow guardrails to block missing release artifacts and to make release-note screenshot targeting explicit and reviewable.
- **Artifacts Produced:** `docs/workflow/124-release-guardrails/tasks.md`, `docs/workflow/124-release-guardrails/work-protocol.md`, `docs/workflow/124-release-guardrails/release-notes.md`, `scripts/validate-release-notes.sh`, `scripts/generate-release-screenshots.sh`, `src/tests/shell/validate_release_notes_test.sh`, `docs/agents.md`, `.github/skills/generate-release-screenshots/SKILL.md`, `docs/release-notes-template.md`
- **Problems Encountered:** Existing release-note validation only covered changed feature/issue folders, so it could not catch missing workflow artifacts or missing Release Manager log entries.

### Workflow Engineer (follow-up)
- **Date:** 2026-05-16
- **Summary:** Added deterministic pre-Release-Manager verification to the orchestrator agent. The orchestrator now reads `work-protocol.md` and checks that all required agents (by workflow type) have logged entries before delegating to Release Manager. Missing entries cause the orchestrator to re-delegate to the skipped agent, catching the problem during orchestration instead of in CI.
- **Artifacts Produced:** `.github/agents/workflow-orchestrator-coding-agent.agent.md` (updated Check Agent Output step, Handle Feedback Loops, Definition of Done, Complete Workflow step), `docs/workflow/124-release-guardrails/tasks.md` (Task 3 added and marked Done)
- **Problems Encountered:** A previous attempt added the validation to `validate-release-notes.sh` (CI) instead of to the orchestrator (early, during the agent session). That approach was reverted because adding more CI checks makes the problem worse, not better. The correct fix is early detection in the orchestrator.
