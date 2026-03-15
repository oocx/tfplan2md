# Work Protocol: ArgumentNullException When Parsing Plan Without resource_changes

**Work Item:** `docs/issues/113-argument-null-source/`
**Branch:** `copilot/fix-argument-null-error`
**Workflow Type:** Bug Fix
**Created:** 2025-07-14

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2025-07-14
- **Summary:** Investigated the `ArgumentNull_Generic Arg_ParamName_Name, source` error reported in v1.36.0. Identified root cause as `plan.ResourceChanges` being null when the `resource_changes` field is absent from (or explicitly null in) the Terraform plan JSON. Also identified a secondary null risk on `Change.Actions` and `OutputChange.Actions`. Produced issue analysis document.
- **Artifacts Produced:** `docs/issues/113-argument-null-source/analysis.md`, `docs/issues/113-argument-null-source/work-protocol.md`
- **Problems Encountered:** None

### Developer
- **Date:** 2025-07-14
- **Summary:** Implemented defensive null guards at all three crash sites. Wrote failing tests first to confirm the regression, then applied fixes and verified all 1197 tests pass with zero skips.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.cs` — `?? []` guard before `.Select()`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.Helpers.cs` — `DetermineAction` accepts nullable, guards `null || Count == 0`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs` — null-safe pattern for `outputChange.Actions`
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/no-resource-changes-plan.json` (new)
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/null-resource-changes-plan.json` (new)
  - `src/tests/Oocx.TfPlan2Md.TUnit/Parsing/TerraformPlanParserTests.cs` — 2 new tests
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Stages/ResourceChangeStageTests.cs` — 1 new test
  - `src/tests/Oocx.TfPlan2Md.TUnit/EndToEnd/NullResourceChangesRegressionTests.cs` (new) — 3 end-to-end regression tests
- **Commit:** `2de4452` — `fix: handle null resource_changes/actions in tfplan.json gracefully`
- **Test Results:** All 1197 tests passed, 0 skipped, 0 failed
- **Security:** CodeQL analysis: 0 alerts. No new dependencies added.
- **Problems Encountered:** None. Transient build error (missing apphost file) resolved by using `--no-build` for subsequent test runs.
- **Next Agent:** Code Reviewer

### Technical Writer
- **Date:** 2025-07-14
- **Summary:** Reviewed documentation for the null `resource_changes` fix. README.md and docs/features.md already described lenient parsing, but neither specifically noted that output-only plans (where `resource_changes` is absent or null) are supported. Updated `docs/features.md` Terraform Compatibility section with a targeted bullet clarifying this behavior.
- **Artifacts Produced:** `docs/features.md` — added bullet to Terraform Compatibility section
- **Problems Encountered:** None
- **Next Agent:** Code Reviewer
