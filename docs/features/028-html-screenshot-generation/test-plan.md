# Test Plan: HTML Screenshot Generation Tool

## Overview

This test plan covers the standalone .NET tool for generating screenshots from HTML files using Playwright and Chromium. It ensures that the CLI behaves correctly, validates inputs, and produces high-quality screenshots in various formats and sizes.

Reference: [specification.md](specification.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Standalone .NET 10 console application | TC-01 | Integration |
| Playwright for .NET integrated | TC-11, TC-12, TC-13, TC-14, TC-15 | Integration |
| Unit test project created | TC-01 to TC-10 | Unit |
| CLI accepts all specified options | TC-01 to TC-06 | Unit |
| CLI validates inputs correctly | TC-07 to TC-10 | Unit |
| Default viewport (1920x1080) screenshots | TC-11 | Integration |
| Custom viewport dimensions | TC-12 | Integration |
| Full-page capture mode | TC-13 | Integration |
| PNG output format (default and explicit) | TC-11, TC-05 | Integration |
| JPEG output format with quality | TC-14 | Integration |
| WebP output format with quality | TC-15 | Integration |
| Image format detection from extension | TC-05 | Unit |
| Explicit --format overrides extension | TC-05 | Unit |
| Output filename derivation | TC-02 | Unit |
| Quality parameter affects output | TC-14, TC-15 | Integration |
| Tool works on local machines | TC-11 to TC-15 | Integration |
| Tool works in GitHub Actions | TC-19 | Non-Functional |
| Error messages are clear and actionable | TC-07 to TC-10, TC-17 | Unit/Integration |
| Process HTML from feature 027 | TC-18 | Integration |

## User Acceptance Scenarios

> **Purpose**: For user-facing features (especially rendering changes), define scenarios for manual Maintainer review via Test PRs in GitHub and Azure DevOps. These help catch rendering bugs and validate real-world usage before merge.

### Scenario 1: Generate Full-Page Screenshot of Comprehensive Demo

**User Goal**: Generate a high-quality PNG screenshot of the entire comprehensive demo report for documentation.

**Test PR Context**:
- **GitHub**: Verify the generated PNG is attached to the PR and looks identical to the HTML rendering.

**Expected Output**:
- A PNG file named `comprehensive-demo.github.png`.
- The image should contain the entire report, not just the first 1080 pixels.
- Text should be crisp and colors should match the HTML version.

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown (as an image)
- [ ] Information is accurate and complete
- [ ] Feature solves the stated user problem

---

### Scenario 2: Custom Viewport Screenshot for Mobile View

**User Goal**: Generate a 375x667 (iPhone SE) screenshot to verify mobile responsiveness of the report.

**Expected Output**:
- A PNG file with exactly 375x667 dimensions.
- The report should be rendered in its mobile layout.

**Success Criteria**:
- [ ] Image dimensions are exactly 375x667.
- [ ] Mobile layout is correctly triggered in the screenshot.

## Test Cases

### TC-01: CLI Parsing - Input HTML path

**Type:** Unit

**Description:**
Verify that the `--input` (or `-i`) parameter is correctly parsed and required.

**Test Steps:**
1. Run tool without `--input`.
2. Run tool with `--input path/to/file.html`.

**Expected Result:**
1. Tool exits with code 1 and shows error about missing input.
2. Tool proceeds to validate the file path.

---

### TC-02: CLI Parsing - Output path derivation

**Type:** Unit

**Description:**
Verify that the output path is correctly derived when not provided.

**Test Steps:**
1. Provide `--input report.html` without `--output`.
2. Provide `--input dir/report.html` without `--output`.

**Expected Result:**
1. Output path is `report.png`.
2. Output path is `dir/report.png`.

---

### TC-03: CLI Parsing - Viewport dimensions

**Type:** Unit

**Description:**
Verify that `--width` and `--height` are correctly parsed and have correct defaults.

**Test Steps:**
1. Run without width/height.
2. Run with `--width 1280 --height 720`.

**Expected Result:**
1. Defaults to 1920x1080.
2. Uses 1280x720.

---

### TC-04: CLI Parsing - Full-page flag

**Type:** Unit

**Description:**
Verify that `--full-page` (or `-f`) flag is correctly parsed.

**Test Steps:**
1. Run without flag.
2. Run with flag.

**Expected Result:**
1. Full-page mode is false.
2. Full-page mode is true.

---

### TC-05: CLI Parsing - Format detection

**Type:** Unit

**Description:**
Verify format detection from extension and override via `--format`.

**Test Steps:**
1. `--output file.jpg`
2. `--output file.webp`
3. `--output file.png --format jpeg`

**Expected Result:**
1. Format is JPEG.
2. Format is WebP.
3. Format is JPEG (override).

---

### TC-06: CLI Parsing - Quality settings

**Type:** Unit

**Description:**
Verify quality settings and defaults.

**Test Steps:**
1. Run without quality for JPEG.
2. Run with `--quality 50` for JPEG.

**Expected Result:**
1. Default quality 90.
2. Quality 50.

---

### TC-07: CLI Validation - Missing input file

**Type:** Unit

**Description:**
Verify error when input file does not exist.

**Test Steps:**
1. Provide non-existent path to `--input`.

**Expected Result:**
Tool exits with code 1 and "Error: Input file not found: ..." message.

---

### TC-08: CLI Validation - Invalid viewport dimensions

**Type:** Unit

**Description:**
Verify error for non-positive viewport dimensions.

**Test Steps:**
1. `--width 0`
2. `--height -10`

**Expected Result:**
Tool exits with code 1 and descriptive error message.

---

### TC-09: CLI Validation - Invalid quality values

**Type:** Unit

**Description:**
Verify error for quality outside 0-100.

**Test Steps:**
1. `--quality 101`
2. `--quality -1`

**Expected Result:**
Tool exits with code 1 and descriptive error message.

---

### TC-10: CLI Validation - Invalid format

**Type:** Unit

**Description:**
Verify error for unsupported format.

**Test Steps:**
1. `--format gif`

**Expected Result:**
Tool exits with code 1 and lists supported formats (png, jpeg, webp).

---

### TC-11: Screenshot Generation - Default viewport (PNG)

**Type:** Integration (Skippable if no browser)

**Description:**
Verify that a basic HTML file is rendered to a 1920x1080 PNG.

**Preconditions:**
- Chromium installed.

**Test Steps:**
1. Create a simple `test.html`.
2. Run tool: `--input test.html --output test.png`.

**Expected Result:**
- `test.png` exists.
- `test.png` is a valid PNG file.
- `test.png` dimensions are 1920x1080.

---

### TC-12: Screenshot Generation - Custom viewport

**Type:** Integration (Skippable if no browser)

**Description:**
Verify custom viewport dimensions in output.

**Test Steps:**
1. Run tool: `--input test.html --output test.png --width 800 --height 600`.

**Expected Result:**
- `test.png` dimensions are 800x600.

---

### TC-13: Screenshot Generation - Full-page capture

**Type:** Integration (Skippable if no browser)

**Description:**
Verify full-page capture for tall content.

**Test Steps:**
1. Create `tall.html` with content exceeding 1080px height.
2. Run tool: `--input tall.html --output tall.png --full-page`.

**Expected Result:**
- `tall.png` height is greater than 1080px (matches content height).

---

### TC-14: Screenshot Generation - JPEG format with quality

**Type:** Integration (Skippable if no browser)

**Description:**
Verify JPEG generation and quality effect.

**Test Steps:**
1. Run tool: `--input test.html --output test.jpg --quality 10`.
2. Run tool: `--input test.html --output test-high.jpg --quality 100`.

**Expected Result:**
- Both files exist and are valid JPEGs.
- `test.jpg` file size is significantly smaller than `test-high.jpg`.

---

### TC-15: Screenshot Generation - WebP format with quality

**Type:** Integration (Skippable if no browser)

**Description:**
Verify WebP generation.

**Test Steps:**
1. Run tool: `--input test.html --output test.webp`.

**Expected Result:**
- `test.webp` exists and is a valid WebP file.

---

### TC-16: Screenshot Generation - Output directory creation

**Type:** Integration

**Description:**
Verify that missing output directories are created.

**Test Steps:**
1. Run tool: `--input test.html --output non-existent-dir/test.png`.

**Expected Result:**
- `non-existent-dir/` is created.
- `test.png` is saved inside it.

---

### TC-17: Screenshot Generation - Browser not installed error

**Type:** Integration

**Description:**
Verify actionable error message when Chromium is missing.

**Preconditions:**
- Environment without Chromium (or mocked failure).

**Expected Result:**
- Tool exits with code 1.
- Message includes `playwright install chromium`.

---

### TC-18: Integration - Process HTML from feature 027

**Type:** Integration

**Description:**
Verify that the tool can process a real report generated by the HTML renderer.

**Test Steps:**
1. Use `artifacts/comprehensive-demo.github.html` (if exists, or generate it).
2. Run screenshot tool on it.

**Expected Result:**
- Screenshot is generated successfully.

---

### TC-19: Non-Functional - GitHub Actions Compatibility

**Type:** Non-Functional

**Description:**
Verify the tool runs in a GitHub Actions environment.

**Test Steps:**
1. Run the tool in a CI workflow with `playwright install chromium --with-deps`.

**Expected Result:**
- Tool completes successfully without dependency errors.

## Test Data Requirements

List any new test data files needed:
- [test.html](test.html) - Minimal HTML for basic tests.
- [tall.html](tall.html) - HTML with large height to test full-page capture.
- [artifacts/comprehensive-demo.github.html](artifacts/comprehensive-demo.github.html) - Real-world test case from feature 027.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty input file | Exit code 1, error message | TC-07 |
| Input file is not HTML (e.g. binary) | Exit code 1, browser load error | TC-07 |
| Extremely large viewport | Handle gracefully or error if browser limit reached | TC-08 |
| Output file locked | Exit code 1, IO error message | TC-16 |

## Non-Functional Tests

- **Performance**: Screenshot generation should typically take < 5 seconds for standard reports.
- **Compatibility**: Must work on Windows, Linux, and macOS.

## Open Questions

- Should we validate the HTML content before passing it to Playwright? (Currently assuming Playwright handles it).
- Are there specific fonts required for consistent screenshots across platforms? (Usually handled by the HTML renderer's CSS).
