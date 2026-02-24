# Issue: Subscription Name Not Shown in Role Assignment Summary When Mapped

## Problem Description

When an `azurerm_role_assignment` has a **subscription-level** scope (e.g., `scope = /subscriptions/{id}`), the summary line currently always displays the raw subscription ID, even when a subscription name mapping is available. The expected behavior is to show the human-friendly subscription name (just the name, not "name (id)") in the summary when a mapping exists.

**Current output:**
```
azurerm_role_assignment additional_subscriptions["Azure Local-Contributor"] —
💻 principal-name → 🛡️ Contributor on subscription 🔑 subscription-id
```

**Expected output (when mapped):**
```
azurerm_role_assignment additional_subscriptions["Azure Local-Contributor"] —
💻 principal-name → 🛡️ Contributor on subscription 🔑 My Subscription Name
```

## Steps to Reproduce

1. Have a `tfplan2md` mapping file with a subscription entry, e.g.:
   ```json
   { "subscriptions": [{ "id": "12345678-...", "displayName": "Production" }] }
   ```
2. Have an `azurerm_role_assignment` resource with a **subscription-level** scope:
   ```hcl
   resource "azurerm_role_assignment" "example" {
     scope                = "/subscriptions/12345678-..."
     role_definition_name = "Contributor"
     principal_id         = ...
   }
   ```
3. Run `tfplan2md` with the mapping file.
4. Observe the summary shows `🔑 12345678-...` (the raw ID) instead of `🔑 Production`.

## Expected Behavior

When a subscription ID is found in the mapping/principals config, the summary text for subscription-level scoped role assignments should show only the subscription **display name** (e.g., `🔑 Production`) instead of the raw ID (e.g., `🔑 12345678-...`).

The table `scope` attribute already correctly shows `🔑 Production (12345678-...)` (name + ID) — the fix is specifically for the **summary text** (the `<summary>` element), which should show just the name with the 🔑 icon.

## Actual Behavior

The summary always shows the raw subscription ID regardless of whether a name mapping exists. The `scopeFormatter` parameter is available in the `BuildScopeSummary` method but is **not consulted** for the `ScopeLevel.Subscription` branch.

## Root Cause Analysis

### Affected Components

- **Primary:** `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs`
  - Method: `BuildScopeSummary()` — lines 249–276
  - Specifically the `ScopeLevel.Subscription` branch at lines 260–264
- **Supporting (already correct):** `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs`
  - Method: `FormatScopeValue()` — correctly calls `scopeFormatter?.GetSubscriptionDisplayName()` for table display
- **Mapping infrastructure (no changes needed):**
  - `src/Oocx.TfPlan2Md/Platforms/Azure/AzureEntityMapper.cs` — `GetSubscriptionDisplayName()` (line 65)
  - `src/Oocx.TfPlan2Md/Platforms/Azure/EnrichedAzureScopeFormatter.cs` — `GetSubscriptionDisplayName()` (line 77)

### What's Broken

In `RoleAssignmentViewModelFactory.cs`, the `BuildScopeSummary` method handles the `ScopeLevel.Subscription` case at lines 260–264:

```csharp
if (scope.Level == ScopeLevel.Subscription)
{
    var subscriptionId = scope.SubscriptionId ?? scope.SummaryName;
    return $"subscription {FormatAttributeValueSummary("subscription_id", subscriptionId, null)}";
}
```

This always uses the raw `subscriptionId` — it never consults `scopeFormatter` (which has the subscription name mapping). Compare this with:

1. **The table formatter** (`FormatScopeValue`, same file) — correctly calls `scopeFormatter?.GetSubscriptionDisplayName(scope.SubscriptionId, resourceAddress)` before formatting.
2. **The management group branch** in the same `BuildScopeSummary` method — correctly calls `scopeFormatter?.GetManagementGroupLabel(scope.Name, resourceAddress)` to get the display name.

### Why It Happened

The subscription display name enrichment was added to the table formatter (`FormatScopeValue`) and to the `EnrichedAzureScopeFormatter.Format()` path, but the `BuildScopeSummary` summary-text path was not updated to match. The management group case was correctly handled but the subscription case was overlooked.

### Subscription Mapping Infrastructure

The mapping infrastructure is fully in place and working:

| Class | Method | Returns |
|-------|--------|---------|
| `AzureEntityMapper` | `GetSubscriptionDisplayName(id, addr)` | `"DisplayName (id)"` if mapped; raw `id` if not |
| `EnrichedAzureScopeFormatter` | `GetSubscriptionDisplayName(id, addr)` | Delegates to `AzureEntityMapper` |

For the **summary text**, we want **only the display name** (not `"DisplayName (id)"`), so we need to either:
- Add a new `GetSubscriptionName(id, addr)` method to `AzureEntityMapper` / `EnrichedAzureScopeFormatter` that returns just the display name (falling back to the ID when unmapped), or
- Inline the logic in `BuildScopeSummary` using the existing `_subscriptions` dictionary exposed via a new method.

The preferred approach (minimal change) is to **add a new method** `GetSubscriptionName()` to `AzureEntityMapper` returning just the display name (or the raw ID when unmapped), expose it via `EnrichedAzureScopeFormatter`, and call it in `BuildScopeSummary`.

## Suggested Fix Approach

### 1. Add `GetSubscriptionName()` to `AzureEntityMapper`

In `src/Oocx.TfPlan2Md/Platforms/Azure/AzureEntityMapper.cs`, add a new method alongside `GetSubscriptionDisplayName`:

```csharp
/// <summary>
/// Gets the subscription display name only (without the ID) when available.
/// </summary>
/// <returns>The display name when mapped; the raw ID otherwise.</returns>
internal string GetSubscriptionName(string? subscriptionId, string? resourceAddress = null)
{
    if (string.IsNullOrWhiteSpace(subscriptionId))
        return string.Empty;

    if (_subscriptions.TryGetValue(subscriptionId, out var displayName))
        return displayName;

    RecordFailure(FailedResolutionType.Subscription, subscriptionId, resourceAddress);
    return subscriptionId;
}
```

### 2. Expose `GetSubscriptionName()` via `EnrichedAzureScopeFormatter`

In `src/Oocx.TfPlan2Md/Platforms/Azure/EnrichedAzureScopeFormatter.cs`, add:

```csharp
internal string GetSubscriptionName(string? subscriptionId, string? resourceAddress = null)
{
    return _entityMapper.GetSubscriptionName(subscriptionId, resourceAddress);
}
```

### 3. Update `BuildScopeSummary` in `RoleAssignmentViewModelFactory`

In `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs`, update the `ScopeLevel.Subscription` branch (lines 260–264) from:

```csharp
if (scope.Level == ScopeLevel.Subscription)
{
    var subscriptionId = scope.SubscriptionId ?? scope.SummaryName;
    return $"subscription {FormatAttributeValueSummary("subscription_id", subscriptionId, null)}";
}
```

To:

```csharp
if (scope.Level == ScopeLevel.Subscription)
{
    var subscriptionId = scope.SubscriptionId ?? scope.SummaryName;
    var subscriptionDisplay = scopeFormatter?.GetSubscriptionName(subscriptionId, resourceAddress) ?? subscriptionId;
    return $"subscription {FormatAttributeValueSummary("subscription_id", subscriptionDisplay, null)}";
}
```

This ensures that when no `scopeFormatter` is provided (or the subscription isn't mapped), the raw ID is used as before — fully backward compatible.

## Related Tests

### New Tests Required

Add to `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/RoleAssignmentManagementGroupFormattingTests.cs` (or a new file `RoleAssignmentSubscriptionScopeFormattingTests.cs`):

- [ ] `Build_WhenSubscriptionScopeAndMappingProvided_UsesMappedNameInSummary` — verifies summary shows display name, not ID
- [ ] `Build_WhenSubscriptionScopeAndNoMapping_UsesIdInSummary` — verifies fallback to raw ID still works
- [ ] `Build_WhenSubscriptionScopeAndNoScopeFormatter_UsesIdInSummary` — verifies null `scopeFormatter` still works

Pattern to follow: `RoleAssignmentManagementGroupFormattingTests.Build_WhenManagementGroupScope_FormatsSummaryWithIcon()`.

### Existing Tests That Need Updating

If a subscription-level scoped role assignment is added to the snapshot test data:

- [ ] `MarkdownSnapshotTests.Snapshot_RoleAssignments_MatchesBaseline` — snapshot at `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/role-assignments.md` — would need to be updated if new subscription-scope test entries are added to `role-assignments.json`

The existing `RoleAssignmentViewModelFactoryTests.Build_WhenScopeFormatterProvided_UsesSubscriptionDisplayName` test only verifies the **table** attribute (not the summary), so it does not need updating, but it could serve as a reference pattern.

### Tests That Already Pass (No Update Needed)

- All existing snapshot tests — the current test data only has resource-group and resource-level scopes for role assignments; none have subscription-level scope, so existing snapshots are unaffected.
- `RoleAssignmentViewModelFactoryTests` — existing tests pass `scopeFormatter: null`, which remains the fallback path.

## Test Data to Add

To fully test the fix, add a subscription-level scoped role assignment to `src/tests/Oocx.TfPlan2Md.TUnit/TestData/role-assignments.json` and register a matching subscription name in the mapping used by the snapshot test (or use the unit test approach without snapshot data).

A minimal entry to add to the JSON plan:
```json
{
  "address": "azurerm_role_assignment.subscription_scope",
  "module_address": "module.security",
  "mode": "managed",
  "type": "azurerm_role_assignment",
  "name": "subscription_scope",
  "provider_name": "registry.terraform.io/hashicorp/azurerm",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": {
      "scope": "/subscriptions/sub-mapped",
      "role_definition_id": "/subscriptions/sub-mapped/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c",
      "role_definition_name": "Contributor",
      "principal_id": "11111111-1111-1111-1111-111111111111",
      "principal_type": "User",
      "name": "ra-sub-scope"
    }
  }
}
```

And add to the principals mapping:
```json
{ "id": "sub-mapped", "displayName": "My Subscription" }
```

## Additional Context

- **Related parallel:** The management group scope in `BuildScopeSummary` (lines 268–272) already correctly uses `scopeFormatter?.GetManagementGroupLabel()` — the subscription case should follow the same pattern.
- **Table display is correct:** `FormatScopeValue()` at line ~306 already calls `scopeFormatter?.GetSubscriptionDisplayName()` for the `scope` table attribute, showing `"Production (12345678-...)"`. The fix only affects the summary text.
- **Consistent with design intent:** `EnrichedAzureScopeFormatter.Format()` (used for table/details scope display) also resolves the subscription display name via `_entityMapper.GetSubscriptionDisplayName()`.
- **No breaking changes:** The fix falls back to the raw ID when `scopeFormatter` is null or when no mapping exists, preserving all existing behavior.
