# UAT Report: azapi Output Values (Feature 106)

**Date:** 2026-02-28  
**Branch:** `copilot/add-separate-table-for-azapi-output`  
**GitHub PR:** [oocx/tfplan2md-uat#116](https://github.com/oocx/tfplan2md-uat/pull/116)  
**Azure DevOps PR:** [test#107](https://dev.azure.com/oocx/test/_git/test/pullrequest/107)  
**Status:** ✅ **PASSED**

---

## Summary

UAT validated the rendering of `azapi_resource` Output Values sections on both GitHub and
Azure DevOps. The feature-specific report (first comment) exercised all three required
scenarios; the comprehensive demo regression test confirmed no side effects.

---

## Validation Results

### Feature-Specific Artifact (`docs/features/106-azapi-output-values/uat-plan.md`)

#### Resource 1 — `azapi_resource.automation_create` (create, output unknown)

| Criterion | Result |
|-----------|--------|
| `#### Output Values` heading is completely absent | ✅ PASS |
| Resource block ends after `#### Body` section | ✅ PASS |
| Section suppressed when all outputs are unknown after apply | ✅ PASS |

#### Resource 2 — `azapi_resource.automation_update` (update, grouped output + display names)

| Criterion | Result |
|-----------|--------|
| `#### Output Values` heading appears after `#### Body Changes` | ✅ PASS |
| `linkedWorkspaceId` row shows human-readable Azure resource description (NOT raw path) | ✅ PASS |
| `###### \`sku\`` H6 sub-heading rendered inside Output Values | ✅ PASS |
| `sku` table has Property/Before/After columns with 3 rows (capacity, name, tier) | ✅ PASS |
| Data values formatted as inline code (e.g. `` `Running` ``, `` `1` ``) | ✅ PASS |
| No output values appear in `#### Body Changes` (clear separation) | ✅ PASS |

#### Resource 3 — `azapi_resource.sql_delete` (delete, sensitive field)

| Criterion | Result |
|-----------|--------|
| `#### Output Values` heading appears | ✅ PASS |
| Table has `Property \| Value` columns (single-column delete mode) | ✅ PASS |
| `apiKey` field displays `(sensitive)` rather than actual value | ✅ PASS |
| `state` field shows `` `Online` `` in code formatting | ✅ PASS |

---

### Regression Artifact (comprehensive demo)

| Criterion | Result |
|-----------|--------|
| Non-azapi resources have no `#### Output Values` section | ✅ PASS |
| Existing body rendering (grouping, sensitivity, large values) unchanged | ✅ PASS |
| Summary table, module headings, findings sections render correctly | ✅ PASS |

---

## Platform Rendering

| Platform | Feature Test | Regression Test | Status |
|----------|-------------|-----------------|--------|
| GitHub | ✅ Correct | ✅ Correct | ✅ Approved |
| Azure DevOps | ✅ Correct | ✅ Correct | ✅ Approved |

---

## Notes

- The `.tmp/uat-run/last-run.json` state file was absent (UAT PRs were pre-created by a
  previous agent session). The state file was reconstructed from PR metadata before running
  `scripts/uat-run.sh --cleanup-last`.
- Both PRs were closed/abandoned and UAT branches deleted during cleanup.
