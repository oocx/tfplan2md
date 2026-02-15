# UAT Test Plan: Azure Display Enhancements

## Goal
Validate Azure display enhancements (role and scope enrichment, DNS/PIM/Policy summaries) in GitHub and Azure DevOps PR comments using a focused artifact.

## Artifacts
**Artifact to use:**
- Azure DevOps: `artifacts/azure-display-enhancements-demo.md`
- GitHub: `artifacts/azure-display-enhancements-demo-simple-diff.md`

**Creation Instructions (if new artifact needed):**
- **Source Plan:** `examples/azure-display-enhancements.json`
- **Principal Mapping:** `examples/azure-mappings-extended.json`
- **Commands:**
	- `dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- examples/azure-display-enhancements.json --principal-mapping examples/azure-mappings-extended.json --output artifacts/azure-display-enhancements-demo.md`
	- `dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- examples/azure-display-enhancements.json --principal-mapping examples/azure-mappings-extended.json --render-target github --output artifacts/azure-display-enhancements-demo-simple-diff.md`
- **Rationale:** This plan isolates DNS, PIM, and role policy resources to keep UAT focused on the new feature.

## Test Steps
1. Run UAT using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- `azurerm_private_dns_a_record.example`: Summary shows the FQDN `record1.contoso.local`.
- `azurerm_pim_eligible_role_assignment.example`: Summary shows "Assign `Owner` to `Jane Doe`".
- `azurerm_role_management_policy.example`: Summary shows "`Reader` in management group `mg-root`".
- Subscription display: `scope` should render as `Production (sub-123)` instead of the raw ID.

**Exact Attributes:**
- `role_definition_id`: Shows role names (e.g., `Reader`, `Owner`) instead of GUIDs.

**Expected Outcome:**
Summaries are concise and readable, with role names resolved and scope strings formatted consistently.

**Before/After Context:**
Previously, these resources used generic summaries with raw IDs. Now, summaries show FQDNs, role names, and readable scope text.
