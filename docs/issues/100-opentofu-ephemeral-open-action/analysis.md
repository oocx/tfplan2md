# Issue: OpenTofu Ephemeral Resource `open` Action Not Recognized

## Problem Description

When processing an OpenTofu JSON plan that includes ephemeral resources (e.g. Vault secrets
via `vault_kv_secret_v2`), `tfplan2md` emits a warning and classifies the resource as `unknown`:

```
Warning: Encountered unknown Terraform action set: [open]; classifying as 'unknown'.
```

The `open` action appears in `resource_changes[*].change.actions` for resources with
`"mode": "ephemeral"`.

## Steps to Reproduce

```bash
cat plan.json | tfplan2md --render-target github
# Warning: Encountered unknown Terraform action set: [open]; classifying as 'unknown'.
```

Where `plan.json` contains a resource change like:

```json
{
  "address": "ephemeral.vault_kv_secret_v2.ephemeral_grafana_users_password",
  "mode": "ephemeral",
  "type": "vault_kv_secret_v2",
  "name": "ephemeral_grafana_users_password",
  "provider_name": "registry.opentofu.org/hashicorp/vault",
  "change": {
    "actions": ["open"],
    "before": null,
    "after": null,
    "after_unknown": {},
    "before_sensitive": false,
    "after_sensitive": false
  }
}
```

## Expected Behavior

No warning is emitted; the resource is classified and rendered without error.

## Actual Behavior

Warning is printed to stderr; resource is classified as `unknown` (⚠️ icon).

## Root Cause Analysis

### Affected Components

- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — `DetermineAction` method (line ~202) does not have a case for `"open"`, so it falls through to the warning/unknown branch.

### What's Broken

`DetermineAction` handles: `create`, `delete`, `update`, `read`, `no-op`, `forget`, and the
`[create, delete]` / `[delete, create]` replace combinations. It does not handle `open`.

### Why It Happened

The `open` action is an OpenTofu extension added with ephemeral resource support (OpenTofu 1.10+).
It is not listed in the HashiCorp Terraform JSON format docs
(<https://developer.hashicorp.com/terraform/internals/json-format>) because it is an
OpenTofu-specific feature that HashiCorp's `terraform-json` parsing library also does not
include.

---

## Research Findings: OpenTofu vs HashiCorp Terraform Action Differences

### 1. The `open` action

**OpenTofu** defines `Open Action = '⁐'` in `internal/plans/action.go` and serializes it to
`["open"]` in the JSON plan output via `actionString("Open")` in
`internal/command/jsonplan/plan.go`. It is emitted in `resource_changes` for ephemeral
resources.

**HashiCorp Terraform** defines the same `Open Action = '⟃'` constant in
`internal/plans/action.go`, but its `actionString` function in
`internal/command/jsonplan/plan.go` has **no case for `Open`** — it falls through to `default:
return []string{action}`, which would serialize as `["Open"]` (capital O). In practice
Terraform does **not** emit `"open"` in its JSON plan output. The canonical
`hashicorp/terraform-json` parsing library does not define an `ActionOpen` constant.

### 2. Other OpenTofu-specific actions that appear in the JSON plan

| JSON value | OpenTofu source constant | Description |
|---|---|---|
| `["open"]` | `Open` | Open an ephemeral resource — **emitted to JSON plan** |
| `["forget","create"]` | `ForgetThenCreate` | Forget old state, create new — **emitted to JSON plan** — a replace variant |

Actions that exist internally but are **not** serialized to the JSON plan in OpenTofu:
`Renew` and `Close`. The OpenTofu source explicitly documents this:
_"NOTE: Renew and Close missing on purpose. Those are not meant to be stored in the plan.
Instead, we have hooks for those to show progress."_

### 3. HashiCorp Terraform-specific actions that appear in the JSON plan

| JSON value | Terraform source constant | Description |
|---|---|---|
| `["create","forget"]` | `CreateThenForget` | Create new resource, forget old — **emitted to JSON plan** — a replace variant |

`CreateThenForget` does not exist in OpenTofu. `ForgetThenCreate` does not exist in HashiCorp
Terraform.

### 4. Summary: all serialized actions compared

| JSON action(s) | OpenTofu | HashiCorp Terraform |
|---|---|---|
| `["no-op"]` | ✅ | ✅ |
| `["create"]` | ✅ | ✅ |
| `["delete"]` | ✅ | ✅ |
| `["update"]` | ✅ | ✅ |
| `["read"]` | ✅ | ✅ |
| `["forget"]` | ✅ | ✅ |
| `["delete","create"]` (replace) | ✅ | ✅ |
| `["create","delete"]` (replace) | ✅ | ✅ |
| `["open"]` | ✅ **OpenTofu only** | ❌ not emitted |
| `["forget","create"]` (replace) | ✅ **OpenTofu only** | ❌ not present |
| `["create","forget"]` (replace) | ❌ not present | ✅ **Terraform only** |

### 5. Current `tfplan2md` gaps

| Gap | Symptom | Severity |
|---|---|---|
| `["open"]` not recognized | Warning + classified as `unknown` (⚠️) | **Reported by user** |
| `["forget","create"]` not recognized | Silently classified as `create` (wrong — should be `replace`) | Bug |
| `["create","forget"]` not recognized | Silently classified as `create` (wrong — should be `replace`) | Bug |

---

## Suggested Fix Approach

All three gaps are in `DetermineAction` in
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`:

1. **Add `open` constant and handling** — after the `read` check, add:
   ```csharp
   private const string OpenAction = "open";
   // ...
   if (actions.Contains(OpenAction)) { return OpenAction; }
   ```
   Map to `ActionIcons.Add` in `GetActionSymbol` (same semantics as `read` — a read-only
   fetch, not an infrastructure mutation). Do **not** count in any summary total.

2. **Handle `["forget","create"]` as replace** — before the single-action `create` check:
   ```csharp
   if (actions.Contains(CreateAction) && actions.Contains(ForgetAction)) { return ReplaceAction; }
   ```

3. **Handle `["create","forget"]` as replace** — same check covers both orderings since
   `Contains` is order-independent.

4. **Tests** — add three tests to `ReportModelBuilderRefactoringTests.cs`:
   - `Build_OpenAction_ActionIsOpen` — verifies `open` → `"open"`, not `"unknown"`
   - `Build_ForgetThenCreateAction_ClassifiedAsReplace` — verifies `["forget","create"]` → `"replace"`
   - `Build_CreateThenForgetAction_ClassifiedAsReplace` — verifies `["create","forget"]` → `"replace"`

## Related Tests

Tests that should pass after the fix:

- [ ] `Build_OpenAction_ActionIsOpen`
- [ ] `Build_ForgetThenCreateAction_ClassifiedAsReplace`
- [ ] `Build_CreateThenForgetAction_ClassifiedAsReplace`
- [ ] All existing `ReportModelBuilderRefactoringTests` remain green

## Additional Context

- OpenTofu ephemeral resources docs: <https://opentofu.org/docs/language/ephemerality/ephemeral-resources/>
- OpenTofu JSON format docs: <https://opentofu.org/docs/internals/json-format/>
- OpenTofu source — action constants: [`internal/plans/action.go`](https://github.com/opentofu/opentofu/blob/main/internal/plans/action.go)
- OpenTofu source — JSON serialization: [`internal/command/jsonplan/plan.go`](https://github.com/opentofu/opentofu/blob/main/internal/command/jsonplan/plan.go)
- HashiCorp Terraform source — action constants: [`internal/plans/action.go`](https://github.com/hashicorp/terraform/blob/main/internal/plans/action.go)
- HashiCorp Terraform source — JSON serialization: [`internal/command/jsonplan/plan.go`](https://github.com/hashicorp/terraform/blob/main/internal/command/jsonplan/plan.go)
- HashiCorp `terraform-json` library (canonical action enum): [`action.go`](https://github.com/hashicorp/terraform-json/blob/main/action.go) — does not define `open`
