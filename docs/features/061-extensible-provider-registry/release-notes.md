# Extensible Provider Registry & Enhanced Iconography

This release introduces a powerful architectural improvement with the **Extensible Provider Registry System**. This system decouples iconography and formatting logic from the core templates, allowing for more flexible, pattern-based matching of resources and attributes.

## ✨ Features

- **Extensible Provider Registry**: A new core system for registering resource view model factories, value formatters, and icon providers using flexible pattern matching (Provider, Resource Type, Attribute Name, and Value).
- **Enhanced Azure AD Iconography**: Detailed icons for Azure AD resources including Users (👤), Groups (👥), Service Principals (💻), and email addresses (📧).
- **Consolidated Action Icons**: Standardized action symbols (➕ Add, 🔄 Change, ♻️ Replace, ❌ Destroy, ⏺️ No Change) are now centralized, ensuring visual consistency across all report sections.
- **Improved Value Formatting**: Automatic formatting of common attribute patterns, such as shortening Azure Resource IDs (📁) and highlighting Subscription IDs (🔑).
- **Semantic Icons for Networking**: Improved visibility for network rules with icons for Allow (✅), Deny (⛔), Inbound (⬇️), Outbound (⬆️), and various protocols (TCP 🔗, UDP 📨, ICMP 📡).
- **Variable Group Icons**: Added change status icons to variable groups in Azure DevOps plans.

## 🔗 Commits

- [`2fcd3f08`](https://github.com/oocx/tfplan2md/commit/2fcd3f08) feat: add change icons to variable groups
- [`3e9bde16`](https://github.com/oocx/tfplan2md/commit/3e9bde16) fix: centralize action icons and restore azuread icons
- [`c6077555`](https://github.com/oocx/tfplan2md/commit/c6077555) feat: add provider snapshot coverage
- [`abcc1425`](https://github.com/oocx/tfplan2md/commit/abcc1425) feat: migrate provider icon rules to registry
- [`25ea6453`](https://github.com/oocx/tfplan2md/commit/25ea6453) feat: add json-based icon provider
- [`d9850fb5`](https://github.com/oocx/tfplan2md/commit/d9850fb5) feat: add formatter and icon registries
- [`e68ead3c`](https://github.com/oocx/tfplan2md/commit/e68ead3c) feat: add pattern-matching registry core

## ▶️ Getting started

No changes to CLI usage or configuration are required. The new registry system works automatically with existing Terraform plans.
