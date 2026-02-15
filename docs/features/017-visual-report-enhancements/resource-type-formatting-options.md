# Resource Type Formatting Options

This document explores different ways to visually distinguish the resource type from the resource name for better scannability.

---

## Current Format (Baseline)

<details>
<summary>➕ <b>azurerm_resource_group.core</b> — <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

</details>

<br>

<details>
<summary>🔄 <b>azurerm_storage_account.data</b> — <code>sttfplan2mddata</code> | Changed: account_replication_type</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |

</details>

<br>

<details>
<summary>➕ <b>azurerm_virtual_network.hub</b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |

</details>

---

## Option A: Type in Code, Name in Bold

<details>
<summary>➕ <code>azurerm_resource_group</code> <b>core</b> — <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

</details>

<br>

<details>
<summary>🔄 <code>azurerm_storage_account</code> <b>data</b> — <code>sttfplan2mddata</code> | Changed: account_replication_type</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |

</details>

<br>

<details>
<summary>➕ <code>azurerm_virtual_network</code> <b>hub</b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |

</details>

---

## Option B: Type Plain, Name Bold+Code

<details>
<summary>➕ azurerm_resource_group <b><code>core</code></b> — <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

</details>

<br>

<details>
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>sttfplan2mddata</code> | Changed: account_replication_type</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |

</details>

<br>

<details>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |

</details>

<br>

<details>
<summary>➕ azurerm_subnet <b><code>app[0]</code></b> — <code>snet-app</code> | `vnet-spoke` | `10.1.1.0/24`</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_prefixes[0] | `10.1.1.0/24` |
| name | `snet-app` |

</details>

<br>

<details>
<summary>🔄 azurerm_network_security_rule <b><code>allow_https[2]</code></b> — Changed: destination_port_range</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| destination_port_range | `443` | `8443` |

</details>

---

## Option C: Type with Separator Icon

<details>
<summary>➕ <b>azurerm_resource_group</b> ▸ <b>core</b> — <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

</details>

<br>

<details>
<summary>🔄 <b>azurerm_storage_account</b> ▸ <b>data</b> — <code>sttfplan2mddata</code> | Changed: account_replication_type</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |

</details>

<br>

<details>
<summary>➕ <b>azurerm_virtual_network</b> ▸ <b>hub</b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |

</details>

<br>

<details>
<summary>➕ <b>azurerm_subnet</b> ▸ <b>app[0]</b> — <code>snet-app</code> | `vnet-spoke` | `10.1.1.0/24`</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_prefixes[0] | `10.1.1.0/24` |
| name | `snet-app` |

</details>

<br>

<details>
<summary>🔄 <b>azurerm_network_security_rule</b> ▸ <b>allow_https[2]</b> — Changed: destination_port_range</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| destination_port_range | `443` | `8443` |

</details>

---

## Option D: Type in Italics

<details>
<summary>➕ <i>azurerm_resource_group</i> <b>core</b> — <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

</details>

<br>

<details>
<summary>🔄 <i>azurerm_storage_account</i> <b>data</b> — <code>sttfplan2mddata</code> | Changed: account_replication_type</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |

</details>

<br>

<details>
<summary>➕ <i>azurerm_virtual_network</i> <b>hub</b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |

</details>

---

## Option E: Brackets Around Type

<details>
<summary>➕ <b>[azurerm_resource_group]</b> <b>core</b> — <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

</details>

<br>

<details>
<summary>🔄 <b>[azurerm_storage_account]</b> <b>data</b> — <code>sttfplan2mddata</code> | Changed: account_replication_type</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |

</details>

<br>

<details>
<summary>➕ <b>[azurerm_virtual_network]</b> <b>hub</b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |

</details>

---

## Option F: Type Badge Style

<details>
<summary>➕ <code>azurerm_resource_group</code> • <b>core</b> — <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |

</details>

<br>

<details>
<summary>🔄 <code>azurerm_storage_account</code> • <b>data</b> — <code>sttfplan2mddata</code> | Changed: account_replication_type</summary>

<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |

</details>

<br>

<details>
<summary>➕ <code>azurerm_virtual_network</code> • <b>hub</b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> (<code>eastus</code>)</summary>

<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |

</details>

---

## Comparison Notes

- **Current**: Everything in bold - type and name not visually distinguished
- **Option A**: Type in code format, name in bold - clear separation
- **Option B**: Type plain text, name bold+code - emphasizes the resource name
- **Option C**: Uses ▸ separator - visual break between type and name
- **Option D**: Type in italics - subtle distinction
- **Option E**: Brackets around type - groups the type visually
- **Option F**: Bullet separator (•) - clean modern look

## Your Feedback

Which formatting do you prefer for resource type vs resource name? Consider:
1. Readability when scanning for specific resource names
2. Ability to quickly identify resource types
3. Visual hierarchy and aesthetics
4. Consistency with the overall report style
