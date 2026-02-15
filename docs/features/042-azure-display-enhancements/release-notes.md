# Azure Display Enhancements

This release significantly improves the readability of Terraform plans containing Azure resources by enriching technical IDs with human-readable display names and providing more descriptive resource summaries.

## ✨ Features

- **Enriched Resource IDs**: Automatically resolves subscription names, management group names, and tenant names from a mapping file and injects them into the report.
- **Improved Role Definitions**: Automatically recognizes built-in Azure roles and supports custom role mappings. Role IDs are now displayed as `🛡️ Role Name (GUID)`.
- **Resource-Specific Summaries**:
  - `azurerm_private_dns_a_record`: Displays the Full Qualified Domain Name (FQDN).
  - `azurerm_pim_eligible_role_assignment`: Displays "Assign `🛡️ Role` to `👤 Principal`".
  - `azurerm_role_management_policy`: Displays "`🛡️ Role` in `Scope name`".
- **Broadened ID Detection**: Any attribute value matching an Azure resource ID pattern is now automatically formatted, regardless of the attribute name.
- **Enhanced Diagnostics**: Improved failure tracking in debug mode to identify which resources have unmapped IDs.

## 🔗 Commits

- [`10f194f4`](https://github.com/oocx/tfplan2md/commit/10f194f4) feat: broaden azure resource id detection
- [`768a8755`](https://github.com/oocx/tfplan2md/commit/768a8755) feat: add pim and role policy summaries
- [`f65c5eb7`](https://github.com/oocx/tfplan2md/commit/f65c5eb7) feat: add private dns a record summaries
- [`f089433c`](https://github.com/oocx/tfplan2md/commit/f089433c) feat: resolve azure role definitions
- [`c715577d`](https://github.com/oocx/tfplan2md/commit/c715577d) feat: enrich azure scope formatting
- [`645d7363`](https://github.com/oocx/tfplan2md/commit/645d7363) feat: extend azure mapping loader and tests
- [`1820e2f5`](https://github.com/oocx/tfplan2md/commit/1820e2f5) fix: adjust azure display summaries

## ▶️ Getting started

To take advantage of display name resolution, update your principal mapping JSON file to include the new `subscriptions`, `managementGroups`, `tenants`, and `roles` sections:

```json
{
  "principals": { ... },
  "subscriptions": [
    { "id": "d1828a48-fced-4ea2-b2ec-4b9623f327fd", "displayName": "Production" }
  ],
  "managementGroups": [
    { "id": "mg-prod", "displayName": "Production Resources" }
  ],
  "roles": [
    { "id": "8e3af657-a8ff-443c-a75c-2fe8c4bcb635", "displayName": "Owner" }
  ]
}
```

Refer to the README for Azure CLI commands to help populate these sections.
