# UAT Test Plan: Code Quality Metrics Enforcement

## Goal
Verify that code quality metrics are correctly enforced in CI and that the refactoring of large files meets the project's maintainability standards.

## Artifacts
**Artifact to use:** Build logs and PR status for a branch with intentional violations.

**Creation Instructions:**
- **Branch:** `uat/code-quality-violations`
- **Changes:** 
  - Add a method with complexity > 15 to a new class.
  - Add a line with > 160 characters.
  - Add a class with low maintainability index (< 20).
- **Command:** `dotnet build` and push to create a PR.

## Test Steps
1. Create a PR with the intentional violations.
2. Verify that the CI build fails on both GitHub and Azure DevOps (if applicable).
3. Inspect the error messages in the build logs.
4. Verify that the refactored files (`ScribanHelpers.AzApi.cs`, `ReportModel.cs`, etc.) are indeed split and below the line count thresholds.

## Validation Instructions (Test Description)
**Specific Resources/Sections:**
- Build Log error: Verify `CA1502` (Complexity) and `IDE0055` (Line Length) are reported as **Errors**, not just Warnings.
- File `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.AzApi.cs`: Check if it has been converted to `partial` and split into logical parts (e.g., `ScribanHelpers.AzApi.Resources.cs`, `ScribanHelpers.AzApi.Data.cs`).

**Expected Outcome:**
- The build should fail explicitly due to the new violations.
- Existing files that exceed the guidelines but are in the baseline should NOT fail the build (they should only show as warnings or be suppressed).
- The targeted large files should now have significantly lower line counts, making them easier to navigate.

**Before/After Context:**
- **Before:** Files like `ScribanHelpers.AzApi.cs` were over 1,000 lines long and hard to maintain. Quality rules were not enforced by the build.
- **After:** Quality standards are automatically checked. Large files are broken down into smaller, focused components.
