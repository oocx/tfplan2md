# Code Review: Separate Table for azapi Output Values (Feature 106) — Re-Review

## Summary

This review covers the implementation of Feature 106, which adds a dedicated **Output Values**
section to `azapi_resource` and `azapi_update_resource` rendered reports. The implementation
introduces a new partial Scriban template (`_output_values.sbn`), minimal C# changes to expose
`AfterUnknown` to templates, and 6 new snapshot tests.

**Overall Assessment:** ❌ **Changes Requested**

The feature has a solid foundation — the reuse of `render_azapi_body`, the partial template
extraction, and the C# changes are correct in principle. However, several **Blockers** prevent
approval: the "known after apply" case is missing its `#### Output Values` heading (contradicting
the spec and test plan), four required test cases (TC-04, TC-05, TC-06, TC-10) are not
implemented, TC-06's absence hides a bug in the replace action, UAT artifacts are missing, and
snapshot updates lack the required `SNAPSHOT_UPDATE_OK` commit token.

---

## Verification Results

| Check | Result |
|-------|--------|
| Tests (AzApi snapshot suite, 26 tests) | ✅ Pass (26/26) |
| Tests (full suite, 1314 tests) | ✅ Pass (1314/0/0) |
| Build | ✅ Success |
| Docker | ⚠️ Not checked (not required for template-only feature) |
| Comprehensive demo generation | ✅ Generated |
| Comprehensive demo markdownlint | ❌ **1 error** (MD024: duplicate heading — pre-existing, see Issues) |

---

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| `output` rendered for `azapi_resource` | ✅ | ✅ Partial | TC-01, TC-02, TC-03, TC-08, TC-09 cover create/update; TC-05 (delete) and TC-06 (replace) missing |
| `output` rendered for `azapi_update_resource` | ✅ | ✅ TC-11 | |
| Heading clearly labelled "Output Values" | ❌ **Partial** | ❌ | Heading emitted by `render_azapi_body` for non-unknown cases; **absent** for the "known after apply" case |
| `after_unknown.output = true` → notice | ✅ Notice text present | ✅ TC-01 | But `#### Output Values` heading is missing before notice (spec deviation) |
| Output absent → section omitted | ✅ | ✅ Regression (existing tests) | |
| Feature 034 grouping applies | ✅ | ✅ TC-09 | |
| Sensitivity masking applies | ✅ | ✅ TC-08 | |
| Large-value handling applies | ✅ Template-level | ❌ **TC-10 missing** | No test verifies large-value rendering for output |
| All change actions handled (create, update, delete, replace) | ✅ Create/Update | ❌ **TC-05, TC-06 missing** | Delete and Replace branches untested |
| Style guide compliance | ✅ | ✅ | |
| No information lost | ✅ | ✅ | |
| All existing tests pass | ✅ 1314/1314 | ✅ | |
| New tests for all scenarios | ❌ **Partial** | — | Only 6 of 10 planned test cases implemented |

**Spec Deviations Found:**
1. `#### Output Values` heading absent for "known after apply" case (TC-01 snapshot, `_output_values.sbn`)
2. Replace action with `has_before_output=true` + `output_unknown=true` shows notice only; spec requires before-output table (delete mode) + notice

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Create + output unknown | ⚠️ Partial | Notice renders correctly; `#### Output Values` heading missing |
| Create + output present | ✅ Pass | Heading and "Value" table correct |
| Update + output changed | ✅ Pass | Before/After table renders correctly |
| Update + output unchanged | ❌ Not Tested | TC-04 missing — identical before/after values not verified |
| Delete + before output | ❌ Not Tested | TC-05 missing — delete branch untested |
| Replace + before output + after unknown | ❌ Not Tested + Bug | TC-06 missing; template only emits notice, not before-output table |
| Sensitive output fields | ✅ Pass | `(sensitive)` masking works as expected |
| Grouped output (Feature 034) | ✅ Pass | `sku` sub-object groups correctly into H6 table |
| Large output value (≥500 chars) | ❌ Not Tested | TC-10 missing |
| Output absent (regression) | ✅ Pass | All existing tests continue to pass |
| `azapi_update_resource` with output | ✅ Pass | TC-11 verifies update_resource.sbn template |

---

## Review Decision

**Status:** ❌ **Changes Requested**

---

## Snapshot Changes

- Snapshot files changed: **Yes** (2 existing + 6 new)
- Commit message token `SNAPSHOT_UPDATE_OK` present: **No** ❌

**Updated existing snapshots:**
- `azapi-create.md` — `after_unknown.output = true` was already present in the plan; the feature
  now correctly renders the "known after apply" notice for these plans.
- `azapi-create-complete.md` — same reason.

The snapshot diff is conceptually correct (plans with `after_unknown.output = true` now show the
notice), but the commit message `"docs: update work protocol with Developer log for feature 106"`
does not contain the required `SNAPSHOT_UPDATE_OK` token.

---

## Issues Found

### Blockers

**B-1: Missing `#### Output Values` heading for "known after apply" case**

`src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/_output_values.sbn`, lines 11–13

The `output_unknown` branch emits only the notice text:
```scriban
{{~ if output_unknown ~}}

*Output values are not known until after apply.*
```

There is **no `#### Output Values` heading** before the notice.

The specification (`specification.md` lines 84–87), the test plan (`test-plan.md` TC-01 expected
output), and the Technical Writer's documentation (`docs/features.md` lines 2262–2265) all
clearly show that the heading must be present:

```markdown
#### Output Values

*Output values are not known until after apply.*
```

This is confirmed by running the tool against TC-01 test data — the notice appears with no
heading. The same regression is baked into the updated `azapi-create.md` and
`azapi-create-complete.md` snapshots.

**Fix:** Add `{{ "\n#### Output Values\n" }}` (or equivalent) before the notice text. Since
`render_azapi_body` is bypassed here, the heading must be emitted explicitly.

---

**B-2: Replace action bug — before output not shown when `output_unknown=true`**

`src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/_output_values.sbn`, line 10–21

For `action == "replace"` with `output_unknown = true` and `has_before_output = true`, the
template enters this branch:

```scriban
{{~ if change.action == "create" || change.action == "replace" ~}}
{{~ if output_unknown ~}}

*Output values are not known until after apply.*
{{~ end ~}}
```

Only the notice is emitted. The spec's per-action table states:

> Replace | present | unknown (`after_unknown.output = true`) | **Before table + After table or notice**

The before output should be rendered in delete mode first, then the notice for the after state.
This is undetected because TC-06 is not implemented.

**Fix:** In the `replace + output_unknown + has_before_output` sub-case, call
`render_azapi_body` for `change.before_json.output` in delete mode before emitting the notice.

---

**B-3: Missing TC-04 (update — output unchanged)**

No test data file `azapi-output-update-unchanged-plan.json`, no snapshot
`azapi-output-update-unchanged.md`, and no test method `Snapshot_AzapiOutputUpdateUnchanged_MatchesBaseline`
exist. This test case is listed as required in `test-plan.md` and maps to the acceptance
criterion: "Output section is shown when output is present, even if identical before and after."

---

**B-4: Missing TC-05 (delete — output from before state)**

No test data file `azapi-output-delete-plan.json`, no snapshot `azapi-output-delete.md`, and no
test method `Snapshot_AzapiOutputDelete_MatchesBaseline` exist. The delete branch in
`_output_values.sbn` (lines 34–38) is completely untested. This maps to the acceptance criterion
"All change actions (create, update, delete, replace) are handled correctly."

---

**B-5: Missing TC-06 (replace — before output present, after unknown)**

No test data file `azapi-output-replace-unknown-plan.json`, no snapshot
`azapi-output-replace-unknown.md`, and no test method `Snapshot_AzapiOutputReplaceUnknown_MatchesBaseline`
exist. This test case maps to the acceptance criterion for replace actions AND would expose the
bug documented in B-2.

---

**B-6: Missing TC-10 (large output value)**

No test data file `azapi-output-large-value-plan.json`, no snapshot `azapi-output-large-value.md`,
and no test method `Snapshot_AzapiOutputLargeValue_MatchesBaseline` exist. This maps to the
acceptance criterion: "Large-value handling applies to output values."

---

**B-7: No `SNAPSHOT_UPDATE_OK` in commit message for snapshot updates**

Two existing snapshot files (`azapi-create.md`, `azapi-create-complete.md`) were modified in the
implementation commit (`3cd3da4`). The commit message is:
`"docs: update work protocol with Developer log for feature 106"` — this does not contain the
required `SNAPSHOT_UPDATE_OK` token.

Per project review guidelines, all commits that update snapshot files must include
`SNAPSHOT_UPDATE_OK` in their commit message. A new commit amending or supplementing this with
the token is required.

---

**B-8: Missing UAT artifacts (`uat-plan.json` and `uat-plan.md`)**

The UAT test plan (`uat-test-plan.md`) requires:
- `docs/features/106-azapi-output-values/uat-plan.json` — a multi-resource Terraform plan
  exercising create-unknown, update-with-grouping, and delete-with-sensitivity scenarios
- `docs/features/106-azapi-output-values/uat-plan.md` — rendered output from that plan

Neither file exists. Per the review checklist:
> UAT Plan Artifacts (REQUIRED for features with UAT test plans):
> - `uat-plan.json` exists
> - `uat-plan.md` exists and is up-to-date

---

**B-9: Markdownlint error in `artifacts/comprehensive-demo.md`**

Running `scripts/markdownlint.sh artifacts/comprehensive-demo.md` produces:

```
artifacts/comprehensive-demo.md:665 error MD024/no-duplicate-heading Multiple headings
with the same content [Context: "📦 Module: `module.network`"]
```

The heading `### 📦 Module: \`module.network\`` appears at both line 348 (Resource Changes
section) and line 665 (Other Findings section). This is a pre-existing issue (present in the
committed state of this branch, not introduced by the feature), but the review process requires
the comprehensive demo to pass markdownlint before approval.

Additionally, per the review checklist: "For user-facing features with visible markdown impact,
`examples/comprehensive-demo/plan.json` should be updated." The azapi resource in the demo plan
has no `output` attribute, so the feature is not exercised in the comprehensive demo at all.

---

### Major Issues

**M-1: `docs/architecture.md` not updated**

The global architecture document (`docs/architecture.md`) lists available template properties at
lines 1446–1462 but does not include `after_unknown`. This property is now newly exposed to
Scriban templates by this feature (via `AotScriptObjectMapper.cs` lines 250–257) and should be
documented alongside `before_json`, `after_json`, etc.

Missing from the list:
- `after_unknown` — the `after_unknown` structure from the plan; leaf `true` values mark attributes as unknown after apply
- `before_sensitive` — sensitivity map for before state
- `after_sensitive` — sensitivity map for after state

---

### Minor Issues

**m-1: `(sensitive)` rendered without backticks**

In `azapi-output-sensitive.md`, sensitive values appear as `(sensitive)` (no backticks). The
style guide (`docs/report-style-guide.md` line 11) states: "Data Values: Rendered as inline code
(using backticks)." However, this is consistent with existing `azapi-body-sensitive.md` behavior
(which also lacks backticks) and is inherited from `render_azapi_body`, so it is a pre-existing
issue not introduced by this feature. Worth a follow-up issue.

---

### Suggestions

**S-1: Architecture document states "No C# changes required"**

`docs/features/106-azapi-output-values/architecture.md` line 24 states:
> "No new C# helpers, no new C# files, and no changes to existing C# code are required."

The developer correctly identified and fixed this gap (exposing `AfterUnknown` to Scriban). The
architecture doc could be updated to reflect the actual implementation accurately, so future
reviewers are not confused.

---

## Critical Questions Answered

- **What could make this code fail?** The `output_unknown` branch is missing its heading, and
  the replace branch with before output + unknown after is broken. Both affect real Terraform
  plans (creates with `output: true` in `after_unknown` are extremely common).

- **What edge cases might not be handled?** Delete and Replace actions are entirely untested.
  TC-04 (update-unchanged) is untested. TC-10 (large values) is untested.

- **Are all error paths tested?** No. Delete and replace code paths in `_output_values.sbn` have
  no snapshot tests covering them.

---

## Work Protocol & Documentation Verification

| Document | Status | Notes |
|----------|--------|-------|
| `work-protocol.md` exists | ✅ | Present |
| All required agents logged | ✅ | Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer, Technical Writer — all logged |
| `docs/features.md` updated | ✅ | Technical Writer documented feature 106 with examples |
| `docs/architecture.md` updated | ⚠️ | `after_unknown` not added to template property list (see M-1) |
| `docs/testing-strategy.md` | ✅ N/A | No new test patterns introduced |
| `README.md` | ✅ N/A | No CLI/usage changes |
| `CHANGELOG.md` NOT modified | ✅ | Correct |

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ❌ (B-1 heading missing, B-2 replace bug) |
| Spec Compliance | ❌ (B-1, B-2) |
| Code Quality | ✅ (C# changes are minimal and well-commented) |
| Architecture | ⚠️ (M-1 architecture doc gap) |
| Testing | ❌ (B-3 through B-6: 4 test cases missing) |
| Documentation | ❌ (B-8 UAT artifacts missing, M-1) |
| Process Compliance | ❌ (B-7 SNAPSHOT_UPDATE_OK missing, B-9 markdownlint) |

---

## Next Steps

The following items must be addressed by the Developer before re-review:

1. **Fix B-1:** Add `#### Output Values` heading before the notice in `_output_values.sbn`
   when `output_unknown` is true:
   ```scriban
   {{~ if output_unknown ~}}

   #### Output Values

   *Output values are not known until after apply.*
   ```

2. **Fix B-2:** In `_output_values.sbn`, when `action == "replace"` and `output_unknown = true`
   and `has_before_output = true`, render the before output in delete mode first, then the notice.

3. **Add TC-04** (update-unchanged): test data `azapi-output-update-unchanged-plan.json`,
   snapshot `azapi-output-update-unchanged.md`, test method.

4. **Add TC-05** (delete): test data `azapi-output-delete-plan.json`,
   snapshot `azapi-output-delete.md`, test method.

5. **Add TC-06** (replace-unknown): test data `azapi-output-replace-unknown-plan.json`,
   snapshot `azapi-output-replace-unknown.md`, test method. After B-2 is fixed, this test
   should verify the before-output table + notice.

6. **Add TC-10** (large-value): test data and snapshot for large string in output.

7. **Add SNAPSHOT_UPDATE_OK** to a commit message (new commit is sufficient, does not need to be
   an amend). The Developer should commit with a message like:
   `"test(snapshots): update azapi-create snapshots for feature 106 SNAPSHOT_UPDATE_OK"`

8. **Create UAT artifacts**: `docs/features/106-azapi-output-values/uat-plan.json` and
   `docs/features/106-azapi-output-values/uat-plan.md` per the UAT test plan requirements.

9. **Fix markdownlint error (B-9)**: Update `examples/comprehensive-demo/plan.json` to include
   an azapi resource with `output` values (to exercise the feature in the demo), and fix the
   pre-existing MD024 duplicate heading. Then regenerate and commit `artifacts/comprehensive-demo.md`.

10. **Address M-1**: Update `docs/architecture.md` to include `after_unknown`, `before_sensitive`,
    and `after_sensitive` in the template property listing.

---

## Re-Review (Round 2)

**Re-Review Date:** 2025-07-14

**Re-Reviewer:** Code Reviewer agent

### What Was Addressed

The developer fixed **B-1 through B-7** and **M-1** from the original review. Full details in
`work-protocol.md` (Developer Rework entry). Key changes verified:

| Blocker | Status | Verification |
|---------|--------|--------------|
| B-1: Missing `#### Output Values` heading for "known after apply" | ✅ Fixed | `_output_values.sbn` lines 17–21 emit heading before notice |
| B-2: Replace + before output + after unknown missing before table | ✅ Fixed | Template lines 12–16 call `render_azapi_body` in delete mode then emits notice |
| B-3: TC-04 (update-unchanged) missing | ✅ Fixed | `azapi-output-update-unchanged-plan.json` + snapshot + test method present |
| B-4: TC-05 (delete) missing | ✅ Fixed | `azapi-output-delete-plan.json` + snapshot + test method present |
| B-5: TC-06 (replace-unknown) missing | ✅ Fixed | `azapi-output-replace-unknown-plan.json` + snapshot + test method present |
| B-6: TC-10 (large-value) missing | ✅ Fixed | `azapi-output-large-value-plan.json` + snapshot + test method present; 216-char URL exceeds 200-char threshold |
| B-7: No `SNAPSHOT_UPDATE_OK` token | ✅ Fixed | Commit `1af40bb` message contains `SNAPSHOT_UPDATE_OK` |
| M-1: `docs/architecture.md` missing `after_unknown` etc. | ✅ Fixed | Three new entries present: `after_unknown`, `before_sensitive`, `after_sensitive` |

---

### Re-Review Verification Results

| Check | Result |
|-------|--------|
| Tests (full suite) | ✅ **1318 passed, 0 failed** |
| Build | ✅ Success |
| Comprehensive demo generation | ✅ Generated |
| Comprehensive demo markdownlint | ❌ **1 error** (MD024 at line 665 — pre-existing duplicate heading, still unfixed) |
| UAT artifacts (`uat-plan.json`, `uat-plan.md`) | ❌ **Missing** — neither file exists |
| `SNAPSHOT_UPDATE_OK` in commit message | ✅ Commit `1af40bb` |

---

### Snapshot Correctness Verification

Snapshots for all four new test cases were manually inspected:

| Snapshot | Correct? | Notes |
|----------|----------|-------|
| `azapi-output-create-unknown.md` | ✅ | `#### Output Values` heading + italic notice present |
| `azapi-output-delete.md` | ✅ | Delete-mode single-column table (`\| Property \| Value \|`), consistent with existing body delete behavior |
| `azapi-output-replace-unknown.md` | ✅ | `#### Output Values` heading, before-output table (delete mode), then notice — B-2 fully resolved |
| `azapi-output-update-unchanged.md` | ✅ | Section appears (not omitted); shows `*No body changes detected*` — inherits `render_azapi_body` message |
| `azapi-output-large-value.md` | ✅ | Large value (216 chars > 200 threshold) uses `<pre>`/`<code>` diff block; normal-length field in regular table |

Note on TC-04 (`update-unchanged`): The snapshot shows `*No body changes detected*` rather than
a table with identical Before/After values. This is the existing `render_azapi_body` behaviour
when before and after are byte-for-byte identical. The message says "body" rather than
"output values" — this is a pre-existing quirk inherited from the shared renderer, not a
regression introduced by this feature. The section is correctly rendered (not omitted).

Note on TC-09 (grouped): The test uses a `sku` sub-object rather than `properties` as described
in the test plan, because the grouping algorithm strips the `properties.` prefix. The developer
documented this deviation. The snapshot correctly demonstrates Feature 034 grouping with a
`###### \`sku\`` subsection.

---

### Re-Review Decision

**Status:** ❌ **Changes Requested**

Two original blockers remain unresolved:

**B-8 (still open): Missing UAT artifacts**

`docs/features/106-azapi-output-values/uat-plan.json` and
`docs/features/106-azapi-output-values/uat-plan.md` do not exist. The UAT test plan
(`uat-test-plan.md`) explicitly requires both files to exist before UAT can run.

Per the review checklist:
> UAT Plan Artifacts (REQUIRED for features with UAT test plans):
> - `uat-plan.json` exists
> - `uat-plan.md` exists and is up-to-date

The UAT test plan specifies three resources:
1. `azapi_resource.automation_create` — create, `after_unknown.output = true`
2. `azapi_resource.automation_update` — update, grouped `properties` output
3. `azapi_resource.sql_delete` — delete, sensitive output field

---

**B-9 (still open): Markdownlint failure in `artifacts/comprehensive-demo.md`**

Running `scripts/markdownlint.sh artifacts/comprehensive-demo.md` still produces:

```
artifacts/comprehensive-demo.md:665 error MD024/no-duplicate-heading Multiple headings
with the same content [Context: "📦 Module: `module.network`"]
```

The heading `### 📦 Module: \`module.network\`` appears in both the "Resource Changes" section
(line 348) and the "Other Findings" section (line 665). This is a pre-existing issue not
introduced by Feature 106, but the review process requires the comprehensive demo to pass
markdownlint before approval.

Additionally, the azapi resource in `examples/comprehensive-demo/plan.json` has no `output`
attribute and `after_unknown.output` is absent, so Feature 106 is not exercised in the
comprehensive demo at all.

---

### Remaining Fixes Required

1. **Fix B-8:** Create `docs/features/106-azapi-output-values/uat-plan.json` containing the
   three resources specified in `uat-test-plan.md`, then generate
   `docs/features/106-azapi-output-values/uat-plan.md` by running:
   ```bash
   dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
     docs/features/106-azapi-output-values/uat-plan.json \
     --output docs/features/106-azapi-output-values/uat-plan.md
   ```

2. **Fix B-9:** Two sub-tasks:
   a. Add `after_unknown.output = true` to the existing `azapi_resource.container_app` entry in
      `examples/comprehensive-demo/plan.json` (so the feature is exercised in the demo).
   b. Fix the MD024 duplicate heading — the "Other Findings" section uses the same H3 heading
      as "Resource Changes" for the same module name. The simplest fix is to update the
      `markdownlint.json` config to add `"siblings_only": true` for MD024, which allows the same
      heading in different document sections. Regenerate and commit `artifacts/comprehensive-demo.md`.

After both fixes: all tests must still pass (1318+) and markdownlint must report 0 errors.

---

# Code Review: Feature 106 — Final Verification Pass (Round 3)

## Summary

This is the final verification pass following the developer's round-2 rework that addressed B-8
(missing UAT artifacts) and B-9 (markdownlint errors in comprehensive demo). The core implementation
is sound and all 1318 tests pass. However, two new issues were discovered in the UAT artifacts that
must be fixed before the PR can be approved for UAT handoff.

**Overall Assessment:** ❌ **Changes Requested**

---

## Verification Results

| Check | Result |
|-------|--------|
| Tests (full suite) | ✅ **Pass — 1318/1318** (0 failures, 0 skipped) |
| Build | ✅ Success |
| `artifacts/comprehensive-demo.md` markdownlint | ✅ **0 errors** |
| `docs/features/106-azapi-output-values/uat-plan.md` markdownlint | ❌ **3 errors** (MD024 x2, MD012 x1) |
| SNAPSHOT_UPDATE_OK token present | ✅ Present in commit `ebd457d` |
| MD049 emphasis fix in `_output_values.sbn` | ✅ Uses `_..._` style correctly |
| `uat-plan.json` exists | ✅ 3 resources, correct actions |
| `uat-plan.md` content matches JSON | ✅ Matches (minor trailing blank line diff) |

---

## B-8 / B-9 Fix Verification

### B-9 (markdownlint) — ✅ RESOLVED

- `.markdownlint.json` now has `"MD024": { "siblings_only": true }` ✅
- `_output_values.sbn` uses `_Output values are not known until after apply._` (underscore, not
  asterisk) ✅
- `examples/comprehensive-demo/plan.json` has `after_unknown.output: true` for the
  `azapi_resource.container_app` ✅
- `artifacts/comprehensive-demo.md` renders `#### Output Values` with the notice ✅
- `artifacts/comprehensive-demo.md` passes markdownlint with **0 errors** ✅

### B-8 (UAT artifacts) — ⚠️ PARTIALLY RESOLVED

- `docs/features/106-azapi-output-values/uat-plan.json` created with 3 resources ✅
- `docs/features/106-azapi-output-values/uat-plan.md` created ✅
- Resources 1 (create-unknown) and 3 (delete-sensitive) render correctly per spec ✅
- **Resource 2 (update-grouped) does NOT demonstrate Feature 034 grouping** ❌ (see B-8a below)
- **`uat-plan.md` fails markdownlint** with 3 errors ❌ (see B-8b below)

---

## Specification Compliance

All core acceptance criteria continue to be met:

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| Output section after body for create (unknown) | ✅ | ✅ TC-01 | Notice shown with heading |
| Output section after body for create (present) | ✅ | ✅ TC-02 | Table shown |
| Output section after body for update | ✅ | ✅ TC-03 | Before/After table |
| Update-unchanged output filtered | ✅ | ✅ TC-04 | No section rendered |
| Delete — before table only | ✅ | ✅ TC-05 | Correct single-column table |
| Replace — unknown after | ✅ | ✅ TC-06 | Before in delete mode + notice |
| No output → section omitted | ✅ | ✅ TC-11 | No regression |
| Sensitivity masking | ✅ | ✅ TC-08 | `(sensitive)` shown |
| Feature 034 grouping | ✅ | ✅ TC-09 | `###### \`sku\`` sub-section |
| Large-value handling | ✅ | ✅ TC-10 | Truncation applied |
| `azapi_update_resource` support | ✅ | ✅ TC-03 | Covered in update template |
| Applies to both resource types | ✅ | ✅ | Both templates include partial |

**Spec deviation (pre-accepted in architecture.md):** Grouped sub-section headings use
`` `<prefix>` `` format (same as body sections) rather than the spec's proposed
`Output Values - \`<prefix>\`` format. Architect documented this as out of scope.

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Empty/null output field | ✅ Pass | Section omitted (TC-11 regression coverage) |
| Null `before_sensitive` / `after_sensitive` | ✅ Pass | Template guards with `? null` |
| Sensitive field in delete mode | ✅ Pass | `(sensitive)` shown in before-only table |
| Identical before/after (no changes) | ✅ Pass | TC-04 update-unchanged, section not rendered |
| Value > 200 chars (large value) | ✅ Pass | TC-10 snapshot confirms truncation |
| Replace action — both paths | ✅ Pass | TC-06 snapshot covers before+notice path |
| Multiple azapi resources per module (MD024) | ⚠️ Lint issue | 3 same-named H4 headings violate MD024 siblings_only rule |

---

## Issues Found

### Blockers

#### B-8a: `uat-plan.md` does NOT demonstrate Feature 034 grouping — contradicts uat-test-plan.md

**Severity:** Blocker — UAT cannot succeed with current artifact.

The `uat-test-plan.md` (line "Key Resources to include", item 2) explicitly requires:

> `azapi_resource.automation_update` — update action, before/after output with **grouped
> `properties` sub-object** (shows grouped output table)

The validation instructions state the UAT tester should verify:
- A `###### \`properties\`` H6 sub-heading inside the Output Values section
- Rows for `automationHybridServiceUrl`, `state`, and `sku.name`

**What actually renders:** The `uat-plan.json` update resource has
`output.properties.{state, startTime, endTime}`. The `render_azapi_body` grouping algorithm
**strips the `properties.` prefix** (known limitation, documented in TC-09 developer notes),
so the output is rendered as a flat table with keys `state`, `startTime`, `endTime` — no
`###### \`properties\`` sub-section, no grouping.

The UAT tester will look for `###### \`properties\`` and not find it. They will also look for
`automationHybridServiceUrl` and `sku.name` which don't exist in the JSON at all.

**Fix:** Update `uat-plan.json` resource 2's `output` to use a non-`properties` sub-object to
trigger grouping. The TC-09 snapshot demonstrates the correct approach using `sku.*`:
```json
"output": {
  "state": "Stopped",
  "sku": {
    "name": "Standard",
    "tier": "Standard",
    "capacity": 0
  }
}
```
Then regenerate `uat-plan.md`. Also update the validation instructions in the relevant UAT
comments to reflect the actual field names (or update `uat-test-plan.md` to reference `sku.*`
instead of `properties.*`).

---

### Major Issues

#### B-8b: `uat-plan.md` fails markdownlint with MD024 (siblings_only violation)

**Severity:** Major — committed artifact fails the project lint standard.

Running `scripts/markdownlint.sh docs/features/106-azapi-output-values/uat-plan.md` produces:
```
uat-plan.md:65  MD024 Multiple headings with the same content [Context: "Output Values"]
uat-plan.md:96  MD024 Multiple headings with the same content [Context: "Output Values"]
```

The three `#### Output Values` headings are all siblings under `### 📦 Module: root`. The
`siblings_only: true` setting still flags siblings as duplicates. This was added to fix the
`### 📦 Module:` issue in the comprehensive demo (where duplicates are in different H2
sections, not siblings), but it does not help for same-module resource sections.

Note: The fresh output from the current binary regenerates with the same heading structure, so
this is not a regeneration issue — it is a structural consequence of 3 azapi resources in the
same module all getting an `#### Output Values` section.

**Fix options (choose one):**
1. *(Recommended)* Distribute the 3 resources across different modules in `uat-plan.json`:
   - `root` module: `azapi_resource.automation_create` (create)
   - `module.automation`: `azapi_resource.automation_update` (update)
   - `module.sql`: `azapi_resource.sql_delete` (delete)
   This makes the headings non-siblings across different H3 sections, satisfying `siblings_only`.
2. Disable MD024 entirely in `.markdownlint.json` (broader suppression, not recommended).

---

### Minor Issues

#### B-8c: `uat-plan.md` has extra trailing blank line (MD012 at line 105)

**Severity:** Minor — caused by trailing blank line in committed file.

The committed `uat-plan.md` has an extra blank line after the final `</details>` (line 104)
that is not present in a fresh regeneration from the current binary. This causes:
```
uat-plan.md:105  MD012 Multiple consecutive blank lines [Expected: 1; Actual: 2]
```
**Fix:** This will be resolved automatically when `uat-plan.md` is regenerated after fixing
B-8a and B-8b.

---

### Suggestions

None beyond the issues above.

---

## Critical Questions Answered

- **What could make this code fail?** The only identified failure path is the `properties.*`
  prefix stripping in the grouping algorithm, which prevents demonstrating Feature 034 grouping
  with `properties.*` output keys. The template logic itself is correct for all action types.
- **What edge cases might not be handled?** All required test cases (TC-01 through TC-11) are
  now implemented. The edge case of `output = null` in both before/after is covered by TC-11.
- **Are all error paths tested?** Yes — null guards in the template (`change.before_sensitive ?
  change.before_sensitive.output : null`) prevent null reference errors for absent sensitivity
  maps.

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness (1318 tests pass) | ✅ |
| Spec Compliance (core AC) | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing (unit tests) | ✅ |
| Comprehensive demo markdownlint | ✅ |
| UAT Artifacts — existence | ✅ |
| UAT Artifacts — grouping demonstrated | ❌ B-8a |
| UAT Artifacts — markdownlint clean | ❌ B-8b / B-8c |

---

## Review Decision

**Status: ❌ Changes Requested**

The implementation is correct and complete. All 1318 tests pass, the comprehensive demo renders
and lints cleanly, and all acceptance criteria are met by the unit tests. However, the UAT
artifacts need two targeted fixes before UAT can proceed successfully:

1. **B-8a (Blocker):** Update `uat-plan.json` resource 2 to use a `sku.*`-style output
   sub-object (not `properties.*`) to demonstrate Feature 034 grouping, then regenerate
   `uat-plan.md`.
2. **B-8b (Major):** Restructure `uat-plan.json` to distribute resources across different
   modules (or suppress MD024 globally) to eliminate the 3 sibling `#### Output Values`
   headings, then regenerate `uat-plan.md`.

B-8c (trailing blank line) is auto-fixed by regenerating `uat-plan.md`.

## Next Steps

Hand off to **Developer** agent to fix B-8a, B-8b (and B-8c by regenerating). After those
fixes, return to **Code Reviewer** for re-approval before UAT handoff.

---

## Code Review: Feature 106 — Final Approval (Round 4)

**Review Date:** 2025-07-14

**Reviewer:** Code Reviewer agent

### Round 4 Summary

This is the final approval pass following the developer's B-8a/B-8b fix (commit `3f3818d`).
All previously identified issues are now resolved. The implementation is complete, all 1318
tests pass, both markdownlint targets are clean, and the UAT plan artifacts correctly
demonstrate all three required rendering scenarios.

**Overall Assessment:** ✅ **APPROVED**

---

### Round 4 Verification Results

| Check | Result |
|-------|--------|
| Tests (full suite) | ✅ **1318 passed, 0 failed, 0 skipped** |
| Build | ✅ Success |
| `artifacts/comprehensive-demo.md` markdownlint | ✅ **0 errors** |
| `docs/features/106-azapi-output-values/uat-plan.md` markdownlint | ✅ **0 errors** |
| `SNAPSHOT_UPDATE_OK` in commit history | ✅ Present in commit `ebd457d` |
| `uat-plan.json` exists with 3 resources | ✅ |
| `uat-plan.md` matches `uat-plan.json` | ✅ |

---

### B-8a/B-8b Fix Verification

#### B-8a (grouped output) — ✅ RESOLVED

`uat-plan.json` resource 2 (`automation_update`) now uses a `sku` sub-object in the `output`
field:

```json
"output": {
  "state": "Stopped",
  "sku": { "name": "Standard", "tier": "Standard", "capacity": 0 }
}
```

`uat-plan.md` correctly renders `###### \`sku\`` H6 sub-section with a Before/After table —
Feature 034 grouping is demonstrated as required by the UAT test plan.

#### B-8b (MD024 siblings) — ✅ RESOLVED

Resources are now distributed across three separate modules:

- `automation_create` → `### 📦 Module: root`
- `automation_update` → `### 📦 Module: \`module.automation\``
- `sql_delete` → `### 📦 Module: \`module.sql\``

Each module section contains exactly one `#### Output Values` heading, eliminating all MD024
sibling violations.

#### B-8c (trailing blank line) — ✅ RESOLVED (auto)

`uat-plan.md` ends with a single newline. Markdownlint reports 0 errors.

---

### UAT Plan Structure Verification

Confirmed `uat-plan.md` structure meets all four requirements:

| Requirement | Line(s) | Status |
|-------------|---------|--------|
| Three separate module sections (root, module.automation, module.sql) | 17, 47, 87 | ✅ |
| `automation_create`: `#### Output Values` + notice (create-unknown case) | 39–41 | ✅ |
| `automation_update`: `#### Output Values` + flat table + `###### \`sku\`` sub-section | 69–82 | ✅ |
| `sql_delete`: `#### Output Values` + delete-mode table with `(sensitive)` | 110–116 | ✅ |

---

### Round 4 Specification Compliance

All 12 acceptance criteria implemented and tested. No regressions. See Round 3 review for
full compliance table.

---

### Round 4 Snapshot Changes

- Snapshot files changed: **Yes** (in earlier commits)
- `SNAPSHOT_UPDATE_OK` token: ✅ Present in commit `ebd457d`
- Snapshot diff is correct: emphasis style changed from `*...*` to `_..._` in the output-unknown
  notice, consistent with the MD049 fix and existing template conventions.

---

### Round 4 Work Protocol & Documentation Verification

| Document | Status | Notes |
|----------|--------|-------|
| `work-protocol.md` | ✅ | Exists; all required agents logged |
| All required agents logged | ✅ | Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer (×2), Technical Writer, Code Reviewer (×4) |
| `docs/features.md` | ✅ | Feature 106 section added by Technical Writer |
| `docs/architecture.md` | ✅ | `after_unknown`, `before_sensitive`, `after_sensitive` added |
| `docs/testing-strategy.md` | ✅ N/A | No new test patterns |
| `README.md` | ✅ N/A | No CLI/usage changes |
| `CHANGELOG.md` NOT modified | ✅ | Correct |

---

### Round 4 Review Decision

**Status:** ✅ **APPROVED**

The implementation is correct, complete, and well-tested. All acceptance criteria are met,
1318 tests pass, markdownlint is clean on all targets, and the UAT plan artifacts correctly
demonstrate the three required rendering scenarios. Ready for UAT handoff.

---

### Round 4 Checklist Summary

| Category | Status |
|----------|--------|
| Correctness (1318 tests pass) | ✅ |
| Spec Compliance (all 12 AC) | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing (10 TC implemented) | ✅ |
| UAT Artifacts — existence | ✅ |
| UAT Artifacts — grouping demonstrated | ✅ |
| UAT Artifacts — markdownlint clean | ✅ |
| Comprehensive demo markdownlint | ✅ |
| SNAPSHOT_UPDATE_OK | ✅ |
| Work Protocol | ✅ |
| Global Documentation | ✅ |

---

### Round 4 Next Steps

**APPROVED for UAT.** Hand off to **UAT Tester** agent to validate rendering in a real
GitHub pull request. The `uat-plan.md` and `uat-test-plan.md` in
`docs/features/106-azapi-output-values/` contain the complete UAT scenario.

---

## Round 5 — Post-UAT Fix Review (commit `8102733`)

**Reviewer:** Code Reviewer agent
**Date:** 2026-02-28
**Scope:** Latest commit `8102733` only — B-1 suppression fix + UAT plan update.

### Round 5 Summary

This round reviews a focused fix that:
1. Removes the "Output Values" section entirely for the B-1 case (create/replace where
   `after_unknown.output = true` and no before/after output data exists)
2. Adds `linkedWorkspaceId` Azure resource ID to the UAT plan to demonstrate display name
   mapping
3. Updates the UAT test plan and regenerates the UAT plan MD
4. Updates 4 snapshot files to match the new "section suppressed" behavior

### Round 5 Verification Results

- **Tests:** ✅ **1318 passed, 0 failed, 0 skipped** (full suite via `scripts/test-with-timeout.sh`)
- **Comprehensive demo markdownlint:** ✅ 0 errors (`artifacts/comprehensive-demo.md`)
- **UAT plan markdownlint:** ✅ 0 errors (`docs/features/106-azapi-output-values/uat-plan.md`)
- **Comprehensive demo content:** ✅ Regenerated output differs only in header timestamp vs.
  committed artifact — content is identical
- **`SNAPSHOT_UPDATE_OK` token:** ✅ Present in commit `8102733` message

### Round 5 Template Logic Analysis

The change to `_output_values.sbn` is correct. The outer condition narrows from
`has_before_output || has_after_output || output_unknown` to `has_before_output || has_after_output`,
which means the entire block is skipped when there is no actual output data to show — exactly the
intended B-1 suppression.

All existing code paths remain correct:

| Case | Before → After | Result |
|------|---------------|--------|
| B-1: create/replace, `output_unknown=true`, no before/after output | Old: heading + notice shown | New: section suppressed entirely ✅ |
| B-2: replace, `output_unknown=true`, `has_before_output=true` | Before output shown in delete mode + notice | Still shown (outer=TRUE via `has_before_output`) ✅ |
| Update with before+after output | Before/After table | Unchanged ✅ |
| Create with `has_after_output=true` | After-only table | Unchanged ✅ |
| Delete with `has_before_output=true` | Before-only table | Unchanged ✅ |
| No output on either side | Section absent | Unchanged ✅ |

Edge case considered: replace with `output_unknown=true` AND `has_after_output=true` (unusual
combination). The template correctly stays in the `output_unknown` branch and does not render the
after output, which is the right behavior — `after_unknown.output = true` means the `after.output`
value cannot be trusted.

### Round 5 Snapshot Changes

All 4 snapshot changes are correct and minimal — they each remove exactly the 4 lines that
constituted the old B-1 rendering (`#### Output Values`, blank line, notice text, blank line):

| Snapshot | Reason for change |
|----------|------------------|
| `azapi-output-create-unknown.md` | Dedicated TC-01 test case: B-1 suppression |
| `azapi-create.md` | Existing create plan had `after_unknown.output = true` |
| `azapi-create-complete.md` | Existing complete-create plan had `after_unknown.output = true` |
| `comprehensive-demo-full.md` | azapi resource in comprehensive demo has `after_unknown.output = true` |

### Round 5 UAT Plan Quality

The `uat-plan.json` addition of `linkedWorkspaceId` (an Azure resource ID) in the
`automation_update` resource's output before/after values is correct and purposeful:
it demonstrates display name mapping — a key rendering quality feature that UAT testers
should verify is working in real GitHub/AzureDevOps markdown rendering.

The `uat-plan.md` regeneration is consistent with the JSON change (confirmed by manual
inspection of the output: the `linkedWorkspaceId` row renders as a formatted description
rather than a raw resource ID path).

The `uat-test-plan.md` update correctly reflects the new B-1 behavior: Resource 1
(`automation_create`) now specifies that the Output Values section must be **absent**
rather than showing a notice.

### Round 5 Issues Found

#### Major Issues

**M-1: `specification.md` not updated to reflect B-1 behavior change**

File: `docs/features/106-azapi-output-values/specification.md`

The specification was written when B-1 showed a "known after apply" notice, and was
never updated to reflect the new "section suppressed" behavior. Two locations are
inconsistent:

- Line 126 (Behaviour Per Action table): `| Create | absent | unknown ... | "known after apply" notice |`
  → Should say: `| Create | absent | unknown ... | *(section suppressed — nothing shown)* |`
- Lines 157–158 (Success Criteria): `When after_unknown.output = true (create/replace), a "known after apply" notice is shown`
  → Should say: `When after_unknown.output = true and no before/after output exists (B-1), the Output Values section is omitted entirely`
- Lines 82–88 (UX Example): `###### Output Values` / `*Output values are not known until after apply.*`
  → Should be removed or replaced with a note that the section is absent

**Impact:** Future developers reading the specification would implement the wrong behavior (showing
the notice instead of suppressing the section). Future code reviewers checking spec compliance would
incorrectly flag the current code as non-compliant.

**M-2: `test-plan.md` TC-01 description is wrong**

File: `docs/features/106-azapi-output-values/test-plan.md`

TC-01 and the feature→test mapping still describe the old behavior:
- Line 27: `| after_unknown.output = true → "known after apply" notice | TC-01 | Snapshot |`
  → Should say: `| after_unknown.output = true → Output Values section absent | TC-01 | Snapshot |`
- Lines 50–52 (TC-01 Description): "a brief italic notice is rendered under an `#### Output Values` heading"
  → Should say: "the Output Values section is suppressed entirely — no heading and no notice text"
- Lines 65–68 (TC-01 Expected Snapshot Output): shows `#### Output Values` / `*Output values are not known until after apply.*`
  → Should show that neither heading nor notice appears
- Line 432: `| after_unknown.output = true on create | "known after apply" notice, no table | TC-01 |`
  → Should say: `| after_unknown.output = true on create | No Output Values section | TC-01 |`

**Impact:** Same risk as M-1 — misleading guidance for future developers and reviewers.

**M-3: `AzapiSnapshotTests.cs` XML doc comment for TC-01 test method is wrong**

File: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzapiSnapshotTests.cs`, line ~212

The XML doc summary for `Snapshot_AzapiOutputCreateUnknown_MatchesBaseline` still reads:

> "Verifies that a create action with unknown output shows the known-after-apply notice."

This is incorrect — the test now verifies the **absence** of the Output Values section. The
comment should read something like: "Verifies that a create action with unknown output suppresses
the Output Values section entirely (B-1 case)."

**Impact:** Developers reading the test code are misled about what is being tested.

#### Minor Issues

**m-1 (Minor): Template comment inaccurate — "without before output" should be "without before or after output"**

File: `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/_output_values.sbn`, lines 3–4

The comment says:
> "When output is entirely unknown (create/replace without before output), no section is rendered"

The actual suppression condition (`has_before_output || has_after_output`) fires when **both** before
and after output are absent. The comment should say "without before or after output data" to
accurately reflect this. While create actions never have before output (making "without before output"
technically sufficient for that case), replace actions could have after output present without before
output, so the precise condition matters for correctness of the documentation.

Fix: Change line 4 in `_output_values.sbn` to:
> "without before or after output data"

#### Suggestions

None.

### Round 5 Critical Questions Answered

- **What could make this code fail?** The template logic handles all known combinations of
  `has_before_output`, `has_after_output`, and `output_unknown`. No failure scenarios identified.
- **What edge cases might not be handled?** `has_after_output=true` + `output_unknown=true`
  simultaneously on a create is an unusual Terraform edge case; the template correctly silences
  the after output in that branch. No practical failure.
- **Are all error paths tested?** Yes — all 4 distinct B-1 scenarios (create, replace w/no
  before, azapi-create.md, comprehensive demo) are covered by the updated snapshots.

### Round 5 Decision

**Status: Changes Requested**

Three documentation inconsistencies (M-1, M-2, M-3) need to be fixed before this can be
approved. The implementation itself is correct; all tests pass; snapshots are right. Only
doc updates are required.

### Round 5 Checklist Summary

| Category | Status |
|----------|--------|
| Correctness (1318 tests pass) | ✅ |
| Template logic — B-1 suppression | ✅ |
| Template logic — B-2 still works | ✅ |
| Snapshot changes (4 files, SNAPSHOT_UPDATE_OK) | ✅ |
| UAT plan JSON (`linkedWorkspaceId` added) | ✅ |
| UAT plan MD (regenerated, lints clean) | ✅ |
| UAT test plan (updated for new behavior) | ✅ |
| Comprehensive demo markdownlint | ✅ |
| `specification.md` updated | ❌ M-1 |
| `test-plan.md` updated | ❌ M-2 |
| `AzapiSnapshotTests.cs` doc comment updated | ❌ M-3 |
| Work Protocol — Round 5 entry | ⏳ (to be added) |

### Round 5 Next Steps

**Changes requested.** Hand off to **Developer** agent to fix M-1, M-2, and M-3:

1. Update `specification.md` to replace the B-1 "known after apply" notice description with
   "section suppressed entirely" in the behaviour table, success criteria, and UX example.
2. Update `test-plan.md` TC-01 description, expected snapshot output, and feature→test mapping
   to reflect "no section shown" rather than "notice shown".
3. Update the XML doc comment for `Snapshot_AzapiOutputCreateUnknown_MatchesBaseline` in
   `AzapiSnapshotTests.cs` to say "suppresses the Output Values section entirely (B-1 case)"
   instead of "shows the known-after-apply notice".
