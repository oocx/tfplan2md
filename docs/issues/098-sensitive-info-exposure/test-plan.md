# Test Plan: Sensitive Information Exposure (Issue 098)

## Overview

This test plan covers the security fixes described in `analysis.md` and `architecture.md` for issue 098.
The core requirement is: **sensitive values must never appear in generated Markdown unless `--show-sensitive` is explicitly passed**.

Six distinct exposure paths are confirmed by the analysis:

1. **AzApi create/delete/replace body** — entire body rendered with no sensitivity check  
2. **AzApi update body** — `is_sensitive` flag is computed but never used for masking  
3. **Scriban template context** — `before_sensitive`/`after_sensitive` not mapped by `AotScriptObjectMapper`  
4. **Variable Group `IsSecret` transition** — only `after.IsSecret` checked; `true → false` leaks old value  
5. **Root boolean sensitivity** — `after_sensitive: true` produces `{"": "true"}` in flat dict; empty-string key never checked  
6. **Top-level array parent sensitivity without dot** — `secrets[0]` not linked to parent `secrets: true`

All test cases in this plan are designed to **fail with the current codebase** before fixes are applied.

---

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type | Status before fix |
|---|---|---|---|
| AC-1: AzApi create body masks sensitive field | TC-01 | Snapshot (direct assertion) | **FAILING** |
| AC-1: AzApi create body masks when entire body sensitive | TC-02 | Snapshot (direct assertion) | **FAILING** |
| AC-1: AzApi delete body masks sensitive field | TC-03 | Snapshot (direct assertion) | **FAILING** |
| AC-1: AzApi replace body masks sensitive field | TC-04 | Snapshot (direct assertion) | **FAILING** |
| AC-1: `--show-sensitive` reveals values in AzApi create | TC-05 | Snapshot (direct assertion) | Pass after fix only |
| AC-2: AzApi update body masks sensitive field | TC-06 | Unit + Snapshot | **FAILING** |
| AC-2: `--show-sensitive` reveals values in AzApi update | TC-07 | Unit | Pass after fix only |
| AC-3: Scriban context contains `before_sensitive` | TC-08 | Unit | **FAILING** |
| AC-3: Scriban context contains `after_sensitive` | TC-09 | Unit | **FAILING** |
| AC-3: `before_json`/`after_json` in context are masked | TC-10 | Unit | **FAILING** |
| AC-4: Variable Group `true → false` masks before-value | TC-11 | Unit | **FAILING** |
| AC-4: Variable Group `false → true` masks before-value | TC-12 | Unit | **FAILING** |
| AC-5: Root boolean `after_sensitive: true` masks all attrs | TC-13 | Unit | **FAILING** |
| AC-5: Root boolean `before_sensitive: true` masks all attrs | TC-14 | Unit | **FAILING** |
| AC-6: Array parent `secrets: true` masks `secrets[0]` | TC-15 | Unit | **FAILING** |
| AC-6: Array parent `secrets: true` masks `secrets[1]` | TC-16 | Unit | **FAILING** |
| AC-7: `GetHierarchicalPaths` emits base for `key[n]` (no dot) | TC-17 | Unit | **FAILING** |
| AC-7: `GetHierarchicalPaths` handles `a[0].b[1]` correctly | TC-18 | Unit | Pass (verify no regression) |
| AC-7: `IsSensitiveAttribute` catches empty-string root key | TC-19 | Unit | **FAILING** |
| Regression: existing sensitive attrs still masked | TC-20 | Existing test (guard) | Pass |
| Regression: non-sensitive attributes are not masked | TC-21 | Existing test (guard) | Pass |

---

## Test Cases

### TC-01: AzApi create body masks individual sensitive field

**Type:** Snapshot / direct assertion  
**Test class:** `AzapiSnapshotTests` or new `AzApiSensitiveMaskingTests`  
**Method name:** `RenderAzapiCreate_WithSensitiveBodyProperty_MasksValue`

**Description:**  
Assert that rendering an `azapi_resource` create plan where `after_sensitive.body.properties.administratorLoginPassword = true`
produces `(sensitive)` for that field and does NOT render the plaintext password.

**Preconditions:**
- Test data: `TestData/azapi-sensitive-plan.json` (already exists; contains `P@ssw0rd123!` as plaintext with `after_sensitive.body.properties.administratorLoginPassword = true`)

**Test Steps:**
1. Load `azapi-sensitive-plan.json` through the full rendering pipeline (same helper as `AssertAzapiSnapshot`).
2. Assert output does NOT contain `P@ssw0rd123!`.
3. Assert output CONTAINS `(sensitive)` in the Body table row for `administratorLoginPassword`.
4. Assert non-sensitive values (`sqladmin`, `12.0`, `Enabled`) still appear.

**Expected Result:** Password is masked; non-sensitive values are visible.

**Why it currently fails:** `RenderCreateDeleteBody` renders all body values without sensitivity checks. The existing snapshot `azapi-sensitive.md` encodes the broken behavior (plaintext `P@ssw0rd123!`), confirming no masking occurs.

> **Note:** The existing snapshot test `Snapshot_AzapiSensitive_MatchesBaseline` will need its baseline updated once the fix is applied. This new test uses direct assertions and will catch the bug in the current state.

---

### TC-02: AzApi create body masks when entire body is sensitive (`body: true`)

**Type:** Snapshot / direct assertion  
**Test class:** `AzapiSnapshotTests` or new `AzApiSensitiveMaskingTests`  
**Method name:** `RenderAzapiCreate_WithAllBodySensitive_MasksAllProperties`

**Description:**  
When `after_sensitive.body = true` (the whole body is sensitive), every property in the Body table must be masked.

**Preconditions:**
- Test data: `TestData/azapi-body-sensitive-plan.json` (already exists; `after_sensitive.body = true`, plaintext: `tenantId`, `sku.name`, `sku.family`)

**Test Steps:**
1. Render `azapi-body-sensitive-plan.json`.
2. Assert output does NOT contain `12345678-1234-1234-1234-123456789012` (tenantId).
3. Assert output does NOT contain `standard` or `A` (sku values).
4. Assert output DOES contain `(sensitive)` for each body property row.

**Expected Result:** All body properties masked.

**Why it currently fails:** `RenderCreateDeleteBody` has no sensitivity logic; the snapshot `azapi-body-sensitive.md` shows all body values in plaintext.

---

### TC-03: AzApi delete body masks sensitive field

**Type:** Snapshot / direct assertion  
**Test class:** `AzapiSnapshotTests` or new `AzApiSensitiveMaskingTests`  
**Method name:** `RenderAzapiDelete_WithSensitiveBodyProperty_MasksValue`

**Description:**  
For delete actions the rendered value comes from `change.before.body`. If `before_sensitive` marks a property sensitive, it must be masked.

**Preconditions:**
- **New test data required:** `TestData/azapi-delete-sensitive-plan.json`  
  Structure: delete action on an `azapi_resource` whose `before.body.properties` contains a secret (e.g., `clientSecret: "actual-secret"`), with `before_sensitive.body.properties.clientSecret = true`.

**Test Steps:**
1. Render `azapi-delete-sensitive-plan.json`.
2. Assert the output does NOT contain `actual-secret`.
3. Assert the output CONTAINS `(sensitive)` for the `clientSecret` property.

**Expected Result:** Secret is masked in delete rendering.

**Why it currently fails:** Same root cause as TC-01 — `RenderCreateDeleteBody` ignores sensitivity metadata regardless of action.

---

### TC-04: AzApi replace body masks sensitive field

**Type:** Snapshot / direct assertion  
**Test class:** `AzapiSnapshotTests` or new `AzApiSensitiveMaskingTests`  
**Method name:** `RenderAzapiReplace_WithSensitiveBodyProperty_MasksValue`

**Description:**  
Replace actions produce both delete (before) and create (after) sections. Both must be masked.

**Preconditions:**
- **New test data required:** `TestData/azapi-replace-sensitive-plan.json`  
  Structure: replace action (`["delete", "create"]`) with `before.body.properties.secret = "old-secret"` and `after.body.properties.secret = "new-secret"`, with both `before_sensitive.body.properties.secret = true` and `after_sensitive.body.properties.secret = true`.

**Test Steps:**
1. Render `azapi-replace-sensitive-plan.json`.
2. Assert output does NOT contain `old-secret`.
3. Assert output does NOT contain `new-secret`.
4. Assert output CONTAINS `(sensitive)` (at least once, in the Body section).

**Expected Result:** Both before and after secret values are masked.

---

### TC-05: `--show-sensitive` reveals plaintext in AzApi create body

**Type:** Snapshot / direct assertion  
**Test class:** `AzapiSnapshotTests` or `AzApiSensitiveMaskingTests`  
**Method name:** `RenderAzapiCreate_ShowSensitive_RevealsValue`

**Description:**  
When `--show-sensitive` is enabled, sensitive body values should be rendered as-is.

**Preconditions:**
- Test data: `TestData/azapi-sensitive-plan.json` (existing)

**Test Steps:**
1. Render `azapi-sensitive-plan.json` with `showSensitive = true`.
2. Assert the output CONTAINS `P@ssw0rd123!`.

**Expected Result:** Plaintext value visible when `--show-sensitive` is set.

> **Note:** This test will only pass AFTER the fix; include it to verify the escape hatch works correctly.

---

### TC-06: AzApi update body masks sensitive property

**Type:** Unit (direct helper call) + Snapshot  
**Test class:** `ScribanHelpersAzApiUpdateRenderingTests`  
**Method name:** `RenderAzapiBody_UpdateMode_WithSensitiveProperty_MasksValue`

**Description:**  
In update mode, `is_sensitive` is set on comparison objects but the rendering path never uses it to mask values. This test passes `afterSensitive` with a marked field and asserts the value is masked.

**Preconditions:**
- Inline test data constructed in the test method:
  - `before` JSON: `{ "properties": { "clientSecret": "old-secret", "name": "test" } }`
  - `after` JSON: `{ "properties": { "clientSecret": "new-secret", "name": "test" } }`
  - `afterSensitive` JSON: `{ "properties": { "clientSecret": true } }`

**Test Steps:**
1. Call `AzApiHelpers.RenderAzapiBody(bodyJson: after, mode: "update", beforeJson: before, afterSensitive: afterSensitiveJson, showUnchanged: false, ...)`.
2. Assert output does NOT contain `new-secret`.
3. Assert output does NOT contain `old-secret`.
4. Assert output CONTAINS `(sensitive)`.
5. Assert `name` value `test` is still visible (not over-masked).

**Expected Result:** `clientSecret` is masked; `name` is shown.

**Why it currently fails:** `ComparePairs` computes `is_sensitive = true` but `RenderUpdateProperties` renders raw values without checking this flag.

---

### TC-07: AzApi update body reveals value when `--show-sensitive` is set

**Type:** Unit  
**Test class:** `ScribanHelpersAzApiUpdateRenderingTests`  
**Method name:** `RenderAzapiBody_UpdateMode_ShowSensitive_RevealsValue`

**Description:**  
With `showSensitive = true`, even properties marked sensitive in `afterSensitive` should appear in plaintext.

**Preconditions:** Same inline data as TC-06.

**Test Steps:**
1. Call `RenderAzapiBody` with same inputs but `showSensitive = true`.
2. Assert output CONTAINS `new-secret` or `old-secret`.

**Expected Result:** Plaintext visible with `--show-sensitive`.

---

### TC-08: Scriban context contains `before_sensitive` mapping

**Type:** Unit  
**Test class:** New `AotScriptObjectMapperTests` or existing `MarkdownRendererTests`  
**Method name:** `MapResourceChange_WithSensitiveChange_MapsBeforeSensitiveToContext`

**Description:**  
`AotScriptObjectMapper.MapResourceChange` must populate a `before_sensitive` key in the Scriban `ScriptObject`. Currently it maps `before_json`/`after_json` but not the sensitivity maps.

**Preconditions:**
- A `ResourceChangeModel` instance with non-null `BeforeSensitive` (e.g., `{"password": true}`).

**Test Steps:**
1. Build a `ResourceChangeModel` with `BeforeSensitive` serialized to a `JsonElement`.
2. Call `AotScriptObjectMapper.MapResourceChange(...)`.
3. Assert the resulting `ScriptObject` has a key `before_sensitive` that is not null.
4. Assert `before_sensitive.password` resolves to `true`.

**Expected Result:** `before_sensitive` is present in the template context.

**Why it currently fails:** `AotScriptObjectMapper` only maps raw JSON blobs; sensitivity metadata is never forwarded.

---

### TC-09: Scriban context contains `after_sensitive` mapping

**Type:** Unit  
**Test class:** `AotScriptObjectMapperTests`  
**Method name:** `MapResourceChange_WithSensitiveChange_MapsAfterSensitiveToContext`

**Description:** Mirror of TC-08 for `after_sensitive`.

**Test Steps:**
1. Build a `ResourceChangeModel` with `AfterSensitive` set.
2. Assert the `ScriptObject` has a non-null `after_sensitive` key with correct structure.

---

### TC-10: `before_json`/`after_json` in Scriban context are masked by default

**Type:** Unit  
**Test class:** `AotScriptObjectMapperTests`  
**Method name:** `MapResourceChange_WithSensitiveValues_MasksJsonInContext_WhenShowSensitiveFalse`

**Description:**  
When `showSensitive = false`, the `before_json`/`after_json` values exposed to templates must have sensitive leaves replaced with `(sensitive)`.

**Preconditions:**
- `ResourceChangeModel` with `AfterJson = { "name": "test", "password": "secret123" }` and `AfterSensitive = { "password": true }`.

**Test Steps:**
1. Call mapper with `showSensitive = false`.
2. Assert `after_json.password` in the context equals `(sensitive)`.
3. Assert `after_json.name` equals `test` (not over-masked).

**Expected Result:** Only sensitive leaves are masked in the JSON tree.

**Why it currently fails:** Masking of JSON blobs before passing to context is not implemented.

---

### TC-11: Variable Group diff masks before-value when `IsSecret true → false`

**Type:** Unit  
**Test class:** `VariableGroupViewModelFactoryTests`  
**Method name:** `CreateDiff_SecretToNonSecret_MasksBeforeValue`

**Description:**  
When a variable transitions from `is_secret = true` (before) to `is_secret = false` (after), the **before** value must be masked because it was a secret.

**Preconditions:**
- Inline plan data: before variable `{ "name": "API_KEY", "value": "old-plaintext-secret", "is_secret": true }`, after variable `{ "name": "API_KEY", "value": "new-value", "is_secret": false }`.

**Test Steps:**
1. Build a `VariableGroupViewModel` from a plan where `API_KEY` changes from `is_secret: true` to `is_secret: false`.
2. Find the `API_KEY` entry in `VariableChanges`.
3. Assert the before-value in the diff is NOT `old-plaintext-secret`.
4. Assert the diff contains `(sensitive / hidden)` or equivalent mask for the before column.

**Expected Result:** Before-value masked because it was a secret.

**Why it currently fails:** `VariableGroupFormatters.CreateDiffRow` at line 139 checks only `after.IsSecret`:
```csharp
var valueDisplay = after.IsSecret  // BUG: should be (before.IsSecret || after.IsSecret)
```

---

### TC-12: Variable Group diff masks both sides when `IsSecret false → true`

**Type:** Unit  
**Test class:** `VariableGroupViewModelFactoryTests`  
**Method name:** `CreateDiff_NonSecretToSecret_MasksBothValues`

**Description:**  
When a variable transitions from `is_secret = false` to `is_secret = true`, masking the `after` value is already correct with the current code. However, the `||` parity pattern also requires that the `before` plaintext value is masked (defense-in-depth, consistent with `BuildDefinitionFormatters`).

**Preconditions:**
- Before: `{ "name": "TOKEN", "value": "plaintext-before", "is_secret": false }`, after: `{ "name": "TOKEN", "value": "new-secret", "is_secret": true }`.

**Test Steps:**
1. Build the diff view model.
2. Assert `after` column for `TOKEN` is masked (should pass today).
3. Assert `before` column is also masked (currently may reveal `plaintext-before`).

**Expected Result:** Both sides masked once variable becomes secret.

---

### TC-13: Root boolean `after_sensitive: true` masks all attributes

**Type:** Unit  
**Test class:** `ReportModelBuilderTests` or new `SensitivityHierarchyTests`  
**Method name:** `IsSensitiveAttribute_RootBooleanAfterSensitive_MasksAllAttributes`

**Description:**  
When `after_sensitive = true` (JSON boolean), `JsonFlattener` produces `{"": "true"}`. `IsSensitiveAttribute` must detect the empty-string key and treat every attribute as sensitive.

**Preconditions:**
- `afterSensitiveDict = new Dictionary<string, string> { [""] = "true" }`

**Test Steps:**
1. Call `IsSensitiveAttribute("api_key", beforeSensitiveDict: [], afterSensitiveDict: {"": "true"})`.
2. Assert result is `true`.
3. Repeat for a nested key: `IsSensitiveAttribute("properties.password", ...)`.
4. Assert result is `true`.

**Expected Result:** All attributes reported as sensitive when root is `true`.

**Why it currently fails:** `GetHierarchicalPaths("api_key")` splits on `.` and yields only `["api_key"]` — the empty-string key is never generated or checked.

---

### TC-14: Root boolean `before_sensitive: true` masks all attributes

**Type:** Unit  
**Test class:** `ReportModelBuilderTests` or `SensitivityHierarchyTests`  
**Method name:** `IsSensitiveAttribute_RootBooleanBeforeSensitive_MasksAllAttributes`

**Description:** Mirror of TC-13 for `before_sensitive = true`.

**Test Steps:**
1. Call `IsSensitiveAttribute("api_key", beforeSensitiveDict: {"": "true"}, afterSensitiveDict: [])`.
2. Assert result is `true`.

---

### TC-15: Array parent marked sensitive masks top-level array items (`secrets[0]`)

**Type:** Unit  
**Test class:** `ReportModelBuilderTests` or `SensitivityHierarchyTests`  
**Method name:** `IsSensitiveAttribute_ArrayParentSensitive_MasksIndexedChild_NoSelector`

**Description:**  
When `afterSensitiveDict["secrets"] = "true"`, a flattened key `secrets[0]` (which has no `.` separator) must be detected as sensitive. Currently `GetHierarchicalPaths("secrets[0]")` yields only `["secrets[0]"]` — the base `secrets` path is never emitted.

**Preconditions:**
- `afterSensitiveDict = {"secrets": "true"}`

**Test Steps:**
1. Call `IsSensitiveAttribute("secrets[0]", beforeSensitiveDict: [], afterSensitiveDict: {"secrets": "true"})`.
2. Assert result is `true`.
3. Call for `secrets[1]`, assert `true`.

**Expected Result:** Array items are sensitive when their parent is marked.

**Why it currently fails:** `GetHierarchicalPaths("secrets[0]")` returns `["secrets[0]"]` without yielding `"secrets"`.

---

### TC-16: Array parent marked sensitive masks second and later array items

**Type:** Unit  
**Test class:** `SensitivityHierarchyTests`  
**Method name:** `IsSensitiveAttribute_ArrayParentSensitive_MasksMultipleIndexedChildren`

**Description:** Same as TC-15 but verifies indices beyond `[0]` (e.g., `[5]`, `[10]`).

**Test Steps:**
1. For each of `secrets[0]`, `secrets[5]`, `secrets[10]`:
   - Call `IsSensitiveAttribute` with `afterSensitiveDict = {"secrets": "true"}`.
   - Assert `true`.

---

### TC-17: `GetHierarchicalPaths` emits parent base for key with array index and no dot

**Type:** Unit  
**Test class:** `SensitivityHierarchyTests`  
**Method name:** `GetHierarchicalPaths_ArrayKeyWithoutDot_EmitsBaseName`

**Description:**  
`GetHierarchicalPaths("secrets[0]")` must yield `"secrets"` as one of its paths so parent-level sensitivity is checked. This directly enables TC-15.

**Test Steps:**
1. Call `GetHierarchicalPaths("secrets[0]")`.
2. Assert the result contains `"secrets"`.
3. Call `GetHierarchicalPaths("items[3]")`.
4. Assert the result contains `"items"`.

---

### TC-18: `GetHierarchicalPaths` handles nested dotted+indexed path correctly (regression)

**Type:** Unit  
**Test class:** `SensitivityHierarchyTests`  
**Method name:** `GetHierarchicalPaths_NestedDottedAndIndexed_EmitsAllParents`

**Description:**  
For a key like `a[0].b[1]`, `GetHierarchicalPaths` should emit: `a[0].b[1]`, `a[0].b`, `a[0]`, `a`. This verifies no regression in existing dotted path logic.

**Test Steps:**
1. Call `GetHierarchicalPaths("a[0].b[1]")`.
2. Assert result contains `"a[0].b[1]"`, `"a[0].b"`, `"a[0]"`, `"a"`.

---

### TC-19: `IsSensitiveAttribute` detects empty-string root key in flat dict

**Type:** Unit  
**Test class:** `SensitivityHierarchyTests`  
**Method name:** `IsSensitiveAttribute_EmptyStringRoot_DetectedAsGlobalSensitivity`

**Description:**  
`IsSensitiveAttribute` must explicitly check for an empty-string key `""` in either sensitivity dictionary before checking hierarchical paths.

**Test Steps:**
1. Build `afterSensitiveDict = {"": "true"}`.
2. Assert `IsSensitiveAttribute("anything", [], {"": "true"}) == true`.
3. Assert `IsSensitiveAttribute("nested.deep.key", [], {"": "true"}) == true`.
4. Assert `IsSensitiveAttribute("arr[0]", [], {"": "true"}) == true`.

---

### TC-20: Existing sensitive attributes remain masked after refactoring (regression guard)

**Type:** Unit (existing tests)  
**Test class:** `ReportModelBuilderTests`  
**Method name:** Existing `ReportModelBuilder_SensitiveArrayAttribute_AttributesMasked` (and peers)

**Description:**  
Verify that the fixes do not break existing sensitivity behavior for dotted paths.  
These tests should PASS both before and after the fix.

**Test Steps:**
Run existing tests:
- Array attributes with dotted+indexed paths.
- `showSensitive = true` reveals values.

---

### TC-21: Non-sensitive attributes are NOT over-masked (regression guard)

**Type:** Snapshot / direct assertion  
**Test class:** `AzapiSnapshotTests`  
**Method name:** `Snapshot_AzapiCreate_NonSensitiveValues_NotMasked`

**Description:**  
After the fix, non-sensitive body properties must still render in plaintext.

**Preconditions:** `TestData/azapi-sensitive-plan.json` — `version`, `publicNetworkAccess`, and `administratorLogin` are not marked sensitive.

**Test Steps:**
1. Render `azapi-sensitive-plan.json`.
2. Assert `sqladmin` is present (it is NOT sensitive).
3. Assert `12.0` is present.
4. Assert `Enabled` is present.

---

## Test Data Requirements

| File | Description | Action |
|---|---|---|
| `TestData/azapi-sensitive-plan.json` | AzApi create with `administratorLoginPassword` sensitive | **Exists** — no change needed |
| `TestData/azapi-body-sensitive-plan.json` | AzApi create with entire `body` sensitive | **Exists** — no change needed |
| `TestData/azapi-delete-sensitive-plan.json` | AzApi delete with sensitive `before.body` field | **New — must be created** |
| `TestData/azapi-replace-sensitive-plan.json` | AzApi replace (delete+create) with sensitive field on both sides | **New — must be created** |
| `TestData/azapi-update-sensitive-plan.json` | AzApi update with sensitive property in `afterSensitive` | **New — must be created** |
| Inline test data | Variable Group `is_secret` transition scenarios | Inline in test class |
| Inline test data | `AotScriptObjectMapper` with sensitive `ResourceChangeModel` | Inline in test class |

### Schema guide for new test data files

**`azapi-delete-sensitive-plan.json`**:
```json
{
  "change": {
    "actions": ["delete"],
    "before": {
      "body": { "properties": { "clientSecret": "actual-secret", "sku": "standard" } }
    },
    "after": null,
    "before_sensitive": { "body": { "properties": { "clientSecret": true } } },
    "after_sensitive": {}
  }
}
```

**`azapi-replace-sensitive-plan.json`**:
```json
{
  "change": {
    "actions": ["delete", "create"],
    "before": { "body": { "properties": { "secret": "old-secret" } } },
    "after":  { "body": { "properties": { "secret": "new-secret" } } },
    "before_sensitive": { "body": { "properties": { "secret": true } } },
    "after_sensitive":  { "body": { "properties": { "secret": true } } }
  }
}
```

**`azapi-update-sensitive-plan.json`**:
```json
{
  "change": {
    "actions": ["update"],
    "before": { "body": { "properties": { "clientSecret": "old-secret", "name": "keep" } } },
    "after":  { "body": { "properties": { "clientSecret": "new-secret", "name": "keep" } } },
    "before_sensitive": { "body": { "properties": { "clientSecret": true } } },
    "after_sensitive":  { "body": { "properties": { "clientSecret": true } } }
  }
}
```

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|---|---|---|
| `after_sensitive: true` (root boolean) | All attributes masked | TC-13, TC-19 |
| `before_sensitive: true` (root boolean) | All attributes masked | TC-14, TC-19 |
| `secrets[0]` with parent `secrets: true` | `secrets[0]` masked | TC-15, TC-17 |
| `secrets[5]` with parent `secrets: true` | `secrets[5]` masked | TC-16 |
| `a[0].b[1]` with parent `a` sensitive | Nested key masked | TC-18 |
| Variable `is_secret: true → false` | Before-value masked | TC-11 |
| Variable `is_secret: false → true` | Both values masked | TC-12 |
| AzApi create, body entirely sensitive | All body props masked | TC-02 |
| AzApi delete, `before` property sensitive | Before-value masked | TC-03 |
| AzApi replace, both sides sensitive | Both sections masked | TC-04 |
| `--show-sensitive` in AzApi create | Plaintext revealed | TC-05 |
| `--show-sensitive` in AzApi update | Plaintext revealed | TC-07 |
| Scriban context: `before_sensitive` key | Present and structured | TC-08 |
| Scriban context: `after_json` pre-masked | Sensitive leaves show `(sensitive)` | TC-10 |
| Non-sensitive attrs not masked | Plaintext preserved | TC-21 |

---

## Snapshot Baseline Updates

The following existing snapshot baselines **encode broken behavior** and must be regenerated as part of the fix (using `scripts/update-test-snapshots.sh`):

| Snapshot file | Current broken content | Expected correct content |
|---|---|---|
| `TestData/Snapshots/azapi-sensitive.md` | `administratorLoginPassword \| \`P@ssw0rd123!\`` | `administratorLoginPassword \| \`(sensitive)\`` |
| `TestData/Snapshots/azapi-body-sensitive.md` | `tenantId \| \`12345678-...\`` etc. | All body rows show `(sensitive)` |

These will be the visible indicators that the fix is working when running `scripts/update-test-snapshots.sh`.

---

## Suggested Test Class Organization

| New/updated class | Location | Covers |
|---|---|---|
| `SensitivityHierarchyTests` (new) | `MarkdownGeneration/` | TC-13 – TC-19 (`GetHierarchicalPaths`, `IsSensitiveAttribute`) |
| `AzApiSensitiveMaskingTests` (new) | `Providers/AzApi/` | TC-01 – TC-05 (create/delete/replace direct assertions) |
| `ScribanHelpersAzApiUpdateRenderingTests` (extend) | `Providers/AzApi/` | TC-06, TC-07 |
| `AotScriptObjectMapperTests` (new) | `MarkdownGeneration/` | TC-08 – TC-10 |
| `VariableGroupViewModelFactoryTests` (extend) | `Providers/AzureDevOps/` | TC-11, TC-12 |
| `AzapiSnapshotTests` (update baselines) | `MarkdownGeneration/` | TC-21 (regression), snapshot baseline updates |
| `ReportModelBuilderTests` (extend) | `MarkdownGeneration/` | TC-13, TC-14 (integration-level root boolean) |

---

## Non-Functional Tests

- All masking tests must pass without any network calls or file I/O beyond test data.
- No test may require manual intervention.
- All tests must execute via `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`.

---

## Open Questions

None — all acceptance criteria are derived directly from the confirmed bug report in `analysis.md` and cross-referenced with the current source code.
