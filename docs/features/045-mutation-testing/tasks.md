# Tasks: Mutation Testing

## Overview

Implement mutation testing using Stryker.NET to validate test effectiveness for critical code paths (`Parsing/` and `MarkdownGeneration/Summaries/`). This includes local tooling, CI integration, and automated reporting via GitHub Issues.

Reference Specification: [specification.md](specification.md)
Reference Architecture: [architecture.md](architecture.md)
Test Plan: [test-plan.md](test-plan.md)

## Tasks

### Task 1: Tooling Setup and Compatibility Spike

**Priority:** High

**Description:**
Install Stryker.NET as a local tool and perform a compatibility spike with TUnit to ensure mutants can be killed successfully using the existing test runner.

**Acceptance Criteria:**
- [ ] `dotnet-stryker` is added to the local tool manifest (`dotnet-tools.json`).
- [ ] A minimal mutation run executes successfully against a single file in the `Parsing` directory.
- [ ] TUnit tests are correctly identified and executed by Stryker via `dotnet test`.
- [ ] At least one mutant is successfully "Killed".
- [ ] No major execution errors or framework incompatibilities are identified.

**Dependencies:** None

---

### Task 2: Scoped Configuration and Local Script

**Priority:** High

**Description:**
Configure Stryker to target the critical code paths and create a wrapper script to simplify local execution with scoping support.

**Acceptance Criteria:**
- [ ] `stryker-config.json` (or `.stryker-mutator.json`) is created with targets:
    - `Parsing/`
    - `MarkdownGeneration/Summaries/`
- [ ] Scoping is implemented via include/exclude filters.
- [ ] `scripts/mutation-test.sh` is created and supports `--target [Parsing|Summaries|All]`.
- [ ] The script correctly opens or points to the generated HTML report in `StrykerOutput/`.
- [ ] **TC-02** and **TC-04** pass.

**Dependencies:** Task 1

---

### Task 3: CI Workflow Integration

**Priority:** Medium

**Description:**
Create a GitHub Actions workflow for periodic and manual mutation testing runs.

**Acceptance Criteria:**
- [ ] `.github/workflows/mutation-testing.yml` is created.
- [ ] Triggered by `workflow_dispatch` (manual) and `schedule` (weekly).
- [ ] Workflow runs on `main` branch.
- [ ] Mutation results (HTML report) are uploaded as workflow artifacts.
- [ ] Job timeout is set to ~35 minutes (spec allows 30m run time).
- [ ] **TC-05** passes.

**Dependencies:** Task 2

---

### Task 4: Automated GitHub Issue Reporting

**Priority:** Medium

**Description:**
Implement logic to parse Stryker's JSON output and post a summary report as a new GitHub Issue for each run.

**Acceptance Criteria:**
- [ ] A script or workflow step parses `StrykerOutput/reports/mutation-report.json`.
- [ ] A new GitHub Issue is created for each run with title `Mutation Testing Report - [YYYY-MM-DD]`.
- [ ] The issue body includes:
    - Mutation score.
    - Summary table (Killed, Survived, Timeout).
    - Top 5 survived mutants (file and line).
    - Link/Instruction to download the full report from Actions.
- [ ] **TC-06** passes.

**Dependencies:** Task 3

---

### Task 5: Documentation and Baseline Establishment

**Priority:** Medium

**Description:**
Update the project's testing documentation and establish the initial mutation score baseline.

**Acceptance Criteria:**
- [ ] `docs/testing-strategy.md` is updated with a "Mutation Testing" section.
- [ ] Documentation includes purpose, local run instructions, and report interpretation.
- [ ] A full run is performed on all critical paths to establish the baseline.
- [ ] Combined mutation score is verified to be ≥ 75%.
- [ ] **TC-03** passes.

**Dependencies:** Task 4

---

### Task 6: Process Validation (Kill a Mutant)

**Priority:** Low

**Description:**
Demonstrate the effectiveness of the process by identifying a survived mutation and adding/improving tests to kill it.

**Acceptance Criteria:**
- [ ] At least one survived mutation is identified from the baseline run.
- [ ] A new TUnit test case is added to `src/tests/Oocx.TfPlan2Md.TUnit/` that covers the survival.
- [ ] Re-running mutation testing for that file shows the mutant as "Killed".
- [ ] **TC-07** passes.

**Dependencies:** Task 5

## Implementation Order

1. **Task 1: Tooling Setup** - Foundational requirement.
2. **Task 2: Local Script** - Enables local development and testing of the config.
3. **Task 3: CI Workflow** - Moves the process to the server.
4. **Task 4: Reporting** - Adds visibility to the CI runs.
5. **Task 5: Documentation** - Finalizes the feature for other contributors.
6. **Task 6: Validation** - Proves the end-to-end value.

## Open Questions

- None.
