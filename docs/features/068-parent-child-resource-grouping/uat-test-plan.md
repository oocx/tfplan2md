# UAT Test Plan: Parent-Child Resource Grouping

## Goal

Verify that parent-child resources (like Azure AD Groups or Azure DevOps Teams) render correctly in GitHub and Azure DevOps PR comments, with children displayed in inline tables rather than separate sections. This includes verification of configuration reference matching for `(known after apply)` scenarios.

## Artifacts

### Primary Artifact

**Artifact to use:** `artifacts/comprehensive-demo.md`

**Creation Instructions:**
- **Source Plan:** `examples/comprehensive-demo/plan.json`
- **Command:** `tfplan2md --plan examples/comprehensive-demo/plan.json --output artifacts/comprehensive-demo.md --changed-attributes-summary`
- **Rationale:** Contains real-world Azure AD group with members where parent ID is `(known after apply)`. Exercises configuration reference matching in production-like scenario.

### Supplementary Artifacts (if needed)

**For focused testing of specific scenarios:**

1. **`artifacts/parent-child-value-matching-demo.md`**
   - Plan with parent ID already known (update scenario)
   - Exercises value-based matching (non-fallback path)

2. **`artifacts/parent-child-mixed-demo.md`**
   - Plan with both inline and separate members
   - Exercises mixed management warning

3. **`artifacts/parent-child-nested-modules-demo.md`**
   - Plan with parent/child in nested modules
   - Exercises module-qualified reference matching

## Test Steps
1. Run UAT using the `UAT Tester` agent with the primary artifact (`artifacts/comprehensive-demo.md`).
2. Verify the generated PRs on GitHub and Azure DevOps.
3. Complete all validation checklist items below.

## Validation Instructions

### Critical: Configuration Reference Matching (Known After Apply)

**Resource:** `azuread_group.platform_engineers`

**Context:** This is a CREATE action, so the `id` and `object_id` are `(known after apply)`. The plan should have separate `azuread_group_member` resources that reference this group via Terraform expressions (e.g., `azuread_group.platform_engineers.id`).

**What to Verify:**

1. **Single Section:** There should be ONLY ONE section for `azuread_group.platform_engineers`. Check that NO separate sections exist for `azuread_group_member.platform_*` resources.

2. **Members Table:** Within the `azuread_group.platform_engineers` section, verify a "Members" table exists.

3. **Table Contents:**
   - **Inline members** (from `members` attribute): Should show `members attribute` in the "Terraform Resource" column.
   - **Separate members** (from `azuread_group_member` resources): Should show their full Terraform address (e.g., `azuread_group_member.platform_admin_member`) in the "Terraform Resource" column.

4. **Change Indicators:** Each row should have appropriate change indicators (➕ for additions, ❌ for removals).

5. **Member Formatting:** Member object IDs should be formatted with person icons and readable names (if principal mapper data is available).

**Expected Outcome:** The separate `azuread_group_member` resources are correctly merged into the parent group's section via configuration reference matching, even though the parent's ID is unknown.

**Before/After Context:** Without configuration reference matching, these separate members would appear as standalone sections, causing excessive scrolling. With proper implementation, they merge cleanly into the parent table.

---

### Value-Based Matching (Update Scenario)

**Resource:** Any group with known ID (UPDATE action)

**What to Verify:**

1. Find a parent resource with a known ID (not `(known after apply)`).
2. Verify that separate child resources referencing this ID are correctly merged.
3. This exercises the non-fallback matching path (value comparison).

---

### Mixed Management Warning

**What to Verify:**

1. Find a parent resource that has BOTH:
   - Inline children (from an attribute like `members`)
   - Separate children (from child resources like `azuread_group_member`)

2. **Warning Message:** The section should display:
   > ⚠️ **Warning:** This resource has children managed both inline and as separate resources. This configuration will cause conflicts.

3. **Table Rows:** All children (both inline and separate) should be in the same table.

**Expected Outcome:** Users are warned about the problematic configuration but can see all members in one place.

---

### Azure DevOps Team Multiple Tables

**Resource:** `azuredevops_team.platform_team` (if present)

**What to Verify:**

1. Should show TWO separate tables:
   - "Administrators" table
   - "Members" table

2. Each table should have appropriate rows for administrators and members.

3. Values should be readable (member names or descriptors), not raw JSON.

---

### Change Summary

**What to Verify:**

1. Look at parent resource headers (e.g., `➕ azuread_group.platform_engineers`).

2. The summary should include child counts:
   - Example: `➕ azuread_group.platform_engineers | ➕ 4 members`

3. Counts should aggregate all child changes (inline + separate).

**Expected Outcome:** Summary line clearly shows how many children are being added/changed/removed.

---

### Code Analysis Findings

**What to Verify:**

1. If any code analysis findings are mapped to a child resource address (e.g., `azuread_group_member.platform_admin_member`), they should appear within the PARENT resource section.

2. The finding should clearly indicate the original child resource address it applies to.

**Expected Outcome:** Findings are not lost when child resources are merged inline.

---

### Cross-Platform Layout

**GitHub:**
- Tables have proper markdown headers (`| Header |`)
- Change indicators display correctly (➕, 🔄, ❌)
- Resource addresses are formatted as monospace code
- Warning messages display with emoji

**Azure DevOps:**
- Tables render cleanly (no broken markdown)
- Change indicators display correctly
- No layout issues or overflow
- Warning messages are visible

**Expected Outcome:** Both platforms render the parent-child tables consistently and readably.

---

## Success Criteria

- [ ] All parent resources have children merged into inline tables (no standalone child sections)
- [ ] Configuration reference matching works for `(known after apply)` scenarios
- [ ] Value-based matching works for known ID scenarios
- [ ] Mixed management warnings display correctly
- [ ] Change summaries include child counts
- [ ] Code analysis findings are preserved
- [ ] Rendering is clean and consistent on both GitHub and Azure DevOps

## Feedback Opportunities

- Does the table format make it easier to understand parent-child relationships?
- Are the resource addresses clear (inline vs. separate)?
- Is the mixed management warning helpful?
- Are there any rendering issues or layout problems?
- Does the change summary provide enough detail?
