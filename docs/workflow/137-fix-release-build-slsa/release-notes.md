# Fix SLSA provenance build step in release workflow

Internal workflow-only fix. No user-facing changes.

## 🐛 Bug fixes

- The SLSA provenance generation step in `release.yml` was failing because `generate-builder.sh` (inside the SLSA generator) only accepts tag refs (`refs/tags/vX.Y.Z`). The workflow was calling the reusable workflow with a commit SHA, causing a deterministic failure with exit code 27. Fixed by switching to the required tag ref `@v2.1.0` — the same pattern used by the OpenSSF Scorecard project itself.

## 🔗 Commits

- [`119060c1`](https://github.com/oocx/tfplan2md/commit/119060c1) fix: use tag ref for SLSA generator instead of SHA + compile-generator
