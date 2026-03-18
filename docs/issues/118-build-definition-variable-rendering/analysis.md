# Issue: Build Definition Variable Rendering — Sensitive Attributes and Missing Tabular Format

## Problem Description

When an `azuredevops_build_definition` resource has a variable with `is_secret = true`, **all**
variable attributes (including `name`, `allow_override`, and `is_secret`) are rendered as
`(sensitive)` in the generated markdown report. Only the variable value (`value` /
`secret_value`) should be masked.

Additionally, `azuredevops_build_definition` variables are not rendered in a tabular format like
`azuredevops_variable_group` variables are — they fall back to a generic flat attribute table.

## Steps to Reproduce

1. Create a Terraform plan where `azuredevops_build_definition` has at least one variable with `is_secret = true`.
2. Run `tfplan2md` to generate a markdown report.
3. Observe the variable section — all attributes of the secret variable appear as `(sensitive)`.

**Example broken output (current):**

```
variable[0].allow_override	(sensitive)	(sensitive)
variable[0].is_secret	    (sensitive)	(sensitive)
variable[0].name	        (sensitive)	(sensitive)
variable[0].value	        (sensitive)	(sensitive)
```

**Expected output:**

| Name | Value | Secret | Allow Override |
| ---- | ----- | ------ | -------------- |
| `API_KEY` | `(sensitive / hidden)` | `✅ true` | `✅ true` |

## Expected Behavior

- Only the `value` / `secret_value` field should be shown as `(sensitive / hidden)` for secret variables.
- The `name`, `is_secret`, and `allow_override` attributes are **not sensitive** and should show their actual values.
- Variables should be rendered in a structured table (one row per variable, columns per attribute), matching the rendering style of `azuredevops_variable_group`.

## Actual Behavior

All attributes of a secret variable (`name`, `is_secret`, `allow_override`, `value`) are shown as
`(sensitive)` because the `DefaultResourceRenderer` is used and it checks sensitivity
hierarchically — when Terraform marks `variable[0]` as sensitive in `after_sensitive`, the entire
object's attributes inherit the sensitivity flag.

## Root Cause Analysis

### Affected Components

- **Primary**: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs#L178`
  — registers `AzureDevOpsDelegatingRenderer("azuredevops_build_definition")` which delegates to
  `DefaultResourceRenderer`.
- **Rendering pipeline**: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs`
  — `IsSensitiveAttribute` checks hierarchically up the attribute path tree.
- **Unused infrastructure (exists but not connected)**:
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionViewModelFactory.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionFormatters.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionChangeBuilders.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionExtractors.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionViewModel.cs`

### What's Broken

**Part 1 — Sensitive attribute bleeding**

Terraform's azuredevops provider marks the **entire `variable[N]` object** as sensitive in
`after_sensitive` / `before_sensitive` when any attribute within it is a secret (e.g., `secret_value`).
The plan JSON contains something like:

```json
"after_sensitive": {
  "variable": [
    { "secret_value": true, "value": true }
  ]
}
```

The `SensitivityHelper.IsSensitiveAttribute` method checks hierarchically: for
`variable[0].name`, it checks `variable[0].name`, then `variable[0]`, then `variable`. If
`variable[0]` is marked `true` (the whole block), then ALL attributes of that variable are
treated as sensitive by the `DefaultResourceRenderer`.

**Part 2 — Missing dedicated renderer**

The entire rendering infrastructure for `azuredevops_build_definition` (view model factory,
formatters, change builders, extractors) was implemented in feature `094-build-definition-tables`
but **a dedicated `BuildDefinitionRenderer` class was never created**. The provider module still
registers `AzureDevOpsDelegatingRenderer` (generic fallback).

The `BuildDefinitionViewModelFactory` correctly extracts variable metadata from the raw
`before`/`after` JSON directly — bypassing the sensitive metadata entirely — and explicitly
only masks the `Value` field for secret variables:

```csharp
// BuildDefinitionFormatters.cs FormatVariableValue()
if (variable.IsSecret) {
    return "`(sensitive / hidden)`";
}
```

### Why It Happened

Feature `094-build-definition-tables` implemented all the underlying data model and formatting
logic for build definition rendering but stopped short of connecting it to a renderer. The
`AzureDevOpsDelegatingRenderer` was left as a placeholder. As a result, the carefully-written
sensitive-value handling in `BuildDefinitionFormatters` is never exercised.

## Suggested Fix Approach

### Step 1 — Create `BuildDefinitionRenderer`

Add a new `BuildDefinitionRenderer` class to
`src/Oocx.TfPlan2Md/Providers/AzureDevOps/Renderers/AzureDevOpsResourceRenderers.cs`,
modelled exactly after the existing `VariableGroupRenderer`:

- Use `BuildDefinitionViewModelFactory.Build(change.ResourceChange, ...)` to get the view model.
- Render header metadata (name, path, agent pool name, queue status).
- Render variables:
  - For create/delete: `AfterVariables` / `BeforeVariables` — table with columns `Name | Value | Secret | Allow Override`.
  - For update/replace: `VariableChanges` — table with columns `Change | Name | Value | Secret | Allow Override`.
- Render CI triggers, PR triggers, schedules, repositories, jobs as supplementary tables.
- Use large-value collapsible sections for variables with `IsLargeValue = true`.

The renderer reads directly from `before`/`after` JSON via the extractor, so it is completely
insulated from the hierarchical sensitivity check used by the default renderer.

### Step 2 — Register the New Renderer

In `AzureDevOpsModule.cs`, change:

```csharp
// BEFORE
registry.Register(new AzureDevOpsDelegatingRenderer("azuredevops_build_definition"));

// AFTER
registry.Register(new BuildDefinitionRenderer(_azdoRepositoryMapper));
```

### Step 3 — Add/Update Test Coverage

1. **Add a snapshot test** for `azuredevops_build_definition` (new or extend existing
   `AzureDevOpsSnapshotTests.cs`). Use the existing
   `TestData/azuredevops-build-definitions.json` as input.
2. **Add a test scenario with `after_sensitive`** markers to confirm that even when Terraform
   marks variable blocks as sensitive, the renderer shows `name`/`is_secret`/`allow_override`
   with their actual values.
3. **Update `ProviderResourceRenderersTests.cs`** to reference `BuildDefinitionRenderer` instead
   of `AzureDevOpsDelegatingRenderer("azuredevops_build_definition")`.

### Step 4 — Verify with Command

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx \
  --filter "FullyQualifiedName~AzureDevOps" --verbosity normal
```

## Related Tests

Tests that should pass after the fix:

- [ ] `AzureDevOpsSnapshotTests.Snapshot_AzureDevOps_BuildDefinitions_MatchesBaseline` (new)
- [ ] `ProviderResourceRenderersTests.ProviderRenderers_ExposeExpectedResourceTypes` (update reference)
- [ ] `BuildDefinitionViewModelFactoryTests.*` (all existing — no change required, already passing)
- [ ] All existing `AzureDevOpsSnapshotTests.*` (must not regress)

## Additional Context

### Reference Implementation

`azuredevops_variable_group` uses `VariableGroupRenderer` in
`AzureDevOpsResourceRenderers.cs` and `VariableGroupViewModelFactory`. This is the exact
pattern to replicate for `azuredevops_build_definition`.

See the live rendering reference at: https://oocx.github.io/tfplan2md/features/azdo-variable-groups.html

### Existing View Model Columns

The `BuildDefinitionVariableRowViewModel` exposes:
- `Name` — formatted variable name
- `Value` — masked as `(sensitive / hidden)` for secrets, actual value otherwise
- `IsSecret` — boolean icon (`✅ true` / `❌ false`)
- `AllowOverride` — boolean icon (`✅ true` / `❌ false`)
- `IsLargeValue` — flag for collapsible section

The `BuildDefinitionVariableChangeRowViewModel` adds:
- `Change` — change label (added/modified/removed/unchanged)
- `ChangeIcon` — rendered icon (➕/🔄/❌/—)

### Feature History

- Feature `094-build-definition-tables`: Implemented `BuildDefinitionViewModelFactory` and all
  supporting infrastructure but left `AzureDevOpsDelegatingRenderer` registered (placeholder).
  Work Protocol: `docs/features/094-build-definition-tables/work-protocol.md`.

### Related Issues

- `docs/issues/093-sensitive-attribute-disclosure/analysis.md` — the hierarchical sensitivity
  check was added to prevent the inverse problem (secrets leaking in other providers).
- `docs/issues/098-sensitive-info-exposure/analysis.md` — further sensitive value hardening.
