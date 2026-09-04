# Issue: Role assignment principal missing icon/type when `principal_type` is unknown

## Problem Description

When generating a report from a Terraform plan that contains an `azurerm_role_assignment` change where `principal_type` is **unknown at plan time**, the report renders the principal as just the mapped display name (e.g., `user@example.com`) instead of the expected decorated form (e.g., `👤 user@example.com (User)`).

This is surprising because:
- The report style guide documents identity icons and expects principals to be displayed with an icon and type.
- The principal mapping file uses the **nested** format (`users` / `groups` / `servicePrincipals`), which implicitly provides the principal type even when the plan does not.

## Steps to Reproduce

1. Use these inputs:
   - `examples/firewall-rules-demo/plan3.json`
   - `examples/firewall-rules-demo/principals.json`

2. Run:

```bash
cd /home/mathias/git/tfplan2md

dotnet run --project src/Oocx.TfPlan2Md -- \
  --principal-mapping examples/firewall-rules-demo/principals.json \
  examples/firewall-rules-demo/plan3.json
```

3. Observe the output in the role assignment section:

- Summary line shows the mapped name without icon/type:
  - `... azurerm_role_assignment ... — user@example.com → 🛡️ Contributor ...`
- Table row for `principal_id` shows:
  - ``| principal_id | `user@example.com` [`9f01f5d0-acc0-4228-a444-1293bbf46f9a`] |``

## Expected Behavior

The role assignment principal should be rendered as:

- `👤 user@example.com (User)` (and still include the GUID in brackets)

## Actual Behavior

The role assignment principal is rendered as:

- `user@example.com` (no icon, and no `(User)` type)

## Root Cause Analysis

### Affected Components

- Principal extraction in role assignment view model:
  - `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs`
  - `GetPrincipalInfo(...)` reads:
    - `principal_id` from state
    - `principal_type` from state (string)
  - If `principal_type` is missing in the plan state, the extracted `PrincipalInfo.Type` becomes empty.

- Principal formatting in role assignment table:
  - `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs`
  - `case "principal_id"` decorates with icon/type **only when** `principal.Type` is `User` / `Group` / `ServicePrincipal`.

- Principal mapping load behavior:
  - `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMapper.cs`
  - When a nested principals file is provided, it is flattened into a `Dictionary<string, string>` and the **type information is discarded**.

### What’s Broken

In `examples/firewall-rules-demo/plan3.json`, the `azurerm_role_assignment` has:

- `principal_id` present in `planned_values` / `change.after`
- `principal_type` *not* present as a concrete value (it only appears under `after_unknown`, meaning it will be known only after apply)

As a result:

1. `RoleAssignmentViewModelFactory.GetPrincipalInfo(...)` reads `principal_type` as `""` (empty string).
2. `principalMapper.GetName(...)` returns `user@example.com` (from the mapping file).
3. The `principal_id` formatter has no type → it does not add the icon or `(User)`.

### Why It Happened

The current implementation assumes `principal_type` is available in the Terraform plan state to drive identity decoration.

However, for `azurerm_role_assignment`, `principal_type` commonly shows up as **unknown at plan time** (computed), so the report generator should not rely solely on the plan value.

Because the principal mapping file is in nested format, the tool *could* infer the principal type from the section (`users` / `groups` / `servicePrincipals`), but `PrincipalMapper` currently flattens the structure and loses that data.

## Suggested Fix Approach

High-level approach: infer principal type from the nested principal mapping file when `principal_type` is missing/empty.

Two viable implementation options:

### Option A (recommended): Preserve type in `PrincipalMapper`

- Change `PrincipalMapper` to retain the nested mapping structure (or an additional `FrozenDictionary<string, string>` for type).
- Add an API to resolve principal **type** by ID (e.g., `TryGetPrincipalType(principalId, out type)`), then:
  - In `RoleAssignmentViewModelFactory.GetPrincipalInfo(...)`, if `principal_type` is empty, ask the mapper for the inferred type.

Pros:
- Keeps the mapping values clean (still just names).
- Keeps the formatting logic in the view model / template layer.

Cons:
- Requires a small interface/API expansion and plumbing.

### Option B: Decorate names on load (no API changes)

- When loading nested mapping sections, transform values into decorated strings:
  - Users: `👤 <name> (User)`
  - Groups: `👥 <name> (Group)`
  - ServicePrincipals: `💻 <name> (Service Principal)`

Pros:
- Minimal changes to call sites (type inference happens implicitly).

Cons:
- Mixes semantic formatting into the mapping layer.
- Risk of double-decoration depending on other formatting paths.

## Related Tests

- Update/add tests around role assignment principal formatting when `principal_type` is absent:
  - Existing: `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/PrincipalMapperTests.cs` currently asserts nested format returns only the raw name.
  - Existing: role assignment view model tests include cases for when principal type is provided (see `Build_WhenPrincipalTypeProvided_FormatsPrincipalId` in the test suite).

Suggested new test coverage:
- A role assignment JSON state with `principal_id` present and `principal_type` missing/unknown should render `principal_id` decorated using type inferred from nested mapping.

## Additional Context

- Report style guide documents identity icons:
  - `docs/report-style-guide.md` (Identity & Roles section)
- The plan data indicates `principal_type` is unknown-at-plan-time for this resource via `after_unknown.principal_type: true`.
