# OpenSSF Score Improvements

Implemented the top 5 changes to improve the repository's OpenSSF Scorecard score:

1. **Pinned GitHub Actions dependencies** — All 43 `uses:` references across 8 workflow files are now pinned to immutable SHA hashes instead of mutable version tags, preventing supply-chain attacks.

2. **Added CodeQL SAST workflow** — A new `codeql.yml` workflow runs C# static analysis security scanning on every push, pull request, and weekly schedule, satisfying the OpenSSF `SAST` check.

3. **Added SLSA build provenance attestation** — Release binaries now include SLSA Level 2 provenance attestations via `actions/attest-build-provenance`, making artifact integrity verifiable.

4. **Tightened workflow token permissions** — All workflows now declare minimum-required `permissions:` at the job level rather than the workflow level, following the principle of least privilege.

5. **Added `.github/CODEOWNERS`** — Code ownership is now declared, enabling automatic review requests and satisfying the code-owner requirement for branch protection.
