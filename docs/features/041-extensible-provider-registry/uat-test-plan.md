# UAT Test Plan: Extensible Provider Registry System

## Goal
Verify that the new extensible provider registry system correctly renders icons and formatted values in GitHub and Azure DevOps PR comments, and that Azure AD and Azure DevOps providers have proper icon representation.

## Artifacts
**Artifact to use:** `artifacts/extensible-registry-uat.md`

**Creation Instructions (if new artifact needed):**
- **Source Plan:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-azuredevops-plan.json` (for Azurerm/AzDO) and `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuread-user-plan.json` (for Azure AD).
- **Command:** `tfplan2md plan --plan test.json --output artifacts/extensible-registry-uat.md`
- **Rationale:** We need to verify that resources from all four providers (AzureRM, AzApi, AzureAD, AzureDevOps) render with their respective icons and custom formatting.

## Test Steps
1. Run UAT using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

### 1. Provider Iconography & Identifiers
Verify that the following resource types display their specific icons in the summary and detail view:
- **Azure AD User**: Verify `user_principal_name` displays with the 🆔 icon and `mail` with the 📧 icon.
- **Azure AD Group**: Verify `display_name` (or resource name) displays with the 👥 icon.
- **Azure AD Service Principal**: Verify `display_name` displays with the 💻 icon.
- **Azure DevOps Variable Group**: Verify variables are listed clearly.

### 2. Value Formatting
Verify that complex identifiers are formatted correctly:
- **Azure Resource IDs**: Should be shortened to just the resource name where possible (e.g., `rg-name` instead of full path), using the 📁 icon for resource groups.
- **Subscription IDs**: Should display with the 🔑 icon.

### 3. Semantic Icons (NSG / Firewall)
- **Action/Access**: Verify `Allow` shows ✅ and `Deny` shows ⛔.
- **Direction**: Verify `Inbound` shows ⬇️ and `Outbound` shows ⬆️.
- **Protocols**: Verify `TCP` (🔗), `UDP` (📨), `ICMP` (📡), and `Any` (✳️) icons.

**Expected Outcome:**
All icons should be followed by a non-breaking space (documented in `SemanticFormatting.cs` as `\u00A0`) ensuring they stay attached to the text. No raw regex patterns or registration errors should be visible in the output.
