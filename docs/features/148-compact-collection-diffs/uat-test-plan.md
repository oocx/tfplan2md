# UAT Test Plan: Compact Collection Diffs

## Goal

Confirm that compact collection rows, `➕`/`➖`/`🔀` markers, `<br>`-joined multi-line
cells and resource-level collection-drift `<details>` blocks render readably on GitHub,
Azure DevOps and Bitbucket, and that sensitive entries are masked individually, show whether they changed, and never leak a value, and that unresolved collections stay protected.
The cell layout is user-visible and explicitly confirmed at this gate (architecture.md).

## Required contents of uat-plan.json

The Developer builds `uat-plan.json` (and rendered `uat-plan.md`) in this work-item
folder from a valid Terraform plan JSON fixture with `resource_changes`, `resource_drift`,
`before_sensitive`/`after_sensitive`, `after_unknown` and `configuration`.

### Resource inventory

| Resource scenario | Required values / edge case |
|---|---|
| msgraph_resource.policy["ca"], update | `body.conditions.users.excludeUsers`: remove one GUID, add one GUID; `body.conditions.users.includeUsers` same GUIDs reordered only (order change) |
| msgraph_resource.policy["ca"] (same) | settings `api_version`, `url` unchanged: provider-settings block sits beside the compact row |
| azurerm_virtual_network.v, update | `dns_servers` with first entry changed order; `address_space` add one CIDR |
| azurerm_network_security_group.nsg, update | `security_rule` object list (not compacted, indices 0..11 incl. `[2]`/`[10]`) with a nested `source_address_prefixes` primitive list changed |
| generic review_object.dup, update | list with duplicates: `[a,a,b]` -> `[a,b]` and `[a,b]` -> `[a,a,b]` |
| review_object.insert, update | 12-element list with one insertion at index 3 |
| review_object.sorted, update | sorted set-like GUID list, membership change only |
| review_object.states, update | one attribute per state transition: absent -> `[]`, `null` -> `[]`, `[]` -> `[]` (all element-less on both sides, so no row by default), `[]` -> `[x]`, `[x]` -> `[]` (rows shown) |
| review_object.types, update | `[1,"1",true,"true",null]` -> `["1",1,true,null]` |
| review_object.unknown, update | whole list unknown after apply; separate list with one unknown element |
| review_object.secret_entries, update | `["a",<s1>,"b"]` -> `["a",<s2>,"c"]` (changed secret), a list where the secret is unchanged at the same position, an added and a removed masked entry, two masked entries reordered, and two equal secrets on one side. Sentinel values (`SECRET-AAA`, ...) that must never appear |
| review_object.secret_visible, update | `["x"]` -> `[<x>]` (visible becomes sensitive, same value) and the mirror `[<x>]` -> `["x"]`; plus an entry sensitive on the `after` side only |
| review_object.secret_whole, update | list marked sensitive as a whole, reordered and with membership change; a sensitive empty list on create and delete (no row expected) |
| review_object.secret_large, update | a masked entry whose value exceeds the large-value threshold beside a large visible element |
| review_object.secret_unknown, update | an entry both unknown and sensitive, beside a known masked entry |
| review_object.azure_ids, update | `ids_case_only`: Azure resource IDs differing only in case (expected hidden, also with `--show-unchanged-values`); `ids_case_plus_add`: same plus an added member (expected shown); `ids_case_masked`: masked ID pair differing only in case (expected shown as masked removed/added) |
| review_object.large, update | element exceeding the large-value threshold, plus a reorder of large elements |
| review_object.special, create | elements containing `|`, backtick, `<br>`, `<`, `&`, empty string, very long GUID |
| review_object.created / deleted | primitive list on create and on delete (single-value table) |
| review_object.replaced, replace | primitive list change forcing replacement |
| review_object.mixed, update | object list containing nested primitive list (non-compacted outer, compact inner); list of nested lists |
| azuread_group.admins, azuread_group.devs, azuread_group.ops (drift) | `resource_drift`: admins `members` gains a middle member; admins also has `owners` reordered; devs identical `members` change; ops scalar drift on `description` shared with another group to prove scalar grouping persists |
| azuredevops_variable_group / azuread provider resources | one each, collection unchanged, to confirm provider-specific tables unchanged |

Include resource-level drift cases with a wholly sensitive collection, a collection with a masked entry (visible members shown, masked entry marked), one with an unknown element, and one case-only Azure ID collection (expected no drift entry).

## Rendering runs

Render the fixture for `github`, `azuredevops`, `bitbucket` with:

1. Default options.
2. `--drift relevant`.
3. `--drift none` (collection drift and scalar drift both absent).
4. `--drift all`.
5. `--show-sensitive` (sentinels visible, the full diff identical to one without sensitive entries).
   Run also `--ignore-azure-id-case-changes` disabled (case-only lists become visible changes).
6. With a GUID display-name mapping (feature 147) enabled on the policy resource and
   without it, to compare annotations.

The UAT Tester obtains platform-rendered evidence through the repository UAT skills. If
a platform is unavailable, record the limitation in uat-report.md; a Markdown snapshot is not platform validation.

## Test steps and what to look for

1. **Compact rows.** Every primitive list is exactly one row. Row count in the detail table
   matches the "changed attributes" number in the resource summary line.
2. **Markers.** Removed occurrences show `➖` only in Before, added show `➕` only in After.
   Duplicates are visible as repeated lines. The cell stays readable for the 12-element list; note if the tall cell is
   too unwieldy (design follow-up: collapse unchanged runs).
3. **Order.** The reordered list shows the sequences and a `🔀 order changed` line, no `➖`/`➕`.
   The sorted set-like list shows no order line.
4. **States.** Element-less collections (absent, `null`, `[]` on both sides, including sensitive ones) produce no row by default; the absent -> `[]`, `null` -> `[]` and `[]` -> `[]` attributes must not appear. When the other side has elements, `[]` is shown and is distinguishable from `-` (absent/null). With `--show-unchanged-values` the element-less rows appear.
5. **Types.** `1` versus `"1"` shows as a visible change, not as equal.
6. **Unknown.** Whole-unknown shows the known-after-apply text; partial-unknown shows a label in place with no markers.
7. **Sensitive.** Default run, with the sentinel text absent from the whole document in every case:
   - Per entry: only entries Terraform marks are `(sensitive)`; sibling entries are shown normally.
   - Change markers: a changed secret shows `➖ (sensitive)` before and `➕ (sensitive)` after; an unchanged secret has no marker; added, removed and reordered masked entries carry markers and `🔀 order changed`.
   - Visible `x` that becomes sensitive `x` shows `➖ x` and `➕ (sensitive)`, never unchanged. An entry masked on one side only is masked only there.
   - Equal secrets on one side are shown as separate placeholders, with no count or duplicate label. Masked entries show no name annotation, even with a mapping.
   - A wholly sensitive list is one `(sensitive)` value per side with no count, markers or order line.
   - A masked entry with a very long value does not widen the table or move the list to the large section.
   - `--show-sensitive`: normal diff, identical to the same list without sensitive entries.
8. **Special characters** do not break the table on any platform (pipes, `<br>`, backticks, HTML).
9. **Large values** appear in the large-attribute section one occurrence per line with markers; masked entries there are `(sensitive)` lines with markers and no value.
10. **Non-compact collections** keep indexed rows, `[2]` above `[10]`; the nested primitive list is a compact row.
11. **Drift.** One `<details>` per affected resource; `admins` shows both `members` and `owners` paths; `devs` has its own entry; scalar drift for `description` still grouped across resources; summary text reports number of paths. Drift mode selections behave per run list above. A masked entry in drift is marked but shows no value; the case-only Azure ID list produces no drift entry.
12. **Provider tables** (azuread, azuredevops, NSG) look as in the previous release.
13. **Names.** With a display-name mapping, names annotate beside the GUID and the GUID remains; without it, raw GUID lines are sufficient to understand the change.
14. **Case-only Azure IDs.** `ids_case_only` is absent by default and under `--show-unchanged-values`, and not counted in the summary; `ids_case_plus_add` is shown; `ids_case_masked` is shown as masked removed/added; with the case filter disabled all appear.
15. On every target verify card structure intact (details/summary collapse), markers legible (emoji rendering), and cells not truncated. Record any broken presentation as a failure linked to the test case and criterion.

## Acceptance evidence

Record fixture paths, commands and options, platform evidence, per-scenario results and
Maintainer feedback (especially on the cell layout) in uat-report.md. The workflow driver owns the UAT gate decision.
