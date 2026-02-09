# Issue: Duplicate Header in azapi_resource and azuredevops_variable_group Templates

## Problem Description

The rendered markdown output for `azapi_resource` and `azuredevops_variable_group` resources shows duplicate header information. The resource type and name appear twice:
1. First in the collapsible `<summary>` tag
2. Again as a markdown `###` heading immediately below

This creates visual redundancy and differs from the pattern used by other custom templates.

## Steps to Reproduce

1. Generate markdown from a Terraform plan containing `azapi_resource` or `azuredevops_variable_group` changes
2. Open the generated `comprehensive-demo.md` artifact
3. Observe the duplicate headers

**Example for azapi_resource:**
```markdown
<summary>➕ azapi_resource <b><code>container_app</code></b> — <code>🆔 ca-tfplan2md-demo</code> <code>🌍 eastus</code></summary>
<br>

### ➕ azapi_resource.container_app
```

**Example for azuredevops_variable_group:**
```markdown
<summary>🔄 azuredevops_variable_group <b><code>pipeline_vars</code></b> — <code>🆔 deploy-pipeline-vars</code> | 5🔧 ...</summary>
<br>

### 🔄 azuredevops_variable_group.pipeline_vars
```

## Expected Behavior

Custom templates should follow the same pattern as the default `_resource.sbn` template, which only uses the `<summary>` tag without an additional markdown heading. The summary already contains all the necessary information (action symbol, resource type, resource name, and metadata).

## Actual Behavior

Two templates include an explicit `###` heading after the summary:
- `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn` (line 11)
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/variable_group.sbn` (line 10)

Both contain:
```handlebars
### {{ change.action_symbol }} {{ change.address | escape_markdown }}
```

## Root Cause Analysis

### Affected Components
- File: `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn#L11`
- File: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/variable_group.sbn#L10`
- Component: Scriban template rendering for custom resource types

### What's Broken
The two custom templates include an explicit heading line that duplicates information already present in the `change.summary_html` variable, which is rendered in the `<summary>` tag.

### Why It Happened
These templates were likely created before the pattern of using only the `<summary>` tag was established. The explicit heading was probably added for clarity but is now redundant given that `change.summary_html` already contains the same information in a more visually appealing format.

### Pattern Verification
I verified all other custom templates to confirm they do NOT have this issue:
- `azuread/group.sbn` - ✅ No explicit heading
- `azuread/group_member.sbn` - ✅ No explicit heading
- `azuread/group_without_members.sbn` - ✅ No explicit heading
- `azuread/invitation.sbn` - ✅ No explicit heading
- `azuread/service_principal.sbn` - ✅ No explicit heading
- `azuread/user.sbn` - ✅ No explicit heading
- `azurerm/firewall_application_rule_collection.sbn` - ✅ No explicit heading
- `azurerm/firewall_network_rule_collection.sbn` - ✅ No explicit heading
- `azurerm/network_security_group.sbn` - ✅ No explicit heading

All other custom templates correctly rely on the `<summary>{{ change.summary_html }}</summary>` pattern alone.

## Suggested Fix Approach

Remove the explicit heading line from both templates:

1. **In `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn`**:
   - Delete line 11: `### {{ change.action_symbol }} {{ change.address | escape_markdown }}`
   - Remove the blank line after it (line 12) to maintain consistent spacing

2. **In `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/variable_group.sbn`**:
   - Delete line 10: `### {{ change.action_symbol }} {{ change.address | escape_markdown }}`
   - Remove the blank line after it (line 11) to maintain consistent spacing

This will make both templates consistent with:
- The default `_resource.sbn` template pattern
- All other custom templates (azuread, azurerm)
- The visual design that uses collapsible sections with rich summary information

## Related Tests

Tests that should pass after the fix:
- [ ] `AzapiResourceTemplateTests` - All azapi_resource template rendering tests
- [ ] Test snapshots in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azapi-*.md` 
- [ ] Any tests for `azuredevops_variable_group` rendering
- [ ] Integration tests that verify the comprehensive demo output

**Note:** Test snapshots will need to be updated to reflect the removal of the duplicate header line. The snapshot update process is documented and handled by the Developer agent.

## Additional Context

- **Issue origin**: Reported in GitHub/Azure DevOps as duplicate header in Azure DevOps rendering
- **Related feature**: Originally implemented in `docs/features/040-azapi-resource-template/specification.md`
- **Affected artifacts**: `artifacts/comprehensive-demo.md` and any Terraform plan reports containing these resource types
- **Visual impact**: The duplicate header creates unnecessary visual clutter in the rendered markdown, especially in Azure DevOps PR comments
