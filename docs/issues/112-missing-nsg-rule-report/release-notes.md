# NSG Separate Rule Rendering Fix

This release is a bug-fix-only update. It fixes AzureRM network security group rendering so separately managed NSG rules are shown in the parent `Security Rules` table instead of being counted in the summary but missing from the report body.

## 🐛 Bug fixes

- Fixed `azurerm_network_security_group` output so merged child `azurerm_network_security_rule` changes remain visible for separate create, update, delete, and mixed inline-plus-separate scenarios.

## 📸 Screenshots

The restored row is visible in the parent NSG table for the create scenario.

![Restored separate NSG rule row](https://raw.githubusercontent.com/oocx/tfplan2md/v1.37.1/docs/issues/112-missing-nsg-rule-report/nsg-separate-rule-fix.png)

## 🔗 Commits

- [`36269688`](https://github.com/oocx/tfplan2md/commit/36269688) fix: complete issue 112 review rework
- [`c7229936`](https://github.com/oocx/tfplan2md/commit/c7229936) chore: update comprehensive demo

## ▶️ Getting started
