# UAT Test Plan: Custom Report Title

## Goal
Verify that the custom report title renders correctly in GitHub and Azure DevOps PR comments, including proper escaping of markdown special characters.

## Artifacts
**Artifact to use:** `artifacts/uat-custom-report-title.md`

**Creation Instructions:**
- **Source Plan:** `examples/comprehensive-demo/plan.json`
- **Command:** `dotnet run --project src/Oocx.TfPlan2Md -- examples/comprehensive-demo/plan.json --report-title "Drift Detection # Results [Production]" --output artifacts/uat-custom-report-title.md`
- **Rationale:** This command exercises the `--report-title` flag with a title containing characters that require escaping (`#`, `[`, `]`) to ensure they render literally in the H1 heading.

## Test Steps
1. Run the UAT simulation using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)
**Specific Resources/Sections:**
- The main report heading (H1).

**Exact Attributes:**
- The text of the H1 heading.

**Expected Outcome:**
- The heading should display exactly as `Drift Detection # Results [Production]`.
- The `#` should NOT be interpreted as a markdown heading (it's inside the H1).
- The `[` and `]` should NOT be interpreted as a markdown link.

**Before/After Context:**
- Previously, the title was hardcoded to "Terraform Plan Summary". This feature allows users to customize it, which is useful for differentiating reports in PR comments (e.g., "Drift Detection" vs "Plan Summary").
