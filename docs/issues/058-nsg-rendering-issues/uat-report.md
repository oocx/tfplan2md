# UAT Report: NSG Rendering Issues (Issue #058)

**Status:** ❌ FAILED
**Date:** 2026-02-03
**Branch:** `fix/058-nsg-rendering-issues`

## Summary of Results

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Remove duplicate header | ❌ FAIL | `azurerm/network_security_group.sbn` still has H3 address header |
| Align create fallback columns | ✅ PASS | Verified in focused UAT artifact |
| Improve '>' escaping | ⚠️ PARTIAL | `>` is not escaped, but `<` is still escaped causing visible backslashes in code spans |

## ❌ Issues Found

### 1. Duplicate Address Header
The specialized templates for NSG and Firewall Rule Collections still contain a redundant address header that matches the `<summary>`.
- **Affected Files:**
  - [src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/network_security_group.sbn](../../../src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/network_security_group.sbn)
  - [src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_network_rule_collection.sbn](../../../src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_network_rule_collection.sbn)
- **Observed Output:**
  ```markdown
  <summary>➕ azurerm_network_security_group no_rules ...</summary>
  <br>
  ### ➕ azurerm_network_security_group.no_rules
  ```

### 2. Over-escaping of '<' in Code Spans
While `>` is no longer escaped, `<` is still being escaped to `\<` in `Markdown.cs`. Since `ValueFormatting.cs` wraps these values in backticks, the backslash is rendered literally.
- **Affected File:** [src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Markdown.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Markdown.cs)
- **Repro:** Use a tag like `"html": "<div>"`.
- **Observed Output:** `` `html: \<div>` ` ` (renders as `html: \<div>`)
- **Expected Output:** `` `html: <div>` ` `

## Next Steps
- Handoff to Developer to remove the redundant `###` headers from semantic templates.
- Developer should reconsider escaping `<` when the value is known to be rendered inside backticks.
