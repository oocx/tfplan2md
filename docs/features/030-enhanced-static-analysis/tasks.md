# Tasks: Enhanced Static Analysis with Multiple Analyzers

## Overview

This document provides an actionable task breakdown for implementing Feature #044: Enhanced Static Analysis with Multiple Analyzers. The feature adds four static code analyzers (StyleCop, SonarAnalyzer, Meziantou, Roslynator) to complement the existing Microsoft.CodeAnalysis.NetAnalyzers.

**Implementation Strategy**: Phased rollout - add one analyzer at a time, fix violations, promote critical rules to errors, then move to the next analyzer.

**Reference Documents**:
- [Specification](specification.md) - Requirements and user stories
- [Architecture](architecture.md) - Technical design and rationale
- [Test Plan](test-plan.md) - 29 test cases for validation

**Timeline Estimate**: 7-12 weeks (7-12 PRs)

---

## Task Priority Legend

- **P0 (Critical Path)**: Must be completed in sequence; blocks next phase
- **P1 (High Priority)**: Important for phase completion; can be parallelized with other P1 tasks
- **P2 (Medium Priority)**: Nice-to-have; can be deferred if timeline is tight
- **P3 (Low Priority)**: Optional improvements; can be done after feature completion

---

## Phase 1: StyleCop.Analyzers (Weeks 1-2)

Focus: XML documentation and code consistency enforcement

### Task P1-T1: Add StyleCop.Analyzers Package

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 15 minutes  
**Dependencies**: None  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: Low

**Description:**  
Add StyleCop.Analyzers v1.2.0-beta.556 package reference to `src/Directory.Build.props` following the existing Microsoft.CodeAnalysis.NetAnalyzers pattern.

**Acceptance Criteria:**
- [ ] Package reference added to `src/Directory.Build.props` in existing analyzer ItemGroup
- [ ] Version pinned to exact version `1.2.0-beta.556`
- [ ] `PrivateAssets` set to `all`
- [ ] `IncludeAssets` set to `runtime; build; native; contentfiles; analyzers`
- [ ] `dotnet restore src/tfplan2md.slnx` succeeds without errors
- [ ] Test case TC-P1-01 passes (analyzer loaded in verbose build output)

**Implementation Notes:**
- Place package reference immediately after Microsoft.CodeAnalysis.NetAnalyzers
- Use consistent formatting with existing analyzer entries
- Run `dotnet restore` and verify package is downloaded to NuGet cache

**Validation Command:**
```bash
dotnet build src/tfplan2md.slnx --verbosity detailed 2>&1 | grep -i "StyleCop"
```

---

### Task P1-T2: Baseline Build and Document StyleCop Violations

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 1-2 hours  
**Dependencies**: P1-T1  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: Medium (violation count unknown)

**Description:**  
Run build with StyleCop enabled, collect all emitted warnings, and document violations by rule ID and frequency. This creates a baseline for triage.

**Acceptance Criteria:**
- [ ] Run `dotnet build src/tfplan2md.slnx` successfully (warnings OK, errors fail task)
- [ ] Capture build output to file: `build-stylecop-baseline.txt`
- [ ] Extract unique StyleCop rule IDs with counts (e.g., "SA1600: 47 violations")
- [ ] Document violations in PR description or issue comment
- [ ] Identify top 5 most frequent violations for prioritization
- [ ] Confirm build time increase <20% from baseline (~45-60s → <72s)

**Implementation Notes:**
- Measure baseline build time first: `time dotnet build src/tfplan2md.slnx --no-incremental`
- Then add analyzer and measure again
- Expected violations: ~50-100 (primarily SA1600 missing XML docs)
- Use script to count violations by rule ID:
  ```bash
  dotnet build src/tfplan2md.slnx 2>&1 | grep -oP 'SA\d{4}' | sort | uniq -c | sort -rn
  ```

**Risk Mitigation:**
- If violation count >200, consult maintainer on approach (may need longer timeline)
- If build time increase >20%, identify expensive rules and consider disabling

---

### Task P1-T3: Configure StyleCop Rules in .editorconfig

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 1-2 hours  
**Dependencies**: P1-T2  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: Low

**Description:**  
Add StyleCop rule configurations to `.editorconfig` with all rules starting as `warning` severity. Disable rules that are not applicable to this project (e.g., SA1633 file headers).

**Acceptance Criteria:**
- [ ] New section added to `.editorconfig`: `# StyleCop.Analyzers Rules`
- [ ] All StyleCop rules explicitly configured (or inherit default `warning`)
- [ ] SA1633 (file header) set to `none` (project doesn't use file headers)
- [ ] SA1101 (prefix with `this`) aligned with existing `dotnet_style_qualification_*` rules
- [ ] Comments explain rationale for `none`/`silent` severity rules
- [ ] Test case TC-CONF-02 passes (rule propagation verified)
- [ ] Build runs with configured severities (no unintended errors)

**Implementation Notes:**
- Add section after existing C# formatting rules
- Document common suppressions (if any) with justification
- Reference architecture.md section "3. Progressive Severity Escalation" for critical rule list

---

### Task P1-T4: Fix StyleCop Violations (Critical Rules)

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 3-7 days  
**Dependencies**: P1-T3  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: High (time-intensive)

**Description:**  
Fix all StyleCop violations that will be promoted to `error` severity. Focus on SA1600 (missing XML documentation) and other critical rules identified in baseline. Use suppressions sparingly with clear justification comments.

**Acceptance Criteria:**
- [ ] All SA1600 violations fixed (public/internal members documented)
- [ ] XML documentation follows project conventions (see `docs/spec.md`)
- [ ] Suppressions used only where legitimate (with `#pragma warning disable` and comment)
- [ ] All suppression comments explain why suppression is necessary
- [ ] Build produces zero SA1600 warnings (and zero warnings for other critical rules)
- [ ] Test cases TC-P1-02, TC-P1-03 pass (rule detection and severity validation)
- [ ] All existing tests continue to pass (`dotnet test`)

**Implementation Notes:**
- Focus on public API members first (most important for documentation)
- Use XML doc templates in IDE for consistency
- Suppression format:
  ```csharp
  #pragma warning disable SA1600 // Test helper class - documentation not required
  internal class TestHelper { }
  #pragma warning restore SA1600
  ```

**Risk Mitigation:**
- If violation count is very high (>100), split into multiple PRs by project

---

### Task P1-T5: Fix StyleCop Violations (Non-Critical Rules)

**Priority**: P1 (High Priority)  
**Estimated Effort**: 2-4 days  
**Dependencies**: P1-T4  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: Medium

**Description:**  
Fix remaining StyleCop violations that won't be promoted to errors but improve code consistency (e.g., ordering rules, spacing rules). Configure false positives as `silent` or `suggestion`.

**Acceptance Criteria:**
- [ ] All remaining StyleCop warnings addressed (fixed, suppressed, or configured)
- [ ] Build produces zero StyleCop warnings
- [ ] False positives configured in `.editorconfig` as `silent` or `suggestion`
- [ ] Configuration decisions documented in `.editorconfig` comments
- [ ] All tests pass

**Implementation Notes:**
- Common non-critical rules: SA1200 (using directives), SA1309 (field names), SA1413 (trailing commas)
- If a rule conflicts with project style, set to `silent` and document why

---

### Task P1-T6: Promote StyleCop Critical Rules to Error

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 30 minutes  
**Dependencies**: P1-T4, P1-T5  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: Low

**Description:**  
Update `.editorconfig` to promote critical StyleCop rules from `warning` to `error` severity. Verify build still passes (all violations should be fixed in T4).

**Acceptance Criteria:**
- [ ] SA1600 (elements should be documented) promoted to `error`
- [ ] Other critical rules promoted to `error` (per architecture.md)
- [ ] Build succeeds with zero errors and zero warnings
- [ ] Test case TC-P1-03 passes (severity escalation validated)
- [ ] CI build passes (`dotnet format --verify-no-changes` and `dotnet build`)

**Implementation Notes:**
- Update `.editorconfig` severity settings to `error` for critical rules
- Create temporary test file with SA1600 violation to verify error is raised

---

### Task P1-T7: Validate Test Plan Test Cases (Phase 1)

**Priority**: P1 (High Priority)  
**Estimated Effort**: 1-2 hours  
**Dependencies**: P1-T6  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: Low

**Description:**  
Execute all Phase 1 test cases from test-plan.md to ensure StyleCop integration is complete and correct.

**Acceptance Criteria:**
- [ ] TC-P1-01: Analyzer package installation ✓
- [ ] TC-P1-02: Documentation rule detection ✓
- [ ] TC-P1-03: Severity configuration validation ✓
- [ ] TC-P1-04: File header rule configuration ✓
- [ ] All test cases pass with documented results
- [ ] Any failures documented with root cause and mitigation

---

### Task P1-T8: Measure and Document Performance Impact

**Priority**: P1 (High Priority)  
**Estimated Effort**: 30 minutes  
**Dependencies**: P1-T6  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: Low

**Description:**  
Measure build time impact of StyleCop.Analyzers and document results. Verify build time increase is within acceptable threshold (<20%).

**Acceptance Criteria:**
- [ ] Baseline build time measured (before StyleCop)
- [ ] Build time with StyleCop measured
- [ ] Percentage increase calculated
- [ ] Test case TC-PERF-01 passes (build time <20% increase)
- [ ] Results documented in PR description or issue comment
- [ ] If >20%, consult maintainer on expensive rules to disable

**Implementation Notes:**
- Run clean builds to measure accurately (3 runs each, take average)
- Document results in PR

---

### Task P1-T9: Create PR for Phase 1 (StyleCop)

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 1 hour  
**Dependencies**: P1-T7, P1-T8  
**Phase**: Phase 1 - StyleCop  
**Risk Level**: Low

**Description:**  
Create pull request for Phase 1 implementation with comprehensive description, test evidence, and performance metrics.

**Acceptance Criteria:**
- [ ] PR created with title: `feat: add StyleCop.Analyzers for documentation enforcement`
- [ ] PR description includes summary, violations fixed, test results, performance metrics
- [ ] All files committed: `src/Directory.Build.props`, `.editorconfig`, source code fixes
- [ ] CI pipeline passes (format check, build, tests, markdown lint)
- [ ] PR reviewed and merged before starting Phase 2

---

## Phase 2: SonarAnalyzer.CSharp (Weeks 3-5)

Focus: Code smells, null reference risks, cognitive complexity

### Task P2-T1: Add SonarAnalyzer.CSharp Package

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 15 minutes  
**Dependencies**: P1-T9 (Phase 1 complete)  
**Phase**: Phase 2 - SonarAnalyzer  
**Risk Level**: Low

**Description:**  
Add SonarAnalyzer.CSharp v9.16.0 package reference to `src/Directory.Build.props` following the same pattern as StyleCop.

**Acceptance Criteria:**
- [ ] Package reference added to `src/Directory.Build.props`
- [ ] Version pinned to exact version `9.16.0`
- [ ] `PrivateAssets` and `IncludeAssets` configured correctly
- [ ] `dotnet restore src/tfplan2md.slnx` succeeds
- [ ] Test case TC-P2-01 passes (analyzer loaded in verbose build)

---

### Task P2-T2 through P2-T9: [Similar Pattern as Phase 1]

**Note**: Phase 2 follows the same 9-task pattern as Phase 1:
- T2: Baseline violations
- T3: Configure rules
- T4: Fix critical violations
- T5: Fix non-critical violations
- T6: Promote to errors
- T7: Validate tests
- T8: Measure performance
- T9: Create PR

**Expected violations**: ~20-50 (null checks, complexity warnings)
**Critical rules to promote**: S1066, S2259, S2583, S3925
**Estimated effort**: 2-3 weeks

---

## Phase 3: Meziantou.Analyzer (Weeks 6-7)

Focus: Best practices, performance patterns, StringComparison enforcement

### Task P3-T1: Add Meziantou.Analyzer Package

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 15 minutes  
**Dependencies**: P2-T9 (Phase 2 complete)  
**Phase**: Phase 3 - Meziantou  
**Risk Level**: Low

**Description:**  
Add Meziantou.Analyzer v2.0.127 package reference to `src/Directory.Build.props`.

**Acceptance Criteria:**
- [ ] Package reference added with version `2.0.127`
- [ ] Test case TC-P3-01 passes

---

### Task P3-T2 through P3-T9: [Similar Pattern as Phase 1]

**Expected violations**: ~10-30 (StringComparison, ConfigureAwait, LINQ patterns)
**Critical rules to promote**: MA0004 (ConfigureAwait - may disable for console app), MA0011 (IFormatProvider), MA0015 (StringComparison)
**Estimated effort**: 1-2 weeks

---

## Phase 4: Roslynator.Analyzers (Weeks 8-10)

Focus: Code simplification and refactoring suggestions

### Task P4-T1: Add Roslynator.Analyzers Package

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 15 minutes  
**Dependencies**: P3-T9 (Phase 3 complete)  
**Phase**: Phase 4 - Roslynator  
**Risk Level**: Low

**Description:**  
Add Roslynator.Analyzers package reference to `src/Directory.Build.props`. Use version pattern `4.*` for major version pinning (per architecture.md).

**Acceptance Criteria:**
- [ ] Package reference added with version `4.*` or exact `4.x.x`
- [ ] Test case TC-P4-01 passes

---

### Task P4-T2: Baseline Build and Document Roslynator Violations

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 2-3 hours  
**Dependencies**: P4-T1  
**Phase**: Phase 4 - Roslynator  
**Risk Level**: High (largest rule set - 200+ rules)

**Description:**  
Run build with Roslynator.Analyzers enabled, collect all emitted warnings, and document violations. **Note**: Roslynator has 200+ rules, expect high violation count.

**Acceptance Criteria:**
- [ ] Build output captured
- [ ] Violations documented and categorized
- [ ] Top 10 most frequent violations identified
- [ ] Build time increase measured

**Expected violations**: ~30-60 (simplification opportunities)
**Risk Mitigation**: If violation count >100, plan for selective rule enabling

---

### Task P4-T3: Configure Roslynator Rules (Selective Enabling)

**Priority**: P0 (Critical Path)  
**Estimated Effort**: 2-3 hours  
**Dependencies**: P4-T2  
**Phase**: Phase 4 - Roslynator  
**Risk Level**: Medium

**Description:**  
Add Roslynator rule configurations to `.editorconfig`. **Important**: Most Roslynator rules should start as `suggestion` or `silent`, only valuable rules as `warning`.

**Acceptance Criteria:**
- [ ] Most rules configured as `suggestion` (not `warning` or `error`)
- [ ] Only high-value rules (readability, consistency) configured as `warning`
- [ ] Comments explain rationale for severity choices

**Implementation Notes:**
- Architecture.md: "Most Roslynator rules should remain as suggestion or warning"
- Focus on rules that align with critical project conventions

---

### Task P4-T4 through P4-T9: [Similar Pattern with Adjustments]

**Key differences from other phases**:
- T4: Fix only selected warnings (not all violations)
- T5: Optional refactorings (P2 priority, can be deferred)
- T6: Very few rules promoted to error (0-3 rules)
- T8: Performance critical (largest analyzer, may exceed threshold)

**Estimated effort**: 2-4 weeks

---

## Cross-Phase Tasks

These tasks can be done concurrently with phases:

### Task XP-T1: Update Dependabot Configuration (if needed)

**Priority**: P2 (Medium Priority)  
**Estimated Effort**: 30 minutes  
**Dependencies**: P1-T1  
**Phase**: Cross-Phase  

**Description:**  
Verify Dependabot is configured to update analyzer packages. According to specification, no changes should be needed (already configured for NuGet).

**Acceptance Criteria:**
- [ ] Verify `dependabot.yml` includes `package-ecosystem: nuget`
- [ ] Test case TC-DEP-01 passes (Dependabot creates update PRs)

---

### Task XP-T2: Document Suppression Policy in CONTRIBUTING.md

**Priority**: P1 (High Priority)  
**Estimated Effort**: 1 hour  
**Dependencies**: P1-T4  
**Phase**: Cross-Phase  

**Description:**  
Update `CONTRIBUTING.md` to document the suppression policy for analyzer rules.

**Acceptance Criteria:**
- [ ] New section added: "Static Analysis Suppression Policy"
- [ ] Policy explains when suppressions are acceptable
- [ ] Required format documented with examples
- [ ] Test case TC-SUP-01 passes

---

### Task XP-T3: Create Code Quality Documentation

**Priority**: P2 (Medium Priority)  
**Estimated Effort**: 2 hours  
**Dependencies**: P4-T9  
**Phase**: Cross-Phase  

**Description:**  
Create comprehensive documentation explaining the project's static analysis setup (`docs/code-quality.md`).

**Acceptance Criteria:**
- [ ] New file created: `docs/code-quality.md`
- [ ] Document includes overview of all five analyzers, purpose, and usage guidance
- [ ] Linked from main README.md

---

### Task XP-T4: Validate CI/CD Integration

**Priority**: P1 (High Priority)  
**Estimated Effort**: 1 hour  
**Dependencies**: P1-T9  
**Phase**: Cross-Phase  

**Description:**  
Verify CI/CD pipeline correctly enforces analyzer rules without requiring workflow changes.

**Acceptance Criteria:**
- [ ] Test case TC-CI-01 passes (CI fails on analyzer violations)
- [ ] No workflow file changes required

---

### Task XP-T5: Validate Configuration Centralization

**Priority**: P1 (High Priority)  
**Estimated Effort**: 1 hour  
**Dependencies**: P2-T1  
**Phase**: Cross-Phase  

**Description:**  
Verify all analyzer configuration is centralized in `src/Directory.Build.props` and `.editorconfig`.

**Acceptance Criteria:**
- [ ] Test case TC-CONF-01 passes (centralized configuration enforced)
- [ ] No project-specific `.csproj` analyzer overrides

---

### Task XP-T6: Measure Cumulative Performance Impact

**Priority**: P1 (High Priority)  
**Estimated Effort**: 1 hour  
**Dependencies**: P4-T8  
**Phase**: Cross-Phase  

**Description:**  
Measure final cumulative build time with all four analyzers enabled and document complete performance analysis.

**Acceptance Criteria:**
- [ ] Final build time measured
- [ ] Cumulative increase from baseline calculated
- [ ] Test case TC-PERF-02 passes (cumulative build time <72s / <20% increase)
- [ ] Per-analyzer performance breakdown documented

**Risk Mitigation:**
- If cumulative >20%, create mitigation plan (disable expensive rules or remove analyzer)

---

### Task XP-T7: Run Regression Test Suite

**Priority**: P1 (High Priority)  
**Estimated Effort**: 30 minutes  
**Dependencies**: P4-T9  
**Phase**: Cross-Phase  

**Description:**  
Execute regression test cases to ensure existing functionality is not broken.

**Acceptance Criteria:**
- [ ] TC-REG-01: All existing tests pass
- [ ] TC-REG-02: Format check passes
- [ ] TC-REG-03: CI pipeline passes

---

## Implementation Order Summary

**Critical Path** (must be sequential):
```
Phase 1: P1-T1 → P1-T2 → P1-T3 → P1-T4 → P1-T6 → P1-T9
Phase 2: P2-T1 → P2-T2 → P2-T3 → P2-T4 → P2-T6 → P2-T9
Phase 3: P3-T1 → P3-T2 → P3-T3 → P3-T4 → P3-T6 → P3-T9
Phase 4: P4-T1 → P4-T2 → P4-T3 → P4-T4 → P4-T6 → P4-T9
```

**Parallel Tasks** (can overlap with phases):
- XP-T2 (suppression policy) - after P1-T4
- XP-T4 (CI validation) - after P1-T9
- XP-T5 (config centralization) - after P2-T1
- XP-T3, XP-T6, XP-T7 - after P4-T9

---

## Risk Items Summary

### High-Risk Tasks

| Task | Risk | Mitigation |
|------|------|------------|
| P1-T4 | High violation count (>100) | Split into multiple PRs by project |
| P2-T4 | Violations reveal design issues | Consult maintainer on design fixes |
| P4-T2 | Overwhelming rule count (200+) | Selective rule enabling, disable if needed |
| XP-T6 | Build time exceeds 20% threshold | Disable expensive rules, remove analyzer |

---

## Time Estimates

- **Phase 1 (StyleCop)**: 1-2 weeks
- **Phase 2 (SonarAnalyzer)**: 2-3 weeks
- **Phase 3 (Meziantou)**: 1-2 weeks
- **Phase 4 (Roslynator)**: 2-4 weeks
- **Cross-Phase & Documentation**: 2-3 days (parallel)
- **Grand Total**: 7-12 weeks (matches architecture.md estimate)

---

## Success Criteria

This feature is complete when:

- [ ] All four analyzers integrated
- [ ] Critical rules promoted to errors, zero warnings in CI
- [ ] Build time <72s (20% increase from baseline)
- [ ] All 29 test cases pass
- [ ] Suppression policy documented
- [ ] Code quality documentation created
- [ ] Dependabot creating update PRs
- [ ] No false positive complaints
- [ ] At least 5 real bugs caught in next 10 PRs

---

## Next Steps

**Ready to Start**: Task P1-T1 (Add StyleCop.Analyzers Package) - no blockers

**Handoff to Developer**: Begin with Phase 1, follow the 9-task pattern for each phase, validate test cases at each step, and monitor performance impact throughout.
