# UAT Report: Azure Display Enhancements

**Status:** ❌ FAILED
**Date:** 2026-02-08

## Summary
The UAT failed due to missing display names for subscriptions and management groups, rendering issues with icons, and empty details blocks for certain resources.

## Tested PRs
- **GitHub:** #57 (FAILED)
- **Azure DevOps:** #62 (FAILED)

## Findings

### 1. Missing Features/Coverage
- Specific demo does not cover:
    - Subscription name mapping.
    - Management group name mapping (including root group special case).
    - Custom role mapping.

### 2. Rendering Issues (Bugs)
- **Missing Subscription Display Names:**
    - Comprehensive demo: `rg-tfplan2md-demo in subscription 🔑 12345678-1234-1234-1234-123456789012` (expected display name).
    - `azurerm_pim_eligible_role_assignment`: Principal ID is raw GUID, scope is missing subscription name.
    - Key Vault scope: `Key Vault kv-tfplan2md in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012` (missing display name).
- **Missing Icons:**
    - `rg-app in subscription Production (sub-123)` is missing icons for subscription and resource group.
- **Empty Detail Blocks:**
    - `azurerm_role_management_policy` details block is empty/has no content when opened.

### 3. Workflow Issues
- The UAT PR should contain **two separate comments**: one for the specific test case and one for regression testing (Comprehensive Demo).

## Requested Enhancements
- **DNS A Record Summary:**
    - Format: `➕ azurerm_private_dns_a_record `🆔 record1` — `record1.contoso.local` `🌐 10.0.0.4``
    - Should show the name and up to 3 records.

## Next Steps
- Handoff to Developer to fix the reported bugs and implement the DNS A record enhancement.
- Update test artifacts to better cover the missing mapping scenarios.
