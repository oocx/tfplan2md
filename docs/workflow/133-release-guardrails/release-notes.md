# Release workflow guardrails

This workflow update closes two recurring release-quality gaps in GitHub cloud agent runs: skipped Release Manager handoffs and unfocused release-note screenshots.

## 🐛 Bug fixes

- PR validation now fails when a workflow/product change does not update a work item with both `release-notes.md` and `work-protocol.md`.
- PR validation now requires a `Release Manager` entry in each changed work item's `work-protocol.md`, so skipped release handoffs are caught before merge.
- Release-note screenshots now require explicit targeting metadata (`selector=` or `target-resource-id=` plus `focus=`), plus valid raw GitHub image URLs that resolve to real PNG files in the repository.

## 📚 Documentation

- Documented the new release-artifact and screenshot-metadata guardrails in the workflow docs and release-notes template.

## 🔗 Commits

- Workflow-only change; Release Manager should reference the finalized workflow commit when preparing the PR/release.
