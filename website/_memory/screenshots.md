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
| 1 | `semantic-diff-example.png` | Semantic Diffs | Before/After table showing inline diff | ✅ Created (316×121) |
| 2 | `firewall-rules-table.png` | Firewall Rule Interpretation | Firewall rule collection rendered as table | ⚠️ Needs manual crop |
| 3 | `nsg-rules-table.png` | NSG Rule Interpretation | NSG rules rendered as table | ℹ️ Use nsg-example-crop.png (580×400) |
| 4 | `role-assignment-mapping.png` | Role Assignment Mapping | GUID-to-name resolution example | ⚠️ Needs manual crop |
| 5 | `large-value-diff.png` | Large Value Formatting | JSON policy diff in collapsible section | ⚠️ Needs manual crop |
| 6 | `plan-summary.png` | Plan Summary | Summary table with resource type breakdown | ✅ Created (636×189) |
| 7 | `module-grouping.png` | Module Grouping | Resources grouped by module hierarchy | ✅ Created (1120×288) |
| 8 | `full-report-github.png` | Overview | Full comprehensive demo (GitHub flavor) | ✅ Created (1920×8088) |
| 9 | `full-report-azdo.png` | Overview | Full comprehensive demo (Azure DevOps flavor) | ✅ Created (1920×8451) |

## Alt Text Documentation

Each screenshot must have descriptive alt text for accessibility (WCAG 2.1 AA compliance).

| Screenshot | Alt Text |
|------------|----------|
| `firewall-example-crop.png` | Terraform plan showing Azure Firewall rule changes with semantic diffs highlighting source addresses and destination ports |
| `firewall-example-crop-dark.png` | Terraform plan showing Azure Firewall rule changes with semantic diffs highlighting source addresses and destination ports (dark theme) |
| `firewall-example-lightbox.png` | Detailed view of Azure Firewall rule collection changes showing before/after values for source addresses, destination addresses, and ports in a table format |
| `firewall-example-lightbox-dark.png` | Detailed view of Azure Firewall rule collection changes showing before/after values for source addresses, destination addresses, and ports in a table format (dark theme) |
| `nsg-example-crop.png` | Network Security Group rules showing security rule changes with priority, direction, access, and port information |
| `semantic-diff-example.png` | Storage account resource showing before and after values side-by-side, with account replication type changing from LRS to GRS |
| `firewall-rules-table.png` | Azure Firewall rule collection rendered as a table showing rule names, protocols, source addresses, destination addresses, and ports |
| `nsg-rules-table.png` | Network Security Group rules displayed in a table with priority, direction, access, protocol, source, destination, and port information |
| `role-assignment-mapping.png` | Azure role assignment showing principal name 'Jane Doe (User)' mapped to 'Reader' role on resource group scope |
| `large-value-diff.png` | Large text value diff showing line-by-line changes with additions highlighted in green and deletions in red |
| `plan-summary.png` | Terraform plan summary table showing resource counts by action type: Add (12), Change (6), Replace (2), Destroy (3) with resource type breakdown |
| `module-grouping.png` | Resources organized by Terraform module hierarchy showing 'Module: root' and 'Module: module.network' sections |
| `full-report-github.png` | Complete Terraform plan report in GitHub markdown style showing summary, resource changes grouped by module, with semantic diffs and Azure-specific visualizations |
| `full-report-azdo.png` | Complete Terraform plan report in Azure DevOps markdown style showing summary, resource changes grouped by module, with semantic diffs and Azure-specific visualizations |

## Generation Commands

Screenshots are generated using the tools in `tools/Oocx.TfPlan2Md.ScreenshotGenerator/`.

### Prerequisites

```bash
# Install Playwright Chromium (one-time setup)
pwsh tools/Oocx.TfPlan2Md.ScreenshotGenerator/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps
```

### Generate Firewall Section Screenshots

```bash
# First generate the HTML with Azure DevOps styling (for more authentic screenshot appearance)
dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor azdo \
  --template tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html \
  --output artifacts/comprehensive-demo.azdo.html

# Create dark mode version by changing data-theme attribute
sed 's/data-theme="light"/data-theme="dark"/' artifacts/comprehensive-demo.azdo.html > artifacts/comprehensive-demo.azdo-dark.html

# Generate full-page screenshots at 1x and 2x DPI for both themes
dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/comprehensive-demo-full.png \
  --full-page --width 1920

dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/comprehensive-demo-full@2x.png \
  --full-page --width 1920 --device-scale-factor 2

dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo-dark.html \
  --output website/assets/screenshots/comprehensive-demo-full-dark.png \
  --full-page --width 1920

dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
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

### Generate Feature-Specific Screenshots (Generated 2026-01-08)

```bash
# Prerequisites: Playwright Chromium installed
pwsh tools/Oocx.TfPlan2Md.ScreenshotGenerator/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps

# ID 1: Semantic Diff Example - Before/After table
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/semantic-diff-example.png \
  --target-selector "details:has(summary:has-text('🔄 azurerm_storage_account')) table"

# ID 6: Plan Summary - Summary table with resource counts
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/plan-summary.png \
  --target-selector "h2:has-text('Summary') + table"

# ID 7: Module Grouping - Module section headers
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/module-grouping.png \
  --target-selector "h3:has-text('Module:')"

# ID 8: Full GitHub Report
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output website/assets/screenshots/full-report-github.png \
  --full-page --width 1920

# ID 9: Full Azure DevOps Report
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/full-report-azdo.png \
  --full-page --width 1920
```

**Result:** Created 5 screenshots successfully (IDs 1, 6, 7, 8, 9). Screenshots with dimensions:
- semantic-diff-example.png: 316×121px
- plan-summary.png: 636×189px
- module-grouping.png: 1120×288px
- full-report-github.png: 1920×8088px (full page)
- full-report-azdo.png: 1920×8451px (full page)

### Screenshots Requiring Manual Cropping

The following screenshots could not be generated using automated tools due to technical limitations with capturing content inside `<details>` elements:

- **ID 2: firewall-rules-table.png** - Firewall rule collection table (inside collapsed details)
- **ID 3: nsg-rules-table.png** - NSG rules table (inside collapsed details) - NOTE: `nsg-example-crop.png` already exists and may be sufficient
- **ID 4: role-assignment-mapping.png** - Role assignment with principal/scope/role name resolution (inside collapsed details)
- **ID 5: large-value-diff.png** - Large value diff with line-level changes (inside collapsed details)

**Workaround for manual cropping:**

1. Generate HTML with all details opened:
```bash
sed 's/<details style="/<details open style="/g' artifacts/comprehensive-demo.azdo.html > /tmp/comprehensive-demo-azdo-open.html
```

2. Generate full-page screenshot:
```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input /tmp/comprehensive-demo-azdo-open.html \
  --output /tmp/comprehensive-demo-azdo-open-full.png \
  --full-page --width 1920
```

3. Use image editing tool to crop specific sections from the full-page screenshot.

## Decision Log

- 2026-01-03: Initial inventory created. No screenshots exist yet.
- 2026-01-03: Documented planned screenshots and generation commands.
- 2026-01-08: Generated IDs 1, 6, 7, 8, 9 using ScreenshotGenerator tool.
- 2026-01-08: IDs 2-5 marked as requiring manual cropping due to technical limitation with details elements.
