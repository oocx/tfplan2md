# Feature: Configurable, Aggregated Drift Rendering

## Overview

Terraform provider upgrades can cause many resources to report equivalent refresh-time drift. Rendering every entry separately can make a report hundreds of thousands of characters long, bury the planned change that a reviewer needs to assess, and exceed pull-request comment limits. Reviewers need a concise drift summary while retaining access to the affected resources and the ability to choose how much drift appears in a report.

## User Goals

- When many resources show the same kind of drift, reviewers can identify the pattern and its scale without scrolling through repeated resource cards.
- When needed, reviewers can expand a grouped entry to see every affected resource address.
- Pipeline users can choose to show all drift, only drift for resources participating in planned changes, or no drift.
- Existing users receive the current breadth of drift information unless they opt into another drift display mode.

## Scope

### In Scope

- Aggregate drift entries only when they have the same resource type, changed attribute path, and normalized before-and-after values. Entries with different value transitions remain separate groups.
- Each grouped entry shows the number of affected resources, the changed attribute path, and the shared before-and-after value.
- A grouped entry keeps every affected resource address available in a collapsed `<details>` list.
- Add a `--drift <all|relevant|none>` command-line option.
  - `all` shows every displayable drift entry and is the default when `--drift` is omitted.
  - `relevant` shows only displayable drift for resources that also participate in the plan's resource changes.
  - `none` omits the `🌀 Drift Detected` section entirely.
- Apply grouping after the selected drift mode has determined which drift entries are shown.
- Preserve the existing filtering of no-op and fully suppressed drift entries in modes that show drift.

### Out of Scope

- Classifying, labelling, or separately grouping likely provider-upgrade/read-back artifacts.
- Changing the rendering of regular planned resource changes, refactoring operations, relevant attributes, or plan-status banners.
- Changing Terraform plan generation or Terraform's own drift heuristic.
- Adding configuration beyond the `--drift` command-line option.

## User Experience

With the default behavior, a report that has multiple matching drift entries shows one concise entry rather than repeated cards. Its summary identifies the resource type, count, changed attribute path, and their shared value transition. Expanding its collapsed list reveals the addresses of all resources in the group. Entries whose value transitions differ are not combined.

```text
tfplan2md plan.json
tfplan2md --drift all plan.json
tfplan2md --drift relevant plan.json
tfplan2md --drift none plan.json
```

`--drift all` and omitting `--drift` include all displayable drift. `--drift relevant` limits the section to drift on resources involved in the plan's resource changes. `--drift none` produces no drift heading or drift content. Invalid `--drift` values are rejected with a clear command-line error that identifies the accepted values.

### Grouped drift preview

For two build definitions whose `repository[0].branch_name` value changed from `refs/heads/main` to `main`, the drift section is rendered like this:

```markdown
## 🌀 Drift Detected

<details>
<summary>🌀 2 azuredevops_build_definition resources — <code>repository[0].branch_name</code>: <code>refs/heads/main</code> → <code>main</code></summary>
<br>

- `azuredevops_build_definition.api`
- `azuredevops_build_definition.worker`

</details>
```

An otherwise matching entry whose value changes from `refs/heads/release` to `release` appears in a separate group, so the summary never implies that all resources had the same value transition when they did not.

## Success Criteria

- [ ] A report with multiple displayable drift entries with the same resource type, changed attribute path, and normalized before-and-after values renders one grouped drift entry instead of a separate full resource card for each entry.
- [ ] Drift entries with different normalized before-and-after values render in separate groups, even when their resource type and changed attribute path match.
- [ ] Each grouped entry identifies its resource type, changed attribute path, count of affected resources, and the shared before-and-after value.
- [ ] Every affected resource address in a grouped entry is available in a collapsed `<details>` list.
- [ ] `--drift all` includes all displayable drift entries; omitting the option has the same selection behavior.
- [ ] `--drift relevant` includes drift only when its resource has a displayable planned change; Terraform no-op resource-change entries do not make drift relevant.
- [ ] `--drift none` omits the `🌀 Drift Detected` section completely, including its heading.
- [ ] The selected drift mode is applied before grouping; no resource excluded by that mode appears in a group or its address list.
- [ ] Existing no-op and fully suppressed drift entries remain absent from every drift mode that renders drift.
- [ ] An invalid `--drift` value is rejected and tells the user that `all`, `relevant`, and `none` are valid choices.
- [ ] Reports with no displayable drift remain free of a drift section regardless of `--drift` mode.
- [ ] Automated tests cover the three drift modes, grouped drift with its collapsed address list, and the unchanged no-op/suppressed-drift filtering.

## Maintainer Decisions

- **Grouping values:** The grouping key includes normalized before-and-after values. Different value transitions are not grouped.
- **Relevant membership:** `relevant` includes only resources with displayable planned changes; Terraform no-op resource-change entries are excluded.
