# Style Guide vs Implementation Comparison Analysis

**Date:** 2026-02-16 (Updated: 2026-02-17)  
**Analyzed Files:**
- Style Guide: `docs/report-style-guide.md` (last updated: 2026-02-16, v1.18.1)
- Artifacts: `artifacts/*.md` (regenerated with v1.18.1)
- Examples: `examples/*/report.md` (regenerated with v1.18.1)
- Templates: `src/Oocx.TfPlan2Md/**/*.sbn`
- Release Notes: Reviewed all releases since style guide last updated (v1.18.0+)

**Update 2026-02-17:** 
- All artifacts regenerated using tfplan2md v1.18.1 (3a2284b) to ensure findings reflect current implementation
- Reviewed release notes from v1.16.0+ for new features not yet documented in style guide
- Identified parent-child resource grouping (v1.16.0, v1.17.0) as major undocumented feature
- Reclassified issues 2.2 and 6.1 from "clarification needed" to "implementation fix required" per maintainer feedback
- The script `scripts/generate-demo-artifacts.sh` was run, plus additional manual regeneration for azapi artifacts from the `TestData/` folder

---

## Executive Summary

This document provides a comprehensive analysis of differences between the Report Style Guide (`docs/report-style-guide.md`) and the current implementation as seen in generated reports and template files. Each difference is categorized as either requiring a style guide update or an implementation fix.

**Key Findings:**
- **16 differences found** across various categories (1 fixed in v1.18.1)
- **8 require style guide updates** (undocumented features)
- **7 require implementation fixes** (violations of style guide) ← increased from 5
- **1 is a discrepancy** requiring clarification/decision ← reduced from 3

**Note:** Analysis initially found 17 differences, but Issue 2.1 (H3 headings in AzAPI template) was fixed in v1.18.1. All artifacts have been regenerated with the latest version to ensure findings are current.

**Update based on maintainer feedback:**
- Issue 2.2 (empty resource names) and 6.1 (tags format) changed from "clarification needed" to "implementation fix required"
- Issue 9.2 expanded to cover parent-child resource grouping (NEW feature from v1.16.0 and v1.17.0)
- Issue 9.3 merged into 9.2 as part of comprehensive parent-child grouping documentation

---

## Category 1: Icons - Missing from Style Guide

### 1.1 ID Icon (🆔) - **STYLE GUIDE NEEDS UPDATE**

**Status:** Used extensively but not documented in style guide

**Current Usage in Implementation:**
- Resource names (e.g., `<code>🆔 rg-tfplan2md-demo</code>`)
- Resource IDs in attribute tables
- Azure AD user principal names
- API Management operation IDs
- Group mail nicknames
- Container names

**Examples from artifacts/comprehensive-demo.md:**
```markdown
<summary>➕ azurerm_resource_group <b><code>core</code></b> — <code>🆔 rg-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
| name | `🆔 rg-tfplan2md-demo` |
| user_principal_name | `🆔 jane.doe@example.com` |
```

**Recommendation:** Add to style guide under "Semantic Value Icons" section with:
```markdown
| Icon | Value Type | Pattern | Example | When to Use |
|------|------------|---------|---------|-------------|
| 🆔 | Resource ID/Name | `🆔 <id>` | `🆔 rg-demo` | Resource identifiers, names, IDs |
```

---

### 1.2 Folder Icon (📁) - **STYLE GUIDE NEEDS UPDATE**

**Status:** Used for resource groups but not documented

**Current Usage in Implementation:**
- Resource group names in summaries
- Resource group references in attribute tables (resource_group_name)

**Examples from artifacts/comprehensive-demo.md:**
```markdown
<summary>➕ azurerm_storage_account <b><code>logs</code></b> — <code>🆔 sttfplan2mdlogs</code> in <code>📁 rg-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
| resource_group_name | `📁 rg-tfplan2md-demo` |
```

**Recommendation:** Add to style guide under "Semantic Value Icons":
```markdown
| Icon | Value Type | Pattern | Example | When to Use |
|------|------------|---------|---------|-------------|
| 📁 | Resource Group | `📁 <rg-name>` | `📁 rg-demo` | Resource group references |
```

---

### 1.3 Key Icon (🔑) - **STYLE GUIDE NEEDS UPDATE**

**Status:** Used for subscriptions but not documented

**Current Usage in Implementation:**
- Subscription names and IDs
- Subscription references in scopes

**Examples from artifacts/comprehensive-demo.md:**
```markdown
<summary>➕ azurerm_subscription <b><code>demo</code></b> — <code>🔑 Production (12345678-1234-1234-1234-123456789012)</code></summary>
| subscription | `🔑 Production` |
| subscription_id | `🔑 12345678-1234-1234-1234-123456789012` |
```

**Recommendation:** Add to style guide under "Semantic Value Icons":
```markdown
| Icon | Value Type | Pattern | Example | When to Use |
|------|------------|---------|---------|-------------|
| 🔑 | Subscription | `🔑 <name> (<id>)` | `🔑 Production (guid)` | Azure subscription references |
```

---

### 1.4 Email Icon (📧) - **STYLE GUIDE NEEDS UPDATE**

**Status:** Used for email addresses but not documented

**Current Usage in Implementation:**
- User email addresses
- Invitation email addresses
- Mail attributes

**Examples from artifacts/comprehensive-demo.md:**
```markdown
<summary>➕ azuread_user <b><code>platform_admin</code></b> — <code>👤 Platform Admin</code> (<code>🆔 platform.admin@contoso.com</code>) <code>📧 platform.admin@contoso.com</code></summary>
| mail | `📧 platform.admin@contoso.com` |
```

**Recommendation:** Add to style guide under "Semantic Value Icons":
```markdown
| Icon | Value Type | Pattern | Example | When to Use |
|------|------------|---------|---------|-------------|
| 📧 | Email Address | `📧 <email>` | `📧 user@domain.com` | Email addresses |
```

---

### 1.5 Office Building Icon (🏢) - **STYLE GUIDE NEEDS UPDATE**

**Status:** Used for Azure AD tenant IDs but not documented

**Current Usage in Implementation:**
- Tenant IDs in Key Vault resources

**Examples from artifacts/comprehensive-demo.md:**
```markdown
| tenant_id | `🏢 Contoso Tenant (11111111-2222-3333-4444-555555555555)` |
```

**Recommendation:** Add to style guide under "Semantic Value Icons":
```markdown
| Icon | Value Type | Pattern | Example | When to Use |
|------|------------|---------|---------|-------------|
| 🏢 | Tenant | `🏢 <name> (<id>)` | `🏢 Contoso (guid)` | Azure AD tenant references |
```

---

### 1.6 Card File Box Icon (🗂️) - **STYLE GUIDE NEEDS UPDATE**

**Status:** Used for management groups but not documented

**Current Usage in Implementation:**
- Management group references in role management policies

**Examples from artifacts/comprehensive-demo.md:**
```markdown
<summary>🔄 azurerm_role_management_policy <b><code>ops</code></b> — <code>🛡️ Reader</code> in <code>🗂️ Tenant Contoso Corp (mg-root) root</code></summary>
```

**Recommendation:** Add to style guide under "Semantic Value Icons":
```markdown
| Icon | Value Type | Pattern | Example | When to Use |
|------|------------|---------|---------|-------------|
| 🗂️ | Management Group | `🗂️ <name> (<id>)` | `🗂️ Corp (mg-root)` | Azure management group references |
```

---

### 1.7 Question Mark Icon (❓) - **STYLE GUIDE NEEDS UPDATE**

**Status:** Used for unknown member types in groups but not documented

**Current Usage in Implementation:**
- Azure AD group member type indicators when type cannot be determined

**Examples from artifacts/azuread-enhancements-demo.md:**
```markdown
<summary>➕ azuread_group <b><code>platform_team</code></b> — <code>👥 Platform Team</code> (<code>🆔 Platform Engineering</code>) Core platform engineering team | <code>2 👤 1 👥 1 💻 1 ❓</code></summary>
```

**Recommendation:** Add to style guide under "Identity & Roles":
```markdown
| Icon | Value Type | Pattern | Example | When to Use |
|------|------------|---------|---------|-------------|
| ❓ | Unknown Type | `❓ Unknown` | `1 ❓` | When member type cannot be determined |
```

---

### 1.8 Lock Icon (🔒) - **STYLE GUIDE NEEDS UPDATE**

**Status:** Used for security findings metadata but not documented

**Current Usage in Implementation:**
- Security & Quality findings summary banner

**Examples from artifacts/comprehensive-demo.md:**
```markdown
🔒 **Security & Quality:** ⚠️ 1 High, ⚠️ 1 Medium
```

**Recommendation:** Add to style guide under "Other Markers":
```markdown
| Icon | Purpose | Pattern | Example | When to Use |
|------|---------|---------|---------|-------------|
| 🔒 | Security Findings | `🔒 **Security & Quality:**` | - | Security findings header |
```

---

## Category 2: AzAPI Resource Template Issues

### 2.1 H3 Headings Inside Details Blocks - **FIXED IN v1.18.1** ✅

**Status:** ~~Violates style guide section "Structure & Hierarchy"~~ **RESOLVED**

**Original Issue (pre-v1.18.1):**
Older versions of the `azapi/resource.sbn` template contained H3 headings INSIDE the `<details>` block, which violated the style guide.

**Current Implementation (v1.18.1+):**
The azapi template now uses H4 (`####`) headings for Body sections, which is correct:

```markdown
<details open style="...">
<summary>➕ azapi_resource <b></b> — <code>🆔 example-vm</code> <code>🌍 eastus</code></summary>
<br>

**Type:** `Microsoft.Compute/virtualMachines@2023-03-01`

#### Body
```

**Verified in artifacts/azapi-mixed-mappings-demo.md (v1.18.1):**
- Line 17: `### 📦 Module: root` (CORRECT - module level)
- Line 32: `#### Body` (CORRECT - H4 inside details)
- Line 51: `#### Body` (CORRECT - H4 inside details)
- Line 73: `#### Body Changes` (CORRECT - H4 inside details)

**No action required** - This issue has been resolved.

---

### 2.2 Empty Resource Names in Summaries - **IMPLEMENTATION FIX REQUIRED**

**Status:** Violates style guide specification

**Style Guide Says:**
> - **Resource Name**: Bold + code-formatted (e.g., `<b><code>hub</code></b>`)

The style guide requires a resource name to be present and formatted.

**Current Implementation:**
AzAPI resources without a mapped friendly name show empty bold/code tags in summaries:

```html
<summary>➕ azapi_resource <b></b> — <code>🆔 example-vm</code> <code>🌍 eastus</code></summary>
```

This violates the style guide because:
1. Empty `<b></b>` tags provide no value
2. The resource name should always be present (either friendly name or Terraform resource name)

**Correct Implementation:**
Resources with mapped names show proper formatting:
```html
<summary>➕ azapi_resource <b><code>container_app</code></b> — <code>🆔 ca-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
```

**Examples:**
- artifacts/azapi-mixed-mappings-demo.md: All 4 resources show `<b></b>` (incorrect)
- artifacts/comprehensive-demo.md line 201: `<b><code>container_app</code></b>` (correct, has mapping)

**Recommendation:** Fix implementation to use Terraform resource name as fallback when no friendly name is available. For example, `azapi_resource.vm` should display as `<b><code>vm</code></b>` instead of `<b></b>`.

---

## Category 3: Update Summary Format

### 3.1 Space After Wrench Icon - **IMPLEMENTATION CORRECT** ✅

**Status:** Implementation follows style guide correctly

**Style Guide Says:**
> - `<count> 🔧 <attributes>` (non-breaking space between wrench icon and attributes)

**Current Implementation:**
Binary analysis shows non-breaking space (U+00A0, bytes `c2 a0`) is correctly used:

```
🔧 (F0 9F 94 A7) + NON-BREAKING SPACE (C2 A0) + attribute text
```

**Examples verified in artifacts/comprehensive-demo.md:**
- Line 74: `2🔧 account_replication_type, tags.cost_center`
- Line 319: `5🔧 secret_variable[0].value, variable[0].value, ...`
- Line 412: `1🔧 address_space[1]`

All show correct non-breaking space after 🔧 icon.

**No action required** - Implementation is correct.

---

### 3.2 Missing Space Between Count and Wrench - **IMPLEMENTATION FIX REQUIRED**

**Status:** Violates style guide format specification

**Style Guide Says:**
> - `<count> 🔧 <attributes>` (shows space between count and wrench)

**Current Implementation:**
The count and wrench icon are directly adjacent with no space:
```
2🔧 account_replication_type, tags.cost_center
```

Should be:
```
2 🔧 account_replication_type, tags.cost_center
```

**Examples from artifacts/comprehensive-demo.md:**
- Line 74: `2🔧` instead of `2 🔧`
- Line 319: `5🔧` instead of `5 🔧`
- Line 412: `1🔧` instead of `1 🔧`
- Line 422: `6🔧` instead of `6 🔧`

**Recommendation:** Add space (preferably non-breaking space U+00A0) between count and wrench icon in summary generation code.

---

## Category 4: Module Header Format

### 4.1 Non-Breaking Space After Module Icon - **IMPLEMENTATION CORRECT** ✅

**Status:** Implementation follows style guide correctly

**Style Guide Says:**
> Note: The space between 📦 and "Module:" is a non-breaking space (U+00A0).

**Current Implementation:**
Binary analysis confirms non-breaking space is used:
```
📦 (F0 9F 93 A6) + NON-BREAKING SPACE (C2 A0) + "Module:"
```

**Verified in artifacts/comprehensive-demo.md:**
- Line 31: `### 📦 Module: root`
- Line 340: `### 📦 Module: `module.network``

Hex analysis shows: `F0 9F 93 A6 C2 A0 4D 6F 64 75 6C 65`

**No action required** - Implementation is correct.

---

## Category 5: Action Icons in Summary Table

### 5.1 Non-Breaking Spaces After Action Icons - **IMPLEMENTATION CORRECT** ✅

**Status:** Implementation follows style guide correctly

**Style Guide Says:**
> All icons followed by text labels must use a non-breaking space (U+00A0, `\u00A0`) between the icon and the label.

**Current Implementation:**
Binary analysis shows correct non-breaking spaces after all action icons:

- ➕ Add: `E2 9E 95 C2 A0` ✅
- 🔄 Change: `F0 9F 94 84 C2 A0` ✅
- ❌ Delete: `E2 9D 8C C2 A0` ✅
- ♻️ Replace: `E2 99 BB EF B8 8F C2 A0` ✅ (includes variation selector)

**Examples verified in artifacts/comprehensive-demo.md:**
- Line 9: `| ➕ Add | 26 |` (correct non-breaking space)
- Line 10: `| 🔄 Change | 8 |` (correct non-breaking space)
- Line 11: `| ♻️ Replace | 2 |` (correct with variation selector + non-breaking space)
- Line 12: `| ❌ Destroy | 3 |` (correct non-breaking space)

**No action required** - Implementation is correct.

---

## Category 6: Tags Display

### 6.1 Tags Format in AzAPI Resources - **IMPLEMENTATION FIX REQUIRED**

**Status:** Violates style guide specification

**Style Guide Says:**
```markdown
**🏷️ Tags:** `environment: production` `owner: devops` `cost_center: 1234`
```

The style guide specifies a standard format with 🏷️ icon and inline tag badges.

**Current Implementation for AzureRM:**
Matches style guide correctly:
```markdown
**🏷️ Tags:** `environment: demo` `owner: tfplan2md`
```

**Current Implementation for AzAPI:**
Uses non-standard format without 🏷️ icon and different layout:
```markdown
**Tags:**
 `environment: demo`
```

**Examples:**
- artifacts/comprehensive-demo.md line 42: AzureRM format (correct)
- artifacts/comprehensive-demo.md line 189-190: AzAPI format (incorrect - missing 🏷️, newline instead of inline)
- artifacts/azure-rm-parent-child-demo.md: AzureRM format (correct)

**Recommendation:** Update azapi/resource.sbn template to match the standard tags format specified in the style guide. The format should be consistent across all providers.

---

## Category 7: Code Analysis Integration

### 7.1 Security Findings Banner - **STYLE GUIDE NEEDS UPDATE**

**Status:** Feature exists but not documented in style guide

**Current Implementation:**
Resources with security findings show a banner with 🔒 icon:

```markdown
🔒 **Security & Quality:** ⚠️ 1 High, ⚠️ 1 Medium
```

**Examples from artifacts/comprehensive-demo.md:**
- Line 50: Storage account with findings
- Line 346: Virtual network with findings
- Line 542: Key vault with findings

**Recommendation:** Add section to style guide documenting this feature:

```markdown
### Code Analysis Metadata

Resources with security or quality findings display a banner after the summary:

**Format:** `🔒 **Security & Quality:** <severity_counts>`

**Example:**
```markdown
<details open>
<summary>➕ azurerm_storage_account ...</summary>
<br>

🔒 **Security & Quality:** ⚠️ 1 High, ⚠️ 1 Medium
```

---

### 7.2 Attribute Finding Indicators - **STYLE GUIDE NEEDS UPDATE**

**Status:** Feature exists but not documented in style guide

**Current Implementation:**
Attributes with security findings show a warning triangle indicator:

```markdown
| min_tls_version ⚠️ | `TLS1_2` |
```

**Examples from artifacts/comprehensive-demo.md:**
- Line 58: `min_tls_version ⚠️`

**Recommendation:** Add to style guide:

```markdown
### Attribute Finding Indicators

Attributes flagged by code analysis tools show a ⚠️ indicator next to the attribute name in tables:

**Format:** `| <attribute_name> ⚠️ | <value> |`
```

---

## Category 8: Refactoring Operations

### 8.1 Import/Move Operation Display - **STYLE GUIDE NEEDS UPDATE**

**Status:** Feature exists but not fully documented

**Current Implementation:**
Resources with import or move operations show special formatting in summaries and a refactoring summary table:

**Import Summary:**
```markdown
<summary>➕ azurerm_resource_group <b><code>imported</code></b> — 📥 *Imported* | <code>🆔 rg-imported-existing</code> <code>🌍 eastus</code></summary>
```

**Move Summary:**
```markdown
<summary>  azurerm_virtual_network <b><code>migrated</code></b> — 🔀 *Moved from* <code>module.legacy.azurerm_virtual_network.main</code> (⚠️ *already moved*) | ...</summary>
```

**Refactoring Summary Table:**
```markdown
## Refactoring Summary

| Operation | Resource | Details | Status |
| --------- | -------- | ------- | ------ |
| 📥 Import | azurerm_resource_group `rg-imported-existing` | ID: `📁 rg-imported-existing` in subscription `🔑 12345678-...` | ✅ Ready |
| 🔀 Move | azurerm_virtual_network `vnet-legacy` | From: `module.legacy.azurerm_virtual_network.main` | ⚠️ Already moved |
```

**Style Guide Coverage:**
- 📥 Import icon is NOT documented
- 🔀 Move icon is NOT documented
- "Refactoring Summary" section format is NOT documented
- Move operation summary format is NOT documented
- Import operation summary format is partially documented

**Recommendation:** Add comprehensive refactoring section to style guide covering:
1. Import and move icons (📥, 🔀)
2. Resource summary format for imported/moved resources
3. Refactoring Summary table format and columns
4. Status indicators (✅ Ready, ⚠️ Already moved/imported)

---

## Category 9: Resource-Specific Summaries

### 9.1 AzureDevOps Variable Group Format - **STYLE GUIDE NEEDS UPDATE**

**Status:** Resource-specific format not documented

**Current Implementation:**
Variable groups show custom summary format with variable group name:

```markdown
<summary>🔄 azuredevops_variable_group <b><code>pipeline_vars</code></b> — <code>🆔 deploy-pipeline-vars</code> | 5🔧 secret_variable[0].value, variable[0].value, variable[1].value, +2 more</summary>
```

Content includes:
```markdown
**Variable Group:** <code>deploy-pipeline-vars</code>

**Description:** <code>Pipeline variables for deployment</code>

#### Variables

| Change | Name | Value | Enabled | Content Type | Expires |
```

**Recommendation:** Document AzureDevOps variable group template in style guide.

---

### 9.2 Parent-Child Resource Grouping - **STYLE GUIDE NEEDS UPDATE**

**Status:** NEW FEATURE (v1.16.0, v1.17.0) not documented in style guide

**Feature Background:**
Parent-child resource grouping was introduced in two releases:
- **v1.16.0 (Feature 068)**: Azure AD groups/members, Azure DevOps groups/teams
- **v1.17.0 (Feature 072)**: Azure RM resources (VNets/subnets, DNS zones/records, Route tables/routes, NSGs/rules)

**Current Implementation:**
Resources with parent-child relationships show inline tables instead of separate collapsible sections:

**Virtual Networks with Subnets:**
```markdown
<summary>➕ azurerm_virtual_network <b><code>spoke</code></b> — ... | ➕ 1 subnets | ♻️ 1 subnets</summary>
```

Content includes subnet table:
```markdown
#### Subnets

| Change | Name | Address Prefixes | NSG | Delegation | Terraform Resource |
```

**Azure AD Groups with Members:**
```markdown
<summary>➕ azuread_group <b><code>platform_engineers</code></b> — <code>👥 Platform Engineers</code> ... | <code>3 👤 1 👥 1 💻</code> | ➕ 5 members</summary>
```

Content includes members table and mixed management warning:
```markdown
#### Members

⚠️ **Warning:** This resource has children managed both inline
and as separate resources. This configuration will cause conflicts.

| Change | Member | Terraform Resource |
```

**Supported Patterns:**
- Azure AD: Groups + members
- Azure DevOps: Groups/teams + members
- Azure RM: VNets + subnets, DNS zones + records, Route tables + routes, NSGs + rules

**Features:**
- Inline child resource tables within parent section
- Aggregate counts in parent summaries (e.g., "➕ 2 members")
- Mixed management detection warnings
- Character-level diff highlighting for inline changes
- Conditional "Terraform Resource" column (only when mixed management)

**Recommendation:** Add comprehensive section to style guide documenting:
1. Parent-child resource grouping pattern
2. Inline table format and structure
3. Summary line format with child counts
4. Mixed management warning format
5. Supported resource types
6. Examples for each pattern (Azure AD groups, VNets/subnets, DNS zones/records, etc.)

**Note:** This section supersedes and merges the former section 9.3 (Azure AD groups with members), which is now covered as part of the comprehensive parent-child grouping pattern.

---

## Category 10: API Management Resources

### 10.1 APIM Operation Summary Format - **STYLE GUIDE NEEDS UPDATE**

**Status:** Resource-specific format not documented

**Current Implementation:**
API operations show display name and operation hierarchy:

```markdown
<summary>➕ azurerm_api_management_api_operation <b><code>get_profile</code></b> <code>Get Profile</code> — <code>users</code>/<code>get-profile</code> @ <code>apim-demo</code> in <code>📁 rg-tfplan2md-demo</code></summary>
```

Format: `{display_name}` — `{api_name}`/`{operation_id}` @ `{apim_name}` in `{resource_group}`

**Recommendation:** Document APIM resource summary formats.

---

## Category 11: Role Assignment Summaries

### 11.1 Role Assignment Arrow Format - **IMPLEMENTATION CORRECT** ✅

**Status:** Implementation matches style guide

**Style Guide Example:**
```html
<summary>➕ azurerm_role_assignment <b><code>rg_reader</code></b> — <code>👤 Jane Doe (User)</code> → <code>🛡️ Reader</code> on <code>rg-demo</code></summary>
```

**Current Implementation:**
```html
<summary>➕ azurerm_role_assignment <b><code>rg_reader</code></b> — <code>👤 Jane Doe</code> → <code>🛡️ Reader</code> on <code>📁 rg-tfplan2md-demo</code></summary>
```

**Difference:** Implementation uses 📁 icon for resource group (correct), style guide example doesn't.

**No action required** - Implementation is correct, style guide example could be updated to show 📁 icon.

---

## Summary Table

| # | Issue | Category | Action Required | Priority |
|---|-------|----------|-----------------|----------|
| 1.1 | ID Icon (🆔) missing | Style Guide Update | Add to semantic icons | High |
| 1.2 | Folder Icon (📁) missing | Style Guide Update | Add to semantic icons | High |
| 1.3 | Key Icon (🔑) missing | Style Guide Update | Add to semantic icons | High |
| 1.4 | Email Icon (📧) missing | Style Guide Update | Add to semantic icons | Medium |
| 1.5 | Office Building Icon (🏢) missing | Style Guide Update | Add to semantic icons | Low |
| 1.6 | Card File Box Icon (🗂️) missing | Style Guide Update | Add to semantic icons | Low |
| 1.7 | Question Mark Icon (❓) missing | Style Guide Update | Add to identity icons | Low |
| 1.8 | Lock Icon (🔒) missing | Style Guide Update | Add to other markers | Medium |
| 2.1 | ~~H3 headings in azapi template~~ | ✅ Fixed in v1.18.1 | ~~Remove H3 from azapi/resource.sbn~~ | ~~High~~ |
| 2.2 | Empty azapi resource names | Implementation Fix | Use Terraform resource name as fallback | High |
| 3.2 | Missing space before wrench | Implementation Fix | Add space in summary code | Medium |
| 6.1 | AzAPI tags format different | Implementation Fix | Match standard tags format | Medium |
| 7.1 | Security findings banner | Style Guide Update | Document feature | Medium |
| 7.2 | Attribute finding indicators | Style Guide Update | Document feature | Medium |
| 8.1 | Import/Move operations | Style Guide Update | Add comprehensive section | High |
| 9.1 | AzureDevOps variable groups | Style Guide Update | Document template | Low |
| 9.2 | Parent-child grouping (NEW v1.16-17) | Style Guide Update | Document comprehensive patterns | High |
| 9.3 | ~~Azure AD groups with members~~ | ~~Merged into 9.2~~ | ~~Covered by parent-child grouping~~ | ~~Low~~ |
| 10.1 | APIM operation summaries | Style Guide Update | Document format | Low |

---

## Recommendations

### Immediate Actions (High Priority)

1. **Update Style Guide** - Add missing core icons (🆔, 📁, 🔑) that are used extensively
2. **Fix Empty AzAPI Resource Names** - Use Terraform resource name as fallback instead of empty `<b></b>` tags
3. **Document Parent-Child Resource Grouping** - Add comprehensive section covering the new feature from v1.16.0 and v1.17.0
4. **Add Refactoring Section** - Document import/move operation display format
5. **Expand generate-demo-artifacts.sh** - Add generation for all artifacts currently tracked in git (azapi demos, azuread demos, etc.) to ensure they stay current with each release

### Medium Priority Actions

1. **Fix Wrench Spacing** - Add space between count and wrench icon (` 🔧` not `🔧`)
2. **Fix AzAPI Tags Format** - Update azapi/resource.sbn to match standard tags format with 🏷️ icon
3. Document security findings integration features
4. Add email icon (📧) to style guide

### Low Priority Actions

1. Document remaining resource-specific templates (AzureDevOps variable groups, APIM, etc.)
2. Add remaining icons (🏢, 🗂️, ❓, 🔒)
3. Update style guide examples to use 📁 icon consistently

---

## Conclusion

The implementation is generally consistent with the style guide, with most differences being **undocumented features** rather than violations. However, based on maintainer feedback, several issues previously marked as "clarification needed" are actually **implementation bugs** that need fixing.

### Style Guide Needs Expansion

The style guide needs significant expansion to cover:

1. Additional semantic icons being used (8 icons missing)
2. Security/quality findings integration
3. Refactoring operations (import/move)
4. **Parent-child resource grouping** (NEW major feature from v1.16.0 and v1.17.0)
5. Resource-specific templates and formats

### Implementation Issues Requiring Fixes

The main implementation issues are:

1. ~~AzAPI template using H3 headings~~ ✅ **FIXED in v1.18.1**
2. **Empty resource names in AzAPI summaries** - Should use Terraform resource name as fallback
3. **AzAPI tags format** - Should match standard format with 🏷️ icon
4. Missing space between count and wrench icon
5. Potential inconsistency in value formatting

### Process Improvement

The `generate-demo-artifacts.sh` script should be expanded to include all tracked artifacts (azapi demos, azuread demos, etc.) to prevent version drift in future releases.

All verified differences are documented above with specific examples, file locations, and recommendations for resolution.
