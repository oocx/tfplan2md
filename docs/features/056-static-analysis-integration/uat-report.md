# UAT Report: Static Code Analysis Integration

**Status:** ❌ FAILED
**Date:** 2026-01-31
**UAT PR (GitHub):** #38 (Closed)
**UAT PR (AzDO):** #48 (Abandoned)

## Summary
The UAT session for Feature #056 failed because the summary output layout does not match the requirements.

## Detailed Findings

### 1. Summary Layout Regression
The security & quality summary line is not positioned immediately after the main resource summary line. 
**Expectation:** The summary line (e.g., "Code Analysis: 🚨 2 Critical, 1 High...") should follow the resource plan summary ("Plan: 3 to add, 0 to change...") directly to provide an integrated overview.

### 2. Previous Findings (Still Unresolved)
The following issues from previous UAT runs remain:
- **Broken Markdown Tables (Unmatched Findings):** Multi-line messages in "Unmatched Findings" cause table breakage.
- **Resource alignment:** The test artifact alignment issues persist, leading to findings appearing in "Unmatched Findings" instead of their respective resources.

## Repro Steps
1. Generate the problematic artifact:
   ```bash
   scripts/generate-demo-artifacts.sh
   ```
   (Or run the specific command for static analysis demo)
2. Create UAT PR using `artifacts/static-analysis-comprehensive-demo.md`.
3. Observe the summary section layout in the PR comment.

## Next Steps
- Handoff to **Developer** to adjust the summary line placement and address previous rendering/mapping issues.
