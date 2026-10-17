# Release Notes: Compact Collection Diffs

Lists of plain values (strings, numbers, booleans) in a Terraform plan used to
render as one table row per index. Insert one item near the front of a 13-item
list and every later row looked changed. This release renders each such list as
one attribute with a Before and an After cell, and marks only the occurrences
that actually changed.

## ✨ Features

- **One row per collection.** A collection whose elements are all primitive values
  is one attribute row for create, update, replace and delete. It counts as one
  changed attribute in the resource summary, however many elements changed.
- **Added and removed occurrences are marked** with ➕ and ➖. Duplicates keep their
  multiplicity: removing one of two equal values is one removal.
- **Order changes are shown.** plan JSON carries no schema, so every primitive
  collection is treated as ordered, and a reorder-only change is reported
  (moved entries are marked 🔀). Terraform emits set elements in canonical order, so
  sets do not produce order-only noise.
- **Numeric index ordering.** Wherever indexed paths remain visible (for example in
  collections of objects), index 2 now sorts before index 10.
- **Drift grouped per resource.** Collection drift is one entry per affected resource
  listing every changed collection path, not one entry per shifted index.
  `--drift all|relevant|none` behaves as before.
- **Sensitive values stay protected.** Without `--show-sensitive`, only the entries
  Terraform marks as sensitive are masked; the other entries are still shown. A
  masked entry can carry a change marker, and is only ever compared with other
  masked entries, so the report reveals no value and no match with a visible value.
- **Unknown and element-less states.** Unknown values stay visibly unknown. An
  absent, `null` or empty collection on both sides is unchanged and produces no
  row, as before, but the states stay distinguishable when a row is shown (for
  example with `--show-unchanged-values`).
- **Case-only Azure ID filter** (`--ignore-azure-id-case-changes`) applies to whole
  collections: a collection whose only change is ID letter case is hidden; with
  any other change it is shown.
- **Large collections** render as labelled Before and After blocks, and the summary
  counts one change per collection, including unknown ones.
- **Ambiguous strings are quoted** and adjacent index brackets are escaped, so
  `a` and `"a"`, or empty strings, can be told apart.

Provider-specific tables (for example NSG rules, firewall rules) and collections
of objects keep their existing rendering.

## 🐛 Fixes

- Empty, `null` and absent collections on both sides no longer produce a row or drift
  entry, matching the previous per-index behavior (found in code review).

## 📚 Docs

- Specification, architecture, test plan, `docs/features.md`,
  `docs/report-style-guide.md` and `docs/testing-strategy.md` describe the behavior.
- Workflow: the code review no longer asks for CI evidence. CI is verified by the
  Release Manager on the pull request, which the reviewer can never see.

## ▶️ Getting started

No new options. Upgrade and run as usual:

```bash
terraform show -json plan.tfplan | docker run -i oocx/tfplan2md:latest > plan.md
```

## 📸 Screenshots

One item inserted near the start of a 13-item list: one row, one ➕ marker, no
index-shift cascade.

<!-- release-screenshot: selector="details:has(summary:has-text('insert')) table"; focus="Shows a single list attribute with one inserted item marked instead of one row per index" -->
![Compact collection diff with one inserted item](https://raw.githubusercontent.com/oocx/tfplan2md/v1.48.0/docs/features/148-compact-collection-diffs/compact-collection-insert.png)

## 🔗 Commits

- feat: compact primitive collections into one model row with unknown and sensitive handling
- feat: add type-accurate collection element diff
- feat: order attribute paths by numeric index
- feat: render compact collection rows with markers and large-value support
- feat: evaluate attribute filters element-wise for collection rows
- feat: group collection drift per resource
- feat: render collection drift entries per resource and verify drift modes
- feat: quote ambiguous strings, escape adjacent index brackets
- feat: mask sensitive collection entries per side with opaque comparison tokens
- feat: render masked collection entries as placeholders with change markers
- feat: show masked collection entries as placeholders in collection drift
- fix: hide element-less collection rows and quote only ambiguous strings
- fix: treat sensitive empty collections as unchanged
- fix: keep collection rows with masked changes or order changes out of the case-only filter
- fix: show null and absent sides of large collections
- fix: count unknown collections as changes in large-value summary
