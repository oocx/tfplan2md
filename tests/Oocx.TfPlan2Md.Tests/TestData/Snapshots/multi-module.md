# Terraform Plan Report

**Terraform Version:** 1.14.0

## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 4 | 1 azurerm_postgresql_server<br/>1 azurerm_resource_group<br/>1 azurerm_subnet<br/>1 azurerm_virtual_network |
| 🔄 Change | 1 | 1 azurerm_app_service |
| ♻️ Replace | 0 |  |
| ❌ Destroy | 0 |  |
| **Total** | **5** | |

## Resource Changes

### 📦 Module: root

<!-- tfplan2md:resource-start address=azurerm_resource_group.rg_root -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_resource_group <b><code>rg_root</code></b> — <code>rg-root</code> <code>🌍 westeurope</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| location | `🌍 westeurope` |
| name | `rg-root` |

</details>
<!-- tfplan2md:resource-end address=azurerm_resource_group.rg_root -->

---

### 📦 Module: `module.network`

<!-- tfplan2md:resource-start address=module.network.azurerm_virtual_network.vnet -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_virtual_network <b><code>vnet</code></b> — <code>vnet</code> <code>🌐 10.0.0.0/16</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `🌐 10.0.0.0/16` |
| name | `vnet` |

</details>
<!-- tfplan2md:resource-end address=module.network.azurerm_virtual_network.vnet -->

---

### 📦 Module: `module.network.module.subnet`

<!-- tfplan2md:resource-start address=module.network.module.subnet.azurerm_subnet.subnet1 -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_subnet <b><code>subnet1</code></b> — <code>subnet1</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| address_prefix | `🌐 10.0.1.0/24` |
| name | `subnet1` |

</details>
<!-- tfplan2md:resource-end address=module.network.module.subnet.azurerm_subnet.subnet1 -->

---

### 📦 Module: `module.app`

<!-- tfplan2md:resource-start address=module.app.azurerm_app_service.app -->
<details style="margin-bottom:12px;">
<summary>🔄 azurerm_app_service <b><code>app</code></b> — <code>example-app</code> | 1🔧 plan</summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| plan | `B1` | `S1` |

</details>
<!-- tfplan2md:resource-end address=module.app.azurerm_app_service.app -->

---

### 📦 Module: `module.app.module.database`

<!-- tfplan2md:resource-start address=module.app.module.database.azurerm_postgresql_server.db -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_postgresql_server <b><code>db</code></b> — <code>example-db</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `example-db` |
| sku | `GP_Gen5_2` |

</details>
<!-- tfplan2md:resource-end address=module.app.module.database.azurerm_postgresql_server.db -->
