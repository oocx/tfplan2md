# Feature Extension: Azure RM Parent-Child Resource Grouping (Batch 2)

## Overview

This specification extends Feature 068 (Parent-Child Resource Grouping and Inline Rendering) by adding support for 4 additional Azure RM resource types that follow the parent-child pattern. The generic framework implemented in Feature 068 is already in place and working - this extension adds new relationship registrations and row extractors for:

- `azurerm_virtual_network` with `azurerm_subnet` children
- `azurerm_dns_zone` with DNS record children
- `azurerm_route_table` with `azurerm_route` children
- `azurerm_network_security_group` with `azurerm_network_security_rule` children

All four of these resource types are already documented in the parent-child resource catalog (`parent-child-resource-catalog.md` lines 69-157) as "⏳ Planned" and were explicitly listed in the original Feature 068 specification as "Not in Initial Implementation" items to be "added incrementally in future releases."

## User Goals

- **Network Infrastructure Clarity**: View complete network configurations (VNets with subnets, NSGs with rules, route tables with routes) in single cohesive sections
- **DNS Management Efficiency**: See all DNS records for a zone together rather than in dozens of separate sections
- **Consistent Experience**: Benefit from the same inline rendering pattern already working for Azure AD and Azure DevOps resources
- **Reduced Scrolling**: Review network changes without excessive context switching between parent and child sections

## Scope

### In Scope

#### Resource Types

1. **azurerm_virtual_network / azurerm_subnet**
   - Inline via `subnet` attribute (list of subnet blocks)
   - Separate via `azurerm_subnet` resources
   - Handle `azurerm_virtual_network_dns_servers` if needed
   - Warning for mixed inline/separate subnets (Terraform conflict scenario)

2. **azurerm_dns_zone / DNS Record Types**
   - **No inline attributes** - all records are separate resources
   - Group records under parent zone for readability
   - Support both public and private DNS zones
   - Supported record types:
     - `azurerm_dns_a_record` / `azurerm_private_dns_a_record`
     - `azurerm_dns_aaaa_record` / `azurerm_private_dns_aaaa_record`
     - `azurerm_dns_cname_record` / `azurerm_private_dns_cname_record`
     - `azurerm_dns_mx_record` / `azurerm_private_dns_mx_record`
     - `azurerm_dns_ns_record`
     - `azurerm_dns_ptr_record` / `azurerm_private_dns_ptr_record`
     - `azurerm_dns_srv_record` / `azurerm_private_dns_srv_record`
     - `azurerm_dns_txt_record` / `azurerm_private_dns_txt_record`
     - `azurerm_dns_caa_record`

3. **azurerm_route_table / azurerm_route**
   - Inline via `route` attribute (list of route blocks)
   - Separate via `azurerm_route` resources
   - Warning for mixed inline/separate routes (Terraform conflict scenario)

4. **azurerm_network_security_group / azurerm_network_security_rule**
   - Inline via `security_rule` attribute (list of rule blocks)
   - Separate via `azurerm_network_security_rule` resources
   - Warning for mixed inline/separate rules (Terraform conflict scenario)

#### Rendering Strategy

Each resource type will have:

- **Table Columns**: Optimized for the specific resource attributes
- **Change Indicators**: ➕, 🔄, ❌, ⏺️ for each child row
- **Terraform Resource Column**: Shows separate resource address or inline attribute name
- **Value Formatting**: Use existing formatters (emojis, highlighting, readable IDs)
- **Summary Line**: Parent resource summary includes child change counts
- **Mixed Management Warning**: Display warning when both inline and separate children detected

#### Implementation Approach

For each of the 4 resource types:

1. Register `ParentChildRelationship` in `AzureRmModule.RegisterParentChildRelationships()`
2. Create resource-specific `IChildRowExtractor` implementation
3. Define table columns with appropriate headers
4. Test with realistic plan JSON fixtures
5. Generate snapshot baselines
6. Create rendering examples in `azure-rm-rendering-examples.md`

### Out of Scope

#### Not in This Extension

- Other Azure RM parent-child patterns from the catalog (e.g., `azurerm_firewall` → `azurerm_firewall_*_rule_collection`)
- Azure AD application child resources
- Azure DevOps variable group variables
- Custom template overrides for these resources
- Column visibility configuration options
- Performance optimizations beyond the existing framework

#### Implementation Constraints

- No new command-line options
- No opt-out mechanism for these resource types
- No changes to the core parent-child framework architecture
- Must use existing `ChildResourceGroup` model and `_child_resources.sbn` template

## User Experience

### Viewing Virtual Network with Subnets

**Scenario:** User reviews a plan that creates a VNet with 3 subnets (2 inline, 1 separate).

**Current Experience:**
- 4 separate collapsible sections (1 for VNet, 3 for subnets)
- Must cross-reference subnet names with parent VNet
- Difficult to see complete network topology at a glance

**New Experience:**
- Single section for the VNet
- All subnets shown in a table with columns: Change, Name, Address Prefixes, NSG, Delegation, Terraform Resource
- Clear indication of which subnets are inline vs separate
- Summary line: "➕ azurerm_virtual_network `vnet-hub` — `🆔 vnet-hub` in `📁 rg-network` `🌍 eastus` `🌐 10.0.0.0/16` | ➕ 3 subnets"
- Warning displayed if mixing inline and separate subnets

### Reviewing DNS Zone with Records

**Scenario:** User reviews a plan that adds 15 new DNS records to an existing zone.

**Current Experience:**
- 16 separate collapsible sections (1 for zone, 15 for records)
- Extensive scrolling to review all records
- Hard to see which records belong to which zone

**New Experience:**
- Single section for the DNS zone
- All records shown in a table with columns: Change, Name, Type, TTL, Value/Target, Terraform Resource
- Summary line: "🔄 azurerm_dns_zone `example-com` — `🆔 example.com` in `📁 rg-dns` | ➕ 15 records"
- Can scan all DNS changes in one location

### Updating Route Table Routes

**Scenario:** User reviews a plan that adds 2 routes and modifies 1 route in a route table.

**Current Experience:**
- 4 separate sections (1 for route table, 3 for routes)
- Must mentally track which routes belong to which table
- Difficult to understand overall routing changes

**New Experience:**
- Single section for the route table
- All routes shown in table with columns: Change, Name, Address Prefix, Next Hop Type, Next Hop Address, Terraform Resource
- Inline diffs for modified routes
- Summary line: "🔄 azurerm_route_table `rt-app` — `🆔 rt-app-tier` in `📁 rg-network` | ➕ 2 routes, 🔄 1 route"

### Managing Network Security Rules

**Scenario:** User reviews a plan that creates an NSG with 5 security rules (3 inline, 2 separate).

**Current Experience:**
- 6 separate sections
- Must identify which rules belong to which NSG
- Hard to review security posture at a glance

**New Experience:**
- Single section for the NSG
- All rules shown in table with columns: Change, Name, Priority, Direction, Access, Protocol, Source, Destination, Ports, Description, Terraform Resource
- Summary line: "➕ azurerm_network_security_group `nsg-app` — `🆔 nsg-app-tier` in `📁 rg-demo` | ➕ 5 rules"
- Warning if mixing inline and separate rules

## Success Criteria

### Functional Requirements

- [ ] **Relationship Registration**: All 4 parent-child relationships registered in `AzureRmModule`
- [ ] **Row Extractors**: Custom `IChildRowExtractor` implementations for each resource type
- [ ] **Table Rendering**: Child resources render as tables within parent sections
- [ ] **Change Indicators**: Each child row shows correct change indicator (➕, 🔄, ❌, ⏺️)
- [ ] **Resource Address**: Separate child resources show their Terraform address
- [ ] **Inline Source**: Inline children show the attribute name (e.g., `subnet` attribute)
- [ ] **Mixed Warning**: Warning displayed when both inline and separate children detected
- [ ] **Summary Counts**: Parent summary line includes child change counts
- [ ] **Value Formatting**: All values use existing formatters (emojis, IDs, highlighting)
- [ ] **Configuration Reference Fallback**: Child matching works correctly for `(known after apply)` parent IDs

### Table Column Specifications

#### azurerm_virtual_network → azurerm_subnet

| Column | Description | Example |
|--------|-------------|---------|
| Change | Change indicator | ➕ / 🔄 / ❌ / ⏺️ |
| Name | Subnet name with 🆔 icon | `🆔 snet-app` |
| Address Prefixes | Subnet CIDR with 🌐 icon | `🌐 10.0.1.0/24` |
| NSG | Network security group reference | `🛡️ nsg-app` |
| Delegation | Service delegation (if present) | `Microsoft.Web/serverFarms` |
| Terraform Resource | Resource address or attribute | `azurerm_subnet.app` or `subnet` attribute |

**Complex Attributes Handling:**
- Service endpoints: Show as comma-separated list if ≤3, otherwise show count
- Delegation: Show service name only (e.g., "Microsoft.Web/serverFarms")
- Private endpoint policies: Show "Enabled" / "Disabled"

#### azurerm_dns_zone → DNS Records

| Column | Description | Example |
|--------|-------------|---------|
| Change | Change indicator | ➕ / 🔄 / ❌ / ⏺️ |
| Name | Record name | `www` / `@` / `mail` |
| Type | Record type | `A` / `CNAME` / `MX` / `TXT` |
| TTL | Time to live | `3600` |
| Value/Target | Record value(s) | `🌐 192.0.2.1` / `example.com` |
| Terraform Resource | Resource address | `azurerm_dns_a_record.www` |

**Record Type Specifics:**
- A/AAAA: Show IP addresses with 🌐 icon
- CNAME: Show target hostname
- MX: Show priority + mail server
- TXT: Show truncated text (max 50 chars) with "..." if longer
- SRV: Show priority, weight, port, target

#### azurerm_route_table → azurerm_route

| Column | Description | Example |
|--------|-------------|---------|
| Change | Change indicator | ➕ / 🔄 / ❌ / ⏺️ |
| Name | Route name with 🆔 icon | `🆔 to-firewall` |
| Address Prefix | Destination CIDR with 🌐 icon | `🌐 0.0.0.0/0` |
| Next Hop Type | Hop type | `VirtualAppliance` / `VnetLocal` / `Internet` |
| Next Hop Address | Hop IP (if applicable) | `🌐 10.0.1.4` or `-` |
| Terraform Resource | Resource address or attribute | `azurerm_route.to_firewall` or `route` attribute |

#### azurerm_network_security_group → azurerm_network_security_rule

| Column | Description | Example |
|--------|-------------|---------|
| Change | Change indicator | ➕ / 🔄 / ❌ / ⏺️ |
| Name | Rule name with 🆔 icon | `🆔 allow-https-inbound` |
| Priority | Rule priority | `100` |
| Direction | Inbound/Outbound with icon | `⬇️ Inbound` / `⬆️ Outbound` |
| Access | Allow/Deny with icon | `✅ Allow` / `⛔ Deny` |
| Protocol | Protocol with icon | `🔗 TCP` / `🔗 UDP` / `✳️` (Any) |
| Source | Source address/prefix | `✳️` / `🌐 10.0.0.0/8` |
| Destination | Destination address/prefix | `✳️` / `🌐 192.168.1.0/24` |
| Ports | Port ranges | `🔌 443` / `🔌 80,443` / `✳️` |
| Terraform Resource | Resource address or attribute | `azurerm_network_security_rule.allow_https` or `security_rule` attribute |

**Note:** If Description attribute is present and table width allows, add Description column after Ports.

### Quality Requirements

- [ ] **Test Coverage**: New test fixtures for all 4 resource types with inline, separate, and mixed scenarios
- [ ] **Snapshot Tests**: Updated snapshots demonstrate inline rendering for all resource types
- [ ] **Row Extractor Tests**: Unit tests for each custom row extractor
- [ ] **Configuration Reference Tests**: Tests verify fallback matching for `(known after apply)` scenarios
- [ ] **Rendering Examples**: `azure-rm-rendering-examples.md` shows expected output for each resource type
- [ ] **UAT Coverage**: UAT test plan includes scenarios for all 4 resource types
- [ ] **No Regressions**: Existing parent-child rendering (Azure AD, Azure DevOps) remains unchanged
- [ ] **Docker Build**: Code builds successfully in Docker environment

### Documentation Requirements

- [ ] **Catalog Update**: Mark the 4 resource types as "✅ Implemented" in `parent-child-resource-catalog.md`
- [ ] **Rendering Examples**: Create `azure-rm-rendering-examples.md` with examples for all 4 resource types
- [ ] **Feature Documentation**: Update `docs/features.md` to list the new supported resource types
- [ ] **Work Protocol**: Update `work-protocol.md` with agent work log entries
- [ ] **Architecture**: No changes needed (framework already supports these patterns)

## Related Work

### Existing Features

- **Feature 068 Initial Implementation**: Already implemented the generic framework, Azure AD groups, Azure DevOps groups/teams
- **Feature 026**: azurerm_firewall_network_rule_collection (uses custom implementation, not the generic framework)
- **Feature 060**: azurerm_firewall_application_rule_collection (uses custom implementation, not the generic framework)

### Future Features

After this extension is implemented, future features can:
- Add remaining Azure RM patterns from the catalog (firewall → rule collections)
- Add Azure AD application child resources
- Add Azure DevOps variable group variables
- Implement performance optimizations if needed at scale

## Open Questions

*None.* All required information is documented in the parent-child resource catalog.

---

## Implementation Notes

This is an **extension** of Feature 068, not a new feature. Therefore:

1. **Work in the same feature branch**: Use `copilot/implement-parent-child-grouping` (already exists)
2. **Update existing documentation**: Extend `specification.md` "In Scope" section, update catalog status
3. **Preserve existing implementation**: All Azure AD and Azure DevOps functionality remains unchanged
4. **Follow established patterns**: Use the same architecture, testing approach, and rendering style

The generic framework is already proven and working - this extension demonstrates its extensibility by adding 4 new resource types with minimal code (<50 lines per relationship).
