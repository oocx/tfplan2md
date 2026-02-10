# Architecture Rules

## Overview

This document defines the architectural layer boundaries and dependency rules for the tfplan2md codebase. These rules are automatically enforced by architecture tests using [NetArchTest.Rules](https://github.com/BenMorris/NetArchTest), which run as part of every PR validation.

**Purpose:** Prevent architectural drift and maintain clean separation of concerns as the codebase evolves.

**Enforcement:** Architecture tests run automatically in CI with every commit. Failed tests block PR merge.

**Test Location:** `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs`

**Related ADR:** [ADR-007: Architecture Boundary Enforcement](adr-007-architecture-boundary-enforcement.md)

### How Tests Work

- Tests analyze compiled assemblies using .NET reflection
- Each architectural rule is implemented as a single test method
- NetArchTest.Rules verifies dependencies at the namespace level
- Tests complete in <10 seconds (currently ~3 seconds)
- Violations produce clear error messages with guidance

### Running Tests Locally

```bash
# Run all architecture tests
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/*

# Run a specific test
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ArchitectureBoundaryTests/Parsing_ShouldNotDependOn_MarkdownGeneration
```

## Layer Definitions

The tfplan2md codebase is organized into 8 architectural layers, each with specific responsibilities and allowed dependencies:

| Layer | Namespace | Purpose | Can Depend On | Cannot Depend On |
|-------|-----------|---------|---------------|------------------|
| **CLI** | `Oocx.TfPlan2Md.CLI` | Command-line interface, argument parsing, orchestration | All layers (orchestration) | - |
| **Parsing** | `Oocx.TfPlan2Md.Parsing` | Terraform JSON parsing, domain model creation | None (core domain) | CLI, MarkdownGeneration, Providers, Platforms* |
| **MarkdownGeneration** | `Oocx.TfPlan2Md.MarkdownGeneration` | Template rendering, report building, markdown generation | Parsing, Platforms | Providers* |
| **Providers** | `Oocx.TfPlan2Md.Providers.*` | Provider-specific rendering (AzureRM, AzApi, AzureDevOps, AzureAD) | Parsing, MarkdownGeneration, Platforms | - |
| **Platforms** | `Oocx.TfPlan2Md.Platforms.*` | Platform-specific rendering and metadata (Azure roles, principals, scopes, formatters) | Parsing, MarkdownGeneration | - |
| **CodeAnalysis** | `Oocx.TfPlan2Md.CodeAnalysis` | SARIF parsing, security findings integration | Parsing | MarkdownGeneration |
| **Diagnostics** | `Oocx.TfPlan2Md.Diagnostics` | Error reporting, diagnostic context | None | All domain layers |
| **RenderTargets** | `Oocx.TfPlan2Md.RenderTargets` | Output target configuration (GitHub, Azure DevOps) | None | All domain layers |

*Known exemptions exist - see [Known Exemptions](#known-exemptions) section.

### Layer Responsibilities

#### CLI Layer
- Parse command-line arguments
- Orchestrate application workflow
- Handle user input/output
- Coordinate all other layers
- Entry point for application

#### Parsing Layer
- Parse Terraform JSON plan files
- Create domain models (TerraformPlan, ResourceChange, etc.)
- Validate plan structure
- No dependencies on other domain layers (pure domain logic)

#### MarkdownGeneration Layer
- Render Terraform plans to markdown using Scriban templates
- Generate reports with styling and formatting
- Provide base rendering infrastructure
- Use parsed domain models from Parsing layer

#### Providers Layer
- Extend base rendering with provider-specific logic
- Format provider-specific resource types (azurerm_*, azapi_*, etc.)
- Enhance resources with provider metadata
- Build on MarkdownGeneration infrastructure

#### Platforms Layer
- Provide platform-specific rendering and formatting (icons, labels, value formatters)
- Provide platform metadata (Azure roles, principals, management groups)
- Load and parse mapping files
- Platform-specific rendering that's shared across providers

#### CodeAnalysis Layer
- Parse SARIF files
- Integrate security findings
- Independent of rendering (analysis should be reusable)

#### Diagnostics Layer
- Cross-cutting error reporting
- Diagnostic context collection
- No dependencies (usable anywhere)

#### RenderTargets Layer
- Configure output targets (GitHub markdown, Azure DevOps wiki)
- No dependencies (simple configuration)

## Dependency Rules

### Forbidden Dependencies

These rules prevent architectural violations by blocking dependencies that would create coupling or circular dependencies:

#### 1. Parsing → MarkdownGeneration ❌

**Rule:** The Parsing layer must not depend on the MarkdownGeneration layer.

**Rationale:** Parsing is a core domain layer responsible for understanding Terraform plan structure. It should not know about how plans are rendered. This prevents circular dependencies (MarkdownGeneration depends on Parsing for domain models) and maintains clean separation between parsing and rendering concerns.

**Example Violation:**
```csharp
// ❌ BAD: Parsing layer referencing MarkdownGeneration
namespace Oocx.TfPlan2Md.Parsing;

using Oocx.TfPlan2Md.MarkdownGeneration; // VIOLATION!

public class TerraformPlanParser
{
    private readonly IMarkdownRenderer _renderer; // Don't do this!
}
```

**Correct Pattern:**
```csharp
// ✅ GOOD: Parsing layer with no rendering concerns
namespace Oocx.TfPlan2Md.Parsing;

public class TerraformPlanParser
{
    public TerraformPlan Parse(string json)
    {
        // Pure parsing logic, no rendering
        return JsonSerializer.Deserialize<TerraformPlan>(json, _options);
    }
}
```

#### 2. Parsing → CLI ❌

**Rule:** The Parsing layer must not depend on the CLI layer.

**Rationale:** Core domain logic should be independent of user interface concerns. This allows parsing to be reused in different contexts (CLI, API, library, tests) without coupling to command-line infrastructure.

#### 3. Parsing → Providers ❌

**Rule:** The Parsing layer must not depend on the Providers layer.

**Rationale:** Core parsing logic should be provider-agnostic. Provider-specific handling happens in the Providers layer, which depends on Parsing (allowed direction).

#### 4. Parsing → Platforms ❌*

**Rule:** The Parsing layer must not depend on the Platforms layer.

**Rationale:** Core domain should be independent of platform-specific metadata concerns.

**Known Exemption:** `TfPlanJsonContext.cs` - JSON source generation requires all serialized types in one context (System.Text.Json limitation). See [Known Exemptions](#known-exemptions).

#### 5. MarkdownGeneration → Providers ❌*

**Rule:** The MarkdownGeneration layer must not depend on the Providers layer.

**Rationale:** General rendering logic should not depend on specific providers. Provider-specific rendering should happen in the Providers layer.

**Known Exemptions:** 3 AOT script mapping files need refactoring. See [Known Exemptions](#known-exemptions).

#### 6. CodeAnalysis → MarkdownGeneration ❌

**Rule:** The CodeAnalysis layer must not depend on the MarkdownGeneration layer.

**Rationale:** Static analysis results should be independent of rendering concerns, allowing analysis to be used in different contexts.

#### 7. Diagnostics → Any Domain Layer ❌

**Rule:** The Diagnostics layer must not depend on any domain layer (CLI, Parsing, MarkdownGeneration, Providers, Platforms, CodeAnalysis).

**Rationale:** Cross-cutting concerns like diagnostics should not depend on domain layers, ensuring they can be used anywhere without circular dependencies.

### Allowed Dependencies (Documentation)

These rules document expected dependency directions. They verify the architecture is correct:

#### 8. CLI → All Layers ✅

**Rule:** The CLI layer can depend on all other layers.

**Rationale:** CLI is the top-level orchestration layer that coordinates all other layers. This is the entry point for the application.

#### 9. MarkdownGeneration → Parsing ✅

**Rule:** The MarkdownGeneration layer should depend on Parsing.

**Rationale:** Rendering logic needs access to parsed domain models to generate output. This is the expected and correct dependency direction.

#### 10. Platforms → MarkdownGeneration ✅

**Rule:** The Platforms layer should depend on MarkdownGeneration.

**Rationale:** Platform-specific rendering needs the general rendering infrastructure. Value formatters, icon rendering, and label formatting require MarkdownGeneration services.

#### 11. Providers → Parsing ✅

**Rule:** The Providers layer should depend on Parsing.

**Rationale:** Provider-specific rendering needs access to parsed domain models.

#### 12. Providers → MarkdownGeneration ✅

**Rule:** The Providers layer should depend on MarkdownGeneration.

**Rationale:** Provider-specific rendering extends base rendering capabilities.

## Naming Convention Rules

These rules enforce standard .NET naming conventions:

### 13. Exception Classes → "Exception" Suffix

**Rule:** All exception classes must end with "Exception" suffix.

**Rationale:** Standard .NET naming convention for clarity and consistency.

**Example:**
```csharp
// ✅ GOOD
public class TerraformPlanParseException : Exception { }

// ❌ BAD
public class TerraformPlanError : Exception { }
```

### 14. Test Classes → "Tests" Suffix

**Rule:** All test classes should end with "Tests" suffix.

**Rationale:** Project naming convention for test discoverability and organization.

**Exemptions:** Helper classes, fixtures, entry points, and utility classes are excluded.

### 15. Interfaces → "I" Prefix

**Rule:** All interface names must start with "I" prefix.

**Rationale:** Standard .NET naming convention for immediate recognition of interfaces.

**Example:**
```csharp
// ✅ GOOD
public interface IMarkdownRenderer { }

// ❌ BAD
public interface MarkdownRenderer { }
```

## Known Exemptions

The following files violate architectural rules but are temporarily exempted with documented justifications. These represent technical debt that should be addressed in future refactorings.

### Category 1: Parsing → Platforms (JSON Source Generation)

**Files Affected:**
- `src/Oocx.TfPlan2Md/Parsing/TfPlanJsonContext.cs`

**Violation:** References `PrincipalMappingFile` from `Oocx.TfPlan2Md.Platforms.Azure` in JSON serialization attributes.

**Justification:** System.Text.Json source generation requires all serialized types to be referenced in a single `JsonSerializerContext`. `PrincipalMappingFile` is defined in Platforms but needs to be included in the same context as Terraform plan models.

**Why Not Fix?**
- Moving `PrincipalMappingFile` to Parsing would create a reverse architectural violation
- Using multiple contexts breaks AOT compilation
- Using runtime serialization defeats the purpose of source generation
- This is a tooling limitation, not an architectural flaw

**Resolution:** Document as acceptable exception. This is the least-bad solution given .NET's JSON source generation constraints.

**Tracking Issue:** None (accepted as permanent exception)

### Category 2: MarkdownGeneration → Providers (AOT Script Mapping)

**Files Affected:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValueSummary.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs`

**Violation:** These files reference provider-specific view models (e.g., `NetworkSecurityGroupViewModel`, `TeamProjectViewModel`) for AOT-compatible Scriban script object mapping.

**Justification:** AOT-compatible Scriban mapping requires explicit type registration. Core MarkdownGeneration currently registers provider-specific types directly.

**Resolution Options:**
- **Option A (Recommended):** Providers register their own types with MarkdownGeneration via registration callbacks
- **Option B:** Move AOT mapping logic to provider modules (each provider registers itself)
- **Option C:** Use reflection-based registration (loses AOT benefits)

**Tracking Issue:** TBD (create issue for refactoring)

**Priority:** Low - Current implementation enables AOT compilation, but creates coupling.

## Violation Resolution Process

### If You Encounter a Violation

1. **Read the Error Message:**
   ```
   Architecture Violation: Parsing layer must not depend on MarkdownGeneration
   
   Rationale: Parsing is a core domain layer and should not know about rendering concerns.
   
   Violations found in:
     - Oocx.TfPlan2Md.Parsing.SomeClass
   
   See docs/architecture-rules.md for guidance.
   ```

2. **Understand the Rule:** Review this document to understand why the rule exists.

3. **Fix the Violation:**
   - **Refactor:** Move code to the correct layer
   - **Invert Dependency:** Use dependency injection to reverse the dependency
   - **Extract Interface:** Create an abstraction in the lower layer

4. **Run Tests Locally:** Verify your fix passes architecture tests.

5. **Commit Changes:** Architecture tests run automatically in CI.

### If You Need an Exemption (Rare)

Exemptions should be **extremely rare** and require maintainer approval. Only request an exemption if:
- The violation is a tooling limitation (like JSON source generation)
- All alternative solutions have worse trade-offs
- The exemption is temporary with a clear refactoring plan

**Process:**
1. Create a GitHub issue explaining:
   - What rule you want to exempt
   - Why the violation exists
   - Why alternatives don't work
   - Proposed refactoring plan (if temporary)

2. Request maintainer review and approval

3. If approved, add exemption to architecture test with:
   - `.DoNotHaveNameMatching("ClassName")` exclusion
   - Inline comment with justification and issue number
   - Update this documentation with the exemption

### If You Want to Challenge a Rule

Architecture rules should evolve with the codebase. If a rule is too restrictive:

1. Create a GitHub discussion or issue proposing a rule change
2. Explain:
   - Which rule you want to change
   - Why the current rule is problematic
   - What the new rule should be
   - Impact on existing code

3. Discuss with maintainers and team
4. If approved, create a PR to:
   - Update ADR-007 with decision rationale
   - Modify architecture tests
   - Update this documentation

## References

- **ADR-007:** [Architecture Boundary Enforcement](adr-007-architecture-boundary-enforcement.md)
- **NetArchTest Documentation:** https://github.com/BenMorris/NetArchTest
- **Architecture Overview:** [docs/architecture.md](architecture.md)
- **Test Implementation:** `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/ArchitectureBoundaryTests.cs`
- **Multi-Model Analysis Finding M-2:** `docs/workflow/multi-model-review/merged-findings.md`
- **Feature Specification:** [docs/features/066-architecture-boundary-enforcement/specification.md](features/066-architecture-boundary-enforcement/specification.md)
