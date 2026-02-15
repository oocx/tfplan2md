# Tasks: HTML Screenshot Generation Tool

## Overview

Implementation of a standalone .NET tool that generates screenshots from HTML files using Playwright and Chromium. This tool enables visual regression testing and documentation screenshot generation.

Reference: [specification.md](specification.md), [architecture.md](architecture.md)

## Tasks

### Task 1: Environment Preparation and Project Setup

**Priority:** High

**Description:**
Prepare the development environment by installing Playwright dependencies and create the project structure for the screenshot generator tool and its test project.

**Acceptance Criteria:**
- [x] Playwright CLI installed and Chromium browser binaries downloaded (`dotnet tool install --global Microsoft.Playwright.CLI` and `playwright install chromium`).
- [x] `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/Oocx.TfPlan2Md.ScreenshotGenerator.csproj` created (Target .NET 10).
- [x] `src/tests/Oocx.TfPlan2Md.ScreenshotGenerator.Tests/Oocx.TfPlan2Md.ScreenshotGenerator.Tests.csproj` created.
- [x] Both projects added to `tfplan2md.slnx`.
- [x] `Microsoft.Playwright` NuGet package added to the tool project.
- [x] Basic `Program.cs` created in the tool project.

**Dependencies:** None

---

### Task 2: CLI Options and Parsing

**Priority:** High

**Description:**
Implement the CLI infrastructure including options, parsing, and validation, following the pattern used in the HTML Renderer tool.

**Acceptance Criteria:**
- [x] `CliOptions` record created with all specified fields.
- [x] `CliParser` implemented to handle:
    - `--input` / `-i` (required)
    - `--output` / `-o` (optional)
    - `--width` / `-w` (default 1920)
    - `--height` / `-h` (default 1080)
    - `--full-page` / `-f` (default false)
    - `--format` (optional: png, jpeg)
    - `--quality` / `-q` (default 90 for JPEG)
- [x] `HelpTextProvider` implemented with usage examples.
- [x] Validation logic implemented for:
    - Input file existence.
    - Positive width/height.
    - Quality between 0-100.
    - Supported formats.

**Dependencies:** Task 1

---

### Task 3: Screenshot Capturing Engine

**Priority:** High

**Description:**
Implement the core screenshot generation logic using Playwright.

**Acceptance Criteria:**
- [x] `HtmlScreenshotCapturer` implemented in `Capturing/` namespace.
- [x] `CaptureAsync` method supports:
    - Navigating to `file://` URL of the input HTML.
    - Setting viewport size.
    - Taking screenshots in PNG and JPEG formats (WebP deferred).
    - Applying quality settings for JPEG.
    - Full-page capture mode.
- [x] Browser launch logic handles missing Chromium by providing clear instructions (`playwright install chromium`).

**Dependencies:** Task 1

---

### Task 4: Application Orchestration

**Priority:** Medium

**Description:**
Implement the main application logic that ties CLI parsing, validation, and capturing together.

**Acceptance Criteria:**
- [x] `ScreenshotGeneratorApp` implemented.
- [x] Output path derivation logic implemented (e.g., `report.html` -> `report.png` if output not specified).
- [x] Format detection from output extension implemented.
- [x] Output directory creation if it doesn't exist.
- [x] `Program.cs` updated to call `ScreenshotGeneratorApp.RunAsync`.
- [x] Proper exit codes (0 for success, 1 for error) and error messages to stderr.

**Dependencies:** Task 2, Task 3

---

### Task 5: Unit Testing

**Priority:** Medium

**Description:**
Write unit tests for the non-browser parts of the tool.

**Acceptance Criteria:**
- [x] Tests for `CliParser` covering all options and combinations.
- [x] Tests for validation logic (invalid dimensions, quality, formats).
- [x] Tests for output path derivation and format detection rules.
- [x] Tests for `HelpTextProvider`.

**Dependencies:** Task 2, Task 4

---

### Task 6: Integration Testing

**Priority:** Medium

**Description:**
Write integration tests that use a real browser to generate screenshots.

**Acceptance Criteria:**
- [x] Integration tests created using `Xunit.SkippableFact` (skip if Chromium not installed).
- [x] Test case for default viewport PNG generation.
- [x] Test case for custom viewport dimensions.
- [x] Test case for full-page capture.
- [x] Test case for JPEG format with quality settings.
- [x] Test case using a real report from the HTML renderer (if available in test data).

**Dependencies:** Task 3, Task 4

---

### Task 7: Documentation and UAT

**Priority:** Low

**Description:**
Update documentation and perform final validation.

**Acceptance Criteria:**
- [x] `README.md` updated with Screenshot Generator section and examples.
- [x] UAT Scenario 1 (Full-page demo) completed and verified.
- [x] UAT Scenario 2 (Mobile viewport) completed and verified.
- [x] Verified that the tool works in a simulated CI environment (or documentation updated with CI setup instructions).

**Status Notes:**
- README updated with usage and Playwright install guidance.
- UAT Scenario 1 (AzDO): `artifacts/comprehensive-demo.azdo.html` rendered with azdo wrapper, captured full-page to `artifacts/comprehensive-demo.azdo.png`.
- UAT Scenario 2 (GitHub/simple-diff): `artifacts/comprehensive-demo-simple-diff.github.html` rendered with github wrapper template, captured full-page to `artifacts/comprehensive-demo-simple-diff.github.png`; mobile viewport sample remains at `artifacts/comprehensive-demo.mobile.png` (375x667).
- CI guidance covered via Playwright install notes (chromium `--with-deps`).

**Dependencies:** Task 4, Task 6

## Implementation Order

1. **Task 1 (Environment & Setup)**: Foundation for all other tasks, including browser binaries.
2. **Task 2 (CLI)**: Define the interface.
3. **Task 3 (Engine)**: Core functionality.
4. **Task 4 (Orchestration)**: Connect everything.
5. **Task 5 & 6 (Testing)**: Ensure quality and correctness.
6. **Task 7 (Docs/UAT)**: Finalize and document.

## Open Questions

None.
