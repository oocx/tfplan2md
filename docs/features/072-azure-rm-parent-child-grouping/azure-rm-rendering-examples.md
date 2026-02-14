# Azure RM Parent-Child Resource Rendering Examples (Batch 2)

This document shows expected markdown output for the 4 Azure RM resource types being added in Feature 068 Batch 2:
- azurerm_virtual_network / azurerm_subnet
- azurerm_dns_zone / DNS records
- azurerm_route_table / azurerm_route
- azurerm_network_security_group / azurerm_network_security_rule

**Note:** Examples are shown as raw markdown (not in code blocks) for easy copy/paste into Azure DevOps PRs.

---

## Example 1: azurerm_virtual_network with Inline Subnets (Create)

**Scenario:** Creating a new virtual network with 3 subnets defined inline via the `subnet` attribute.

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_virtual_network <b><code>hub_vnet</code></b> — <code>🆔 vnet-hub</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | ➕ 3 subnets</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 vnet-hub` |
| location | `🌍 eastus` |
| resource_group_name | `📁 rg-network` |
| address_space | `🌐 10.0.0.0/16` |

#### Subnets

| Change | Name | Address Prefixes | NSG | Delegation | Terraform Resource |
| -------- | ------ | ------------------ | ----- | ------------ | -------------------- |
| ➕ | `🆔 snet-app` | `🌐 10.0.1.0/24` | `🛡️ nsg-app` | - | `subnet` attribute |
| ➕ | `🆔 snet-data` | `🌐 10.0.2.0/24` | `🛡️ nsg-data` | - | `subnet` attribute |
| ➕ | `🆔 snet-firewall` | `🌐 10.0.3.0/24` | - | `Microsoft.Network/azureFirewalls` | `subnet` attribute |

**🏷️ DNS Servers:** `🌐 10.0.0.4`, `🌐 10.0.0.5`

</details>

---

## Example 2: azurerm_virtual_network with Separate Subnets (Update)

**Scenario:** Updating a virtual network with subnets managed as separate `azurerm_subnet` resources.

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azurerm_virtual_network <b><code>spoke_vnet</code></b> — <code>🆔 vnet-spoke-001</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | 3🔧 ➕ 1 subnet, 🔄 1 subnet, ❌ 1 subnet</summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_space[1] | - | `🌐 10.2.0.0/16` |

#### Subnet Changes

| Change | Name | Address Prefixes | NSG | Delegation | Terraform Resource |
| -------- | ------ | ------------------ | ----- | ------------ | -------------------- |
| ➕ | `🆔 snet-integration` | `🌐 10.1.4.0/24` | - | `Microsoft.Web/serverFarms` | `azurerm_subnet.integration` |
| 🔄 | `🆔 snet-app` | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- 🌐 10.1.1.0/24</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 🌐 10.1.1.0/23</span></code> | `🛡️ nsg-app` | - | `azurerm_subnet.app` |
| ❌ | `🆔 snet-temp` | `🌐 10.1.5.0/24` | - | - | `azurerm_subnet.temp` |
| ⏺️ | `🆔 snet-data` | `🌐 10.1.2.0/24` | `🛡️ nsg-data` | - | `azurerm_subnet.data` |
| ⏺️ | `🆔 snet-mgmt` | `🌐 10.1.3.0/24` | `🛡️ nsg-mgmt` | - | `azurerm_subnet.mgmt` |

</details>

---

## Example 3: azurerm_virtual_network with Mixed Subnets (Warning)

**Scenario:** Virtual network with subnets managed both inline and as separate resources (conflict scenario).

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azurerm_virtual_network <b><code>legacy_vnet</code></b> — <code>🆔 vnet-legacy</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | Mixed subnet management detected</summary>
<br>

⚠️ **Warning:** This virtual network has subnets managed both inline (via `subnet` attribute) and as separate `azurerm_subnet` resources. This configuration will cause conflicts and overwrite subnets.

#### Subnet Changes

| Change | Name | Address Prefixes | NSG | Delegation | Terraform Resource |
| -------- | ------ | ------------------ | ----- | ------------ | -------------------- |
| ➕ | `🆔 snet-app` | `🌐 10.3.1.0/24` | - | - | `subnet` attribute |
| ➕ | `🆔 snet-data` | `🌐 10.3.2.0/24` | - | - | `subnet` attribute |
| ➕ | `🆔 snet-web` | `🌐 10.3.3.0/24` | `🛡️ nsg-web` | - | `azurerm_subnet.web` |

</details>

---

## Example 4: azurerm_dns_zone with Multiple Record Types (Create)

**Scenario:** Creating a new DNS zone with various record types.

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_dns_zone <b><code>example_com</code></b> — <code>🆔 example.com</code> in <code>📁 rg-dns</code> | ➕ 8 records</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 example.com` |
| resource_group_name | `📁 rg-dns` |

#### DNS Records

| Change | Name | Type | TTL | Value/Target | Terraform Resource |
| -------- | ------ | ------ | ----- | -------------- | -------------------- |
| ➕ | `@` | `A` | `3600` | `🌐 192.0.2.1` | `azurerm_dns_a_record.root` |
| ➕ | `www` | `A` | `3600` | `🌐 192.0.2.1` | `azurerm_dns_a_record.www` |
| ➕ | `www` | `AAAA` | `3600` | `🌐 2001:db8::1` | `azurerm_dns_aaaa_record.www` |
| ➕ | `blog` | `CNAME` | `3600` | `www.example.com` | `azurerm_dns_cname_record.blog` |
| ➕ | `@` | `MX` | `3600` | `10 mail.example.com` | `azurerm_dns_mx_record.root` |
| ➕ | `@` | `TXT` | `3600` | `"v=spf1 include:_spf.example.com ~all"` | `azurerm_dns_txt_record.spf` |
| ➕ | `_dmarc` | `TXT` | `3600` | `"v=DMARC1; p=quarantine; rua=mailto:dm..."` | `azurerm_dns_txt_record.dmarc` |
| ➕ | `@` | `CAA` | `3600` | `0 issue "letsencrypt.org"` | `azurerm_dns_caa_record.root` |

</details>

---

## Example 5: azurerm_dns_zone with Record Updates (Mixed Changes)

**Scenario:** Updating DNS records in an existing zone.

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>⏺️ azurerm_dns_zone <b><code>example_com</code></b> — <code>🆔 example.com</code> in <code>📁 rg-dns</code> | 4🔧 ➕ 2 records, 🔄 1 record, ❌ 1 record</summary>
<br>

#### DNS Record Changes

| Change | Name | Type | TTL | Value/Target | Terraform Resource |
| -------- | ------ | ------ | ----- | -------------- | -------------------- |
| ➕ | `api` | `A` | `300` | `🌐 192.0.2.10` | `azurerm_dns_a_record.api` |
| ➕ | `cdn` | `CNAME` | `3600` | `cdn.cloudprovider.com` | `azurerm_dns_cname_record.cdn` |
| 🔄 | `www` | `A` | `3600` | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- 🌐 192.0.2.1</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 🌐 192.0.2.2</span></code> | `azurerm_dns_a_record.www` |
| ❌ | `old` | `CNAME` | `3600` | `legacy.example.com` | `azurerm_dns_cname_record.old` |
| ⏺️ | `@` | `A` | `3600` | `🌐 192.0.2.1` | `azurerm_dns_a_record.root` |
| ⏺️ | `mail` | `A` | `3600` | `🌐 192.0.2.5` | `azurerm_dns_a_record.mail` |

</details>

---

## Example 6: azurerm_private_dns_zone with A Records

**Scenario:** Creating a private DNS zone with A records for internal services.

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_private_dns_zone <b><code>internal_example_com</code></b> — <code>🆔 internal.example.com</code> in <code>📁 rg-dns</code> | ➕ 5 records</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 internal.example.com` |
| resource_group_name | `📁 rg-dns` |

#### DNS Records

| Change | Name | Type | TTL | Value/Target | Terraform Resource |
| -------- | ------ | ------ | ----- | -------------- | -------------------- |
| ➕ | `db01` | `A` | `300` | `🌐 10.0.2.10` | `azurerm_private_dns_a_record.db01` |
| ➕ | `db02` | `A` | `300` | `🌐 10.0.2.11` | `azurerm_private_dns_a_record.db02` |
| ➕ | `app01` | `A` | `300` | `🌐 10.0.1.10` | `azurerm_private_dns_a_record.app01` |
| ➕ | `app02` | `A` | `300` | `🌐 10.0.1.11` | `azurerm_private_dns_a_record.app02` |
| ➕ | `redis` | `A` | `300` | `🌐 10.0.3.10` | `azurerm_private_dns_a_record.redis` |

</details>

---

## Example 7: azurerm_route_table with Inline Routes (Create)

**Scenario:** Creating a new route table with routes defined inline via the `route` attribute.

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_route_table <b><code>app_routes</code></b> — <code>🆔 rt-app-tier</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | ➕ 3 routes</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 rt-app-tier` |
| location | `🌍 eastus` |
| resource_group_name | `📁 rg-network` |
| disable_bgp_route_propagation | `❌ false` |

#### Routes

| Change | Name | Address Prefix | Next Hop Type | Next Hop Address | Terraform Resource |
| -------- | ------ | ---------------- | --------------- | ------------------ | -------------------- |
| ➕ | `🆔 default-route` | `🌐 0.0.0.0/0` | `VirtualAppliance` | `🌐 10.0.1.4` | `route` attribute |
| ➕ | `🆔 to-onprem` | `🌐 10.20.0.0/16` | `VirtualNetworkGateway` | - | `route` attribute |
| ➕ | `🆔 to-vnet` | `🌐 10.0.0.0/8` | `VnetLocal` | - | `route` attribute |

</details>

---

## Example 8: azurerm_route_table with Separate Routes (Update)

**Scenario:** Updating a route table with routes managed as separate `azurerm_route` resources.

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>⏺️ azurerm_route_table <b><code>app_routes</code></b> — <code>🆔 rt-app-tier</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | 3🔧 ➕ 1 route, 🔄 1 route, ❌ 1 route</summary>
<br>

#### Route Changes

| Change | Name | Address Prefix | Next Hop Type | Next Hop Address | Terraform Resource |
| -------- | ------ | ---------------- | --------------- | ------------------ | -------------------- |
| ➕ | `🆔 to-firewall` | `🌐 0.0.0.0/0` | `VirtualAppliance` | `🌐 10.0.1.4` | `azurerm_route.to_firewall` |
| 🔄 | `🆔 to-onprem` | `🌐 10.20.0.0/16` | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- VPN Gateway</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ VirtualNetworkGateway</span></code> | - | `azurerm_route.to_onprem` |
| ❌ | `🆔 old-route` | `🌐 10.30.0.0/16` | `VnetLocal` | - | `azurerm_route.old_route` |
| ⏺️ | `🆔 to-vnet` | `🌐 10.0.0.0/8` | `VnetLocal` | - | `azurerm_route.to_vnet` |

</details>

---

## Example 9: azurerm_route_table with Mixed Routes (Warning)

**Scenario:** Route table with routes managed both inline and as separate resources (conflict scenario).

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azurerm_route_table <b><code>mixed_routes</code></b> — <code>🆔 rt-mixed</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | Mixed route management detected</summary>
<br>

⚠️ **Warning:** This route table has routes managed both inline (via `route` attribute) and as separate `azurerm_route` resources. This configuration will cause conflicts and overwrite routes.

#### Route Changes

| Change | Name | Address Prefix | Next Hop Type | Next Hop Address | Terraform Resource |
| -------- | ------ | ---------------- | --------------- | ------------------ | -------------------- |
| ➕ | `🆔 inline-route` | `🌐 10.1.0.0/16` | `VnetLocal` | - | `route` attribute |
| ➕ | `🆔 separate-route` | `🌐 10.2.0.0/16` | `VirtualAppliance` | `🌐 10.0.1.10` | `azurerm_route.separate_route` |

</details>

---

## Example 10: azurerm_network_security_group with Inline Rules (Create)

**Scenario:** Creating a new NSG with security rules defined inline via the `security_rule` attribute.

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_network_security_group <b><code>app_nsg</code></b> — <code>🆔 nsg-app-tier</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | ➕ 4 rules</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 nsg-app-tier` |
| location | `🌍 eastus` |
| resource_group_name | `📁 rg-network` |

#### Security Rules

| Change | Name | Priority | Direction | Access | Protocol | Source | Destination | Ports | Terraform Resource |
| -------- | ------ | ---------- | ----------- | -------- | ---------- | -------- | ------------- | ------- | -------------------- |
| ➕ | `🆔 allow-https-inbound` | `100` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `🔌 443` | `security_rule` attribute |
| ➕ | `🆔 allow-http-inbound` | `110` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `🔌 80` | `security_rule` attribute |
| ➕ | `🆔 allow-sql-outbound` | `200` | `⬆️ Outbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `🌐 10.0.2.0/24` | `🔌 1433` | `security_rule` attribute |
| ➕ | `🆔 deny-all-inbound` | `4096` | `⬇️ Inbound` | `⛔ Deny` | `✳️` | `✳️` | `✳️` | `✳️` | `security_rule` attribute |

</details>

---

## Example 11: azurerm_network_security_group with Separate Rules (Update)

**Scenario:** Updating an NSG with security rules managed as separate `azurerm_network_security_rule` resources.

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>⏺️ azurerm_network_security_group <b><code>app_nsg</code></b> — <code>🆔 nsg-app-tier</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | 3🔧 ➕ 1 rule, 🔄 1 rule, ❌ 1 rule</summary>
<br>

#### Security Rule Changes

| Change | Name | Priority | Direction | Access | Protocol | Source | Destination | Ports | Terraform Resource |
| -------- | ------ | ---------- | ----------- | -------- | ---------- | -------- | ------------- | ------- | -------------------- |
| ➕ | `🆔 allow-ssh-from-bastion` | `150` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `🌐 10.0.0.0/24` | `✳️` | `🔌 22` | `azurerm_network_security_rule.allow_ssh` |
| 🔄 | `🆔 allow-https-inbound` | `100` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- ✳️</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 🌐 203.0.113.0/24</span></code> | `✳️` | `🔌 443` | `azurerm_network_security_rule.allow_https` |
| ❌ | `🆔 temp-allow-rdp` | `300` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `🌐 198.51.100.0/24` | `✳️` | `🔌 3389` | `azurerm_network_security_rule.temp_rdp` |
| ⏺️ | `🆔 allow-http-inbound` | `110` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `🔌 80` | `azurerm_network_security_rule.allow_http` |
| ⏺️ | `🆔 deny-all-inbound` | `4096` | `⬇️ Inbound` | `⛔ Deny` | `✳️` | `✳️` | `✳️` | `✳️` | `azurerm_network_security_rule.deny_all` |

</details>

---

## Example 12: azurerm_network_security_group with Mixed Rules (Warning)

**Scenario:** NSG with security rules managed both inline and as separate resources (conflict scenario).

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azurerm_network_security_group <b><code>mixed_nsg</code></b> — <code>🆔 nsg-mixed</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | Mixed rule management detected</summary>
<br>

⚠️ **Warning:** This network security group has security rules managed both inline (via `security_rule` attribute) and as separate `azurerm_network_security_rule` resources. This configuration will cause conflicts and overwrite rules.

#### Security Rule Changes

| Change | Name | Priority | Direction | Access | Protocol | Source | Destination | Ports | Terraform Resource |
| -------- | ------ | ---------- | ----------- | -------- | ---------- | -------- | ------------- | ------- | -------------------- |
| ➕ | `🆔 inline-allow-https` | `100` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `🔌 443` | `security_rule` attribute |
| ➕ | `🆔 separate-allow-ssh` | `200` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `🌐 10.0.0.0/24` | `✳️` | `🔌 22` | `azurerm_network_security_rule.allow_ssh` |

</details>

---

## Design Notes

### Change Indicators

- ➕ Add - Resource or child element is being created
- 🔄 Change - Resource or child element is being modified
- ❌ Destroy - Resource or child element is being deleted
- ⏺️ No-op - Resource or child element exists but no changes
- ♻️ Replace - Resource is being replaced (destroy + create)

### Terraform Resource Column

- Shows the Terraform resource address when child is a separate resource (e.g., `azurerm_subnet.app`)
- Shows the attribute name when child is from an inline attribute (e.g., `subnet` attribute, `route` attribute)
- Helps users understand the Terraform structure and identify potential conflicts

### Summary Line Updates

When parent resources have inlined children, the summary line should indicate:
- Change counts for children (e.g., "➕ 3 subnets", "➕ 2 records, 🔄 1 record")
- Use "+N more" pattern when there are many changes
- For no-op parents with child changes, show "N🔧" prefix with child change summary

### Mixed Management Warnings

When a parent resource has both inline and separate children (Terraform conflict scenario):
- Display prominent warning message at the top of the child section
- Render all children in the same table
- Clearly indicate source via Terraform Resource column
- This makes configuration conflicts immediately visible

### Value Formatting

- **IP Addresses**: Use 🌐 icon (e.g., `🌐 10.0.1.0/24`, `🌐 192.0.2.1`)
- **Resource Names**: Use 🆔 icon (e.g., `🆔 snet-app`, `🆔 rt-app-tier`)
- **NSG References**: Use 🛡️ icon (e.g., `🛡️ nsg-app`)
- **Port Numbers**: Use 🔌 icon (e.g., `🔌 443`, `🔌 80,443`)
- **Protocols**: Use 🔗 icon (e.g., `🔗 TCP`, `🔗 UDP`)
- **Any/Wildcard**: Use ✳️ symbol
- **Allow/Deny**: Use ✅ / ⛔ icons
- **Direction**: Use ⬇️ / ⬆️ arrows
- **Resource Groups**: Use 📁 icon
- **Locations**: Use 🌍 icon
- **Missing/Not Applicable**: Use `-` (dash)

### Complex Attributes

For attributes that don't fit well in table cells:
- **Service Endpoints**: Show count if >3 (e.g., "4 endpoints")
- **Delegation**: Show service name only
- **Long TXT Records**: Truncate to 50 characters with "..."
- **MX Records**: Format as "priority mailserver"
- **SRV Records**: Format as "priority weight:port target"

### Table Width Considerations

- Keep essential columns only for readability
- Omit Description column if space is tight
- Use multi-line inline diffs for complex value changes
- Preserve horizontal scrollability for wide tables
