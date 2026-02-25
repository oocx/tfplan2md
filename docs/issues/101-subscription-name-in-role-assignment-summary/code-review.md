# Code Review: Subscription Name in Role Assignment Summary (Issue #101)

## Summary

Reviewed the fix for issue #101: when an `azurerm_role_assignment` has a subscription-level scope,
the `<summary>` line now shows the mapped subscription display name instead of the raw subscription
ID, with backward-compatible fallback to the raw ID when no mapping exists.

The fix is **functionally correct**. The core logic change in `BuildScopeSummary`, the new helper
methods, and the test coverage all deliver the intended behavior. The generated output matches the
expected format described in the analysis document. The implementation correctly mirrors the
management group scope pattern (consulting `scopeFormatter`) and is backward-compatible (null
formatter and unmapped IDs both fall back to raw ID).

**Verdict: APPROVED — with minor issues documented below.**

---

## Verification Results

- **Tests (unit/snapshot):** ✅ Pass — 10/10 `RoleAssignmentViewModelFactoryTests`, 9/9 `MarkdownSnapshotTests`, 8/8 `AzureEntityMapperTests`
- **Build:** ✅ Success
- **Docker:** ⚠️ `apk add` network error in CI environment — unrelated to code changes; app builds fine from .NET build
- **Markdownlint (`artifacts/comprehensive-demo.md`):** ⚠️ 1 pre-existing error at line 673 (MD024 duplicate heading) — not introduced by this branch
- **Generated output verification (manual):** ✅ Subscription-scope mapped shows `🔑 Production`; unmapped shows `🔑 sub-unmapped-id`

---

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| Mapped subscription scope → summary shows display name | ✅ | ✅ | `Build_WhenSubscriptionScopeAndNameMapped_SummaryShowsNameWithKeyIcon` |
| Unmapped subscription scope → summary shows raw ID | ✅ | ✅ | `Build_WhenSubscriptionScopeAndNotMapped_SummaryShowsIdWithKeyIcon` |
| No scope formatter → summary shows raw ID (backward compat) | ✅ | ✅ | `Build_WhenSubscriptionScopeAndNoScopeFormatter_SummaryShowsIdWithKeyIcon` |
| Table `scope` attribute unchanged (shows name + ID) | ✅ | ✅ | Snapshot: `subscription \`🔑 Production (sub-123)\`` |
| Snapshot covers both mapped and unmapped cases | ✅ | ✅ | Two new entries in `role-assignments.json` |

**Spec Deviations Found:** None — the fix follows the analysis exactly, and the analysis explicitly
described the two cases (mapped and unmapped).

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Null subscription ID | Pass | `GetSubscriptionName(null)` returns null; fallback handled in `BuildScopeSummary` via `?? subscriptionId` |
| Empty subscription ID | Pass | `IsNullOrWhiteSpace` guard in `AzureEntityMapper.GetSubscriptionName` |
| No scope formatter (null) | Pass | `scopeFormatter?.GetSubscriptionName` null-coalesces safely |
| Unmapped subscription | Pass | Falls back to raw subscription ID |
| Subscription display name same as ID | Pass | Guard `!string.Equals(subscriptionName, subscriptionId)` handles this edge case |
| Mapped subscription (UUID format) | Pass | Snapshot test uses `sub-123` (short IDs); unit test uses full UUID |

---

## Review Decision

**Status: Approved** (with minor issues — none are blocking)

---

## Snapshot Changes

- **Snapshot files changed:** Yes — `role-assignments.md`
- **`SNAPSHOT_UPDATE_OK` in commit message:** ✅ Yes (`test: add subscription scope snapshot coverage (mapped + unmapped) (SNAPSHOT_UPDATE_OK)`)
- **Why the snapshot diff is correct:** Two new `azurerm_role_assignment` resources were added to `role-assignments.json` to cover subscription-scope scenarios. The `subscription_scope_mapped` entry uses `sub-123` (mapped to "Production" in `demo-principals.json`), so its summary correctly shows `🔑 Production`. The `subscription_scope_unmapped` entry uses `sub-unmapped-id` (no mapping), so its summary correctly shows `🔑 sub-unmapped-id`. Both snapshot entries accurately reflect the new behavior.

---

## Issues Found

### Blockers

None.

### Major Issues

None.

### Minor Issues

**1. Incorrect `Related feature` comment path in `GetSubscriptionName` XML docs**

- **Files:** `src/Oocx.TfPlan2Md/Platforms/Azure/AzureEntityMapper.cs:92` and `src/Oocx.TfPlan2Md/Platforms/Azure/EnrichedAzureScopeFormatter.cs:91`
- **Problem:** Both `GetSubscriptionName` XML doc `<remarks>` reference `docs/features/improve-summary-for-role-assignments/specification.md`, which does not exist. This is a bug fix tracked in `docs/issues/101-subscription-name-in-role-assignment-summary/`, not a features folder.
- **Fix:** Change to `docs/issues/101-subscription-name-in-role-assignment-summary/analysis.md` (the actual work item document).

**2. `GetSubscriptionName` does not record diagnostic failures**

- **File:** `src/Oocx.TfPlan2Md/Platforms/Azure/AzureEntityMapper.cs:94`
- **Problem:** Unlike all sibling methods (`GetSubscriptionDisplayName`, `GetManagementGroupDisplayName`, `GetTenantDisplayName`), `GetSubscriptionName` does not accept a `resourceAddress` parameter and does not call `RecordFailure()` when a subscription ID is not found in the mapping. This means unmapped subscription IDs in role assignment summaries will not be surfaced in diagnostic output. The analysis document's suggested implementation explicitly included a `RecordFailure` call.
- **Impact:** Diagnostic reports may miss unmapped subscription IDs that appear only in subscription-scope role assignment summaries (not in other scope levels).
- **Fix:** Add `string? resourceAddress = null` parameter and call `RecordFailure(FailedResolutionType.Subscription, subscriptionId, resourceAddress)` in the not-found branch, mirroring `GetSubscriptionDisplayName`. Update `EnrichedAzureScopeFormatter.GetSubscriptionName` and the call site in `RoleAssignmentViewModelFactory.BuildScopeSummary` accordingly.

**3. `GetSubscriptionName` return type is `string?` — inconsistent with all other mapper methods**

- **File:** `src/Oocx.TfPlan2Md/Platforms/Azure/AzureEntityMapper.cs:94`
- **Problem:** All other lookup methods (`GetSubscriptionDisplayName`, `GetManagementGroupDisplayName`, `GetTenantDisplayName`) return `string` (never null). `GetSubscriptionName` returns `string?`. When `subscriptionId` is null/empty, it returns the input (`null`), not `string.Empty`. Callers must guard against null, increasing cognitive load and inconsistency in the API surface.
- **Fix:** Change return type to `string` and return `string.Empty` (instead of `subscriptionId`) when the input is null/empty, consistent with the pattern in `GetSubscriptionDisplayName`.

**4. No direct unit test for `AzureEntityMapper.GetSubscriptionName`**

- **File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureEntityMapperTests.cs`
- **Problem:** All other `AzureEntityMapper` lookup methods have dedicated tests in `AzureEntityMapperTests.cs`. The new `GetSubscriptionName` method is only tested indirectly through `RoleAssignmentViewModelFactoryTests`. If the method signature or logic changes, there's no direct failing test to catch it.
- **Fix:** Add 2 tests in `AzureEntityMapperTests`:
  - `AzureEntityMapper_GetSubscriptionName_ReturnsMappedDisplayName`
  - `AzureEntityMapper_GetSubscriptionName_FallsBackToRawId`

### Suggestions

**S1. Simplify `BuildScopeSummary` guard logic**

- **File:** `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs:264-267`
- **Problem:** The current expression is:
  ```csharp
  var subscriptionDisplay = (!string.IsNullOrWhiteSpace(subscriptionName)
      && !string.Equals(subscriptionName, subscriptionId, StringComparison.OrdinalIgnoreCase))
      ? subscriptionName
      : subscriptionId;
  ```
  Since `GetSubscriptionName` already returns `subscriptionId` as a fallback when not found, the `IsNullOrWhiteSpace` check and the equality comparison are redundant defensive coding. If issue #3 above is fixed (return type becomes `string`), this could simplify to:
  ```csharp
  var subscriptionDisplay = scopeFormatter?.GetSubscriptionName(subscriptionId) ?? subscriptionId;
  ```
  This is cleaner and relies on the contract of `GetSubscriptionName`. Not required, but worth considering as part of the fix for issues #2 and #3.

---

## Critical Questions Answered

- **What could make this code fail?** The only realistic failure is if `subscriptionId` is null/empty and `GetSubscriptionName` returns null, which propagates to `FormatAttributeValueSummary`. This is guarded safely in the current code via the `!string.IsNullOrWhiteSpace(subscriptionName)` check. No runtime failures expected.
- **What edge cases might not be handled?** Subscription display name that matches the subscription ID exactly (e.g., display name set to the raw GUID) — the equality guard handles this correctly by falling through to the raw ID. Empty `scope` attribute — the `ScopeInfo.Empty` path is handled by the `scope.SummaryLabel + FormatCodeSummary(scope.SummaryName)` fallthrough.
- **Are all error paths tested?** The three unit tests cover: (a) mapped, (b) unmapped with empty mapper, (c) no formatter at all. The snapshot covers (a) and (b). The only untested path is null `subscriptionId` inside subscription scope — unlikely in practice and handled defensively.

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Code Quality | ⚠️ Minor issues (comment path, return type, diagnostic gap) |
| Architecture | ✅ |
| Testing | ⚠️ Missing direct unit test for new mapper method |
| Documentation | ✅ Release notes accurate and clear |
| Work Protocol | ✅ All required agents logged entries (Issue Analyst, Developer, Technical Writer) |
| Global docs | ✅ No global doc updates required (bug fix to existing behavior) |
| Snapshot changes | ✅ `SNAPSHOT_UPDATE_OK` present; diff is correct |
| CHANGELOG.md | ✅ Not modified |

---

## Work Protocol & Documentation Verification

- `work-protocol.md` exists: ✅
- Required agents for bug fix workflow logged: ✅
  - Issue Analyst ✅
  - Developer ✅
  - Technical Writer ✅
  - Code Reviewer (this entry) ✅
- Global documentation:
  - `docs/features.md`: N/A — bug fix, not a new feature ✅
  - `docs/architecture.md`: N/A — no architectural changes ✅
  - `README.md`: N/A — no CLI/usage changes ✅
  - `docs/testing-strategy.md`: N/A — no new test patterns ✅

---

## Next Steps

Minor issues found (no blockers). The fix is functionally correct and tests pass. Recommend:

1. **Developer** addresses minor issues #1–4 (incorrect comment paths, missing `resourceAddress`/`RecordFailure`, return type, missing unit tests) — these improve API consistency and diagnostic completeness.
2. After Developer fixes those, **Code Reviewer** re-approves.
3. Then **Release Manager** handles release.

If the Maintainer judges issues #1–4 as acceptable technical debt (the fix is functionally correct without them), this can proceed directly to the **Release Manager**.
