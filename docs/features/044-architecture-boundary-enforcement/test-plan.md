# Test Plan: Architecture Boundary Enforcement with Tests

## Overview

This test plan covers the architecture boundary enforcement feature using NetArchTest.Rules. The feature implements 13 automated architecture rules (10 dependency rules + 3 naming conventions) to prevent architectural violations during development. The tests verify layer boundaries are maintained, exemptions are properly handled, and error messages provide clear guidance to developers.

**Related Documents:**
- Feature Specification: `docs/features/044-architecture-boundary-enforcement/specification.md`
- Architecture Design: `docs/features/044-architecture-boundary-enforcement/architecture.md`
- ADR-007: `docs/adr-007-architecture-boundary-enforcement.md`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| NetArchTest.Rules package (1.3.2+) added to test project | TC-01 | Configuration |
| Architecture tests in `Architecture/ArchitectureBoundaryTests.cs` | TC-02 | Structural |
| All layer dependency rules verified (10 rules) | TC-03 through TC-12 | Unit |
| Naming convention rules verified (3 rules) | TC-13 through TC-15 | Unit |
| Tests run automatically in CI | TC-16 | Integration |
| Tests integrate with TUnit (no special runner) | TC-17 | Integration |
| Failed tests block PR merge with clear errors | TC-18 | Integration |
| `docs/architecture-rules.md` documents all rules | TC-19 | Documentation |
| Current codebase passes OR violations documented | TC-20 | Exemption |
| Tests execute in under 10 seconds | TC-21 | Performance |

## Test Strategy

### Testing Philosophy

Architecture tests are **meta-tests** that verify the codebase structure itself, not runtime behavior. The testing approach must ensure:

1. **Rules Detect Violations**: Each rule correctly identifies when code violates architectural boundaries
2. **Rules Allow Valid Code**: Each rule doesn't produce false positives for valid code
3. **Exemptions Work**: Known violations are properly exempted without breaking the tests
4. **Error Messages Help**: Violations produce clear, actionable error messages
5. **Performance Is Acceptable**: All tests complete in <10 seconds

### Test Types

#### 1. Rule Validation Tests (TC-03 through TC-15)

Each architectural rule is implemented as a single TUnit test method that:
- Uses NetArchTest.Rules to verify the rule
- Has a descriptive test name following the pattern: `<Layer>_ShouldNotDependOn_<TargetLayer>`
- Produces clear error messages when violated
- Includes exemptions for known violations with justification comments

**Implementation Pattern:**
```csharp
[Test]
public void Parsing_ShouldNotDependOn_MarkdownGeneration()
{
    var result = Types.InCurrentDomain()
        .That().ResideInNamespace("Oocx.TfPlan2Md.Parsing")
        .And().DoNotHaveNameMatching("TfPlanJsonContext") // Exempt: JSON source generation limitation
        .ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.MarkdownGeneration")
        .GetResult();
    
    Assert.That(result.IsSuccessful).IsTrue(
        CreateErrorMessage(
            "Parsing layer must not depend on MarkdownGeneration",
            "Parsing is a core domain layer and should not know about rendering concerns.",
            result.FailingTypes));
}
```

#### 2. Meta-Tests (TC-20)

**Purpose**: Verify that exemptions are working correctly and tests are actually detecting violations.

**Approach**: 
- Temporarily remove an exemption and verify the test fails
- Temporarily add a violation and verify the test detects it
- **Important**: These meta-tests are manual validation during development, not automated tests in CI

**Why Not Automated**: We cannot commit code with violations to test that tests detect violations. Meta-tests would require:
- Creating intentional violations (fails CI)
- Disabling the tests to commit violations (defeats the purpose)
- Complex test fixture manipulation (brittle and hard to maintain)

**Manual Validation Process** (documented in work protocol):
1. Developer temporarily removes exemption for `TfPlanJsonContext.cs`
2. Runs tests locally: `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*`
3. Verifies `Parsing_ShouldNotDependOn_Platforms` test fails with clear error message
4. Restores exemption and verifies test passes
5. Documents validation in work protocol

#### 3. Integration Tests (TC-16, TC-17, TC-18)

Verify architecture tests work correctly in the CI pipeline:
- Tests discovered by TUnit automatically
- Tests run as part of standard `dotnet test` command
- Failed tests produce clear output in CI logs
- Failed tests block PR merge

#### 4. Performance Tests (TC-21)

Verify all 13 architecture tests complete in <10 seconds total:
- Measure execution time of entire `ArchitectureBoundaryTests` class
- Use `scripts/test-with-timeout.sh` to enforce timeout
- Monitor performance over time as codebase grows

## Test Cases

### Configuration Tests

#### TC-01: NetArchTest_Package_IsAdded

**Type:** Configuration

**Description:**
Verifies that NetArchTest.Rules NuGet package (version 1.3.2 or later) is added to the test project.

**Preconditions:**
- None

**Test Steps:**
1. Inspect `src/tests/Oocx.TfPlan2Md.TUnit/Oocx.TfPlan2Md.TUnit.csproj`
2. Verify `<PackageReference Include="NetArchTest.Rules" Version="1.3.2" />` (or later) exists

**Expected Result:**
Package reference exists with correct version constraint.

**Test Data:**
N/A

---

#### TC-02: ArchitectureTests_File_Exists

**Type:** Structural

**Description:**
Verifies that architecture test file exists in the correct location.

**Preconditions:**
- None

**Test Steps:**
1. Check that `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs` exists
2. Verify file contains a class named `ArchitectureBoundaryTests`
3. Verify class is in namespace `Oocx.TfPlan2Md.TUnit.Architecture`

**Expected Result:**
File exists with correct class structure.

**Test Data:**
N/A

---

### Layer Dependency Rules (Forbidden Dependencies)

#### TC-03: Parsing_ShouldNotDependOn_MarkdownGeneration

**Type:** Unit

**Description:**
Verifies that the Parsing layer does not depend on the MarkdownGeneration layer.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.Parsing` namespace
2. Check they don't reference `Oocx.TfPlan2Md.MarkdownGeneration`
3. Verify result is successful

**Expected Result:**
No types in Parsing depend on MarkdownGeneration (prevents circular dependency).

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/Parsing/`

**Rationale:**
Parsing is a core domain layer and should not know about rendering concerns. This prevents circular dependencies and maintains clean separation between parsing and rendering.

**Known Exemptions:**
None (all code complies)

---

#### TC-04: Parsing_ShouldNotDependOn_CLI

**Type:** Unit

**Description:**
Verifies that the Parsing layer does not depend on the CLI layer.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.Parsing` namespace
2. Check they don't reference `Oocx.TfPlan2Md.CLI`
3. Verify result is successful

**Expected Result:**
No types in Parsing depend on CLI (core domain shouldn't know about UI).

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/Parsing/`

**Rationale:**
Core domain layer should be independent of user interface concerns, allowing parsing logic to be reused in different contexts (CLI, API, library).

**Known Exemptions:**
None (all code complies)

---

#### TC-05: Parsing_ShouldNotDependOn_Providers

**Type:** Unit

**Description:**
Verifies that the Parsing layer does not depend on the Providers layer.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.Parsing` namespace
2. Check they don't reference `Oocx.TfPlan2Md.Providers`
3. Verify result is successful

**Expected Result:**
No types in Parsing depend on Providers (core domain shouldn't know about provider-specific logic).

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/Parsing/`

**Rationale:**
Core parsing logic should be provider-agnostic. Provider-specific handling happens in the Providers layer, which depends on Parsing (not the reverse).

**Known Exemptions:**
None (all code complies)

---

#### TC-06: Platforms_ShouldNotDependOn_MarkdownGeneration

**Type:** Unit

**Description:**
Verifies that the Platforms layer does not depend on the MarkdownGeneration layer.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.Platforms` namespace
2. Exclude known exempt files (4 value formatter files)
3. Check remaining types don't reference `Oocx.TfPlan2Md.MarkdownGeneration`
4. Verify result is successful

**Expected Result:**
No types in Platforms depend on MarkdownGeneration, except for documented exemptions.

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/Platforms/`

**Rationale:**
Platforms layer should provide metadata only, not rendering concerns. This maintains separation between data and presentation.

**Known Exemptions:**
- `AzureValueFormatterRegistration.cs` - Temporary exemption pending refactoring
- `EnrichedAzureScopeFormatter.cs` - Temporary exemption pending refactoring
- `ManagementGroupIdFormatter.cs` - Temporary exemption pending refactoring
- `TenantIdFormatter.cs` - Temporary exemption pending refactoring

**Tracking Issue:** To be created during implementation (formatters should move to MarkdownGeneration layer)

---

#### TC-07: CodeAnalysis_ShouldNotDependOn_MarkdownGeneration

**Type:** Unit

**Description:**
Verifies that the CodeAnalysis layer does not depend on the MarkdownGeneration layer.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.CodeAnalysis` namespace
2. Check they don't reference `Oocx.TfPlan2Md.MarkdownGeneration`
3. Verify result is successful

**Expected Result:**
No types in CodeAnalysis depend on MarkdownGeneration (analysis is independent of rendering).

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/CodeAnalysis/`

**Rationale:**
Static analysis results should be independent of rendering concerns, allowing analysis to be used in different contexts.

**Known Exemptions:**
None (all code complies)

---

#### TC-08: MarkdownGeneration_ShouldNotDependOn_Providers

**Type:** Unit

**Description:**
Verifies that the MarkdownGeneration layer does not depend on the Providers layer.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.MarkdownGeneration` namespace
2. Exclude known exempt files (3 AOT script mapping files)
3. Check remaining types don't reference `Oocx.TfPlan2Md.Providers`
4. Verify result is successful

**Expected Result:**
No types in MarkdownGeneration depend on Providers, except for documented exemptions.

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/MarkdownGeneration/`

**Rationale:**
General rendering logic should not depend on specific providers. Provider-specific rendering should happen in the Providers layer.

**Known Exemptions:**
- `LargeValueSummary.cs` - AOT script object mapping (temporary, pending refactoring)
- `ResourceChangeModel.cs` - AOT script object mapping (temporary, pending refactoring)
- `AotScriptObjectMapper.cs` - AOT script object mapping (temporary, pending refactoring)

**Tracking Issue:** To be created during implementation (AOT mapping should use provider self-registration)

---

#### TC-09: Diagnostics_ShouldNotDependOn_AnyLayer

**Type:** Unit

**Description:**
Verifies that the Diagnostics layer (cross-cutting concern) has no dependencies on domain layers.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.Diagnostics` namespace
2. Check they don't reference any domain layers (CLI, Parsing, MarkdownGeneration, Providers, Platforms)
3. Verify result is successful

**Expected Result:**
Diagnostics layer is fully independent (utility layer).

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/Diagnostics/`

**Rationale:**
Cross-cutting concerns like diagnostics should not depend on domain layers, ensuring they can be used anywhere without circular dependencies.

**Known Exemptions:**
None (all code complies)

---

### Layer Dependency Rules (Allowed Dependencies - Documentation Tests)

#### TC-10: CLI_CanDependOn_AllLayers

**Type:** Unit

**Description:**
Documents that the CLI layer is allowed to depend on all other layers (orchestration layer).

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.CLI` namespace
2. Verify they CAN reference other layers (no restriction)
3. Test passes (documentation only, no actual restrictions)

**Expected Result:**
Test passes, documenting the allowed dependency direction.

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/CLI/`

**Rationale:**
CLI is the top-level orchestration layer and must be able to coordinate all other layers. This is explicitly allowed.

**Implementation Note:**
This is a documentation test - it verifies that there are NO restrictions on CLI dependencies. The test can either be a no-op that always passes, or it can verify that CLI actually does depend on multiple layers (proving the architecture is correct).

---

#### TC-11: MarkdownGeneration_CanDependOn_Parsing

**Type:** Unit

**Description:**
Documents that the MarkdownGeneration layer is allowed to depend on Parsing (rendering needs parsed data).

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.MarkdownGeneration` namespace
2. Verify they CAN reference `Oocx.TfPlan2Md.Parsing` (no restriction)
3. Test passes (documentation only)

**Expected Result:**
Test passes, documenting the allowed dependency direction.

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/MarkdownGeneration/`

**Rationale:**
Rendering logic needs access to parsed domain models to generate output. This dependency is expected and correct.

---

#### TC-12: Providers_CanDependOn_ParsingAndMarkdownGeneration

**Type:** Unit

**Description:**
Documents that the Providers layer is allowed to depend on both Parsing and MarkdownGeneration (provider-specific templates extend base rendering).

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.Providers` namespace
2. Verify they CAN reference `Oocx.TfPlan2Md.Parsing` and `Oocx.TfPlan2Md.MarkdownGeneration`
3. Test passes (documentation only)

**Expected Result:**
Test passes, documenting the allowed dependency direction.

**Test Data:**
Production code in `src/Oocx.TfPlan2Md/Providers/`

**Rationale:**
Provider-specific rendering extends base rendering capabilities and needs access to both parsed models and rendering infrastructure.

---

### Naming Convention Rules

#### TC-13: Exceptions_ShouldHave_ExceptionSuffix

**Type:** Unit

**Description:**
Verifies that all exception classes end with "Exception" suffix.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types that inherit from `System.Exception` in `Oocx.TfPlan2Md` namespace
2. Verify all names end with "Exception"
3. Verify result is successful

**Expected Result:**
All exception classes follow naming convention.

**Test Data:**
All exception classes in production code

**Rationale:**
.NET naming conventions require exception classes to end with "Exception" for clarity and consistency.

**Known Exemptions:**
None (all code complies)

---

#### TC-14: Tests_ShouldHave_TestsSuffix

**Type:** Unit

**Description:**
Verifies that all test classes end with "Tests" suffix.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all types in `Oocx.TfPlan2Md.TUnit` namespace
2. Exclude helper classes and base classes
3. Verify test classes end with "Tests"
4. Verify result is successful

**Expected Result:**
All test classes follow naming convention.

**Test Data:**
All test classes in `src/tests/Oocx.TfPlan2Md.TUnit/`

**Rationale:**
Consistent test naming makes test discovery and organization easier.

**Known Exemptions:**
- `AssemblyInfo.cs` - Not a test class
- Helper classes in `Assertions/` - Utility classes, not tests

---

#### TC-15: Interfaces_ShouldHave_IPrefix

**Type:** Unit

**Description:**
Verifies that all interface names start with "I" prefix.

**Preconditions:**
- NetArchTest.Rules is available
- Test assembly is loaded

**Test Steps:**
1. Load all interfaces in `Oocx.TfPlan2Md` namespace
2. Verify all names start with "I"
3. Verify result is successful

**Expected Result:**
All interfaces follow naming convention.

**Test Data:**
All interfaces in production code

**Rationale:**
.NET naming conventions require interface names to start with "I" for immediate recognition.

**Known Exemptions:**
None (all code complies)

---

### Integration Tests

#### TC-16: ArchitectureTests_RunAutomatically_InCI

**Type:** Integration

**Description:**
Verifies that architecture tests run automatically as part of the CI pipeline.

**Preconditions:**
- Changes committed to feature branch
- PR created

**Test Steps:**
1. Create a PR with architecture test changes
2. Observe CI workflow execution
3. Verify architecture tests are discovered and executed
4. Verify test results appear in PR checks

**Expected Result:**
Architecture tests run automatically without special configuration.

**Test Data:**
PR validation workflow results

**Validation Method:**
Manual inspection of CI logs during feature implementation.

---

#### TC-17: ArchitectureTests_IntegrateWith_TUnit

**Type:** Integration

**Description:**
Verifies that architecture tests integrate seamlessly with TUnit test runner.

**Preconditions:**
- NetArchTest.Rules is added
- Architecture tests are written

**Test Steps:**
1. Run `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/`
2. Verify architecture tests are discovered
3. Verify tests run without special configuration
4. Verify results are reported correctly

**Expected Result:**
Architecture tests appear in test output alongside other tests.

**Test Data:**
Local test execution results

**Validation Method:**
```bash
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/* --output Detailed
```

---

#### TC-18: ArchitectureTests_Failure_BlocksPR

**Type:** Integration

**Description:**
Verifies that failed architecture tests block PR merge with clear error messages.

**Preconditions:**
- Architecture tests are implemented
- CI is configured

**Test Steps:**
1. Intentionally introduce an architectural violation (manual test during development)
2. Commit and push to feature branch
3. Observe CI failure
4. Verify error message is clear and actionable
5. Revert violation and verify CI passes

**Expected Result:**
PR validation fails with clear error message indicating which rule was violated and which types violate the rule.

**Test Data:**
CI failure logs

**Validation Method:**
Manual validation during development by temporarily introducing a violation.

**Example Error Message:**
```
Architecture Violation: Parsing layer must not depend on MarkdownGeneration

Rationale: Parsing is a core domain layer and should not know about rendering concerns.

Violations found in:
  - Oocx.TfPlan2Md.Parsing.SomeClass

See docs/architecture-rules.md for guidance.
Related ADR: docs/adr-007-architecture-boundary-enforcement.md
```

---

### Documentation Tests

#### TC-19: ArchitectureRules_Documentation_Exists

**Type:** Documentation

**Description:**
Verifies that `docs/architecture-rules.md` documents all enforced rules with rationale.

**Preconditions:**
- None

**Test Steps:**
1. Check that `docs/architecture-rules.md` exists
2. Verify it contains sections for:
   - Overview
   - Layer Definitions
   - Dependency Rules (all 10 rules)
   - Naming Convention Rules (all 3 rules)
   - Known Exemptions (all 8 files)
   - Violation Resolution Process
   - References
3. Verify each rule includes rationale

**Expected Result:**
Documentation is complete and comprehensive.

**Test Data:**
N/A

**Validation Method:**
Manual review during Technical Writer phase.

---

### Exemption Tests

#### TC-20: KnownViolations_Are_Exempted

**Type:** Exemption Validation

**Description:**
Verifies that all 8 known violations are properly exempted and tests pass with exemptions in place.

**Preconditions:**
- Architecture tests are implemented
- All exemptions are in place

**Test Steps:**
1. Run all architecture tests
2. Verify all tests pass
3. Verify known violations are documented in test code with justification comments

**Expected Result:**
All tests pass with exemptions in place.

**Test Data:**
- `TfPlanJsonContext.cs` (Parsing → Platforms)
- `AzureValueFormatterRegistration.cs` (Platforms → MarkdownGeneration)
- `EnrichedAzureScopeFormatter.cs` (Platforms → MarkdownGeneration)
- `ManagementGroupIdFormatter.cs` (Platforms → MarkdownGeneration)
- `TenantIdFormatter.cs` (Platforms → MarkdownGeneration)
- `LargeValueSummary.cs` (MarkdownGeneration → Providers)
- `ResourceChangeModel.cs` (MarkdownGeneration → Providers)
- `AotScriptObjectMapper.cs` (MarkdownGeneration → Providers)

**Exemption Pattern:**
```csharp
.And().DoNotHaveNameMatching("TfPlanJsonContext") // Exempt: JSON source generation limitation (ADR-007)
```

**Validation Method:**
Developer runs tests locally and verifies they pass. Also validates that removing an exemption causes the test to fail (proves detection works).

---

### Performance Tests

#### TC-21: ArchitectureTests_Complete_Under10Seconds

**Type:** Performance

**Description:**
Verifies that all architecture tests complete in under 10 seconds total.

**Preconditions:**
- All architecture tests are implemented

**Test Steps:**
1. Run architecture tests with timing: `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*`
2. Measure total execution time
3. Verify time is <10 seconds

**Expected Result:**
All 13 architecture tests complete in <10 seconds (target: 2-5 seconds).

**Test Data:**
Test execution timing

**Validation Method:**
```bash
time scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*
```

**Performance Budget:**
- Individual test: <1 second
- Total suite: <10 seconds
- CI overhead: Negligible (tests run with other tests)

---

## Test Data Requirements

### Production Code

All tests use the actual production codebase as test data:
- `src/Oocx.TfPlan2Md/` - Production code to analyze
- All namespaces and types in the project

### No External Test Data Files

Architecture tests don't require external test data files - they analyze the codebase structure directly.

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| CompositionRoot and Program.cs | Excluded from layer rules (orchestration entry points) | Implicit in TC-03 through TC-09 |
| Test projects | Excluded from rules (test code can depend on anything) | Automatic (tests only check production namespace) |
| Cross-cutting concerns (Diagnostics, RenderTargets) | Can be depended on by any layer | TC-09 |
| Third-party dependencies | Excluded from analysis | Automatic (NetArchTest only loads project assemblies) |
| Partial namespace matches | Use exact namespace matching to avoid false positives | All dependency tests |
| Generic types and nested classes | NetArchTest handles correctly by default | All tests |

---

## Error Message Validation

### Error Message Requirements

Each architecture test must produce error messages with:
1. **Rule Statement**: Clear description of what is forbidden/required
2. **Rationale**: Why this rule exists (architectural principle)
3. **Violations**: List of specific types that violate the rule
4. **Guidance Link**: Reference to `docs/architecture-rules.md`
5. **ADR Reference**: Link to `docs/adr-007-architecture-boundary-enforcement.md`

### Example Error Message Format

```
Architecture Violation: Parsing layer must not depend on MarkdownGeneration

Rationale: Parsing is a core domain layer and should not know about rendering concerns.
This prevents circular dependencies and maintains clean separation between parsing and rendering.

Violations found in:
  - Oocx.TfPlan2Md.Parsing.TerraformPlanParser
  - Oocx.TfPlan2Md.Parsing.ResourceChangeParser

See docs/architecture-rules.md for guidance on architectural boundaries.
Related ADR: docs/adr-007-architecture-boundary-enforcement.md
```

### Error Message Test Cases

Error message validation is part of TC-18 (integration test). During manual validation:
1. Temporarily introduce a violation
2. Run tests and observe error message
3. Verify all 5 required components are present
4. Verify message is clear and actionable
5. Verify references are correct and links exist

---

## Manual Validation Checklist

Since architecture tests are meta-tests that verify codebase structure, some validations must be done manually during development:

### During Implementation (Developer)

- [ ] **Violation Detection**: Temporarily remove an exemption and verify test fails
  - Remove `.And().DoNotHaveNameMatching("TfPlanJsonContext")` from TC-06
  - Run tests: `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/Platforms_ShouldNotDependOn_MarkdownGeneration`
  - Verify test fails with clear error message listing `TfPlanJsonContext`
  - Restore exemption and verify test passes

- [ ] **False Positives**: Verify tests don't fail on valid code
  - Run all tests: `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*`
  - Verify all tests pass
  - Review any failures and verify they are real violations, not false positives

- [ ] **Error Message Quality**: Introduce violation and verify error message
  - Temporarily add `using Oocx.TfPlan2Md.MarkdownGeneration;` to a Parsing class
  - Run tests and verify error message contains all 5 required components
  - Remove violation

- [ ] **Performance**: Measure execution time
  - Run: `time dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*`
  - Verify time is <10 seconds
  - Document actual time in work protocol

### During Code Review (Code Reviewer)

- [ ] **Exemption Justification**: Review all exemptions have clear comments
- [ ] **Error Messages**: Spot-check error message format matches requirements
- [ ] **Test Coverage**: Verify all 13 tests are present (10 dependency + 3 naming)
- [ ] **Documentation**: Verify `docs/architecture-rules.md` is complete

### During CI Integration (Release Manager)

- [ ] **CI Execution**: Verify tests run in PR validation workflow
- [ ] **Failure Blocks PR**: Verify failed tests prevent merge
- [ ] **Test Output**: Verify test results appear correctly in PR checks

---

## Integration with CI

### PR Validation Workflow

Architecture tests run automatically as part of the existing test suite:

```yaml
# .github/workflows/pr-validation.yml (no changes needed)
- name: Test
  run: dotnet test --configuration Release --no-build --verbosity normal
```

### Expected CI Behavior

1. **On every commit to PR**:
   - Architecture tests run with all other tests
   - Results appear in PR checks
   - Execution time adds ~2-5 seconds to test suite

2. **On architecture test failure**:
   - PR validation status: ❌ Failed
   - Test output shows which rule was violated
   - Test output lists violating types
   - Error message provides guidance on how to fix

3. **On architecture test success**:
   - PR validation status: ✅ Passed
   - Architecture boundaries verified
   - No impact on merge process

---

## Open Questions

### Resolved During Architecture Phase

1. ✅ **Library selection**: NetArchTest.Rules chosen for TUnit compatibility
2. ✅ **Current violations**: 8 files documented with exemption strategy
3. ✅ **Test structure**: Single file with 13 test methods
4. ✅ **Performance target**: <10 seconds confirmed as acceptable

### For Developer to Resolve

1. **Tracking Issues**: Create GitHub issues for each exemption category:
   - Issue #XXX: Refactor value formatters from Platforms to MarkdownGeneration
   - Issue #XXX: Refactor AOT script mapping to use provider self-registration

2. **Exemption Pattern**: Confirm exact NetArchTest.Rules API for exclusions:
   - `.DoNotHaveNameMatching("ClassName")` - Preferred?
   - `.And().Are().Not().Named("ClassName")` - Alternative?
   - Test both and choose most readable

---

## Definition of Done

Testing is complete when:

- [ ] All 13 architecture rules are implemented as tests
- [ ] All tests pass with documented exemptions
- [ ] Error messages follow required format
- [ ] Tests run in CI automatically
- [ ] Tests complete in <10 seconds
- [ ] Manual validation checklist completed
- [ ] Known violations documented with tracking issues
- [ ] `docs/architecture-rules.md` created and complete
- [ ] Work protocol updated with validation results

---

## References

- Feature Specification: `docs/features/044-architecture-boundary-enforcement/specification.md`
- Architecture Design: `docs/features/044-architecture-boundary-enforcement/architecture.md`
- ADR-007: `docs/adr-007-architecture-boundary-enforcement.md`
- Testing Strategy: `docs/testing-strategy.md`
- NetArchTest.Rules Documentation: https://github.com/BenMorris/NetArchTest
- TUnit Documentation: https://github.com/thomhurst/TUnit
