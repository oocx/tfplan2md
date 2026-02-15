# Presentation Drafts for Static Code Analysis Integration

This document shows different visual approaches for displaying security and quality findings in the markdown report. Each variant is shown with realistic data to evaluate readability, aesthetics, and information density.

---

## Variant A: Inline Annotations

Findings are displayed directly next to the affected attributes, using emoji indicators and inline badges.

### Example: Resource with Multiple Findings

## Resource Changes

### 🔄 azurerm_storage_account.example (update)

**Type:** `azurerm_storage_account`  
**Action:** Update in-place

#### Changes

| Attribute | Before | After |
|-----------|--------|-------|
| `enable_https_traffic_only` | `false` | `true` |
| `min_tls_version` | `TLS1_0` 🚨 **Critical:** TLS 1.0 is deprecated and insecure [CKV_AZURE_44](https://docs.checkov.io/CKV_AZURE_44) | `TLS1_2` |
| `allow_blob_public_access` | `(known after apply)` | `true` ⚠️ **High:** Public blob access should be disabled [CKV_AZURE_59](https://docs.checkov.io/CKV_AZURE_59) |
| `network_rules.0.default_action` | `Allow` | `Deny` |
| `network_rules.0.ip_rules` | `[]` | `["10.0.0.0/16"]` |
| `tags.environment` ℹ️ **Low:** Missing required tag 'cost-center' [TFLint:001] | `dev` | `production` |

---

### ➕ azurerm_network_security_group **`web`** — `🆔 web-nsg` in `📁 rg-web` `🌍 eastus`

#### Attributes

| Attribute | Value |
|-----------|-------|
| `name` | `🆔 web-nsg` |
| `location` | `🌍 eastus` |
| `resource_group_name` | `📁 rg-web` |
| `security_rule.0.name` | `"allow-https"` |
| `security_rule.0.source_address_prefix` | `"*"` 🚨 **Critical:** Unrestricted inbound access from any source [CKV_AZURE_10](https://docs.checkov.io/CKV_AZURE_10) |
| `security_rule.0.destination_port_range` | `"443"` |
| `security_rule.0.protocol` | `"Tcp"` |
| `security_rule.0.access` | `"Allow"` |
| `security_rule.0.priority` ⚠️ **Medium:** Priority 100 may conflict with default rules [Trivy:AVD-AZU-0047] | `100` |
| `security_rule.0.direction` | `"Inbound"` |

**Pros:**
- Immediate visual connection between finding and attribute
- Compact representation
- Easy to scan for critical issues (red flags jump out)

**Cons:**
- Can make attribute table very wide
- May be cluttered with many findings
- Remediation links interrupt the flow

---

## Variant B: Separate Findings Section

Findings are displayed in a dedicated section below the resource, grouped in a table format.

### Example: Resource with Multiple Findings

## Resource Changes

### 🔄 azurerm_storage_account **`example`** — `🆔 stexamplestorage` in `📁 rg-example` `🌍 eastus` | 3🔧 min_tls_version, allow_blob_public_access, tags.environment

#### Changes

| Attribute | Before | After |
|-----------|--------|-------|
| `enable_https_traffic_only` | `false` | `true` |
| `min_tls_version` | `TLS1_0` | `TLS1_2` |
| `allow_blob_public_access` | `(known after apply)` | `true` |
| `network_rules.0.default_action` | `Allow` | `Deny` |
| `network_rules.0.ip_rules` | `[]` | `["10.0.0.0/16"]` |
| `tags.environment` | `dev` | `production` |

#### 🔒 Security & Quality Findings

| Severity | Attribute | Finding | Remediation |
|----------|-----------|---------|-------------|
| 🚨 Critical | `min_tls_version` | TLS 1.0 is deprecated and insecure. Use TLS 1.2 or higher. | [CKV_AZURE_44](https://docs.checkov.io/CKV_AZURE_44) |
| ⚠️ High | `allow_blob_public_access` | Public blob access exposes data to the internet. Consider disabling unless explicitly required. | [CKV_AZURE_59](https://docs.checkov.io/CKV_AZURE_59) |
| ℹ️ Low | `tags.environment` | Missing required tag 'cost-center' for resource governance. | [TFLint:001](https://github.com/terraform-linters/tflint) |

---

### ➕ azurerm_network_security_group **`web`** — `🆔 web-nsg` in `📁 rg-web` `🌍 eastus`

#### Attributes

| Attribute | Value |
|-----------|-------|
| `name` | `🆔 web-nsg` |
| `location` | `🌍 eastus` |
| `resource_group_name` | `📁 rg-web` |
| `security_rule.0.name` | `"allow-https"` |
| `security_rule.0.source_address_prefix` | `"*"` |
| `security_rule.0.destination_port_range` | `"443"` |
| `security_rule.0.protocol` | `"Tcp"` |
| `security_rule.0.access` | `"Allow"` |
| `security_rule.0.priority` | `100` |
| `security_rule.0.direction` | `"Inbound"` |

#### 🔒 Security & Quality Findings

| Severity | Attribute | Finding | Remediation |
|----------|-----------|---------|-------------|
| 🚨 Critical | `security_rule.0.source_address_prefix` | Unrestricted inbound access from any source (0.0.0.0/0). Limit to specific IP ranges or VNets. | [CKV_AZURE_10](https://docs.checkov.io/CKV_AZURE_10) |
| ⚠️ Medium | `security_rule.0.priority` | Priority 100 may conflict with Azure default security rules. Consider using 1000+ for custom rules. | [Trivy:AVD-AZU-0047](https://avd.aquasec.com/azure/AVD-AZU-0047) |

**Pros:**
- Clean separation between changes and findings
- More detailed finding descriptions possible
- Table format easy to scan
- Doesn't clutter attribute values

**Cons:**
- User must mentally map findings back to attributes
- Slightly more scrolling required
- Less immediate visual impact

---

## Variant C: Hybrid with Severity Indicators

Combines inline severity indicators with a detailed findings section, providing both quick scanning and detailed context.

### Example: Resource with Multiple Findings

## Resource Changes

### 🔄 azurerm_storage_account **`example`** — `🆔 stexamplestorage` in `📁 rg-example` `🌍 eastus` | 3🔧 min_tls_version, allow_blob_public_access, tags.environment  
🔒 **Security & Quality:** 🚨 1 Critical, ⚠️ 1 High, ℹ️ 1 Low

#### Changes

| Attribute | Before | After |
|-----------|--------|-------|
| `enable_https_traffic_only` | `false` | `true` |
| `min_tls_version` 🚨 | `TLS1_0` | `TLS1_2` |
| `allow_blob_public_access` ⚠️ | `(known after apply)` | `true` |
| `network_rules.0.default_action` | `Allow` | `Deny` |
| `network_rules.0.ip_rules` | `[]` | `["10.0.0.0/16"]` |
| `tags.environment` ℹ️ | `dev` | `production` |

#### 🔒 Security & Quality Findings

| Severity | Attribute | Finding | Remediation |
|----------|-----------|---------|-------------|
| 🚨 Critical | `min_tls_version` | TLS 1.0 is deprecated and insecure. Use TLS 1.2 or higher. | [CKV_AZURE_44](https://docs.checkov.io/CKV_AZURE_44) |
| ⚠️ High | `allow_blob_public_access` | Public blob access exposes data to the internet. Consider disabling unless explicitly required. | [CKV_AZURE_59](https://docs.checkov.io/CKV_AZURE_59) |
| ℹ️ Low | `tags.environment` | Missing required tag 'cost-center' for resource governance. | [TFLint:001](https://github.com/terraform-linters/tflint) |

---

### ➕ azurerm_network_security_group **`web`** — `🆔 web-nsg` in `📁 rg-web` `🌍 eastus`  
🔒 **Security & Quality:** 🚨 1 Critical, ⚠️ 1 Medium

#### Attributes

| Attribute | Value |
|-----------|-------|
| `name` | `🆔 web-nsg` |
| `location` | `🌍 eastus` |
| `resource_group_name` | `📁 rg-web` |
| `security_rule.0.name` | `"allow-https"` |
| `security_rule.0.source_address_prefix` 🚨 | `"*"` |
| `security_rule.0.destination_port_range` | `"443"` |
| `security_rule.0.protocol` | `"Tcp"` |
| `security_rule.0.access` | `"Allow"` |
| `security_rule.0.priority` ⚠️ | `100` |
| `security_rule.0.direction` | `"Inbound"` |

#### 🔒 Security & Quality Findings

| Severity | Attribute | Finding | Remediation |
|----------|-----------|---------|-------------|
| 🚨 Critical | `security_rule.0.source_address_prefix` | Unrestricted inbound access from any source (0.0.0.0/0). Limit to specific IP ranges or VNets. | [CKV_AZURE_10](https://docs.checkov.io/CKV_AZURE_10) |
| ⚠️ Medium | `security_rule.0.priority` | Priority 100 may conflict with Azure default security rules. Consider using 1000+ for custom rules. | [Trivy:AVD-AZU-0047](https://avd.aquasec.com/azure/AVD-AZU-0047) |

**Pros:**
- Best of both worlds: quick scanning + detailed context
- Always-visible findings table provides complete information
- Summary counts in resource metadata provide immediate awareness
- Emoji indicators don't clutter attribute values
- No reliance on collapsible sections (works on all platforms)

**Cons:**
- Two places to look for information (indicators + details)
- More vertical space required compared to collapsible version
- Findings table adds length to reports with many findings

---

## Summary Section Examples

All variants would include the same summary section at the top of the report:

# Terraform Plan Report

Generated by tfplan2md 1.5.0 on 2026-01-30 15:30:00 UTC | Terraform 1.14.0

## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 2 | 1 azurerm_network_security_group<br/>1 azurerm_storage_account |
| 🔄 Change | 1 | 1 azurerm_storage_account |
| **Total** | **3** | |

### 🔒 Code Analysis Summary

**Status:** ⚠️ 2 critical findings require attention

| Severity | Count | Rule IDs |
|----------|-------|----------|
| 🚨 Critical | 2 | 1 CKV_AZURE_10<br/>1 CKV_AZURE_7 |
| ⚠️ High | 1 | 1 CKV_AZURE_59 |
| ⚠️ Medium | 1 | 1 Trivy:AVD-AZU-0047 |
| ℹ️ Low | 1 | 1 TFLint:001 |

**Tools Used:** Checkov 3.2.10, Trivy 0.48.0, TFLint 0.50.0

> **Note:** Side-by-side layout for Plan Summary and Code Analysis Summary would improve information density, but markdown table limitations in GitHub and Azure DevOps make this challenging. Current vertical layout ensures consistent rendering across platforms.

---

## Unmatched Findings Section (Bottom of Report)

All variants would include this section at the end:

## 🔍 Additional Findings

### Module: network-security

| Severity | Finding | Remediation |
|----------|---------|-------------|
| ⚠️ High | Module 'azure-network' uses deprecated provider hashicorp/azurerm 3.0.0. Upgrade to 3.85.0+. | [Migration Guide](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/guides/3.0-upgrade-guide) |

### Configuration-Level Findings

| Severity | Finding | Remediation |
|----------|---------|-------------|
| ⚠️ Medium | Terraform version constraint allows 1.3.x which is EOL. Require 1.6.0+. | [Version Constraints](https://developer.hashicorp.com/terraform/language/expressions/version-constraints) |
| ℹ️ Low | Backend configuration missing encryption settings for state file. | [CKV_TF_1](https://docs.checkov.io/CKV_TF_1) |

---

## Edge Case: Resource with Many Findings

Example showing how each variant handles a resource with 8 findings:

### Variant A (Inline) - Truncated Example

### 🔄 azurerm_kubernetes_cluster **`main`** — `🆔 aks-cluster-main` in `📁 rg-kubernetes` `🌍 eastus` | 8🔧

| Attribute | Before | After |
|-----------|--------|-------|
| `network_profile.0.network_policy` 🚨 **Critical:** Network policy not enforced [CKV_AZURE_7] | `null` | `"calico"` |
| `role_based_access_control.0.enabled` 🚨 **Critical:** RBAC must be enabled [CKV_AZURE_4] | `false` | `true` |
| `addon_profile.0.oms_agent.0.enabled` ⚠️ **High:** Container insights not enabled [CKV_AZURE_6] | `false` | `true` |
| `addon_profile.0.azure_policy.0.enabled` ⚠️ **High:** Azure Policy addon recommended [CKV_AZURE_117] | `false` | `false` |
| `private_cluster_enabled` ⚠️ **High:** Public API endpoint exposes cluster [CKV_AZURE_115] | `false` | `false` |
| `api_server_authorized_ip_ranges` ⚠️ **Medium:** API server accepts connections from any IP [CKV_AZURE_116] | `[]` | `[]` |
| `identity.0.type` ℹ️ **Low:** Consider using user-assigned identity [CKV_AZURE_114] | `SystemAssigned` | `SystemAssigned` |
| `tags.environment` ℹ️ **Low:** Missing required tags [TFLint:002] | `dev` | `production` |

*Note: Table becomes very wide and cluttered*

### Variant B (Separate Section) - Truncated Example

### 🔄 azurerm_kubernetes_cluster **`main`** — `🆔 aks-cluster-main` in `📁 rg-kubernetes` `🌍 eastus` | 8🔧

#### Changes
| Attribute | Before | After |
|-----------|--------|-------|
| `network_profile.0.network_policy` | `null` | `"calico"` |
| `role_based_access_control.0.enabled` | `false` | `true` |
| `addon_profile.0.oms_agent.0.enabled` | `false` | `true` |
| `addon_profile.0.azure_policy.0.enabled` | `false` | `false` |
| `private_cluster_enabled` | `false` | `false` |
| `api_server_authorized_ip_ranges` | `[]` | `[]` |
| `identity.0.type` | `SystemAssigned` | `SystemAssigned` |
| `tags.environment` | `dev` | `production` |

#### 🔒 Security & Quality Findings

| Severity | Attribute | Finding | Remediation |
|----------|-----------|---------|-------------|
| 🚨 Critical | `network_profile.0.network_policy` | Network policy not enforced. Enable to restrict pod communication. | [CKV_AZURE_7](link) |
| 🚨 Critical | `role_based_access_control.0.enabled` | RBAC must be enabled for access control. | [CKV_AZURE_4](link) |
| ⚠️ High | `addon_profile.0.oms_agent.0.enabled` | Container insights provides monitoring and diagnostics. | [CKV_AZURE_6](link) |
| ⚠️ High | `addon_profile.0.azure_policy.0.enabled` | Azure Policy addon enforces governance policies on cluster. | [CKV_AZURE_117](link) |
| ⚠️ High | `private_cluster_enabled` | Public API endpoint exposes cluster to internet threats. | [CKV_AZURE_115](link) |
| ⚠️ Medium | `api_server_authorized_ip_ranges` | Limit API server access to specific IP ranges. | [CKV_AZURE_116](link) |
| ℹ️ Low | `identity.0.type` | User-assigned identity recommended for better lifecycle management. | [CKV_AZURE_114](link) |
| ℹ️ Low | `tags.environment` | Missing required tags: cost-center, owner. | [TFLint:002](link) |

*Note: Clean table, but long scrolling required*

### Variant C (Hybrid) - Truncated Example

### 🔄 azurerm_kubernetes_cluster **`main`** — `🆔 aks-cluster-main` in `📁 rg-kubernetes` `🌍 eastus` | 8🔧  
🔒 **Security & Quality:** 🚨 2 Critical, ⚠️ 4 High, ℹ️ 2 Low

#### Changes
| Attribute | Before | After |
|-----------|--------|-------|
| `network_profile.0.network_policy` 🚨 | `null` | `"calico"` |
| `role_based_access_control.0.enabled` 🚨 | `false` | `true` |
| `addon_profile.0.oms_agent.0.enabled` ⚠️ | `false` | `true` |
| `addon_profile.0.azure_policy.0.enabled` ⚠️ | `false` | `false` |
| `private_cluster_enabled` ⚠️ | `false` | `false` |
| `api_server_authorized_ip_ranges` ⚠️ | `[]` | `[]` |
| `identity.0.type` ℹ️ | `SystemAssigned` | `SystemAssigned` |
| `tags.environment` ℹ️ | `dev` | `production` |

#### 🔒 Security & Quality Findings

[Same detailed table as Variant B]

*Note: Compact emoji indicators in table, full details below - no collapsible sections needed*

---

## Recommendation Request

Please review all three variants and indicate which presentation style best meets your needs for:
1. Quick scanning during PR review
2. Readability with many findings
3. Aesthetic quality
4. Information density

Consider both typical cases (1-3 findings per resource) and edge cases (8+ findings per resource).
