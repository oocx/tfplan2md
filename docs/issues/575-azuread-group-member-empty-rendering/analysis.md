# Issue: azuread_group_member Resources Render with Empty Attribute Table

## Problem Description

`azuread_group_member` resources (and any other resource where all attribute values are "known after apply") render with an empty attribute table in the tfplan2md markdown report. The summary line is shown but without any attribute details — effectively a "no content" resource block.

## Steps to Reproduce

Use a Terraform configuration where `azuread_group_member.group_object_id` and `member_object_id` are both derived from other resources (e.g., `each.value.group_object_id`), resulting in a plan where `after` is `null` and `after_unknown` contains the attribute names.

Example Terraform config:
```hcl
resource "azuread_group_member" "user_groups" {
  for_each         = { for value in local.user_groups_members : "${value.group_name} - ${value.user_name}" => value }
  group_object_id  = each.value.group_object_id
  member_object_id = each.value.user_object_id
}
```

The resulting Terraform plan JSON for each instance looks like:
```json
{
  "address": "module.azure.azuread_group_member.user_groups[\"team-example - user@example.de\"]",
  "type": "azuread_group_member",
  "name": "user_groups",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": null,
    "after_unknown": {
      "group_object_id": true,
      "id": true,
      "member_object_id": true
    },
    "before_sensitive": {},
    "after_sensitive": {}
  }
}
```

Running tfplan2md produces:
```
➕ azuread_group_member user_groups —

➕ azuread_group_member user_groups —
```

No attribute table is shown (no `group_object_id`, no `member_object_id`).

## Expected Behavior

Attributes marked as "known after apply" in `after_unknown` should be rendered with a `(known after apply)` value, matching Terraform's own `terraform show` output format:

```
➕ azuread_group_member user_groups

| Attribute         | Value                 |
| ----------------- | --------------------- |
| group_object_id   | (known after apply)   |
| id                | (known after apply)   |
| member_object_id  | (known after apply)   |
```

## Actual Behavior

The resource is rendered with an empty attribute section (template falls through to `_No attribute changes._`), because `change.After` is `null` and no attribute keys are found.

## Root Cause Analysis

### Affected Components

- **Primary file:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`, method `BuildAttributeChanges` (lines 92–133)
- **Supporting file:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/JsonFlattener.cs` — `ConvertToFlatDictionary`
- **Model:** `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs` — `Change.AfterUnknown` (line 63)

### What's Broken

The `BuildAttributeChanges` method (lines 94–99) computes the set of attribute keys to render from only two sources:

```csharp
var beforeDict = ConvertToFlatDictionary(change.Before);
var afterDict  = ConvertToFlatDictionary(change.After);
// ...
var allKeys = beforeDict.Keys.Union(afterDict.Keys).Order();
```

It does **not** consult `change.AfterUnknown`. The `AfterUnknown` field in the `Change` record is parsed from the plan JSON (it's declared in `TerraformPlan.cs` line 63) but is never used in `BuildAttributeChanges`.

### Why It Happened

For most resources, `change.After` contains known values (e.g., `name`, `location`, `sku`) and `after_unknown` only carries computed identifiers (e.g., `id`, `etag`). In those cases, the attribute table is populated from `change.After` and the unknown fields (like `id`) are simply absent from the table — an acceptable UX since resource IDs are usually not important to review.

However, for `azuread_group_member` (and any resource where all user-settable attributes are computed from references to other planned resources), the plan JSON has `"after": null` because **no** attribute values are known at plan time. Without any keys from `before` or `after`, `allKeys` is empty, and no attribute rows are generated.

### Related Scenario: Null JSON Values in `after`

There is a second related case (partially overlapping with the known-after-apply test fixture `azuread-group-members-known-after-apply-plan.json`): when Terraform emits `"after": {"group_object_id": null}` alongside `"after_unknown": {"group_object_id": true}`, the attribute IS in `allKeys`, but `afterValue = null`. Since `before` is also null (create action), `valuesEqual = true`, and the row is dropped by the unchanged-values filter. This attribute effectively disappears even though it will be set at apply time.

The fix should handle both sub-cases:
1. **`after` is null** — entire `after` object is null; all attributes come from `after_unknown`
2. **`after` has null values** — `after[attr] = null` AND `after_unknown[attr] = true` — attribute should be shown as `(known after apply)` rather than dropped

### Terraform Plan JSON Specification Reference

According to the [Terraform plan JSON format spec](https://developer.hashicorp.com/terraform/internals/json-format#change-representation):
- `after_unknown`: An object with `true` for each attribute whose **final value is not yet known** at plan time (i.e., depends on a resource not yet created or computed)
- A `true` value in `after_unknown[attr]` means the attribute WILL be set at apply time but the value is not available during planning

The `TerraformShowRenderer` in this codebase (used for the "terraform show"-style renderer) already correctly handles `after_unknown` — see `DiffRenderer.cs` lines 258–263 and `DiffRenderer.Utilities.cs` `EnumerateProperties`. The markdown table renderer simply has not been extended with the same logic.

## Suggested Fix Approach

In `BuildAttributeChanges` in `ReportModelBuilder.ResourceChanges.cs`:

1. **Add an `afterUnknownDict`** by calling `ConvertToFlatDictionary(change.AfterUnknown)` — this produces a flat dictionary where `true` appears as `"true"` for each unknown attribute name.

2. **Extend `allKeys`** to also include keys from `afterUnknownDict`:
   ```csharp
   var allKeys = beforeDict.Keys
       .Union(afterDict.Keys)
       .Union(afterUnknownDict.Keys)
       .Order();
   ```

3. **Override `afterDisplay`** for attributes marked unknown: if `afterUnknownDict[key] == "true"` (the attribute is in `after_unknown` and not sensitive), set `afterDisplay = "(known after apply)"`.

4. **Override the `valuesEqual` check**: if `afterDisplay` is `"(known after apply)"` and `beforeDisplay` is null (create action), treat this as a meaningful change (not "unchanged"), so the row is NOT dropped by `!_showUnchangedValues`.

The resulting pseudo-code:

```csharp
private List<AttributeChangeModel> BuildAttributeChanges(Change change, string providerName)
{
    var beforeDict = ConvertToFlatDictionary(change.Before);
    var afterDict  = ConvertToFlatDictionary(change.After);
    var afterUnknownDict = ConvertToFlatDictionary(change.AfterUnknown);
    var beforeSensitiveDict = ConvertToFlatDictionary(change.BeforeSensitive);
    var afterSensitiveDict  = ConvertToFlatDictionary(change.AfterSensitive);

    var allKeys = beforeDict.Keys
        .Union(afterDict.Keys)
        .Union(afterUnknownDict.Keys)
        .Order();

    var changes = new List<AttributeChangeModel>();

    foreach (var key in allKeys)
    {
        beforeDict.TryGetValue(key, out var beforeValue);
        afterDict.TryGetValue(key, out var afterValue);

        var isUnknown = afterUnknownDict.TryGetValue(key, out var unknownFlag)
            && string.Equals(unknownFlag, "true", StringComparison.OrdinalIgnoreCase);

        var isSensitive = IsSensitiveAttribute(key, beforeSensitiveDict, afterSensitiveDict);
        var beforeDisplay = isSensitive && !_showSensitive ? "(sensitive)" : beforeValue;
        var afterDisplay  = isUnknown
            ? "(known after apply)"
            : (isSensitive && !_showSensitive ? "(sensitive)" : afterValue);

        // Treat unknown (known after apply) values as changed
        var valuesEqual = !isUnknown && string.Equals(beforeValue, afterValue, StringComparison.Ordinal);

        if (!_showUnchangedValues && valuesEqual)
        {
            continue;
        }

        var isLarge = IsLargeValue(beforeDisplay, providerName)
            || IsLargeValue(afterDisplay, providerName);

        changes.Add(new AttributeChangeModel
        {
            Name = key,
            Before = beforeDisplay,
            After  = afterDisplay,
            IsSensitive = isSensitive,
            IsLarge = isLarge
        });
    }

    return changes;
}
```

**Note on `ConvertToFlatDictionary(change.AfterUnknown)`:** `AfterUnknown` in the JSON is an object where values are `true` (scalar bool) for unknown attributes or nested objects for unknown blocks. `JsonFlattener` already handles `JsonValueKind.True` → `"true"` (line in `FlattenJsonElement`). For object-typed unknowns (nested block), the value won't be `"true"` at the top key, but the nested paths will. This is sufficient for the flat-attribute use case.

**Caveat — `after_unknown` for nested blocks:** Nested blocks in `after_unknown` are represented as objects `{"block_attr": {"sub_attr": true}}` rather than scalar `true`. The flat-dictionary approach handles leaf paths correctly. For the common scalar attributes (`group_object_id`, `member_object_id`), this is not an issue.

## Related Tests

After the fix, the following tests should be added or updated:

### New Test Fixture
Create `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuread-group-member-all-unknown-plan.json`:
- A plan with an `azuread_group_member` resource where `"after": null` and `"after_unknown": {"group_object_id": true, "id": true, "member_object_id": true}`
- This directly reproduces the bug reported

### New Snapshot
Create `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azuread-group-member-all-unknown.md`:
- The snapshot should show a `group_object_id` and `member_object_id` row in the attribute table with `(known after apply)` as the value

### New Unit Tests
Add to `AzureAdSnapshotTests.cs` (or a new focused test class):
- `Snapshot_AzureAd_GroupMember_AllAttributesUnknown_MatchesBaseline` — covers `after: null` case
- Update `ReportModelBuilderTests` (or create `ReportModelBuilder_AfterUnknownTests`) with unit tests for:
  - `BuildAttributeChanges` with `after: null` and populated `after_unknown`
  - `BuildAttributeChanges` with `after: {}` (empty) and populated `after_unknown`
  - `BuildAttributeChanges` with mixed `after` (some known, some null) and corresponding `after_unknown` entries

### Existing Snapshot to Verify
The existing snapshot `azuread-group-members-known-after-apply.md` may change if the fix also handles the `after.group_object_id = null + after_unknown.group_object_id = true` sub-case. The Developer should verify whether this snapshot needs updating.

## Additional Context

- The `TerraformShowRenderer` (used for the `--format diff` option) **already handles** `after_unknown` correctly in `DiffRenderer.cs` — attributes with `after_unknown: true` are rendered as `(known after apply)`. The markdown table renderer is the only path missing this logic.
- The Outputs renderer (`ReportModelBuilder.Outputs.cs` line 47) also handles `AfterUnknown` as a boolean flag for output values. The pattern for resource attributes is structurally similar but uses an object (per-attribute map) rather than a single boolean.
- The `azuread_group_member` provider implementation marks `group_object_id` and `member_object_id` as `computed + required`, which is why they appear in `after_unknown` even though the user explicitly sets them — their final values depend on provider-computed GUIDs resolved at apply time.
