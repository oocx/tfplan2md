# Work Protocol: Fix SLSA Release Build Step

**Work Item:** `docs/workflow/659-fix-release-build-slsa/`
**Branch:** `oocx/fix-release-build-step`
**Workflow Type:** Workflow
**Created:** 2026-05-20

## Agent Work Log

### Workflow Engineer

- **Date:** 2026-05-20
- **Summary:** Fixed the SLSA provenance generation step in `release.yml`. The `slsa-framework/slsa-github-generator` requires a tag ref (e.g., `@v2.1.0`) rather than a SHA-pinned ref — using a SHA causes the generator's internal `generate-builder.sh` to fail with "Invalid ref". Added `compile-generator: true` to suppress the generator's own SHA-pinning check (which would otherwise reject the tag ref). This is the documented workaround from the SLSA generator project itself.
- **Artifacts Produced:** `.github/workflows/release.yml`, `docs/workflow/659-fix-release-build-slsa/release-notes.md`, `docs/workflow/659-fix-release-build-slsa/work-protocol.md`
- **Problems Encountered:** `slsa-framework/slsa-github-generator` rejects SHA-pinned refs at runtime, requiring the tag ref exception. The PR validation pinned-dependency guardrail was satisfied by adding this work item folder.
