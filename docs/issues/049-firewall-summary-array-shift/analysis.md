# Issue: Firewall rule collection summary overcounts changes on array index shifts

## Problem Description

When a firewall rule is removed from the start of the `rule` array, Terraform reports many index-based attribute diffs (e.g., `rule[0]...`, `rule[1]...`) even when most rules are semantically unchanged.

tfplan2md already renders the *detailed* firewall rule diff semantically (matching rules by `name`), but the one-line `<summary>` still uses the raw flattened attribute changes list, producing a misleading “N🔧 … +more” summary.

## Steps to Reproduce

1. Generate a report from the provided plan:

   ```bash
   cd /home/mathias/git/tfplan2md
   dotnet run --project src/Oocx.TfPlan2Md -- examples/firewall-rules-demo/plan2.json --output .tmp/firewall-plan2.md
   ```

2. Inspect the generated summary line (from the markdown output):

  ```
  <summary>… | 42🔧 rule[0].destination_ports[0], rule[0].name, rule[0].source_addresses[1], +39 more</summary>
  ```

3. In the same report, inspect the “Rule Changes” table for that resource.
  It shows only:
  - `allow-https` modified (source address list expanded)
  - `allow-http` deleted

## Expected Behavior

The firewall rule collection `<summary>` should reflect the semantic rule-level changes (e.g., “1 rule modified, 1 rule deleted”) rather than the index-shift noise from Terraform’s flattened diff.

## Actual Behavior

The firewall rule collection `<summary>` includes a large `N🔧` attribute count and index-based attribute names such as:

- `rule[0].destination_ports[0]`
- `rule[0].name`
- `rule[0].source_addresses[1]`

…even though the detailed “Rule Changes” table shows only:

- `allow-https` modified (source address list expanded)
- `allow-http` deleted

## Root Cause Analysis

### Affected Components

- Summary rendering uses `ResourceChangeModel.ChangedAttributesSummary` in:
  - [src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs#L23-L70](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs#L23-L70)

- `ChangedAttributesSummary` is computed from raw flattened `AttributeChanges` in:
  - [src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs#L18-L49](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs#L18-L49)

- The firewall semantic diff logic already exists and matches rules by name (not by array index):
  - [src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallNetworkRuleCollectionViewModelFactory.cs#L36-L59](../../../src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallNetworkRuleCollectionViewModelFactory.cs#L36-L59)
  - Specifically, name-based matching for modified/unchanged:
    - [src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallNetworkRuleCollectionViewModelFactory.cs#L160-L188](../../../src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallNetworkRuleCollectionViewModelFactory.cs#L160-L188)

### What’s Broken

`ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary(...)` currently counts the number of flattened attribute keys (via `JsonFlattener`) that differ between before/after. For list/array fields this produces a large number of changes whenever an element is removed and the remaining items shift indices.

For `azurerm_firewall_network_rule_collection`, tfplan2md already builds a semantic, rule-name-based view model (`ResourceChangeModel.FirewallNetworkRuleCollection`), but the summary computation ignores it and always uses `AttributeChanges`.

### Why It Happened

The “rich summary” feature is implemented generically for all resources. Provider-specific view models were added later to simplify templates, but the summary logic was not updated to take advantage of those view models.

## Suggested Fix Approach (High Level)

- Make `ChangedAttributesSummary` resource-aware for `azurerm_firewall_network_rule_collection` updates.

  Options:

  1. **Compute `ChangedAttributesSummary` from `model.FirewallNetworkRuleCollection.RuleChanges`** when present.
     - Count rule-level changes excluding unchanged (`⏺️`).
     - Prefer listing a few rule names (e.g., first 1–3) and then `+N more`, similar to current truncation behavior.

  2. **Move the logic into the firewall factory** by allowing it to set a precomputed summary field.
     - This likely requires changing `ReportModelBuilder` to not overwrite `ChangedAttributesSummary` after `ApplyViewModel(...)`.

- Add a regression test covering the “remove rule[0] causing index-shift” scenario.
  - Suggested place: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderSummaryTests.cs`.
  - Suggested fixture: a minimal plan JSON where the first firewall rule is removed and one rule is modified, mirroring `examples/firewall-rules-demo/plan2.json`.

## Related Tests

Existing tests confirm semantic rendering of firewall rule changes (but not summary correctness):

- `Render_FirewallRuleCollection_UsesResourceSpecificTemplate`
- `Render_FirewallModifiedRules_ShowsDiffForChangedAttributes`

See: [src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererResourceTemplateTests.cs#L44-L120](../../../src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererResourceTemplateTests.cs#L44-L120)

## Additional Context

This issue is a classic Terraform list/array diff problem:
- rules are provided as an array
- removing index 0 shifts every subsequent element
- Terraform emits index-based diffs even when items are semantically unchanged

tfplan2md’s detailed firewall template already mitigates this by matching rules by `name`; the summary should align with that semantic view.
