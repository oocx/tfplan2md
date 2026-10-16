# Feature: Compact, Semantically Accurate Collection Diffs

## Overview

Terraform collections are currently difficult to review in generic attribute tables. Long primitive collections expand into one row per numeric index, lexicographic ordering places index 10 before index 2, and an insertion into a collection can look like a cascade of unrelated replacements. The same index-shift noise can create many drift entries for one resource even when the meaningful change is a single added or removed member.

This feature makes primitive collection changes compact while preserving the distinctions reviewers need: added and removed occurrences, duplicate values, order changes in ordered collections, Terraform set semantics, and protected or unresolved values. It covers the generic report path and collection drift without changing established provider-specific table semantics.

## User Goals

- Review a primitive collection as one logical attribute instead of scanning one row per index.
- See which collection members were added or removed without interpreting an index-shift cascade.
- Distinguish a membership change from an order-only change when order is meaningful.
- See all collection drift for an affected resource together instead of receiving a separate drift entry for each shifted index.
- Continue to understand changes when identifiers have no display-name mapping, while benefiting from resolved names when they are available.

## Scope

### In Scope

- Sort numeric collection indices by their numeric value wherever indexed attribute paths remain visible, so index 2 appears before index 10. Non-index path components retain their existing ordering semantics.
- In generic attribute tables, present each collection whose elements are all primitive values as one logical attribute row for create, update, replace, and delete actions.
- For collection updates, show added and removed element occurrences as a collection diff. Repeated equal values are retained with their multiplicity; removing one of two equal occurrences is one removal, not removal of the value altogether.
- Treat every primitive collection as ordered. Plan JSON serializes lists, tuples, and sets identically as arrays and carries no provider schema, so tfplan2md cannot tell a set from a list.
  - Report an order-only change even when the same element occurrences exist before and after.
  - This loses nothing for sets: Terraform emits set elements in a canonical sorted order, so a set never produces an order-only change, and its membership changes keep the remaining elements in relative order.
- Compare elements by their underlying Terraform values. A display name supplied by feature 147 (`guid-display-name-resolution`) may annotate an element, but it never changes whether two elements are equal.
- Keep `null`, an absent collection, an empty collection, an unknown value, and a sensitive value visibly distinct whenever a row or drift entry shows them, following the report's existing value-handling rules.
  - An absent collection, a `null` collection, and an empty collection are equivalent for change detection. A collection with no elements on either side is unchanged, whatever state each side has. By default it produces no row and no drift entry, which is how the per-index rows on `main` behave today. Terraform writes every unset nested block as an empty collection, so reporting these transitions would add a noise row for each unset block.
  - When a row is shown, because the other side has elements or because `--show-unchanged-values` is set, the reader can still tell each side's state apart.
  - Unknown elements or collections remain visibly unknown and are not presented as known additions or removals. An unknown value is always a change, regardless of the element-less rule.
- Never show a sensitive value unless `--show-sensitive` is set, and still show the non-sensitive members of a collection. Mask exactly the entries that Terraform marks as sensitive. Mask the whole collection only when Terraform marks the collection itself as sensitive. [Sensitive Values in Collections](#sensitive-values-in-collections) states the detailed rules.
- Apply the existing case-only Azure resource ID filter (`--ignore-azure-id-case-changes`, on by default) to compact collections. If the only difference in a collection is the letter case of Azure resource IDs, the collection counts as unchanged and is hidden. If the collection has any other change, the row is shown.
- Treat a collection as primitive only when every known, non-masked element is a scalar value or `null`. Collections containing nested collections or objects continue through their existing rendering path; their visible numeric indices receive natural sorting.
- In the drift section, summarize primitive collection drift once per affected resource rather than once per element index.
  - A resource's collection-drift entry contains every changed primitive collection path for that resource.
  - Each path shows its meaningful added occurrences, removed occurrences, and any meaningful order change.
  - Scalar drift continues to use feature 145's grouping across resources by resource type, path, and normalized transition.
  - The existing `--drift all`, `--drift relevant`, and `--drift none` selection behavior is unchanged and is applied to collection-drift entries in the same way as other drift.
- Count a compact primitive collection as one changed attribute regardless of its number of elements or added and removed occurrences. Resource summaries and detail tables therefore agree on the number of logical attributes changed. A resource-level collection-drift entry may contain multiple changed collection paths, and its count is the number of those paths.
- Preserve the semantic layouts and comparison rules of existing provider-specific tables. This feature changes generic attribute rendering and the shared drift presentation only.

### Out of Scope

- Compacting collections that contain objects or nested collections into one row.
- Defining provider-specific identity keys or semantic matching for object collections.
- Changing existing provider-specific resource tables, including their collection layouts or diff rules.
- Adding GUID or principal lookup data. Feature 147 owns display-name resolution; this feature remains useful when no mapping is present.
- Changing resource labels, resource addresses, known-after-apply reference hints, or other report clarity concerns owned by feature 146 (`generic-resource-review-clarity`).
- Changing which resources qualify for `--drift relevant`, which drift is suppressed, or how scalar drift is grouped by feature 145.
- Changing Terraform plan generation or inferring provider schema semantics that are not observable in the input available to tfplan2md.

## User Experience

A primitive list is shown under its logical path rather than as many indexed rows. For example, an update to `body.conditions.users.excludeUsers` presents one collection change containing the removed and added occurrences. GUIDs remain sufficient to understand the change; if a display-name mapping is available, the name appears as an annotation beside the same GUID.

```text
body.conditions.users.excludeUsers
  - 11111111-1111-1111-1111-111111111111
  + 22222222-2222-2222-2222-222222222222 (Example User)
```

The exact Markdown layout is an architecture decision. The user-visible contract is that the collection has one logical row, additions and removals are unambiguous, duplicates are not collapsed, and names do not replace the underlying identity.

For an ordered list whose values only move, the report explicitly identifies an order change. It does not claim that one occurrence was removed and an equal occurrence added, and it does not omit the change. Terraform still updates the resource for such a reorder, even where the provider's API ignores order (for example the user GUIDs of a Conditional Access policy), so the report shows why the resource is updated. Order is significant in other cases, such as `dns_servers`, where the first entry is the primary server.

When one `azuread_group` gains a member in the middle of `members`, the drift section contains one collection-drift entry for that resource and one changed path for `members`. It reports the added member instead of a sequence of `members[n]: old → new` transitions. If the same resource also has drift in another primitive collection, both paths appear in that resource's collection-drift entry. Other resources receive their own collection-drift entries.

Unknown and sensitive states remain placeholders. A transition involving a partially or wholly unknown collection is described as unresolved rather than converted into a definitive element diff.

A collection that has no elements on either side is not reported, even if one side is absent, `null`, or `[]` and the other side is a different one of these states. When a collection gains its first elements or loses its last ones, the row shows the element-less side as absent, `null`, or `[]`, so the reader can tell which it was.

If a list of Azure resource IDs differs only in letter case, for example `/subscriptions/ABC/...` becoming `/subscriptions/abc/...` with nothing else changed, the collection is hidden in the same way that `--ignore-azure-id-case-changes` hides a scalar attribute today. If the same list also gains or loses a member, or its order changes, the collection is shown.

### Sensitive Values in Collections

The Maintainer requires that sensitive values are never shown, that non-sensitive values are still shown, that masking applies only to the entries that are actually sensitive, and that the report indicates whether a masked entry changed. Leaking a sensitive value by accident is the most serious failure this feature can have. The rules below therefore define what the report may reveal about a masked entry, in addition to forbidding its value.

These rules apply when `--show-sensitive` is not set. With `--show-sensitive`, the collection renders as if nothing were sensitive, as other attributes do today.

1. **Whole collection marked sensitive.** If Terraform marks the collection itself as sensitive, rather than individual entries, on either side, the collection is shown as one masked value on both sides. The report reveals no element count, positions, or per-entry markers. This matches the current behaviour for sensitive attributes.
2. **Individual entries marked sensitive.** If Terraform marks only some entries as sensitive, each of those entries is shown as a `(sensitive)` placeholder at its position. All other entries are shown normally, with their added, removed, and order information.
3. **Per side.** An entry is masked on each side where Terraform marks it as sensitive. A value that Terraform does not mark as sensitive on its side is shown.
4. **Masked entries are compared only with masked entries.** A masked entry can be equal only to a masked entry on the other side with the same underlying value. Against a non-masked entry it always counts as different, whatever the two values are. Without this rule, per-side masking and change markers together would leak a value: if `"x"` is visible before and a masked entry equal to `"x"` were shown as unchanged after, the reader would know the secret is `"x"`.
5. **What may be revealed about a masked entry.** Only information that comes from Terraform's sensitivity markers, or from comparing the masked entry with masked entries under rule 4:
   - that a masked entry exists, and its position on each side, and therefore the number of masked entries on each side. The per-index rows on `main` already reveal this.
   - whether the masked entry changed: it may carry an added, removed, unchanged, or moved marker, exactly as a non-sensitive entry does, computed under rule 4.
   - that it causes, or is part of, an order change, computed under rule 4.
   - that the collection as a whole changed. Terraform's plan already reveals this by planning an update.
6. **What must never be revealed about a masked entry:**
   - its value, or any part, prefix, length, or casing of it;
   - a display name or other annotation derived from its value, such as a feature 147 name;
   - whether it equals, or differs from, a non-masked entry on either side. Rule 4 makes every such comparison "different", so the report carries no information about it;
   - whether it equals another masked entry on the same side. Masked entries are never collapsed, counted, or labelled as duplicates of each other; each occurrence is its own `(sensitive)` placeholder. *(Assumption, see Open Questions.)*
7. **Masked values cannot affect anything except masked-to-masked comparisons.** The rendered report depends on a masked entry's value only through rule 4's comparisons with masked entries on the other side. As a test: replace the value of any masked entry with a different value, keeping it masked and keeping it equal to, or different from, each masked entry on the other side as before. The rendered report must not change. In particular, replacing a masked entry's value with a value equal to any visible entry, on either side, must not change the report.
8. **Every output path follows these rules:** the generic attribute table, the large-value section, collection drift entries, resource summaries and changed-attribute counts, and the case-only Azure ID filter. The case-only filter never treats a difference involving a masked entry as case-only, so masked content cannot decide whether a collection is hidden.
9. **Unknown sensitive entries.** An entry that is both sensitive and unknown follows the report's existing combined placeholder rule for sensitive, computed values.

Examples, where only the entries written as `<…>` are marked sensitive:

- Before `["a", <s1>, "b"]`, after `["a", <s2>, "c"]`, with `s1` different from `s2`. The row shows `a`, `➖ (sensitive)`, and `➖ b` before, and `a`, `➕ (sensitive)`, and `➕ c` after. The reader learns that the secret changed, but not either value.
- Before `["a", <s1>, "b"]`, after `["a", <s1>, "c"]`. The `(sensitive)` placeholder is unchanged on both sides, like `a`; only `b` and `c` carry markers.
- Before `["x"]`, where `"x"` is not sensitive, after `[<x>]`, where the same value `"x"` is now marked sensitive. Under rule 4 the entries are different: the row shows `➖ x` before and `➕ (sensitive)` after, exactly as it would if the secret held any other value.

## Source-Idea Traceability

| Source | Requirement coverage |
|---|---|
| Issue #674, idea 5 | Numeric indices use natural ordering wherever indexed paths remain visible. |
| Issue #674, idea 6 | Primitive collections use one logical row; updates show meaningful added and removed occurrences. |
| Issue #674, idea 8 | Collection drift is summarized once per affected resource, with all changed primitive collection paths together and without index-shift cascades. |
| Feature 145 | Scalar drift grouping and `--drift all/relevant/none` behavior remain compatible. |
| Feature 147 | Resolved names may annotate collection members, while raw values determine identity and unresolved identifiers remain meaningful. |
| Feature 146 | Resource and report clarity changes remain separately owned and are not required for collection diffs. |

## Success Criteria

- [ ] Numeric indices in visible attribute paths are ordered naturally, including multi-digit indices (`[2]` before `[10]`) and nested indexed paths.
- [ ] A generic table renders an all-primitive collection as one logical attribute row for create, update, replace, and delete actions.
- [ ] A primitive collection update identifies added and removed occurrences without emitting a replacement cascade caused only by shifted indices.
- [ ] Duplicate values retain their multiplicity: adding or removing one equal occurrence changes the diff by exactly one occurrence.
- [ ] An order-only change to a primitive collection is visible and is identified as an order change, not as removed and added occurrences.
- [ ] Adding or removing members of a sorted collection, as Terraform emits sets, reports only the membership change and no order change.
- [ ] Equality uses underlying Terraform values; enabling or changing GUID display-name mappings cannot turn an unchanged member into a change or merge two distinct members.
- [ ] The same collection diff remains understandable with raw GUIDs only and gains optional name annotations when feature 147 can resolve them.
- [ ] Empty, absent, and `null` collection states stay visibly distinct whenever a row or drift entry shows them, but they are equivalent for change detection: a collection with no elements on either side, in any combination of these states, produces no row by default and never a drift entry, and is shown under `--show-unchanged-values` with each side's state distinguishable.
- [ ] Whole or partial unknown collection transitions remain visibly unresolved and are not represented as definitive known additions or removals.
- [ ] Without `--show-sensitive`, no sensitive value appears anywhere in the report, while every non-sensitive entry of the same collection is shown: only entries Terraform marks as sensitive are replaced by `(sensitive)`, and a collection Terraform marks as sensitive as a whole is one masked value with no element count.
- [ ] Mixed primitive scalar types can use the compact row without conflating values of different Terraform types.
- [ ] Collections containing objects or nested collections retain their existing rendering semantics, with natural ordering applied to any visible numeric indices.
- [ ] Primitive collection drift produces one collection-drift entry per affected resource, not one entry per indexed element.
- [ ] If one resource has drift in multiple primitive collection paths, all those paths appear in that resource's single collection-drift entry and each path has its own meaningful diff.
- [ ] Two resources with collection drift receive separate resource-level collection-drift entries, even when their collection paths and transitions match.
- [ ] Scalar drift continues to group across resources according to feature 145.
- [ ] `--drift all`, omission of `--drift`, `--drift relevant`, and `--drift none` select or omit collection-drift entries consistently with feature 145, including existing no-op and suppression behavior.
- [ ] A compact collection contributes one to the resource's changed-attribute count; a collection-drift entry containing multiple changed collection paths reports the number of paths, not the number of member occurrences or source indices.
- [ ] Existing provider-specific tables retain their current semantic layouts and comparison behavior.
- [ ] A masked entry shows whether it changed: it carries an added, removed, unchanged, or moved marker and can take part in an order change, computed by comparing it only with masked entries on the other side.
- [ ] A masked entry and a non-masked entry always count as different, whatever their values: a visible value before that becomes a masked entry with the same value after is shown as removed and added, never as unchanged or moved.
- [ ] A masked entry shows no value, part, length, or casing of its value, and no value-derived annotation such as a feature 147 name.
- [ ] Masked entries on the same side are never collapsed, counted, or labelled as duplicates of each other; each occurrence is its own `(sensitive)` placeholder.
- [ ] Replacing the value of any masked entry with a different value, while keeping it masked and keeping its equality with each masked entry on the other side unchanged, leaves the rendered report unchanged in the generic table, the large-value section, drift, summaries, and counts. This includes replacing it with a value equal to any visible entry on either side.
- [ ] With `--show-sensitive`, a collection with sensitive entries renders the same full diff as a collection without sensitive entries.
- [ ] A primitive collection whose only differences are letter-case differences of Azure resource IDs is hidden under `--ignore-azure-id-case-changes`, including under `--show-unchanged-values`, produces no drift entry, and does not count as a changed attribute; a collection with any other change is shown.
- [ ] A difference in a masked entry is never treated as a case-only Azure resource ID change.

## Open Questions and Assumptions

- The issue calls the proposed update rendering a "set diff" but does not define behavior for Terraform lists, tuples, duplicates, or reorder-only changes. Resolved 2026-10-16 by the Maintainer: every primitive collection is treated as ordered and duplicate occurrences are preserved.
  - Evidence: with Terraform 1.14, reordering a set in configuration yields a `no-op` with identical, sorted before and after values, while reordering a list yields an `update`.
  - Both kinds are common in practice. Current schemas define 885 list and 600 set primitive attributes in azurerm 5.10, and 38 list and 52 set attributes in azuread 3.10. Arrays inside azapi `body` are always ordered.
  - Many schema lists are semantically unordered in the provider's API, for example Conditional Access user GUIDs, Key Vault access-policy permissions, and firewall rule addresses. A reorder there is noise, but it still triggers an update, so it is reported as an order change rather than hidden.
- Should absent, `null`, and `[]` collections be distinct for change detection, as the earlier wording of criterion 9 required, or only in display? This was raised by code review round 3 (B1, M1) and is recorded in architecture section 7a. Resolved 2026-10-16 by the Maintainer: they stay visibly distinct whenever a row is shown, but are equivalent for change detection. A collection with no elements on either side produces no row or drift entry by default, as on `main`. As a consequence, the UAT test plan no longer expects `absent → []` or `null → []` rows.
- If one entry of a primitive collection is sensitive, should the whole collection be masked, or only that entry? The Architect had assumed whole-collection masking. Resolved 2026-10-16 by the Maintainer, reversing that assumption: "sensitive values must never be shown. However, non-sensitive values must be shown, don't mask the whole collection, but only the entries which are actually sensitive. Be super careful here to not expose sensitive values by accident." [Sensitive Values in Collections](#sensitive-values-in-collections) states what may and may not be revealed about masked entries.
- Should a collection whose only difference is the letter case of Azure resource IDs be shown? Resolved 2026-10-16 by the Maintainer: "hide if that's the only change." If the collection also has another change, the row is shown. In that row, the case-variant entries may appear as a removed and an added occurrence, because the Maintainer ruled only on the case where case is the only difference.
- Should an entry that is sensitive on one side only be masked on both sides, or per side? Resolved 2026-10-16 by the Maintainer: mask per side. A value that Terraform does not mark as sensitive on its own side is shown.
- May the report indicate whether a masked entry changed? The previous draft forbade any marker on a masked entry. Resolved 2026-10-16 by the Maintainer, reversing that: "I want you to indicate whether the secret changed and per side masking." A masked entry may carry added, removed, unchanged, and moved markers and may cause or be part of an order change. To keep this compatible with per-side masking, a masked entry is compared only with masked entries and always counts as different from a non-masked entry (rule 4).
- Assumption (open): the report never indicates that two masked entries on the same side are equal. Each occurrence is shown as its own placeholder, so multiplicity is preserved without revealing that two secrets match. The Maintainer's decision covers whether a secret changed between before and after, which is a cross-side comparison; equality between two secrets on the same side is additional information about the values that the diff does not need. The alternative would allow duplicate indication between masked entries, as for visible entries.
