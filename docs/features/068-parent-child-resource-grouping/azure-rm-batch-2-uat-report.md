# UAT Report: Azure RM Parent-Child Resource Grouping (Batch 2)

**Feature:** Parent-Child Resource Grouping - Azure RM Batch 2 Extension  
**Test Date:** 2025-02-12  
**Tester:** UAT Tester Agent  
**Status:** ⏸️ **PENDING APPROVAL** (GitHub), ❌ **BLOCKED** (Azure DevOps)

---

## Executive Summary

UAT was initiated for Azure RM Batch 2, which extends parent-child resource grouping to 4 additional Azure RM resource types:
- `azurerm_virtual_network` / `azurerm_subnet`
- `azurerm_dns_zone` / DNS records (9+ types)
- `azurerm_route_table` / `azurerm_route`
- `azurerm_network_security_group` / `azurerm_network_security_rule`

**GitHub UAT:** ✅ PR created successfully with both feature-specific and regression artifacts  
**Azure DevOps UAT:** ❌ Failed due to Azure CLI authentication issue (environment configuration problem)

---

## UAT Artifacts

### Feature-Specific Artifact

**File:** `artifacts/azure-rm-batch-2-uat.md`

**Content:**
- 6 resource examples demonstrating all 4 Azure RM parent-child patterns
- VNet with inline subnets (CREATE)
- VNet with mixed management (CREATE with inline + separate)
- VNet with separate subnets (UPDATE with various changes)
- DNS zone with multiple record types (CREATE)
- Route table with inline routes (CREATE)
- NSG with inline security rules (CREATE, includes both Feature 016 and parent-child framework tables)

**Validation Points Included:**
- Single section per parent resource (no standalone child sections)
- Correct table columns for each resource type
- Change column as first column in all tables
- Icon usage: 🆔 (names), 🌐 (IPs), 🛡️ (NSGs), 🔌 (ports), 🔗 (protocols), ✅ (allow), ⛔ (deny), ⬇️ (inbound), ⬆️ (outbound), ✳️ (wildcard)
- Mixed management warnings
- Terraform Resource column distinguishing inline vs separate children
- Summary lines with child change counts
- Cross-platform validation checklist

### Regression Artifact

**GitHub:** `artifacts/comprehensive-demo-simple-diff.md`  
**Azure DevOps:** `artifacts/comprehensive-demo.md`

**Purpose:** Ensure no unintended side effects in existing parent-child patterns (Azure AD groups, Azure DevOps teams/groups) or other resource rendering.

---

## UAT Execution

### GitHub UAT

**PR:** #68  
**URL:** https://github.com/oocx/tfplan2md-uat/pull/68  
**Status:** ✅ **CREATED**

**Comments Posted:**
1. **Feature-Specific Report** (13,482 chars) - Azure RM Batch 2 artifact with detailed validation points
2. **Regression Report** (29,883 chars) - comprehensive-demo-simple-diff.md for side-effects validation

**What to Verify:**

#### VNet/Subnets
- [ ] Inline subnets render in table under VNet (no separate sections)
- [ ] Separate subnets group under VNet
- [ ] Mixed management warning appears for mixed inline/separate
- [ ] Table columns: Change, Name, Address Prefixes, NSG, Delegation, Terraform Resource
- [ ] Icons render: 🆔 for names, 🌐 for IPs, 🛡️ for NSGs (if icon provider configured)
- [ ] Change column shows ➕, 🔄, ❌, ⏺️ indicators
- [ ] Terraform Resource shows "subnet attribute" for inline, full address for separate

#### DNS Zones/Records
- [ ] All DNS records group under parent zone (no separate record sections)
- [ ] Records grouped by type in multiple "DNS Records" tables
- [ ] Table columns: Change, Name, Type, TTL, Value/Target, Terraform Resource
- [ ] A/AAAA records show IP addresses with 🌐 icon
- [ ] CNAME records show target hostname
- [ ] MX/TXT records render (may show "Unknown" type - known limitation)
- [ ] Summary line includes record counts

#### Route Tables/Routes
- [ ] Inline routes render in table under route table
- [ ] Table columns: Change, Name, Address Prefix, Next Hop Type, Next Hop Address, Terraform Resource
- [ ] Icons: 🆔 for names, 🌐 for address prefixes and next hop IPs
- [ ] Next Hop Address shows IP for VirtualAppliance, `-` for VnetLocal/Internet/Gateway
- [ ] Terraform Resource shows "route attribute" for inline

#### NSG/Security Rules
- [ ] Two "Security Rules" tables render (by design: Feature 016 + parent-child framework)
- [ ] Feature 016 table (first): Shows detailed columns (Source Addresses, Source Ports, Destination Addresses, Destination Ports, Description)
- [ ] Parent-child framework table (second): Shows consolidated columns (Change, Name, Priority, Direction, Access, Protocol, Source, Destination, Ports, Terraform Resource)
- [ ] Icons render: 🆔, ⬇️, ⬆️, ✅, ⛔, 🔗, 🔌, ✳️
- [ ] Direction icons: ⬇️ Inbound, ⬆️ Outbound
- [ ] Access icons: ✅ Allow, ⛔ Deny
- [ ] Protocol icons: 🔗 TCP/UDP, ✳️ Any
- [ ] Port formatting: 🔌 with port number, ✳️ for any
- [ ] Wildcard sources/destinations show ✳️

#### Cross-Platform
- [ ] All tables render cleanly (no broken markdown)
- [ ] All icons display correctly
- [ ] Warning messages visible and clear
- [ ] Resource addresses formatted as monospace code
- [ ] No horizontal scrolling issues

### Azure DevOps UAT

**Status:** ❌ **FAILED - ENVIRONMENT ISSUE**

**Error:** Azure CLI authentication failed  
**Root Cause:** `az account show` requires Azure subscription login (`az login`), but the GitHub Actions environment only has `AZURE_DEVOPS_EXT_PAT` set. The copilot-setup-steps workflow should have configured Azure CLI authentication but did not.

**Impact:** Unable to create Azure DevOps UAT PR

**Workarounds Attempted:**
1. Verified `AZURE_DEVOPS_EXT_PAT` is set (✅ confirmed)
2. Checked Azure CLI authentication (`az account show` fails)
3. Attempted to use PAT for authentication (requires subscription login, not just DevOps PAT)

**Decision:** GitHub UAT should be sufficient for visual validation. Azure DevOps uses similar markdown rendering, and the feature-specific artifact includes cross-platform validation checklist.

---

## Known Issues (Non-Blocking)

### Minor Issues Noted in Code Review

1. **NSG Icon Missing:** Subnet NSG references show `` `nsg-app` `` instead of `` `🛡️ nsg-app` ``  
   **Severity:** Cosmetic only  
   **Impact:** No functional impact, slight readability reduction  
   **Decision:** Can be addressed in future enhancement

2. **Duplicate "Security Rules" Heading:** NSG rendering shows two "Security Rules" headings  
   **Severity:** Info only  
   **Reason:** By design - Feature 016 semantic diff table and parent-child framework table coexist  
   **Decision:** Acceptable, provides dual-level access to rule information

---

## Environment Issues

### Azure CLI Authentication

**Issue:** Azure CLI not authenticated in GitHub Actions environment  
**Expected:** copilot-setup-steps workflow should configure both `gh` and `az` CLI authentication  
**Actual:** Only `gh` CLI authenticated, `az` CLI requires manual `az login`  
**Impact:** Cannot create Azure DevOps UAT PRs in this environment

**Root Cause Analysis:**
- GitHub CLI authentication works via `gh auth setup-git` using `GH_UAT_TOKEN` secret
- Azure DevOps CLI requires Azure subscription authentication (`az login`) before `az devops` commands work
- Having `AZURE_DEVOPS_EXT_PAT` set is not sufficient - Azure CLI needs subscription context
- The setup workflow likely needs to authenticate Azure CLI differently for DevOps-only operations

**Recommendation:** Update copilot-setup-steps workflow to properly configure Azure CLI for DevOps operations, or modify UAT scripts to work with PAT-only authentication.

---

## Test Coverage

### Scenarios Covered in Feature-Specific Artifact

| Resource Type | Scenario | Test Case |
|--------------|----------|-----------|
| VNet/Subnet | Inline subnets | VNet hub_vnet with 3 inline subnets (CREATE) |
| VNet/Subnet | Mixed management | VNet legacy_vnet with 2 inline + 1 separate subnet (CREATE) |
| VNet/Subnet | Separate subnets | VNet spoke_vnet with 5 separate subnets, mixed actions (UPDATE) |
| DNS Zone/Records | Multiple record types | DNS zone example_com with A, CNAME, MX, TXT records (CREATE) |
| Route Table/Routes | Inline routes | Route table app_routes with 3 inline routes (CREATE) |
| NSG/Rules | Inline rules | NSG app_nsg with 3 inline security rules (CREATE) |

### Validation Points Per Resource Type

**VNet/Subnets:** 7 validation points  
**DNS Zones/Records:** 7 validation points  
**Route Tables/Routes:** 6 validation points  
**NSG/Security Rules:** 10 validation points  
**Cross-Platform:** 5 validation points  

**Total Validation Points:** 35

---

## Success Criteria

From [azure-rm-batch-2-uat-test-plan.md](azure-rm-batch-2-uat-test-plan.md):

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

**GitHub Validation:** ⏸️ Pending maintainer review  
**Azure DevOps Validation:** ❌ Blocked (environment issue)

---

## Next Steps

### Immediate Actions

1. **Maintainer Review Required:**
   - Review GitHub PR #68: https://github.com/oocx/tfplan2md-uat/pull/68
   - Verify all validation points in both feature-specific and regression artifacts
   - Apply label `uat-approved` to PR #68 if validation passes

2. **Decision Required:**
   - **Option A:** Accept GitHub-only UAT validation (recommended - GitHub and Azure DevOps use similar markdown rendering)
   - **Option B:** Fix Azure CLI authentication in copilot environment and retry Azure DevOps UAT

### After Approval

1. **UAT Cleanup:**
   - Close GitHub UAT PR #68
   - Update this report with final validation results

2. **Documentation:**
   - Update UAT test plan with any lessons learned
   - Document environment authentication issue for future UAT runs

3. **Handoff:**
   - Hand off to Release Manager for merge and release preparation

---

## Feedback Opportunities

Questions from [azure-rm-batch-2-uat-test-plan.md](azure-rm-batch-2-uat-test-plan.md):

- Do the table formats make it easier to understand Azure network infrastructure?
- Are the column choices appropriate for each resource type?
- Are there too many columns in the NSG rules table? (9 columns in parent-child framework table)
- Is the icon usage helpful or distracting?
- Are the mixed management warnings clear and actionable?
- Do DNS zones with 20+ records remain readable?
- Should any additional attributes be shown in the tables?
- Are there any rendering issues or layout problems?

---

## Appendix: Artifact Generation Process

### Test Files Used

Individual test plan JSON files from `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`:
- `azurerm-vnet-inline-subnets-plan.json` - VNet with 3 inline subnets
- `azurerm-vnet-mixed-subnets-plan.json` - VNet with mixed inline/separate subnets
- `azurerm-vnet-separate-subnets-plan.json` - VNet with 5 separate subnets (various changes)
- `azurerm-dns-zone-records-plan.json` - DNS zone with A, CNAME, MX, TXT records
- `azurerm-route-table-inline-routes-plan.json` - Route table with 3 inline routes
- `azurerm-nsg-inline-rules-plan.json` - NSG with 3 inline security rules

### Generation Commands

```bash
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-vnet-inline-subnets-plan.json \
  --output /tmp/test-vnet-inline.md --render-target github

# (repeated for each test file)
```

### Composite Artifact Creation

Combined generated outputs into single `artifacts/azure-rm-batch-2-uat.md` with:
- Custom header explaining UAT purpose
- Organized sections for each resource type
- Detailed validation points for each example
- Cross-platform validation checklist
- Success criteria from UAT test plan
- Known issues from code review

---

**Report Generated:** 2025-02-12  
**Last Updated:** 2025-02-12  
**Next Update:** After maintainer approval/feedback
