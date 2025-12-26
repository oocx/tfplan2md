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

### Module: root

#### ➕ azurerm_resource_group.core

**Summary:** `rg-tfplan2md-demo` (`eastus`)

<details>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| tags.environment | `demo` |
| tags.owner | `tfplan2md` |

</details>

#### ➕ azurerm_storage_account.logs

**Summary:** `sttfplan2mdlogs` in `rg-tfplan2md-demo` (`eastus`) | `Standard LRS`

<details>

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| allow_blob_public_access | `false` |
| location | `eastus` |
| min_tls_version | `TLS1_2` |
| name | `sttfplan2mdlogs` |
| resource_group_name | `rg-tfplan2md-demo` |
| tags.cost_center | `ops` |
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

#### ❌ azurerm_storage_account.legacy

**Summary:** `sttfplan2mdlegacy`

<details>

| Attribute | Value |
| ----------- | ------- |
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| location | `eastus` |
| name | `sttfplan2mdlegacy` |
| resource_group_name | `rg-old` |
| tags.environment | `old` |

</details>

### Module: `module.network`

#### ➕ module.network.azurerm_virtual_network.hub

**Summary:** `vnet-hub` in `rg-tfplan2md-demo` (`eastus`) | `10.0.0.0/16`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.0.0.0/16` |
| location | `eastus` |
| name | `vnet-hub` |
| resource_group_name | `rg-tfplan2md-demo` |

</details>

#### ➕ module.network.azurerm_virtual_network.spoke

**Summary:** `vnet-spoke` in `rg-tfplan2md-demo` (`eastus`) | `10.1.0.0/16`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.1.0.0/16` |
| location | `eastus` |
| name | `vnet-spoke` |
| resource_group_name | `rg-tfplan2md-demo` |

</details>

#### ➕ module.network.azurerm_subnet.app

**Summary:** `snet-app` | `vnet-spoke` | `10.1.1.0/24`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_prefixes[0] | `10.1.1.0/24` |
| name | `snet-app` |
| resource_group_name | `rg-tfplan2md-demo` |
| service_endpoints[0] | `Microsoft.Storage` |
| virtual_network_name | `vnet-spoke` |

</details>

### ➕ module.network.azurerm_firewall_network_rule_collection.new_public

**Collection:** `public-egress` | **Priority:** `110` | **Action:** `Allow`

#### Rules

| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| `allow-http` | `TCP` | `10.1.1.0/24` | `*` | `80` | `Allow outbound HTTP` |
| `allow-https` | `TCP` | `10.1.1.0/24` | `*` | `443` | `Allow outbound HTTPS` |

#### 🔄 module.network.azurerm_virtual_network.branch

**Summary:** `vnet-branch` | Changed: address_space[1]

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_space[1] | - | `10.3.0.0/16` |

</details>

### 🔄 module.network.azurerm_firewall_network_rule_collection.network_rules

**Collection:** `network-rules` | **Priority:** `120` | **Action:** `Allow`

#### Rule Changes

| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| -------- | ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| ➕ | `allow-web-secure` | `TCP` | `10.1.1.0/24` | `10.1.3.0/24` | `443` | `Secure web` |
| ➕ | `allow-log-ingest` | `TCP` | `10.1.4.0/24` | `10.1.5.0/24` | `8080` | `Log ingestion` |
| 🔄 | `allow-dns` | <code>UDP</code> | <code><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: -4px;">- 10.1.1.0/24</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: -4px;">+ 10.1.1.0/24<span style="background-color: #acf2bd; color: #24292e;">, 10.1.2.0/24</span></span></code> | <code>168.63.129.16</code> | <code>53</code> | <code>DNS to Azure</code> |
| 🔄 | `allow-api` | <code>TCP</code> | <code>10.1.1.0/24</code> | <code><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: -4px;">- 10.<span style="background-color: #ffc0c0; color: #24292e;">1</span>.2.0/24</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: -4px;">+ 10.<span style="background-color: #acf2bd; color: #24292e;">2</span>.2.0/24</span></code> | <code><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: -4px;">- 8443</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: -4px;">+ 8443<span style="background-color: #acf2bd; color: #24292e;">, 9443</span></span></code> | <code>API tier</code> |
| ❌ | `allow-web` | `TCP` | `10.1.1.0/24` | `10.1.3.0/24` | `80` | `Legacy HTTP` |
| ⏺️ | `allow-monitoring` | `TCP` | `10.1.1.0/24` | `10.1.4.0/24` | `443` | `Monitoring` |

#### ♻️ module.network.azurerm_subnet.db

**Summary:** recreating `snet-db` (1 changed)

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_prefixes[0] | `10.1.2.0/24` | `10.1.20.0/24` |

</details>

### ♻️ module.network.azurerm_network_security_group.app

**Network Security Group:** `nsg-app`

#### Security Rules

| Change | Name | Priority | Direction | Access | Protocol | Source Addresses | Source Ports | Destination Addresses | Destination Ports | Description |
| -------- | ------ | ---------- | ----------- | -------- | ---------- | ------------------ | ------------ | ---------------------- | ------------------- | ------------- |
| ➕ | `allow-https` | `100` | `Inbound` | `Allow` | `Tcp` | `*` | `*` | `*` | `443` | `-` |
| ❌ | `allow-http` | `100` | `Inbound` | `Allow` | `Tcp` | `*` | `*` | `*` | `80` | `-` |

#### ❌ module.network.azurerm_virtual_network.decom

**Summary:** `vnet-old`

<details>

| Attribute | Value |
| ----------- | ------- |
| address_space[0] | `10.50.0.0/16` |
| location | `eastus` |
| name | `vnet-old` |
| resource_group_name | `rg-old` |

</details>

### Module: `module.security`

#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** `Jane Doe (User)` → `Reader` on `rg-tfplan2md-demo`

<details>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `12345678-1234-1234-1234-123456789012` |
| role_definition_id | `Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |
| principal_id | `Jane Doe (User)` [`00000000-0000-0000-0000-000000000001`] |

</details>

#### ➕ module.security.azurerm_role_assignment.storage_reader

**Summary:** `DevOps Team (Group)` → `Storage Blob Data Reader` on Storage Account `sttfplan2mdlogs`

<details>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-tfplan2md-demo` in subscription `12345678-1234-1234-1234-123456789012` |
| role_definition_id | `Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) |
| principal_id | `DevOps Team (Group)` [`00000000-0000-0000-0000-000000000002`] |

</details>

#### ➕ module.security.azurerm_key_vault.main

**Summary:** `kv-tfplan2md` in `rg-tfplan2md-demo` (`eastus`) | `standard`

<details>

| Attribute | Value |
| ----------- | ------- |
| enabled_for_deployment | `true` |
| location | `eastus` |
| name | `kv-tfplan2md` |
| public_network_access_enabled | `true` |
| resource_group_name | `rg-tfplan2md-demo` |
| sku_name | `standard` |
| tags.environment | `demo` |
| tags.owner | `security` |
| tenant_id | `11111111-2222-3333-4444-555555555555` |

</details>

#### ➕ module.security.azurerm_log_analytics_workspace.security

**Summary:** `law-security` in `rg-tfplan2md-demo` (`eastus`) | `PerGB2018`

<details>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `law-security` |
| resource_group_name | `rg-tfplan2md-demo` |
| retention_in_days | `90` |
| sku | `PerGB2018` |

</details>

#### ➕ module.security.azurerm_key_vault_secret.db_password

**Summary:** `db-password` | Key Vault **kv-tfplan2md** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012**

<details>

| Attribute | Value |
| ----------- | ------- |
| content_type | `password` |
| key_vault_id | Key Vault **kv-tfplan2md** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| name | `db-password` |
| value | `super-secret-value` |

</details>

#### 🔄 module.security.azurerm_key_vault_secret.audit_policy

**Summary:** `audit-policy` | Changed: value

<details>
<summary>Large values: value (4 lines, 2 changed)</summary>

##### **value:**

<pre style="font-family: monospace; line-height: 1.5;"><code>line1: allow
<span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">- line2: log <span style="background-color: #ffc0c0; color: #24292e;">o</span>l<span style="background-color: #ffc0c0; color: #24292e;">d</span> activity</span>
<span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">+ line2: log <span style="background-color: #acf2bd; color: #24292e;">critica</span>l activity</span>
line3: end
</code></pre>

</details>

#### 🔄 module.security.azurerm_storage_account.analytics

**Summary:** `sttfplan2mdanalytics` | Changed: account_replication_type, min_tls_version, tags.retention

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `ZRS` |
| min_tls_version | - | `TLS1_2` |
| tags.retention | - | `long` |

</details>

#### 🔄 module.security.azurerm_key_vault.audit

**Summary:** `kv-audit` | Changed: public_network_access_enabled, tags.tier

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| public_network_access_enabled | `true` | `false` |
| tags.tier | - | `gold` |

</details>

#### ❌ module.security.azurerm_role_assignment.obsolete

**Summary:** remove `Reader` on `rg-old` from `00000000-0000-0000-0000-000000000005`

<details>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-old` in subscription `12345678-1234-1234-1234-123456789012` |
| role_definition_id | `Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |
| principal_id | `00000000-0000-0000-0000-000000000005` [`00000000-0000-0000-0000-000000000005`] |

</details>

### Module: `module.network.module.monitoring`

#### ➕ module.network.module.monitoring.azurerm_log_analytics_workspace.core

**Summary:** `law-core` in `rg-tfplan2md-demo` (`eastus`) | `PerGB2018`

<details>

| Attribute | Value |
| ----------- | ------- |
| location | `eastus` |
| name | `law-core` |
| resource_group_name | `rg-tfplan2md-demo` |
| retention_in_days | `30` |
| sku | `PerGB2018` |

</details>
