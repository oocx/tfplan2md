# Terraform Plan Report

**Terraform Version:** 1.14.0

## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 12 | 1 azurerm\_firewall\_network\_rule\_collection<br/>1 azurerm\_key\_vault<br/>1 azurerm\_key\_vault\_secret<br/>2 azurerm\_log\_analytics\_workspace<br/>1 azurerm\_resource\_group<br/>2 azurerm\_role\_assignment<br/>1 azurerm\_storage\_account<br/>1 azurerm\_subnet<br/>2 azurerm\_virtual\_network |
| 🔄 Change | 5 | 1 azurerm\_firewall\_network\_rule\_collection<br/>1 azurerm\_key\_vault<br/>2 azurerm\_storage\_account<br/>1 azurerm\_virtual\_network |
| ♻️ Replace | 2 | 1 azurerm\_network\_security\_group<br/>1 azurerm\_subnet |
| ❌ Destroy | 3 | 1 azurerm\_role\_assignment<br/>1 azurerm\_storage\_account<br/>1 azurerm\_virtual\_network |
| **Total** | **42** | |

## Resource Changes

### Module: root

#### ➕ azurerm\_resource\_group.core

<details>

| Attribute | Value |
| ----------- | ------- |
| `location` | eastus |
| `name` | rg-tfplan2md-demo |
| `tags.environment` | demo |
| `tags.owner` | tfplan2md |

</details>

#### ➕ azurerm\_storage\_account.logs

<details>

| Attribute | Value |
| ----------- | ------- |
| `account\_replication\_type` | LRS |
| `account\_tier` | Standard |
| `allow\_blob\_public\_access` | false |
| `location` | eastus |
| `min\_tls\_version` | TLS1\_2 |
| `name` | sttfplan2mdlogs |
| `resource\_group\_name` | rg-tfplan2md-demo |
| `tags.cost\_center` | ops |
| `tags.environment` | demo |

</details>

#### 🔄 azurerm\_storage\_account.data

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `account\_replication\_type` | LRS | GRS |
| `account\_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `name` | sttfplan2mddata | sttfplan2mddata |
| `resource\_group\_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `tags.cost\_center` | - | 1234 |
| `tags.environment` | demo | demo |
| `tags.owner` | data | data |

</details>

#### ❌ azurerm\_storage\_account.legacy

<details>

| Attribute | Value |
| ----------- | ------- |
| `account\_replication\_type` | LRS |
| `account\_tier` | Standard |
| `location` | eastus |
| `name` | sttfplan2mdlegacy |
| `resource\_group\_name` | rg-old |
| `tags.environment` | old |

</details>

---

### Module: `module.network`

#### ➕ module.network.azurerm\_virtual\_network.hub

<details>

| Attribute | Value |
| ----------- | ------- |
| `address\_space\[0\]` | 10.0.0.0/16 |
| `location` | eastus |
| `name` | vnet-hub |
| `resource\_group\_name` | rg-tfplan2md-demo |

</details>

#### ➕ module.network.azurerm\_virtual\_network.spoke

<details>

| Attribute | Value |
| ----------- | ------- |
| `address\_space\[0\]` | 10.1.0.0/16 |
| `location` | eastus |
| `name` | vnet-spoke |
| `resource\_group\_name` | rg-tfplan2md-demo |

</details>

#### ➕ module.network.azurerm\_subnet.app

<details>

| Attribute | Value |
| ----------- | ------- |
| `address\_prefixes\[0\]` | 10.1.1.0/24 |
| `name` | snet-app |
| `resource\_group\_name` | rg-tfplan2md-demo |
| `service\_endpoints\[0\]` | Microsoft.Storage |
| `virtual\_network\_name` | vnet-spoke |

</details>

### ➕ module.network.azurerm\_firewall\_network\_rule\_collection.new\_public

**Collection:** `public-egress` | **Priority:** 110 | **Action:** Allow

#### Rules

| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| allow-http | TCP | 10.1.1.0/24 | \* | 80 | Allow outbound HTTP |
| allow-https | TCP | 10.1.1.0/24 | \* | 443 | Allow outbound HTTPS |

#### 🔄 module.network.azurerm\_virtual\_network.branch

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `address\_space\[0\]` | 10.2.0.0/16 | 10.2.0.0/16 |
| `address\_space\[1\]` | - | 10.3.0.0/16 |
| `location` | eastus | eastus |
| `name` | vnet-branch | vnet-branch |
| `resource\_group\_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |

</details>

### 🔄 module.network.azurerm\_firewall\_network\_rule\_collection.network\_rules

**Collection:** `network-rules` | **Priority:** 120 | **Action:** Allow

#### Rule Changes

| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
| -------- | ----------- | ----------- | ------------------ | ---------------------- | ------------------- | ------------- |
| ➕ | allow-web-secure | TCP | 10.1.1.0/24 | 10.1.3.0/24 | 443 | Secure web |
| ➕ | allow-log-ingest | TCP | 10.1.4.0/24 | 10.1.5.0/24 | 8080 | Log ingestion |
| 🔄 | allow-dns | UDP | - 10.1.1.0/24\<br\>+ 10.1.1.0/24, 10.1.2.0/24 | 168.63.129.16 | 53 | DNS to Azure |
| 🔄 | allow-api | TCP | 10.1.1.0/24 | - 10.1.2.0/24\<br\>+ 10.2.2.0/24 | - 8443\<br\>+ 8443, 9443 | API tier |
| ❌ | allow-web | TCP | 10.1.1.0/24 | 10.1.3.0/24 | 80 | Legacy HTTP |
| ⏺️ | allow-monitoring | TCP | 10.1.1.0/24 | 10.1.4.0/24 | 443 | Monitoring |

#### ♻️ module.network.azurerm\_subnet.db

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `address\_prefixes\[0\]` | 10.1.2.0/24 | 10.1.20.0/24 |
| `name` | snet-db | snet-db |
| `resource\_group\_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `virtual\_network\_name` | vnet-spoke | vnet-spoke |

</details>

#### ♻️ module.network.azurerm\_network\_security\_group.app

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `location` | eastus | eastus |
| `name` | nsg-app | nsg-app |
| `resource\_group\_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `security\_rule\[0\].access` | Allow | Allow |
| `security\_rule\[0\].destination\_address\_prefix` | \* | \* |
| `security\_rule\[0\].destination\_port\_range` | 80 | 443 |
| `security\_rule\[0\].direction` | Inbound | Inbound |
| `security\_rule\[0\].name` | allow-http | allow-https |
| `security\_rule\[0\].priority` | 100 | 100 |
| `security\_rule\[0\].protocol` | Tcp | Tcp |
| `security\_rule\[0\].source\_address\_prefix` | \* | \* |
| `security\_rule\[0\].source\_port\_range` | \* | \* |

</details>

#### ❌ module.network.azurerm\_virtual\_network.decom

<details>

| Attribute | Value |
| ----------- | ------- |
| `address\_space\[0\]` | 10.50.0.0/16 |
| `location` | eastus |
| `name` | vnet-old |
| `resource\_group\_name` | rg-old |

</details>

---

### Module: `module.security`

#### ➕ module.security.azurerm\_role\_assignment.rg\_reader

**Summary:** `Jane Doe \(User\)` → `Reader` on `rg-tfplan2md-demo`

<details>
| Attribute | Value |
| ----------- | ------- |

| `scope` | rg-tfplan2md-demo in subscription 12345678-1234-1234-1234-123456789012 |

| `role\_definition\_id` | Reader \(acdd72a7-3385-48ef-bd42-f606fba81ae7\) |

| `principal\_id` | Jane Doe \(User\) \[00000000-0000-0000-0000-000000000001\] |

</details>

#### ➕ module.security.azurerm\_role\_assignment.storage\_reader

**Summary:** `DevOps Team \(Group\)` → `Storage Blob Data Reader` on Storage Account `sttfplan2mdlogs`

<details>
| Attribute | Value |
| ----------- | ------- |

| `scope` | Storage Account sttfplan2mdlogs in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 |

| `role\_definition\_id` | Storage Blob Data Reader \(2a2b9908-6ea1-4ae2-8e65-a410df84e7d1\) |

| `principal\_id` | DevOps Team \(Group\) \[00000000-0000-0000-0000-000000000002\] |

</details>

#### ➕ module.security.azurerm\_key\_vault.main

<details>

| Attribute | Value |
| ----------- | ------- |
| `enabled\_for\_deployment` | true |
| `location` | eastus |
| `name` | kv-tfplan2md |
| `public\_network\_access\_enabled` | true |
| `resource\_group\_name` | rg-tfplan2md-demo |
| `sku\_name` | standard |
| `tags.environment` | demo |
| `tags.owner` | security |
| `tenant\_id` | 11111111-2222-3333-4444-555555555555 |

</details>

#### ➕ module.security.azurerm\_log\_analytics\_workspace.security

<details>

| Attribute | Value |
| ----------- | ------- |
| `location` | eastus |
| `name` | law-security |
| `resource\_group\_name` | rg-tfplan2md-demo |
| `retention\_in\_days` | 90 |
| `sku` | PerGB2018 |

</details>

#### ➕ module.security.azurerm\_key\_vault\_secret.db\_password

<details>

| Attribute | Value |
| ----------- | ------- |
| `content\_type` | password |
| `key\_vault\_id` | /subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-tfplan2md-demo/providers/Microsoft.KeyVault/vaults/kv-tfplan2md |
| `name` | db-password |
| `value` | \(sensitive\) |

</details>

#### 🔄 module.security.azurerm\_storage\_account.analytics

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `account\_replication\_type` | LRS | ZRS |
| `account\_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `min\_tls\_version` | - | TLS1\_2 |
| `name` | sttfplan2mdanalytics | sttfplan2mdanalytics |
| `resource\_group\_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `tags.environment` | demo | demo |
| `tags.owner` | analytics | analytics |
| `tags.retention` | - | long |

</details>

#### 🔄 module.security.azurerm\_key\_vault.audit

<details>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| `enabled\_for\_deployment` | true | true |
| `location` | eastus | eastus |
| `name` | kv-audit | kv-audit |
| `public\_network\_access\_enabled` | true | false |
| `resource\_group\_name` | rg-tfplan2md-demo | rg-tfplan2md-demo |
| `sku\_name` | standard | standard |
| `tags.environment` | demo | demo |
| `tags.owner` | security | security |
| `tags.tier` | - | gold |
| `tenant\_id` | 11111111-2222-3333-4444-555555555555 | 11111111-2222-3333-4444-555555555555 |

</details>

#### ❌ module.security.azurerm\_role\_assignment.obsolete

**Summary:** remove `Reader` on `rg-old` from `00000000-0000-0000-0000-000000000005`

<details>
| Attribute | Value |
| ----------- | ------- |

| `scope` | rg-old in subscription 12345678-1234-1234-1234-123456789012 |

| `role\_definition\_id` | Reader \(acdd72a7-3385-48ef-bd42-f606fba81ae7\) |

| `principal\_id` | 00000000-0000-0000-0000-000000000005 \[00000000-0000-0000-0000-000000000005\] |

</details>

### Module: `module.network.module.monitoring`

#### ➕ module.network.module.monitoring.azurerm\_log\_analytics\_workspace.core

<details>

| Attribute | Value |
| ----------- | ------- |
| `location` | eastus |
| `name` | law-core |
| `resource\_group\_name` | rg-tfplan2md-demo |
| `retention\_in\_days` | 30 |
| `sku` | PerGB2018 |

</details>

---
