# UAT Test Plan: Parent-Child Resource Grouping

## Goal
Verify that parent-child resources (like Azure AD Groups or Azure DevOps Teams) render correctly in GitHub and Azure DevOps PR comments, with children displayed in inline tables rather than separate sections.

## Artifacts
**Artifact to use:** `artifacts/parent-child-demo.md`

**Creation Instructions:**
- **Source Plan:** `TestData/parent-child-integration-test.json` (A mocked plan containing azuread and azuredevops targets)
- **Command:** `tfplan2md --plan TestData/parent-child-integration-test.json --output artifacts/parent-child-demo.md`
- **Rationale:** This plan contains mixed management scenarios and multiple child groups to exercise the generic rendering logic.

## Test Steps
1. Run UAT using the `UAT Tester` agent (or manual script).
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions

### 1. Azure AD Group (`azuread_group.admins`)
- **Verification**: Ensure there is ONLY one section for the group.
- **Table**: Check the "Members" table. It should contain rows for both `members` (inline) and `azuread_group_member` (separate).
- **Resource Column**: Separate members should show their full Terraform resource address. Inline members should show `members` (the attribute name).
- **Mixed Management**: Look for a warning message: "This resource has children managed both inline and as separate resources."

### 2. Azure DevOps Team (`azuredevops_team.platform_team`)
- **Verification**: Should show TWO tables: "Administrators" and "Members".
- **Formatting**: Values should be readable (e.g. member names or descriptors), not just raw JSON.

### 3. Change Summary
- **Verification**: Check the parent resource header line (e.g. `➕ azuread_group.admins | ➕ 3 members`). It should correctly count the total number of child changes.

### 4. Code Analysis Findings
- **Verification**: If a finding is mapped to a member resource, it should appear right under the parent group's attributes, with a prefix indicating the original resource address.

### 5. Layout (Cross-platform)
- **GitHub**: Tables should have proper headers and markers.
- **Azure DevOps**: Tables should be rendered cleanly (no broken markdown).
