# Work Protocol: Performance Investigation — Potential O(n²) Patterns

**Work Item:** `docs/issues/105-performance-investigation/`
**Branch:** `copilot/investigate-tfplan2md-performance`
**Workflow Type:** Bug Fix
**Created:** 2025-01-27

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2025-01-27
- **Summary:** Investigated codebase for performance issues causing 20-minute runtimes. Identified 9 findings across the LCS diff algorithm, AzureDevOps formatter, and model building code. The primary bottleneck is the AzureDevOps diff formatter calling the O(m×n) LCS algorithm for every single changed attribute (not just large ones), combined with a character-level LCS on every changed line pair.
- **Artifacts Produced:** `docs/issues/105-performance-investigation/analysis.md`
- **Problems Encountered:** None
