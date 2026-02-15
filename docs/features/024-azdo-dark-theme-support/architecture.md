# Architecture: Azure DevOps Dark Theme Support

## Status

Proposed

## Context

The Terraform resource containers in tfplan2md reports are rendered as HTML `<details>` elements inside Markdown.

Today, multiple Scriban templates hard-code a light-theme border in an inline style:

- `_resource.sbn` and several resource-specific templates use `border: 1px solid #f0f0f0`.

This looks acceptable on light backgrounds but is visually jarring in Azure DevOps Services dark theme (too bright / too much contrast).

Azure DevOps Services provides theme-aware CSS custom properties (CSS variables) that change values based on the selected theme. We can leverage these variables in the inline style so the same report renders well in both Azure DevOps light and dark themes.

Important rendering constraint:
- GitHub removes inline styles from `<details>` entirely, so the change is expected to be a no-op on GitHub.
- Azure DevOps preserves inline styles on `<details>`, so changes must be made at the template level (wrapper CSS cannot reliably override inline `border:` declarations).

## Options Considered

### Option 1: Update inline border color in Scriban templates (recommended)

Replace the hard-coded border color with Azure DevOps’s theme-aware variable, keeping the existing border width/style:

- Use: `border: 1px solid rgb(var(--palette-neutral-10, 153, 153, 153));`

Pros:
- Works in Azure DevOps PR comment rendering (no dependency on an outer HTML wrapper)
- Automatically adapts to Azure DevOps light/dark theme
- Minimal change; keeps current layout/spacing/border width
- Safe fallback when the variable is absent

Cons:
- Relies on Azure DevOps defining `--palette-neutral-10` in the rendered environment
- Inline style remains duplicated across a few templates (unless follow-up refactoring is done)

### Option 2: Remove inline border styling and rely on platform CSS

Stop specifying borders in templates and rely on the host (Azure DevOps / GitHub) to style `<details>`.

Pros:
- Removes hard-coded visual styling from content
- Reduces styling duplication

Cons:
- Azure DevOps PR comment rendering does not provide tfplan2md-controlled CSS; results are unpredictable
- Loses the consistent “resource container” look across environments

### Option 3: Fix only the HTML renderer wrapper CSS (preview tooling)

Update [src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html](src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html) to use Azure DevOps variables for `.markdown-body details { border-color: ... }`.

Pros:
- Improves local preview artifacts and screenshots

Cons:
- Does not affect actual Azure DevOps Services PR comment rendering because wrapper CSS is not present there
- Inline border styles in Markdown templates still override wrapper CSS

## Decision

Adopt Option 1: Update the inline `<details>` border styling in the Scriban templates to use Azure DevOps’s theme-aware CSS variable `--palette-neutral-10`, with a neutral gray fallback.

## Rationale

This feature’s target environment is Azure DevOps Services PR comment rendering, where:
- The report is Markdown containing inline HTML
- tfplan2md cannot inject or control outer page CSS
- Inline styles are respected, so they are the reliable hook point

Using `rgb(var(--palette-neutral-10, 153, 153, 153))` delegates the final color choice to Azure DevOps’s theme system, producing subtle borders in dark mode while remaining appropriate in light mode.

## Consequences

### Positive
- Azure DevOps reports gain theme-aware borders with no user configuration
- Dark theme readability improves immediately
- GitHub behavior remains unchanged (inline styles on `<details>` are stripped)

### Negative / Risks
- If Azure DevOps changes or removes `--palette-neutral-10`, the fallback color will be used
- Preview tooling (HTML wrapper) will not automatically “magically” match Azure DevOps unless it defines the variable for both themes

## Implementation Notes

Developer guidance (no code in this document):

1. Update all templates that define the outer resource `<details>` container border inline.
   - Primary: [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn)
   - Also update any resource-specific templates that replicate the same `<details ... border:1px solid #f0f0f0 ...>` line:
     - [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn)
     - [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn)
     - [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn)

2. Preferred style snippet (keep current spacing, just replace the color):
   - `border: 1px solid rgb(var(--palette-neutral-10, 153, 153, 153));`

3. (Optional but recommended for repo demo artifacts)
   - Update the Azure DevOps HTML wrapper template to define `--palette-neutral-10` for both `[data-theme="light"]` and `[data-theme="dark"]` so screenshots/artifacts reflect theme-adaptive borders even outside Azure DevOps.
   - This is only for local preview tooling; Azure DevOps Services will provide the real variable values.

4. Validation approach
   - Regenerate the Azure DevOps HTML demo artifacts and verify borders are subtle in dark mode.
   - If using the screenshot generator workflow, verify the dark variant visually.
