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

### Option 2: Upgrade `format_diff` (Recommended)
- Update `format_diff` to accept an optional `format` parameter.
- Use the same rendering logic as `format_large_value` (supporting `inline-diff` and `standard-diff`).
- **Pros:** consistent API, reuses existing logic, easy to update templates.
- **Cons:** requires updating `ScribanHelpers.cs`.

## Decision

We will upgrade `ScribanHelpers.FormatDiff` and update the Scriban templates.

### 1. Update `ScribanHelpers.FormatDiff`
- Change signature to `public static string FormatDiff(string? before, string? after, string format = "standard-diff")`.
- Implement logic to parse the format string (using `ParseLargeValueFormat` or similar).
- Use the same diff rendering logic as `FormatLargeValue` to support:
  - `inline-diff`: HTML-based diff with character highlighting.
  - `standard-diff`: Text-based diff with `+`/`-` markers.
- Ensure it handles "small" values correctly (no collapsing needed, just formatting).

### 2. Update Scriban Templates
- **`default.sbn`**:
  - Swap backticks in attribute tables: `| {{ attr.name }} | `{{ attr.value }}` |`.
- **`role_assignment.sbn`**:
  - Swap backticks in attribute tables.
  - Update summary lines to only code-format values (e.g., principal name, role name), not connecting text.
- **`firewall_network_rule_collection.sbn` & `network_security_group.sbn`**:
  - Update headers to code-format values (Collection name, Priority, Action).
  - Update rule tables to code-format all data columns.
  - Update `format_diff` calls to pass `large_value_format` (e.g., `{{ format_diff rule.protocols.before rule.protocols.after large_value_format }}`).

## Rationale

- **Consistency:** Using the same diff logic for small and large values ensures a uniform look and feel.
- **Configurability:** Passing `large_value_format` allows the user to control the diff style globally via the existing CLI option.
- **Readability:** Reversing the backticks focuses attention on the data, which is the primary goal of the report.

## Implementation Notes

- **Shared Logic:** Consider refactoring the diff rendering logic from `FormatLargeValue` into a private helper method to avoid code duplication in `FormatDiff`.
- **Template Variable:** The `large_value_format` variable is available in the root `ReportModel` and can be accessed in templates.
- **Escaping:** Ensure `EscapeMarkdown` is called correctly within the new `FormatDiff` implementation to prevent XSS or broken markdown.
