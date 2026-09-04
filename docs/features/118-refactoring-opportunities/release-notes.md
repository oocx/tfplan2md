# Internal Architecture Refactoring: Report Pipeline and Provider Model

This release is an internal code quality improvement. There are no changes to CLI behavior,
rendered Markdown output, or user-facing functionality. If your pipelines work today, they will
continue to work the same way after upgrading.

## ✨ Features

- **Explicit report-generation pipeline** — `ReportModelBuilder` has been decomposed into five
  explicit pipeline stages (`ResourceChangeStage`, `AttributeFilteringStage`,
  `SummaryEnrichmentStage`, `DisplayFilteringStage`, `ReportAssemblyStage`). Each stage has a
  well-defined input/output contract and can be unit-tested independently.

- **Narrower provider contribution model** — The broad `IProviderModule` contract has been
  replaced by a narrower `IProvider` interface with six optional capability interfaces
  (`IResourceFactory`, `IValueFormatter`, `IResourceRenderer`, `IGroupNamingConvention`,
  `IAttributeFilter`, `IDetailsSectionProvider`). Registered via `ProviderContributionSet`,
  providers now declare only the capabilities they implement. Adding a new provider capability
  no longer requires editing all existing providers.

- **Instance-based role definition resolution** — The static mutable `AzureRoleDefinitionMapper`
  has been replaced by `IRoleDefinitionResolver` / `AzureRoleDefinitionResolver`, scoped per
  application run. Custom role definitions loaded from a plan no longer bleed across multiple
  in-process runs, improving correctness in multi-call scenarios.

- **Diagnostics decomposition** — Diagnostic collection has been split into an append-only
  `IDiagnosticSink`, an immutable `DiagnosticReport` snapshot, and a dedicated
  `DiagnosticMarkdownFormatter`. Producers now append to the sink; reporting is assembled from
  the snapshot, keeping the two concerns separate.

- **AzApi render-planning separation** — Policy decisions (what to render) have been extracted
  into `AzApiBodyRenderPlanner` / `AzApiBodyRenderPlans`, leaving `AzApiBodyRenderer` focused
  on layout and emission. Similarly, `DefaultResourceRenderPolicy` now holds the scenario
  detection heuristics for the default renderer.

## 🔗 Commits

- [`e3e3a14`](https://github.com/oocx/tfplan2md/commit/e3e3a14d) feat: complete feature 110 refactoring work
- [`2a7ab3d`](https://github.com/oocx/tfplan2md/commit/2a7ab3dd) refactor: complete feature 110 review rework for tasks 7-9
