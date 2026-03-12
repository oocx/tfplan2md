# Issue: Missing Separate NSG Rule In Generated Report

## Problem Description

When tfplan2md processes certain plans with network security groups, the generated report does not render the created NSG rule `MyRuleName`.

The generated markdown still reports one added `azurerm_network_security_rule` in the summary and annotates the parent NSG with `➕ 1 security rules`, but the rule row itself is missing from the rendered `Security Rules` table.

## Steps to Reproduce

1. Run tfplan2md against `/home/re/git/dca/Dca-Lv3-Vending-Machine/instance-infrastructure/terraform/plan.json`.
2. Inspect the generated markdown report.
3. Compare the report with the Terraform `resource_changes` entries for NSGs and NSG rules.

## Expected Behavior

The generated report should include a `Security Rules` table row for the created rule `MyRuleName` under the parent NSG `my-nsg`.

## Actual Behavior

The generated report contains:

- Summary: `➕ Add | 1 | 1 azurerm_network_security_rule`
- Parent NSG summary suffix: `➕ 1 security rules`
- Rendered table rows: only 9 unchanged rules for `my-nsg`

The created rule is therefore counted but not shown.

## Root Cause Analysis

### Affected Components

- `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs`

### What's Broken

The current rendering pipeline has two separate sources of truth for `azurerm_network_security_group` output:

1. `ReportModelBuilder.MergeParentChildRelationships()` merges separate `azurerm_network_security_rule` resources into the parent NSG's `ChildResourceGroups` and removes those child resources from the top-level display list.
2. The specialized `NsgRenderer` does **not** render `ChildResourceGroups`. Instead, it rebuilds the `Security Rules` table exclusively from `change.ResourceChange` via `NetworkSecurityGroupViewModelFactory.Build(...)`.

For this plan, the parent resource `module.monitoring_vnet[0].azurerm_network_security_group.this["pep"]` is `no-op`, while the created child resource is:

- `module.monitoring_vnet[0].azurerm_network_security_rule.this["prtg-http-push-inbound"]`
- Action: `create`
- Name: `MyRuleName`
- NSG: `my-nsg`

Because the parent NSG state does not itself contain a changed inline `security_rule` entry for that separate child resource, `NsgRenderer` never renders the new rule row. At the same time, the merge stage already removed the child resource from the top-level display list, so the change becomes invisible in the final report body.

### Why It Happened

This regression was introduced after `v1.29.0`.

Evidence:

- `v1.29.0` predates `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs` entirely.
- The current specialized AzureRM renderer file was introduced in commit `47980b8c` (`refactor: remove Scriban and migrate to pure C# rendering`).
- Parent-child grouping for no-op parents exists in the current pipeline, but the specialized NSG renderer bypasses that merged data and renders from parent state only.

This explains why:

- the summary is correct,
- the parent summary suffix is correct,
- but the rendered rule table is incomplete.

## Similar Lost-Change Scenarios

### Confirmed Similar Cases

Any `azurerm_network_security_group` rendered by the specialized `NsgRenderer` can lose separate child-rule changes when those changes exist only as `azurerm_network_security_rule` resources and not as inline parent-state deltas.

This affects at least these child actions:

- Separate child `create`
- Separate child `update`
- Separate child `delete`
- Mixed inline + separate child scenarios

The loss mode is the same: the merge stage counts and attaches the child rows, but the specialized renderer ignores them.

### Other Parent-Child Types Checked

The currently registered parent-child parent resource types are:

- `azuread_group`
- `azurerm_virtual_network`
- `azurerm_route_table`
- `azurerm_network_security_group`
- `azurerm_dns_zone`
- `azurerm_private_dns_zone`
- `azuredevops_group`
- `azuredevops_team`

Among these, `azurerm_network_security_group` is the only confirmed parent-child type with a specialized renderer in the current pipeline that bypasses `ChildResourceGroups`.

The others currently delegate to the default renderer, which does render merged child rows.

### Related Historical Issue

`docs/issues/088-no-op-parent-hides-child-changes/analysis.md` fixed a broader filtering bug where no-op parents with changed children could disappear entirely. That fix preserved the parent resource in the display list, but it did not address the specialized NSG renderer path described here.

## Suggested Fix Approach

High-level options:

- Update `NsgRenderer` to render merged `ChildResourceGroups` when they exist, instead of rebuilding the table solely from `NetworkSecurityGroupViewModelFactory`.
- Or merge separate child rule actions into the NSG-specific view model before rendering.

The safer direction appears to be aligning `NsgRenderer` with the merged `ChildResourceGroups`, because that uses the same canonical parent-child merge result already used for summaries and filtering.

## Related Tests

- [ ] Add a regression test with a no-op NSG parent plus a separate created `azurerm_network_security_rule`
- [ ] Add coverage for separate child `update`
- [ ] Add coverage for separate child `delete`
- [ ] Verify that mixed inline + separate child scenarios render all rows

## Additional Context

- Reproduced with tfplan2md `1.37.0`
- The missing rule payload from the plan is:
  - Name: `MyRuleName`
  - Direction: `Inbound`
  - Access: `Allow`
  - Protocol: `Tcp`
  - Destination ports: `5050-5051`
  - Source prefixes: `127.1.0.0/22`
  - NSG: `my-nsg`
- Regression introduction point: commit `47980b8c` (`refactor: remove Scriban and migrate to pure C# rendering`)
