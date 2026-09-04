# Architecture: Separate Table for azapi Output Values

## Status

No architectural changes required. Feature can be implemented through template changes only.

## Analysis

The feature can be implemented entirely through additions to the two existing azapi Scriban
templates. The `RenderAzapiBody` C# helper — already registered as `render_azapi_body` in
Scriban — is generic enough to handle any JSON sub-object, not just `body`. It already
provides:

- **Feature 034 attribute grouping** via `IdentifyGroupedPrefixes` (automatic, no configuration
  needed)
- **Sensitivity masking** via `beforeSensitive` / `afterSensitive` parameters
- **Large-value handling** via `largeValueFormat` parameter
- **All rendering modes:** `"create"`, `"update"`, `"delete"`

The `output` attribute in the azapi plan JSON is a direct sibling of `body` at the top level
of `change.after_json` / `change.before_json`. It can be passed directly to `render_azapi_body`
as `change.after_json.output` or `change.before_json.output`.

**No new C# helpers, no new C# files, and no changes to existing C# code are required.**

## Implementation Guidance

### Template Strategy

Add an **Output Values rendering block** to both templates, immediately after the existing body
rendering block. The block is wrapped in a visibility guard so the Output Values section is
omitted entirely when there is no relevant output data.

### Visibility Guard

The output section is rendered only when at least one of these is true:

```scriban
{{~ has_before_output = change.before_json && change.before_json.output ~}}
{{~ has_after_output = change.after_json && change.after_json.output ~}}
{{~ output_unknown = change.after_unknown && change.after_unknown.output ~}}
{{~ if has_before_output || has_after_output || output_unknown ~}}
  ... output rendering logic ...
{{~ end ~}}
```

When none of these conditions is true, no heading and no table are emitted.

### "Known After Apply" Handling

Before calling `render_azapi_body`, the template checks `change.after_unknown.output`. If
that field is truthy (Terraform marks the whole output block as unknown during a create before
apply), render a notice paragraph instead of calling the helper:

```scriban
{{~ if output_unknown ~}}

*Output values are not known until after apply.*
{{~ else ~}}
  ... render_azapi_body call ...
{{~ end ~}}
```

This is a pure template-level check — no C# change is required.

### Sensitivity Handling

The templates pass the output-specific sensitivity sub-objects:

- `change.after_sensitive.output` as `afterSensitive`
- `change.before_sensitive.output` as `beforeSensitive`

These are accessed identically to `change.after_sensitive.body` in the existing body sections.
If `after_sensitive` or `before_sensitive` is null (no sensitive markers at all), the Scriban
null-safe access `change.after_sensitive ? change.after_sensitive.output : null` returns null,
and `render_azapi_body` behaves as if no fields are sensitive.

### Heading Design

The string `"Output Values"` is passed as the `heading` parameter to `render_azapi_body`.
The helper renders this as `#### Output Values` at the section level (H4). This is determined
by the hardcoded `sb.AppendLine($"#### {heading}");` in `AzApi.Rendering.cs` line 60.
Grouped sub-sections are rendered at H6 (`###### \`prefix\``) by the grouping renderers in
`AzApi.Rendering.CreateDelete.cs` and `AzApi.Rendering.Update.cs`.

> **Note on sub-section headings:** The Feature Specification's success criteria mentions
> sub-section headings of the form `` Output Values - `properties` ``. However,
> `RenderAzapiBody`'s grouped sub-sections are rendered as `` ###### `properties` `` (group
> path only, no parent heading prefix — consistent with how body sub-sections appear today).
> The `#### Output Values` top-level heading provides sufficient context. Modifying the C#
> grouping renderer to prepend the section heading is out of scope for this minimal change; the
> developer should implement using the existing heading format.

No action-specific heading suffix is needed (e.g., no "Output Values (being deleted)"). The
"Before" / "After" / "Value" column headers already convey change direction.

### Per-Action Logic for `resource.sbn` (azapi_resource)

| Action | Before Output | After Output | Rendered As |
|--------|--------------|--------------|-------------|
| create | absent | `after_unknown.output = true` | `*Output values are not known until after apply.*` |
| create | absent | present | `render_azapi_body(after_json.output, "Output Values", "create", null, null, after_sensitive.output, ...)` |
| update | present | present | `render_azapi_body(after_json.output, "Output Values", "update", before_json.output, before_sensitive.output, after_sensitive.output, ...)` |
| update | absent | present | treat as create mode (after only) |
| update | present | absent | treat as delete mode (before only) |
| delete | present | absent | `render_azapi_body(before_json.output, "Output Values", "delete", null, before_sensitive.output, null, ...)` |
| replace | present | `after_unknown.output = true` | render before output in delete mode + notice |
| replace | present | present | `render_azapi_body(after_json.output, "Output Values", "update", before_json.output, ...)` |
| replace | absent | `after_unknown.output = true` | notice only |
| replace | absent | present | `render_azapi_body(after_json.output, "Output Values", "create", ...)` |

For the replace action, the template can share the create/replace branch already used for body
rendering; the `after_unknown` check handles the "unknown after apply" sub-case.

### Per-Action Logic for `update_resource.sbn` (azapi_update_resource)

`azapi_update_resource` supports only `update` and `delete` actions (it cannot `create` or
`replace`). The output rendering logic is identical to the corresponding rows in the table
above.

### Scriban Snippet — resource.sbn Output Block

Add immediately after the closing `{{~ end ~}}` of the body rendering block (before the
`code_analysis_findings` include):

```scriban
{{~ # Output Values rendering ~}}
{{~ has_before_output = change.before_json && change.before_json.output ~}}
{{~ has_after_output = change.after_json && change.after_json.output ~}}
{{~ output_unknown = change.after_unknown && change.after_unknown.output ~}}
{{~ if has_before_output || has_after_output || output_unknown ~}}
{{~ if change.action == "create" || change.action == "replace" ~}}
{{~ if output_unknown ~}}

*Output values are not known until after apply.*
{{~ else if has_before_output && has_after_output ~}}
{{~ before_sensitive_output = change.before_sensitive ? change.before_sensitive.output : null ~}}
{{~ after_sensitive_output = change.after_sensitive ? change.after_sensitive.output : null ~}}
{{ render_azapi_body change.after_json.output "Output Values" "update" change.before_json.output before_sensitive_output after_sensitive_output false "inline-diff" show_sensitive }}
{{~ else if has_after_output ~}}
{{~ after_sensitive_output = change.after_sensitive ? change.after_sensitive.output : null ~}}
{{ render_azapi_body change.after_json.output "Output Values" "create" null null after_sensitive_output false "inline-diff" show_sensitive }}
{{~ end ~}}
{{~ else if change.action == "update" ~}}
{{~ if has_before_output && has_after_output ~}}
{{~ before_sensitive_output = change.before_sensitive ? change.before_sensitive.output : null ~}}
{{~ after_sensitive_output = change.after_sensitive ? change.after_sensitive.output : null ~}}
{{ render_azapi_body change.after_json.output "Output Values" "update" change.before_json.output before_sensitive_output after_sensitive_output false "inline-diff" show_sensitive }}
{{~ else if has_after_output ~}}
{{~ after_sensitive_output = change.after_sensitive ? change.after_sensitive.output : null ~}}
{{ render_azapi_body change.after_json.output "Output Values" "create" null null after_sensitive_output false "inline-diff" show_sensitive }}
{{~ else if has_before_output ~}}
{{~ before_sensitive_output = change.before_sensitive ? change.before_sensitive.output : null ~}}
{{ render_azapi_body change.before_json.output "Output Values" "delete" null before_sensitive_output null false "inline-diff" show_sensitive }}
{{~ end ~}}
{{~ else if change.action == "delete" ~}}
{{~ if has_before_output ~}}
{{~ before_sensitive_output = change.before_sensitive ? change.before_sensitive.output : null ~}}
{{ render_azapi_body change.before_json.output "Output Values" "delete" null before_sensitive_output null false "inline-diff" show_sensitive }}
{{~ end ~}}
{{~ end ~}}
{{~ end ~}}
```

### Scriban Snippet — update_resource.sbn Output Block

Add immediately after the closing `{{~ end ~}}` of the body rendering block (before the
`code_analysis_findings` include):

```scriban
{{~ # Output Values rendering ~}}
{{~ has_before_output = change.before_json && change.before_json.output ~}}
{{~ has_after_output = change.after_json && change.after_json.output ~}}
{{~ if has_before_output || has_after_output ~}}
{{~ if change.action == "update" ~}}
{{~ if has_before_output && has_after_output ~}}
{{~ before_sensitive_output = change.before_sensitive ? change.before_sensitive.output : null ~}}
{{~ after_sensitive_output = change.after_sensitive ? change.after_sensitive.output : null ~}}
{{ render_azapi_body change.after_json.output "Output Values" "update" change.before_json.output before_sensitive_output after_sensitive_output false "inline-diff" show_sensitive }}
{{~ else if has_after_output ~}}
{{~ after_sensitive_output = change.after_sensitive ? change.after_sensitive.output : null ~}}
{{ render_azapi_body change.after_json.output "Output Values" "create" null null after_sensitive_output false "inline-diff" show_sensitive }}
{{~ else if has_before_output ~}}
{{~ before_sensitive_output = change.before_sensitive ? change.before_sensitive.output : null ~}}
{{ render_azapi_body change.before_json.output "Output Values" "delete" null before_sensitive_output null false "inline-diff" show_sensitive }}
{{~ end ~}}
{{~ else if change.action == "delete" ~}}
{{~ if has_before_output ~}}
{{~ before_sensitive_output = change.before_sensitive ? change.before_sensitive.output : null ~}}
{{ render_azapi_body change.before_json.output "Output Values" "delete" null before_sensitive_output null false "inline-diff" show_sensitive }}
{{~ end ~}}
{{~ end ~}}
{{~ end ~}}
```

Note: `update_resource.sbn` uses `{{~ if has_before_output || has_after_output ~}}` (no
`output_unknown` guard) because `azapi_update_resource` cannot perform create or replace
actions, so `after_unknown.output = true` does not apply.

## Components Affected

The following files require changes:

| File | Change |
|------|--------|
| `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn` | Add output rendering block after body block (before `_code_analysis_findings.sbn` include) |
| `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/update_resource.sbn` | Add output rendering block after body block (before `_code_analysis_findings.sbn` include) |
| `TestData/` | New test plan JSON files covering output scenarios |
| `tests/` snapshot files | New approved snapshot files for output rendering scenarios |

No C# source files require changes.

## Test Scenarios Required

The Quality Engineer should define test cases covering:

1. **create, output unknown** — `after_unknown.output = true` → notice rendered, no table
2. **create, output present** — `after_json.output` populated → create-mode table
3. **update, output changed** — both before/after output present with differences → update table
4. **update, output unchanged** — both before/after output identical → "no changes" message
5. **delete, output present** — `before_json.output` populated → delete-mode table
6. **replace, after unknown** — `after_unknown.output = true` + `before_json.output` present
7. **no output** — neither before nor after output present → section omitted entirely
8. **sensitive output** — `after_sensitive.output` or `before_sensitive.output` marks fields →
   values masked
9. **grouped output** — `output` has nested properties triggering Feature 034 grouping →
   sub-section tables rendered
10. **large output value** — a string value in `output` exceeds the large-value threshold →
    collapsible rendering used
