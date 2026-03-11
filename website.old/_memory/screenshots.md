# Website Screenshot Inventory

This document lists the screenshots used on the website and how to generate them.

## Rules

- Every screenshot referenced by the website must have an entry here.
- Screenshots must be generated using the HTML renderer + screenshot generator tools.
- Screenshots should be stored under `website/assets/screenshots/`.

## Current State

**Status:** Homepage uses firewall rules with static analysis findings.

The homepage (`/index.html`) displays a 580×400px cropped screenshot showing firewall rule changes with a static analysis security warning. Clicking opens a lightbox modal showing a 1200×900px detailed view.

Screenshots currently in use:
- **firewall-example-crop-azdo.png** (580×400, 1x): Firewall rules thumbnail for homepage (light mode)
- **firewall-example-crop-azdo@2x.png** (1160×800, 2x): Firewall rules thumbnail (light mode, high-DPI)
- **firewall-example-crop-azdo-dark.png** (580×400, 1x): Firewall rules thumbnail (dark mode)
- **firewall-example-crop-azdo-dark@2x.png** (1160×800, 2x): Firewall rules thumbnail (dark mode, high-DPI)
- **firewall-example-lightbox-azdo.png** (1200×900, 1x): Firewall rules lightbox view (light mode)
- **firewall-example-lightbox-azdo@2x.png** (2400×1800, 2x): Firewall rules lightbox view (light mode, high-DPI)
- **firewall-example-lightbox-azdo-dark.png** (1200×900, 1x): Firewall rules lightbox view (dark mode)
- **firewall-example-lightbox-azdo-dark@2x.png** (2400×1800, 2x): Firewall rules lightbox view (dark mode, high-DPI)

Source: `examples/firewall-with-static-analysis/` (firewall rule changes with tfsec security finding)

The `/examples.html` page includes:
- **Firewall Rule Semantic Diffing**: Real output from `examples/firewall-rules-demo/` (generated via HtmlRenderer)
- **Module Grouping**: Hand-crafted example (needs to be replaced with real artifact)
- **Role Assignment Display**: Hand-crafted example (needs to be replaced with real artifact)
- **Sensitive Value Masking**: Hand-crafted example (needs to be replaced with real artifact)

The `website/assets/screenshots/` directory is currently empty. Screenshots need to be generated from the comprehensive-demo artifacts before they can be used on the website.

## Planned Screenshots

The following screenshots are needed for the website (based on feature page requirements):

| ID | File | Feature | Capture Target | Status |
|----|------|---------|----------------|--------|
| 0a | `firewall-example-crop.png` | Homepage Preview (Light 1x) | Firewall rules with semantic diffs | ✅ Created (580×400) |
| 0a2 | `firewall-example-crop@2x.png` | Homepage Preview (Light 2x) | Firewall rules with semantic diffs | ✅ Created (1160×800) |
| 0a-dark | `firewall-example-crop-dark.png` | Homepage Preview (Dark 1x) | Firewall rules with semantic diffs | ✅ Created (580×400) |
| 0a2-dark | `firewall-example-crop-dark@2x.png` | Homepage Preview (Dark 2x) | Firewall rules with semantic diffs | ✅ Created (1160×800) |
| 0b | `firewall-example-lightbox.png` | Homepage Lightbox (Light 1x) | Detailed firewall rules view | ✅ Created (1200×800) |
| 0b2 | `firewall-example-lightbox@2x.png` | Homepage Lightbox (Light 2x) | Detailed firewall rules view | ✅ Created (2400×1600) |
| 0b-dark | `firewall-example-lightbox-dark.png` | Homepage Lightbox (Dark 1x) | Detailed firewall rules view | ✅ Created (1200×800) |
| 0b2-dark | `firewall-example-lightbox-dark@2x.png` | Homepage Lightbox (Dark 2x) | Detailed firewall rules view | ✅ Created (2400×1600) |
| 1 | `semantic-diff-example.png` | Semantic Diffs | Before/After table showing inline diff | ⬜ Not created |
| 2 | `firewall-rules-table.png` | Firewall Rule Interpretation | Firewall rule collection rendered as table | ⬜ Not created |
| 3 | `nsg-rules-table.png` | NSG Rule Interpretation | NSG rules rendered as table | ⬜ Not created |
| 3a | `nsg-example-crop.png` | NSG Rule Interpretation | Cropped NSG rules example | ✅ Created (1x) |
| 4 | `role-assignment-mapping.png` | Role Assignment Mapping | GUID-to-name resolution example | ⬜ Not created |
| 5 | `large-value-diff.png` | Large Value Formatting | Key vault secret with line-by-line diff | ✅ Created (1200×250) |
| 5a | `storage-account-diff.png` | Simple Attribute Changes | Storage account Before/After table | ✅ Created (1x) |
| 5a2 | `storage-account-diff@2x.png` | Simple Attribute Changes | Storage account Before/After table | ✅ Created (2x) |
| 6 | `plan-summary.png` | Plan Summary | Summary table with resource type breakdown | ⬜ Not created |
| 7 | `module-grouping.png` | Module Grouping | Resources grouped by module hierarchy | ⬜ Not created |
| 8 | `full-report-github.png` | Overview | Full comprehensive demo (GitHub flavor) | ⬜ Not created |
| 9 | `full-report-azdo.png` | Overview | Full comprehensive demo (Azure DevOps flavor) | ⬜ Not created |

## Generation Commands

Screenshots are generated using the tools in `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/`.

### Prerequisites

```bash
# Install Playwright Chromium (one-time setup)
pwsh src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps
```

### Generate Homepage Firewall Screenshots

```bash
# Using the generate-screenshot.sh script
scripts/generate-screenshot.sh \
  --plan examples/firewall-with-static-analysis/plan.json \
  --output-prefix firewall-example \
  --selector "details:has(summary:has-text('azurerm_firewall_network_rule_collection'))" \
  --thumbnail-width 580 --thumbnail-height 400 \
  --thumbnail-offset-x 0 --thumbnail-offset-y 0 \
  --lightbox-width 1200 --lightbox-height 900 \
  --lightbox-offset-x 0 --lightbox-offset-y 0 \
  --full-page-width 1920 \
  --render-target azdo \
  --open-details-selector "details"
```

**Result:** Creates firewall rule screenshots with native high-DPI versions for both light and dark themes:
- Thumbnail: 580×400 (1x) and 1160×800 (2x)
- Lightbox: 1200×900 (1x) and 2400×1800 (2x)
- Both light and dark modes
- Azure DevOps rendering style
- Shows firewall rules with a security analysis warning (tfsec finding on wildcard destination)
- All files prefixed with `firewall-example-*-azdo`
- All `<details>` elements expanded before capture

**Options:**
- `--open-details-selector "details"`: Open all details elements (default)
- `--open-details-selector "details:has(summary:has-text('firewall'))"`: Open only firewall-related details
- `--open-details-selector "details.specific-class"`: Open only details with a specific CSS class
- Omit the parameter to keep details elements in their default state (collapsed unless marked with `open` attribute)

### Generate Full Report Screenshot (Azure DevOps flavor)

```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output website/assets/screenshots/firewall-rules-table.png \
  --target-terraform-resource-id "azurerm_firewall_policy_rule_collection_group.example"
```

### Generate Targeted Screenshot (by selector)

```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output website/assets/screenshots/plan-summary.png \
  --target-selector "table:has(th:has-text('Action'))"
```

### Generate Semantic Diffs Feature Screenshots

```bash
# Storage account simple attribute changes (with high-DPI version)
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/storage-account-diff.png \
  --target-terraform-resource-id "azurerm_storage_account.data" \
  --width 1200

# Note: 2x version generation requires manual duplication with different width or post-processing

# Large value diff (cropped from full comprehensive demo screenshot)
magick website/assets/screenshots/comprehensive-demo-full.png \
  -crop 1200x250+350+3000 \
  website/assets/screenshots/large-value-diff.png
```

**Result:** Creates screenshots for the semantic-diffs.html feature page:
- `storage-account-diff.png`: Storage account showing Before/After attribute changes in a table
- `storage-account-diff@2x.png`: High-DPI version (generated manually)
- `large-value-diff.png`: Key vault secret with line-by-line character-level diffs
- Uses existing screenshots: `firewall-example-crop.png`, `nsg-example-crop.png`

## Decision Log

- 2026-01-03: Initial inventory created. No screenshots exist yet.
- 2026-01-03: Documented planned screenshots and generation commands.
- 2026-01-10: Regenerated all firewall screenshots to show feature 031 improvements (Azure DevOps dark theme border colors). Screenshots now use `--palette-neutral-10` CSS variable that adapts to light/dark themes.
- 2026-02-01: Created new `examples/firewall-with-static-analysis/` with firewall rules + tfsec security finding. Created `scripts/generate-screenshot.sh` to automate screenshot generation with all variants (light/dark, 1x/2x, azdo/github, thumbnail/lightbox). Updated homepage to use these new screenshots showing both semantic diffs and static analysis warnings. Added `--open-details` argument to ScreenshotGenerator CLI to control which `<details>` elements should be expanded before capture (uses Playwright selector syntax). Updated `generate-screenshot.sh` to support `--open-details-selector` parameter for granular control.
