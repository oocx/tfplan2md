# Architecture: Code Quality Metrics Enforcement

## Status

Proposed

## Context

The codebase currently has several files that exceed the 200-300 line guideline (specified in [docs/spec.md](../../spec.md)), with the largest file at 1,076 lines. There are no automated checks for cyclomatic complexity, line length, or maintainability index beyond what's built into existing analyzers. This feature will add automated enforcement of these quality metrics to prevent code quality degradation and ensure maintainability.

### Existing Analyzer Infrastructure

The project already has a robust analyzer stack configured in [src/Directory.Build.props](../../../src/Directory.Build.props):

1. **Microsoft.CodeAnalysis.NetAnalyzers** (v10.0.101) - CA rules including CA1505/CA1506
2. **StyleCop.Analyzers** (v1.2.0-beta.556) - Documentation and style rules
3. **SonarAnalyzer.CSharp** (v9.16.0.82469) - Code quality, complexity, and null checks
4. **Meziantou.Analyzer** (v2.0.127) - Best practices and performance patterns
5. **Roslynator.Analyzers** (v4.12.11) - Code simplification suggestions

All analyzers are configured via [.editorconfig](../../../.editorconfig) with `TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true`.

### Files Requiring Refactoring

Current file sizes (from `wc -l` analysis):

| File | Lines | Target | Strategy |
|------|-------|--------|----------|
| [ScribanHelpers.AzApi.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.AzApi.cs) | 1,067 | <300 | Split into 4+ partial files |
| [ReportModel.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs) | 774 | <300 | Split into separate class files |
| [VariableGroupViewModelFactory.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Models/VariableGroupViewModelFactory.cs) | 587 | <400 | Extract helper classes |
| [ResourceSummaryBuilder.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/ResourceSummaryBuilder.cs) | 471 | <400 | Extract helper classes |
| [AzureRoleDefinitionMapper.Roles.cs](../../../src/Oocx.TfPlan2Md/Azure/AzureRoleDefinitionMapper.Roles.cs) | 488 | <400 | Extract/refactor as needed |

Note: Test files are intentionally excluded from line count restrictions per project policy.

## Options Considered

### Option 1: Incremental Fix with Baseline (Recommended for Large Files)

**Description**: Create a baseline of existing violations, enable all rules as errors, and fix violations incrementally.

**Implementation**:
1. Enable all metrics as errors in `.editorconfig`
2. Create analyzer baseline files to suppress existing violations
3. Fix violations in phases (prioritize most complex/longest files first)
4. Remove baseline suppressions as violations are fixed

**Pros**:
- Prevents new violations immediately while allowing time to fix existing code
- CI/CD fails on new violations, protecting code quality going forward
- Developer can focus on one refactoring at a time
- No risk of breaking functionality with rushed fixes
- Clear visibility of progress (baseline suppressions decrease)

**Cons**:
- Baseline files add repository clutter
- Requires discipline to actually fix baseline violations (not just ignore them forever)
- More complex initial setup

**Baseline Configuration**:
```bash
# Generate baseline for SonarAnalyzer
dotnet build /p:SonarAnalyzerBaselinePath=$(pwd)/build-sonaranalyzer-baseline.txt

# Generate baseline for Meziantou
dotnet build /p:MeziantouAnalyzerBaselinePath=$(pwd)/build-meziantou-baseline.txt
```

Baseline files are already in use in this repository (see [build-meziantou-baseline.txt](../../../build-meziantou-baseline.txt) and [build-sonaranalyzer-baseline.txt](../../../build-sonaranalyzer-baseline.txt)).

---

### Option 2: Fix-First Approach (All-or-Nothing)

**Description**: Fix all violations before enabling rules as errors.

**Implementation**:
1. Leave rules as `warning` or `suggestion` initially
2. Fix all existing violations
3. Enable rules as errors only after all code complies
4. Verify with full build and test pass

**Pros**:
- Clean cutover - no suppressions needed
- Simple configuration (no baselines)
- Code quality immediately enforced globally

**Cons**:
- Large upfront work (all 5 files must be refactored before any enforcement)
- Risk of introducing bugs in large refactoring
- Blocks other development work
- All-or-nothing: cannot merge incremental progress

---

### Option 3: Suppression-Based Approach (Not Recommended)

**Description**: Enable rules as errors but add per-method/per-class suppressions for existing violations.

**Implementation**:
1. Enable all rules as errors
2. Add `[SuppressMessage]` attributes with justifications to existing violations
3. Fix violations incrementally, removing suppressions
4. Require maintainer approval for new suppressions

**Pros**:
- Immediate enforcement for new code
- Explicit documentation of why each violation is suppressed

**Cons**:
- Pollutes code with dozens of suppression attributes
- Risk of "suppression creep" - easier to suppress than fix
- Harder to track progress (suppressions scattered across files)
- Suppressions require code changes, not configuration changes

---

## Answers to Open Questions

### Q1: SonarAnalyzer S1541 Configuration

**Answer**: SonarAnalyzer does **not** support configurable thresholds via `.editorconfig` for S1541 (cyclomatic complexity). The threshold is hard-coded in the analyzer itself.

**Options**:
- **Option A**: Accept the default threshold (likely 10 or 15) and adjust our guideline to match
- **Option B**: Use SonarLint/SonarQube server for custom thresholds (out of scope for this feature)
- **Option C**: Disable S1541 and use a different cyclomatic complexity analyzer with configurable thresholds (e.g., CA1502 from Microsoft.CodeAnalysis.NetAnalyzers)

**Recommendation**: **Option C** - Use CA1502 instead of S1541. CA1502 supports configurable thresholds via AdditionalFiles configuration (see [Microsoft documentation](https://learn.microsoft.com/en-us/visualstudio/code-quality/how-to-generate-code-metrics-data)).

CA1502 Configuration:
```xml
<!-- CodeMetricsConfig.txt -->
CA1502: 15
```

```xml
<!-- src/Directory.Build.props -->
<ItemGroup>
  <AdditionalFiles Include="$(MSBuildThisFileDirectory)../CodeMetricsConfig.txt" />
</ItemGroup>
```

---

### Q2: CA1505/CA1506 Availability

**Answer**: CA1505 (method maintainability) and CA1506 (class maintainability) are **available** in Microsoft.CodeAnalysis.NetAnalyzers v10.0.101 (already referenced in the project), but they are **disabled by default** even with `AnalysisLevel=latest-recommended`.

**Configuration** (via `.editorconfig`):
```ini
# CA1505: Avoid unmaintainable code (method level)
dotnet_diagnostic.CA1505.severity = error

# CA1506: Avoid excessive class coupling (class level)
dotnet_diagnostic.CA1506.severity = error
```

**Threshold Configuration** (via AdditionalFiles):
```
CA1505: 20
CA1506: 20
```

---

### Q3: Test File Exemptions

**Answer**: Test files **should be explicitly exempted** from cyclomatic complexity rules.

**Justification**:
- Test setup/arrange phases can require complex object initialization
- Test methods often contain multiple assertion branches for edge cases
- Readability of test intent outweighs complexity reduction
- Test files are already exempted from StyleCop documentation rules (see `.editorconfig` lines 25-82)

**Configuration**:
```ini
[{src/tests/**/*.cs,tests/**/*.cs}]
# CA1502: Cyclomatic complexity not enforced in tests
dotnet_diagnostic.CA1502.severity = none
# CA1505: Method maintainability not enforced in tests
dotnet_diagnostic.CA1505.severity = none
# CA1506: Class maintainability not enforced in tests
dotnet_diagnostic.CA1506.severity = none
```

---

### Q4: Baseline vs. Fix-First

**Answer**: **Incremental fix with baseline** (Option 1) is strongly recommended.

**Justification**:
- Project already uses baselines successfully (see existing baseline files)
- 5 files need refactoring (1,067 lines down to <300 lines = significant work)
- Prevents new violations immediately while allowing time to fix existing code safely
- Aligns with project's existing pattern (Meziantou and SonarAnalyzer baselines already in use)
- Lower risk of introducing bugs compared to rushing all refactoring before enforcement

---

### Q5: Line Length Exceptions

**Answer**: String literals (URLs, error messages) **should be exempted** from line length limits.

**Justification**:
- Breaking long URLs across multiple lines reduces readability
- Error messages should be on one line for greppability
- C# 13 raw string literals (`"""`) already handle multi-line strings gracefully
- IDE0055 (Fix formatting) enforces line length, but has no mechanism to exclude specific patterns

**Configuration Recommendation**:
1. Set `max_line_length = 160` for `*.cs` files in `.editorconfig`
2. IDE0055 will flag violations
3. Maintainers manually approve exceptions during PR review (no automatic exemption available)
4. Document in [docs/commenting-guidelines.md](../../commenting-guidelines.md) that long URLs and error messages are acceptable exceptions

Alternative: Use `#pragma warning disable IDE0055` for specific long lines, but this requires code changes and is verbose.

---

## Decision

**Adopt Option 1 (Incremental Fix with Baseline)** for the following approach:

### Cyclomatic Complexity

- Use **CA1502** (not SonarAnalyzer S1541) with threshold of **15**
- Configure via `CodeMetricsConfig.txt` + AdditionalFiles
- Enable as error in `.editorconfig`
- Exempt test files
- Create baseline for existing violations

### Line Length

- Configure `max_line_length = 160` in `.editorconfig` for `*.cs` files
- Enable IDE0055 as **error** (currently warning)
- No automated exemption for long URLs/strings - maintainer reviews case-by-case
- Document exception policy in [docs/commenting-guidelines.md](../../commenting-guidelines.md)

### Maintainability Index

- Enable **CA1505** (method) and **CA1506** (class) with threshold of **20**
- Configure via `CodeMetricsConfig.txt` + AdditionalFiles
- Enable as error in `.editorconfig`
- Exempt test files
- Create baseline for existing violations

### File Length Refactoring Strategy

| Priority | File | Strategy | Estimated Effort |
|----------|------|----------|------------------|
| 1 | [ScribanHelpers.AzApi.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.AzApi.cs) | Split into 4 partial files by Azure resource type | High (1,067 → 4×250) |
| 2 | [ReportModel.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs) | Split records/classes into separate files | Medium (774 → 8×100) |
| 3 | [VariableGroupViewModelFactory.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Models/VariableGroupViewModelFactory.cs) | Extract builder/mapper classes | Medium (587 → 300+287) |
| 4 | [ResourceSummaryBuilder.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/ResourceSummaryBuilder.cs) | Extract per-resource summary builders | Medium (471 → 300+171) |
| 5 | [AzureRoleDefinitionMapper.Roles.cs](../../../src/Oocx.TfPlan2Md/Azure/AzureRoleDefinitionMapper.Roles.cs) | Evaluate if data file (JSON/XML) is more appropriate | Low-Medium (488 → ?) |

Each file will be refactored in a separate PR to minimize merge conflicts and allow focused code review.

---

## Rationale

### Why Incremental Fix with Baseline?

1. **Project precedent**: Repository already uses baselines for Meziantou and SonarAnalyzer successfully
2. **Risk management**: Large refactorings (1,076 lines → <300 lines) require careful planning and testing
3. **Immediate protection**: Prevents *new* violations today while allowing time to fix *existing* violations safely
4. **Parallel work**: Other features can proceed while refactoring happens incrementally

### Why CA1502 instead of S1541?

1. **Configurability**: CA1502 supports thresholds via AdditionalFiles; S1541 does not
2. **Consistency**: Keeps all metrics configuration in one place (`CodeMetricsConfig.txt`)
3. **Maintainability**: Threshold changes don't require analyzer version upgrades

### Why Threshold = 15 for Cyclomatic Complexity?

- Aligns with feature specification requirement
- Industry standard (McCabe's original recommendation: 10-15)
- Balances simplicity with real-world complexity (some branching is unavoidable)

### Why Threshold = 20 for Maintainability Index?

- Minimum "good maintainability" band per Microsoft documentation (20-100 = green)
- 0-9 = red (low maintainability), 10-19 = yellow (moderate maintainability)
- Conservative threshold that flags only genuinely problematic code

---

## Consequences

### Positive

- **Automated enforcement**: Quality metrics are checked on every build, not just during manual review
- **Clear expectations**: Developers know exact thresholds before writing code
- **Gradual improvement**: Baseline approach allows incremental refactoring without rushing
- **No regressions**: New code cannot introduce violations, even while old violations are being fixed
- **Unified configuration**: All metrics configured in `.editorconfig` and `CodeMetricsConfig.txt`

### Negative

- **Baseline maintenance**: Requires tracking and removing baseline entries as violations are fixed
- **Initial build noise**: Developers will see baseline warnings during local builds (suppressions prevent CI failure but not local warnings)
- **Manual exemptions for line length**: No automated way to exempt long URLs/strings from IDE0055
- **Refactoring effort**: 5 files require significant refactoring (priority 1 is 1,067 lines)

### Risks to Monitor

1. **Baseline debt accumulation**: If violations are never fixed, baseline becomes permanent technical debt
   - **Mitigation**: Include baseline reduction as part of Definition of Done for related features

2. **Over-suppression**: Developers may be tempted to suppress complexity instead of refactoring
   - **Mitigation**: Require maintainer approval for all suppressions via PR review

3. **Test complexity explosion**: Exempt rules may encourage overly complex test methods
   - **Mitigation**: Monitor test method length during PR review; suggest refactoring if test setup is complex

---

## Implementation Notes

### Configuration Files

1. **.editorconfig** changes (add to `[*.cs]` section):
   ```ini
   # Line length enforcement
   max_line_length = 160
   dotnet_diagnostic.IDE0055.severity = error
   
   # Cyclomatic complexity (CA1502)
   dotnet_diagnostic.CA1502.severity = error
   
   # Maintainability index
   dotnet_diagnostic.CA1505.severity = error
   dotnet_diagnostic.CA1506.severity = error
   ```

2. **.editorconfig** changes (add test exemptions):
   ```ini
   [{src/tests/**/*.cs,tests/**/*.cs}]
   # Cyclomatic complexity not enforced in tests
   dotnet_diagnostic.CA1502.severity = none
   # Method maintainability not enforced in tests
   dotnet_diagnostic.CA1505.severity = none
   # Class maintainability not enforced in tests
   dotnet_diagnostic.CA1506.severity = none
   ```

3. **CodeMetricsConfig.txt** (new file at repository root):
   ```
   CA1502: 15
   CA1505: 20
   CA1506: 20
   ```

4. **src/Directory.Build.props** (add AdditionalFiles):
   ```xml
   <ItemGroup>
     <AdditionalFiles Include="$(MSBuildThisFileDirectory)../CodeMetricsConfig.txt" />
   </ItemGroup>
   ```

5. **Generate baselines** (after enabling rules):
   ```bash
   # Build and generate baseline for new violations
   dotnet build --no-incremental > build-code-metrics-baseline.txt 2>&1
   
   # Alternative: Use existing baseline files and append
   dotnet build --no-incremental >> build-sonaranalyzer-baseline.txt 2>&1
   ```

### Developer Workflow

1. **Local build**: Warnings appear for baseline violations but build succeeds
2. **CI build**: Baseline violations are suppressed; new violations fail the build
3. **PR review**: Maintainer checks for new suppressions and requests refactoring if needed
4. **Refactoring**: Developer fixes violations and removes corresponding baseline entries

### Testing Strategy

1. **Before enabling rules**: Run `dotnet build` to capture current violation count
2. **After enabling rules**: Verify builds succeed with baselines
3. **After refactoring**: Remove baseline entries and verify builds still succeed
4. **Regression test**: Introduce artificial violation and verify CI fails

---

## Related Decisions

- [ADR-003: Modern C# Patterns](../../adr-003-modern-csharp-patterns.md) - Aligns with preference for clean, maintainable code
- [docs/spec.md](../../spec.md) - Documents 200-300 line file guideline (now enforced via line length and maintainability metrics)
- [docs/commenting-guidelines.md](../../commenting-guidelines.md) - Should be updated with line length exception policy

---

## References

- [Microsoft Code Metrics Documentation](https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-values)
- [CA1502: Avoid excessive complexity](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1502)
- [CA1505: Avoid unmaintainable code](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1505)
- [CA1506: Avoid excessive class coupling](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1506)
- [IDE0055: Fix formatting](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0055)
- [Configuring Code Metrics Thresholds](https://learn.microsoft.com/en-us/visualstudio/code-quality/how-to-generate-code-metrics-data)

---

## Next Steps for Developer Agent

1. Create `CodeMetricsConfig.txt` with thresholds
2. Update `src/Directory.Build.props` to reference AdditionalFiles
3. Update `.editorconfig` with new rules and test exemptions
4. Generate baseline file after enabling rules
5. Verify CI build succeeds with baseline suppressions
6. Create separate PRs for each file refactoring (priority order listed above)
7. Update [docs/commenting-guidelines.md](../../commenting-guidelines.md) with line length exception policy
