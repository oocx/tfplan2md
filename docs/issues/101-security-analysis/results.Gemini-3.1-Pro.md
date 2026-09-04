# Security Analysis Results (Gemini 3.1 Pro)

This document contains the findings from an independent security review of the `tfplan2md` codebase, focusing on high-severity vulnerabilities (CVSS v4) such as sensitive data exposure, injection flaws, and logic errors that could lead to reviewer deception.

## Executive Summary

The review identified three significant vulnerabilities in the markdown generation and parsing logic. The most critical issue is a flaw in the sensitive data masking logic that can leak secrets if they are stored in top-level arrays. Additionally, there are vulnerabilities related to HTML injection and the mishandling of unrecognized Terraform actions, both of which can be leveraged to deceive reviewers during the Pull Request process.

---

## Findings

### 1. Sensitive Data Exposure via Array Index Masking Bypass

**Severity:** High
**CVSS v4 Score:** 7.1 (`CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N`)
**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` -> `GetHierarchicalPaths`

**Description:**
The application attempts to mask sensitive values by checking if any parent path of an attribute is marked as sensitive in the Terraform plan. However, the logic in `GetHierarchicalPaths` contains a flaw when handling array indices without dot notation. 

The method splits the key by `.` and uses a `for` loop (`for (var i = parts.Length - 1; i > 0; i--)`) to yield parent paths and strip array indices. If a top-level attribute is an array (e.g., `secrets[1]`), `parts.Length` is 1, and the loop body never executes. Consequently, the base array name (`secrets`) is never yielded or checked against the sensitive attributes dictionary. If the parent `secrets` array is marked sensitive, its individual elements will bypass the masking logic and be leaked in plain text in the generated markdown report.

**Recommendation:**
Refactor `GetHierarchicalPaths` to strip array indices from the key itself before or independently of the dot-splitting loop. Ensure that any key containing `[` yields its base name (e.g., `secrets[1]` -> `secrets`) regardless of whether it contains a dot.

---

### 2. Reviewer Deception / Destructive Change Concealment via Unrecognized Actions

**Severity:** Medium / High (Context Dependent)
**CVSS v4 Score:** 5.5 (`CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N`)
**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` -> `DetermineAction`

**Description:**
The `DetermineAction` method evaluates the `actions` array from the Terraform plan JSON to determine the operation type (`create`, `delete`, `update`, `read`). If the array contains an unrecognized action (or is empty), the method falls through to `return NoOpAction`.

Terraform 1.7+ introduced new actions such as `"forget"` (used with `removed` blocks). Because `tfplan2md` does not recognize `"forget"`, it treats the operation as a `no-op`. This causes the resource change to be rendered with a benign "no-op" icon (⊘) and categorized as a non-destructive change. This flaw can hide significant state modifications from reviewers, effectively bypassing manual security and infrastructure review gates.

**Recommendation:**
1. Add explicit handling for new Terraform actions like `"forget"`.
2. Change the fallback behavior: unrecognized actions should not default to `NoOpAction`. Instead, they should be flagged with a distinct warning icon (⚠️) and categorized as an "Unknown" or "Warning" change to ensure they draw the reviewer's attention.
3. Emit a diagnostic warning to stderr when an unrecognized action is encountered.

---

### 3. HTML Injection / Cross-Site Scripting (XSS) via Unescaped Resource Type

**Severity:** Medium
**CVSS v4 Score:** 5.5 (`CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N`)
**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs` -> `BuildSummaryHtml`

**Description:**
In `BuildSummaryHtml`, the `model.Type` property is interpolated directly into the HTML `<summary>` element without being HTML-encoded:
```csharp
var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(displayName)}</b>";
```
While `displayName` is properly encoded via `FormatCodeSummary`, `model.Type` is not. If an attacker can control the resource type string (e.g., via a crafted SARIF file where `logicalLocation.fullyQualifiedName` is free-form text, or a manipulated Terraform plan), they can inject arbitrary HTML or Markdown. 

While modern platforms like GitHub and Azure DevOps sanitize JavaScript (mitigating traditional XSS), an attacker can still inject formatting tags (`<b>`, `<s>`, `<span style="display:none">`) to visually spoof the report. This could be used to hide malicious infrastructure changes or trick the reviewer into approving a dangerous Pull Request.

**Recommendation:**
Wrap `model.Type` with the existing `EscapeHtmlForCode()` method (or a similar HTML-encoding utility) before interpolating it into the HTML string.

---

## Note on Arbitrary File Writes
The application writes the generated markdown to the path specified by the `--output` CLI argument (`ProgramEntry.cs`). This path is not sanitized. However, this is standard behavior for a CLI tool, as the execution context is bounded by the filesystem permissions of the invoking user. This does not constitute a vulnerability unless the CLI is wrapped in a privileged web service or API without proper input validation at that boundary.