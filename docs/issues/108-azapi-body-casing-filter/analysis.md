# Issue: azapi Body Changes Show Casing-Only Azure Resource ID Differences

## Problem Description

When an `azapi_update_resource` or `azapi_resource` has a Terraform plan change where the only
difference between a "before" and "after" body property value is letter casing in an Azure
resource ID, tfplan2md displays that property as a real change. This is misleading because
Azure ARM API occasionally returns resource IDs with different capitalisation on successive
reads (a known platform quirk), so casing differences in resource IDs carry no infrastructure
significance.

**Example output (incorrect):**
```
Body Changes
Property      Before                                                                                 After
diskAccessId  DiskAccesses 🆔 app-gwc in resource group 📁 APP-RG-GWC of subscription 🔑 <id>    DiskAccesses 🆔 app-gwc in resource group 📁 app-rg-gwc of subscription 🔑 <id>
```

Only the resource group segment differs in casing (`APP-RG-GWC` vs `app-rg-gwc`); the actual
resource has not changed.

Resource type: `azapi_update_resource` (provider: `registry.terraform.io/azure/azapi`)

## Steps to Reproduce

1. Create a Terraform plan JSON for an `azapi_update_resource` resource where a body property
   (e.g. `properties.diskAccessId`) contains an Azure resource ID.
2. The plan's `before` value contains the ID with an uppercase resource group segment
   (e.g. `.../resourceGroups/APP-RG-GWC/...`).
3. The plan's `after` value contains the same ID with a lowercase resource group segment
   (e.g. `.../resourceGroups/app-rg-gwc/...`).
4. Run `tfplan2md --ignore-azure-id-case-changes plan.json`.
5. Observe the "Body Changes" section: the property is shown as changed even though the
   only difference is casing.

## Expected Behavior

When `--ignore-azure-id-case-changes` is active (which is the **default**), a body property
where before and after values are Azure resource IDs that differ only in letter casing should
be suppressed — it should not appear in the "Body Changes" table.

## Actual Behavior

The property is shown as a change in the "Body Changes" table because the comparison is
case-sensitive. The user sees a visually misleading diff.

## Root Cause Analysis

### Affected Components

| File | Location | Role |
|------|----------|------|
| `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Data.cs` | `ValuesEqual()` | Determines whether two body property values are equal |
| `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Data.cs` | `CompareJsonProperties()` | Calls `ValuesEqual`; decides which properties are "changed" |
| `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs` | `RenderUpdateBody()` | Calls `CompareJsonProperties`; does not pass `ignoreAzureIdCaseChanges` |
| `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.cs` | `RenderAzapiBody()` | Entry-point from Scriban template; does not accept `ignoreAzureIdCaseChanges` |
| `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/update_resource.sbn` | `render_azapi_body` call | Does not pass `ignore_azure_id_case_changes` global |
| `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn` | `render_azapi_body` call | Does not pass `ignore_azure_id_case_changes` global |

### What's Broken

The codebase has **two separate rendering pipelines** for changes:

#### Pipeline 1 — Core Attribute Changes (NOT the primary bug here)

`ReportModelBuilder.ResourceChanges.cs` → `BuildAttributeChanges()` uses an
`AttributeChangeFilterRegistry` to suppress casing-only Azure ID rows. For `azurerm` resources,
`AzureResourceIdCaseChangeFilter` handles this correctly (feature 103). However:

- `AzApiModule` does not override `RegisterAttributeChangeFilters`, so **no filter is registered
  for the `azapi` provider** in Pipeline 1.
- This means the flattened attributes of the `azapi_update_resource` resource (e.g.
  `body.properties.diskAccessId`) are NOT filtered.
- However, since neither `update_resource.sbn` nor `resource.sbn` render
  `change.attribute_changes`, **Pipeline 1 output is not visible in the final report**.
  It does, however, inflate `AttributeChanges.Count`, which prevents the resource from being
  suppressed in `ReportModelBuilder.Build.cs`'s `displayChanges` filter (line ~66), even
  when all body changes are casing-only.

#### Pipeline 2 — AzAPI Body Rendering (PRIMARY BUG)

`AzApi.Rendering.cs` → `RenderAzapiBody()` → `RenderUpdateBody()` → `CompareJsonProperties()`.

`CompareJsonProperties()` (in `AzApi.Data.cs`) compares each flattened property path in the
body JSON using `ValuesEqual()`. The `ValuesEqual()` implementation is **case-sensitive**:

```csharp
// AzApi.Data.cs — ValuesEqual (simplified)
private static bool ValuesEqual(object? before, object? after)
{
    if (before is null && after is null) return true;
    if (before is null || after is null) return false;
    if (IsNumeric(before) && IsNumeric(after)) return Convert.ToDouble(before) == Convert.ToDouble(after);
    return before.Equals(after);  // ← string.Equals with OrdinalIgnoreCase? No — uses object.Equals which is case-sensitive for strings
}
```

`object.Equals` for strings performs an **ordinal case-sensitive** comparison, so
`"/subscriptions/X/resourceGroups/APP-RG-GWC/..."` does not equal
`"/subscriptions/X/resourceGroups/app-rg-gwc/..."`, and the property is incorrectly included
as a change.

The `--ignore-azure-id-case-changes` flag (stored as `ignore_azure_id_case_changes` in the
Scriban template context, see `AotScriptObjectMapper.cs` line 41) is **not threaded through** to
`RenderAzapiBody` or `CompareJsonProperties`, so the flag has no effect on body rendering.

### Why It Happened

Feature 103 (`docs/features/103-azure-id-case-insensitive-filter/`) explicitly scoped the
implementation to the `azurerm` provider and listed `azapi` as "out of scope" (future work).
The `CompareJsonProperties` helper was written independently for the azapi body rendering
pipeline and has no integration with the `IAttributeChangeFilter` / `AttributeChangeFilterRegistry`
extension point — it does not know about the `ignore_azure_id_case_changes` flag at all.

## Suggested Fix Approach

The fix requires changes to both pipelines, but Pipeline 2 is the user-visible part.

### Fix 1 — Pipeline 2 (Primary): Thread `ignoreAzureIdCaseChanges` into `CompareJsonProperties`

**Step 1: Extend `ValuesEqual` in `AzApi.Data.cs`**

Add a `bool ignoreAzureIdCaseChanges` parameter. When `true`, strings that compare equal
under `OrdinalIgnoreCase` **and** where at least one is an Azure resource ID
(`AzureScopeParser.IsAzureResourceId`) are treated as equal:

```csharp
private static bool ValuesEqual(object? before, object? after, bool ignoreAzureIdCaseChanges = false)
{
    if (before is null && after is null) return true;
    if (before is null || after is null) return false;
    if (IsNumeric(before) && IsNumeric(after)) return Convert.ToDouble(before) == Convert.ToDouble(after);

    // Azure ID casing-only comparison
    if (ignoreAzureIdCaseChanges
        && before is string beforeStr
        && after is string afterStr
        && string.Equals(beforeStr, afterStr, StringComparison.OrdinalIgnoreCase)
        && (AzureScopeParser.IsAzureResourceId(beforeStr) || AzureScopeParser.IsAzureResourceId(afterStr)))
    {
        return true;
    }

    return before.Equals(after);
}
```

**Step 2: Thread through `CompareJsonProperties` in `AzApi.Data.cs`**

Add `bool ignoreAzureIdCaseChanges = false` parameter to `CompareJsonProperties` and pass
it to `ValuesEqual`.

**Step 3: Extend `UpdateBodyRenderInput` in `AzApi.Rendering.Update.cs`**

Add `bool IgnoreAzureIdCaseChanges` to the sealed record and pass it in both
`CompareJsonProperties` calls inside `RenderUpdateBody`.

**Step 4: Extend `RenderAzapiBody` in `AzApi.Rendering.cs`**

Add `bool ignoreAzureIdCaseChanges = false` parameter (at end of signature to stay
backward-compatible). Pass it through the `UpdateBodyRenderInput` constructor.

**Step 5: Update templates**

In `update_resource.sbn` and `resource.sbn`, pass `ignore_azure_id_case_changes` as the
new last argument to `render_azapi_body`:

```scriban
{{ render_azapi_body change.after_json.body "Body Changes" "update" change.before_json.body before_sensitive_body after_sensitive_body false "inline-diff" show_sensitive ignore_azure_id_case_changes }}
```

### Fix 2 — Pipeline 1 (Secondary): Register azapi attribute change filter

Add `RegisterAttributeChangeFilters` to `AzApiModule` to suppress flattened
`body.*` attribute changes that are casing-only Azure IDs. This prevents those entries from
inflating `AttributeChanges.Count` and ensures that a resource where all changes are
casing-only body properties is correctly excluded from the report:

```csharp
// In AzApiModule.cs
public void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry)
{
    registry.Register(new AzApiBodyResourceIdCaseChangeFilter());
}
```

Create `AzApiBodyResourceIdCaseChangeFilter` (or extend the existing
`AzureResourceIdCaseChangeFilter` to also match `azapi` provider) analogous to the existing
`AzureResourceIdCaseChangeFilter`.

Alternatively, the existing `AzureResourceIdCaseChangeFilter` regex could be widened to also
match `azapi`:

```csharp
// Old:
private static readonly Regex AzureRmProviderPattern =
    new(@"(^azurerm$|.*/azurerm$)", ...);

// Possible: a shared Azure-provider pattern
private static readonly Regex AzureProviderPattern =
    new(@"(^azurerm$|.*/azurerm$|^azapi$|.*/azapi$)", ...);
```

However, this would change an existing class that is well-tested for `azurerm`-only behaviour.
The cleaner approach is a separate `AzApiAttributeChangeFilter` registered by `AzApiModule`.

### Fix 3 — Update `Build.cs` (only if Fix 1 + 2 are both implemented)

Once Pipeline 2 correctly filters body casing changes and Pipeline 1 no longer inflates
`AttributeChanges.Count` with casing-only body attributes, the `Build.cs` resource-level
filter will automatically work correctly for `azapi_update_resource` resources.

## Related Tests

After the fix, the following tests should pass:

### New unit tests to add (Pipeline 2)

- [ ] `CompareJsonProperties_IgnoreAzureIdCaseChanges_AzureIdDiffersOnlyCasing_ReturnsNoChange`
  — `ValuesEqual` / `CompareJsonProperties` with a body property whose before/after values
  are Azure resource IDs differing only in casing, `ignoreAzureIdCaseChanges: true` → not included as change.
- [ ] `CompareJsonProperties_IgnoreAzureIdCaseChanges_FlagFalse_CasingChangeStillShown`
  — Same scenario with flag `false` → still included as change.
- [ ] `RenderAzapiBody_UpdateMode_AzureIdCasingOnlyChange_FlagTrue_NoChangesMessage`
  — `RenderAzapiBody` with `ignoreAzureIdCaseChanges: true` and a body where only an Azure
  resource ID casing differs → renders "No body changes detected".
- [ ] `RenderAzapiBody_UpdateMode_AzureIdCasingOnlyChange_FlagFalse_ShowsChange`
  — Same with flag `false` → still renders the change row.

### Existing tests to verify remain passing

- [ ] `AzureResourceIdCaseChangeFilterTests` — All 9 test cases; TC-19b (azapi NOT suppressed
  by the existing azurerm filter) should still pass.
- [ ] `ReportModelBuilderIgnoreAzureIdCaseChangesTests` — All existing tests for azurerm.
- [ ] `ScribanHelpersAzApiUpdateRenderingTests` — All existing tests.
- [ ] `AzapiUpdateResourceTemplateTests` — All snapshot tests.

## Edge Cases to Consider

| Edge Case | Notes |
|-----------|-------|
| Non-Azure-ID string casing (e.g. `MyApp` vs `myapp`) | Must NOT be suppressed. Only strings where `AzureScopeParser.IsAzureResourceId` returns `true` should be filtered. |
| Genuine content difference (different subscription or resource) | Must NOT be suppressed. `OrdinalIgnoreCase` equality check ensures only true casing-only differences are suppressed. |
| Null before or after value | `ValuesEqual` already returns `false` immediately when either value is null — no change needed. |
| Non-string body property (number, boolean) | `ignoreAzureIdCaseChanges` guard only activates when both values are strings, so numeric/boolean properties are unaffected. |
| `showUnchanged: true` flag interaction | `CompareJsonProperties` is called twice — once with `showUnchanged: true` (allComparisons) and once with `showUnchanged: input.ShowUnchanged`. Both calls should pass `ignoreAzureIdCaseChanges` so that casing-only properties are consistently treated as unchanged across both invocations. |
| `resource.sbn` (azapi_resource, create/update/delete modes) | Only the "update" code path in `RenderAzapiBody` calls `CompareJsonProperties`; create and delete modes do not compare before/after, so the flag has no effect there. The parameter change is still needed to keep the function signature consistent. |
| `--no-ignore-azure-id-case-changes` flag | The flag is opt-out of filtering (default is on). When user explicitly passes `--no-ignore-azure-id-case-changes`, casing differences should be shown — verified by the `FlagFalse` test cases above. |

## Additional Context

- Related feature: `docs/features/103-azure-id-case-insensitive-filter/specification.md` —
  The original feature explicitly excluded `azapi` provider from scope.
- `AzureScopeParser.IsAzureResourceId` (in `src/Oocx.TfPlan2Md/Platforms/Azure/AzureScopeParser.cs`)
  is the correct helper to detect Azure resource IDs (handles subscriptions, resource groups,
  full resource paths, and management group paths).
- `ignore_azure_id_case_changes` is already mapped to the Scriban template context in
  `AotScriptObjectMapper.cs` line 41 (`scriptObject["ignore_azure_id_case_changes"] = model.IgnoreAzureIdCaseChanges`),
  so template access is already available.
- The `CliParser.cs` default for `ignoreAzureIdCaseChanges` is `true` (line 141) — so the fix
  will suppress casing noise by default, matching the existing `azurerm` behaviour.
