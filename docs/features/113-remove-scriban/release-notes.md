# Remove Scriban: Pure C# Rendering + NativeAOT Hardening

This release replaces the Scriban template engine with pure C# rendering internally. There are
no user-visible output changes — reports look exactly the same as before. The notable effects
are: zero third-party NuGet package dependencies, a smaller NativeAOT binary, and several
AzApi rendering fidelity improvements that came out of the migration.

## ✨ Features

- **Zero third-party runtime dependencies.** Scriban was the only third-party NuGet package
  in `tfplan2md`. It has been replaced entirely with pure C# rendering classes. The binary now
  has no external library dependencies at all.

- **Full reflection-free NativeAOT binary.** A new Roslyn source generator (`JsonEmbedGenerator`)
  embeds role definitions, AzApi documentation mappings, and icon rules as Brotli-compressed
  compile-time constants (`IlcDisableReflection=true` is now enabled). This improves trim
  safety and reduces the risk of NativeAOT publish surprises.

- **Smaller NativeAOT binary.** Removed the Scriban and `System.Xml.Linq` assembly references,
  reducing the published image size.

## 🐛 Bug fixes

- **AzApi resource rendering fidelity.** The migration from Scriban templates to C# renderers
  exposed several gaps in AzApi output:
  - Restored per-resource `Type`, API documentation link, `Body`, and `Output Values` sections
  - Fixed grouped body section rendering and sensitive-path masking for `azapi_resource` and
    `azapi_update_resource`
  - Fixed sensitive empty-container placeholders (e.g., `accessPolicies`) — rows were
    previously omitted
  - Fixed array table rendering: nested arrays (e.g., `siteConfig.appSettings`) now render as
    index-row matrices instead of per-item mini-tables
  - Fixed AzApi numeric value preservation (`12.0` is no longer normalized to `12`)
  - Fixed output-values ordering for grouped/sensitive output snapshots

- **Smaller Docker build context.** Added `.dockerignore` to exclude docs, test results, and
  generated artifacts from the build context, reducing it from ~3.39 GB to ~78 KB.

## 🔗 Commits

- [`6f517161`](https://github.com/oocx/tfplan2md/commit/6f517161) refactor: remove Scriban and migrate to pure C# rendering
- [`424753ab`](https://github.com/oocx/tfplan2md/commit/424753ab) feat: implement azapi-specific C# rendering pipeline for fix #1
- [`eb1ac573`](https://github.com/oocx/tfplan2md/commit/eb1ac573) fix: finalize azapi output values snapshot parity
- [`7864eb88`](https://github.com/oocx/tfplan2md/commit/7864eb88) fix: implement snapshot parity fixes 12, 4, and 5
- [`9df5978c`](https://github.com/oocx/tfplan2md/commit/9df5978c) fix: implement snapshot parity fixes 11 6 7 8
- [`2a285718`](https://github.com/oocx/tfplan2md/commit/2a285718) feat: finalize reflection-free embedded json generator integration
- [`724271ea`](https://github.com/oocx/tfplan2md/commit/724271ea) fix: drop System.Xml.Linq to reduce NativeAOT size
- [`d8cc7d68`](https://github.com/oocx/tfplan2md/commit/d8cc7d68) chore: add .dockerignore to exclude unnecessary files from image build context
