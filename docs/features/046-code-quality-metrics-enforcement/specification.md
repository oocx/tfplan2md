# Feature: Code Quality Metrics Enforcement

## Overview

Implement automated enforcement of code quality metrics to ensure maintainable, readable code throughout the codebase. This feature addresses issue #328 (file length guideline violations) and adds automated checks for cyclomatic complexity, maintainability scores, and line length.

## User Goals

- **Maintainers** want to prevent code quality degradation through automated enforcement
- **Developers** want clear, measurable guidelines for code complexity and maintainability
- **Code reviewers** want automated checks to reduce manual review burden for quality issues
- **Future contributors** want consistent, maintainable code that's easy to understand and modify

This matters because:
- Large files (1,076 lines) and complex methods reduce maintainability
- Manual enforcement of quality guidelines is inconsistent and time-consuming
- Automated enforcement prevents technical debt accumulation
- Clear thresholds help developers write better code upfront

## Scope

### In Scope

1. **File Length Enforcement**
   - Refactor 5 files exceeding 200-300 line guideline:
     - `ScribanHelpers.AzApi.cs` (1,076 lines) → split into focused partials
     - `ReportModel.cs` (671 lines) → split into separate class files
     - `VariableGroupViewModelFactory.cs` (572 lines) → extract helper classes
     - `ResourceSummaryBuilder.cs` (471 lines) → extract helper classes
     - `AzureRoleDefinitionMapper.Roles.cs` (485 lines) → extract/refactor as needed
   - Target: 300 lines max, ideally under 250 lines per file

2. **Cyclomatic Complexity Enforcement**
   - Configure SonarAnalyzer rule S1541 (cyclomatic complexity)
   - Threshold: Maximum 15 per method
   - Severity: Error (build fails)
   - Applies to all production code (not test code)

3. **Line Length Enforcement**
   - Configure .editorconfig max_line_length
   - Threshold: 160 characters maximum
   - Severity: Error (build fails)
   - Applies to C# files (*.cs)

4. **Maintainability Index Enforcement**
   - Configure Visual Studio Code Metrics (CA1505, CA1506)
   - Threshold: Minimum maintainability index of 20 (A grade on 0-100 scale)
   - Applies to both method level (CA1505) and class level (CA1506)
   - Severity: Error (build fails)

5. **Suppression Policy**
   - Individual methods/classes may be exempted from specific thresholds
   - Requires explicit suppression attribute with justification comment
   - Requires maintainer approval before merge
   - Suppression must document why the violation is necessary

### Out of Scope

- Changes to existing analyzer configuration beyond these specific metrics
- Refactoring files under 400 lines (unless related to complexity/maintainability violations)
- Test file length restrictions (tests follow different guidelines)
- Performance impact analysis of added analyzers
- Custom analyzer development (using existing analyzers only)
- Automated refactoring tools (manual refactoring by developers)

## User Experience

### For Developers

**Build-time feedback:**
```bash
# Developer writes method with complexity 16
dotnet build

Error S1541: Method 'ProcessComplexLogic' has cyclomatic complexity of 16 which exceeds maximum of 15
  at ComplexProcessor.cs(42)

Build failed.
```

**Suppression with justification:**
```csharp
// Complex state machine requires 18 branches for RFC compliance
// Approved by maintainer in PR #346
[SuppressMessage("SonarAnalyzer.CSharp", "S1541:Cyclomatic complexity too high", 
    Justification = "State machine for RFC 9110 HTTP semantics requires explicit branch handling")]
public HttpStatus ProcessRequest(HttpRequest request)
{
    // ... complex but necessary logic
}
```

**Line length violation:**
```bash
# Developer writes line exceeding 160 characters
dotnet build

Error IDE0055: Line exceeds maximum length of 160 characters
  at ReportGenerator.cs(128)
```

### For Maintainers

**Reviewing suppression requests:**
- Agent proposes suppression with justification comment
- Maintainer evaluates if complexity is truly necessary
- Maintainer suggests alternatives (extract method, simplify logic) if possible
- Maintainer approves suppression only when refactoring would harm readability

**Quality metrics dashboard** (no new tooling, just awareness):
- Build output shows violations clearly
- CI logs capture all quality metric violations
- Violations block PR merge automatically

## Success Criteria

### File Length Refactoring
- [ ] `ScribanHelpers.AzApi.cs` split into ≤4 partial files, each under 300 lines
- [ ] `ReportModel.cs` split into separate class files, each under 300 lines
- [ ] `VariableGroupViewModelFactory.cs` refactored to under 400 lines (target 300)
- [ ] `ResourceSummaryBuilder.cs` refactored to under 400 lines (target 300)
- [ ] `AzureRoleDefinitionMapper.Roles.cs` refactored to under 400 lines (target 300)

### Cyclomatic Complexity
- [ ] SonarAnalyzer S1541 configured with threshold=15, severity=error
- [ ] Configuration added to .editorconfig
- [ ] All existing methods comply OR have justified suppressions
- [ ] Build fails with clear message when threshold exceeded

### Line Length
- [ ] .editorconfig configured with `max_line_length = 160` for *.cs files
- [ ] IDE0055 configured as error
- [ ] All existing code complies (lines ≤160 characters)
- [ ] Build fails when line length exceeded

### Maintainability Index
- [ ] CA1505 (method maintainability) configured with threshold=20, severity=error
- [ ] CA1506 (class maintainability) configured with threshold=20, severity=error  
- [ ] Configuration added to .editorconfig or Directory.Build.props
- [ ] All existing code complies OR has justified suppressions
- [ ] Build fails with clear message when threshold not met

### General
- [ ] All existing tests pass after refactoring
- [ ] No functional changes to application behavior
- [ ] CI build enforces all quality thresholds
- [ ] Documentation updated with suppression policy

## Open Questions

1. **SonarAnalyzer S1541 configuration**: Does SonarAnalyzer support configurable complexity thresholds via .editorconfig, or do we need a separate configuration file?

2. **CA1505/CA1506 availability**: Are these Code Analysis rules available in the Microsoft.CodeAnalysis.NetAnalyzers package currently referenced, or do they require an additional package?

3. **Test file exemptions**: Should test files be explicitly exempted from cyclomatic complexity rules (they're already exempted from line count), given complex test setup scenarios?

4. **Baseline violations**: Should we create a baseline of current violations and fix incrementally, or fix all violations before enabling error severity?

5. **Line length exceptions**: Should string literals (e.g., long URLs, error messages) be exempted from line length limits?
