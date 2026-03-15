# Fix: ArgumentNullException on output-only Terraform plans

This release is a bug-fix-only update. It fixes a crash (`ArgumentNullException`) that occurred when processing Terraform plans where the `resource_changes` field is absent or explicitly `null` — a situation that arises with output-only plans (plans that change only outputs, with no infrastructure resource changes).

## 🐛 Bug fixes

- **Fixed crash on output-only plans**: `tfplan2md` previously threw `ArgumentNull_Generic Arg_ParamName_Name, source` when the input plan JSON had no `resource_changes` key (or `"resource_changes": null`). The tool now treats a missing or null `resource_changes` as an empty list and produces a valid report showing only the output changes.
- **Null-safe action handling**: Additional null guards added for `Change.Actions` and `OutputChange.Actions` to protect against similarly missing fields in unusual plan formats.

### Affected scenarios

- Terraform plans generated on workspaces with only `output` block changes and no resource add/change/destroy operations
- Plans produced by Terraform 1.13.x (confirmed with user-reported plan from Terraform 1.13.3)
- Any plan where `"resource_changes"` is omitted or set to `null` in the JSON

## 🔗 Commits

- [`2de4452`](https://github.com/oocx/tfplan2md/commit/2de44523b375d6decf34c96ee081bf75a32f4e6f) fix: handle null resource_changes/actions in tfplan.json gracefully
