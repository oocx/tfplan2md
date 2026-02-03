# Test Plan: Enhanced Static Analysis with Multiple Analyzers

## Overview

This test plan validates the phased integration of four static code analyzers into the tfplan2md project:
- **Phase 1**: StyleCop.Analyzers (v1.2.0-beta.556)
- **Phase 2**: SonarAnalyzer.CSharp (v9.16.0)
- **Phase 3**: Meziantou.Analyzer (v2.0.127)
- **Phase 4**: Roslynator.Analyzers (v4.*)

**Special Testing Considerations**: This feature is unique because it tests **build tooling and configuration**, not application logic. Testing focuses on:
- Verifying analyzer packages are loaded correctly
- Confirming rules detect violations as expected
- Validating configuration propagates through `.editorconfig`
- Ensuring CI/CD integration enforces rules
- Measuring build performance impact

**Reference Documents**:
- [Specification](specification.md)
- [Architecture](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| AT1: StyleCop enforces documentation | TC-P1-01, TC-P1-02, TC-P1-03, TC-CI-01 | Integration, CI |
| AT2: SonarAnalyzer detects null risks | TC-P2-01, TC-P2-02, TC-P2-03 | Integration, CI |
| AT3: Meziantou enforces StringComparison | TC-P3-01, TC-P3-02, TC-P3-03 | Integration, CI |
| AT4: Dependabot proposes updates | TC-DEP-01, TC-DEP-02 | Manual Observation |
| AT5: Suppression requires comment | TC-SUP-01, TC-SUP-02 | Code Review |
| AT6: Build performance acceptable | TC-PERF-01, TC-PERF-02 | Performance |
| FR2: Centralized configuration | TC-CONF-01, TC-CONF-02 | Integration |
| NFR4: Existing builds continue to pass | TC-REG-01, TC-REG-02, TC-REG-03 | Regression |

## Test Strategy

### 1. Analyzer Installation Validation
**Approach**: Verify each analyzer package is correctly referenced in `Directory.Build.props` and loaded by the MSBuild analyzer host.

**Validation Method**:
```bash
# Check package reference exists
grep -A 2 "StyleCop.Analyzers" src/Directory.Build.props

# Verify analyzer DLL is loaded during build (verbose output)
dotnet build src/tfplan2md.slnx --verbosity detailed 2>&1 | grep -i "StyleCop"
```

### 2. Rule Detection Validation
**Approach**: Create intentional violations and verify the analyzer detects them.

**Validation Method**: For each phase, create a temporary test file with known violations, build, and verify the expected diagnostic is emitted.

### 3. Configuration Validation
**Approach**: Verify `.editorconfig` rules are applied with correct severity levels.

**Validation Method**:
```bash
# Build and capture warnings/errors
dotnet build src/tfplan2md.slnx 2>&1 | tee build-output.txt

# Verify specific rule severity (e.g., SA1600 as warning initially)
grep "SA1600" build-output.txt
```

### 4. Severity Escalation Validation
**Approach**: Verify warnings can be promoted to errors and build fails appropriately.

**Validation Method**: After violations are fixed, update `.editorconfig` to promote rules to `error` severity, introduce a violation, and verify build fails.

### 5. Suppression Mechanism Validation
**Approach**: Verify `#pragma warning disable` works correctly and suppressions require justification.

**Validation Method**: Add suppression directives and verify:
- Suppression prevents the diagnostic
- Code review enforces comment requirement

### 6. CI/CD Integration Validation
**Approach**: Verify PR validation workflow fails when violations are introduced.

**Validation Method**: Create a PR with intentional violations, verify CI build fails with diagnostic output.

### 7. Performance Validation
**Approach**: Measure build time before/after each analyzer is added.

**Validation Method**:
```bash
# Baseline (before analyzer)
time dotnet build src/tfplan2md.slnx --no-incremental

# After analyzer
time dotnet build src/tfplan2md.slnx --no-incremental

# Calculate percentage increase
```

**Target**: <20% increase per analyzer, <72 seconds total (baseline ~45-60s)

## Phase-Specific Test Cases

---

## Phase 1: StyleCop.Analyzers

### TC-P1-01: Analyzer Package Installation

**Type:** Integration

**Description:**
Verify StyleCop.Analyzers package is correctly added to `Directory.Build.props` and loaded by MSBuild.

**Preconditions:**
- Clean build environment (no build artifacts)
- `src/Directory.Build.props` contains StyleCop.Analyzers package reference

**Test Steps:**
1. Review `src/Directory.Build.props` and confirm:
   - Package reference exists: `<PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556">`
   - `PrivateAssets` and `IncludeAssets` are correctly configured
2. Run `dotnet restore src/tfplan2md.slnx`
3. Run `dotnet build src/tfplan2md.slnx --verbosity detailed 2>&1 | tee build-verbose.txt`
4. Search for StyleCop in verbose output: `grep -i "StyleCop" build-verbose.txt`

**Expected Result:**
- Package reference exists with exact version `1.2.0-beta.556`
- Restore succeeds without errors
- Verbose build output shows StyleCop analyzer loaded: 
  - `"Loading analyzer assembly .../StyleCop.Analyzers.dll"`
- No analyzer load failures

**Test Data:** N/A

---

### TC-P1-02: Documentation Rule Detection

**Type:** Integration

**Description:**
Verify StyleCop.Analyzers detects undocumented public methods (SA1600).

**Preconditions:**
- StyleCop.Analyzers package is installed
- `.editorconfig` has `dotnet_diagnostic.SA1600.severity = warning`

**Test Steps:**
1. Create temporary test file `src/Oocx.TfPlan2Md/TestViolation_P1.cs`:
   ```csharp
   namespace Oocx.TfPlan2Md.TestViolation;
   
   public class UndocumentedClass
   {
       public void UndocumentedMethod() { }
   }
   ```
2. Run `dotnet build src/tfplan2md.slnx 2>&1 | tee build-output.txt`
3. Check for SA1600 diagnostic: `grep "SA1600" build-output.txt`
4. Delete test file: `rm src/Oocx.TfPlan2Md/TestViolation_P1.cs`

**Expected Result:**
- Build emits warning: `warning SA1600: Elements should be documented`
- Warning identifies `UndocumentedClass` and `UndocumentedMethod`
- Build succeeds (warnings don't fail build initially)
- After deleting test file, clean build produces no SA1600 warnings

**Test Data:**
- Temporary violation file created during test (deleted afterward)

---

### TC-P1-03: Severity Configuration Validation

**Type:** Integration

**Description:**
Verify `.editorconfig` severity levels are applied correctly and can be escalated from `warning` to `error`.

**Preconditions:**
- StyleCop.Analyzers package is installed
- All existing SA1600 violations are fixed

**Test Steps:**
1. Verify `.editorconfig` has `dotnet_diagnostic.SA1600.severity = warning`
2. Create temporary test file with SA1600 violation (same as TC-P1-02)
3. Run `dotnet build src/tfplan2md.slnx` and verify warning is emitted (exit code 0)
4. Update `.editorconfig` to `dotnet_diagnostic.SA1600.severity = error`
5. Run `dotnet build src/tfplan2md.slnx` again
6. Verify build fails (exit code 1) with error
7. Restore `.editorconfig` to `warning` severity
8. Delete test file

**Expected Result:**
- Step 3: Build succeeds with SA1600 warning
- Step 5: Build fails with SA1600 error
- Error message clearly identifies missing documentation
- Reverting configuration restores original behavior

**Test Data:**
- Temporary violation file and configuration change (reverted afterward)

---

### TC-P1-04: File Header Rule Configuration

**Type:** Integration

**Description:**
Verify file header rule (SA1633) is configured as `none` since the project does not require file headers.

**Preconditions:**
- StyleCop.Analyzers package is installed

**Test Steps:**
1. Check `.editorconfig` for `dotnet_diagnostic.SA1633.severity = none`
2. Run `dotnet build src/tfplan2md.slnx 2>&1 | grep "SA1633"`
3. Verify no SA1633 diagnostics are emitted

**Expected Result:**
- `.editorconfig` explicitly sets SA1633 to `none`
- Build produces zero SA1633 warnings/errors
- Existing code without file headers builds cleanly

**Test Data:** N/A

---

## Phase 2: SonarAnalyzer.CSharp

### TC-P2-01: Analyzer Package Installation

**Type:** Integration

**Description:**
Verify SonarAnalyzer.CSharp package is correctly added and loaded.

**Preconditions:**
- Phase 1 (StyleCop) is complete and stable
- Clean build environment

**Test Steps:**
1. Review `src/Directory.Build.props` and confirm SonarAnalyzer.CSharp package reference
2. Run `dotnet restore src/tfplan2md.slnx`
3. Run `dotnet build src/tfplan2md.slnx --verbosity detailed 2>&1 | grep -i "SonarAnalyzer"`

**Expected Result:**
- Package reference exists with version `9.16.0`
- Restore succeeds
- Verbose build shows analyzer loaded: `"Loading analyzer assembly .../SonarAnalyzer.CSharp.dll"`

**Test Data:** N/A

---

### TC-P2-02: Null Reference Detection

**Type:** Integration

**Description:**
Verify SonarAnalyzer detects potential null pointer dereferences (S2259).

**Preconditions:**
- SonarAnalyzer.CSharp package is installed
- `.editorconfig` has `dotnet_diagnostic.S2259.severity = warning`

**Test Steps:**
1. Create temporary test file `src/Oocx.TfPlan2Md/TestViolation_P2.cs`:
   ```csharp
   namespace Oocx.TfPlan2Md.TestViolation;
   
   public class NullReferenceTest
   {
       public void PotentialNullDeref(string? input)
       {
           var length = input.Length; // Potential null dereference
       }
   }
   ```
2. Run `dotnet build src/tfplan2md.slnx 2>&1 | grep "S2259"`
3. Delete test file

**Expected Result:**
- Build emits warning: `warning S2259: Null pointer dereference`
- Warning identifies `input.Length` as potentially null
- Build succeeds (warning, not error initially)

**Test Data:**
- Temporary violation file (deleted afterward)

---

### TC-P2-03: Cognitive Complexity Detection

**Type:** Integration

**Description:**
Verify SonarAnalyzer detects high cognitive complexity (S3776).

**Preconditions:**
- SonarAnalyzer.CSharp package is installed

**Test Steps:**
1. Create temporary test file with deeply nested logic (intentionally complex)
2. Run `dotnet build src/tfplan2md.slnx 2>&1 | grep "S3776"`
3. Verify complexity warning is emitted or suppressed appropriately
4. Delete test file

**Expected Result:**
- If cognitive complexity exceeds threshold, S3776 warning is emitted
- Warning provides cognitive complexity score
- No false positives on reasonably complex methods

**Test Data:**
- Temporary high-complexity method

---

## Phase 3: Meziantou.Analyzer

### TC-P3-01: Analyzer Package Installation

**Type:** Integration

**Description:**
Verify Meziantou.Analyzer package is correctly added and loaded.

**Preconditions:**
- Phases 1-2 are complete and stable

**Test Steps:**
1. Review `src/Directory.Build.props` and confirm Meziantou.Analyzer package reference
2. Run `dotnet restore src/tfplan2md.slnx`
3. Run `dotnet build src/tfplan2md.slnx --verbosity detailed 2>&1 | grep -i "Meziantou"`

**Expected Result:**
- Package reference exists with version `2.0.127`
- Restore succeeds
- Verbose build shows analyzer loaded

**Test Data:** N/A

---

### TC-P3-02: StringComparison Enforcement

**Type:** Integration

**Description:**
Verify Meziantou.Analyzer enforces StringComparison parameter usage (MA0015).

**Preconditions:**
- Meziantou.Analyzer package is installed
- `.editorconfig` has `dotnet_diagnostic.MA0015.severity = warning`

**Test Steps:**
1. Create temporary test file `src/Oocx.TfPlan2Md/TestViolation_P3.cs`:
   ```csharp
   namespace Oocx.TfPlan2Md.TestViolation;
   
   public class StringComparisonTest
   {
       public bool CompareStrings(string a, string b)
       {
           return a.Equals(b); // Missing StringComparison parameter
       }
   }
   ```
2. Run `dotnet build src/tfplan2md.slnx 2>&1 | grep "MA0015"`
3. Delete test file

**Expected Result:**
- Build emits warning: `warning MA0015: Specify StringComparison`
- Warning suggests fix: `a.Equals(b, StringComparison.Ordinal)`
- Build succeeds (warning, not error initially)

**Test Data:**
- Temporary violation file (deleted afterward)

---

### TC-P3-03: ConfigureAwait Detection

**Type:** Integration

**Description:**
Verify Meziantou.Analyzer detects missing ConfigureAwait (MA0004).

**Preconditions:**
- Meziantou.Analyzer package is installed

**Test Steps:**
1. Create temporary test file with `await` without `ConfigureAwait(false)`
2. Run `dotnet build src/tfplan2md.slnx 2>&1 | grep "MA0004"`
3. Verify MA0004 warning or confirm it's suppressed if not applicable
4. Delete test file

**Expected Result:**
- If MA0004 is enabled, warning is emitted for missing ConfigureAwait
- If rule is disabled (console app, not library), no warning emitted
- Configuration clearly documents decision

**Test Data:**
- Temporary async method without ConfigureAwait

---

## Phase 4: Roslynator.Analyzers

### TC-P4-01: Analyzer Package Installation

**Type:** Integration

**Description:**
Verify Roslynator.Analyzers package is correctly added and loaded.

**Preconditions:**
- Phases 1-3 are complete and stable

**Test Steps:**
1. Review `src/Directory.Build.props` and confirm Roslynator.Analyzers package reference
2. Verify version pattern matches `4.*` (major version pinning)
3. Run `dotnet restore src/tfplan2md.slnx`
4. Run `dotnet build src/tfplan2md.slnx --verbosity detailed 2>&1 | grep -i "Roslynator"`

**Expected Result:**
- Package reference exists with version `4.*` or exact version like `4.12.11`
- Restore succeeds
- Verbose build shows analyzer loaded

**Test Data:** N/A

---

### TC-P4-02: Code Simplification Suggestions

**Type:** Integration

**Description:**
Verify Roslynator suggests code simplifications (e.g., RCS1036: Remove unnecessary blank line).

**Preconditions:**
- Roslynator.Analyzers package is installed

**Test Steps:**
1. Create temporary test file with unnecessary blank lines or verbose code
2. Run `dotnet build src/tfplan2md.slnx 2>&1 | grep "RCS"`
3. Verify Roslynator diagnostics are emitted or appropriately configured as `suggestion`
4. Delete test file

**Expected Result:**
- Roslynator rules emit `suggestion` or `warning` severity (not `error` by default)
- Suggestions are helpful and not overwhelming
- False positives are configured as `silent` in `.editorconfig`

**Test Data:**
- Temporary code with refactoring opportunities

---

## Configuration & Integration Tests

### TC-CONF-01: Centralized Configuration Enforcement

**Type:** Integration

**Description:**
Verify all analyzer packages are defined in `src/Directory.Build.props` and inherited by all projects.

**Preconditions:**
- All four analyzers are integrated

**Test Steps:**
1. Review `src/Directory.Build.props` and confirm all four analyzer package references
2. Verify no project-specific `.csproj` files override analyzer configuration
3. Run `dotnet list package --include-transitive` and verify analyzers appear for all projects
4. Confirm `.editorconfig` is at repository root (not per-project)

**Expected Result:**
- `Directory.Build.props` contains all analyzer packages with correct versions
- No `.csproj` files have analyzer-specific `<PackageReference>` elements
- All projects inherit analyzer configuration automatically
- Single `.editorconfig` at repository root

**Test Data:** N/A

---

### TC-CONF-02: EditorConfig Rule Propagation

**Type:** Integration

**Description:**
Verify `.editorconfig` rule configurations propagate to all C# files in the solution.

**Preconditions:**
- All analyzers are integrated
- `.editorconfig` has rules for all analyzers

**Test Steps:**
1. Review `.editorconfig` and confirm sections for:
   - StyleCop.Analyzers
   - SonarAnalyzer.CSharp
   - Meziantou.Analyzer
   - Roslynator.Analyzers
2. Create a test file in a nested directory (e.g., `src/Oocx.TfPlan2Md/Nested/Test.cs`)
3. Introduce violations for rules from all four analyzers
4. Build and verify all diagnostics are emitted

**Expected Result:**
- `.editorconfig` sections exist for all analyzers with documented severity levels
- Rules apply to files in nested directories (`.editorconfig` is hierarchical)
- No per-directory `.editorconfig` overrides (centralized configuration)

**Test Data:**
- Temporary nested test file (deleted afterward)

---

### TC-CI-01: PR Validation Enforces Analyzer Rules

**Type:** CI/CD Integration

**Description:**
Verify GitHub Actions PR validation workflow fails when analyzer rules are violated.

**Preconditions:**
- All analyzers are integrated with critical rules set to `error` severity
- PR validation workflow (`.github/workflows/pr-validation.yml`) is configured

**Test Steps:**
1. Create a feature branch with an intentional violation (e.g., undocumented public method)
2. Push branch and create a PR
3. Wait for PR validation workflow to run
4. Verify workflow fails at the "Build" step
5. Check workflow logs for diagnostic output
6. Fix violation, push again, and verify workflow passes

**Expected Result:**
- PR validation fails with exit code 1 at `dotnet build` step
- Workflow logs show clear diagnostic message (e.g., `error SA1600: Elements should be documented`)
- After fix, workflow passes successfully
- PR cannot be merged until build passes (branch protection enforced)

**Test Data:**
- Feature branch with intentional violation

**Note:** This test validates CI/CD integration automatically enforces analyzer rules via existing `TreatWarningsAsErrors=true` configuration.

---

### TC-CI-02: Format Check Includes Analyzer Warnings

**Type:** CI/CD Integration

**Description:**
Verify `dotnet format --verify-no-changes` includes analyzer diagnostics.

**Preconditions:**
- All analyzers are integrated

**Test Steps:**
1. Create a feature branch with a minor formatting violation (e.g., extra blank line)
2. Push and observe PR validation workflow
3. Verify `dotnet format` step catches the violation
4. Run `dotnet format --verify-no-changes` locally and confirm same behavior

**Expected Result:**
- `dotnet format --verify-no-changes` fails if code violates `.editorconfig` rules
- Analyzer-related style rules are enforced by format check
- Workflow clearly identifies formatting violations

**Test Data:**
- Feature branch with formatting violation

---

## Suppression Tests

### TC-SUP-01: Pragma Suppression Works

**Type:** Integration

**Description:**
Verify `#pragma warning disable` correctly suppresses analyzer diagnostics.

**Preconditions:**
- Any analyzer is integrated

**Test Steps:**
1. Create temporary test file with violation (e.g., undocumented method)
2. Build and confirm diagnostic is emitted
3. Add suppression:
   ```csharp
   #pragma warning disable SA1600 // Elements should be documented
   public void UndocumentedMethod() { }
   #pragma warning restore SA1600
   ```
4. Build and verify diagnostic is suppressed
5. Delete test file

**Expected Result:**
- Without suppression: Diagnostic is emitted
- With suppression: Diagnostic is suppressed, build succeeds
- Suppression is scoped correctly (only affects surrounded code)

**Test Data:**
- Temporary test file with suppression (deleted afterward)

---

### TC-SUP-02: Suppression Comment Policy Enforcement

**Type:** Code Review

**Description:**
Verify code review process enforces suppression comment requirements.

**Preconditions:**
- Suppression policy is documented in `CONTRIBUTING.md`

**Test Steps:**
1. Create a PR with suppression that lacks an explanatory comment:
   ```csharp
   #pragma warning disable SA1600
   public void Something() { }
   #pragma warning restore SA1600
   ```
2. Request code review from maintainer
3. Verify reviewer requests explanatory comment
4. Update suppression with justification:
   ```csharp
   #pragma warning disable SA1600 // Public for testing; documentation intentionally omitted
   public void Something() { }
   #pragma warning restore SA1600
   ```
5. Verify reviewer approves after comment added

**Expected Result:**
- PR is not approved until suppression has clear explanatory comment
- Reviewer identifies missing justification
- Process enforces policy without automated tooling (manual review)

**Test Data:**
- PR with suppression requiring review

**Note:** This is a **manual test** relying on code review discipline, not automated validation.

---

## Regression Tests

### TC-REG-01: Existing Tests Continue to Pass

**Type:** Regression

**Description:**
Verify all existing unit and integration tests pass after each analyzer is added.

**Preconditions:**
- Analyzer is newly added to `Directory.Build.props`
- No code changes other than analyzer configuration

**Test Steps:**
1. Run full test suite: `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`
2. Verify all tests pass (exit code 0)
3. Check test count matches baseline (no tests skipped or disabled)

**Expected Result:**
- All 370+ tests pass (exact count per `docs/testing-strategy.md`)
- No test failures introduced by analyzer
- Test execution time is not significantly impacted (<5% increase acceptable)

**Test Data:**
- Existing test suite in `src/tests/Oocx.TfPlan2Md.TUnit/`

---

### TC-REG-02: Existing Code Builds Without Errors

**Type:** Regression

**Description:**
Verify existing production code builds successfully with new analyzer (warnings allowed initially, errors prohibited).

**Preconditions:**
- Analyzer is newly added with all rules set to `warning` severity

**Test Steps:**
1. Run `dotnet build src/tfplan2md.slnx --configuration Release`
2. Verify build succeeds (exit code 0)
3. Capture warnings: `dotnet build src/tfplan2md.slnx 2>&1 | grep "warning" | wc -l`
4. Document warning count for triage

**Expected Result:**
- Build succeeds with warnings (not errors)
- Warnings are documented for review and fixing
- No build-breaking errors introduced by analyzer
- CI/CD pipeline passes (warnings don't fail build initially)

**Test Data:** N/A

---

### TC-REG-03: Docker Integration Tests Pass

**Type:** Regression

**Description:**
Verify Docker-based integration tests continue to pass after analyzer integration.

**Preconditions:**
- All analyzers are integrated
- Docker test image is built with `scripts/prepare-test-image.sh`

**Test Steps:**
1. Run Docker integration tests (subset of TUnit test suite)
2. Verify Docker container builds and runs successfully
3. Confirm CLI behavior is unchanged

**Expected Result:**
- Docker image builds without analyzer-related errors
- Docker integration tests pass
- Container size is not significantly increased (<5% acceptable)

**Test Data:**
- Existing Docker integration tests in `src/tests/Oocx.TfPlan2Md.TUnit/Docker/`

---

## Performance Tests

### TC-PERF-01: Build Time Impact Per Analyzer

**Type:** Performance

**Description:**
Measure build time impact of each analyzer phase and verify <20% increase per analyzer.

**Preconditions:**
- Clean environment with no build cache
- Baseline build time measured before Phase 1

**Test Steps:**
1. **Baseline**: Run `time dotnet build src/tfplan2md.slnx --no-incremental --configuration Release` (3 runs, average)
2. **After Phase 1**: Add StyleCop, run same command (3 runs, average)
3. **After Phase 2**: Add SonarAnalyzer, run same command (3 runs, average)
4. **After Phase 3**: Add Meziantou, run same command (3 runs, average)
5. **After Phase 4**: Add Roslynator, run same command (3 runs, average)
6. Calculate percentage increase per phase

**Expected Result:**
- **Baseline**: ~45-60 seconds (per architecture doc)
- **Per-phase increase**: <20% (e.g., 45s → 54s max per analyzer)
- **Total with all analyzers**: <72 seconds (45s * 1.2^4 ≈ 93s theoretical, but target is <72s)
- If any phase exceeds threshold, investigate and disable expensive rules

**Test Data:**
- Build time measurements recorded in PR descriptions

**Measurement Script:**
```bash
#!/bin/bash
echo "Measuring build time (3 runs)..."
for i in {1..3}; do
  dotnet clean src/tfplan2md.slnx > /dev/null 2>&1
  time dotnet build src/tfplan2md.slnx --no-incremental --configuration Release
done
```

---

### TC-PERF-02: Incremental Build Impact

**Type:** Performance

**Description:**
Verify incremental builds (after minor code changes) are not significantly slowed by analyzers.

**Preconditions:**
- All analyzers are integrated
- Initial full build completed

**Test Steps:**
1. Run full build: `dotnet build src/tfplan2md.slnx`
2. Make a trivial code change (e.g., add a comment to a single file)
3. Measure incremental build time: `time dotnet build src/tfplan2md.slnx`
4. Verify incremental build is fast (<10 seconds)

**Expected Result:**
- Incremental build completes in <10 seconds
- Analyzers only re-analyze changed files (not entire solution)
- Developer experience is not degraded

**Test Data:**
- Trivial code change in single file

---

## Dependabot Integration Tests

### TC-DEP-01: Analyzer Packages Included in Updates

**Type:** Manual Observation

**Description:**
Verify Dependabot creates PRs for analyzer package updates.

**Preconditions:**
- All analyzers are integrated
- Dependabot is enabled (already configured per `dependabot.yml`)

**Test Steps:**
1. Wait for Dependabot weekly check (or trigger manually if possible)
2. Check for PRs titled like "Bump StyleCop.Analyzers from X.Y.Z to A.B.C"
3. Review PR description and verify analyzer package is detected

**Expected Result:**
- Dependabot creates PRs for analyzer updates (when available)
- PRs include changelog links and version comparison
- No manual configuration needed (NuGet ecosystem already covered)

**Test Data:** N/A

**Note:** This is an **observational test** that validates integration over time.

---

### TC-DEP-02: Analyzer Update PRs Pass CI

**Type:** CI/CD Integration

**Description:**
Verify analyzer update PRs automatically run CI validation before merge.

**Preconditions:**
- Dependabot created an analyzer update PR

**Test Steps:**
1. Open Dependabot PR for analyzer update
2. Verify PR validation workflow runs automatically
3. Check that all steps pass (restore, build, test, format check)
4. Verify new analyzer version doesn't introduce breaking changes
5. Merge if CI passes, or investigate failures

**Expected Result:**
- PR validation runs automatically for Dependabot PRs
- CI detects if new analyzer version breaks build or introduces new errors
- Maintainer can review and merge with confidence

**Test Data:**
- Dependabot-generated analyzer update PR

---

## Edge Cases & Error Scenarios

### TC-EDGE-01: Conflicting Rules Across Analyzers

**Type:** Integration

**Description:**
Verify conflicting rules between analyzers are resolved via `.editorconfig`.

**Preconditions:**
- Multiple analyzers are integrated

**Test Steps:**
1. Identify a potential rule conflict (e.g., naming conventions)
2. Verify `.editorconfig` disables one conflicting rule and documents decision:
   ```ini
   # SA1101 conflicts with dotnet_style_qualification_for_method = false
   dotnet_diagnostic.SA1101.severity = none
   ```
3. Build and confirm only one rule is enforced

**Expected Result:**
- Conflicting rules are documented in `.editorconfig` comments
- One rule is disabled (`none` severity) with justification
- No ambiguous feedback to developers

**Test Data:** N/A

---

### TC-EDGE-02: Analyzer Fails to Load

**Type:** Error Handling

**Description:**
Verify build fails gracefully if an analyzer DLL cannot be loaded.

**Preconditions:**
- Corrupt or delete an analyzer DLL from the NuGet cache (manual intervention)

**Test Steps:**
1. Corrupt analyzer DLL (e.g., `rm ~/.nuget/packages/stylecop.analyzers/*/analyzers/dotnet/cs/*.dll`)
2. Run `dotnet build src/tfplan2md.slnx`
3. Observe error message
4. Restore packages: `dotnet restore --force-evaluate`

**Expected Result:**
- Build fails with clear error: `"Could not load analyzer assembly..."`
- Error message indicates which analyzer failed
- Restore fixes the issue

**Test Data:** N/A

**Note:** This is a **destructive test** best performed in isolated environment.

---

### TC-EDGE-03: .editorconfig Syntax Error

**Type:** Error Handling

**Description:**
Verify build detects and reports `.editorconfig` syntax errors.

**Preconditions:**
- Valid `.editorconfig` exists

**Test Steps:**
1. Introduce syntax error in `.editorconfig` (e.g., invalid rule name, missing `=`)
2. Run `dotnet build src/tfplan2md.slnx`
3. Check build output for warnings/errors
4. Restore valid `.editorconfig`

**Expected Result:**
- Build may emit warning about invalid `.editorconfig` entry
- Analyzer rules may not apply as expected
- IDE (VS Code, Visual Studio) may show errors in `.editorconfig` file

**Test Data:**
- Intentional `.editorconfig` syntax error (reverted afterward)

---

## Test Data Requirements

### Existing Test Data
- **Production code**: `src/Oocx.TfPlan2Md/` (used for regression testing)
- **Test suite**: `src/tests/Oocx.TfPlan2Md.TUnit/` (used for test pass validation)
- **CI configuration**: `.github/workflows/pr-validation.yml` (used for CI integration testing)

### Temporary Test Data (Created During Testing)
- **Violation test files**: `src/Oocx.TfPlan2Md/TestViolation_P*.cs` (intentional violations, deleted after test)
- **Build output logs**: `build-output.txt`, `build-verbose.txt` (captured during testing, not committed)
- **Timing measurements**: Recorded in PR descriptions or test reports

**Policy**: All temporary test files must be deleted after test execution. No test violation code should be committed to the repository.

---

## Non-Functional Tests

### NFR1: Build Performance Monitoring

**Test Cases:** TC-PERF-01, TC-PERF-02

**Success Criteria:**
- Full build time increase <20% per analyzer
- Total build time with all analyzers <72 seconds (baseline ~45-60s)
- Incremental builds remain fast (<10 seconds)

**Monitoring Approach:**
- Measure and record build times in each phase PR
- Compare against baseline in PR description
- Fail phase if threshold exceeded

---

### NFR2: Developer Experience

**Test Cases:** TC-P1-02, TC-P2-02, TC-P3-02, TC-P4-02 (diagnostic quality)

**Success Criteria:**
- Diagnostics are clear and actionable
- False positives are minimal (<5% of warnings)
- IDE feedback is helpful, not overwhelming

**Evaluation Approach:**
- Maintainer reviews diagnostic messages during implementation
- Gather feedback from code reviews
- Adjust `.editorconfig` severities based on value/noise ratio

---

### NFR3: Maintainability

**Test Cases:** TC-CONF-01, TC-CONF-02, TC-DEP-01

**Success Criteria:**
- Single source of truth for analyzer versions (`Directory.Build.props`)
- Single source of truth for rule configuration (`.editorconfig`)
- Dependabot updates work automatically

**Validation:**
- No project-specific analyzer overrides
- `.editorconfig` changes propagate to all files
- Update PRs are created automatically

---

## Open Questions

1. **Should StyleCop file header rule (SA1633) be enabled?**
   - Current assumption: No, project doesn't require file headers
   - Test: TC-P1-04 validates SA1633 is disabled

2. **Should Meziantou MA0004 (ConfigureAwait) apply to console app?**
   - Decision needed: Console apps often don't need ConfigureAwait
   - Test: TC-P3-03 validates configuration decision

3. **How many Roslynator rules should be promoted to `error`?**
   - Strategy: Most should remain `suggestion`, only critical consistency rules as `error`
   - Test: TC-P4-02 validates severity configuration

**Note:** These questions should be resolved during implementation. Test plan will validate whatever decisions are made.

---

## Rollback Testing

### TC-ROLLBACK-01: Remove Analyzer

**Type:** Rollback Validation

**Description:**
Verify an analyzer can be removed cleanly if it causes unresolvable issues.

**Preconditions:**
- Analyzer is integrated and causing problems (performance, false positives, etc.)

**Test Steps:**
1. Remove analyzer `<PackageReference>` from `src/Directory.Build.props`
2. Remove corresponding rules from `.editorconfig`
3. Run `dotnet restore --force-evaluate`
4. Run `dotnet build src/tfplan2md.slnx`
5. Run test suite

**Expected Result:**
- Build succeeds without the analyzer
- No orphaned `.editorconfig` rules cause warnings
- Tests pass
- CI pipeline passes

**Test Data:** N/A

**Note:** This test validates the rollback procedure documented in the architecture.

---

## Definition of Done

This test plan is complete when:

- [x] All acceptance criteria from specification.md have mapped test cases
- [x] Each phase (1-4) has installation, detection, and configuration tests
- [x] CI/CD integration is validated
- [x] Performance impact measurement is defined
- [x] Regression test coverage exists
- [x] Suppression mechanism is tested
- [x] Dependabot integration is validated
- [x] Edge cases and error scenarios are covered
- [x] Rollback procedure is tested

The **feature implementation** is complete when:

- [ ] All test cases in this plan pass
- [ ] Build time increase is <20% per architecture target
- [ ] Zero errors in CI builds (all promoted rules pass)
- [ ] Suppression policy is documented in `CONTRIBUTING.md`
- [ ] At least 5 real issues caught by analyzers in subsequent PRs (measured over time)

---

## Test Execution Notes

### Execution Order

1. **Pre-Phase Tests**: TC-REG-01, TC-REG-02 (establish baseline)
2. **Phase 1 Tests**: TC-P1-01 → TC-P1-04, TC-PERF-01
3. **Phase 2 Tests**: TC-P2-01 → TC-P2-03, TC-PERF-01
4. **Phase 3 Tests**: TC-P3-01 → TC-P3-03, TC-PERF-01
5. **Phase 4 Tests**: TC-P4-01 → TC-P4-02, TC-PERF-01
6. **Post-Integration Tests**: TC-CONF-01, TC-CONF-02, TC-CI-01, TC-CI-02, TC-PERF-02
7. **Ongoing Tests**: TC-DEP-01, TC-DEP-02 (observational, over weeks)

### Who Executes Tests

- **Developer Agent**: Runs all automated integration tests during implementation
- **Code Reviewer Agent**: Validates suppression policy (TC-SUP-02) during PR review
- **Maintainer**: Observes Dependabot integration (TC-DEP-01, TC-DEP-02) over time
- **CI/CD**: Automatically enforces analyzer rules (TC-CI-01, TC-CI-02)

### Test Environment

- **Local Development**: Developer workstation with .NET SDK 9.0 (per `src/global.json`)
- **CI Environment**: GitHub Actions Ubuntu runner (`ubuntu-latest` per `pr-validation.yml`)
- **Docker Environment**: For Docker integration tests (TC-REG-03)

### Test Artifacts

- **Build logs**: Captured for diagnostic validation
- **Performance measurements**: Recorded in PR descriptions
- **CI workflow results**: Visible in GitHub Actions UI
- **Test violation files**: Temporary, deleted after execution

---

## References

- **Feature Specification**: [specification.md](specification.md)
- **Architecture Document**: [architecture.md](architecture.md)
- **Testing Strategy**: `docs/testing-strategy.md`
- **CI/CD Configuration**: `.github/workflows/pr-validation.yml`
- **Current Analyzer Setup**: `src/Directory.Build.props`
- **Rule Configuration**: `.editorconfig`
- **StyleCop Rules**: <https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/DOCUMENTATION.md>
- **SonarAnalyzer Rules**: <https://rules.sonarsource.com/csharp/>
- **Meziantou.Analyzer Rules**: <https://github.com/meziantou/Meziantou.Analyzer/tree/main/docs>
- **Roslynator Rules**: <https://github.com/dotnet/roslynator/tree/main/docs>

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-22 | Quality Engineer Agent | Initial test plan based on approved specification and architecture |
