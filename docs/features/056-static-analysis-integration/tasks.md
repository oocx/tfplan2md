# Tasks: Static Code Analysis Integration

## Overview

Integrate static code analysis findings from security and quality tools (Checkov, Trivy, TFLint, Semgrep) in SARIF 2.1.0 format directly into the Terraform plan markdown report.

Reference: [Specification](specification.md), [Architecture](architecture.md)

## Tasks

### Task 1: Core Domain Models and SARIF Subset Parser

**Priority:** High

**Description:**
Create the internal data structures for code analysis findings and implement a lightweight, defensive SARIF 2.1.0 parser using `System.Text.Json`.

**Acceptance Criteria:**
- [x] Internal `CodeAnalysisModel` and `Finding` models created (immutable, following project patterns).
- [x] `SarifParser` correctly extracts tool name/version, results, locations, severities, and remediation links from valid SARIF 2.1.0.
- [x] Parser is defensive and handles missing optional SARIF fields gracefully.
- [x] Basic unit tests (TC-01) passing for valid SARIF files.

**Dependencies:** None

---


### Task 2: CLI Flags and Wildcard Expansion

**Priority:** High

**Description:**
Implement the new CLI arguments and the logic to find and expand SARIF files from patterns.

**Acceptance Criteria:**
- [x] `--code-analysis-results <pattern>`, `--code-analysis-minimum-level <level>`, and `--fail-on-static-code-analysis-errors <level>` flags added to CLI.
- [x] Custom wildcard expansion logic (TC-02, TC-03) supports `*.sarif` and `**/*.sarif`.
- [x] Multiple flags are correctly aggregated.
- [x] File paths are sorted ordinally for deterministic output.

**Dependencies:** None

---

### Task 3: Severity Derivation and Resource Mapping

**Priority:** High

**Description:**
Implement the logic to map SARIF results to Terraform addresses and calculate the unified severity level.

**Acceptance Criteria:**
- [x] `SeverityMapper` implements the hybrid mapping: `security-severity` (0-10) -> `rank` -> `level` (TC-09).
- [x] `ResourceMapper` extracts resource addresses from `logicalLocations[].fullyQualifiedName` for standard and module-nested resources (TC-04).
- [x] Attribute-level mapping is implemented as best-effort (TC-10).
- [x] Findings with multiple locations result in multiple mapped findings (TC-20).

**Dependencies:** Task 1

---

### Task 4: Report Model Integration and Findings-only Resources

**Priority:** Medium

**Description:**
Extend the existing `ReportModelBuilder` to integrate code analysis data and ensure resources with findings appear in the report even if they have no plan changes.

**Acceptance Criteria:**
- [x] `ReportModel` includes the aggregated findings and summary metrics.
- [x] Resources only appearing in SARIF findings are injected into the model for rendering (AC-15, TC-15).
- [x] Common `effectiveMinimumLevel` logic is applied to filter displayed findings.

**Dependencies:** Task 1, Task 3

---

### Task 5: Scriban Template Updates for Summary and Findings (Variant C)

**Priority:** High

**Description:**
Update the Scriban templates to render the "Code Analysis Summary" and the per-resource "Security & Quality Findings" tables following Variant C.

**Acceptance Criteria:**
- [x] `Summary` section in markdown includes tool list and severity counts (TC-13, TC-14).
- [x] Resource entries show the count/indicator metadata line and the findings table (TC-08).
- [x] Findings are ordered from most severe to least severe (AC-9).
- [x] Remediation links are correctly formatted as clickable markdown links (AC-11).
- [x] Critical findings use 🚨 icon and are visually emphasized (AC-12).

**Dependencies:** Task 4

---

### Task 6: Unmatched Findings and Error Reporting

**Priority:** Medium

**Description:**
Implement the sections for findings that could not be mapped to specific resources and the warning display for invalid SARIF files.

**Acceptance Criteria:**
- [x] "Other Findings" section appears at the end of the report (TC-16).
- [x] Module-level findings are grouped under their respective module headers (TC-17).
- [x] Malformed SARIF files trigger a non-fatal warning section in the report with error details (TC-18, AC-18).
- [x] Empty findings/warnings sections are suppressed.

**Dependencies:** Task 5

---

### Task 7: Provider-specific Template Support

**Priority:** Medium

**Description:**
Ensure that custom provider templates (e.g., Azure AD, API Management) correctly render code analysis findings by using shared partials.

**Acceptance Criteria:**
- [x] Core resource rendering for findings is extracted into a shared partial (e.g., `_code_analysis_findings.sbn`).
- [x] Provider-specific templates include this shared partial.
- [x] Findings render correctly for specialized resource views (API Management policy changes, etc.).

**Dependencies:** Task 5

---

### Task 8: Failure Logic and Exit Codes

**Priority:** High

**Description:**
Implement the logic to exit with code 10 when findings at or above the threshold are found, ensuring the report is still completely written.

**Acceptance Criteria:**
- [ ] Application returns exit code 10 if `--fail-on-static-code-analysis-errors` threshold is met (TC-19).
- [ ] Exit code logic runs AFTER the report has been successfully written to the output file/stdout.
- [ ] Standard error message reflects the count of findings causing the failure.

**Dependencies:** Task 4

---

### Task 9: End-to-End Integration and Snapshots

**Priority:** Low

**Description:**
Final validation using real tool outputs and updating test snapshots.

**Acceptance Criteria:**
- [ ] Integration tests pass with real Checkov, Trivy, and TFLint SARIF outputs (TC-21, TC-24, TC-27).
- [ ] Multi-tool aggregation is verified (TC-30).
- [ ] All test data requirements from `test-plan.md` are met.
- [ ] Snapshots for standard and provider-specific outputs are updated (`SNAPSHOT_UPDATE_OK`).

**Dependencies:** Task 6, Task 7, Task 8

## Implementation Order

1. **Foundational (Tasks 1-3):** Define the models, parser, and mapping logic. This allows unit testing to proceed immediately.
2. **Infrastructure (Task 4, 8):** Model building and exit code logic.
3. **Rendering (Tasks 5-7):** Visual display in markdown, ensuring both core and provider-specific templates work.
4. **Validation (Task 9):** Final integration tests and snapshot generation.

## Open Questions

None. All architectural decisions (parsing strategy, presentation variant, severity mapping) have been resolved in the specification and architecture documents.
