# Summary Resource Type Breakdown - Formatting Options

This document shows different formatting options for displaying resource type breakdown in the summary table.

## Example Scenario

Assume a Terraform plan with the following changes:
- **Add**: 3 `azurerm_storage_account`, 1 `azurerm_resource_group`, 2 `azurerm_virtual_network`
- **Change**: 2 `azurerm_app_service`, 1 `azurerm_sql_database`
- **Replace**: 1 `azurerm_kubernetes_cluster`
- **Destroy**: 2 `azurerm_network_security_group`, 1 `azurerm_public_ip`

---

## Option 1: Single-line comma-separated text

| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | 3 azurerm_storage_account, 1 azurerm_resource_group, 2 azurerm_virtual_network |
| 🔄 Change | 3 | 2 azurerm_app_service, 1 azurerm_sql_database |
| ♻️ Replace | 1 | 1 azurerm_kubernetes_cluster |
| ❌ Destroy | 3 | 2 azurerm_network_security_group, 1 azurerm_public_ip |
| **Total** | **13** | |

**Pros:**
- Compact, fits in one line per row
- Easy to scan horizontally

**Cons:**
- Can become very wide with many resource types
- May wrap awkwardly in narrow displays

---

## Option 2: Bulleted list (HTML details/summary)

| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | <details><summary>6 types</summary><ul><li>3 azurerm_storage_account</li><li>1 azurerm_resource_group</li><li>2 azurerm_virtual_network</li></ul></details> |
| 🔄 Change | 3 | <details><summary>2 types</summary><ul><li>2 azurerm_app_service</li><li>1 azurerm_sql_database</li></ul></details> |
| ♻️ Replace | 1 | <details><summary>1 type</summary><ul><li>1 azurerm_kubernetes_cluster</li></ul></details> |
| ❌ Destroy | 3 | <details><summary>2 types</summary><ul><li>2 azurerm_network_security_group</li><li>1 azurerm_public_ip</li></ul></details> |
| **Total** | **13** | |

**Pros:**
- Keeps table compact by default
- Expandable for details
- Clear structure when expanded

**Cons:**
- Requires user interaction to see details
- More complex to render
- May not work in all markdown viewers

---

## Option 3: Multi-line text (line breaks)

| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | 3 azurerm_storage_account<br/>1 azurerm_resource_group<br/>2 azurerm_virtual_network |
| 🔄 Change | 3 | 2 azurerm_app_service<br/>1 azurerm_sql_database |
| ♻️ Replace | 1 | 1 azurerm_kubernetes_cluster |
| ❌ Destroy | 3 | 2 azurerm_network_security_group<br/>1 azurerm_public_ip |
| **Total** | **13** | |

**Pros:**
- Clear vertical layout
- All information visible without interaction
- Easier to read when many types

**Cons:**
- Takes more vertical space
- Table rows can become tall

---

## Option 4: Code-style formatting

| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | `3 azurerm_storage_account`, `1 azurerm_resource_group`, `2 azurerm_virtual_network` |
| 🔄 Change | 3 | `2 azurerm_app_service`, `1 azurerm_sql_database` |
| ♻️ Replace | 1 | `1 azurerm_kubernetes_cluster` |
| ❌ Destroy | 3 | `2 azurerm_network_security_group`, `1 azurerm_public_ip` |
| **Total** | **13** | |

**Pros:**
- Visually distinguishes resource types
- Clear separation between items
- Professional appearance

**Cons:**
- Slightly more verbose visually
- May not provide significant value over plain text

---

## Option 5: Short names only (type count summary)

| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | storage_account (3), resource_group (1), virtual_network (2) |
| 🔄 Change | 3 | app_service (2), sql_database (1) |
| ♻️ Replace | 1 | kubernetes_cluster (1) |
| ❌ Destroy | 3 | network_security_group (2), public_ip (1) |
| **Total** | **13** | |

**Pros:**
- More compact than full type names
- Reduces visual clutter
- Easier to scan

**Cons:**
- Loses provider information (azurerm)
- Less precise for users who need exact resource types

---

## Option 6: No breakdown for single-type rows

| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | 3 azurerm_storage_account, 1 azurerm_resource_group, 2 azurerm_virtual_network |
| 🔄 Change | 3 | 2 azurerm_app_service, 1 azurerm_sql_database |
| ♻️ Replace | 1 | azurerm_kubernetes_cluster |
| ❌ Destroy | 3 | 2 azurerm_network_security_group, 1 azurerm_public_ip |
| **Total** | **13** | |

**Pros:**
- Cleaner when only one type affected
- Reduces redundancy (no "1" prefix for single items)

**Cons:**
- Inconsistent formatting between rows
