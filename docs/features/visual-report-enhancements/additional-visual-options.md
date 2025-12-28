# Additional Visual Enhancement Options

This document explores more visual improvements for IP addresses, tags, and changed attribute lists.

---

## Option 1: IP Addresses and CIDR Blocks

### Current Format

<details>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (🌍 eastus) | <code>10.0.0.0/16</code></summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

### Option A: Icon Outside Code Block

<details>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (🌍 eastus) | 🌐 <code>10.0.0.0/16</code></summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | 🌐 `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

<br>

<details>
<summary>➕ azurerm_subnet <b><code>app[0]</code></b> — <code>snet-app</code> | <code>vnet-spoke</code> | 🌐 <code>10.1.1.0/24</code></summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_prefixes[0] | 🌐 `10.1.1.0/24` |
| name | `snet-app` |

</details>

### Option B: Icon Inside Code Block

<details>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (🌍 eastus) | <code>🌐 10.0.0.0/16</code></summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `🌐 10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

<br>

<details>
<summary>➕ azurerm_subnet <b><code>app[0]</code></b> — <code>snet-app</code> | <code>vnet-spoke</code> | <code>🌐 10.1.1.0/24</code></summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_prefixes[0] | `🌐 10.1.1.0/24` |
| name | `snet-app` |

</details>

---

## Option 3: Tags Presentation

### Current Format (Tags in Table)

<details>
<summary>➕ azurerm_resource_group <b><code>core</code></b> — <code>rg-tfplan2md-demo</code> (🌍 eastus)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |
| tags.owner | `tfplan2md` |
| tags.cost_center | `ops` |

</details>

### Option A: Tags with Icon in Table

<details>
<summary>➕ azurerm_resource_group <b><code>core</code></b> — <code>rg-tfplan2md-demo</code> (🌍 eastus)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| 🏷️ tags.environment | `demo` |
| 🏷️ tags.owner | `tfplan2md` |
| 🏷️ tags.cost_center | `ops` |

</details>

### Option B: Tags Section Outside Table (Azure Portal Style)

<details>
<summary>➕ azurerm_resource_group <b><code>core</code></b> — <code>rg-tfplan2md-demo</code> (🌍 eastus)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

**🏷️ Tags:**
- environment: `demo`
- owner: `tfplan2md`
- cost_center: `ops`

</details>

### Option C: Tags as Inline Badges (Azure Portal Style)

<details>
<summary>➕ azurerm_resource_group <b><code>core</code></b> — <code>rg-tfplan2md-demo</code> (🌍 eastus)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

**🏷️ Tags:** `environment: demo` `owner: tfplan2md` `cost_center: ops`

</details>

### Option D: Tags in Compact Table

<details>
<summary>➕ azurerm_resource_group <b><code>core</code></b> — <code>rg-tfplan2md-demo</code> (🌍 eastus)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

**🏷️ Tags:**

| Tag | Value |
| ----- | ------- |
| environment | `demo` |
| owner | `tfplan2md` |
| cost_center | `ops` |

</details>

---

## Option 6: Changed Attributes List

### Current Format

<details>
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>sttfplan2mddata</code> | Changed: account_replication_type, tags.cost_center</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

### Option A: Icon Only (No "Changed:" Text)

<details>
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>sttfplan2mddata</code> | 🔧 account_replication_type, tags.cost_center</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

### Option B: Icon Inside Parentheses

<details>
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>sttfplan2mddata</code> | (🔧 2) account_replication_type, tags.cost_center</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

### Option C: Number + Icon (No Parentheses)

<details>
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>sttfplan2mddata</code> | 2 🔧 account_replication_type, tags.cost_center</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

---

## Comparison Questions

**IP Addresses:**
- ✅ **Decision: Icon INSIDE code block** (Option B applies to IPs, regions, and similar elements)

**Tags:**
- ✅ **Decision: Option C** (Inline badges style)

**Changed Attributes:**
- ✅ **Decision: Option C** - Number + icon (no parentheses) `2 🔧`

Please review the changed attributes options!
