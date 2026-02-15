# Feature: Architecture Boundary Enforcement with Tests

## Overview

Add automated enforcement of architectural layer boundaries using ArchUnitNET. The tfplan2md codebase has clear namespace organization (CLI, Parsing, MarkdownGeneration, Platforms, Providers) representing distinct architectural layers, but currently lacks automated verification of dependency rules. This feature will implement executable architecture tests that prevent unintended coupling between layers and document architectural decisions as code.

**Related Issue:** GitHub Issue (Multi-model analysis findings M-2)  
**Priority:** Major  
**Source:** Multi-model analysis findings from issues #312, #313, #314, #319 (`docs/workflow/multi-model-review/merged-findings.md` M-2)

## User Goals

- **Prevent architectural violations:** Automatically detect when code violates layer dependencies during PR validation
- **Document architecture:** Capture architectural rules as executable tests that serve as living documentation
- **Maintain clean architecture:** Ensure the codebase maintains separation of concerns as it evolves
- **Catch violations early:** Identify boundary violations in CI before merge, not during refactoring
- **Reduce coupling:** Prevent accidental dependencies that increase complexity and reduce maintainability

## Scope

### In Scope

- Add ArchUnitNET NuGet package to test project
- Create architecture tests in `tests/Oocx.TfPlan2Md.TUnit/Architecture/` directory
- Document architectural layers and their allowed dependencies in `docs/architecture-rules.md`
- Define and enforce layer dependency rules:
  - **CLI layer** can depend on all other layers (top-level orchestration)
  - **Parsing layer** cannot depend on MarkdownGeneration (separation of parsing from rendering)
  - **MarkdownGeneration layer** can depend on Parsing (needs parsed data)
  - **Providers layer** can depend on Parsing and MarkdownGeneration (provider-specific rendering)
  - **Platforms layer** can depend on Parsing (platform-specific rendering and metadata)
- Enforce naming conventions (e.g., exceptions must end with "Exception" suffix)
- Run architecture tests in CI with every PR as part of existing test suite
- Provide clear error messages when architectural rules are violated
- Integrate seamlessly with existing TUnit test infrastructure

### Out of Scope

- Refactoring existing code to fix current violations (if any) - violations will be documented for future work
- Module-level dependency rules (only namespace-level enforcement)
- Cyclomatic complexity rules (already handled by existing code metrics enforcement)
- Custom ArchUnitNET rules beyond standard boundary checks
- Architecture visualization diagrams (C4 diagrams already exist in `docs/architecture.md`)
- Runtime architecture enforcement (tests only run during build/CI)

## User Experience

### For Developers (PR Authors)

1. **Write code** → Make changes in any namespace
2. **Run tests locally** → `dotnet test` includes architecture tests automatically
3. **If violation occurs:**
   - Test fails with clear message indicating which rule was broken
   - Error shows source namespace, target namespace, and violated rule
   - Developer can reference `docs/architecture-rules.md` for guidance
   - Example error: `"Types in Oocx.TfPlan2Md.Parsing should not depend on Oocx.TfPlan2Md.MarkdownGeneration"`
4. **Fix violation** → Refactor to respect architectural boundaries
5. **Commit changes** → CI validates architecture tests pass

### For Maintainers

1. **Review PR** → Architecture test results visible in PR validation workflow
2. **If architecture test fails:**
   - Review violation details in CI logs
   - Assess if violation is legitimate architectural issue or rule needs updating
   - Provide feedback to author on proper layer usage
   - If rule change needed, update both tests and `docs/architecture-rules.md`
3. **Merge PR** → Confidence that architecture remains clean

### For New Contributors

1. **Read documentation** → `docs/architecture-rules.md` explains layer structure and rules
2. **Write code** → Tests provide immediate feedback on violations
3. **Learn architecture** → Architecture tests serve as executable documentation showing allowed patterns

## Success Criteria

Architecture enforcement is successful when:

- [ ] ArchUnitNET package (version 0.10.* or later) added to test project
- [ ] Architecture tests located in `tests/Oocx.TfPlan2Md.TUnit/Architecture/LayerBoundaryTests.cs`
- [ ] Tests verify all defined layer dependency rules (CLI, Parsing, MarkdownGeneration, Providers, Platforms)
- [ ] Tests verify naming conventions (exceptions end with "Exception")
- [ ] Architecture tests run automatically in CI as part of `pr-validation.yml` workflow
- [ ] Architecture tests integrate with TUnit infrastructure (no special test runner required)
- [ ] Failed architecture tests block PR merge with clear error messages
- [ ] `docs/architecture-rules.md` documents all enforced rules with rationale
- [ ] Current codebase passes all architecture tests OR known violations are documented with justification
- [ ] Architecture tests execute in under 10 seconds

## Architectural Layers

Based on analysis of the current codebase structure (`src/Oocx.TfPlan2Md/`), the following layers exist:

| Layer | Namespace | Purpose | Allowed Dependencies |
|-------|-----------|---------|---------------------|
| **CLI** | `Oocx.TfPlan2Md.CLI` | Command-line interface, argument parsing, orchestration | All layers (top-level) |
| **Parsing** | `Oocx.TfPlan2Md.Parsing` | Terraform JSON parsing, domain model creation | None (core domain) |
| **MarkdownGeneration** | `Oocx.TfPlan2Md.MarkdownGeneration.*` | Template rendering, report building, markdown generation | Parsing, Platforms |
| **Providers** | `Oocx.TfPlan2Md.Providers.*` | Provider-specific rendering (AzApi, AzureRM, AzureAD, AzureDevOps) | Parsing, MarkdownGeneration, Platforms |
| **Platforms** | `Oocx.TfPlan2Md.Platforms.*` | Platform metadata (Azure roles, principals) | Parsing |
| **CodeAnalysis** | `Oocx.TfPlan2Md.CodeAnalysis` | SARIF parsing, security findings integration | Parsing |
| **Diagnostics** | `Oocx.TfPlan2Md.Diagnostics` | Error reporting, diagnostic context | None |
| **RenderTargets** | `Oocx.TfPlan2Md.RenderTargets` | Output target configuration (GitHub, Azure DevOps) | None |

### Dependency Rules

1. **Parsing → MarkdownGeneration**: ❌ Forbidden (prevents circular dependency)
2. **Parsing → CLI**: ❌ Forbidden (core domain shouldn't know about UI)
3. **Parsing → Providers**: ❌ Forbidden (core domain shouldn't know about providers)
4. **CodeAnalysis → MarkdownGeneration**: ❌ Forbidden (analysis independent of rendering)
5. **MarkdownGeneration → Providers**: ❌ Forbidden (general rendering shouldn't depend on specific providers)
6. **CLI → ***: ✅ Allowed (orchestration layer can use everything)
7. **MarkdownGeneration → Parsing**: ✅ Allowed (rendering needs parsed data)
8. **Platforms → Parsing**: ✅ Allowed (platform metadata needs domain model)
9. **Platforms → MarkdownGeneration**: ✅ Allowed (platform-specific rendering uses general infrastructure)
10. **Providers → Parsing**: ✅ Allowed (providers need domain model)
11. **Providers → MarkdownGeneration**: ✅ Allowed (provider-specific templates extend base rendering)

### Naming Conventions

1. Exception classes must end with "Exception" suffix
2. Test classes must end with "Tests" suffix
3. Interface names must start with "I" prefix

## Technical Requirements

### ArchUnitNET Integration

- **Package**: `TngTech.ArchUnitNET` version 0.10.* or later
- **Package**: `TngTech.ArchUnitNET.xUnit` or `TngTech.ArchUnitNET.NUnit` (TUnit compatibility needs verification)
- **Alternative**: If ArchUnitNET doesn't work with TUnit, use `NetArchTest.Rules` (MIT licensed, more lightweight)

### Test Structure

```csharp
// Location: tests/Oocx.TfPlan2Md.TUnit/Architecture/LayerBoundaryTests.cs

namespace Oocx.TfPlan2Md.TUnit.Architecture;

/// <summary>
/// Architecture tests that enforce layer boundaries and dependency rules.
/// See docs/architecture-rules.md for rule documentation.
/// </summary>
public class LayerBoundaryTests
{
    // One test method per architectural rule
    // Clear test names indicating what is being verified
    // Descriptive failure messages showing violated rule
}
```

### CI Integration

- Architecture tests must run as part of existing `dotnet test` command in `pr-validation.yml`
- No separate workflow or test runner required
- Tests should be discoverable by TUnit's standard test discovery
- Failed tests should appear in standard test results output

### Error Messages

Architecture test failures must provide:
1. **Source namespace**: Where the violation originates
2. **Target namespace**: What the source incorrectly depends on
3. **Rule violated**: Which architectural principle was broken
4. **Guidance**: Reference to `docs/architecture-rules.md` for resolution

Example:
```
Architecture Violation Detected

Rule: Parsing layer must not depend on MarkdownGeneration
Source: Oocx.TfPlan2Md.Parsing.TerraformPlanParser
Target: Oocx.TfPlan2Md.MarkdownGeneration.MarkdownRenderer
Reason: Parsing is a core domain layer and should not know about rendering concerns.

See docs/architecture-rules.md for guidance on architectural boundaries.
```

## Documentation Requirements

`docs/architecture-rules.md` must include:

1. **Overview**: Purpose of architecture enforcement
2. **Layer Definitions**: Description of each layer and its responsibilities
3. **Dependency Rules**: Complete list of allowed and forbidden dependencies
4. **Rationale**: Why each rule exists (e.g., prevent circular dependencies, maintain testability)
5. **Examples**: Code examples showing correct and incorrect patterns
6. **Exception Process**: How to request architectural rule changes
7. **References**: Link to ArchUnitNET documentation and related ADRs

## Open Questions

1. **ArchUnitNET vs NetArchTest.Rules**: Which library works best with TUnit?
   - ArchUnitNET is more feature-rich but may have xUnit/NUnit coupling
   - NetArchTest.Rules is simpler and more test-framework agnostic
   - Architect should evaluate compatibility and recommend

2. **Current Violations**: Does the current codebase have any architectural violations?
   - Need to run initial tests to discover existing violations
   - If violations exist, document as known issues with migration plan OR update rules to match current architecture

3. **CompositionRoot and Program**: Where do these fit in the layer structure?
   - `CompositionRoot.cs` and `Program.cs` are likely part of CLI layer
   - Need architect confirmation on their classification

4. **Test Timeout**: Should architecture tests have a shorter timeout than functional tests?
   - Recommend: Use default TUnit timeout, ensure tests complete in <10s

## References

- Multi-model analysis finding M-2: `docs/workflow/multi-model-review/merged-findings.md`
- Current architecture documentation: `docs/architecture.md`
- Testing strategy: `docs/testing-strategy.md`
- Namespace structure: `src/Oocx.TfPlan2Md/` directory layout
- ArchUnitNET: https://github.com/TNG/ArchUnitNET
- NetArchTest.Rules: https://github.com/BenMorris/NetArchTest
