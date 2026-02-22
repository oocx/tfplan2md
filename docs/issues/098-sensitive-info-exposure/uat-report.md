# UAT Report: Sensitive Information Exposure Fix (Issue 098)

**Date:** 2026-02-22  
**Branch:** `fix/098-sensitive-info-exposure`  
**Status:** ✅ PASSED

## Summary

UAT executed on both GitHub and Azure DevOps using the feature-specific plan artifact (`uat-plan.md`) and the comprehensive demo for regression.

## PRs

| Platform | PR | Result |
|---|---|---|
| GitHub | [#94](https://github.com/oocx/tfplan2md-uat/pull/94) | ✅ PASSED |
| Azure DevOps | [#92](https://dev.azure.com/oocx/test/_git/test/pullrequest/92) | ✅ PASSED |

## Feature Test Results (`uat-plan.md`)

| # | Check | Expected | Result |
|---|---|---|---|
| 1 | `azapi_resource.sql_server` create — `administratorLoginPassword` | `` `(sensitive)` `` shown; `sqladmin` still visible | ✅ PASS |
| 2 | `azapi_resource.policy_assignment` update — `clientSecret` Before + After | Both columns show `` `(sensitive)` `` | ✅ PASS |
| 3 | `azapi_resource.app_registration` delete — sensitive before-value | `` `(sensitive)` `` shown | ✅ PASS |
| 4 | `azuredevops_variable_group.pipeline_vars` — `is_secret: true→false` Before | `` `(sensitive / hidden)` `` shown | ✅ PASS |
| 5 | `azurerm_key_vault_secret.root_sensitive` — root `after_sensitive: true` | All attributes masked | ✅ PASS (by design — root boolean masks all) |
| 6 | `azapi_resource.container_app` — array-parent `secrets: true` | `secrets` entry shows `(sensitive)` | ✅ PASS |

**Note on check #5:** The Maintainer queried why all attributes of `azurerm_key_vault_secret.root_sensitive` are masked. This is intentional test data: `after_sensitive: true` (a root-level boolean, not an object) is what Terraform emits when it cannot enumerate specific sensitive attributes — the correct response is to mask the entire resource. A typical `azurerm_key_vault_secret` would have `after_sensitive: { "value": true }`, masking only `value`, as seen in the comprehensive demo.

## Regression Test Results (`comprehensive-demo.md`)

- No `(sensitive)` placeholder appeared for resources without sensitivity metadata. ✅
- Standard Azure resources (VMs, storage, VNets) rendered correctly. ✅
- Layout (tables, details blocks, summary counts) intact. ✅

## Artifacts Used

| Artifact | Purpose |
|---|---|
| `docs/issues/098-sensitive-info-exposure/uat-plan.md` | Feature-specific test (all 6 exposure paths) |
| `artifacts/comprehensive-demo-simple-diff.md` | GitHub regression test |
| `artifacts/comprehensive-demo.md` | Azure DevOps regression test |
