# Issue: NSG rendering – duplicate header, wrong columns on create, and over-escaping of `>`

## Problem Description

Three related markdown rendering problems were reported:

1. **Duplicate header line for Network Security Group (NSG)**: the rendered resource block repeats the NSG name after it is already shown in the `<summary>` header.
2. **Create action shows a “Before” column**: for add/create operations, the NSG template can still render a two-column Before/After table.
3. **Unnecessary escaping**: values that contain `->` show up as `-\>` (example: NSG rule `description`).

## Steps to Reproduce

### 1) Duplicate header line

This is visible in an existing artifact:

1. Open `artifacts/static-analysis-comprehensive-demo.md`.
2. Scroll to the `azurerm_network_security_group` block.
3. Observe that the `<summary>` already contains the NSG name (`🆔 nsg-app`), but an additional line prints `**Network Security Group:** `nsg-app``.

Evidence:
- `artifacts/static-analysis-comprehensive-demo.md#L290-L306`

### 2) “Before” column during create

This is reproducible logically from the template:

- In the NSG semantic template, the fallback “Attribute Changes” table is always rendered as `| Attribute | Before | After |`.
- For `change.action == "create"`, if `change.network_security_group.after_rules.size` is `0` (or the view model does not populate rules), the template falls through to the fallback and renders the Before/After table.

Evidence:
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/network_security_group.sbn#L44-L58`

### 3) Over-escaping of `>` inside inline code

The escape function used for markdown values escapes `>` into `\>`.

When values are additionally wrapped in backticks (inline code), this backslash becomes visible and yields output like `-\>`.

Evidence:
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Markdown.cs#L25-L36` (escapes `>` at line 29)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/ValueFormatting.cs#L44-L57` (wraps `EscapeMarkdown(value)` in backticks)

## Expected Behavior

1. NSG should not repeat a redundant “Network Security Group:” line when the `<summary>` already displays the NSG identity.
2. For create/add actions, attribute tables should not include a “Before” column (mirroring the behavior of the shared resource template).
3. Strings containing `->` should render as `->` (no `\>`).

## Actual Behavior

1. Duplicate NSG identity line.
2. NSG semantic template can display a Before/After table even for create.
3. `>` is escaped to `\>` and becomes visible inside inline code (e.g., `-\>`).

## Root Cause Analysis

### Affected Components

- NSG semantic template:
  - `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/network_security_group.sbn#L11-L13` (extra `**Network Security Group:** ...` line)
  - `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/network_security_group.sbn#L44-L58` (fallback attribute changes always uses Before/After)
- Firewall network rule collection semantic template has the same structural issue:
  - `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_network_rule_collection.sbn#L44-L58`
- Shared resource template shows the *intended* create/delete column behavior:
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn#L21-L56`
- Markdown escaping causes visible `\>` in inline code:
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Markdown.cs#L25-L36`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/ValueFormatting.cs#L44-L57`

### What’s Broken

- The semantic resource templates (`azurerm_network_security_group.sbn`, `azurerm_firewall_network_rule_collection.sbn`) duplicate some of the shared rendering logic but **do not mirror** the shared template’s action-specific column layout:
  - `_resource.sbn` renders `| Attribute | Value |` for create/delete and `| Attribute | Before | After |` for updates.
  - The semantic templates always render `Before/After` in their fallback attribute table.

- `EscapeMarkdown` escapes `>` even though most value rendering is inside inline code spans (via `FormatValue` / `format_attribute_value_table`). In inline code spans, backslash escapes are not processed, so the backslash becomes visible.

### Why It Happened

- The semantic templates were built to provide specialized “rules diff” rendering; the fallback attribute table appears to have been implemented as a simplified copy/paste without the create/delete branching that exists in `_resource.sbn`.
- `EscapeMarkdown` is designed for “markdown-breaking characters” and was made conservative (it escapes `<` and `>`), but it is reused in contexts where it is wrapped inside backticks (inline code), which changes how escaping behaves.

## Similar Issues / Broader Impact

- The create/delete “single column” behavior is consistently correct in the shared template `_resource.sbn`.
- The “always Before/After in fallback attribute changes” pattern exists in at least:
  - `azurerm_network_security_group.sbn`
  - `azurerm_firewall_network_rule_collection.sbn`
- The `>` over-escaping can affect **any** attribute value rendered via `FormatValue` (and thus via `format_attribute_value_table`) when the raw value contains `>` (common for `->`).

## Suggested Fix Approach (High-level)

1. **Remove or gate the redundant NSG header line** in `azurerm/network_security_group.sbn`:
   - Either drop `**Network Security Group:** ...` entirely, or only show it when the summary is *not* already showing an ID/name.
2. **Align semantic templates with `_resource.sbn` column behavior**:
   - In the semantic templates’ fallback attribute changes block, render:
     - `| Attribute | Value |` for create/delete
     - `| Attribute | Before | After |` for update/replace
   - Consider reusing shared helpers/partials to avoid divergence.
3. **Fix `->` rendering by changing escaping behavior for inline-code contexts**:
   - Option A: stop escaping `>` in `EscapeMarkdown`.
   - Option B (safer): introduce a distinct escape helper for inline-code values (used by `FormatValue`) that does not escape `>` (and possibly doesn’t escape `<` either).

## Related Tests

Potential areas to update/add tests after implementing a fix:

- Unit tests:
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersMarkdownTests.cs` (extend to cover `>` escaping expectations)
- Snapshot tests:
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azapi-special-chars.md` may be impacted if escaping rules change for `<`/`>`.

## Additional Context

- The duplicate NSG identity is visible in `artifacts/static-analysis-comprehensive-demo.md#L290-L306`.
- The semantic templates are under `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/` and appear to be the main divergence point from the shared `_resource.sbn` behavior.
