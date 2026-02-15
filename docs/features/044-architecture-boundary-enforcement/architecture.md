# Architecture: Architecture Boundary Enforcement with Tests

**Related ADR:** [ADR-007: Architecture Boundary Enforcement](../../adr-007-architecture-boundary-enforcement.md)

## Status

**Design Complete** - Ready for implementation

## Library Selection

**Selected:** NetArchTest.Rules (version 1.3.2 or later)

### Comparison Summary

| Aspect | NetArchTest.Rules | ArchUnitNET |
|--------|------------------|-------------|
| **TUnit Compatibility** | ✅ Perfect (framework-agnostic) | ⚠️ Requires manual integration |
| **API Complexity** | Simple, easy to learn | Complex, feature-rich |
| **Error Messages** | Good, actionable | Excellent, detailed |
| **Maintenance** | Stable, less active | Active, frequent updates |
| **Dependency Size** | Lightweight | Moderate |
| **Rule Expressiveness** | Sufficient for our needs | More powerful |

### Rationale

NetArchTest.Rules was chosen because:

1. **Framework Agnostic:** Explicitly designed to work with any .NET test framework, including TUnit, without requiring adapters
2. **Simplicity:** Straightforward API makes tests easy to write, read, and maintain
3. **Sufficient Capabilities:** Provides all rule types needed for our 10 dependency rules + 3 naming rules
4. **Proven:** Widely used in .NET community with stable implementation
5. **Performance:** Lightweight library with fast execution (<10 seconds expected)

ArchUnitNET offers more features but would require manual integration with TUnit and has a steeper learning curve without providing significant value for our use case.

## Current Violations Analysis

### Discovered Violations

Analysis of the codebase revealed **3 categories of violations** affecting **8 files**:

#### 1. Parsing → Platforms (JSON Source Generation)

**Files Affected:**
- `src/Oocx.TfPlan2Md/Parsing/TfPlanJsonContext.cs`

**Violation:**
```csharp
using Oocx.TfPlan2Md.Platforms.Azure;  // References PrincipalMappingFile

[JsonSerializable(typeof(PrincipalMappingFile))]
[JsonSerializable(typeof(MappingEntry))]
internal partial class TfPlanJsonContext : JsonSerializerContext
```

**Root Cause:** System.Text.Json source generation requires all serialized types to be referenced in a single `JsonSerializerContext`. `PrincipalMappingFile` is defined in Platforms but needs to be included in the same context as Terraform plan models.

**Recommendation:** **Document as acceptable exception**
- This is a tooling limitation, not an architectural flaw
- Moving `PrincipalMappingFile` to Parsing would create a reverse dependency
- Alternative solutions (multiple contexts, runtime serialization) have worse trade-offs
- Exempt this specific file from the Parsing → Platforms rule with clear justification

#### 2. Platforms → MarkdownGeneration (Value Formatters)

**Files Affected:**
- `src/Oocx.TfPlan2Md/Platforms/Azure/AzureValueFormatterRegistration.cs`
- `src/Oocx.TfPlan2Md/Platforms/Azure/EnrichedAzureScopeFormatter.cs`
- `src/Oocx.TfPlan2Md/Platforms/Azure/ManagementGroupIdFormatter.cs`
- `src/Oocx.TfPlan2Md/Platforms/Azure/TenantIdFormatter.cs`

**Violation:**
```csharp
using Oocx.TfPlan2Md.MarkdownGeneration;           // ValueFormatter interface
using Oocx.TfPlan2Md.MarkdownGeneration.Services;  // IValueFormatter, ValueFormatterRegistry
```

**Root Cause:** Value formatters implement rendering logic but are located in the Platforms layer. The Platforms layer should provide metadata only, not rendering concerns.

**Recommendation:** **Future refactoring required**
- **Option A:** Move formatters to MarkdownGeneration and reference Platforms (allowed direction)
- **Option B:** Introduce a new "Formatters" layer between Platforms and MarkdownGeneration
- **Option C:** Abstract the formatter interface to remove MarkdownGeneration dependency
- **Short-term:** Document as known violation, create tracking issue for refactoring
- **Test exemption:** Exclude these 4 files from Platforms → MarkdownGeneration rule temporarily

#### 3. MarkdownGeneration → Providers (AOT Script Mapping)

**Files Affected:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValueSummary.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs`

**Violation:**
```csharp
using Oocx.TfPlan2Md.Providers.AzureRM.Models;      // NetworkSecurityGroupViewModel, etc.
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models; // Provider-specific models
```

**Root Cause:** AOT-compatible Scriban script object mapping requires explicit type registration. Core MarkdownGeneration directly references provider-specific view models for registration.

**Recommendation:** **Future refactoring required**
- **Option A:** Use reflection-free registration where providers register themselves with MarkdownGeneration
- **Option B:** Move AOT mapping to provider modules themselves (each provider registers its own types)
- **Option C:** Abstract the mapping interface so MarkdownGeneration doesn't directly reference concrete types
- **Short-term:** Document as known violation, create tracking issue for refactoring
- **Test exemption:** Exclude these 3 files from MarkdownGeneration → Providers rule temporarily

### Impact on Feature Implementation

**Blocking Issues:** None - all violations can be exempted with documentation

**Implementation Strategy:**
1. Implement all 10 dependency rules + 3 naming rules
2. Add explicit exemptions for 8 files with violation justifications
3. Document each exemption with rationale and tracking issue reference
4. Rules fail for NEW violations, pass for documented exemptions
5. Future refactorings can remove exemptions incrementally

## Test Structure Design

### File Organization

**Single File Approach:**
- Location: `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs`
- All architecture rules in one class for easy discovery and maintenance
- Test methods grouped by concern (dependencies, naming conventions)

**Alternative Rejected:** Multiple files (one per layer)
- Adds complexity without clear benefits
- Harder to see all rules at a glance
- More files to maintain

### Test Class Structure

```csharp
namespace Oocx.TfPlan2Md.TUnit.Architecture;

/// <summary>
/// Architecture tests that enforce layer boundaries and dependency rules.
/// See docs/architecture-rules.md for rule documentation.
/// Related ADR: docs/adr-007-architecture-boundary-enforcement.md
/// </summary>
public class ArchitectureBoundaryTests
{
    // === LAYER DEPENDENCY RULES ===
    
    [Test]
    public void Parsing_ShouldNotDependOn_MarkdownGeneration() { }
    
    [Test]
    public void Parsing_ShouldNotDependOn_CLI() { }
    
    [Test]
    public void Parsing_ShouldNotDependOn_Providers() { }
    
    [Test]
    public void Platforms_ShouldNotDependOn_MarkdownGeneration() { }
    
    [Test]
    public void CodeAnalysis_ShouldNotDependOn_MarkdownGeneration() { }
    
    [Test]
    public void MarkdownGeneration_ShouldNotDependOn_Providers() { }
    
    // === ALLOWED DEPENDENCIES (DOCUMENTATION TESTS) ===
    
    [Test]
    public void CLI_CanDependOn_AllLayers() { }
    
    [Test]
    public void MarkdownGeneration_CanDependOn_Parsing() { }
    
    [Test]
    public void Providers_CanDependOn_Parsing() { }
    
    [Test]
    public void Providers_CanDependOn_MarkdownGeneration() { }
    
    // === NAMING CONVENTION RULES ===
    
    [Test]
    public void Exceptions_ShouldHave_ExceptionSuffix() { }
    
    [Test]
    public void Tests_ShouldHave_TestsSuffix() { }
    
    [Test]
    public void Interfaces_ShouldHave_IPrefix() { }
}
```

### Test Method Naming

**Pattern:** `<Layer>_<Should|ShouldNot|CanDependOn>_<Rule>`

**Examples:**
- `Parsing_ShouldNotDependOn_MarkdownGeneration` - Forbidden dependency
- `CLI_CanDependOn_AllLayers` - Allowed dependency (documentation)
- `Exceptions_ShouldHave_ExceptionSuffix` - Naming convention

**Benefits:**
- Clear, scannable test names
- Easy to understand rule purpose from test name
- Consistent naming makes rules predictable

### Architecture Definition

```csharp
private static IEnumerable<Type> GetLayerTypes(string layerNamespace)
{
    return Types.InCurrentDomain()
        .That()
        .ResideInNamespace($"Oocx.TfPlan2Md.{layerNamespace}")
        .GetTypes();
}
```

**Considerations:**
- Load types once per test class (static field) for performance
- Filter to production code only (exclude tests, tools)
- Use full namespace matching to avoid partial matches

### Error Message Format

**Goal:** Provide clear, actionable feedback when rules are violated

**Format:**
```csharp
var result = Types.InCurrentDomain()
    .That().ResideInNamespace("Oocx.TfPlan2Md.Parsing")
    .ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.MarkdownGeneration")
    .GetResult();

if (!result.IsSuccessful)
{
    var violatingTypes = string.Join("\n  - ", result.FailingTypes);
    var message = $@"
Architecture Violation: Parsing layer must not depend on MarkdownGeneration

Rationale: Parsing is a core domain layer and should not know about rendering concerns.
This prevents circular dependencies and maintains clean separation between parsing and rendering.

Violations found in:
  - {violatingTypes}

See docs/architecture-rules.md for guidance on architectural boundaries.
Related ADR: docs/adr-007-architecture-boundary-enforcement.md
";
    Assert.Fail(message);
}
```

**Error Message Components:**
1. **Rule Statement:** Clear description of what is forbidden/required
2. **Rationale:** Why this rule exists (architectural principle)
3. **Violations:** List of specific types that violate the rule
4. **Guidance Link:** Reference to documentation for resolution
5. **ADR Reference:** Link to architecture decision record

### Handling Exemptions

For known violations that are temporarily exempt:

```csharp
var result = Types.InCurrentDomain()
    .That().ResideInNamespace("Oocx.TfPlan2Md.Platforms")
    .And().DoNotHaveNameMatching(".*ValueFormatterRegistration") // Exempt known violation
    .And().DoNotHaveNameMatching(".*AzureScopeFormatter")        // Exempt known violation
    .And().DoNotHaveNameMatching(".*IdFormatter")                // Exempt known violation
    .ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.MarkdownGeneration")
    .GetResult();

// Add comment explaining exemption and tracking issue
// TODO: Refactor formatters to MarkdownGeneration layer (Issue #XXX)
```

## CI Integration

### Execution in pr-validation.yml

Architecture tests run automatically as part of the existing test suite:

```yaml
- name: Test
  run: dotnet test --configuration Release --no-build --verbosity normal
```

**No changes required** - TUnit discovers and runs architecture tests like any other test.

### Performance Considerations

**Target:** <10 seconds total execution time for all architecture tests

**Optimization Strategies:**
- Load assemblies once per test class (static field)
- Use NetArchTest's efficient reflection-based analysis
- Keep rule count reasonable (13 tests total: 10 dependency + 3 naming)
- No special timeout configuration needed

**Expected Impact:**
- Minimal overhead (~2-5 seconds added to test suite)
- Well within acceptable CI performance budget
- Faster than functional tests, comparable to unit tests

### Failure Behavior

When an architecture test fails:
1. **PR Validation workflow fails** with test failure status
2. **Test output shows clear violation details** (which types, which rule)
3. **Developer receives guidance** via error message and docs links
4. **PR cannot be merged** until violation is fixed or exempted with maintainer approval

## Documentation Structure

### docs/architecture-rules.md

New documentation file that serves as the canonical reference for architecture rules.

**Contents:**

#### 1. Overview
- Purpose of architecture enforcement
- How tests work (NetArchTest + TUnit)
- How to run tests locally
- CI integration

#### 2. Layer Definitions
For each layer (CLI, Parsing, MarkdownGeneration, etc.):
- Purpose and responsibilities
- Allowed dependencies (incoming and outgoing)
- Key classes
- Example namespace

#### 3. Dependency Rules
Complete list of 10 dependency rules with:
- Rule statement (e.g., "Parsing must not depend on MarkdownGeneration")
- Rationale (why this rule exists)
- Examples of correct usage
- Examples of violations to avoid

#### 4. Naming Convention Rules
Complete list of 3 naming rules with:
- Rule statement (e.g., "Exception classes must end with 'Exception'")
- Rationale
- Examples

#### 5. Known Exemptions
Document each of the 8 files with known violations:
- File path
- Violation type
- Justification for exemption
- Tracking issue for resolution
- Approval date and maintainer

#### 6. Violation Resolution Process
- How to fix a violation (with examples)
- How to request an exemption (rare, requires maintainer approval)
- How to challenge a rule (propose ADR amendment)

#### 7. References
- Link to ADR-007
- Link to NetArchTest documentation
- Link to architecture.md
- Link to test file location

### What to Document vs. What Tests Enforce

**Document in architecture-rules.md:**
- High-level architectural principles and rationale
- Layer responsibilities and purposes
- Examples of correct and incorrect patterns
- Guidance for resolving violations
- Process for requesting exemptions

**Enforce in tests:**
- Specific namespace dependency rules
- Specific naming conventions
- Concrete, machine-verifiable constraints

**Do not duplicate:**
- Tests are the source of truth for rules
- Documentation explains WHY and provides context
- Documentation should reference tests, not restate them

## Edge Cases

### CompositionRoot and ProgramEntry

**Issue:** These orchestration files legitimately depend on all layers.

**Solution:**
```csharp
// Exclude orchestration files from layer dependency rules
var types = Types.InCurrentDomain()
    .That().ResideInNamespace("Oocx.TfPlan2Md.Parsing")
    .And().DoNotResideInNamespace("Oocx.TfPlan2Md")  // Exclude root namespace
```

Only check types in layer-specific namespaces, not the root `Oocx.TfPlan2Md` namespace.

### Cross-Cutting Concerns

**Diagnostics and RenderTargets:**
- These are utility layers with no dependencies
- All other layers can depend on them
- No restriction rules needed (allowed by default)

**Test Approach:**
```csharp
[Test]
public void Diagnostics_ShouldNotDependOn_AnyLayer()
{
    var result = Types.InCurrentDomain()
        .That().ResideInNamespace("Oocx.TfPlan2Md.Diagnostics")
        .ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.CLI")
        .And().ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.Parsing")
        .And().ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.MarkdownGeneration")
        .And().ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.Providers")
        .And().ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.Platforms")
        .GetResult();
    
    Assert.That(result.IsSuccessful, Is.True);
}
```

### Test Projects

**Issue:** Test projects can depend on anything (they verify production code).

**Solution:** Architecture rules only apply to production code in `Oocx.TfPlan2Md` namespace. Test project namespace is `Oocx.TfPlan2Md.TUnit`, which is automatically excluded.

### Third-Party Dependencies

**Issue:** Rules should not apply to NuGet packages or .NET framework types.

**Solution:** NetArchTest automatically filters to types in the loaded assemblies. Use `.InCurrentDomain()` to load only project assemblies, excluding third-party dependencies.

## Implementation Checklist

- [ ] Add NetArchTest.Rules NuGet package to test project (version 1.3.2+)
- [ ] Create `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/` directory
- [ ] Create `ArchitectureBoundaryTests.cs` with all 13 test methods
- [ ] Implement 10 dependency rules with clear error messages
- [ ] Implement 3 naming convention rules
- [ ] Add exemptions for 8 known violation files with justification comments
- [ ] Create `docs/architecture-rules.md` documentation
- [ ] Update `docs/architecture.md` to reference new architecture rules
- [ ] Verify tests pass locally with exemptions
- [ ] Verify tests fail when exemptions are removed (validate detection works)
- [ ] Verify tests run in CI (pr-validation.yml)
- [ ] Verify execution time <10 seconds
- [ ] Update work protocol with implementation notes

## Future Improvements

### Remove Known Violations

As refactoring opportunities arise:
1. **Platforms → MarkdownGeneration**: Move formatters to MarkdownGeneration
2. **MarkdownGeneration → Providers**: Abstract AOT mapping to provider registration
3. Remove test exemptions incrementally as violations are resolved

### Additional Rules

Consider adding in the future:
- **Module-level rules** (not just namespace-level)
- **Cyclomatic complexity gates** per layer
- **Public API surface rules** (no unnecessary public types)
- **Dependency injection rules** (constructor injection only)

### Monitoring

- Track number of exemptions over time (goal: reduce to zero)
- Monitor test execution time as codebase grows
- Review and update rules annually as architecture evolves
