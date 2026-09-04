# Work Protocol: Rename-aware release-note validation

**Work Item:** `docs/workflow/144-release-notes-rename-validation/`
**Branch:** `codex-docs-work-item-renumbering`
**Workflow Type:** Workflow
**Created:** 2026-09-04

## Agent Work Log

### Workflow Engineer

- **Date:** 2026-09-04
- **Summary:** Made the release-note guardrail distinguish work-item directory renames from new work items, then added a regression test.
- **Artifacts Produced:** `scripts/validate-release-notes.sh`, `src/tests/shell/validate_release_notes_test.sh`, and this workflow item.
- **Problems Encountered:** The original path-only check treated a documentation-wide renumbering as new releases.
