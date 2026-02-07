# UAT Test Plan: Azure Display Enhancements

## Goal
Verify that Azure subscriptions, management groups, roles, and resource-specific summaries (DNS/PIM/Policies) render correctly in GitHub and Azure DevOps PR comments with human-readable names.

## Artifacts
**Artifact to use:** `artifacts/azure-display-enhancements-demo.md`

**Creation Instructions (if new artifact needed):**
- **Source Plan:** `examples/azure-display-enhancements.json`
- **Command:** `tfplan2md examples/azure-display-enhancements.json --principal-mapping examples/azure-mappings-extended.json --output artifacts/azure-display-enhancements-demo.md`
- **Rationale:** This plan contains all resources targeted by the enhancements (DNS, PIM, Role Policies, Subscriptions).

## Test Steps
1. Run UAT using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- **Subscription Display**: In any `azurerm` resource (e.g., `azurerm_resource_group.example`), verify that the subscription in the title or attribute table renders as `Production (d1828a48-fced-4ea2-b2ec-4b9623f327fd)` instead of just the GUID.
- **PIM Assignments**: The `azurerm_pim_eligible_role_assignment` resource should have a summary like: `### ➕ azurerm_pim_eligible_role_assignment "example": Assign "Owner" to "Jane Doe"`.
- **Private DNS**: The `azurerm_private_dns_a_record` resource should show the FQDN in the summary: `### ➕ azurerm_private_dns_a_record "example": record1.contoso.local`.
- **Role Management Policies**: Verify `azurerm_role_management_policy` renders as `"Contributor" in resource group "foo" of subscription "Production (...)"`.
- **Management Groups**: Verify that `azurerm_management_group` references show the Display Name (e.g., `Corporate IT`) instead of the ID (e.g., `mg-corp-it`).

**Exact Attributes:**
- `role_definition_id`: Should show names like `Reader`, `Contributor`, or custom role names.
- `subscription_id`: Should show `Name (GUID)`.

**Expected Outcome:**
The report should be significantly more readable, with most hex strings and GUIDs replaced by names provided in the mapping file.

**Before/After Context:**
Previously, these resources only showed GUIDs or truncated paths. Now, they provide rich contextual names, making it immediate to understand which environment and permissions are being changed.
