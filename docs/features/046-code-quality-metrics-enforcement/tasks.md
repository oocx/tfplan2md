# Tasks: Code Quality Metrics Enforcement

## Overview

Implement automated enforcement of code quality metrics (cyclomatic complexity, maintainability index, line length, and file length) to ensure a maintainable and readable codebase. This includes refactoring several large files that exceed project guidelines.

Reference: [Specification](specification.md) | [Architecture](architecture.md)

## Tasks

### Task 1: Initialize Metrics Configuration

**Priority:** High

**Description:**
Create the foundation for metrics enforcement by defining thresholds and integrating them into the build system.

**Acceptance Criteria:**
- [ ] Create `CodeMetricsConfig.txt` in the repository root with the following thresholds:
  - CA1502 (Complexity): 15
  - CA1505 (Method Maintainability): 20
  - CA1506 (Class Maintainability): 20
- [ ] Update `src/Directory.Build.props` to include `CodeMetricsConfig.txt` as `AdditionalFiles`.
- [ ] Update `.editorconfig` to set severities:
  - `dotnet_diagnostic.CA1502.severity = error`
  - `dotnet_diagnostic.CA1505.severity = error`
  - `dotnet_diagnostic.CA1506.severity = error`
  - `dotnet_diagnostic.IDE0055.severity = error` (with `max_line_length = 160`)
- [ ] Add test file exemptions to `.editorconfig` (disable CA1502, CA1505, CA1506 for `src/tests/**/*.cs`).

**Dependencies:** None

---

### Task 2: Establish Quality Baseline

**Priority:** High

**Description:**
Generate/update baseline files to suppress existing violations, allowing the build to pass while enforcing rules for new code.

**Acceptance Criteria:**
- [ ] Run a clean build and capture all new quality violations.
- [ ] Update `build-sonaranalyzer-baseline.txt` (or create a new metrics baseline if necessary) to suppress current violations.
- [ ] Verify that `dotnet build` succeeds on the current codebase with the baseline in place.
- [ ] Verify that a *new* violation (e.g., a method with complexity 20 in a new file) causes a build failure.

**Dependencies:** Task 1

---

### Task 3: Document Suppression Policy

**Priority:** Medium

**Description:**
Update project documentation to guide developers on how to handle legitimate exceptions to the quality metrics.

**Acceptance Criteria:**
- [ ] Update `docs/commenting-guidelines.md` with a section on Quality Metric Suppressions.
- [ ] Define requirements for suppression: `SuppressMessage` attribute, justification comment, and maintainer approval.
- [ ] Document acceptable exceptions for line length (e.g., long URLs, error messages).

**Dependencies:** Task 1

---

### Task 4: Refactor ScribanHelpers.AzApi.cs

**Priority:** High

**Description:**
Break down the large `ScribanHelpers.AzApi.cs` (1,067 lines) into smaller, focused partial files.

**Acceptance Criteria:**
- [ ] Convert `ScribanHelpers` class in `ScribanHelpers.AzApi.cs` to `partial`.
- [ ] Move resource-specific logic into separate files:
  - `ScribanHelpers.AzApi.Resources.cs`
  - `ScribanHelpers.AzApi.Data.cs`
  - (Other splits as logical)
- [ ] Ensure each resulting file is under 300 lines.
- [ ] Remove corresponding baseline entries for this file.
- [ ] Verify all tests pass.

**Dependencies:** Task 2

---

### Task 5: Refactor ReportModel.cs

**Priority:** Medium

**Description:**
Split `ReportModel.cs` (774 lines) into separate class/record files.

**Acceptance Criteria:**
- [ ] Extract logical entities/records into their own files.
- [ ] Target file size: under 300 lines per file.
- [ ] Remove corresponding baseline entries for this file.
- [ ] Verify all tests pass.

**Dependencies:** Task 2

---

### Task 6: Refactor VariableGroupViewModelFactory.cs

**Priority:** Medium

**Description:**
Refactor `VariableGroupViewModelFactory.cs` (587 lines) to reduce complexity and length.

**Acceptance Criteria:**
- [ ] Extract helper classes or private methods into focused service/mapper classes.
- [ ] Target file size: under 400 lines (ideally 300).
- [ ] Remove corresponding baseline entries for this file.
- [ ] Verify all tests pass.

**Dependencies:** Task 2

---

### Task 7: Refactor ResourceSummaryBuilder.cs

**Priority:** Medium

**Description:**
Refactor `ResourceSummaryBuilder.cs` (471 lines) by extracting per-resource summary logic.

**Acceptance Criteria:**
- [ ] Extract specific resource summary building logic into smaller components.
- [ ] Target file size: under 300 lines.
- [ ] Remove corresponding baseline entries for this file.
- [ ] Verify all tests pass.

**Dependencies:** Task 2

---

### Task 8: Refactor AzureRoleDefinitionMapper.Roles.cs

**Priority:** Low

**Description:**
Refactor `AzureRoleDefinitionMapper.Roles.cs` (488 lines) and evaluate if data-driven approach is better.

**Acceptance Criteria:**
- [ ] Reduce file size below 400 lines.
- [ ] Remove corresponding baseline entries for this file.
- [ ] Verify all tests pass.

**Dependencies:** Task 2

---

### Task 9: Final Quality Audit

**Priority:** Medium

**Description:**
Perform a final audit of the metrics enforcement and baseline files.

**Acceptance Criteria:**
- [ ] Verify no "stale" baseline entries remain for refactored files.
- [ ] Run full test suite (`scripts/test-with-timeout.sh`).
- [ ] Verify CI (GitHub Actions) correctly blocks a branch with an intentional violation.

**Dependencies:** Task 4, Task 5, Task 6, Task 7, Task 8

## Implementation Order

1. **Task 1 & 2** (Foundational Setup): Must be done first to protect the codebase immediately.
2. **Task 3** (Documentation): Can be done in parallel or immediately after foundation.
3. **Task 4** (ScribanHelpers): Highest priority refactoring due to file size (1,000+ lines).
4. **Tasks 5, 6, 7** (Medium Priority Refactoring): Order can vary based on developer preference.
5. **Task 8** (Low Priority Refactoring): Smallest of the targeted files.
6. **Task 9** (Verification): Final step.

## Open Questions

- Should we split the refactoring tasks into separate PRs? (Recommended by Architecture: Yes).
- Are there any specific naming conventions for the split partial files? (e.g., `Class.Purpose.cs`).
