# Security fixes: SARIF fail gate, path traversal, action classification, HTML injection

Five security and correctness issues identified in the Issue 097 security audit are resolved in this release. All five had already been tracked as open after Issue 098 addressed the sensitive-value masking category; this release closes the remaining findings.

## 🐛 Bug fixes

- **SARIF fail gate now catches parse failures** (`--fail-on-static-code-analysis-errors`): previously, a corrupted or truncated SARIF file would silently reduce the in-memory finding count to zero and the gate would pass. The gate now treats any SARIF load warning as an immediate failure — the failed file paths and error messages are written to stderr and the process exits with code 10. A clean SARIF batch continues to behave as before.

- **Path traversal blocked in custom template directory**: when `--template-dir` is used, template names containing `../` segments could previously resolve to files outside the configured directory. The loader now canonicalizes both the template directory and every candidate path and rejects any path that escapes the root, throwing a clear `InvalidOperationException` with the offending path.

- **`[Details]` links in code-analysis tables use angle-bracket URLs**: `help_uri` values from SARIF were previously rendered as `[Details](url)`, which breaks for any URL containing `)`. Links now use the CommonMark angle-bracket form `[Details](<url>)`, and `<`/`>` characters in the URI are percent-encoded, so the link is robust to any URL content.

- **HTML injection via `model.Type` fixed**: the resource type field in the summary HTML block was rendered unencoded, allowing a crafted plan with a `<script>` tag in the resource type to inject HTML. The field is now encoded with `HtmlEncoder.Default.Encode`.

- **Terraform `forget` action correctly classified**: the `forget` action (Terraform 1.7+, removes a resource from state without destroying infrastructure) was previously treated as a no-op. It is now recognized as a distinct action (reported as ❌ `forget`) and counted in the **Destroy** total in the plan summary, matching Terraform's own semantic. Non-empty unrecognized action sets are classified as `unknown` (⚠️) rather than no-op.

## 🔗 Commits

- [`20084372`](https://github.com/oocx/tfplan2md/commit/20084372d1342dd62e1cb46e0ac9d74c16de386a) fix: close issue 099 remaining security findings
- [`9647176d`](https://github.com/oocx/tfplan2md/commit/9647176dd1f24d1afd79b09d977c408bd1cb3490) fix: address issue 099 code review findings (SNAPSHOT_UPDATE_OK)
