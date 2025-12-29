# Terraform Plan Report

**Terraform Version:** 1.14.0

## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 12 | 1 azurerm_firewall_network_rule_collection<br/>1 azurerm_key_vault<br/>1 azurerm_key_vault_secret<br/>2 azurerm_log_analytics_workspace<br/>1 azurerm_resource_group<br/>2 azurerm_role_assignment<br/>1 azurerm_storage_account<br/>1 azurerm_subnet<br/>2 azurerm_virtual_network |
| 🔄 Change | 6 | 1 azurerm_firewall_network_rule_collection<br/>1 azurerm_key_vault<br/>1 azurerm_key_vault_secret<br/>2 azurerm_storage_account<br/>1 azurerm_virtual_network |
| ♻️ Replace | 2 | 1 azurerm_network_security_group<br/>1 azurerm_subnet |
| ❌ Destroy | 3 | 1 azurerm_role_assignment<br/>1 azurerm_storage_account<br/>1 azurerm_virtual_network |
| **Total** | **23** | |

## Resource Changes

### 📦 Module: root

<!-- tfplan2md:resource-start address=azurerm_resource_group.core -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_resource_group <b><code>core</code></b> — <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| location | `🌍 eastus` |
| name | `rg-tfplan2md-demo` |

**🏷️ Tags:** `environment: demo` `owner: tfplan2md`

</details>
<!-- tfplan2md:resource-end address=azurerm_resource_group.core -->

<!-- tfplan2md:resource-start address=azurerm_storage_account.logs -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_storage_account <b><code>logs</code></b> — <code>sttfplan2mdlogs</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| allow_blob_public_access | `❌ false` |
| location | `🌍 eastus` |
| min_tls_version | `TLS1_2` |
| name | `sttfplan2mdlogs` |
| resource_group_name | `rg-tfplan2md-demo` |

**🏷️ Tags:** `cost_center: ops` `environment: demo`

</details>
<!-- tfplan2md:resource-end address=azurerm_storage_account.logs -->

<!-- tfplan2md:resource-start address=azurerm_storage_account.data -->
<details style="margin-bottom:12px;">
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>sttfplan2mddata</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code> | 2🔧 account_replication_type, tags.cost_center</summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| tags.cost_center | - | `1234` |

</details>
<!-- tfplan2md:resource-end address=azurerm_storage_account.data -->

<!-- tfplan2md:resource-start address=azurerm_storage_account.legacy -->
<details style="margin-bottom:12px;">
<summary>❌ azurerm_storage_account <b><code>legacy</code></b> — <code>sttfplan2mdlegacy</code> in <code>rg-old</code> <code>🌍 eastus</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| location | `🌍 eastus` |
| name | `sttfplan2mdlegacy` |
| resource_group_name | `rg-old` |

**🏷️ Tags:** `environment: old`

</details>
<!-- tfplan2md:resource-end address=azurerm_storage_account.legacy -->

---

### 📦 Module: `module.network`

<!-- tfplan2md:resource-start address=module.network.azurerm_virtual_network.hub -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code> <code>🌐 10.0.0.0/16</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `🌐 10.0.0.0/16` |
| location | `🌍 eastus` |
| name | `vnet-hub` |
| resource_group_name | `rg-tfplan2md-demo` |

</details>
<!-- tfplan2md:resource-end address=module.network.azurerm_virtual_network.hub -->

<!-- tfplan2md:resource-start address=module.network.azurerm_virtual_network.spoke -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_virtual_network <b><code>spoke</code></b> — <code>vnet-spoke</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code> <code>🌐 10.1.0.0/16</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `🌐 10.1.0.0/16` |
| location | `🌍 eastus` |
| name | `vnet-spoke` |
| resource_group_name | `rg-tfplan2md-demo` |

</details>
<!-- tfplan2md:resource-end address=module.network.azurerm_virtual_network.spoke -->

<!-- tfplan2md:resource-start address=module.network.azurerm_subnet.app -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_subnet <b><code>app</code></b> — <code>snet-app</code> in <code>rg-tfplan2md-demo</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| address_prefixes[0] | `🌐 10.1.1.0/24` |
| name | `snet-app` |
| resource_group_name | `rg-tfplan2md-demo` |
| service_endpoints[0] | `Microsoft.Storage` |
| virtual_network_name | `vnet-spoke` |

</details>
<!-- tfplan2md:resource-end address=module.network.azurerm_subnet.app -->

<div style="margin-bottom:12px;">
<!-- tfplan2md:resource-start address=module.network.azurerm_firewall_network_rule_collection.new_public -->

### ➕ module.network.azurerm_firewall_network_rule_collection.new_public

**Collection:** `public-egress` | **Priority:** `110` | **Action:** `✅ Allow`

#### Rules

| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| `allow-http` | `🔗 TCP` | `🌐 10.1.1.0/24` | `✳️` | `🔌 80` | `Allow outbound HTTP` |
| `allow-https` | `🔗 TCP` | `🌐 10.1.1.0/24` | `✳️` | `🔌 443` | `Allow outbound HTTPS` |

<!-- tfplan2md:resource-end address=module.network.azurerm_firewall_network_rule_collection.new_public -->

</div>

<!-- tfplan2md:resource-start address=module.network.azurerm_virtual_network.branch -->
<details style="margin-bottom:12px;">
<summary>🔄 azurerm_virtual_network <b><code>branch</code></b> — <code>vnet-branch</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code> <code>🌐 10.2.0.0/16</code> | 1🔧 address_space[1]</summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_space[1] | - | `🌐 10.3.0.0/16` |

</details>
<!-- tfplan2md:resource-end address=module.network.azurerm_virtual_network.branch -->

<div style="margin-bottom:12px;">
<!-- tfplan2md:resource-start address=module.network.azurerm_firewall_network_rule_collection.network_rules -->

### 🔄 module.network.azurerm_firewall_network_rule_collection.network_rules

**Collection:** `network-rules` | **Priority:** `120` | **Action:** `✅ Allow`

#### Rule Changes

| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| -------- | ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| ➕ | `allow-web-secure` | `🔗 TCP` | `🌐 10.1.1.0/24` | `🌐 10.1.3.0/24` | `🔌 443` | `Secure web` |
| ➕ | `allow-log-ingest` | `🔗 TCP` | `🌐 10.1.4.0/24` | `🌐 10.1.5.0/24` | `🔌 8080` | `Log ingestion` |
| ➕ | `allow-icmp-ping` | `📡 ICMP` | `🌐 10.1.1.0/24` | `🌐 10.1.4.0/24` | `✳️` | `ICMP ping for network diagnostics` |
| 🔄 | `allow-dns` | 📨 UDP | <code><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- 🌐 10.1.1.0/24</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 🌐 10.1.1.0/24<span style="background-color: #acf2bd; color: #24292e;">, 🌐 10.1.2.0/24</span></span></code> | 🌐 168.63.129.16 | 🔌 53 | <code>DNS to Azure</code> |
| 🔄 | `allow-api` | 🔗 TCP | 🌐 10.1.1.0/24 | <code><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- 🌐 10.<span style="background-color: #ffc0c0; color: #24292e;">1</span>.2.0/24</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 🌐 10.<span style="background-color: #acf2bd; color: #24292e;">2</span>.2.0/24</span></code> | <code><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- 🔌 8443</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 🔌 8443<span style="background-color: #acf2bd; color: #24292e;">, 🔌 9443</span></span></code> | <code>API tier</code> |
| ❌ | `allow-web` | `🔗 TCP` | `🌐 10.1.1.0/24` | `🌐 10.1.3.0/24` | `🔌 80` | `Legacy HTTP` |
| ⏺️ | `allow-monitoring` | `🔗 TCP` | `🌐 10.1.1.0/24` | `🌐 10.1.4.0/24` | `🔌 443` | `Monitoring` |

<!-- tfplan2md:resource-end address=module.network.azurerm_firewall_network_rule_collection.network_rules -->

</div>

<!-- tfplan2md:resource-start address=module.network.azurerm_subnet.db -->
<details style="margin-bottom:12px;">
<summary>♻️ azurerm_subnet <b><code>db</code></b> — <code>snet-db</code> in <code>rg-tfplan2md-demo</code></summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_prefixes[0] | `🌐 10.1.2.0/24` | `🌐 10.1.20.0/24` |

</details>
<!-- tfplan2md:resource-end address=module.network.azurerm_subnet.db -->

<div style="margin-bottom:12px;">
<!-- tfplan2md:resource-start address=module.network.azurerm_network_security_group.app -->

### ♻️ module.network.azurerm_network_security_group.app

**Network Security Group:** `nsg-app`

#### Security Rules

| Change | Name | Priority | Direction | Access | Protocol | Source Addresses | Source Ports | Destination Addresses | Destination Ports | Description |
| -------- | ------ | ---------- | ----------- | -------- | ---------- | ------------------ | ------------ | ---------------------- | ------------------- | ------------- |
| ➕ | `allow-https` | `100` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `✳️` | `🔌 443` | `-` |
| ➕ | `deny-rdp` | `200` | `⬇️ Inbound` | `⛔ Deny` | `🔗 TCP` | `✳️` | `✳️` | `✳️` | `🔌 3389` | `Block RDP from internet` |
| ➕ | `allow-outbound-https` | `300` | `⬆️ Outbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `✳️` | `🔌 443` | `Allow outbound HTTPS` |
| ❌ | `allow-http` | `100` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `✳️` | `🔌 80` | `-` |

<!-- tfplan2md:resource-end address=module.network.azurerm_network_security_group.app -->

</div>

<!-- tfplan2md:resource-start address=module.network.azurerm_virtual_network.decom -->
<details style="margin-bottom:12px;">
<summary>❌ azurerm_virtual_network <b><code>decom</code></b> — <code>vnet-old</code> in <code>rg-old</code> <code>🌍 eastus</code> <code>🌐 10.50.0.0/16</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `🌐 10.50.0.0/16` |
| location | `🌍 eastus` |
| name | `vnet-old` |
| resource_group_name | `rg-old` |

</details>
<!-- tfplan2md:resource-end address=module.network.azurerm_virtual_network.decom -->

---

### 📦 Module: `module.security`

<!-- tfplan2md:resource-start address=module.security.azurerm_role_assignment.rg_reader -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_role_assignment <b><code>rg_reader</code></b> — <code>👤 Jane Doe (User)</code> → <code>🛡️ Reader</code> on <code>rg-tfplan2md-demo</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `12345678-1234-1234-1234-123456789012` |
| role_definition_id | `🛡️ Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |
| principal_id | `👤 Jane Doe (User)` (`👤 User`) [`00000000-0000-0000-0000-000000000001`] |
| principal_type | `👤 User` |
| role_definition_name | `🛡️ Reader` |

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_role_assignment.rg_reader -->

<!-- tfplan2md:resource-start address=module.security.azurerm_role_assignment.storage_reader -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_role_assignment <b><code>storage_reader</code></b> — <code>👥 DevOps Team (Group)</code> → <code>🛡️ Storage Blob Data Reader</code> on Storage Account <code>sttfplan2mdlogs</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `12345678-1234-1234-1234-123456789012` |
| role_definition_id | `🛡️ Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) |
| principal_id | `👥 DevOps Team (Group)` (`👥 Group`) [`00000000-0000-0000-0000-000000000002`] |
| principal_type | `👥 Group` |
| role_definition_name | `🛡️ Storage Blob Data Reader` |

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_role_assignment.storage_reader -->

<!-- tfplan2md:resource-start address=module.security.azurerm_key_vault.main -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_key_vault <b><code>main</code></b> — <code>kv-tfplan2md</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| enabled_for_deployment | `✅ true` |
| location | `🌍 eastus` |
| name | `kv-tfplan2md` |
| public_network_access_enabled | `✅ true` |
| resource_group_name | `rg-tfplan2md-demo` |
| sku_name | `standard` |
| tenant_id | `11111111-2222-3333-4444-555555555555` |

**🏷️ Tags:** `environment: demo` `owner: security`

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_key_vault.main -->

<!-- tfplan2md:resource-start address=module.security.azurerm_log_analytics_workspace.security -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_log_analytics_workspace <b><code>security</code></b> — <code>law-security</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| location | `🌍 eastus` |
| name | `law-security` |
| resource_group_name | `rg-tfplan2md-demo` |
| retention_in_days | `90` |
| sku | `PerGB2018` |

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_log_analytics_workspace.security -->

<!-- tfplan2md:resource-start address=module.security.azurerm_key_vault_secret.db_password -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_key_vault_secret <b><code>db_password</code></b> — <code>db-password</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| content_type | `password` |
| key_vault_id | Key Vault `kv-tfplan2md` in resource group `rg-tfplan2md-demo` of subscription `12345678-1234-1234-1234-123456789012` |
| name | `db-password` |
| value | `(sensitive)` |

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_key_vault_secret.db_password -->

<!-- tfplan2md:resource-start address=module.security.azurerm_key_vault_secret.audit_policy -->
<details style="margin-bottom:12px;">
<summary>🔄 azurerm_key_vault_secret <b><code>audit_policy</code></b> — <code>audit-policy</code> | 1🔧 value</summary>
<br>

<br/>
<details>
<summary>Large values: value (4 lines, 2 changed)</summary>

##### **value:**

<pre style="font-family: monospace; line-height: 1.5;"><code>line1: allow
<span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: 0;">- line2: log <span style="background-color: #ffc0c0; color: #24292e;">o</span>l<span style="background-color: #ffc0c0; color: #24292e;">d</span> activity</span>
<span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: 0;">+ line2: log <span style="background-color: #acf2bd; color: #24292e;">critica</span>l activity</span>
line3: end
</code></pre>

</details>

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_key_vault_secret.audit_policy -->

<!-- tfplan2md:resource-start address=module.security.azurerm_storage_account.analytics -->
<details style="margin-bottom:12px;">
<summary>🔄 azurerm_storage_account <b><code>analytics</code></b> — <code>sttfplan2mdanalytics</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code> | 3🔧 account_replication_type, min_tls_version, tags.retention</summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `ZRS` |
| min_tls_version | - | `TLS1_2` |
| tags.retention | - | `long` |

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_storage_account.analytics -->

<!-- tfplan2md:resource-start address=module.security.azurerm_key_vault.audit -->
<details style="margin-bottom:12px;">
<summary>🔄 azurerm_key_vault <b><code>audit</code></b> — <code>kv-audit</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code> | 2🔧 public_network_access_enabled, tags.tier</summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| public_network_access_enabled | `✅ true` | `❌ false` |
| tags.tier | - | `gold` |

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_key_vault.audit -->

<!-- tfplan2md:resource-start address=module.security.azurerm_role_assignment.obsolete -->
<details style="margin-bottom:12px;">
<summary>❌ azurerm_role_assignment <b><code>obsolete</code></b> — remove <code>🛡️ Reader</code> on <code>rg-old</code> from <code>💻 Legacy App (Service Principal)</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-old` in subscription `12345678-1234-1234-1234-123456789012` |
| role_definition_id | `🛡️ Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |
| principal_id | `💻 Legacy App (Service Principal)` (`💻 ServicePrincipal`) [`00000000-0000-0000-0000-000000000005`] |
| principal_type | `💻 ServicePrincipal` |
| role_definition_name | `🛡️ Reader` |

</details>
<!-- tfplan2md:resource-end address=module.security.azurerm_role_assignment.obsolete -->

---

### 📦 Module: `module.network.module.monitoring`

<!-- tfplan2md:resource-start address=module.network.module.monitoring.azurerm_log_analytics_workspace.core -->
<details style="margin-bottom:12px;">
<summary>➕ azurerm_log_analytics_workspace <b><code>core</code></b> — <code>law-core</code> in <code>rg-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| location | `🌍 eastus` |
| name | `law-core` |
| resource_group_name | `rg-tfplan2md-demo` |
| retention_in_days | `30` |
| sku | `PerGB2018` |

</details>
<!-- tfplan2md:resource-end address=module.network.module.monitoring.azurerm_log_analytics_workspace.core -->
