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

### Module: root

#### ➕ azurerm_resource_group.rg_root

<details>

| Attribute | Value |
| ----------- | ------- |
| `location` | westeurope |
| `name` | rg-root |

</details>

---

### Module: `module.network`

#### ➕ module.network.azurerm_virtual_network.vnet

<details>

| Attribute | Value |
| ----------- | ------- |
| `address_space[0]` | 10.0.0.0/16 |
| `name` | vnet |

</details>

---

### Module: `module.network.module.subnet`

#### ➕ module.network.module.subnet.azurerm_subnet.subnet1

<details>

| Attribute | Value |
| ----------- | ------- |
| `address_prefix` | 10.0.1.0/24 |
| `name` | subnet1 |

</details>

---

### Module: `module.app`

#### 🔄 module.app.azurerm_app_service.app

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `name` | example-app | example-app |
| `plan` | B1 | S1 |

</details>

---

### Module: `module.app.module.database`

#### ➕ module.app.module.database.azurerm_postgresql_server.db

<details>

| Attribute | Value |
| ----------- | ------- |
| `name` | example-db |
| `sku` | GP_Gen5_2 |

</details>

---
