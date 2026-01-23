# Feature: Mutation Testing

## Overview

Implement mutation testing to validate that the existing test suite actually catches bugs, not just achieves code coverage. Mutation testing introduces small code changes (mutations) and verifies that at least one test fails, proving the tests are effective.

This addresses the gap between **code coverage** (lines executed) and **test effectiveness** (bugs caught). The project currently has 84.48% line coverage and 72.80% branch coverage, but coverage alone doesn't guarantee that tests catch actual defects.

## User Goals

- **DevOps/Infrastructure Teams**: Confidence that the test suite will catch regressions when modifying tfplan2md
- **Maintainers**: Identify weak spots in the test suite where mutations survive (indicating missing or ineffective tests)
- **Contributors**: Understand which code paths have robust test coverage vs superficial coverage

## Scope

### In Scope

**1. Mutation Testing Tool Selection**
- Evaluate mutation testing tools for .NET (Stryker.NET, others)
- Select tool based on:
  - TUnit compatibility (the project's test framework)
  - .NET 10/C# 13 support
  - Performance and reporting quality
  - CI/CD integration capabilities

**2. Mutation Testing Configuration**
- Configure mutation testing for critical code paths:
  - `Parsing/` - Terraform plan JSON parsing logic
  - `MarkdownGeneration/Summaries/` - Summary generation and aggregation
- Define mutation strategies (which mutations to apply)
- Set baseline mutation score thresholds

**3. Reporting and Metrics**
- Generate mutation testing reports showing:
  - Mutation score (% of mutations caught)
  - Survived mutations (bugs tests didn't catch)
  - Killed mutations (bugs tests did catch)
  - Timeout mutations (mutations causing infinite loops/hangs)
- Identify specific code locations with weak test coverage

**4. Workflow Integration**
- Document how to run mutation testing locally
- Determine appropriate CI/CD integration:
  - **Not in PR validation** (too slow for every PR)
  - **Periodic runs** (weekly/monthly) or manual workflow dispatch
  - Store mutation reports as artifacts

**5. Documentation**
- Document mutation testing purpose and interpretation
- Provide guidance on addressing survived mutations
- Update testing strategy documentation

### Out of Scope

- Achieving 100% mutation score (unrealistic and often counterproductive)
- Mutation testing for ALL code (focus on critical paths initially)
- Adding mutation testing to PR validation workflow (performance cost too high)
- Automatic PR creation for survived mutations (requires human judgment)

## User Experience

### Running Mutation Testing Locally

```bash
# Run mutation testing on critical paths
scripts/mutation-test.sh

# Run mutation testing on specific project/directory
scripts/mutation-test.sh --target Parsing
```

### Interpreting Results

```
Mutation Testing Results
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Killed:    127 mutations (85.2%)
✗ Survived:   18 mutations (12.1%)
⏱ Timeout:     4 mutations (2.7%)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Mutation Score: 85.2%

Survived Mutations:
  1. TerraformPlanParser.cs:45 - Conditional boundary changed
  2. SummaryBuilder.cs:123 - Return value mutated
  ...
```

### CI/CD Workflow

- Mutation testing runs on schedule (e.g., weekly on Sunday)
- Reports uploaded as workflow artifacts
- Results posted as GitHub Issues for review and tracking

## Success Criteria

- [ ] Mutation testing tool selected and configured for TUnit compatibility
- [ ] Mutation tests run successfully on `Parsing/` and `MarkdownGeneration/Summaries/`
- [ ] Baseline mutation score established (target: ≥75% for critical paths)
- [ ] Mutation testing script created for local execution
- [ ] CI workflow configured for periodic mutation testing runs
- [ ] Documentation updated with mutation testing guidance
- [ ] At least one survived mutation identified and addressed (proving the process works)

## Open Questions

1. **Tool Selection**: Is Stryker.NET the best choice, or are there better alternatives for .NET 10 + TUnit? *(Architect to evaluate)*
2. **Performance**: What is actual runtime for mutation testing on critical paths? *(Acceptable: ≤30 minutes)*
3. **Scope Expansion**: Should we extend mutation testing to other directories after initial implementation? *(Defer to retrospective)*
4. **Threshold Enforcement**: Should CI fail if mutation score drops below a threshold, or just report? *(Start with reporting only)*
5. **Integration with Coverage**: Should mutation reports include correlation with code coverage metrics? *(Nice-to-have, not required)*

## Maintainer Clarifications

- **Performance Tolerance**: 30 minutes is acceptable for mutation testing runs
- **Tool Selection**: Architect should evaluate options (Maintainer has no tool preference)
- **Reporting Method**: Post mutation testing results as GitHub Issues for tracking and discussion
