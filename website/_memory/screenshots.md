# Website Screenshot Inventory

This document lists the screenshots used on the website and how to generate them.

## Rules

- Every screenshot referenced by the website must have an entry here.
- Screenshots must be generated using the HTML renderer + screenshot generator tools.
- Screenshots should be stored under `website/assets/screenshots/`.

## Current State

**Status:** No screenshots exist yet.

The `website/assets/screenshots/` directory is currently empty. Screenshots need to be generated from the comprehensive-demo artifacts before they can be used on the website.

## Planned Screenshots

The following screenshots are needed for the website (based on feature page requirements):

| ID | File | Feature | Capture Target | Status |
|----|------|---------|----------------|--------|
| 1 | `semantic-diff-example.png` | Semantic Diffs | Before/After table showing inline diff | ⬜ Not created |
| 2 | `firewall-rules-table.png` | Firewall Rule Interpretation | Firewall rule collection rendered as table | ⬜ Not created |
| 3 | `nsg-rules-table.png` | NSG Rule Interpretation | NSG rules rendered as table | ⬜ Not created |
| 4 | `role-assignment-mapping.png` | Role Assignment Mapping | GUID-to-name resolution example | ⬜ Not created |
| 5 | `large-value-diff.png` | Large Value Formatting | JSON policy diff in collapsible section | ⬜ Not created |
| 6 | `plan-summary.png` | Plan Summary | Summary table with resource type breakdown | ⬜ Not created |
| 7 | `module-grouping.png` | Module Grouping | Resources grouped by module hierarchy | ⬜ Not created |
| 8 | `full-report-github.png` | Overview | Full comprehensive demo (GitHub flavor) | ⬜ Not created |
| 9 | `full-report-azdo.png` | Overview | Full comprehensive demo (Azure DevOps flavor) | ⬜ Not created |

## Generation Commands

Screenshots are generated using the tools in `tools/Oocx.TfPlan2Md.ScreenshotGenerator/`.

### Prerequisites

```bash
# Install Playwright Chromium (one-time setup)
pwsh tools/Oocx.TfPlan2Md.ScreenshotGenerator/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps
```

### Generate Full Report Screenshot

```bash
# Generate GitHub-flavored HTML first
dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor github \
  --template tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html \
  --output artifacts/comprehensive-demo.github.html

# Capture full-page screenshot
dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output website/assets/screenshots/full-report-github.png \
  --full-page
```

### Generate Targeted Screenshot (by Terraform resource)

```bash
dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output website/assets/screenshots/firewall-rules-table.png \
  --target-terraform-resource-id "azurerm_firewall_policy_rule_collection_group.example"
```

### Generate Targeted Screenshot (by selector)

```bash
dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output website/assets/screenshots/plan-summary.png \
  --target-selector "table:has(th:has-text('Action'))"
```

## Decision Log

- 2026-01-03: Initial inventory created. No screenshots exist yet.
- 2026-01-03: Documented planned screenshots and generation commands.
