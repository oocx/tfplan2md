# Fix SLSA provenance build step in release workflow

Internal workflow-only fix. No user-facing changes.

## 🐛 Bug fixes

- The SLSA provenance generation step in `release.yml` was broken due to the use of a SHA-pinned ref for `slsa-framework/slsa-github-generator`. The generator's own `generate-builder.sh` script rejects SHA refs and requires a tag ref (e.g., `v2.1.0`). Switched to the tag ref and added the `compile-generator: true` option to suppress the SHA-pinning check inside the SLSA generator itself.

## 🔗 Commits

- [`91544a0f`](https://github.com/oocx/tfplan2md/commit/91544a0f) fix: use compile-generator for SLSA provenance step
- [`119060c1`](https://github.com/oocx/tfplan2md/commit/119060c1) fix: use tag ref for SLSA generator instead of SHA + compile-generator
