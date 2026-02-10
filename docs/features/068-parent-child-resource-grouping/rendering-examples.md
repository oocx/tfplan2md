# Parent-Child Resource Rendering Examples

This document shows example markdown output for parent-child resources with inline table rendering.

**Note:** Examples are shown as raw markdown (not in code blocks) for easy copy/paste into Azure DevOps PRs.

---

## Example 1: azuread_group with Inline Members (Create)

**Scenario:** Creating a new Azure AD group with members defined inline via the `members` attribute.

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azuread_group <b><code>engineering</code></b> — <code>🆔 Engineering Team</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| display_name | `🆔 Engineering Team` |
| security_enabled | `✅ true` |
| mail_enabled | `❌ false` |
| description | `Engineering team members` |

#### Members

| Member | Terraform Resource |
| -------- | -------------------- |
| `👤 John Doe (12345678-1234-1234-1234-123456789012)` | `members` attribute |
| `👤 Jane Smith (23456789-2345-2345-2345-234567890123)` | `members` attribute |
| `👤 Bob Johnson (34567890-3456-3456-3456-345678901234)` | `members` attribute |

#### Owners

| Owner | Terraform Resource |
| ------- | -------------------- |
| `👤 Admin User (45678901-4567-4567-4567-456789012345)` | `owners` attribute |

</details>

---

## Example 2: azuread_group with Separate azuread_group_member Resources (Mixed Changes)

**Scenario:** Updating an existing group where members are managed as separate `azuread_group_member` resources.

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azuread_group <b><code>engineering</code></b> — <code>🆔 Engineering Team</code> | 3🔧 ➕ 2 members, ❌ 1 member</summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| description | `Engineering team` | `Engineering team members - updated` |

#### Member Changes

| Change | Member | Terraform Resource |
| -------- | -------- | -------------------- |
| ➕ | `👤 Alice Williams (56789012-5678-5678-5678-567890123456)` | `azuread_group_member.alice` |
| ➕ | `👤 Charlie Brown (67890123-6789-6789-6789-678901234567)` | `azuread_group_member.charlie` |
| ❌ | `👤 Bob Johnson (34567890-3456-3456-3456-345678901234)` | `azuread_group_member.bob` |
| ⏺️ | `👤 John Doe (12345678-1234-1234-1234-123456789012)` | `azuread_group_member.john` |
| ⏺️ | `👤 Jane Smith (23456789-2345-2345-2345-234567890123)` | `azuread_group_member.jane` |

</details>

---

## Example 3: azuread_group with Mixed (Inline + Separate Members)

**Scenario:** Group has some members defined inline via `members` attribute and others via separate resources (this is a CONFLICT scenario that Terraform doesn't allow, but our tool should detect and render both sources).

**Note:**  In reality, Terraform doesn't allow mixing inline and separate members - this would cause conflicts. However, if a plan somehow contains both (e.g., during migration), we should render them clearly.

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azuread_group <b><code>engineering</code></b> — <code>🆔 Engineering Team</code> | Mixed member management detected</summary>
<br>

⚠️ **Warning:** This group has members managed both inline (via `members` attribute) and as separate `azuread_group_member` resources. This configuration will cause conflicts.

#### Member Changes

| Change | Member | Terraform Resource |
| -------- | -------- | -------------------- |
| ➕ | `👤 John Doe (12345678-1234-1234-1234-123456789012)` | `members` attribute |
| ➕ | `👤 Jane Smith (23456789-2345-2345-2345-234567890123)` | `members` attribute |
| ➕ | `👤 Bob Johnson (34567890-3456-3456-3456-345678901234)` | `azuread_group_member.bob` |

</details>

---

## Example 4: azuredevops_team with Inline Administrators and Members (Create)

**Scenario:** Creating a new Azure DevOps team with administrators and members defined inline.

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azuredevops_team <b><code>platform_team</code></b> — <code>🆔 Platform Engineering Team</code> in <code>📁 MyProject</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 Platform Engineering Team` |
| project_id | `📁 MyProject` |
| description | `Team responsible for platform infrastructure` |

#### Administrators

| Administrator | Terraform Resource |
| --------------- | -------------------- |
| `👤 aadgp.Uy0...Admin1` | `administrators` attribute |
| `👤 aadgp.Uy0...Admin2` | `administrators` attribute |

#### Members

| Member | Terraform Resource |
| -------- | -------------------- |
| `👤 aadgp.Uy0...Member1` | `members` attribute |
| `👤 aadgp.Uy0...Member2` | `members` attribute |
| `👤 aadgp.Uy0...Member3` | `members` attribute |
| `👤 aadgp.Uy0...Member4` | `members` attribute |

</details>

---

## Example 5: azuredevops_team with Separate Resources (Update with Changes)

**Scenario:** Updating team administrators and members managed as separate resources.

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azuredevops_team <b><code>platform_team</code></b> — <code>🆔 Platform Engineering Team</code> in <code>📁 MyProject</code> | 4🔧 ➕ 1 admin, ❌ 1 member, +2 more</summary>
<br>

#### Administrator Changes

| Change | Administrator | Terraform Resource |
| -------- | --------------- | -------------------- |
| ➕ | `👤 aadgp.Uy0...NewAdmin` | `azuredevops_team_administrators.new_admin` |
| ⏺️ | `👤 aadgp.Uy0...Admin1` | `administrators` attribute |

#### Member Changes

| Change | Member | Terraform Resource |
| -------- | -------- | -------------------- |
| ➕ | `👤 aadgp.Uy0...NewMember1` | `azuredevops_team_members.new_member1` |
| ➕ | `👤 aadgp.Uy0...NewMember2` | `azuredevops_team_members.new_member2` |
| ❌ | `👤 aadgp.Uy0...OldMember` | `azuredevops_team_members.old_member` |
| ⏺️ | `👤 aadgp.Uy0...Member1` | `members` attribute |
| ⏺️ | `👤 aadgp.Uy0...Member2` | `members` attribute |

</details>

---

## Example 6: azuredevops_group with Separate Membership (Detailed)

**Scenario:** Azure DevOps group with membership managed via separate `azuredevops_group_membership` resources. Uses descriptors instead of friendly names.

**Current Rendering (Before Feature):**

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azuredevops_group <b><code>release_managers</code></b> — <code>🆔 Release Managers</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| display_name | `🆔 Release Managers` |
| description | `Team members who can approve releases` |
| scope | `📁 vstfs:///Organization/12345678-1234-1234-1234-123456789012` |

</details>

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azuredevops_group_membership <b><code>release_managers_membership_alice</code></b> — Add member to <code>🆔 Release Managers</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| group | `aadgp.Uy0...ReleaseManagers` |
| member | `aadgp.Uy0...AliceUser` |

</details>

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azuredevops_group_membership <b><code>release_managers_membership_bob</code></b> — Add member to <code>🆔 Release Managers</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| group | `aadgp.Uy0...ReleaseManagers` |
| member | `aadgp.Uy0...BobUser` |

</details>

**After Feature Implementation (Inline Rendering):**

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azuredevops_group <b><code>release_managers</code></b> — <code>🆔 Release Managers</code> | ➕ 2 members</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| display_name | `🆔 Release Managers` |
| description | `Team members who can approve releases` |
| scope | `📁 vstfs:///Organization/12345678-1234-1234-1234-123456789012` |

#### Members

| Change | Member | Terraform Resource |
| -------- | -------- | -------------------- |
| ➕ | `👤 aadgp.Uy0...AliceUser` | `azuredevops_group_membership.release_managers_membership_alice` |
| ➕ | `👤 aadgp.Uy0...BobUser` | `azuredevops_group_membership.release_managers_membership_bob` |

</details>

---

## Example 7: azurerm_network_security_group with Mixed Rules

**Scenario:** NSG with some rules inline and some as separate resources (comparison between current and proposed rendering).

**Current Rendering (Separate Sections):**

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_network_security_group <b><code>app_nsg</code></b> — <code>🆔 nsg-app-tier</code> in <code>📁 rg-demo</code> <code>🌍 eastus</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 nsg-app-tier` |
| location | `🌍 eastus` |
| resource_group_name | `📁 rg-demo` |

</details>

<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_network_security_rule <b><code>allow_https</code></b> — <code>🆔 allow-https-inbound</code> in <code>🆔 nsg-app-tier</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 allow-https-inbound` |
| priority | `100` |
| direction | `⬇️ Inbound` |
| access | `✅ Allow` |
| protocol | `🔗 TCP` |
| source_port_range | `✳️` |
| destination_port_range | `🔌 443` |
| source_address_prefix | `✳️` |
| destination_address_prefix | `✳️` |

</details>

**Proposed Rendering (Inline Table):**

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_network_security_group <b><code>app_nsg</code></b> — <code>🆔 nsg-app-tier</code> in <code>📁 rg-demo</code> <code>🌍 eastus</code> | ➕ 3 rules</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 nsg-app-tier` |
| location | `🌍 eastus` |
| resource_group_name | `📁 rg-demo` |

#### Security Rules

| Change | Name | Priority | Direction | Access | Protocol | Source | Dest | Ports | Terraform Resource |
| -------- | ------ | ---------- | ----------- | -------- | ---------- | -------- | ------ | ------- | -------------------- |
| ➕ | `🆔 allow-https-inbound` | `100` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `🔌 443` | `azurerm_network_security_rule.allow_https` |
| ➕ | `🆔 allow-http-inbound` | `110` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `🔌 80` | `security_rule` attribute |
| ➕ | `🆔 deny-all-inbound` | `4096` | `⬇️ Inbound` | `⛔ Deny` | `✳️` | `✳️` | `✳️` | `✳️` | `security_rule` attribute |

</details>

---

## Example 8: azurerm_route_table with Route Changes

**Scenario:** Route table with mixed inline and separate routes being updated.

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azurerm_route_table <b><code>app_routes</code></b> — <code>🆔 rt-app-tier</code> in <code>📁 rg-demo</code> <code>🌍 eastus</code> | 3🔧 ➕ 1 route, 🔄 1 route, ❌ 1 route</summary>
<br>

#### Route Changes

| Change | Name | Address Prefix | Next Hop Type | Next Hop Address | Terraform Resource |
| -------- | ------ | ---------------- | --------------- | ------------------ | -------------------- |
| ➕ | `🆔 to-firewall` | `🌐 0.0.0.0/0` | `VirtualAppliance` | `🌐 10.0.1.4` | `azurerm_route.to_firewall` |
| 🔄 | `🆔 to-onprem` | `🌐 10.20.0.0/16` | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- VPN Gateway</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ VirtualNetworkGateway</span></code> | - | `route` attribute |
| ❌ | `🆔 old-route` | `🌐 10.30.0.0/16` | `VnetLocal` | - | `azurerm_route.old_route` |
| ⏺️ | `🆔 to-vnet` | `🌐 10.0.0.0/8` | `VnetLocal` | - | `route` attribute |

</details>

---

## Example 9: azurerm_virtual_network with Subnet Changes (Complex)

**Scenario:** Virtual network with subnets showing both simple and complex changes.

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azurerm_virtual_network <b><code>hub_vnet</code></b> — <code>🆔 vnet-hub</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> <code>🌐 10.0.0.0/16</code> | 4🔧 ➕ 1 subnet, 🔄 1 subnet, +2 more</summary>
<br>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| address_space[1] | - | `🌐 10.1.0.0/16` |

#### Subnet Changes

| Change | Name | Address Prefixes | NSG | Terraform Resource |
| -------- | ------ | ------------------ | ----- | -------------------- |
| ➕ | `🆔 snet-firewall` | `🌐 10.0.3.0/24` | - | `azurerm_subnet.firewall` |
| 🔄 | `🆔 snet-app` | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- 🌐 10.0.1.0/24</span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 🌐 10.0.1.0/23</span></code> | `🛡️ nsg-app` | `subnet` attribute |
| ⏺️ | `🆔 snet-data` | `🌐 10.0.2.0/24` | `🛡️ nsg-data` | `subnet` attribute |

**🏷️ DNS Servers:** `🌐 10.0.0.4`, `🌐 10.0.0.5`

</details>

---

## Example 10: azurerm_virtual_network with Complex Subnets (Too Many Attributes for Table)

**Scenario:** Virtual network with subnets that have many attributes set (delegation, service endpoints, policies, etc.) - too many to fit in a readable table row.

**Rendering Strategy:** For subnets with many attributes, provide BOTH:
1. A horizontal table showing all key attributes at-a-glance (with complex attributes formatted inline)
2. Expandable details sections below for full context and easy navigation

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azurerm_virtual_network <b><code>enterprise_vnet</code></b> — <code>🆔 vnet-enterprise</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> <code>🌐 10.0.0.0/16</code> | ➕ 2 subnets</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 vnet-enterprise` |
| location | `🌍 eastus` |
| resource_group_name | `📁 rg-network` |
| address_space | `🌐 10.0.0.0/16` |

#### Subnets

| Change | Name | Address | Delegation | Service Endpoints | Endpoint Policies | Private Endpoint Policies | Terraform Resource |
| -------- | ------ | --------- | ------------ | ------------------- | ------------------- | --------------------------- | -------------------- |
| ➕ | `🆔 snet-aks` | `🌐 10.0.1.0/24` | `aks-delegation` delegates `Microsoft.Network/virtualNetworks/subnets/join/action` to `Microsoft.ContainerService/managedClusters` | `Microsoft.ContainerRegistry`, `Microsoft.Storage`, `Microsoft.Sql`, `Microsoft.KeyVault` | - | `Disabled` | `subnet` attribute |
| ➕ | `🆔 snet-webapp` | `🌐 10.0.2.0/24` | `webapp-delegation` delegates `Microsoft.Network/virtualNetworks/subnets/action` to `Microsoft.Web/serverFarms` | `Microsoft.Storage`, `Microsoft.KeyVault`, `Microsoft.Web` | Service Endpoint Policy `🔗 policy-storage` in resource group `📁 rg-network` of subscription `🔑 sub-id` | `Enabled` | `azurerm_subnet.webapp` |

**Subnet Details:**

<details style="margin-left: 20px; margin-bottom: 8px; border-left: 3px solid #28a745; padding-left: 12px;">
<summary>➕ <b><code>🆔 snet-aks</code></b> (<code>🌐 10.0.1.0/24</code>) — AKS cluster subnet with delegation</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 snet-aks` |
| address_prefixes | `🌐 10.0.1.0/24` |
| default_outbound_access_enabled | `✅ true` |
| private_endpoint_network_policies | `Disabled` |
| private_link_service_network_policies_enabled | `✅ true` |
| delegation | `aks-delegation` delegates `Microsoft.Network/virtualNetworks/subnets/join/action` to `Microsoft.ContainerService/managedClusters` |
| service_endpoints | `Microsoft.ContainerRegistry`, `Microsoft.Storage`, `Microsoft.Sql`, `Microsoft.KeyVault` |

</details>

<details style="margin-left: 20px; margin-bottom: 8px; border-left: 3px solid #28a745; padding-left: 12px;">
<summary>➕ <b><code>🆔 snet-webapp</code></b> (<code>🌐 10.0.2.0/24</code>) — Web app subnet with service delegation</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 snet-webapp` |
| address_prefixes | `🌐 10.0.2.0/24` |
| default_outbound_access_enabled | `❌ false` |
| private_endpoint_network_policies | `Enabled` |
| private_link_service_network_policies_enabled | `✅ true` |
| sharing_scope | `Tenant` |
| delegation | `webapp-delegation` delegates `Microsoft.Network/virtualNetworks/subnets/action` to `Microsoft.Web/serverFarms` |
| service_endpoints | `Microsoft.Storage`, `Microsoft.KeyVault`, `Microsoft.Web` |
| service_endpoint_policy_ids | Service Endpoint Policy `🔗 policy-storage` in resource group `📁 rg-network` of subscription `🔑 sub-id` |

</details>

</details>

**Design Rationale:**
- **At-a-glance table:** Shows all key attributes in one view; complex attributes formatted inline for readability
- **Expandable details:** Provides full context in structured format for users who need to dive deeper
- **Delegation formatting:** Concise "`name` delegates `actions` to `service`" format fits in table cell
- **Service endpoints:** Simple comma-separated list suitable for table display
- **Service endpoint policies:** Uses readable Azure resource ID format (Feature 019) for consistency
- **Both table + details:** Users get the benefits of both scanning (table) and deep-diving (expandable sections)

---

## Design Notes

### Change Indicators

- ➕ Add - Resource or child element is being created
- 🔄 Change - Resource or child element is being modified
- ❌ Destroy - Resource or child element is being deleted
- ⏺️ No-op - Resource or child element exists but no changes
- ♻️ Replace - Resource is being replaced (destroy + create)

### Terraform Resource Column

- Shows the Terraform resource address when child is a separate resource (e.g., `azuread_group_member.alice`)
- Shows the attribute name when child is from an inline attribute (e.g., `members` attribute, `security_rule` attribute)
- Helps users understand the Terraform structure and identify potential conflicts when both patterns are used

### Summary Line Updates

When parent resources have inlined children, the summary line should indicate:
- Change counts for children (e.g., "➕ 2 members, ❌ 1 member")
- Use "+N more" pattern when there are many changes

### Table Column Design

- Keep essential columns only
- Truncate or summarize long values
- Use emojis and formatting for scanability
- Break complex changes into inline diff blocks when needed
- **For complex attributes:** Format inline using established patterns (see Layout Selection below) rather than hiding them
- **Complex + simple together:** When children have both simple and complex attributes, provide BOTH a horizontal table (with inline formatting) AND expandable details sections for complete information at different levels of detail

### Layout Selection for Child Resources

**Use horizontal table layout only when:**
- Children have ≤5 simple attributes (e.g., members with just name/ID, simple firewall rules)
- All values are short and don't need wrapping
- The table fits comfortably in typical viewport widths

**Use horizontal table + expandable details when:**
- Children have 6+ attributes including complex nested structures
- Need both at-a-glance scanning (table) AND full detail access (expandable sections)
- Complex attributes can be formatted inline (delegations, comma-separated endpoints, formatted resource IDs)
- Table provides overview; expandable details provide deep-dive capability

**Format complex attributes in tables as:**
- **Service endpoints:** Comma-separated list (e.g., `Microsoft.Storage`, `Microsoft.KeyVault`)
- **Delegation:** "`name` delegates `comma-separated actions` to `service name`"
- **Resource ID references (policies, etc.):** Use readable Azure resource ID format (Feature 019) - e.g., `Service Endpoint Policy` `🔗 policy-storage` in resource group `📁 rg-network` of subscription `🔑 sub-id`

Both table and expandable details can coexist for the same child resources, providing different views for different user needs.

### Mixed Scenarios

When a resource has both inline and separate children (which Terraform doesn't officially support but might appear during transitions):
- Show a warning message at the top of the section
- Render all children in the same table
- Clearly indicate the source (attribute name vs separate resource address in the "Terraform Resource" column)
- This makes conflicts immediately visible to users
