# Security Analysis Report

## Metadata
- Repository: `oocx/tfplan2md`
- Branch analyzed: `copilot/extend-mapping-to-azure-devops`
- Analyzer: GPT-5.3-Codex
- Date: 2026-02-21
- Scope: High-impact security and integrity issues with emphasis on:
  - Sensitive data disclosure
  - Untrusted input handling (plans, mappings, SARIF)
  - Rendering completeness/integrity (missing or incorrect critical information)
  - Report trustworthiness for PR approval workflows

## Executive Summary
This review identified **3 high-severity security/integrity findings**.

1. **Confirmed secret disclosure in AzApi rendering path** (plaintext sensitive values rendered).
2. **AzApi update diff path computes sensitivity but does not enforce masking** (potential disclosure path for updates).
3. **Fail-on static analysis gate can be bypassed with malformed SARIF input** (critical findings may be omitted from fail decision).

No native memory corruption vectors (e.g., buffer overflows) were identified in the reviewed .NET managed code paths. No direct plan-driven arbitrary file-write primitive was found in current CLI workflow.

---

## Methodology
The analysis focused on:
- Entry points and data flow from CLI input to rendering/output.
- Parsing and model-building behavior for Terraform plan JSON and auxiliary files.
- Provider-specific template/mapping code where generic safeguards may be bypassed.
- Code-analysis ingestion and fail-gate logic controlling CI/PR blocking behavior.
- Existing tests/snapshots as exploit evidence where applicable.

### Key review targets
- `src/Oocx.TfPlan2Md/ProgramEntry.cs`
- `src/Oocx.TfPlan2Md/Parsing/*`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/*`
- `src/Oocx.TfPlan2Md/Providers/AzApi/*`
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/*`
- `src/Oocx.TfPlan2Md/CodeAnalysis/*`
- Relevant test fixtures/snapshots in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/*`

---

## Findings

## 1) High: AzApi sensitive values are rendered in plaintext

### Severity
- **Estimated CVSS v4.0:** **8.6 (High)**
- **Primary impact:** Confidentiality High
- **Attack precondition:** None beyond normal tfplan2md processing of legitimate Terraform plan JSON that contains sensitive AzApi attributes.

### Description
The AzApi rendering path can output values marked sensitive in Terraform plan metadata (`before_sensitive`/`after_sensitive`) as plaintext in generated markdown.

This is a critical leak for workflows where generated markdown is posted to PRs, stored in CI artifacts/logs, or shared externally.

### Evidence
1. AzApi templates render body content directly for create/delete paths without passing sensitivity metadata:
   - [src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn](src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn#L62)
   - [src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn](src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn#L78)

2. Update path attempts to read `change.before_sensitive`/`change.after_sensitive` in templates:
   - [src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn](src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn#L69-L71)
   - [src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/update_resource.sbn](src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/update_resource.sbn#L43-L45)

3. But AOT script mapping exposes only `before_json` and `after_json` (not `before_sensitive`/`after_sensitive`):
   - [src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs](src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs#L195-L198)

4. Test fixture marks password sensitive:
   - [src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-sensitive-plan.json](src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-sensitive-plan.json#L32-L35)

5. Snapshot output still contains plaintext password:
   - [src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azapi-sensitive.md](src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azapi-sensitive.md#L38)

### Why this is high risk
This undermines a core security guarantee: sensitive Terraform values should remain masked unless explicitly requested. Exposure in PR comments/artifacts can lead to credential compromise.

### Can this occur during regular usage?
**Yes.** This can happen with normal `terraform show -json` output and default built-in tfplan2md templates.

- No malicious plan manipulation is required; a legitimate plan can contain sensitive AzApi fields in `after_sensitive`.
- No malicious template is required; the built-in AzApi templates currently drive the vulnerable rendering path.
- The existing test fixture/snapshot pair demonstrates this behavior under standard test execution.

### Recommended remediation
- Ensure AzApi templates receive sensitivity structures in script model (`before_sensitive`, `after_sensitive`).
- Enforce masking at rendering helper level (defense-in-depth), not only in templates.
- Add regression tests that assert sensitive AzApi fields are masked by default in create/update/delete/replace.
- Treat known-sensitive key patterns (e.g., password/secret/token/key) conservatively when sensitivity metadata is ambiguous.

---

## 2) High: AzApi update renderer computes sensitivity but does not apply masking

### Severity
- **Estimated CVSS v4.0:** **8.2 (High)**
- **Primary impact:** Confidentiality High

### Description
AzApi comparison logic computes `is_sensitive`, but update rendering formats raw values without checking this flag. This creates a second disclosure path even when sensitivity detection works.

### Evidence
1. Sensitivity is computed and attached to comparison records:
   - [src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Data.cs](src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Data.cs#L67-L90)

2. Update rendering prints before/after formatted raw values without masking based on `is_sensitive`:
   - [src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs](src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs#L217-L225)
   - [src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs](src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs#L293-L296)

### Why this is high risk
Even if template wiring is fixed, helper-level omission can still leak sensitive values during update diffs. A reliable secure design should mask at the deepest common rendering boundary.

### Can this occur during regular usage?
**Yes.** This is a logic bug in normal update rendering behavior.

- Any regular AzApi update plan that includes sensitive properties can trigger this path.
- The issue does not depend on custom templates or crafted payloads; it is in built-in helper logic used by standard rendering.
- Therefore, standard CI/PR report generation is potentially affected.

### Recommended remediation
- In update rendering helpers, replace sensitive values with masked marker (e.g., `(sensitive)`) unless explicit opt-in is active.
- Pass and honor a `showSensitive` control consistently across helper APIs.
- Add direct unit tests for update diff output containing sensitive fields.

---

## 3) High: Static-analysis fail gate bypass via malformed SARIF inputs

### Severity
- **Estimated CVSS v4.0:** **8.0 (High)**
- **Primary impact:** Integrity High (security gate bypass)

### Description
Malformed/unreadable SARIF files are converted to warnings and excluded from finding counts. The fail-on gate uses only successfully parsed findings, allowing pipeline pass despite intended strict fail policy.

### Evidence
1. Loader swallows per-file exceptions and records warnings:
   - [src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisLoader.cs](src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisLoader.cs#L56-L61)

2. Fail decision counts only parsed findings in model:
   - [src/Oocx.TfPlan2Md/ProgramEntry.cs](src/Oocx.TfPlan2Md/ProgramEntry.cs#L202-L205)
   - [src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisFailureEvaluator.cs](src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisFailureEvaluator.cs#L17-L29)

3. Result: A broken SARIF source can suppress blocker findings from fail calculation.

### Why this is high risk
In PR governance contexts, teams may rely on `--fail-on-static-code-analysis-errors`. If attackers (or accidental corruption) can invalidate SARIF input, critical findings can be omitted from blocking logic, increasing chance of unsafe approvals.

### Can this occur during regular usage?
**Yes (accidental path), and yes (abuse path).**

- **Regular usage / non-malicious:** A truncated, partially written, or tool-incompatible SARIF file (common CI artifact issue) is enough to trigger warning-only handling and reduce fail-gate effectiveness.
- **Abuse path:** If an actor can influence SARIF inputs, they can intentionally provide malformed files to suppress blocking behavior.
- No malicious Terraform plan or template is required; this is independent of plan rendering and resides in code-analysis ingestion/fail logic.

### Recommended remediation
- Add strict mode semantics for fail-on:
  - If `--fail-on-static-code-analysis-errors` is set and any SARIF input fails to parse/load, return non-zero fail code.
- Emit explicit “gate integrity failure” error for unreadable analysis inputs.
- Add tests for malformed SARIF ensuring fail-on exits with failure.

---

## Additional observations (non-high)
- No direct memory-unsafety vectors (buffer overflow class) were found in reviewed managed C# paths.
- No direct plan-driven arbitrary write primitive was identified beyond user-specified output path behavior.
- Existing defensive checks for JSON type mismatches in parent/child/configuration parsing appear present in reviewed code paths.

---

## Risk Prioritization
1. **Immediate:** Fix AzApi sensitive-data leakage paths (Findings 1 and 2).
2. **Immediate:** Harden SARIF fail-gate semantics (Finding 3).
3. **Near-term:** Expand negative tests and fuzz-like malformed input tests for plan/sarif/template edges.

---

## Suggested Validation Plan After Fixes
- Add/adjust unit tests:
  - AzApi create/update/delete with `after_sensitive` and `before_sensitive` coverage.
  - Ensure snapshot baselines never contain known sensitive markers (password/token/secret) unless `--show-sensitive` explicitly enabled.
  - Malformed SARIF + fail-on should hard-fail.
- Run full test suite and regenerate only intentional snapshots.
- Manual smoke test with representative CI workflow.

---

## Confidence
- **High confidence** for all three findings due to direct code-path evidence and, for Finding 1, confirmed fixture→snapshot leakage proof.
