# UAT Report: Feature 094 — Azure DevOps Build Definition Tables

## UAT Run #2 (Re-run with Regenerated Artifacts)

**Date:** 2026-02-20
**Branch:** `copilot/add-build-definition-tables-again`
**UAT GitHub PR:** https://github.com/oocx/tfplan2md-uat/pull/87
**UAT Azure DevOps PR:** https://dev.azure.com/oocx/test/_git/test/pullrequest/86

## Result: ⏳ PENDING APPROVAL

**Note:** This is a re-run of UAT. The first attempt (PR #86/85) failed because `uat-plan.md` was stale — generated with old code that didn't include semantic icons. The artifact has been regenerated with the correct output including `🆔` icons for variable names and `✅`/`❌` icons for boolean values.

---

## UAT Run #1 (Failed - Stale Artifacts)

**Date:** 2026-02-20
**Branch:** `copilot/add-build-definition-tables-again`
**UAT GitHub PR:** https://github.com/oocx/tfplan2md-uat/pull/86
**UAT Azure DevOps PR:** https://dev.azure.com/oocx/test/_git/test/pullrequest/85

## Result: ❌ FAILED (Stale artifact without semantic icons)

Validation pending maintainer review of rendered output in UAT PRs.

## Expected Validation Results (To Be Verified)

### Variables Table

| Criterion | Expected Result |
| --------- | --------------- |
| Table header shows: Name, Value, Is Secret, Allow Override | Should display all columns |
| Variable names show `🆔` icon (e.g., `` `🆔 API_KEY` ``) | Should show semantic icon |
| Secret variable `API_KEY` shows `(sensitive / hidden)` | Should mask sensitive data |
| Regular variable `BUILD_CONFIGURATION` shows actual value `Release` | Should show actual value |
| Boolean `is_secret` shows `✅ true` or `❌ false` | Should show semantic icons |
| Boolean `allow_override` shows `✅ true` or `❌ false` | Should show semantic icons |

### CI Trigger Table

| Criterion | Expected Result |
| --------- | --------------- |
| Table header shows: Use YAML, Override (Branch Filters) | Should display both columns |
| `use_yaml` shows `✅ true` with semantic icon | Should show semantic icon |
| Branch filter list `main`, `develop`, `feature/*` displayed | Should show branch filters |

### Repository Table

| Criterion | Expected Result |
| --------- | --------------- |
| Table header shows: Type, Repo ID, Branch, YAML Path, Report Build Status | Should display all columns |
| Repo type `TfsGit` code-formatted | Should be code-formatted |
| YAML path `azure-pipelines.yml` code-formatted | Should be code-formatted |
| `report_build_status` shows `✅ true` with semantic icon | Should show semantic icon |

### Security Validation

| Criterion | Expected Result |
| --------- | --------------- |
| No actual secret values exposed in rendered output | Should mask all secrets |
| `is_secret: true` variables consistently show `(sensitive / hidden)` | Should be consistent |

### Formatting

| Criterion | Expected Result |
| --------- | --------------- |
| Summary line uses `<code>` tags and semantic icons | Should show `🆔` icon |
| Metadata labels bold, values in `<code>` | Should be formatted |
| Table values use backticks for code formatting | Should be code-formatted |
| Boolean values show `✅` or `❌` icons | Should show semantic icons |
| No empty tables shown | Should only show populated tables |

### Regression Test

| Criterion | Expected Result |
| --------- | --------------- |
| Comprehensive demo renders without errors | Should render successfully |
| Existing resource types unchanged | Should not affect existing resources |

## Expected Rendered Output (Feature Test)

The feature test comment in GitHub PR #87 should show:

```
#### Variables

| Name | Value | Is Secret | Allow Override |
| ---- | ----- | --------- | -------------- |
| `🆔 API_KEY` | `(sensitive / hidden)` | `✅ true` | `❌ false` |
| `🆔 BUILD_CONFIGURATION` | `Release` | `❌ false` | `✅ true` |

#### CI Trigger

| Use YAML | Override (Branch Filters) |
| -------- | ------------------------- |
| `✅ true` | `main`, `develop`, `feature/*` |

#### Repository

| Type | Repo ID | Branch | YAML Path | Report Build Status |
| ---- | ------- | ------ | --------- | ------------------- |
| `TfsGit` | `aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee` | `refs/heads/main` | `azure-pipelines.yml` | `✅ true` |
```

**Key Differences from First UAT:**
- Variable names now show `🆔` icon prefix (e.g., `🆔 API_KEY`)
- Boolean values now show `✅` or `❌` icons (e.g., `✅ true`, `❌ false`)
- Summary line in collapsed section should also show `🆔` icon

## Conclusion

Awaiting maintainer approval of UAT PRs to verify:
1. All semantic icons render correctly in GitHub and Azure DevOps
2. Secret masking works as expected
3. No regressions in comprehensive demo

Once approved, feature 094 will be ready for release.
