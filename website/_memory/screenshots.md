# Website Screenshot Inventory

This document lists the screenshots used on the website and how to generate them.

## Rules

- Every screenshot referenced by the website must have an entry here.
- Screenshots must be generated using the HTML renderer + screenshot generator tools.
- Screenshots should be stored under `website/assets/screenshots/`.

## Current State

**Status:** Homepage uses firewall rules crop with lightbox.

The homepage (`/index.html`) displays a 580×400px cropped screenshot showing firewall rule semantic diffs. Clicking opens a lightbox modal showing a 1200×800px detailed view.

Screenshots currently in use:
- **firewall-example-crop.png** (580×400): Firewall rules section for homepage
- **firewall-example-lightbox.png** (1200×800): Detailed firewall view for lightbox

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

### Generate Firewall Section Screenshots

```bash
# First generate the HTML with Azure DevOps styling (for more authentic screenshot appearance)
dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor azdo \
  --template src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html \
  --output artifacts/comprehensive-demo.azdo.html

# Create dark mode version by changing data-theme attribute
sed 's/data-theme="light"/data-theme="dark"/' artifacts/comprehensive-demo.azdo.html > artifacts/comprehensive-demo.azdo-dark.html

# Generate full-page screenshots at 1x and 2x DPI for both themes
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/comprehensive-demo-full.png \
  --full-page --width 1920

dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/comprehensive-demo-full@2x.png \
  --full-page --width 1920 --device-scale-factor 2

dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo-dark.html \
  --output website/assets/screenshots/comprehensive-demo-full-dark.png \
  --full-page --width 1920

dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo-dark.html \
  --output website/assets/screenshots/comprehensive-demo-full-dark@2x.png \
  --full-page --width 1920 --device-scale-factor 2

# Crop light mode screenshots
magick website/assets/screenshots/comprehensive-demo-full.png \
  -crop 580x400+400+1300 \
  website/assets/screenshots/firewall-example-crop.png

magick website/assets/screenshots/comprehensive-demo-full@2x.png \
  -crop 1160x800+800+2600 \
  website/assets/screenshots/firewall-example-crop@2x.png

magick website/assets/screenshots/comprehensive-demo-full.png \
  -crop 1200x800+370+1200 \
  website/assets/screenshots/firewall-example-lightbox.png

magick website/assets/screenshots/comprehensive-demo-full@2x.png \
  -crop 2400x1600+740+2400 \
  website/assets/screenshots/firewall-example-lightbox@2x.png

# Crop dark mode screenshots
magick website/assets/screenshots/comprehensive-demo-full-dark.png \
  -crop 580x400+400+1300 \
  website/assets/screenshots/firewall-example-crop-dark.png

magick website/assets/screenshots/comprehensive-demo-full-dark@2x.png \
  -crop 1160x800+800+2600 \
  website/assets/screenshots/firewall-example-crop-dark@2x.png

magick website/assets/screenshots/comprehensive-demo-full-dark.png \
  -crop 1200x800+370+1200 \
  website/assets/screenshots/firewall-example-lightbox-dark.png

magick website/assets/screenshots/comprehensive-demo-full-dark@2x.png \
  -crop 2400x1600+740+2400 \
  website/assets/screenshots/firewall-example-lightbox-dark@2x.png
```

**Result:** Creates firewall rule screenshots with native high-DPI versions for both light and dark themes:
- Light mode: `firewall-example-crop.png` (580×400, 1x) and `firewall-example-crop@2x.png` (1160×800, 2x)
- Dark mode: `firewall-example-crop-dark.png` (580×400, 1x) and `firewall-example-crop-dark@2x.png` (1160×800, 2x)
- Light mode lightbox: `firewall-example-lightbox.png` (1200×800, 1x) and `firewall-example-lightbox@2x.png` (2400×1600, 2x)
- Dark mode lightbox: `firewall-example-lightbox-dark.png` (1200×800, 1x) and `firewall-example-lightbox-dark@2x.png` (2400×1600, 2x)
- All 2x versions generated natively at high resolution, not scaled
- Crops start at x=400 (small) and x=370 (large) with minimal left margin
- Browser automatically selects appropriate DPI version based on screen

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
