# OpenTofu/Terraform ephemeral resource support (`open` action)

This patch adds support for the `open` action used by ephemeral resources in OpenTofu 1.10+ and Terraform 1.10+, eliminating warnings when processing plans containing ephemeral resources.

## 🐛 Bug fixes

### Fixed "unknown action" warning for ephemeral resources

**Problem:** When processing OpenTofu or Terraform plans containing ephemeral resources (such as Vault secrets via `vault_kv_secret_v2`), `tfplan2md` would emit a warning and misclassify the resource:

```
Warning: Encountered unknown Terraform action set: [open]; classifying as 'unknown'.
```

The `open` action is used by ephemeral resources to fetch temporary values (like secrets or tokens) at plan/apply time. These resources are never persisted to state and are cleaned up automatically after use.

**Symptom:** Resources with the `open` action appeared in reports with:
- ⚠️ Unknown action icon
- Warning messages in stderr
- Incorrect classification

**Fix:** Added support for the `open` action, which now displays correctly with the ➕ Add icon and "open" classification. The warning no longer appears.

### Fixed incorrect "replace" classification for ephemeral resource lifecycle

**Problem:** OpenTofu and Terraform use `["create", "forget"]` or `["forget", "create"]` action combinations when replacing ephemeral resources. These were incorrectly classified as "create" instead of "replace".

**Fix:** Added proper "replace" classification for these action combinations, consistent with how `["create", "delete"]` is handled for regular resources.

## 📋 Impact

**Who was affected:** Users of OpenTofu 1.10+ or Terraform 1.10+ with ephemeral resources in their configurations (common use cases include Vault secrets, temporary credentials, time-limited tokens).

**When it occurred:** When running `tfplan2md` on any plan containing ephemeral resources with the `open` action.

**What caused confusion:** The warning made it appear as if there was a problem with the plan or the tool, when in fact the plan was valid and the action was simply not yet supported.

## ✅ What now works correctly

- Ephemeral resources with `actions: ["open"]` are classified correctly as **open** with ➕ Add icon
- Replace operations for ephemeral resources (`["create", "forget"]` or `["forget", "create"]`) are classified as **replace** with 🔄 Replace icon
- No warnings are emitted for ephemeral resource actions
- Reports accurately reflect the ephemeral resource lifecycle without confusion

### Action classification after fix

| Action(s) | Previous Behavior | New Behavior | Icon |
|-----------|------------------|--------------|------|
| `["open"]` | ⚠️ unknown (warning) | ➕ open | Add icon |
| `["forget","create"]` | ➕ create (wrong) | 🔄 replace | Replace icon |
| `["create","forget"]` | ➕ create (wrong) | 🔄 replace | Replace icon |

## 🔍 About ephemeral resources

Ephemeral resources are a security feature introduced in OpenTofu 1.10 and Terraform 1.10 that allows fetching sensitive values (like secrets, passwords, tokens) without persisting them to state files. Key characteristics:

- **Lifecycle:** Resources are "opened" (fetched) during plan/apply and automatically "closed" (cleaned up) after use
- **Security:** Values never written to state or plan files, reducing attack surface
- **Use cases:** Vault secrets, temporary credentials, time-limited tokens, API keys
- **Compliance:** Supports SOC2, HIPAA, PCI-DSS requirements by minimizing sensitive data exposure

## 🔗 Commits

- [`a506ccd`](https://github.com/oocx/tfplan2md/commit/a506ccd02c7dec0c7ce46556054fd03d844e55e2) fix: add support for ephemeral resource 'open' action and replace variants

## 🧪 Test coverage

Added 3 new test cases to verify the fix:

1. **`Build_OpenAction_ActionIsOpen`** - Verifies `["open"]` → `"open"` action classification
2. **`Build_ForgetThenCreateAction_ClassifiedAsReplace`** - Verifies `["forget","create"]` → `"replace"`
3. **`Build_CreateThenForgetAction_ClassifiedAsReplace`** - Verifies `["create","forget"]` → `"replace"`

All tests passing.
