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

### Developer
- **Date:** 2026-03-04
- **Summary:** Implemented AOT isolation for `JsonEmbedGenerator` so `PublishAot` no longer breaks analyzer restore/build during binary publish. Added focused regression tests and validated both local reproduction and full solution tests.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj`
  - `src/tools/JsonEmbedGenerator/JsonEmbedGenerator.csproj`
  - `src/tests/Oocx.TfPlan2Md.TUnit/Workflows/AotPublishIsolationTests.cs`
  - `docs/issues/108-binary-builds-failed/work-protocol.md`
- **Problems Encountered:** Initial `ProjectReference` metadata fix alone did not resolve restore-time propagation of `PublishAot`; added generator-side `TreatAsLocalProperty="PublishAot"` and explicit `<PublishAot>false</PublishAot>` to fully prevent NETSDK1207.

### Release Manager
- **Date:** 2026-03-04
- **Summary:** Pre-release checks passed (clean working directory, branch up to date). Skipped Technical Writer and Code Reviewer per Maintainer instruction (minor CI-only fix). Generated user-facing release notes and committed. Created PR; will merge after PR Validation passes.
- **Artifacts Produced:**
  - `docs/issues/108-binary-builds-failed/release-notes.md`
- **Problems Encountered:** None.
