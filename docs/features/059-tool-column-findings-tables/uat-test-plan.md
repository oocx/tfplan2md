# UAT Test Plan: Tool Column in Findings Tables

## Goal

Verify that the Tool column in code analysis findings tables renders correctly in GitHub and Azure DevOps PR comments, displaying tool names clearly and maintaining professional table layout.

## Artifacts

### Feature-Specific Test Artifact (Required)

**Purpose:** Focus testing on the Tool column addition in findings tables.

**Artifact Path:** `artifacts/tool-column-uat.md`

**Creation Instructions:**
- **Source Plan:** `examples/comprehensive-demo.tfplan.json` (already contains code analysis)
- **Source SARIF:** Use existing SARIF files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/code-analysis/`
- **Command:** 
  ```bash
  tfplan2md \
    examples/comprehensive-demo.tfplan.json \
    --sarif src/tests/Oocx.TfPlan2Md.TUnit/TestData/code-analysis/checkov.sarif \
    --sarif src/tests/Oocx.TfPlan2Md.TUnit/TestData/code-analysis/tfsec.sarif \
    --sarif src/tests/Oocx.TfPlan2Md.TUnit/TestData/code-analysis/trivy.sarif \
    > artifacts/tool-column-uat.md
  ```
- **Rationale:** Using multiple SARIF files from different tools (Checkov, tfsec, Trivy) demonstrates the Tool column's value in distinguishing between scanners. The comprehensive demo plan provides sufficient resources for findings to map to.
- **Key Resources:** 
  - Security & Quality findings table (per-resource) - shows Tool column between Severity and Attribute
  - Module findings table in "Other Findings" section - shows Tool column between Severity and Finding
  - Unmatched findings table in "Other Findings" section - shows Tool column between Severity and Finding

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in other areas of the report.

**Artifact Paths:**
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Creation Instructions:**
- Use the `generate-demo-artifacts` skill to generate these artifacts
- These are standard regression test artifacts maintained by the project

**Note:** This artifact is generated automatically by the Developer using `generate-demo-artifacts` skill.

## Test Steps

1. Run UAT using the `uat-tester-coding-agent` agent or manually follow steps below.
2. UAT will post TWO separate PR comments:
   - **Feature-Specific Report**: Tests the Tool column addition
   - **Comprehensive Demo**: Regression test for side effects
3. Verify both reports on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

### Feature-Specific Validation

In the **feature-specific report** (first comment, labeled "🎯 Feature Test: Tool Column in Findings Tables"):

**Specific Tables/Sections:**

1. **Security & Quality Findings Table** (per-resource findings)
2. **Module Findings Table** (in "Other Findings" section)
3. **Unmatched Findings Table** (in "Other Findings" section)

**Exact Column Structure to Verify:**

**For Security & Quality Findings:**
- Column headers: `| Severity | Tool | Attribute | Finding | Remediation |`
- Separator line: `| -------- | ---- | --------- | ------- | ----------- |`
- Data rows show tool name (e.g., "Checkov", "tfsec", "Trivy") in the Tool column
- Tool column appears BETWEEN Severity and Attribute columns

**For Module and Unmatched Findings:**
- Column headers: `| Severity | Tool | Finding | Remediation |`
- Separator line: `| -------- | ---- | ------- | ----------- |`
- Data rows show tool name in the Tool column
- Tool column appears BETWEEN Severity and Finding columns

**Expected Outcome:**

- **Tool Names Visible:** Tool names (e.g., "Checkov", "tfsec", "Trivy") are clearly displayed in the Tool column for all findings that have tool information.
- **Fallback for Missing Tool Names:** If any finding lacks a tool name, the Tool column shows "-" (hyphen/dash) instead of being empty.
- **Column Position:** Tool column is positioned immediately after the Severity column and before Attribute/Finding column.
- **Table Alignment:** All columns align vertically in both GitHub and Azure DevOps rendering.
- **Professional Appearance:** Tables maintain a clean, readable layout without overflow or wrapping issues.
- **Multiple Tools Distinguished:** When viewing findings from multiple tools (Checkov, tfsec, Trivy), users can immediately identify which tool produced each finding.

**Before/After Context:**

- **Before:** Findings tables did not show which tool produced each finding. When using multiple security scanners, users had to guess or refer to external documentation to determine the source of findings.
- **After:** Each finding clearly displays its source tool in a dedicated Tool column, making it easy to assess credibility and take appropriate action based on the tool's focus area (e.g., Checkov for cloud security, tfsec for Terraform-specific checks, Trivy for vulnerabilities).

**Key Visual Checks:**

1. **Column Headers:** Verify "Tool" appears in the header between "Severity" and "Attribute/Finding".
2. **Data Rows:** Verify tool names appear in the correct column (second column after Severity).
3. **Consistency:** Verify ALL three table types (Security & Quality, Module, Unmatched) include the Tool column.
4. **Alignment:** Verify columns line up properly in the rendered view (not just in raw markdown).
5. **No Empty Cells:** Verify that findings without tool names show "-" rather than blank cells.

---

### Regression Validation

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test: Comprehensive Demo"):

**Verify:**

1. **No Unintended Changes:** All sections outside of findings tables remain unchanged:
   - Plan Summary statistics
   - Resource change details
   - Code Analysis Summary (tool listing and counts)
   - Import/Moved Blocks section
   - Footer

2. **Findings Tables Updated Consistently:** All findings tables in the comprehensive demo show the Tool column:
   - Security & Quality findings tables for each resource
   - Module-level findings tables
   - Unmatched findings table

3. **Code Analysis Summary Unaffected:** The "Code Analysis Summary" section at the top (showing tool names and versions) should render identically to before:
   - "**Tools Used:** Checkov 3.2.10, tfsec 1.28.4, Trivy 0.51.1"
   - Severity count tables unchanged
   - Resource type breakdown unchanged

4. **Findings Content Unchanged:** The actual finding messages, severity levels, remediation links, and attribute paths should be identical to previous versions (only the Tool column is new).

5. **Table Rendering Quality:** All tables render cleanly in both GitHub and Azure DevOps without visual artifacts or layout issues.

**Success Criteria:**

- ✅ Tool column appears in all three types of findings tables
- ✅ Tool names display correctly (Checkov, tfsec, Trivy, etc.)
- ✅ Column alignment is clean and professional in both GitHub and Azure DevOps
- ✅ Null/empty tool names show "-" (if any test data includes this)
- ✅ No layout breaking or overflow issues with tool names
- ✅ Code Analysis Summary section unchanged
- ✅ All other report sections render identically to before

---

## Manual Testing Steps (if not using UAT Tester agent)

If you're manually executing this UAT:

### Step 1: Generate Artifacts

```bash
# Generate feature-specific artifact
tfplan2md \
  examples/comprehensive-demo.tfplan.json \
  --sarif src/tests/Oocx.TfPlan2Md.TUnit/TestData/code-analysis/checkov.sarif \
  --sarif src/tests/Oocx.TfPlan2Md.TUnit/TestData/code-analysis/tfsec.sarif \
  --sarif src/tests/Oocx.TfPlan2Md.TUnit/TestData/code-analysis/trivy.sarif \
  > artifacts/tool-column-uat.md

# Generate comprehensive demo artifacts (using skill)
# Run: generate-demo-artifacts skill
```

### Step 2: Create Test PRs

**GitHub:**
1. Create a draft PR from `feature/059-tool-column-findings-tables` to `main`
2. Post PR comment with feature-specific artifact content from `artifacts/tool-column-uat.md`
3. Post PR comment with comprehensive demo content from `artifacts/comprehensive-demo-simple-diff.md`
4. Review rendering in GitHub's markdown viewer

**Azure DevOps:**
1. Create a draft PR in the Azure DevOps mirror repository
2. Add PR description with feature-specific artifact content
3. Add PR description with comprehensive demo content
4. Review rendering in Azure DevOps markdown viewer

### Step 3: Visual Inspection

For each PR (GitHub and Azure DevOps):

1. **Locate Findings Tables:**
   - Scroll to any resource with Security & Quality findings
   - Scroll to "Other Findings" section (if present)

2. **Verify Column Structure:**
   - Check header row includes "Tool" column
   - Check separator line has correct number of dashes
   - Check data rows show tool names in the Tool column

3. **Check Alignment:**
   - Verify columns align vertically
   - Verify no visual artifacts or rendering glitches
   - Verify table maintains professional appearance

4. **Compare Before/After:**
   - (Optional) Compare with a report from main branch to see the difference

### Step 4: Provide Feedback

- Comment on the PR with approval or requested changes
- Note any rendering issues specific to GitHub or Azure DevOps
- Approve if all validation criteria are met

---

## Expected Test Outcomes

### Passing Criteria

- ✅ All findings tables include the Tool column
- ✅ Tool names are displayed correctly
- ✅ Column alignment is professional and readable
- ✅ No markdown rendering errors
- ✅ Null/empty tool names handled gracefully (show "-")
- ✅ No regressions in other report sections

### Failing Criteria (Request Changes)

- ❌ Tool column missing from any table type
- ❌ Tool names not displaying or showing incorrect values
- ❌ Column alignment issues (misaligned or overlapping)
- ❌ Markdown rendering errors (broken tables, missing content)
- ❌ Empty cells instead of "-" for missing tool names
- ❌ Regressions in Code Analysis Summary or other sections
- ❌ Horizontal scrolling required (table too wide)
- ❌ Unreadable text due to poor wrapping

---

## Edge Cases to Test

While reviewing the UAT artifacts, specifically look for these edge cases:

| Edge Case | Where to Check | Expected Behavior |
|-----------|----------------|-------------------|
| Missing tool name | Any finding with ToolName = null | Shows "-" in Tool column |
| Multiple different tools | Compare rows across tables | Each row shows its correct tool |
| Long tool names | If any tool name > 20 chars | Name displays in full, wraps if needed |
| Special characters in tool name | Tool names with hyphens, dots, etc. | Characters display correctly |

---

## Feedback Template

Use this template when providing feedback on the UAT:

```markdown
## UAT Feedback: Tool Column in Findings Tables

### GitHub Rendering
- [ ] Feature-specific artifact renders correctly
- [ ] Comprehensive demo renders correctly
- [ ] Tool column visible in all tables
- [ ] Column alignment acceptable
- [ ] No visual artifacts or layout issues

**Issues Found:**
- (List any issues or "None")

### Azure DevOps Rendering
- [ ] Feature-specific artifact renders correctly
- [ ] Comprehensive demo renders correctly
- [ ] Tool column visible in all tables
- [ ] Column alignment acceptable
- [ ] No visual artifacts or layout issues

**Issues Found:**
- (List any issues or "None")

### Overall Assessment
- [ ] Approve - Ready to merge
- [ ] Request Changes - Issues found

**Additional Comments:**
(Any additional feedback)
```

---

## Notes for UAT Tester Agent

When executing this UAT plan:

1. **Generate Both Artifacts:**
   - Feature-specific: Use the tfplan2md command above with multiple SARIF files
   - Comprehensive demo: Use `generate-demo-artifacts` skill

2. **Create PRs in Both Platforms:**
   - GitHub: Use standard PR creation
   - Azure DevOps: May require special handling (check project docs)

3. **Post Separate Comments:**
   - First comment: Feature-specific test (label it clearly)
   - Second comment: Comprehensive demo regression test

4. **Validation Focus:**
   - PRIMARY: Tool column appears and displays correctly
   - SECONDARY: No regressions in other sections
   - TERTIARY: Professional appearance and readability

5. **Report Findings:**
   - Use the feedback template above
   - Include screenshots if issues found
   - Tag Maintainer for review

---

## Acceptance

This UAT is considered **PASSED** when:

- ✅ Maintainer reviews both test artifacts in GitHub and Azure DevOps
- ✅ Maintainer confirms Tool column renders correctly
- ✅ Maintainer confirms no regressions in comprehensive demo
- ✅ Maintainer approves via PR comment or review approval

This UAT is considered **FAILED** if:

- ❌ Any findings table is missing the Tool column
- ❌ Tool names are not displaying correctly
- ❌ Column alignment is poor or unprofessional
- ❌ Any regressions in existing functionality
- ❌ Markdown rendering errors occur

In case of failure, the Developer agent should address issues and re-run UAT.
