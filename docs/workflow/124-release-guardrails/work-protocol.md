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
