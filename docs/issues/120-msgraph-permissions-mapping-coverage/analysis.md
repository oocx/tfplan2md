# Issue 120: Missing Microsoft Graph Permission Names in Built-in Mapping

## Problem Description

When rendering an `azuread_app_role_assignment` resource that grants the Microsoft Graph application permission `Policy.ReadWrite.Authorization` (app role GUID `fb221be6-99f2-473f-bd32-01c6a0e9ca3b`), tfplan2md does not resolve the GUID to its human-readable permission name. The raw GUID is rendered both in the resource summary line and in the attributes table.

The general GUID-to-name resolution mechanism for Microsoft Graph app roles already exists and works correctly for other permissions (e.g., `User.Read.All`, `Directory.Read.All`). The defect is that the embedded mapping table is **incomplete** — it does not cover the full set of well-known Microsoft Graph permissions documented at <https://learn.microsoft.com/en-us/graph/permissions-reference>.

## Steps to Reproduce

1. Create a Terraform plan that contains an `azuread_app_role_assignment` whose `app_role_id` is `fb221be6-99f2-473f-bd32-01c6a0e9ca3b` (Microsoft Graph → `Policy.ReadWrite.Authorization`), e.g.:
   ```hcl
   resource "azuread_app_role_assignment" "this" {
     app_role_id         = "fb221be6-99f2-473f-bd32-01c6a0e9ca3b"
     principal_object_id = "55fb3b1b-e3f4-4fcc-8edf-837e722ec927"
     resource_object_id  = "31939070-7533-4b31-8a67-29e18a4ad777"
   }
   ```
2. Provide a principal mapping file that maps `55fb3b1b-...` → `governance-id-lv1-gwc` and `31939070-...` → `Microsoft Graph`.
3. Run `tfplan2md` against the resulting plan JSON.
4. Inspect the rendered `azuread_app_role_assignment.this` section.

## Expected Behavior

Both the summary line and the attributes table show the resolved permission name alongside the GUID, consistent with the format already used for other well-known Graph permissions.

Summary line:
```
azuread_app_role_assignment this — 👤 governance-id-lv1-gwc (55fb3b1b-…) → 🛡️ Policy.ReadWrite.Authorization (fb221be6-…) → 🎯 Microsoft Graph (31939070-…)
```

Attributes table:
```
app_role_id    🛡️ Policy.ReadWrite.Authorization (fb221be6-99f2-473f-bd32-01c6a0e9ca3b)
```

## Actual Behavior

The GUID is rendered without a name in both places.

Summary line:
```
azuread_app_role_assignment this — 👤 governance-id-lv1-gwc (55fb3b1b-…) → 🛡️ fb221be6-99f2-473f-bd32-01c6a0e9ca3b → 🎯 Microsoft Graph (31939070-…)
```

Attributes table:
```
app_role_id    🛡️ fb221be6-99f2-473f-bd32-01c6a0e9ca3b
```

## Root Cause Analysis

### Affected Components

- Embedded mapping data:
  - `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json` — only **131 entries**; the official Microsoft Graph permissions reference contains several hundred application permissions (and a similarly large set of delegated permissions / OAuth2 scopes).
- Resolution / formatting (working correctly, no change needed for this defect):
  - `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoleResolver.cs` — falls back to the raw GUID when the dictionary has no entry (`return new RoleDefinitionInfo(appRoleId, appRoleId, appRoleId);`).
  - `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRolesRegistry.cs` — loads the embedded JSON.
  - `src/Oocx.TfPlan2Md/Platforms/Azure/AppRoleIdFormatter.cs` — value formatter that produces the `🛡️ {name} ({id})` table cell. Returns `null` (i.e., no formatting) when `roleInfo.Name == roleInfo.Id`, which is exactly the unresolved case.
  - `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.AppRoleAssignments.cs` — builds the summary line; uses `ResolveAppRoleName` against the same resolver.
- Existing tests illustrating the working path:
  - `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/MicrosoftGraphAppRoleResolverTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AppRoleIdFormatterTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdAppRoleAssignmentTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzureAdSnapshotTests.cs`

### What's Broken

The mapping from app-role GUID to permission name is data-driven via a curated JSON file. That file is missing a large number of well-known Microsoft Graph permissions, including (but not limited to) `Policy.ReadWrite.Authorization` (`fb221be6-99f2-473f-bd32-01c6a0e9ca3b`). When the GUID is not present in the dictionary, both the summary builder and the attribute formatter intentionally fall back to the raw GUID with no annotation, producing the observed behavior.

The current file contains only `Policy.Read.All`, `Policy.ReadWrite.ConditionalAccess`, `Policy.Read.PermissionGrant`, and `Policy.ReadWrite.PermissionGrant` from the `Policy.*` family — most of the family is missing.

### Why It Happened

Feature 116 (`docs/features/116-azuread-app-role-assignment/specification.md`, scope item §6 "Well-Known Microsoft Graph App Roles List") explicitly seeded the mapping with "the most commonly used" permissions only. The list was hand-curated and never reconciled against the full Microsoft Graph permissions reference, so any permission outside the seed set fails to resolve. There is currently no automation or pipeline that keeps the embedded JSON in sync with the upstream reference (compare with `scripts/update-azure-api-mappings.py`, which maintains a different mapping file).

## Suggested Fix Approach

The fix is **purely data**: extend the embedded mapping so it covers all well-known Microsoft Graph permissions. The Developer should not need to change the resolver, formatter, or summary-builder code paths.

High-level steps for the Developer:

1. **Source of truth.** Use the published Microsoft Graph permissions reference as the authoritative source:
   - <https://learn.microsoft.com/en-us/graph/permissions-reference>
   - The same data is also exposed programmatically via the `00000003-0000-0000-c000-000000000000` (Microsoft Graph) service principal in any Entra tenant (`appRoles[]` for application permissions and `oauth2PermissionScopes[]` for delegated permissions). A Microsoft Graph PowerShell or `az rest` query against `/v1.0/servicePrincipals(appId='00000003-0000-0000-c000-000000000000')` returns the canonical, machine-readable list. This avoids manual transcription of the documentation HTML.
2. **Extend `MicrosoftGraphAppRoles.json`** to include every entry in `appRoles[]` (application permissions), keyed by the role `id` GUID with the role `value` (e.g., `Policy.ReadWrite.Authorization`) as the value. Verify that `fb221be6-99f2-473f-bd32-01c6a0e9ca3b` resolves to `Policy.ReadWrite.Authorization` after the change.
3. **Decide on delegated permissions / OAuth2 scopes.** The maintainer's request says "all well-known msgraph permissions … in all resources where this is appropriate." Delegated permissions (`oauth2PermissionScopes`) surface in `azuread_service_principal_delegated_permission_grant.claim_values`, but those values are already permission **names** (e.g., `User.Read`), not GUIDs — see `AzureAdSummaryBuilder.AppRoleAssignments.cs` lines around `claim_values`. So no GUID-to-name mapping is needed there today. However, this should be confirmed (see Open Questions).
4. **Add an automation script** (recommended, follows the pattern of `scripts/update-azure-api-mappings.py`) that regenerates `MicrosoftGraphAppRoles.json` from the Microsoft Graph service principal so the mapping can be refreshed as Microsoft adds new permissions. This keeps maintenance cost bounded.
5. **Tests.** Extend the existing tests (or add new ones) to assert that a representative cross-section of permissions, including `fb221be6-99f2-473f-bd32-01c6a0e9ca3b` → `Policy.ReadWrite.Authorization`, resolves correctly. Update or add a snapshot test in `AzureAdSnapshotTests` reproducing the maintainer's scenario.

### Scope of the Fix

In scope:

- Replace / extend `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json` so it covers the full set of well-known Microsoft Graph **application** permissions.
- Add unit tests pinning a representative subset of the new mappings (including the maintainer's example).
- Add a snapshot/integration test for the `azuread_app_role_assignment` rendering with `Policy.ReadWrite.Authorization`.
- Optionally: add a generator script under `scripts/` to regenerate the JSON from the Microsoft Graph service principal.
- Update `docs/features/116-azuread-app-role-assignment/specification.md` (or a follow-up note) to record that the seed list has been replaced with full coverage.

Out of scope (unless the maintainer answers the open questions in the affirmative):

- Mapping delegated permission GUIDs (OAuth2 scope IDs) to names — not currently needed because `claim_values` are already names.
- Adding well-known permissions for resource APIs other than Microsoft Graph (e.g., SharePoint, Exchange, Intune, Azure Service Management). Feature 116 explicitly scoped the built-in list to Microsoft Graph only.
- Changes to the resolver / formatter / summary-builder code.

## Related Tests

Tests that should continue to pass and that must be extended for the fix:

- [ ] `MicrosoftGraphAppRoleResolverTests` — add a case for `fb221be6-99f2-473f-bd32-01c6a0e9ca3b` → `Policy.ReadWrite.Authorization` and ideally a parameterized check across a representative subset of the new mappings.
- [ ] `AppRoleIdFormatterTests` — add a case verifying the `🛡️ Policy.ReadWrite.Authorization (fb221be6-…)` cell is produced.
- [ ] `AzureAdAppRoleAssignmentTests` — add a case for the maintainer's exact scenario.
- [ ] `AzureAdSnapshotTests` — refresh / extend the snapshot to cover the new resolution.

## Additional Context

- Maintainer report: see PR/issue thread that triggered this analysis.
- Authoritative permissions reference: <https://learn.microsoft.com/en-us/graph/permissions-reference>.
- Programmatic source for regeneration: Microsoft Graph service principal (`appId = 00000003-0000-0000-c000-000000000000`), endpoint `/v1.0/servicePrincipals(appId='00000003-0000-0000-c000-000000000000')`, fields `appRoles` and `oauth2PermissionScopes`.
- Feature that introduced the current (incomplete) mapping: `docs/features/116-azuread-app-role-assignment/specification.md` §6.
- Comparable maintenance pattern in this repo: `scripts/update-azure-api-mappings.py`.

## Open Questions for the Maintainer

These require maintainer input and should not be answered by the Issue Analyst:

1. **Delegated permission GUIDs.** `azuread_service_principal_delegated_permission_grant.claim_values` already contains scope **names** (e.g., `User.Read`), so no GUID-to-name mapping is needed for that resource today. Are there other places (e.g., raw `oauth2_permission_scope_ids` on `azuread_service_principal` definitions, or `requested_access_token_version`-style attributes on `azuread_application` `required_resource_access` blocks) where delegated permission **GUIDs** appear and should also be mapped? If yes, we should extend the JSON with `oauth2PermissionScopes` and broaden formatter registration.
2. **Other resource APIs.** Feature 116 deliberately scoped the built-in mapping to Microsoft Graph. Do you want to extend coverage to other well-known Microsoft first-party APIs (SharePoint Online, Exchange Online, Intune / Microsoft Graph Intune, Azure Service Management, Power BI) in this fix, or keep that as a separate follow-up?
3. **Generation strategy.** Do you want the JSON regenerated from a tenant via Graph (requires authentication) at build time, generated offline by a maintainer script committed to `scripts/` (preferred — same pattern as `update-azure-api-mappings.py`), or hand-curated from the public permissions-reference page? This affects how often the data can be refreshed.
4. **Stable display name choice.** Microsoft Graph app roles expose both a `value` (e.g., `Policy.ReadWrite.Authorization`) and a `displayName` (e.g., `Read and write your organization's authorization policy`). The current JSON uses the short `value`. Please confirm this should remain the convention for the expanded list.
