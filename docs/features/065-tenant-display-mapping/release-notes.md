# Azure Display Enhancements and Tenant Mapping

This release introduces major improvements to how Azure resources and identities are displayed in your Terraform plans. It includes universal Azure resource ID enrichment, better summaries for role assignments and PIM, and visual icons for tenants and management groups.

## ✨ Features

- **Universal Azure Resource ID Enrichment**: Automatically detects and formats Azure resource IDs across all providers.
- **Subscription & Management Group Display Names**: Enrich IDs with human-readable names from your principal mapping file. Subscriptions are formatted as `DisplayName (ID)`.
- **Tenant Display Name Mapping**: Support for mapping tenant IDs to names, formatted with a 🏢 icon: `🏢 Contoso Corp (guid)`.
- **Visual Icons**: Added visual icons for better scannability:
  - 🏢 for Tenants
  - 🗂️ for Management Groups
- **Enhanced Resource Summaries**:
  - `azurerm_private_dns_a_record`: Shows FQDN (name.zone_name).
  - `azurerm_pim_eligible_role_assignment`: Clearer "Assign <role> to <principal>" summary.
  - `azurerm_role_management_policy`: More descriptive summary.
- **Role Definition Resolution**: Automatically recognizes built-in Azure roles and supports custom role mappings (with override capability).
- **Extended Mapping File Format**: Unified JSON format to include subscriptions, tenants, management groups, and roles.
- **Azure CLI Export Commands**: Documented commands to easily export your Azure environment metadata for mapping.

## 🐛 Bug fixes

- **Icon Placement**: Fixed an issue where Azure icons were placed outside of code spans; they are now consistently placed inside backticks per standard.
- **Management Group Scope Formatting**: Corrected formatting for management group scopes in attribute tables and summaries.

## 🔗 Commits

- [`2d3b8ebf`](https://github.com/oocx/tfplan2md/commit/2d3b8ebf) feat: add tenant and management group formatting
- [`600ce45e`](https://github.com/oocx/tfplan2md/commit/600ce45e) fix: place Azure icons inside code spans
- [`80f1320a`](https://github.com/oocx/tfplan2md/commit/80f1320a) fix: correct management group scope formatting

## ▶️ Getting started

The `--principal-mapping` file now supports a unified format:

```json
{
  "tenants": [
    { "id": "guid", "displayName": "Contoso Corp" }
  ],
  "subscriptions": [
    { "id": "guid", "displayName": "Production" }
  ],
  "managementGroups": [
    { "id": "mg-id", "displayName": "Workloads" }
  ]
}
```
