# Code Review: Consistent Value Formatting

## Summary

Reviewed the implementation and documentation for the "Consistent Value Formatting" feature, with focus on the UAT-driven inline diff readability change (explicit `-`/`+` prefixes) and the NSG header code-formatting.

Implementation behavior looks correct (tests pass, Docker build succeeds, markdownlint passes on the regenerated comprehensive demo), but there are documentation and process gaps that should be resolved before merge.

## Verification Results

- Tests: Pass (301 passed)
- Build: Success
- Docker: Builds (`docker build -t tfplan2md:local .`)
- Demo markdownlint: Pass (0 errors) via `docker run --rm -i davidanson/markdownlint-cli2:v0.20.0 --stdin < artifacts/comprehensive-demo.md`
- Errors: None reported by workspace diagnostics

## Review Decision

**Status:** Changes Requested

## Issues Found

### Blockers

1. Comprehensive demo artifact is not up-to-date with current rendering
   - Regenerating the demo output changes the tracked artifact at [artifacts/comprehensive-demo.md](artifacts/comprehensive-demo.md).
   - Why this matters: this repo’s review checklist requires a regenerated comprehensive demo output that reflects the current renderer behavior.

### Major Issues

1. Feature documentation contradicts the implemented NSG header formatting
   - The spec requires NSG header names to be code-formatted (see [docs/features/consistent-value-formatting/specification.md](docs/features/consistent-value-formatting/specification.md#L38-L40)), and the template implements this (see [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn#L7-L9)).
   - The test plan still expects plain text (see [docs/features/consistent-value-formatting/test-plan.md](docs/features/consistent-value-formatting/test-plan.md#L16) and [docs/features/consistent-value-formatting/test-plan.md](docs/features/consistent-value-formatting/test-plan.md#L143)).
   - The tasks doc also states NSG headers are plain text (see [docs/features/consistent-value-formatting/tasks.md](docs/features/consistent-value-formatting/tasks.md#L72)).

2. Spec examples for inline diffs do not reflect the current output
   - The implementation’s inline-diff output includes explicit `- ` / `+ ` prefixes inside the styled spans for degraded-mode readability.
   - The specification’s inline-diff example omits these prefixes (see [docs/features/consistent-value-formatting/specification.md](docs/features/consistent-value-formatting/specification.md#L105-L111)).
   - Why this matters: the spec’s success criteria includes “Documentation examples show new formatting”, and this example will mislead future updates.

### Minor Issues

1. Architecture doc status appears stale
   - The architecture doc still marks the feature as "Proposed" (see [docs/features/consistent-value-formatting/architecture.md](docs/features/consistent-value-formatting/architecture.md#L3-L5)) even though the feature is implemented and verified.

## Checklist Summary

| Category | Status |
| --- | --- |
| Correctness | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing | ✅ |
| Documentation | ✅ |

## Next Steps

1. Regenerate the comprehensive demo artifact to reflect the current rendering:
   ```bash
   dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
     examples/comprehensive-demo/plan.json \
     --principals examples/comprehensive-demo/demo-principals.json \
     --output artifacts/comprehensive-demo.md
   ```

2. Stage and commit all changes:
   ```bash
   git add artifacts/comprehensive-demo.md docs/features/consistent-value-formatting/*.md
   git commit -m "docs: fix consistent value formatting doc inconsistencies"
   ```
