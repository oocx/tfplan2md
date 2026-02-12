# Test Plan: Azure RM Parent-Child Resource Grouping (Batch 2)

## Overview

This test plan covers the extension of Feature 068 (Parent-Child Resource Grouping) to add support for 4 additional Azure RM resource types:
- `azurerm_virtual_network` / `azurerm_subnet`
- `azurerm_dns_zone` / DNS record types
- `azurerm_route_table` / `azurerm_route`
- `azurerm_network_security_group` / `azurerm_network_security_rule`

The generic framework is already implemented and working (tested in initial implementation with Azure AD and Azure DevOps resources). This extension adds new relationship registrations and resource-specific row extractors.

References:
- [azure-rm-batch-specification.md](azure-rm-batch-specification.md)
- [azure-rm-rendering-examples.md](azure-rm-rendering-examples.md)
- [architecture.md](architecture.md) (Azure RM Batch 2 section)
- [test-plan.md](test-plan.md) (original test plan for framework)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| **VNet/Subnet Relationship**: Register and render azurerm_virtual_network → azurerm_subnet | TC-AZ-01, TC-AZ-02, TC-AZ-03 | Unit (Registry), Integration (Snapshot) |
| **VNet Inline Subnets**: Extract subnets from `subnet` attribute and render as table | TC-AZ-02 | Integration (Snapshot) |
| **VNet Separate Subnets**: Match separate azurerm_subnet resources to parent VNet | TC-AZ-03 | Integration (Snapshot) |
| **VNet Mixed Management**: Warning when both inline and separate subnets detected | TC-AZ-04 | Integration (Snapshot) |
| **VNet Subnet Columns**: Name, Address Prefixes, NSG, Delegation, Terraform Resource | TC-AZ-05 | Unit (Extractor) |
| **VNet Complex Attributes**: Service endpoints, delegations, private endpoint policies | TC-AZ-06 | Unit (Extractor) |
| **DNS Zone/Records Relationship**: Register and render azurerm_dns_zone → DNS record types | TC-AZ-07, TC-AZ-08, TC-AZ-09 | Unit (Registry), Integration (Snapshot) |
| **DNS Public/Private Zones**: Handle both azurerm_dns_zone and azurerm_private_dns_zone | TC-AZ-08, TC-AZ-09 | Integration (Snapshot) |
| **DNS Record Types**: Support 9+ record types (A, AAAA, CNAME, MX, NS, PTR, SRV, TXT, CAA) | TC-AZ-07, TC-AZ-10 | Unit (Registry), Unit (Extractor) |
| **DNS Record Columns**: Name, Type, TTL, Value/Target, Terraform Resource | TC-AZ-10 | Unit (Extractor) |
| **DNS Record Values**: Format IP addresses, hostnames, MX/SRV records, truncate TXT | TC-AZ-10 | Unit (Extractor) |
| **Route Table/Route Relationship**: Register and render azurerm_route_table → azurerm_route | TC-AZ-11, TC-AZ-12, TC-AZ-13 | Unit (Registry), Integration (Snapshot) |
| **Route Table Inline Routes**: Extract routes from `route` attribute and render as table | TC-AZ-12 | Integration (Snapshot) |
| **Route Table Separate Routes**: Match separate azurerm_route resources to parent | TC-AZ-13 | Integration (Snapshot) |
| **Route Table Mixed Management**: Warning when both inline and separate routes detected | TC-AZ-14 | Integration (Snapshot) |
| **Route Table Columns**: Name, Address Prefix, Next Hop Type, Next Hop Address, Terraform Resource | TC-AZ-15 | Unit (Extractor) |
| **NSG/Rule Relationship**: Register and render azurerm_network_security_group → azurerm_network_security_rule | TC-AZ-16, TC-AZ-17, TC-AZ-18 | Unit (Registry), Integration (Snapshot) |
| **NSG Inline Rules**: Extract rules from `security_rule` attribute and render as table | TC-AZ-17 | Integration (Snapshot) |
| **NSG Separate Rules**: Match separate azurerm_network_security_rule resources to parent | TC-AZ-18 | Integration (Snapshot) |
| **NSG Mixed Management**: Warning when both inline and separate rules detected | TC-AZ-19 | Integration (Snapshot) |
| **NSG Rule Columns**: Name, Priority, Direction, Access, Protocol, Source, Destination, Ports, Terraform Resource | TC-AZ-20 | Unit (Extractor) |
| **NSG Rule Formatting**: Icons for direction (⬇️/⬆️), access (✅/⛔), protocol (🔗), wildcards (✳️) | TC-AZ-20 | Unit (Extractor) |
| **NSG Port Ranges**: Format port ranges and multiple ports with 🔌 icon | TC-AZ-21 | Unit (Extractor) |
| **Configuration Reference Matching**: All 4 resource types use configuration fallback for (known after apply) | TC-AZ-22, TC-AZ-23, TC-AZ-24, TC-AZ-25 | Integration (Snapshot) |
| **Change Indicators**: All child rows show correct ➕, 🔄, ❌, ⏺️ indicators | TC-AZ-26 | Unit (Extractor) |
| **Summary Counts**: Parent summaries include child change counts for all 4 resource types | TC-AZ-27 | Unit (Model Builder) |
| **Terraform Resource Column**: Show resource address for separate, attribute name for inline | TC-AZ-28 | Unit (Extractor) |

## Test Cases

### TC-AZ-01: Azure RM Relationship Registry Validation

**Type:** Unit

**Description:**
Verify that `AzureRmModule.RegisterParentChildRelationships()` correctly registers all 4 new parent-child relationships.

**Preconditions:**
- Provider modules registered.

**Test Steps:**
1. Query registry for `azurerm_virtual_network`.
2. Query registry for `azurerm_dns_zone`.
3. Query registry for `azurerm_private_dns_zone`.
4. Query registry for `azurerm_route_table`.
5. Query registry for `azurerm_network_security_group`.

**Expected Result:**
- `azurerm_virtual_network` has 1 relationship (subnets).
- `azurerm_dns_zone` has 9+ relationships (one per DNS record type).
- `azurerm_private_dns_zone` has 9+ relationships (one per DNS record type).
- `azurerm_route_table` has 1 relationship (routes).
- `azurerm_network_security_group` has 1 relationship (security rules).
- All child resource types are registered in the child type set.

**Test Class:** `AzureRmParentChildRegistryTests.cs`

---

### TC-AZ-02: VNet with Inline Subnets (Create Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azurerm_virtual_network` with subnets defined via the `subnet` attribute (inline).

**Test Data:**
Plan with:
- 1 `azurerm_virtual_network` (CREATE action)
- `subnet` attribute containing 3 subnet blocks
- Subnets with varying NSG assignments and delegations

**Expected Result:**
- Single section for the VNet
- "Subnets" table with 3 rows
- Terraform Resource column shows "`subnet` attribute" for all rows
- NSG references formatted with 🛡️ icon
- Delegation shown for appropriate subnets
- Summary line: "➕ azurerm_virtual_network ... | ➕ 3 subnets"

**Snapshot File:** `azurerm-vnet-inline-subnets.md`

**Test Class:** `AzureRmVirtualNetworkSnapshotTests.cs`

---

### TC-AZ-03: VNet with Separate Subnets (Update Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azurerm_virtual_network` with subnets managed as separate `azurerm_subnet` resources.

**Test Data:**
Plan with:
- 1 `azurerm_virtual_network` (UPDATE action)
- 5 separate `azurerm_subnet` resources (1 add, 1 change, 1 delete, 2 no-op)
- Subnets reference VNet via `virtual_network_name` attribute

**Expected Result:**
- Single section for the VNet
- "Subnet Changes" table with 5 rows showing change indicators
- Terraform Resource column shows resource addresses (e.g., `azurerm_subnet.app`)
- No separate sections for `azurerm_subnet` resources
- Inline diffs for modified subnet attributes
- Summary line: "🔄 azurerm_virtual_network ... | ➕ 1 subnet, 🔄 1 subnet, ❌ 1 subnet"

**Snapshot File:** `azurerm-vnet-separate-subnets.md`

**Test Class:** `AzureRmVirtualNetworkSnapshotTests.cs`

---

### TC-AZ-04: VNet with Mixed Subnet Management (Warning Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify that a VNet with both inline and separate subnets displays the mixed management warning.

**Test Data:**
Plan with:
- 1 `azurerm_virtual_network` with 2 subnets in `subnet` attribute
- 2 separate `azurerm_subnet` resources referencing the same VNet

**Expected Result:**
- Warning message: "⚠️ **Warning:** This virtual network has subnets managed both inline (via `subnet` attribute) and as separate `azurerm_subnet` resources. This configuration will cause conflicts and overwrite subnets."
- All 4 subnets in a single table
- Terraform Resource column distinguishes inline vs separate
- `HasMixedManagement` flag is true

**Snapshot File:** `azurerm-vnet-mixed-subnets.md`

**Test Class:** `AzureRmVirtualNetworkSnapshotTests.cs`

---

### TC-AZ-05: VNet Subnet Row Extractor - Column Mapping

**Type:** Unit (Extractor)

**Description:**
Verify that `AzureRmSubnetRowExtractor.ExtractRow()` correctly extracts all required columns from subnet JSON.

**Test Steps:**
1. Create subnet JSON with all attributes populated.
2. Call `ExtractRow()`.
3. Verify returned dictionary contains keys: `name`, `address_prefixes`, `nsg`, `delegation`, `terraform_resource`.

**Expected Result:**
- `name`: Formatted with 🆔 icon (e.g., `🆔 snet-app`)
- `address_prefixes`: Formatted with 🌐 icon (e.g., `🌐 10.0.1.0/24`)
- `nsg`: Formatted with 🛡️ icon and readable name (e.g., `🛡️ nsg-app`)
- `delegation`: Service name (e.g., `Microsoft.Web/serverFarms`)
- Missing values return `-`

**Test Class:** `AzureRmSubnetRowExtractorTests.cs`

---

### TC-AZ-06: VNet Subnet Row Extractor - Complex Attributes

**Type:** Unit (Extractor)

**Description:**
Verify that `AzureRmSubnetRowExtractor` handles complex subnet attributes: service endpoints (list), delegations (nested object), private endpoint policies.

**Test Data:**
- Subnet with 5 service endpoints
- Subnet with service delegation (nested `service_delegation` block)
- Subnet with private endpoint network policies enabled/disabled

**Expected Result:**
- **Service endpoints (≤3)**: Comma-separated list (e.g., `Microsoft.Storage, Microsoft.KeyVault`)
- **Service endpoints (>3)**: First endpoint + count (e.g., `Microsoft.Storage, +4 more`)
- **Delegation**: Service name extracted from `service_delegation[0].name`
- **Private endpoint policies**: Show "Enabled" / "Disabled"

**Test Class:** `AzureRmSubnetRowExtractorTests.cs`

---

### TC-AZ-07: DNS Zone Relationship Registry - All Record Types

**Type:** Unit (Registry)

**Description:**
Verify that all DNS record types are registered for both public and private DNS zones.

**Test Steps:**
1. Query registry for `azurerm_dns_zone`.
2. Query registry for `azurerm_private_dns_zone`.
3. Verify child resource types.

**Expected Result:**
- `azurerm_dns_zone` has relationships for:
  - `azurerm_dns_a_record`
  - `azurerm_dns_aaaa_record`
  - `azurerm_dns_cname_record`
  - `azurerm_dns_mx_record`
  - `azurerm_dns_ns_record`
  - `azurerm_dns_ptr_record`
  - `azurerm_dns_srv_record`
  - `azurerm_dns_txt_record`
  - `azurerm_dns_caa_record`
- `azurerm_private_dns_zone` has relationships for:
  - `azurerm_private_dns_a_record`
  - `azurerm_private_dns_aaaa_record`
  - `azurerm_private_dns_cname_record`
  - `azurerm_private_dns_mx_record`
  - `azurerm_private_dns_ptr_record`
  - `azurerm_private_dns_srv_record`
  - `azurerm_private_dns_txt_record`

**Test Class:** `AzureRmDnsZoneRegistryTests.cs`

---

### TC-AZ-08: Public DNS Zone with Multiple Record Types (Create Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azurerm_dns_zone` with various DNS record types.

**Test Data:**
Plan with:
- 1 `azurerm_dns_zone` (CREATE action)
- 8 separate DNS record resources: A, AAAA, CNAME, MX, TXT, CAA records
- No inline attributes (DNS zones don't have inline record attributes)

**Expected Result:**
- Single section for the DNS zone
- "DNS Records" table with 8 rows
- Type column shows record type (A, AAAA, CNAME, etc.)
- Value/Target column shows formatted values (IPs with 🌐, hostnames, MX with priority)
- Terraform Resource column shows resource addresses
- Summary line: "➕ azurerm_dns_zone ... | ➕ 8 records"

**Snapshot File:** `azurerm-dns-zone-public-records.md`

**Test Class:** `AzureRmDnsZoneSnapshotTests.cs`

---

### TC-AZ-09: Private DNS Zone with A Records (Create Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azurerm_private_dns_zone` with private A records.

**Test Data:**
Plan with:
- 1 `azurerm_private_dns_zone` (CREATE action)
- 5 `azurerm_private_dns_a_record` resources for internal services

**Expected Result:**
- Single section for the private DNS zone
- "DNS Records" table with 5 rows
- Private IP addresses formatted with 🌐 icon (10.0.x.x)
- Terraform Resource column shows `azurerm_private_dns_a_record.*` addresses
- Summary line: "➕ azurerm_private_dns_zone ... | ➕ 5 records"

**Snapshot File:** `azurerm-private-dns-zone-a-records.md`

**Test Class:** `AzureRmDnsZoneSnapshotTests.cs`

---

### TC-AZ-10: DNS Record Row Extractor - All Record Types

**Type:** Unit (Extractor)

**Description:**
Verify that `AzureRmDnsRecordRowExtractor.ExtractRow()` correctly formats values for all DNS record types.

**Test Data:**
Create JSON for each record type:
- A record: `records = ["192.0.2.1", "192.0.2.2"]`
- AAAA record: `records = ["2001:db8::1"]`
- CNAME record: `record = "www.example.com"`
- MX record: `records = [{"preference": 10, "exchange": "mail.example.com"}]`
- SRV record: `records = [{"priority": 10, "weight": 60, "port": 5060, "target": "sip.example.com"}]`
- TXT record: `records = ["v=spf1 include:_spf.example.com ~all"]`
- TXT record (long): `records = ["very long text exceeding 50 characters for truncation testing"]`

**Expected Result:**
- **A/AAAA**: IP addresses with 🌐 icon, comma-separated if multiple (e.g., `🌐 192.0.2.1, 🌐 192.0.2.2`)
- **CNAME**: Target hostname (e.g., `www.example.com`)
- **MX**: Priority + mail server (e.g., `10 mail.example.com`)
- **SRV**: Priority, weight, port, target (e.g., `10 60:5060 sip.example.com`)
- **TXT (short)**: Full text with quotes (e.g., `"v=spf1 include:_spf.example.com ~all"`)
- **TXT (long)**: Truncated to 50 chars with "..." (e.g., `"very long text exceeding 50 characters for tr..."`)
- **Record name "@"**: Display as `@` (root record)

**Test Class:** `AzureRmDnsRecordRowExtractorTests.cs`

---

### TC-AZ-11: Route Table Relationship Registry

**Type:** Unit (Registry)

**Description:**
Verify that `azurerm_route_table` → `azurerm_route` relationship is registered.

**Test Steps:**
1. Query registry for `azurerm_route_table`.

**Expected Result:**
- `azurerm_route_table` has 1 relationship (routes).
- `InlineAttributeName` is `"route"`.
- `ChildReferenceAttribute` is `"route_table_name"`.

**Test Class:** `AzureRmRouteTableRegistryTests.cs`

---

### TC-AZ-12: Route Table with Inline Routes (Create Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azurerm_route_table` with routes defined via the `route` attribute (inline).

**Test Data:**
Plan with:
- 1 `azurerm_route_table` (CREATE action)
- `route` attribute containing 3 route blocks
- Routes with varying next hop types (VirtualAppliance, VirtualNetworkGateway, VnetLocal)

**Expected Result:**
- Single section for the route table
- "Routes" table with 3 rows
- Terraform Resource column shows "`route` attribute" for all rows
- Next hop addresses formatted with 🌐 icon where applicable
- `-` shown for empty next hop addresses (e.g., VnetLocal)
- Summary line: "➕ azurerm_route_table ... | ➕ 3 routes"

**Snapshot File:** `azurerm-route-table-inline-routes.md`

**Test Class:** `AzureRmRouteTableSnapshotTests.cs`

---

### TC-AZ-13: Route Table with Separate Routes (Update Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azurerm_route_table` with routes managed as separate `azurerm_route` resources.

**Test Data:**
Plan with:
- 1 `azurerm_route_table` (no-op)
- 4 separate `azurerm_route` resources (1 add, 1 change, 1 delete, 1 no-op)
- Routes reference route table via `route_table_name` attribute

**Expected Result:**
- Single section for the route table
- "Route Changes" table with 4 rows showing change indicators
- Terraform Resource column shows resource addresses (e.g., `azurerm_route.to_firewall`)
- No separate sections for `azurerm_route` resources
- Inline diffs for modified route attributes
- Summary line: "⏺️ azurerm_route_table ... | ➕ 1 route, 🔄 1 route, ❌ 1 route"

**Snapshot File:** `azurerm-route-table-separate-routes.md`

**Test Class:** `AzureRmRouteTableSnapshotTests.cs`

---

### TC-AZ-14: Route Table with Mixed Route Management (Warning Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify that a route table with both inline and separate routes displays the mixed management warning.

**Test Data:**
Plan with:
- 1 `azurerm_route_table` with 1 route in `route` attribute
- 1 separate `azurerm_route` resource referencing the same route table

**Expected Result:**
- Warning message: "⚠️ **Warning:** This route table has routes managed both inline (via `route` attribute) and as separate `azurerm_route` resources. This configuration will cause conflicts and overwrite routes."
- Both routes in a single table
- Terraform Resource column distinguishes inline vs separate
- `HasMixedManagement` flag is true

**Snapshot File:** `azurerm-route-table-mixed-routes.md`

**Test Class:** `AzureRmRouteTableSnapshotTests.cs`

---

### TC-AZ-15: Route Table Route Row Extractor - Column Mapping

**Type:** Unit (Extractor)

**Description:**
Verify that `AzureRmRouteRowExtractor.ExtractRow()` correctly extracts all required columns from route JSON.

**Test Steps:**
1. Create route JSON with all attributes populated.
2. Call `ExtractRow()` for different next hop types.

**Expected Result:**
- `name`: Formatted with 🆔 icon (e.g., `🆔 default-route`)
- `address_prefix`: Formatted with 🌐 icon (e.g., `🌐 0.0.0.0/0`)
- `next_hop_type`: Shown as-is (e.g., `VirtualAppliance`, `VnetLocal`)
- `next_hop_address`: Formatted with 🌐 icon if present, `-` if empty
- Routes with no next hop address (VnetLocal, Internet) show `-`

**Test Class:** `AzureRmRouteRowExtractorTests.cs`

---

### TC-AZ-16: NSG Relationship Registry

**Type:** Unit (Registry)

**Description:**
Verify that `azurerm_network_security_group` → `azurerm_network_security_rule` relationship is registered.

**Test Steps:**
1. Query registry for `azurerm_network_security_group`.

**Expected Result:**
- `azurerm_network_security_group` has 1 relationship (security rules).
- `InlineAttributeName` is `"security_rule"`.
- `ChildReferenceAttribute` is `"network_security_group_name"`.

**Test Class:** `AzureRmNetworkSecurityGroupRegistryTests.cs`

---

### TC-AZ-17: NSG with Inline Security Rules (Create Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azurerm_network_security_group` with security rules defined via the `security_rule` attribute (inline).

**Test Data:**
Plan with:
- 1 `azurerm_network_security_group` (CREATE action)
- `security_rule` attribute containing 4 rule blocks
- Rules with varying directions (Inbound/Outbound), access (Allow/Deny), protocols (TCP/UDP/Any), and port ranges

**Expected Result:**
- Single section for the NSG
- "Security Rules" table with 4 rows
- Terraform Resource column shows "`security_rule` attribute" for all rows
- Direction icons: ⬇️ Inbound, ⬆️ Outbound
- Access icons: ✅ Allow, ⛔ Deny
- Protocol icons: 🔗 TCP, 🔗 UDP, ✳️ Any
- Port icons: 🔌 443, 🔌 80,443, ✳️ (any)
- Wildcards: ✳️ for any/asterisk values
- Summary line: "➕ azurerm_network_security_group ... | ➕ 4 rules"

**Snapshot File:** `azurerm-nsg-inline-rules.md`

**Test Class:** `AzureRmNetworkSecurityGroupSnapshotTests.cs`

---

### TC-AZ-18: NSG with Separate Security Rules (Update Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azurerm_network_security_group` with security rules managed as separate `azurerm_network_security_rule` resources.

**Test Data:**
Plan with:
- 1 `azurerm_network_security_group` (no-op)
- 5 separate `azurerm_network_security_rule` resources (1 add, 1 change, 1 delete, 2 no-op)
- Rules reference NSG via `network_security_group_name` attribute

**Expected Result:**
- Single section for the NSG
- "Security Rule Changes" table with 5 rows showing change indicators
- Terraform Resource column shows resource addresses (e.g., `azurerm_network_security_rule.allow_https`)
- No separate sections for `azurerm_network_security_rule` resources
- Inline diffs for modified rule attributes
- Summary line: "⏺️ azurerm_network_security_group ... | ➕ 1 rule, 🔄 1 rule, ❌ 1 rule"

**Snapshot File:** `azurerm-nsg-separate-rules.md`

**Test Class:** `AzureRmNetworkSecurityGroupSnapshotTests.cs`

---

### TC-AZ-19: NSG with Mixed Rule Management (Warning Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify that an NSG with both inline and separate rules displays the mixed management warning.

**Test Data:**
Plan with:
- 1 `azurerm_network_security_group` with 1 rule in `security_rule` attribute
- 1 separate `azurerm_network_security_rule` resource referencing the same NSG

**Expected Result:**
- Warning message: "⚠️ **Warning:** This network security group has security rules managed both inline (via `security_rule` attribute) and as separate `azurerm_network_security_rule` resources. This configuration will cause conflicts and overwrite rules."
- Both rules in a single table
- Terraform Resource column distinguishes inline vs separate
- `HasMixedManagement` flag is true

**Snapshot File:** `azurerm-nsg-mixed-rules.md`

**Test Class:** `AzureRmNetworkSecurityGroupSnapshotTests.cs`

---

### TC-AZ-20: NSG Security Rule Row Extractor - All Columns

**Type:** Unit (Extractor)

**Description:**
Verify that `AzureRmNetworkSecurityRuleRowExtractor.ExtractRow()` correctly extracts and formats all columns.

**Test Steps:**
1. Create rule JSON with various combinations of attributes.
2. Test different directions (Inbound/Outbound).
3. Test different access (Allow/Deny).
4. Test different protocols (TCP/UDP/Any).
5. Test different source/destination values (IP, CIDR, Any, Service Tag).

**Expected Result:**
- `name`: Formatted with 🆔 icon (e.g., `🆔 allow-https-inbound`)
- `priority`: Shown as-is (e.g., `100`)
- `direction`: `⬇️ Inbound` or `⬆️ Outbound`
- `access`: `✅ Allow` or `⛔ Deny`
- `protocol`: `🔗 TCP`, `🔗 UDP`, or `✳️` (Any)
- `source`: IP with 🌐 icon, service tag, or ✳️ (any)
- `destination`: IP with 🌐 icon, service tag, or ✳️ (any)
- `ports`: Formatted correctly (see TC-AZ-21)

**Test Class:** `AzureRmNetworkSecurityRuleRowExtractorTests.cs`

---

### TC-AZ-21: NSG Security Rule Row Extractor - Port Range Formatting

**Type:** Unit (Extractor)

**Description:**
Verify that `AzureRmNetworkSecurityRuleRowExtractor` correctly formats port ranges and multiple ports.

**Test Data:**
- Single port: `destination_port_range = "443"`
- Multiple ports: `destination_port_ranges = ["80", "443", "8080"]`
- Port range: `destination_port_range = "1024-65535"`
- Any port: `destination_port_range = "*"`

**Expected Result:**
- Single port: `🔌 443`
- Multiple ports: `🔌 80,443,8080`
- Port range: `🔌 1024-65535`
- Any port: `✳️`

**Test Class:** `AzureRmNetworkSecurityRuleRowExtractorTests.cs`

---

### TC-AZ-22: VNet with Subnets - Configuration Reference Matching (Known After Apply Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify that separate `azurerm_subnet` resources are correctly matched to parent `azurerm_virtual_network` when the VNet's `name` is `(known after apply)` using configuration reference matching.

**Test Data:**
Plan with:
- 1 `azurerm_virtual_network` (CREATE action, `name` in `after_unknown`)
- 2 separate `azurerm_subnet` resources referencing the VNet via `virtual_network_name = azurerm_virtual_network.hub.name`
- `configuration` block with expression references

**Expected Result:**
- Single section for the VNet (subnets are merged, not standalone)
- "Subnets" table with 2 rows
- Configuration reference matching used (value-based matching cannot work)
- No separate sections for `azurerm_subnet` resources

**Snapshot File:** `azurerm-vnet-subnets-known-after-apply.md`

**Test Class:** `AzureRmConfigurationReferenceSnapshotTests.cs`

---

### TC-AZ-23: DNS Zone with Records - Configuration Reference Matching (Known After Apply Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify that DNS record resources are correctly matched to parent `azurerm_dns_zone` when the zone's `name` is `(known after apply)` using configuration reference matching.

**Test Data:**
Plan with:
- 1 `azurerm_dns_zone` (CREATE action, `name` in `after_unknown`)
- 3 DNS record resources (A, CNAME, MX) referencing the zone via `zone_name = azurerm_dns_zone.example.name`
- `configuration` block with expression references

**Expected Result:**
- Single section for the DNS zone (records are merged, not standalone)
- "DNS Records" table with 3 rows
- Configuration reference matching used

**Snapshot File:** `azurerm-dns-zone-records-known-after-apply.md`

**Test Class:** `AzureRmConfigurationReferenceSnapshotTests.cs`

---

### TC-AZ-24: Route Table with Routes - Configuration Reference Matching (Known After Apply Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify that separate `azurerm_route` resources are correctly matched to parent `azurerm_route_table` when the route table's `name` is `(known after apply)` using configuration reference matching.

**Test Data:**
Plan with:
- 1 `azurerm_route_table` (CREATE action, `name` in `after_unknown`)
- 2 separate `azurerm_route` resources referencing the route table via `route_table_name = azurerm_route_table.app.name`
- `configuration` block with expression references

**Expected Result:**
- Single section for the route table (routes are merged, not standalone)
- "Routes" table with 2 rows
- Configuration reference matching used

**Snapshot File:** `azurerm-route-table-routes-known-after-apply.md`

**Test Class:** `AzureRmConfigurationReferenceSnapshotTests.cs`

---

### TC-AZ-25: NSG with Rules - Configuration Reference Matching (Known After Apply Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify that separate `azurerm_network_security_rule` resources are correctly matched to parent `azurerm_network_security_group` when the NSG's `name` is `(known after apply)` using configuration reference matching.

**Test Data:**
Plan with:
- 1 `azurerm_network_security_group` (CREATE action, `name` in `after_unknown`)
- 3 separate `azurerm_network_security_rule` resources referencing the NSG via `network_security_group_name = azurerm_network_security_group.app.name`
- `configuration` block with expression references

**Expected Result:**
- Single section for the NSG (rules are merged, not standalone)
- "Security Rules" table with 3 rows
- Configuration reference matching used

**Snapshot File:** `azurerm-nsg-rules-known-after-apply.md`

**Test Class:** `AzureRmConfigurationReferenceSnapshotTests.cs`

---

### TC-AZ-26: Change Indicators for Azure RM Child Resources

**Type:** Unit (Extractor)

**Description:**
Verify that all Azure RM row extractors correctly determine and return change indicators (➕, 🔄, ❌, ⏺️) based on the resource's action.

**Test Steps:**
1. Create child resource JSON with different actions: "create", "update", "delete", "no-op".
2. Call each row extractor's `ExtractRow()` method.
3. Verify `ChangeIndicator` property in returned row.

**Expected Result:**
- CREATE action: ➕
- UPDATE action: 🔄
- DELETE action: ❌
- NO-OP action: ⏺️
- Applies to all 4 Azure RM child types (subnet, DNS record, route, NSG rule)

**Test Class:** `AzureRmRowExtractorChangeIndicatorTests.cs`

---

### TC-AZ-27: Parent Summary Counts for Azure RM Resources

**Type:** Unit (Model Builder)

**Description:**
Verify that parent resource summary lines include child change counts for all 4 Azure RM resource types.

**Test Steps:**
1. Build report models for each resource type with varying child changes.
2. Verify `ChangedAttributesSummary` and `SummaryHtml` include child counts.

**Expected Result:**
- VNet: "➕ 3 subnets" or "➕ 1 subnet, 🔄 1 subnet, ❌ 1 subnet"
- DNS Zone: "➕ 8 records" or "➕ 2 records, 🔄 1 record, ❌ 1 record"
- Route Table: "➕ 3 routes" or "➕ 1 route, 🔄 1 route, ❌ 1 route"
- NSG: "➕ 4 rules" or "➕ 1 rule, 🔄 1 rule, ❌ 1 rule"
- Format matches existing pattern from Azure AD/Azure DevOps

**Test Class:** `AzureRmParentSummaryTests.cs`

---

### TC-AZ-28: Terraform Resource Column Values

**Type:** Unit (Extractor)

**Description:**
Verify that the "Terraform Resource" column correctly distinguishes inline vs separate children for all Azure RM resource types.

**Test Steps:**
1. Build child rows from inline attributes (e.g., `subnet` attribute).
2. Build child rows from separate resources (e.g., `azurerm_subnet.app`).
3. Verify `TerraformResource` property in returned rows.

**Expected Result:**
- **Inline children**: "`subnet` attribute", "`route` attribute", "`security_rule` attribute"
- **Separate children**: Full resource address (e.g., `azurerm_subnet.app`, `azurerm_dns_a_record.www`, `azurerm_route.to_firewall`, `azurerm_network_security_rule.allow_https`)
- Applies to all 4 resource types

**Test Class:** `AzureRmTerraformResourceColumnTests.cs`

---

## Test Data Requirements

### New Test Data Files

#### VNet/Subnet Test Data

1. **`azurerm-vnet-inline-subnets-plan.json`**
   - 1 `azurerm_virtual_network` with `subnet` attribute containing 3 subnets
   - Subnets with varying NSG assignments and delegations
   - Required for TC-AZ-02

2. **`azurerm-vnet-separate-subnets-plan.json`**
   - 1 `azurerm_virtual_network` (UPDATE)
   - 5 separate `azurerm_subnet` resources (1 add, 1 change, 1 delete, 2 no-op)
   - Required for TC-AZ-03

3. **`azurerm-vnet-mixed-subnets-plan.json`**
   - 1 `azurerm_virtual_network` with 2 inline subnets + 2 separate `azurerm_subnet` resources
   - Required for TC-AZ-04

4. **`azurerm-vnet-subnets-known-after-apply-plan.json`**
   - 1 `azurerm_virtual_network` (CREATE, `name` in `after_unknown`)
   - 2 separate `azurerm_subnet` resources
   - **MUST include `configuration` block with expression references**
   - Required for TC-AZ-22

#### DNS Zone Test Data

5. **`azurerm-dns-zone-public-records-plan.json`**
   - 1 `azurerm_dns_zone` (CREATE)
   - 8 separate DNS record resources (A, AAAA, CNAME, MX, TXT, CAA, etc.)
   - Required for TC-AZ-08

6. **`azurerm-private-dns-zone-a-records-plan.json`**
   - 1 `azurerm_private_dns_zone` (CREATE)
   - 5 `azurerm_private_dns_a_record` resources
   - Required for TC-AZ-09

7. **`azurerm-dns-zone-records-known-after-apply-plan.json`**
   - 1 `azurerm_dns_zone` (CREATE, `name` in `after_unknown`)
   - 3 DNS record resources (A, CNAME, MX)
   - **MUST include `configuration` block with expression references**
   - Required for TC-AZ-23

#### Route Table Test Data

8. **`azurerm-route-table-inline-routes-plan.json`**
   - 1 `azurerm_route_table` with `route` attribute containing 3 routes
   - Routes with varying next hop types
   - Required for TC-AZ-12

9. **`azurerm-route-table-separate-routes-plan.json`**
   - 1 `azurerm_route_table` (no-op)
   - 4 separate `azurerm_route` resources (1 add, 1 change, 1 delete, 1 no-op)
   - Required for TC-AZ-13

10. **`azurerm-route-table-mixed-routes-plan.json`**
    - 1 `azurerm_route_table` with 1 inline route + 1 separate `azurerm_route` resource
    - Required for TC-AZ-14

11. **`azurerm-route-table-routes-known-after-apply-plan.json`**
    - 1 `azurerm_route_table` (CREATE, `name` in `after_unknown`)
    - 2 separate `azurerm_route` resources
    - **MUST include `configuration` block with expression references**
    - Required for TC-AZ-24

#### NSG Test Data

12. **`azurerm-nsg-inline-rules-plan.json`**
    - 1 `azurerm_network_security_group` with `security_rule` attribute containing 4 rules
    - Rules with varying directions, access, protocols, and port ranges
    - Required for TC-AZ-17

13. **`azurerm-nsg-separate-rules-plan.json`**
    - 1 `azurerm_network_security_group` (no-op)
    - 5 separate `azurerm_network_security_rule` resources (1 add, 1 change, 1 delete, 2 no-op)
    - Required for TC-AZ-18

14. **`azurerm-nsg-mixed-rules-plan.json`**
    - 1 `azurerm_network_security_group` with 1 inline rule + 1 separate `azurerm_network_security_rule` resource
    - Required for TC-AZ-19

15. **`azurerm-nsg-rules-known-after-apply-plan.json`**
    - 1 `azurerm_network_security_group` (CREATE, `name` in `after_unknown`)
    - 3 separate `azurerm_network_security_rule` resources
    - **MUST include `configuration` block with expression references**
    - Required for TC-AZ-25

### Comprehensive Demo Update

16. **Update `comprehensive-demo/plan.json`**
    - Add at least one example of each of the 4 Azure RM resource types
    - Ensures regression testing covers all new patterns
    - Update corresponding snapshot baseline

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| **VNet with empty subnet attribute** | No table rendered, no "Subnets" section | TC-AZ-E1 |
| **DNS zone with 100+ records** | All records render in table, acceptable performance (<500ms) | TC-AZ-E2 |
| **NSG with 50+ rules** | All rules render in table, acceptable performance (<500ms) | TC-AZ-E3 |
| **Subnet with null/missing NSG** | NSG column shows `-` | TC-AZ-E4 |
| **Subnet with null/missing delegation** | Delegation column shows `-` | TC-AZ-E5 |
| **DNS TXT record with empty string** | Shows `""` (empty quoted string) | TC-AZ-E6 |
| **Route with empty next hop address** | Next hop address shows `-` | TC-AZ-E7 |
| **NSG rule with wildcard source/destination** | Shows ✳️ symbol | TC-AZ-E8 |
| **NSG rule with service tag** | Shows service tag name (e.g., `Internet`, `VirtualNetwork`) | TC-AZ-E9 |
| **Child references non-existent parent** | Child remains a separate section (no merge) | TC-AZ-E10 |
| **Multiple VNets same module, separate subnets** | Configuration reference matching correctly distinguishes parents | TC-AZ-E11 |
| **Extractor throws exception** | Exception caught and logged, child remains unmerged | TC-AZ-E12 |
| **Invalid JSON in child state** | Gracefully handle parse errors, child remains unmerged | TC-AZ-E13 |

### TC-AZ-E1: VNet with Empty Subnet Attribute

**Type:** Unit (Edge Case)

**Description:**
Verify that a VNet with an empty `subnet` attribute does not render a "Subnets" section.

**Test Data:**
- `azurerm_virtual_network` with `subnet = []`

**Expected Result:**
- No "Subnets" section rendered
- No summary count for subnets

---

### TC-AZ-E2: DNS Zone with 100+ Records (Performance)

**Type:** Integration (Performance)

**Description:**
Verify that a DNS zone with 100+ records renders correctly and completes within acceptable time.

**Test Data:**
- 1 `azurerm_dns_zone`
- 150 separate DNS record resources

**Expected Result:**
- All 150 records render in table
- Report generation completes in <500ms (overhead for merging logic)
- No pagination or truncation

**Test Class:** `AzureRmDnsZonePerformanceTests.cs`

---

### TC-AZ-E3: NSG with 50+ Rules (Performance)

**Type:** Integration (Performance)

**Description:**
Verify that an NSG with 50+ rules renders correctly and completes within acceptable time.

**Test Data:**
- 1 `azurerm_network_security_group`
- 75 separate `azurerm_network_security_rule` resources

**Expected Result:**
- All 75 rules render in table
- Report generation completes in <500ms
- No pagination or truncation

**Test Class:** `AzureRmNsgPerformanceTests.cs`

---

### TC-AZ-E4: Subnet with Null/Missing NSG

**Type:** Unit (Edge Case)

**Description:**
Verify that a subnet with no NSG assignment shows `-` in the NSG column.

**Test Data:**
- Subnet JSON with `network_security_group_id = null` or attribute missing

**Expected Result:**
- NSG column shows `-`

---

### TC-AZ-E5: Subnet with Null/Missing Delegation

**Type:** Unit (Edge Case)

**Description:**
Verify that a subnet with no service delegation shows `-` in the Delegation column.

**Test Data:**
- Subnet JSON with `service_delegation = null` or attribute missing

**Expected Result:**
- Delegation column shows `-`

---

### TC-AZ-E6: DNS TXT Record with Empty String

**Type:** Unit (Edge Case)

**Description:**
Verify that a TXT record with an empty string value displays correctly.

**Test Data:**
- TXT record JSON with `records = [""]`

**Expected Result:**
- Value/Target column shows `""` (empty quoted string)

---

### TC-AZ-E7: Route with Empty Next Hop Address

**Type:** Unit (Edge Case)

**Description:**
Verify that a route with no next hop address (e.g., VnetLocal, Internet) shows `-`.

**Test Data:**
- Route JSON with `next_hop_type = "VnetLocal"` and `next_hop_in_ip_address = null`

**Expected Result:**
- Next hop address column shows `-`

---

### TC-AZ-E8: NSG Rule with Wildcard Source/Destination

**Type:** Unit (Edge Case)

**Description:**
Verify that an NSG rule with wildcard/any source or destination shows ✳️ symbol.

**Test Data:**
- Rule JSON with `source_address_prefix = "*"` and `destination_address_prefix = "*"`

**Expected Result:**
- Source column shows ✳️
- Destination column shows ✳️

---

### TC-AZ-E9: NSG Rule with Service Tag

**Type:** Unit (Edge Case)

**Description:**
Verify that an NSG rule with service tags displays the tag name.

**Test Data:**
- Rule JSON with `source_address_prefix = "Internet"` and `destination_address_prefix = "VirtualNetwork"`

**Expected Result:**
- Source column shows `Internet`
- Destination column shows `VirtualNetwork`
- No special formatting (service tags are recognizable by name)

---

### TC-AZ-E10: Child References Non-Existent Parent

**Type:** Integration (Edge Case)

**Description:**
Verify that a child resource referencing a non-existent parent remains as a standalone section.

**Test Data:**
- 1 `azurerm_subnet` referencing `virtual_network_name = "nonexistent-vnet"`
- No matching `azurerm_virtual_network` in the plan

**Expected Result:**
- Subnet renders as standalone section (not merged)
- No crashes or errors

---

### TC-AZ-E11: Multiple VNets Same Module, Separate Subnets (Configuration Precision)

**Type:** Integration (Edge Case)

**Description:**
Verify that configuration reference matching correctly distinguishes between multiple parents of the same type in the same module.

**Test Data:**
Plan with:
- 2 `azurerm_virtual_network` resources (both CREATE, names in `after_unknown`)
- 2 `azurerm_subnet` resources: one references vnet_a, one references vnet_b
- `configuration` block with expression references

**Expected Result:**
- Each VNet section has the correct subnet (not cross-matched)
- Subnet referencing `vnet_a` merges only with `vnet_a`
- Subnet referencing `vnet_b` merges only with `vnet_b`

**Test Class:** `AzureRmConfigurationReferenceEdgeCaseTests.cs`

---

### TC-AZ-E12: Row Extractor Exception Handling

**Type:** Unit (Error Handling)

**Description:**
Verify that if an Azure RM row extractor throws an exception, the merging logic handles it gracefully.

**Test Steps:**
1. Create a mock row extractor that throws an exception.
2. Attempt to merge children using this extractor.

**Expected Result:**
- Exception is caught and logged.
- Child resource remains in change list (not merged).
- No application crash.

---

### TC-AZ-E13: Invalid JSON in Child State

**Type:** Unit (Error Handling)

**Description:**
Verify that invalid or malformed JSON in child resource state doesn't cause failures.

**Test Steps:**
1. Create child resource with malformed `after` JSON.
2. Attempt merging.

**Expected Result:**
- Gracefully handles parse errors.
- Child may remain unmerged or show empty values.
- No crashes.

---

## Non-Functional Tests

### Performance

**Requirement:** Building a report with 100+ child resources for a single parent should not significantly increase processing time.

**Test Cases:**
- TC-AZ-E2: DNS zone with 150 records completes in <500ms
- TC-AZ-E3: NSG with 75 rules completes in <500ms

**Acceptance Criteria:**
- Linear time complexity (no nested loops over children).
- Overhead for merging and rendering: <500ms for 100+ children.

### Scalability

**Requirement:** The framework should handle realistic Azure deployments with multiple parents and many children.

**Test Case:** TC-AZ-29 (new)

**Description:**
Create a comprehensive plan with all 4 Azure RM resource types and many children:
- 3 VNets with 5 subnets each (15 subnets total)
- 2 DNS zones with 50 records each (100 records total)
- 2 route tables with 10 routes each (20 routes total)
- 3 NSGs with 20 rules each (60 rules total)
- Total: 195 child resources across 10 parents

**Expected Result:**
- All children correctly merged and rendered
- Report generation completes in <2 seconds
- Memory usage is reasonable (<100MB increase)

**Test Class:** `AzureRmScalabilityTests.cs`

---

## Open Questions

### Resolved

- ~~Should DNS zones group all record types in one table or separate tables per type?~~ → **RESOLVED:** Single table with "Type" column (matches architecture decision).
- ~~How to handle DNS zones with 100+ records?~~ → **RESOLVED:** Render all records in table, no pagination for initial implementation (acceptable performance).
- ~~Should NSG rules include a Description column?~~ → **RESOLVED:** Omit for initial implementation (table width considerations). Can be added in future if needed.

### Current

- **Should we add filtering/search for large DNS zones or NSGs?** → Consider in future feature if users report usability issues with 100+ child tables.

- **Should we add column visibility configuration?** → Out of scope for this extension (no command-line options added). Could be a future feature.

## Definition of Done

Test plan is complete when:
- [x] All 28 test cases defined with clear preconditions, steps, and expected results
- [x] 13 edge cases documented with expected behaviors
- [x] All 4 Azure RM resource types covered (VNet/subnet, DNS zone/records, route table/routes, NSG/rules)
- [x] Configuration reference matching test cases for all 4 resource types
- [x] Performance and scalability tests defined
- [x] 16 test data files specified with requirements
- [x] Test coverage matrix maps all acceptance criteria to test cases
- [x] Snapshot test files identified with naming convention
- [ ] The Maintainer has approved the test plan

---

**Next Steps:** Hand off to Maintainer for approval, then to Developer for implementation.
