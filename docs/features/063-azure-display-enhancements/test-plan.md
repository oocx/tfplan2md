# Test Plan: Azure Display Enhancements

## Overview

This test plan covers the enhancements to Azure resource display across multiple providers (azurerm, azapi, azuread, azdevops). It verifies universal resource ID detection, enrichment of IDs with display names (subscriptions, management groups, tenants, roles), and resource-specific summary improvements.

Reference: [specification.md](specification.md), [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Universal Azure Resource ID Detection (all attributes) | TC-01 | Integration |
| Subscription Display Names mapping | TC-02 | Unit |
| Subscription name enrichment in scoped resource names | TC-03 | Unit |
| Management Group Display Names mapping | TC-04 | Unit |
| Root Management Group formatting ("Tenant 'X' root") | TC-05 | Unit |
| Built-in Azure Role recognition by GUID | TC-06 | Unit |
| Custom Role resolution from mapping | TC-07 | Unit |
| Custom Role override of built-in roles | TC-08 | Unit |
| `azurerm_private_dns_a_record` summary (`name.zone`) | TC-09 | Snapshot |
| `azurerm_pim_eligible_role_assignment` summary | TC-10 | Snapshot |
| `azurerm_role_management_policy` summary | TC-11 | Snapshot |
| `role_definition_id` attribute display | TC-12 | Unit |
| Raw ID fallback for unmapped entities | TC-13 | Unit |
| Debug output failure tracking and context | TC-14 | Unit |
| Backward compatibility with old mapping files | TC-15 | Unit |
| `AzureMappingFileLoader` parsing (array of objects) | TC-16 | Unit |
| Azure CLI script validation (Automated) | TC-17 | Scripted |

## User Acceptance Scenarios

> **Purpose**: For user-facing features (especially rendering changes), define scenarios for manual Maintainer review via Test PRs in GitHub and Azure DevOps. These help catch rendering bugs and validate real-world usage before merge.

### Scenario 1: Subscription and Management Group Display

**User Goal**: Quickly identify which subscription and management group a resource belongs to without looking up GUIDs.

**Test PR Context**:
- **GitHub**: Verify in PR comments.
- **Azure DevOps**: Verify in PR description.

**Expected Output**:
- Subscription IDs in complex resource names (e.g., Key Vault) should appear as `DisplayName (ID)`.
- Management group IDs in attribute tables and headers should appear as their `DisplayName`.
- The root management group should be formatted as "Tenant `<tenant_name>` root".

**Success Criteria**:
- [ ] Subscriptions render as `Production (d1828a48-...)`
- [ ] Management groups render as `Cloud Infrastructure` (instead of `mg-cloud`)
- [ ] Root MG renders as `Tenant 'Contoso' root`

---

### Scenario 2: Role and PIM Policy Summaries

**User Goal**: Understand permissions and governance policies through descriptive summaries.

**Test PR Context**:
- **GitHub**: Verify in PR comments.
- **Azure DevOps**: Verify in PR description.

**Expected Output**:
- `azurerm_pim_eligible_role_assignment`: "Assign `Owner` to `John Doe`"
- `azurerm_role_management_policy`: "`Contributor` in resource group `app-rg` of subscription `Development (abc-...)`"
- Built-in roles like `Owner` or `Contributor` should appear as their names, not GUIDs.

**Success Criteria**:
- [ ] Summaries are concise and accurate.
- [ ] Principal and role names match the provided mapping.
- [ ] Hierarchy (RSG, Subscription) is correctly resolved.

## Test Cases

### TC-01: UniversalAzureResourceIdDetection_AnyAttribute_FormatsAsResource

**Type:** Integration

**Description:**
Verify that any attribute value matching the Azure resource ID regex is formatted by `AzureResourceIdFormatter`, regardless of whether it's a known attribute.

**Preconditions:**
- A JSON plan containing an unknown attribute (e.g., `some_custom_prop`) with an Azure resource ID value.

**Expected Result:**
The output markdown shows the formatted (shortened/readable) version of the ID.

---

### TC-02: AzureEntityMapper_SubscriptionId_ResolvesToDisplayName

**Type:** Unit

**Description:**
Verify that `AzureEntityMapper.GetSubscriptionName` returns `DisplayName (Id)` when a mapping exists.

**Test Data:**
Mapping: `{ "id": "123...", "displayName": "Prod" }`
Input: `123...`
Expected: `Prod (123...)`

---

### TC-03: EnrichedAzureScopeFormatter_ResourceScope_IncludesSubscriptionName

**Type:** Unit

**Description:**
Verify that `EnrichedAzureScopeFormatter` injects the subscription display name into the standard resource scope string.

**Expected Result:**
"resource group `rg1` of subscription `Prod (123...)`" instead of "resource group `rg1` of subscription `123...`"

---

### TC-04: AzureEntityMapper_ManagementGroupId_ResolvesToDisplayName

**Type:** Unit

**Description:**
Verify that management group IDs are replaced by their display names.

---

### TC-05: EnrichedAzureScopeFormatter_RootManagementGroup_FormatsCorrectly

**Type:** Unit

**Description:**
Verify that a management group ID that matches the tenant ID in mappings is formatted as "Tenant `Name` root".

---

### TC-06: AzureRoleDefinitionMapper_BuiltInRoleGuid_ReturnsName

**Type:** Unit

**Description:**
Verify that `8e3af657-a8ff-443c-a75c-2fe8c4bcb635` resolves to `Owner`.

---

### TC-07: AzureRoleDefinitionMapper_CustomRoleGuid_ReturnsMappedName

**Type:** Unit

**Description:**
Verify that a GUID in the `roles` section of the mapping file is correctly resolved.

---

### TC-08: AzureRoleDefinitionMapper_CustomRole_OverridesBuiltIn

**Type:** Unit

**Description:**
Verify that if the `Owner` GUID is provided in the `roles` mapping with a different name (e.g., "Full Owner"), the mapping takes precedence.

---

### TC-09: ResourceSummaryBuilder_PrivateDnsARecord_ShowsFqdn

**Type:** Snapshot

**Description:**
Verify `azurerm_private_dns_a_record` summary shows `name.zone_name`.

---

### TC-10: PimEligibleRoleAssignmentFactory_Summary_ResolvesNames

**Type:** Unit/Snapshot

**Description:**
Verify that it produces "Assign `<role>` to `<principal>`" with resolved names.

---

### TC-11: RoleManagementPolicyFactory_Summary_ResolvesNamesAndScope

**Type:** Unit/Snapshot

**Description:**
Verify that it produces "`<role>` in `<scope>`" with resolved names and enriched scope.

---

### TC-12: RoleDefinitionFormatter_RoleAttributes_FormatsAsNames

**Type:** Unit

**Description:**
Verify `role_definition_id` and `role_definition_resource_id` attributes are formatted using role names.

---

### TC-13: Fallback_UnmappedIds_ShowsRawValue

**Type:** Unit

**Description:**
Verify that subscriptions, roles, and management groups without mappings fall back to their raw IDs (or raw resource IDs).

---

### TC-14: DiagnosticContext_FailedMappings_TracksWithContext

**Type:** Unit

**Description:**
Verify that when resolution fails, `DiagnosticContext` records the ID, the type (Subscription/Role/MG), and the resource name that referenced it.

---

### TC-15: AzureMappingFileLoader_BackwardCompatibility_LoadsOldStylePrincipals

**Type:** Unit

**Description:**
Verify that a mapping file with only `users` (as a dictionary) still loads correctly.

---

### TC-16: AzureMappingFileLoader_NewSections_LoadsArrayOfObjects

**Type:** Unit

**Description:**
Verify that `subscriptions`, `managementGroups`, `tenants`, and `roles` sections using the `[{"id": "...", "displayName": "..."}]` format are parsed correctly.

## Test Data Requirements

- `azure-display-enhancements.json`: A comprehensive terraform plan containing:
    - `azurerm_private_dns_a_record`
    - `azurerm_pim_eligible_role_assignment`
    - `azurerm_role_management_policy`
    - Multiple `azurerm_role_assignment` resources
    - `azapi` resources with embedded Azure IDs in various attributes
- `azure-mappings-extended.json`: A mapping file containing all new sections.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty mapping file | All IDs fall back to raw display | TC-13 |
| Duplicate IDs in mapping | Last one wins (or as per JSON parser) | TC-16 |
| Case sensitivity in IDs | Resource IDs should be case-insensitive matched | TC-02 |
| Invalid GUIDs in role mapping | Ignored or logged as warning | TC-16 |

## Non-Functional Tests

- **Performance**: Ensure that loading a large mapping file (1000+ entries) does not noticeably slow down report generation to more than 2-3 seconds.
- **Error Handling**: Missing mapping file should not crash the tool (already handled, but verify no regressions).

### TC-17: AzureCLIScript_PopulateMappings_OutputValidation

**Type:** Scripted (Automated)

**Description:**
Verify that the Azure CLI commands or scripts provided in the documentation (README.md/docs) are correct and produce JSON compatible with `AzureMappingFileLoader`. This is automated via a shell script that runs the commands against a mock `az` CLI.

**Test Steps:**
1. Run `scripts/validate-azure-cli-commands.sh`.
2. The script extracts commands from documentation, executes them using a mock `az` wrapper that returns predefined JSON structures.
3. Compare produced JSON against the expected schema/mappings.
4. Verify `tfplan2md` loads the generated output without errors.

**Expected Result:**
Commands are syntactically correct for the documented Azure CLI version and produce valid JSON compatible with the extended mapping schema.
