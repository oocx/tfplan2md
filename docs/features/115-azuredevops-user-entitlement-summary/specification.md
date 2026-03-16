# Feature: Azure DevOps User Entitlement Summary Fields

## Overview

The `azuredevops_user_entitlement` Terraform resource currently falls back to the generic Azure DevOps summary (showing `name` and `project_id`). These fields are typically empty or absent for user entitlement resources. Instead, the report should display the user's `principal_name`, their `account_license_type`, and `licensing_source` in the summary line — but only when those values are non-empty.

## User Goals

- **Readable user entitlement summaries**: Users reviewing a Terraform plan want to immediately see _who_ is being granted access and what license type they receive, without having to expand the full change details.
- **Actionable at a glance**: When multiple user entitlements are added or modified in one plan, the summary line should distinguish them clearly (e.g., by principal name and license type).
- **No visual noise for absent fields**: If a field is not set in the plan (e.g., `licensing_source` is unset or empty), it should be omitted from the summary rather than shown as blank or `null`.

## Scope

### In Scope

- Add a resource-type mapping for `azuredevops_user_entitlement` to `ResourceSummaryMappings.ResourceMappings` with the ordered attribute list: `["principal_name", "account_license_type", "licensing_source"]`.
- The existing `ResourceSummaryBuilder` already skips empty/null values, so the "only display if non-empty" condition is satisfied automatically by the mapping.
- Snapshot tests and/or unit tests covering the new mapping, including cases where one or more fields are empty.

### Out of Scope

- Custom renderer for `azuredevops_user_entitlement` (the existing `ResourceSummaryBuilder` pipeline is sufficient).
- Mapping or display of any other `azuredevops_user_entitlement` attributes beyond the three listed above.
- Changes to the CLI interface.
- Changes to how other Azure DevOps resource summaries are displayed.

## User Experience

### Current Behaviour

A plan containing an `azuredevops_user_entitlement` create action produces a summary line similar to:

```
➕ azuredevops_user_entitlement john.doe@example.com
```

The resource address (`john.doe@example.com`) is the only context shown, because the provider fallback keys (`name`, `project_id`) are not present on this resource type.

### New Behaviour

After this change, when `principal_name`, `account_license_type`, and `licensing_source` are present, the summary should read:

```
➕ azuredevops_user_entitlement john.doe@example.com — john.doe@example.com | express | msdn
```

When `licensing_source` is empty (a common case):

```
➕ azuredevops_user_entitlement john.doe@example.com — john.doe@example.com | express
```

When only `principal_name` is populated:

```
➕ azuredevops_user_entitlement john.doe@example.com — john.doe@example.com
```

### Error / Edge Cases

- If all three fields are empty, the summary falls back to the Terraform resource address (existing behaviour — no regression).
- Update summaries show the resolved name plus the list of changed attributes (existing `ResourceSummaryBuilder` update logic, unchanged).

## Success Criteria

- [ ] `ResourceSummaryMappings.ResourceMappings` contains an entry for `azuredevops_user_entitlement` with keys `["principal_name", "account_license_type", "licensing_source"]`.
- [ ] A create summary for a resource with all three fields set includes `principal_name`, `account_license_type`, and `licensing_source` separated by the standard `" | "` delimiter.
- [ ] A create summary for a resource where `licensing_source` is empty omits that field from the output.
- [ ] A create summary for a resource where all three fields are empty falls back to the resource address (no regression).
- [ ] A unit test or snapshot test covers at least the following cases:
  - All three fields populated.
  - Only `principal_name` populated.
  - All fields empty (fallback behaviour).
- [ ] No existing tests are broken.

## Technical Notes

The implementation is expected to be a single-line addition to `ResourceSummaryMappings.ResourceMappings` in:

```
src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/ResourceSummaryMappings.cs
```

Add the following entry under the `// azuredevops` section:

```csharp
["azuredevops_user_entitlement"] = ["principal_name", "account_license_type", "licensing_source"],
```

No changes to renderers, templates, or other infrastructure are required.

## Open Questions

None. The approach is straightforward and consistent with how all other resource type summary mappings are defined.
