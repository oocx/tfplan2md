# Issue: OpenTofu Ephemeral Resource `open` Action Not Recognized

## Problem Description

When processing an OpenTofu JSON plan that includes ephemeral resources (e.g., Vault secrets via `vault_kv_secret_v2`), `tfplan2md` emits a warning and classifies the resource as `unknown`:

```
Warning: Encountered unknown Terraform action set: [open]; classifying as 'unknown'.
```

The `open` action appears in `resource_changes[*].change.actions` for resources with `"mode": "ephemeral"`.

## Steps to Reproduce

1. Create an OpenTofu configuration using ephemeral resources:
   ```hcl
   ephemeral "vault_kv_secret_v2" "ephemeral_grafana_users_password" {
     # configuration
   }
   ```

2. Generate a JSON plan:
   ```bash
   tofu plan -out=plan.tfplan
   tofu show -json plan.tfplan > plan.json
   ```

3. Process with tfplan2md:
   ```bash
   cat plan.json | tfplan2md --render-target github
   # Warning: Encountered unknown Terraform action set: [open]; classifying as 'unknown'.
   ```

**Example JSON plan snippet:**
```json
{
  "resource_changes": [
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
  ]
}
```

## Expected Behavior

- No warning is emitted
- The resource is classified as `open` with an appropriate icon (➕ Add icon)
- The resource is rendered in the markdown report without error

## Actual Behavior

- Warning is printed to stderr: `Warning: Encountered unknown Terraform action set: [open]; classifying as 'unknown'.`
- Resource is classified as `unknown` with ⚠️ icon
- This creates confusion for users working with OpenTofu ephemeral resources

## Root Cause Analysis

### Affected Components

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`

**Lines:**
- Line 18-25: Action constants definition
- Line 160-206: `DetermineAction` method
- Line 217-227: `GetActionSymbol` method

### What's Broken

The `DetermineAction` method handles the following actions:
- ✅ `create`, `delete`, `update`, `read`, `no-op`, `forget`
- ✅ `["create", "delete"]` and `["delete", "create"]` as replace
- ❌ `open` - **missing**
- ❌ `["forget", "create"]` - **missing** (OpenTofu replace variant)
- ❌ `["create", "forget"]` - **missing** (Terraform replace variant)

When the `open` action is encountered, it falls through to line 202-205 where unknown actions trigger a warning and are classified as `UnknownAction`.

### Why It Happened

The `open` action is an **OpenTofu-specific extension** added with ephemeral resource support in OpenTofu 1.10+. This action is not documented in the HashiCorp Terraform JSON format documentation because ephemeral resources are a feature unique to OpenTofu (though Terraform also has ephemeral resources starting in 1.10, they use the same `open` action).

**Key findings from research:**

1. **Ephemeral Resource Lifecycle:**
   - **Open**: Resource is "opened" (value fetched) at plan/apply time - **serialized to JSON plan**
   - **Renew**: Optional renewal of time-bound resources during execution - **NOT serialized to JSON plan** (internal only)
   - **Close**: Resource is "closed" (cleaned up) after use - **NOT serialized to JSON plan** (internal only)

2. **Actions that appear in JSON plans:**
   - `["open"]` - OpenTofu ephemeral resource lifecycle (this issue)
   - `["forget", "create"]` - OpenTofu replace variant (also missing)
   - `["create", "forget"]` - Terraform replace variant (also missing)

3. **Actions that do NOT appear in JSON plans:**
   - `renew` and `close` - These are internal lifecycle hooks for progress display only, never serialized

**Source verification:**
- OpenTofu source code comment in `internal/command/jsonplan/plan.go`: *"NOTE: Renew and Close missing on purpose. Those are not meant to be stored in the plan. Instead, we have hooks for those to show progress."*
- HashiCorp Terraform has similar internal-only handling for `renew` and `close`

## Suggested Fix Approach

All three gaps are in the `DetermineAction` method in `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`:

### 1. Add `open` action support

**Add constant (after line 22):**
```csharp
private const string OpenAction = "open";
```

**Add handling in `DetermineAction` (after the `ReadAction` check, around line 190):**
```csharp
if (actions.Contains(OpenAction))
{
    return OpenAction;
}
```

**Add symbol mapping in `GetActionSymbol` (around line 222):**
```csharp
OpenAction => ActionIcons.Add,
```

**Rationale:** The `open` action has semantics similar to `read` - it's a non-destructive fetch operation that brings data into the plan without modifying infrastructure. Using the Add icon (➕) is consistent with how `read` is displayed.

### 2. Handle `["forget", "create"]` as replace (OpenTofu variant)

**Add handling in `DetermineAction` (BEFORE the single-action `CreateAction` check, around line 172):**
```csharp
if (actions.Contains(CreateAction) && actions.Contains(ForgetAction))
{
    return ReplaceAction;
}
```

**Rationale:** `["forget", "create"]` is OpenTofu's forget-then-create replace pattern. The order is normalized by using `Contains`, so this single check handles both `["forget", "create"]` and `["create", "forget"]`.

### 3. Handle `["create", "forget"]` as replace (Terraform variant)

**No separate code needed** - the check in step 2 handles both orderings since `Contains` is order-independent.

### 4. Add tests

Add three tests to `tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/ReportModelBuilderRefactoringTests.cs`:

1. **`Build_OpenAction_ActionIsOpen`** - Verifies `["open"]` → `"open"` action, not `"unknown"`
2. **`Build_ForgetThenCreateAction_ClassifiedAsReplace`** - Verifies `["forget","create"]` → `"replace"`
3. **`Build_CreateThenForgetAction_ClassifiedAsReplace`** - Verifies `["create","forget"]` → `"replace"`

### Summary of Changes

| Action(s) | Current Behavior | Fixed Behavior | Icon |
|-----------|-----------------|----------------|------|
| `["open"]` | ⚠️ unknown (warning) | ➕ open | `ActionIcons.Add` |
| `["forget","create"]` | ➕ create (wrong) | 🔄 replace | `ActionIcons.Replace` |
| `["create","forget"]` | ➕ create (wrong) | 🔄 replace | `ActionIcons.Replace` |

## Recommended Icon for `open` Action

**Icon:** ➕ Add (`ActionIcons.Add`)

**Reasoning:**
- The `open` action is semantically similar to `read` - both are non-destructive fetch operations
- Neither modifies infrastructure; they only bring data into the plan
- The existing code already uses `ActionIcons.Add` for `read` action (line 222)
- Consistency: ephemeral resource "open" is conceptually "reading" a temporary value
- The icon visually indicates "adding to plan" without implying infrastructure mutation

## Related Tests

Tests that should pass after the fix:

- [ ] `Build_OpenAction_ActionIsOpen` - New test
- [ ] `Build_ForgetThenCreateAction_ClassifiedAsReplace` - New test
- [ ] `Build_CreateThenForgetAction_ClassifiedAsReplace` - New test
- [ ] All existing `ReportModelBuilderRefactoringTests` remain green

## Additional Context

### References

**OpenTofu Documentation:**
- Ephemeral resources: https://opentofu.org/docs/language/ephemerality/ephemeral-resources/
- JSON format: https://opentofu.org/docs/internals/json-format/
- Blog: https://opentofu.org/blog/ephemeral-ready-for-testing/

**OpenTofu Source Code:**
- Action constants: https://github.com/opentofu/opentofu/blob/main/internal/plans/action.go
- JSON serialization: https://github.com/opentofu/opentofu/blob/main/internal/command/jsonplan/plan.go

**Terraform Documentation:**
- Ephemeral resources: https://developer.hashicorp.com/terraform/plugin/framework/ephemeral-resources
- Ephemeral values: https://developer.hashicorp.com/terraform/language/manage-sensitive-data/ephemeral
- Renew action: https://developer.hashicorp.com/terraform/plugin/framework/ephemeral-resources/renew

**Terraform Source Code:**
- Action constants: https://github.com/hashicorp/terraform/blob/main/internal/plans/action.go
- JSON serialization: https://github.com/hashicorp/terraform/blob/main/internal/command/jsonplan/plan.go
- terraform-json library: https://github.com/hashicorp/terraform-json/blob/main/action.go

**Related PR:**
- PR #546: Comprehensive analysis of OpenTofu vs Terraform action differences

### Security and Compliance Notes

Ephemeral resources are designed for security:
- Values never written to state or plan files
- Ideal for secrets, tokens, temporary credentials
- Reduces attack surface for sensitive data
- Supports compliance requirements (SOC2, HIPAA, PCI-DSS)

### Versions

- **OpenTofu:** Ephemeral resources introduced in v1.10, matured in v1.11
- **Terraform:** Ephemeral resources introduced in v1.10
- Both use the same `open` action in JSON plans for ephemeral resource lifecycle
