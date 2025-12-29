# Test Plan: Cumulative Release Notes

## Overview

This test plan covers the validation of the cumulative release notes generation logic. The core logic involves extracting specific sections from `CHANGELOG.md` based on the current version and the last released version.

## Test Strategy

Since the logic involves parsing a Markdown file using bash/awk, we will:
1.  Extract the logic into a standalone script (`scripts/extract-changelog.sh`).
2.  Create a C# test class `ChangelogExtractionTests` in the test project.
3.  Use `dotnet test` to execute these tests, which will invoke the script via `System.Diagnostics.Process` with various inputs and assert on the output.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Extract only current version if no previous release exists | TC-01 | Unit |
| Extract all sections between current and last release | TC-02, TC-03 | Unit |
| Preserve Markdown formatting | TC-04 | Unit |
| Handle 'v' prefix in versions | TC-05 | Unit |
| Idempotent execution | TC-06 | Unit |

## Test Cases

### TC-01: No Previous Release

**Type:** Unit

**Description:**
Verify that if no last version is provided (simulating first release), only the current version's changelog is extracted.

**Preconditions:**
- `tests/Oocx.TfPlan2Md.Tests/TestData/changelog-full.md` exists.

**Test Steps:**
1.  Run extraction script with:
    - File: `changelog-full.md`
    - Current Version: `0.12.0`
    - Last Version: (empty)

**Expected Result:**
- Output contains only the `0.12.0` section.
- Output does not contain `0.11.0` or older.

---

### TC-02: Consecutive Versions

**Type:** Unit

**Description:**
Verify that if the last release was the immediate predecessor, only the current version is extracted.

**Preconditions:**
- `tests/Oocx.TfPlan2Md.Tests/TestData/changelog-full.md` exists.

**Test Steps:**
1.  Run extraction script with:
    - File: `changelog-full.md`
    - Current Version: `0.12.0`
    - Last Version: `0.11.0`

**Expected Result:**
- Output contains only the `0.12.0` section.

---

### TC-03: Version Gap (Cumulative)

**Type:** Unit

**Description:**
Verify that if there is a gap between the last release and current version, all intermediate versions are extracted.

**Preconditions:**
- `tests/Oocx.TfPlan2Md.Tests/TestData/changelog-full.md` exists.

**Test Steps:**
1.  Run extraction script with:
    - File: `changelog-full.md`
    - Current Version: `0.12.0`
    - Last Version: `0.9.0`

**Expected Result:**
- Output contains sections for `0.12.0`, `0.11.0`, and `0.10.0`.
- Output does not contain `0.9.0`.

---

### TC-04: Formatting Preservation

**Type:** Unit

**Description:**
Verify that complex Markdown (lists, code blocks, links) is preserved exactly as is.

**Preconditions:**
- `tests/Oocx.TfPlan2Md.Tests/TestData/changelog-complex.md` exists.

**Test Steps:**
1.  Run extraction script.
2.  Compare output with expected content string.

**Expected Result:**
- Output matches expected string exactly (including whitespace).

---

### TC-05: Version Prefix Handling

**Type:** Unit

**Description:**
Verify that the script handles version inputs with and without 'v' prefix.

**Preconditions:**
- `tests/Oocx.TfPlan2Md.Tests/TestData/changelog-full.md` exists.

**Test Steps:**
1.  Run with `v0.12.0` and `v0.11.0`.
2.  Run with `0.12.0` and `0.11.0`.

**Expected Result:**
- Both executions produce identical output.

---

### TC-06: Idempotency

**Type:** Unit

**Description:**
Verify that running the extraction multiple times produces the same result.

**Test Steps:**
1.  Run extraction script.
2.  Run it again.

**Expected Result:**
- Outputs are identical.

## Test Data Requirements

- `tests/Oocx.TfPlan2Md.Tests/TestData/changelog-full.md`: A comprehensive changelog with multiple versions (0.12.0 down to 0.1.0).
- `tests/Oocx.TfPlan2Md.Tests/TestData/changelog-complex.md`: A changelog entry with nested lists, code blocks, and links.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Last version not found in file | Extract until end of file | TC-07 |
| Current version not found | Empty output | TC-08 |

## Non-Functional Tests

- **Performance**: Script should run in < 1s for typical changelog sizes.

## Open Questions

None.
