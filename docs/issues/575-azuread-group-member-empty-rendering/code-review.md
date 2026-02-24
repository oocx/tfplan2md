# Code Review: azuread_group_member Empty Rendering Bug Fix (Issue 575)

## Summary

Reviewed the fix for `azuread_group_member` (and any resource where all attributes are "known after
apply") rendering with an empty attribute table. The fix correctly extends `BuildAttributeChanges` in
`ReportModelBuilder.ResourceChanges.cs` to also consult `change.AfterUnknown`, populates `allKeys`
from it, displays those attributes as `(known after apply)`, and bypasses the unchanged-values filter
for them.

**Decision: ✅ Approved** with two minor notes documented below.

---

## Verification Results

- **Tests:** 1237 non-Docker tests passing (0 failed, 0 skipped); Docker tests skipped (Docker not
  available in CI environment)
- **Build:** ✅ Success
- **Docker:** Skipped (not available)
- **AzureAd snapshot tests (targeted run):** All 5 passed in 3.4 s
- **Markdownlint on `artifacts/comprehensive-demo.md`:** 1 pre-existing MD024 error at line 710
  (duplicate heading `📦 Module: module.network`). **Not introduced by this fix** — verified by
  checking the error against `origin/main`.
- **Manual generation of `azuread-group-member-all-unknown-plan.json`:** Output matches snapshot
  baseline exactly — `group_object_id`, `id`, and `member_object_id` all rendered as
  `(known after apply)`.

---

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---|---|---|---|
| Resources with `after: null` and all attrs in `after_unknown` render attribute table | ✅ | ✅ | New fixture + snapshot |
| Attributes shown as `(known after apply)` | ✅ | ✅ | `KnownAfterApplyDisplay` constant |
| Mixed case (`after.attr = null` + `after_unknown.attr = true`) shows `(known after apply)` | ✅ | ✅ | Covered by `azuread-group-members-known-after-apply` fixture |
| Previously filtered `null == null` rows now shown | ✅ | ✅ | `valuesEqual = false` when `isUnknown` |
| Unchanged rows still filtered when `--show-unchanged-values` not used | ✅ | ✅ | Existing snapshot regressions pass |
| Sensitive attributes still masked as `(sensitive)` | ✅ | ✅ | `isUnknown` check is first; sensitivity applied to `beforeDisplay` |

**Spec Deviations Found:** None

---

## Adversarial Testing

| Test Case | Result | Notes |
|---|---|---|
| `after: null`, all attrs in `after_unknown` | ✅ Pass | Primary bug scenario |
| Mixed: some known in `after`, some in `after_unknown` | ✅ Pass | `group_object_id` unknown, `member_object_id` known |
| `after_unknown: {}` (empty) | ✅ Pass | No change — existing behavior preserved |
| `--show-unchanged-values` with unknown attrs | ✅ Pass | Unknown rows always shown regardless of flag |
| Sensitive + unknown combo (`primary_access_key`) | ✅ Pass | Renders `(sensitive)` before, `(known after apply)` after |
| Nested `after_unknown` objects | Not explicitly tested | `JsonFlattener` produces dotted-path keys (e.g., `network_interface.ip_configuration.private_ip_address`); these appear correctly in the flat table |
| Resource with no `after_unknown` key | ✅ Pass | `afterUnknownDict` is empty → `allKeys` unchanged |

---

## Snapshot Changes

- **Snapshot files changed:** Yes — 7 existing snapshots updated, 1 new snapshot added
- **`SNAPSHOT_UPDATE_OK` in commit message:** ✅ Yes — commit `c27a1270`
- **Why the snapshot diff is correct:**
  - **New snapshot** (`azuread-group-member-all-unknown.md`): Shows `group_object_id`, `id`, and
    `member_object_id` as `(known after apply)` — this is the primary bug fix.
  - **`azuread-group-members-known-after-apply.md`**: Adds `id` and `object_id` rows to
    `azuread_group.platform_engineers` — these attributes were in `after_unknown` but previously
    silently dropped due to `null == null` being treated as "unchanged". Correct to now show them.
  - **`no-configuration-block.md`**: Adds `group_object_id | (known after apply)` to the
    standalone group member resource — attribute was in `after_unknown` but previously dropped.
    Correct.
  - **`ephemeral-open.md`**: `null_resource.app_config` gains `id | (known after apply)` — the
    `null_resource` has `after_unknown: { id: true }`. Previously rendered "_No attribute
    changes._". Correct.
  - **`azure-display-enhancements.md`, `azuredevops-snapshot.md`, `comprehensive-demo.md`,
    `comprehensive-demo-full.md`**: Various `id`, `object_id`, `vault_uri`, `primary_access_key`
    attributes added as `(known after apply)`. All are in `after_unknown` in the underlying plan
    fixtures. Notably `azurerm_key_vault_secret` update now shows `id | - | (known after apply)`,
    which is correct because key vault secret updates produce a new version with a new ID.

---

## Issues Found

### Blockers

None.

### Major Issues

None.

### Minor Issues

**1. Test method name mismatch in release notes (documentation inaccuracy)**

- **File:** `docs/issues/575-azuread-group-member-empty-rendering/release-notes.md`, line 61
- **Problem:** The release notes name the new test as
  `AzureAdSnapshotTests.Snapshot_AzureAd_GroupMemberAllUnknown_MatchesBaseline`, but the actual
  method in `AzureAdSnapshotTests.cs` (line 71) is
  `Snapshot_AzureAd_GroupMember_AllAttributesUnknown_MatchesBaseline`. The analysis doc at line
  205 uses the correct name.
- **Impact:** Minor confusion if someone tries to find the test by name from the release notes.

### Suggestions

**1. `(sensitive)` shown in Before column for attributes absent from `before`**

- **File:** `ReportModelBuilder.ResourceChanges.cs`, line 135
- **Context:** For the `primary_access_key` attribute on `azurerm_storage_account.data` (an UPDATE
  action), `primary_access_key` is not present in `before` but IS in `after_sensitive`. The code:
  ```csharp
  var beforeDisplay = isSensitive && !_showSensitive ? "(sensitive)" : beforeValue;
  ```
  evaluates to `"(sensitive)"` because `isSensitive = true` (attribute is in `after_sensitive`),
  even though `beforeValue` is `null` (the attribute was not in the previous state). This produces
  `| primary_access_key | (sensitive) | (known after apply) |` in the Before column, implying
  there was a prior hidden value when there may have been none.
- **Note:** This is **pre-existing behavior** in the sensitivity logic (not introduced by this
  fix). The fix simply adds `primary_access_key` to `allKeys` (previously it was absent), making
  this pre-existing behavior newly visible.
- **Recommendation:** Consider separating sensitivity detection into before-sensitive and
  after-sensitive: `beforeDisplay = (beforeSensitiveDict.ContainsKey(key) && !_showSensitive) ? "(sensitive)" : beforeValue`. This would show `-` in Before when the attribute was not sensitive
  in the old state. **Out of scope for this PR.**

**2. No dedicated unit tests for `BuildAttributeChanges` edge cases**

- The analysis doc suggested unit tests for `BuildAttributeChanges` directly (empty `after` dict,
  all-null plan, mixed case). Coverage is currently provided exclusively via snapshot tests. This
  is consistent with the project's snapshot-first testing philosophy, but a focused unit test
  would make regressions easier to diagnose. **Out of scope for this PR.**

---

## Critical Questions Answered

- **What could make this code fail?** Malformed `after_unknown` values (e.g., a numeric value
  instead of `true`) would cause `isUnknown = false` for that attribute — it would silently not
  render as `(known after apply)`. This is defensive behaviour, not a bug.
- **What edge cases might not be handled?** Nested block unknowns (objects in `after_unknown`)
  produce dotted-path keys in `allKeys` — these render correctly in the flat table as leaf paths.
  No special handling needed.
- **Are all error paths tested?** The sensitivity masking, unchanged-values filtering, and
  large-value detection all interact correctly with the new `isUnknown` flag, as confirmed by the
  snapshot baselines.

---

## Specification Compliance Table

| Category | Status |
|---|---|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing | ✅ |
| Documentation | ✅ (minor release-notes name inaccuracy) |

---

## Work Protocol & Documentation Verification

| Document | Status | Notes |
|---|---|---|
| `work-protocol.md` | ✅ Exists | Issue Analyst, Developer, Technical Writer all logged |
| `docs/features.md` | ✅ Updated | Accurately describes `(known after apply)` rendering |
| `docs/architecture.md` | ✅ No change needed | Bug fix, no architectural change |
| `docs/testing-strategy.md` | ✅ No change needed | Follows existing snapshot pattern |
| `README.md` | ✅ No change needed | No CLI/usage change |
| `CHANGELOG.md` | ✅ Not modified | Correct (auto-generated) |
| Release notes | ✅ Created | Minor test name inaccuracy (Minor issue #1 above) |

**All required agents for a Bug Fix workflow have logged entries** (Issue Analyst ✅, Developer ✅,
Technical Writer ✅). Code Reviewer and Release Manager entries will be added as the workflow
proceeds.

---

## Next Steps

**Approved — ready for Release Manager.**

The release notes minor inaccuracy (test method name) can be fixed opportunistically but is not a
blocker. The suggestion around `(sensitive)` in Before column for new attributes is tracked as a
pre-existing issue for a future fix.
