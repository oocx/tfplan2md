# Feature: Separate Table for azapi Output Values

## Overview

azapi resources (both `azapi_resource` and `azapi_update_resource`) have an `output` attribute that
contains the JSON of the Azure REST API response. Currently, this output is not rendered in the
Markdown report at all — users have no visibility into the API response values associated with
the resources in their plan.

This feature introduces a dedicated **Output Values** table rendered after the body (input) attributes,
providing clear separation between the values a user configures (inputs, via `body`) and the values
the Azure API returns (outputs, via `output`). Feature 034 attribute grouping, sensitivity masking,
and large-value handling are applied to output values using the same mechanisms already in place for
body attributes.

## User Goals

- **See API response values:** Users want to understand what Azure returns for a resource — such as
  computed URLs, state fields, provisioning information, and generated identifiers — without having
  to look up the raw plan JSON.
- **Clearly distinguish inputs from outputs:** Users reviewing an azapi resource should immediately
  know which values they configured (body) and which values come from the API response (output).
- **Consistent rendering quality:** Output values should be as readable as body values — with
  grouping, array rendering, sensitivity masking, and large-value handling all working identically.

## Scope

### In Scope

- Render the `output` attribute of `azapi_resource` and `azapi_update_resource` in a dedicated
  section labelled **Output Values**, displayed after the body (input) section
- Apply Feature 034 attribute grouping and array rendering to output values (same as body)
- Apply sensitivity masking to output values (same as body), respecting `before_sensitive.output`
  and `after_sensitive.output`
- Apply large-value handling to output values (same as body)
- Support all change actions: create, update, delete, and replace
- Handle the case where output is entirely unknown at plan time (e.g. `after_unknown.output = true`
  during a create with no before output) — the Output Values section is suppressed entirely (no
  heading or content rendered); for a replace with before output present, the before output renders
  in delete mode followed by a "known after apply" notice
- Output section is only shown when an output value is present or known-after-apply; it is omitted
  entirely when output is absent in both before and after states
- Applies to both `azapi_resource` and `azapi_update_resource` templates

### Out of Scope

- Changes to the attribute grouping algorithm itself (Feature 034)
- Rendering output for non-azapi resource types
- Configurable labelling or toggling of the output section
- Expanding or changing the set of top-level attributes shown for azapi resources
- Parsing or interpreting the semantic meaning of specific output fields

## User Experience

### What "Output" Is

In the Terraform azapi provider, every resource can have an `output` argument that captures part of
the Azure REST API response. For example, after creating an Automation Account, the API returns
the provisioning state, the service URL, and SKU details. These are _not_ values the user sets —
they are values returned by Azure:

```json
{
  "output": {
    "properties": {
      "automationHybridServiceUrl": "https://eus-jobruntimedata.azure-automation.net",
      "state": "Ok",
      "sku": { "name": "Basic" }
    }
  }
}
```

### Before This Feature

The `output` attribute is completely invisible in the generated Markdown report. Users must read
the raw Terraform plan JSON to discover what the API returned.

### After This Feature

A separate **Output Values** section appears below the body attributes, using the same grouped
table format. For a create action with no known output yet, a brief notice is shown instead.

**Example: Create (output unknown at plan time)**

*(Output Values section is omitted entirely — no heading or notice is rendered)*

**Example: Update (output has before and after values)**

```markdown
###### Output Values - `properties`

| Property | Before | After |
|----------|--------|-------|
| state | `Ok` | `Updating` |
| automationHybridServiceUrl | `https://eus-jobruntimedata.azure-automation.net` | `https://eus-jobruntimedata.azure-automation.net` |

###### Output Values - `properties.sku`

| Property | Before | After |
|----------|--------|-------|
| name | `Basic` | `Standard` |
```

**Example: Delete (output shown from before state)**

```markdown
###### Output Values - `properties`

| Property | Before |
|----------|--------|
| state | `Ok` |
| automationHybridServiceUrl | `https://eus-jobruntimedata.azure-automation.net` |
```

**Example: No output present**

*(Output Values section is omitted entirely — no section heading is rendered)*

### Behaviour Per Action

| Action | Before Output | After Output | Rendered As |
|--------|--------------|--------------|-------------|
| Create | absent | unknown (`after_unknown.output = true`) | output section absent (suppressed) |
| Create | absent | present (pre-populated) | After table only |
| Update | present | present | Before / After table |
| Delete | present | absent | Before table only |
| Replace | present | unknown or present | Before table + After table or notice |

### Section Heading

The Output Values section uses the heading **"Output Values"** (rather than "Body") to clearly
distinguish it from the input body section. When attribute grouping produces sub-sections (as it
does for body attributes), the sub-section heading pattern becomes
**"Output Values - `<prefix>`"** (e.g., `Output Values - \`properties\``).

### Sensitive Values

If `before_sensitive.output` or `after_sensitive.output` marks fields as sensitive, those fields
are masked in the same way as sensitive body attributes (replaced with `(sensitive)` unless
`--show-sensitive` is passed).

### Large Values

Long string values in the output are handled with the same large-value truncation/collapsible
behaviour already used for body attributes.

## Success Criteria

- [ ] `output` values from `azapi_resource` are rendered in a dedicated **Output Values** section
      after the body section
- [ ] `output` values from `azapi_update_resource` are rendered in a dedicated **Output Values**
      section after the body section
- [ ] The Output Values section heading clearly distinguishes output from body (input) attributes
- [ ] When `after_unknown.output = true` during a create (no before output), the Output Values
      section is suppressed entirely (no heading or content rendered)
- [ ] When `after_unknown.output = true` during a replace with before output present, the before
      output renders in delete mode followed by a "known after apply" notice
- [ ] When output is absent in both before and after states, the Output Values section is omitted
      entirely
- [ ] Feature 034 attribute grouping and array rendering applies to output values, producing
      sub-section headings of the form `Output Values - \`<prefix>\`` when grouping fires
- [ ] Sensitivity masking is applied to output values, respecting `before_sensitive.output` /
      `after_sensitive.output`
- [ ] Large-value handling applies to output values
- [ ] All change actions (create, update, delete, replace) are handled correctly
- [ ] Rendering follows the project report style guide (data as code, labels as text)
- [ ] No information is lost — all output fields present in the plan are rendered
- [ ] All existing tests continue to pass
- [ ] New tests validate output rendering for create, update, delete, replace, sensitive, and
      grouping scenarios

## Open Questions

None. The analysis of the existing codebase confirms the implementation path is clear:
`RenderAzapiBody` already supports any JSON object and grouping is automatic. The Architect will
determine the exact template and C# changes needed.
