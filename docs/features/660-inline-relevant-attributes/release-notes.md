# Inline relevant attributes

tfplan2md now renders Terraform plan context inline on the affected resource cards instead of isolating `relevant_attributes[]` at the bottom of the report. This makes replacement cascades and upstream dependencies visible where reviewers make decisions.

## ✨ Features

- Replaced or destroyed resources now show inline `⚠️ Forced replacement` callouts when a `replace_paths` entry traces back to an upstream relevant attribute.
- Replaced or destroyed resources also show inline `🔗 Depends on:` / `🔗 Also depends on:` context for correlated upstream inputs.
- Upstream inputs that are themselves changing in the same plan are highlighted as **changing in this plan**.
- Uncorrelated plan inputs are preserved in a collapsible `🔗 Other plan inputs` section near the end of the report.

## 📚 Documentation

- Updated `docs/features.md` to describe the new inline relevant-attribute annotations and fallback details block.

## 📸 Screenshots

### Inline annotations
<!-- release-screenshot: target-resource-id="example_resource.api"; focus="Shows forced replacement and dependency context rendered directly inside the affected resource card" -->
![Inline annotations](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/660-inline-relevant-attributes/feature-660-inline-annotations.png)

### Fallback inputs
<!-- release-screenshot: selector="details:has(summary:has-text('Other plan inputs'))"; focus="Shows uncorrelated relevant attributes preserved in the fallback details section" -->
![Fallback inputs](https://raw.githubusercontent.com/oocx/tfplan2md/v{VERSION}/docs/features/660-inline-relevant-attributes/feature-660-fallback-inputs.png)

## 🔗 Commits

- [`1dc93917`](https://github.com/oocx/tfplan2md/commit/1dc93917) feat: implement inline relevant attributes (Tasks 1-7, partial - build fixes in progress)

## 🚨 Breaking changes

None.
