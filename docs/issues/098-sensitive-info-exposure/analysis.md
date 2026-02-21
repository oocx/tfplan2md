# Issue: Sensitive Information Exposure in Rendering Paths

## Problem Description

Several rendering paths can disclose sensitive values (secrets) in generated Markdown output.

This primarily affects:
- AzApi body rendering (create/delete and update)
- Any Scriban template that reads `before_json` / `after_json` directly (because sensitivity metadata is not available in the template context)
- Azure DevOps Variable Group diffs when `is_secret` changes
- Attribute-level masking when Terraform marks the *entire* object as sensitive (`before_sensitive: true` / `after_sensitive: true`)
- Attribute-level masking for top-level array keys without `.` (e.g., `secrets[0]`)

Reference: the aggregated findings in `docs/issues/097-security-analysis/results.md`.

## Steps to Reproduce

### AzApi body secrets disclosed (create/delete)
1. Use any plan containing an `azapi_resource` whose `change.after.body` (create/replace) or `change.before.body` (delete) contains sensitive fields (e.g., password, client secret).
2. Run tfplan2md against the plan JSON.
3. Observe that the rendered “Body” table contains plaintext values.

### AzApi body secrets disclosed (update)
1. Use an `azapi_resource` update plan where Terraform emits `before_sensitive` / `after_sensitive` metadata for `body` fields.
2. Run tfplan2md.
3. Observe that the update table renders the `before` / `after` values in plaintext.

### Azure DevOps Variable Group secret disclosure on `is_secret` transition
1. Use a plan containing a Variable Group variable where `before.is_secret != after.is_secret`.
2. Render the diff.
3. Observe that the diff can show a plaintext value from the “secret” side of the transition.

### Root-level sensitivity bypass (`before_sensitive: true` / `after_sensitive: true`)
1. Use any resource whose Terraform plan emits `before_sensitive: true` or `after_sensitive: true` (boolean root, not object).
2. Render the plan.
3. Observe that individual attributes are not masked despite the resource being sensitive as a whole.

### Top-level array parent sensitivity bypass
1. Use a resource with a top-level array attribute (flattened keys like `secrets[0]`, `secrets[1]`).
2. Have Terraform mark the *parent* array attribute as sensitive (`secrets: true`).
3. Render the plan.
4. Observe that `secrets[0]` is not masked because the parent `secrets` is never checked.

## Expected Behavior

- When Terraform marks a value or container as sensitive, tfplan2md should not output plaintext values unless an explicit “show sensitive” option is enabled.
- Templates must have access to sensitivity metadata if they are allowed to read raw `before_json` / `after_json` state.
- Secret masking must be consistent across Azure DevOps resources (Variable Groups should behave like Build Definitions).

## Actual Behavior

- AzApi create/delete body rendering flattens and renders all body values without sensitivity checks.
- AzApi update rendering computes `is_sensitive` but does not use it to mask values.
- AzApi templates attempt to use `change.before_sensitive` / `change.after_sensitive`, but these are not mapped into the Scriban context, so the template sees `null`.
- Variable Group diffs mask only when `after.is_secret` is true; transitions can leak values.
- Attribute masking can be bypassed when root-level `before_sensitive`/`after_sensitive` is boolean `true`.
- Attribute masking can be bypassed when the key has array-index notation but no dot separator (e.g., `secrets[0]`).

## Root Cause Analysis

### Affected Components (confirmed)

#### AzApi rendering
- `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn`
  - Create/replace calls `render_azapi_body ... null null null ...` for sensitivity arguments.
  - Update path reads `change.before_sensitive` / `change.after_sensitive`, which are missing in the template context.
- `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.cs`
  - For non-update modes, calls `RenderCreateDeleteBody(...)` without any sensitivity metadata.
- `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.CreateDelete.cs`
  - `RenderCreateDeleteBody(...)` flattens JSON and renders all values into markdown tables; no sensitivity checks.
- `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Data.cs`
  - `CompareJsonProperties(...)` sets `is_sensitive` but the `showSensitive` parameter is unused.
- `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs`
  - Rendering uses `FormatAttributeValueTable(path, before?.ToString(), ...)` and `after?.ToString()` with no sensitivity masking.

#### Scriban template context mapping (architectural)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs`
  - Maps `before_json` / `after_json` into the template context.
  - Does not map `before_sensitive` / `after_sensitive`.
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`
  - Contains `BeforeJson` / `AfterJson`, but no `BeforeSensitive` / `AfterSensitive` properties.
- `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`
  - Sensitive metadata exists at parse layer (`Change.BeforeSensitive` / `Change.AfterSensitive`).

#### Azure DevOps Variable Group secret masking
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupFormatters.cs`
  - `CreateDiffRow(...)` masks only when `after.IsSecret` is true.
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionFormatters.cs`
  - `CreateDiffRow(...)` correctly masks when `(before.IsSecret || after.IsSecret)`.

#### Attribute-level masking (hierarchical sensitivity)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/JsonFlattener.cs`
  - Root boolean sensitivity (`true`) becomes a flat dictionary entry with key `""`.
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`
  - `IsSensitiveAttribute(...)` iterates `GetHierarchicalPaths(key)` but never checks the empty-string root key.
  - `GetHierarchicalPaths(key)` does not emit parent array base-name when there is no `.` (e.g., `secrets[0]` only yields itself).

### What’s Broken

1. **AzApi create/delete path ignores sensitivity entirely.** The helper flattens and renders every value, and the template passes `null` for sensitivity.
2. **AzApi update path computes sensitivity but never masks.** `CompareJsonProperties` marks `is_sensitive`, but rendering never checks it and `showSensitive` is unused.
3. **Sensitivity metadata is not available to Scriban templates.** The AOT mapper exposes `before_json` / `after_json` but not `before_sensitive` / `after_sensitive`, so templates have raw state without the sensitivity map.
4. **Variable Group diffs only check `after.IsSecret`.** Secret transitions can leak the before or after value.
5. **Root boolean sensitivity (`"" -> true`) is not handled.** The hierarchical sensitivity check never inspects the empty-string key.
6. **Top-level array parent sensitivity is not handled.** For keys like `secrets[0]`, the function never checks the base `secrets` path.

### Why It Happened

- The codebase has two “rendering models”:
  - Attribute-level rendering (uses flattened dictionaries and hierarchical sensitivity checks)
  - Template-level rendering (passes raw JSON into Scriban)

The attribute-level path has sensitivity support but currently misses some sensitivity encodings (root boolean, some array patterns). The template-level path currently lacks sensitivity metadata entirely, so any template reading raw JSON is at risk.

## Suggested Fix Approach (High Level)

### 1) Propagate sensitivity into template context
- Extend `ResourceChangeModel` to include `BeforeSensitive` / `AfterSensitive` (plumbed from `Parsing.ResourceChange.Change.BeforeSensitive/AfterSensitive`).
- Update `AotScriptObjectMapper.MapResourceChange(...)` to map:
  - `before_sensitive` from `ResourceChangeModel.BeforeSensitive`
  - `after_sensitive` from `ResourceChangeModel.AfterSensitive`
  - Use the same `ConvertToScriptObject` mapping as `before_json` / `after_json`.

This enables templates like AzApi to safely check sensitivity while continuing to use semantic JSON rendering.

### 2) Fix AzApi body rendering to respect sensitivity
- Create/delete/replace:
  - Thread sensitivity into `RenderCreateDeleteBody(...)` and mask per flattened path.
  - Ensure body sections never print plaintext for sensitive paths unless the CLI show-sensitive option is enabled.
- Update:
  - Either mask within C# render helpers using the `is_sensitive` field (preferable), or ensure the template uses `is_sensitive` to render `"(sensitive)"`.
  - Remove or implement the currently-unused `showSensitive` parameter in `CompareJsonProperties`.

### 3) Fix Variable Group secret transition masking
- Change `VariableGroupFormatters.CreateDiffRow(...)` to mask when `(before.IsSecret || after.IsSecret)` (parity with Build Definitions).

### 4) Fix hierarchical sensitivity detection edge cases
- In `IsSensitiveAttribute(...)`, add an explicit root check:
  - If `beforeSensitive[""] == "true"` or `afterSensitive[""] == "true"`, treat all attributes as sensitive.
- In `GetHierarchicalPaths(key)`:
  - If `key` contains `[` and has no `.`, also yield the base name (e.g., `secrets` for `secrets[0]`).
  - Consider improving array stripping for nested parent paths (current logic uses the first `[`, which collapses `a[0].b[1]` to `a`).

### 5) Optional hardening (non-exposure but relevant)
- Remove `BuildDefinitionVariableValues.SecretValue` (or ensure it is never rendered) to reduce future risk.

## Related Tests / Verification

These issues should be verified with tests/snapshots that include sensitive data:
- AzApi fixture(s) that include sensitive body values (create/delete/update), ensuring output contains `(sensitive)` instead of plaintext.
- Azure DevOps variable group change fixture where `is_secret` toggles in both directions.
- Unit tests for `GetHierarchicalPaths` + `IsSensitiveAttribute` covering:
  - `after_sensitive: true` root
  - `secrets[0]` with parent `secrets: true`
  - Nested array/dot combinations

## Additional Context

- The AzApi template already expects `change.before_sensitive` / `change.after_sensitive`, but those values are not currently present in the Scriban context.
- The attribute-level sensitivity logic references a prior issue: `docs/issues/093-sensitive-attribute-disclosure/analysis.md`.
