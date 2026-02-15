# ADR-004: CLI Option for Unchanged Value Filtering

## Status

Proposed

## Context

Users have requested a way to reduce noise in the generated markdown reports by hiding attributes that haven't changed. Currently, the tool displays all attributes that are present in the plan's `before` or `after` state, even if the values are identical. This makes it harder to spot the actual changes in large resources.

The requirement is to hide unchanged values by default, but provide a CLI option to show them if needed.

## Options Considered

### Option 1: Filter in Template
- Pass the flag to the template.
- Update the default template (and all other templates) to check `if show_unchanged_values || attr.before != attr.after`.
- **Pros**: Logic is visible in the template.
- **Cons**: Requires updating all templates. Custom templates would need to implement this logic manually to match the default behavior. Harder to enforce consistency.

### Option 2: Filter in ReportModelBuilder (Recommended)
- Pass the flag to `ReportModelBuilder`.
- Filter the `AttributeChanges` list during model construction.
- **Pros**: Consistent behavior across all templates (default and custom). Simplifies templates (they just iterate over what they are given). Performance benefit (smaller list passed to Scriban).
- **Cons**: Templates lose access to unchanged values if they wanted to show them for some reason (unless they use raw JSON).

## Decision

We will implement **Option 2: Filter in ReportModelBuilder**.

This approach ensures that the "noise reduction" goal is met consistently across the application without requiring template changes. It aligns with the philosophy that the `ReportModel` should be a view-model prepared for rendering.

## Implementation Details

1.  **CLI**: Add `--show-unchanged-values` (boolean) to `CliOptions` and `CliParser`.
2.  **ReportModelBuilder**:
    -   Update constructor to accept `bool showUnchangedValues`.
    -   In `BuildAttributeChanges`, filter the list:
        ```csharp
        if (!_showUnchangedValues && before == after) continue;
        ```
3.  **ReportModel**: Add `public bool ShowUnchangedValues { get; init; }` to the model, so templates can be aware of the setting (e.g., to display a disclaimer).
4.  **Program.cs**: Pass the option from `CliOptions` to `ReportModelBuilder`.

## Consequences

### Positive
-   Cleaner default output.
-   No changes needed in existing templates.
-   Consistent filtering logic.

### Negative
-   If a custom template *specifically* wants to show unchanged values while the user didn't pass the flag, it won't be able to use the convenient `AttributeChanges` list. It would have to parse `BeforeJson`/`AfterJson`. This is an acceptable trade-off as the CLI flag is the intended control mechanism.
