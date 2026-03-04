# Binary release builds fixed (NETSDK1207)

Build-only fix: the release pipeline's binary build jobs were failing for all targets (Linux, macOS, Windows). No user-facing output changes.

## 🐛 Bug fixes

- **Binary release builds failed with NETSDK1207**: `PublishAot=true` was propagating from the main `Oocx.TfPlan2Md` project into the `JsonEmbedGenerator` analyzer project reference during `dotnet publish`. Because `JsonEmbedGenerator` targets `netstandard2.0`, the .NET SDK rejected the AOT property with `NETSDK1207: Ahead-of-time compilation is not supported for the target framework`. All binary targets (linux-x64, linux-musl-x64, osx-x64, osx-arm64, win-x64) were blocked. The fix marks the analyzer project reference as AOT-isolated via `GlobalPropertiesToRemove` on the `ProjectReference` and adds `TreatAsLocalProperty="PublishAot"` + `<PublishAot>false</PublishAot>` in the generator project to prevent restore-time propagation.

## 🔗 Commits

- [`079df51a`](https://github.com/oocx/tfplan2md/commit/079df51a) fix: isolate AOT publish from JsonEmbedGenerator build
