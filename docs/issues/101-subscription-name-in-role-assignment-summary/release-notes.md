# Subscription name now shown in role assignment summary

This patch fixes the `azurerm_role_assignment` summary line so that a mapped subscription display name is used instead of the raw subscription ID when the scope is at the subscription level.

## 🐛 Bug fixes

### Fixed subscription-level role assignment summary showing raw ID instead of display name

**Problem:** When an `azurerm_role_assignment` resource had a subscription-level scope (e.g., `scope = /subscriptions/12345678-...`) and a subscription name mapping was configured, the `<summary>` line still showed the raw subscription ID instead of the friendly display name.

For example, with a mapping of `{ "id": "sub-123", "displayName": "Production" }`, the summary incorrectly rendered as:

```
➕ azurerm_role_assignment my_assignment — 👤 Jane Doe → 🛡️ Contributor on subscription 🔑 sub-123
```

**Fix:** The `BuildScopeSummary()` method in `RoleAssignmentViewModelFactory` now consults the scope formatter for a subscription display name before falling back to the raw subscription ID — consistent with how the management group scope and the table `scope` attribute already behaved.

After the fix, the summary correctly renders as:

```
➕ azurerm_role_assignment my_assignment — 👤 Jane Doe → 🛡️ Contributor on subscription 🔑 Production
```

The table `scope` attribute continues to show the full `🔑 Production (sub-123)` format (name + ID), unchanged.

**Backward compatible:** When no mapping exists, or when no scope formatter is provided, the summary falls back to the raw subscription ID as before.

### Affected components

- `AzureEntityMapper` — Added `GetSubscriptionName()` method that returns just the display name (falling back to the raw ID when unmapped)
- `EnrichedAzureScopeFormatter` — Added `GetSubscriptionName()` delegating method
- `RoleAssignmentViewModelFactory` — Updated `BuildScopeSummary()` to use `scopeFormatter.GetSubscriptionName()` for subscription-level scopes

## 🧪 Test coverage

Added 5 new unit tests:

1. **`Build_WhenSubscriptionScopeAndMappingProvided_UsesMappedNameInSummary`** — verifies the summary shows the display name when a subscription mapping exists
2. **`Build_WhenSubscriptionScopeAndNoMapping_UsesIdInSummary`** — verifies fallback to raw ID when no mapping is configured
3. **`Build_WhenSubscriptionScopeAndNoScopeFormatter_UsesIdInSummary`** — verifies fallback to raw ID when no scope formatter is provided
4. **`AzureEntityMapper_GetSubscriptionName_ReturnsMappedDisplayName`** — verifies `AzureEntityMapper.GetSubscriptionName` returns just the display name (no ID suffix) when a mapping exists
5. **`AzureEntityMapper_GetSubscriptionName_FallsBackToRawId`** — verifies `AzureEntityMapper.GetSubscriptionName` returns the raw ID when no mapping is found

Added 2 subscription-level entries to the snapshot test data (`role-assignments.json`) and regenerated the snapshot:

- `subscription_scope_mapped` — scope `/subscriptions/sub-123` with a display name of `Production`; summary shows `🔑 Production`
- `subscription_scope_unmapped` — scope `/subscriptions/sub-unmapped-id` with no mapping; summary shows `🔑 sub-unmapped-id`

All 1230 tests passing.
