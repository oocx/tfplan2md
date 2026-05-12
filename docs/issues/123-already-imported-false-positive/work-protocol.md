# Work Protocol: False Positive "Already Imported" Warning for Pending Import Blocks

**Work Item:** `docs/issues/123-already-imported-false-positive/`
**Branch:** `copilot/fix-tfplan2md-import-blocks`
**Workflow Type:** Bug Fix
**Created:** 2026-05-12

## Agent Work Log

### Issue Analyst
- **Date:** 2026-05-12
- **Summary:** Investigated the current import-warning bug on the existing `copilot/*` branch, confirmed the old `read`-action root cause no longer matches the code, and identified the likely remaining problem as an over-broad `no-op => already imported` heuristic in the staged report pipeline.
- **Artifacts Produced:** `docs/issues/123-already-imported-false-positive/analysis.md`, `docs/issues/123-already-imported-false-positive/work-protocol.md`
- **Problems Encountered:** `scripts/next-issue-number.sh` returned `123` but emitted `integer expression expected`; repository history also contains an older closed issue (`docs/issues/063-already-imported-false-positive/`) for a previous version of this bug, so a fresh issue artifact was created to avoid reusing stale analysis.

### Developer
- **Date:** 2026-05-12
- **Summary:** Added a regression-first fix that stops treating `importing.id + no-op` as proof that an import was already applied, while keeping `no-op` moved resources marked as already moved. Split the staged model into import-specific and move-specific already-applied flags so summary rendering and the refactoring table can classify each operation independently.
- **Artifacts Produced:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`, `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.cs`, `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ReportAssemblyStage.cs`, `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderRefactoringOperationTests.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ResourceSummaryHtmlBuilderRefactoringTests.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Stages/ReportAssemblyStageTests.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderStageDelegationTests.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/refactoring-comprehensive.md`
- **Problems Encountered:** The first full test run failed only on the existing `refactoring-comprehensive.md` snapshot because the rendered output intentionally changed from `⚠️ Already imported` to `✅ Ready`; regenerated and reviewed the snapshot baseline with the required snapshot update workflow.
