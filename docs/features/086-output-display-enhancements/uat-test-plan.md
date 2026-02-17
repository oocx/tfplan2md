# UAT Test Plan: Output Display Enhancements

## Goal
Verify that the collapsible debug section and no-changes summary format render correctly in GitHub and Azure DevOps PR comments.

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)
**Purpose:** Focus testing on the specific changes in this feature. This artifact MUST be real tfplan2md output, not synthetic or simulated.

**Source Plan Path:** `docs/features/086-output-display-enhancements/uat-plan.json`

**Rendered Output Path:** `docs/features/086-output-display-enhancements/uat-plan.md`

**Plan Requirements:**
- **MUST be a real Terraform plan JSON** that exercises both feature enhancements
- **MUST include a no-changes scenario** (module with zero resource changes)
- **MUST include debug output** (with principal mapping and template resolutions)
- **Rationale:** This plan demonstrates both collapsible debug section and no-changes summary rendering in a realistic scenario
- **Key Resources:** 
  1. **No-changes module**: A Terraform module with only no-op resources (or no resources) to demonstrate "No changes" summary
  2. **Debug section**: Principal mapping and template resolution diagnostics
  3. **Changes module** (optional): A module with actual changes to show that existing functionality is unaffected
- **Coverage:** 
  - Collapsible debug section structure (`<details>` tag, collapsed by default)
  - Debug section summary with bug emoji and non-breaking space
  - "No changes" text in Summary section (not empty table)
  - Absence of Resource Changes section for no-changes modules
  - Full summary table for modules with changes (if included)

**Example Creation Command:**
```bash
# Create a test plan with no changes (or include a no-changes module)
# Generate with --debug flag to produce debug output
tfplan2md docs/features/086-output-display-enhancements/uat-plan.json \
  --debug \
  --principal-mapping path/to/principals.json > \
  docs/features/086-output-display-enhancements/uat-plan.md
```

### Comprehensive Demo (Regression Test)
**Purpose:** Ensure no unintended side effects in other areas.

**Artifact Path:** 
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using `generate-demo-artifacts` skill.

## Test Steps
1. Developer creates `uat-plan.json` based on this specification
2. Developer generates `uat-plan.md` from the plan using `--debug` flag
3. Code Reviewer validates both files exist and are complete
4. UAT Tester uses `uat-plan.md` for testing
5. UAT will post TWO separate PR comments:
   - **Feature-Specific Report**: Tests the specific changes using `uat-plan.md`
   - **Comprehensive Demo**: Regression test for side effects
6. Verify both reports on GitHub and Azure DevOps

## Validation Instructions

### Feature-Specific Validation

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"):

#### Specific Sections:

**1. Debug Information Section**

**Location:** End of the document (after all main report sections)

**Expected Structure:**
- Starts with a collapsed `<details>` tag (NOT expanded by default)
- Summary line: `🐛 Debug Information` (clickable)
- When clicked/expanded, should reveal:
  - `### Principal Mapping` subsection
  - Principal mapping file status ("Loaded successfully from..." or failure message)
  - Principal type counts (e.g., "5 users, 3 groups")
  - `### Template Resolution` subsection
  - List of resource types and their template assignments
  - Any failed resolutions (if applicable)

**What to Verify:**
- [ ] Debug section is **collapsed by default** (not expanded when page loads)
- [ ] Clicking on "🐛 Debug Information" expands the section smoothly
- [ ] All debug content is properly formatted and readable when expanded
- [ ] Non-breaking space renders correctly (emoji and text don't wrap separately)
- [ ] Subsection headings (`###`) render correctly inside the details block
- [ ] No `## Debug Information` H2 heading visible (replaced by summary)

**GitHub-Specific:**
- [ ] Collapsible section works (click to expand/collapse)
- [ ] All content renders correctly when expanded

**Azure DevOps-Specific:**
- [ ] Collapsible section works (click to expand/collapse)
- [ ] All content renders correctly when expanded
- [ ] HTML `<code>` tags in summary line render correctly

---

**2. Summary Section (No Changes Module)**

**Location:** Near the top of the document, under `## Summary`

**Expected Content:**
- Text: `No changes` (plain text, simple message)
- **NOT** a summary table with columns and zero counts
- **NOT** rows for ➕ Add, 🔄 Change, ♻️ Replace, ❌ Destroy

**What to Verify:**
- [ ] "No changes" text is displayed clearly
- [ ] No empty summary table with zero counts
- [ ] Professional, clean appearance

**Before/After Context:**
- **Before:** Would show a table like:
  ```
  | Action | Count | Resource Types |
  | -------- | ------- | ---------------- |
  | ➕ Add | 0 | |
  | 🔄 Change | 0 | |
  | ♻️ Replace | 0 | |
  | ❌ Destroy | 0 | |
  | **Total** | **0** | |
  ```
- **After:** Shows: `No changes`

---

**3. Resource Changes Section Omission**

**Location:** Should NOT appear after Summary section for no-changes module

**Expected Behavior:**
- When summary shows "No changes", there should be **NO** separate `## Resource Changes` section
- The document should proceed directly to Code Analysis Summary (if present) or Debug Information

**What to Verify:**
- [ ] No `## Resource Changes` heading for the no-changes module
- [ ] No redundant "No changes" message in a separate section
- [ ] Cleaner report structure (one "No changes" message, not two)

**Before/After Context:**
- **Before:** Would show both:
  ```markdown
  ## Summary
  | Action | Count | ...
  | Total | 0 | |
  
  ## Resource Changes
  No changes
  ```
- **After:** Only shows:
  ```markdown
  ## Summary
  No changes
  ```

---

**4. Plans With Changes (If Included)**

**Location:** If the UAT plan includes a module with actual changes

**Expected Behavior:**
- Summary section shows **full summary table** with action counts
- Resource Changes section **IS present** with module and resource details
- No regression in existing functionality

**What to Verify:**
- [ ] Summary table renders normally with non-zero counts
- [ ] Resource Changes section appears with resource details
- [ ] All existing formatting (icons, emojis, code formatting) is preserved
- [ ] Collapsible resource details work correctly

---

### Regression Validation

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

**Verify:**
- [ ] Debug section (if present) is collapsed by default
- [ ] No unintended changes to existing resource rendering
- [ ] Summary tables with changes render correctly
- [ ] Resource Changes sections with actual changes render correctly
- [ ] All collapsible resource sections work properly
- [ ] No formatting breakage in any section
- [ ] Report style guide conventions maintained throughout

**Key Areas to Check:**
- Summary section formatting (for plans with changes)
- Resource Changes section formatting
- Collapsible resource details (`<details>` blocks for individual resources)
- Code formatting (backticks, HTML `<code>` tags)
- Icon rendering (emojis, action icons)
- Non-breaking spaces between icons and labels

---

## Platform-Specific Checks

### GitHub Markdown Rendering

**Verify:**
- [ ] `<details>` tags render as collapsible sections
- [ ] Summary tag is clickable to expand/collapse
- [ ] `<br>` tag creates proper spacing after summary
- [ ] Debug section content is hidden by default
- [ ] "No changes" text is clearly visible in Summary section
- [ ] No markdown syntax errors or rendering issues

### Azure DevOps Markdown Rendering

**Verify:**
- [ ] `<details>` tags render as collapsible sections (ADO supports this)
- [ ] Summary tag is clickable to expand/collapse
- [ ] `<br>` tag creates proper spacing after summary
- [ ] Debug section content is hidden by default
- [ ] "No changes" text is clearly visible in Summary section
- [ ] HTML `<code>` tags in summary lines render correctly (ADO quirk)
- [ ] No markdown syntax errors or rendering issues

---

## Success Criteria

The feature is approved when:
- [ ] Debug section renders as a collapsed `<details>` block in both platforms
- [ ] Debug section can be expanded to reveal all content
- [ ] "No changes" text appears in Summary instead of empty table
- [ ] Resource Changes section is omitted for no-changes plans
- [ ] Plans with changes continue to render normally (no regression)
- [ ] Both GitHub and Azure DevOps render the output correctly
- [ ] No visual or functional issues reported by Maintainer
- [ ] Output meets professional quality standards

## Feedback Opportunities

During UAT review, consider:
- Is the collapsed debug section discoverable enough? (Bug emoji and label clear?)
- Is "No changes" message prominent enough in the Summary section?
- Does the absence of Resource Changes section improve report clarity?
- Are there any edge cases not covered by the test artifacts?
- Does the feature work well in combination with other features (code analysis, principal mapping)?

## Notes for UAT Tester Agent

When executing this UAT:
1. Create TWO separate PR comments (feature-specific and comprehensive demo)
2. Use the exact validation instructions above as the PR description
3. Post to BOTH GitHub and Azure DevOps test repositories
4. Verify collapsibility by checking HTML structure, not just visual inspection
5. Check for presence/absence of specific headings and sections
6. Validate that the `<details>` tag does NOT have `open` attribute (collapsed by default)
7. Report any rendering differences between GitHub and Azure DevOps
