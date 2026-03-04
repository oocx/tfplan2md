# Work Protocol: Binary builds failed (NETSDK1207)

**Work Item:** `docs/issues/108-binary-builds-failed/`
**Branch:** `fix/108-binary-builds-failed`
**Workflow Type:** Bug Fix
**Created:** 2026-03-04

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2026-03-04
- **Summary:** Investigated GitHub Actions run 22680751597; identified common failure across all binary targets: NETSDK1207 triggered by `PublishAot=true` flowing into the netstandard2.0 analyzer project `JsonEmbedGenerator` during `dotnet publish`.
- **Artifacts Produced:**
  - `docs/issues/108-binary-builds-failed/work-protocol.md`
  - `docs/issues/108-binary-builds-failed/analysis.md`
- **Problems Encountered:** `scripts/check-workflow-status.sh logs --step` is incompatible with the current `gh` CLI (`--log-failed` is now a boolean), so step-scoped log extraction required full-log filtering.
