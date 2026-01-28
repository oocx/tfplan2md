# UAT Test Plan: Azure DevOps Variable Group Template

## Goal

Verify that Azure DevOps variable group changes render correctly and clearly in GitHub and Azure DevOps PR comments, ensuring users can effectively review variable modifications in Terraform plans.

## Artifacts

**Artifact to use:** `examples/azuredevops/terraform_plan2.json` (existing)

**Creation Instructions:**

The existing example already contains an `azuredevops_variable_group.example` resource with a replace (delete+create) action. This is sufficient for UAT as it exercises the core rendering scenarios.

To generate the markdown:

```bash
# From repository root
dotnet run --project src/Oocx.TfPlan2Md -- examples/azuredevops/terraform_plan2.json > artifacts/uat-variable-group.md
```

**Rationale:** The existing Azure DevOps example plan contains a real variable group with both regular and secret variables, providing authentic test data for visual validation of the new template.

## Test Steps

1. Run UAT using the `UAT Tester` agent or manual scripts.
2. Create temporary PRs on GitHub and Azure DevOps test repositories.
3. Post the generated markdown as PR comments.
4. Verify the rendering on both platforms.

## Validation Instructions (Test Description)

### What to Verify

Review the rendering of the `azuredevops_variable_group.example` resource in both GitHub and Azure DevOps PR comments.

### Specific Sections to Check

**1. Summary Line**

Look for the summary line at the top of the collapsible section:

```
♻️ azuredevops_variable_group example — example-variables | X 🔧 variables
```

**Expected:**
- Replace icon (♻️) appears correctly
- Resource name `example` is **bold and code-formatted**
- Variable group name `example-variables` is code-formatted
- Change count displays (e.g., "3 🔧 variables")
- Non-breaking space between icon and resource type (no line wrapping)

**2. Variable Group Metadata**

Look for these lines after the heading:

```markdown
**Variable Group:** `example-variables`

**Description:** `Variable group for example pipeline`
```

**Expected:**
- Variable group name is code-formatted
- Description is code-formatted
- Labels ("Variable Group:", "Description:") are bold plain text
- Clean, readable layout

**3. Variables Table**

Look for the main variables table with these columns:

```
| Change | Name | Value | Enabled | Content Type | Expires |
```

**Expected:**
- Table header is plain text (no code formatting)
- Table renders correctly (no broken rows, proper alignment)
- All variables from the plan appear in the table

### Specific Variables to Verify

**Regular Variables:**

For variables like `APP_VERSION`:

```
| 🔄 | `APP_VERSION` | - `1.0.0`<br>+ `1.0.0` | - `false`<br>+ `false` | - | - |
```

**Expected:**
- Change indicator (🔄, ➕, ❌, ⏺️) displays correctly
- Variable name is code-formatted
- Value column shows before/after diff with `-` and `+` prefixes
- Each before/after on separate line (uses `<br>`)
- Empty attributes show as `-` (plain text, not code-formatted)

**Secret Variables:**

For secret variables like `SECRET_KEY`:

```
| 🔄 | `SECRET_KEY` | `(sensitive / hidden)` | - `true`<br>+ `false` | - | - |
```

**Expected:**
- Value column displays `(sensitive / hidden)` (code-formatted)
- Metadata (enabled, content_type, expires) is **visible** and shows diff
- No actual secret value is exposed

### Key Vault Section (if present)

If the plan includes Key Vault integration, look for:

```markdown
#### Key Vault Integration

| Name | Service Endpoint ID | Search Depth |
| ---- | ------------------- | ------------ |
| `my-keyvault` | `a1b2c3d4-...` | `1` |
```

**Expected:**
- Section appears BEFORE Variables section
- All values are code-formatted
- Table renders correctly

### Large Values Section (if present)

If any variables exceed 100 characters or are multi-line, look for:

```markdown
<details>
<summary>Large values: VAR_NAME (X lines, Y changed)</summary>

### `VAR_NAME`

**Before:**
```
value content
```

**After:**
```
value content
```

</details>
```

**Expected:**
- Large values removed from main table
- Separate collapsible section exists
- Before/After comparison is clear
- Values displayed in code blocks

## Platform-Specific Checks

### GitHub

**Verify:**
- [ ] Collapsible `<details>` sections work (can expand/collapse)
- [ ] Tables render with proper alignment
- [ ] Code formatting (backticks) displays correctly
- [ ] HTML `<br>` tags create line breaks in table cells
- [ ] Non-breaking spaces prevent icon wrapping
- [ ] Emoji icons display consistently

### Azure DevOps

**Verify:**
- [ ] HTML `<code>` tags in summary line render correctly
- [ ] Tables render without horizontal scrolling
- [ ] Collapsible sections work
- [ ] Before/after diffs with `<br>` display on separate lines
- [ ] No markdown escaping issues
- [ ] Overall layout is readable

## Success Criteria

- [ ] Summary line is clear and informative
- [ ] Regular variables show actual values with proper diff formatting
- [ ] Secret variables show "(sensitive / hidden)" for values
- [ ] Secret variable metadata is visible (enabled, content_type, expires)
- [ ] Change indicators (➕, 🔄, ❌, ⏺️) are visually distinct
- [ ] Tables are properly formatted and aligned
- [ ] Before/after diffs are easy to read
- [ ] Empty/null values display as `-`
- [ ] No markdown rendering errors
- [ ] Both platforms render consistently

## Before/After Context

**Before (default template):**

Users see completely opaque output:

```markdown
### 🔄 azuredevops_variable_group.example

- variable {
    # At least one attribute in this block is (or was) sensitive,
    # so its contents will not be displayed.
  }
```

**Problem:** Impossible to review what's changing in the variable group.

**After (variable group template):**

Users see clear, semantic output:

```markdown
### ♻️ azuredevops_variable_group.example

**Variable Group:** `example-variables`

#### Variables

| Change | Name | Value | Enabled | Content Type | Expires |
| ------ | ---- | ----- | ------- | ------------ | ------- |
| 🔄 | `APP_VERSION` | - `1.0.0`<br>+ `2.0.0` | - `false`<br>+ `true` | - | - |
| 🔄 | `SECRET_KEY` | `(sensitive / hidden)` | - `true`<br>+ `false` | - | - |
| ➕ | `NEW_VAR` | `production` | - | - | - |
```

**Improvement:** All variable changes are visible with clear diff formatting, while secret values remain protected.

## Common Issues to Watch For

### Rendering Problems

1. **Broken tables**: Look for misaligned columns or rows
2. **Escaped characters**: `\|`, `\*`, `\_` appearing instead of actual characters
3. **Missing line breaks**: Before/after values on same line instead of separate lines
4. **Icon wrapping**: Action icons wrapping to a separate line from resource type

### Formatting Problems

1. **Inconsistent code formatting**: Some values code-formatted, others not
2. **Wrong label formatting**: Labels in code format instead of plain text
3. **Empty values as code**: `-` appearing as `` `-` `` instead of plain `-`
4. **HTML tag visibility**: `<br>` or `<code>` appearing as text instead of rendering

### Content Problems

1. **Secret values exposed**: Actual secret values visible instead of "(sensitive / hidden)"
2. **Missing variables**: Some variables not displayed
3. **Wrong change indicators**: Added variables marked as modified, etc.
4. **Metadata hidden for secrets**: Secret variable metadata (enabled, content_type) not displayed

## Feedback Opportunities

During UAT review, consider:

1. **Clarity**: Is it immediately clear which variables are being added, modified, or removed?
2. **Readability**: Is the table format easy to scan quickly?
3. **Security**: Are you confident that secret values are properly masked?
4. **Completeness**: Does the output provide enough information to make an informed decision about applying the plan?
5. **Consistency**: Does the format match other resource-specific templates (e.g., role assignments, firewall rules)?

## Notes for UAT Tester Agent

When running UAT:

1. **Use existing example**: No need to create new test data, `terraform_plan2.json` is sufficient
2. **Post as comment**: Ensure markdown is posted as PR **comment**, not description
3. **Poll for feedback**: Check for Maintainer comments every 30 seconds
4. **Iterate on feedback**: If Maintainer requests changes, apply them and post updated comment
5. **Cleanup on approval**: Close/abandon PRs and delete branches after explicit approval

## UAT Execution Commands

```bash
# Generate markdown artifact
dotnet run --project src/Oocx.TfPlan2Md -- examples/azuredevops/terraform_plan2.json > artifacts/uat-variable-group.md

# Create UAT PRs and post comments
scripts/uat-github.sh create artifacts/uat-variable-group.md
scripts/uat-azdo.sh create artifacts/uat-variable-group.md

# Poll for approval (autonomous loop)
while true; do
    scripts/uat-github.sh poll "$GH_PR_NUMBER" && GH_APPROVED=true
    scripts/uat-azdo.sh poll "$AZDO_PR_ID" && AZDO_APPROVED=true
    
    [[ "$GH_APPROVED" == "true" && "$AZDO_APPROVED" == "true" ]] && break
    sleep 30
done

# Cleanup after approval
scripts/uat-github.sh cleanup "$GH_PR_NUMBER"
scripts/uat-azdo.sh cleanup "$AZDO_PR_ID"
```
