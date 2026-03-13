# UAT Test Plan: Missing Separate NSG Rule In Generated Report

## Goal

Verify that NSG security-rule changes merged into a parent `azurerm_network_security_group` remain visible in the rendered markdown on both GitHub and Azure DevOps, even when the parent NSG itself is `no-op`.

## Artifacts

### Feature-Specific Test Artifact

**Purpose:** Exercise the exact regression and the closely related failure modes identified in the issue analysis.

**Source Plan Path:** `docs/issues/112-missing-nsg-rule-report/uat-plan.json`

**Rendered Output Path:** `docs/issues/112-missing-nsg-rule-report/uat-plan.md`

**Plan Requirements:**

- **MUST** be a real Terraform plan JSON that produces NSG output through the normal tfplan2md pipeline.
- **MUST** include a no-op NSG parent with a separate created rule whose rendered row would disappear with the broken implementation.
- **MUST** include separate child `update` coverage.
- **MUST** include separate child `delete` coverage.
- **MUST** include a mixed inline plus separate rule scenario.
- **MUST** keep the examples small enough that reviewers can validate row visibility quickly in a PR comment.

**Rationale:**

This issue is a rendering-contract regression. Reviewers need a compact artifact that makes row loss obvious without reading raw Terraform JSON or scanning a large comprehensive demo.

**Recommended Focused Scenarios In The Plan:**

- `nsg-separate-create`: no-op NSG plus one separate created rule
- `nsg-separate-update`: no-op NSG plus one separate updated rule
- `nsg-separate-delete`: no-op NSG plus one separate deleted rule
- `nsg-mixed-sources`: NSG with inline rule data and at least one separate child rule

**Expected Named Rules To Include:**

- `MyRuleName` for the create scenario
- One explicit update rule with a changed port, prefix, or description
- One explicit delete rule
- One mixed-source separate rule whose row can be clearly distinguished from inline rows

**Example Creation Command:**

```bash
tfplan2md docs/issues/112-missing-nsg-rule-report/uat-plan.json --output docs/issues/112-missing-nsg-rule-report/uat-plan.md
```

### Comprehensive Regression Artifact

**Purpose:** Confirm there are no unintended side effects in other rendering areas after fixing NSG table population.

**Artifact Path:**

- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Additional Helpful Regression Artifact:**

- `artifacts/azure-rm-batch-2-feature-test.md` if the Developer regenerates it during implementation, because it already includes separate-rule and mixed-rule NSG scenarios.

## Test Steps

1. Developer creates `docs/issues/112-missing-nsg-rule-report/uat-plan.json` with the focused NSG scenarios above.
2. Developer generates `docs/issues/112-missing-nsg-rule-report/uat-plan.md` from that plan.
3. Code Reviewer verifies both files exist and match this plan.
4. UAT Tester posts the focused artifact as the feature-specific PR comment.
5. UAT Tester posts the comprehensive demo as the regression PR comment.
6. Maintainer verifies both comments in GitHub and Azure DevOps.

## Validation Instructions

### Feature-Specific Validation

In the **feature-specific report** using `docs/issues/112-missing-nsg-rule-report/uat-plan.md`:

#### Check 1: Separate Create Under No-Op Parent

- Locate the NSG section for the create scenario.
- Verify the `Security Rules` table contains a row for `MyRuleName`.
- Verify the row shows `Inbound`, `Allow`, `Tcp`, destination ports `5050-5051`, and source prefix `127.1.0.0/22`.
- Verify the parent NSG summary indicates one added security rule.
- Verify there is no separate top-level `azurerm_network_security_rule` section for the same rule.

#### Check 2: Separate Update Under No-Op Parent

- Locate the NSG section for the update scenario.
- Verify the `Security Rules` table contains the updated rule row.
- Verify at least one changed value is visibly different, such as port, address prefix, or description.
- Verify the update is shown under the parent NSG rather than disappearing after merge.

#### Check 3: Separate Delete Under No-Op Parent

- Locate the NSG section for the delete scenario.
- Verify the `Security Rules` table contains the deleted rule row with delete semantics.
- Verify the row is still visible even though the child resource is removed from the top-level display list.

#### Check 4: Mixed Inline Plus Separate Sources

- Locate the mixed-source NSG section.
- Verify the table contains all expected inline and separate-child rows.
- Verify the separate-child row is not omitted simply because the parent also contains inline rule data.
- Verify there are no duplicate rows for the same logical rule.

### Regression Validation

In the **comprehensive demo** comment:

**Verify:**

- Existing non-NSG rendering remains unchanged.
- Existing inline-only NSG sections still render correctly.
- Summary totals and per-resource details remain aligned.
- No orphan top-level `azurerm_network_security_rule` sections appear where the parent-child merge should inline them.

## Success Criteria

- [ ] The focused artifact clearly shows separate child `create`, `update`, `delete`, and mixed scenarios.
- [ ] Each separate child rule is rendered under its parent NSG `Security Rules` table.
- [ ] No rule change is counted in summaries without also being visible in the rendered body.
- [ ] No duplicate or orphan top-level NSG rule sections appear.
- [ ] GitHub and Azure DevOps both render the tables clearly.
- [ ] The comprehensive demo shows no unintended regressions.

## Reviewer Notes

- This issue is specifically about a mismatch between the canonical merged report model and the specialized NSG renderer.
- The most important review question is whether the rendered body now reflects the same child-rule changes already reflected in summaries.
- If the feature-specific artifact does not make missing rows obvious at a glance, it is not focused enough and should be tightened before UAT.
