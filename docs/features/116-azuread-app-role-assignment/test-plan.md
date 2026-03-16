# Test Plan: Azure AD App Role Assignment Support

## Overview

This test plan covers the `azuread_app_role_assignment` resource support feature, which adds human-readable summary display and GUID-to-name resolution for app role assignments in Terraform plan reports.

**Specification:** `docs/features/116-azuread-app-role-assignment/specification.md`
**Architecture:** `docs/features/116-azuread-app-role-assignment/architecture.md`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `app_role_id` GUIDs resolved to Microsoft Graph permission names | TC-01, TC-02, TC-03, TC-04 | Unit |
| `principal_object_id` resolved using `IPrincipalMapper` | TC-13 | Unit |
| `resource_object_id` resolved using `IPrincipalMapper` | TC-13 | Unit |
| Computed attributes used as fallbacks when mapper lookups fail | TC-14 | Unit |
| Unmapped GUIDs display raw GUID gracefully | TC-02, TC-12 | Unit |
| `AppRoleIdFormatter` formats known app role IDs with icon | TC-05, TC-06, TC-07, TC-08 | Unit |
| Summary format follows established pattern | TC-09 through TC-15 | Unit |
| Resource registered in `AzureADModule` | TC-16 | Integration |
| Summary rendering end-to-end with plan JSON | TC-17 | Integration |
| Backward compatibility maintained | TC-17 | Integration |

## Test Cases

### Component 1: MicrosoftGraphAppRoleResolver

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/MicrosoftGraphAppRoleResolverTests.cs`

Follows the pattern established in `AzureRoleDefinitionResolverTests`.

---

#### TC-01: GetAppRole_KnownGuid_ReturnsMappedRoleInfo

**Type:** Unit

**Description:**
Verifies that a well-known Microsoft Graph app role GUID (e.g., `df021288-bdef-4463-88db-98f22de89214` for `User.Read.All`) resolves to the correct `RoleDefinitionInfo` with Name, Id, and FullName.

**Test Steps:**
1. Create `MicrosoftGraphAppRoleResolver` via `CreateBuiltIn()` factory
2. Call `GetAppRole("df021288-bdef-4463-88db-98f22de89214")`
3. Assert `Name` equals `"User.Read.All"`
4. Assert `Id` equals `"df021288-bdef-4463-88db-98f22de89214"`
5. Assert `FullName` equals `"User.Read.All (df021288-bdef-4463-88db-98f22de89214)"`

**Expected Result:**
All three fields of `RoleDefinitionInfo` populated correctly.

---

#### TC-02: GetAppRole_UnknownGuid_ReturnsGuidAsAllFields

**Type:** Unit

**Description:**
Verifies that an unknown GUID falls back to the raw GUID value for all three fields.

**Test Steps:**
1. Create `MicrosoftGraphAppRoleResolver` via `CreateBuiltIn()` factory
2. Call `GetAppRole("99999999-9999-9999-9999-999999999999")`
3. Assert `Name`, `Id`, and `FullName` all equal `"99999999-9999-9999-9999-999999999999"`

**Expected Result:**
Unmapped GUIDs return the raw GUID as Name, Id, and FullName (same pattern as `AzureRoleDefinitionResolver`).

---

#### TC-03: GetAppRole_NullOrEmptyInput_ReturnsEmptyRoleInfo

**Type:** Unit

**Description:**
Verifies null, empty string, and whitespace-only inputs are handled gracefully.

**Test Steps (parameterized with `[Arguments]`):**
1. Call `GetAppRole(null)`, `GetAppRole("")`, `GetAppRole("  ")`
2. Assert that all three fields return empty or predictable fallback values (following the same pattern as `AzureRoleDefinitionResolver.GetRoleDefinition` with null ID)

**Expected Result:**
No exception thrown; returns a graceful fallback `RoleDefinitionInfo`.

---

#### TC-04: GetAppRole_CaseInsensitiveGuid_ReturnsMappedRoleInfo

**Type:** Unit

**Description:**
Verifies GUID matching is case-insensitive (GUIDs may appear in uppercase or mixed case).

**Test Steps:**
1. Create resolver via `CreateBuiltIn()` factory
2. Call `GetAppRole("DF021288-BDEF-4463-88DB-98F22DE89214")` (uppercase)
3. Assert `Name` equals `"User.Read.All"`

**Expected Result:**
Case-insensitive lookup succeeds, using `OrdinalIgnoreCase` comparer on the `FrozenDictionary`.

---

### Component 2: AppRoleIdFormatter

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AppRoleIdFormatterTests.cs`

Follows the pattern established in `PrincipalIdFormatterTests` and `AzureValueFormatterTests`.

---

#### TC-05: TryFormat_KnownAppRoleId_ReturnsFormattedString

**Type:** Unit

**Description:**
Verifies a known Microsoft Graph app role GUID is formatted with the 🔑 icon.

**Test Steps:**
1. Create `AppRoleIdFormatter` (using default built-in resolver)
2. Create `ServiceResolutionContext("azuread", "azuread_app_role_assignment", "app_role_id", "df021288-bdef-4463-88db-98f22de89214")`
3. Call `TryFormat(context)`
4. Assert result equals the string `` `🔑\u00a0User.Read.All (df021288-bdef-4463-88db-98f22de89214)` `` (literal backticks wrapping icon + non-breaking space + name + GUID, produced by `MarkdownHelpers.FormatCodeTable()`)

**Expected Result:**
Returns the formatted inline-code string: backtick, 🔑, non-breaking space, role name, space, GUID in parens, backtick. This matches the `RoleDefinitionFormatter` output pattern.

---

#### TC-06: TryFormat_UnknownAppRoleId_ReturnsNull

**Type:** Unit

**Description:**
Verifies an unknown GUID returns null, allowing the default raw value display.

**Test Steps:**
1. Create `AppRoleIdFormatter`
2. Create context with unknown GUID `"99999999-9999-9999-9999-999999999999"`
3. Call `TryFormat(context)`
4. Assert result is `null`

**Expected Result:**
Returns null for unknown GUIDs (raw value displayed as fallback).

---

#### TC-07: TryFormat_NullOrEmptyValue_ReturnsNull

**Type:** Unit

**Description:**
Verifies null and empty string values return null.

**Test Steps (parameterized with `[Arguments]`):**
1. Create `AppRoleIdFormatter`
2. Create context with `null`, `""`, and `"  "` values
3. Call `TryFormat(context)` for each
4. Assert all return `null`

**Expected Result:**
No exception; returns null for each.

---

#### TC-08: TryFormat_AnotherKnownAppRoleId_ReturnsFormattedString

**Type:** Unit

**Description:**
Verifies a second well-known GUID (e.g., `7ab1d382-f21e-4acd-a863-ba3e13f7da61` for `Directory.Read.All`) to confirm the built-in mapping covers multiple entries.

**Test Steps:**
1. Create `AppRoleIdFormatter`
2. Create context with `"7ab1d382-f21e-4acd-a863-ba3e13f7da61"`
3. Call `TryFormat(context)`
4. Assert result contains `Directory.Read.All`

**Expected Result:**
Correctly resolves and formats a different well-known app role.

---

### Component 3: AzureAdSummaryBuilder.BuildAppRoleAssignmentSummaryHtml

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdAppRoleAssignmentSummaryTests.cs`

Follows the summary builder test patterns from `AzureAdGroupSummaryRebuilderTests`.

---

#### TC-09: BuildSummaryHtml_AllAttributesMapped_ReturnsFullSummary

**Type:** Unit

**Description:**
Verifies summary HTML when all three GUIDs (app_role_id, principal_object_id, resource_object_id) are fully resolved.

**Preconditions:**
- `IAppRoleResolver` resolves `app_role_id` to `"User.Read.All"`
- `IPrincipalMapper` resolves `principal_object_id` to `"My Service Principal"`
- `IPrincipalMapper` resolves `resource_object_id` to `"Microsoft Graph"`

**Expected Result:**
Summary HTML: `➕\u00A0azuread_app_role_assignment <b><code>example</code></b> — <code>User.Read.All</code> → <code>My Service Principal</code> on <code>Microsoft Graph</code>`

---

#### TC-10: BuildSummaryHtml_NoMappings_DisplaysRawGuids

**Type:** Unit

**Description:**
Verifies summary HTML when no mappings are available — all three components display raw GUIDs.

**Preconditions:**
- Empty `IPrincipalMapper` (no mappings)
- `IAppRoleResolver` with unknown GUID
- No computed attributes in state

**Expected Result:**
Summary HTML shows raw GUIDs in `<code>` tags for role, principal, and resource.

---

#### TC-11: BuildSummaryHtml_DeleteAction_UsesDeleteIcon

**Type:** Unit

**Description:**
Verifies the delete action uses the ❌ icon prefix.

**Expected Result:**
Summary starts with `❌\u00A0azuread_app_role_assignment`.

---

#### TC-12: BuildSummaryHtml_PartialMapping_MixesResolvedAndRaw

**Type:** Unit

**Description:**
Verifies behavior when only some GUIDs are mapped (e.g., app_role_id resolved but principal/resource are not).

**Preconditions:**
- `IAppRoleResolver` resolves `app_role_id` to `"User.Read.All"`
- `IPrincipalMapper` has no mappings
- No computed attributes in state

**Expected Result:**
Summary shows resolved role name but raw GUIDs for principal and resource.

---

#### TC-13: BuildSummaryHtml_PrincipalAndResourceMapped_ShowsDisplayNames

**Type:** Unit

**Description:**
Verifies `principal_object_id` and `resource_object_id` resolution via `IPrincipalMapper`.

**Preconditions:**
- `IPrincipalMapper` maps principal GUID to `"terraform-automation"`
- `IPrincipalMapper` maps resource GUID to `"Microsoft Graph"`

**Expected Result:**
Summary shows `→ <code>terraform-automation</code> on <code>Microsoft Graph</code>`.

---

#### TC-14: BuildSummaryHtml_ComputedAttributeFallbacks_UsesStateValues

**Type:** Unit

**Description:**
Verifies that when `IPrincipalMapper` returns no mapping, the builder falls back to computed attributes from Terraform state (`principal_display_name`, `resource_display_name`).

**Preconditions:**
- Empty `IPrincipalMapper` (no mappings)
- State object contains `principal_display_name: "My SP"` and `resource_display_name: "Microsoft Graph"`

**Expected Result:**
Summary uses computed attribute values: `→ <code>My SP</code> on <code>Microsoft Graph</code>`.

---

#### TC-15: BuildSummaryHtml_MissingAttributes_GracefulHandling

**Type:** Unit

**Description:**
Verifies graceful handling when the resource change model has no `app_role_id`, `principal_object_id`, or `resource_object_id` attributes.

**Expected Result:**
No exception thrown; summary displays with empty or missing components handled gracefully.

---

### Component 4: Integration Tests

---

#### TC-16: AzureADModule_RegistersAppRoleAssignment

**Type:** Integration

**Description:**
Verifies that `AzureADModule` correctly registers the `azuread_app_role_assignment` resource type in the provider registry, including factory, value formatters, and resource renderer.

**Test Steps:**
1. Create `AzureADModule` with default dependencies
2. Register it with a `ProviderRegistry`
3. Verify `azuread_app_role_assignment` is registered as a known resource type
4. Verify the summary factory handles the resource type

**Expected Result:**
Module registration completes without errors; resource type is recognized.

---

#### TC-17: EndToEnd_AppRoleAssignmentPlan_RendersCorrectMarkdown

**Type:** Integration (Snapshot)

**Description:**
End-to-end test using a Terraform plan JSON containing `azuread_app_role_assignment` resources. Verifies the full rendering pipeline produces correct markdown output.

**Test Data:**
- New file: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuread-app-role-assignment-plan.json`
- Contains at least one `azuread_app_role_assignment` create action with:
  - A known `app_role_id` GUID (e.g., `df021288-bdef-4463-88db-98f22de89214`)
  - A `principal_object_id` GUID
  - A `resource_object_id` GUID
  - Computed attributes: `principal_display_name`, `resource_display_name`

**Expected Result:**
- Summary line rendered with resolved app role name
- Detail table shows formatted `app_role_id` with 🔑 icon
- Computed attributes displayed as-is
- No rendering errors or missing sections

---

## Test Data Requirements

| File | Description |
|------|-------------|
| `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuread-app-role-assignment-plan.json` | Terraform plan JSON containing `azuread_app_role_assignment` resource changes. Must include: a create action with known `app_role_id` GUID (`df021288-bdef-4463-88db-98f22de89214`), computed attributes (`principal_display_name`, `resource_display_name`), and at least one delete action for action-icon coverage. |

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Null/empty `app_role_id` | Graceful fallback, no exception | TC-03, TC-07 |
| Unknown GUID (not in built-in mapping) | Raw GUID displayed | TC-02, TC-06 |
| Uppercase/mixed-case GUID | Case-insensitive match succeeds | TC-04 |
| All attributes missing from resource change | No exception; summary renders without resolution | TC-15 |
| Mapper returns no mapping but state has computed attributes | Falls back to computed values | TC-14 |
| Partial mappings (some resolved, some not) | Mix of resolved names and raw GUIDs | TC-12 |

## Non-Functional Tests

### Immutability
The resolver should use `FrozenDictionary` and hold no mutable static state. A structural test (following `AzureRoleDefinitionResolver_HasNoMutableStaticFields`) verifies this.

### Backward Compatibility
TC-17 (end-to-end) ensures existing resource types continue to render correctly when the new module registration is active. The integration test plan JSON should also include non-`azuread_app_role_assignment` resources to verify no side effects.

## Open Questions

None — the specification and architecture provide sufficient detail for all test cases.
