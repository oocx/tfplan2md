# Issue: azuread_group_member Resources Render with Empty Summary and No Details Table

## Problem Description

`azuread_group_member` resources with all-unknown attribute values at plan time render with:
1. An **empty summary line** (just the em dash `—` with nothing after it)
2. **No attributes table** (shows `_No attribute changes._` instead of the group and member IDs)

This affects resources where `group_object_id` and `member_object_id` are both computed
from other resources (e.g., group IDs not yet known at plan time).

### Observed Output

```
➕ azuread_group_member user_groups —

➕ azuread_group_member user_groups —
```

### Expected Output

Something like:
```
➕ azuread_group_member user_groups — (known after apply) → (known after apply)
```

With an attributes table showing:

| Attribute | Value |
| ----------- | ------- |
| group_object_id | `(known after apply)` |
| member_object_id | `(known after apply)` |

## Steps to Reproduce

1. Have a Terraform plan where `azuread_group_member` uses `for_each` with values that reference
   computed outputs (e.g., `group_object_id = each.value.group_object_id` where the group IDs
   come from a data source or other computed attribute):

   ```hcl
   resource "azuread_group_member" "user_groups" {
     for_each         = { for v in local.user_groups_members : "${v.group_name} - ${v.user_name}" => v }
     group_object_id  = each.value.group_object_id
     member_object_id = each.value.user_object_id
   }
   ```

2. Generate the plan JSON: `terraform plan -out=tfplan && terraform show -json tfplan > plan.json`

3. Run: `tfplan2md plan.json`

4. Observe that each `azuread_group_member` entry shows an empty summary and no attribute table.

## Expected Behavior

Each `azuread_group_member` entry should show:
- A summary that indicates values are computed, e.g.
  `— (known after apply) → (known after apply)` or the Terraform address as fallback
- An attributes table that includes `group_object_id` and `member_object_id` rows showing
  `(known after apply)` as the value

## Actual Behavior

- Summary is: `➕ azuread_group_member <b><code>user_groups</code></b> — ` (empty detail text)
- Attributes table shows: `_No attribute changes._`

## Root Cause Analysis

### Affected Components

| Component | File | Lines | Issue |
|-----------|------|-------|-------|
| `BuildGroupMemberSummaryHtml` | `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs` | ~157–190 | Empty summary when `groupId` is empty string |
| `BuildAttributeChanges` | `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` | ~92–133 | Skips attributes where `before=null` and `after=null` |

### What's Broken

#### Bug 1: Empty Summary (AzureAdSummaryBuilder.Groups.cs)

The Terraform JSON plan for the bug scenario looks like:

```json
{
  "actions": ["create"],
  "before": null,
  "after": {
    "group_object_id": null,
    "member_object_id": null,
    "id": null
  },
  "after_unknown": {
    "group_object_id": true,
    "member_object_id": true,
    "id": true
  },
  "before_sensitive": {},
  "after_sensitive": {}
}
```

In `BuildGroupMemberSummaryHtml`, the state is extracted from `after`:

```csharp
var groupId = JsonStateReader.GetStringProperty(state, "group_object_id") ?? string.Empty;
var memberId = JsonStateReader.GetStringProperty(state, "member_object_id") ?? string.Empty;
```

`JsonStateReader.GetStringProperty` for a `JsonValueKind.Null` JSON property falls through
to `_ => property.ToString()`, which returns `""` (empty string) for a null JSON element.
So `groupId = ""` and `memberId = ""`.

Then:
```csharp
var groupName = principalMapper.GetName(groupId, GroupMemberType, model.Address) ?? groupId;
// groupName = null ?? "" = ""

var groupIsMapped = groupName != string.Empty && groupName != groupId;
// groupIsMapped = false (empty string comparison)

var groupSummary = groupIsMapped
    ? BuildPrincipalSummary(...)
    : FormatCodeSummary(groupId);  // FormatCodeSummary("") returns ""!
```

`FormatCodeSummary("")` returns `""` (empty string, since it guards against null/empty input).

Since `memberId = ""` too, no member part is added. The final `summaryText = ""`, and
`BuildSummaryHtml(model, "")` produces:

```
➕ azuread_group_member <b><code>user_groups</code></b> — 
```

(nothing after the em dash `—`)

#### Bug 2: No Attributes Table (ReportModelBuilder.ResourceChanges.cs)

`BuildAttributeChanges` processes `before` and `after` dictionaries, filtering out attributes
that haven't changed:

```csharp
var beforeDict = ConvertToFlatDictionary(change.Before);  // {} (before is null for create)
var afterDict = ConvertToFlatDictionary(change.After);    // {"group_object_id": null, "member_object_id": null, "id": null}

var allKeys = beforeDict.Keys.Union(afterDict.Keys).Order();
// allKeys = ["group_object_id", "id", "member_object_id"]

foreach (var key in allKeys)
{
    beforeDict.TryGetValue(key, out var beforeValue);  // null (not in dict)
    afterDict.TryGetValue(key, out var afterValue);    // null (value is JSON null)

    var valuesEqual = string.Equals(beforeValue, afterValue, StringComparison.Ordinal);
    // string.Equals(null, null) = true!

    if (!_showUnchangedValues && valuesEqual)
    {
        continue;  // SKIPPED! Both null values treated as "unchanged"
    }
    ...
}
```

`string.Equals(null, null, StringComparison.Ordinal)` returns `true`, so every attribute with
a null after-value (which is how Terraform signals "known after apply") is treated as "unchanged"
and skipped. **The `change.AfterUnknown` field is never consulted in this method.**

This is confirmed by grep showing `AfterUnknown` is only used in `ReportModelBuilder.Outputs.cs`
for output changes, but not for resource attribute changes.

Additionally, even if the attributes were included with `After = null`, the template
`format_attribute_value_table(attr.name, attr.after, ...)` returns `""` for null values, and
the template condition `{{ if value != "" }}` would still skip the row.

### Why It Happened

The implementation for skipping unchanged values predates full handling of Terraform's
"known after apply" semantics. The `AfterUnknown` field in the plan JSON specifically marks
attributes whose values will only be known after `terraform apply`. The current code:

1. Never reads `change.AfterUnknown` in the resource attribute-change building path
2. Compares null==null as "unchanged" without recognizing this is a create-with-computed-values case
3. Has no mechanism to display "(known after apply)" for resource attributes (only output values support this)

The `azuread_group_member` summary builder further compounds the issue by not having a fallback
when `groupId` resolves to an empty string.

## Suggested Fix Approach

### Fix 1: `BuildAttributeChanges` — Respect `after_unknown` Values

In `ReportModelBuilder.ResourceChanges.cs::BuildAttributeChanges`:

1. Build a flat dictionary from `change.AfterUnknown`:
   ```csharp
   var afterUnknownDict = ConvertToFlatDictionary(change.AfterUnknown);
   ```

2. Also include keys from `afterUnknownDict` in `allKeys` (handles the case where `after` is null
   but `after_unknown` lists specific attributes or is `true` for the whole resource):
   ```csharp
   var allKeys = beforeDict.Keys.Union(afterDict.Keys).Union(afterUnknownDict.Keys.Where(k => k != "")).Order();
   ```

3. For each key, check if it's a "known after apply" attribute:
   ```csharp
   var isComputedAfterApply = afterUnknownDict.TryGetValue(key, out var unknownVal)
       && string.Equals(unknownVal, "true", StringComparison.OrdinalIgnoreCase);
   // Also handle the whole-resource-unknown case: afterUnknownDict[""] == "true"
   var wholeResourceUnknown = afterUnknownDict.TryGetValue("", out var rootVal)
       && string.Equals(rootVal, "true", StringComparison.OrdinalIgnoreCase);
   isComputedAfterApply = isComputedAfterApply || wholeResourceUnknown;
   ```

4. If computed, override the display value:
   ```csharp
   if (isComputedAfterApply)
   {
       afterDisplay = "(known after apply)";
   }
   ```

5. Don't skip computed attributes even when `valuesEqual`:
   ```csharp
   if (!_showUnchangedValues && valuesEqual && !isComputedAfterApply)
   {
       continue;
   }
   ```

6. Optionally add `IsComputed` flag to `AttributeChangeModel` if template-level differentiation
   is needed (e.g., to style computed values differently).

### Fix 2: `BuildGroupMemberSummaryHtml` — Fallback When IDs Are Unknown

In `AzureAdSummaryBuilder.Groups.cs::BuildGroupMemberSummaryHtml`:

When `groupId` is empty string (null/unknown at plan time), show a meaningful fallback:

```csharp
var groupSummary = string.IsNullOrEmpty(groupId)
    ? FormatCodeSummary("(known after apply)")
    : (groupIsMapped
        ? BuildPrincipalSummary(model, "group_name", groupName, groupId, iconProviderRegistry)
        : FormatCodeSummary(groupId));
```

Similarly for `memberId`:
```csharp
if (!string.IsNullOrEmpty(memberId))
{
    // existing member logic
}
else
{
    summaryText = $"{summaryText} {MemberArrow} {FormatCodeSummary("(known after apply)")}";
}
```

This would produce a summary like:
```
➕ azuread_group_member <b><code>user_groups</code></b> — <code>(known after apply)</code> → <code>(known after apply)</code>
```

Alternatively, the simpler fallback is to only append the member arrow if both group and member
are known. When groupId is empty, just show the resource address as context.

### Fix 3 (Optional): `JsonStateReader.GetStringProperty` — Return `null` for JSON null

`JsonStateReader.GetStringProperty` currently returns `""` for `JsonValueKind.Null` via
`_ => property.ToString()`. Consider explicitly returning `null`:

```csharp
JsonValueKind.Null => null,  // add this case before the wildcard
```

This would make null JSON values consistently return `null` rather than `""`, simplifying
the null-check logic in `BuildGroupMemberSummaryHtml`.

## Minimal Reproduction JSON Plan

Create a file `azuread-group-member-unknown-plan.json`:

```json
{
  "format_version": "1.2",
  "terraform_version": "1.14.0",
  "resource_changes": [
    {
      "address": "module.azure.azuread_group_member.user_groups[\"team-example - user@example.de\"]",
      "module_address": "module.azure",
      "mode": "managed",
      "type": "azuread_group_member",
      "name": "user_groups",
      "provider_name": "registry.terraform.io/hashicorp/azuread",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "group_object_id": null,
          "member_object_id": null,
          "id": null
        },
        "after_unknown": {
          "group_object_id": true,
          "member_object_id": true,
          "id": true
        },
        "before_sensitive": {},
        "after_sensitive": {}
      }
    },
    {
      "address": "module.azure.azuread_group_member.user_groups[\"team-example - user2@example.de\"]",
      "module_address": "module.azure",
      "mode": "managed",
      "type": "azuread_group_member",
      "name": "user_groups",
      "provider_name": "registry.terraform.io/hashicorp/azuread",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "group_object_id": null,
          "member_object_id": null,
          "id": null
        },
        "after_unknown": {
          "group_object_id": true,
          "member_object_id": true,
          "id": true
        },
        "before_sensitive": {},
        "after_sensitive": {}
      }
    }
  ]
}
```

This can also be expressed with `after = null` and `after_unknown = true`:

```json
{
  "change": {
    "actions": ["create"],
    "before": null,
    "after": null,
    "after_unknown": true,
    "before_sensitive": false,
    "after_sensitive": false
  }
}
```

## Related Tests

Tests that should pass after the fix:

- [ ] `AzureAdGroupMemberTemplateTests.Create_WithAllUnknownAttributes_ShowsKnownAfterApplySummary`
  — verifies that when both IDs are null/unknown, the summary shows "(known after apply)"
- [ ] `AzureAdGroupMemberTemplateTests.Create_WithAllUnknownAttributes_ShowsAttributeTable`
  — verifies that the attributes table shows `group_object_id` and `member_object_id` rows with "(known after apply)"
- [ ] `AzureAdGroupMemberTemplateTests.Create_WithPartiallyUnknownAttributes_ShowsMixedSummary`
  — verifies that when `group_object_id` is unknown but `member_object_id` is known, shows "(known after apply) → <member display>"

Existing tests that must continue to pass:
- [ ] `AzureAdGroupMemberTemplateTests.Create_RendersGroupToMemberSummaryWithIcons`
  — known values still render correctly
- [ ] `AzureAdGroupMemberTemplateTests.Create_WithMissingMemberId_StopsAtGroup`
  — missing member still shows only group

Snapshot test to add or update:
- [ ] `azuread-group-member-all-unknown` — new snapshot for all-unknown scenario
- [ ] `azuread-group-member-partially-unknown` — new snapshot for partially-unknown scenario

## Additional Context

- **Related Plan Files:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuread-group-members-known-after-apply-plan.json`
  — existing test data shows the partial-unknown case (group_object_id unknown, member_object_id known),
  which is rendered via the parent-child relationship. The standalone rendering path is not tested.
- **Related Feature:** `docs/features/014-unchanged-values-cli-option/specification.md`
  — the `--show-unchanged` flag controls whether unchanged attributes are shown. The bug means
  "known after apply" attributes are incorrectly treated as "unchanged".
- **Related Code:** `ReportModelBuilder.Outputs.cs:47` — outputs already handle `AfterUnknown`
  correctly with `var isComputed = outputChange.AfterUnknown;`. The same pattern should be
  applied to resource attribute changes.
- **Terraform Documentation:** In Terraform's plan JSON format:
  - `after` contains the known post-apply values; attributes unknown at plan time are set to `null`
  - `after_unknown` contains `true` for each attribute whose value is unknown at plan time
  - These two fields together represent the complete state: `null` in `after` with `true` in
    `after_unknown` means "will have a value after apply, but we don't know what yet"
