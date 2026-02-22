# Issue: Remaining Open Security Findings from 097

## Problem Description

Issue 098 fixed multiple sensitive-value disclosure paths (AzApi body rendering, template-context masking, Azure DevOps variable group secret transitions, and sensitivity hierarchy edge cases).

Issue 097 also identified *non-secret-masking* vulnerabilities and correctness issues that remain open on `main` and can still:
- allow static-analysis gates to pass even when SARIF inputs fail to parse,
- allow path traversal when `--template-dir` is used,
- allow Markdown/HTML injection (primarily “reviewer deception” risks) via unescaped fields,
- misrepresent Terraform actions (e.g., Terraform 1.7+ `forget`) as no-op.

This work item documents the remaining open items and a safe, testable fix approach.

Reference: `docs/issues/097-security-analysis/results.md`.

## What’s Still Open (as of 2026-02-22)

### 1) Static-analysis fail gate bypass via malformed SARIF inputs (Issue 5)
- **Primary locations:**
  - `src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisLoader.cs`
  - `src/Oocx.TfPlan2Md/ProgramEntry.cs` (`HandleCodeAnalysisFailureAsync`)
- **Status on main:** still present.

**What’s broken**
- SARIF parse errors are caught and recorded as warnings, but the fail gate only considers “finding count ≥ threshold” and ignores warnings.
- Result: a corrupted/truncated SARIF file can reduce findings to 0 and the tool exits success even when the code-analysis inputs were incomplete.

**Suggested fix approach (high-level)**
- When `--fail-on-static-code-analysis-errors <level>` is set, treat *any* SARIF load warning as a failure (unless a new opt-out flag is introduced).
- Emit a clear stderr message enumerating files that failed to parse.

**Verification ideas**
- Add a unit test that feeds an invalid SARIF file pattern and asserts:
  - warnings are produced, and
  - exit code indicates failure when fail-on threshold is enabled.

---

### 2) Path traversal via resource type in custom template directory (Issue 7)
- **Primary location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/TemplateLoader.cs`
- **Status on main:** still present.

**What’s broken**
- `NormalizePath` removes a leading `/` and strips `.sbn`, but does not reject `..` segments.
- When a custom template directory is configured, `LoadInternal` uses `Path.Combine(_customTemplateDirectory, normalized...)`, allowing `../..` traversal outside the template directory.

**Suggested fix approach (high-level)**
- For custom directory loads, canonicalize both:
  - the configured template directory (`Path.GetFullPath`), and
  - the candidate file path (`Path.GetFullPath(Path.Combine(...))`)
- Reject any path that is not a descendant of the custom template directory.
- Prefer failing fast with a clear error (and include the requested template path).

**Verification ideas**
- Unit test that sets a custom template directory and requests a template path containing `../`.
- Assert that load fails and does not read files outside the custom directory.

---

### 3) Markdown link injection / broken links via SARIF `help_uri` (Issue 9)
- **Primary location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_findings.sbn`
- **Status on main:** still present.

**What’s broken**
- The template renders `help_uri` using standard markdown link syntax: `[Details]({{ help_uri }})`.
- URLs containing `)` (valid and common) can break link parsing and allow content to “escape” the link, causing misleading table output.

**Suggested fix approach (high-level)**
- Render the URL using CommonMark-safe angle-bracket form:
  - `[Details](<...>)`
- Keep existing escaping for table cells, but avoid relying on it for URL delimiters.

**Verification ideas**
- Snapshot or template test where `help_uri` includes parentheses and verifies correct rendering.

---

### 4) HTML injection via unencoded `model.Type` in summary HTML (Issue 10)
- **Primary location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs`
- **Status on main:** still present.

**What’s broken**
- `BuildSummaryHtml` builds a string containing HTML tags and injects `model.Type` without HTML encoding.
- While GitHub/Azure DevOps sanitize script execution, unencoded HTML can still cause “reviewer deception” (formatting/visibility tricks) in rendered output.

**Suggested fix approach (high-level)**
- HTML-encode `model.Type` using the existing helper used for other summary fields (e.g., the helper already used by `FormatCodeSummary`).

**Verification ideas**
- Unit test that sets `model.Type` to a value containing `<`/`>` and asserts the output contains escaped entities.

---

### 5) Unknown or empty `actions` rendered as no-op (Issue 11)
- **Primary location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` (`DetermineAction`)
- **Status on main:** still present.

**What’s broken**
- `DetermineAction` falls back to `no-op` for unknown action lists.
- Terraform 1.7+ introduced `forget` (and related lifecycle/state operations) which will be misrepresented as no-op, undermining review accuracy.

**Suggested fix approach (high-level)**
- Add explicit handling for known newer actions (at minimum `forget`).
- For truly unknown actions, render a warning/unknown action category rather than `no-op`, and emit a diagnostic warning.

**Verification ideas**
- Unit tests for `DetermineAction` with inputs like `[]`, `["forget"]`, and unknown values.
- Snapshot tests to ensure icons/summaries reflect non-no-op behavior.

---

## Items Likely Already Resolved

### 6) `BuildDefinitionVariableValues.SecretValue` stored but unused (Issue 12)
- On current `main`, `BuildDefinitionExtractors.ExtractVariables` no longer reads `secret_value`, and the record `BuildDefinitionVariableValues` does not include a `SecretValue` field.
- This suggests Issue 12 has already been addressed (or is no longer applicable to the current implementation).

## Not Recommended to Change (Documented in 097)

### 7) No output path validation (Issue 13)
- This is standard CLI behavior and only becomes a vulnerability if tfplan2md is wrapped in a privileged service.
- No change recommended for the current CLI use case.

## Suggested Fix Plan (Implementation Order)

1. Fix SARIF fail-gate behavior (Issue 5) to avoid silent integrity failures.
2. Fix template-dir traversal hardening (Issue 7).
3. Fix output injection/render correctness:
   - SARIF `help_uri` rendering (Issue 9)
   - summary HTML encoding (Issue 10)
4. Fix Terraform action mapping correctness (Issue 11).

## Related Files

- `docs/issues/097-security-analysis/results.md`
- `docs/issues/098-sensitive-info-exposure/analysis.md`
