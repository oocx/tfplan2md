# Fix Release: Linux x64 NativeAOT build linker prerequisite

Fix release for Linux x64 binary packaging in the release workflow.

## 🐛 Bug fixes

- **Fixed Linux x64 release binary build failure (Issue #463)** by installing the missing NativeAOT linker prerequisite (`clang`) in the Linux build job before `dotnet publish -p:PublishAot=true`.

## 🔗 Commits

- [`db5d107`](https://github.com/oocx/tfplan2md/commit/db5d107) fix: install clang for linux x64 NativeAOT publish
