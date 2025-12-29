# Resource Display Format Options

This document compares different approaches to displaying resource information more compactly.

---

## Option A: Current Format (3 rows)

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

#### ➕ azurerm_virtual_network.hub

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (`eastus`) | `10.0.0.0/16`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

#### ❌ azurerm_storage_account.legacy

**Summary:** `sttfplan2mdlegacy`

<details>

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| name | `sttfplan2mdlegacy` |

</details>


#### ➕ azurerm_virtual_network.hub

<details>
<summary><b>Summary:</b> <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>) | <code>10.0.0.0/16</code></summary>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

#### ❌ azurerm_storage_account.legacy

<details>
<summary><b>Summary:</b> <code>sttfplan2mdlegacy</code></summary>

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| name | `sttfplan2mdlegacy` |

</details>

#### ♻️ azurerm_subnet.app

<details>
<summary><b>Summary:</b> <code>snet-app</code> | <code>vnet-spoke</code> | <code>10.1.1.0/24</code> | Replaced due to: address_prefixes</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_prefixes[0] | `10.1.0.0/24` | `10.1.1.0/24` |
| name | `snet-app` | `snet-app` |

</details>
#### ♻️ azurerm_subnet.app

**Summary:** `snet-app` | `vnet-spoke` | `10.1.1.0/24` | Replaced due to: address_prefixes

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_prefixes[0] | `10.1.0.0/24` | `10.1.1.0/24` |
| name | `snet-app` | `snet-app` |

</details>

---

## Option B: Summary in Details Tag (2 rows)

#### ➕ azurerm_resource_group.core

<details>
<summary><b>Summary:</b> <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

#### 🔄 azurerm_storage_account.data

<details>
<summary><b>Summary:</b> <code>sttfplan2mddata</code> | Changed: account_replication_type, tags.cost_center</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

---

## Option C: Everything in Details (1 row)

<details>
<summary>➕ <b>azurerm_resource_group.core</b> — <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

<br>

<details>
<summary>🔄 <b>azurerm_storage_account.data</b> — <code>sttfplan2mddata</code> | Changed: account_replication_type, tags.cost_center</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

<br>

<details>
<summary>➕ <b>azurerm_virtual_network.hub</b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>) | <code>10.0.0.0/16</code></summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

<br>

<details>
<summary>❌ <b>azurerm_storage_account.legacy</b> — <code>sttfplan2mdlegacy</code></summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| name | `sttfplan2mdlegacy` |

</details>

<br>

<details>
<summary>♻️ <b>azurerm_subnet.app</b> — <code>snet-app</code> | <code>vnet-spoke</code> | <code>10.1.1.0/24</code> | Replaced due to: address_prefixes</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_prefixes[0] | `10.1.0.0/24` | `10.1.1.0/24` |
| name | `snet-app` | `snet-app` |

</details>

---

## Option D: Compact with Inline HTML (2 rows)

#### ➕ azurerm_resource_group.core

<details>
<summary><strong>Summary:</strong> <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<table>
<tr><th>Attribute</th><th>Value</th></tr>
<tr><td>location</td><td><code>eastus</code></td></tr>
<tr><td>name</td><td><code>rg-tfplan2md-demo</code></td></tr>
<tr><td>tags.environment</td><td><code>demo</code></td></tr>
</table>

</details>

#### 🔄 azurerm_storage_account.data

<details>
<summary><strong>Summary:</strong> <code>sttfplan2mddata</code> | Changed: account_replication_type, tags.cost_center</summary>

<table>
<tr><th>Attribute</th><th>Before</th><th>After</th></tr>
<tr><td>account_replication_type</td><td><code>LRS</code></td><td><code>GRS</code></td></tr>
<tr><td>tags.cost_center</td><td>-</td><td><code>1234</code></td></tr>
</table>

</details>

#### ➕ azurerm_virtual_network.hub

<details>
<summary><strong>Summary:</strong> <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>) | <code>10.0.0.0/16</code></summary>

<table>
<tr><th>Attribute</th><th>Value</th></tr>
<tr><td>address_space[0]</td><td><code>10.0.0.0/16</code></td></tr>
<tr><td>location</td><td><code>eastus</code></td></tr>
<tr><td>name</td><td><code>vnet-hub</code></td></tr>
</table>

</details>

#### ❌ azurerm_storage_account.legacy

<details>
<summary><strong>Summary:</strong> <code>sttfplan2mdlegacy</code></summary>

<table>
<tr><th>Attribute</th><th>Value</th></tr>
<tr><td>account_replication_type</td><td><code>LRS</code></td></tr>
<tr><td>account_tier</td><td><code>Standard</code></td></tr>
<tr><td>name</td><td><code>sttfplan2mdlegacy</code></td></tr>
</table>

</details>

#### ♻️ azurerm_subnet.app

<details>
<summary><strong>Summary:</strong> <code>snet-app</code> | <code>vnet-spoke</code> | <code>10.1.1.0/24</code> | Replaced due to: address_prefixes</summary>

<table>
<tr><th>Attribute</th><th>Before</th><th>After</th></tr>
<tr><td>address_prefixes[0]</td><td><code>10.1.0.0/24</code></td><td><code>10.1.1.0/24</code></td></tr>
<tr><td>name</td><td><code>snet-app</code></td><td><code>snet-app</code></td></tr>
</table>

</details>

---

## Option E: Ultra-Compact (1 row, minimal details label)

<details>
<summary>➕ <b>azurerm_resource_group.core</b></summary>

**Summary:** `rg-tfplan2md-demo` (`eastus`)

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

<br>

<details>
<summary>🔄 <b>azurerm_storage_account.data</b></summary>

**Summary:** `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

<br>

<details>
<summary>➕ <b>azurerm_virtual_network.hub</b></summary>

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (`eastus`) | `10.0.0.0/16`

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

<br>

<details>
<summary>❌ <b>azurerm_storage_account.legacy</b></summary>

**Summary:** `sttfplan2mdlegacy`

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| name | `sttfplan2mdlegacy` |

</details>

<br>

<details>
<summary>♻️ <b>azurerm_subnet.app</b></summary>

**Summary:** `snet-app` | `vnet-spoke` | `10.1.1.0/24` | Replaced due to: address_prefixes

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_prefixes[0] | `10.1.0.0/24` | `10.1.1.0/24` |
| name | `snet-app` | `snet-app` |

</details>

---

## Option F: Two-Level Collapse (2 rows, nested details)

#### ➕ azurerm_resource_group.core

<details>
<summary><b>Summary:</b> <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<details>
<summary>View Attributes</summary>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

</details>

#### 🔄 azurerm_storage_account.data

<details>
<summary><b>Summary:</b> <code>sttfplan2mddata</code> | Changed: account_replication_type, tags.cost_center</summary>

<details>
<summary>View Changes</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

</details>

#### ➕ azurerm_virtual_network.hub

<details>
<summary><b>Summary:</b> <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>) | <code>10.0.0.0/16</code></summary>

<details>
<summary>View Attributes</summary>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

</details>

#### ❌ azurerm_storage_account.legacy

<details>
<summary><b>Summary:</b> <code>sttfplan2mdlegacy</code></summary>

<details>
<summary>View Attributes</summary>

| Attribute | Value |
| --------

#### ➕ azurerm_virtual_network.hub

<details>
<summary>📋 <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>) | <code>10.0.0.0/16</code></summary>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |

</details>

#### ❌ azurerm_storage_account.legacy

<details>
<summary>📋 <code>sttfplan2mdlegacy</code></summary>

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| name | `sttfplan2mdlegacy` |

</details>

#### ♻️ azurerm_subnet.app

<details>
<summary>📋 <code>snet-app</code> | <code>vnet-spoke</code> | <code>10.1.1.0/24</code> | Replaced due to: address_prefixes</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_prefixes[0] | `10.1.0.0/24` | `10.1.1.0/24` |
| name | `snet-app` | `snet-app` |

</details>--- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| name | `sttfplan2mdlegacy` |

</details>

</details>

#### ♻️ azurerm_subnet.app

<details>
<summary><b>Summary:</b> <code>snet-app</code> | <code>vnet-spoke</code> | <code>10.1.1.0/24</code> | Replaced due to: address_prefixes</summary>

<details>
<summary>View Changes</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_prefixes[0] | `10.1.0.0/24` | `10.1.1.0/24` |
| name | `snet-app` | `snet-app` |

</details>

</details>

---

## Option G: Summary Badge Style (2 rows)

#### ➕ azurerm_resource_group.core

<details>
<summary>📋 <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |

</details>

#### 🔄 azurerm_storage_account.data

<details>
<summary>📋 <code>sttfplan2mddata</code> | Changed: account_replication_type, tags.cost_center</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>

---

## Comparison Notes

- **Option A**: Current format - 3 visual rows per resource
- **Option B**: Summary as details label - 2 rows, uses HTML tags for formatting
- **Option C**: Everything collapsed - 1 row, most compact but resource name + summary in one line
- **Option D**: HTML tables inside - 2 rows, guarantees Azure DevOps compatibility
- **Option E**: Minimal collapse - 1 row heading + expanded summary inside
- **Option F**: Two-level collapse - 2 rows, extra click to see details
- **Option G**: Badge-style with icon - 2 rows, clean look

## Your Feedback

Which format do you prefer? Consider:
1. Readability when scanning multiple resources
2. Compactness (reducing vertical space)
3. Aesthetics and professional appearance
4. Ease of finding specific information
