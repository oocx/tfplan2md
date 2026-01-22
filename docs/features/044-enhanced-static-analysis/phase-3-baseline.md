# Phase 3 Baseline: Meziantou.Analyzer Violations

**Status**: Baseline Complete  
**Date**: 2025-01-22  
**Analyzer**: Meziantou.Analyzer v2.0.127

## Summary

- **Total Violations**: 792
- **Unique Rule IDs**: 10
- **Build Status**: Passing (all rules configured as `suggestion`)

## Violations by Rule ID

| Rule ID | Count | Description | Severity Plan |
|---------|-------|-------------|---------------|
| MA0002  | 268   | IEqualityComparer<string> missing | ⚠️ Warning → Error |
| MA0009  | 124   | Regex needs timeout (ReDoS) | ⚠️ Warning → Error |
| MA0048  | 100   | File name must match type name | 💡 Suggestion (keep) |
| MA0006  | 80    | Use String.Equals vs == | ⚠️ Warning |
| MA0051  | 76    | Method too long (>60 lines) | 💡 Suggestion |
| MA0004  | 56    | Use ConfigureAwait(false) | 💡 Suggestion (console app) |
| MA0023  | 36    | Add RegexOptions.ExplicitCapture | ⚠️ Warning |
| MA0013  | 28    | Don't extend ApplicationException | ⚠️ Warning |
| MA0011  | 16    | IFormatProvider missing | ⚠️ Warning |
| MA0008  | 8     | Add StructLayoutAttribute | 💡 Suggestion |

## Top 5 Violations (Priority Order)

### 1. MA0002: IEqualityComparer<string> missing (268 violations)
**Severity**: Critical (will promote to error)  
**Rationale**: Culture-specific string comparison can cause bugs in production  
**Fix**: Add `StringComparer.Ordinal` or `StringComparer.OrdinalIgnoreCase`

Sample violations:
- `DiagnosticContext.cs(109,66)`: Dictionary without comparer
- `ReportModel.cs(454,25)`: LINQ Contains without comparer
- Multiple collection operations across codebase

**Fix Strategy**:
- Add `StringComparer.Ordinal` for case-sensitive comparisons
- Add `StringComparer.OrdinalIgnoreCase` for case-insensitive comparisons
- Review each usage for correct culture semantics

### 2. MA0009: Regex needs timeout (124 violations)
**Severity**: Critical (will promote to error)  
**Rationale**: Prevents ReDoS (Regular Expression Denial of Service) attacks  
**Fix**: Add timeout parameter to all Regex constructors

Sample violations:
- `GitHubHtmlPostProcessor.cs`: Multiple regex patterns without timeout
- `AzureDevOpsHtmlPostProcessor.cs`: HTML processing regexes
- `CommonHtmlNormalization.cs`: Normalization patterns

**Fix Strategy**:
- Add `matchTimeout: TimeSpan.FromSeconds(1)` to all Regex constructors
- Review complex patterns for potential backtracking issues
- Consider using `RegexOptions.NonBacktracking` for .NET 7+ performance

### 3. MA0006: Use String.Equals instead of == (80 violations)
**Severity**: Medium (keep as warning)  
**Rationale**: Complements MA0002, enforces explicit string comparison  
**Fix**: Replace `==` with `String.Equals(..., StringComparison.Ordinal)`

**Fix Strategy**:
- Replace equality operators with explicit `String.Equals`
- Use appropriate `StringComparison` value for context

### 4. MA0023: Add RegexOptions.ExplicitCapture (36 violations)
**Severity**: Medium (keep as warning)  
**Rationale**: Performance optimization - prevents capturing unneeded groups  
**Fix**: Add `RegexOptions.ExplicitCapture` to regex options

**Fix Strategy**:
- Add to existing regex options where groups aren't needed
- Review named groups vs numbered groups usage

### 5. MA0013: Don't extend ApplicationException (28 violations)
**Severity**: Medium (keep as warning)  
**Rationale**: `ApplicationException` is obsolete design pattern  
**Fix**: Change base class to `Exception` or custom exception base

Sample violations:
- `CliParseException.cs(10,23)`: CLI exceptions
- `MarkdownRenderException.cs(6,14)`: Rendering exceptions
- `TerraformPlanParseException.cs(6,14)`: Parsing exceptions

**Fix Strategy**:
- Change to inherit from `Exception` directly
- No functional impact - purely architectural improvement

## Rules to Keep as Suggestion

### MA0004: Use ConfigureAwait(false) (56 violations)
**Decision**: Keep as suggestion  
**Rationale**: This is a console application with no UI synchronization context  
**Explanation**: `ConfigureAwait(false)` is a micro-optimization for libraries to avoid
capturing synchronization context. In a console app, there's no SynchronizationContext,
so this adds no value and reduces readability.

### MA0048: File name must match type name (100 violations)
**Decision**: Keep as suggestion  
**Rationale**: Project convention allows multiple related types per file  
**Conflicts with**: SA1402 (multiple types per file) which is intentionally relaxed

### MA0051: Method too long (76 violations)
**Decision**: Keep as suggestion, suppress with justification where needed  
**Rationale**: Complex parsing/rendering logic sometimes requires longer methods  
**Threshold**: 60 lines per method (Meziantou default)

## Implementation Plan

### P3-T4: Fix Critical Violations
1. MA0002 (268): String comparers
2. MA0009 (124): Regex timeouts
3. MA0013 (28): Exception inheritance

### P3-T5: Fix Non-Critical Violations
1. MA0006 (80): String.Equals
2. MA0023 (36): RegexOptions.ExplicitCapture
3. MA0011 (16): IFormatProvider

### P3-T6: Promote Rules to Error
- MA0002: IEqualityComparer → **error**
- MA0009: Regex timeout → **error**

## Build Performance

- **Baseline (with StyleCop + SonarAnalyzer)**: ~45-60 seconds
- **With Meziantou.Analyzer**: TBD (will measure in P3-T8)
- **Target**: <72 seconds (<20% increase)

## Notes

- All rules currently set to `suggestion` in `.editorconfig` to allow build to pass
- Will promote to `warning` after fixes (P3-T5)
- Will promote critical rules to `error` after verification (P3-T6)
- Test files may need relaxed rules or suppressions for certain violations
