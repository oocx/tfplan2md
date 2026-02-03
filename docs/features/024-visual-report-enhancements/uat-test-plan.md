# UAT Test Plan: Visual Report Enhancements

## Goal
Verify that the new visual enhancements (icons, spacing, layout, semantic formatting) render correctly in both GitHub and Azure DevOps PR comments, ensuring cross-platform compatibility and improved readability.

## Artifacts
**Artifact to use:** `artifacts/comprehensive-demo.md`

**Creation Instructions:**
- **Source Plan:** `examples/comprehensive-demo/plan.json`
- **Command:** `tfplan2md examples/comprehensive-demo/plan.json > artifacts/comprehensive-demo.md`
- **Rationale:** This plan contains a wide variety of resources (VNet, Storage, NSG, Firewall, Role Assignments) and change types (Create, Update, Delete, Replace), making it ideal for verifying all visual enhancements.

## Test Steps
1. Run UAT using the `UAT Tester` agent: `scripts/uat-run.sh artifacts/comprehensive-demo.md "<validation-description>"`
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

### 1. Module Separation and Headers
- **Specific Resources:** `Module: ./modules/networking`, `Module: ./modules/security`
- **Expected Outcome:** Verify a horizontal rule (`---`) exists between modules. Verify the module icon (📦) appears before the module path.

### 2. Resource Layout and Summaries
- **Specific Resources:** `azurerm_virtual_network.hub`, `azurerm_storage_account.data`
- **Expected Outcome:** 
    - Resources must be collapsed in `<details>` tags.
    - Summary lines must contain: Action icon (➕/🔄/♻️/❌), Resource Type (plain text), Resource Name (**bold** `code`).
    - **CRITICAL (AzDO):** Verify that code formatting in the summary line (e.g., the resource name) renders correctly in Azure DevOps. It should use HTML `<code>` tags.

### 3. Semantic Value Formatting
- **Location:** Check `azurerm_virtual_network.hub`. Verify location displays as `(<code>🌍 eastus</code>)` in the summary and `` `🌍 eastus` `` in the table.
- **IP Addresses:** Check `address_space` in `azurerm_virtual_network.hub`. Verify it displays as `` `🌐 10.0.0.0/16` `` (icon inside code).
- **Booleans:** Check `https_only` in `azurerm_storage_account.data`. Verify it displays as `✅ true` or `❌ false`.
- **Security Rules:** Check `azurerm_network_security_rule.allow_https`. Verify:
    - Access: `✅ Allow`
    - Direction: `⬇️ Inbound`
    - Protocol: `🔗 TCP`

### 4. Tags and Changed Attributes
- **Tags:** Check `azurerm_virtual_network.hub` (Create). Verify tags appear as inline badges below the table: `**🏷️ Tags:** `env: prod` `owner: devops``.
- **Changed Attributes:** Check `azurerm_storage_account.data` (Update). Verify the summary line shows the count and wrench icon: `2 🔧 account_replication_type, tags.cost_center`.

### 5. Resource-Specific Templates
- **NSG/Firewall:** Verify that `azurerm_network_security_group` and `azurerm_firewall_network_rule_collection` also use the new collapsible layout and semantic icons.

## Before/After Context
The previous reports were "walls of text" with H4 headings for every resource. The new version uses collapsible sections, semantic icons for quick scanning, and improved spacing to make the report look professional and scannable.
