# Work Protocol: azapi Body Casing Filter

**Work Item:** `docs/issues/108-azapi-body-casing-filter/`
**Branch:** `copilot/filter-out-casing-changes`
**Workflow Type:** Bug Fix
**Created:** 2026-03-02

## Agent Work Log
### Developer — 2026-03-02

**Summary:** Implemented the fix for casing-only Azure ID change suppression in azapi body comparison. The fix threads the existing `--ignore-azure-id-case-changes` CLI flag through to the body comparison helper (`ValuesEqual`) and registers a new attribute-level filter for azapi.

**Artifacts Produced:**
- `src/Oocx.TfPlan2Md/Providers/AzApi/AzApiResourceIdCaseChangeFilter.cs` — New attribute-level filter for azapi provider
- Modified `AzApi.Data.cs` — `ignoreAzureIdCaseChanges` parameter in `CompareJsonProperties` and `ValuesEqual`
- Modified `AzApi.Rendering.cs` — `ignoreAzureIdCaseChanges` parameter in `RenderAzapiBody`
- Modified `AzApi.Rendering.Update.cs` — `IgnoreAzureIdCaseChanges` field in `UpdateBodyRenderInput`
- Modified `AzApi.Registration.cs` — Updated Scriban function imports
- Modified `resource.sbn`, `update_resource.sbn`, `_output_values.sbn` — Pass `ignore_azure_id_case_changes` to `render_azapi_body`
- Modified `AzApiModule.cs` — Override `RegisterAttributeChangeFilters` with new filter
- New tests: `AzApiResourceIdCaseChangeFilterTests.cs` (9 unit tests), 4 new `CompareJsonProperties` tests, 2 new `RenderAzapiBody` tests
- All 1333 tests pass with 0 failures, 0 skips

**Problems Encountered:**
- Initial `ValuesEqual` implementation used nested `if` which triggered SonarSource S1066 rule. Fixed by merging conditions into a single `if` with short-circuit evaluation.
- Code review suggested optimizing to check case-insensitive equality before the regex `IsAzureResourceId` check — applied the optimization.


**Summary:** Investigated the azapi casing-only change bug. Identified two distinct rendering pipelines where the fix must be applied, with the primary pipeline being the AzAPI body comparison helper (`CompareJsonProperties`/`ValuesEqual`). Documented root cause, affected files with line numbers, proposed fix approach, and edge cases.

**Artifacts Produced:**
- `docs/issues/108-azapi-body-casing-filter/analysis.md` — Full root cause analysis with file references and proposed fix

**Problems Encountered:** None. Root cause was clear from the code.

### Technical Writer — 2026-03-02

**Summary:** Updated documentation to reflect that the `--ignore-azure-id-case-changes` filter now also applies to `azapi` body properties. Two documentation files contained explicit statements that the filter was `azurerm`-only; both have been updated to reflect the expanded scope of the bug fix.

**Artifacts Produced:**
- `README.md` (line 337): Updated scope note — filter now covers `azurerm` resources and body properties of `azapi_resource` / `azapi_update_resource`
- `docs/features.md` (Feature 103 section, line 3024): Same scope note update

**Problems Encountered:** None. Changes were minimal and targeted.

### Code Reviewer — 2026-03-02

**Summary:** Reviewed the implementation. All 1333 tests pass. Markdownlint clean.
Implementation is correct and consistent with the existing `AzureResourceIdCaseChangeFilter`
pattern. Approved with minor suggestions.

**Artifacts Produced:**
- `docs/issues/108-azapi-body-casing-filter/code-review.md` — Full code review report

**Problems Encountered:**
- Minor: Multiple files reference `docs/issues/filter-out-casing-changes` (non-existent path)
  instead of `docs/issues/108-azapi-body-casing-filter/`. Cosmetic only; does not affect
  behaviour or test correctness. Noted in review as Minor issue.

**Decision:** ✅ Approved — ready for UAT Tester (user-facing markdown rendering change).
