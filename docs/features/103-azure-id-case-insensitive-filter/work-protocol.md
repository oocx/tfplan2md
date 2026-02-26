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

### Architect (Revision)
- **Date:** 2025-07-14
- **Summary:** Revised architecture based on maintainer requirement that the filter must (1) only apply to Azure resource ID attributes and (2) be implemented in Azure platform-specific code. The previous design (blanket `isCasingOnlyChange` in core `BuildAttributeChanges()`) was replaced with a new `IAttributeChangeFilter` / `AttributeChangeFilterRegistry` extension point (mirroring the existing `IValueFormatter` / `ValueFormatterRegistry` pattern). The Azure-specific logic now lives entirely in `Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs`, using the existing `AzureScopeParser.IsAzureResourceId()` from `Platforms/Azure/`. The core pipeline only gains a single delegate call to the filter registry — no Azure knowledge in `MarkdownGeneration/`. The specification was also updated to reflect the revised scope (only Azure resource IDs filtered, only azurerm provider, not a blanket string filter). The `IProviderModule` interface gains a new `RegisterAttributeChangeFilters()` method with a default no-op. Updated success criteria now require tests for non-Azure-ID strings and non-azurerm provider resources.
- **Artifacts Produced:** Updated `docs/features/103-azure-id-case-insensitive-filter/architecture.md`, updated `docs/features/103-azure-id-case-insensitive-filter/specification.md`
- **Problems Encountered:** None — the `IValueFormatter` / `ValueFormatterRegistry` pattern was a direct model for the new extension point.

### Task Planner
- **Date:** 2025-07-14
- **Summary:** Read specification, architecture, and test plan documents. Inspected existing source files for context (`CliParser.cs`, `HelpTextProvider.cs`, `ReportModelBuilder.cs`, `ReportModelBuilder.ResourceChanges.cs`, `ReportModel.cs`, `AotScriptObjectMapper.cs`, `CompositionRoot.cs`) to verify exact change locations. Produced 11 developer tasks ordered by dependency (test data → CLI layer → model → filter logic → propagation → wiring → tests → docs), each with precise file paths, acceptance criteria, and code snippets from the architecture document.
- **Artifacts Produced:** `docs/features/103-azure-id-case-insensitive-filter/tasks.md`
- **Problems Encountered:** None. **Note:** Tasks.md was produced against the original architecture and will need to be updated by the Task Planner to reflect the revised design (new `IAttributeChangeFilter` extension point + Azure-specific `AzureResourceIdCaseChangeFilter` class).

### Quality Engineer (Revision)
- **Date:** 2025-07-14
- **Summary:** Revised the test plan to align with the updated architecture (revised 2025-07-14). Key changes: (1) Updated the overview and all relevant test cases to emphasise that only **Azure resource ID** values (as detected by `AzureScopeParser.IsAzureResourceId()`) are filtered — non-Azure-ID strings are never suppressed. (2) Added TC-15: non-Azure-ID string with casing-only change is NOT suppressed. (3) Added TC-16: non-azurerm provider resources are NOT filtered. (4) Added a new test class `AzureResourceIdCaseChangeFilterTests.cs` (TC-17–TC-21) with isolated unit tests for `AzureResourceIdCaseChangeFilter.ShouldSuppress()` covering all guard conditions. (5) Added a new test class `AttributeChangeFilterRegistryTests.cs` (TC-22–TC-24) for the registry infrastructure. (6) Updated the test data requirements table to include a non-Azure-ID string casing row and a non-azurerm provider resource. (7) Updated the coverage matrix and edge-cases table to reflect the 24 total test cases. Removed the old "blanket string comparison" framing from descriptions throughout.
- **Artifacts Produced:** Updated `docs/features/103-azure-id-case-insensitive-filter/test-plan.md`
- **Problems Encountered:** None.

### Task Planner (Revision)
- **Date:** 2025-07-14
- **Summary:** Replaced `tasks.md` to align with the revised architecture (revised 2025-07-14). The old tasks.md was written for the original blanket-string approach; the new tasks reflect the `IAttributeChangeFilter`/`AttributeChangeFilterRegistry` extension point design. Key changes: (1) Replaced old Task 5 (single `_ignoreCaseChanges` constructor param) with two tasks — Task 5 creates the three core infrastructure files (`AttributeChangeFilterContext`, `IAttributeChangeFilter`, `AttributeChangeFilterRegistry` — no Azure logic) and Task 6 adds `RegisterAttributeChangeFilters()` to `IProviderModule` and `RegisterAllAttributeChangeFilters()` to `ProviderRegistry`. (2) Added Task 7: create `AzureResourceIdCaseChangeFilter` in `Providers/AzureRM/` with the five-guard `ShouldSuppress()` logic using `AzureScopeParser.IsAzureResourceId()`. (3) Added Task 8: override `RegisterAttributeChangeFilters()` in `AzureRMModule`. (4) Updated Task 9 (`ReportModelBuilder` constructor) to accept both `ignoreCaseChanges` and `attributeChangeFilterRegistry` parameters with null-coalescing fallback. (5) Replaced old Task 6 (blanket `isCasingOnlyChange` guard) with new Task 10: single delegate call to `_attributeChangeFilterRegistry.ShouldSuppress()` with no Azure-specific logic in the core. (6) Updated Task 13 (`CompositionRoot`) to add a `CreateAttributeChangeFilterRegistry()` helper method. (7) Updated Task 14 (tests) to cover all 24 test cases (TC-01–TC-24), including the three new test classes `AzureResourceIdCaseChangeFilterTests.cs` (TC-17–TC-21) and `AttributeChangeFilterRegistryTests.cs` (TC-22–TC-24). (8) Updated test data requirements in Task 1 to include two new resources for TC-15 and TC-16. Total task count: 15 (was 11). Implementation order updated to reflect new dependency graph.
- **Artifacts Produced:** Replaced `docs/features/103-azure-id-case-insensitive-filter/tasks.md`
- **Problems Encountered:** None.

### Developer
- **Date:** 2025-07-14
- **Summary:** Implemented all 15 tasks for feature 103. Key implementation notes: `role_definition_id` values in test data use `/subscriptions/.../providers/Microsoft.Authorization/...` format (subscription-scoped paths) rather than bare `/providers/...` paths, because `AzureScopeParser.IsAzureResourceId()` only recognises bare `/providers/` paths for management group scope (`/providers/Microsoft.Management/managementGroups/...`); subscription-scoped role definition IDs use the `IsSubscriptionProviderScope` path which IS recognized. The `AttributeChangeFilterRegistry.ShouldSuppress()` uses `List<T>.Exists()` (not LINQ `.Any()`) to satisfy Sonar rule S6605. All 1286 tests pass with zero failures.
- **Artifacts Produced:**
  - New source: `MarkdownGeneration/Services/AttributeChangeFilterContext.cs`, `IAttributeChangeFilter.cs`, `AttributeChangeFilterRegistry.cs`
  - New source: `Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs`
  - Modified: `CLI/CliParser.cs`, `CLI/HelpTextProvider.cs`, `MarkdownGeneration/ReportModel.cs`, `MarkdownGeneration/AotScriptObjectMapper.cs`, `MarkdownGeneration/ReportModelBuilder.cs`, `MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`, `MarkdownGeneration/ReportModelBuilder.Build.cs`, `Providers/IProviderModule.cs`, `Providers/AzureRM/AzureRMModule.cs`, `MarkdownGeneration/Services/ProviderRegistry.cs`, `CompositionRoot.cs`
  - New tests: `ReportModelBuilderIgnoreCaseChangesTests.cs`, `AzureResourceIdCaseChangeFilterTests.cs`, `AttributeChangeFilterRegistryTests.cs`; updated `CliParserTests.cs`, `HelpTextProviderTests.cs`, `AotScriptObjectMapperTests.cs`, `MarkdownRendererTests.cs`
  - New test data: `TestData/azurerm-case-only-ids-plan.json`
  - Updated: `README.md` (new `--ignore-case-changes` flag documentation + `#case-insensitive-azure-resource-id-filter` section)
  - Updated: all demo artifacts (version/timestamp bump)
- **Problems Encountered:** (1) Sonar rule S3267 required loop in `AttributeChangeFilterRegistry` to use LINQ, then S6605 required `List<T>.Exists()` instead of `.Any()`. (2) `/providers/Microsoft.Authorization/roleDefinitions/XYZ` is not recognized as an Azure resource ID by `AzureScopeParser` — required updating test data to use subscription-scoped role definition ID paths.

### Release Manager
- **Date:** 2026-02-26
- **Summary:** Assessed PR #564 for release readiness. Found **multiple blockers** that prevent merging:
  1. **PR is in DRAFT state** — cannot be merged until converted to ready for review.
  2. **Technical Writer has not logged work** — required for feature workflow; no `docs/features/103-azure-id-case-insensitive-filter/` Technical Writer entry exists.
  3. **Code Reviewer has not logged work** — required for feature workflow; no `code-review.md` in the feature folder, no GitHub reviews on PR #564, and the PR checklist shows Code Review as incomplete.
  4. **CI checks are "action_required"** — the most recent PR Validation and UAT artifact validation runs need manual workflow approval from the maintainer (GitHub's fork/copilot branch restriction).
  5. **A Copilot workflow ("Addressing comment on PR #564") is still in-progress** — triggered by `@copilot Continue` comment; must complete before new CI runs can be approved.
- **Artifacts Produced:** Updated `work-protocol.md` with this release manager entry.
- **Problems Encountered:** Release is **blocked** pending Technical Writer, Code Reviewer, CI approval, and PR draft-to-ready conversion. See release summary report for next steps.
