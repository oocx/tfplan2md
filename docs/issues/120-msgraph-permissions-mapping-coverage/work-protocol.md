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

### Technical Writer
- **Date:** 2025-04-20
- **Summary:** Updated user-facing and contributor documentation to reflect the expanded Microsoft Graph application-permission mapping (131 → 673) and the new maintainer regeneration script. Made explicit the scope (Graph application permissions only; delegated scopes and non-Graph APIs are out of scope) so users are not surprised when a delegated-scope GUID still renders as a raw GUID. No code or tests were modified.
- **Artifacts Produced:**
  - `docs/features.md` — Expanded the Feature 116 "GUID Resolution" bullet to mention that all 673 well-known Graph **application** permission GUIDs are now resolved (with `Policy.ReadWrite.Authorization` as a second example), added an explicit "Scope of the mapping" bullet, and pointed maintainers at the new regeneration script.
  - `CONTRIBUTING.md` — Added a new "Maintaining Microsoft Graph App Role Mappings" section mirroring the existing "Maintaining Azure API Documentation Mappings" section: when to update, prerequisites, regeneration / dry-run / custom-source steps, script options, and the flat `{guid: name}` mapping file format. Linked from `docs/features.md`.
- **Problems Encountered:** None.

### Code Reviewer
- **Date:** 2025-04-20
- **Summary:** Reviewed the diff `origin/main..HEAD` (5 commits) against the maintainer's stated scope. **Verdict: Approve.** The fix is purely a data + tooling change; no behaviour changes to `MicrosoftGraphAppRoleResolver`, `AppRoleIdFormatter`, `MicrosoftGraphAppRolesRegistry`, or `AzureAdSummaryBuilder` (`git diff` confirms only `MicrosoftGraphAppRoles.json` under `src/Oocx.TfPlan2Md/`). Maintainer's exact scenario is pinned in two tests (`MicrosoftGraphAppRoleResolverTests.GetAppRole_PolicyReadWriteAuthorization_ResolvesToName`, `AppRoleIdFormatterTests.TryFormat_PolicyReadWriteAuthorizationGuid_ReturnsFormattedString`) plus a parameterised cross-section guarding 8 well-known GUIDs across Policy/User/Directory/Application/AuditLog/RoleManagement families.
- **Verification:**
  - **Test suite:** `scripts/test-with-timeout.sh --timeout-seconds 600 -- dotnet test --solution src/tfplan2md.slnx` → **1254 passed, 0 failed, 0 skipped** (3m 06s). Green.
  - **Data-file integrity:** 673 entries; all keys are valid lowercase GUIDs; sorted by GUID (matches existing convention); no duplicate `value`s; no malformed names; flat `{guid: value}` shape preserved so the existing registry loads it unchanged.
  - **Scope correctness (Graph application permissions only):** spot-checked `fb221be6-…` → `Policy.ReadWrite.Authorization`, `df021288-…` → `User.Read.All`, `7ab1d382-…` → `Directory.Read.All`, `06b708a9-…` → `AppRoleAssignment.ReadWrite.All`, `62a82d76-…` → `Group.ReadWrite.All` — all correct. Confirmed the previously incorrect delegated-GUID `89fe6a52-…` (mislabelled `AuditLog.Read.All` in the old hand-curated file) is gone and replaced by the canonical app GUID `b0afded3-3588-46d8-8b3d-9842eff778da`.
  - **Parser smoke test:** loaded `parse_app_permissions` and fed it a synthetic markdown sample with one app-only, one delegated-only (`-` in column 2), and one mixed permission — only the two with a real app GUID are extracted. Delegated-only entries correctly skipped.
  - **Snapshot compatibility:** the only real Microsoft Graph GUID in `azuread-app-role-assignment-plan.json` (`df021288-…` / `User.Read.All`) is preserved; the `azuread-app-role-assignment.md` snapshot is unchanged.
- **Generator script (`scripts/update-msgraph-app-roles.py`):** stdlib-only (argparse/json/re/urllib/pathlib), executable shebang `#!/usr/bin/env python3`, mode `0755`, mirrors the structure of `scripts/update-azure-api-mappings.py`, supports `--source`, `--output`, `--dry-run`, `--help`. Output is sorted by GUID for deterministic diffs and writes a trailing newline. Idempotent (rerun produces identical file). Sensible failure modes: explicit `SystemExit` on network errors, missing local source, non-dict existing JSON, and zero-permission parses (signals upstream format change).
- **Docs:** `docs/features.md` (Feature 116) accurately states the 673-entry coverage, the application-only scope (delegated and non-Graph APIs explicitly excluded), and links to the regeneration script. `CONTRIBUTING.md` adds a new "Maintaining Microsoft Graph App Role Mappings" section that mirrors the style of the adjacent "Maintaining Azure API Documentation Mappings" section (when-to-update, prerequisites, regenerate / dry-run / custom-source steps, options reference, output format).
- **Convention compliance:** conventional commit messages used throughout (`fix(azure): …`, `chore(scripts): …`, `docs: …`); branch name preserved (`copilot/map-policy-readwrite-authorization-role`); `CHANGELOG.md` not modified; no unrelated files touched.
- **Issues found:**
  - **Blockers:** none.
  - **Major:** none.
  - **Minor:** none.
  - **Suggestions (non-blocking, optional, for the Developer or a future change):**
    1. The pre-existing `00000000-0000-0000-0000-000000000000` → `"Default Access"` placeholder from the old hand-curated file is dropped by the regeneration (it's not a real Graph application permission and isn't in the upstream). This is the right call — but if a real-world plan ever surfaces that GUID, it'll now render as a raw GUID. Worth a one-line note in the script docstring (or in `docs/features.md`) acknowledging the all-zero sentinel is intentionally not mapped. Not worth blocking on.
    2. `scripts/update-msgraph-app-roles.py` `parse_app_permissions` discards the delegated GUID. If a future feature wants to cover delegated `oauth2_permission_grant` resources (currently explicitly out of scope per the maintainer), capturing it now in a side dict would save a parser revisit. Noted only as forward-looking — current scope is correct.
- **Decision:** **Approved.** No production code changes requested. Hand off to the **UAT Tester** as the next agent, since this fix changes user-visible markdown output (a previously raw GUID now renders as `🛡️ Policy.ReadWrite.Authorization (fb221be6-…)` in both the summary line and the attribute table).
- **Artifacts Produced:** This work-protocol entry. No standalone `code-review.md` produced (bug fix workflow; findings inlined here per the existing pattern).
- **Problems Encountered:** None.
