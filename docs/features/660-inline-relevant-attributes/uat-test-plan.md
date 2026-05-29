# UAT Test Plan: Inline Relevant Attributes (Feature 660)

## Goal

Verify that inline relevant-attribute annotations (forced-replacement callouts, depends-on lines, and the fallback section) render correctly in GitHub and Azure DevOps PR comments, and that no regressions appear in existing output.

---

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)

**Purpose:** Focus testing on the specific inline annotation changes in this feature. This artifact MUST be real tfplan2md output, not synthetic or simulated.

**Source Plan Path:** `docs/features/660-inline-relevant-attributes/uat-plan.json`

**Rendered Output Path:** `docs/features/660-inline-relevant-attributes/uat-plan.md`

**Plan Requirements:**

The `uat-plan.json` MUST be a Terraform plan JSON that exercises all of the following:

1. **Resource being replaced with forced-replacement annotation (upstream changing in this plan):**
   - E.g. `azurerm_virtual_machine.web` replaced because `network_interface_ids` changed
   - `azurerm_network_interface.web` also being replaced in the same plan
   - `relevant_attributes` contains `{resource: "azurerm_network_interface.web", attribute: ["id"]}`
   - Expected: `⚠️ **Forced replacement** — \`network_interface_ids\` reads \`azurerm_network_interface.web.id\`, which is **changing in this plan**.`

2. **Resource being replaced with combined card (forced + also depends on):**
   - E.g. `azurerm_app_service.api` replaced because `app_settings` changed (forced by `azurerm_key_vault.main` vault_uri)
   - `azurerm_app_service.api` also has `identity` that references `data.azurerm_client_config.current.tenant_id`
   - Both upstream resources in `relevant_attributes`
   - Expected: both `⚠️ **Forced replacement**` AND `🔗 **Also depends on:**` lines

3. **Resource being replaced with depends-on only (no forced path traces to upstream):**
   - E.g. `azurerm_storage_account.logs` replaced for unrelated reason, but depends on `data.azurerm_subscription.current.subscription_id`
   - Expected: `🔗 **Depends on:** \`data.azurerm_subscription.current.subscription_id\``

4. **In-place update resource with correlated upstream (no annotations expected):**
   - E.g. `azurerm_resource_group.main` updated (not replaced)
   - Its `ConfigurationReferences` references an upstream in `relevant_attributes`
   - Expected: NO annotation lines on this resource card

5. **Fallback section (at least one uncorrelated attribute):**
   - At least one relevant attribute that cannot be correlated to any changed resource
   - Expected: `<details>` fallback section at end of report

**Rationale:** This plan exercises all five distinct rendering paths introduced by the feature in a single report, making it easy to visually verify every path in one PR comment.

**Key Resources to check:**
1. `azurerm_virtual_machine.web` — forced-replacement callout with "changing in this plan"
2. `azurerm_app_service.api` — combined card (forced + also depends on)
3. `azurerm_storage_account.logs` — depends-on only
4. Fallback section at end of report

**Coverage:**
- ⚠️ Forced replacement callout with "changing in this plan" bold phrase
- ⚠️ Forced replacement callout WITHOUT "changing in this plan" phrase
- 🔗 Depends on line (standalone)
- 🔗 Also depends on line (combined with forced)
- Fallback `<details>` section for uncorrelated attributes
- In-place update resource with no annotations

**Example Creation Command:**
```bash
# Generate the rendered output from the plan (run from repo root)
tfplan2md docs/features/660-inline-relevant-attributes/uat-plan.json > docs/features/660-inline-relevant-attributes/uat-plan.md
```

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in other areas of the report.

**Artifact Path:**
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using the `generate-demo-artifacts` skill.

---

## Test Steps

1. Developer creates `uat-plan.json` based on the requirements above
2. Developer runs `tfplan2md uat-plan.json > uat-plan.md` to generate the rendered output
3. Code Reviewer validates both files exist and are complete
4. UAT Tester posts TWO separate PR comments:
   - **Feature-Specific Report**: Tests the specific inline annotation changes using `uat-plan.md`
   - **Comprehensive Demo**: Regression test using `artifacts/comprehensive-demo.md` (or `comprehensive-demo-simple-diff.md` for GitHub)
5. Verify both reports on GitHub and Azure DevOps

---

## Validation Instructions (Test Description)

### Feature-Specific Validation

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"):

#### 1. Verify forced-replacement callout with "changing in this plan"

**Resource to find:** `azurerm_virtual_machine.web` card  

**Expected line inside the `<details>` block, above the diff table:**
```
⚠️ Forced replacement — `network_interface_ids` reads `azurerm_network_interface.web.id`, which is **changing in this plan**.
```

**Check:**
- The phrase "**changing in this plan**" is **bold** in the rendered output
- The attribute names are in backtick code formatting
- The line appears above the diff table, not below it

**Before/After context:** Previously this causal link would only appear in a separate `## Relevant Attributes` table at the bottom of the report — far from the replaced resource. Now it appears directly on the card.

---

#### 2. Verify combined card (forced + also depends on)

**Resource to find:** `azurerm_app_service.api` card  

**Expected lines:**
```
⚠️ Forced replacement — `app_settings` reads `azurerm_key_vault.main.vault_uri`, which is **changing in this plan**.
🔗 Also depends on: `data.azurerm_client_config.current.tenant_id`
```

**Check:**
- **Two separate blockquote lines** appear
- The second line uses "**Also depends on:**" label (not just "Depends on:")
- Both lines appear above the diff table

---

#### 3. Verify depends-on only line

**Resource to find:** `azurerm_storage_account.logs` card  

**Expected line:**
```
🔗 Depends on: `data.azurerm_subscription.current.subscription_id`
```

**Check:**
- Line uses "**Depends on:**" label (NOT "Also depends on:")
- No `⚠️ Forced replacement` line on this resource
- Line appears above the diff table

---

#### 4. Verify in-place update has no annotations

**Resource to find:** `azurerm_resource_group.main` card (or whichever update resource is in the plan)  

**Check:**
- NO `⚠️ Forced replacement` line
- NO `🔗 Depends on` line
- Card content unchanged from previous behaviour

---

#### 5. Verify fallback section at end of report

**Location:** End of report, after all resource cards  

**Expected section:**
```html
<details>
<summary>🔗 Other plan inputs (N) — read by this plan but not tied to a specific change</summary>

> These existing values were read to compute the plan. If they change before apply, the plan may be stale.

- `resource.attribute_path`
...

</details>
```

**Check:**
- The `<details>` block is collapsible (click to expand in the rendered view)
- The `(N)` count in the summary matches the number of items listed
- No `## Relevant Attributes` heading appears anywhere in the report

---

#### 6. Verify ## Relevant Attributes H2 table is gone

**Check:** Search the entire rendered report for "## Relevant Attributes". It must NOT appear.

---

### Regression Validation

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

**Verify:**
- All existing resource cards render correctly with no unexpected lines
- Summary table counts are correct
- No extra blank lines or spacing issues introduced
- Drift section (if present) renders correctly without inline annotations
- No `## Relevant Attributes` H2 table (the comprehensive demo includes relevant_attributes from feature 122 UAT)
- If the comprehensive demo plan has no `relevant_attributes`, no fallback section appears

---

## Success Criteria

- [ ] ⚠️ Forced replacement callout renders correctly in GitHub Markdown
- [ ] ⚠️ Forced replacement callout renders correctly in Azure DevOps Markdown
- [ ] "changing in this plan" bold phrase renders bold in both platforms
- [ ] 🔗 Depends on line renders correctly in both platforms
- [ ] 🔗 Also depends on line renders correctly in both platforms
- [ ] Combined card (both lines) renders correctly in both platforms
- [ ] Fallback `<details>` section is collapsible in both platforms
- [ ] In-place update resource shows no annotations
- [ ] `## Relevant Attributes` H2 table is absent from all reports
- [ ] No regressions in the comprehensive demo output
