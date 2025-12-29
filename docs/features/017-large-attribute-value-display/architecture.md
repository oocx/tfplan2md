# Architecture: Large Attribute Value Display

## Status

Implemented

## Context

The current table-based display for attribute values is unsuitable for large values (multi-line text, long strings), causing layout issues and poor readability. Users need a way to view these large values comfortably, with support for diffing in update/replace scenarios. The solution must support both Azure DevOps (which supports inline HTML styles) and GitHub (which requires standard markdown).

Reference: `specification.md`

## Options Considered

### Option 1: Pure Template Logic
- **Description**: Implement all detection and formatting logic directly in Scriban templates.
- **Pros**: No C# code changes required for logic.
- **Cons**: Complex logic in templates is hard to test and maintain. Diff algorithms are difficult to implement in Scriban.

### Option 2: C# Helper Functions (Recommended)
- **Description**: Implement core logic (detection, diffing, formatting) in C# as Scriban helper functions.
- **Pros**: 
  - Logic is testable via unit tests.
  - Templates remain clean and focused on layout.
  - Performance is better for complex diff operations.
  - Easier to handle the "Complete Replacement" logic (detecting common lines).
- **Cons**: Requires modifying `ScribanHelpers.cs`.

## Decision

**Option 2: C# Helper Functions** is chosen. The complexity of the diff logic (especially the "inline-diff" with character-level highlighting and the "complete replacement" detection) warrants implementation in C#.

## Rationale

- **Testability**: The diff logic has many edge cases (no common lines, mixed line endings, character escaping) that are best verified with C# unit tests.
- **Maintainability**: Keeping complex logic out of templates prevents them from becoming unreadable "spaghetti code".
- **Extensibility**: C# helpers can be easily extended to support new formats or smarter diff algorithms in the future.

## Implementation Notes

### 1. CLI Configuration
- Update `CliOptions` to include `LargeValueFormat` (enum: `InlineDiff`, `StandardDiff`).
- Update `CliParser` to handle `--large-value-format`.
- Default to `InlineDiff`.

### 2. Report Model
- Add `LargeValueFormat` property to `ReportModel` to make the user's choice available in templates.

### 3. Scriban Helpers (`ScribanHelpers.cs`)

Implement the following new helpers:

- **`is_large_value(value)`**:
  - Returns `true` if `value` contains newlines OR length > 100 characters.
  - Returns `false` otherwise.

- **`format_large_value(before, after, format)`**:
  - **Inputs**: `before` (string?), `after` (string?), `format` (string - "inline-diff" or "standard-diff").
  - **Logic**:
    - **Create/Delete**: Return a single code block with the value.
    - **Update/Replace**:
      - If `format == "standard-diff"`: Generate a standard `diff` code block.
      - If `format == "inline-diff"`:
        - Check for common lines between `before` and `after`.
        - **No common lines**: Return two separate code blocks ("Before:" and "After:").
        - **Common lines exist**: Generate HTML `<pre>` block with inline styles for background colors and borders.
          - Use `DiffPlex` or a simple custom diff algorithm to identify changed lines and character-level differences.
          - Apply the specific colors defined in the specification.
          - **Crucial**: Ensure `color: #24292e` is applied to all text elements to support dark mode.

- **`large_attributes_summary(attributes_list, before_obj, after_obj)`**:
  - Generates the summary string: `Large values: attr1 (X lines, Y changed), ...`
  - Calculates total lines and changed lines for each attribute.

### 4. Template Updates (`default.sbn`)

- Iterate through attributes to separate them into `small_attributes` and `large_attributes` lists using `is_large_value`.
- Render `small_attributes` in the existing table.
- Render `large_attributes` in a new `<details>` section below the table.
- Use `large_attributes_summary` for the `<summary>` text.
- Loop through `large_attributes` and call `format_large_value`.

### 5. Dependencies
- No new external dependencies are strictly required if a simple diff algorithm is sufficient. However, `DiffPlex` is a robust option if complex character-level diffing is needed. Given the requirement for "character-level diff highlighting", a library or a robust custom implementation is needed.
- **Decision**: Implement a lightweight custom diff helper or use `DiffPlex` if already available. If not, a simple line-by-line comparison with basic word diffing is sufficient for the "inline-diff" requirement. *Correction*: The spec asks for character-level diffs. A simple custom implementation using `LCS` (Longest Common Subsequence) for lines and then for characters within lines is feasible and avoids extra dependencies.

## Components Affected

- `src/Oocx.TfPlan2Md/CLI/CliOptions.cs`
- `src/Oocx.TfPlan2Md/CLI/CliParser.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`
