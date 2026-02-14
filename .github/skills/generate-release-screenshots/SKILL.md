---
name: generate-release-screenshots
description: Generate PNG screenshots for release notes using the repository's HtmlRenderer and ScreenshotGenerator tools. Use when asked to add screenshots to release notes or documentation.
---

# Skill Instructions

## Purpose
Provide clear, actionable guidance for generating actual PNG screenshot files for release notes and documentation, preventing common mistakes like creating markdown links to source files or referencing non-existent images.

## Hard Rules
### Must
- [ ] Generate actual PNG files, NOT markdown links to source files or empty image references.
- [ ] Use `scripts/generate-release-screenshots.sh` for release note screenshots (includes retry logic and error reporting).
- [ ] Use `scripts/generate-screenshot.sh` for individual screenshots with full control (light/dark themes, DPI, crops).
- [ ] Verify generated PNG files exist at expected paths before adding markdown references.
- [ ] Verify screenshots show the intended content (not blank pages or errors).
- [ ] Use focused, small screenshots for release notes: **max 580×400 pixels**.
- [ ] Use only `*-crop*.png` files in release notes, or generate single screenshots using the release wrapper.

### Must Not
- [ ] Add `![Screenshot](path/to/image.png)` syntax to markdown before verifying the PNG file exists.
- [ ] Replace actual screenshots with markdown links to source files (e.g., `[View in file.md (lines X-Y)]`).
- [ ] Use text descriptions or placeholders instead of actual PNG files.
- [ ] Proceed with release if screenshot generation fails due to timeouts or tooling issues.

## Golden Example

### For Release Notes (Preferred Method)
```bash
# Generate focused screenshots for release notes
scripts/generate-release-screenshots.sh \
  --plan examples/comprehensive-demo/plan.json \
  --output-prefix feature-name \
  --output-dir docs/features/NNN-feature-slug/ \
  --selector "details:has(summary:has-text('resource_name'))"

# This script:
# - Includes retry logic (3 attempts with 5-second delays)
# - Provides detailed error reporting and troubleshooting guidance
# - Generates focused crop screenshots suitable for release notes
```

### Alternative Methods

#### Using Markdown File as Input
```bash
scripts/generate-release-screenshots.sh \
  --markdown-file artifacts/comprehensive-demo.md \
  --output-prefix demo-screenshot \
  --output-dir docs/features/NNN-feature-slug/ \
  --target-resource-id "azurerm_firewall_network_rule_collection"
```

#### For Individual Screenshots with Full Control
```bash
# Generate with light/dark themes, DPI options, and custom crops
scripts/generate-screenshot.sh \
  --plan examples/comprehensive-demo/plan.json \
  --output-prefix feature-name \
  --selector "details:has(summary:has-text('resource_name'))" \
  --thumbnail-width 580 --thumbnail-height 400 \
  --lightbox-width 1200 --lightbox-height 900 \
  --render-target azdo \
  --open-details-selector "details"

# This generates 12 variants:
# - Thumbnail and lightbox crops
# - Light and dark themes  
# - 1x and 2x DPI versions
```

## Actions

### 1. Understand What Screenshots Are Needed
Clarify with the user:
- What content should the screenshots show?
- Are they for release notes (small, focused) or documentation (detailed)?
- What resources or sections should be highlighted?

### 2. Choose the Appropriate Script

**For release notes:** Use `scripts/generate-release-screenshots.sh`
- Includes retry logic and error handling
- Generates focused, appropriately-sized screenshots
- Best for user-facing release documentation

**For full control:** Use `scripts/generate-screenshot.sh`
- Supports light/dark themes
- Supports multiple DPI levels (1x, 2x)
- Supports custom crop sizes
- Best for website and detailed documentation

### 3. Generate the Screenshots
Run the chosen script with appropriate parameters:
- Specify input source (`--plan` or `--markdown-file`)
- Set output prefix and directory
- Use selectors to focus on specific content
- For release notes, ensure max 580×400 pixel size

### 4. Verify Generation Success
Before proceeding:
- [ ] Check that PNG files exist at the expected paths
- [ ] Open or view the screenshots to confirm they show correct content
- [ ] Verify no blank pages or error messages in screenshots
- [ ] Confirm file sizes are appropriate for release notes

### 5. Add Markdown References
Only after verification:
```markdown
![Feature demonstration](docs/features/NNN-feature-slug/feature-name-crop-light-1x.png)
```

### 6. Handle Failures
If screenshot generation fails:
- **DO NOT proceed** with release or commit broken image references
- Report the failure to the Maintainer with full error details
- Document the specific error (timeout, CDN failure, tooling issue)
- Wait for tooling fix or Maintainer guidance

## Common Mistakes to Avoid

### ❌ Wrong: Adding references before generating files
```markdown
# Release notes created first
![Screenshot](docs/features/072/screenshot.png)

# Then trying to generate the screenshot
# Result: Broken link if generation fails
```

### ✅ Correct: Generate first, then reference
```bash
# 1. Generate the screenshot
scripts/generate-release-screenshots.sh --plan ... --output-prefix feature-name

# 2. Verify it exists
ls -lh docs/features/NNN/feature-name-crop-light-1x.png

# 3. Then add the markdown reference
echo '![Feature](docs/features/NNN/feature-name-crop-light-1x.png)' >> release-notes.md
```

### ❌ Wrong: Using markdown links instead of screenshots
```markdown
See the changes in [comprehensive-demo.md (lines 45-67)](comprehensive-demo.md#L45-L67)
```

### ✅ Correct: Using actual PNG screenshots
```markdown
![Network security rules demonstration](docs/features/072/nsg-rules-crop-light-1x.png)
```

## Technical Details

### How Screenshot Generation Works
1. **Markdown → HTML**: `src/tools/Oocx.TfPlan2Md.HtmlRenderer` renders markdown to HTML
2. **HTML → PNG**: `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator` (Playwright) captures PNG screenshots
3. **Repository scripts** wrap these tools with:
   - Retry logic for network failures
   - Error reporting and troubleshooting guidance
   - Batch generation for multiple variants

### For Website Screenshots
Use the `website-visual-assets` skill (`.github/skills/website-visual-assets/SKILL.md`) for:
- Website-specific screenshot workflows
- Screenshot inventory management (`website/_memory/screenshots.md`)
- Website asset organization (`website/assets/screenshots/`)

## References
- **Scripts**: `scripts/generate-release-screenshots.sh`, `scripts/generate-screenshot.sh`
- **Tools**: `src/tools/Oocx.TfPlan2Md.HtmlRenderer`, `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator`
- **Related skill**: `.github/skills/website-visual-assets/SKILL.md` (for website screenshots)
