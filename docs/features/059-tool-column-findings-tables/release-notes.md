# Tool Column in Findings Tables

This release adds a "Tool" column to all code analysis findings tables, making it easier to identify which security/quality scanner produced each finding when using multiple tools.

## ✨ Features

**Tool Column Display**: A new "Tool" column now appears between "Severity" and "Attribute/Finding" columns in all three types of findings tables:
- **Per-Resource Security & Quality Findings** (inline with resource changes)
  - Column order: `Severity | Tool | Attribute | Finding | Remediation`
- **Module-Level Findings** (module findings section)
  - Column order: `Severity | Tool | Finding | Remediation`
- **Unmatched Findings** (unmatched findings section)
  - Column order: `Severity | Tool | Finding | Remediation`

The tool name is extracted directly from SARIF files and displayed as provided by the scanner (e.g., "Checkov", "Trivy", "tflint", "Semgrep"). When tool name information is missing from SARIF, the column displays "-".

**Why This Matters**: When reviewing infrastructure code with multiple security scanners (a common best practice), you can now immediately see which tool flagged each issue. This helps you:
- Quickly assess finding credibility based on the tool's strengths
- Identify tool-specific false positives
- Understand coverage gaps when certain tools don't flag an issue
- Make faster remediation decisions based on tool reputation

## 🔗 Commits

User-facing changes only (internal task tracking and documentation commits excluded):

- [`d0d15ec`](https://github.com/oocx/tfplan2md/commit/d0d15ec) feat: add Tool column to code analysis findings tables
- [`0644647`](https://github.com/oocx/tfplan2md/commit/0644647) fix: handle empty tool names correctly in templates
- [`83ad0f6`](https://github.com/oocx/tfplan2md/commit/83ad0f6) test: add unit tests for Tool column in findings tables

## 🚨 Breaking changes

⚠️ **Custom Template Users**: If you maintain custom Scriban templates for code analysis findings (`_code_analysis_findings.sbn` or `_code_analysis_other_findings.sbn`), you'll need to add the Tool column to match the new table structure. See the built-in templates for the exact syntax:

```scriban
| Severity | Tool | Attribute | Finding | Remediation |
| -------- | ---- | --------- | ------- | ----------- |
| ... | {{ if finding.tool_name && finding.tool_name != "" }}{{ finding.tool_name }}{{ else }}-{{ end }} | ... |
```

This change does not affect default template users or require any CLI flag changes.

## ▶️ Getting started

No usage changes required. The Tool column appears automatically in all findings tables when using `--sarif` flag with SARIF input files:

```bash
tfplan2md plan.json --sarif checkov.sarif --sarif trivy.sarif > plan.md
```

The tool name is read from the `runs[].tool.driver.name` field in each SARIF file and displayed in the new Tool column.
