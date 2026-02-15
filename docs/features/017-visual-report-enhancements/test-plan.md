# Test Plan: Visual Report Enhancements

## Overview

This test plan covers the visual enhancements to the Terraform plan reports, ensuring improved readability, professional appearance, and cross-platform compatibility (GitHub and Azure DevOps). The implementation follows a "model-first" approach where complex logic is handled in C# and Scriban templates remain simple.

Reference: [specification.md](specification.md), [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Module separators (---) between modules | TC-01 | Integration (Renderer) |
| Module icons (📦) in headers | TC-01 | Integration (Renderer) |
| Resource entries in `<details>` tags | TC-02 | Integration (Renderer) |
| Resource type (plain) and name (**bold** `code`) | TC-03 | Unit (Model/Helper) |
| Location icons (🌍) in `(<code>🌍 location</code>)` | TC-04 | Unit (Helper) |
| IP/CIDR icons (🌐) inside code blocks | TC-05 | Unit (Helper) |
| Security rule actions (✅ Allow / ⛔ Deny) | TC-06 | Unit (Helper) |
| Boolean values (✅ true / ❌ false) | TC-07 | Unit (Helper) |
| Network direction (⬇️ Inbound / ⬆️ Outbound) | TC-08 | Unit (Helper) |
| Protocol icons (🔗 TCP, 📨 UDP, 📡 ICMP, ✳️ *) | TC-09 | Unit (Helper) |
| Tags as inline badges with 🏷️ icon | TC-10 | Unit (Model/Helper) |
| Changed attributes as `count 🔧 attributes` | TC-11 | Unit (Model/Helper) |
| GitHub/Azure DevOps compatibility (HTML `<code>` in summary) | TC-12 | Integration (Renderer) |
| Stable resource block anchors (HTML comments) | TC-13 | Integration (Renderer) |
| Resource-specific templates alignment | TC-14 | Integration (Renderer) |
| Markdown quality validation | TC-15 | Integration (Lint) |

## User Acceptance Scenarios

> **Purpose**: Verify rendering in real-world PR environments (GitHub and Azure DevOps) to ensure visual quality and compatibility.

### Scenario 1: Comprehensive Visual Review

**User Goal**: Verify all new visual elements render correctly in a realistic plan.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR comments.

**Expected Output**:
- Horizontal rules between modules.
- 📦 icon before module paths.
- Collapsible resources with rich summary lines.
- HTML `<code>` used in summaries (renders correctly in AzDO).
- Semantic icons (🌍, 🌐, ✅, ❌, etc.) appearing in correct positions.
- Tags displayed as badges below tables.

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown.
- [ ] Output renders correctly in Azure DevOps Markdown.
- [ ] No "broken" markdown (e.g., tables inside summaries).
- [ ] Spacing looks professional (no double horizontal rules, proper `<br>` usage).

---

### Scenario 2: Resource-Specific Template Consistency

**User Goal**: Ensure NSG and Firewall rule templates match the new visual style.

**Test PR Context**:
- **GitHub/AzDO**: Use a plan with NSG and Firewall rule changes.

**Expected Output**:
- NSG/Firewall resources are also collapsed in `<details>`.
- Summary lines follow the same pattern (Action, Type, Name, Context).
- Internal tables use the same semantic icons for Access, Direction, Protocol, and IPs.

---

## Test Cases

### TC-01: ModuleSeparation_MultipleModules_RendersHorizontalRulesAndIcons

**Type:** Integration (Renderer)

**Description:**
Verifies that horizontal rules are placed between modules and the 📦 icon is added to module headers.

**Preconditions:**
- Plan with at least two modules.

**Test Steps:**
1. Render the plan.
2. Verify `---` exists between module sections.
3. Verify `### 📦 Module:` exists for each module.

**Expected Result:**
Modules are clearly separated and icons are present.

---

### TC-02: ResourceLayout_AllResources_WrappedInDetailsAndSummary

**Type:** Integration (Renderer)

**Description:**
Verifies that every resource change is wrapped in a `<details>` block with a `<summary>` and a `<br>` for spacing.

**Preconditions:**
- Plan with various resource changes (create, update, delete).

**Test Steps:**
1. Render the plan.
2. Verify each resource starts with `<details>` and contains a `<summary>`.
3. Verify `<br>` follows the `</summary>` tag.

**Expected Result:**
All resources are collapsible.

---

### TC-03: ResourceNaming_TypeAndName_FormattedCorrectly

**Type:** Unit (Model/Helper)

**Description:**
Verifies that resource type is plain text and name is **bold** `code`.

**Test Steps:**
1. Call `format_summary_html` or check `SummaryHtml` property.
2. Verify output contains `type **<code>name</code>**`.

**Expected Result:**
`azurerm_virtual_network **<code>hub</code>**`

---

### TC-04: LocationFormatting_SummaryAndTable_UsesGlobeIcon

**Type:** Unit (Helper)

**Description:**
Verifies location formatting with 🌍 icon.

**Test Steps:**
1. Call `format_attribute_value_summary` with a location attribute.
2. Call `format_attribute_value_table` with a location attribute.

**Expected Result:**
- Summary: `(<code>🌍 eastus</code>)`
- Table: `` `🌍 eastus` ``

---

### TC-05: NetworkFormatting_IPsAndCIDRs_UsesNetworkIconInsideCode

**Type:** Unit (Helper)

**Description:**
Verifies IP/CIDR formatting with 🌐 icon inside code blocks.

**Test Steps:**
1. Call helpers with IP/CIDR values.

**Expected Result:**
- Summary: `<code>🌐 10.0.0.0/16</code>`
- Table: `` `🌐 10.0.0.0/16` ``

---

### TC-06: SecurityRuleActions_AllowDeny_UsesIcons

**Type:** Unit (Helper)

**Description:**
Verifies `Allow` and `Deny` formatting.

**Expected Result:**
- `Allow` -> `✅ Allow`
- `Deny` -> `⛔ Deny`

---

### TC-07: BooleanFormatting_TrueFalse_UsesIcons

**Type:** Unit (Helper)

**Description:**
Verifies boolean formatting.

**Expected Result:**
- `true` -> `✅ true`
- `false` -> `❌ false`

---

### TC-08: NetworkDirection_InboundOutbound_UsesArrows

**Type:** Unit (Helper)

**Description:**
Verifies direction formatting.

**Expected Result:**
- `Inbound` -> `⬇️ Inbound`
- `Outbound` -> `⬆️ Outbound`

---

### TC-09: ProtocolFormatting_AllProtocols_UsesIcons

**Type:** Unit (Helper)

**Description:**
Verifies protocol formatting.

**Expected Result:**
- `Tcp` -> `🔗 TCP`
- `Udp` -> `📨 UDP`
- `Icmp` -> `📡 ICMP`
- `*` -> `✳️ *`

---

### TC-10: TagsFormatting_CreateDelete_RendersAsBadges

**Type:** Unit (Model/Helper)

**Description:**
Verifies tags are rendered as inline badges for create/delete operations.

**Expected Result:**
`**🏷️ Tags:** `key1: value1` `key2: value2``

---

### TC-11: ChangedAttributes_Update_RendersSummaryLine

**Type:** Unit (Model/Helper)

**Description:**
Verifies the changed attributes summary for update operations.

**Expected Result:**
`2 🔧 attribute1, attribute2`

---

### TC-12: AzDOCompatibility_Summary_UsesHtmlCode

**Type:** Integration (Renderer)

**Description:**
Verifies that `<summary>` tags use `<code>` instead of backticks.

**Test Steps:**
1. Render a plan.
2. Search for `<summary>` tags.
3. Verify no backticks (`` ` ``) exist inside `<summary>`.
4. Verify `<code>` tags are used instead.

---

### TC-13: ReplacementAnchors_ResourceBlocks_UsesHtmlComments

**Type:** Integration (Renderer)

**Description:**
Verifies that resource blocks are wrapped in stable HTML comment anchors.

**Expected Result:**
`<!-- tfplan2md:resource-start address=... -->` and `<!-- tfplan2md:resource-end address=... -->` are present.

---

### TC-14: ResourceSpecificTemplates_Alignment_MatchesNewStyle

**Type:** Integration (Renderer)

**Description:**
Verifies that NSG, Firewall, and Role Assignment templates use the new `<details>/<summary>` structure and anchors.

---

### TC-15: MarkdownLint_NewOutput_PassesValidation

**Type:** Integration (Lint)

**Description:**
Verifies that the new output format passes all markdownlint rules.

---

### TC-16: LargeAttributeSpacing_WithOtherContent_AddsBreakBeforeDetails

**Type:** Unit (Template)

**Description:**
Verifies that a `<br/>` is added before the large attributes `<details>` block when the resource has small attributes or tags.

**Expected Result:**
Markdown contains `<br/>\n<details>\n<summary>Large values` when small attributes or tags exist.

---

### TC-17: LargeAttributeSpacing_WithoutOtherContent_NoExtraBreak

**Type:** Unit (Template)

**Description:**
Verifies that no extra `<br/>` is added before large attributes when the resource has no small attributes or tags.

**Expected Result:**
Markdown does not contain `<br/>\n<details>\n<summary>Large values`.

---

### TC-18: LargeAttributesOnly_RendersInlineNotCollapsible

**Type:** Unit (Template)

**Description:**
Verifies that resources with only large attributes (no small attributes or tags) render large values inline without an inner collapsible section.

**Expected Result:**
- Markdown contains `Large values:` summary text
- Markdown does not contain `<details>\n<summary>Large values`
- Large value content is directly visible under the resource summary

---

### TC-19: InlineDiff_BlockCodeStyling_AlignsInTables

**Type:** Unit (Helper)

**Description:**
Verifies that inline diff output uses block-level code styling with reset padding/whitespace for proper table alignment in Azure DevOps.

**Expected Result:**
Inline diff output contains `<code style="display:block; white-space:normal; padding:0; margin:0;">` wrapper.

---

## Test Data Requirements

- `multi-module-plan.json`: Existing, but ensure it has enough modules for TC-01.
- `firewall-rule-changes.json`: Existing, for TC-14.
- `comprehensive-demo/plan.json`: For Scenario 1.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Resource with no name/type | Fallback to address | TC-03 |
| Very long list of changed attributes | Truncate with "+N more" | TC-11 |
| Resource with no tags | No tags badge rendered | TC-10 |
| Unknown location/protocol | Render as-is without icon | TC-04, TC-09 |

## Non-Functional Tests

- **Compatibility**: Verify rendering in both GitHub and Azure DevOps (UAT).
- **Performance**: Ensure precomputing model properties doesn't significantly slow down rendering.

## Open Questions

None.
