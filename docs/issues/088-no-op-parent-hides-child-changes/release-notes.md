# No-op parent resources with child changes now visible in reports

This patch fixes a critical bug where child resource changes (like network security rules or subnet updates) would disappear from the Resource Changes section when their parent resource had no direct changes.

## 🐛 Bug fixes

### Fixed missing child resources in Resource Changes section

**Problem:** When a Terraform plan contained a parent resource with no changes (no-op action) but had child resources with actual changes, the entire Resource Changes section would be omitted from the report. This affected parent-child relationships like:

- `azurerm_network_security_group` → `azurerm_network_security_rule`
- `azurerm_virtual_network` → `azurerm_subnet`
- `azurerm_route_table` → `azurerm_route`
- `azurerm_dns_zone` → `azurerm_dns_*_record`
- `azuread_group` → `azuread_group_member`
- `azuredevops_team` → `azuredevops_team_members`

**Symptom:** The Summary table correctly counted child changes (e.g., "🔄 Change | 2 | 2 azurerm_network_security_rule"), but the Resource Changes section was completely missing from the output.

**Root cause:** The no-op filtering logic removed parent resources with no direct changes from the report, even when those parents contained child resources with changes. Since children are displayed inside their parent's section, removing the parent caused the children to disappear as well.

**Fix:** Modified the display filter in `ReportModelBuilder.Build.cs` to preserve no-op parent resources that have children with changes (`ChildResourceGroups.Count > 0`). Now the complete parent-child hierarchy is visible in reports.

### Impact on reports

- **Before:** Child changes counted in Summary but missing from Resource Changes
- **After:** Parent resource appears in Resource Changes with child changes shown in inline tables

### Affected resource types

This fix impacts all provider resources configured with parent-child relationships:

**Azure (azurerm):**
- Network security groups and rules
- Virtual networks and subnets
- Route tables and routes
- DNS zones and records

**Azure Active Directory (azuread):**
- Groups and group members

**Azure DevOps (azuredevops):**
- Teams and team members

## 🔗 Commits

- [`917a5cd`](https://github.com/oocx/tfplan2md/commit/917a5cd) fix: preserve no-op parents with child changes in Resource Changes section
- [`86c71e3`](https://github.com/oocx/tfplan2md/commit/86c71e3) docs: add issue analysis for no-op parent hiding child changes

## 🧪 Test coverage

Added 3 new test cases to verify the fix:

1. **No-op parent with child updates** - Verifies parent appears when children have changes
2. **No-op parent without children** - Verifies parent is still filtered when no children exist
3. **No-op parent with no-op children** - Verifies parent is filtered when children also have no changes

All 1088 tests passing.
