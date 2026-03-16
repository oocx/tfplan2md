# Test Plan: Azure DevOps User Entitlement Summary Fields

## Overview

This test plan covers the single-line addition of `azuredevops_user_entitlement` to
`ResourceSummaryMappings.ResourceMappings` with keys `["principal_name",
"account_license_type", "licensing_source"]`.

All test cases are fully automated and run via:
```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

Reference: `docs/features/048-azuredevops-user-entitlement-summary/specification.md`

---

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---|---|---|
| Mapping entry exists in `ResourceMappings` for `azuredevops_user_entitlement` | TC-01 | Unit |
| Create summary with all three fields shows all three values separated by ` \| ` | TC-01 | Unit |
| Create summary with `licensing_source` empty omits that field | TC-02 | Unit |
| Create summary with only `principal_name` populated shows only that value | TC-03 | Unit |
| Create summary with all three fields empty falls back to resource address | TC-04 | Unit |
| Full plan render including `azuredevops_user_entitlement` matches snapshot | TC-05 | Snapshot |

---

## Test Cases

### TC-01: BuildSummary_AzureDevOpsUserEntitlement_AllFieldsPopulated_ShowsAllThreeFields

**Type:** Unit — `ResourceSummaryBuilderTests.cs`

**Acceptance Criterion Covered:**
- Mapping entry exists for `azuredevops_user_entitlement`
- All three fields appear in output separated by ` | `

**Description:**
Creates a `ResourceChangeModel` with `type = "azuredevops_user_entitlement"`, action
`"create"`, and `afterJson` containing all three mapped attributes with non-empty values.
Calls `ResourceSummaryBuilder.BuildSummary` and asserts that all three attribute values
appear in the result, delimited by ` | `.

**Preconditions:**
- `ResourceSummaryMappings.ResourceMappings` must contain the `azuredevops_user_entitlement`
  entry (the mapping under test).

**Test Steps:**
1. Create a `ResourceChangeModel`:
   - `type = "azuredevops_user_entitlement"`
   - `action = "create"`
   - `afterJson = { "principal_name": "john.doe@example.com", "account_license_type": "express", "licensing_source": "msdn" }`
   - `Address = "azuredevops_user_entitlement.john"`
2. Call `_builder.BuildSummary(change)`.
3. Assert the result **contains** `"john.doe@example.com"`.
4. Assert the result **contains** `"express"`.
5. Assert the result **contains** `"msdn"`.
6. Assert the result **contains** `" | "` (standard field delimiter).

**Expected Result:**
Summary includes all three field values, e.g.:
```
`azuredevops_user_entitlement.john` | `john.doe@example.com` | `express` | `msdn`
```

**Test Data:** Inline JSON in test method.

---

### TC-02: BuildSummary_AzureDevOpsUserEntitlement_LicensingSourceEmpty_OmitsLicensingSource

**Type:** Unit — `ResourceSummaryBuilderTests.cs`

**Acceptance Criterion Covered:**
- Empty/null field is omitted from summary.

**Description:**
Creates a change with `principal_name` and `account_license_type` populated but
`licensing_source` set to an empty string. Asserts that `licensing_source` does not
appear in the output and that the two non-empty fields do appear.

**Preconditions:** Same as TC-01.

**Test Steps:**
1. Create a `ResourceChangeModel`:
   - `type = "azuredevops_user_entitlement"`
   - `action = "create"`
   - `afterJson = { "principal_name": "jane.smith@example.com", "account_license_type": "stakeholder", "licensing_source": "" }`
2. Call `_builder.BuildSummary(change)`.
3. Assert the result **contains** `"jane.smith@example.com"`.
4. Assert the result **contains** `"stakeholder"`.
5. Assert the result **does not contain** `"licensing_source"`.

**Expected Result:**
Summary includes only two field values with no trailing ` | `, e.g.:
```
`azuredevops_user_entitlement.jane` | `jane.smith@example.com` | `stakeholder`
```

**Test Data:** Inline JSON in test method.

---

### TC-03: BuildSummary_AzureDevOpsUserEntitlement_OnlyPrincipalNamePopulated_ShowsOnlyPrincipalName

**Type:** Unit — `ResourceSummaryBuilderTests.cs`

**Acceptance Criterion Covered:**
- Only non-empty fields are shown; missing/empty fields are silently skipped.

**Description:**
Creates a change where only `principal_name` has a value; the other two fields are absent
or empty. Asserts that only `principal_name` appears beyond the resource address.

**Preconditions:** Same as TC-01.

**Test Steps:**
1. Create a `ResourceChangeModel`:
   - `type = "azuredevops_user_entitlement"`
   - `action = "create"`
   - `afterJson = { "principal_name": "only.user@example.com" }`
2. Call `_builder.BuildSummary(change)`.
3. Assert the result **contains** `"only.user@example.com"`.
4. Assert the result **does not contain** `"account_license_type"`.
5. Assert the result **does not contain** `"licensing_source"`.

**Expected Result:**
Summary includes only the principal name after the address, e.g.:
```
`azuredevops_user_entitlement.only` | `only.user@example.com`
```

**Test Data:** Inline JSON in test method.

---

### TC-04: BuildSummary_AzureDevOpsUserEntitlement_AllFieldsEmpty_FallsBackToAddress

**Type:** Unit — `ResourceSummaryBuilderTests.cs`

**Acceptance Criterion Covered:**
- No visual noise when all three fields are absent; resource address is used as-is.

**Description:**
Creates a change where `afterJson` contains none of the three mapped attributes
(or all three are explicitly empty strings). Asserts that the returned summary
equals the resource address formatted as a code span (existing fallback behaviour).

**Preconditions:** Same as TC-01.

**Test Steps:**
1. Create a `ResourceChangeModel`:
   - `type = "azuredevops_user_entitlement"`
   - `action = "create"`
   - `afterJson = { }`
   - `Address = "azuredevops_user_entitlement.empty"`
2. Call `_builder.BuildSummary(change)`.
3. Assert the result **equals** `` `azuredevops_user_entitlement.empty` `` (address as code span)
   OR assert it does not contain `" | "` (no field delimiter present).

**Expected Result:**
Only the resource address appears in the summary with no ` | ` delimited fields.

**Test Data:** Inline JSON in test method.

---

### TC-05: Snapshot_AzureDevOps_UserEntitlement_MatchesBaseline

**Type:** Snapshot — `AzureDevOpsSnapshotTests.cs`

**Acceptance Criterion Covered:**
- End-to-end rendering with the new mapping produces stable, correct markdown output.
- No regression in existing Azure DevOps snapshot output.

**Description:**
Parses a dedicated test data file containing `azuredevops_user_entitlement` resources
with varied field populations. Renders the full markdown report and compares against
an approved snapshot file. Failing the snapshot means unintended output changes.

**Preconditions:**
- Test data file `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuredevops-user-entitlement-plan.json` must exist.
- Approved snapshot file `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azuredevops-user-entitlement.md` must exist (generated from the first passing run).

**Test Steps:**
1. Parse `TestData/azuredevops-user-entitlement-plan.json` via `TerraformPlanParser`.
2. Build a `ReportModel` using `ReportModelBuilder` with Azure DevOps provider registry.
3. Render to markdown via `MarkdownRenderer`.
4. Assert the output matches `TestData/Snapshots/azuredevops-user-entitlement.md`.
5. Assert no emoji immediately followed by a regular space (style-guide invariant).

**Expected Result:**
Rendered markdown matches the approved snapshot including all three summary scenarios:
- Resource with all three fields.
- Resource with `licensing_source` absent.
- Resource with all fields absent (address fallback).

**Test Data:**
New file: `TestData/azuredevops-user-entitlement-plan.json`

The plan must include at least three `azuredevops_user_entitlement` resource_changes:
1. A **create** action with `principal_name`, `account_license_type`, and `licensing_source` all populated.
2. A **create** action with `principal_name` and `account_license_type` populated, `licensing_source` empty/absent.
3. A **create** action with no mapped fields (empty `after` object) to exercise the address fallback.

---

## Test Data Requirements

| File | Location | Description |
|---|---|---|
| `azuredevops-user-entitlement-plan.json` | `src/tests/Oocx.TfPlan2Md.TUnit/TestData/` | Terraform plan JSON with three `azuredevops_user_entitlement` resources covering all field-population variants |
| `azuredevops-user-entitlement.md` | `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/` | Approved snapshot output; generated on first passing run |

---

## Edge Cases

| Scenario | Expected Behaviour | Test Case |
|---|---|---|
| All three fields populated | All three values in summary, ` \| ` separated | TC-01 |
| `licensing_source` empty string | Only `principal_name` and `account_license_type` shown | TC-02 |
| `licensing_source` key absent entirely | Same as empty string — skipped silently | TC-02 (variant) |
| Only `principal_name` present | Only `principal_name` shown after address | TC-03 |
| All three fields absent/empty | Resource address only, no ` \| ` delimiter | TC-04 |
| Full plan render | Stable markdown output; snapshot regression guard | TC-05 |

---

## Non-Functional Tests

No performance, compatibility, or CLI interface changes are introduced by this feature.
The existing `AppendRemainingParts` null/empty skip logic is already unit-tested in
the `ResourceSummaryBuilder` test suite for other resource types.

---

## Open Questions

None. The approach is confirmed in the Architecture Review (see `work-protocol.md`).
