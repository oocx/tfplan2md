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
