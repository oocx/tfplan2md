# Azure AD assignment summaries now resolve Graph roles and principals

This release extends `tfplan2md`'s Azure AD coverage for permission and role assignment resources. Instead of reviewing raw GUIDs in pull requests, you now get summary lines that resolve common Microsoft Graph app roles, reuse principal mapping data for display names, and keep Azure AD assignment icons consistent between summaries and detail tables.

## ✨ Features

- **`azuread_app_role_assignment` summaries now show who gets what on which API**: summary lines follow the familiar `{principal} → {role} → {resource}` shape, with built-in Microsoft Graph app role resolution for common `app_role_id` values such as `User.Read.All` and `Group.ReadWrite.All`.
- **`azuread_directory_role_assignment` gets first-class summary output**: directory role assignments now render a concise principal-to-role summary instead of leaving reviewers to interpret raw attributes.
- **`azuread_service_principal_delegated_permission_grant` is now summarized explicitly**: delegated permission grants show the service principal, granted claims, and target resource in a single readable line.
- **Existing principal mapping support carries over to Azure AD assignments**: if you already use `--principal-mapping` / `--principals`, the same mapping data is used to resolve Azure AD principals and target resources. For `azuread_app_role_assignment`, plan-provided display names are also used as a fallback when mappings are unavailable.
- **Detail-table formatting matches the summaries**: `app_role_id`, `principal_object_id`, `service_principal_object_id`, and `resource_object_id` now use the same Azure AD-specific formatters, so the summary line and attribute table tell the same story.

## 🐛 Bug fixes

- **Consistent Azure AD assignment icons in summary lines**: Feature 116 follow-up work corrected the summary/UAT rendering so Azure AD assignment summaries use the same semantic icons as the attribute table (`👤` principal, `🛡️` app role / directory role, `🎯` target resource). This makes the summaries easier to scan and keeps create/delete output visually consistent.

## 📸 Screenshots

### Built-in Microsoft Graph role resolution in the summary line

![Known Microsoft Graph app role resolution](https://raw.githubusercontent.com/oocx/tfplan2md/main/docs/features/116-azuread-app-role-assignment/azuread-app-role-assignment-known-role.png)

### Fallback display names when no built-in app role mapping exists

![Fallback principal and resource display names](https://raw.githubusercontent.com/oocx/tfplan2md/main/docs/features/116-azuread-app-role-assignment/azuread-app-role-assignment-fallbacks.png)

## 🔗 Commits

- [`182606d`](https://github.com/oocx/tfplan2md/commit/182606d) feat: add azuread_app_role_assignment support with Microsoft Graph app role resolution
- [`a028fa9`](https://github.com/oocx/tfplan2md/commit/a028fa9) feat: add directory role assignment and delegated permission grant support
- [`1b511cd`](https://github.com/oocx/tfplan2md/commit/1b511cd) fix: restore deleted snapshots and correct Feature 116 summary icons SNAPSHOT_UPDATE_OK
