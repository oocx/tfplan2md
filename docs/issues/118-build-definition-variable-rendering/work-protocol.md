# Work Protocol: Build Definition Variable Rendering

**Work Item:** `docs/issues/118-build-definition-variable-rendering/`
**Branch:** `copilot/add-azuredevops-variable-rendering`
**Workflow Type:** Bug Fix
**Created:** 2026-03-18

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Technical Writer
- **Date:** 2026-03-18
- **Summary:** Created release notes for issue 118 and updated `docs/features.md` to correct the variable column names (`Is Secret` → `Secret`) and update the example output tables to match the actual renderer output (including `🆔` name icons, `✅`/`❌` boolean formatting, and correct CI trigger/repository column headings).
- **Artifacts Produced:**
  - `docs/issues/118-build-definition-variable-rendering/release-notes.md` — New release notes describing the bug fix (sensitive attribute bleed) and the feature (connected tabular renderer)
  - `docs/features.md` — Updated Azure DevOps Build Definitions section: corrected column names, added create-operation example, updated update-operation example to match snapshot
- **Problems Encountered:** The existing documentation (from Feature 094) used column header "Is Secret" while the actual implementation uses "Secret". This was corrected to match the snapshot output.

### Developer
- **Date:** 2026-03-18
- **Summary:** Implemented `BuildDefinitionRenderer` class in `AzureDevOpsResourceRenderers.cs` modelled after `VariableGroupRenderer`. The renderer uses `BuildDefinitionViewModelFactory` to read variable data directly from before/after JSON, completely bypassing the `SensitivityHelper` hierarchical check that caused the bug. Only the `value`/`secret_value` field is masked for secret variables; `name`, `is_secret`, and `allow_override` correctly display actual values.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Renderers/AzureDevOpsResourceRenderers.cs` — Added `BuildDefinitionRenderer` with tabular rendering for variables, CI triggers, PR triggers, schedules, repositories, and jobs
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs` — Replaced `AzureDevOpsDelegatingRenderer("azuredevops_build_definition")` with `BuildDefinitionRenderer(_azdoRepositoryMapper)`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzureDevOpsSnapshotTests.cs` — Added `Snapshot_AzureDevOps_BuildDefinitions_MatchesBaseline` test
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ProviderResourceRenderersTests.cs` — Updated to use `BuildDefinitionRenderer()` instead of `AzureDevOpsDelegatingRenderer("azuredevops_build_definition")`
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azuredevops-build-definitions.md` — New snapshot baseline confirming correct rendering
- **Test Results:** 1236 tests passed, 0 failed, 0 skipped
- **Problems Encountered:** Cyclomatic complexity (CA1502) required extracting `RenderHeader`, `RenderVariableSection`, and `RenderSupplementarySections` helper methods from the main `Render` method.
- **Date:** 2026-03-18
- **Summary:** Investigated the bug where `azuredevops_build_definition` variables with `is_secret = true` show all variable attributes as `(sensitive)` instead of only the value attribute. Identified the root cause (missing dedicated renderer) and the scope of the fix. Also confirmed that all the infrastructure for a proper tabular renderer already exists (BuildDefinitionViewModelFactory, BuildDefinitionFormatters, etc.) but is not yet connected.
- **Artifacts Produced:** `docs/issues/118-build-definition-variable-rendering/analysis.md`, `docs/issues/118-build-definition-variable-rendering/work-protocol.md`
- **Problems Encountered:** None
