# UAT Test Plan: Terraform Show Output Approximation Tool

## Goal
Verify that the `TerraformShowRenderer` tool generates output that is visually indistinguishable from real `terraform show` output when viewed in a terminal or code block.

## Artifacts
**Artifact to use:** `artifacts/uat-terraform-show-approximation.txt`

**Creation Instructions:**
- **Source Plan:** `src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.json`
- **Command:** `dotnet run --project src/tools/Oocx.TfPlan2Md.TerraformShowRenderer -- -i src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.json -o artifacts/uat-terraform-show-approximation.txt`
- **Rationale:** This plan includes a replacement (`-/+`) operation, which is one of the most complex visual elements to render correctly with ANSI colors.

## Test Steps
1. Run the UAT simulation using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)
**Specific Resources/Sections:**
- Legend section at the top.
- `azuredevops_build_definition.example` resource (replacement).
- Plan summary line at the bottom.

**Exact Attributes:**
- Check the `-/+` marker for `azuredevops_build_definition.example`. The `-` should be red and the `+` should be green.
- Verify that the resource header `# azuredevops_build_definition.example must be replaced` has "replaced" in bold and red.

**Expected Outcome:**
- The output should look like a real Terraform plan.
- ANSI colors should be correctly applied to action symbols and changed values.
- Indentation should match the baseline `src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.txt`.

**Before/After Context:**
- This tool is new. It allows us to show "Before" examples on the website without needing the original binary `.tfplan` files.
