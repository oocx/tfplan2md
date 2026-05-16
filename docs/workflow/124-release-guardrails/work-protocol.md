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

### Workflow Engineer (follow-up — deterministic hook)
- **Date:** 2026-05-16
- **Summary:** Added a `preToolUse` hook (`validate-work-protocol`) that deterministically blocks `report_progress` when `work-protocol.md` is missing required agent log entries for the workflow type. The hook fires only after the "pre-Release-Manager" agent has run (Code Reviewer for features/bugs; Workflow Engineer for workflow items), so early-stage intermediate pushes are unaffected. This catches missing agents during the orchestrator session — before any commit is pushed to the PR — avoiding expensive CI failures.
- **Artifacts Produced:** `scripts/hooks/validate-work-protocol.sh` (hook script), `.github/hooks/validate-work-protocol.json` (hook config), `src/tests/shell/validate_work_protocol_test.sh` (10 test cases), `docs/agents.md` (new Agent Hooks section + Verification update), `docs/workflow/124-release-guardrails/tasks.md` (Task 4 added and marked Done)
- **Problems Encountered:** Previous attempts added checks to CI (`validate-release-notes.sh`) or to orchestrator instructions — both non-deterministic approaches. The correct fix is a `preToolUse` hook, which is enforced by the hook runner regardless of agent instructions.
