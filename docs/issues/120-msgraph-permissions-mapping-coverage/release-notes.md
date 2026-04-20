# Microsoft Graph permission GUIDs now resolve in Azure AD assignments

Bug fix: `azuread_app_role_assignment` resources rendered most Microsoft Graph permission GUIDs raw (e.g. `🛡️ fb221be6-…`) because the bundled lookup table only covered 131 of the well-known application permissions. The mapping has been regenerated from the Microsoft Graph permissions reference and now covers all 673 Microsoft Graph application permissions, so common roles such as `Policy.ReadWrite.Authorization` show by name in both summary lines and attribute tables.

## 🐛 Bug fixes

- **Well-known Microsoft Graph application permissions now resolve to their display name** in `azuread_app_role_assignment` summaries and the `app_role_id` attribute row. Previously only 131 GUIDs were recognized; 542 additional Microsoft Graph application permissions (including `Policy.ReadWrite.Authorization`, `RoleManagement.ReadWrite.Directory`, `Sites.FullControl.All`, and others) rendered as raw GUIDs.
- **Corrected entries that pointed at the delegated GUID instead of the application GUID** for several roles in the previous hand-curated list (for example `AuditLog.Read.All`), so the resolved name now matches what the Azure portal and Graph API report.

## 📚 Documentation

- New `CONTRIBUTING.md` section "Maintaining Microsoft Graph App Role Mappings" documents the new `scripts/update-msgraph-app-roles.py` regeneration script (stdlib-only, `--source` / `--output` / `--dry-run`).
- `docs/features.md` clarifies the scope of the GUID resolution: Microsoft Graph **application** permissions only — delegated `oauth2PermissionScopes` and non-Graph APIs (SharePoint, Exchange, Office, Intune, ARM, …) are intentionally out of scope.

## 🔗 Commits

- [`1b7a35e`](https://github.com/oocx/tfplan2md/commit/1b7a35e) docs: add issue analysis for missing msgraph permission name mappings (#120)
- [`157223d`](https://github.com/oocx/tfplan2md/commit/157223d) chore(scripts): add update-msgraph-app-roles.py generator (#120)
- [`7f43e67`](https://github.com/oocx/tfplan2md/commit/7f43e67) fix(azure): map well-known Microsoft Graph permission GUIDs to display names (#120)
- [`5d54f0e`](https://github.com/oocx/tfplan2md/commit/5d54f0e) docs: document expanded Microsoft Graph permission mapping (#120)
