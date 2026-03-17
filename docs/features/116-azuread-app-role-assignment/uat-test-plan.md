# UAT Test Plan: Azure AD App Role Assignment Support

## Goal
Verify that `azuread_app_role_assignment` resources render correctly in GitHub and Azure DevOps PR comments, with human-readable summary lines showing resolved app role names, principal display names, and resource display names instead of raw GUIDs.

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)
**Purpose:** Focus testing on the specific changes in this feature — summary display and GUID resolution for `azuread_app_role_assignment` resources.

**Source Plan Path:** `docs/features/116-azuread-app-role-assignment/uat-plan.json`

**Rendered Output Path:** `docs/features/116-azuread-app-role-assignment/uat-plan.md`

**Plan Requirements:**
- **MUST be a real Terraform plan JSON** that exercises the feature
- **MUST contain `azuread_app_role_assignment` resources** with create and delete actions
- **MUST include at least one known `app_role_id` GUID** (e.g., `df021288-bdef-4463-88db-98f22de89214` for `User.Read.All`)
- **MUST include computed attributes** (`principal_display_name`, `resource_display_name`) for fallback testing
- **Rationale:** This plan exercises all three resolution paths (app role ID, principal, resource) and verifies both mapped and unmapped GUID display
- **Key Resources:**
  - `azuread_app_role_assignment.user_read_all` — creates a role with a known app role GUID
  - `azuread_app_role_assignment.unknown_role` — creates a role with an unknown GUID to verify fallback
- **Coverage:**
  - App role ID resolution (known GUID → permission name)
  - Unknown GUID fallback (raw GUID display)
  - Summary format: `{action} azuread_app_role_assignment <b><code>{name}</code></b> — <code>{role}</code> → <code>{principal}</code> on <code>{resource}</code>`
  - Detail table value formatting with 🛡️ icon
  - Computed attribute display

**Example Creation Command:**
```bash
# Generate the rendered output from the plan
tfplan2md docs/features/116-azuread-app-role-assignment/uat-plan.json > docs/features/116-azuread-app-role-assignment/uat-plan.md
```

### Comprehensive Demo (Regression Test)
**Purpose:** Ensure no unintended side effects in other areas.

**Artifact Path:**
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using `generate-demo-artifacts` skill.

## Test Steps
1. Developer creates `uat-plan.json` based on this specification
2. Developer generates `uat-plan.md` from the plan
3. Code Reviewer validates both files exist and are complete
4. UAT Tester uses `uat-plan.md` for testing
5. UAT will post TWO separate PR comments:
   - **Feature-Specific Report**: Tests the specific changes using `uat-plan.md`
   - **Comprehensive Demo**: Regression test for side effects
6. Verify both reports on GitHub and Azure DevOps

## Validation Instructions (Test Description)

**Feature-Specific Validation:**

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"):

**Specific Resources/Sections:**
- `azuread_app_role_assignment.user_read_all` — create action with a known Microsoft Graph app role
- `azuread_app_role_assignment.unknown_role` — create action with an unknown (fabricated) GUID

**Exact Attributes:**
- `app_role_id` — should display with 🛡️ icon and resolved name (e.g., `🛡️ User.Read.All (df021288-...)`) for known GUIDs
- `principal_object_id` — should display with principal icon when mapped, or raw GUID as fallback
- `resource_object_id` — should display with principal icon when mapped, or use computed `resource_display_name` as fallback

**Expected Outcome:**
- Summary line for known role: `➕ azuread_app_role_assignment **`user_read_all`** — `User.Read.All` → `{principal}` on `{resource}`
- Summary line for unknown role: shows raw GUID in the role position
- Detail table: `app_role_id` cell shows `🛡️ User.Read.All (df021288-bdef-4463-88db-98f22de89214)` for the known role
- Computed attributes (`principal_display_name`, `resource_display_name`) display their string values as-is

**Before/After Context:**
- **Before:** `azuread_app_role_assignment` resources would show a generic summary with no GUID resolution — all attributes display as raw GUIDs
- **After:** Summary line shows human-readable role name, principal name, and resource name with consistent summary icons; detail table formats `app_role_id` with 🛡️ and the resolved permission name

---

**Regression Validation:**

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

**Verify:**
- No unintended changes to existing resources (especially `azurerm_role_assignment` which uses the sibling `IRoleDefinitionResolver` pattern)
- Existing Azure AD resources (`azuread_group`, `azuread_user`, etc.) render identically to before
- All sections render correctly (summaries, details, static analysis)
- No new warnings or error markers in the output
