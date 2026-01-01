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
- [ ] Playwright CLI installed and Chromium browser binaries downloaded (`dotnet tool install --global Microsoft.Playwright.CLI` and `playwright install chromium`).
- [ ] `tools/Oocx.TfPlan2Md.ScreenshotGenerator/Oocx.TfPlan2Md.ScreenshotGenerator.csproj` created (Target .NET 10).
- [ ] `tests/Oocx.TfPlan2Md.ScreenshotGenerator.Tests/Oocx.TfPlan2Md.ScreenshotGenerator.Tests.csproj` created.
- [ ] Both projects added to `tfplan2md.slnx`.
- [ ] `Microsoft.Playwright` NuGet package added to the tool project.
- [ ] Basic `Program.cs` created in the tool project.

**Dependencies:** None

---

### Task 2: CLI Options and Parsing

**Priority:** High

**Description:**
Implement the CLI infrastructure including options, parsing, and validation, following the pattern used in the HTML Renderer tool.

**Acceptance Criteria:**
- [ ] `CliOptions` record created with all specified fields.
- [ ] `CliParser` implemented to handle:
    - `--input` / `-i` (required)
    - `--output` / `-o` (optional)
    - `--width` / `-w` (default 1920)
    - `--height` / `-h` (default 1080)
    - `--full-page` / `-f` (default false)
    - `--format` (optional: png, jpeg, webp)
    - `--quality` / `-q` (default 90 for JPEG, 85 for WebP)
- [ ] `HelpTextProvider` implemented with usage examples.
- [ ] Validation logic implemented for:
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
- [ ] `HtmlScreenshotCapturer` implemented in `Capturing/` namespace.
- [ ] `CaptureAsync` method supports:
    - Navigating to `file://` URL of the input HTML.
    - Setting viewport size.
    - Taking screenshots in PNG, JPEG, and WebP formats.
    - Applying quality settings for lossy formats.
    - Full-page capture mode.
- [ ] Browser launch logic handles missing Chromium by providing clear instructions (`playwright install chromium`).

**Dependencies:** Task 1

---

### Task 4: Application Orchestration

**Priority:** Medium

**Description:**
Implement the main application logic that ties CLI parsing, validation, and capturing together.

**Acceptance Criteria:**
- [ ] `ScreenshotGeneratorApp` implemented.
- [ ] Output path derivation logic implemented (e.g., `report.html` -> `report.png` if output not specified).
- [ ] Format detection from output extension implemented.
- [ ] Output directory creation if it doesn't exist.
- [ ] `Program.cs` updated to call `ScreenshotGeneratorApp.RunAsync`.
- [ ] Proper exit codes (0 for success, 1 for error) and error messages to stderr.

**Dependencies:** Task 2, Task 3

---

### Task 5: Unit Testing

**Priority:** Medium

**Description:**
Write unit tests for the non-browser parts of the tool.

**Acceptance Criteria:**
- [ ] Tests for `CliParser` covering all options and combinations.
- [ ] Tests for validation logic (invalid dimensions, quality, formats).
- [ ] Tests for output path derivation and format detection rules.
- [ ] Tests for `HelpTextProvider`.

**Dependencies:** Task 2, Task 4

---

### Task 6: Integration Testing

**Priority:** Medium

**Description:**
Write integration tests that use a real browser to generate screenshots.

**Acceptance Criteria:**
- [ ] Integration tests created using `Xunit.SkippableFact` (skip if Chromium not installed).
- [ ] Test case for default viewport PNG generation.
- [ ] Test case for custom viewport dimensions.
- [ ] Test case for full-page capture.
- [ ] Test case for JPEG and WebP formats with quality settings.
- [ ] Test case using a real report from the HTML renderer (if available in test data).

**Dependencies:** Task 3, Task 4

---

### Task 7: Documentation and UAT

**Priority:** Low

**Description:**
Update documentation and perform final validation.

**Acceptance Criteria:**
- [ ] `README.md` updated with Screenshot Generator section and examples.
- [ ] UAT Scenario 1 (Full-page demo) completed and verified.
- [ ] UAT Scenario 2 (Mobile viewport) completed and verified.
- [ ] Verified that the tool works in a simulated CI environment (or documentation updated with CI setup instructions).

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
