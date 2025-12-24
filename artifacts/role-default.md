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

**Summary:** `ra-create`

<details>

| Attribute | Value |
| ----------- | ------- |
| `name` | ra-create |
| `principal_id` | 11111111-1111-1111-1111-111111111111 |
| `principal_type` | User |
| `scope` | /subscriptions/sub-one/resourceGroups/rg-tfplan2md-demo |

</details>

<details>
<summary>Large values: role_definition_id (2 lines, 2 changed)</summary>

### `role_definition_id`

```
/subscriptions/sub-one/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7
```

</details>

#### ➕ azurerm_role_assignment.create_with_description

**Summary:** `ra-storage-reader`

<details>

| Attribute | Value |
| ----------- | ------- |
| `description` | Allow DevOps team to read logs from the storage account |
| `name` | ra-storage-reader |
| `principal_id` | 22222222-2222-2222-2222-222222222222 |
| `principal_type` | Group |

</details>

<details>
<summary>Large values: role_definition_id (2 lines, 2 changed), scope (2 lines, 2 changed)</summary>

### `role_definition_id`

```
/subscriptions/sub-one/providers/Microsoft.Authorization/roleDefinitions/2a2b9908-6ea1-4ae2-8e65-a410df84e7d1
```

### `scope`

```
/subscriptions/sub-one/resourceGroups/rg-tfplan2md-demo/providers/Microsoft.Storage/storageAccounts/sttfplan2mdlogs-with-extended-name-1234567890
```

</details>

#### 🔄 azurerm_role_assignment.update_assignment

**Summary:** `azurerm_role_assignment.update_assignment` | Changed: condition, description, principal_id, +3 more

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `condition` | - | request.clientip != '10.0.0.0/24' |
| `description` | Allow team to read storage data | Upgraded permissions for security auditing |
| `principal_id` | 22222222-2222-2222-2222-222222222222 | 33333333-3333-3333-3333-333333333333 |
| `skip_service_principal_aad_check` | false | true |

</details>

<details>
<summary>Large values: role_definition_id (2 lines, 2 changed), scope (2 lines, 2 changed)</summary>

### `role_definition_id`

**Before:**
```
/subscriptions/sub-one/providers/Microsoft.Authorization/roleDefinitions/2a2b9908-6ea1-4ae2-8e65-a410df84e7d1
```

**After:**
```
/subscriptions/sub-one/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe
```

### `scope`

**Before:**
```
/subscriptions/sub-one/resourceGroups/rg-tfplan2md-demo/providers/Microsoft.Storage/storageAccounts/sttfplan2mdlogs
```

**After:**
```
/subscriptions/sub-one/resourceGroups/rg-tfplan2md-demo/providers/Microsoft.Storage/storageAccounts/sttfplan2mddata
```

</details>

#### ♻️ azurerm_role_assignment.replace_assignment

**Summary:** recreating `azurerm_role_assignment.replace_assignment` (4 changed)

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `description` | Read-only access for DevOps | Updated role assignment with new permissions |
| `principal_id` | 22222222-2222-2222-2222-222222222222 | 33333333-3333-3333-3333-333333333333 |
| `role_definition_name` | - | Custom Contributor Long Name 1234567890 |

</details>

<details>
<summary>Large values: role_definition_id (2 lines, 2 changed)</summary>

### `role_definition_id`

```
/subscriptions/sub-two/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7
```

</details>

#### ❌ azurerm_role_assignment.delete_assignment

**Summary:** `azurerm_role_assignment.delete_assignment`

<details>

| Attribute | Value |
| ----------- | ------- |
| `description` | Legacy access |
| `principal_id` | 33333333-3333-3333-3333-333333333333 |
| `principal_type` | User |
| `scope` | /subscriptions/sub-three/resourceGroups/rg-legacy |

</details>

<details>
<summary>Large values: role_definition_id (2 lines, 2 changed)</summary>

### `role_definition_id`

```
/subscriptions/sub-three/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c
```

</details>

#### ➕ azurerm_role_assignment.unmapped_principal

**Summary:** `unmapped`

<details>

| Attribute | Value |
| ----------- | ------- |
| `name` | unmapped |
| `principal_id` | 99999999-9999-9999-9999-999999999999 |
| `principal_type` | ServicePrincipal |
| `role_definition_name` | Extremely Verbose Custom Role Name For Long Output Validation 1234567890 |
| `scope` | /subscriptions/sub-four/resourceGroups/rg-long-names-example |

</details>

---
