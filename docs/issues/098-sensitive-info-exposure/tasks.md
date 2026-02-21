# Tasks: Sensitive Information Exposure (Fix 098)

## Overview

Six confirmed paths in tfplan2md can disclose Terraform-sensitive values in generated Markdown
output. This task plan implements all fixes described in `analysis.md` and `architecture.md`
following a **Red → Green → Refactor** discipline: failing tests are committed before each fix.

Reference: [analysis.md](analysis.md) · [architecture.md](architecture.md) · [test-plan.md](test-plan.md)

### Exposure paths addressed

| # | Description | Severity |
|---|---|---|
| A | AzApi create/delete/replace body — no sensitivity check | Critical |
| B | AzApi update body — `is_sensitive` computed but never used for masking | Critical |
| C | Scriban template context — `before/after_sensitive` not mapped; raw JSON exposed | High |
| D | Variable Group `IsSecret` transition — only `after.IsSecret` checked | High |
| E | Root boolean sensitivity (`before/after_sensitive: true`) — empty-string key ignored | Medium |
| F | Top-level array parent sensitivity (`secrets[0]` not linked to parent `secrets: true`) | Medium |

---

## Task 1: Failing unit tests — hierarchical sensitivity edge cases ✅

**Status:** Complete  
**Priority:** High  
**Test plan coverage:** TC-13, TC-14, TC-15, TC-16, TC-17, TC-18, TC-19  
**Exposure paths:** E, F

**Description:**  
Create a new TUnit test class `SensitivityHierarchyTests` in
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/`.
Write unit tests that exercise `IsSensitiveAttribute` and `GetHierarchicalPaths` for the two
known edge cases.  These tests must be **red** (failing) when committed before Task 2.

**Acceptance Criteria:**
- [ ] `SensitivityHierarchyTests.cs` exists in `MarkdownGeneration/` under the TUnit project.
- [ ] TC-13: `IsSensitiveAttribute("api_key", [], {"": "true"})` returns `true`.
- [ ] TC-14: `IsSensitiveAttribute("api_key", {"": "true"}, [])` returns `true`.
- [ ] TC-15: `IsSensitiveAttribute("secrets[0]", [], {"secrets": "true"})` returns `true`.
- [ ] TC-16: `IsSensitiveAttribute("secrets[1]", [], {"secrets": "true"})` returns `true`.
- [ ] TC-17: `GetHierarchicalPaths("secrets[0]")` includes `"secrets"` in its output.
- [ ] TC-18 (regression guard): `GetHierarchicalPaths("a[0].b[1]")` includes `"a[0].b[1]"`, `"a[0].b"`, `"a[0]"`, `"a"`.
- [ ] TC-19: `IsSensitiveAttribute("anything", [], {"": "true"})` returns `true`; also for `"nested.deep.key"` and `"arr[0]"`.
- [ ] All new tests fail before Task 2 changes are applied (verify by running them against unmodified code).

**Dependencies:** None

**Notes:**  
`IsSensitiveAttribute` and `GetHierarchicalPaths` are private `static` methods in
`ReportModelBuilder.ResourceChanges.cs`. They may need to be made `internal` (with
`InternalsVisibleTo`) or extracted to a testable static class to enable direct unit testing.
Prefer extraction to a dedicated `SensitivityHelper` class in `MarkdownGeneration/Helpers/`
rather than changing access modifiers on the partial class.

---

## Task 2: Fix hierarchical sensitivity detection ✅

**Status:** Complete  
**Priority:** High  
**Test plan coverage:** TC-13, TC-14, TC-15, TC-16, TC-17, TC-19  
**Exposure paths:** E, F

**Description:**  
Fix `IsSensitiveAttribute` and `GetHierarchicalPaths` (extracted in Task 1 or kept in place)
to handle the two missed encodings.

**Acceptance Criteria:**
- [ ] `IsSensitiveAttribute` checks whether the empty-string key `""` is present in either
  sensitivity dictionary before iterating hierarchical paths. If found, returns `true`
  immediately for any attribute name.
- [ ] `GetHierarchicalPaths` emits the base array name for keys that contain `[` but have no
  `.` (e.g., `"secrets[0]"` yields `"secrets"` in addition to `"secrets[0]"`).
- [ ] All tests from Task 1 pass (TC-13 to TC-17, TC-19).
- [ ] TC-18 continues to pass (no regression in existing dotted paths).
- [ ] `ReportModelBuilderTests` — existing sensitive attribute tests pass (TC-20, TC-21).

**Dependencies:** Task 1

**Notes:**  
`GetHierarchicalPaths` currently handles the parent-array-without-index case only when the
path contains a `.` (line ~197 in `ReportModelBuilder.ResourceChanges.cs`). The new case is
when the entire key is just `name[n]` with no dot. Add this as a secondary yield before the
main parent-splitting loop.

---

## Task 3: Failing unit tests — Variable Group `IsSecret` transitions ✅

**Status:** Complete  
**Priority:** High  
**Test plan coverage:** TC-11, TC-12  
**Exposure paths:** D

**Description:**  
In `VariableGroupViewModelFactoryTests.cs`, add two new test methods that cover the
`is_secret` direction transitions. Tests must be **red** when committed.

**Acceptance Criteria:**
- [ ] TC-11: A variable diff where `before.is_secret = true`, `after.is_secret = false` must
  NOT expose the original secret value; the before-column display must be masked.
- [ ] TC-12: A variable diff where `before.is_secret = false`, `after.is_secret = true` must
  mask both columns (the existing `after.IsSecret` path already masks the after column, but the
  before plaintext can still leak; verify both are masked after the fix).
- [ ] Both tests fail before Task 4 is applied.

**Dependencies:** None

---

## Task 4: Fix Variable Group `IsSecret` masking ✅

**Status:** Complete  
**Priority:** High  
**Test plan coverage:** TC-11, TC-12  
**Exposure paths:** D

**Description:**  
Change the masking condition in `VariableGroupFormatters.CreateDiffRow` from
`after.IsSecret` to `(before.IsSecret || after.IsSecret)`, matching the pattern already used
in `BuildDefinitionFormatters.CreateDiffRow`.

**Acceptance Criteria:**
- [ ] `VariableGroupFormatters.CreateDiffRow` uses `(before.IsSecret || after.IsSecret)` as the
  masking predicate.
- [ ] TC-11 and TC-12 pass.
- [ ] All existing `VariableGroupViewModelFactoryTests` continue to pass.
- [ ] All existing `VariableGroupTemplateTests` continue to pass.

**Dependencies:** Task 3

**Notes:**  
One-line change. The display string for the secret case already uses the masked value
`"(sensitive / hidden)"` — only the condition guard needs updating.

---

## Task 5: Failing tests — AzApi create/delete/replace sensitivity ✅

**Status:** Complete  
**Priority:** High  
**Test plan coverage:** TC-01, TC-02, TC-03, TC-04, TC-05, TC-21  
**Exposure paths:** A

**Description:**  
Write failing assertion tests in a new class `AzApiSensitiveMaskingTests` (located in
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/`). Also create two new test data JSON
files for delete and replace scenarios.  Tests must be **red** when committed.

**Acceptance Criteria:**
- [ ] New test data file `TestData/azapi-delete-sensitive-plan.json` exists (delete action,
  `before.body.properties.clientSecret = "actual-secret"`, `before_sensitive.body.properties.clientSecret = true`).
- [ ] New test data file `TestData/azapi-replace-sensitive-plan.json` exists (replace action,
  `before/after.body.properties.secret` with both sides sensitive).
- [ ] TC-01: Render `azapi-sensitive-plan.json`; assert output does NOT contain `P@ssw0rd123!`
  and DOES contain `(sensitive)`.
- [ ] TC-02: Render `azapi-body-sensitive-plan.json`; assert no plaintext tenant ID or sku
  values; assert `(sensitive)` present.
- [ ] TC-03: Render `azapi-delete-sensitive-plan.json`; assert `actual-secret` absent,
  `(sensitive)` present.
- [ ] TC-04: Render `azapi-replace-sensitive-plan.json`; assert neither `old-secret` nor
  `new-secret` appears; assert `(sensitive)` present.
- [ ] TC-05: Render `azapi-sensitive-plan.json` with `showSensitive = true`; assert
  `P@ssw0rd123!` is present (this test remains red until Task 6 is done).
- [ ] TC-21: Render `azapi-sensitive-plan.json`; assert non-sensitive values `sqladmin`,
  `12.0`, `Enabled` are still present (this test should stay green throughout).
- [ ] All failing tests are confirmed red against unmodified code.

**Dependencies:** None

**Notes:**  
Consult the JSON schema guide in `test-plan.md § Test Data Requirements` for the exact shape
of the new fixture files. The test helper pattern to use is the same `RenderAzapiPlan` approach
from `AzapiSnapshotTests` (load via `TerraformPlanParser`, build via `ReportModelBuilder`,
render via `MarkdownRenderer`). The `ReportModelBuilder` constructor needs `showSensitive`
to be settable for TC-05; check its existing constructor options.

---

## Task 6: Fix AzApi create/delete/replace body sensitivity ✅

**Status:** Complete  
**Priority:** High  
**Test plan coverage:** TC-01, TC-02, TC-03, TC-04, TC-05  
**Exposure paths:** A

**Description:**  
Thread `beforeSensitive` / `afterSensitive` into `RenderCreateDeleteBody` and mask each
flattened body property using the centralized sensitivity API before appending it to the
markdown output.

**Acceptance Criteria:**
- [ ] `RenderCreateDeleteBody` accepts sensitivity parameters (either pass-through from the
  outer `RenderAzapiBody` call or a lambda/strategy).
- [ ] For each flattened property `(key, value)`, check `IsSensitiveAttribute(key, ...)` using
  the centralized helper (Task 2 extraction). If sensitive and `showSensitive = false`, render
  `(sensitive)` instead of the raw value.
- [ ] TC-01 through TC-04 pass.
- [ ] TC-05 (`--show-sensitive`) pass.
- [ ] TC-21 (non-sensitive values not over-masked) pass.
- [ ] `showSensitive` flag is threaded through from `RenderAzapiBody` all the way down to the
  value emission without using any global state.
- [ ] Existing AzApi snapshot tests continue to pass (or their baselines are updated as part of
  this task — see Task 11).

**Dependencies:** Task 2 (centralized sensitivity API), Task 5 (test data & failing tests)

**Notes:**  
The `showSensitive` flag is currently available in `ReportModelBuilder` as `_showSensitive`.
It must be threaded into the `RenderAzapiBody` Scriban helper call. The helper signature
already accepts `beforeSensitive` and `afterSensitive`; verify these are correctly forwarded
from the template call in `resource.sbn` which currently passes `null` for those args.
Also check `AzApi.Rendering.cs` — `RenderAzapiBody` already accepts these parameters but drops
them in the non-update branch.

---

## Task 7: Failing tests — AzApi update body sensitivity

**Priority:** High  
**Test plan coverage:** TC-06, TC-07  
**Exposure paths:** B

**Description:**  
Add failing unit tests to a new or extended `ScribanHelpersAzApiUpdateRenderingTests` class
(located in `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/` or
`Providers/AzApi/`). Also create `TestData/azapi-update-sensitive-plan.json`.  Tests must
be **red** when committed.

**Acceptance Criteria:**
- [ ] New test data file `TestData/azapi-update-sensitive-plan.json` exists (update action,
  `before/after.body.properties.clientSecret` differs, `after_sensitive.body.properties.clientSecret = true`).
- [ ] TC-06: Direct call to `AzApiHelpers.RenderAzapiBody` (update mode) with sensitive
  `afterSensitive`; assert `new-secret` absent, `(sensitive)` present, `name` value visible.
- [ ] TC-07: Same call with `showSensitive = true`; assert `new-secret` or `old-secret` present.
- [ ] Both tests fail against unmodified code.

**Dependencies:** None

**Notes:**  
The `RenderAzapiBody` method is accessible as a static method on `ScribanHelpers`. If it
requires Scriban context to be set up, write the test using the same fixture approach as
existing `ScribanHelpers` tests in the project.

---

## Task 8: Fix AzApi update body sensitivity

**Priority:** High  
**Test plan coverage:** TC-06, TC-07  
**Exposure paths:** B

**Description:**  
In `AzApi.Rendering.Update.cs` and `AzApi.Data.cs`, wire the `is_sensitive` property into
the value-rendering code so that sensitive update rows emit `(sensitive)` instead of the
raw `before`/`after` values.

**Acceptance Criteria:**
- [x] `CompareJsonProperties` properly passes `showSensitive` (remove the `#pragma warning
  disable IDE0060` suppression) or the flag is removed from the signature and masking is handled
  at the render layer.
- [x] Each comparison row with `is_sensitive = true` renders `(sensitive)` for its before/after
  value columns when `showSensitive = false`.
- [x] TC-06 and TC-07 pass.
- [x] No plaintext secret values appear in the existing `azapi-sensitive` update snapshot (to be
  verified by the snapshot update task).

**Dependencies:** Task 2 (centralized sensitivity API), Task 7 (failing tests)

**Notes:**  
`AzApi.Data.cs` currently sets `is_sensitive` on the comparison object but the `showSensitive`
parameter of `CompareJsonProperties` is suppressed with `#pragma warning disable IDE0060`.
The cleanest fix is to implement masking in the render layer
(`AzApi.Rendering.Update.cs`) using the already-set `is_sensitive` flag, which avoids
threading `showSensitive` deeply into the data comparison functions. The `showSensitive`
parameter of `CompareJsonProperties` can then be removed since masking moves to the renderer.

---

## Task 9: Failing tests — Scriban template context sensitivity mapping

**Priority:** Medium  
**Test plan coverage:** TC-08, TC-09, TC-10  
**Exposure paths:** C

**Description:**  
Create a new test class `AotScriptObjectMapperTests` in
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/`.  Tests must be **red** when committed.

**Acceptance Criteria:**
- [ ] TC-08: `MapResourceChange` with a `ResourceChangeModel` where `BeforeSensitive` is
  `{"password": true}` produces a `ScriptObject` whose `before_sensitive.password` is truthy.
- [ ] TC-09: Same for `AfterSensitive` → `after_sensitive` in the context.
- [ ] TC-10: `MapResourceChange` with `showSensitive = false`, a `ResourceChangeModel` where
  `AfterJson = {"name": "test", "password": "secret123"}` and `AfterSensitive = {"password": true}`
  produces an `after_json` in the `ScriptObject` where `after_json.password == "(sensitive)"`
  and `after_json.name == "test"`.
- [ ] All three tests fail before Task 10 is applied.

**Dependencies:** Task 2 (centralized sensitivity API — needed for JSON masking in Task 10)

---

## Task 10: Propagate sensitivity into Scriban template context

**Priority:** Medium  
**Test plan coverage:** TC-08, TC-09, TC-10  
**Exposure paths:** C

**Description:**  
Extend the Scriban template context to include masked-by-default JSON and explicit sensitivity
maps, as specified in `architecture.md § Option 2`.

**Acceptance Criteria:**
- [ ] `ResourceChangeModel` gains `BeforeSensitive` and `AfterSensitive` properties (type
  `object?`, consistent with `BeforeJson` / `AfterJson`).
- [ ] `ReportModelBuilder.BuildResourceChangeModel` populates these from
  `rc.Change.BeforeSensitive` and `rc.Change.AfterSensitive`.
- [ ] `AotScriptObjectMapper.MapResourceChange` maps `before_sensitive` and `after_sensitive`
  into the `ScriptObject` using the same `ConvertToScriptObject` helper as `before_json` /
  `after_json`.
- [ ] `AotScriptObjectMapper.MapResourceChange` applies the sensitivity mask to `before_json` /
  `after_json` before placing them in the `ScriptObject`: when `showSensitive = false`,
  any leaf value whose key is sensitive (per the centralized sensitivity API) is replaced with
  the string `"(sensitive)"`.
- [ ] TC-08, TC-09, TC-10 pass.
- [ ] Existing template-based snapshot tests continue to pass (no unintended masking of
  non-sensitive values).

**Dependencies:** Task 2 (centralized sensitivity API), Task 9 (failing tests)

**Notes:**  
The `showSensitive` flag must be accessible to `AotScriptObjectMapper`. The mapper is
currently a static class; check how `ReportModelBuilder` passes options down — a parameter to
`MapResourceChange` or an injected options object both are acceptable.  
The masking transformation should traverse the JSON tree (using `JsonElement` or
`ScriptObject` graph) and replace only leaf values that are sensitive per the sensitivity
dictionaries.

---

## Task 11: Update snapshot baselines and verify regression guards

**Priority:** Medium  
**Test plan coverage:** TC-20, TC-21, snapshot updates  
**Exposure paths:** A, B (observable through snapshots)

**Description:**  
After Tasks 6 and 8 make the AzApi renderers sensitivity-aware, regenerate the two snapshot
baseline files that currently encode the broken (plaintext) behavior. Verify all regression
guard tests pass.

**Acceptance Criteria:**
- [ ] `TestData/Snapshots/azapi-sensitive.md` updated: `administratorLoginPassword` row shows
  `` `(sensitive)` `` instead of `` `P@ssw0rd123!` ``.
- [ ] `TestData/Snapshots/azapi-body-sensitive.md` updated: all body property rows show
  `` `(sensitive)` `` instead of plaintext values.
- [ ] TC-20 (regression guard — existing sensitive attribute masking) passes.
- [ ] TC-21 (regression guard — non-sensitive attributes not over-masked) passes.
- [ ] All `AzapiSnapshotTests` pass after baseline regeneration.
- [ ] Commit message for the snapshot update includes the token `SNAPSHOT_UPDATE_OK` (project
  convention required by `.github/copilot-instructions.md`).

**Dependencies:** Task 6, Task 8

**Notes:**  
Use `scripts/update-test-snapshots.sh` to regenerate baselines. Review the diff carefully to
confirm only the expected plaintext→`(sensitive)` substitutions appear. Reject any unintended
changes to non-sensitive value rows.

---

## Implementation Order

Recommended sequence following the Red → Green → Refactor discipline:

1. **Task 1** — Write failing tests for hierarchical sensitivity (no code changes yet; establishes Red baseline for E/F bugs).
2. **Task 2** — Fix `IsSensitiveAttribute` + `GetHierarchicalPaths`; tests go Green. Also extracts the sensitivity API to a testable location for reuse in later tasks.
3. **Task 3** — Write failing tests for Variable Group `IsSecret` transitions.
4. **Task 4** — One-line fix in `VariableGroupFormatters`; tests go Green.
5. **Task 5** — Write failing tests + new test data for AzApi create/delete/replace sensitivity.
6. **Task 6** — Fix `RenderCreateDeleteBody` sensitivity; tests go Green.
7. **Task 7** — Write failing tests + new test data for AzApi update sensitivity.
8. **Task 8** — Fix AzApi update render path; tests go Green.
9. **Task 9** — Write failing tests for Scriban context sensitivity mapping.
10. **Task 10** — Propagate `before/after_sensitive` into Scriban context + mask `before/after_json`; tests go Green.
11. **Task 11** — Regenerate snapshot baselines; run all regression guards.

Tasks 1–4 (simple bug fixes) and Tasks 5–8 (AzApi body) are independent of Task 9–10 (Scriban context) and can proceed in parallel if two developers are available.

---

## Open Questions

None — all requirements are directly traceable to confirmed bugs in `analysis.md` and
architecture decisions in `architecture.md`.
