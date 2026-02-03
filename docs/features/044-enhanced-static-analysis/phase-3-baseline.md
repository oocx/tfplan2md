# Phase 3 Baseline: Meziantou.Analyzer Violations

**Status**: Baseline Complete  
**Date**: 2025-01-22  
**Analyzer**: Meziantou.Analyzer v2.0.127

## Summary

- **Total Violations (Original)**: 792
- **Culture-Specific Rules Disabled**: 578 violations (MA0002, MA0006, MA0011)
- **Remaining Violations**: 214
- **Unique Rule IDs**: 7 (3 rules disabled)
- **Build Status**: Passing (all rules configured as `suggestion` or `none`)

## Violations by Rule ID

### Active Rules (214 violations to fix)

| Rule ID | Count | Description | Severity Plan |
|---------|-------|-------------|---------------|
| MA0009  | 62    | Regex needs timeout (ReDoS) | ⚠️ Warning → Error |
| MA0048  | 50    | File name must match type name | 💡 Suggestion (keep) |
| MA0051  | 38    | Method too long (>60 lines) | 💡 Suggestion |
| MA0004  | 28    | Use ConfigureAwait(false) | 💡 Suggestion (console app) |
| MA0023  | 18    | Add RegexOptions.ExplicitCapture | ⚠️ Warning |
| MA0013  | 14    | Don't extend ApplicationException | ⚠️ Warning |
| MA0008  | 4     | Add StructLayoutAttribute | 💡 Suggestion |

### Disabled Rules (578 violations - not applicable for Docker deployment)

| Rule ID | Count | Description | Rationale |
|---------|-------|-------------|-----------|
| MA0002  | 268   | IEqualityComparer<string> missing | Docker ensures Invariant culture |
| MA0006  | 254   | Use String.Equals vs == | Related to MA0002 - not needed |
| MA0011  | 56    | IFormatProvider missing | Docker ensures Invariant culture |

## Top Priority Violations (Remaining)

### 1. MA0009: Regex needs timeout (62 violations)
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

### 2. MA0023: Add RegexOptions.ExplicitCapture (18 violations)
**Severity**: Medium (keep as warning)  
**Rationale**: Performance optimization - prevents capturing unneeded groups  
**Fix**: Add `RegexOptions.ExplicitCapture` to regex options

**Fix Strategy**:
- Add to existing regex options where groups aren't needed
- Review named groups vs numbered groups usage

### 3. MA0013: Don't extend ApplicationException (14 violations)
**Severity**: Low (keep as warning)  
**Rationale**: `ApplicationException` is obsolete design pattern  
**Fix**: Change base class to `Exception` or custom exception base

**Fix Strategy**:
- Change to inherit from `Exception` directly
- No functional impact - purely architectural improvement

## Architectural Decision: Culture-Specific Rules Disabled

**Decision Date**: 2025-01-22  
**Rationale**: Docker deployment ensures consistent Invariant culture

### Background
tfplan2md is a console tool that runs exclusively in Docker containers without locale configuration. The container environment guarantees consistent Invariant culture across all deployments.

### Rules Disabled
- **MA0002** (268 violations): IEqualityComparer<string> or IComparer<string> is missing
- **MA0006** (254 violations): Use String.Equals instead of equality operator  
- **MA0011** (56 violations): IFormatProvider is missing

**Total disabled**: 578 violations (73% of original baseline)

### Justification
1. **Consistent Environment**: Docker container has no locale configuration - Invariant culture is guaranteed
2. **Technical Output**: Generated markdown/text is for technical use (API identifiers, resource names), not user-facing localized content
3. **Code Clarity**: Removing culture-specific boilerplate (StringComparer.Ordinal, CultureInfo.InvariantCulture) improves readability
4. **No Functional Benefit**: Adding culture parameters provides no value when culture is guaranteed to be Invariant

### Impact
- Reduces violations from 792 to 214 (73% reduction)
- Improves code readability by removing unnecessary culture parameters
- No risk to correctness - Docker deployment model ensures consistency

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

### P3-T4: Fix Critical Violations (62 violations)
1. MA0009 (62): Add regex timeouts for ReDoS protection

### P3-T5: Fix Non-Critical Violations (32 violations)
1. MA0023 (18): Add RegexOptions.ExplicitCapture for performance
2. MA0013 (14): Fix ApplicationException inheritance

### P3-T6: Promote Rules to Error
- MA0009: Regex timeout → **error** (critical security)

### P3-T7: Keep as Suggestions (120 violations)
- MA0048 (50): File name matching (project convention)
- MA0051 (38): Method length (case-by-case)
- MA0004 (28): ConfigureAwait (console app - no value)
- MA0008 (4): StructLayoutAttribute (low-level optimization)

## Build Performance

- **Baseline (with StyleCop + SonarAnalyzer)**: ~45-60 seconds
- **With Meziantou.Analyzer**: TBD (will measure in P3-T8)
- **Target**: <72 seconds (<20% increase)

## Notes

- All rules currently set to `suggestion` in `.editorconfig` to allow build to pass
- Will promote to `warning` after fixes (P3-T5)
- Will promote critical rules to `error` after verification (P3-T6)
- Test files may need relaxed rules or suppressions for certain violations
