# Terraform Plan Report

**Terraform Version:** 1.14.0

## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 3 | 3 azurerm_role_assignment |
| 🔄 Change | 1 | 1 azurerm_role_assignment |
| ♻️ Replace | 1 | 1 azurerm_role_assignment |
| ❌ Destroy | 1 | 1 azurerm_role_assignment |
| **Total** | **6** | |

## Resource Changes

### Module: `module.security`

#### ➕ azurerm_role_assignment.create_no_description

**Summary:** `11111111-1111-1111-1111-111111111111` (User) → `Reader` on `rg-tfplan2md-demo`

<details>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `sub-one` |
| role_definition_id | `Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |
| principal_id | `11111111-1111-1111-1111-111111111111` (User) [`11111111-1111-1111-1111-111111111111`] |
| principal_type | `User` |
| name | `ra-create` |

</details>

#### ➕ azurerm_role_assignment.create_with_description

**Summary:** `22222222-2222-2222-2222-222222222222` (Group) → `Storage Blob Data Reader` on Storage Account `sttfplan2mdlogs-with-extended-name-1234567890`

Allow DevOps team to read logs from the storage account

<details>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `sub-one` |
| role_definition_id | `Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) |
| principal_id | `22222222-2222-2222-2222-222222222222` (Group) [`22222222-2222-2222-2222-222222222222`] |
| principal_type | `Group` |
| name | `ra-storage-reader` |
| description | `Allow DevOps team to read logs from the storage account` |

</details>

#### 🔄 azurerm_role_assignment.update_assignment

**Summary:** `33333333-3333-3333-3333-333333333333` (Group) → `Storage Blob Data Contributor` on Storage Account `sttfplan2mddata`

Upgraded permissions for security auditing

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `sub-one` | `rg-tfplan2md-demo` in subscription `sub-one` |
| role_definition_id | `Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) | `Storage Blob Data Contributor` (`ba92f5b4-2d11-453d-a403-e96b0029c9fe`) |
| principal_id | `22222222-2222-2222-2222-222222222222` (Group) [`22222222-2222-2222-2222-222222222222`] | `33333333-3333-3333-3333-333333333333` (Group) [`33333333-3333-3333-3333-333333333333`] |
| description | `Allow team to read storage data` | `Upgraded permissions for security auditing` |
| condition | - | `request.clientip != '10.0.0.0/24'` |
| skip_service_principal_aad_check | `false` | `true` |

</details>

#### ♻️ azurerm_role_assignment.replace_assignment

**Summary:** recreate as `33333333-3333-3333-3333-333333333333` (Group) → `Custom Contributor Long Name 1234567890` on `rg-production`

Updated role assignment with new permissions

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| role_definition_id | `Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) | `Custom Contributor Long Name 1234567890` |
| principal_id | `22222222-2222-2222-2222-222222222222` (Group) [`22222222-2222-2222-2222-222222222222`] | `33333333-3333-3333-3333-333333333333` (Group) [`33333333-3333-3333-3333-333333333333`] |
| description | `Read-only access for DevOps` | `Updated role assignment with new permissions` |

</details>

#### ❌ azurerm_role_assignment.delete_assignment

**Summary:** remove `Contributor` on `rg-legacy` from User `33333333-3333-3333-3333-333333333333`

Legacy access

<details>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-legacy` in subscription `sub-three` |
| role_definition_id | `Contributor` (`b24988ac-6180-42a0-ab88-20f7382dd24c`) |
| principal_id | `33333333-3333-3333-3333-333333333333` (User) [`33333333-3333-3333-3333-333333333333`] |
| principal_type | `User` |
| description | `Legacy access` |

</details>

#### ➕ azurerm_role_assignment.unmapped_principal

**Summary:** `99999999-9999-9999-9999-999999999999` (ServicePrincipal) → `Extremely Verbose Custom Role Name For Long Output Validation 1234567890` on `rg-long-names-example`

<details>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-long-names-example` in subscription `sub-four` |
| principal_id | `99999999-9999-9999-9999-999999999999` (ServicePrincipal) [`99999999-9999-9999-9999-999999999999`] |
| principal_type | `ServicePrincipal` |
| name | `unmapped` |

</details>
