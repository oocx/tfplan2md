# Code Review: Known-After-Apply Rendering

## Summary

Reviewed the implementation of Feature 102, which surfaces computed (`after_unknown`) Terraform
attributes in rendered reports instead of silently dropping them. The implementation covers all
9 specification scenarios, introduces two new helpers (`AfterUnknownHelper`, `ReferenceSelector`),
updates the model builder and AzureAD provider, adds a template branch for whole-resource unknown,
and adds 3 new test files.

All 1270 tests pass and coverage thresholds are met. The core implementation is **sound and
correct**. However, **three Blocker issues** prevent approval: missing `SNAPSHOT_UPDATE_OK`
commit token, missing required UAT artifact, and uncommitted documentation changes.

---

## Verification Results

- **Tests:** Pass — 1270 passed, 0 failed
- **Coverage:** Line 86.75% (threshold ≥84.48%) ✅ | Branch 78.35% (threshold ≥72.80%) ✅
- **Build:** Passed (dotnet build succeeds; Docker build was started but interrupted after 2m+;
  NativeAOT builds take >5 min on this machine — not conclusive)
- **Errors (workspace):** 3 pre-existing lint warnings in `ScribanHelpersFormatDiffTests.cs`
  (repeated string literals, unrelated to this feature)
- Comprehensive demo markdownlint has 1 pre-existing MD024 error at line 673 (duplicate
  `### 📦 Module: \`module.network\`` heading — confirmed present on `origin/main` as well)

---

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---|---|---|---|
| AzureAD group member with all-unknown IDs shows non-empty summary (Scenario 1) | ✅ | ✅ TC-12 | Correct |
| Static config refs in summary and table (Scenarios 2, 5) | ✅ | ✅ TC-13, TC-16 | Correct; Scenario 5 numeric key appended |
| for_each string key in summary; `each.value.*` refs in table (Scenario 3) | ✅ | ✅ TC-14 | Correct |
| Mixed known/computed renders both correctly (Scenario 4) | ✅ | ✅ TC-15 | Correct |
| Computed attributes shown with `(known after apply)` placeholder | ✅ | ✅ TC-17 | Correct |
| Attributes absent from `after` NOT added even if in `after_unknown` (Decision A1) | ✅ | ✅ TC-18 | Decision A1 upheld |
| `🔒(known after apply)` for sensitive+computed (Scenario 7) | ✅ | ✅ TC-19, TC-20 | Lock icon correct |
| Sensitive+computed counted in update change summary | ✅ | ✅ TC-21 | Correct |
| Whole-resource unknown shows `_(all values known after apply)_` (Scenario 8) | ✅ | ✅ TC-22 | OQ-01 resolved |
| Child resource with computed `ChildReferenceAttribute` renders standalone (Scenario 9) | ✅ | ✅ TC-23 | Correct |
| Computed create attrs NOT in attribute change count | ✅ | ✅ TC-24 | Correct |
| Regression: clean resources unchanged | ✅ | ✅ TC-25 | Correct |
| Reference strings are Terraform paths, never sensitive values | ✅ | ✅ TC-26 | Correct |
| `--show-sensitive` still hides before value for sensitive+computed | ✅ | ✅ TC-20 | Invariant 10 upheld |
| Existing snapshots continue to pass (TC-27) | ✅ | ✅ | All 1270 tests pass |

**Spec Deviations Found:** None.

---

## Adversarial Testing

| Test Case | Result | Notes |
|---|---|---|
| `afterUnknown` is `null` | Pass | TC-04 — returns false, no throw |
| `afterUnknown` is wrong type (string instead of array) | Pass | TC-04 — malformed path returns false |
| `after[attr] = null` but NOT in `after_unknown` | Pass | TC-18 — correctly excluded |
| Bare `each.value` reference (no attribute) | Pass | TC-09 — treated as useless, returns null |
| Numeric instance key with no static reference | Pass | TC-16 — numeric key NOT used alone as label |
| Sensitive attribute remains masked under `--show-sensitive` | Pass | TC-20 — Invariant 10 holds |
| Empty `references` list | Pass | TC-09 — returns null, no throw |
| Module-qualified reference | Pass | TC-11 — `module.identity.azuread_user.admin` returned correctly |

---

## Snapshot Changes

- **Snapshot files changed:** Yes — 3 files
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/known-after-apply-all-scenarios.md`
    (new file: combined snapshot for all 9 scenarios)
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/ephemeral-open.md`
    (updated: `null_resource.app_config` now shows `| id | \`(known after apply)\` |` instead
    of `_No attribute changes._` — correct per feature spec)
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/no-configuration-block.md`
    (updated: `azuread_group_member.platform_admin_member` summary now shows
    `(known after apply)` for computed `group_object_id` — correct, this was the original bug)
- **`SNAPSHOT_UPDATE_OK` token present:** ❌ **No** — none of the 8 commits on this branch
  contain the required token. The closest commit is `9bd51d4c test: update
  known-after-apply snapshots` but it lacks the token.
- **Are the snapshot diffs correct?** Yes — all three changes reflect intentionally correct
  behavior that the feature introduces. The bug being fixed is visible in the
  `no-configuration-block.md` diff (blank summary → `(known after apply)` label).

---

## Issues Found

### Blockers

**B-01 — Missing `SNAPSHOT_UPDATE_OK` commit token** (required by project policy)

Three snapshot files were created/modified on this branch but none of the 8 feature branch
commits contain the `SNAPSHOT_UPDATE_OK` token in their message. Policy requires this token
(with justification) when snapshot files change.

*Fix:* Add a new commit (or amend an existing one) with `SNAPSHOT_UPDATE_OK` in the message
and a brief explanation of why each snapshot diff is correct, per the template in
[`.github/copilot-instructions.md`](../../.github/copilot-instructions.md) § Snapshot updates.

---

**B-02 — Missing required UAT artifact `uat-plan.md`** (Blocker per code review instructions)

The UAT test plan at `docs/features/102-known-after-apply-rendering/uat-test-plan.md` § Artifacts
explicitly states:

> **Rendered Output Path:** `docs/features/102-known-after-apply-rendering/uat-plan.md`
> …Code Reviewer validates both `uat-plan.json` and `uat-plan.md` exist and are non-empty
> before approving.

`uat-plan.json` exists but `uat-plan.md` is absent.

*Fix:* Generate and commit the artifact:
```bash
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  docs/features/102-known-after-apply-rendering/uat-plan.json \
  > docs/features/102-known-after-apply-rendering/uat-plan.md
```

---

**B-03 — Technical Writer's documentation edits are uncommitted**

`docs/features.md`, `README.md`, and `work-protocol.md` have unstaged changes. The Technical
Writer logged their work in the work protocol, but the three files were never committed.

*Fix:* Commit the staged/unstaged changes from the Technical Writer (after fixing B-04 in
`README.md` first):
```bash
git add docs/features.md README.md docs/features/102-known-after-apply-rendering/work-protocol.md
git commit -m "docs: add known-after-apply rendering documentation"
```

---

### Minor Issues

**M-01 — Garbled emoji on two lines in README.md** (uncommitted change)

The unstaged `README.md` diff introduces a Unicode replacement character (`U+FFFD`,
`\xef\xbf\xbd`) on two lines:

1. **Line 55** — "Known-after-apply visibility" bullet: `- U+FFFD **Known-after-apply…**`
   (should be `🔮` or another appropriate emoji)
2. **Line 56** — "Specialized templates" bullet: prepended with `U+FFFD` before `🔧`
   (the original `🔧` emoji is now `U+FFFD🔧`)

Confirmed via raw byte inspection:
```
Line 55: b'- \xef\xbf\xbd **Known-after-apply visibility**…'
Line 56: b'- \xef\xbf\xbd\xf0\x9f\x94\xa7 **Specialized templates**…'
```

*Fix (before committing B-03):* Edit README.md to replace `U+FFFD` with an appropriate emoji
(e.g., `🔮`) on line 55, and remove the extraneous `U+FFFD` from line 56.

---

### Suggestions

**S-01 — `ReportModelBuilder.ResourceChanges.cs` feature reference could be updated**

The file header comment at the top of
[src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs](../../src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs)
still only references features 020 and 014. The new `ApplyComputedKnownAfterApplyOverride`
logic is documented in the method-level XML doc, but the class-level `remarks` doesn't mention
Feature 102. Consider adding it for navigability.

---

## Critical Questions Answered

- **What could make this code fail?** If `AfterUnknown` is a type not handled by
  `TryGetJsonElement` (e.g., a non-`JsonElement` object), the helper silently returns false.
  This is safe behavior but could silently miss computed attributes on unusual plan shapes.
  Tests cover the documented Terraform plan shapes; the defensive `false` fallback is correct.
- **What edge cases might not be handled?** `after_unknown` as `false` (boolean false) —
  tested via TC-02 (object), but false-boolean: the `IsWholeResourceUnknownAfterApply` check
  returns false (correct). Module-qualified child addresses (e.g.,
  `module.network.azurerm_subnet.app`) for parent-child matching with computed
  `ChildReferenceAttribute` — not explicitly tested, though TC-23 covers the non-module case
  and existing module snapshot tests continue to pass.
- **Are all error paths tested?** Yes — TC-03 and TC-04 cover null, malformed, and missing-key
  paths. The never-throw contract is upheld.

---

## Checklist Summary

| Category | Status |
|---|---|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing | ✅ |
| Documentation | ❌ (uncommitted edits; garbled emoji; missing UAT artifact) |
| Snapshot token | ❌ (SNAPSHOT_UPDATE_OK missing) |

---

## Work Protocol & Documentation Verification

| Check | Status | Notes |
|---|---|---|
| `work-protocol.md` exists | ✅ | Present |
| All required agents logged | ✅ | Requirements Engineer, Architect (×2), Quality Engineer, Task Planner, Developer (×3), Technical Writer |
| `docs/features.md` updated | ⚠️ | Written but **uncommitted** (B-03) |
| `README.md` updated | ⚠️ | Written but **uncommitted**, and has garbled emoji (M-01, B-03) |
| `docs/architecture.md` updated | N/A | Feature-local architecture doc was created; global doc not required for helper additions |
| `docs/testing-strategy.md` | N/A | No new test framework or pattern introduced |
| `docs/agents.md` | N/A | No workflow changes |

---

## Review Decision

**Status: Changes Requested**

The implementation is correct and well-tested. Return to Developer to resolve the three
blockers and one minor issue, then re-submit for re-review.

---

## Next Steps

1. Fix README.md garbled emoji (M-01)
2. Commit Technical Writer's documentation changes (B-03)
3. Generate and commit `uat-plan.md` (B-02)
4. Add a commit with `SNAPSHOT_UPDATE_OK` token explaining the snapshot diffs (B-01)
5. Re-submit for code review re-pass

After re-approval, this feature requires **UAT** (user-facing markdown rendering change),
so the next handoff will be to the **UAT Tester**.
