# Work Protocol: Microsoft Graph Permissions Mapping Coverage

**Work Item:** `docs/issues/120-msgraph-permissions-mapping-coverage/`
**Branch:** `copilot/map-policy-readwrite-authorization-role`
**Workflow Type:** Bug Fix
**Created:** 2025-04-20

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2025-04-20
- **Summary:** Investigated maintainer report that the app role GUID `fb221be6-99f2-473f-bd32-01c6a0e9ca3b` (`Policy.ReadWrite.Authorization`) is not resolved to its display name in either the resource summary line or the attributes table for `azuread_app_role_assignment`. Confirmed root cause: the GUID-to-name mapping infrastructure (`MicrosoftGraphAppRoleResolver`, `AppRoleIdFormatter`, `AzureAdSummaryBuilder.AppRoleAssignments`) is functioning correctly, but the embedded mapping data file `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json` (131 entries) is incomplete relative to the published Microsoft Graph permissions reference and is missing this and many other well-known Graph permissions.
- **Artifacts Produced:**
  - `docs/issues/120-msgraph-permissions-mapping-coverage/analysis.md`
  - `docs/issues/120-msgraph-permissions-mapping-coverage/work-protocol.md`
- **Problems Encountered:** None.

### Developer
- **Date:** 2025-04-20
- **Summary:** Fixed by replacing the hand-curated 131-entry `MicrosoftGraphAppRoles.json` with the full set of well-known Microsoft Graph **application** permissions (673 entries) parsed from the upstream markdown source of the Microsoft Learn permissions reference page (`microsoftgraph/microsoft-graph-docs-contrib/concepts/permissions-reference.md`, which is the source rendered at <https://learn.microsoft.com/en-us/graph/permissions-reference>). The defect GUID `fb221be6-99f2-473f-bd32-01c6a0e9ca3b` now resolves to `Policy.ReadWrite.Authorization`. No resolver / formatter / summary-builder behaviour changes — purely a data + tooling fix.
- **Artifacts Produced:**
  - `scripts/update-msgraph-app-roles.py` — new maintainer regeneration script (stdlib only, mirrors the conventions of `scripts/update-azure-api-mappings.py`). Default source is the upstream raw markdown URL; supports `--source <url|path>`, `--output`, and `--dry-run`. Prints an added/removed/total summary on each run. Idempotent and re-runnable.
  - `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json` — regenerated; 673 application permissions, sorted by GUID for deterministic diffs. Same flat `{guid: value}` shape as before so `MicrosoftGraphAppRolesRegistry` loads it unchanged.
  - `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/MicrosoftGraphAppRoleResolverTests.cs` — added the maintainer's exact-scenario test (`fb221be6-…` → `Policy.ReadWrite.Authorization`) plus a parameterised cross-section covering 8 well-known permissions across Policy/User/Directory/Application/AuditLog/RoleManagement families.
  - `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AppRoleIdFormatterTests.cs` — added the corresponding formatter test asserting the exact rendered cell `` `🛡️\u00a0Policy.ReadWrite.Authorization (fb221be6-…)` ``.
- **Test Run:** `scripts/test-with-timeout.sh --timeout-seconds 600 -- dotnet test --solution src/tfplan2md.slnx` → **1254 passed, 0 failed, 0 skipped** (3m 07s).
- **Snapshot Updates:** None required. The only real Microsoft Graph GUID present in `azuread-app-role-assignment-plan.json` (`df021288-bdef-4463-88db-98f22de89214` / `User.Read.All`) is preserved in the upstream reference, so the existing `azuread-app-role-assignment.md` snapshot is unchanged.
- **Problems Encountered:** Network egress in this sandbox does not allow `learn.microsoft.com`; the script's default source URL points at the upstream raw markdown on GitHub, which is the actual source of truth for the Learn page (the Learn page is generated from that markdown). This is documented in the script's docstring and `--help`. Discovered as a side-effect that several of the original 131 entries had wrong GUIDs (delegated-permission GUIDs labelled as app-permission GUIDs, e.g. AuditLog.Read.All `89fe6a52-…` is actually the *delegated* GUID, not the app GUID). The regenerated file replaces them with the canonical upstream values.
