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
- Respect observable Terraform collection semantics:
  - For lists and tuples, preserve order and report an order-only change even when the same element occurrences exist before and after.
  - For sets, disregard presentation order and report only membership changes.
  - When the available plan information does not establish whether a collection is ordered or unordered, treat it as ordered so a potentially meaningful reorder is not hidden.
- Compare elements by their underlying Terraform values. A display name supplied by feature 147 (`guid-display-name-resolution`) may annotate an element, but it never changes whether two elements are equal.
- Keep `null`, an absent collection, an empty collection, an unknown value, and a sensitive value observably distinct according to the report's existing value-handling rules.
  - Unknown elements or collections remain visibly unknown and are not presented as known additions or removals.
  - Sensitive elements or collections remain masked unless existing user options permit their display, and the diff must not reveal a protected value through labels, counts, ordering, or comparison text.
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

For an ordered list whose values only move, the report explicitly identifies an order change. It does not claim that one occurrence was removed and an equal occurrence added, and it does not omit the change. The same presentation-only order difference in a Terraform set produces no collection change.

When one `azuread_group` gains a member in the middle of `members`, the drift section contains one collection-drift entry for that resource and one changed path for `members`. It reports the added member instead of a sequence of `members[n]: old → new` transitions. If the same resource also has drift in another primitive collection, both paths appear in that resource's collection-drift entry. Other resources receive their own collection-drift entries.

Unknown and sensitive states remain placeholders. A transition involving a partially or wholly unknown collection is described as unresolved rather than converted into a definitive element diff. Masked values remain masked through the same existing options and policies used elsewhere in the report.

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
- [ ] An order-only change to a known ordered collection is visible and is identified as an order change.
- [ ] An order-only difference in a known Terraform set is omitted, while genuine set membership additions and removals remain visible.
- [ ] When collection kind is unavailable, a reorder is preserved under the documented ordered-collection assumption.
- [ ] Equality uses underlying Terraform values; enabling or changing GUID display-name mappings cannot turn an unchanged member into a change or merge two distinct members.
- [ ] The same collection diff remains understandable with raw GUIDs only and gains optional name annotations when feature 147 can resolve them.
- [ ] Empty, absent, and `null` collection states retain their existing distinct meanings and are not silently treated as equivalent.
- [ ] Whole or partial unknown collection transitions remain visibly unresolved and are not represented as definitive known additions or removals.
- [ ] Sensitive collection values remain protected under the existing sensitive-value policy, including in membership, duplicate, order, count, and drift presentation.
- [ ] Mixed primitive scalar types can use the compact row without conflating values of different Terraform types.
- [ ] Collections containing objects or nested collections retain their existing rendering semantics, with natural ordering applied to any visible numeric indices.
- [ ] Primitive collection drift produces one collection-drift entry per affected resource, not one entry per indexed element.
- [ ] If one resource has drift in multiple primitive collection paths, all those paths appear in that resource's single collection-drift entry and each path has its own meaningful diff.
- [ ] Two resources with collection drift receive separate resource-level collection-drift entries, even when their collection paths and transitions match.
- [ ] Scalar drift continues to group across resources according to feature 145.
- [ ] `--drift all`, omission of `--drift`, `--drift relevant`, and `--drift none` select or omit collection-drift entries consistently with feature 145, including existing no-op and suppression behavior.
- [ ] A compact collection contributes one to the resource's changed-attribute count; a collection-drift entry containing multiple changed collection paths reports the number of paths, not the number of member occurrences or source indices.
- [ ] Existing provider-specific tables retain their current semantic layouts and comparison behavior.

## Open Questions and Assumptions

- The issue calls the proposed update rendering a "set diff" but does not define behavior for Terraform lists, tuples, duplicates, or reorder-only changes. This specification assumes lists and tuples remain order-sensitive, sets remain order-insensitive, duplicate occurrences are preserved, and an unknown collection kind is treated as ordered. Maintainer approval of this specification confirms or revises that assumption.
