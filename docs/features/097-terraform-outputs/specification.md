# Feature 097 - Terraform Outputs Support

## Overview

Support rendering Terraform output changes from the `output_changes` section of the plan JSON in the generated markdown report.

## Background

When running `terraform show -json`, the plan JSON contains an `output_changes` section showing how terraform outputs will change. This feature adds support for parsing and rendering these outputs in the markdown report.

## Requirements

1. **Parse outputs from plan JSON**: Add `OutputChanges` to `TerraformPlan` as `IReadOnlyDictionary<string, OutputChange>`
2. **Create output model**: Create `OutputChangeModel` in `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/`
3. **Add outputs to module groups**: Add `Outputs` (IReadOnlyList<OutputChangeModel>) to `ModuleChangeGroup`
4. **Build output models**: In `ReportModelBuilder.Build.cs`, extract outputs from plan and add to root module group
5. **Create outputs template**: Create `_outputs.sbn` that renders a table with: `Name`, `Description`, `Sensitive`, `Value`
6. **Update default.sbn**: Render outputs at the end of the module section (after resources, before `---` separator)
7. **Sensitive value masking**: If `after_sensitive` is `true` OR if `before_sensitive` is `true`, mask the value unless `show_sensitive` is enabled

## Terraform Plan JSON Format

The `output_changes` section looks like this:
```json
"output_changes": {
  "pipeline_id": {
    "actions": ["update"],
    "before": "6",
    "after_unknown": true,
    "before_sensitive": false,
    "after_sensitive": false
  },
  "project_id": {
    "actions": ["no-op"],
    "before": "0f0b93a6-f450-49b2-ad52-fe3303c2f9aa",
    "after": "0f0b93a6-f450-49b2-ad52-fe3303c2f9aa",
    "after_unknown": false,
    "before_sensitive": false,
    "after_sensitive": false
  },
  "secret_output": {
    "actions": ["create"],
    "before": null,
    "after_sensitive": true,
    "after_unknown": false,
    "before_sensitive": false
  }
}
```

The description for each output is in `configuration.root_module.outputs`:
```json
"configuration": {
  "root_module": {
    "outputs": {
      "pipeline_id": {
        "expression": {...},
        "description": "The ID of the created build pipeline"
      }
    }
  }
}
```

## Implementation

### Data Model

- `OutputChange` record in `TerraformPlan.cs`
- `OutputChangeModel` class in `MarkdownGeneration/Models/`
- `Outputs` property on `ModuleChangeGroup`

### Template

Create `_outputs.sbn` to render a table with columns:
- Name
- Description
- Action (with icon)
- Sensitive (Yes/No with 🔒 icon)
- Value (masked if sensitive)

### Sensitive Value Handling

- When `after_sensitive` is true OR `before_sensitive` is true, show `***` instead of actual value
- When `show_sensitive` CLI option is enabled, show actual values
- When `after_unknown` is true, show `(known after apply)`

## Acceptance Criteria

- [ ] Outputs are parsed from plan JSON
- [ ] Outputs are rendered in markdown report
- [ ] Sensitive outputs are masked by default
- [ ] Sensitive outputs are shown when `--show-sensitive` is enabled
- [ ] Unknown values show "(known after apply)"
- [ ] Action icons match resource change icons
- [ ] All existing tests pass
- [ ] Snapshot tests updated if needed
