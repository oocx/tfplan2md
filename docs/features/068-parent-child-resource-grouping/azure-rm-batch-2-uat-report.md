# UAT Report: Azure RM Parent-Child Resource Grouping (Batch 2)

**Feature:** Parent-Child Resource Grouping - Azure RM Batch 2 Extension  
**Test Date:** 2026-02-13  
**Tester:** UAT Tester Agent  
**Status:** ⏸️ **PENDING APPROVAL** (GitHub + Azure DevOps)

---

## Executive Summary

UAT was initiated for Azure RM Batch 2, which extends parent-child resource grouping to 4 additional Azure RM resource types:
- `azurerm_virtual_network` / `azurerm_subnet`
- `azurerm_dns_zone` / DNS records (9+ types)
- `azurerm_route_table` / `azurerm_route`
- `azurerm_network_security_group` / `azurerm_network_security_rule`

**GitHub UAT:** ✅ PR created successfully with both feature-specific and regression artifacts  
**Azure DevOps UAT:** ✅ PR created successfully with both feature-specific and regression artifacts

---

## UAT Artifacts

### Feature-Specific Test Plan (Added 2026-02-13)

**Purpose:** Demonstrate specific features added in this PR with comprehensive edge case coverage (not just regression)

**File:** `artifacts/azure-rm-batch-2-feature-test.md`
- **286 lines** of real tfplan2md output
- **48 resource changes** across 4 resource types
- **NSG with 11-column Feature 016 structure** (Source Addresses, Source Ports, Destination Addresses, Destination Ports, Description columns)
- **DNS with merged single table** (10 records of 7 types: A, AAAA, CNAME, MX, TXT, CAA, NS)
- **Complete edge case coverage** (mixed management, known-after-apply, wildcards, service tags, port ranges, multiple values)

**Posted to:**
- GitHub PR #72: https://github.com/oocx/tfplan2md-uat/pull/72#issuecomment-3895034841
- Azure DevOps PR #74: https://dev.azure.com/oocx/test/_git/test/pullrequest/74

**What This Tests:**
- VNet/Subnets: Inline, separate, mixed, known-after-apply scenarios
- DNS Zones: 10 records merged into single table (not separate tables)
- Route Tables: Inline and separate routes with all next hop types
- NSG/Security Rules: 11-column Feature 016 structure with all edge cases (service tags, wildcards, port ranges, multiple addresses)

### Original Feature-Specific Artifact

**File:** `artifacts/azure-rm-batch-2-uat.md`

**⚠️ IMPORTANT:** This artifact is now **REAL tfplan2md output** generated from the comprehensive demo plan, not manually created markdown.

**Generation:**
```bash
dotnet run --project src/Oocx.TfPlan2Md --configuration Release -- \
  examples/comprehensive-demo/plan.json \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --report-title "Terraform Plan Report - Azure RM Batch 2 UAT" \
  --output artifacts/azure-rm-batch-2-uat.md \
  --render-target azuredevops
```

**Content:**
- **Real tfplan2md output** from comprehensive demo (36 total resource changes)
- 5 VNet resources demonstrating inline and separate subnet management
- 1 NSG resource demonstrating inline security rules with 11-column table
- 1 Private DNS A record demonstrating DNS record rendering
- Comprehensive regression test coverage with Azure AD and Azure DevOps resources

**Key Features Demonstrated:**
- VNet `hub` with inline subnets
- VNet `spoke` with mixed management (inline + separate subnets, showing ➕ 1 subnets | ♻️ 1 subnets)
- VNet `branch` with attribute changes
- NSG `app` with 11-column security rules table (Replace action with ➕ 3 security rules | ❌ 1 security rules)
- Full icon usage: 🆔 (names), 🌐 (IPs), ⬇️ (inbound), ⬆️ (outbound), ✅ (allow), ⛔ (deny), 🔗 (protocols), 🔌 (ports), ✳️ (wildcards)

### Regression Artifact

**GitHub:** `artifacts/comprehensive-demo-simple-diff.md`  
**Azure DevOps:** `artifacts/comprehensive-demo.md`

**Purpose:** Ensure no unintended side effects in existing parent-child patterns (Azure AD groups, Azure DevOps teams/groups) or other resource rendering.

---

## UAT Execution

### GitHub UAT

**PR:** #72  
**URL:** https://github.com/oocx/tfplan2md-uat/pull/72  
**Status:** ✅ **CREATED**

**Comments Posted:**
1. **🎯 Feature Test** - Real tfplan2md output from comprehensive demo (30KB, 628 lines)
2. **🔄 Regression Test** - comprehensive-demo-simple-diff.md for side-effects validation
3. **🔧 Bug Fix Notice** - Terraform Resource column bug fix comment

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

### Azure DevOps UAT (Created 2026-02-13, Updated 2026-02-13)

**PR:** #74  
**URL:** https://dev.azure.com/oocx/test/_git/test/pullrequest/74  
**Status:** ✅ **ARTIFACTS POSTED** (Awaiting Approval)

**Comments Posted (2026-02-13 10:00 UTC):**
1. **🎯 Feature Test** - `azure-rm-batch-2-feature-test.md` with 48 resource changes across 4 Azure RM resource types
   - Posted via `scripts/uat-azdo.sh comment 74 /tmp/feature-comment.md`
   - 327 lines of real tfplan2md output
2. **🔄 Regression Test** - `comprehensive-demo.md` with 36 resources for comprehensive validation
   - Posted via `scripts/uat-azdo.sh comment 74 /tmp/regression-comment.md`
   - Full comprehensive demo for side-effects validation

**Platform-Specific Validation:**
- Markdown rendering on Azure DevOps
- Table formatting differences compared to GitHub
- Icon display (🆔, 🌐, 🛡️, ⬇️, ⬆️, ✅, ⛔, 🔗, 🔌, ✳️)
- Link behavior in Azure DevOps UI
- Code block syntax highlighting differences
- Azure DevOps-specific markdown features

**Fix Applied:** Terraform Resource column now visible in all child resource tables (bug fixed before PR creation).

**Credential Configuration:**
The Azure DevOps UAT was successfully created after configuring a PAT-based credential helper for the git submodule. The `AZDO_UAT_TOKEN` (configured as `AZURE_DEVOPS_EXT_PAT`) is used for authentication.

---

## Test Coverage

### Scenarios Covered in Feature-Specific Artifact (Real tfplan2md Output)

| Resource Type | Scenario | Test Case |
|--------------|----------|-----------|
| VNet/Subnet | No subnets | VNet hub (CREATE) - baseline without children |
| VNet/Subnet | Mixed management | VNet spoke with inline + separate subnets (CREATE, showing "➕ 1 subnets \| ♻️ 1 subnets") |
| VNet/Subnet | Attribute change | VNet branch with address_space change (UPDATE) |
| VNet/Subnet | Moved resource | VNet migrated (moved from module.legacy) |
| VNet/Subnet | Destroy | VNet decom (DESTROY) |
| DNS/Records | Private DNS A record | Private DNS A record for api.contoso.local (CREATE) |
| NSG/Rules | Inline rules | NSG app with 3 inline rules and Replace action (shows "➕ 3 security rules \| ❌ 1 security rules") |

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
**Azure DevOps Validation:** ⏸️ Pending maintainer review

---

## Next Steps

### Immediate Actions

1. **Maintainer Review Required:**
   - Review GitHub PR #72: https://github.com/oocx/tfplan2md-uat/pull/72
   - Review Azure DevOps PR #74: https://dev.azure.com/oocx/test/_git/test/pullrequest/74
   - Verify all validation points in both feature-specific and regression artifacts
   - **Note:** Artifacts are REAL tfplan2md output (not manually created) with Terraform Resource column bug fix applied
   - Apply label `uat-approved` to GitHub PR #72 if validation passes
   - Apply "Approve" vote to Azure DevOps PR #74 if validation passes

### After Approval

1. **UAT Cleanup:**
   - Run `scripts/uat-run.sh --cleanup-last` to close GitHub UAT PR #72
   - Update this report with final validation results

2. **Documentation:**
   - Update UAT test plan with lessons learned (real output > manually created fragments)
   - Document Git credential configuration issue for future UAT runs

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

### Source Plan

**File:** `examples/comprehensive-demo/plan.json`  
**Purpose:** Comprehensive demo plan with 36 resource changes covering Azure RM, Azure AD, and Azure DevOps resources

### Generation Commands

**Feature-Specific Artifact (Azure DevOps rendering):**
```bash
dotnet build src/tfplan2md.slnx --configuration Release
dotnet run --project src/Oocx.TfPlan2Md --configuration Release --no-build -- \
  examples/comprehensive-demo/plan.json \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --report-title "Terraform Plan Report - Azure RM Batch 2 UAT" \
  --output artifacts/azure-rm-batch-2-uat.md \
  --render-target azuredevops
```

**GitHub Version (for reference):**
```bash
dotnet run --project src/Oocx.TfPlan2Md --configuration Release --no-build -- \
  examples/comprehensive-demo/plan.json \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --report-title "Terraform Plan Report - Azure RM Batch 2 UAT" \
  --output artifacts/azure-rm-batch-2-uat-github.md \
  --render-target github
```

### Key Insight

**Previous approach (Feature 068 initial UAT):** Manually created markdown fragments with "Validation Points" sections  
**Current approach (Azure RM Batch 2 UAT):** Real tfplan2md output from comprehensive demo plan  

**Advantages of real output:**
1. ✅ Validates actual rendering code paths (not just manually created examples)
2. ✅ Catches rendering bugs that wouldn't appear in handwritten markdown
3. ✅ Provides realistic "does this work?" validation
4. ✅ Comprehensive regression testing (36 resources vs 6 synthetic examples)
5. ✅ Maintainer sees exactly what users will see in production

**Lesson learned:** Always use real tfplan2md output for UAT artifacts, not manually created fragments.

---

**Report Generated:** 2026-02-13  
**Last Updated:** 2026-02-13 10:00 UTC (Artifacts posted to Azure DevOps PR #74)  
**Next Update:** After maintainer approval/feedback
