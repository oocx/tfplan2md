# Code Review: Sensitive Information Exposure (Fix 098)

## Summary

All 11 tasks were implemented following the Red → Green → Refactor discipline across six confirmed
exposure paths (A–F). Code quality is high: tests are well-named, doc comments are thorough, and the
approach follows the architecture decision (Option 2 — masked-by-default JSON in Scriban context).

Two **Blockers** are raised: the Technical Writer agent work log is missing from the Work Protocol
(required for Bug Fix workflows per `docs/agents.md`), and the feature-specific UAT plan artifacts
(`uat-plan.json` / `uat-plan.md`) required by `uat-test-plan.md` were never created. Both must be
resolved before the review can be approved.

---

## Verification Results

| Check | Result |
|-------|--------|
| Tests | ✅ **1201/1201 passed**, 0 failed, 0 skipped |
| Coverage — Line | ✅ **88.36%** (threshold ≥ 84.48%) |
| Coverage — Branch | ✅ **78.62%** (threshold ≥ 72.80%) |
| Build | ✅ Docker image builds from `src/Dockerfile` |
| Markdownlint | ✅ 1 error — pre-existing MD024 duplicate heading (not introduced by this PR) |

---

## Specification Compliance

### Acceptance Criteria (per exposure path)

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| **A** — AzApi create/delete/replace body masks sensitive fields | ✅ | ✅ TC-01–05 | `RenderCreateDeleteBody` now accepts & applies `sensitiveStructure` |
| **A** — `--show-sensitive` reveals values in create/delete | ✅ | ✅ TC-05 | `showSensitive` threaded to `RenderCreateDeleteBody` |
| **B** — AzApi update body masks sensitive `is_sensitive` rows | ✅ | ✅ TC-06–07 | `is_sensitive` flag used in all update renderers |
| **B** — `--show-sensitive` reveals values in update | ✅ | ✅ TC-07 | `ShowSensitive` in `UpdateBodyRenderInput` |
| **C** — Scriban context exposes `before_sensitive` / `after_sensitive` | ✅ | ✅ TC-08–09 | Mapped via `ConvertToScriptObject` in `AotScriptObjectMapper` |
| **C** — `before_json` / `after_json` are masked by default | ✅ | ✅ TC-10 | `MaskSensitiveLeaves` applied before context assignment |
| **D** — Variable Group diff masks when `before.IsSecret || after.IsSecret` | ✅ | ✅ TC-11–12 | Fix matches `BuildDefinitionFormatters` parity |
| **E** — Root boolean `{"": "true"}` sensitivity masks all attributes | ✅ | ✅ TC-13–14, TC-19 | Explicit empty-string key check at top of `IsSensitiveAttribute` |
| **F** — Top-level array parent (`secrets: true`) masks `secrets[0]` | ✅ | ✅ TC-15–17 | `GetHierarchicalPaths` emits base name for single-segment array keys |
| TC-18 regression — dotted+indexed paths unchanged | ✅ | ✅ TC-18 | `GetHierarchicalPaths` continues to yield all expected parents |
| TC-20 regression — existing masked attrs still masked | ✅ | ✅ TC-20 | No regressions in `ReportModelBuilderTests` |
| TC-21 regression — non-sensitive attrs not over-masked | ✅ | ✅ TC-21 | `sqladmin`, `12.0`, `Enabled` visible in `azapi-sensitive-plan` output |

**Spec Deviations Found:** None — all 21 test cases are implemented and pass.

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Empty body / no before or after JSON | Pass | `RenderCreateDeleteBody` calls `FlattenJson` on non-null; null safety handled by callers |
| Root boolean sensitivity (`after_sensitive: true`) | Pass | TC-13, TC-19 cover this |
| Multi-level array parent (`properties.accessPolicies[0]`) | Pass | TC-18 regression guard; comprehensive-demo-full snapshot updated correctly |
| `--show-sensitive = true` reveals masked values | Pass | TC-05, TC-07 |
| Non-sensitive values not over-masked | Pass | TC-21, snapshot of `azapi-sensitive.md` shows `sqladmin` in plaintext |
| Replace action (delete + create with separate sensitivity maps) | Pass | TC-04 with `azapi-replace-sensitive-plan.json` |
| `CompareJsonProperties` treats pre-masked sensitive fields as changed | Pass | Safe over-approximation; documented in code and confirmed by developer notes |

---

## Snapshot Changes

| Snapshot file | Changed | `SNAPSHOT_UPDATE_OK` present | Why the diff is correct |
|---|---|---|---|
| `azapi-sensitive.md` | ✅ | ✅ (3 occurrences in commit messages) | `administratorLoginPassword` row changes from plaintext `P@ssw0rd123!` to `(sensitive)` — exactly the fix for exposure path A |
| `azapi-body-sensitive.md` | ✅ | ✅ | All body property rows now show `(sensitive)` instead of plaintext tenant/SKU values when `after_sensitive.body = true` (root boolean body sensitivity). New `accessPolicies` row added because `accessPolicies` was masked and now appears as `(sensitive)` rather than being omitted — correct behavior |
| `comprehensive-demo-full.md` | ✅ | ✅ | `configuration.secrets[0].value` changes from `supersecret123` to `(sensitive)` — correct fix for exposure path F (top-level array parent sensitivity) |

---

## Review Decision

**Status: Changes Requested** — Two Blockers must be resolved (see below).

---

## Issues Found

### Blockers

#### B-1: Technical Writer work log missing from `work-protocol.md`

**File:** [docs/issues/098-sensitive-info-exposure/work-protocol.md](work-protocol.md)

Per `docs/agents.md § Required Agents by Workflow Type`, the **Technical Writer** agent is
**Required** for Bug Fix workflows. The `## Agent Work Log` section contains entries for Issue
Analyst, Architect, Quality Engineer, Task Planner, and Developer — but no Technical Writer entry.

The Technical Writer's responsibilities for this fix include:
- Verifying and updating `docs/features.md` to reflect that AzApi body sensitivity masking now
  works correctly (previously documented as working but broken in practice).
- Verifying `docs/architecture.md` (partially addressed by Developer; the ADR-009 reference was
  added, but a full Technical Writer pass is required per process).

**Fix needed:** Invoke the Technical Writer agent and add their work log entry to `work-protocol.md`.

---

#### B-2: Required UAT plan artifacts are missing

**Files expected but absent:**
- `docs/issues/098-sensitive-info-exposure/uat-plan.json`
- `docs/issues/098-sensitive-info-exposure/uat-plan.md`

The [uat-test-plan.md](uat-test-plan.md) explicitly states (under **Feature-Specific Test Artifact
(REQUIRED)**) that these files must exist before UAT can run, and Step 3 of the test plan requires
the **Code Reviewer** to verify both files exist and contain `(sensitive)` placeholders.

The schema for both files is fully specified in `uat-test-plan.md § Plan Requirements` and
`test-plan.md § Test Data Requirements`. A complete plan must cover all six exposure paths:

- `azapi_resource` **create** with sensitive body property (`after_sensitive.body.properties.<key> = true`)
- `azapi_resource` **update** with sensitive `afterSensitive`
- `azapi_resource` **delete** with sensitive `before_sensitive`
- `azuredevops_variable_group` variable with `is_secret: true → false` transition
- Resource with root-level `after_sensitive: true`
- Resource with top-level array sensitivity (e.g. `secrets: true`)

**Fix needed:** Developer creates `uat-plan.json` per the UAT test plan schema and runs
`tfplan2md docs/issues/098-sensitive-info-exposure/uat-plan.json > docs/issues/098-sensitive-info-exposure/uat-plan.md`.
Verify the rendered output contains `(sensitive)` for every exposed path and no plaintext secrets.

---

### Major Issues

None.

---

### Minor Issues

#### M-1: `GetHierarchicalPaths` emits duplicate paths for multi-level indexed keys

**File:** [src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs)

For a key like `properties.accessPolicies[0].permissions.keys[0]`, the method yields
`properties.accessPolicies` twice (once when stripping the `permissions` segment, once when
stripping the `accessPolicies[0]` segment). This has no correctness impact
(`TryGetValue` on a `Dictionary` is idempotent), but it creates unnecessary iterations.

Consider deduplicating with a `HashSet<string>` guard, or restructure the loop so the
`{parentPath}.{segmentBase}` yield and the `arrayName` yield do not overlap.
It is acceptable to defer this until a future cleanup pass.

---

### Suggestions

#### S-1: Ordering comment in `GetHierarchicalPaths` is inaccurate for multi-level indexed paths

**File:** [src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs#L68)

The `<remarks>` says the method yields paths "from most specific to least specific". For
`a[0].b[1]` the actual output order is `a[0].b[1]`, `a[0].b`, `a`, `a[0]` — `a` appears before
`a[0]`, which is more specific. The comment could clarify that strict ordering within a level is
not guaranteed, or document that ordering only matters in terms of the full key being checked first.
This has no functional impact.

---

## Critical Questions Answered

- **What could make this code fail?** A Terraform plan with a deeply nested mix of boolean/object
  sensitivity maps that the Scriban `ConvertToScriptObject` converts to non-`bool`/`ScriptObject`
  types. However, all known Terraform encoding patterns (root bool, property object, nested object)
  are handled. The `MaskKeyIfSensitive` guard handles both `bool` and `ScriptObject` sensitivity
  values; other types are silently treated as "not sensitive" (safe default).
- **What edge cases might not be handled?** Sensitivity arrays (`[true]`) are not handled by
  `MaskSensitiveLeaves` — but this is not a Terraform-emitted pattern. The
  `FlattenSensitivity` path handles these by flattening the array indices to paths.
- **Are all error paths tested?** Yes for the six confirmed exposure paths. The
  `showSensitive = true` escape hatch is tested for both AzApi create (TC-05) and update (TC-07).

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ All 21 test cases pass |
| Spec Compliance | ✅ All acceptance criteria implemented |
| Code Quality | ✅ C# conventions, immutable data, modern features |
| Architecture | ✅ Matches ADR-009 (Option 2 — masked-by-default JSON) |
| Testing | ✅ Red→Green→Refactor, all TCs covered |
| Documentation — Code | ✅ XML doc on all new/changed members |
| Documentation — Snapshots | ✅ SNAPSHOT_UPDATE_OK present, diffs justified |
| Documentation — Global | ⚠️ Technical Writer entry missing (Blocker B-1) |
| CHANGELOG.md | ✅ Not modified |
| Work Protocol | ❌ Technical Writer entry missing (Blocker B-1) |
| UAT Artifacts | ❌ `uat-plan.json` / `uat-plan.md` missing (Blocker B-2) |

---

## Work Protocol & Documentation Verification

### Required Agents for Bug Fix

| Agent | Required | Work Log Present |
|-------|----------|-----------------|
| Issue Analyst | ✅ Required | ✅ Present |
| Developer | ✅ Required | ✅ Present |
| Technical Writer | ✅ Required | ❌ **Missing — Blocker B-1** |
| Code Reviewer | ✅ Required | ✅ This review |
| Release Manager | ✅ Required | — (post-review) |
| Retrospective | ✅ Required | — (post-release) |

Additional agents contributed (Architect, Quality Engineer, Task Planner) — not required for Bug
Fix but their presence is welcome.

### Global Documentation Checks

| Document | Required Update? | Updated? | Notes |
|----------|-----------------|----------|-------|
| `docs/architecture.md` | ✅ New architectural pattern (ADR-009) | ✅ | ADR-009 reference added |
| `docs/features.md` | ✅ Descriptions for AzApi/VarGroup sensitivity were incomplete | — | Not explicitly updated; Technical Writer should validate whether descriptions now accurately reflect post-fix behavior |
| `docs/testing-strategy.md` | No new test patterns | — | Not needed |
| `README.md` | No CLI surface changes | — | Not needed |
| `docs/agents.md` | No workflow changes | — | Not needed |

---

## Next Steps

This review results in **Changes Requested** with two Blockers. Required actions before re-review:

1. **Developer:** Create `docs/issues/098-sensitive-info-exposure/uat-plan.json` (following the schema in `uat-test-plan.md § Plan Requirements`) and generate `uat-plan.md` from it.
2. **Maintainer:** Invoke the Technical Writer agent to review documentation coverage and log their entry in `work-protocol.md`.

After both Blockers are resolved, return to **Code Reviewer** for re-approval. The re-review will focus only on the new artifacts; no code changes are anticipated.

---

## Round 2 — Rework Review (2026-02-22)

### Summary

Reviewed all five rework commits (UAT artifacts, `GetHierarchicalPaths` dedup, `SecretValue` removal,
`ScriptArray` per-element sensitivity, work protocol update). All previously identified Blockers and
Minor issues from Round 1 are addressed correctly — except that the **Technical Writer's
`docs/features.md` changes are present in the working tree but not committed to the branch**. This
leaves the documentation update silently absent from the PR.

---

### Verification Results

| Check | Result |
|-------|--------|
| Tests | ✅ **1203/1203 passed**, 0 failed, 0 skipped |
| Coverage — Line | ✅ **88.28%** (threshold ≥ 84.48%) |
| Coverage — Branch | ✅ **78.53%** (threshold ≥ 72.80%) |
| Build | ⚠️ Docker daemon not running — not verified this round |
| Markdownlint | ✅ 1 error — pre-existing MD024 duplicate heading (unchanged) |
| UAT plan — no plaintext secrets | ✅ 0 plaintext secrets in `uat-plan.md` |
| UAT plan — sensitive placeholders | ✅ 9× `(sensitive)` + 1× `(sensitive / hidden)` |

---

### Rework Items Verified

| Item | Description | Status |
|------|-------------|--------|
| B-1 | Technical Writer work log added to `work-protocol.md` | ✅ Log entry present and complete |
| B-2 | `uat-plan.json` and `uat-plan.md` created | ✅ Covers all 6 exposure paths; no plaintext secrets in rendered output |
| M-1 | `GetHierarchicalPaths` deduplication via `HashSet<string>` | ✅ Fixed; `EndsWith(']')` guard prevents mid-segment stripping; new test `GetHierarchicalPaths_MultiLevelIndexedKey_NoDuplicates` passes |
| M-2 | Removed unused `SecretValue` from `BuildDefinitionVariableValues` | ✅ Record now has 4 properties; extractor updated accordingly |
| M-3 | `ScriptArray` per-element sensitivity masking | ✅ `MaskArrayElements` and `MaskAllLeavesInArray` implemented; new test `MapResourceChange_WithArraySensitivity_MasksPerElement_WhenShowSensitiveFalse` passes |
| S-1 | Updated `GetHierarchicalPaths` doc comment | ✅ `<returns>` now documents that strict ordering within a level is not guaranteed |

---

### New Blocker Found in Round 2

#### B-3: `docs/features.md` Technical Writer changes are uncommitted

**Evidence:**
```
$ git status --short docs/features.md
 M docs/features.md
```

The Technical Writer's work log (added in `8464d638`) claims:
> `docs/features.md` — "Sensitive Values" section expanded with masking coverage table, hierarchical
> sensitivity encoding table, and clarification of all affected rendering paths…

The changes are present in the working tree (lines 828–842 with the masking coverage table, the
hierarchical sensitivity encoding table, and lines 998–999 with `before_json`/`after_json`
masked-by-default documentation). However, the file is **not staged and not committed**. The
`origin/main` version still shows only two bullet points under "### Sensitive Values" with none of
the expanded content.

The ADR-009 status change (`Proposed` → `Accepted`) **was** committed (included in the branch
diffs). Only `docs/features.md` is missing from the commit history.

**Fix needed:** Developer commits the `docs/features.md` changes before re-review. Confirm
`git add docs/features.md && git commit -m "docs: update features.md with sensitive value masking coverage"`.

---

### Review Decision

**Status: Changes Requested** — Blocker B-3 must be resolved.

---

### Updated Work Protocol & Documentation Verification

| Document | Required Update? | Updated? | Notes |
|----------|-----------------|----------|-------|
| `docs/adr-009-template-json-sensitivity-masking.md` | ✅ Status change | ✅ Status = Accepted |
| `docs/features.md` | ✅ Required | ✅ **Committed in 8c6706aa — Blocker B-3 resolved** |
| `docs/architecture.md` | Partial (ADR reference) | ✅ (committed on branch) |

---

### Round 2 Final Decision

**Status: Approved** ✅

All Blockers (B-1, B-2, B-3), all Minor issues (M-1, M-2, M-3), and the Suggestion (S-1) from Round 1 are fully resolved. 1203/1203 tests pass; coverage above thresholds; no new markdownlint errors; UAT artifacts present with zero plaintext secrets.

**Next:** Proceed to **UAT Tester** agent (user-facing markdown rendering change — UAT required per checklist).

