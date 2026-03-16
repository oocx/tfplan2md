# Security fix: sensitive output values no longer leak; large JSON outputs are now pretty-printed

Two bugs in the Terraform output value renderer have been fixed. One is a **critical security issue** that could expose sensitive secret values in generated Markdown. The other is a formatting regression where large JSON outputs appeared as compact single-line blobs instead of readable indented JSON.

## 🐛 Bug fixes

- **[Security] Sensitive + large output values no longer leak their raw secret.**
  When a Terraform output was both `sensitive = true` and larger than 80 characters (so it would normally appear in a below-table code block), the table cell incorrectly showed `_(see below)_` instead of `(sensitive value)`, and the below-table block emitted the raw secret verbatim. The root cause was two issues in `RenderOutputTable`: the `IsMasked` check was evaluated *after* `IsLargeOutputValue` in the table-row path, and the below-table rendering loop had no `IsMasked` guard at all. Both are now fixed: `IsMasked` is checked first in the table cell, and masked outputs are unconditionally skipped in the below-table loop.

- **Large JSON output values are now pretty-printed in below-table blocks.**
  When a large output value was a JSON object or array, the below-table code block rendered it as a compact single-line string (e.g. `[{"a":1},{"b":2}]`). This happened because the below-table rendering path called `JsonElement.ToString()` directly, bypassing the existing pretty-print logic used for table cells. A new `FormatLargeOutputValueContent` helper using `Utf8JsonWriter` with `Indented = true` now produces correctly indented JSON in these blocks.

## 🔗 Commits

- [`db04a59`](https://github.com/oocx/tfplan2md/commit/db04a59) fix: sensitive large output values no longer leak; JSON large values are pretty-printed
