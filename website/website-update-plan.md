# Website Update Plan — Features Since Feb 2, 2026

Covers releases **v1.9.0 – v1.36.0** (Feb 3 – Mar 7, 2026). Only user-facing changes.

---

## 1. `--details` CLI Option

**Version:** v1.23.0 (Feature 092)

Controls whether resource detail sections (`<details>`) are expanded or collapsed in the output. Values: `open` (default, current behavior), `closed`, `none`.

| Attribute | Value |
|---|---|
| **Feature page category** | Built-in Capabilities |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — add `--details <mode>` to CLI options table |
| **Examples page update** | No |
| **Feature sub-page update** | Update [features/misc.njk](website/src/pages/features/misc.njk) collapsible-details section to mention the new CLI flag |
| **Other pages** | None |

---

## 2. `--ignore-azure-id-case-changes` CLI Option

**Version:** v1.31.0 (Feature 103)

Suppresses noise from Azure resource ID casing-only changes. Enabled by default. Filtered resources show a "filtering note" in the report.

| Attribute | Value |
|---|---|
| **Feature page category** | Built-in Capabilities |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — add `--ignore-azure-id-case-changes` to CLI options table (with note it defaults to `true`) |
| **Examples page update** | No |
| **Feature sub-page update** | Update [features/azure-optimizations.njk](website/src/pages/features/azure-optimizations.njk) or add a note to [features/misc.njk](website/src/pages/features/misc.njk) |
| **Other pages** | None |

---

## 3. Azure DevOps `tfplan2md_haschanges` Pipeline Variable

**Version:** v1.35.0 (Feature 109)

When run with `-o` in Azure DevOps pipelines, tfplan2md emits `##vso[task.setvariable variable=tfplan2md_haschanges]true/false`. Enables conditional pipeline steps.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — document the pipeline variable in a new "Azure DevOps Integration" sub-section |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 4. Azure DevOps `azuredevops_build_definition` Tables

**Version:** v1.24.0 (Feature 094)

Structured rendering for build definitions: repository settings, triggers, variable groups in organized tables with ✅/❌ boolean icons and 🗃️/⎇ icons.

| Attribute | Value |
|---|---|
| **Feature page category** | Built-in Capabilities |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | **Yes** — add an example showing build definition rendering |
| **Feature sub-page update** | Create a new feature page (or extend [features/azdo-variable-groups.njk](website/src/pages/features/azdo-variable-groups.njk) into a broader "Azure DevOps Resources" page) |
| **Other pages** | [providers/azuredevops.njk](website/src/pages/providers/azuredevops.njk) — update from "Partial Support" to "Implemented"; add build definitions to implemented resources; remove "Build and release pipelines" from planned |

---

## 5. Azure DevOps Principal Mapping

**Version:** v1.18.0 (Feature 085)

Maps Azure DevOps user, group, project, and team GUIDs to display names. Extends the existing `--principal-mapping` JSON file with `azdoUsers`, `azdoGroups`, `azdoProjects`, `azdoTeams` sections.

| Attribute | Value |
|---|---|
| **Feature page category** | Built-in Capabilities |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — update `--principal-mapping` description to mention Azure DevOps support; add mapping file format example |
| **Examples page update** | No |
| **Feature sub-page update** | Update [features/azure-optimizations.njk](website/src/pages/features/azure-optimizations.njk) to show AzDO mapping alongside Azure mapping |
| **Other pages** | [providers/azuredevops.njk](website/src/pages/providers/azuredevops.njk) — mention principal mapping support |

---

## 6. Azure DevOps Repository Mapping and Icons

**Version:** v1.26.0 (Feature 096)

Maps Azure DevOps repository IDs to display names. Adds 🗃️ (repository) and ⎇ (branch) icons in build definition tables.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (covered by #4 build definition card) |
| **Homepage carousel** | No (part of #4) |
| **Docs page update** | **Yes** — mention `azdoRepositories` in the mapping file format |
| **Examples page update** | No |
| **Feature sub-page update** | Include in build definition feature page from #4 |
| **Other pages** | [providers/azuredevops.njk](website/src/pages/providers/azuredevops.njk) — mention repository mapping and icons for build definitions |

---

## 7. `azurerm_firewall_application_rule_collection` Template

**Version:** v1.11.0 (Feature 060)

Dedicated template for firewall application rule collections, showing rules with protocol/port and target FQDN details.

| Attribute | Value |
|---|---|
| **Feature page category** | What Sets Us Apart (extend existing Firewall Rules card) |
| **Homepage carousel** | No (already covered by "Firewall Rules" card — update its description to mention application rule collections) |
| **Docs page update** | No |
| **Examples page update** | **Yes** — add an example for application rule collection rendering |
| **Feature sub-page update** | Update [features/firewall-rules.njk](website/src/pages/features/firewall-rules.njk) to include application rule collections alongside network rule collections |
| **Other pages** | [providers/azurerm.njk](website/src/pages/providers/azurerm.njk) — add "Firewall Application Rule Collections" to specialized resources |

---

## 8. Terraform Outputs Section

**Version:** v1.27.0 (Feature 097)

Reports now include a dedicated "Outputs" table showing output values with Change column (create/update/delete), semantic formatting, principal mapping, and JSON compaction.

| Attribute | Value |
|---|---|
| **Feature page category** | Also Included |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — mention outputs section in the default template description |
| **Examples page update** | **Yes** — add an example showing output values rendering |
| **Feature sub-page update** | Create a new feature sub-page or add a section to [features/misc.njk](website/src/pages/features/misc.njk) |
| **Other pages** | None |

---

## 9. Azapi Output Values Table

**Version:** v1.32.0 (Feature 106)

Azapi resources render output values in a separate table, handling known-after-apply and sensitive masking.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (covered by #8 outputs feature) |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | Mention in the outputs feature page from #8 |
| **Other pages** | [providers/azapi.njk](website/src/pages/providers/azapi.njk) — mention output values table support for azapi resources |

---

## 10. Known-After-Apply Rendering

**Version:** v1.30.0 (Feature 102)

Attributes marked "known after apply" are rendered with a `(known after apply)` placeholder instead of being omitted. Whole-resource-unknown scenarios show an explanatory note.

| Attribute | Value |
|---|---|
| **Feature page category** | Built-in Capabilities |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | Add a note to [features/value-formatting.njk](website/src/pages/features/value-formatting.njk) or [features/misc.njk](website/src/pages/features/misc.njk) |
| **Other pages** | None |

---

## 11. Parent-Child Resource Grouping

**Version:** v1.16.0 – v1.17.0 (Features 068/072)

Related resources are grouped together: Azure AD group + members, Azure DevOps group + members, Azure RM subnets under VNets, NSG rules under NSGs, DNS records, route table routes. Child resources appear as inline tables within the parent section.

| Attribute | Value |
|---|---|
| **Feature page category** | What Sets Us Apart (high) |
| **Homepage carousel** | **Yes** — add a "Resource Grouping" carousel card |
| **Docs page update** | No |
| **Examples page update** | **Yes** — add an example showing parent-child grouping |
| **Feature sub-page update** | Create a new dedicated feature page (e.g., `features/resource-grouping.njk`); optionally link from [features/nsg-rules.njk](website/src/pages/features/nsg-rules.njk) |
| **Other pages** | [providers/azurerm.njk](website/src/pages/providers/azurerm.njk) — add parent-child grouping as a global enhancement; [providers/azuread.njk](website/src/pages/providers/azuread.njk) — mention inline member tables |

---

## 12. Azure Display Enhancements

**Version:** v1.13.0 (Feature 063)

Enriched Azure resource ID formatting: subscription, resource group, resource names parsed and displayed with semantic icons (🔑 subscription, 📦 resource group). PIM/role policy summaries, private DNS A record summaries, broadened resource ID detection.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (enhances existing "Friendly Resource Names" and "Smart Iconography" cards) |
| **Homepage carousel** | No (already represented by existing "Friendly Names" card) |
| **Docs page update** | No |
| **Examples page update** | No (existing examples should reflect the improved rendering) |
| **Feature sub-page update** | Update [features/azure-optimizations.njk](website/src/pages/features/azure-optimizations.njk) with the richer formatting details |
| **Other pages** | [providers/azurerm.njk](website/src/pages/providers/azurerm.njk) — update the "Resource ID Formatting" bullet to mention scope parsing with icons |

---

## 13. Tenant and Management Group Display Mapping

**Version:** v1.15.0 (Feature 065)

Maps Azure tenant IDs and management group IDs to display names via the principal mapping file. Shows 🏢 icon for tenants and formatted scope labels for management groups.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (enhances existing Role Assignment Mapping card) |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — update `--principal-mapping` description to mention tenant and management group mapping |
| **Examples page update** | No |
| **Feature sub-page update** | Update [features/azure-optimizations.njk](website/src/pages/features/azure-optimizations.njk) to mention tenant/management group mapping |
| **Other pages** | None |

---

## 14. Enhanced Role Assignment Display

**Version:** v1.27.0, v1.29.0

Subscription names in role assignment summaries. Support for `user_principal_id` and `object_id` in principal mapping. 💻 icon for service principals.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (enhances existing Role Assignment Mapping card) |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No (regenerate existing role assignment examples to show updated rendering) |
| **Feature sub-page update** | Update [features/azure-optimizations.njk](website/src/pages/features/azure-optimizations.njk) to reflect the improved display |
| **Other pages** | None |

---

## 15. Ephemeral Resource `open` Action Support

**Version:** v1.28.0

Support for OpenTofu's `open` action type and `['create', 'forget']` replace variant.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 16. 'No Changes' Display for Zero-Change Plans

**Version:** v1.21.0

Reports show "No changes" instead of an empty report when a plan has no changes.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 17. Collapsible Debug Section

**Version:** v1.21.0 (Feature 086)

Debug section (`--debug`) is now wrapped in a collapsed `<details>` block.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (already covered by existing "Debug Output" card in Also Included) |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | Update existing "Debug Output" card description in [features.js](website/src/_data/features.js) to mention the collapsible display |
| **Other pages** | None |

---

## 18. Code Analysis Tool Column

**Version:** v1.10.0 (Feature 059)

Static analysis findings tables now include a "Tool" column showing which SARIF tool (Checkov, TfLint, Trivy) produced each finding.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (enhances existing Static Code Analysis card) |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | **Yes** — regenerate static analysis examples to show the new Tool column |
| **Feature sub-page update** | Update [features/static-analysis.njk](website/src/pages/features/static-analysis.njk) to mention the Tool column |
| **Other pages** | None |

---

## 19. Style Guide Compliance Fixes

**Version:** v1.20.0

Non-breaking spaces after emoji icons, consistent heading spacing, markdown rendering improvements.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | **Yes** — regenerate all examples to pick up improved styling |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 20. Sensitive Value Masking Improvements

**Version:** v1.23.1, v1.26.1 (Issues 093/098)

Expanded masking to nested/array attributes, AzApi body rendering, Variable Group secrets, and before_json/after_json fields.

| Attribute | Value |
|---|---|
| **Feature page category** | Also Included (update existing "Sensitive Value Masking" card description) |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | **Yes** — regenerate sensitive-masking example |
| **Feature sub-page update** | Update [features/sensitive-masking.njk](website/src/pages/features/sensitive-masking.njk) to mention broader coverage |
| **Other pages** | None |

---

## 21. Subresource Integrity (SRI) for HTML Templates

**Version:** v1.26.1

HTML report templates include SRI hashes for external resources.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 22. Multi-Platform Binary Distribution

**Version:** v1.17.0 – v1.19.0

NativeAOT standalone binaries for Linux x64, Linux ARM64, macOS x64, macOS ARM64, Windows x64 with checksums. No .NET runtime needed.

| Attribute | Value |
|---|---|
| **Feature page category** | Built-in Capabilities (update existing "Container Support" card or add new "Multi-Platform Binaries" card) |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — add binary download/install instructions |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | [getting-started.njk](website/src/pages/getting-started.njk) — **Yes, high priority** — add "Binary Download" as an installation method alongside Docker |

---

## 23. Alpine/musl Binaries

**Version:** v1.27.0

Additional binaries for `linux-musl-x64` and `linux-musl-arm64` (Alpine Linux).

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (part of #22) |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | [getting-started.njk](website/src/pages/getting-started.njk) — mention musl targets in the binary download section from #22 |

---

## 24. UPX Compression for Binaries

**Version:** v1.34.0

Linux and Windows binaries compressed with UPX for smaller downloads.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 25. Homebrew Installation

**Version:** v1.22.0 (Feature 089)

`brew install oocx/tap/tfplan2md` — Homebrew formula auto-updated on each release.

| Attribute | Value |
|---|---|
| **Feature page category** | Also Included |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | [getting-started.njk](website/src/pages/getting-started.njk) — **Yes, high priority** — add "Homebrew" as an installation method |

---

## 26. Performance Optimizations

**Version:** v1.31.1 (Issue 105)

LCS matrix size guard, caching, diff cutoffs. Prevents memory blowup with large attribute values.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 27. Scriban Removed — Pure C# Rendering Engine

**Version:** v1.33.0 (Feature 107)

Scriban template engine replaced with native C# rendering pipeline. Transparent to users, but custom `.sbn` template files are no longer supported.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — update `--template` CLI description: remove "or a custom Scriban template file path"; update built-in template descriptions. Remove/update "Custom Templates" references |
| **Examples page update** | No |
| **Feature sub-page update** | **Remove or repurpose** [features/custom-templates.njk](website/src/pages/features/custom-templates.njk) — custom Scriban templates are no longer supported. Update the "Custom Templates" card in `features.js` |
| **Other pages** | [architecture page](website/src/pages/architecture.njk) — remove Scriban references; update rendering pipeline description. **Also mention on architecture page that tfplan2md now has zero third-party dependencies** (good for binary size and security) |

---

## 28. AzAPI Casing-Only ID Filter

**Version:** v1.34.0 (Issue 108)

Azure API resource IDs with only casing differences in the body JSON are filtered from update diffs.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention (related to #2 `--ignore-azure-id-case-changes`) |
| **Homepage carousel** | No |
| **Docs page update** | **Yes** — document that AzAPI body-level casing-only ID changes are automatically filtered (part of `--ignore-azure-id-case-changes` behavior) |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 29. No-Op Parent Resources No Longer Hide Child Changes

**Version:** v1.21.1 (Issue 088)

Bug fix: parent resources with no-op changes that have children with actual changes are now preserved.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 30. Nested Array Rendering Fix

**Version:** v1.22.0 (Issue 089)

Arrays show only changed items in update mode. Improved diff rendering for new/removed elements.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 31. AzAPI Resources Auto-Expand Only With Warnings

**Version:** v1.22.1 (Issue 091)

Resources are auto-expanded only when code analysis warnings exist, not unconditionally.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 32. Decimal Numbers No Longer Show IP Icon

**Version:** v1.20.1 (Issue 087)

Bug fix: decimal numbers (e.g., `1.5`) no longer incorrectly get a 🌐 icon.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 33. Handle Terraform `read` Action

**Version:** v1.17.2

Prevents false "Already imported" warnings for data sources using the `read` action.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## 34. `OutputChange.AfterUnknown` Type Fix

**Version:** v1.31.2

Fixed parsing of `after_unknown` field (bool vs. object) in Terraform plan JSON.

| Attribute | Value |
|---|---|
| **Feature page category** | No direct mention |
| **Homepage carousel** | No |
| **Docs page update** | No |
| **Examples page update** | No |
| **Feature sub-page update** | No |
| **Other pages** | None |

---

## Summary: High-Priority Website Updates

### Feature Page — New/Updated Cards

| Section | Action |
|---|---|
| **What Sets Us Apart** | Add: Parent-Child Resource Grouping (#11) |
| **Built-in Capabilities** | Add: `--details` option (#1), `--ignore-azure-id-case-changes` (#2), Build Definition Tables (#4), Known-After-Apply (#10). Update: "Custom Templates" card (Scriban removed #27), "Container Support" → mention binaries (#22) |
| **Also Included** | Add: Terraform Outputs (#8), Homebrew (#25). Update: "Sensitive Value Masking" card (#20) |

### Homepage Carousel — New Cards

- Parent-Child Resource Grouping (#11)

### Docs Page Updates

- Add CLI options: `--details`, `--ignore-azure-id-case-changes`
- Update `--principal-mapping` description (Azure DevOps + tenant mapping)
- Update `--template` description (remove Scriban custom template reference)
- Add Azure DevOps pipeline variable (`tfplan2md_haschanges`) documentation
- Add outputs section to template descriptions
- Document AzAPI casing-only ID filtering (#28) as part of `--ignore-azure-id-case-changes` behavior

### Getting Started Page Updates

- Add Homebrew installation method (#25)
- Add binary download installation method (#22, #23)

### Examples Page Updates

- New examples: build definition tables (#4), terraform outputs (#8), parent-child grouping (#11), firewall application rules (#7)
- Regenerate existing examples for: static analysis (#18), sensitive masking (#20), style fixes (#19)

### Provider Page Updates

- [providers/azuredevops.njk](website/src/pages/providers/azuredevops.njk): upgrade from "Partial Support" to "Implemented"; add build definitions, principal mapping, repository mapping (#6)
- [providers/azurerm.njk](website/src/pages/providers/azurerm.njk): add firewall application rules, parent-child grouping, enhanced resource ID formatting
- [providers/azapi.njk](website/src/pages/providers/azapi.njk): mention output values table support (#9)

### Architecture Page Updates

- Remove Scriban references; update rendering pipeline description (#27)
- Mention zero third-party dependencies (good for binary size and security) (#27)
- Update Docker image size from 14.7 MB to 2.1 MB (see global update below)

### Feature Sub-Page Updates

- **Remove/repurpose** `features/custom-templates.njk` (Scriban no longer supported)
- **New page:** resource grouping (or extend misc)
- **Update:** firewall-rules, azure-optimizations, static-analysis, sensitive-masking, misc, azdo-variable-groups

### Global: Docker Image Size Update

All mentions of the Docker image size must be updated from **14.7 MB** to **2.1 MB** (98.5% reduction from baseline). Affected source files:

- `website/src/_data/architecturePage.js` — 3 occurrences (quality attribute, container base, NativeAOT pattern)
- `website/src/_data/features.js` — 1 occurrence (Container Support card description)
