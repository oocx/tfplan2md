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
| `tags.owner` | [bracket] user |

</details>

<details>
<summary>Large values: tags.description (3 lines, 3 changed)</summary>

##### `tags.description`

```
This has a | pipe and a 
 newline
```

</details>

#### 🔄 azurerm_storage_account.multiline

**Summary:** `azurerm_storage_account.multiline` | Changed: tags.note

<details>
<summary>Large values: tags.note (2 lines, 1 changed)</summary>

##### `tags.note`

<pre style="font-family: monospace; line-height: 1.5;"><code>line1
<span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">line2</span>
</code></pre>

</details>
