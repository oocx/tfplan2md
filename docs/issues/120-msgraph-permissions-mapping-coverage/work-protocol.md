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
