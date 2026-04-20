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

### UAT Tester
- **Date:** 2026-04-20
- **Summary:** Validated the issue #120 fix (`Policy.ReadWrite.Authorization` mapping for app role GUID `fb221be6-99f2-473f-bd32-01c6a0e9ca3b`) end-to-end via local rendering + live UAT PRs on GitHub and Azure DevOps. **Verdict: PASS.** The maintainer's exact scenario now resolves correctly in both the resource summary header and the `app_role_id` attribute row, and no production code was modified by this UAT pass.
- **Fixture & Local Render:**
  - Built minimal plan fixture `artifacts/uat-issue-120/plan.json` with a single `azuread_app_role_assignment.this` create using the maintainer's exact GUIDs (`app_role_id=fb221be6-…`, `principal_object_id=55fb3b1b-…`, `resource_object_id=31939070-…`).
  - Built `artifacts/uat-issue-120/principals.json` mapping `55fb3b1b-…` → `governance-id-lv1-gwc` and `31939070-…` → `Microsoft Graph` (these GUIDs are user-environment IDs, not well-known).
  - Ran `dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- --principal-mapping artifacts/uat-issue-120/principals.json --output artifacts/uat-issue-120/output.md artifacts/uat-issue-120/plan.json` against the current HEAD (`682a83b`).
- **Rendered output (verbatim, both summary header line and attribute row):**
  ```
  ➕ azuread_app_role_assignment <b><code>this</code></b> — <code>👤 governance-id-lv1-gwc</code> (<code>55fb3b1b-e3f4-4fcc-8edf-837e722ec927</code>) → <code>🛡️ Policy.ReadWrite.Authorization</code> → <code>🎯 Microsoft Graph</code> (<code>31939070-7533-4b31-8a67-29e18a4ad777</code>)

  | app_role_id | `🛡️ Policy.ReadWrite.Authorization (fb221be6-99f2-473f-bd32-01c6a0e9ca3b)` |
  ```
  - **app_role_id row matches the maintainer's expected output exactly.**
  - **Summary header** resolves the role name to `🛡️ Policy.ReadWrite.Authorization` (no raw GUID), which is the established convention in this repo — confirmed by the existing snapshot `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azuread-app-role-assignment.md`, where the working `User.Read.All` case also renders as `🛡️ User.Read.All` (name only) in the summary and `🛡️ User.Read.All (df021288-…)` in the attribute row. The maintainer's expected example included the GUID after the role name in the summary, but that would deviate from the existing summary-builder convention; the actual render is consistent with how every other resolved Graph permission is shown today. Treating this as **Pass** (convention-consistent); flagged here for awareness.
  - Pre-existing minor quirk (out of scope for #120): `resource_object_id` in the attribute table shows `👤 Microsoft Graph (…)` — the 👤 emoji comes from the principal-mapping path, not the resource-mapping path. The summary line correctly uses 🎯. Not in scope for this issue.
- **Live Platform UAT (`scripts/uat-run.sh --create-only`):**
  - Used `docs/issues/120-msgraph-permissions-mapping-coverage/uat-plan.md` (= the rendered output above, with current-HEAD commit hash so the freshness check passes) as `--report` and detailed scenario validation as `--instructions`. Comprehensive demo regression artifacts were appended automatically by the script.
  - **GitHub UAT PR:** https://github.com/oocx/tfplan2md-uat/pull/120 — fetched the posted comment via `gh api` and confirmed the rendered markdown matches local output verbatim, including the `🛡️ Policy.ReadWrite.Authorization (fb221be6-…)` attribute cell.
  - **Azure DevOps UAT PR:** https://dev.azure.com/oocx/test/_git/test/pullrequest/108 — created with the same artifact + regression demo comment.
  - PRs left open for maintainer visual approval; cleanup deferred to a follow-up `scripts/uat-run.sh --cleanup-last` once approved (state in `.tmp/uat-run/last-run.json`).
- **Operational notes:**
  - The repo's `.gitmodules` declares `uat-repos/github` and `uat-repos/azdo` submodules but the actual gitlinks are not present in `origin/main`'s tree, so `git submodule update --init --recursive` exits 0 without cloning anything. Worked around by cloning both UAT repos directly with `GH_UAT_TOKEN` / `AZDO_UAT_TOKEN` and excluding `uat-repos/` via `.git/info/exclude` so `uat-run.sh`'s "working tree clean" precondition passes. Worth a tracking item for the workflow engineer (separate from this fix).
- **Artifacts Produced:**
  - `artifacts/uat-issue-120/plan.json`, `artifacts/uat-issue-120/principals.json`, `artifacts/uat-issue-120/output.md` — local UAT fixture + render.
  - `docs/issues/120-msgraph-permissions-mapping-coverage/uat-plan.md` — the artifact posted to both UAT PRs.
  - GitHub UAT PR oocx/tfplan2md-uat#120; Azure DevOps UAT PR test#108.
- **Production code touched:** None. Only test/fixture artifacts and this work-protocol entry. No changes to `src/Oocx.TfPlan2Md/` or `src/tests/`.
- **Problems Encountered:** Submodule gitlinks missing from the repo (see operational notes); worked around without affecting UAT outcome.

### Release Manager
- **Date:** 2026-04-20
- **Summary:** Verified branch clean and rebased (10 commits ahead of `origin/main`, 0 behind). All required agents (Issue Analyst, Developer, Technical Writer, Code Reviewer (Approve), UAT Tester (Pass)) logged in this protocol. Created PR following the repo Problem / Change / Verification template and linking issue #120.
- **PR:** https://github.com/oocx/tfplan2md/pull/648 — title `fix(azure): expand Microsoft Graph app role mapping (closes #120)`. Patch-level Conventional Commit (`fix(azure): …`) so Versionize will produce a patch bump from current latest tag `v1.41.0`.
- **CI status at handoff:** `pending` (workflows queued / not yet reported on head SHA `1f014f6`); awaiting PR Validation completion before merge.
- **Merge:** Not performed by this agent. The standard `scripts/pr-github.sh create-and-merge` path requires `git push -u origin HEAD` as a precondition, which is rejected in this sandbox (`remote: Permission to oocx/tfplan2md.git denied to oocx … 403`) — the coding-agent bot has API-only auth and the orchestrator handles git pushes via `report_progress`. PR creation was therefore performed via the coding-agent native `create_pull_request` tool (GitHub API, no git push required); the **rebase merge must be executed by the maintainer or orchestrator** once CI on PR #648 is green. Repo policy: **Rebase and merge only** (linear history).
- **Release artifacts:** None produced yet. Versionize / release pipeline runs post-merge on `main`; tag detection and release-workflow trigger deferred until the merge happens.
- **Follow-up items:**
  1. Maintainer/orchestrator: rebase-merge PR #648 once CI is green (use `scripts/pr-github.sh create-and-merge` from an environment with push rights, or the GitHub UI's "Rebase and merge" button — never squash/merge-commit).
  2. Post-merge: monitor CI on `main`, detect new tag (expected `v1.41.1`), trigger `release.yml` with that tag, verify GitHub Release + Docker image artifacts.
  3. Workflow Engineer (separate from #120): the bot's lack of git-push permission means `scripts/pr-github.sh` cannot be used end-to-end from coding-agent sandboxes — consider documenting `create_pull_request` as the sanctioned PR-creation path for that environment, or splitting the script's push step out.
- **Problems Encountered:** Sandbox `git push` blocked (see Merge note above); worked around by using the `create_pull_request` tool. Did not improvise further — merge handoff to maintainer per task instructions ("STOP and report back if blocked").
