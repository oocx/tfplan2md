# ADR-007: Architecture Boundary Enforcement with Tests

## Status

Proposed

## Context

The tfplan2md codebase has evolved with a clear namespace organization representing distinct architectural layers:
- CLI (command-line interface and orchestration)
- Parsing (Terraform JSON parsing and domain models)
- MarkdownGeneration (template rendering and report building)
- Providers (provider-specific rendering: AzApi, AzureRM, AzureAD, AzureDevOps)
- Platforms (platform-specific rendering and metadata: Azure roles, principals, formatters)
- CodeAnalysis (SARIF parsing and security findings)
- Diagnostics (error reporting and diagnostic context)
- RenderTargets (output target configuration: GitHub, Azure DevOps)

However, there is currently no automated verification that these boundaries are respected. Analysis of the codebase revealed several existing violations:

**Current Violations:**
1. **Parsing → Platforms**: `TfPlanJsonContext.cs` references `PrincipalMappingFile` from Platforms for JSON source generation
2. **MarkdownGeneration → Providers**: Three files reference provider-specific models (AzureRM, AzureDevOps) for AOT script object mapping

These violations indicate architectural drift and potential for circular dependencies as the codebase grows. Implementing automated architecture boundary enforcement will:
- Prevent new violations during development
- Document the intended architecture as executable tests
- Make architectural decisions explicit and reviewable
- Catch violations early in CI before merge

## Options Considered

### Option 1: ArchUnitNET

**Description:** Feature-rich architecture testing library with first-class test framework integration.

**Pros:**
- Comprehensive rule API with deep analysis capabilities
- First-class support for xUnit, NUnit, MSTest via dedicated NuGet packages
- More active maintenance and richer documentation
- Familiar API for Java ArchUnit users
- Better error messages with detailed violation reports

**Cons:**
- Dedicated test framework packages are for xUnit/NUnit/MSTest only
- TUnit is not officially supported with a dedicated package
- More complex API with steeper learning curve
- Slightly heavier dependency footprint

**TUnit Compatibility:**
- ArchUnitNET core library (`TngTech.ArchUnitNET`) is test-framework-agnostic
- Framework-specific packages only provide convenience extensions
- Can be used with TUnit by calling `.Check(architecture)` and using TUnit assertions to handle violations
- Requires manual integration but fully functional

### Option 2: NetArchTest.Rules

**Description:** Lightweight, test-framework-agnostic architecture testing library.

**Pros:**
- **Explicitly test-framework-agnostic** (works with any .NET test runner)
- Simpler API, easier to learn and adopt
- Lighter dependency footprint
- Returns result objects that work with any assertion library
- Works seamlessly with TUnit without any adapter code
- .NET Standard 2.0 compatible

**Cons:**
- Less feature-rich than ArchUnitNET
- Simpler error messages (though still actionable)
- Less active maintenance (stable but fewer updates)
- Fewer advanced rule capabilities

**TUnit Compatibility:**
- Perfect compatibility - designed to be framework-agnostic
- Natural integration pattern with TUnit's assertion methods

### Option 3: Custom Architecture Tests

**Description:** Write manual tests using reflection to check dependencies.

**Pros:**
- No external dependencies
- Full control over rule logic and error messages
- No compatibility concerns

**Cons:**
- Significant development effort
- Ongoing maintenance burden
- Reinventing the wheel
- Missing battle-tested edge case handling
- Not a proven pattern

## Decision

**Use NetArchTest.Rules for architecture boundary enforcement.**

## Rationale

NetArchTest.Rules is the best fit for our requirements:

1. **TUnit Compatibility:** NetArchTest is explicitly designed to be test-framework-agnostic, making it a natural fit for TUnit without requiring adapters or workarounds. ArchUnitNET would require manual integration work.

2. **Simplicity:** The simpler API aligns with our goal of maintainable, AI-assisted development. Clear, straightforward rules are easier to document and understand.

3. **Sufficient Capabilities:** NetArchTest provides all the rule types we need:
   - Namespace dependency rules (10 rules in specification)
   - Naming convention rules (3 rules: exceptions, tests, interfaces)
   - Class modifier rules (public, internal, sealed)
   - Custom predicates for edge cases

4. **Lightweight:** Minimal dependency footprint aligns with our lean architecture philosophy.

5. **Proven Pattern:** Widely used in .NET community with stable, battle-tested implementation.

While ArchUnitNET offers more features, NetArchTest's simplicity and perfect TUnit compatibility make it the pragmatic choice. The additional features of ArchUnitNET are not needed for our layer boundary enforcement goals.

## Consequences

### Positive

- **Automated boundary enforcement** prevents architectural drift
- **Early violation detection** in CI pipeline before merge
- **Living documentation** - tests serve as executable architecture specification
- **TUnit integration** works seamlessly without special adapters
- **Clear failure messages** guide developers to fix violations
- **Minimal maintenance** due to simple, stable API
- **Fast execution** (target: <10 seconds) won't slow down CI pipeline

### Negative

- **Existing violations must be addressed** before rules can be enforced:
  - Parsing → Platforms violation (JSON source generation)
  - MarkdownGeneration → Providers violations (AOT script mapping)
- **Not a complete solution** - only enforces namespace-level rules, not all architectural concerns
- **Requires discipline** - rules can be bypassed with suppressions if not carefully reviewed
- **Less rich error messages** compared to ArchUnitNET (though still actionable)

### Migration Path for Existing Violations

The existing violations fall into three categories:

1. **JSON Source Generation (Parsing → Platforms):**
   - **Rationale for exemption:** System.Text.Json source generation requires all serialized types to be referenced in the same `JsonSerializerContext`. Moving `PrincipalMappingFile` to Parsing would create a reverse dependency. This is a tooling limitation, not an architectural flaw.
   - **Recommendation:** Document as an acceptable exception with clear justification in architecture tests.

2. **AOT Script Mapping (MarkdownGeneration → Providers):**
   - **Architectural issue:** Core MarkdownGeneration should not depend on provider-specific types.
   - **Recommendation:** Use abstraction or dynamic registration to break the direct dependency. Provider models should register themselves with MarkdownGeneration, not be referenced directly.
   - **Short-term:** Document as known violation with tracking issue.

## Implementation Notes

### Test Structure

**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs`

**Organization:**
- Single test class for all architecture rules
- One test method per architectural rule
- Clear test names describing the rule (e.g., `Parsing_ShouldNotDependOn_MarkdownGeneration`)
- Grouped by concern using test method naming convention

**Error Message Format:**
```
Architecture Violation Detected

Rule: Parsing layer must not depend on MarkdownGeneration
Violations found in:
  - Oocx.TfPlan2Md.Parsing.SomeClass
  - Oocx.TfPlan2Md.Parsing.AnotherClass

See docs/architecture-rules.md for guidance on architectural boundaries.
```

### Architecture Definition

Use NetArchTest's `Types.InCurrentDomain()` to load assemblies, filtering to main project types:

```csharp
var types = Types.InCurrentDomain()
    .That()
    .ResideInNamespace("Oocx.TfPlan2Md");
```

### Performance

- Target execution time: <10 seconds for all architecture tests
- NetArchTest analyzes compiled assemblies, which is fast
- No special performance tuning expected to be needed

### CI Integration

- Tests run automatically as part of `dotnet test` in `pr-validation.yml`
- No separate workflow or configuration required
- TUnit discovers and runs tests automatically
- Failed tests appear in standard test output with clear violation details

### Edge Cases

**CompositionRoot and ProgramEntry:**
- These are orchestration entry points in the root namespace `Oocx.TfPlan2Md`
- They legitimately depend on all layers (their purpose is composition)
- Exclude from dependency rules using `.And().DoNotResideInNamespace("Oocx.TfPlan2Md")`
- Only check types in layer namespaces (CLI, Parsing, MarkdownGeneration, etc.)

**Test Projects:**
- Test projects can depend on anything (they verify the production code)
- Architecture rules only apply to production code in `Oocx.TfPlan2Md` namespace

**Cross-Cutting Concerns:**
- Diagnostics layer has no dependencies - all layers can depend on it
- RenderTargets layer has no dependencies - all layers can depend on it
- These are utility layers, not domain layers

## References

- Feature Specification: `docs/features/066-architecture-boundary-enforcement/specification.md`
- NetArchTest.Rules: https://github.com/BenMorris/NetArchTest
- ArchUnitNET (considered but not selected): https://github.com/TNG/ArchUnitNET
- Multi-model analysis finding M-2: `docs/workflow/multi-model-review/merged-findings.md`
- Current architecture documentation: `docs/architecture.md`
