# UAT Test Plan: Configurable, Aggregated Drift Rendering

## Goal

Demonstrate in rendered markdown and platform preview that repeated drift is compact,
expandable, selected correctly by `--drift`, and never hides or invents addresses.

## Required `uat-plan.json` Contents

The Developer must create a self-contained fixture with these exact elements. The
addresses make the UAT evidence unambiguous.

| Fixture element | Required contents | Purpose |
| --- | --- | --- |
| Shared drift group | `azuredevops_build_definition.api` and `.worker`, drifted on `repository[0].branch_name` from `refs/heads/main` to `main` | Aggregation, count, transition, full addresses |
| Split transition | `azuredevops_build_definition.release`, same path, `refs/heads/release` to `release` | Different transitions stay separate |
| Relevant member | A displayable planned change for `.api` | Relevant selection |
| Excluded peer | No planned resource change for `.worker` | Filtering precedes grouping |
| Terraform no-op | Drift plus a no-op `resource_changes` entry | No-op is not relevant |
| Filtered drift | A drift record removed by existing no-op or provider suppression behavior | Preserved filtering |
| Escaping case | A displayable transition needing markdown/HTML/code escaping | Safe grouped rendering |

It must render unchanged with all commands:

```text
tfplan2md uat-plan.json
tfplan2md --drift all uat-plan.json
tfplan2md --drift relevant uat-plan.json
tfplan2md --drift none uat-plan.json
```

## Test Steps and Expected Results

1. Render without `--drift` and with `--drift all`; outputs have identical drift
   selection.
2. All mode has one collapsed details entry for `.api` and `.worker`: summary shows
   `2 azuredevops_build_definition`, `repository[0].branch_name`,
   `refs/heads/main`, and `main`. Expanding shows both, and only both, addresses.
3. `.release` is a separate details entry and is not counted in the main-branch group.
4. In `relevant`, the `.api` group has count `1` and lists `.api` only. `.worker`,
   `.release`, and the no-op resource are absent unless deliberately given a
   displayable planned change.
5. `none` has no drift heading, details element, or drift address.
6. Filtered drift is absent from all and relevant output; the escaping case renders
   safely rather than being interpreted as markup.
7. In the UAT pull-request preview, expand each details entry and verify the collapsed
   behavior and readable code formatting.

## Maintainer Review Checklist

- Default and all mode show the same drift coverage.
- Matching entries are compact but every address is discoverable on expansion.
- Different transitions remain separate.
- Relevant mode does not retain a resource merely because Terraform recorded a no-op.
- None mode has no drift heading.
- Previewed entries have no broken markdown or raw unsafe markup.
