# UAT Test Plan: Enhanced Azure AD Resource Display

## Goal
Verify that Azure AD resources (Users, Groups, Service Principals, Invitations) render correctly with semantic icons and richer summaries in GitHub and Azure DevOps PR comments.

## Artifacts
**Artifact to use:** `artifacts/azuread-enhancements-demo.md`

**Creation Instructions (if new artifact needed):**
- **Source Plan:** `examples/azuread-resources-demo.json`
- **Principal Mapping:** `examples/principal-mapping-azuread.json`
- **Command:** `tfplan2md --plan examples/azuread-resources-demo.json --principal-mapping examples/principal-mapping-azuread.json --output artifacts/azuread-enhancements-demo.md`
- **Rationale:** This plan contains a variety of Azure AD resources in different states to verify icon logic and summary composition.

## Test Steps
1. Run UAT using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- `azuread_user.jane`: Verify the summary shows `👤 Jane Doe`, `🆔 jane.doe@example.com`, and `📧 jane.doe@example.com` all within backticks. Check table icons for these fields.
- `azuread_group.platform_team`: Verify the member counts show `5 👤 1 👥 0 💻` (or matching counts from test data). Ensure the description is present below the names.
- `azuread_group_member.devops_jane`: Verify the relationship format `👥 Group` → `👤 Member`.
- `azuread_service_principal.terraform_spn`: Verify the `💻` icon is used for the display name.

**Expected Outcome:**
- All identity-related data points (names, IDs, emails) are prefixed with their corresponding semantic icon.
- Icons and text are both inside code formatting (backticks in MD, `<code>` in HTML).
- There is a non-breaking space between the icon and the text.
- No "unknown" or empty placeholders for missing optional attributes like `description`.

**Before/After Context:**
- Previously, these resources used default generic summaries that only showed the resource name. This enhancement makes identity changes much more informative and visually scan-friendly.
