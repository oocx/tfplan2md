# Issue Closure Report

This document contains the analysis of open issues and identifies which ones have been fixed.

## Issues Ready to Close (Already Fixed)

### Issue #374: [Workflow]: only create new releases when the actual docker file changes

**Status:** ✅ FIXED by PR #377 (merged 2026-01-29)

**What was implemented:**
The CI workflow now only creates a new version tag when the published Docker image would change (Release Gating). This includes changes to runtime code (`src/` excluding test directories), example files (`examples/`), and Docker build configuration.

Test-only changes and workflow/internal-tooling changes (in `.github/`, `scripts/`, `docs/`, `website/`) intentionally do not trigger releases.

**Evidence:**
- PR #377: https://github.com/oocx/tfplan2md/pull/377
- `docs/spec.md` line 84: *"Release Gating: The CI workflow only creates a new version tag when the published Docker image would change."*

**Suggested comment to add before closing:**
```
This issue was fixed by PR #377 (merged on 2026-01-29).

The CI workflow now implements Release Gating: it only creates a new version tag when the published Docker image would change. This includes changes to runtime code (`src/` excluding test directories), example files (`examples/`), and Docker build configuration.

Workflow/internal-tooling changes (`.github/`, `scripts/`, `docs/`, `website/`) intentionally do not trigger releases.

Evidence: See `docs/spec.md` line 84.

Closing as completed.
```

---

### Issue #375: [Workflow]: workflow changes must never increase versions

**Status:** ✅ FIXED by PR #377 (merged 2026-01-29) and commit dd9b742 (2026-02-08)

**What was implemented:**
1. PR Validation guardrail: PRs that only change internal/workflow/tooling paths are blocked from using version-bumping commit types (`feat:`, `fix:`, `perf:` or `BREAKING CHANGE:`).
2. Commit Guardrails documented in `docs/spec.md` requiring workflow-only PRs to use non-version-bumping types like `workflow:`, `docs:`, `chore:`, or `ci:`.
3. Agent instructions updated to enforce this rule.

**Evidence:**
- PR #377: https://github.com/oocx/tfplan2md/pull/377
- Commit dd9b742: https://github.com/oocx/tfplan2md/commit/dd9b742b3865cd1dc578f3213253dee3db613b9b
- `docs/spec.md` line 86: *"Commit Guardrails: Pull requests that only change workflow/internal tooling (e.g., `.github/`, `scripts/`, `docs/`, `website/`) must not use version-bumping Conventional Commit types such as `feat:` or `fix:`. Use `workflow:`, `docs:`, `chore:`, or `ci:` instead."*

**Suggested comment to add before closing:**
```
This issue was fixed by PR #377 (merged on 2026-01-29) and commit dd9b742 (2026-02-08).

The solution includes:
1. PR Validation guardrail that blocks version-bumping commit types for workflow-only changes
2. Commit Guardrails documented in docs/spec.md (line 86)
3. Agent instructions updated to enforce this rule

Workflow changes now correctly use `workflow:`, `docs:`, `chore:`, or `ci:` commit types instead of `feat:` or `fix:`, preventing unintended version bumps.

Closing as completed.
```

---

### Issue #326: Add Code Coverage Reporting and Enforcement to CI

**Status:** ✅ FIXED by PR #334 (merged 2026-01-21)

**What was implemented:**
- Integrated code coverage collection in PR validation workflow
- Added `Oocx.TfPlan2Md.CoverageEnforcer` tool for threshold enforcement
- Added support for coverage overrides via PR comments
- Automated publication of coverage summary and badges
- Added historical coverage trend tracking

**Evidence:**
- PR #334: https://github.com/oocx/tfplan2md/pull/334
- PR title: "feat: code coverage reporting and enforcement in CI (#043)"

**Suggested comment to add before closing:**
```
This issue was fixed by PR #334 (merged on 2026-01-21).

The implementation includes:
- Code coverage collection in PR validation workflow
- CoverageEnforcer tool for threshold enforcement
- Coverage overrides via PR comments
- Automated coverage summary and badges
- Historical coverage trend tracking

All success criteria from the issue have been met.

Closing as completed.
```

---

## Issues Still Open (Not Fixed)

### Issue #427: Add explicit model lists (primary + fallback) to all agent definitions
**Status:** 🔵 OPEN (created 2026-02-09, very recent)
**Reason:** This is a brand new issue from 2 days ago. Not enough time to be addressed yet.

### Issue #365: Technical debt: allow providers to act dynamically without fixed resource-type lists
**Status:** 🔵 OPEN
**Reason:** No evidence of implementation found. This is a technical debt/refactoring issue.

### Issue #341: Cleanliness: Prevent misplaced chat logs in source directories
**Status:** 🔵 OPEN
**Reason:** No validation check added to `scripts/validate-agents.py` yet.

### Issue #332: Improve Code Quality: Immutability, Public Surface, and Constructor Parameters
**Status:** 🔵 OPEN
**Reason:** No evidence of implementation found. These are code quality improvements.

### Issue #330: Add Dependency Management Improvements: CPM, Lock Files, Vulnerability Scanning
**Status:** 🔵 OPEN
**Reason:** No evidence of implementation found.

### Issue #329: Improve Testing Infrastructure: Mutation Testing, Integration Tests, Flaky Test Detection
**Status:** 🔵 OPEN
**Reason:** No evidence of implementation found.

### Issue #327: Add Architecture Boundary Enforcement with Tests
**Status:** 🔵 OPEN
**Reason:** No evidence of implementation found.

### Issue #325: Code Quality Enhancements: Performance Benchmarks, PR Labeling, Build Optimization
**Status:** 🔵 OPEN
**Reason:** No evidence of implementation found.

### Issue #323: Improve Documentation: Templates, Architecture Diagrams, Code Quality Guide
**Status:** 🔵 OPEN
**Reason:** No evidence of implementation found.

### Issue #324: Add XML Documentation and Enforce CS1591 Warnings
**Status:** 🟡 PARTIALLY ADDRESSED
**Reason:** PR #337 addressed related issue #331 which added StyleCop.Analyzers and extensive XML documentation, but CS1591 itself is not explicitly enforced. StyleCop SA1600 series rules (which cover XML documentation) are configured in `.editorconfig` but not CS1591 specifically.

---

## Summary

**Issues to close:** 3 (✅)
- #374 - Fixed by PR #377
- #375 - Fixed by PR #377 and commit dd9b742  
- #326 - Fixed by PR #334

**Issues to keep open:** 10
- #427 - Brand new issue (2 days old)
- #365, #341, #332, #330, #329, #327, #325, #323 - No evidence of implementation
- #324 - Partially addressed but CS1591 enforcement not complete
