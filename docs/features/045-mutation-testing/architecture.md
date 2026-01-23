# Architecture: Mutation Testing

## Status

Proposed

## Context

This feature adds **mutation testing** to validate that the existing TUnit test suite actually catches defects, not just executes lines.

- Feature spec: `docs/features/045-mutation-testing/specification.md`
- Testing strategy: `docs/testing-strategy.md` (TUnit on Microsoft.Testing.Platform)

Key constraints from the specification:

- Focus mutation testing on **critical paths** initially:
  - `Parsing/`
  - `MarkdownGeneration/Summaries/`
- Provide a **local developer workflow** (`scripts/mutation-test.sh`) with optional scoping (e.g., `--target Parsing`).
- CI integration must be **periodic** (weekly/monthly) or manually dispatched.
  - Explicitly **not** part of PR validation.
- Store mutation reports as CI artifacts.
- Post results as GitHub Issues for tracking and discussion.
- Performance tolerance: **≤ 30 minutes** for mutation testing runs.
- Start with **reporting only** (do not block merges on mutation score initially).

## Options Considered

### Option 1: Stryker.NET (dotnet tool) targeting production project(s)

Use Stryker.NET as the mutation test runner:
- instrument production code
- run tests via `dotnet test` (TUnit project)
- generate HTML reports under `StrykerOutput/`

**Pros**
- De-facto standard for .NET mutation testing; active ecosystem and report formats
- Good UX and reporting (HTML + machine-readable outputs)
- Supports scoping via include/exclude filters (critical paths first)
- Compatible with a periodic GitHub Actions workflow and artifact upload

**Cons / Risks**
- **TUnit compatibility risk**: Stryker traditionally documents xUnit/NUnit/MSTest flows; while TUnit runs via `dotnet test`, Stryker’s integration assumptions may not perfectly match Microsoft.Testing.Platform.
- Runtime cost can be significant (especially if the test suite is large or if mutants trigger expensive paths)
- Some survived mutants are not meaningful (tests shouldn’t necessarily assert every internal branch)

### Option 2: VisualMutator / legacy GUI mutation tools

Use older mutation testing tooling (primarily Windows GUI-oriented).

**Pros**
- Can be useful for interactive investigation on Windows

**Cons**
- Not aligned with the repo’s CI-first, script-driven workflow
- Typically unmaintained or behind current .NET versions
- Poor CI integration compared to modern CLI tools

### Option 3: Custom “mutation harness” (bespoke)

Build a custom solution that performs source transforms and drives `dotnet test`.

**Pros**
- Full control over mutations and integration
- Can be tailored specifically for TUnit

**Cons**
- High implementation/maintenance cost and a large surface area for bugs
- Reinvents established tooling; unlikely to match Stryker’s maturity

## Decision

Adopt **Option 1 (Stryker.NET)** with an explicit **compatibility validation step** for TUnit, and start with **reporting-only** in CI.

If Stryker.NET proves incompatible with TUnit/Microsoft.Testing.Platform in practice, the fallback is to re-evaluate tools (or a minimal custom harness), but only after confirming the incompatibility is not solvable via configuration.

## Rationale

- The feature’s primary goal is to improve **test effectiveness visibility**, not to introduce a new testing framework.
- Stryker.NET provides the best balance of maturity, documentation, and reporting compared to alternatives.
- Keeping mutation testing out of PR validation preserves the repo’s fast PR feedback loop while still enabling periodic quality discovery.

## Consequences

### Positive

- Maintainers get actionable signals about weak assertions and untested branches in critical logic.
- Contributors can iteratively improve tests with concrete “survived mutation” locations.
- CI remains fast for PRs while still producing regular quality telemetry.

### Negative / Risks

- Mutation runs may be slow and/or flaky without careful timeouts and scoping.
- A “mutation score” can incentivize counterproductive tests unless positioned as a guide, not a KPI.
- TUnit integration may require experimentation; if Stryker cannot drive TUnit reliably, this feature must pivot.

## Implementation Notes

High-level guidance for the Developer agent (no implementation in this document).

### 1) Tooling and configuration

- Install/run Stryker as a .NET tool (prefer local tool manifest if consistent with repo conventions).
- Store Stryker configuration in-repo (e.g., `stryker-config.json` or `.stryker-mutator.json`).
- Ensure the configuration:
  - Targets the production project(s) under `src/`.
  - Executes the TUnit test project under `src/tests/Oocx.TfPlan2Md.TUnit/` via `dotnet test`.

**Critical requirement:** validate that Stryker can run tests successfully via the same `dotnet test` approach documented in `docs/testing-strategy.md`.

### 2) Scope control (critical paths first)

Start with mutation scope limited to:
- `Parsing/`
- `MarkdownGeneration/Summaries/`

Recommended approach:
- Provide named “targets” in `scripts/mutation-test.sh` that map to Stryker include filters.
- Keep exclusions explicit and minimal; the purpose is to illuminate weak tests, not to curate the score.

### 3) Runtime and timeouts

- Configure global run time budget to align with the spec’s **≤ 30 minute** tolerance.
- Ensure individual mutant timeouts are enforced to prevent infinite loops/hangs.
- Prefer a CI job-level timeout plus tool-level mutant/test timeouts.

### 4) Reporting outputs

Generate and persist:
- Human-readable HTML report (Stryker default)
- A machine-readable output (JSON) to enable CI summarization

Upload `StrykerOutput/` as a GitHub Actions artifact for drill-down.

### 5) CI workflow integration (periodic + manual)

Add a dedicated workflow (e.g., `.github/workflows/mutation-testing.yml`) that:
- Triggers on `workflow_dispatch` and `schedule` (weekly recommended initially)
- Runs on `main` only
- Enforces a job timeout around ~35 minutes (buffer over the 30 minute budget)
- Uploads mutation output artifacts

### 6) Issue-based tracking

Post results as GitHub Issues for visibility and historical tracking.

Two viable patterns:

**A) Rolling issue + comment per run (recommended)**
- Maintain a single open issue (e.g., “Mutation testing: latest results”).
- Each scheduled run adds a new comment with:
  - mutation score
  - counts for killed/survived/timeout
  - a short list of top survived mutants
  - a link/instructions to download the artifact

**B) New issue per run**
- Create a dated issue each run.

Recommendation: **A** to avoid issue spam while preserving history.

### 7) Thresholds and enforcement

- Establish an initial baseline target of **≥ 75% mutation score** for the scoped critical paths.
- Start with **reporting-only** in CI (do not fail the workflow solely due to a drop).
- If/when the process stabilizes, consider gating only on:
  - catastrophic regression (e.g., score drops below a much lower “floor”), or
  - an explicit maintainer-triggered “enforce” mode.

### 8) Documentation updates

Update documentation to ensure the workflow is adoptable:
- Add a short “Mutation testing” section to `docs/testing-strategy.md` describing:
  - intent (how it differs from coverage)
  - local run command(s)
  - how to interpret survived mutants
  - expected runtime

## Open Questions (for follow-up)

- If Stryker cannot drive TUnit reliably, should we:
  - change only the mutation-testing execution harness, or
  - reconsider the tool entirely?

(Answer after the first compatibility spike.)
