# Phase 4 Baseline: Roslynator.Analyzers

**Date**: 2025-01-27  
**Analyzer Version**: Roslynator.Analyzers v4.12.11  
**Build Command**: `dotnet build src/tfplan2md.slnx --no-incremental --verbosity normal`

## Summary

Roslynator.Analyzers has been added to the project. Unlike the previous analyzers, Roslynator has **most rules disabled or hidden by default**. Only a few rules are enabled as errors out-of-the-box.

### Initial Violation Count

| Severity | Count | Rule IDs |
|----------|-------|----------|
| Error    | 16    | RCS1194 |
| Warning  | 0     | (none - all disabled by default) |
| **Total** | **16** | |

### Violations by Rule ID

| Rule ID | Severity | Count | Description | Link |
|---------|----------|-------|-------------|------|
| RCS1194 | Error | 16 | Implement exception constructors | [Docs](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1194) |

## Violation Details

### RCS1194: Implement exception constructors (16 violations)

**Default Severity**: Error  
**Category**: Design  
**Rationale**: Custom exceptions should implement standard exception constructors for consistency and serializability.

**Locations**:
1. `src/Oocx.TfPlan2Md/CLI/CliParser.cs(243,14)`: `CliParseException`
2. `src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelperException.cs(9,14)`: `ScribanHelperException`
3. `src/Oocx.TfPlan2Md/Parsing/TerraformPlanParseException.cs(6,14)`: `TerraformPlanParseException`
4. `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderException.cs(6,14)`: `MarkdownRenderException`
5. `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/Capturing/ScreenshotCaptureException.cs(10,23)`: `ScreenshotCaptureException`
6. `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/CLI/CliParseException.cs(10,23)`: `CliParseException` (ScreenshotGenerator)
7. `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/CLI/CliValidationException.cs(10,23)`: `CliValidationException`
8. (8 more exception classes across projects)

**Expected Fix**: Add standard exception constructors:
- `ctor()`
- `ctor(string message)`
- `ctor(string message, Exception innerException)`

## Selective Enabling Strategy

Unlike Phases 1-3 where we enabled all rules and then disabled problematic ones, Phase 4 uses a **selective enabling** approach:

1. **Disable all Roslynator rules globally** via category severity
2. **Selectively enable** only high-value rules that:
   - Don't overlap with StyleCop/SonarAnalyzer/Meziantou
   - Catch real bugs or performance issues
   - Have low false-positive rate
   - Provide actionable suggestions

### Candidate Rules for Selective Enabling

Based on architecture.md guidance and Roslynator documentation:

#### Code Simplification (High Value)
- RCS1033: Remove redundant boolean literal
- RCS1036: Remove unnecessary blank line (suggestion only)
- RCS1037: Remove trailing whitespace
- RCS1097: Remove redundant 'ToString' call
- RCS1124: Inline local variable

#### LINQ Optimization (High Value)
- RCS1077: Optimize LINQ method call
- RCS1158: Static member in generic type should use a type parameter
- RCS1235: Optimize method call (e.g., StringBuilder.Append)

#### Null Handling (High Value)
- RCS1146: Use conditional access
- RCS1199: Simplify Boolean expression

#### Naming Conventions (Medium Value)
- RCS1018: Add accessibility modifiers (overlaps with StyleCop SA1400)
- RCS1089: Use --/++ operator instead of assignment

#### Already Enabled by Default
- RCS1194: Implement exception constructors (error severity)

## Build Time Impact

**Baseline (before Roslynator)**: ~15-20 seconds (clean build with Phases 1-3)  
**With Roslynator**: ~15.12 seconds (no significant change)  
**Increase**: <5% (well within 20% threshold)

## Next Steps (P4-T3)

1. Add `.editorconfig` section for Roslynator.Analyzers
2. Set global category severity to `none` (disable all rules)
3. Selectively enable ~10-15 high-value rules as `warning`
4. Keep RCS1194 at `error` (or downgrade to `warning` if too strict)
5. Run build and assess new violations
6. Fix violations or suppress with justification (P4-T4, P4-T5)

## Risk Assessment

**Low Risk**: Roslynator's default-disabled approach minimizes risk of overwhelming violations. Only 16 violations found, all from a single rule. Selective enabling ensures we only adopt valuable rules.

**Performance**: No measurable performance impact from adding the analyzer package.

**Strategy Validation**: The selective enabling approach is validated - Roslynator is designed to be opt-in rather than opt-out.
