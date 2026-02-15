# Tasks: Architecture Boundary Enforcement with Tests

## Overview

This document breaks down the implementation of Feature 066: Architecture Boundary Enforcement with Tests into actionable tasks. The feature adds automated enforcement of architectural layer boundaries using NetArchTest.Rules to prevent unintended coupling between layers.

**Related Documents:**
- Feature Specification: `specification.md`
- Architecture Design: `architecture.md`
- ADR-007: `../../adr-007-architecture-boundary-enforcement.md`
- Test Plan: `test-plan.md`
- Work Protocol: `work-protocol.md`

**Feature Branch:** `copilot/add-architecture-boundary-enforcement`

**Implementation Summary:**
- Add NetArchTest.Rules package (version 1.3.2+) to test project
- Create `ArchitectureBoundaryTests.cs` with 13 test methods (10 dependency + 3 naming)
- Exempt 8 known violations with justification comments
- Format error messages with 5 required components
- Validate tests detect violations (manual meta-testing)
- Create architecture rules documentation
- Create tracking issues for known violations

---

## Tasks

### Task 1: Add NetArchTest.Rules Package

**Priority:** Critical

**Description:**
Add the NetArchTest.Rules NuGet package to the test project to enable architecture boundary testing.

**Acceptance Criteria:**
- [ ] NetArchTest.Rules package (version 1.3.2 or later) added to `src/tests/Oocx.TfPlan2Md.TUnit/Oocx.TfPlan2Md.TUnit.csproj`
- [ ] Package reference uses minimum version constraint: `Version="1.3.2"`
- [ ] Project builds successfully after adding package: `dotnet build src/tests/Oocx.TfPlan2Md.TUnit/`
- [ ] No dependency conflicts or warnings

**Dependencies:** None

**Notes:**
- NetArchTest.Rules is test-framework-agnostic and works seamlessly with TUnit
- The package is lightweight and should not significantly impact build time
- Version 1.3.2 or later is required for .NET 10 compatibility

**Implementation Guidance:**
```xml
<PackageReference Include="NetArchTest.Rules" Version="1.3.2" />
```

---

### Task 2: Create Architecture Test File Structure

**Priority:** Critical

**Description:**
Create the directory structure and empty test class file for architecture boundary tests.

**Acceptance Criteria:**
- [ ] Directory created: `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/`
- [ ] File created: `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs`
- [ ] Class exists in namespace `Oocx.TfPlan2Md.TUnit.Architecture`
- [ ] Class has XML documentation comment referencing docs and ADR
- [ ] File compiles without errors

**Dependencies:** Task 1 (package must be added first)

**Notes:**
- Single file approach keeps all architecture rules in one place for easy discovery
- Clear namespace indicates these are architecture-level tests
- XML documentation provides context for developers discovering these tests

**Implementation Guidance:**
```csharp
namespace Oocx.TfPlan2Md.TUnit.Architecture;

/// <summary>
/// Architecture tests that enforce layer boundaries and dependency rules.
/// See docs/architecture-rules.md for rule documentation.
/// Related ADR: docs/adr-007-architecture-boundary-enforcement.md
/// </summary>
public class ArchitectureBoundaryTests
{
    // Tests will be added in subsequent tasks
}
```

---

### Task 3: Implement Parsing Layer Dependency Tests (3 rules)

**Priority:** High

**Description:**
Implement tests that verify the Parsing layer does not depend on higher layers (MarkdownGeneration, CLI, Providers). Parsing is a core domain layer and must remain independent.

**Acceptance Criteria:**
- [ ] Test method: `Parsing_ShouldNotDependOn_MarkdownGeneration()` implemented
- [ ] Test method: `Parsing_ShouldNotDependOn_CLI()` implemented
- [ ] Test method: `Parsing_ShouldNotDependOn_Providers()` implemented
- [ ] Each test uses NetArchTest.Rules with correct namespace filters
- [ ] Each test includes clear error message with all 5 required components (rule, rationale, violations, guidance link, ADR reference)
- [ ] Tests pass when run locally: `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*`
- [ ] No exemptions needed (all code complies)

**Dependencies:** Task 2 (test file must exist)

**Notes:**
- The Parsing layer currently has NO violations for these rules
- These are the most critical rules as they protect the core domain layer
- Error messages must follow the format specified in architecture.md

**Implementation Pattern:**
```csharp
[Test]
public void Parsing_ShouldNotDependOn_MarkdownGeneration()
{
    var result = Types.InCurrentDomain()
        .That().ResideInNamespace("Oocx.TfPlan2Md.Parsing")
        .ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.MarkdownGeneration")
        .GetResult();
    
    Assert.That(result.IsSuccessful).IsTrue(
        CreateViolationMessage(
            "Parsing layer must not depend on MarkdownGeneration",
            "Parsing is a core domain layer and should not know about rendering concerns.",
            result.FailingTypes));
}
```

**Error Message Format (all 5 components):**
1. Rule statement
2. Rationale
3. Violations list
4. Guidance link to docs/architecture-rules.md
5. ADR reference to docs/adr-007-architecture-boundary-enforcement.md

---

### Task 4: Implement Platforms Layer Dependency Test (1 rule with exemptions)

**Priority:** High

**Description:**
Implement test that verifies the Platforms layer does not depend on MarkdownGeneration, with exemptions for 4 known value formatter files.

**Acceptance Criteria:**
- [ ] Test method: `Platforms_ShouldNotDependOn_MarkdownGeneration()` implemented
- [ ] Test uses NetArchTest.Rules with correct namespace filter
- [ ] Test exempts 4 known violations with `.DoNotHaveNameMatching()`:
  - `AzureValueFormatterRegistration`
  - `EnrichedAzureScopeFormatter`
  - `ManagementGroupIdFormatter`
  - `TenantIdFormatter`
- [ ] Each exemption has inline comment with justification and tracking issue reference
- [ ] Test includes clear error message with all 5 required components
- [ ] Test passes when run locally with exemptions in place
- [ ] Test fails when exemptions are removed (manual validation during implementation)

**Dependencies:** Task 2 (test file must exist)

**Notes:**
- These 4 files are value formatters incorrectly placed in Platforms layer
- They should eventually be refactored to MarkdownGeneration layer
- Tracking issue will be created in Task 10

**Exemption Pattern:**
```csharp
.And().DoNotHaveNameMatching("AzureValueFormatterRegistration") // Exempt: Value formatter in Platforms, needs refactoring (Issue #XXX)
.And().DoNotHaveNameMatching("EnrichedAzureScopeFormatter")     // Exempt: Value formatter in Platforms, needs refactoring (Issue #XXX)
.And().DoNotHaveNameMatching("ManagementGroupIdFormatter")      // Exempt: Value formatter in Platforms, needs refactoring (Issue #XXX)
.And().DoNotHaveNameMatching("TenantIdFormatter")               // Exempt: Value formatter in Platforms, needs refactoring (Issue #XXX)
```

---

### Task 5: Implement MarkdownGeneration Layer Dependency Test (1 rule with exemptions)

**Priority:** High

**Description:**
Implement test that verifies the MarkdownGeneration layer does not depend on Providers, with exemptions for 3 known AOT script mapping files.

**Acceptance Criteria:**
- [ ] Test method: `MarkdownGeneration_ShouldNotDependOn_Providers()` implemented
- [ ] Test uses NetArchTest.Rules with correct namespace filter
- [ ] Test exempts 3 known violations with `.DoNotHaveNameMatching()`:
  - `LargeValueSummary`
  - `ResourceChangeModel`
  - `AotScriptObjectMapper`
- [ ] Each exemption has inline comment with justification and tracking issue reference
- [ ] Test includes clear error message with all 5 required components
- [ ] Test passes when run locally with exemptions in place
- [ ] Test fails when exemptions are removed (manual validation during implementation)

**Dependencies:** Task 2 (test file must exist)

**Notes:**
- These 3 files reference provider-specific models for AOT-compatible Scriban script object mapping
- They should eventually be refactored to use provider self-registration
- Tracking issue will be created in Task 10

**Exemption Pattern:**
```csharp
.And().DoNotHaveNameMatching("LargeValueSummary")      // Exempt: AOT script mapping, needs refactoring (Issue #YYY)
.And().DoNotHaveNameMatching("ResourceChangeModel")    // Exempt: AOT script mapping, needs refactoring (Issue #YYY)
.And().DoNotHaveNameMatching("AotScriptObjectMapper")  // Exempt: AOT script mapping, needs refactoring (Issue #YYY)
```

---

### Task 6: Implement Remaining Forbidden Dependency Tests (2 rules)

**Priority:** High

**Description:**
Implement remaining tests for forbidden layer dependencies that protect architectural boundaries.

**Acceptance Criteria:**
- [ ] Test method: `CodeAnalysis_ShouldNotDependOn_MarkdownGeneration()` implemented
- [ ] Test method: `Diagnostics_ShouldNotDependOn_AnyLayer()` implemented
- [ ] Each test uses NetArchTest.Rules with correct namespace filters
- [ ] Each test includes clear error message with all 5 required components
- [ ] Tests pass when run locally
- [ ] No exemptions needed (all code complies)

**Dependencies:** Task 2 (test file must exist)

**Notes:**
- CodeAnalysis should be independent of rendering concerns
- Diagnostics is a cross-cutting concern with no domain dependencies
- These rules prevent architectural drift as the codebase evolves

**Diagnostics Test Pattern:**
```csharp
[Test]
public void Diagnostics_ShouldNotDependOn_AnyLayer()
{
    var result = Types.InCurrentDomain()
        .That().ResideInNamespace("Oocx.TfPlan2Md.Diagnostics")
        .ShouldNot().HaveDependencyOnAny(
            "Oocx.TfPlan2Md.CLI",
            "Oocx.TfPlan2Md.Parsing",
            "Oocx.TfPlan2Md.MarkdownGeneration",
            "Oocx.TfPlan2Md.Providers",
            "Oocx.TfPlan2Md.Platforms")
        .GetResult();
    // ... assertion with error message
}
```

---

### Task 7: Implement Allowed Dependency Tests (4 documentation rules)

**Priority:** Medium

**Description:**
Implement tests that document ALLOWED dependencies between layers. These are pass-through tests that verify the architecture supports expected dependency directions.

**Acceptance Criteria:**
- [ ] Test method: `CLI_CanDependOn_AllLayers()` implemented
- [ ] Test method: `MarkdownGeneration_CanDependOn_Parsing()` implemented
- [ ] Test method: `Providers_CanDependOn_Parsing()` implemented
- [ ] Test method: `Providers_CanDependOn_MarkdownGeneration()` implemented
- [ ] Tests use NetArchTest.Rules to verify dependencies exist or simply pass to document the rule
- [ ] Each test has descriptive comment explaining why this dependency is allowed
- [ ] Tests pass when run locally

**Dependencies:** Task 2 (test file must exist)

**Notes:**
- These tests serve as DOCUMENTATION, not enforcement
- They communicate the intended architecture to developers
- They can verify that expected dependencies actually exist (proving the architecture is correct)
- Alternative approach: simple pass-through tests that always succeed but document the rule

**Implementation Pattern (verification approach):**
```csharp
[Test]
public void MarkdownGeneration_CanDependOn_Parsing()
{
    // This test documents that MarkdownGeneration SHOULD depend on Parsing
    // Rendering logic needs access to parsed domain models
    var result = Types.InCurrentDomain()
        .That().ResideInNamespace("Oocx.TfPlan2Md.MarkdownGeneration")
        .Should().HaveDependencyOn("Oocx.TfPlan2Md.Parsing")
        .GetResult();
    
    Assert.That(result.IsSuccessful).IsTrue(
        "MarkdownGeneration should depend on Parsing (rendering needs parsed data)");
}
```

**Implementation Pattern (documentation-only approach):**
```csharp
[Test]
public void CLI_CanDependOn_AllLayers()
{
    // This test documents that CLI is allowed to depend on all layers
    // CLI is the top-level orchestration layer
    // No actual verification needed - this rule allows dependencies
    Assert.Pass("CLI is allowed to depend on all layers (orchestration layer)");
}
```

---

### Task 8: Implement Naming Convention Tests (3 rules)

**Priority:** Medium

**Description:**
Implement tests that enforce .NET naming conventions for exceptions, tests, and interfaces.

**Acceptance Criteria:**
- [ ] Test method: `Exceptions_ShouldHave_ExceptionSuffix()` implemented
- [ ] Test method: `Tests_ShouldHave_TestsSuffix()` implemented
- [ ] Test method: `Interfaces_ShouldHave_IPrefix()` implemented
- [ ] Each test uses NetArchTest.Rules with appropriate type filters
- [ ] Each test includes clear error message
- [ ] Tests pass when run locally
- [ ] No exemptions needed (all code complies with .NET naming conventions)

**Dependencies:** Task 2 (test file must exist)

**Notes:**
- These rules enforce standard .NET naming conventions
- Current codebase already complies with these rules
- Tests should check production code only (exclude test helpers, AssemblyInfo)

**Exception Test Pattern:**
```csharp
[Test]
public void Exceptions_ShouldHave_ExceptionSuffix()
{
    var result = Types.InCurrentDomain()
        .That().Inherit(typeof(Exception))
        .And().ResideInNamespace("Oocx.TfPlan2Md")
        .Should().HaveNameEndingWith("Exception")
        .GetResult();
    
    Assert.That(result.IsSuccessful).IsTrue(
        $"All exception classes must end with 'Exception' suffix. Violations: {string.Join(", ", result.FailingTypes)}");
}
```

---

### Task 9: Implement Error Message Helper Method

**Priority:** High

**Description:**
Create a helper method to generate consistent, well-formatted error messages with all 5 required components.

**Acceptance Criteria:**
- [ ] Private static method `CreateViolationMessage()` added to test class
- [ ] Method accepts parameters: rule statement, rationale, failing types list
- [ ] Method returns formatted string with all 5 components:
  1. Rule statement
  2. Rationale
  3. Violations list (formatted as bullet points)
  4. Guidance link: `docs/architecture-rules.md`
  5. ADR reference: `docs/adr-007-architecture-boundary-enforcement.md`
- [ ] Method used by all dependency rule tests (Tasks 3-6)
- [ ] Error messages are clear and actionable

**Dependencies:** Task 2 (test file must exist)

**Notes:**
- This helper reduces duplication and ensures consistent error messages
- Developers need clear guidance when they violate architectural rules
- All 5 components are required per architecture.md

**Implementation:**
```csharp
private static string CreateViolationMessage(string rule, string rationale, IEnumerable<Type> failingTypes)
{
    var violations = failingTypes.Any() 
        ? string.Join("\n  - ", failingTypes.Select(t => t.FullName))
        : "(none)";
    
    return $@"
Architecture Violation: {rule}

Rationale: {rationale}

Violations found in:
  - {violations}

See docs/architecture-rules.md for guidance on architectural boundaries.
Related ADR: docs/adr-007-architecture-boundary-enforcement.md
";
}
```

---

### Task 10: Create Tracking Issues for Known Violations

**Priority:** Medium

**Description:**
Create GitHub issues to track the 2 categories of known violations that need future refactoring.

**Acceptance Criteria:**
- [ ] Issue created: "Refactor value formatters from Platforms to MarkdownGeneration"
  - Lists 4 affected files
  - References ADR-007
  - Suggests refactoring approach
  - Labeled: `technical-debt`, `architecture`
- [ ] Issue created: "Refactor AOT script mapping to use provider self-registration"
  - Lists 3 affected files
  - References ADR-007
  - Suggests refactoring approach
  - Labeled: `technical-debt`, `architecture`
- [ ] Issue numbers recorded in work-protocol.md
- [ ] Test exemption comments updated with actual issue numbers (replace `#XXX`, `#YYY`)

**Dependencies:** Tasks 4, 5 (tests with exemptions must exist)

**Notes:**
- These issues document technical debt for future improvement
- Issues should not block feature completion
- Low priority - can be addressed in future work
- Use GitHub CLI or web interface to create issues

**Issue Template (Value Formatters):**
```markdown
## Problem
Four value formatter classes are located in the Platforms layer but depend on MarkdownGeneration services. This violates the architectural principle that Platforms should provide metadata only, not rendering concerns.

## Affected Files
- `src/Oocx.TfPlan2Md/Platforms/Azure/AzureValueFormatterRegistration.cs`
- `src/Oocx.TfPlan2Md/Platforms/Azure/EnrichedAzureScopeFormatter.cs`
- `src/Oocx.TfPlan2Md/Platforms/Azure/ManagementGroupIdFormatter.cs`
- `src/Oocx.TfPlan2Md/Platforms/Azure/TenantIdFormatter.cs`

## Proposed Solution
Move formatters to MarkdownGeneration layer and reference Platforms for metadata (allowed dependency direction).

## References
- ADR-007: docs/adr-007-architecture-boundary-enforcement.md
- Architecture Design: docs/features/044-architecture-boundary-enforcement/architecture.md
```

---

### Task 11: Execute Manual Meta-Testing Validation

**Priority:** High

**Description:**
Perform manual validation to verify that architecture tests correctly detect violations when introduced. This meta-testing ensures the tests are actually working.

**Acceptance Criteria:**
- [ ] Validation 1: Temporarily remove exemption for `TfPlanJsonContext` from a test, verify test fails, restore exemption
- [ ] Validation 2: Temporarily add a violation (e.g., `using Oocx.TfPlan2Md.MarkdownGeneration;` in a Parsing class), verify test fails, remove violation
- [ ] Validation 3: Review error message quality - verify all 5 components are present
- [ ] Validation 4: Measure execution time of all architecture tests - verify <10 seconds
- [ ] All validation results documented in work-protocol.md under "Manual Meta-Testing Results" section

**Dependencies:** Tasks 3-8 (all tests must be implemented)

**Notes:**
- This is MANUAL validation during development, not automated tests
- Cannot commit violations to verify tests work - must be done locally
- This proves the tests are actually detecting architectural issues
- See test-plan.md "Manual Validation Checklist" for detailed steps

**Validation Commands:**
```bash
# Run all architecture tests with timing
time dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ \
  --treenode-filter /*/*/ArchitectureBoundaryTests/* \
  --output Detailed

# Run specific test
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ \
  --treenode-filter /*/*/ArchitectureBoundaryTests/Platforms_ShouldNotDependOn_MarkdownGeneration
```

**Work Protocol Documentation Format:**
```markdown
### Manual Meta-Testing Results

**Date:** YYYY-MM-DD

**Validation 1 - Exemption Removal:**
- Removed exemption for `TfPlanJsonContext` from test
- Test failed as expected with error message listing TfPlanJsonContext
- Restored exemption, test passed
- ✅ Violation detection confirmed

**Validation 2 - Introduced Violation:**
- Added `using Oocx.TfPlan2Md.MarkdownGeneration;` to Parsing class
- Test failed as expected
- Error message contained all 5 required components
- ✅ Error message quality confirmed

**Validation 3 - Performance:**
- All 13 tests completed in X.XX seconds
- ✅ Performance target (<10s) met
```

---

### Task 12: Create Architecture Rules Documentation

**Priority:** High

**Description:**
Create comprehensive documentation of architectural layers, dependency rules, naming conventions, and violation resolution process.

**Acceptance Criteria:**
- [ ] File created: `docs/architecture-rules.md`
- [ ] Document includes all required sections:
  - Overview (purpose, how tests work, CI integration)
  - Layer Definitions (8 layers with purpose, responsibilities, allowed dependencies)
  - Dependency Rules (10 rules with rationale and examples)
  - Naming Convention Rules (3 rules with rationale)
  - Known Exemptions (8 files with justification and tracking issues)
  - Violation Resolution Process (how to fix, how to request exemption)
  - References (ADR-007, NetArchTest docs, architecture.md, test file)
- [ ] Each rule includes rationale explaining WHY it exists
- [ ] Known exemptions list actual issue numbers from Task 10
- [ ] Examples provided for correct and incorrect patterns
- [ ] Markdown formatting is clean and readable

**Dependencies:** Task 10 (tracking issues must be created for exemption documentation)

**Notes:**
- This documentation serves as the canonical reference for developers
- Tests are the source of truth; documentation explains WHY
- Keep documentation in sync with test implementation
- See architecture.md for detailed content structure

**Document Structure:**
```markdown
# Architecture Rules

## Overview
[Purpose, how tests work, CI integration]

## Layer Definitions
[Table with 8 layers, purpose, allowed dependencies]

## Dependency Rules
### 1. Parsing must not depend on MarkdownGeneration
**Rationale:** [Why this rule exists]
**Examples:** [Code examples]

[Repeat for all 10 rules]

## Naming Convention Rules
[3 rules with rationale]

## Known Exemptions
[8 files with justification and tracking issues]

## Violation Resolution Process
[How to fix violations, request exemptions]

## References
- ADR-007: docs/adr-007-architecture-boundary-enforcement.md
- NetArchTest Documentation: https://github.com/BenMorris/NetArchTest
- Architecture Overview: docs/architecture.md
- Test Implementation: src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs
```

---

### Task 13: Verify CI Integration

**Priority:** High

**Description:**
Verify that architecture tests run automatically in the CI pipeline and block PRs when violations are detected.

**Acceptance Criteria:**
- [ ] All tests pass locally: `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/`
- [ ] Commit all changes to feature branch
- [ ] Push to remote: `git push origin HEAD`
- [ ] Verify PR validation workflow runs and includes architecture tests
- [ ] Verify test results appear in PR checks
- [ ] Verify execution time is reasonable (<10 seconds added to test suite)
- [ ] No special CI configuration changes required (tests discovered automatically by TUnit)

**Dependencies:** Tasks 3-12 (all implementation must be complete)

**Notes:**
- Architecture tests should "just work" with existing CI setup
- TUnit automatically discovers and runs all tests including architecture tests
- No changes to `.github/workflows/pr-validation.yml` should be needed
- If tests don't run, investigate TUnit test discovery

**CI Validation:**
1. Ensure all tests pass locally first
2. Commit and push changes
3. Observe PR validation workflow in GitHub Actions
4. Check test output in workflow logs
5. Verify architecture tests appear in test results

---

### Task 14: Update Work Protocol with Completion Summary

**Priority:** Low

**Description:**
Document the implementation results, validation findings, and any issues encountered in the work-protocol.md file.

**Acceptance Criteria:**
- [ ] Work protocol updated with Developer agent entry
- [ ] Summary of work performed documented
- [ ] Artifacts produced listed (test file, documentation)
- [ ] Key decisions documented
- [ ] Manual meta-testing results included (from Task 11)
- [ ] Performance measurements recorded
- [ ] Any problems encountered documented
- [ ] Tracking issue numbers recorded
- [ ] Recommendation for next agent (Technical Writer or Code Reviewer)

**Dependencies:** Tasks 1-13 (all work must be complete)

**Notes:**
- This provides a historical record of the implementation
- Captures lessons learned for future features
- Documents any deviations from the plan

**Work Protocol Entry Template:**
```markdown
### Developer - [Date]

**Summary:** Implemented architecture boundary enforcement with 13 automated tests.

**Work Performed:**
- Added NetArchTest.Rules package
- Created ArchitectureBoundaryTests.cs with 13 test methods
- Implemented 10 dependency rules and 3 naming convention rules
- Added exemptions for 8 known violations
- Created error message helper method
- Executed manual meta-testing validation
- Created tracking issues #XXX and #YYY
- Created docs/architecture-rules.md documentation
- Verified CI integration

**Artifacts Produced:**
- src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs
- docs/architecture-rules.md
- GitHub Issues #XXX, #YYY

**Key Decisions:**
- Used .DoNotHaveNameMatching() for exemptions (simpler than alternatives)
- Implemented documentation tests for allowed dependencies
- Manual meta-testing approach (cannot automate without committing violations)

**Performance:**
- All 13 tests completed in X.XX seconds (target: <10s)

**Problems Encountered:**
- [Any issues and how they were resolved]

**Next Agent Recommendation:** Technical Writer (to review docs/architecture-rules.md)
```

---

## Implementation Order

Recommended sequence for implementation:

1. **Setup (Tasks 1-2)**: Package and file structure
   - Task 1: Add NetArchTest.Rules package
   - Task 2: Create test file structure

2. **Core Rules (Tasks 3-6)**: Implement forbidden dependency tests
   - Task 3: Parsing layer tests (no exemptions)
   - Task 4: Platforms layer test (4 exemptions)
   - Task 5: MarkdownGeneration layer test (3 exemptions)
   - Task 6: Remaining forbidden dependency tests (no exemptions)

3. **Supporting Implementation (Tasks 7-9)**: Documentation rules and helpers
   - Task 7: Allowed dependency tests (documentation)
   - Task 8: Naming convention tests
   - Task 9: Error message helper method

4. **Validation and Documentation (Tasks 10-14)**: Complete feature
   - Task 10: Create tracking issues
   - Task 11: Execute manual meta-testing
   - Task 12: Create architecture rules documentation
   - Task 13: Verify CI integration
   - Task 14: Update work protocol

**Rationale:**
- Setup tasks must come first (can't write tests without package/file)
- Core dependency rules are highest priority (protect architecture)
- Helper method can be added anytime but makes sense after first few tests
- Tracking issues needed before documentation (issues referenced in docs)
- Meta-testing validates everything works before documentation
- Documentation and CI verification happen after implementation complete

---

## Open Questions

### For Developer

1. **Exemption API**: Test both `.DoNotHaveNameMatching("ClassName")` and `.And().Are().Not().Named("ClassName")` patterns - choose the more readable approach

2. **Documentation Tests**: Choose between verification approach (`.Should().HaveDependencyOn()`) vs. simple pass-through (`Assert.Pass()`) for allowed dependency tests

3. **Error Message Formatting**: Adjust message format based on actual NetArchTest.Rules API output

### For Code Reviewer

1. **Exemption Justification**: Review that all 8 exemptions have clear comments with rationale
2. **Error Message Quality**: Spot-check that error messages are helpful to developers
3. **Test Coverage**: Verify all 13 tests are present and properly structured

---

## Definition of Done

Implementation is complete when:

- [ ] All 13 tasks completed and acceptance criteria met
- [ ] NetArchTest.Rules package added (Task 1)
- [ ] ArchitectureBoundaryTests.cs file created with 13 test methods (Tasks 2-8)
- [ ] Error message helper method implemented (Task 9)
- [ ] 8 known violations exempted with justification comments (Tasks 4-5)
- [ ] 2 tracking issues created for technical debt (Task 10)
- [ ] Manual meta-testing validation completed and documented (Task 11)
- [ ] docs/architecture-rules.md created and complete (Task 12)
- [ ] All tests pass locally and in CI (Task 13)
- [ ] Work protocol updated with completion summary (Task 14)
- [ ] Tests execute in <10 seconds (verified in Task 11)
- [ ] Changes committed to feature branch
- [ ] Ready for Code Reviewer handoff

---

## Success Metrics

- **Test Coverage**: 13 tests implemented (10 dependency + 3 naming)
- **Known Violations**: 8 files exempted with tracking issues
- **Performance**: All tests complete in <10 seconds
- **CI Integration**: Tests run automatically without special configuration
- **Documentation**: Complete architecture-rules.md with all sections
- **Validation**: Manual meta-testing confirms tests detect violations
