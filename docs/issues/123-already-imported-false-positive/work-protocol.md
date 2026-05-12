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

### Technical Writer
- **Date:** 2026-05-12
- **Summary:** Updated the directly related documentation to match the implemented fix. The issue analysis now records the shipped behavior change, and the Terraform import/moved-block docs now distinguish pending imports from already-applied moves so they no longer claim that `no-op` imports should render `⚠️ Already imported`.
- **Artifacts Produced:** `docs/issues/123-already-imported-false-positive/analysis.md`, `docs/issues/123-already-imported-false-positive/work-protocol.md`, `docs/features/038-terraform-import-moved-blocks/specification.md`, `docs/features/038-terraform-import-moved-blocks/architecture.md`, `docs/features/038-terraform-import-moved-blocks/tasks.md`, `docs/features/038-terraform-import-moved-blocks/test-plan.md`, `docs/features/038-terraform-import-moved-blocks/uat-test-plan.md`, `docs/features/038-terraform-import-moved-blocks/release-notes.md`
- **Problems Encountered:** None. I reviewed `README.md`, `docs/features.md`, `docs/architecture.md`, `docs/testing-strategy.md`, and `docs/agents.md`; they do not describe this import-status edge case directly, so no global-document edits were needed.
- **Next Agent:** Code Reviewer
