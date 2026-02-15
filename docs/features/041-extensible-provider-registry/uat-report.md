# UAT Report: Extensible Provider Registry System

## Status: PASSED

## Test Details

| Field | Value |
|---|---|
| **Feature** | 061 — Extensible Provider Registry |
| **Artifact** | `artifacts/extensible-registry-uat.md` |
| **Date** | 2026-02-07 |
| **Tester** | Copilot (UAT Tester agent) |
| **Reviewer** | Maintainer (manual) |

## UAT PRs

| Platform | PR | Result |
|---|---|---|
| GitHub | [#54](https://github.com/oocx/tfplan2md-uat/pull/54) | PASS |
| Azure DevOps | [#60](https://dev.azure.com/oocx/test/_git/test/pullrequest/60) | PASS |

## Validation Summary

Verified per the [UAT Test Plan](uat-test-plan.md):

1. **Provider Iconography & Identifiers** — Azure AD User (🆔, 📧), Group (👥), Service Principal (💻) icons render correctly.
2. **Value Formatting** — Resource IDs shortened with 📁 icon; subscription IDs display with 🔑 icon.
3. **Semantic Icons (NSG / Firewall)** — Allow (✅), Deny (⛔), Inbound (⬇️), Outbound (⬆️), TCP (🔗), UDP (📨), ICMP (📡), Any (✳️) icons all render correctly on both platforms.

## Notes

- WSL interop was initially missing in the VS Code terminal, preventing Azure DevOps UAT PR creation. Fixed by re-registering interop and adding `ensure_azdo_credential_helper()` to the UAT scripts as a permanent safeguard.
