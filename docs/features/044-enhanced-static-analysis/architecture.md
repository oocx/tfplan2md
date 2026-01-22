# Architecture: Enhanced Static Analysis with Multiple Analyzers

## Status

Proposed

## Context

tfplan2md currently uses only Microsoft.CodeAnalysis.NetAnalyzers for code quality enforcement. The project has strong quality standards with `TreatWarningsAsErrors=true` in CI and comprehensive `.editorconfig` rules. However, adding multiple complementary analyzers can catch more issues:

- **Documentation quality**: XML documentation completeness and consistency (StyleCop)
- **Code smells and complexity**: Potential bugs, maintainability issues (SonarAnalyzer)
- **Performance patterns**: Best practices and efficiency (Meziantou.Analyzer)
- **Refactoring opportunities**: Code improvement suggestions (Roslynator)

### Current State

- **Build system**: Centralized analyzer configuration via `src/Directory.Build.props`
- **Current analyzer**: Microsoft.CodeAnalysis.NetAnalyzers v10.0.101
- **Configuration**: Comprehensive `.editorconfig` with 247 lines of style rules
- **CI/CD**: PR validation enforces zero warnings via `dotnet format --verify-no-changes`
- **Quality gates**: Format check, build, test, markdown lint, vulnerability scan all required before merge
- **Build time**: Current baseline ~45-60 seconds for build + test

### Maintainer Decisions

1. **Analyzer versions**: Pin to latest stable versions at implementation time
2. **Severity**: Start all rules as warnings, promote critical rules to errors after violations addressed
3. **Suppression policy**: Require clear comment explaining why suppression is necessary
4. **Implementation order**: Add analyzers one at a time, fix critical issues, then add the next
5. **Dependabot**: Include analyzer packages in automated dependency updates

### Key Constraints

- **Zero disruption**: Existing build must continue to pass during incremental adoption
- **Maintainability**: Configuration must be centralized and easy to update
- **Performance**: Build time impact must be acceptable (target: <20% increase)
- **Developer experience**: IDE feedback must be helpful, not overwhelming

## Architecture Overview

This feature introduces a **phased, incremental adoption strategy** where analyzers are added sequentially with progressive severity escalation. The architecture consists of four major components:

### 1. Analyzer Selection & Phasing

Four analyzers complement the existing Microsoft.CodeAnalysis.NetAnalyzers:

| Analyzer | Version | Focus Area | Phase |
|----------|---------|-----------|-------|
| StyleCop.Analyzers | 1.2.0-beta.556 | XML documentation, code consistency | 1 |
| SonarAnalyzer.CSharp | 9.16.0 | Code smells, bugs, complexity | 2 |
| Meziantou.Analyzer | 2.0.127 | Best practices, performance | 3 |
| Roslynator.Analyzers | 4.* | Refactoring suggestions | 4 |

**Rationale for order:**
1. **StyleCop first**: Enforces XML documentation standard (already a project requirement per `docs/spec.md`), lowest risk of false positives
2. **SonarAnalyzer second**: Catches bugs and design issues, high value/noise ratio
3. **Meziantou third**: Performance and best practices, moderate rule count
4. **Roslynator last**: Largest rule set, many are suggestions rather than requirements

### 2. Configuration Strategy

**Centralized Package Management** (`src/Directory.Build.props`):
```xml
<ItemGroup>
  <!-- Existing analyzer -->
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="10.0.101">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
  
  <!-- Phase 1: Documentation and consistency -->
  <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
  
  <!-- Phase 2: Code quality (added after Phase 1 complete) -->
  <!-- <PackageReference Include="SonarAnalyzer.CSharp" Version="9.16.0"> ... -->
  
  <!-- Phase 3: Best practices (added after Phase 2 complete) -->
  <!-- <PackageReference Include="Meziantou.Analyzer" Version="2.0.127"> ... -->
  
  <!-- Phase 4: Refactoring (added after Phase 3 complete) -->
  <!-- <PackageReference Include="Roslynator.Analyzers" Version="4.12.11"> ... -->
</ItemGroup>
```

**Rule Configuration** (`.editorconfig`):

New section added for each analyzer with:
- All rules initially set to `warning` severity
- Critical rules identified and marked for promotion to `error`
- Documented suppression patterns with justification requirements

**Version Pinning Strategy**:
- Pin exact versions in Directory.Build.props (no wildcards except Roslynator 4.*)
- Document version selection rationale in commit message
- Let Dependabot propose updates weekly via PRs
- Evaluate each update for rule changes/new warnings

### 3. Progressive Severity Escalation

**Phase Workflow** (repeat for each analyzer):

```
1. Add analyzer to Directory.Build.props with all rules as warnings
2. Run build, collect all new warnings
3. Triage warnings:
   - Fix legitimate issues
   - Configure false positives as "silent" or "suggestion"
   - Document suppressions with clear comments
4. Identify critical rules (security, correctness, maintainability)
5. Update .editorconfig to promote critical rules to errors
6. Verify CI passes with new error-level rules
7. Commit changes and move to next analyzer
```

**Critical Rule Identification Criteria**:
- **Security**: Potential vulnerabilities (SQL injection, XSS, insecure crypto)
- **Correctness**: Logic errors, null reference risks, resource leaks
- **Maintainability**: Violations that make code hard to understand or modify
- **Consistency**: Breaks established project conventions

**Suppression Requirements**:
```csharp
// ✅ GOOD: Clear explanation of why suppression is necessary
#pragma warning disable CA1822 // Method could be static but kept instance for DI container registration
public void RegisterServices(IServiceCollection services) { }
#pragma warning restore CA1822

// ❌ BAD: No explanation, suppression not justified
#pragma warning disable CA1822
public void DoSomething() { }
#pragma warning restore CA1822
```

### 4. CI/CD Integration

**No Changes to Existing Workflows** - analyzers run automatically via existing steps:

- **PR Validation** (`pr-validation.yml`):
  - Line 78: `dotnet format` already checks analyzer warnings (enforces `.editorconfig` rules)
  - Line 82: `dotnet build` fails on warnings due to `TreatWarningsAsErrors=true`
  - New analyzer warnings automatically fail the build

- **Dependabot** (`dependabot.yml`):
  - Already configured for NuGet package updates (line 3-14)
  - Will automatically create PRs for analyzer updates
  - No configuration changes needed

**Build Performance Monitoring**:
- Baseline: Current build time ~45-60 seconds
- Target: <20% increase per analyzer (<72 seconds total)
- Measure: CI run time before/after each phase
- Mitigation: If exceeded, investigate rule-specific performance issues

**Rollback Strategy**:
1. **Immediate rollback** (within PR): Remove analyzer package, revert `.editorconfig` changes
2. **Post-merge rollback**: Create hotfix PR to remove analyzer and revert rules
3. **Partial rollback**: Downgrade problem rules from `error` to `warning` or `silent`

## Decision

Adopt all four analyzers using the phased implementation strategy described above.

## Rationale

### Why Multiple Analyzers?

- **Coverage gaps**: Different analyzers specialize in different areas (documentation vs. performance vs. design)
- **Defense in depth**: Overlapping rules catch issues missed by others
- **Best practices**: Leverages collective wisdom of .NET community
- **Industry standard**: Major projects (Roslyn, ASP.NET Core) use multiple analyzers

### Why Phased Implementation?

- **Risk mitigation**: Each analyzer introduced independently, easy to isolate issues
- **Manageable workload**: Fix violations incrementally rather than all at once
- **Learning opportunity**: Team learns analyzer patterns progressively
- **Continuous deployment**: No "big bang" merge that blocks other work

### Why These Specific Analyzers?

**StyleCop.Analyzers**:
- Enforces XML documentation (already a project requirement per `docs/spec.md`)
- Widely adopted in .NET community (36M+ downloads)
- Stable rule set, low false positive rate
- Complements existing `.editorconfig` formatting rules

**SonarAnalyzer.CSharp**:
- Catches bugs that compilers miss (null reference, resource leaks)
- Industry-standard rules from SonarQube ecosystem
- High value/noise ratio (rules are well-researched)
- Actively maintained by SonarSource

**Meziantou.Analyzer**:
- Modern .NET best practices (async/await patterns, LINQ, etc.)
- Performance-focused rules (allocation reduction, efficient APIs)
- Smaller rule set than alternatives, less overwhelming
- Excellent rule documentation

**Roslynator.Analyzers**:
- Comprehensive refactoring suggestions (200+ rules)
- Code style consistency across C# patterns
- Active development, regular updates
- Can be selectively enabled (use only valuable rules)

### Why Start with Warnings?

- **Avoid breaking existing builds** during initial adoption
- **Gather data** on violation frequency before committing to errors
- **Allow discussion** of rule applicability to this codebase
- **Gradual escalation** to errors after team buy-in

## Consequences

### Positive

- **Higher code quality**: Catch more bugs, performance issues, and design problems
- **Better documentation**: StyleCop enforces comprehensive XML comments
- **Consistency**: Automated enforcement of best practices
- **Learning tool**: Rules teach modern C# patterns to contributors
- **Preventative**: Issues caught at PR time, not in production
- **Maintainability**: Reduced technical debt accumulation

### Negative

- **Build time impact**: Expected 10-20% increase per analyzer (monitoring required)
- **Initial violation fixing**: Time investment to resolve existing violations
- **False positives**: Some rules may need to be disabled for this codebase
- **Suppression management**: Need discipline to document suppressions properly
- **Cognitive load**: More feedback in IDE (can be overwhelming initially)
- **Dependency count**: Four additional packages to maintain

### Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Build time exceeds 20% increase | Disable expensive rules or remove analyzer |
| Too many false positives | Configure rules to `silent` or `suggestion` in `.editorconfig` |
| Analyzer breaks on .NET update | Pin versions, test updates in separate PR |
| Rules conflict across analyzers | Disable conflicting rule in one analyzer, document decision |
| Overwhelming IDE feedback | Initial rules as `suggestion`, promote gradually |

## Implementation Guidance

### Phase 1: StyleCop.Analyzers

**Files to Modify**:
- `src/Directory.Build.props`: Add StyleCop package reference
- `.editorconfig`: Add StyleCop rule configurations

**Expected Violations**:
- Missing XML documentation on public/internal members
- Documentation style inconsistencies
- File header requirements (if enabled)

**Critical Rules to Promote to Error** (after fixing violations):
- `SA1600`: Elements should be documented
- `SA1633`: File should have header (decide if needed)
- `SA1101`: Prefix local calls with this (align with existing `dotnet_style_qualification_*` rules)

**Estimated Effort**: 1-2 PRs (1 to add + fix violations, 1 to promote to errors)

### Phase 2: SonarAnalyzer.CSharp

**Files to Modify**:
- `src/Directory.Build.props`: Add SonarAnalyzer package reference
- `.editorconfig`: Add Sonar rule configurations

**Expected Violations**:
- Cognitive complexity warnings
- Null reference risks
- Exception handling issues
- LINQ usage patterns

**Critical Rules to Promote to Error**:
- `S1066`: Collapsible if statements (maintainability)
- `S2259`: Null pointer dereference (correctness)
- `S2583`: Condition always true/false (logic error)
- `S3925`: Update ISerializable implementation (correctness)

**Estimated Effort**: 2-3 PRs

### Phase 3: Meziantou.Analyzer

**Files to Modify**:
- `src/Directory.Build.props`: Add Meziantou.Analyzer package reference
- `.editorconfig`: Add Meziantou rule configurations

**Expected Violations**:
- Async/await best practices
- String handling efficiency
- Collection usage patterns
- Disposal patterns

**Critical Rules to Promote to Error**:
- `MA0004`: Use Task.ConfigureAwait (async best practice)
- `MA0011`: Use IFormatProvider (globalization)
- `MA0015`: Specify StringComparison (correctness)
- `MA0051`: Method too long (maintainability threshold)

**Estimated Effort**: 2-3 PRs

### Phase 4: Roslynator.Analyzers

**Files to Modify**:
- `src/Directory.Build.props`: Add Roslynator.Analyzers package reference
- `.editorconfig`: Add Roslynator rule configurations (selective enabling)

**Expected Violations**:
- Code simplification opportunities
- Pattern matching suggestions
- LINQ optimization hints
- Naming convention variations

**Critical Rules to Promote to Error**:
- Most Roslynator rules should remain as `suggestion` or `warning`
- Only promote rules that align with critical project conventions
- Focus on readability and consistency rules

**Estimated Effort**: 2-4 PRs (largest rule set, more triage needed)

### .editorconfig Structure

Add sections for each analyzer following existing pattern:

```ini
# Existing sections...
# (C# formatting rules, naming conventions, etc.)

# ========================================
# StyleCop.Analyzers Rules
# ========================================

# SA1600: Elements should be documented
dotnet_diagnostic.SA1600.severity = warning

# SA1633: File should have header
dotnet_diagnostic.SA1633.severity = none  # Not requiring file headers

# ... (other StyleCop rules)

# ========================================
# SonarAnalyzer.CSharp Rules
# ========================================

# S1066: Collapsible if statements
dotnet_diagnostic.S1066.severity = warning

# ... (other Sonar rules)

# ========================================
# Meziantou.Analyzer Rules
# ========================================

# MA0004: Use ConfigureAwait
dotnet_diagnostic.MA0004.severity = warning

# ... (other Meziantou rules)

# ========================================
# Roslynator.Analyzers Rules
# ========================================

# RCS1001: Add braces to if-else
dotnet_diagnostic.RCS1001.severity = suggestion

# ... (other Roslynator rules)
```

### Testing Strategy

**For Each Phase**:
1. Add analyzer package, run `dotnet build` locally
2. Document all new warnings in PR description
3. Fix violations or justify suppressions
4. Run full test suite: `dotnet test`
5. Run format check: `dotnet format --verify-no-changes`
6. Push to PR branch, verify CI passes
7. After merge, monitor next few PRs for unexpected issues

**Validation Checklist**:
- [ ] All tests pass
- [ ] No new warnings introduced
- [ ] Build time increase <20%
- [ ] CI/CD pipeline passes
- [ ] Documentation updated (if rules affect contributing guidelines)

### Rollback Procedure

**If an analyzer causes issues**:

1. **Option A: Remove analyzer completely**
   ```bash
   # Edit src/Directory.Build.props - remove package reference
   git add src/Directory.Build.props
   git commit -m "revert: remove <analyzer-name> due to <reason>"
   ```

2. **Option B: Downgrade specific rules**
   ```ini
   # In .editorconfig
   dotnet_diagnostic.<RuleId>.severity = silent  # or suggestion
   ```

3. **Option C: Disable analyzer for specific files**
   ```csharp
   // At top of problematic file
   #pragma warning disable <RuleId> // Explanation of why disabled
   ```

### Documentation Updates

**Files to Update** (if needed during implementation):
- `CONTRIBUTING.md`: Add section on analyzer suppression policy
- `docs/spec.md`: Update "Code Quality" section to mention analyzers
- `docs/commenting-guidelines.md`: Ensure alignment with StyleCop rules

## Success Criteria

This architecture is successful when:

- [ ] All four analyzers are integrated and running in CI
- [ ] Critical rules promoted to errors, zero warnings in builds
- [ ] Build time increase <20% from baseline
- [ ] Suppression policy documented and followed
- [ ] Dependabot updating analyzer packages weekly
- [ ] No false positive complaints from developers (rules properly tuned)
- [ ] At least 5 real bugs or design issues caught in next 10 PRs

## References

- [StyleCop.Analyzers GitHub](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [SonarAnalyzer.CSharp Documentation](https://rules.sonarsource.com/csharp/)
- [Meziantou.Analyzer GitHub](https://github.com/meziantou/Meziantou.Analyzer)
- [Roslynator GitHub](https://github.com/dotnet/roslynator)
- [Microsoft Docs: Code Analysis in .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
- Project: `docs/spec.md` - Code Quality section
- Project: `.editorconfig` - Existing rule configuration
- Project: `src/Directory.Build.props` - Build configuration
