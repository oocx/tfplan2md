# Visual Spacing Options Comparison

This document shows different approaches to visual spacing and breaks in the generated reports.

---

## Option A: Current Format (Baseline)

### Module: root

#### ➕ azurerm_resource_group.core

**Summary:** `rg-tfplan2md-demo` (`eastus`)

<details>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

#### 🔄 azurerm_storage_account.data

**Summary:** `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

### Module: `module.network`

#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (`eastus`) | `10.0.0.0/16`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

---

## Option B: Horizontal Rules Between Modules

### Module: root

#### ➕ azurerm_resource_group.core

**Summary:** `rg-tfplan2md-demo` (`eastus`)

<details>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

#### 🔄 azurerm_storage_account.data

**Summary:** `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

---

### Module: `module.network`

#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (`eastus`) | `10.0.0.0/16`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

---

## Option C: Visual Module Header with Icon

### 📦 Module: root

#### ➕ azurerm_resource_group.core

**Summary:** `rg-tfplan2md-demo` (`eastus`)

<details>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

#### 🔄 azurerm_storage_account.data

**Summary:** `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

### 📦 Module: `module.network`

#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (`eastus`) | `10.0.0.0/16`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

---

## Option D: Horizontal Rules + Module Icons

### 📦 Module: root

#### ➕ azurerm_resource_group.core

**Summary:** `rg-tfplan2md-demo` (`eastus`)

<details>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

#### 🔄 azurerm_storage_account.data

**Summary:** `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

---

### 📦 Module: `module.network`

#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (`eastus`) | `10.0.0.0/16`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

---

## Option E: Resource Separators (Subtle)

### Module: root

#### ➕ azurerm_resource_group.core

**Summary:** `rg-tfplan2md-demo` (`eastus`)

<details>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

---

#### 🔄 azurerm_storage_account.data

**Summary:** `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

---

### Module: `module.network`

#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (`eastus`) | `10.0.0.0/16`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

---

## Your Feedback

Please review these options in your markdown viewer and let me know:
1. Which spacing/separator approach do you prefer?
2. Do you want module icons?
3. Any other visual elements you'd like to see?
