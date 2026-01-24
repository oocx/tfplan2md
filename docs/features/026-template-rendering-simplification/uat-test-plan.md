# UAT Test Plan: Template Rendering Simplification

## Goal
Verify that the refactored template rendering system produces output that is visually equivalent to the previous version, specifically for complex resources like Network Security Groups, Firewalls, and Role Assignments.

## Artifacts
**Artifact to use:** `artifacts/comprehensive-demo.md`

**Creation Instructions:**
- **Source Plan:** `src/tests/Oocx.TfPlan2Md.Tests/TestData/comprehensive-demo.json`
- **Command:** `dotnet run --project src/Oocx.TfPlan2Md -- render --plan src/tests/Oocx.TfPlan2Md.Tests/TestData/comprehensive-demo.json --out artifacts/comprehensive-demo.md`
- **Rationale:** This plan exercises all resource-specific templates (NSG, Firewall, Role Assignment) and the default rendering logic, making it the best candidate for regression testing.

## Test Steps
1. Run the UAT simulation using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- **Network Security Group**: `azurerm_network_security_group.web`
- **Firewall**: `azurerm_firewall_network_rule_collection.example`
- **Role Assignment**: `azurerm_role_assignment.example`

**Exact Attributes:**
- **NSG Rules**: Verify the table columns (Name, Priority, Access, Protocol, Source, Destination) are aligned and icons (✅/❌) are present.
- **Firewall Rules**: Verify the "Before/After" display for source/destination addresses and ports.
- **Role Assignments**: Verify the principal and role names are displayed with icons (👤/👥/🔑).

**Expected Outcome:**
- The output should be **visually identical** to the version before the refactoring.
- No "anchor comments" (e.g., `<!-- tfplan2md:resource-start -->`) should be visible in the raw markdown or rendered output.
- Table formatting must be perfect (no broken rows or literal `<br>` tags where they shouldn't be).
- Heading spacing should be consistent.

**Before/After Context:**
- **Before**: The system used a fragile two-pass rendering with regex replacement of HTML anchors. This often led to whitespace issues and was hard to maintain.
- **After**: The system uses direct template dispatch. The output should look the same, but the underlying mechanism is much cleaner and more robust. This refactoring is a "no-op" for the end-user but a major improvement for maintainability.

## Success Criteria
- [ ] Output renders correctly in GitHub Markdown.
- [ ] Output renders correctly in Azure DevOps Markdown.
- [ ] All resource-specific formatting is preserved.
- [ ] No regressions in icon display or table alignment.
- [ ] No visible HTML anchor comments in the output.
