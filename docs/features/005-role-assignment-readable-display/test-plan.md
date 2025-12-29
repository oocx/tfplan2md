# Test Plan: Enhanced Azure Role Assignment Display

## Overview

This test plan covers the "Enhanced Azure Role Assignment Display" feature, which aims to make `azurerm_role_assignment` resources in the markdown report more readable. It includes testing the mapping of Azure Role GUIDs to names, parsing of scope strings, and optional user-provided principal ID mapping.

Reference: [Specification](specification.md), [Architecture](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Built-in Azure role definition GUIDs are mapped to friendly names | `GetRoleName_KnownGuid_ReturnsNameAndGuid` | Unit |
| Role display format includes both name and GUID | `GetRoleName_KnownGuid_ReturnsNameAndGuid` | Unit |
| Scope paths are parsed and displayed with hierarchical context | `ParseScope_SubscriptionScope_ReturnsFormattedString` <br> `ParseScope_ResourceGroupScope_ReturnsFormattedString` <br> `ParseScope_ResourceScope_ReturnsFormattedString` | Unit |
| Management group, subscription, resource group, and resource-level scopes are handled | `ParseScope_ManagementGroupScope_ReturnsFormattedString` <br> `ParseScope_SubscriptionScope_ReturnsFormattedString` <br> `ParseScope_ResourceGroupScope_ReturnsFormattedString` <br> `ParseScope_ResourceScope_ReturnsFormattedString` | Unit |
| Resource names are visually distinguished from connecting text | `ParseScope_ResourceGroupScope_ReturnsFormattedString` (check markdown syntax) | Unit |
| Optional `--principal-mapping` CLI argument loads JSON mapping file | `Parse_PrincipalMappingOption_SetsPrincipalMappingFile` | Unit |
| Principal IDs are enhanced when mapping is provided | `GetPrincipalName_MappedId_ReturnsNameAndType` | Unit |
| Principal IDs display raw GUID when no mapping exists | `GetPrincipalName_UnmappedId_ReturnsOriginalId` | Unit |
| Unmapped principals gracefully fall back to GUID display | `GetPrincipalName_UnmappedId_ReturnsOriginalId` | Unit |
| Changes only affect `azurerm_role_assignment` resource type | `Render_RoleAssignment_UsesEnhancedTemplate` | Integration |
| Backward compatibility maintained (existing reports still work) | `Render_RoleAssignment_NoMapping_ReturnsEnhancedDefault` | Integration |

## Test Cases

### Unit Tests: AzureRoleDefinitionMapper

#### TC-01: GetRoleName_KnownGuid_ReturnsNameAndGuid
**Type:** Unit
**Description:** Verifies that a known Azure Role Definition GUID is correctly mapped to its display name.
**Input:** `"/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7"` (Reader)
**Expected Result:** `"Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)"`

#### TC-02: GetRoleName_UnknownGuid_ReturnsOriginalId
**Type:** Unit
**Description:** Verifies that an unknown GUID returns the original ID string.
**Input:** `"/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/unknown-guid"`
**Expected Result:** `"/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/unknown-guid"`

### Unit Tests: AzureScopeParser

#### TC-03: ParseScope_ManagementGroupScope_ReturnsFormattedString
**Type:** Unit
**Description:** Verifies parsing of a Management Group scope.
**Input:** `"/providers/Microsoft.Management/managementGroups/my-mg"`
**Expected Result:** `"**my-mg** (Management Group)"`

#### TC-04: ParseScope_SubscriptionScope_ReturnsFormattedString
**Type:** Unit
**Description:** Verifies parsing of a Subscription scope.
**Input:** `"/subscriptions/12345678-1234-1234-1234-123456789012"`
**Expected Result:** `"subscription **12345678-1234-1234-1234-123456789012**"`

#### TC-05: ParseScope_ResourceGroupScope_ReturnsFormattedString
**Type:** Unit
**Description:** Verifies parsing of a Resource Group scope.
**Input:** `"/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/my-rg"`
**Expected Result:** `"**my-rg** in subscription **12345678-1234-1234-1234-123456789012**"`

#### TC-06: ParseScope_ResourceScope_ReturnsFormattedString
**Type:** Unit
**Description:** Verifies parsing of a specific Resource scope.
**Input:** `"/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.KeyVault/vaults/my-kv"`
**Expected Result:** `"Key Vault **my-kv** in resource group **my-rg** of subscription **sub-id**"`

### Unit Tests: PrincipalMapper

#### TC-07: GetPrincipalName_MappedId_ReturnsNameAndType
**Type:** Unit
**Description:** Verifies that a principal ID present in the mapping file is correctly resolved.
**Preconditions:** Mapping loaded with `{"abc-123": "John Doe (User)"}`.
**Input:** `"abc-123"`
**Expected Result:** `"John Doe (User) [abc-123]"`

#### TC-08: GetPrincipalName_UnmappedId_ReturnsOriginalId
**Type:** Unit
**Description:** Verifies that a principal ID NOT present in the mapping file returns the original ID.
**Input:** `"unmapped-id"`
**Expected Result:** `"unmapped-id"`

### Unit Tests: CLI Parser

#### TC-09: Parse_PrincipalMappingOption_SetsPrincipalMappingFile
**Type:** Unit
**Description:** Verifies that the `--principal-mapping` argument is correctly parsed.
**Input:** `["--principal-mapping", "principals.json"]`
**Expected Result:** `options.PrincipalMappingFile` should be `"principals.json"`.

### Integration Tests: Markdown Rendering

#### TC-10: Render_RoleAssignment_UsesEnhancedTemplate
**Type:** Integration
**Description:** Verifies that an `azurerm_role_assignment` resource is rendered using the new template and helpers.
**Preconditions:**
- `PrincipalMapper` configured with a mock mapping.
- `AzureRoleDefinitionMapper` and `AzureScopeParser` available.
**Input:** A `ReportModel` containing an `azurerm_role_assignment`.
**Expected Result:** Markdown output contains readable Role Name, formatted Scope, and mapped Principal Name.

### Integration Tests: Docker/CLI

#### TC-11: Docker_Run_WithPrincipalMapping_GeneratesReport
**Type:** Integration (Docker)
**Description:** Verifies the full end-to-end flow using the Docker container and a mapping file.
**Preconditions:**
- `plan.json` with role assignment.
- `principals.json` with mapping.
**Test Steps:**
1. Run docker container mounting both files.
2. Pass `--principal-mapping /workspace/principals.json`.
**Expected Result:** Generated markdown contains mapped principal names.

## Test Data Requirements

- **`principals.json`**: A simple JSON file for testing the mapping feature.
  ```json
  {
    "test-principal-id": "Test User (User)"
  }
  ```
- **`role-assignment-plan.json`**: A Terraform plan JSON containing `azurerm_role_assignment` resources with various scopes and roles.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Malformed `principals.json` | Log warning, continue without mapping | `PrincipalMapper_MalformedJson_LogsWarningAndContinues` |
| Empty `principals.json` | Continue without mapping | `PrincipalMapper_EmptyJson_ReturnsOriginalIds` |
| Scope string with unexpected format | Return original scope string | `ParseScope_InvalidFormat_ReturnsOriginalString` |
| Role Definition ID not in built-in list | Return original ID | `GetRoleName_UnknownGuid_ReturnsOriginalId` |
| Missing `--principal-mapping` argument | Default behavior (raw GUIDs) | `Parse_NoPrincipalMapping_ReturnsNull` |

## Non-Functional Tests

- **Performance**: The `AzureRoleDefinitionMapper` uses `FrozenDictionary` for O(1) lookups, ensuring negligible impact on rendering time.
- **Compatibility**: The feature is purely additive to the markdown generation; existing plans without role assignments are unaffected.

## Open Questions

None.
