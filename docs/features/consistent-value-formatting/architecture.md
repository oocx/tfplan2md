# Architecture: Consistent Value Formatting

## Status

Proposed

## Context

The "Consistent Value Formatting" feature aims to improve readability by ensuring that actual data values (what changes) are code-formatted, while labels and attribute names remain plain text. Additionally, it requires enhanced diff formatting for small values, matching the style of large value diffs.

Reference: [Specification](specification.md)

## Options Considered

### Option 1: Separate Helper for Styled Diffs
- Create a new helper `format_styled_diff` specifically for this purpose.
- **Pros:** clear separation from existing `format_diff`.
- **Cons:** `format_diff` is already widely used; better to upgrade it than replace it everywhere with a new name.

### Option 3: Register Configured Helper (Selected)
- Update `RegisterHelpers` to accept the configuration (e.g., `LargeValueFormat`).
- Register `format_diff` as a closure that captures this configuration.
- **Pros:** Keeps templates clean (`{{ format_diff before after }}`), prevents errors (forgetting to pass the arg), centralizes configuration.
- **Cons:** Requires threading configuration through `MarkdownRenderer`.

## Decision

We will upgrade `ScribanHelpers.FormatDiff` and register it with the configured format.

### 1. Update `ScribanHelpers.FormatDiff`
- Change signature to `public static string FormatDiff(string? before, string? after, string format)`.
- Implement logic to parse the format string (using `ParseLargeValueFormat` or similar).
- Use a **table-compatible** rendering logic (avoiding block elements like `<pre>` or ` ``` `):
  - `inline-diff`: HTML-based diff with character highlighting, using `<br>` for line breaks and `<code>` spans.
  - `standard-diff`: Text-based diff with `+`/`-` markers, using `<br>` for line breaks.
- Ensure it handles "small" values correctly (no collapsing needed, just formatting).

### 2. Update `ScribanHelpers.RegisterHelpers`
- Update signature to accept `LargeValueFormat`.
- Register `format_diff` as a closure: `(b, a) => FormatDiff(b, a, formatString)`.

### 3. Update `MarkdownRenderer`
- Pass `ReportModel.LargeValueFormat` to `RegisterHelpers`.
- Update `RenderResourceChange` and `RenderResourceWithTemplate` to accept `LargeValueFormat`.

### 4. Update Scriban Templates
- **`default.sbn`**:
  - Swap backticks in attribute tables: `| {{ attr.name }} | `{{ attr.value }}` |`.
- **`role_assignment.sbn`**:
  - Swap backticks in attribute tables.
  - Update summary lines to only code-format values (e.g., principal name, role name), not connecting text.
- **`firewall_network_rule_collection.sbn` & `network_security_group.sbn`**:
  - Update headers to code-format values (Collection name, Priority, Action).
  - Update rule tables to code-format all data columns.
  - **No change needed for `format_diff` calls** (they remain `{{ format_diff ... }}`).

## Rationale

- **Clean Templates:** Templates don't need to know about global configuration options.
- **Consistency:** Using the same diff logic for small and large values ensures a uniform look and feel.
- **Configurability:** The `format_diff` helper automatically respects the CLI option.
- **Readability:** Reversing the backticks focuses attention on the data.

## Implementation Notes

- **Shared Logic:** Consider refactoring the diff rendering logic from `FormatLargeValue` into a private helper method to avoid code duplication in `FormatDiff`.
- **Escaping:** Ensure `EscapeMarkdown` is called correctly within the new `FormatDiff` implementation to prevent XSS or broken markdown.
