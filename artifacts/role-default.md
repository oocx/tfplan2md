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

### 📦 Module: `module.security`

<!-- tfplan2md:resource-start address=azurerm_role_assignment.create_no_description -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_role_assignment <b><code>create_no_description</code></b> — <code>👤 </code> → <code>🛡️ Reader</code> on <code>rg-tfplan2md-demo</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `sub-one` |
| role_definition_id | `🛡️ Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |
| principal_id |  (`👤 User`) [`11111111-1111-1111-1111-111111111111`] |
| principal_type | `👤 User` |
| name | `ra-create` |

</details>
<!-- tfplan2md:resource-end address=azurerm_role_assignment.create_no_description -->

<!-- tfplan2md:resource-start address=azurerm_role_assignment.create_with_description -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_role_assignment <b><code>create_with_description</code></b> — <code>👥 </code> → <code>🛡️ Storage Blob Data Reader</code> on Storage Account <code>sttfplan2mdlogs-with-extended-name-1234567890</code></summary>
<br>

Allow DevOps team to read logs from the storage account

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `sub-one` |
| role_definition_id | `🛡️ Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) |
| principal_id |  (`👥 Group`) [`22222222-2222-2222-2222-222222222222`] |
| principal_type | `👥 Group` |
| name | `ra-storage-reader` |
| description | `Allow DevOps team to read logs from the storage account` |

</details>
<!-- tfplan2md:resource-end address=azurerm_role_assignment.create_with_description -->

<!-- tfplan2md:resource-start address=azurerm_role_assignment.update_assignment -->
<details style="margin-bottom:12px;">
<summary>🔄 azurerm_role_assignment <b><code>update_assignment</code></b> — <code>👥 </code> → <code>🛡️ Storage Blob Data Contributor</code> on Storage Account <code>sttfplan2mddata</code></summary>
<br>

Upgraded permissions for security auditing

| Attribute | Before | After |
| ----------- | -------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `sub-one` | `rg-tfplan2md-demo` in subscription `sub-one` |
| role_definition_id | `🛡️ Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) | `🛡️ Storage Blob Data Contributor` (`ba92f5b4-2d11-453d-a403-e96b0029c9fe`) |
| principal_id |  (`👥 Group`) [`22222222-2222-2222-2222-222222222222`] |  (`👥 Group`) [`33333333-3333-3333-3333-333333333333`] |
| description | `Allow team to read storage data` | `Upgraded permissions for security auditing` |
| condition | - | `request.clientip != '10.0.0.0/24'` |
| skip_service_principal_aad_check | `False` | `True` |

</details>
<!-- tfplan2md:resource-end address=azurerm_role_assignment.update_assignment -->

<!-- tfplan2md:resource-start address=azurerm_role_assignment.replace_assignment -->
<details style="margin-bottom:12px;">
<summary>♻️ azurerm_role_assignment <b><code>replace_assignment</code></b> — recreate as <code>👥 </code> → <code>🛡️ Custom Contributor Long Name 1234567890</code> on <code>rg-production</code></summary>
<br>

Updated role assignment with new permissions

| Attribute | Before | After |
| ----------- | -------- | ------- |
| role_definition_id | `🛡️ Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) | `🛡️ Custom Contributor Long Name 1234567890` |
| principal_id |  (`👥 Group`) [`22222222-2222-2222-2222-222222222222`] |  (`👥 Group`) [`33333333-3333-3333-3333-333333333333`] |
| description | `Read-only access for DevOps` | `Updated role assignment with new permissions` |
| role_definition_name | - | `🛡️ Custom Contributor Long Name 1234567890` |

</details>
<!-- tfplan2md:resource-end address=azurerm_role_assignment.replace_assignment -->

<!-- tfplan2md:resource-start address=azurerm_role_assignment.delete_assignment -->
<details style="margin-bottom:12px;">
<summary>❌ azurerm_role_assignment <b><code>delete_assignment</code></b> — remove <code>🛡️ Contributor</code> on <code>rg-legacy</code> from <code>👤 </code></summary>
<br>

Legacy access

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-legacy` in subscription `sub-three` |
| role_definition_id | `🛡️ Contributor` (`b24988ac-6180-42a0-ab88-20f7382dd24c`) |
| principal_id |  (`👤 User`) [`33333333-3333-3333-3333-333333333333`] |
| principal_type | `👤 User` |
| description | `Legacy access` |

</details>
<!-- tfplan2md:resource-end address=azurerm_role_assignment.delete_assignment -->

<!-- tfplan2md:resource-start address=azurerm_role_assignment.unmapped_principal -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_role_assignment <b><code>unmapped_principal</code></b> — <code>💻 </code> → <code>🛡️ Extremely Verbose Custom Role Name For Long Output Validation 1234567890</code> on <code>rg-long-names-example</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-long-names-example` in subscription `sub-four` |
| principal_id |  (`💻 ServicePrincipal`) [`99999999-9999-9999-9999-999999999999`] |
| principal_type | `💻 ServicePrincipal` |
| name | `unmapped` |
| role_definition_name | `🛡️ Extremely Verbose Custom Role Name For Long Output Validation 1234567890` |

</details>
<!-- tfplan2md:resource-end address=azurerm_role_assignment.unmapped_principal -->
