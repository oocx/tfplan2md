# Terraform Plan Report

**Terraform Version:** 1.14.0

## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 1 | 1 azurerm_resource_group |
| 🔄 Change | 1 | 1 azurerm_storage_account |
| ♻️ Replace | 0 |  |
| ❌ Destroy | 0 |  |
| **Total** | **2** | |

## Resource Changes

### Module: root

#### ➕ azurerm_resource_group.breaking_name

**Summary:** `rg-with-pipe\|and*asterisk` (eastus)

<details>

| Attribute | Value |
| ----------- | ------- |
| `location` | eastus |
| `name` | rg-with-pipe\|and*asterisk |
| `tags.description` | This has a \| pipe and a <br/> newline |
| `tags.owner` | [bracket] user |

</details>

#### 🔄 azurerm_storage_account.multiline

**Summary:** `azurerm_storage_account.multiline` | Changed: tags.note

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `tags.note` | line1 | line1<br/>line2 |

</details>

---
