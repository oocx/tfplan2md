# Azure Firewall Application Rules: Rule-by-Rule Diffing

This release adds semantic diffing for Azure Firewall application rule collections, completing the firewall rules feature set that was previously limited to network rules only.

## ✨ Features

### Custom Template for `azurerm_firewall_application_rule_collection`

tfplan2md now renders application firewall rules with the same rule-by-rule semantic diffing that network rules have had since the beginning. Instead of seeing cryptic index-based array changes like `rules[1].protocols[0]`, you now see clear, human-readable tables showing exactly which rules changed.

**What changed:**
- ✅ Added: New rules with protocols, FQDNs, and source addresses
- 🔄 Modified: Inline diffs showing what changed in existing rules
- ❌ Removed: Clearly marked deleted rules
- ⏺️ Unchanged: Rules that didn't change (shown for context)

**Key capabilities:**
- **Protocol formatting**: Shows `Https:443` or `Http:80, Https:443` instead of raw JSON
- **FQDN truncation**: Long lists (>5 FQDNs) show first 3 + "...+N more"
- **Optional properties**: Handles `source_ip_groups` and `fqdn_tags` (e.g., `WindowsUpdate`)
- **All scenarios**: Create, update, delete operations with context-appropriate tables
- **Inline diffs**: Modified properties show before/after with highlighting

This mirrors the existing `azurerm_firewall_network_rule_collection` template but handles application-specific properties (FQDNs instead of destination IPs, HTTP/HTTPS/MSSQL protocols instead of TCP/UDP).

## 🐛 Bug fixes

- **Protocol property parsing**: Fixed factory to use correct `protocols` (plural) property name instead of `protocol` (singular) to match Azure Terraform provider schema
- **AOT compilation**: Added `FirewallApplicationRuleCollection` mapping to `AotScriptObjectMapper` for Native AOT compatibility

## 📚 Documentation

- Updated README.md with application rule collection support
- Updated docs/features.md with feature description
- Updated website feature page to accurately reflect application rule support

## 📸 Screenshots

### Application Rule Collection (Create)

Shows rules being added with protocols, source addresses, and target FQDNs:

![Create example showing two rules with GitHub and Azure FQDNs](https://raw.githubusercontent.com/oocx/tfplan2md/37892f2/artifacts/firewall-application-rules-uat.md#L17-L28)

### Application Rule Collection (Update)

Shows semantic change detection with added (➕), modified (🔄), removed (❌), and unchanged (⏺️) rules:

![Update example showing rule changes with inline diffs](https://raw.githubusercontent.com/oocx/tfplan2md/37892f2/artifacts/firewall-application-rules-uat.md#L30-L48)

### Application Rule with FQDN Tags

Shows optional properties like `source_ip_groups` and `fqdn_tags` (Windows Update, App Service Environment):

![FQDN tags example](https://raw.githubusercontent.com/oocx/tfplan2md/37892f2/artifacts/firewall-application-rules-uat.md#L56-L67)

## 🔗 Commits

User-facing commits:

- [`da7cbfc`](https://github.com/oocx/tfplan2md/commit/da7cbfc) fix: add FirewallApplicationRuleCollection mapping to AotScriptObjectMapper
- [`8314532`](https://github.com/oocx/tfplan2md/commit/8314532) fix: use correct 'protocols' property name in firewall application rule factory
- [`c34a275`](https://github.com/oocx/tfplan2md/commit/c34a275) docs: add azurerm_firewall_application_rule_collection to documentation
- [`16cc46b`](https://github.com/oocx/tfplan2md/commit/16cc46b) docs: regenerate demo artifacts after protocol fix

## 🚨 Breaking changes

None. This is a pure addition - existing templates and features continue to work unchanged.
