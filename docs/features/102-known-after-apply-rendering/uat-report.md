# UAT Report: Known-After-Apply Rendering

**Feature:** 102 — Known-After-Apply Rendering  
**Branch:** `feature/102-known-after-apply-rendering`  
**Date:** 2026-02-26  
**Tester:** UAT Tester Agent  

## Result: PASSED ✅

## PR References

| Platform | PR | Status |
|---|---|---|
| GitHub | [#111](https://github.com/oocx/tfplan2md-uat/pull/111) | Closed (passed) |
| Azure DevOps | [#102](https://dev.azure.com/oocx/test/_git/test/pullrequest/102) | Abandoned (passed) |

## Artifacts Tested

| Artifact | Purpose |
|---|---|
| `docs/features/102-known-after-apply-rendering/uat-plan.md` | Feature-specific: all 6 UAT scenarios |
| `artifacts/comprehensive-demo-simple-diff.md` | Regression: GitHub render |
| `artifacts/comprehensive-demo.md` | Regression: Azure DevOps render |

## Validation Checklist

| # | Scenario | Criteria | Result |
|---|---|---|---|
| 1 | `azuread_group_member.all_unknown` — All IDs Computed | Summary shows `(known after apply) → (known after apply)` | ✅ Pass |
| 2 | `azuread_group_member.all_unknown` | Attribute table shows `(known after apply)` for all three attributes | ✅ Pass |
| 3 | `azuread_group_member.platform_admin_member` — Static Config Refs | Summary shows `azuread_group.platform_engineers → azuread_user.admin` | ✅ Pass |
| 4 | `azuread_group_member.platform_admin_member` | Table shows `(known after apply: azuread_group.platform_engineers)` for `group_object_id` | ✅ Pass |
| 5 | `azuread_group_member.platform_admin_member` | Table shows `(known after apply: azuread_user.admin)` for `member_object_id` | ✅ Pass |
| 6 | `azuread_group_member.platform_reader_member` — Mixed | Summary shows `azuread_group.platform_engineers → user-201` | ✅ Pass |
| 7 | `azuread_group_member.platform_reader_member` | `group_object_id` shows `(known after apply: azuread_group.platform_engineers)` | ✅ Pass |
| 8 | `azuread_group_member.platform_reader_member` | `member_object_id` shows concrete value `user-201` | ✅ Pass |
| 9 | `azurerm_resource_group.demo` — Generic computed `id` | `id` row present with `(known after apply)` | ✅ Pass |
| 10 | `azurerm_storage_account.data` — Sensitive + Computed | `primary_access_key` After column shows `🔒(known after apply)` | ✅ Pass |
| 11 | `azurerm_storage_account.data` | Before column shows `(sensitive)`, NOT the actual secret | ✅ Pass |
| 12 | `null_resource.app_config` — Whole-Resource Unknown | Shows `_(all values known after apply)_` | ✅ Pass |
| 13 | `null_resource.app_config` | Does NOT show `_No attribute changes._` | ✅ Pass |
| 14 | Regression | Comprehensive demo renders without unintended changes | ✅ Pass |

## Platform Rendering

| Platform | Result | Notes |
|---|---|---|
| GitHub Markdown | ✅ Pass | All 6 scenarios and regression render correctly |
| Azure DevOps Markdown | ✅ Pass | All 6 scenarios and regression render correctly |

## Maintainer Decision

**Passed** — confirmed by Maintainer in chat on 2026-02-26.
