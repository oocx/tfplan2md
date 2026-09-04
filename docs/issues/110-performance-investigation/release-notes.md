# Performance: improve diff computation for edge cases with large attribute values

This release improves performance when processing Terraform plans that contain large
attribute values (such as minified JSON IAM policies) or a high number of changed attributes.
Several algorithmic improvements reduce worst-case complexity and add fast paths for common
short-value comparisons.

## ⚡ Performance improvements

### Added LCS matrix size guard for large attribute values

**Problem:** The character-level diff algorithm used an O(m×n) LCS (Longest Common
Subsequence) matrix that could become very expensive for large attribute values. For example,
comparing two 50K-character values would require 2.5 billion matrix iterations.

**Fix:** Added a `MaxLcsMatrixCells` guard (10 million cells). When both values exceed this
threshold, the tool gracefully degrades to a whole-value diff (red/green) instead of
attempting a character-level diff. No data is lost — the full before and after values are
still shown.

**User-visible change:** Very large attribute values (where `before.Length × after.Length`
exceeds 10M) now display as whole-value red/green diff instead of character-level highlighting.

### Cached LCS results to eliminate double computation for large value summaries

**Problem:** When rendering a large value summary (the collapsed `<summary>` line),
`BuildLineDiff` was called twice with the same inputs — once for the summary and once for
the expanded content — because the Scriban template invoked both helpers independently.

**Fix:** Added a `[ThreadStatic]` cache for `BuildLineDiff` results, cleared in `finally`
blocks after each render pass. The second call hits the cache instead of recomputing the
full LCS. This is purely an internal optimization with no output change.

### Improved list removal in parent–child merging from O(c×n) to O(n)

**Problem:** `ReportModelBuilder.ParentChildMerging` used a loop of `List.Remove()` calls
to remove merged children — each `Remove()` scans the full list, giving O(c×n) cost.

**Fix:** Replaced with a `HashSet` lookup and a single `RemoveAll()` pass. No output change.

### Improved module group ordering from O(g×n) to O(g)

**Problem:** When ordering resource change groups by their original plan position,
`FindIndex` was called inside a LINQ chain for each group — scanning the full changes list
each time.

**Fix:** Pre-computed a `firstIndexByModule` dictionary in a single O(n) pass, replacing
O(g×n) `FindIndex` calls with O(1) dictionary lookups. No output change.

### Improved configuration reference lookup from O(n) to O(1)

**Problem:** Looking up configuration references by resource address scanned the full
reference list linearly for every resource.

**Fix:** Built a secondary `_configurationReferencesByAddress` dictionary index (keyed by
address) for O(1) lookups. No output change.

### Compiled regex instances

**Problem:** Five regex patterns in `MarkdownRenderer` were re-compiled on every invocation
via inline `Regex.Replace()` calls.

**Fix:** Converted to `static readonly Regex` fields with `RegexOptions.Compiled`. The
patterns and timeouts are unchanged. No output change.

### Added fast path for short attribute values in Azure DevOps diff

**Problem:** The Azure DevOps diff formatter ran the full LCS algorithm on ALL changed
attribute values, including trivial ones like `"true"` → `"false"` or short resource names.

**Fix:** Added a fast path (`FastPathMaxLength = 50`) that bypasses LCS for single-line
values under 50 characters. These short values render as whole-value red/green diff (the
same format as the LCS guard for large values).

**User-visible change:** Short single-line attribute values (under 50 characters) in Azure
DevOps output now show whole-value red/green diff instead of character-level highlighting.
For values this short, the visual difference is negligible.

### Added JSON/XML structural heuristics before parse attempts

**Problem:** `TryFormatStructuredContent()` attempted `JsonDocument.Parse()` and XML parsing
on every large value, including plain strings and numbers. The parse exceptions added overhead
and noise.

**Fix:** Added cheap structural heuristics: JSON requires `{`+`}` or `[`+`]`; XML requires
`<`+`>`. Values that don't match these markers skip the parse attempt entirely. No output
change — real-world structured Terraform values (IAM policies, rule sets, config blocks)
always contain these markers.

## 🔗 Commits

- [`9704fd5`](https://github.com/oocx/tfplan2md/commit/9704fd5fbcce57e9b8666efc2f3af0bdc1742b0b) perf: add LCS matrix size guard to prevent O(m×n) blowup with large values
- [`be945e2`](https://github.com/oocx/tfplan2md/commit/be945e2d93c7d13ff193e380a0eb37c84e7d522d) perf: implement findings 2-9 performance optimizations
- [`60d35cb`](https://github.com/oocx/tfplan2md/commit/60d35cb49596be28ef056e75355d4e62baacc23e) fix: address code review findings — remove redundant HashSet copy and fix CSS style consistency
