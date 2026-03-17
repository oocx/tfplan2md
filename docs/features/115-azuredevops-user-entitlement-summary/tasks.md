# Tasks: Azure DevOps User Entitlement Summary Fields

## Overview

Add a resource summary mapping for `azuredevops_user_entitlement` so that plan reports
display `principal_name`, `account_license_type`, and `licensing_source` in the summary
line — omitting any field that is empty or absent. This is a single-line production code
change backed by four unit tests and one snapshot test.

Reference: `docs/features/115-azuredevops-user-entitlement-summary/specification.md`

---

## Tasks

### Task 1: Add `azuredevops_user_entitlement` mapping entry

**Priority:** High

**Description:**
Add the following entry to `ResourceSummaryMappings.ResourceMappings` in
`src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/ResourceSummaryMappings.cs`,
immediately after the existing `["azuredevops_project"]` line under the
`// azuredevops` section comment:

```csharp
["azuredevops_user_entitlement"] = ["principal_name", "account_license_type", "licensing_source"],
```

No other production-code changes are required — the existing `ResourceSummaryBuilder`
pipeline already handles empty-field suppression and fallback to the resource address.

**Acceptance Criteria:**
- [ ] `ResourceSummaryMappings.ResourceMappings` contains a key `"azuredevops_user_entitlement"` with the value `["principal_name", "account_license_type", "licensing_source"]`.
- [ ] The entry is placed under the `// azuredevops` section, adjacent to `azuredevops_project`.
- [ ] No other production files are modified.
- [ ] The solution still builds without errors or warnings.

**Dependencies:** None

**Notes:**
The Architect confirmed this is the complete and correct implementation (see
`work-protocol.md` Architecture Review section). The `AppendRemainingParts` method
already skips empty/null strings, satisfying the "no visual noise" requirement at
zero additional cost.

---

### Task 2: Add unit tests TC-01 through TC-04

**Priority:** High

**Description:**
Add four new test methods to
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Summaries/ResourceSummaryBuilderTests.cs`
covering all field-population variants for `azuredevops_user_entitlement`. Follow the
existing `CreateChange(...)` helper pattern already used in that file.

**Test methods to add (matching the test-plan names exactly):**

| Method name | Scenario |
|---|---|
| `BuildSummary_AzureDevOpsUserEntitlement_AllFieldsPopulated_ShowsAllThreeFields` | TC-01 |
| `BuildSummary_AzureDevOpsUserEntitlement_LicensingSourceEmpty_OmitsLicensingSource` | TC-02 |
| `BuildSummary_AzureDevOpsUserEntitlement_OnlyPrincipalNamePopulated_ShowsOnlyPrincipalName` | TC-03 |
| `BuildSummary_AzureDevOpsUserEntitlement_AllFieldsEmpty_FallsBackToAddress` | TC-04 |

**Acceptance Criteria:**
- [ ] **TC-01**: Given `afterJson` with all three fields set (`"john.doe@example.com"`, `"express"`, `"msdn"`), the summary contains each value and at least one `" | "` delimiter.
- [ ] **TC-02**: Given `afterJson` with `licensing_source` set to `""`, the summary contains `"jane.smith@example.com"` and `"stakeholder"` but does **not** contain the literal string `"licensing_source"`.
- [ ] **TC-03**: Given `afterJson` with only `"principal_name"` present, the summary contains `"only.user@example.com"` and does **not** contain `"account_license_type"` or `"licensing_source"`.
- [ ] **TC-04**: Given an empty `afterJson` (`{}`), the summary does **not** contain `" | "` (address-only fallback) — or it equals the resource address formatted as a code span.
- [ ] All four tests pass (`dotnet test`).
- [ ] No existing tests are broken.

**Dependencies:** Task 1 (the mapping entry must exist for TC-01 through TC-03 to pass)

**Notes:**
- Use the existing `CreateChange` private helper at the bottom of the file — it accepts
  `type`, `address`, `action`, and `afterJson` keyword arguments.
- Inline JSON strings for test data (no external test-data files needed).
- See `test-plan.md` §TC-01 through §TC-04 for exact assertion details.

---

### Task 3: Add snapshot test data and baseline (TC-05)

**Priority:** Medium

**Description:**
Create two new files to support the snapshot regression test TC-05:

1. **Test-data JSON**
   `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuredevops-user-entitlement-plan.json`
   A minimal Terraform plan JSON containing exactly three `azuredevops_user_entitlement`
   `resource_changes` (each with action `"create"`):
   - Resource 1: all three fields populated (`principal_name`, `account_license_type`, `licensing_source`)
   - Resource 2: `principal_name` and `account_license_type` populated, `licensing_source` absent/empty
   - Resource 3: no mapped fields (empty `after` object) — address-only fallback

2. **Snapshot test method**
   Add a new `[Test]` method in
   `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzureDevOpsSnapshotTests.cs`:

   ```csharp
   [Test]
   public void Snapshot_AzureDevOps_UserEntitlement_MatchesBaseline()
   {
       AssertAzureDevOpsSnapshot(
           "azuredevops-user-entitlement-plan.json",
           "azuredevops-user-entitlement.md");
   }
   ```

3. **Approved snapshot baseline**
   Run the test once (it will fail on first run because the snapshot file does not yet
   exist). Capture the actual rendered output and save it to
   `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azuredevops-user-entitlement.md`.
   Re-run the test — it must pass on the second run.

**Acceptance Criteria:**
- [ ] `TestData/azuredevops-user-entitlement-plan.json` exists and is valid JSON containing the three resource scenarios described above.
- [ ] The snapshot test method `Snapshot_AzureDevOps_UserEntitlement_MatchesBaseline` exists in `AzureDevOpsSnapshotTests.cs`.
- [ ] `TestData/Snapshots/azuredevops-user-entitlement.md` exists and the snapshot test passes.
- [ ] The snapshot output shows all three summary variants: full three-field, two-field, and address-only.
- [ ] The style-guide invariant check (`AssertNoEmojiFollowedByRegularSpace`) passes.
- [ ] No existing snapshot tests are broken.

**Dependencies:** Task 1 (mapping must be in place so the rendered output is correct)

**Notes:**
- Model an existing plan JSON on `TestData/azuredevops-snapshot-plan.json` for structure.
- The `AssertAzureDevOpsSnapshot` helper in `AzureDevOpsSnapshotTests.cs` handles
  snapshot comparison automatically — pass the file names and it does the rest.
- The snapshot update skill (`update-test-snapshots`) is available if the snapshot needs
  regenerating in a later iteration.

---

### Task 4: Verify all tests pass end-to-end

**Priority:** Medium

**Description:**
Run the full test suite to confirm no regressions were introduced and all new tests pass.

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

**Acceptance Criteria:**
- [ ] All pre-existing tests continue to pass.
- [ ] TC-01, TC-02, TC-03, TC-04 pass.
- [ ] TC-05 (snapshot) passes.
- [ ] No build warnings or errors.

**Dependencies:** Tasks 1, 2, and 3

**Notes:**
Use `scripts/test-with-timeout.sh` as the test runner — it handles TUnit's dual test
runner modes and prevents hangs (see `run-dotnet-tests` skill).

---

## Implementation Order

1. **Task 1** — Add the mapping entry (unblocks all test tasks)
2. **Task 2** — Add unit tests TC-01 through TC-04 (validates the core behaviour)
3. **Task 3** — Add snapshot test data, test method, and approved baseline
4. **Task 4** — Full test-suite verification pass

## Open Questions

None. Architecture is confirmed; approach is consistent with existing patterns. See
`work-protocol.md` for the Architect's sign-off.
