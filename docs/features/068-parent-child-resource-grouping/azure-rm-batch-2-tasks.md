# Implementation Tasks: Azure RM Parent-Child Resource Grouping (Batch 2)

## Overview

This document breaks down the implementation of Feature 068 Batch 2, which extends the generic parent-child framework by adding support for 4 additional Azure RM resource types:

1. **azurerm_virtual_network** / **azurerm_subnet** (inline via `subnet` attribute + separate resources)
2. **azurerm_dns_zone** / DNS record types (separate resources only, no inline attribute)
3. **azurerm_route_table** / **azurerm_route** (inline via `route` attribute + separate resources)
4. **azurerm_network_security_group** / **azurerm_network_security_rule** (inline via `security_rule` attribute + separate resources)

**Key architectural principle:** The existing generic framework fully supports all 4 resource types without modifications. This extension adds only provider-specific row extractors and relationship registrations.

**References:**
- [azure-rm-batch-specification.md](azure-rm-batch-specification.md) - Extension specification
- [architecture.md](architecture.md) - Azure RM Batch 2 Implementation section (lines 635-1100)
- [azure-rm-batch-2-test-plan.md](azure-rm-batch-2-test-plan.md) - Comprehensive test plan with 28 test cases
- [azure-rm-batch-2-uat-test-plan.md](azure-rm-batch-2-uat-test-plan.md) - UAT validation instructions

**Total Estimated Effort:** ~370 lines of code (~40 subnet, ~90 DNS, ~30 route, ~90 NSG, ~120 registration)

---

## Phase 1: Row Extractors (Core Implementation)

### Task 1.1: Implement AzureRmSubnetRowExtractor

**Priority:** High

**Description:**

Create `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/AzureRmSubnetRowExtractor.cs` to extract subnet details from inline `subnet` blocks or separate `azurerm_subnet` resources.

**Implementation Requirements:**

- **Columns to extract:**
  - `name`: Subnet name with 🆔 icon (via `FormatAttributeValueTableWithRegistry`)
  - `address_prefixes`: CIDR blocks with 🌐 icon (format list as comma-separated or single value)
  - `nsg`: Network security group reference with 🛡️ icon (extract from `security_group` attribute, show `-` if not present)
  - `delegation`: Service delegation name if present (e.g., "Microsoft.Web/serverFarms"), extract from `service_delegation[0].name`

- **Complex attribute handling:**
  - `address_prefixes` is a list/array: Format as comma-separated if ≤2 items, otherwise show first + count
  - `delegation` is a nested object: Extract `service_delegation[0].name` attribute, show `-` if not present
  - `security_group`: May be a resource reference or name string

- **Pattern:** Follow the same structure as `AzureAdGroupMemberRowExtractor`:
  - Implement `IChildRowExtractor` interface
  - Use `JsonStateReader.GetStringProperty()` for attribute access
  - Use `ScribanHelpers.FormatAttributeValueTableWithRegistry()` for icon formatting
  - Return `IReadOnlyDictionary<string, string>` with column name → formatted value mappings

**Acceptance Criteria:**

- [ ] `AzureRmSubnetRowExtractor.cs` created in `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/`
- [ ] Implements `IChildRowExtractor` interface with `ExtractRow()` method
- [ ] Extracts all 4 required columns: name, address_prefixes, nsg, delegation
- [ ] Handles inline subnet blocks (from parent `subnet` attribute array)
- [ ] Handles separate `azurerm_subnet` resources (from child `AfterJson`)
- [ ] Formats address prefixes list correctly (comma-separated if ≤2, otherwise first + count)
- [ ] Extracts delegation from nested `service_delegation[0].name` path
- [ ] Shows `-` for missing optional attributes (nsg, delegation)
- [ ] Uses existing icon providers (🆔 for name, 🌐 for IPs, 🛡️ for NSG)
- [ ] Estimated ~40 lines of code

**Test Cases Validated:**
- TC-AZ-03: Subnet column mapping and formatting (inline subnets)
- TC-AZ-04: Separate subnets merged correctly
- TC-AZ-06: Complex attributes (delegations, service endpoints) formatted correctly
- TC-AZ-E5: Null/empty subnet attributes handled gracefully

**Dependencies:** None

---

### Task 1.2: Implement AzureRmDnsRecordRowExtractor

**Priority:** High

**Description:**

Create `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/AzureRmDnsRecordRowExtractor.cs` to extract DNS record details from any of the 9+ DNS record types (A, AAAA, CNAME, MX, NS, PTR, SRV, TXT, CAA).

**Implementation Requirements:**

- **Columns to extract:**
  - `name`: Record name (e.g., "www", "@", "_dmarc")
  - `type`: Record type (e.g., "A", "CNAME", "MX") - infer from resource type name
  - `ttl`: Time to live in seconds
  - `value`: Formatted record value(s) - varies by record type

- **Complex attribute handling by record type:**
  - **A/AAAA records:** `records` array → format as comma-separated IPs with 🌐 icon
  - **CNAME records:** `record` string → show target hostname
  - **MX records:** `record` array of objects → format as "priority mailserver" (e.g., "10 mail.example.com")
  - **TXT records:** `record` array of objects → truncate to 50 chars with "..." if longer
  - **SRV records:** `record` array of objects → format as "priority weight:port target"
  - **PTR records:** `records` array → show reverse DNS targets
  - **NS records:** `records` array → show nameservers
  - **CAA records:** `record` array of objects → format as "flag tag value"

- **Record type detection:** Infer from resource type name (e.g., `azurerm_dns_a_record` → "A", `azurerm_private_dns_aaaa_record` → "AAAA")

- **Special considerations:**
  - Must handle both public (`azurerm_dns_*_record`) and private (`azurerm_private_dns_*_record`) record types
  - Must handle 9+ different record schemas
  - Consider creating helper methods for each record type's value formatting

**Acceptance Criteria:**

- [ ] `AzureRmDnsRecordRowExtractor.cs` created in `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/`
- [ ] Implements `IChildRowExtractor` interface with `ExtractRow()` method
- [ ] Extracts all 4 required columns: name, type, ttl, value
- [ ] Detects record type from resource type name (public and private variants)
- [ ] Formats A/AAAA records with 🌐 icon for IP addresses
- [ ] Formats CNAME records with target hostname
- [ ] Formats MX records with priority + mail server
- [ ] Formats TXT records with truncation at 50 chars
- [ ] Formats SRV records with priority, weight, port, target
- [ ] Formats PTR, NS, CAA records correctly
- [ ] Handles both public and private DNS record types
- [ ] Shows `-` for missing optional attributes
- [ ] Estimated ~90 lines of code (due to multiple record type schemas)

**Test Cases Validated:**
- TC-AZ-08: Public DNS zone with multiple record types
- TC-AZ-09: Private DNS zone with A records
- TC-AZ-10: All DNS record types (A, AAAA, CNAME, MX, SRV, TXT, PTR, NS, CAA) formatted correctly
- TC-AZ-E6: DNS records with null TTL handled gracefully

**Dependencies:** None

---

### Task 1.3: Implement AzureRmRouteRowExtractor

**Priority:** High

**Description:**

Create `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/AzureRmRouteRowExtractor.cs` to extract route details from inline `route` blocks or separate `azurerm_route` resources.

**Implementation Requirements:**

- **Columns to extract:**
  - `name`: Route name with 🆔 icon
  - `address_prefix`: Destination CIDR with 🌐 icon
  - `next_hop_type`: Hop type (e.g., "VirtualAppliance", "VnetLocal", "Internet", "VirtualNetworkGateway")
  - `next_hop_address`: Hop IP with 🌐 icon if present, otherwise "-"

- **Complex attribute handling:**
  - `next_hop_in_ip_address`: Optional attribute, show "-" if not present
  - Next hop type values are enums with specific Azure values
  - For VnetLocal/Internet/VirtualNetworkGateway types, next hop address is not applicable (show "-")

**Acceptance Criteria:**

- [ ] `AzureRmRouteRowExtractor.cs` created in `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/`
- [ ] Implements `IChildRowExtractor` interface with `ExtractRow()` method
- [ ] Extracts all 4 required columns: name, address_prefix, next_hop_type, next_hop_address
- [ ] Handles inline route blocks (from parent `route` attribute array)
- [ ] Handles separate `azurerm_route` resources (from child `AfterJson`)
- [ ] Formats next hop address with 🌐 icon when present
- [ ] Shows "-" for next hop address when not applicable (VnetLocal, Internet, etc.)
- [ ] Uses existing icon providers (🆔 for name, 🌐 for IPs)
- [ ] Estimated ~30 lines of code

**Test Cases Validated:**
- TC-AZ-12: Inline routes formatted correctly
- TC-AZ-13: Separate routes merged correctly
- TC-AZ-15: Next hop type and address formatted correctly
- TC-AZ-E7: Routes with null next hop address handled gracefully

**Dependencies:** None

---

### Task 1.4: Implement AzureRmNetworkSecurityRuleRowExtractor

**Priority:** High

**Description:**

Create `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/AzureRmNetworkSecurityRuleRowExtractor.cs` to extract security rule details from inline `security_rule` blocks or separate `azurerm_network_security_rule` resources.

**Implementation Requirements:**

- **Columns to extract:**
  - `name`: Rule name with 🆔 icon
  - `priority`: Rule priority (100-4096)
  - `direction`: "⬇️ Inbound" or "⬆️ Outbound" with icon
  - `access`: "✅ Allow" or "⛔ Deny" with icon
  - `protocol`: "🔗 TCP", "🔗 UDP", or "✳️" (Any) with icon
  - `source`: Source address prefix with 🌐 icon or "✳️" for wildcard
  - `destination`: Destination address prefix with 🌐 icon or "✳️" for wildcard
  - `ports`: Port ranges with 🔌 icon (e.g., "🔌 443", "🔌 80,443", "✳️")

- **Complex attribute handling:**
  - **Port ranges:** May be single port, list, or wildcard (`*`)
    - `destination_port_range` (single) vs `destination_port_ranges` (list)
    - Format as comma-separated with 🔌 icon or "✳️" for wildcard
  - **Address prefixes:** Similar pattern with `source_address_prefix` vs `source_address_prefixes` and `destination_address_prefix` vs `destination_address_prefixes`
  - **Service tags:** May use Azure service tags instead of IP ranges (e.g., "Internet", "VirtualNetwork", "AzureLoadBalancer")
  - **Direction/Access/Protocol:** Map enum values to icons and readable names
  - **Wildcards:** Multiple attributes can be `*` - show as "✳️"

- **Special considerations:**
  - Most complex extractor due to 8 columns and multiple attribute variations
  - Rules have both singular and plural attribute names (handle both)
  - Consider extracting description only if present (optional 9th column)

**Acceptance Criteria:**

- [ ] `AzureRmNetworkSecurityRuleRowExtractor.cs` created in `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/`
- [ ] Implements `IChildRowExtractor` interface with `ExtractRow()` method
- [ ] Extracts all 8 required columns: name, priority, direction, access, protocol, source, destination, ports
- [ ] Handles inline security rule blocks (from parent `security_rule` attribute array)
- [ ] Handles separate `azurerm_network_security_rule` resources (from child `AfterJson`)
- [ ] Formats direction with icons: ⬇️ Inbound, ⬆️ Outbound
- [ ] Formats access with icons: ✅ Allow, ⛔ Deny
- [ ] Formats protocol with icons: 🔗 TCP, 🔗 UDP, ✳️ Any
- [ ] Handles both singular and plural port range attributes
- [ ] Handles both singular and plural address prefix attributes
- [ ] Shows ✳️ for wildcard sources, destinations, and ports
- [ ] Formats port ranges with 🔌 icon
- [ ] Formats IP addresses with 🌐 icon
- [ ] Handles Azure service tags (Internet, VirtualNetwork, etc.)
- [ ] Estimated ~90 lines of code (due to attribute variations and formatting logic)

**Test Cases Validated:**
- TC-AZ-17: Inline security rules formatted correctly
- TC-AZ-18: Separate security rules merged correctly
- TC-AZ-20: All NSG rule columns formatted with correct icons
- TC-AZ-21: Port ranges formatted correctly (single, multiple, wildcard)
- TC-AZ-E8: Wildcard sources/destinations show ✳️
- TC-AZ-E9: Service tags rendered correctly

**Dependencies:** None

---

## Phase 2: Provider Registration

### Task 2.1: Register VNet/Subnet Relationship

**Priority:** High

**Description:**

Add Virtual Network → Subnet relationship registration to Azure RM provider module. This enables the framework to detect and merge subnets into their parent VNet sections.

**Implementation Requirements:**

Update `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs` to add the subnet relationship in the `RegisterParentChildRelationships()` method:

```csharp
registry.Register(new ParentChildRelationship
{
    ParentResourceType = "azurerm_virtual_network",
    ChildResourceType = "azurerm_subnet",
    InlineAttributeName = "subnet",
    ChildReferenceAttribute = "virtual_network_name",
    ParentIdAttribute = "name",  // Subnets reference parent by name, not ID
    ChildGroupLabel = "Subnets",
    TableColumns =
    [
        new ChildTableColumn("Name", "name"),
        new ChildTableColumn("Address Prefixes", "address_prefixes"),
        new ChildTableColumn("NSG", "nsg"),
        new ChildTableColumn("Delegation", "delegation")
    ],
    RowExtractor = new AzureRmSubnetRowExtractor()
});
```

**Acceptance Criteria:**

- [ ] `AzureRMModule.cs` updated with subnet relationship registration
- [ ] Relationship defines parent type as `azurerm_virtual_network`
- [ ] Relationship defines child type as `azurerm_subnet`
- [ ] Inline attribute set to `subnet` (for inline subnet blocks)
- [ ] Child reference attribute set to `virtual_network_name` (subnets reference VNets by name)
- [ ] Parent ID attribute set to `name` (not `id` - name-based matching)
- [ ] Child group label set to "Subnets"
- [ ] Table columns match Task 1.1 specification (4 columns)
- [ ] Row extractor references the `AzureRmSubnetRowExtractor` instance
- [ ] Estimated ~15 lines of registration code

**Test Cases Validated:**
- TC-AZ-01: VNet/Subnet relationship registered in provider registry
- TC-AZ-02: Inline subnets detected and merged into parent VNet section
- TC-AZ-04: Separate subnets detected and merged into parent VNet section
- TC-AZ-05: Mixed inline+separate subnets trigger warning message

**Dependencies:** Task 1.1 (AzureRmSubnetRowExtractor must exist)

---

### Task 2.2: Register DNS Zone/Record Relationships

**Priority:** High

**Description:**

Add DNS Zone → DNS Record relationships for all 9+ DNS record types. This is unique because:
- DNS zones have **no inline attribute** (all records are separate resources)
- Multiple child types (A, AAAA, CNAME, MX, SRV, TXT, PTR, NS, CAA) map to the same parent
- Must handle both public and private DNS zones

**Implementation Requirements:**

Update `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs` to add DNS record relationships:

```csharp
// DNS record types to register
var dnsRecordTypes = new[]
{
    "azurerm_dns_a_record",
    "azurerm_dns_aaaa_record",
    "azurerm_dns_cname_record",
    "azurerm_dns_mx_record",
    "azurerm_dns_ns_record",
    "azurerm_dns_ptr_record",
    "azurerm_dns_srv_record",
    "azurerm_dns_txt_record",
    "azurerm_dns_caa_record",
    "azurerm_private_dns_a_record",
    "azurerm_private_dns_aaaa_record",
    "azurerm_private_dns_cname_record",
    "azurerm_private_dns_mx_record",
    "azurerm_private_dns_ptr_record",
    "azurerm_private_dns_srv_record",
    "azurerm_private_dns_txt_record"
};

var dnsRecordExtractor = new AzureRmDnsRecordRowExtractor();
var dnsRecordColumns = new ChildTableColumn[]
{
    new("Name", "name"),
    new("Type", "type"),
    new("TTL", "ttl"),
    new("Value/Target", "value")
};

// Public DNS zone
foreach (var recordType in dnsRecordTypes.Where(t => !t.Contains("private")))
{
    registry.Register(new ParentChildRelationship
    {
        ParentResourceType = "azurerm_dns_zone",
        ChildResourceType = recordType,
        InlineAttributeName = null,  // DNS records are always separate resources
        ChildReferenceAttribute = "zone_name",
        ParentIdAttribute = "name",
        ChildGroupLabel = "DNS Records",
        TableColumns = dnsRecordColumns,
        RowExtractor = dnsRecordExtractor
    });
}

// Private DNS zone
foreach (var recordType in dnsRecordTypes.Where(t => t.Contains("private")))
{
    registry.Register(new ParentChildRelationship
    {
        ParentResourceType = "azurerm_private_dns_zone",
        ChildResourceType = recordType,
        InlineAttributeName = null,
        ChildReferenceAttribute = "zone_name",
        ParentIdAttribute = "name",
        ChildGroupLabel = "DNS Records",
        TableColumns = dnsRecordColumns,
        RowExtractor = dnsRecordExtractor
    });
}
```

**Acceptance Criteria:**

- [ ] `AzureRMModule.cs` updated with DNS record relationships
- [ ] All 9 public DNS record types registered (`azurerm_dns_*_record`)
- [ ] All 7 private DNS record types registered (`azurerm_private_dns_*_record`)
- [ ] Public records reference `azurerm_dns_zone` as parent
- [ ] Private records reference `azurerm_private_dns_zone` as parent
- [ ] Inline attribute set to `null` (DNS records have no inline attribute)
- [ ] Child reference attribute set to `zone_name`
- [ ] Parent ID attribute set to `name`
- [ ] Child group label set to "DNS Records"
- [ ] Table columns match Task 1.2 specification (4 columns)
- [ ] Single `AzureRmDnsRecordRowExtractor` instance shared across all record types
- [ ] Estimated ~60 lines of registration code (includes loop and array definition)

**Test Cases Validated:**
- TC-AZ-07: DNS zone/record relationships registered for all record types
- TC-AZ-08: Public DNS records merged into parent zone section
- TC-AZ-09: Private DNS records merged into parent zone section
- TC-AZ-10: All record types formatted correctly in single table

**Dependencies:** Task 1.2 (AzureRmDnsRecordRowExtractor must exist)

---

### Task 2.3: Register Route Table/Route Relationship

**Priority:** High

**Description:**

Add Route Table → Route relationship registration to Azure RM provider module.

**Implementation Requirements:**

Update `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs`:

```csharp
registry.Register(new ParentChildRelationship
{
    ParentResourceType = "azurerm_route_table",
    ChildResourceType = "azurerm_route",
    InlineAttributeName = "route",
    ChildReferenceAttribute = "route_table_name",
    ParentIdAttribute = "name",
    ChildGroupLabel = "Routes",
    TableColumns =
    [
        new ChildTableColumn("Name", "name"),
        new ChildTableColumn("Address Prefix", "address_prefix"),
        new ChildTableColumn("Next Hop Type", "next_hop_type"),
        new ChildTableColumn("Next Hop Address", "next_hop_address")
    ],
    RowExtractor = new AzureRmRouteRowExtractor()
});
```

**Acceptance Criteria:**

- [ ] `AzureRMModule.cs` updated with route relationship registration
- [ ] Relationship defines parent type as `azurerm_route_table`
- [ ] Relationship defines child type as `azurerm_route`
- [ ] Inline attribute set to `route`
- [ ] Child reference attribute set to `route_table_name`
- [ ] Parent ID attribute set to `name`
- [ ] Child group label set to "Routes"
- [ ] Table columns match Task 1.3 specification (4 columns)
- [ ] Row extractor references the `AzureRmRouteRowExtractor` instance
- [ ] Estimated ~15 lines of registration code

**Test Cases Validated:**
- TC-AZ-11: Route table/route relationship registered
- TC-AZ-12: Inline routes merged into parent route table section
- TC-AZ-13: Separate routes merged into parent route table section
- TC-AZ-14: Mixed inline+separate routes trigger warning message

**Dependencies:** Task 1.3 (AzureRmRouteRowExtractor must exist)

---

### Task 2.4: Register NSG/Security Rule Relationship

**Priority:** High

**Description:**

Add Network Security Group → Security Rule relationship registration to Azure RM provider module.

**Implementation Requirements:**

Update `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs`:

```csharp
registry.Register(new ParentChildRelationship
{
    ParentResourceType = "azurerm_network_security_group",
    ChildResourceType = "azurerm_network_security_rule",
    InlineAttributeName = "security_rule",
    ChildReferenceAttribute = "network_security_group_name",
    ParentIdAttribute = "name",
    ChildGroupLabel = "Security Rules",
    TableColumns =
    [
        new ChildTableColumn("Name", "name"),
        new ChildTableColumn("Priority", "priority"),
        new ChildTableColumn("Direction", "direction"),
        new ChildTableColumn("Access", "access"),
        new ChildTableColumn("Protocol", "protocol"),
        new ChildTableColumn("Source", "source"),
        new ChildTableColumn("Destination", "destination"),
        new ChildTableColumn("Ports", "ports")
    ],
    RowExtractor = new AzureRmNetworkSecurityRuleRowExtractor()
});
```

**Acceptance Criteria:**

- [ ] `AzureRMModule.cs` updated with NSG rule relationship registration
- [ ] Relationship defines parent type as `azurerm_network_security_group`
- [ ] Relationship defines child type as `azurerm_network_security_rule`
- [ ] Inline attribute set to `security_rule`
- [ ] Child reference attribute set to `network_security_group_name`
- [ ] Parent ID attribute set to `name`
- [ ] Child group label set to "Security Rules"
- [ ] Table columns match Task 1.4 specification (8 columns)
- [ ] Row extractor references the `AzureRmNetworkSecurityRuleRowExtractor` instance
- [ ] Estimated ~15 lines of registration code

**Test Cases Validated:**
- TC-AZ-16: NSG/rule relationship registered
- TC-AZ-17: Inline security rules merged into parent NSG section
- TC-AZ-18: Separate security rules merged into parent NSG section
- TC-AZ-19: Mixed inline+separate rules trigger warning message

**Dependencies:** Task 1.4 (AzureRmNetworkSecurityRuleRowExtractor must exist)

---

## Phase 3: Test Data Creation

### Task 3.1: Create VNet Test Data Files

**Priority:** High

**Description:**

Create 4 synthetic Terraform plan JSON files for VNet/subnet testing covering inline, separate, mixed, and known-after-apply scenarios.

**Files to Create:**

1. **`TestData/azurerm-vnet-subnets-inline-plan.json`** (TC-AZ-02, TC-AZ-03)
   - 1 VNet (CREATE action) with 3 inline subnets in the `subnet` attribute array
   - Subnet attributes: name, address_prefixes (list), security_group (optional), service_delegation (optional)
   - Must include varied subnet configurations (one with NSG, one with delegation, one plain)

2. **`TestData/azurerm-vnet-subnets-separate-plan.json`** (TC-AZ-04)
   - 1 VNet (CREATE action)
   - 3 separate `azurerm_subnet` resources referencing the VNet via `virtual_network_name`
   - Must include `configuration` block with expression references: `virtual_network_name = azurerm_virtual_network.example.name`

3. **`TestData/azurerm-vnet-subnets-mixed-plan.json`** (TC-AZ-05)
   - 1 VNet (UPDATE action) with 2 inline subnets in `subnet` attribute
   - 2 separate `azurerm_subnet` resources referencing the same VNet
   - Should trigger mixed management warning

4. **`TestData/azurerm-vnet-subnets-known-after-apply-plan.json`** (TC-AZ-22)
   - 1 VNet (CREATE action) with `name` in `after_unknown` (known after apply)
   - 3 separate `azurerm_subnet` resources
   - Must include `configuration` block with expression references for fallback matching

**Acceptance Criteria:**

- [ ] All 4 VNet test data files created in `TestData/` directory
- [ ] Inline plan has realistic `subnet` attribute array with varied configurations
- [ ] Separate plan has `configuration.root_module.resources[].expressions.virtual_network_name.references` pointing to parent
- [ ] Mixed plan has both `subnet` attribute AND separate `azurerm_subnet` resources
- [ ] Known-after-apply plan has parent `name` in `after_unknown` and configuration references
- [ ] All plans have valid JSON structure matching Terraform plan schema
- [ ] Resource addresses are consistent and realistic (e.g., `azurerm_virtual_network.hub`, `azurerm_subnet.app`)
- [ ] Estimated ~200-300 lines per file

**Test Cases Validated:**
- TC-AZ-02, TC-AZ-03, TC-AZ-04, TC-AZ-05, TC-AZ-06, TC-AZ-22

**Dependencies:** Tasks 1.1, 2.1 (row extractor and registration must exist to validate test data)

---

### Task 3.2: Create DNS Zone Test Data Files

**Priority:** High

**Description:**

Create 4 synthetic Terraform plan JSON files for DNS zone/record testing covering public zones, private zones, multiple record types, and known-after-apply scenarios.

**Files to Create:**

1. **`TestData/azurerm-dns-zone-records-public-plan.json`** (TC-AZ-08, TC-AZ-10)
   - 1 public `azurerm_dns_zone` (CREATE action)
   - 8-10 separate DNS record resources of various types: A (2), AAAA (1), CNAME (2), MX (1), TXT (1), SRV (1), CAA (1)
   - Each record type should have realistic attributes (e.g., MX with priority, SRV with weight/port, TXT with long strings)
   - Must include `configuration` block with expression references: `zone_name = azurerm_dns_zone.example.name`

2. **`TestData/azurerm-dns-zone-records-private-plan.json`** (TC-AZ-09)
   - 1 private `azurerm_private_dns_zone` (CREATE action)
   - 5 separate private A record resources
   - Must include `configuration` block with expression references

3. **`TestData/azurerm-dns-zone-records-mixed-types-plan.json`** (TC-AZ-10)
   - 1 DNS zone with all 9 record types (one of each)
   - Should test all record type formatting variations

4. **`TestData/azurerm-dns-zone-records-known-after-apply-plan.json`** (TC-AZ-23)
   - 1 DNS zone (CREATE action) with `name` in `after_unknown`
   - 5 separate DNS record resources
   - Must include `configuration` block with expression references for fallback matching

**Acceptance Criteria:**

- [ ] All 4 DNS zone test data files created in `TestData/` directory
- [ ] Public zone plan includes diverse record types with realistic attributes
- [ ] Private zone plan uses `azurerm_private_dns_*` resource types
- [ ] Mixed types plan covers all 9 DNS record types
- [ ] Known-after-apply plan has parent `name` in `after_unknown` and configuration references
- [ ] MX records have `record` array with `preference` and `exchange` objects
- [ ] SRV records have `record` array with `priority`, `weight`, `port`, `target` objects
- [ ] TXT records have long strings (>50 chars) to test truncation
- [ ] All plans have valid JSON structure matching Terraform plan schema
- [ ] Estimated ~300-400 lines per file (due to multiple record types)

**Test Cases Validated:**
- TC-AZ-08, TC-AZ-09, TC-AZ-10, TC-AZ-23

**Dependencies:** Tasks 1.2, 2.2 (row extractor and registration must exist)

---

### Task 3.3: Create Route Table Test Data Files

**Priority:** High

**Description:**

Create 4 synthetic Terraform plan JSON files for route table/route testing covering inline, separate, mixed, and known-after-apply scenarios.

**Files to Create:**

1. **`TestData/azurerm-route-table-routes-inline-plan.json`** (TC-AZ-12, TC-AZ-15)
   - 1 route table (CREATE action) with 3 inline routes in the `route` attribute array
   - Route attributes: name, address_prefix, next_hop_type, next_hop_in_ip_address (optional)
   - Must include varied next hop types: VirtualAppliance (with IP), VnetLocal (no IP), Internet (no IP)

2. **`TestData/azurerm-route-table-routes-separate-plan.json`** (TC-AZ-13)
   - 1 route table (CREATE action)
   - 3 separate `azurerm_route` resources referencing the table via `route_table_name`
   - Must include `configuration` block with expression references: `route_table_name = azurerm_route_table.example.name`

3. **`TestData/azurerm-route-table-routes-mixed-plan.json`** (TC-AZ-14)
   - 1 route table (UPDATE action) with 2 inline routes in `route` attribute
   - 2 separate `azurerm_route` resources referencing the same table
   - Should trigger mixed management warning

4. **`TestData/azurerm-route-table-routes-known-after-apply-plan.json`** (TC-AZ-24)
   - 1 route table (CREATE action) with `name` in `after_unknown`
   - 3 separate `azurerm_route` resources
   - Must include `configuration` block with expression references for fallback matching

**Acceptance Criteria:**

- [ ] All 4 route table test data files created in `TestData/` directory
- [ ] Inline plan has realistic `route` attribute array with varied next hop types
- [ ] Separate plan has `configuration` block with route table name references
- [ ] Mixed plan has both `route` attribute AND separate `azurerm_route` resources
- [ ] Known-after-apply plan has parent `name` in `after_unknown` and configuration references
- [ ] Routes include both types with and without next hop IP addresses
- [ ] All plans have valid JSON structure matching Terraform plan schema
- [ ] Estimated ~200-300 lines per file

**Test Cases Validated:**
- TC-AZ-12, TC-AZ-13, TC-AZ-14, TC-AZ-15, TC-AZ-24

**Dependencies:** Tasks 1.3, 2.3 (row extractor and registration must exist)

---

### Task 3.4: Create NSG Test Data Files

**Priority:** High

**Description:**

Create 4 synthetic Terraform plan JSON files for NSG/security rule testing covering inline, separate, mixed, and known-after-apply scenarios.

**Files to Create:**

1. **`TestData/azurerm-nsg-rules-inline-plan.json`** (TC-AZ-17, TC-AZ-20, TC-AZ-21)
   - 1 NSG (CREATE action) with 4 inline security rules in the `security_rule` attribute array
   - Rule attributes: name, priority, direction, access, protocol, source_address_prefix(es), destination_address_prefix(es), destination_port_range(s)
   - Must include varied rules: TCP/80 Allow Inbound, TCP/443 Allow Inbound, Deny All Outbound, UDP any-port
   - Must test both singular and plural port range attributes
   - Must test wildcards (`*`) for sources/destinations

2. **`TestData/azurerm-nsg-rules-separate-plan.json`** (TC-AZ-18)
   - 1 NSG (CREATE action)
   - 4 separate `azurerm_network_security_rule` resources referencing the NSG via `network_security_group_name`
   - Must include `configuration` block with expression references: `network_security_group_name = azurerm_network_security_group.example.name`

3. **`TestData/azurerm-nsg-rules-mixed-plan.json`** (TC-AZ-19)
   - 1 NSG (UPDATE action) with 2 inline rules in `security_rule` attribute
   - 2 separate `azurerm_network_security_rule` resources referencing the same NSG
   - Should trigger mixed management warning

4. **`TestData/azurerm-nsg-rules-known-after-apply-plan.json`** (TC-AZ-25)
   - 1 NSG (CREATE action) with `name` in `after_unknown`
   - 4 separate `azurerm_network_security_rule` resources
   - Must include `configuration` block with expression references for fallback matching

**Acceptance Criteria:**

- [ ] All 4 NSG test data files created in `TestData/` directory
- [ ] Inline plan has realistic `security_rule` attribute array with varied protocols, directions, access
- [ ] Rules include both singular (`destination_port_range`) and plural (`destination_port_ranges`) attributes
- [ ] Rules include wildcard sources/destinations (`*`)
- [ ] Rules include service tags (e.g., "Internet", "VirtualNetwork")
- [ ] Separate plan has `configuration` block with NSG name references
- [ ] Mixed plan has both `security_rule` attribute AND separate `azurerm_network_security_rule` resources
- [ ] Known-after-apply plan has parent `name` in `after_unknown` and configuration references
- [ ] All plans have valid JSON structure matching Terraform plan schema
- [ ] Estimated ~300-400 lines per file (due to rule complexity)

**Test Cases Validated:**
- TC-AZ-17, TC-AZ-18, TC-AZ-19, TC-AZ-20, TC-AZ-21, TC-AZ-25, TC-AZ-E8, TC-AZ-E9

**Dependencies:** Tasks 1.4, 2.4 (row extractor and registration must exist)

---

### Task 3.5: Update Comprehensive Demo Plan

**Priority:** Medium

**Description:**

Update `TestData/comprehensive-demo/plan.json` and `examples/comprehensive-demo-full.json` to include at least one example of each Azure RM resource type (VNet with subnets, DNS zone with records, route table with routes, NSG with rules).

This ensures the comprehensive demo artifact demonstrates the new Azure RM batch 2 functionality alongside existing Azure AD and Azure DevOps patterns.

**Implementation Requirements:**

- Add 1 VNet with 2 inline subnets to the comprehensive demo plan
- Add 1 DNS zone with 5 DNS records (mix of A, CNAME, MX)
- Add 1 route table with 2 inline routes
- Add 1 NSG with 3 inline security rules
- Ensure resources are realistic and fit the "comprehensive demo" theme
- Update both test data and examples directories if separate

**Acceptance Criteria:**

- [ ] `TestData/comprehensive-demo/plan.json` updated to include 1 VNet, 1 DNS zone, 1 route table, 1 NSG
- [ ] Each parent resource has 2-5 child resources (inline or separate)
- [ ] Resources have realistic names and attributes
- [ ] JSON structure is valid
- [ ] File size remains reasonable (<500KB)

**Test Cases Validated:**
- Regression testing (no impact on existing patterns)
- Comprehensive demo includes all new resource types

**Dependencies:** Tasks 1.1-1.4, 2.1-2.4 (all extractors and registrations must exist)

---

## Phase 4: Snapshot Generation

### Task 4.1: Generate VNet Snapshots

**Priority:** High

**Description:**

Generate snapshot baseline files for all 4 VNet test data files. These snapshots document the expected markdown output and serve as regression tests.

**Snapshots to Generate:**

1. `TestData/azurerm-vnet-subnets-inline-plan.md`
2. `TestData/azurerm-vnet-subnets-separate-plan.md`
3. `TestData/azurerm-vnet-subnets-mixed-plan.md`
4. `TestData/azurerm-vnet-subnets-known-after-apply-plan.md`

**Acceptance Criteria:**

- [ ] All 4 VNet snapshot files generated
- [ ] Snapshots show VNets with inline subnet tables (columns: Name, Address Prefixes, NSG, Delegation, Terraform Resource)
- [ ] Inline subnets show "`subnet` attribute" in Terraform Resource column
- [ ] Separate subnets show full resource address (e.g., `azurerm_subnet.app`)
- [ ] Mixed management snapshot includes warning message
- [ ] Known-after-apply snapshot shows subnets merged despite parent name being unknown
- [ ] Icons display correctly: 🆔 for names, 🌐 for IPs, 🛡️ for NSGs
- [ ] Parent summaries include subnet counts (e.g., "➕ 3 subnets")

**Test Cases Validated:**
- TC-AZ-02, TC-AZ-03, TC-AZ-04, TC-AZ-05, TC-AZ-06, TC-AZ-22

**Dependencies:** Task 3.1 (VNet test data files must exist)

---

### Task 4.2: Generate DNS Zone Snapshots

**Priority:** High

**Description:**

Generate snapshot baseline files for all 4 DNS zone test data files.

**Snapshots to Generate:**

1. `TestData/azurerm-dns-zone-records-public-plan.md`
2. `TestData/azurerm-dns-zone-records-private-plan.md`
3. `TestData/azurerm-dns-zone-records-mixed-types-plan.md`
4. `TestData/azurerm-dns-zone-records-known-after-apply-plan.md`

**Acceptance Criteria:**

- [ ] All 4 DNS zone snapshot files generated
- [ ] Snapshots show DNS zones with inline record tables (columns: Name, Type, TTL, Value/Target, Terraform Resource)
- [ ] A/AAAA records show IPs with 🌐 icon
- [ ] CNAME records show target hostname
- [ ] MX records show "priority mailserver" format
- [ ] TXT records are truncated at 50 chars with "..." if longer
- [ ] SRV records show "priority weight:port target" format
- [ ] All records show full resource address (e.g., `azurerm_dns_a_record.www`)
- [ ] Known-after-apply snapshot shows records merged despite parent name being unknown
- [ ] Parent summaries include record counts (e.g., "➕ 8 records")

**Test Cases Validated:**
- TC-AZ-08, TC-AZ-09, TC-AZ-10, TC-AZ-23

**Dependencies:** Task 3.2 (DNS zone test data files must exist)

---

### Task 4.3: Generate Route Table Snapshots

**Priority:** High

**Description:**

Generate snapshot baseline files for all 4 route table test data files.

**Snapshots to Generate:**

1. `TestData/azurerm-route-table-routes-inline-plan.md`
2. `TestData/azurerm-route-table-routes-separate-plan.md`
3. `TestData/azurerm-route-table-routes-mixed-plan.md`
4. `TestData/azurerm-route-table-routes-known-after-apply-plan.md`

**Acceptance Criteria:**

- [ ] All 4 route table snapshot files generated
- [ ] Snapshots show route tables with inline route tables (columns: Name, Address Prefix, Next Hop Type, Next Hop Address, Terraform Resource)
- [ ] Routes with VirtualAppliance show next hop IP with 🌐 icon
- [ ] Routes with VnetLocal/Internet show "-" for next hop address
- [ ] Inline routes show "`route` attribute" in Terraform Resource column
- [ ] Separate routes show full resource address (e.g., `azurerm_route.to_firewall`)
- [ ] Mixed management snapshot includes warning message
- [ ] Known-after-apply snapshot shows routes merged despite parent name being unknown
- [ ] Parent summaries include route counts (e.g., "➕ 3 routes")

**Test Cases Validated:**
- TC-AZ-12, TC-AZ-13, TC-AZ-14, TC-AZ-15, TC-AZ-24

**Dependencies:** Task 3.3 (route table test data files must exist)

---

### Task 4.4: Generate NSG Snapshots

**Priority:** High

**Description:**

Generate snapshot baseline files for all 4 NSG test data files.

**Snapshots to Generate:**

1. `TestData/azurerm-nsg-rules-inline-plan.md`
2. `TestData/azurerm-nsg-rules-separate-plan.md`
3. `TestData/azurerm-nsg-rules-mixed-plan.md`
4. `TestData/azurerm-nsg-rules-known-after-apply-plan.md`

**Acceptance Criteria:**

- [ ] All 4 NSG snapshot files generated
- [ ] Snapshots show NSGs with inline security rule tables (8 columns: Name, Priority, Direction, Access, Protocol, Source, Destination, Ports, Terraform Resource)
- [ ] Direction shows icons: ⬇️ Inbound, ⬆️ Outbound
- [ ] Access shows icons: ✅ Allow, ⛔ Deny
- [ ] Protocol shows icons: 🔗 TCP, 🔗 UDP, ✳️ Any
- [ ] Wildcards show ✳️ symbol for sources, destinations, ports
- [ ] Port ranges show 🔌 icon (e.g., "🔌 443", "🔌 80,443")
- [ ] IP addresses show 🌐 icon
- [ ] Service tags displayed correctly (e.g., "Internet", "VirtualNetwork")
- [ ] Inline rules show "`security_rule` attribute" in Terraform Resource column
- [ ] Separate rules show full resource address (e.g., `azurerm_network_security_rule.allow_https`)
- [ ] Mixed management snapshot includes warning message
- [ ] Known-after-apply snapshot shows rules merged despite parent name being unknown
- [ ] Parent summaries include rule counts (e.g., "➕ 4 rules")

**Test Cases Validated:**
- TC-AZ-17, TC-AZ-18, TC-AZ-19, TC-AZ-20, TC-AZ-21, TC-AZ-25, TC-AZ-E8, TC-AZ-E9

**Dependencies:** Task 3.4 (NSG test data files must exist)

---

### Task 4.5: Update Comprehensive Demo Snapshot

**Priority:** Medium

**Description:**

Regenerate `TestData/comprehensive-demo-full.md` snapshot to include the new Azure RM resource types added in Task 3.5.

**Acceptance Criteria:**

- [ ] Comprehensive demo snapshot regenerated
- [ ] Snapshot includes VNet with subnets section
- [ ] Snapshot includes DNS zone with records section
- [ ] Snapshot includes route table with routes section
- [ ] Snapshot includes NSG with security rules section
- [ ] Existing Azure AD and Azure DevOps sections remain unchanged (regression check)
- [ ] All new sections follow the same formatting and icon conventions

**Test Cases Validated:**
- Regression testing for existing patterns
- Integration testing for all 4 new resource types in a single document

**Dependencies:** Task 3.5 (updated comprehensive demo plan must exist)

---

## Phase 5: UAT Artifact Creation

### Task 5.1: Create UAT Test Plan JSON

**Priority:** High

**Description:**

Create `examples/azure-rm-batch-2/plan.json` - a comprehensive test plan that exercises all 4 Azure RM resource types with inline, separate, and mixed management scenarios. This plan is the source for the UAT artifact.

**Requirements per UAT Test Plan:**

**4 VNets:**
- 1 with 3 inline subnets (CREATE)
- 1 with inline subnets + separate subnets (mixed management, UPDATE)
- 1 with separate subnets only (CREATE, name in `after_unknown`)
- 1 with separate subnets (UPDATE, various changes)

**2 DNS Zones:**
- 1 public zone with 8-10 records of various types (CREATE)
- 1 private zone with 5 A records (CREATE, name in `after_unknown`)

**2 Route Tables:**
- 1 with 3 inline routes (CREATE)
- 1 with separate routes (UPDATE, mixed changes)

**2 NSGs:**
- 1 with 4 inline rules (CREATE)
- 1 with inline rules + separate rules (mixed management, UPDATE)

**Critical:**
- MUST include `configuration` block with expression references for all separate child resources
- Required to demonstrate configuration reference matching for `(known after apply)` scenarios

**Acceptance Criteria:**

- [ ] `examples/azure-rm-batch-2/plan.json` created
- [ ] Contains 4 VNets with ~12 total subnets (mix of inline, separate, mixed)
- [ ] Contains 2 DNS zones (1 public, 1 private) with ~15 total records
- [ ] Contains 2 route tables with ~8 total routes
- [ ] Contains 2 NSGs with ~10 total rules
- [ ] Includes configuration block with expression references for all separate children
- [ ] At least 2 parent resources have `(known after apply)` names to test fallback matching
- [ ] Mixed management scenarios included for VNet, route table, and NSG
- [ ] All change indicators present: CREATE (➕), UPDATE (🔄), DELETE (❌), NO-OP (⏺️)
- [ ] Realistic resource names and attributes
- [ ] Valid JSON structure matching Terraform plan schema
- [ ] File size reasonable (<1MB)
- [ ] Estimated ~1500-2000 lines

**Test Cases Validated:**
- All UAT validation checkpoints from azure-rm-batch-2-uat-test-plan.md

**Dependencies:** All Phase 1 and Phase 2 tasks (extractors and registrations must be working)

---

### Task 5.2: Generate UAT Artifact

**Priority:** High

**Description:**

Generate `artifacts/azure-rm-batch-2-uat.md` from the UAT test plan JSON using the tfplan2md tool. This artifact will be used for visual validation in GitHub and Azure DevOps PRs.

**Command:**
```bash
dotnet run --project src/Oocx.TfPlan2Md -- examples/azure-rm-batch-2/plan.json > artifacts/azure-rm-batch-2-uat.md
```

**Acceptance Criteria:**

- [ ] `artifacts/azure-rm-batch-2-uat.md` generated
- [ ] Artifact includes 4 VNet sections with subnet tables
- [ ] Artifact includes 2 DNS zone sections with record tables
- [ ] Artifact includes 2 route table sections with route tables
- [ ] Artifact includes 2 NSG sections with security rule tables
- [ ] All tables have correct columns per specification
- [ ] All icons display correctly (🆔 🌐 🛡️ 🔌 🔗 ✅ ⛔ ⬇️ ⬆️ ✳️)
- [ ] Mixed management warnings appear in appropriate sections
- [ ] Terraform Resource column correctly shows inline vs separate sources
- [ ] Change indicators correct for all child rows
- [ ] Parent summaries include child change counts
- [ ] Markdown is valid and renders correctly
- [ ] No separate sections for child resources (all merged)
- [ ] File is stable (no random IDs or timestamps)

**Test Cases Validated:**
- All UAT validation checkpoints from azure-rm-batch-2-uat-test-plan.md

**Dependencies:** Task 5.1 (UAT test plan JSON must exist)

---

## Phase 6: Documentation Updates

### Task 6.1: Update Parent-Child Resource Catalog

**Priority:** Medium

**Description:**

Update `docs/features/068-parent-child-resource-grouping/parent-child-resource-catalog.md` to mark the 4 Azure RM resource types as "✅ Implemented" instead of "🚧 In Progress (Batch 2)".

**Acceptance Criteria:**

- [ ] Catalog updated for `azurerm_virtual_network` / `azurerm_subnet` (line ~69-81)
- [ ] Catalog updated for `azurerm_dns_zone` / DNS records (line ~82-109)
- [ ] Catalog updated for `azurerm_route_table` / `azurerm_route` (line ~110-122)
- [ ] Catalog updated for `azurerm_network_security_group` / `azurerm_network_security_rule` (line ~123-157)
- [ ] Status changed from "🚧 In Progress (Batch 2)" to "✅ Implemented"
- [ ] Implementation notes added for each (e.g., "Implemented in Feature 068 Batch 2")

**Test Cases Validated:**
- Documentation accuracy

**Dependencies:** All implementation tasks complete (Phases 1-5)

---

### Task 6.2: Update Work Protocol

**Priority:** Medium

**Description:**

Add a Task Planner work log entry to `docs/features/068-parent-child-resource-grouping/work-protocol.md` documenting the task breakdown work.

**Entry Structure:**

```markdown
### Task Planner - Batch 2 (Azure RM Resources)
- **Date:** [Current date]
- **Summary:** Created implementation task breakdown for Azure RM Batch 2 extension (4 resource types: VNet/subnet, DNS zone/records, route table/routes, NSG/rules). Organized into 6 phases with 19 tasks covering row extractors, provider registration, test data, snapshots, UAT artifacts, and documentation.
- **Artifacts Produced:**
  - azure-rm-batch-2-tasks.md - Detailed task breakdown with acceptance criteria, test case mappings, and effort estimates
  - work-protocol.md - This log entry
- **Problems Encountered:** None. Specification, architecture, and test plan provided comprehensive implementation guidance.
- **Key Decisions:**
  - **Phase organization**: Structured tasks by implementation dependency (extractors → registration → test data → snapshots → UAT → docs)
  - **Task granularity**: Each row extractor is a separate task (~30-90 lines each)
  - **Test data strategy**: 4 files per resource type (inline, separate, mixed, known-after-apply) for comprehensive coverage
  - **UAT focus**: Single comprehensive artifact with 8 parents and ~45 children covering all scenarios
- **Implementation Estimates:**
  - Phase 1 (Row Extractors): ~250 lines (4 extractors)
  - Phase 2 (Registration): ~120 lines (4 relationships + DNS loop)
  - Phase 3 (Test Data): ~16 new files + 1 update
  - Phase 4 (Snapshots): ~17 snapshots to generate/update
  - Phase 5 (UAT): 1 comprehensive plan + 1 artifact
  - Phase 6 (Docs): Catalog + work protocol updates
  - **Total implementation**: ~370 lines of production code
- **Next Steps:** Hand off to Developer for implementation. Developer should follow phase order for logical progression.
```

**Acceptance Criteria:**

- [ ] Work protocol entry added to `work-protocol.md`
- [ ] Entry follows established format
- [ ] Summary captures key aspects of task planning work
- [ ] Artifacts produced listed
- [ ] Key decisions documented
- [ ] Implementation estimates included

**Dependencies:** None (can be done independently)

---

## Implementation Order

### Recommended Sequence:

The tasks are organized into phases that should be completed in order:

1. **Phase 1: Row Extractors** (Tasks 1.1-1.4)
   - Start here - these are the core implementation
   - Can be done in parallel (4 independent extractors)
   - Each task is self-contained

2. **Phase 2: Provider Registration** (Tasks 2.1-2.4)
   - Depends on Phase 1 (extractors must exist)
   - Register each relationship after its extractor is complete
   - Can be done incrementally (one resource type at a time)

3. **Phase 3: Test Data Creation** (Tasks 3.1-3.5)
   - Depends on Phases 1 & 2 (need working implementation to validate test data)
   - Create test data incrementally (one resource type at a time)
   - Comprehensive demo can be done last

4. **Phase 4: Snapshot Generation** (Tasks 4.1-4.5)
   - Depends on Phase 3 (test data must exist)
   - Generate snapshots incrementally as test data is created
   - Use `update-test-snapshots` skill or manual generation

5. **Phase 5: UAT Artifact Creation** (Tasks 5.1-5.2)
   - Depends on Phases 1-4 (complete implementation required)
   - Create comprehensive test plan
   - Generate UAT artifact for visual validation

6. **Phase 6: Documentation Updates** (Tasks 6.1-6.2)
   - Can be done last
   - Update catalog status
   - Document work in protocol

### Parallel Work Opportunities:

- **Row extractors** (Tasks 1.1-1.4) can be implemented in parallel
- **Test data creation** (Tasks 3.1-3.4) can be done in parallel after Phase 2 complete
- **Snapshot generation** (Tasks 4.1-4.4) can be done in parallel after Phase 3 complete

### Milestones:

- **Milestone 1**: Phase 1 complete → Core extractors working
- **Milestone 2**: Phase 2 complete → All relationships registered
- **Milestone 3**: Phase 4 complete → Full test coverage with snapshots
- **Milestone 4**: Phase 5 complete → UAT-ready artifact generated
- **Milestone 5**: Phase 6 complete → Documentation updated, ready for code review

---

## Open Questions

None. All required information is documented in the specification, architecture, test plan, and UAT test plan.

---

## Notes for Developer Agent

### Code Style Guidelines:

- **Follow existing patterns**: Use `AzureAdGroupMemberRowExtractor` as the template for row extractor structure
- **Use helper utilities**: `JsonStateReader.GetStringProperty()`, `ScribanHelpers.FormatAttributeValueTableWithRegistry()`
- **Handle nulls gracefully**: All attribute access should handle missing/null values without throwing
- **Show "-" for missing optional attributes**: Don't leave cells blank
- **Use existing icon providers**: Don't hard-code icons except for extractors (✅ ⛔ ⬇️ ⬆️ ✳️)
- **Format lists consistently**: Comma-separated if ≤2 items, otherwise "first, +N more"
- **Add XML documentation**: Document classes, methods, and complex logic
- **Reference feature in comments**: Include `/// <remarks>Related feature: docs/features/068-parent-child-resource-grouping/azure-rm-batch-specification.md</remarks>`

### Testing Guidelines:

- **Run snapshot tests after each extractor**: Validate formatting immediately
- **Test edge cases**: Null values, empty arrays, wildcards, service tags
- **Validate icons**: Check that icons display correctly in generated markdown
- **Test configuration reference matching**: Ensure `(known after apply)` scenarios work
- **Check mixed management warnings**: Verify warnings appear for inline+separate children

### Performance Considerations:

- **Row extraction is called frequently**: Keep extractor logic efficient (no expensive operations)
- **Avoid unnecessary object allocations**: Reuse extractor instances across relationships
- **JSON parsing is already done**: Don't re-parse JSON, work with `JsonElement` provided

### Common Pitfalls:

- **Forgetting to handle plural attributes**: NSG rules have both `destination_port_range` and `destination_port_ranges`
- **Not testing configuration fallback**: Ensure test data includes `configuration` blocks
- **Hard-coding icons instead of using providers**: Use `FormatAttributeValueTableWithRegistry()` where possible
- **Missing null checks**: Always handle missing/null JSON properties gracefully

---

## Definition of Done

Implementation is complete when:

- [ ] All 19 tasks completed with acceptance criteria met
- [ ] All 4 row extractors implemented and tested (Phase 1)
- [ ] All 4 relationships registered (Phase 2)
- [ ] 16 test data files created + comprehensive demo updated (Phase 3)
- [ ] 17 snapshots generated/updated (Phase 4)
- [ ] UAT artifact created and validated (Phase 5)
- [ ] Documentation updated (Phase 6)
- [ ] All test cases from azure-rm-batch-2-test-plan.md passing
- [ ] No regressions in existing parent-child patterns (Azure AD, Azure DevOps)
- [ ] Code builds successfully in Docker environment
- [ ] Code review feedback addressed
- [ ] CodeQL security scan passes
- [ ] Work protocol entry added

**Next Agent:** Developer (to implement the tasks in phase order)
