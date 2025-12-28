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

### 📦 Module: root

<!-- tfplan2md:resource-start address=azurerm_resource_group.breaking_name -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_resource_group <b><code>breaking_name</code></b> — <code>rg-with-pipe&#124;and*asterisk</code> (<code>🌍 eastus</code>)</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| location | `🌍 eastus` |
| name | `rg-with-pipe\|and*asterisk` |

**🏷️ Tags:** `description: This has a \| pipe and a <br/> newline` `owner: [bracket] user`

<br/>
<details>
<summary>Large values: tags.description (3 lines, 3 changed)</summary>

##### **tags.description:**

```
This has a | pipe and a 
 newline
```

</details>

</details>
<!-- tfplan2md:resource-end address=azurerm_resource_group.breaking_name -->

<!-- tfplan2md:resource-start address=azurerm_storage_account.multiline -->
<details style="margin-bottom:12px;">
<summary>🔄 azurerm_storage_account <b><code>multiline</code></b> — | 1 🔧 tags.note</summary>
<br>

<br/>
<details>
<summary>Large values: tags.note (2 lines, 1 changed)</summary>

##### **tags.note:**

<pre style="font-family: monospace; line-height: 1.5;"><code>line1
<span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">+ line2</span>
</code></pre>

</details>

</details>
<!-- tfplan2md:resource-end address=azurerm_storage_account.multiline -->
