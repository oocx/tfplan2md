# Architecture: Firewall Rule Before/After Attributes Display

## Status

Proposed

## Analysis

The current implementation of the `azurerm_firewall_network_rule_collection` template uses the `diff_array` helper to identify added, modified, and removed rules. However, for modified rules, it only displays the "after" values.

To implement the requirement of showing before/after values for changed attributes, we need a mechanism to:
1.  Compare the "before" and "after" values of each attribute.
2.  Format the output with `-` and `+` prefixes and a line break if they differ.
3.  Return the single value if they are the same.

Implementing this logic directly in the Scriban template using `if/else` blocks for every column would result in a very verbose and hard-to-maintain template.

## Implementation Guidance

### 1. New Scriban Helper: `format_diff`

We will introduce a new helper function `format_diff` in `ScribanHelpers.cs`.

**Signature:**
```csharp
public static string FormatDiff(string? before, string? after)
```

**Logic:**
- If `before` equals `after`, return `after`.
- If they differ, return `- {before}<br>+ {after}`.
- Handle `null` values by treating them as empty strings or "null" (depending on existing patterns, but likely empty string for display).

**Registration:**
Register this function as `format_diff` in `ScribanHelpers.RegisterHelpers`.

### 2. Template Update

Update `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn`.

In the loop for `diff.modified`:
- Iterate through the columns (Protocols, Source Addresses, etc.).
- For each column, format the `before` and `after` values (e.g., using `array.join ", "`).
- Pass these formatted strings to `format_diff`.

**Example:**
```scriban
{{~ for item in diff.modified ~}}
| 🔄 | {{ item.after.name }} | {{ format_diff (item.before.protocols | array.join ", ") (item.after.protocols | array.join ", ") }} | ...
{{ end }}
```

## Components Affected

- **`src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs`**: Add `FormatDiff` method and register it.
- **`src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn`**: Update the template to use `format_diff`.

## Testing Strategy

- **Unit Tests (`ScribanHelpersTests.cs`)**:
    - Test `FormatDiff` with equal strings.
    - Test `FormatDiff` with different strings.
    - Test `FormatDiff` with nulls.
- **Integration/Template Tests (`MarkdownRendererResourceTemplateTests.cs`)**:
    - Verify that the firewall rule template renders correctly with the new helper.
    - Ensure unchanged attributes show a single value.
    - Ensure changed attributes show the diff format.
