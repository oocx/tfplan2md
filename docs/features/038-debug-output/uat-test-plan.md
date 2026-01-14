# UAT Test Plan: Debug Output

## Goal

Verify that debug output renders correctly in GitHub and Azure DevOps PR comments when the `--debug` flag is used. This includes validating the display of principal mapping diagnostics and template resolution information.

## Artifacts

**Artifact to use:** `artifacts/debug-output-demo.md`

**Creation Instructions:**

### Prerequisites
1. Create test principal mapping file: `test-data/principals-demo.json`
   ```json
   {
     "users": [
       {
         "id": "11111111-1111-1111-1111-111111111111",
         "displayName": "Alice Developer"
       },
       {
         "id": "22222222-2222-2222-2222-222222222222",
         "displayName": "Bob Administrator"
       }
     ],
     "groups": [
       {
         "id": "33333333-3333-3333-3333-333333333333",
         "displayName": "DevOps Team"
       }
     ],
     "serviceprincipals": [
       {
         "id": "44444444-4444-4444-4444-444444444444",
         "displayName": "CI/CD Pipeline"
       }
     ]
   }
   ```

2. Use an existing test plan with role assignments (e.g., `tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-azuredevops-plan.json`)

### Generation Command

```bash
# Run tfplan2md with --debug flag and principal mapping
tfplan2md tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-azuredevops-plan.json \
  --debug \
  --principals test-data/principals-demo.json \
  -o artifacts/debug-output-demo.md
```

**Rationale:** This configuration demonstrates both principal mapping diagnostics (with some IDs that will fail to resolve) and template resolution information across multiple resource types.

## Test Steps

1. Run the UAT simulation using the `UAT Tester` agent or manually via scripts:
   ```bash
   scripts/uat-run.sh run artifacts/debug-output-demo.md
   ```

2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

This PR validates the debug output feature which adds diagnostic information to markdown reports when the `--debug` flag is used.

### Specific Sections to Verify

#### 1. Debug Information Section Location
- Verify the report contains a **"## Debug Information"** section at the end
- This section should appear **after** all resource changes

#### 2. Principal Mapping Diagnostics

Look for the **"### Principal Mapping"** subsection:

- **Load Status**: Should display "Principal Mapping: Loaded successfully from 'test-data/principals-demo.json'" (or similar path)
- **Type Counts**: Should display counts like "Found 2 users, 1 groups, 1 service principals"
- **Failed Resolutions**: If any principal IDs in the plan are not in the mapping file, they should be listed with the resource that referenced them

**Expected Format Example:**
```
### Principal Mapping

Principal Mapping: Loaded successfully from 'test-data/principals-demo.json'
- Found 2 users, 1 groups, 1 service principals

Failed to resolve 3 principal IDs:
- `99999999-9999-9999-9999-999999999999` (referenced in `azurerm_role_assignment.example`)
```

#### 3. Template Resolution Information

Look for the **"### Template Resolution"** subsection:

- Should list resource types and the template used for each
- Template sources should be clearly identified:
  - "Built-in resource-specific template" for resources like `azurerm_firewall_network_rule_collection`
  - "Default template" for standard resources
  - Custom template path if a custom template was used

**Expected Format Example:**
```
### Template Resolution

- `azurerm_role_assignment`: Built-in resource-specific template
- `azurerm_resource_group`: Default template
- `azurerm_storage_account`: Default template
```

### Visual Checks

#### GitHub Rendering
- [ ] Debug section heading (H2) renders properly
- [ ] Subsection headings (H3) are clearly visible and properly nested
- [ ] Code-formatted principal IDs (backticks) render as inline code
- [ ] Resource addresses in parentheses are code-formatted
- [ ] Bullet lists render properly
- [ ] No markdown syntax errors visible

#### Azure DevOps Rendering
- [ ] Debug section heading (H2) renders properly
- [ ] Subsection headings (H3) are clearly visible and properly nested
- [ ] Code-formatted principal IDs render as inline code
- [ ] Resource addresses render as inline code
- [ ] Bullet lists render properly
- [ ] Dark theme compatibility (if applicable)

### Functional Validation

#### Information Accuracy
- [ ] Principal type counts are accurate
- [ ] Failed principal IDs (if any) match resources in the plan
- [ ] Template sources accurately reflect what was used

#### Before/After Context

**Before this feature:**
- Users had no visibility into principal mapping or template resolution
- Troubleshooting required examining code or logs
- No way to verify which templates were being used

**After this feature:**
- Users can see exactly which principals were loaded and which failed
- Template resolution is transparent and verifiable
- Debug information is in the same markdown report, not separate logs

### Success Criteria

✅ **Approve this PR if:**
1. Debug section appears at the end of the report
2. Principal mapping diagnostics are complete and clear
3. Template resolution information is accurate
4. Formatting renders correctly in both GitHub and Azure DevOps
5. Information is actionable for troubleshooting

❌ **Request changes if:**
1. Debug section is missing or malformed
2. Information is inaccurate or incomplete
3. Markdown rendering has errors
4. Formatting is unclear or hard to read

### Optional Enhancements to Consider

If you notice any of these during review, please provide feedback:
- Are principal type names clear (users vs service principals)?
- Should failed resolutions be grouped differently?
- Is the template source naming intuitive?
- Would any additional diagnostic information be helpful?

## Notes for UAT Execution

- The debug section should only appear when `--debug` flag is used
- Without the flag, the report should be identical to current behavior
- The feature should not break any existing markdown formatting or content
- Performance impact should be negligible
