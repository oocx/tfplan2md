# Generic Resource Summary Options

This document presents 5 different approaches for adding summary lines to generic resources (all resource types except those with custom templates like role assignments and firewall rules).

## Current State (No Summary)

#### ➕ azurerm_storage_account.logs

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `allow_blob_public_access` | false |
| `location` | eastus |
| `min_tls_version` | TLS1_2 |
| `name` | sttfplan2mdlogs |
| `resource_group_name` | rg-tfplan2md-demo |
| `tags.cost_center` | ops |
| `tags.environment` | demo |

</details>

---

## Option 1: Key Attribute Summary (Attribute-Based)

Display 2-3 most important attributes that identify the resource.

### CREATE Example
#### ➕ azurerm_storage_account.logs

**Summary:** `sttfplan2mdlogs` in `rg-tfplan2md-demo` (eastus) | Standard LRS

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `allow_blob_public_access` | false |
| `location` | eastus |
| `min_tls_version` | TLS1_2 |
| `name` | sttfplan2mdlogs |
| `resource_group_name` | rg-tfplan2md-demo |
| `tags.cost_center` | ops |
| `tags.environment` | demo |

</details>

### UPDATE Example
#### 🔄 azurerm_storage_account.data

**Summary:** Replication: LRS → GRS | Added tag `cost_center: 1234`

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `account_replication_type` | LRS | GRS |
| `account_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `name` | sttfplan2mddata | sttfplan2mddata |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `tags.cost_center` | - | 1234 |
| `tags.environment` | demo | demo |
| `tags.owner` | data | data |

</details>

### REPLACE Example
#### ♻️ azurerm_subnet.db

**Summary:** recreate `snet-db` | Address prefix: 10.1.2.0/24 → 10.1.20.0/24

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | snet-db | snet-db |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `virtual_network_name` | vnet-spoke | vnet-spoke |
| `address_prefixes[0]` | 10.1.2.0/24 | 10.1.20.0/24 |

</details>

### DELETE Example
#### ❌ azurerm_storage_account.legacy

**Summary:** removing `sttfplan2mdlegacy` (Standard LRS in eastus)

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `location` | eastus |
| `name` | sttfplan2mdlegacy |
| `resource_group_name` | rg-old |
| `tags.environment` | old |

</details>

**Pros:**
- ✅ Most informative - shows what changed
- ✅ Familiar from role assignments
- ✅ Scannable and useful for decision-making

**Cons:**
- ❌ Requires defining "key attributes" per resource type (or heuristics)
- ❌ Complex logic to determine what changed
- ❌ May be verbose for resources with many changes

---

## Option 2: Change Count Summary

Simple count of how many attributes changed.

### CREATE Example
#### ➕ azurerm_storage_account.logs

**Summary:** 9 attributes

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `allow_blob_public_access` | false |
| `location` | eastus |
| `min_tls_version` | TLS1_2 |
| `name` | sttfplan2mdlogs |
| `resource_group_name` | rg-tfplan2md-demo |
| `tags.cost_center` | ops |
| `tags.environment` | demo |

</details>

### UPDATE Example
#### 🔄 azurerm_storage_account.data

**Summary:** 2 attributes changed (6 unchanged)

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `account_replication_type` | LRS | GRS |
| `account_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `name` | sttfplan2mddata | sttfplan2mddata |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `tags.cost_center` | - | 1234 |
| `tags.environment` | demo | demo |
| `tags.owner` | data | data |

</details>

### REPLACE Example
#### ♻️ azurerm_subnet.db

**Summary:** recreating with 1 attribute changed (3 unchanged)

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | snet-db | snet-db |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `virtual_network_name` | vnet-spoke | vnet-spoke |
| `address_prefixes[0]` | 10.1.2.0/24 | 10.1.20.0/24 |

</details>

### DELETE Example
#### ❌ azurerm_storage_account.legacy

**Summary:** 6 attributes

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `location` | eastus |
| `name` | sttfplan2mdlegacy |
| `resource_group_name` | rg-old |
| `tags.environment` | old |

</details>

**Pros:**
- ✅ Simple, generic - works for all resource types
- ✅ Easy to implement
- ✅ Clear indication of change scope

**Cons:**
- ❌ Doesn't tell you WHAT changed
- ❌ Less useful for deciding if you need to review details

---

## Option 3: Resource Name + Type Summary

Just show the resource name (from the `name` attribute if present).

### CREATE Example
#### ➕ azurerm_storage_account.logs

**Summary:** `sttfplan2mdlogs`

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `allow_blob_public_access` | false |
| `location` | eastus |
| `min_tls_version` | TLS1_2 |
| `name` | sttfplan2mdlogs |
| `resource_group_name` | rg-tfplan2md-demo |
| `tags.cost_center` | ops |
| `tags.environment` | demo |

</details>

### UPDATE Example
#### 🔄 azurerm_storage_account.data

**Summary:** `sttfplan2mddata` (2 changes)

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `account_replication_type` | LRS | GRS |
| `account_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `name` | sttfplan2mddata | sttfplan2mddata |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `tags.cost_center` | - | 1234 |
| `tags.environment` | demo | demo |
| `tags.owner` | data | data |

</details>

### REPLACE Example
#### ♻️ azurerm_subnet.db

**Summary:** recreating `snet-db`

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | snet-db | snet-db |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `virtual_network_name` | vnet-spoke | vnet-spoke |
| `address_prefixes[0]` | 10.1.2.0/24 | 10.1.20.0/24 |

</details>

### DELETE Example
#### ❌ azurerm_storage_account.legacy

**Summary:** removing `sttfplan2mdlegacy`

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `location` | eastus |
| `name` | sttfplan2mdlegacy |
| `resource_group_name` | rg-old |
| `tags.environment` | old |

</details>

**Pros:**
- ✅ Very simple
- ✅ Helps identify the resource without expanding
- ✅ Works for most Azure resources (they have a `name` attribute)

**Cons:**
- ❌ Doesn't show what changed
- ❌ Not all resources have a `name` attribute

---

## Option 4: Changed Attribute List

Show which attributes changed (names only, not values).

### CREATE Example
#### ➕ azurerm_storage_account.logs

**Summary:** Creating with: account_tier, account_replication_type, location, name, resource_group_name, tags, +3 more

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `allow_blob_public_access` | false |
| `location` | eastus |
| `min_tls_version` | TLS1_2 |
| `name` | sttfplan2mdlogs |
| `resource_group_name` | rg-tfplan2md-demo |
| `tags.cost_center` | ops |
| `tags.environment` | demo |

</details>

### UPDATE Example
#### 🔄 azurerm_storage_account.data

**Summary:** Changing: account_replication_type, tags.cost_center

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `account_replication_type` | LRS | GRS |
| `account_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `name` | sttfplan2mddata | sttfplan2mddata |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `tags.cost_center` | - | 1234 |
| `tags.environment` | demo | demo |
| `tags.owner` | data | data |

</details>

### REPLACE Example
#### ♻️ azurerm_subnet.db

**Summary:** Recreating due to: address_prefixes[0]

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | snet-db | snet-db |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `virtual_network_name` | vnet-spoke | vnet-spoke |
| `address_prefixes[0]` | 10.1.2.0/24 | 10.1.20.0/24 |

</details>

### DELETE Example
#### ❌ azurerm_storage_account.legacy

**Summary:** Removing: account_replication_type, account_tier, location, name, resource_group_name, tags

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `location` | eastus |
| `name` | sttfplan2mdlegacy |
| `resource_group_name` | rg-old |
| `tags.environment` | old |

</details>

**Pros:**
- ✅ Shows exactly which fields are involved
- ✅ Good for quick scanning
- ✅ Helps identify if critical attributes are changing

**Cons:**
- ❌ Can be verbose for resources with many attributes
- ❌ Requires truncation logic ("+ N more")

---

## Option 5: Hybrid Approach (Intelligent Summary)

Combine approaches based on context:
- For CREATE/DELETE: Show resource name
- For UPDATE: Show changed attributes (up to 3) + count
- For REPLACE: Show replacement reason (when available) + name

### CREATE Example
#### ➕ azurerm_storage_account.logs

**Summary:** `sttfplan2mdlogs`

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `allow_blob_public_access` | false |
| `location` | eastus |
| `min_tls_version` | TLS1_2 |
| `name` | sttfplan2mdlogs |
| `resource_group_name` | rg-tfplan2md-demo |
| `tags.cost_center` | ops |
| `tags.environment` | demo |

</details>

### UPDATE Example
#### 🔄 azurerm_storage_account.data

**Summary:** `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `account_replication_type` | LRS | GRS |
| `account_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `name` | sttfplan2mddata | sttfplan2mddata |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `tags.cost_center` | - | 1234 |
| `tags.environment` | demo | demo |
| `tags.owner` | data | data |

</details>

### REPLACE Example (with replacement reason)
#### ♻️ azurerm_subnet.db

**Summary:** recreate `snet-db` (address_prefixes changed: forces replacement)

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | snet-db | snet-db |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `virtual_network_name` | vnet-spoke | vnet-spoke |
| `address_prefixes[0]` | 10.1.2.0/24 | 10.1.20.0/24 |

</details>

### REPLACE Example (without replacement reason)
#### ♻️ azurerm_network_security_group.app

**Summary:** recreating `nsg-app` (1 changed)

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | nsg-app | nsg-app |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `location` | eastus | eastus |
| `security_rule[0].destination_port_range` | 80 | 443 |

</details>

### DELETE Example
#### ❌ azurerm_storage_account.legacy

**Summary:** `sttfplan2mdlegacy`

<details>

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `location` | eastus |
| `name` | sttfplan2mdlegacy |
| `resource_group_name` | rg-old |
| `tags.environment` | old |

</details>

**Pros:**
- ✅ Most context-appropriate
- ✅ Combines best of all approaches
- ✅ Most useful for users
- ✅ Shows replacement reasons when available

**Cons:**
- ❌ Most complex to implement
- ❌ Different formats for different actions (but this is intentional)

---

## Additional Examples with Multiple Resource Types

### Virtual Network (CREATE)

**Option 1:**
#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (eastus) | Address space: 10.0.0.0/16

**Option 2:**
#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** 4 attributes

**Option 3:**
#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub`

**Option 4:**
#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** Creating with: name, location, address_space, resource_group_name

**Option 5:**
#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub`

### Key Vault (UPDATE)

**Option 1:**
#### 🔄 module.security.azurerm_key_vault.main

**Summary:** Enabled soft delete | Changed purge protection: disabled → enabled

**Option 2:**
#### 🔄 module.security.azurerm_key_vault.main

**Summary:** 2 attributes changed (5 unchanged)

**Option 3:**
#### 🔄 module.security.azurerm_key_vault.main

**Summary:** `kv-tfplan2md` (2 changes)

**Option 4:**
#### 🔄 module.security.azurerm_key_vault.main

**Summary:** Changing: soft_delete_enabled, purge_protection_enabled

**Option 5:**
#### 🔄 module.security.azurerm_key_vault.main

**Summary:** `kv-tfplan2md` | Changed: soft_delete_enabled, purge_protection_enabled

---

## Recommendation

Based on the examples above, **Option 5 (Hybrid Approach)** appears to offer the best balance:
- Simple and clean for CREATE/DELETE (just show the name)
- Informative for UPDATE (shows what changed)
- Comprehensive for REPLACE (shows replacement reason when available)
- Consistent with the existing role assignment pattern

This option provides the most value to users for making decisions about whether to expand the details, while being feasible to implement generically across all resource types.
