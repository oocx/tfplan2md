# Terraform Plan Report

**Terraform Version:** 1.14.0

## Summary

| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 12 | 1 azurerm_firewall_network_rule_collection<br/>1 azurerm_key_vault<br/>1 azurerm_key_vault_secret<br/>2 azurerm_log_analytics_workspace<br/>1 azurerm_resource_group<br/>2 azurerm_role_assignment<br/>1 azurerm_storage_account<br/>1 azurerm_subnet<br/>2 azurerm_virtual_network |
| 🔄 Change | 5 | 1 azurerm_firewall_network_rule_collection<br/>1 azurerm_key_vault<br/>2 azurerm_storage_account<br/>1 azurerm_virtual_network |
| ♻️ Replace | 2 | 1 azurerm_network_security_group<br/>1 azurerm_subnet |
| ❌ Destroy | 3 | 1 azurerm_role_assignment<br/>1 azurerm_storage_account<br/>1 azurerm_virtual_network |
| **Total** | **22** | |

## Resource Changes

### Module: root

#### ➕ azurerm_resource_group.core

<details>

| Attribute | Value |
|-----------|-------|
| `location` | eastus |
| `name` | rg-tfplan2md-demo |
| `tags.environment` | demo |
| `tags.owner` | tfplan2md |

</details>

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

#### 🔄 azurerm_storage_account.data

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

#### ❌ azurerm_storage_account.legacy

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

---

### Module: `module.network`

#### ➕ module.network.azurerm_virtual_network.hub

<details>

| Attribute | Value |
|-----------|-------|
| `address_space[0]` | 10.0.0.0/16 |
| `location` | eastus |
| `name` | vnet-hub |
| `resource_group_name` | rg-tfplan2md-demo |

</details>

#### ➕ module.network.azurerm_virtual_network.spoke

<details>

| Attribute | Value |
|-----------|-------|
| `address_space[0]` | 10.1.0.0/16 |
| `location` | eastus |
| `name` | vnet-spoke |
| `resource_group_name` | rg-tfplan2md-demo |

</details>

#### ➕ module.network.azurerm_subnet.app

<details>

| Attribute | Value |
|-----------|-------|
| `address_prefixes[0]` | 10.1.1.0/24 |
| `name` | snet-app |
| `resource_group_name` | rg-tfplan2md-demo |
| `service_endpoints[0]` | Microsoft.Storage |
| `virtual_network_name` | vnet-spoke |

</details>

### ➕ module.network.azurerm_firewall_network_rule_collection.new_public

**Collection:** `public-egress` | **Priority:** 110 | **Action:** Allow

#### Rules
| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
|-----------|-----------|------------------|----------------------|-------------------|-------------|
| allow-http | TCP | 10.1.1.0/24 | * | 80 | Allow outbound HTTP |
| allow-https | TCP | 10.1.1.0/24 | * | 443 | Allow outbound HTTPS |

#### 🔄 module.network.azurerm_virtual_network.branch

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `address_space[0]` | 10.2.0.0/16 | 10.2.0.0/16 |
| `address_space[1]` | - | 10.3.0.0/16 |
| `location` | eastus | eastus |
| `name` | vnet-branch | vnet-branch |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |

</details>

### 🔄 module.network.azurerm_firewall_network_rule_collection.network_rules

**Collection:** `network-rules` | **Priority:** 120 | **Action:** Allow

#### Rule Changes
| | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
|---|-----------|-----------|------------------|----------------------|-------------------|-------------|
| ➕ | allow-web-secure | TCP | 10.1.1.0/24 | 10.1.3.0/24 | 443 | Secure web |
| ➕ | allow-log-ingest | TCP | 10.1.4.0/24 | 10.1.5.0/24 | 8080 | Log ingestion |
| 🔄 | allow-dns | UDP | - 10.1.1.0/24<br>+ 10.1.1.0/24, 10.1.2.0/24 | 168.63.129.16 | 53 | DNS to Azure |
| 🔄 | allow-api | TCP | 10.1.1.0/24 | - 10.1.2.0/24<br>+ 10.2.2.0/24 | - 8443<br>+ 8443, 9443 | API tier |
| ❌ | allow-web | TCP | 10.1.1.0/24 | 10.1.3.0/24 | 80 | Legacy HTTP |
| ⏺️ | allow-monitoring | TCP | 10.1.1.0/24 | 10.1.4.0/24 | 443 | Monitoring |

#### ♻️ module.network.azurerm_subnet.db

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `address_prefixes[0]` | 10.1.2.0/24 | 10.1.20.0/24 |
| `name` | snet-db | snet-db |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `virtual_network_name` | vnet-spoke | vnet-spoke |

</details>

#### ♻️ module.network.azurerm_network_security_group.app

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `location` | eastus | eastus |
| `name` | nsg-app | nsg-app |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `security_rule[0].access` | Allow | Allow |
| `security_rule[0].destination_address_prefix` | * | * |
| `security_rule[0].destination_port_range` | 80 | 443 |
| `security_rule[0].direction` | Inbound | Inbound |
| `security_rule[0].name` | allow-http | allow-https |
| `security_rule[0].priority` | 100 | 100 |
| `security_rule[0].protocol` | Tcp | Tcp |
| `security_rule[0].source_address_prefix` | * | * |
| `security_rule[0].source_port_range` | * | * |

</details>

#### ❌ module.network.azurerm_virtual_network.decom

<details>

| Attribute | Value |
|-----------|-------|
| `address_space[0]` | 10.50.0.0/16 |
| `location` | eastus |
| `name` | vnet-old |
| `resource_group_name` | rg-old |

</details>

---

### Module: `module.security`

#### ➕ module.security.azurerm_role_assignment.rg_reader (create)

- **scope**: **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012**
- **role_definition_id**: Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)
- **principal_id**: Jane Doe (User) [00000000-0000-0000-0000-000000000001]

#### ➕ module.security.azurerm_role_assignment.storage_reader (create)

- **scope**: Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012**
- **role_definition_id**: Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1)
- **principal_id**: DevOps Team (Group) [00000000-0000-0000-0000-000000000002]

#### ➕ module.security.azurerm_key_vault.main

<details>

| Attribute | Value |
|-----------|-------|
| `enabled_for_deployment` | true |
| `location` | eastus |
| `name` | kv-tfplan2md |
| `public_network_access_enabled` | true |
| `resource_group_name` | rg-tfplan2md-demo |
| `sku_name` | standard |
| `tags.environment` | demo |
| `tags.owner` | security |
| `tenant_id` | 11111111-2222-3333-4444-555555555555 |

</details>

#### ➕ module.security.azurerm_log_analytics_workspace.security

<details>

| Attribute | Value |
|-----------|-------|
| `location` | eastus |
| `name` | law-security |
| `resource_group_name` | rg-tfplan2md-demo |
| `retention_in_days` | 90 |
| `sku` | PerGB2018 |

</details>

#### ➕ module.security.azurerm_key_vault_secret.db_password

<details>

| Attribute | Value |
|-----------|-------|
| `content_type` | password |
| `key_vault_id` | /subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-tfplan2md-demo/providers/Microsoft.KeyVault/vaults/kv-tfplan2md |
| `name` | db-password |
| `value` | super-secret-value |

</details>

#### 🔄 module.security.azurerm_storage_account.analytics

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `account_replication_type` | LRS | ZRS |
| `account_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `min_tls_version` | - | TLS1_2 |
| `name` | sttfplan2mdanalytics | sttfplan2mdanalytics |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `tags.environment` | demo | demo |
| `tags.owner` | analytics | analytics |
| `tags.retention` | - | long |

</details>

#### 🔄 module.security.azurerm_key_vault.audit

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `enabled_for_deployment` | true | true |
| `location` | eastus | eastus |
| `name` | kv-audit | kv-audit |
| `public_network_access_enabled` | true | false |
| `resource_group_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `sku_name` | standard | standard |
| `tags.environment` | demo | demo |
| `tags.owner` | security | security |
| `tags.tier` | - | gold |
| `tenant_id` | 11111111-2222-3333-4444-555555555555 | 11111111-2222-3333-4444-555555555555 |

</details>

#### ❌ module.security.azurerm_role_assignment.obsolete (delete)

- **scope**: **rg-old** in subscription **12345678-1234-1234-1234-123456789012**
- **role_definition_id**: Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)
- **principal_id**: 00000000-0000-0000-0000-000000000005

### Module: `module.network.module.monitoring`

#### ➕ module.network.module.monitoring.azurerm_log_analytics_workspace.core

<details>

| Attribute | Value |
|-----------|-------|
| `location` | eastus |
| `name` | law-core |
| `resource_group_name` | rg-tfplan2md-demo |
| `retention_in_days` | 30 |
| `sku` | PerGB2018 |

</details>

---


