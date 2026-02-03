# Architecture: Improved AzAPI Attribute Grouping and Array Rendering

## Status

Proposed

## Context

This feature improves how AzAPI JSON `body` attributes are rendered in tfplan2md reports, targeting readability in GitHub and Azure DevOps pull request comments.

The current AzAPI rendering (Feature 040) flattens the JSON body into a single table using dot/bracket paths (e.g., `connectionStrings[0].name`). While functional, it becomes hard to review for real-world bodies that contain arrays and nested structures.

The feature specification defines two related problems:

- Prefix/path repetition (visual clutter)
- Poor readability of array-indexed attributes

The specification also includes a resolved rendering strategy selection. See:

- [specification.md](specification.md)
- [rendering-options.md](rendering-options.md)
- [docs/features/040-azapi-resource-template/architecture.md](../040-azapi-resource-template/architecture.md)
- [docs/report-style-guide.md](../../report-style-guide.md)

### Constraints

- Output must render correctly in GitHub and Azure DevOps PR comments.
- Must follow the style guide: “Data is Code, Labels are Text”.
- Must preserve all information; grouping is a presentation change, not data loss.
- Must respect sensitive-value masking behavior.
- Must remain performant on large plans (no significant slowdown).

## Decision Drivers

- Improve scanability and structural understanding of AzAPI bodies.
- Avoid “too many tiny tables” while still separating arrays clearly.
- Keep templates maintainable by putting complex logic in helpers/models, not Scriban.
- Keep the output deterministic and markdownlint-friendly.

## Options Considered

This feature’s rendering strategy selection is already resolved in the feature specification.

### Option 1: Flat table only (status quo)

- Pros: simplest, consistent, minimal logic.
- Cons: noisy for arrays; hard to understand object/array boundaries.

### Option 2: Fully nested sections for all shared prefixes

- Pros: strong hierarchy; clean local property names.
- Cons: can explode into many headings/tables; more complex and less predictable.

### Option 3 (Selected): Hybrid threshold-based sectioning

- Arrays that meet the grouping threshold get dedicated sections and per-item rendering.
- Non-array prefix groups (meeting the threshold) get dedicated sections.

This extends the specification’s “Option 1D” plus “Option 2C/2A” hybrid strategy by also splitting non-array prefix groups into separate sections.

## Decision

Implement the following strategy for AzAPI JSON body attribute rendering:

1. Keep the base approach: flatten JSON body into a list of “path → value” leaf attributes.
2. Apply array-aware grouping for readability:
   - If an array has at least 3 leaf attributes across its items, render it as a dedicated array section.
   - Remove the repetitive array prefix from property names within each array item.
3. Choose array item rendering format based on complexity:
   - If each item has 8 or fewer properties and the array is sufficiently homogeneous, use a compact “items as rows, properties as columns” table.
   - Otherwise, fall back to “separate table per item” rendering.

4. Split non-array prefix groups into dedicated sections:
  - A “non-array prefix group” is the longest common dot-prefix (not ending in an array index) shared by at least 3 leaf attributes.
    - Example: `cors.allowedOrigins[0]`, `cors.allowedOrigins[1]`, `cors.supportCredentials` share the non-array prefix `cors`.
  - Render qualifying non-array prefix groups as a dedicated section headed by `###### <prefix>` where `<prefix>` is code-formatted (e.g., `###### \`cors\``).
  - Within that section, remove the shared prefix from row keys (render local keys such as `allowedOrigins[0]`, `supportCredentials`).
  - Attributes not belonging to any qualifying group remain in the main body table.

5. Resolve overlaps deterministically:
  - Use the “longest common prefix wins” rule to avoid creating both a parent and child group when both would qualify.
  - If a leaf attribute is included in an array section, it must not also appear in a non-array prefix section (no duplication).

6. Ordering:
  - Render sections in the original flattened attribute order, based on the first occurrence of any attribute in that section.
  - Within a section, preserve original order of attributes.

## Rationale

- Dedicated array sections fix the biggest readability pain without fragmenting the entire body into many headings.
- Non-array prefix sectioning removes repetitive paths for common nested objects while keeping the main table focused on “singletons”.
- The dual-format array rendering balances compactness (common case) and clarity (complex arrays).
- Keeping ungrouped properties in the main table preserves context and avoids excessive structural churn in the output.

## Consequences

### Positive

- Reviewers can quickly understand array boundaries and compare items.
- Body tables become shorter and less repetitive.
- Output remains consistent with the report style guide.

### Negative / Risks

- Rendering becomes more complex than a simple “flatten and table” approach.
- Non-array prefix sectioning can increase the number of headings/tables for bodies with many nested objects.
- Homogeneity detection for arrays introduces edge cases that must be tested (heterogeneous item shapes).
- Update rendering for arrays must remain clear and not hide important diffs.

## Implementation Notes

High-level guidance for the Developer agent (no implementation in this document).

### Where the logic should live

- Prefer implementing grouping/format selection in AzAPI-specific helpers/view models rather than in Scriban templates.
- Templates should primarily iterate over a prepared “body render model” such as:
  - Main-table rows
  - Zero or more “non-array prefix sections” (object groups)
  - Zero or more “array sections”, each containing either:
    - A compact matrix table model, or
    - A list of per-item tables

This matches the existing architectural direction in Feature 040.

### Array grouping threshold

- The grouping trigger is the fixed threshold from the specification: only create an array section when the array accounts for at least 3 flattened leaf attributes (across all indices).
- Arrays below the threshold remain in the main table using the existing flat representation.

### Non-array prefix grouping threshold

- Apply the same fixed threshold (≥3 leaf attributes) to non-array prefix groups.
- Prefix groups below the threshold remain in the main table.

### Naming and headings

- Use an H6 heading for array sections consistent with the specification examples (e.g., `###### <path> Array`).
- Render the array “path” as code (backticks) and the word “Array” as plain text.
- For nested arrays (e.g., `cors.allowedOrigins`), prefer the full path (code-formatted) in headings to avoid ambiguity.

- Use an H6 heading for non-array prefix sections as just the prefix in code formatting (e.g., `###### \`cors\``).

- If the attribute key would normally include an icon when rendered in the main table, include the same icon in the section heading as well.
  - Example: `###### 🧩 \`cors\`` (icon is plain text; only the key/path is code-formatted).

### Overlap, precedence, and ordering

- Use “longest common prefix wins” to avoid generating both a parent and child non-array prefix section when both would otherwise qualify.
- Ensure no leaf attribute appears in more than one place:
  - If rendered inside an array section, it must not also appear in a non-array prefix section or the main table.
  - If rendered inside a non-array prefix section, it must not also appear in the main table.
- Preserve original flattened attribute order for section ordering and row ordering:
  - Order sections by the first occurrence of any of their member attributes.
  - Within each section, keep the original ordering of rows.

### Update / replace operations

Adopt the MVP recommendation from the specification:

- If any attribute inside an array section changes, render the full array section (do not show only changed items initially).
- Highlight changes using the existing before/after conventions:
  - For per-item tables: use `Property | Before | After` columns.
  - For compact matrix tables: render per-cell before/after content in a way consistent with existing diff formatting (e.g., stacked `-`/`+` lines) while keeping values code-formatted.

### Nested arrays

- MVP: treat only the outermost array dimension as a grouping boundary.
- Nested arrays should either remain flattened under the item tables or trigger a later enhancement if real-world examples demand deeper nesting.

### Heterogeneous arrays

Recommended MVP behavior:

- If items do not share a stable property set (heterogeneous schemas), use the per-item tables fallback.
- Avoid generating wide matrix tables with many empty cells in PR comment contexts.

### Testing / validation

- Snapshot tests should cover:
  - Create/update/delete/replace for AzAPI resources
  - Arrays that trigger grouping and arrays that do not
  - Heterogeneous arrays (fallback behavior)
  - Sensitive value masking remains correct
- Ensure markdownlint compatibility for the produced markdown.

## Components Affected

Expected areas to be modified by the Developer agent:

- AzAPI provider rendering helpers responsible for body flattening and table rendering.
- AzAPI Scriban templates for resources with JSON bodies.
- Test snapshots for AzAPI rendering.
