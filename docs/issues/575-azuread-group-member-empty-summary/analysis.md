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

---

## Enhanced Context Investigation: Beyond "(known after apply)"

> **Status:** Investigation completed post-initial fix (2025-01-27).
> The initial fix (Developer log above) shows `(known after apply)` as a placeholder.
> This section documents what richer context is available and how to surface it.

### Problem Statement

`(known after apply)` is technically correct but not maximally useful. A reviewer seeing:

```
➕ azuread_group_member user_groups — (known after apply) → (known after apply)
```

still cannot tell which group the user is being added to. The Terraform plan JSON contains
more information than currently displayed. This investigation documents what is available and
the recommended approach to use it.

---

### Finding 1: Configuration Expressions Block Contains Source References

The Terraform plan JSON's `configuration` block contains an `expressions` object for each
resource. Each attribute expression may include a `references` array listing the source
resources or variables.

**Example plan JSON** (from `azuread-group-members-known-after-apply-plan.json`):

```json
{
  "configuration": {
    "root_module": {
      "resources": [
        {
          "address": "azuread_group_member.platform_admin_member",
          "expressions": {
            "group_object_id": {
              "references": [
                "azuread_group.platform_engineers.id",
                "azuread_group.platform_engineers"
              ]
            },
            "member_object_id": {
              "constant_value": "user-200"
            }
          }
        }
      ]
    }
  }
}
```

For a `for_each`-based resource where the IDs come from `each.value`:

```json
{
  "address": "azuread_group_member.members",
  "expressions": {
    "group_object_id": {
      "references": [
        "azuread_group.team.id",
        "azuread_group.team"
      ]
    },
    "member_object_id": {
      "references": [
        "each.value"
      ]
    }
  }
}
```

The `references` array always lists references **from most-specific to least-specific**:
- `azuread_group.team.id` (attribute-level reference)
- `azuread_group.team` (resource-level reference)

The **resource-level reference** (without a trailing attribute) is always the last and
most useful entry for display purposes.

---

### Finding 2: The ConfigurationReferenceResolver Already Parses This Data

`src/Oocx.TfPlan2Md/Parsing/ConfigurationReferenceResolver.cs` already parses the full
configuration block and builds an index:

```
IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>>
```

Keyed by `(resource_address, attribute_name)`, the index maps:

```
("azuread_group_member.platform_admin_member", "group_object_id")
  → ["azuread_group.platform_engineers.id", "azuread_group.platform_engineers"]
```

For `for_each` resources like `azuread_group_member.user_groups["team-example - user@example.de"]`,
the address is **normalized** (by `NormalizeResourceAddressForConfigurationLookup`) to
`azuread_group_member.user_groups` before lookup. This matches the configuration block,
which stores only the base address.

This index is built in `ReportModelBuilder.Build.cs:23` and stored as
`_configurationReferenceIndex` in `ReportModelBuilder.cs`.

**Current limitation**: The index is only used in `ReportModelBuilder.ParentChildMerging.cs`
for parent-child grouping. It is **not passed to** `IResourceViewModelFactory.ApplyViewModel`
or the `AzureAdSummaryBuilder`.

---

### Finding 3: Two Reference Scenarios with Different Handling

#### Scenario A: Direct Resource Reference (e.g., `azuread_group.team.object_id`)

```hcl
resource "azuread_group_member" "members" {
  for_each        = toset(["user1", "user2"])
  group_object_id = azuread_group.team.object_id
  member_object_id = each.value
}
```

Config references for `group_object_id`:
```json
{ "references": ["azuread_group.team.id", "azuread_group.team"] }
```

The resource reference `azuread_group.team` is extractable by filtering to references
with exactly 2 dot-separated parts (or 4 for module resources).

**Improved output possible**:
```
➕ azuread_group_member members — azuread_group.team (known after apply) → user1
```

#### Scenario B: for_each with `each.value` References

```hcl
resource "azuread_group_member" "user_groups" {
  for_each         = { for v in local.user_groups_members : "${v.group_name} - ${v.user_name}" => v }
  group_object_id  = each.value.group_object_id
  member_object_id = each.value.user_object_id
}
```

Config references for `group_object_id`:
```json
{ "references": ["each.value.group_object_id", "each.value"] }
```

These are **dynamic references** (start with `each.`, `var.`, `local.`, etc.) — they point
to the `for_each` collection, not to a static resource. No useful resource name is available
from the configuration block alone.

**But the for_each instance key itself contains the useful data** (see Finding 4).

---

### Finding 4: The for_each Instance Key Contains Human-Readable Context

For `for_each`-based resources, the Terraform resource address includes the instance key
in bracket notation:

```
azuread_group_member.user_groups["team-example - user@example.de"]
module.azure.azuread_group_member.user_groups["platform-team - admin@example.com"]
```

The instance key is extractable from `model.Address`:

```csharp
var bracketIndex = address.LastIndexOf('[');
if (bracketIndex >= 0 && address.EndsWith(']'))
{
    // Strips surrounding quotes for string keys
    var instanceKey = address[(bracketIndex + 1)..^1].Trim('"');
    // instanceKey = "team-example - user@example.de"
}
```

**In the user's real-world scenario**, the for_each key is:
```
"${v.group_name} - ${v.user_name}"
```
So the instance key encodes both the group name and user name, making it highly readable.

**Improved output possible** (Scenario B fallback):
```
➕ azuread_group_member user_groups["team-example - user@example.de"] — (known after apply) → (known after apply)
```

Or even cleaner — surface the instance key as a sub-label:
```
➕ azuread_group_member user_groups — (known after apply) → (known after apply)
   [team-example - user@example.de]
```

However, **the simplest win is already provided** by the resource address in the rendered
summary line prefix (`user_groups` is the resource name). The full address including instance
key is also rendered elsewhere in the template. The _summary line_ improvement is the
high-value target.

---

### Finding 5: What Is Currently Accessible vs What Requires Changes

| Data Source | Currently Accessible in `BuildGroupMemberSummaryHtml`? | Change Required |
|-------------|-------------------------------------------------------|----------------|
| `model.Address` (full address with for_each key) | ✅ Yes — `model.Address` is already available | None |
| `model.Name` (resource name, e.g., `user_groups`) | ✅ Yes — already used in prefix | None |
| `change.AfterUnknown` | ✅ Partial — available via `resourceChange.Change.AfterUnknown` (passed to factory) | None (but not currently read in summary builder) |
| `configuration.expressions.references` via `_configurationReferenceIndex` | ❌ No — not passed to `ApplyViewModel` | Requires API change OR `ResourceChangeModel` enrichment |

---

### Recommended Approach: Two-tier Enhancement

#### Tier 1 (Low-cost, immediate): Use For_Each Instance Key from `model.Address`

When `groupId` is empty (known after apply), check whether the resource address contains
a for_each instance key. If so, include it in the summary to provide instance-level context
**without any API changes**:

```csharp
private static string? ExtractInstanceKey(string address)
{
    if (!address.EndsWith(']')) return null;
    var bracketIndex = address.LastIndexOf('[');
    if (bracketIndex < 0) return null;
    return address[(bracketIndex + 1)..^1].Trim('"');
}
```

Updated summary for Scenario B:
```
➕ azuread_group_member user_groups — (known after apply) ["team-example - user@example.de"] → (known after apply)
```

**Pros**: No interface changes, no new properties, minimal risk  
**Cons**: Only helps for_each resources; doesn't show the source group resource name

#### Tier 2 (Medium-cost): Enrich `ResourceChangeModel` with Attribute References

Add a new property to `ResourceChangeModel`:

```csharp
/// <summary>
/// Gets the configuration references for attributes of this resource,
/// populated from the Terraform plan's configuration block.
/// Key: attribute name; Value: list of reference addresses.
/// Null when no configuration block is present in the plan.
/// </summary>
public IReadOnlyDictionary<string, IReadOnlyList<string>>? AttributeReferences { get; init; }
```

Populate in `ReportModelBuilder.ResourceChanges.cs` during model construction:

```csharp
// After normalizing the address for configuration lookup:
var normalizedAddr = NormalizeResourceAddressForConfigurationLookup(rc.Address);
var attributeRefs = _configurationReferenceIndex
    .Where(kvp => StringComparer.OrdinalIgnoreCase.Equals(kvp.Key.Address, normalizedAddr))
    .ToDictionary(kvp => kvp.Key.Attribute, kvp => kvp.Value);

// ... include in model init:
AttributeReferences = attributeRefs.Count > 0 ? attributeRefs : null,
```

Then in `BuildGroupMemberSummaryHtml`, when `groupId` is empty:

```csharp
// Try to extract a resource reference for group_object_id
string? groupRef = null;
if (model.AttributeReferences?.TryGetValue("group_object_id", out var refs) == true)
{
    // Resource-level references have exactly 2 parts (type.name) for root,
    // or 4+ parts (module.n.type.name) for module resources.
    // Dynamic refs (each.*, var.*, local.*, data.*) are excluded.
    groupRef = refs.FirstOrDefault(r =>
        !r.StartsWith("each.", StringComparison.OrdinalIgnoreCase) &&
        !r.StartsWith("var.", StringComparison.OrdinalIgnoreCase) &&
        !r.StartsWith("local.", StringComparison.OrdinalIgnoreCase) &&
        !r.StartsWith("path.", StringComparison.OrdinalIgnoreCase) &&
        r.Split('.').Length == 2);  // type.name format
}

var groupDisplay = groupRef is not null
    ? FormatCodeSummary(groupRef)         // "azuread_group.platform_engineers"
    : FormatCodeSummary("(known after apply)");
```

**Pros**: Shows the source resource reference when statically known (most useful for reviewers)  
**Cons**: Requires new property on `ResourceChangeModel`, look-up logic in `ReportModelBuilder`

---

### Expected Output After Both Tiers

#### Scenario A (direct resource reference, no for_each instance key):
```
➕ azuread_group_member platform_admin_member — azuread_group.platform_engineers → user-200
```

#### Scenario B (each.value references, with for_each instance key):
```
➕ azuread_group_member user_groups — (known after apply) ["team-example - user@example.de"] → (known after apply)
```

#### Scenario C (direct reference + for_each instance key, numeric index):
```
➕ azuread_group_member members — azuread_group.team → [0]
```

---

### Implementation Summary for Developer

Priority order:

1. **Tier 2 first** (configuration references): Adds the most value — when `group_object_id`
   references a specific `azuread_group.*` resource, show that resource name instead of
   `(known after apply)`.

2. **Tier 1 as fallback** (for_each instance key): When no resource reference is found
   (i.e., only `each.value.*` references), append the instance key from the address for
   Scenario B resources.

3. **Apply same pattern to `member_object_id`**: If `memberId` is also empty, apply the
   same reference lookup for `member_object_id` references.

### Test Data Required

| Scenario | Test Data File | Configuration Block Needed? |
|----------|---------------|----------------------------|
| A: Direct resource ref, group unknown | `azuread-group-member-known-group-ref-plan.json` | ✅ Yes |
| B: `each.value` ref, all unknown | Update `azuread-group-member-all-unknown-plan.json` with configuration block | ✅ Yes |
| C: No configuration block | `azuread-group-member-all-unknown-plan.json` (existing) | ❌ No (fallback to `(known after apply)`) |

### Snapshot Updates Required

- `azuread-group-member-all-unknown.md` — should show instance key when present
- New snapshot: `azuread-group-member-known-group-ref.md` — shows `azuread_group.X` reference
