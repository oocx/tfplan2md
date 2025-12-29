# Terraform Plan Report

**Terraform Version:** 1.14.0

## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 1 | 1 azurerm_firewall_network_rule_collection |
| 🔄 Change | 1 | 1 azurerm_firewall_network_rule_collection |
| ♻️ Replace | 0 |  |
| ❌ Destroy | 1 | 1 azurerm_firewall_network_rule_collection |
| **Total** | **3** | |

## Resource Changes

### 📦 Module: root

<div style="margin-bottom:12px;">
<!-- tfplan2md:resource-start address=azurerm_firewall_network_rule_collection.web_tier -->

### 🔄 azurerm_firewall_network_rule_collection.web_tier

**Collection:** `web-tier-rules` | **Priority:** `100` | **Action:** `✅ Allow`

#### Rule Changes

| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| -------- | ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| ➕ | `allow-dns` | `📨 UDP` | `🌐 10.0.1.0/24`, `🌐 10.0.2.0/24` | `🌐 168.63.129.16` | `🔌 53` | `Allow DNS queries to Azure DNS` |
| 🔄 | `allow-http` | 🔗 TCP | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- 🌐 10.0.1.0/24</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 🌐 10.0.1.0/24<span style="background-color: #acf2bd; color: #24292e;">, 🌐 10.0.3.0/24</span></span></code> | ✳️ | 🔌 80 | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- Allow HTTP traffic</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ Allow HTTP traffic<span style="background-color: #acf2bd; color: #24292e;"> from web and API tiers</span></span></code> |
| ❌ | `allow-ssh-old` | `🔗 TCP` | `🌐 10.0.0.0/8` | `🌐 10.0.2.0/24` | `🔌 22` | `Legacy SSH access - to be removed` |
| ⏺️ | `allow-https` | `🔗 TCP` | `🌐 10.0.1.0/24` | `✳️` | `🔌 443` | `Allow HTTPS traffic to internet` |

<!-- tfplan2md:resource-end address=azurerm_firewall_network_rule_collection.web_tier -->

</div>

<div style="margin-bottom:12px;">
<!-- tfplan2md:resource-start address=azurerm_firewall_network_rule_collection.database_tier -->

### ➕ azurerm_firewall_network_rule_collection.database_tier

**Collection:** `database-tier-rules` | **Priority:** `200` | **Action:** `✅ Allow`

#### Rules

| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| `allow-sql` | `🔗 TCP` | `🌐 10.0.1.0/24` | `🌐 10.0.3.0/24` | `🔌 1433` | `Allow SQL Server connections from web tier` |
| `allow-mysql` | `🔗 TCP` | `🌐 10.0.1.0/24` | `🌐 10.0.3.0/24` | `🔌 3306` | `Allow MySQL connections from web tier` |

<!-- tfplan2md:resource-end address=azurerm_firewall_network_rule_collection.database_tier -->

</div>

<div style="margin-bottom:12px;">
<!-- tfplan2md:resource-start address=azurerm_firewall_network_rule_collection.legacy -->

### ❌ azurerm_firewall_network_rule_collection.legacy

**Collection:** `legacy-rules` | **Priority:** `500` | **Action:** `✅ Allow`

#### Rules (being deleted)

| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| `allow-ftp` | `🔗 TCP` | `✳️` | `🌐 10.0.5.0/24` | `🔌 21` | `Deprecated FTP access - security risk` |

<!-- tfplan2md:resource-end address=azurerm_firewall_network_rule_collection.legacy -->

</div>
