# UAT Test Plan: Azure RM Parent-Child Resource Grouping (Batch 2)

## Goal

Verify that the 4 additional Azure RM parent-child resource types (VNet/subnet, DNS zone/records, route table/routes, NSG/rules) render correctly in GitHub and Azure DevOps PR comments, with children displayed in inline tables rather than separate sections.

This UAT focuses on the **new Azure RM resource types** added in Batch 2. The generic framework and configuration reference matching were already validated in the initial Feature 068 UAT.

## Artifacts

### Feature-Specific Test Artifact (Required)

**Purpose:** Focus testing on the 4 new Azure RM resource types being added.

**Artifact Path:** `artifacts/azure-rm-batch-2-uat.md`

**Creation Instructions:**
- **Source Plan:** `examples/azure-rm-batch-2/plan.json` (create a plan that exercises all 4 resource types)
- **Command:** `tfplan2md examples/azure-rm-batch-2/plan.json > artifacts/azure-rm-batch-2-uat.md`
- **Rationale:** Demonstrates all 4 new parent-child patterns with inline, separate, and mixed management scenarios
- **Key Resources:**
  - `azurerm_virtual_network.hub_vnet` with inline subnets
  - `azurerm_virtual_network.spoke_vnet` with separate subnets (mixed management)
  - `azurerm_dns_zone.example_com` with multiple record types
  - `azurerm_private_dns_zone.internal` with private A records
  - `azurerm_route_table.app_routes` with inline routes
  - `azurerm_route_table.data_routes` with separate routes
  - `azurerm_network_security_group.app_nsg` with inline rules
  - `azurerm_network_security_group.web_nsg` with separate rules (mixed management)

**Resource Count:**
- 4 VNets (2 inline, 2 separate/mixed) with ~12 total subnets
- 2 DNS zones (1 public, 1 private) with ~15 total records
- 2 route tables (1 inline, 1 separate) with ~8 total routes
- 2 NSGs (1 inline, 1 separate/mixed) with ~10 total rules

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in existing parent-child patterns.

**Artifact Paths:** 
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically using the `generate-demo-artifacts` skill. It should include at least one example of each Azure RM resource type alongside existing Azure AD and Azure DevOps patterns.

## Test Steps

1. Run UAT using the `UAT Tester` agent.
2. UAT will post TWO separate PR comments:
   - **Feature-Specific Report**: Tests the 4 new Azure RM resource types
   - **Comprehensive Demo**: Regression test for side effects on existing patterns
3. Verify both reports on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

### Section 1: Virtual Network with Subnets

#### 1.1 VNet with Inline Subnets

**Resource:** `azurerm_virtual_network.hub_vnet`

**What to Verify:**

1. **Single Section:** There should be ONLY ONE section for the VNet. Check that NO separate sections exist for any subnets.

2. **"Subnets" Table:** Within the VNet section, verify a "Subnets" table exists.

3. **Table Columns:** The table should have these columns:
   - Change (➕, 🔄, ❌, ⏺️)
   - Name (with 🆔 icon)
   - Address Prefixes (with 🌐 icon)
   - NSG (with 🛡️ icon or `-`)
   - Delegation (service name or `-`)
   - Terraform Resource

4. **Inline Source:** All subnets should show "`subnet` attribute" in the Terraform Resource column.

5. **Formatting:**
   - Subnet names: `🆔 snet-app`, `🆔 snet-data`, etc.
   - Address prefixes: `🌐 10.0.1.0/24`
   - NSG references: `🛡️ nsg-app` or `-` if none
   - Delegations: `Microsoft.Web/serverFarms` or `-` if none

6. **Summary Line:** Should include subnet count (e.g., "➕ azurerm_virtual_network ... | ➕ 3 subnets")

**Expected Outcome:** All inline subnets render as table rows within the parent VNet section, with proper formatting and icons.

---

#### 1.2 VNet with Separate Subnets (Mixed Management)

**Resource:** `azurerm_virtual_network.spoke_vnet`

**What to Verify:**

1. **Mixed Management Warning:** The section should display:
   > ⚠️ **Warning:** This virtual network has subnets managed both inline (via `subnet` attribute) and as separate `azurerm_subnet` resources. This configuration will cause conflicts and overwrite subnets.

2. **Single Table:** All subnets (both inline and separate) should be in the same "Subnet Changes" table.

3. **Terraform Resource Column:** 
   - Inline subnets: "`subnet` attribute"
   - Separate subnets: Full resource address (e.g., `azurerm_subnet.integration`)

4. **Change Indicators:** Mix of ➕, 🔄, ❌, ⏺️ as appropriate.

5. **Inline Diffs:** For modified subnets (🔄), verify inline before/after formatting:
   ```
   - 🌐 10.1.1.0/24
   + 🌐 10.1.1.0/23
   ```

6. **Summary Line:** Should indicate mixed management (e.g., "Mixed subnet management detected" or child change counts)

**Expected Outcome:** Warning displayed prominently. Users can see all subnets in one place but understand the configuration conflict.

---

### Section 2: DNS Zone with Records

#### 2.1 Public DNS Zone with Multiple Record Types

**Resource:** `azurerm_dns_zone.example_com`

**What to Verify:**

1. **Single Section:** There should be ONLY ONE section for the DNS zone. Check that NO separate sections exist for any DNS records.

2. **"DNS Records" Table:** Within the DNS zone section, verify a "DNS Records" table exists.

3. **Table Columns:**
   - Change
   - Name (record name: `@`, `www`, `mail`, etc.)
   - Type (A, AAAA, CNAME, MX, TXT, CAA, etc.)
   - TTL (time to live in seconds)
   - Value/Target (formatted based on record type)
   - Terraform Resource

4. **Record Type Formatting:**
   - **A records:** IP addresses with 🌐 icon (e.g., `🌐 192.0.2.1`)
   - **AAAA records:** IPv6 addresses with 🌐 icon (e.g., `🌐 2001:db8::1`)
   - **CNAME records:** Target hostname (e.g., `www.example.com`)
   - **MX records:** Priority + mail server (e.g., `10 mail.example.com`)
   - **TXT records:** Quoted text, truncated if >50 chars (e.g., `"v=spf1 include:_spf.example.com ~all"`)
   - **CAA records:** Flag + tag + value (e.g., `0 issue "letsencrypt.org"`)

5. **Terraform Resource Column:** Full resource addresses (e.g., `azurerm_dns_a_record.root`, `azurerm_dns_cname_record.blog`)

6. **Summary Line:** Should include record count (e.g., "➕ azurerm_dns_zone ... | ➕ 8 records")

**Expected Outcome:** All DNS records grouped in a single table with type-specific value formatting. Easy to scan all DNS changes at once.

---

#### 2.2 Private DNS Zone with A Records

**Resource:** `azurerm_private_dns_zone.internal`

**What to Verify:**

1. **Single Section:** Private DNS zone has one section with all private records merged.

2. **Private IPs:** A records should show private IP addresses (e.g., `🌐 10.0.2.10`, `🌐 10.0.2.11`).

3. **Resource Addresses:** Terraform Resource column shows `azurerm_private_dns_a_record.*` addresses.

4. **Summary Line:** Should include record count for private zone.

**Expected Outcome:** Private DNS zones work identically to public zones, with proper formatting for internal resources.

---

### Section 3: Route Table with Routes

#### 3.1 Route Table with Inline Routes

**Resource:** `azurerm_route_table.app_routes`

**What to Verify:**

1. **Single Section:** There should be ONLY ONE section for the route table. No separate route sections.

2. **"Routes" Table:** Within the route table section, verify a "Routes" table exists.

3. **Table Columns:**
   - Change
   - Name (with 🆔 icon)
   - Address Prefix (with 🌐 icon)
   - Next Hop Type (`VirtualAppliance`, `VnetLocal`, `Internet`, `VirtualNetworkGateway`)
   - Next Hop Address (with 🌐 icon or `-`)
   - Terraform Resource

4. **Next Hop Formatting:**
   - **VirtualAppliance:** Shows next hop IP (e.g., `🌐 10.0.1.4`)
   - **VnetLocal/Internet:** Shows `-` (no next hop address)
   - **VirtualNetworkGateway:** Shows `-` (no IP needed)

5. **Inline Source:** All routes should show "`route` attribute" in the Terraform Resource column.

6. **Summary Line:** Should include route count (e.g., "➕ azurerm_route_table ... | ➕ 3 routes")

**Expected Outcome:** All inline routes render as table rows with proper next hop formatting.

---

#### 3.2 Route Table with Separate Routes

**Resource:** `azurerm_route_table.data_routes`

**What to Verify:**

1. **"Route Changes" Table:** Separate routes displayed with change indicators.

2. **Terraform Resource Column:** Full resource addresses (e.g., `azurerm_route.to_firewall`, `azurerm_route.to_onprem`)

3. **Inline Diffs:** For modified routes (🔄), verify before/after formatting.

4. **No Separate Sections:** Confirm no standalone `azurerm_route` sections exist.

**Expected Outcome:** Separate routes correctly merged into parent route table section.

---

### Section 4: Network Security Group with Rules

#### 4.1 NSG with Inline Security Rules

**Resource:** `azurerm_network_security_group.app_nsg`

**What to Verify:**

1. **Single Section:** There should be ONLY ONE section for the NSG. No separate rule sections.

2. **"Security Rules" Table:** Within the NSG section, verify a "Security Rules" table exists.

3. **Table Columns:**
   - Change
   - Name (with 🆔 icon)
   - Priority (rule priority number)
   - Direction (with icon: ⬇️ Inbound, ⬆️ Outbound)
   - Access (with icon: ✅ Allow, ⛔ Deny)
   - Protocol (with icon: 🔗 TCP, 🔗 UDP, ✳️ Any)
   - Source (IP with 🌐 icon, service tag, or ✳️)
   - Destination (IP with 🌐 icon, service tag, or ✳️)
   - Ports (with 🔌 icon or ✳️)
   - Terraform Resource

4. **Icon Formatting:**
   - Direction: `⬇️ Inbound` or `⬆️ Outbound`
   - Access: `✅ Allow` or `⛔ Deny`
   - Protocol: `🔗 TCP`, `🔗 UDP`, or `✳️` (Any)
   - Ports: `🔌 443`, `🔌 80,443`, `🔌 1024-65535`, or `✳️` (any)
   - Wildcards: ✳️ for any/asterisk source/destination

5. **Service Tags:** Should show tag name (e.g., `Internet`, `VirtualNetwork`, `AzureLoadBalancer`)

6. **Inline Source:** All rules should show "`security_rule` attribute" in the Terraform Resource column.

7. **Summary Line:** Should include rule count (e.g., "➕ azurerm_network_security_group ... | ➕ 4 rules")

**Expected Outcome:** All inline rules render as table rows with extensive icon usage for readability.

---

#### 4.2 NSG with Separate Rules (Mixed Management)

**Resource:** `azurerm_network_security_group.web_nsg`

**What to Verify:**

1. **Mixed Management Warning:** The section should display:
   > ⚠️ **Warning:** This network security group has security rules managed both inline (via `security_rule` attribute) and as separate `azurerm_network_security_rule` resources. This configuration will cause conflicts and overwrite rules.

2. **Single Table:** All rules (both inline and separate) should be in the same "Security Rule Changes" table.

3. **Terraform Resource Column:**
   - Inline rules: "`security_rule` attribute"
   - Separate rules: Full resource address (e.g., `azurerm_network_security_rule.allow_ssh`)

4. **Change Indicators:** Mix of ➕, 🔄, ❌, ⏺️ as appropriate.

5. **Inline Diffs:** For modified rules (🔄), verify before/after formatting for source/destination changes.

**Expected Outcome:** Warning displayed. All rules visible in one table with source identification.

---

### Section 5: Configuration Reference Matching (Known After Apply)

**Context:** The UAT plan should include at least one parent resource being created (CREATE action) where the `name` attribute is `(known after apply)`. Separate child resources should reference this parent via Terraform expressions.

**What to Verify:**

1. **Look for parent resources with CREATE action:**
   - Check VNets, DNS zones, route tables, or NSGs being created
   - These should have `(known after apply)` values

2. **Verify children are merged (not standalone):**
   - Children should appear in the parent's table
   - NO standalone sections for the child resources

3. **Configuration Fallback Worked:**
   - Despite parent name being unknown at plan time, children correctly matched to parent
   - This demonstrates configuration reference matching is working

**Expected Outcome:** All separate children merge correctly even when parent IDs/names are `(known after apply)`. This was already validated in initial Feature 068 UAT, so this verification is primarily to confirm it still works with the new Azure RM resource types.

---

### Section 6: Regression Testing (Comprehensive Demo)

**In the comprehensive demo (second PR comment, labeled "🔄 Regression Test"):**

**Verify:**
- Azure AD groups still render correctly with member tables
- Azure DevOps teams still render correctly with member/admin tables
- No unintended changes to existing resource rendering
- The 4 new Azure RM resource types render correctly in a comprehensive context

**Expected Outcome:** All existing parent-child patterns (Azure AD, Azure DevOps) remain unchanged. New Azure RM patterns integrate seamlessly.

---

### Section 7: Cross-Platform Layout

**GitHub:**
- All tables have proper markdown headers (`| Header |`)
- Change indicators display correctly (➕, 🔄, ❌, ⏺️, ✳️)
- Resource addresses formatted as monospace code
- Icons render correctly (🆔, 🌐, 🛡️, 🔌, 🔗, ✅, ⛔, ⬇️, ⬆️, ✳️)
- Warning messages display with emoji
- Inline diffs have proper background colors

**Azure DevOps:**
- Tables render cleanly (no broken markdown)
- All icons and change indicators display correctly
- No layout issues or overflow
- Warning messages are visible
- Inline diffs readable

**Expected Outcome:** Both platforms render the 4 new Azure RM resource types consistently and readably.

---

## Success Criteria

- [ ] **VNet/Subnet:** All VNets have subnets merged into inline tables (no standalone subnet sections)
- [ ] **VNet Mixed Management:** Mixed management warnings display correctly
- [ ] **DNS Zones:** All DNS records grouped by zone with type-specific formatting
- [ ] **DNS Record Types:** Multiple record types (A, AAAA, CNAME, MX, TXT, CAA) render correctly
- [ ] **Route Tables:** All routes merged into parent route table sections
- [ ] **Route Formatting:** Next hop types and addresses formatted correctly
- [ ] **NSGs:** All security rules merged into parent NSG sections
- [ ] **NSG Icons:** Direction, access, protocol, port icons display correctly
- [ ] **NSG Wildcards:** Wildcard sources/destinations show ✳️ symbol
- [ ] **Configuration Reference Matching:** Separate children merge correctly for `(known after apply)` parents
- [ ] **Change Indicators:** All child rows show correct ➕, 🔄, ❌, ⏺️ indicators
- [ ] **Summary Counts:** Parent summaries include child change counts for all 4 resource types
- [ ] **Terraform Resource Column:** Clear distinction between inline vs separate children
- [ ] **Cross-Platform:** Both GitHub and Azure DevOps render all 4 resource types cleanly
- [ ] **Regression:** Existing parent-child patterns (Azure AD, Azure DevOps) remain unchanged

## Feedback Opportunities

- Do the table formats make it easier to understand Azure network infrastructure?
- Are the column choices appropriate for each resource type?
- Are there too many columns in the NSG rules table?
- Is the icon usage helpful or distracting?
- Are the mixed management warnings clear and actionable?
- Do DNS zones with 20+ records remain readable?
- Should any additional attributes be shown in the tables?
- Are there any rendering issues or layout problems?

---

## Notes for UAT Tester Agent

### Creating the Feature-Specific Test Plan

The UAT Tester should work with the Developer to create `examples/azure-rm-batch-2/plan.json` containing:

1. **4 VNets:**
   - 1 with 3 inline subnets (CREATE)
   - 1 with inline subnets + separate subnets (mixed management, UPDATE)
   - 1 with separate subnets only (CREATE, name in `after_unknown`)
   - 1 with separate subnets (UPDATE, various changes)

2. **2 DNS Zones:**
   - 1 public zone with 8-10 records of various types (CREATE)
   - 1 private zone with 5 A records (CREATE, name in `after_unknown`)

3. **2 Route Tables:**
   - 1 with 3 inline routes (CREATE)
   - 1 with separate routes (UPDATE, mixed changes)

4. **2 NSGs:**
   - 1 with 4 inline rules (CREATE)
   - 1 with inline rules + separate rules (mixed management, UPDATE)

5. **Configuration Block:**
   - MUST include `configuration` block with expression references for all separate child resources
   - Required to demonstrate configuration reference matching for `(known after apply)` scenarios

### Validation Focus

The UAT Tester should pay special attention to:
- **Table readability:** Are 9-10 columns (NSG rules) still readable?
- **Icon overload:** Do the many icons (🆔, 🌐, 🛡️, 🔌, 🔗, ✅, ⛔, ⬇️, ⬆️, ✳️) help or hinder?
- **DNS zones:** Do 15+ records in a single table remain scannable?
- **Mixed management:** Are warnings prominent enough?
- **Cross-platform:** Any differences between GitHub and Azure DevOps rendering?

### Success Threshold

UAT **passes** if:
- All 4 resource types render correctly in both platforms
- No separate sections for child resources (all merged)
- Configuration reference matching works for `(known after apply)` scenarios
- Mixed management warnings display correctly
- No regressions in existing patterns

UAT **fails** if:
- Any child resources appear as standalone sections (merging failure)
- Tables don't render correctly on either platform
- Icons or formatting are broken
- Configuration reference matching doesn't work
- Existing patterns (Azure AD, Azure DevOps) are broken

---

**Next Steps:** Hand off to UAT Tester for execution.
