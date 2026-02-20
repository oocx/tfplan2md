# Features

This document describes the features of `tfplan2md` from a user perspective.

## Overview

`tfplan2md` is a CLI tool that converts Terraform plan JSON files into human-readable markdown reports. It is designed primarily for use in CI/CD pipelines and is distributed as a Docker image.

## Debug Output

**Status:** ✅ Implemented

tfplan2md can append diagnostic information to reports to help troubleshoot issues and understand the tool's behavior. Enable debug mode with the `--debug` flag to append a "Debug Information" section at the end of the markdown report.

### Features

- **Command-line flag**: Single `--debug` flag enables all diagnostics
- **Output location**: Debug information is appended to the markdown report in a collapsible `<details>` block (collapsed by default)
- **Collapsible format**: Debug section is wrapped in a `<details>` block to reduce visual clutter while keeping information accessible (Feature 086)
- **Principal mapping diagnostics**:
  - Load status (success/failure) and file path
  - Count of principals by type (users, groups, service principals)
  - Failed ID resolutions with resource context showing which resource referenced each missing ID
- **Template resolution diagnostics**: Shows which templates (custom, built-in resource-specific, or default) were used for each resource type
- **Non-intrusive**: No impact when debug flag is not used

### Usage

```bash
# Enable debug output
tfplan2md --debug plan.json

# With principal mapping
tfplan2md --debug --principal-mapping principals.json plan.json -o report.md

# Docker
docker run -v $(pwd):/data oocx/tfplan2md --debug /data/plan.json
```

### Debug Output Example

When `--debug` is enabled, the report includes a collapsible section like:

```markdown
<details>
<summary>🐛 Debug Information</summary>
<br>

### Principal Mapping

Principal Mapping: Loaded successfully from 'principals.json'
- Found 45 principals

Failed to resolve 2 principal IDs:
- `12345678-1234-1234-1234-123456789012` (referenced in `azurerm_role_assignment.example`)
- `87654321-4321-4321-4321-210987654321` (referenced in `azurerm_role_assignment.reader`)

### Template Resolution

- `azurerm_firewall_network_rule_collection`: Built-in resource-specific template
- `azurerm_role_assignment`: Built-in resource-specific template
- `azurerm_storage_account`: Default template

</details>
```

The debug information is collapsed by default to reduce visual clutter in PR reviews, but can be expanded by clicking the "🐛 Debug Information" summary line. This helps diagnose principal mapping failures and verify which templates are being applied to each resource type.

## Output Display Enhancements

**Status:** ✅ Implemented (Feature 086)

tfplan2md includes display enhancements that improve readability and user experience in markdown reports, particularly for PR reviews:

### Collapsible Debug Section

The debug information section (enabled with `--debug` flag) is displayed in a collapsed `<details>` block by default to reduce visual clutter while keeping debug information accessible when needed.

**Benefits:**
- **Cleaner reports**: Debug information doesn't dominate the visible report
- **Accessible when needed**: Users can expand the section by clicking the summary line
- **Professional appearance**: Reports look clean in PR reviews, especially when multiple modules are being reviewed

**Implementation:** The debug section is wrapped in `<details>` tags with `<summary>🐛 Debug Information</summary>` and is collapsed by default (no `open` attribute).

### No-Changes Summary

When a Terraform plan contains no changes (zero add, change, replace, or destroy actions), the Summary section displays a simple "No changes" message instead of an empty summary table.

**Before (empty table):**
```markdown
## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 0 | |
| 🔄 Change | 0 | |
| ♻️ Replace | 0 | |
| ❌ Destroy | 0 | |
| **Total** | **0** | |

## Resource Changes

No changes
```

**After (clean message):**
```markdown
## Summary

No changes
```

**Benefits:**
- **Professional appearance**: No-changes plans show a clean, simple message
- **Reduced redundancy**: Resource Changes section is omitted when Summary already shows "No changes"
- **Better PR reviews**: Easier to scan multiple modules where many may have no changes (e.g., unaffected modules in a monorepo)

**Implementation:** 
- Templates check `summary.total == 0` and render "No changes" text instead of the table
- Resource Changes section is omitted when `module_changes.size == 0` to avoid redundancy

### User Experience

These enhancements make tfplan2md reports cleaner, more professional, and easier to scan, especially in PR reviews where:
- Debug information may be enabled for troubleshooting but shouldn't clutter the main view
- Many plans may have no changes (e.g., unaffected modules in infrastructure monorepos)
- Reviewers need to quickly identify which modules have changes and which don't

## Markdown to HTML Renderer (Dev Tool)

**Status:** ✅ Implemented

A standalone .NET tool located at [src/tools/Oocx.TfPlan2Md.HtmlRenderer](../src/tools/Oocx.TfPlan2Md.HtmlRenderer) converts tfplan2md markdown reports into HTML fragments or complete documents. This tool approximates GitHub and Azure DevOps rendering, enabling:

- **Automated validation** - Verify markdown renders correctly on target platforms
- **Website examples** - Generate HTML versions of demo reports
- **Visual testing** - Create HTML for screenshot generation and regression testing
- **Documentation** - Produce standalone HTML versions of reports for sharing

### Report Metadata Display

**Status:** ✅ Implemented

Every report includes metadata about the tfplan2md build used to generate it, displayed in the header alongside Terraform version information:

- **Version information**: Shows tfplan2md semantic version (e.g., "0.30.0")
- **Build identification**: Includes short git commit hash (7 characters) for traceability
- **Generation timestamp**: UTC timestamp when the report was generated
- **Single line format**: `Generated by tfplan2md X.Y.Z (abc1234) on YYYY-MM-DD HH:MM:SS UTC | Terraform 1.14.0`
- **CLI suppression**: Use `--hide-metadata` flag to suppress the entire metadata line
- **Test stability**: Metadata provider can be mocked for deterministic snapshot tests

**Usage:**
```bash
# Default (metadata displayed)
tfplan2md plan.json

# Suppress metadata line
tfplan2md plan.json --hide-metadata
```

**Example output:**
```markdown
# Terraform Plan Report

Generated by tfplan2md 0.30.0 (a1b2c3d) on 2026-01-03 14:23:15 UTC | Terraform 1.14.0

## Summary
...
```

### Features

- **Two HTML flavors**: `github` (strips inline styles to match GitHub's sanitization) and `azdo` (preserves inline styles for Azure DevOps compatibility)
- **Fragment or complete documents**: Generate HTML fragments for embedding, or complete HTML documents using wrapper templates
- **Platform-specific rendering**:
  - **GitHub flavor**: Strips `style` attributes from inline HTML (matching GitHub's security policy)
  - **Azure DevOps flavor**: Preserves inline styles for proper diff highlighting and theme-adaptive styling
- **Dark theme support**: Azure DevOps wrapper template supports both light and dark themes via `data-theme` attribute
- **Default wrapper templates**: Includes example templates with platform-approximating CSS and syntax highlighting via Highlight.js
- **Automatic output naming**: Derives output filename from input if not specified (e.g., `report.md` → `report.github.html`)

### CLI Options

| Option | Description |
|--------|-------------|
| `--input`, `-i <file>` | Path to input markdown file (required) |
| `--output`, `-o <file>` | Output HTML file path (auto-derived if omitted) |
| `--flavor`, `-f <github\|azdo>` | Target HTML flavor (required) |
| `--template`, `-t <file>` | Wrapper template file with `{{content}}` placeholder (optional) |
| `--help`, `-h` | Display help information |
| `--version`, `-v` | Display version information |

### Usage Examples

**Generate GitHub-flavored HTML fragment:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor github
# Output: artifacts/comprehensive-demo.github.html
```

**Generate complete Azure DevOps document with template:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor azdo \
  --template src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html \
  --output artifacts/comprehensive-demo.azdo.html
```

### Wrapper Templates

Wrapper templates are HTML files containing a `{{content}}` placeholder where the rendered markdown will be inserted. The tool includes two example templates:

- **github-wrapper.html**: GitHub-style CSS and color scheme
- **azdo-wrapper.html**: Azure DevOps-style CSS and color scheme with theme support
  - Light theme (default): Uses `data-theme="light"` attribute and light color palette
  - Dark theme: Uses `data-theme="dark"` attribute and dark color palette
  - Defines `--palette-neutral-10` CSS variable for theme-adaptive borders matching Azure DevOps behavior

Both templates include:
- Responsive HTML5 structure
- Platform-approximating CSS styles
- Highlight.js integration for syntax highlighting (via CDN)
- Proper emoji rendering support

## HTML Screenshot Generator (Dev Tool)

**Status:** ✅ Implemented

A standalone .NET tool located at [src/tools/Oocx.TfPlan2Md.ScreenshotGenerator](../src/tools/Oocx.TfPlan2Md.ScreenshotGenerator) that generates screenshots from HTML files using Playwright and Chromium. This tool enables visual regression testing, documentation screenshot generation, and automated testing workflows by converting HTML reports into image files.

### Resource Visual Enhancements

**Status:** ✅ Implemented

All resources in generated reports are wrapped in styled `<details>` blocks with consistent border styling to improve visual hierarchy and scanability:

- **Theme-adaptive borders**: Uses Azure DevOps CSS variable `--palette-neutral-10` with fallback to `#999` (applied as inline styles: `border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153))`)
- **Platform-specific handling**:
  - **Azure DevOps**: Preserves inline styles with automatic theme adaptation
    - **Light theme**: Borders use light gray from Azure DevOps theme system
    - **Dark theme**: Borders automatically adjust to subtle dark gray for comfortable viewing
  - **GitHub**: Strips inline styles (GitHub's markdown renderer removes them), but semantic structure remains intact
- **Consistent application**: All resource types use the same border styling
- **Expandable resources**: Resources without existing details blocks use `<details open>` to remain expanded by default
- **No nested borders**: Only the outermost resource block gets borders; nested details (for large attributes) remain unstyled

**Visual effect in Azure DevOps:**
Each resource appears in a bordered box that automatically adapts to the user's selected theme (light or dark), making it easy to scan multiple resources and understand the structure at a glance without visual strain.

**Visual effect in GitHub:**
Borders are not visible (styles stripped), but the semantic structure and content remain unchanged.

### Features

- **Multiple image formats**: PNG (default) and JPEG with configurable quality (WebP deferred per maintainer request)
- **Flexible viewport control**: Default 1920x1080 viewport with customizable dimensions via `--width` and `--height`
- **Full-page capture**: Captures entire scrollable content with variable height using `--full-page` flag
- **Partial capture**: Target specific elements for documentation screenshots:
  - **By Terraform resource address**: Use `--target-terraform-resource-id <address>` to capture a specific resource
  - **By selector**: Use `--target-selector <selector>` for flexible element matching (Playwright selector syntax)
  - **Union bounding box**: When multiple elements match, captures the union of their bounding boxes
  - **Error handling**: Fails with clear error message when target is not found
- **Automatic output naming**: Derives output filename from input if not specified (e.g., `report.html` → `report.png`)
- **Format detection**: Determines image format from output filename extension or explicit `--format` parameter
- **Browser automation**: Uses Playwright for reliable, cross-platform Chromium automation

### CLI Options

| Option | Description |
|--------|-------------|
| `--input`, `-i <file>` | Path to input HTML file (required) |
| `--output`, `-o <file>` | Output image file path (auto-derived if omitted) |
| `--width`, `-w <pixels>` | Viewport width in pixels (default: 1920) |
| `--height`, `-h <pixels>` | Viewport height in pixels (default: 1080; ignored if `--full-page` is set) |
| `--full-page`, `-f` | Capture full scrollable page height (default: false) |
| `--target-terraform-resource-id <address>` | Capture only the resource matching the Terraform address |
| `--target-selector <selector>` | Capture only elements matching a Playwright selector |
| `--open-details <selector>` | Open `<details>` elements matching selector before capture (Playwright syntax) |
| `--device-scale-factor <factor>` | Device scale factor for high-DPI screenshots (e.g., 2 for @2x) |
| `--format <png\|jpeg>` | Image format (auto-detected from output filename if omitted) |
| `--quality`, `-q <0-100>` | JPEG quality setting (default: 90) |
| `--help`, `-h` | Display help information |
| `--version`, `-v` | Display version information |

### Usage Examples

**Default viewport screenshot:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html
# Output: artifacts/comprehensive-demo.github.png (1920x1080)
```

**Custom viewport dimensions:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output artifacts/screenshot-1280x720.png \
  --width 1280 \
  --height 720
```

**Full-page capture:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output artifacts/full-report.png \
  --full-page
```

**JPEG with custom quality:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output artifacts/screenshot.jpg \
  --quality 85
```

**Capture specific resource by Terraform address:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output artifacts/resource.png \
  --target-terraform-resource-id "azurerm_storage_account.example"
```

**Capture by selector (Playwright syntax):**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output artifacts/firewall.png \
  --target-selector "details:has(summary:has-text('azurerm_firewall'))"
```

**Capture with expanded details elements:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output artifacts/firewall-expanded.png \
  --target-selector "details:has(summary:has-text('azurerm_firewall'))" \
  --open-details "details"
```

**High-DPI screenshot (2x):**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.github.html \
  --output artifacts/firewall@2x.png \
  --target-selector "details:has(summary:has-text('azurerm_firewall'))" \
  --device-scale-factor 2
```

**Notes:**
- Terraform resource targeting matches resources by locating the module heading and resource summary text
- Selector targeting supports full Playwright selector syntax (not limited to CSS)
- When multiple elements match, a single screenshot of their union bounding box is captured
- The `--open-details` option expands `<details>` elements before capture, ensuring proper bounding box calculation
- The tool fails with a clear error if the target is not found

### Automated Screenshot Generation

### Screenshot Generation Workflows

#### Quick Start: Release Notes Screenshots

Use `scripts/generate-release-screenshots.sh` for release documentation - generates a single optimized screenshot:

```bash
scripts/generate-release-screenshots.sh \
  --plan examples/comprehensive-demo/comprehensive-demo.json \
  --output-prefix feature-example \
  --output-dir docs/features/XXX-feature-name \
  --selector "article:has(h2:has-text('azurerm_resource'))" \
  --render-target github
```

Output: Single 580×400 PNG (light mode, 1x DPI) suitable for release notes.

#### Advanced: Website Screenshots

For website screenshots, use the `scripts/generate-screenshot.sh` wrapper script that automates the complete workflow:

```bash
scripts/generate-screenshot.sh \
  --plan examples/firewall-with-static-analysis/plan.json \
  --output-prefix firewall-example \
  --selector "details:has(summary:has-text('azurerm_firewall'))" \
  --thumbnail-width 580 --thumbnail-height 400 \
  --lightbox-width 1200 --lightbox-height 900 \
  --render-target azdo \
  --open-details-selector "details"
```

This script automatically:
- Generates markdown from the plan file
- Renders HTML for both light and dark themes
- Creates screenshots at 1x and 2x DPI
- Generates both thumbnail and lightbox crops
- Handles all ImageMagick cropping operations

### Prerequisites

Before using the screenshot generator, install Chromium browser binaries once after building:

```bash
pwsh src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps
```

For CI/CD environments, use the `--with-deps` flag to install system dependencies required by Chromium.

### Diff Format Handling

The tool correctly handles both diff formats generated by tfplan2md (controlled via `--render-target`):

- **`azuredevops` (default)**: Uses inline HTML `<span>` elements with CSS styles for character-level highlighting
  - **Azure DevOps flavor**: Preserves all inline styles (renders as intended)
  - **GitHub flavor**: Strips `style` attributes (content remains readable but loses highlighting)
- **`github`**: Uses markdown code blocks with `diff` language and `+`/`-` markers
  - **Both flavors**: Render identically

This mirrors how these formats behave when posted as comments on actual GitHub PRs and Azure DevOps PRs.

### Technical Details

- **Markdown parser**: Uses [Markdig](https://github.com/xoofx/markdig) with advanced extensions for GFM compatibility
- **Platform approximation**: Post-processes HTML to match platform-specific behaviors (e.g., style attribute stripping for GitHub)
- **Testing**: Integration tests validate rendering of all demo artifacts against both flavors

## Terraform Show Approximation Renderer (Dev Tool)

**Status:** ✅ Implemented

A standalone .NET tool located at [src/tools/Oocx.TfPlan2Md.TerraformShowRenderer](../src/tools/Oocx.TfPlan2Md.TerraformShowRenderer) that generates output approximating `terraform show` from Terraform plan JSON files. This tool is used to create authentic "before tfplan2md" examples for website feature comparison pages.

### Purpose

Website feature pages require side-by-side comparisons showing what Terraform plans look like WITHOUT tfplan2md (raw `terraform show` output) versus WITH tfplan2md (formatted markdown reports). However, the project only has JSON format plan files, not the binary `.tfplan` files that `terraform show` requires.

This tool solves the problem by reading Terraform plan JSON files and producing text output that matches the format, structure, and appearance of real `terraform show` output, including ANSI color codes for authentic terminal rendering.

### Features

- **Authentic output format**: Matches `terraform show` structure and appearance
- **ANSI color support**: Includes terminal color codes (green for additions, yellow for updates, red for deletions)
- **Plain text mode**: `--no-color` flag strips ANSI codes for non-terminal contexts
- **All plan operations**: Supports create, update, delete, replace, read, and no-op actions
- **Complete sections**: Generates legend, resource details with attribute diffs, and plan summary
- **Terraform-style formatting**: Proper action symbols (`+`, `~`, `-`, `-/+`, `<=`), indentation, and diff markers
- **Known-after-apply indicators**: Shows `(known after apply)` for computed values
- **Sensitive value handling**: Renders `(sensitive value)` for sensitive attributes
- **Action reasons**: Includes replacement reasons in parentheses when present

### CLI Options

| Option | Description |
|--------|-------------|
| `--input`, `-i <file>` | Path to Terraform plan JSON file (required) |
| `--output`, `-o <file>` | Output text file path (writes to stdout if omitted) |
| `--no-color` | Suppress ANSI color codes (plain text output) |
| `--help`, `-h` | Display help information |
| `--version`, `-v` | Display version information |

### Usage Examples

**Generate colored terminal output:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input examples/comprehensive-demo/plan.json \
  --output examples/comprehensive-demo/terraform-show.txt
```

**Plain text without ANSI codes:**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.json \
  --no-color \
  --output artifacts/terraform-show-plan1.txt
```

**Output to stdout (for piping):**
```bash
dotnet run --project src/tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input plan.json
```

### Example Output

```
Terraform will perform the following actions:

  # azurerm_storage_account.example will be created
  + resource "azurerm_storage_account" "example" {
      + account_replication_type = "LRS"
      + account_tier             = "Standard"
      + id                       = (known after apply)
      + location                 = "westeurope"
      + name                     = "stexample123"
      + resource_group_name      = "example-rg"
    }

  # azurerm_key_vault.example will be updated in-place
  ~ resource "azurerm_key_vault" "example" {
        id                      = "/subscriptions/.../resourceGroups/rg/providers/Microsoft.KeyVault/vaults/kv"
        name                    = "kv"
      ~ sku_name                = "standard" -> "premium"
        # (10 unchanged attributes hidden)
    }

Plan: 1 to add, 1 to change, 0 to destroy.
```

### Target Audience

This tool is intended for:

- **Website maintainers** generating authentic-looking Terraform output examples
- **Contributors** creating "before" examples when demonstrating new tfplan2md features
- **Documentation writers** showing realistic Terraform output without requiring actual Terraform binary plan files

### Limitations

This is a development tool only, explicitly **not** for production use:

- Does not process actual Terraform binary plan files (`.tfplan`)
- Does not execute Terraform commands or require Terraform installation
- Not distributed in Docker images (source-only tool)
- Not intended for CI/CD pipeline integration

## Architecture Boundary Enforcement

**Status:** ✅ Implemented  
**Since:** Feature 066  
**Specification:** [Feature 066](features/066-architecture-boundary-enforcement/specification.md)

Automated enforcement of architectural layer boundaries using architecture tests. The tfplan2md codebase has clear namespace organization (CLI, Parsing, MarkdownGeneration, Platforms, Providers) representing distinct architectural layers. Architecture tests automatically verify dependency rules between these layers to prevent architectural drift and maintain clean separation of concerns.

### Features

- **Automated boundary enforcement** - Architecture tests run in CI with every PR to catch violations before merge
- **14 architecture rules** - 7 forbidden dependencies + 4 allowed dependencies (documentation) + 3 naming conventions
- **Clear error messages** - Test failures include rule statement, rationale, violations, and links to guidance
- **Fast execution** - All tests complete in ~3 seconds
- **Living documentation** - Rules are documented as executable tests that serve as architectural documentation
- **Known exemptions** - 8 files with documented exemptions for violations requiring refactoring

### Layer Dependency Rules

Key architectural rules enforced by tests:

- ✅ **Parsing** layer must NOT depend on `MarkdownGeneration` (prevents circular dependencies)
- ✅ **Platforms** layer CAN depend on `MarkdownGeneration` (platform-specific rendering uses general infrastructure)
- ✅ **MarkdownGeneration** layer must NOT depend on `Providers` (general rendering independent of specific providers)
- ✅ Exception classes must end with `Exception` suffix (naming convention)

### Usage

Architecture tests run automatically as part of standard test execution:

```bash
# Run all tests (includes architecture tests)
dotnet test

# Run only architecture tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*
```

### Documentation

- **Architecture Rules:** [docs/architecture-rules.md](architecture-rules.md) - Complete list of layer definitions and dependency rules
- **ADR:** [ADR-007](adr-007-architecture-boundary-enforcement.md) - Technical decision rationale
- **Test Location:** `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs`

This is an **internal infrastructure feature** for maintainers and contributors - it does not affect tfplan2md's functionality or user-facing behavior.

## Markdown Quality Validation

**Status:** ✅ Implemented (v0.26.0+)

All generated markdown is validated to ensure correct rendering on GitHub and Azure DevOps:

- **Automated linting** - CI runs markdownlint-cli2 on generated reports
- **Property-based invariants** - Tests verify markdown properties that must always hold (no consecutive blank lines, proper table structure, balanced HTML tags)
- **Snapshot testing** - Detects unexpected changes in output
- **Template isolation testing** - Each template is tested independently
- **Fuzz testing** - Edge cases (special characters, Unicode, long values) are automatically tested
- **Docker-based tools** - No local tool dependencies required

See [docs/markdown-specification.md](markdown-specification.md) for the supported markdown subset and [docs/testing-strategy.md](testing-strategy.md) for testing details.

## Consistent Value Formatting

**Status:** ✅ Implemented

To improve readability and scannability, `tfplan2md` uses a consistent formatting strategy across all reports:

- **Data Values**: All actual data values (resource names, IP addresses, configuration settings) are formatted as `inline code` (backticks). This makes them visually distinct from labels and descriptions.
- **Labels & Keys**: Attribute names, table headers, and descriptive text are rendered as plain text.
- **Summaries**: Resource summaries use code formatting for all identifiers and configuration values (e.g., `name`, `resource_group`, `location`).
- **Diffs**: Large value diffs support both styled HTML (`azuredevops` target) and simple text (`github` target) formats, configurable via `--render-target` CLI option.

This ensures that the most important information—the values changing in your infrastructure—always stands out.

### Semantic Icons for Naming Attributes

**Status:** ✅ Implemented

Common naming attributes are enhanced with semantic icons to improve visual scanning and clarity:

- **Resource names** (`name` attribute): 🆔 icon (ID button)
  - Applied across all providers to the `name` attribute
  - Format: `🆔 my-resource-name`
  - Helps quickly identify resource names in tables and summaries

- **Azure resource group names** (`resource_group_name` attribute): 📁 icon (folder)
  - Applied to `resource_group_name` in Azure resources
  - Format: `📁 my-resource-group`
  - Provides visual distinction for resource group references

**Formatting rules:**
- Icons use a non-breaking space (`&nbsp;`) between the icon and the value
- The value itself is still wrapped in backticks for consistency with other code values
- Icons appear in both attribute tables and resource summary lines
- Context-aware: Icons are only applied to the specific attributes, not to all uses of those strings

**Example in attribute table:**
```markdown
| Attribute | Value |
|-----------|-------|
| name | 🆔 `storage-account-prod` |
| resource_group_name | 📁 `rg-production` |
| location | `eastus` |
```

**Example in resource summary:**
```markdown
#### ➕ azurerm_storage_account.example

**Summary:** 🆔 `storage-prod` in 📁 `rg-demo` (eastus)
```

These icons complement the existing semantic icon system for other value types (🌐 for IPs, 🔌 for ports, 👤 for users, etc.).

## Universal Azure Resource ID Formatting

**Status:** ✅ Implemented

Long Azure resource IDs from the `azurerm` provider are rendered as readable scopes instead of raw GUID paths and are kept inside attribute tables (not relegated to the “Large attributes” section).

- **Automatic detection**: Pattern-based detection recognizes subscription, resource group, and resource IDs across all `azurerm` resources.
- **Readable output**: IDs are formatted via `AzureScopeParser`, with semantic icons and values wrapped as inline code (e.g., Key Vault `🆔 kv-demo` in resource group `📁 rg-demo` of subscription `🔑 00000000-0000-0000-0000-000000000000`).
- **Stays in tables**: Azure IDs are exempt from large-value classification, so even very long IDs remain in the main attribute table.
- **Model-driven routing**: Large-value classification is computed in C#, and templates rely on `attr.is_large` without needing provider context.
- **Summaries included**: Resource summaries use the same formatting, preventing raw IDs from appearing in the header lines.
- **Non-Azure unaffected**: Other providers still render values as backticked inline code.

**Before:**
````markdown
| Attribute | Value |
|-----------|-------|
| key_vault_id | `/subscriptions/.../resourceGroups/.../providers/Microsoft.KeyVault/vaults/kv-demo` |
````

**After:**
````markdown
| Attribute | Value |
|-----------|-------|
| key_vault_id | Key Vault `🆔 kv-demo` in resource group `📁 rg-demo` of subscription `🔑 00000000-0000-0000-0000-000000000000` |
````

## Custom Report Title

**Status:** ✅ Implemented

Users can provide a custom title for generated reports via the `--report-title` CLI option. This allows for better context when sharing reports or distinguishing between different pipeline runs.

- **Contextual Titles**: Add repository names, environment identifiers, or purpose (e.g., "Drift Detection") to the report title.
- **Automatic Escaping**: Special characters in titles are automatically escaped to ensure valid markdown headings.
- **Template Support**: The custom title is available to all templates (built-in and custom) via the `report_title` variable.
- **Fallback**: If no title is provided, templates fall back to their default titles (e.g., "Terraform Plan Report").
  Templates add the `#` heading marker; `report_title` only contains the escaped text.

Example:
```bash
tfplan2md plan.json --report-title "Drift Detection Results"
```

## Input

- **Stdin (default)**: Read Terraform plan JSON from standard input
  ```bash
  terraform show -json plan.tfplan | tfplan2md
  ```
- **File path**: Read from a file specified as an argument
  ```bash
  tfplan2md plan.json
  ```

## Output

- **Stdout (default)**: Write markdown to standard output
- **File**: Write to a file using the `--output` flag
  ```bash
  tfplan2md plan.json --output plan.md
  ```

## Report Content

The default report includes:

- **Summary**: Overview of changes with count and resource type breakdown for each action (e.g., "3 to add: 1 azurerm_resource_group, 2 azurerm_storage_account")
- **Detailed changes**: List of affected resources with their actions and attribute changes

### Summary Resource Type Breakdown

The summary table includes a "Resource Types" column that shows which resource types are affected by each action. This helps users quickly understand what types of resources are being created, modified, replaced, or destroyed without reading through the detailed changes section.

**Features:**
- Shows count and full resource type name for each type (e.g., "3 azurerm_storage_account")
- Resource types are sorted alphabetically within each action
- Each resource type appears on its own line using HTML `<br/>` tags
- Empty when an action has 0 resources
- The Total row shows only the sum of displayed actions (Add + Change + Replace + Destroy), excluding no-op resources

**Example:**
```markdown
| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | 1 azurerm_resource_group<br/>3 azurerm_storage_account<br/>2 azurerm_virtual_network |
| 🔄 Change | 3 | 2 azurerm_app_service<br/>1 azurerm_sql_database |
| ♻️ Replace | 1 | 1 azurerm_kubernetes_cluster |
| ❌ Destroy | 0 | |
| **Total** | **10** | |
```

**Note**: The space between each action icon (➕, 🔄, ♻️, ❌) and the action label (Add, Change, Replace, Destroy) is a non-breaking space (U+00A0) to prevent the icon from wrapping to a different line than its label in narrow layouts.

### No-Op Resources

Resources with no changes (no-op) are **excluded from the Total count** and are **not displayed** in the detailed changes section. This design choice:
- Keeps the Total consistent with the visible action rows in the summary table
- Reduces output noise when reviewing plans with many unchanged resources
- Enables processing of large Terraform plans without hitting template iteration limits

The `summary.no_op` count is available to custom templates but not shown in the default template.

### Action Symbols

Resources are displayed with emoji symbols indicating the action. These symbols appear in both the summary table and resource summaries, with a non-breaking space (U+00A0) between the icon and following text to prevent wrapping:
- `➕` - create (add new resource)
- `🔄` - update (modify existing resource)
- `❌` - delete (remove resource)
- `♻️` - replace (delete and recreate resource)

### Resource Summaries

**Status:** ✅ Implemented

Each resource change displays a concise one-line summary above the `<details>` section to help users quickly scan and understand changes without expanding every resource.

**Summary formats by action:**

- **CREATE**: Shows resource name and key identifying attributes based on resource type
  - Example: `` `sttfplan2mdlogs` in `rg-tfplan2md-demo` (eastus) | Standard LRS ``
  - Uses resource-specific attribute mappings for 43 azurerm resources, plus fallbacks for azuredevops, azapi, and msgraph providers (Azure AD summaries are handled by provider templates)

### Large Attribute Value Display

**Status:** ✅ Implemented

Large attribute values (multi-line text or strings > 100 characters) are automatically moved from the main attribute table to a separate collapsible `<details>` section to improve readability.

**Features:**
- **Automatic Detection**: Values with newlines or exceeding 100 characters are treated as large.
- **Collapsible Section**: Large values are grouped in a `<details>` section below the resource table.
- **Summary Line**: The summary shows the number of lines and changed lines for each large attribute.
- **Formatting Options** (via `--render-target`):
  - `azuredevops` (default, alias `azdo`): Azure DevOps-optimized HTML with character-level diff highlighting.
  - `github`: GitHub-compatible markdown diff blocks with traditional `+`/`-` markers.
- **Complete Replacement**: If a value is completely replaced (no common lines), it shows separate "Before" and "After" blocks.

**Usage:**
```bash
# Default (Azure DevOps format)
tfplan2md plan.json

# GitHub format
tfplan2md plan.json --render-target github
```

- **UPDATE**: Shows resource name and list of changed attributes (up to 3, then "+ N more")
  - Example: `` `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center ``

- **REPLACE**: Shows resource name and replacement reason when available (Terraform 1.2+)
  - With reason: `` recreate `snet-db` (address_prefixes changed: force replacement) ``
  - Without reason: `` recreating `nsg-app` (1 changed) ``
  - Shows up to 3 attributes that forced replacement, then "+ N more"

- **DELETE**: Shows resource name
  - Example: `` `sttfplan2mdlegacy` ``

**Provider-specific mappings:**

The summary builder includes intelligent attribute selection for multiple providers:
- **azurerm**: 43 resource types with specific mappings (storage accounts, VMs, networks, databases, etc.)
- **azuredevops**: Projects and pipelines
- **azuread**: Users, invitations, groups (with and without members), group membership, service principals
- **azapi**: Generic resources and actions
- **msgraph**: Microsoft Graph API resources

When no specific mapping exists, the tool falls back to provider-level defaults (e.g., azurerm → name, resource_group_name, location) or generic fallbacks (name, display_name).

**Replacement reasons:**

For resources with action "replace", the summary includes which attribute(s) caused the replacement. This information is parsed from the `replace_paths` field in Terraform plan JSON (available in Terraform 1.2+). When this field is not available, the summary shows the count of changed attributes instead.

All summary values are automatically escaped to ensure valid markdown output.

### Sensitive Values

- Sensitive values are **masked by default** for security
- Use `--show-sensitive` flag to reveal sensitive values in the output

### Attribute Tables

Attribute tables in the default template now vary by the resource change action to make output more concise and meaningful:

- **create**: 2-column table (`Attribute | Value`) showing the *after* values
- **delete**: 2-column table (`Attribute | Value`) showing the *before* values
- **update** and **replace**: 3-column table (`Attribute | Before | After`)

**Unchanged attribute filtering (default behavior):**
- Attributes where the before and after values are identical are **hidden by default** to reduce noise and help users quickly identify actual changes
- Use `--show-unchanged-values` flag to display all attributes including unchanged ones
- This filtering applies to all attribute change tables regardless of which template is used

Null or unknown attributes are omitted from the tables to avoid meaningless rows, and sensitive attributes are masked unless `--show-sensitive` is used.

## Large Attribute Value Display

**Status:** ✅ Implemented

To improve readability, large attribute values (multi-line or >100 characters) are automatically moved from the main attribute table to a collapsible `<details>` section below the resource summary.

**Features:**
- **Automatic detection**: Attributes with newlines or long values are automatically identified.
- **Collapsible section**: All large attributes are grouped in a single "Large attributes" section.
- **Summary line**: Shows the count of large attributes and total lines (e.g., "Large attributes (2 attributes, 147 lines)").
- **Display formats** (via `--render-target`):
  - **`azuredevops`** (default, alias `azdo`): Azure DevOps-optimized HTML with line-by-line and character-level diff highlighting. Note that GitHub strips these HTML styles (content remains readable), so this format is best for Azure DevOps.
  - **`github`**: Cross-platform markdown code blocks with `diff` syntax highlighting and `+`/`-` markers. This format is fully portable and works on both GitHub and Azure DevOps.

**Usage:**
Control the display format using the `--render-target` CLI option:
```bash
tfplan2md plan.json --render-target github
```

### Module Grouping (NEW)

Resource changes in the report are now grouped by Terraform module. Each module with at least one change is rendered as its own section (module sections for modules without changes are omitted to keep reports concise).

- **Module header**: Each module is shown as an H3 heading ("### 📦 Module: <module_address>"), where `module_address` is the full module path from the Terraform plan (e.g., `module.network.module.subnet`). The root module is shown as `root`. The 📦 icon is followed by a non-breaking space (U+00A0) to prevent wrapping.
- **Resource headings**: Resources within a module are shown as H4 headings ("#### <action_symbol> <address>") to preserve a proper document hierarchy.
- **Ordering**: Modules are listed so that the root module appears first, followed by other modules in lexicographic order. Nested modules are presented in a flat list but the sort order ensures child modules follow their parent modules.
- **Template variable**: Templates have access to a top-level `module_changes` collection (in addition to the existing `changes` collection). Each item has:
  - `module_address` (string, empty for root)
  - `changes` (array of resource change objects, same structure as items in `changes`)

Example usage in a Scriban template:

```scriban
{{ for module in module_changes }}
### Module: {{ if module.module_address && module.module_address != "" }}`{{ module.module_address }}`{{ else }}root{{ end }}

{{ for change in module.changes }}
#### {{ change.action_symbol }} {{ change.address }}

... render attribute tables ...

{{ end }}

---
{{ end }}
```

This grouping is enabled by default and cannot be disabled (it keeps reports concise and improves readability for multi-module plans).

## CI/CD Integration

### Cumulative Release Notes

When creating GitHub releases and Docker deployments, the release workflow automatically generates cumulative release notes that include all changes since the last release. This is particularly useful when not every version is deployed to Docker Hub.

**How it works:**
- The release workflow queries the GitHub API to find the last published release
- It extracts all changelog sections between that version and the current version being released
- GitHub release notes include accumulated changes from all intermediate versions
- If no previous release exists (first release), only the current version's changes are included

**Example:**
- Last Docker release: v0.8.0
- Versions created since then: v0.9.0, v0.10.0, v0.11.0
- When releasing v0.12.0, the release notes will include changes from v0.9.0, v0.10.0, v0.11.0, and v0.12.0

This ensures Docker Hub users can see the complete set of changes included in each deployment, even when intermediate versions were skipped.

## Markdown Quality and Validation

tfplan2md ensures generated markdown is valid and renders correctly on GitHub and Azure DevOps:

- **Automatic escaping**: All external input (resource names, attribute values, module addresses) is automatically escaped to prevent broken tables and headings
- **Selective escaping**: Only structural characters that break markdown (pipes `|`, backticks `` ` ``, angle brackets `<>`, ampersands `&`, backslashes `\`) are escaped; readable characters like parentheses, brackets, asterisks, and underscores are preserved
- **Newline normalization**: Newlines in attribute values are converted to `<br/>` tags for table compatibility
- **Heading spacing**: Blank lines are automatically added before and after headings to ensure proper rendering
- **Table formatting**: Tables use padded separator rows that satisfy markdownlint requirements
- **CI validation**: The comprehensive demo output is validated with markdownlint-cli2 on every PR and commit to main

The escaping logic is centralized in the `escape_markdown` Scriban helper, which all templates must use for external input.

For details on the markdown subset and formatting rules, see [docs/markdown-specification.md](markdown-specification.md).

## Templates

Reports are generated using customizable templates powered by [Scriban](https://github.com/scriban/scriban).

### Built-in Templates

tfplan2md includes multiple built-in templates that can be selected by name:

- **`default`** (used when no template is specified): Full report with summary and detailed resource changes
- **`summary`**: Compact summary showing only Terraform version, plan timestamp, and action counts with resource type breakdown

Select a built-in template using the `--template` option:
```bash
tfplan2md plan.json --template summary
```

### Custom Templates

Provide a custom template file using the `--template` flag:
```bash
tfplan2md plan.json --template /path/to/custom-template.sbn
```

**Template resolution order:**
1. Check if the provided value matches a built-in template name
2. If not, attempt to load it as a file path
3. If neither exists, display an error listing available built-in templates

**Important:** Custom templates must use the `escape_markdown` helper on all external input to ensure valid markdown output. See built-in templates for examples.

### Template Variables

Templates have access to the following variables:

- **`terraform_version`** - Terraform version string (e.g., "1.14.0")
- **`format_version`** - Plan format version (e.g., "1.2")
- **`tf_plan2_md_version`** - tfplan2md semantic version (e.g., "0.30.0")
- **`commit_hash`** - Short git commit hash (7 characters) of the tfplan2md build
- **`generated_at_utc`** - UTC timestamp when the report was generated (DateTimeOffset object)
- **`hide_metadata`** - Boolean indicating whether the metadata line should be suppressed
- **`timestamp`** - Plan generation timestamp in RFC3339 format (e.g., "2025-12-20T10:00:00Z"), if available in the plan JSON
- **`show_unchanged_values`** - Boolean indicating whether unchanged attribute values are included in the output
- **`summary`** - Summary object with action details:
  - `to_add`, `to_change`, `to_destroy`, `to_replace`, `no_op` - Each is an `ActionSummary` object containing:
    - `count` - Number of resources for this action
    - `breakdown` - Array of `ResourceTypeBreakdown` objects, each with `type` (resource type name) and `count` (number of that type)
  - `total` - Total number of resources with changes (excludes no-op resources)
- **`changes`** - List of resource changes (no-op resources excluded), each with:
  - `address` - Full resource address
  - `type` - Resource type
  - `action` - Action string ("create", "update", "delete", "replace")
  - `action_symbol` - Emoji symbol for the action
  - `summary` - One-line summary of the resource change (auto-generated)
  - `replace_paths` - Array of attribute paths that triggered replacement (Terraform 1.2+, may be null)
  - `attribute_changes` - List of attribute changes with `name`, `before`, `after`, and `is_sensitive`
  - `before_json`, `after_json` - Raw JSON state (for resource-specific templates)
- **`module_changes`** - Resource changes grouped by module, each with:
  - `module_address` - Module address (empty string for root)
  - `changes` - Array of resource changes for this module

**Note:** The `timestamp` field is optional and may be `null` if not present in the Terraform plan JSON.

## CLI Interface

Simple single-command interface with flags:

| Flag | Description |
|------|-------------|
| `--output <file>` | Write output to a file instead of stdout |
| `--template <name\|file>` | Use a built-in template by name (default, summary) or a custom Scriban template file |
| `--report-title <text>` | Override the report's level-1 heading |
| `--render-target <github\|azuredevops>` | Target platform for rendering: `github` (simple diff) or `azuredevops` (inline diff, default) |
| `--principal-mapping <file>` | Map Azure principal IDs to names using a JSON file |
| `--show-unchanged-values` | Include unchanged attribute values in tables (hidden by default) |
| `--show-sensitive` | Show sensitive values unmasked |
| `--hide-metadata` | Suppress tfplan2md version and generation timestamp from report header |
| `--debug` | Append diagnostic information to the report for troubleshooting |
| `--help` | Display help information |
| `--version` | Display version information |

## Error Handling

- **Exit codes**: `0` on success, non-zero on failure
- **Error messages**: Clear and actionable, written to stderr
- **No partial output**: The tool fails cleanly without producing incomplete reports
- **Lenient parsing**: Processes the plan as long as required fields are valid (no strict format validation)

## Terraform Compatibility

- Supports Terraform **1.14 and later**
- Lenient parsing approach—does not validate the full plan format, only the fields needed for report generation

## Azure Display Enhancements

**Status:** ✅ Implemented  
**Related specification:** [docs/features/063-azure-display-enhancements/specification.md](features/063-azure-display-enhancements/specification.md)

tfplan2md automatically enhances the display of Azure resources and identities across all Azure providers (azurerm, azapi, azuread, azdevops) by formatting Azure resource IDs, resolving human-readable names for subscriptions, management groups, tenants, and role definitions, and providing descriptive summaries for specific Azure resource types.

### Universal Azure Resource ID Detection

Any attribute value matching the Azure resource ID pattern is automatically detected and formatted with readable scope information, regardless of provider or attribute name. This extends the existing "Universal Azure Resource ID Formatting" feature to detect Azure IDs anywhere they appear in attribute values.

**Example:**
```markdown
| key_vault_id | Key Vault `kv-demo` in resource group `rg-demo` of subscription `Production (d1828a48-fced-4ea2-b2ec-4b9623f327fd)` |
```

### Subscription Display Names

When a mapping file is provided with the `--principal-mapping` flag, subscription IDs are automatically formatted as `DisplayName (ID)` everywhere they appear:

- Within readable resource names
- In standalone subscription ID attributes
- In scope descriptions across all Azure resources

**Before:**
```markdown
| subscription_id | `d1828a48-fced-4ea2-b2ec-4b9623f327fd` |
```

**After (with mapping):**
```markdown
| subscription_id | `Production (d1828a48-fced-4ea2-b2ec-4b9623f327fd)` |
```

### Management Group Display Names

Management group IDs are resolved to display names when mappings are provided, displayed with the 🗂️ icon. Root management groups (where the management group ID matches the tenant ID) are formatted specially as "🗂️ Tenant `<tenant_name>` root" to clearly indicate the tenant root scope.

**Example with mapping:**
```markdown
| management_group_id | `🗂️ Production Workloads` |
```

**Root management group example:**
```markdown
| management_group_id | `🗂️ Tenant Contoso Corp root` |
```

### Tenant Display Names

Tenant IDs are resolved to display names when mappings are provided, displayed with the 🏢 icon in the format `DisplayName (ID)`:

**Before:**
```markdown
| tenant_id | `12345678-1234-1234-1234-123456789012` |
```

**After (with mapping):**
```markdown
| tenant_id | `🏢 Contoso Corp (12345678-1234-1234-1234-123456789012)` |
```

### Role Definition Names

tfplan2md automatically recognizes all 473 built-in Azure role definition GUIDs and displays their friendly names. Custom role definitions can be mapped via the mapping file, and custom mappings can override built-in role names if needed.

Role definitions are displayed across all Azure resources wherever they appear (not just in role assignments):

**Before:**
```markdown
| role_definition_id | `/subscriptions/.../providers/Microsoft.Authorization/roleDefinitions/8e3af657-a8ff-443c-a75c-2fe8c4bcb635` |
```

**After (with built-in role recognition):**
```markdown
| role_definition_id | `🛡️ Owner (8e3af657-a8ff-443c-a75c-2fe8c4bcb635)` |
```

### Enhanced Resource Summaries

Three Azure resource types receive specialized summary formatting for improved readability:

#### azurerm_private_dns_a_record

Private DNS A records display as FQDN format (`name.zone_name`) in the resource summary line:

**Before:**
```markdown
### ➕ azurerm_private_dns_a_record `example`
```

**After:**
```markdown
### ➕ azurerm_private_dns_a_record `record1.contoso.local`
```

#### azurerm_pim_eligible_role_assignment

PIM (Privileged Identity Management) eligible role assignments display as "Assign `<role_name>` to `<principal_name>`" when mappings are available:

**Before:**
```markdown
### ➕ azurerm_pim_eligible_role_assignment `example`
```

**After (with mappings):**
```markdown
### ➕ azurerm_pim_eligible_role_assignment `example`: Assign `Contributor` to `Jane Doe`
```

#### azurerm_role_management_policy

Role management policies display as "`<role_name>` in `<scope_display_name>`" with resolved role names and enriched scope information:

**Before:**
```markdown
### 🔁 azurerm_role_management_policy `this`
```

**After (with mappings):**
```markdown
### 🔁 azurerm_role_management_policy `this`: `Contributor` in resource group `foo` of subscription `Production (abc-123...)`
```

### Visual Icons

Azure entities are displayed with distinctive icons for quick identification:

| Icon | Entity Type | Format Example |
|------|-------------|----------------|
| 🏢 | Tenant | `🏢 Contoso Corp (tenant-guid)` |
| 🗂️ | Management Group | `🗂️ Production Workloads` |
| 🛡️ | Role Definition | `🛡️ Owner (role-guid` |

### Extended Mapping File Format

The mapping file used with `--principal-mapping` now supports Azure infrastructure metadata in addition to principals. The new sections use an array-of-objects format for consistency:

```json
{
  "users": [
    { "id": "user-guid", "displayName": "Jane Doe" }
  ],
  "groups": [
    { "id": "group-guid", "displayName": "DevOps Team" }
  ],
  "servicePrincipals": [
    { "id": "sp-guid", "displayName": "CI/CD Pipeline" }
  ],
  "subscriptions": [
    { "id": "d1828a48-fced-4ea2-b2ec-4b9623f327fd", "displayName": "Production" }
  ],
  "managementGroups": [
    { "id": "mg-production", "displayName": "Production Workloads" }
  ],
  "tenants": [
    { "id": "tenant-guid", "displayName": "Contoso Corp" }
  ],
  "roles": [
    { "id": "custom-role-guid", "displayName": "Custom Deployment Role" }
  ]
}
```

**Backward compatibility:** Existing principal-only mapping files continue to work without modification. The new sections are optional.

### Azure CLI Export Commands

Use the Azure CLI to generate the mapping data for each section:

```bash
# Principals (Azure AD)
az ad user list --all --query "[].{id:id,displayName:displayName}" -o json
az ad group list --query "[].{id:id,displayName:displayName}" -o json
az ad sp list --all --query "[].{id:id,displayName:displayName}" -o json

# Subscriptions
az account list --query "[].{id:id,displayName:name}" -o json

# Management groups
az account management-group list --query "[].{id:name,displayName:displayName}" -o json

# Tenants
az account tenant list --query "[].{id:tenantId,displayName:displayName}" -o json

# Custom roles
az role definition list --custom-role-only true --query "[].{id:name,displayName:roleName}" -o json
```

**Multi-tenant scenarios:** When authenticated to multiple tenants but only needing mappings for specific ones, filter by tenant ID:

```bash
# Get specific tenants only
az account tenant list --query "[?tenantId=='12345678-...' || tenantId=='87654321-...'].{id:tenantId, displayName:displayName}" -o json

# Get principals from specific tenant
az ad user list --tenant 12345678-1234-1234-1234-123456789012 --query "[].{id:id, displayName:displayName}" -o json

# Get subscriptions from specific tenants
az account list --query "[?tenantId=='12345678-...' || tenantId=='87654321-...'].{id:id, displayName:name}" -o json
```

Use `scripts/validate-azure-cli-commands.sh` to validate these commands in your environment.

### Debug Output

When `--debug` is enabled, failed mapping resolutions are reported with context showing which resource referenced each unmapped ID:

```markdown
## Debug Information

### Principal Mapping

Principal Mapping: Loaded successfully from 'azure-mappings.json'
- Found 45 principals
- Found 3 subscriptions
- Found 2 management groups
- Found 1 tenant
- Found 5 custom roles

Failed to resolve 4 IDs:
- Subscription `12345678-1234-1234-1234-123456789012` (referenced in `azurerm_resource_group.example`) - not found in mapping file
- Management group `mg-unknown` (referenced in `azurerm_management_group_policy_assignment.test`) - not found in mapping file
- Tenant `87654321-4321-4321-4321-210987654321` (referenced in `azuread_user.example`) - not found in mapping file
- Role definition `abcdef12-3456-7890-abcd-ef1234567890` (referenced in `azurerm_role_assignment.reader`) - not found in mapping file or built-in roles
```

### Fallback Behavior

When mappings are not available or an ID is not found:
- Subscriptions display as raw GUID
- Management groups display as raw ID (without 🗂️ icon)
- Tenants display as raw GUID (without 🏢 icon)
- Role definitions display as raw GUID or the `role_definition_name` attribute if available (without 🛡️ icon)
- All Azure resource IDs continue to use readable formatting with the IDs they contain

### Usage

```bash
# Without mapping (uses raw IDs and built-in roles)
tfplan2md plan.json

# With extended mapping
tfplan2md --principal-mapping azure-mappings.json plan.json

# Docker with extended mapping
docker run -v $(pwd):/data oocx/tfplan2md \
  --principal-mapping /data/azure-mappings.json \
  /data/plan.json --output /data/plan.md

# With debug output to see failed resolutions
tfplan2md --debug --principal-mapping azure-mappings.json plan.json
```

## Enhanced Azure Role Assignment Display

The `azurerm_role_assignment` resource uses a table-based format with human-readable summaries, semantic icons, and clean resource naming instead of cryptic GUIDs and technical paths.

**Features:**
- **Semantic icons**: Principal types show `👤 User`, `👥 Group`, or `💻 ServicePrincipal`; roles show `🛡️` shield icon
- **Local resource names**: Summaries show clean names (e.g., `rg_reader`) instead of full module paths
- **Comprehensive built-in role mapping**: All 473 Azure built-in role definition GUIDs are automatically mapped to friendly names (e.g., "Reader", "Contributor", "Storage Blob Data Reader")
- **Hierarchical scope display**: Azure resource scopes are parsed and displayed with clear context:
  - Management Groups: `my-mg (Management Group)`
  - Subscriptions: `subscription sub-id`
  - Resource Groups: `my-rg in subscription sub-id`
  - Resources: Recognizes 20+ common Azure resource types with friendly names:
    - Compute: Virtual Machine, Virtual Machine Scale Set, AKS Cluster, Managed Disk
    - Storage: Storage Account, Key Vault, Container Registry, Cosmos DB Account
    - Networking: Virtual Network, Subnet, Load Balancer, Application Gateway, Azure Firewall, Private Endpoint
    - Web/Apps: App Service, App Service Plan, SQL Server, SQL Database
    - Monitoring: Log Analytics Workspace, Application Insights
    - Data: Azure Cache for Redis, Event Hubs Namespace, Service Bus Namespace
    - Graceful fallback to capitalized type names for unmapped resources
- **Optional principal mapping**: Map principal IDs to names using a JSON file with the `--principal-mapping` (or `--principals`, `-p`) flag
- **Table-based output**: Clean collapsible `<details>` sections with attribute tables
- **Smart summaries**: One-line summary showing principal → role → scope relationship

**Example output:**
```markdown
<details>
<summary>➕ azurerm_role_assignment <b><code>rg_reader</code></b> — <code>👤 Jane Doe (User)</code> → <code>🛡️ Reader</code> on <code>rg-demo</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| scope | `rg-demo` in subscription `12345678-1234-1234-1234-123456789012` |
| role_definition_id | `Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |
| principal_id | `Jane Doe (User)` (User) [`00000000-0000-0000-0000-000000000001`] |
| principal_type | `👤 User` |
| role_definition_name | `🛡️ Reader` |

</details>
```

Note how the summary shows:
- Local resource name `rg_reader` instead of full module path
- Principal with icon: `👤 Jane Doe (User)`
- Role with icon: `🛡️ Reader`

**Usage:**
```bash
# Without principal mapping (role and scope are still enhanced)
tfplan2md plan.json

# With principal mapping
tfplan2md --principal-mapping principals.json plan.json

# Docker with principal mapping
docker run -v $(pwd):/data oocx/tfplan2md \
  --principal-mapping /data/principals.json /data/plan.json
```

**Creating a principal mapping file:**

The mapping file supports principals plus Azure metadata (subscriptions, management groups, tenants, and custom roles). The new sections use an array-of-objects format; existing principal-only files remain supported.

```bash
# Principals (Azure AD)
az ad user list --all --query "[].{id:id,displayName:displayName}" -o json
az ad group list --query "[].{id:id,displayName:displayName}" -o json
az ad sp list --all --query "[].{id:id,displayName:displayName}" -o json

# Subscriptions
az account list --query "[].{id:id,displayName:name}" -o json

# Management groups
az account management-group list --query "[].{id:name,displayName:displayName}" -o json

# Tenants
az account tenant list --query "[].{id:tenantId,displayName:displayName}" -o json

# Custom roles
az role definition list --custom-role-only true --query "[].{id:name,displayName:roleName}" -o json
```

**Multi-tenant mapping (specific tenants only):**

Use per-tenant queries when you're signed into multiple tenants but only want a subset. The simplest approach is to switch the active tenant and run the queries for each one.

```bash
# Pick a tenant ID from your allowed tenants
az account tenant list --query "[].{id:tenantId,displayName:displayName}" -o json

# Repeat these commands per tenant
TENANT_ID="12345678-1234-1234-1234-123456789012"
az account set --tenant "$TENANT_ID"

# Principals (Azure AD) for this tenant
az ad user list --all --query "[].{id:id,displayName:displayName}" -o json
az ad group list --query "[].{id:id,displayName:displayName}" -o json
az ad sp list --all --query "[].{id:id,displayName:displayName}" -o json

# Subscriptions for this tenant
az account list --query "[?tenantId=='$TENANT_ID'].{id:id,displayName:name}" -o json

# Management groups for this tenant
az account management-group list --query "[].{id:name,displayName:displayName}" -o json

# Custom roles for this tenant
az role definition list --custom-role-only true --query "[].{id:name,displayName:roleName}" -o json
```

If you're collecting data from multiple tenants, repeat the block for each tenant and merge the resulting JSON arrays (for example, using `jq -s 'add'`) before assembling the final mapping file.

The extended mapping JSON format:
```json
{
  "users": [
    { "id": "user-guid", "displayName": "Jane Doe" }
  ],
  "groups": [
    { "id": "group-guid", "displayName": "DevOps Team" }
  ],
  "servicePrincipals": [
    { "id": "sp-guid", "displayName": "CI/CD Pipeline" }
  ],
  "subscriptions": [
    { "id": "d1828a48-fced-4ea2-b2ec-4b9623f327fd", "displayName": "Production" }
  ],
  "managementGroups": [
    { "id": "mg-production", "displayName": "Production Workloads" }
  ],
  "tenants": [
    { "id": "tenant-guid", "displayName": "Contoso Corp" }
  ],
  "roles": [
    { "id": "custom-role-guid", "displayName": "Custom Deployment Role" }
  ]
}
```

The type (User, Group, ServicePrincipal) is automatically read from the Terraform plan's `principal_type` attribute and displayed as: `Display Name (Type) [guid]`.

## Enhanced Azure AD Resource Display

**Status:** ✅ Implemented  
**Related specification:** [docs/features/053-azuread-resources-enhancements/specification.md](features/053-azuread-resources-enhancements/specification.md)

Azure AD resources now use specialized templates with semantic icons and informative summaries that make identity infrastructure changes easier to review at a glance. Resource summaries display key identity attributes with visual icons, enabling quick scanning without expanding details.

**Supported resources:**
- `azuread_user` - Users with display name, UPN, and email
- `azuread_invitation` - Guest user invitations
- `azuread_group_member` - Group membership relationships
- `azuread_group` - Groups with member count by type
- `azuread_service_principal` - Service principals with application ID
- `azuread_group_without_members` - Groups without member tracking

**Icon mapping:**

| Icon | Meaning | Used For |
|------|---------|----------|
| 👤 | User | User principals, user display names |
| 👥 | Group | Group principals, group display names |
| 💻 | Service Principal | Service principal display names |
| 🆔 | Identifier | User principal names, application IDs, group names |
| 📧 | Email | Email addresses (mail, user_email_address) |
| ❓ | Unknown | Principals with unresolvable type |

**Key features:**
- **Semantic icons**: Visual indicators for principal types, identifiers, and emails
- **Concise summaries**: Key attributes visible without expanding details
- **Member counting**: Groups show member counts by type (users, groups, service principals)
- **Type-adaptive icons**: Group member icons adapt based on member type
- **Principal mapping support**: Resolves principal IDs to names when mapping file is provided
- **Graceful degradation**: Missing optional attributes (description, mail) are omitted cleanly

**Example: azuread_user**

```markdown
<details>
<summary>➕ azuread_user <b><code>jane</code></b> — <code>👤 Jane Doe</code> (<code>🆔 jane.doe@example.com</code>) <code>📧 jane.doe@example.com</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| display_name | `👤 Jane Doe` |
| user_principal_name | `🆔 jane.doe@example.com` |
| mail | `📧 jane.doe@example.com` |

</details>
```

**Example: azuread_group with member counts**

```markdown
<details>
<summary>➕ azuread_group <b><code>platform_team</code></b> — <code>👥 Platform Team</code> (<code>🆔 Platform Engineering</code>) Core platform engineering team | <code>3 👤 2 👥 1 💻 1 ❓</code></summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| display_name | `Platform Team` |
| mail_nickname | `Platform Engineering` |
| description | `Core platform engineering team` |
| members[0] | `user-100` |
| members[1] | `user-101` |
| members[2] | `group-100` |
| members[3] | `spn-200` |
| members[4] | `unknown-999` |

</details>
```

The member count `3 👤 2 👥 1 💻 1 ❓` shows:
- 3 Users (👤)
- 2 Groups (👥)
- 1 Service Principal (💻)
- 1 Unknown principal type (❓)

**Example: azuread_group_member with principal mapping**

```markdown
<details>
<summary>➕ azuread_group_member <b><code>devops_jane</code></b> — <code>👥 DevOps Team</code> (<code>group-100</code>) → <code>👤 Jane Doe</code> (<code>user-100</code>)</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| group_object_id | `group-100` |
| member_object_id | `user-100` |

</details>
```

When principal mapping is provided (`--principal-mapping principals.json`), group and member IDs are resolved to friendly names with appropriate icons. The arrow (`→`) clearly shows the membership relationship: group → member.

**Example: azuread_service_principal**

```markdown
<details>
<summary>➕ azuread_service_principal <b><code>terraform_spn</code></b> — <code>💻 terraform-spn</code> (<code>🆔 app-123-456</code>) Terraform automation service principal</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| display_name | `terraform-spn` |
| application_id | `app-123-456` |
| description | `Terraform automation service principal` |

</details>
```

**Usage:**

```bash
# Without principal mapping (shows IDs)
tfplan2md plan.json

# With principal mapping (resolves names)
tfplan2md --principal-mapping principals.json plan.json

# Docker with principal mapping
docker run -v $(pwd):/data oocx/tfplan2md \
  --principal-mapping /data/principals.json /data/plan.json
```

**Principal mapping file format:**

The nested format organizes principals by type:

```json
{
  "users": {
    "user-guid-1": "Jane Doe",
    "user-guid-2": "John Smith"
  },
  "groups": {
    "group-guid-1": "DevOps Team",
    "group-guid-2": "Platform Team"
  },
  "servicePrincipals": {
    "spn-guid-1": "Terraform Automation"
  }
}
```

The legacy flat format (all principals in one dictionary) is also supported for backwards compatibility.

## Azure DevOps Principal Mapping

**Status:** ✅ Implemented  
**Related specification:** [docs/features/085-azdo-principal-mapping/specification.md](features/085-azdo-principal-mapping/specification.md)

tfplan2md now supports mapping Azure DevOps user IDs, group descriptors, and project IDs to human-readable display names using the same principal mapping JSON file as Azure AD entities. This enhancement makes Terraform plans for Azure DevOps resources significantly more readable by replacing cryptic GUIDs and descriptors with recognizable names.

### Features

- **Three entity types**: Map Azure DevOps users, groups, and projects to display names
- **Automatic resolution**: Entities are automatically resolved in group memberships, team rosters, and project references
- **Unified mapping file**: Add `azdoUsers`, `azdoGroups`, and `azdoProjects` sections to your existing principals.json
- **Consistent display format**: Azure DevOps entities display as `DisplayName (ID)`, matching Azure AD principal format
- **Full descriptor support**: Group descriptors are displayed in full without truncation
- **Diagnostic support**: Debug output shows entity counts and failed resolutions

### Mapping File Format

Add three new optional sections to your principal mapping JSON file:

```json
{
  "users": {
    "00000000-0000-0000-0000-000000000001": "Jane Doe"
  },
  "groups": {
    "00000000-0000-0000-0000-000000000002": "DevOps Team"
  },
  "azdoUsers": {
    "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
    "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
  },
  "azdoGroups": {
    "vssgp.Uy0xLTktMTU1MTM...": "Platform Team",
    "aadgp.Uy0.ReleaseManagers": "Release Managers"
  },
  "azdoProjects": {
    "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project",
    "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": "Application Platform"
  }
}
```

All sections are optional—include only what you need.

### Rendered Output

**Before** (without mapping):
```markdown
#### Members

| Change | Member | Terraform Resource |
| -------- | -------- | -------------------- |
| ➕ | aadgp.Uy0.AliceUser | `azuredevops_group_membership.release_managers_membership_alice` |
| ➕ | aadgp.Uy0.BobUser | `azuredevops_group_membership.release_managers_membership_bob` |
```

**After** (with mapping):
```markdown
#### Members

| Change | Member | Terraform Resource |
| -------- | -------- | -------------------- |
| ➕ | Alice User (aadgp.Uy0.AliceUser) | `azuredevops_group_membership.release_managers_membership_alice` |
| ➕ | Bob Smith (aadgp.Uy0.BobUser) | `azuredevops_group_membership.release_managers_membership_bob` |
```

### Debug Output

When `--debug` is enabled, diagnostic output includes Azure DevOps entity counts and failed resolutions:

```markdown
## Debug Information

### Principal Mapping

Principal Mapping: Loaded successfully from 'principals.json'
- Found 45 principals
- Found 3 azdo users, 2 azdo groups, 1 azdo project

Failed to resolve 1 azdo entity:
- User `unknown-guid` (referenced in `azuredevops_group_membership.example`)
```

### Usage

```bash
# Without mapping
tfplan2md plan.json

# With Azure DevOps mapping
tfplan2md --principal-mapping principals.json plan.json

# Docker with mapping
docker run -v $(pwd):/data oocx/tfplan2md \
  --principal-mapping /data/principals.json \
  /data/plan.json --output /data/plan.md

# With debug output
tfplan2md --debug --principal-mapping principals.json plan.json
```

### Custom Templates

For users writing custom Scriban templates, three new helper functions are available:

- `azdo_user_name(userId)` → resolves Azure DevOps user ID
- `azdo_group_name(groupDescriptor)` → resolves Azure DevOps group descriptor  
- `azdo_project_name(projectId)` → resolves Azure DevOps project ID

These helpers provide explicit control over entity resolution in custom templates, while default rendering automatically resolves entities via value formatters.

**Result:** Azure DevOps resources are easier to review by showing recognizable names instead of GUIDs and descriptors, improving the quality of Azure DevOps infrastructure change reviews.

## Parent-Child Resource Grouping (Inline Child Tables)

**Status:** ✅ Implemented  \
**Related specification:** [docs/features/068-parent-child-resource-grouping/specification.md](features/068-parent-child-resource-grouping/specification.md)

Some Terraform providers model related data in a parent-child pattern: children can be managed either inline on the parent resource (via an attribute like `members`) or as separate standalone resources (like `azuread_group_member`).

tfplan2md detects these configured parent-child relationships and renders the children as tables inside the parent resource section. This reduces scrolling and makes it easier to understand “what belongs to what” during reviews.

### Supported patterns

**Azure Active Directory:**
- `azuread_group` → inline `members` / separate `azuread_group_member`

**Azure DevOps:**
- `azuredevops_group` → inline `members` / separate `azuredevops_group_membership`
- `azuredevops_team` → inline `members` / separate `azuredevops_team_members`
- `azuredevops_team` → inline `administrators` / separate `azuredevops_team_administrators`

**Azure Resource Manager (Network Resources):**
- `azurerm_virtual_network` → inline `subnet` / separate `azurerm_subnet`
- `azurerm_dns_zone` → separate DNS records (`azurerm_dns_a_record`, `azurerm_dns_aaaa_record`, `azurerm_dns_cname_record`, `azurerm_dns_mx_record`, `azurerm_dns_ns_record`, `azurerm_dns_ptr_record`, `azurerm_dns_srv_record`, `azurerm_dns_txt_record`, `azurerm_dns_caa_record`)
- `azurerm_private_dns_zone` → separate private DNS records (same types as public, prefixed with `azurerm_private_dns_*`)
- `azurerm_route_table` → inline `route` / separate `azurerm_route`
- `azurerm_network_security_group` → inline `security_rule` / separate `azurerm_network_security_rule`

### Table behavior

- **Create/Delete**: Child tables omit the “Change” column (the parent action already implies the change).
- **Update/Replace**: Child tables include a “Change” column using the same indicators as resource changes (➕, 🔄, ❌, ⏺️).
- **Terraform Resource column**:
  - Inline children show the inline attribute name (for example: `members attribute`).
  - Separate child resources show their original Terraform address (for example: `azuredevops_group_membership.release_managers_membership_alice`).
- **Mixed inline + separate children**: When both sources appear for the same parent, the report shows a warning and renders both.

### Example (Azure Virtual Network with subnets)

```markdown
<details>
<summary>➕ azurerm_virtual_network <b><code>hub_vnet</code></b> — <code>🆔 vnet-hub</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | ➕ 3 subnets</summary>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 vnet-hub` |
| location | `🌍 eastus` |
| resource_group_name | `📁 rg-network` |
| address_space | `🌐 10.0.0.0/16` |

#### Subnets

| Change | Name | Address Prefixes | NSG | Delegation | Terraform Resource |
| -------- | ------ | ------------------ | ----- | ------------ | -------------------- |
| ➕ | `🆔 snet-app` | `🌐 10.0.1.0/24` | `🛡️ nsg-app` | - | `subnet` attribute |
| ➕ | `🆔 snet-data` | `🌐 10.0.2.0/24` | `🛡️ nsg-data` | - | `subnet` attribute |
| ➕ | `🆔 snet-firewall` | `🌐 10.0.3.0/24` | - | `Microsoft.Network/azureFirewalls` | `subnet` attribute |

</details>
```

### Example (Network Security Group with rules)

```markdown
<details>
<summary>➕ azurerm_network_security_group <b><code>app_nsg</code></b> — <code>🆔 nsg-app-tier</code> in <code>📁 rg-network</code> <code>🌍 eastus</code> | ➕ 4 rules</summary>

| Attribute | Value |
| ----------- | ------- |
| name | `🆔 nsg-app-tier` |
| location | `🌍 eastus` |
| resource_group_name | `📁 rg-network` |

#### Security Rules

| Change | Name | Priority | Direction | Access | Protocol | Source | Destination | Ports | Terraform Resource |
| -------- | ------ | ---------- | ----------- | -------- | ---------- | -------- | ------------- | ------- | -------------------- |
| ➕ | `🆔 allow-https-inbound` | `100` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `🔌 443` | `security_rule` attribute |
| ➕ | `🆔 allow-http-inbound` | `110` | `⬇️ Inbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `✳️` | `🔌 80` | `security_rule` attribute |
| ➕ | `🆔 allow-sql-outbound` | `200` | `⬆️ Outbound` | `✅ Allow` | `🔗 TCP` | `✳️` | `🌐 10.0.2.0/24` | `🔌 1433` | `security_rule` attribute |
| ➕ | `🆔 deny-all-inbound` | `4096` | `⬇️ Inbound` | `⛔ Deny` | `✳️` | `✳️` | `✳️` | `✳️` | `security_rule` attribute |

</details>
```

## Azure API Documentation Mapping

**Status:** ✅ Implemented  
**Related specification:** [docs/features/048-azure-api-doc-mapping/specification.md](features/048-azure-api-doc-mapping/specification.md)

When rendering `azapi_resource` resources, tfplan2md includes links to official Microsoft Learn REST API documentation. Instead of using heuristic URL guessing (which often produced broken links), the tool now uses curated mappings from authoritative Azure sources.

**Features:**
- **Reliable documentation links**: 92 Azure resource types across 37 services have verified documentation URLs
- **Clean degradation**: Unmapped resource types omit the link rather than showing a broken URL
- **Version-agnostic**: API version suffixes (e.g., `@2023-03-01`) are stripped during lookup; mappings work across all versions
- **Fast lookup**: O(1) dictionary lookup using `FrozenDictionary` for optimal performance
- **Embedded mappings**: JSON mappings are compiled into the application for offline use
- **Maintainable**: Discovery script (`scripts/update-azure-api-mappings.py`) automates mapping generation from official sources

**Example output:**

When a mapping exists:
```markdown
**Type:** `Microsoft.Automation/automationAccounts@2021-06-22`

📚 [View API Documentation](https://learn.microsoft.com/rest/api/automation/automation-accounts/)
```

When no mapping exists:
```markdown
**Type:** `Microsoft.UnknownService/unknownResource@2023-01-01`
```
*(No documentation link shown)*

**Supported Azure Services (37 total):**

The current mappings cover major Azure services including Compute, Storage, Networking, Key Vault, SQL, Cosmos DB, Container Instances, API Management, Automation, Analysis Services, Cognitive Services, and many more. See the full list in [AzureApiDocumentationMappings.json](../src/Oocx.TfPlan2Md/Providers/AzApi/Data/AzureApiDocumentationMappings.json).

**Updating mappings:**

Maintainers can refresh mappings by running the discovery script:
```bash
# Generate updated mappings from Microsoft Learn
python3 scripts/update-azure-api-mappings.py

# Validate URLs (slow, not recommended for routine use)
python3 scripts/update-azure-api-mappings.py --validate

# Custom output location
python3 scripts/update-azure-api-mappings.py --output custom-path.json
```

The script scrapes the Azure SDK Specs Inventory to discover all Azure resource types and their documentation URLs, ensuring mappings stay current with Microsoft's official documentation structure.

## Distribution

### Docker Image

tfplan2md is distributed as a minimal Docker image optimized for CI/CD pipelines:

- **Size**: 14.7MB (89.6% reduction from standard .NET runtime)
- **Base image**: FROM scratch with minimal musl libraries for maximum security
- **Compilation**: NativeAOT-compiled native binary (no JIT overhead)
- **Security**: Non-root user (UID 1654), no shell, minimal attack surface
- **Registry**: Docker Hub (`oocx/tfplan2md`)
- **Tagging**: Semantic versioning with multiple tags per release:
  - Full version: `1.2.3`
  - Minor version: `1.2` (updated with each patch)
  - Major version: `1` (updated with each minor/patch)
  - `latest` (always points to the most recent release)

#### Performance Characteristics

- **Image download**: 89.6% faster than standard .NET runtime (141MB → 14.7MB)
- **Container startup**: Instant startup with native binary (no JIT compilation)
- **Build time**: ~90 seconds (2x baseline) - acceptable tradeoff for deployment benefits
- **Runtime performance**: Native code execution without JIT overhead

#### Technical Details

The Docker image uses NativeAOT (Ahead-of-Time) compilation with full trimming to produce a self-contained native binary:

- **Runtime**: linux-musl-x64 (Alpine-based)
- **Libraries**: Only 3 essential files (ld-musl-x86_64.so.1, libgcc_s.so.1, libstdc++.so.6)
- **Trimming**: TrimMode=full with aggressive size optimizations
- **Globalization**: Invariant globalization (no culture-specific assemblies)
- **Reflection**: All reflection patterns handled through explicit mapping (AotScriptObjectMapper)

All features work identically to the standard build - templates, principal mapping, metadata extraction, and embedded resources are fully functional with AOT.

### Usage Example

```bash
# From stdin (using latest)
terraform show -json plan.tfplan | docker run -i oocx/tfplan2md

# Pin to specific version
docker run -i oocx/tfplan2md:1.2.3

# Pin to minor version (get patch updates automatically)
docker run -i oocx/tfplan2md:1.2

# From file (mounted volume)
docker run -v $(pwd):/data oocx/tfplan2md /data/plan.json --output /data/plan.md

# With principal mapping (mount file from host)
docker run -v $(pwd):/data oocx/tfplan2md \
  --principal-mapping /data/principals.json \
  /data/plan.json --output /data/plan.md

# With principal mapping (mount as read-only)
docker run \
  -v $(pwd)/plan.json:/data/plan.json:ro \
  -v $(pwd)/principals.json:/app/principals.json:ro \
  oocx/tfplan2md --principal-mapping /app/principals.json /data/plan.json
```

### Pre-built Binaries

**Status:** ✅ Implemented (Phase 1: Linux x64)

Starting with the next release, tfplan2md provides pre-built native binaries as GitHub Release assets alongside the Docker image. These self-contained executables enable usage in environments without container runtime support.

#### Current Platform Support

**Phase 1 (Available):**
- **linux-x64** - Standard Linux distributions with glibc (Ubuntu, Debian, Fedora, RHEL, CentOS, etc.)

**Phase 2 (Planned):**
- linux-arm64 - ARM-based cloud instances and servers
- darwin-arm64 - macOS Apple Silicon (M1/M2/M3) ✅ **Now Available**
- darwin-x64 - macOS Intel ✅ **Now Available**
- win-x64 - Windows 10/11/Server ✅ **Now Available**

**Phase 3 (Future):**
- linux-musl-x64 - Alpine Linux standalone
- linux-musl-arm64 - ARM Alpine

**Note:** Windows ARM64 builds are not currently provided due to lack of native ARM64 Windows runners in GitHub Actions. Windows ARM64 users can use the x64 binary (runs via emulation on Windows 11) or the Docker image.

#### Binary Characteristics

- **Self-contained**: No .NET runtime or external dependencies required
- **Native AOT compiled**: Fast startup, optimized performance
- **Single executable**: One file per platform, no installation needed
- **Compressed size**: ~5MB tar.gz archive (linux-x64)
- **Security**: SHA256 checksums provided for verification
- **Naming convention**: `tfplan2md_<version>_<os>_<arch>.tar.gz` (OpenTofu-style)

#### Download and Usage

```bash
# Download binary and checksum
VERSION="1.x.x"  # Replace with desired version
wget https://github.com/oocx/tfplan2md/releases/download/v${VERSION}/tfplan2md_${VERSION}_linux_x64.tar.gz
wget https://github.com/oocx/tfplan2md/releases/download/v${VERSION}/SHA256SUMS

# Verify integrity
sha256sum -c SHA256SUMS --ignore-missing

# Extract
tar -xzf tfplan2md_${VERSION}_linux_x64.tar.gz

# Run
./tfplan2md --help
terraform show -json plan.tfplan | ./tfplan2md > plan.md
```

#### Use Cases

Pre-built binaries are ideal for:

- **Closed/air-gapped systems**: Organizations that cannot pull images from public Docker registries
- **Non-containerized environments**: Systems without Docker or container runtime installed
- **Local development**: Quick testing without Docker overhead
- **CI/CD without containers**: Build agents or legacy systems without container support
- **Simplified deployment**: Direct binary execution without container orchestration

For containerized environments, the Docker image remains the recommended distribution method due to its minimal size (14.7MB) and security characteristics.

### Homebrew Installation

**Status:** ✅ Implemented (Feature 089)

tfplan2md is available via Homebrew for macOS and Linux users, providing a convenient package manager experience with automatic updates.

#### Installation

```bash
# Add the tfplan2md tap
brew tap oocx/tfplan2md

# Install tfplan2md
brew install tfplan2md

# Verify installation
tfplan2md --version
```

#### Supported Platforms

- **macOS x64** - Intel-based Macs (macOS 10.15+)
- **macOS ARM64** - Apple Silicon Macs (M1/M2/M3, macOS 11+)
- **Linux x64** - Linux distributions with Homebrew (including WSL)

#### Features

- **Automatic updates**: Get notified of new versions with `brew outdated`
- **Simple upgrades**: Update to the latest version with `brew upgrade tfplan2md`
- **No manual downloads**: Homebrew handles binary download, verification, and PATH configuration
- **Platform detection**: Formula automatically selects the correct binary for your platform
- **Security**: SHA256 checksum verification for all downloads

#### How It Works

The Homebrew formula downloads pre-built native binaries from GitHub Releases (the same binaries available for direct download). The formula:
- Detects your platform (macOS Intel, macOS ARM64, or Linux x64)
- Downloads the appropriate archive from GitHub Releases
- Verifies the SHA256 checksum
- Installs the binary to Homebrew's bin directory
- Adds the binary to your PATH automatically

The formula is automatically updated when new stable releases are published.

#### Use Cases

Homebrew installation is ideal for:
- **macOS users**: Standard installation method on macOS
- **Development environments**: Local testing without Docker
- **WSL users**: Linux package management on Windows
- **Version management**: Easy upgrades and rollbacks via Homebrew
- **Team standardization**: Consistent installation across development teams

For CI/CD pipelines, the Docker image remains the recommended distribution method due to its containerized isolation.

### Releases

Docker images and pre-built binaries are automatically built and pushed when a new version tag is created. See [spec.md](spec.md) for details on the CI/CD process and versioning strategy.

## Resource-Specific Templates

Complex Terraform resources like firewall rule collections can have misleading diffs when using simple attribute-based comparison. When items shift indices in arrays, the default diff shows changes to every attribute after the insertion point.

Resource-specific templates solve this by:
- Comparing collection items semantically by key (e.g., rule name)
- Showing which items were added, removed, modified, or unchanged
- Providing clear, table-based output for complex nested structures

When rendering the full report, the default renderer applies resource-specific templates automatically for any resources that have a matching template; if none exists, the global default template is used.

### Supported Resources

| Provider | Resource Type | Template |
|----------|--------------|----------|
| azapi | `azapi_resource` | Flattened body representation with dot notation |
| azurerm | `azurerm_firewall_application_rule_collection` | Application firewall rule diffing with FQDN targets |
| azurerm | `azurerm_firewall_network_rule_collection` | Network firewall rule diffing with IP/port targets |
| azurerm | `azurerm_network_security_group` | Security rule diffing with `diff_array` |
| azuredevops | `azuredevops_build_definition` | Pipeline configuration tables with secret variable protection |
| azuredevops | `azuredevops_variable_group` | Variable changes with secret value protection |

#### azapi_resource

The `azapi_resource` resource type from the AzAPI Terraform provider manages Azure resources via the Azure Resource Manager REST API. Most configuration resides in a JSON `body` attribute, making changes difficult to review without parsing dense JSON. The custom template transforms this JSON into scannable markdown tables using dot notation for property paths.

**Key Features:**
- **Flattened body representation**: JSON properties displayed in tables with dot notation (e.g., `properties.sku.name`)
- **Before/after comparison**: Update operations show changed properties with before/after values
- **Smart array filtering**: For update operations, only changed array items are displayed in nested array structures (e.g., if `allOf[4]` changes, only that item is shown, not all items in the `allOf` array)
- **Auto-generated documentation links**: Best-effort links to Azure REST API documentation on Microsoft Learn
- **Per-property sensitive masking**: Respects Terraform's sensitivity markers at the property level (not just entire body)
- **Large value handling**: Properties over 200 characters moved to collapsible sections
- **All operations supported**: Create, update, delete, and replace actions handled appropriately

**Example output for create operation:**

```markdown
**Type:** `Microsoft.Automation/automationAccounts@2021-06-22`

📚 [View API Documentation](https://learn.microsoft.com/rest/api/automation/automation-accounts/)

| Attribute | Value |
|-----------|-------|
| name | `completeAccount` |
| parent_id | Resource Group `rg-complete-azapi` |
| location | 🌍 `westeurope` |

**Tags:**
 `environment: production` `cost-center: engineering`

#### Body

| Property | Value |
|----------|-------|
| properties.sku.name | `Basic` |
| properties.disableLocalAuth | ✅ `true` |
| properties.publicNetworkAccess | ✅ `true` |
```

**Example output for update operation:**

```markdown
**Type:** `Microsoft.Automation/automationAccounts@2021-06-22`

📚 [View API Documentation](https://learn.microsoft.com/rest/api/automation/automation-accounts/)

| Attribute | Value |
|-----------|-------|
| name | `updateAccount` |
| parent_id | Resource Group `rg-update-azapi` |
| location | 🌍 `westeurope` |

#### Body Changes

| Property | Before | After |
|----------|--------|-------|
| properties.sku.name | `Basic` | `Standard` |
| properties.disableLocalAuth | ❌ `false` | ✅ `true` |
```

**Documentation Links:**

Documentation links are generated from curated mappings of Azure resource types to their official Microsoft Learn REST API documentation URLs. The mappings currently cover 92 Azure resource types across 37 services including Compute, Storage, Networking, Key Vault, SQL, Cosmos DB, and more.

When a mapping exists, the resource displays a reliable documentation link. When no mapping exists for a resource type, the link is omitted rather than showing a potentially broken URL. This ensures users only see links that work correctly. See [Azure API Documentation Mapping](#azure-api-documentation-mapping) for more details.

**Sensitive Value Handling:**

The template respects Terraform's per-property sensitivity markers. If Terraform marks specific properties as sensitive (e.g., `properties.administratorLoginPassword`), only those properties are masked unless the `--show-sensitive` flag is used. This provides better visibility than masking the entire body.

**Large Properties:**

Properties with values exceeding 200 characters are automatically moved to a collapsible "Large body properties" section below the main table. This keeps the main view scannable while preserving access to detailed configuration values.

#### Firewall Rule Collections

Azure Firewall supports both network rules (IP/port-based filtering) and application rules (FQDN-based filtering). tfplan2md provides specialized templates for both resource types.

##### Network Rule Collections

For `azurerm_firewall_network_rule_collection`, network rules are rendered in a single table showing IP-based filtering rules. Each rule displays: Name, Protocols, Source Addresses, Destination Addresses, Destination Ports, and Description.

- **Added rules**: Shown with ➕ icon and the new values.
- **Removed rules**: Shown with ❌ icon and the old values.
- **Modified rules**: Shown with 🔄 icon. Changed attributes display both before and after values in the same cell, prefixed with `-` and `+` respectively, separated by `<br>` for visual clarity. Unchanged attributes show the single value without any prefix.
- **Unchanged rules**: Shown with ⏺️ icon for completeness.

Example of a modified network rule with changed source addresses:
```markdown
| 🔄 | allow-http | TCP | - 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24 | * | 80 | - Allow HTTP traffic<br>+ Allow HTTP traffic from web and API tiers |
```

##### Application Rule Collections

For `azurerm_firewall_application_rule_collection`, application rules are rendered in a single table showing FQDN-based filtering rules. Each rule displays: Name, Protocols (HTTP/HTTPS/MSSQL with ports), Source Addresses, Target FQDNs, and Description.

Application rules use protocol+port format (e.g., `Https:443`, `Http:80,8080`) instead of separate protocol and port columns. Long FQDN lists (>5 items) are truncated to the first 3 items with "... +N more" to maintain table readability.

- **Change indicators**: Same as network rules (➕, 🔄, ❌, ⏺️)
- **Optional properties**: Source IP Groups and FQDN Tags are shown when present
- **Inline diffs**: Modified properties show before/after values with `-` and `+` prefixes

Example of a modified application rule with changed FQDNs:
```markdown
| 🔄 | allow-microsoft | Https:443 | 10.0.1.0/24 | - *.microsoft.com<br>+ *.microsoft.com, *.azure.com | Microsoft services |
```

Both templates make it easy to inspect per-rule changes without index-shift noise from array diffs, clearly showing which rules were added, modified, or removed.

#### Network Security Groups

For `azurerm_network_security_group`, security rules are rendered in a single table with columns for Name, Priority, Direction, Access, Protocol, Source Addresses, Source Ports, Destination Addresses, Destination Ports, and Description. Rules are matched by name, categorized (➕, 🔄, ❌, ⏺️), and sorted by ascending priority.

- **Added rules**: ➕ with the new values.
- **Removed rules**: ❌ with the old values.
- **Modified rules**: 🔄 with before/after values in the same cell using `-` and `+` prefixes separated by `<br>`.
- **Unchanged rules**: ⏺️ for completeness.

Example of a modified NSG rule with a port change and description update:

```markdown
| 🔄 | allow-http | 110 | Inbound | Allow | Tcp | - *<br>+ 10.0.1.0/24, 10.0.2.0/24 | * | * | - 80<br>+ 8080 | - Allow HTTP<br>+ Allow alternate HTTP |
```

#### Azure DevOps Build Definitions

For `azuredevops_build_definition`, pipeline configuration is displayed as structured tables for variables, triggers, repository settings, schedules, and jobs. This solves the common problem where Terraform marks all build definition nested blocks as sensitive, making it impossible to review pipeline configuration changes.

**Features:**
- **Variables table**: Shows all pipeline variables with Name, Value, Is Secret, and Allow Override columns
- **Secret protection**: Secret variables (`is_secret: true`) display as `(sensitive / hidden)` while showing all metadata
- **Semantic diffing**: Variables matched by name across before/after states, showing Added (➕), Modified (🔄), Removed (❌), or Unchanged (⏺️)
- **Trigger tables**: CI triggers, pull request triggers, and schedules displayed as separate tables when present
- **Repository table**: Source repository configuration showing type, repo ID, branch, YAML path, and build status reporting
- **Jobs table**: Job definitions displayed when present (name, condition, timeout)
- **Conditional rendering**: Tables only appear when the corresponding blocks contain data—no empty tables shown

**Table columns (Variables):**
- **Change**: Icon indicating the type of change (update operations only)
- **Name**: Variable name (formatted as inline code)
- **Value**: Variable value or `(sensitive / hidden)` for secret variables
- **Is Secret**: Boolean flag indicating if the variable is a secret
- **Allow Override**: Boolean flag indicating if the variable can be overridden at queue time

**Example output for update operation:**

```markdown
**Pipeline Name:** `example-pipeline`

**Path:** `\\Pipelines`

#### Variables

| Change | Name | Value | Is Secret | Allow Override |
| ------ | ---- | ----- | --------- | -------------- |
| ➕ | `NEW_VAR` | `value` | `false` | `true` |
| 🔄 | `BUILD_CONFIG` | - `Debug`<br>+ `Release` | `false` | `true` |
| 🔄 | `API_KEY` | `(sensitive / hidden)` | - `false`<br>+ `true` | `true` |
| ❌ | `OLD_VAR` | `old-value` | `false` | `true` |

#### CI Trigger

| Use YAML | Override (Branch Filters) |
| -------- | ------------------------- |
| `true` | - |

#### Repository

| Type | Repo ID | Branch | YAML Path | Report Build Status |
| ---- | ------- | ------ | --------- | ------------------- |
| `TfsGit` | `80128bc2-17ff-45f8-ad59-d7609a605c75` | `refs/heads/master` | `azure-pipelines.yml` | `true` |
```

Note how the `API_KEY` variable shows its metadata (name, is_secret flag, allow_override) but displays `(sensitive / hidden)` instead of the actual secret value. Modified attributes show before/after values with `-` and `+` prefixes; unchanged attributes show a single value without prefix.

This template makes it possible to review build definition changes in CI/CD pipelines without exposing secret values, while providing full visibility into pipeline variables, triggers, and repository configuration.

#### Azure DevOps Variable Groups

For `azuredevops_variable_group`, variables from both the `variable` and `secret_variable` arrays are displayed in a unified table showing all metadata. This solves the common problem where Terraform marks all variable group content as sensitive, making it impossible to review changes.

**Features:**
- **Unified display**: Shows all variables (regular and secret) in a single table
- **Secret protection**: Secret variable values display as `(sensitive / hidden)` while showing all metadata (name, enabled, content_type, expires)
- **Semantic diffing**: Variables matched by name across before/after states, showing Added (➕), Modified (🔄), Removed (❌), or Unchanged (⏺️)
- **Large value handling**: Connection strings and multi-line values moved to collapsible section
- **Key Vault integration**: Displays Key Vault connection metadata in separate table when present

**Table columns:**
- **Change**: Icon indicating the type of change (update operations only)
- **Name**: Variable name (formatted as inline code)
- **Value**: Variable value or `(sensitive / hidden)` for secrets
- **Enabled**: Whether the variable is enabled (true/false/-)
- **Content Type**: Content type metadata if specified
- **Expires**: Expiration date if specified

**Example output for update operation:**

```markdown
**Variable Group:** `example-variables`

**Description:** `Variable group for example pipeline`

#### Variables

| Change | Name | Value | Enabled | Content Type | Expires |
| ------ | ---- | ----- | ------- | ------------ | ------- |
| ➕ | `ENV` | `Production` | - | - | - |
| 🔄 | `APP_VERSION` | - `1.0.0`<br>+ `2.0.0` | `false` | - | - |
| 🔄 | `SECRET_KEY` | `(sensitive / hidden)` | - `true`<br>+ `false` | - | - |
| ❌ | `ENVIRONMENT` | `development` | `false` | - | - |
```

Note how the `SECRET_KEY` variable shows its metadata (name, enabled status) but displays `(sensitive / hidden)` instead of the actual secret value. Modified attributes show before/after values with `-` and `+` prefixes; unchanged attributes show a single value without prefix.

**Key Vault integration example:**

When a variable group is linked to Azure Key Vault, the connection metadata is displayed:

```markdown
#### Key Vault Integration

| Name | Service Endpoint ID | Search Depth |
| ---- | ------------------- | ------------ |
| `my-keyvault` | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` | `1` |
```

This template makes it possible to review variable group changes in CI/CD pipelines without exposing secret values.

### Helper Functions

Templates have access to custom Scriban helper functions:

**`diff_array`** - Semantic collection diffing:
```scriban
{{ diff = diff_array before_json.rule after_json.rule "name" }}
{{ for rule in diff.added }}
  ➕ {{ rule.name }}
{{ end }}
```

**`format_diff`** - Before/after diff formatting:
```scriban
{{ format_diff (item.before.protocols | array.join ", ") (item.after.protocols | array.join ", ") }}
```
Returns the single escaped value if unchanged, or `"- escapedBefore<br>+ escapedAfter"` if different. Values are escaped for markdown safety while the `<br>` tag is preserved to render as a line break in tables.

**AzAPI-specific helpers** (for `azapi_resource` template):

**`flatten_json`** - Flattens JSON into dot-notation key-value pairs:
```scriban
{{ flattened = flatten_json change.after_json.body "" }}
{{ for prop in flattened }}
  {{ prop.path }}: {{ prop.value }}
{{ end }}
```
Converts nested JSON objects into flat property paths (e.g., `properties.sku.name`). Values exceeding 200 characters are marked with `is_large: true` for separate rendering.

**`compare_json_properties`** - Compares before/after JSON and returns changed properties:
```scriban
{{ comparisons = compare_json_properties before_json.body after_json.body before_sensitive after_sensitive false false }}
{{ for prop in comparisons }}
  {{ prop.path }}: {{ prop.before }} → {{ prop.after }}
{{ end }}
```
Parameters: beforeJson, afterJson, beforeSensitive, afterSensitive, showUnchanged, showSensitive. Returns list with path, before, after, is_large, is_sensitive, is_changed.

**`parse_azure_resource_type`** - Parses Azure resource type strings:
```scriban
{{ type_info = parse_azure_resource_type "Microsoft.Automation/automationAccounts@2021-06-22" }}
Provider: {{ type_info.provider }}       // "Microsoft.Automation"
Service: {{ type_info.service }}         // "Automation"
Type: {{ type_info.resource_type }}      // "automationAccounts"
API Version: {{ type_info.api_version }} // "2021-06-22"
```

**`azure_api_doc_link`** - Generates Azure REST API documentation URLs from curated mappings:
```scriban
{{ doc_link = azure_api_doc_link "Microsoft.Automation/automationAccounts@2021-06-22" }}
// Returns: "https://learn.microsoft.com/rest/api/automation/automation-accounts/"
```
Uses curated mappings from Microsoft Learn for 92 Azure resource types. Returns null for unmapped resources or non-Microsoft resource types. API version suffixes are stripped during lookup.

**`extract_azapi_metadata`** - Extracts key attributes from azapi_resource:
```scriban
{{ metadata = extract_azapi_metadata change }}
Name: {{ metadata.name }}
Type: {{ metadata.type }}
Location: {{ metadata.location }}
Parent: {{ metadata.parent_id }}
Tags: {{ metadata.tags }}
```

See [resource-specific-templates.md](features/resource-specific-templates.md) for full specification.

## Markdown Rendering Quality

tfplan2md ensures high-quality markdown output that renders correctly in all markdown viewers:

- **Heading spacing**: All headings are automatically preceded by a blank line, preventing content from running into headings
- **Table formatting**: Blank lines before table rows are collapsed to prevent table breakage
- **Consistent structure**: Proper document hierarchy with module sections (H3) and resource entries (H4)

These improvements are applied automatically during rendering and require no configuration.

## Template Rendering Simplification

**Status:** ✅ Implemented

The template system has been simplified to enable faster, more reliable feature development by both humans and AI agents. This architectural improvement eliminates complexity without changing user-facing output.

**Key Changes:**

- **Direct rendering**: Templates now generate output directly in a single pass. The old render-then-replace mechanism with HTML anchor comments has been eliminated.
- **Logic in C#, not templates**: Value transformation and formatting logic has been moved from Scriban `func` definitions into C# code (model properties and helper functions). Templates now focus purely on layout.
- **Cleaner templates**: All built-in templates are under 100 lines with no `func` definitions, making them easier to understand and maintain.
- **Clear patterns**: When adding formatting features (like new icons), developers now have a clear, single location for implementation instead of scattered changes across templates.

**Benefits:**

- **Faster development**: New formatting features can be added in a single file rather than across multiple templates
- **Better testing**: Compile-time and test-time detection of incomplete implementations
- **Easier maintenance**: Templates are pure layout with discoverable helper functions
- **AI-friendly**: Simpler architecture enables AI agents to implement features without trial-and-error

**For Template Authors:**

Custom templates no longer need to emit HTML anchor comments. Simply focus on layout and use the available helper functions for value formatting. All formatting capabilities are discoverable through the model object and Scriban helper functions.

**Metrics Achieved:**
- Zero HTML anchor comments in templates (eliminated render-then-replace)
- Zero `func` definitions in templates (logic moved to C#)
- All templates under 100 lines (57-83 lines)
- Single-file changes for formatting features

See [docs/features/026-template-rendering-simplification/](features/026-template-rendering-simplification/) for technical details.

## Provider Code Separation

**Status:** ✅ Implemented

The codebase is now organized with clear separation between Terraform provider-specific code (azapi, azurerm, azuredevops) and output platform-specific code (GitHub vs Azure DevOps rendering). This architectural improvement benefits developers working on tfplan2md without affecting end users.

**Key Changes:**

- **Provider modules**: All provider-specific code (templates, helpers, view models) is now isolated in dedicated `Providers/` folders with matching namespaces
- **Explicit registration**: Each provider implements `IProviderModule` and registers explicitly via `ProviderRegistry` (no reflection, maintains AOT compatibility)
- **Render target abstraction**: Platform-specific rendering logic (GitHub vs Azure DevOps) moved to `RenderTargets/` with `IDiffFormatter` interface
- **CLI update**: The `--render-target` flag replaces `--large-value-format` for clearer intent

**Folder Structure:**

```
src/Oocx.TfPlan2Md/
├── Providers/
│   ├── AzApi/           # AzApi provider (azapi_resource, azapi_update_resource)
│   ├── AzureRM/         # AzureRM provider (azurerm_*)
│   └── AzureDevOps/     # AzureDevOps provider (azuredevops_*)
├── RenderTargets/
│   ├── GitHub/          # GitHub PR rendering
│   └── AzureDevOps/     # Azure DevOps PR rendering
└── Platforms/
    └── Azure/           # Shared Azure utilities (principal mapping, role names)
```

**Benefits for Contributors:**

- **Easy navigation**: All code for a specific provider is in one place
- **Clear boundaries**: Explicit interfaces define provider responsibilities
- **Maintainability**: Adding support for new providers follows a clear pattern
- **Discoverability**: Folder structure makes it obvious which providers are supported

**CLI Changes:**

The `--render-target` flag now explicitly controls platform-specific rendering:

```bash
# For GitHub PR comments (simple diff format)
tfplan2md plan.json --render-target github

# For Azure DevOps PR comments (inline diff format)
tfplan2md plan.json --render-target azuredevops
```

**Migration note:** The deprecated `--large-value-format` flag now throws a helpful error directing users to `--render-target`.

See [docs/features/047-provider-code-separation/](features/047-provider-code-separation/) for technical details and the [Provider Development Guide](../src/Oocx.TfPlan2Md/Providers/README.md) for adding new providers.

## Static Code Analysis Integration

**Status:** ✅ Implemented

Integrate security and quality findings from static analysis tools (Checkov, Trivy, TFLint, Semgrep) directly into Terraform plan reports. Findings are displayed inline with affected resources, creating a unified view for PR reviews.

### Features

- **SARIF 2.1.0 format support**: Accept code analysis results in standard SARIF format
- **Multiple file support**: Load findings from multiple SARIF files with wildcard patterns
- **Resource mapping**: Findings are automatically mapped to Terraform resources and attributes
- **Severity levels**: Critical, High, Medium, Low, and Informational with visual indicators
- **Tool identification**: Each finding displays the source tool name (Checkov, Trivy, tflint, Semgrep, etc.) in a dedicated column
- **Remediation links**: Clickable links to fix guides and best practices
- **Summary metrics**: Count of findings by severity and list of tools used
- **Unmatched findings**: Findings that can't be mapped to resources appear in a separate section
- **Severity filtering**: Display only findings at or above specified severity level
- **CI/CD integration**: Exit with error code when critical/high findings are detected
- **Provider support**: Works with specialized templates (Azure AD, API Management, etc.)
- **Error handling**: Invalid SARIF files are skipped with detailed warnings in report

### Usage

```bash
# Basic usage
tfplan2md plan.json --code-analysis-results checkov-results.sarif

# Multiple files with wildcards
tfplan2md plan.json \
  --code-analysis-results "*.sarif" \
  --code-analysis-results "security/*.sarif"

# Filter by severity
tfplan2md plan.json \
  --code-analysis-results "*.sarif" \
  --code-analysis-minimum-level high

# Fail on critical/high findings (for CI/CD)
tfplan2md plan.json \
  --code-analysis-results "*.sarif" \
  --fail-on-static-code-analysis-errors high
```

### CLI Flags

| Flag | Description |
|------|-------------|
| `--code-analysis-results <pattern>` | SARIF file pattern (can be specified multiple times) |
| `--code-analysis-minimum-level <level>` | Minimum severity to display (critical, high, medium, low, informational) |
| `--fail-on-static-code-analysis-errors <level>` | Exit with code 10 when findings at or above this level exist |

### Report Output

**Code Analysis Summary** (at top of report):
```markdown
## Code Analysis Summary

**Status:** ⚠️ 5 critical findings require attention

| Severity | Count |
|----------|-------|
| 🚨 Critical | 5 |
| ⚠️ High | 12 |
| ⚠️ Medium | 23 |
| ℹ️ Low | 8 |
| ℹ️ Informational | 15 |

**Tools Used:** Checkov, Trivy, TFLint
```

**Per-Resource Findings**:
```markdown
### aws_security_group.allow_tls (update)

**Security & Quality:** 🚨 2 critical, ⚠️ 1 high

| Severity | Tool | Attribute | Finding | Remediation |
|----------|------|-----------|---------|-------------|
| 🚨 Critical | Checkov | ingress.0.cidr_blocks | Public ingress rule detected | [Fix Guide](link) |
| ℹ️ Low | tflint | description | Missing security group description | [Best Practices](link) |
```

**Unmatched Findings** (at end of report):
```markdown
## Other Findings

### Module: network-module
| Severity | Tool | Finding | Remediation |
|----------|------|---------|-------------|
| ⚠️ High | Trivy | Module uses deprecated provider version | [Upgrade Guide](link) |
```

### Tool Compatibility

Tested with current versions of:
- Checkov (IaC security scanner)
- Trivy (vulnerability scanner)
- TFLint (linting)
- Semgrep (pattern-based analysis)

Any tool that outputs SARIF 2.1.0 format is supported.

### Example

See [examples/code-analysis/](../examples/code-analysis/) for a complete example with sample SARIF file and generated report.

See [docs/features/056-static-analysis-integration/](features/056-static-analysis-integration/) for specification, architecture, and implementation details.

## Tool Column in Findings Tables

**Status:** ✅ Implemented

Findings tables now include a "Tool" column that displays the name of the security or quality tool that generated each finding. This enhancement improves clarity when reviewing findings from multiple analysis tools in a single report.

### Overview

When using multiple static analysis tools (Checkov, Trivy, tflint, Semgrep, etc.), it's essential to know which tool produced each finding to assess its relevance and credibility. The Tool column makes this information immediately visible in all findings tables.

### Column Structure

**Security & Quality Findings Table** (per-resource):
- Column order: `Severity | Tool | Attribute | Finding | Remediation`
- The Tool column appears between Severity and Attribute columns

**Other Findings Tables** (module-level and unmatched):
- Column order: `Severity | Tool | Finding | Remediation`
- The Tool column appears between Severity and Finding columns

### Tool Name Display

- **Tool names**: Displayed exactly as provided in SARIF files (e.g., "Checkov", "Trivy", "tflint", "Semgrep")
- **Missing tool names**: Displayed as "-" when tool information is not available in the SARIF file
- **Name only**: Only the tool name is shown (not version), keeping tables compact and readable

### Example

```markdown
#### 🔒 Security & Quality Findings

| Severity | Tool | Attribute | Finding | Remediation |
| -------- | ---- | --------- | ------- | ----------- |
| ⚠️ High | Checkov | - | Ensure storage account uses secure transfer<br/>Rule: `CKV_AZURE_3` | [Details](link) |
| ⚠️ Medium | Trivy | `public_network_access_enabled` | Key Vault should have public network access disabled<br/>Rule: `AVD-AZU-0013` | - |
| ℹ️ Low | tflint | - | Virtual Network should have custom DNS servers configured<br/>Rule: `AVD-AZU-0047` | - |
```

### Benefits

- **Multi-tool clarity**: Immediately identify which scanner found each issue
- **Tool credibility**: Assess findings based on the tool's focus area (e.g., Checkov for cloud security, tflint for Terraform-specific checks)
- **Better triage**: Prioritize findings based on tool specialization and your trust in each tool
- **Professional appearance**: Follows common security report patterns

See [docs/features/059-tool-column-findings-tables/](features/059-tool-column-findings-tables/) for architecture and implementation details.

## Resource Details Display Mode Control

**Status:** ✅ Implemented

The `--details` CLI option gives you control over whether resource details blocks are initially expanded or collapsed in the generated markdown report. This feature is particularly useful for large plans or when focusing attention on resources with code analysis findings.

### Features

- **Three display modes**: Choose how resource details blocks are initially rendered
  - `auto` (default): Automatically expand resources with code analysis findings, collapse others
  - `open`: Expand all resource details blocks
  - `closed`: Collapse all resource details blocks
- **Smart auto mode**: Integrates with Static Code Analysis Integration (Feature 056) to automatically expand resources that need attention
- **Merged child handling**: In auto mode, parent resources are expanded if any merged child resource has code analysis findings
- **Manual override**: Users can always manually expand/collapse details blocks by clicking the summary line
- **Consistent debug behavior**: Debug information blocks (enabled with `--debug`) remain collapsed regardless of `--details` setting
- **Backward compatible**: Default behavior is `auto`, which preserves current behavior

### Usage

```bash
# Collapse all resources by default (clean, scannable view)
tfplan2md --details closed plan.json

# Expand all resources by default
tfplan2md --details open plan.json

# Auto mode: expand only resources with code analysis findings (default)
tfplan2md --details auto \
  --code-analysis-results checkov-results.sarif \
  plan.json

# Auto mode is the default when --details is not specified
tfplan2md --code-analysis-results checkov-results.sarif plan.json
```

### CLI Flag

| Flag | Description |
|------|-------------|
| `--details <auto\|open\|closed>` | Control resource details display: `auto` (expand resources with findings, default), `open` (expand all), `closed` (collapse all) |

### Display Modes

#### Auto Mode (Default)

Automatically expand resources that have code analysis findings attached, collapse others. This mode helps focus attention on security and quality issues while keeping the report clean and scannable.

**Behavior:**
- Resources **with** code analysis findings: Rendered as `<details open>` (expanded)
- Resources **without** findings: Rendered as `<details>` (collapsed)
- Parent resources with merged children: Expanded if any child has findings
- Debug block: Always collapsed (not affected by this setting)

**Use cases:**
- Security-focused reviews where you want to immediately see resources with findings
- Large plans where most resources are clean and only a few need attention
- Default behavior that works well for most scenarios

**Example output:**
```html
<!-- Resource with findings: expanded -->
<details open>
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>stdata</code></summary>
<br>

**Security & Quality:** 🚨 1 critical, ⚠️ 2 high

| Severity | Tool | Attribute | Finding | Remediation |
|----------|------|-----------|---------|-------------|
| 🚨 Critical | Checkov | ... | ... | ... |
</details>

<!-- Resource without findings: collapsed -->
<details>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code></summary>
<br>
{content}
</details>
```

#### Open Mode

Expand all resource details blocks by default. All resources are immediately visible without requiring manual expansion.

**Behavior:**
- All resources: Rendered as `<details open>` (expanded)
- Debug block: Always collapsed (exception)

**Use cases:**
- Small plans where reviewing all details is manageable
- Detailed reviews where you want to see everything
- Teams that prefer maximum visibility by default

#### Closed Mode

Collapse all resource details blocks by default. Provides a clean, scannable overview where you can selectively expand resources of interest.

**Behavior:**
- All resources: Rendered as `<details>` (collapsed)
- Debug block: Always collapsed

**Use cases:**
- Large plans with dozens or hundreds of resources
- High-level reviews where you want to scan summaries first
- Clean PR comments that don't dominate the review interface

### Integration with Code Analysis

The `auto` mode is designed to work seamlessly with the Static Code Analysis Integration feature (Feature 056). When you provide SARIF files via `--code-analysis-results`, resources with findings are automatically expanded:

```bash
# Resources with Checkov findings will be expanded automatically
tfplan2md --details auto \
  --code-analysis-results checkov-results.sarif \
  --code-analysis-results trivy-results.sarif \
  plan.json
```

This creates a powerful workflow where security and quality issues are immediately visible, while clean resources remain collapsed to reduce noise.

### Technical Details

- **HTML implementation**: Controls the `open` attribute on `<details>` elements
- **Template integration**: Uses a Scriban helper function (`details_open_attr`) to determine whether to render the `open` attribute
- **Performance**: No impact on rendering performance; decision is made once per resource during template rendering
- **Validation**: Invalid mode values are rejected with a clear error message

See [docs/features/092-details-display-mode/](features/092-details-display-mode/) for specification, architecture, and implementation details.

## Future Considerations

The following features may be added in future versions:

- Additional resource-specific templates based on user feedback
- Provider-default templates (e.g., `Templates/azurerm/default.sbn`)
- `--list-templates` CLI option to list bundled templates
