# Architecture: Code Coverage Reporting and Enforcement in CI

## Status

Proposed

## Context

This feature adds automated **code coverage collection, reporting, and enforcement** to the CI pipeline.

- Feature spec: `docs/features/043-code-coverage-ci/specification.md`
- CI context: GitHub Actions workflows in `.github/workflows/`
  - `pr-validation.yml` currently runs `dotnet test` (TUnit) and publishes TRX results.
  - `ci.yml` intentionally does **not** run tests (versioning-only after merge).

Key constraints from the specification:

- Collect coverage on **every PR**.
- Track **line** and **branch** coverage.
- Enforce **repository-wide** thresholds as a required PR check.
- Show a coverage summary directly in the PR experience with an optional deep-link.
- Maintain **maintainer discretion** to override coverage failures.
- Add < 2 minutes to PR validation.
- Coverage scope applies to all production code in `src/` (no additional exclusions).

## Options Considered

### Option 1: Codecov-managed end-to-end

Generate coverage in CI and let Codecov provide:
- PR comment + status checks
- badge
- hosted HTML reports
- historical graphs

**Pros**
- Best-in-class PR UX (comment, check details, graphs)
- Trivial badge + trend tracking
- Minimal custom logic to maintain

**Cons**
- External service dependency (availability, policy, account configuration)
- “Maintainer override” is less explicit unless relying on GitHub admin bypass or Codecov-specific mechanisms
- Requires careful handling of secrets for private repos and fork PRs
- Not compatible with the current constraint “free services only” while the repository is private (Codecov’s free tier is typically limited to public/open-source repositories)

### Option 2: GitHub Actions native end-to-end (no external services)

Generate coverage in CI, parse results in-workflow, and:
- fail the job if thresholds regress
- post a PR comment or check-run summary
- store reports as workflow artifacts
- publish trend data via GitHub Pages or a dedicated branch

**Pros**
- Full control over enforcement and override behavior
- No third-party dependency
- Clear, auditable “override” path (e.g., label-based)

**Cons**
- Requires building and maintaining reporting + trend plumbing
- Coverage “badge” becomes non-trivial (needs Pages + shields, or a custom endpoint)

### Option 3: Hybrid — GitHub Actions enforces, optional Codecov for badge/trends (future)

Generate coverage once during PR validation, then:
- enforce thresholds in GitHub Actions (deterministic, overrideable)
- publish a PR-visible summary (job summary and/or comment)
- upload reports as workflow artifacts
- also upload the same report to Codecov to get badge + historical trend dashboard

**Pros**
- Meets all feature requirements (especially override + PR visibility + trends)
- Keeps enforcement logic in-repo (predictable and testable)
- Codecov becomes “value add” (badge/trends) rather than the single enforcement gate

**Cons**
- Still introduces an optional external integration
- Slightly more setup than Option 1
- Not viable until the repository is public (or a paid/private Codecov plan is acceptable)

## Decision

Adopt **Option 2 (GitHub Actions native end-to-end)** for now, with an explicit path to Option 3 once the repository is public:

- Use a **single test run** in `pr-validation.yml` that produces TRX results **and** a Cobertura coverage report.
- Enforce line and branch thresholds in GitHub Actions as a required check.
- Provide PR-visible coverage summary via GitHub job summary and/or a PR comment.
- Upload coverage artifacts for drill-down.
- Generate (and optionally commit) a coverage badge SVG and a lightweight historical trend dataset derived from the PR validation run, so “badge” and “trends” remain free while the repo is private.

Decision detail:

- Badge and trend data are **generated from PR validation coverage** and **committed as part of the PR** (CI may update files in the PR branch).

## Rationale

- The project already centralizes quality gates in PR validation and keeps the post-merge workflow fast; coverage fits naturally into `pr-validation.yml`.
- The repository’s `ci.yml` is intentionally versioning-only after merge; coverage should remain a PR-only quality gate to avoid redundant main-branch test execution.
- A label-based override mechanism is straightforward in GitHub Actions and matches the requirement “maintainer discretion”.
- External “free-for-open-source” services are not available while the repo is private; GitHub-native publishing keeps the system free and self-contained.
- Running coverage in the same test invocation minimizes pipeline duration impact.

## Consequences

### Positive

- Coverage regressions are caught before merge with clear, local enforcement.
- Maintainers can explicitly override failures in a documented, auditable way.
- Contributors get immediate PR feedback (status + summary).
- Trend tracking and badge are available without building a custom dashboard.

### Negative / Risks

- External service dependency for trends/badge: outages should not block merges if enforcement is handled locally.
- Fork PRs require careful security posture (avoid exposing tokens).
- “Coverage applies to all production code” can be undermined unintentionally if tooling excludes assemblies or code by attribute.

## Implementation Notes

High-level guidance for the Developer agent (no implementation in this document):

### Coverage collection mechanism

Preferred: use **Microsoft Testing Platform / TUnit coverage** to emit **Cobertura** during the existing test run.

- Goal: one `dotnet test` invocation that produces:
  - `./src/TestResults/*.trx`
  - a Cobertura XML file (single merged report if feasible)

This avoids adding a second test run and helps meet the “< 2 minutes” requirement.

We intentionally do **not** run coverage on `main` after merge. The PR validation run is considered authoritative for the commit that will land on `main` (given the repo’s merge policy and “tests only in PR validation” optimization).

### Threshold enforcement

- Parse the Cobertura report to compute:
  - repository-wide line coverage
  - repository-wide branch coverage
- Fail the workflow when either metric is below threshold.
- Thresholds should be set initially to **current measured coverage on `main`** (not arbitrary).
  - Recommend setting the initial threshold to the measured value, optionally minus a small tolerance (e.g., 0.1–0.5 percentage points) to avoid flaky rounding regressions.

### Maintainer override

Implement an explicit override mechanism:

- Maintain a PR label such as `coverage-override`.
- If the label is present, the “coverage enforcement” step should pass while still publishing the coverage summary.
- The workflow should include a clear message in the check output when an override is active.

This is more auditable and reliable than relying on GitHub admin bypass.

### PR visibility

Provide coverage in at least one of:

- GitHub job summary (`GITHUB_STEP_SUMMARY`) including both metrics and thresholds.
- A PR comment (updated-in-place) with a compact summary and a link to artifacts/report.

### Detailed report link

- Upload HTML coverage report as a GitHub Actions artifact.
- If/when a hosted service is enabled in the future (e.g., Codecov after making the repo public), link to the hosted report in the PR summary/comment.

### Historical trends + badge (free, private-repo compatible)

Implement trends and the README badge using only GitHub-hosted artifacts and repository content:

- **Badge**: generate an `assets/coverage-badge.svg` (or similar) from the PR validation coverage result.
  - If a numeric badge is required in `README.md`, commit the SVG as part of the PR so the merged result reflects the same commit that was measured.
- **Trends**: append the coverage metrics to a versioned file (e.g., `docs/coverage/history.json` or `docs/coverage/history.csv`) as part of the PR.
  - This makes the trendline advance per merged PR without needing a post-merge workflow.

Notes:

- This approach avoids redundant work on `main`, but PRs may include auto-generated updates to history/badge files.

Security guidance:

- For fork PRs, do not use secret tokens.
- Prefer tokenless uploads for public repos, or restrict uploads to same-repo PRs.

Future enhancement (once repo is public): adopt Option 3 and upload to Codecov to gain richer per-PR UI and hosted dashboards.

### Scope: “no exclusions”

To align with the specification:

- Do not configure tool-level exclusions (no exclude globs, no excluded assemblies) beyond unavoidable system defaults.
- Keep coverage scope aligned with “all production code under `src/`”, i.e., all non-test projects.

Additionally, remove existing `[ExcludeFromCodeCoverage]` annotations from production code under `src/` (e.g., `src/tools/...`) so that the measured coverage reflects all production code.

## Notes

This feature’s requirement “coverage applied to all production code in `src/` without exclusions” is interpreted strictly: code-level exclusions via `[ExcludeFromCodeCoverage]` are considered non-compliant and should be removed.
