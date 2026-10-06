# Feature 146 UAT Render Variants

The [default UAT report](uat-plan.md) is rendered from [uat-plan.json](uat-plan.json)
with the CLI's default Azure DevOps target. Run the same plan on all three supported
targets and save comparison files under the repository scratch directory:

```bash
scripts/setup-tmp.sh
mkdir -p .tmp/feature-146-uat

for target in github azuredevops bitbucket; do
  tfplan2md --render-target "$target" docs/features/146-generic-resource-review-clarity/uat-plan.json > ".tmp/feature-146-uat/$target-default.md"
  tfplan2md --render-target "$target" --show-unchanged-values docs/features/146-generic-resource-review-clarity/uat-plan.json > ".tmp/feature-146-uat/$target-show-unchanged.md"
  tfplan2md --render-target "$target" --hide-unchanged-outputs docs/features/146-generic-resource-review-clarity/uat-plan.json > ".tmp/feature-146-uat/$target-hide-unchanged-outputs.md"
  tfplan2md --render-target "$target" --summary-name-attributes body.title,body.properties.title docs/features/146-generic-resource-review-clarity/uat-plan.json > ".tmp/feature-146-uat/$target-summary-overrides.md"
  tfplan2md --render-target "$target" --show-unchanged-values --hide-unchanged-outputs --summary-name-attributes body.title,body.properties.title docs/features/146-generic-resource-review-clarity/uat-plan.json > ".tmp/feature-146-uat/$target-combined-options.md"
  tfplan2md --render-target "$target" --show-sensitive docs/features/146-generic-resource-review-clarity/uat-plan.json > ".tmp/feature-146-uat/$target-show-sensitive.md"
  tfplan2md --render-target "$target" docs/features/146-generic-resource-review-clarity/uat-plan-root-noop.json > ".tmp/feature-146-uat/$target-root-noop-default.md"
  tfplan2md --render-target "$target" --hide-unchanged-outputs docs/features/146-generic-resource-review-clarity/uat-plan-root-noop.json > ".tmp/feature-146-uat/$target-root-noop-filtered.md"
done
```

Inspect the default file and each target variant for escaped fallback names,
preserved indexed labels, safe sensitivity/unknown handling, stable full-address
refactoring labels, import-note placement, accessible collapsed settings, primary
setting counts, and output selection. The summary override applies to generic
fallback naming; mapped provider summaries should keep their identifying content.
The `show-sensitive` variant is only for an authorized review and should reveal the
fixture's synthetic sensitive values. The root-only-no-op plan should lose its entire
Outputs section only when output filtering is enabled.

These commands prepare local review artifacts. They do not constitute visual UAT on
GitHub, Azure DevOps, or Bitbucket; the UAT Tester records platform evidence later.
