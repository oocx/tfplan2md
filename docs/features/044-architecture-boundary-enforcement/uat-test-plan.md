# UAT Test Plan: Architecture Boundary Enforcement with Tests

## Overview

**Note:** This feature is an internal development infrastructure improvement that does not affect user-facing markdown output. Therefore, **no traditional UAT with PR rendering validation is required**.

The "user" for this feature is the **development team**, who will observe architecture test results in CI/CD pipelines and local test runs. UAT for this feature consists of verifying the developer experience when:
1. Tests run successfully in CI
2. Tests fail with clear error messages when violations are introduced
3. Tests integrate seamlessly with existing test infrastructure

## UAT Approach for Internal Features

Since this feature doesn't change markdown rendering, UAT will focus on:

### 1. Developer Experience Validation

**Who:** Maintainer (as developer representative)  
**Where:** Local environment + CI pipeline  
**What:** Verify architecture tests provide value to developers

**Validation Steps:**

#### Step 1: Verify Tests Run Successfully in CI

1. Review PR checks for the feature implementation
2. Verify architecture tests appear in test results
3. Verify tests pass without errors
4. Verify execution time is acceptable (<10 seconds added to test suite)

**Success Criteria:**
- [ ] Architecture tests appear in CI test output
- [ ] All tests pass on first run
- [ ] Test execution adds <10 seconds to CI time
- [ ] No special configuration or manual steps required

---

#### Step 2: Verify Error Messages Are Helpful

1. Developer temporarily introduces an architectural violation
2. Run tests locally: `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*`
3. Observe error message

**Success Criteria:**
- [ ] Error message clearly states which rule was violated
- [ ] Error message lists the specific types that violate the rule
- [ ] Error message includes rationale for the rule
- [ ] Error message provides link to documentation
- [ ] Error message is actionable (developer knows how to fix)

**Example Expected Error Message:**
```
Architecture Violation: Parsing layer must not depend on MarkdownGeneration

Rationale: Parsing is a core domain layer and should not know about rendering concerns.
This prevents circular dependencies and maintains clean separation between parsing and rendering.

Violations found in:
  - Oocx.TfPlan2Md.Parsing.SomeClass

See docs/architecture-rules.md for guidance on architectural boundaries.
Related ADR: docs/adr-007-architecture-boundary-enforcement.md
```

---

#### Step 3: Verify Documentation Clarity

1. Review `docs/architecture-rules.md`
2. Verify it answers common questions:
   - What are the architectural layers?
   - What dependencies are allowed/forbidden?
   - Why do these rules exist?
   - How do I fix a violation?
   - How do I request an exemption?

**Success Criteria:**
- [ ] Documentation is comprehensive and clear
- [ ] Examples show correct and incorrect patterns
- [ ] Rationale for each rule is documented
- [ ] Process for requesting exemptions is clear

---

#### Step 4: Verify Integration with Development Workflow

1. Verify tests run with standard `dotnet test` command
2. Verify tests run with `scripts/test-with-timeout.sh` wrapper
3. Verify tests can be filtered with TUnit's `--treenode-filter`
4. Verify tests appear in IDE test explorers

**Success Criteria:**
- [ ] No special commands needed to run tests
- [ ] Tests integrate with existing test infrastructure
- [ ] Tests discoverable in VS Code/Rider/Visual Studio
- [ ] Test filtering works as expected

---

### 2. Architecture Validation

**Who:** Maintainer (as architecture reviewer)  
**Where:** Code review  
**What:** Verify architecture tests enforce correct boundaries

**Validation Steps:**

#### Step 1: Verify All Rules Are Enforced

Review `ArchitectureBoundaryTests.cs` and verify:
- [ ] All 10 dependency rules are implemented
- [ ] All 3 naming convention rules are implemented
- [ ] Each test has a clear, descriptive name
- [ ] Each test produces helpful error messages

#### Step 2: Verify Exemptions Are Justified

Review exemptions and verify:
- [ ] Only 8 files are exempted (as documented in architecture.md)
- [ ] Each exemption has a clear justification comment
- [ ] Each exemption references a tracking issue (or plan to create one)
- [ ] Exemptions use consistent pattern

#### Step 3: Verify Performance Is Acceptable

Review test execution and verify:
- [ ] All 13 tests complete in <10 seconds
- [ ] No noticeable slowdown in CI pipeline
- [ ] Test execution time is consistent

---

## No Markdown Rendering UAT Required

This feature does **not** require traditional UAT with PR rendering validation because:

1. **No User-Facing Output Changes**: Architecture tests are internal infrastructure - they don't change how markdown is rendered or displayed to users
2. **No Markdown Files Generated**: The feature produces test results, not markdown files
3. **No Platform-Specific Rendering**: There's nothing to validate in GitHub vs Azure DevOps PRs
4. **Internal Developer Tool**: The "users" are developers, not Terraform practitioners viewing plan output

Traditional UAT (posting markdown to GitHub/Azure DevOps PRs) is only required for features that:
- Change markdown rendering logic
- Add new markdown output sections
- Modify templates or formatting
- Affect what users see in PR comments

---

## Manual Testing Checklist

The following manual tests should be performed during implementation:

### During Development (Developer)

- [ ] **Test Failure Detection**: Temporarily remove an exemption and verify test fails
- [ ] **Error Message Quality**: Introduce violation and verify error message is helpful
- [ ] **Performance**: Measure execution time (should be <10 seconds)
- [ ] **Local Execution**: Run tests locally and verify they pass

### During Code Review (Code Reviewer)

- [ ] **Test Coverage**: Verify all 13 rules are present
- [ ] **Exemption Justification**: Verify all exemptions are documented
- [ ] **Error Messages**: Spot-check error message format
- [ ] **Documentation**: Verify `docs/architecture-rules.md` is complete

### During CI Integration (Release Manager)

- [ ] **CI Execution**: Verify tests run automatically in PR validation
- [ ] **Failure Behavior**: Verify failed tests block PR merge
- [ ] **Test Output**: Verify results appear in PR checks

---

## Approval Criteria

UAT is considered **passed** when:

1. **Tests Run Successfully**:
   - [ ] All architecture tests pass in CI
   - [ ] Tests complete in <10 seconds
   - [ ] No special configuration required

2. **Error Messages Are Helpful**:
   - [ ] Violations produce clear, actionable error messages
   - [ ] Error messages include rationale and documentation links
   - [ ] Developers can understand how to fix violations

3. **Documentation Is Clear**:
   - [ ] `docs/architecture-rules.md` is comprehensive
   - [ ] All rules are documented with rationale
   - [ ] Examples show correct and incorrect patterns

4. **Integration Is Seamless**:
   - [ ] Tests run with standard `dotnet test` command
   - [ ] Tests integrate with existing test infrastructure
   - [ ] Tests discoverable in IDE test explorers

---

## UAT Execution Approach

Since this is an internal feature, UAT execution is simplified:

### Option 1: Interactive Validation (Recommended)

**Who:** Maintainer + Developer in VS Code chat  
**When:** After implementation is complete  
**How:**

1. Developer commits all changes to feature branch
2. Maintainer reviews PR in GitHub
3. Maintainer runs tests locally to verify functionality
4. Maintainer introduces a test violation to verify error messages
5. Maintainer reviews documentation for clarity
6. Maintainer provides approval or feedback in PR comments

### Option 2: CI-Based Validation

**Who:** Maintainer  
**When:** After PR is created  
**How:**

1. Developer creates PR
2. Maintainer reviews CI test results
3. Maintainer reviews code and documentation in PR
4. Maintainer provides approval via PR review

---

## Definition of Done

UAT is complete when:

- [ ] All manual testing checklist items completed
- [ ] All approval criteria met
- [ ] Maintainer approves the PR
- [ ] No blocking issues identified

---

## References

- Feature Specification: `docs/features/044-architecture-boundary-enforcement/specification.md`
- Architecture Design: `docs/features/044-architecture-boundary-enforcement/architecture.md`
- Test Plan: `docs/features/044-architecture-boundary-enforcement/test-plan.md`
- ADR-007: `docs/adr-007-architecture-boundary-enforcement.md`
