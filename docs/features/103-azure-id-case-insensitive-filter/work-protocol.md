# Work Protocol: Case-Insensitive Attribute Change Filter

**Work Item:** `docs/features/103-azure-id-case-insensitive-filter/`
**Branch:** `copilot/add-ignore-azure-id-casing-again`
**Workflow Type:** Feature
**Created:** 2025-07-14

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2025-07-14
- **Summary:** Gathered requirements and created the feature specification for the case-insensitive attribute change filter. Reviewed existing CLI flag patterns (CliParser.cs), the unchanged-values filter feature (010), and the feature specification format from other features (010, 102) to ensure consistency. Feature number 103 was determined as the next available number via the next-issue-number script.
- **Artifacts Produced:** `docs/features/103-azure-id-case-insensitive-filter/specification.md`, `docs/features/103-azure-id-case-insensitive-filter/work-protocol.md`
- **Problems Encountered:** None

### Architect
- **Date:** 2025-07-14
- **Summary:** Analyzed the feature specification against the existing codebase. Determined that no new architectural patterns are required — the feature follows the same pipeline as the existing `--show-unchanged-values` flag (feature 014) exactly. Documented the filter placement decision (model-building time in `BuildAttributeChanges()`), the two-guard pattern for `isCasingOnlyChange` vs `valuesEqual`, the non-string value analysis (numbers/booleans are handled automatically), and the full component change list across 8 files.
- **Artifacts Produced:** `docs/features/103-azure-id-case-insensitive-filter/architecture.md`
- **Problems Encountered:** None

### Quality Engineer
- **Date:** 2025-07-14
- **Summary:** Created a comprehensive test plan mapping all 7 acceptance criteria to 14 test cases across three test files. Followed the `ReportModelBuilderUnchangedValuesTests.cs` and `CliParserTests.cs` patterns exactly. Specified a new test data file `azurerm-case-only-ids-plan.json` covering casing-only changes, mixed changes, null before/after, numeric changes, and unchanged (ordinal-equal) attributes. Created a UAT test plan for the user-facing rendering change, requiring a feature-specific `uat-plan.json` / `uat-plan.md` pair for the Developer to produce.
- **Artifacts Produced:** `docs/features/103-azure-id-case-insensitive-filter/test-plan.md`, `docs/features/103-azure-id-case-insensitive-filter/uat-test-plan.md`
- **Problems Encountered:** None

### Task Planner
- **Date:** 2025-07-14
- **Summary:** Read specification, architecture, and test plan documents. Inspected existing source files for context (`CliParser.cs`, `HelpTextProvider.cs`, `ReportModelBuilder.cs`, `ReportModelBuilder.ResourceChanges.cs`, `ReportModel.cs`, `AotScriptObjectMapper.cs`, `CompositionRoot.cs`) to verify exact change locations. Produced 11 developer tasks ordered by dependency (test data → CLI layer → model → filter logic → propagation → wiring → tests → docs), each with precise file paths, acceptance criteria, and code snippets from the architecture document.
- **Artifacts Produced:** `docs/features/103-azure-id-case-insensitive-filter/tasks.md`
- **Problems Encountered:** None
