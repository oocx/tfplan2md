# UAT Test Plan: Azure Display Enhancements

## Goal
Validate Azure display enhancements (role and scope enrichment, DNS/PIM/Policy summaries) in GitHub and Azure DevOps PR comments.

## Artifacts
**Artifact to use:**
- Azure DevOps: `artifacts/comprehensive-demo.md`
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`

**Creation Instructions (if new artifact needed):**
- **Source Plan:** `examples/comprehensive-demo/plan.json`
- **Principal Mapping:** `examples/comprehensive-demo/demo-principals.json`
- **Command:** `scripts/generate-demo-artifacts.sh`
- **Rationale:** The comprehensive demo includes the DNS, PIM, and role policy resources needed for validation.

## Test Steps
1. Run UAT using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- `module.network.azurerm_private_dns_a_record.app`: Summary shows the FQDN `api.contoso.local`.
- `module.security.azurerm_pim_eligible_role_assignment.ops`: Summary shows "Assign `Owner` to `Jane Doe (User)`".
- `module.security.azurerm_role_management_policy.ops`: Summary shows "`Reader` in management group `mg-root`".

**Exact Attributes:**
- `role_definition_id`: Shows role names (e.g., `Reader`, `Owner`) instead of GUIDs.

**Expected Outcome:**
Summaries are concise and readable, with role names resolved and scope strings formatted consistently.

**Before/After Context:**
Previously, these resources used generic summaries with raw IDs. Now, summaries show FQDNs, role names, and readable scope text.
