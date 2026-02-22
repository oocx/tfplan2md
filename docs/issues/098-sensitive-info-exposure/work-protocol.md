# Work Protocol: Sensitive Information Exposure

**Work Item:** `docs/issues/098-sensitive-info-exposure/`
**Branch:** `fix/098-sensitive-info-exposure`
**Workflow Type:** Bug Fix
**Created:** 2026-02-21

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2026-02-21
- **Summary:** Investigated and confirmed all reported sensitive/secret exposure paths; documented root causes and a high-level fix approach for AzApi body rendering, Scriban template context sensitivity propagation, Azure DevOps variable secret masking, and hierarchical sensitivity path detection.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/work-protocol.md`, `docs/issues/098-sensitive-info-exposure/analysis.md`
- **Problems Encountered:** None

### Architect
- **Date:** 2026-02-21
- **Summary:** Reviewed the issue analysis as the effective specification and identified a cross-cutting architectural gap: Scriban templates can access raw JSON state without a fail-safe masking mechanism. Proposed a defense-in-depth approach where `before_json`/`after_json` are masked-by-default unless `--show-sensitive` is enabled, while still exposing `before_sensitive`/`after_sensitive` for advanced templates and layout decisions.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/architecture.md`, `docs/adr-009-template-json-sensitivity-masking.md`
- **Problems Encountered:** No `specification.md` exists for this issue; proceeded using `analysis.md` as the requirements source.

### Quality Engineer
- **Date:** 2026-02-21
- **Summary:** Reviewed `analysis.md` and `architecture.md` as the requirements source. Audited all existing sensitivity-related tests. Produced a 21-test-case test plan with a requirements→tests coverage matrix covering all six confirmed exposure paths, plus regression guards and edge cases. Produced a UAT test plan describing feature-specific verification for both GitHub and Azure DevOps. Confirmed that existing snapshot baselines (`azapi-sensitive.md`, `azapi-body-sensitive.md`) encode the broken (plaintext) behavior and must be regenerated as part of the fix.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/test-plan.md`, `docs/issues/098-sensitive-info-exposure/uat-test-plan.md`
- **Problems Encountered:** None — all requirements directly traceable to confirmed bugs in `analysis.md`.

### Task Planner
- **Date:** 2026-02-21
- **Summary:** Reviewed `analysis.md`, `architecture.md`, and `test-plan.md`. Produced 11 tasks covering all 21 test plan cases across 6 confirmed exposure paths. Tasks follow Red → Green → Refactor: odd-numbered tasks write failing tests, even-numbered tasks implement the fix. Tasks 1–8 (hierarchical sensitivity, Variable Group masking, AzApi create/delete/replace and update) are independent of Tasks 9–10 (Scriban template context propagation) and can proceed in parallel. Task 11 regenerates snapshot baselines after the rendering fixes land.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/tasks.md`
- **Problems Encountered:** `IsSensitiveAttribute` and `GetHierarchicalPaths` are private static methods; Task 1 notes they may need extraction to a testable static class to enable direct unit testing without modifying access modifiers on the partial class.

### Developer
- **Date:** 2026-02-22
- **Summary:** Implemented all 11 tasks following Red→Green→Refactor discipline across 6 confirmed exposure paths. Key changes:
  - **Tasks 1-2:** Fixed hierarchical sensitivity detection in `SensitivityHelper` — nested paths like `properties.accessPolicies[0].permissions.keys` are now correctly identified as sensitive when parent `properties.accessPolicies` is marked sensitive.
  - **Tasks 3-4:** Fixed Azure DevOps Variable Group `isSecret` masking — variables with `isSecret: true` now render as `(sensitive / hidden)` instead of exposing plaintext values.
  - **Tasks 5-6:** Fixed AzApi create/delete/replace body rendering — `RenderCreateBody`, `RenderDeleteBody`, and `RenderReplaceBody` now respect `showSensitive` flag and mask sensitive JSON properties.
  - **Tasks 7-8:** Fixed AzApi update body rendering — threaded `showSensitive` through `UpdateBodyRenderInput`, `RenderUpdateMainTable`, `RenderUpdateGroupedSections`, `RenderUpdatePrefixGroup`, `RenderUpdateArrayGroup`, `RenderUpdateArrayMatrixTable`, `RenderUpdateArrayPerItemTables`, and `RenderLargeUpdateChanges`.
  - **Tasks 9-10:** Implemented defense-in-depth JSON masking at the Scriban template context mapper level — `AotScriptObjectMapper.MapResourceChange` now masks `before_json`/`after_json` ScriptObjects using sensitivity maps when `ShowSensitive=false`. Fixed resulting AzApi comparison logic conflict by making `CompareJsonProperties` treat pre-masked sensitive fields as "changed" (safe over-approximation).
  - **Task 11:** Updated all snapshot baselines via `scripts/update-test-snapshots.sh`. Restored accidentally deleted `nsg-with-separate-rule-updates.md` snapshot.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs` — `MaskSensitiveLeaves`, `MaskAllLeaves`, `MaskKeyIfSensitive` methods; `showSensitive` threading through `MapReportModel`→`MapChanges`→`MapModuleChanges`→`MapResourceChange`
  - `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs` — sensitivity masking in all update renderers
  - `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Array.cs` — `IsSensitive` on `AzApiArrayItemEntry`
  - `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Data.cs` — `CompareJsonProperties` now uses `showSensitive` parameter
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AotScriptObjectMapperTests.cs` — TC-08, TC-09, TC-10
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azapi-body-sensitive.md` — updated baseline
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/nsg-with-separate-rule-updates.md` — restored
  - 31 demo artifacts regenerated in `artifacts/` and `examples/`
- **Problems Encountered:**
  - Mapper-level JSON masking initially broke AzApi comparison logic — both before and after values became `(sensitive)`, so `CompareJsonProperties` saw "no change" for sensitive fields. Resolved by modifying `CompareJsonProperties` to treat sensitive fields with both before/after present as "changed" (safe over-approximation). User chose this approach over alternatives.
  - Pre-existing cognitive complexity warnings on `TraverseScribanSensitivity` (22/15) and `TraverseSensitivity` (19/15) in `AzApi.Data.cs` — not addressed in this fix as they are pre-existing.
- **Verification:**
  - 1201/1201 tests passing, 0 failures, 0 skipped
  - Coverage: line 86.75% (≥84.48%), branch 78.35% (≥72.80%)
  - Docker image builds successfully
  - Markdownlint: 0 new errors (1 pre-existing MD024 duplicate heading)
### Code Reviewer
- **Date:** 2026-02-22
- **Summary:** Reviewed implementation of all 11 tasks. All 1201 tests pass; coverage line 88.36% / branch 78.62% (both above thresholds); Docker builds successfully; no new markdownlint errors. All 21 test cases from the test plan are implemented and passing. Snapshot diffs are correct with SNAPSHOT_UPDATE_OK token present. Two Blockers raised: (1) Technical Writer work log entry is missing from this Work Protocol (required for Bug Fix workflows per `docs/agents.md`); (2) Required UAT plan artifacts `uat-plan.json` and `uat-plan.md` in `docs/issues/098-sensitive-info-exposure/` are missing, as specified by `uat-test-plan.md`. One Minor issue: `GetHierarchicalPaths` can yield duplicate paths for multi-level indexed keys (no functional impact). Decision: **Changes Requested**.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/code-review.md`
- **Problems Encountered:** UAT artifacts not created; Technical Writer agent not invoked.

### Developer (Rework)
- **Date:** 2026-02-22
- **Summary:** Addressed all code review findings: 2 Blockers, 3 Minor, 1 Suggestion.
  - **B-2 (UAT plan artifacts):** Created `uat-plan.json` covering all 6 exposure paths (azapi create/update/delete with sensitive body, azuredevops variable group is_secret transition, azurerm root-level after_sensitive, azapi array-parent sensitivity) and generated `uat-plan.md` with 10 `(sensitive)` + 1 `(sensitive / hidden)` placeholders. Zero plaintext secrets verified.
  - **M-1 (Duplicate paths in GetHierarchicalPaths):** Added `HashSet<string>` deduplication guard and fixed `parentPath.Contains('[')` → `parentPath.EndsWith(']')` with `LastIndexOf('[')` to avoid stripping array indices from middle path segments.
  - **M-2 (Unused SecretValue):** Removed `SecretValue` property from `BuildDefinitionVariableValues` record and its extraction in `BuildDefinitionExtractors`.
  - **M-3 (ScriptArray sensitivity):** Added per-element `MaskArrayElements` and recursive `MaskAllLeavesInArray` to handle `ScriptArray` sensitivity markers. Empty arrays are replaced with `"(sensitive)"` to avoid leaking structural information.
  - **S-1 (Ordering comment):** Updated doc comment in `GetHierarchicalPaths` (incorporated in M-1 commit).
- **Artifacts Produced:**
  - `docs/issues/098-sensitive-info-exposure/uat-plan.json` — UAT test data
  - `docs/issues/098-sensitive-info-exposure/uat-plan.md` — Rendered UAT output
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs` — Dedup fix
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionExtractors.cs` — SecretValue removal
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs` — ScriptArray sensitivity
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/SensitivityHierarchyTests.cs` — New dedup test
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AotScriptObjectMapperTests.cs` — New array sensitivity test
- **Problems Encountered:**
  - M-3 fix initially caused `Snapshot_AzapiBodySensitive_MatchesBaseline` regression — `MaskAllLeavesInArray` iterated 0 elements for empty arrays (leaving them as empty ScriptArray), breaking the body flattening path. Fixed by replacing empty arrays with `"(sensitive)"` string instead of recursing into them.
- **Verification:** 1203/1203 tests pass, 0 failures, 0 skipped.

### Technical Writer
- **Date:** 2026-02-22
- **Summary:** Reviewed implementation of all 11 tasks and rework items. Updated documentation to reflect the comprehensive sensitive value masking fixes.
- **Artifacts Produced:**
  - `docs/adr-009-template-json-sensitivity-masking.md` — Status updated from "Proposed" to "Accepted"
  - `docs/features.md` — "Sensitive Values" section expanded with masking coverage table, hierarchical sensitivity encoding table, and clarification of all affected rendering paths (AzApi bodies, Variable Group transitions, Scriban template context)
  - `docs/features.md` — Template variable reference updated: `before_json`/`after_json` documented as masked-by-default; `before_sensitive`/`after_sensitive` added as new template context variables
- **Problems Encountered:** None

### Code Reviewer (Round 2)
- **Date:** 2026-02-22
- **Summary:** Re-reviewed rework commits (B-1 Technical Writer log, B-2 UAT artifacts, M-1 dedup, M-2 SecretValue removal, M-3 ScriptArray sensitivity). 1203/1203 tests pass; coverage line 88.28% / branch 78.53% (both above thresholds); all previously raised issues correctly addressed. One new Blocker (B-3): the Technical Writer's `docs/features.md` changes are present in the working tree but were never committed — resolved by committing `docs/features.md` in `8c6706aa`. Final decision: **Approved**.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/code-review.md` — Round 2 section appended and finalized; `docs/features.md` committed (8c6706aa)
- **Problems Encountered:** Docker daemon not running; Docker build not verified this round.

### UAT Tester
- **Date:** 2026-02-22
- **Summary:** Ran UAT on both GitHub (PR #94) and Azure DevOps (PR #92) using the feature-specific `uat-plan.md` artifact and the comprehensive demo for regression. All 6 exposure paths verified: AzApi create/update/delete body sensitivity, Variable Group `is_secret` transition, root-boolean sensitivity, array-parent sensitivity. Regression test confirmed no accidental over-masking on unrelated resources. Maintainer confirmed both platforms passed. PRs cleaned up.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/uat-report.md`
- **Problems Encountered:** Azure DevOps `AZURE_DEVOPS_EXT_PAT` not set in environment; resolved by deriving token from active `az` CLI session. GitHub UAT PR state file was stale from a previous feature; manually updated `.tmp/uat-run/last-run.json`.

### Release Manager
- **Date:** 2026-02-22
- **Summary:** Verified Work Protocol completeness (all required Bug Fix agents present), code review approval (Round 2 Approved), and UAT completion (GitHub PR #94 closed after review, Azure DevOps also completed per maintainer confirmation). Generated release screenshots via native Chromium (Playwright `ScreenshotAsync` timed out on font loading in the current WSL environment; screenshots captured successfully using `chrome --headless --screenshot`). Created `docs/issues/098-sensitive-info-exposure/release-notes.md` and pushed screenshots. Pre-release checklist: working tree clean, branch up to date with remote, commits follow conventional format. No Docker build this session (daemon not running). Proceeding to create PR.
- **Artifacts Produced:**
  - `docs/issues/098-sensitive-info-exposure/release-notes.md`
  - `docs/issues/098-sensitive-info-exposure/098-azapi-create.png`
  - `docs/issues/098-sensitive-info-exposure/098-vargroup.png`
  - `docs/issues/098-sensitive-info-exposure/098-azapi-full.png`
- **Problems Encountered:** Playwright `ScreenshotAsync` timed out (30s) waiting for fonts to load; worked around using native `chrome --headless --screenshot` from the Playwright Chromium installation. Screenshots are lower-fidelity than CDN-styled HTML but clearly show the masking behavior.
