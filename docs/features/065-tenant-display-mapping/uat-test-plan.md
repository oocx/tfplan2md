# UAT Test Plan: Tenant Display Name Mapping

## Goal
Verify that tenant display names and management group icons render correctly in GitHub and Azure DevOps PR comments, improving readability for multi-tenant environments.

## Artifacts
**Artifact to use:** `artifacts/tenant-mapping-uat.md`

**Creation Instructions (if new artifact needed):**
- **Source Plan:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-azuredevops-plan.json` (or a specialized multi-tenant plan)
- **Mapping File:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azure-mappings-extended.json`
- **Command:** `tfplan2md plan.json --principal-mapping azure-mappings-extended.json --output report.md`
- **Rationale:** Need a plan that includes `azurerm` and `azuread` resources matching the tenants and management groups in the mapping file.

## Test Steps
1. Run UAT using the `UAT Tester` agent (or `scripts/uat-run.sh`).
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- `azuread_user` resource or any resource with a `tenant_id` attribute.
- `azurerm_management_group` or scopes at the management group level.
- Root management group (e.g., from `azurerm_role_assignment`).

**Exact Attributes:**
- `tenant_id`
- `management_group_id`

**Expected Outcome:**
- **Tenants**: Verify they show 🏢 followed by the display name and then the GUID in backticks. Example: 🏢 `Contoso Corp (1234-5678)`.
- **Management Groups**: Verify they show 🗂️ followed by the display name in backticks. Example: 🗂️ `Production Workloads`.
- **Tenant Root**: Verify it shows 🗂️ Tenant `Contoso Corp` root.
- **Icons**: Ensure icons are OUTSIDE the backticks and there is a non-breaking space between the icon and the name.

**Before/After Context:**
- Before: Tenants showed as raw GUIDs ``1234-5678``. Management groups showed as raw names/IDs ``mg-prod`` without icons.
- After: Human-readable names are displayed with recognition icons, making it immediately clear which tenant or management group a resource belongs to.
