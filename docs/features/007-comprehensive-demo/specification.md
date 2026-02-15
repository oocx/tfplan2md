# Comprehensive Demo Terraform Plan - Specification

## Purpose

This specification defines a comprehensive Terraform plan JSON example that demonstrates **all** supported features of the `tfplan2md` default template. The plan will serve as:

1. A complete feature showcase for documentation and demos
2. A test fixture for validating template rendering
3. A reference implementation for users learning tfplan2md capabilities

**Important**: This example contains **only the plan JSON** (the output result), not the actual Terraform files that would produce such a plan. The plan JSON is crafted to comprehensively demonstrate all tfplan2md features.

## Design Goals

- **Coverage**: Include every feature supported by the default template
- **Realism**: Use realistic Azure infrastructure scenarios
- **Clarity**: Each feature should be easily identifiable in the output
- **Maintainability**: Keep the plan simple enough to update as features evolve
- **Living Example**: This example **must be updated** whenever tfplan2md features are added or changed to ensure it remains comprehensive
- **Easy Testing**: The demo plan JSON and required files must be included in the Docker image to allow users to test tfplan2md immediately without any setup

## Required Features Coverage

### 1. Module Grouping

**Requirement**: Demonstrate multi-module organization with nested modules.

**Implementation**:
- Root module with core infrastructure
- Child module: `module.network` - networking resources
- Child module: `module.security` - security and access control
- Nested module: `module.network.module.monitoring` - monitoring resources within network module

**Rationale**: Shows module grouping, hierarchy, and alphabetical ordering.

### 2. All Action Types

**Requirement**: Include resources for each action type with proper distribution.

**Implementation**:

| Action | Count | Resources |
|--------|-------|-----------|
| ➕ Create | 12 | Various resources across all modules |
| 🔄 Update | 5 | Existing resources being modified |
| ♻️ Replace | 2 | Resources requiring recreation |
| ❌ Destroy | 3 | Resources being removed |
| No-Op | 20+ | Resources with no changes (counted in summary only) |

**Rationale**: Tests all action symbols and summary calculations.

### 3. Summary Resource Type Breakdown

**Requirement**: Multiple different resource types within each action category.

**Implementation** (minimum 3 types per action where count > 0):

**Create (12 resources, 8 types)**:
- 2× `azurerm_resource_group`
- 2× `azurerm_virtual_network`
- 1× `azurerm_subnet`
- 1× `azurerm_storage_account`
- 2× `azurerm_role_assignment`
- 1× `azurerm_key_vault`
- 1× `azurerm_firewall_network_rule_collection`
- 2× `azurerm_log_analytics_workspace`

**Update (5 resources, 4 types)**:
- 2× `azurerm_storage_account` (tags/properties update)
- 1× `azurerm_virtual_network` (address space expansion)
- 1× `azurerm_firewall_network_rule_collection` (rule modifications)
- 1× `azurerm_key_vault` (access policy changes)

**Replace (2 resources, 2 types)**:
- 1× `azurerm_subnet` (requires recreation due to address prefix change)
- 1× `azurerm_network_security_group` (forces replacement)

**Destroy (3 resources, 3 types)**:
- 1× `azurerm_storage_account` (deprecated storage)
- 1× `azurerm_role_assignment` (removed access)
- 1× `azurerm_virtual_network` (old network being removed)

**Rationale**: Demonstrates resource type breakdown in summary table with alphabetical sorting.

### 4. Sensitive Values

**Requirement**: Include sensitive attributes that are masked by default.

**Implementation**:
- `azurerm_key_vault_secret` with sensitive `value` attribute
- `azurerm_storage_account` with sensitive `primary_access_key` in outputs
- Custom sensitive variables used in resource attributes

**Rationale**: Tests `--show-sensitive` flag functionality and masking behavior.

### 5. Attribute Tables

**Requirement**: Demonstrate all three attribute table formats based on action type.

**Implementation**:

**Create (2-column: Attribute | Value)**:
- Resource with 10+ attributes showing the `after` values
- Include null/unknown values (should be omitted)
- Include sensitive attributes (should be masked)

**Delete (2-column: Attribute | Value)**:
- Resource with 8+ attributes showing the `before` values
- Include attributes with different data types (string, number, boolean, list, object)

**Update & Replace (3-column: Attribute | Before | After)**:
- Resources with attribute changes showing both values
- Include unchanged attributes with same before/after values
- Include attributes changing from null to value and vice versa

**Rationale**: Tests all attribute table rendering modes and value handling.

### 6. Azure Role Assignment Display

**Requirement**: Demonstrate all role assignment display features.

**Implementation**:

**Role Assignments to create**:
1. Built-in role: "Reader" role on a resource group
2. Built-in role: "Storage Blob Data Reader" on a storage account
3. Built-in role: "Contributor" on subscription scope
4. Built-in role: "Virtual Machine Contributor" on a VM resource

**Scope Types**:
- Management Group: `azurerm_role_assignment.mg_reader` → `/providers/Microsoft.Management/managementGroups/my-mg`
- Subscription: `azurerm_role_assignment.sub_contributor` → `/subscriptions/12345678-1234-1234-1234-123456789012`
- Resource Group: `azurerm_role_assignment.rg_reader` → `/subscriptions/{sub-id}/resourceGroups/my-rg`
- Resource (Storage Account): `azurerm_role_assignment.storage_reader` → full storage account resource ID

**Principal Mapping** (principals.json):
- Include user principal with name "Jane Doe (User)"
- Include group principal with name "DevOps Team (Group)"
- Include service principal with name "Deployment Pipeline (Service Principal)"
- Include unmapped principal (fallback to GUID display)

**Rationale**: Tests role definition mapping, scope parsing, principal mapping, and fallback behavior.

### 7. Firewall Network Rule Collection Display

**Requirement**: Demonstrate semantic rule diffing for firewall collections.

**Implementation**:

**Update existing collection** (`azurerm_firewall_network_rule_collection.main`):
- **Added rules**: 2 new rules added to the collection
- **Removed rules**: 1 rule removed from the collection
- **Modified rules**: 2 rules with changes (e.g., source addresses expanded, destination ports changed)
- **Unchanged rules**: 3 rules with no changes (shown with ⏺️)

**Create new collection**:
- Fresh collection with 5 rules (rendered as simple table)

**Rationale**: Tests `diff_array` helper, `format_diff` helper, and resource-specific template rendering.

### 8. Module Change Context

**Requirement**: Resources must be properly grouped by module address.

**Implementation**:

**Root module**:
- Resource groups
- Core storage account
- Some role assignments

**module.network**:
- Virtual networks
- Subnets
- Network security groups
- Firewall with rule collections

**module.security**:
- Key vaults
- Role assignments
- Secrets

**module.network.module.monitoring** (nested):
- Log Analytics workspaces
- Diagnostic settings

**Rationale**: Validates module grouping logic and hierarchical display.

### 9. Terraform Version and Metadata

**Requirement**: Include complete plan metadata.

**Implementation**:
- `terraform_version`: "1.14.0" (or latest stable)
- `format_version`: "1.2"
- `timestamp`: RFC3339 formatted timestamp (e.g., "2025-12-20T14:30:00Z")

**Rationale**: Tests metadata rendering in report header.

### 10. Complex Attribute Types

**Requirement**: Include various attribute data types to test rendering.

**Implementation**:
- **Primitives**: strings, numbers, booleans
- **Lists**: `["item1", "item2", "item3"]`
- **Maps/Objects**: `{"key1": "value1", "key2": "value2"}`
- **Nested structures**: objects containing lists, lists containing objects
- **Computed values**: `(known after apply)`
- **Null values**: attributes transitioning to/from null
- **Empty collections**: `[]`, `{}`

**Rationale**: Tests attribute value formatting and edge case handling.

## File Structure

```
examples/comprehensive-demo/
├── README.md                    # Usage documentation and feature coverage matrix
├── demo-principals.json         # Principal ID to name mappings (for role assignment feature)
├── plan.json                    # Comprehensive plan JSON demonstrating all features
├── report.md                    # Sample output (default template)
├── report-with-sensitive.md     # Sample output (with --show-sensitive)
└── report-summary.md            # Sample output (summary template)
```

**Note**: No actual Terraform configuration files (.tf) are included. The plan.json is a handcrafted JSON file designed to exercise all template features.

**Docker Integration**: The `plan.json` and `demo-principals.json` files must be included in the Docker image (in `/examples/comprehensive-demo/`) so users can immediately test tfplan2md without any setup.

## Plan JSON Structure Details

### Metadata

The plan.json should include:
- `terraform_version`: "1.14.0" (or latest stable)
- `format_version`: "1.2"
- `timestamp`: RFC3339 formatted timestamp

### Resource Changes

The plan should simulate:
- Resources that already exist in state (for update/replace/destroy actions)
- New resources being added (for create actions)
- No-op resources (unchanged resources in state)

All changes should be crafted in the JSON structure to represent realistic Terraform plan output with proper `before`, `after`, and `actions` fields.

## Sample Output Files

### report.md
Generated with: `tfplan2md plan.json --principals demo-principals.json`

Should demonstrate:
- Complete summary table with all action types
- Resource type breakdown with 2-3 types per action
- Module grouping with headers
- Masked sensitive values
- All attribute table formats
- Azure role assignment formatting
- Firewall rule semantic diff table

### report-with-sensitive.md
Generated with: `tfplan2md plan.json --principals demo-principals.json --show-sensitive`

Should demonstrate:
- Identical structure to report.md
- Sensitive values revealed in plain text

### report-summary.md
Generated with: `tfplan2md plan.json --template summary`

Should demonstrate:
- Compact summary-only output
- Terraform version and timestamp
- Action counts with resource type breakdown

### Docker Usage Example

The README.md must include an example of how to generate the demo report using Docker:

```bash
# Generate default report
docker run --rm oocx/tfplan2md /examples/comprehensive-demo/plan.json \
  --principals /examples/comprehensive-demo/demo-principals.json

# Generate report with sensitive values
docker run --rm oocx/tfplan2md /examples/comprehensive-demo/plan.json \
  --principals /examples/comprehensive-demo/demo-principals.json \
  --show-sensitive

# Generate summary report
docker run --rm oocx/tfplan2md /examples/comprehensive-demo/plan.json \
  --template summary
```

## Testing Strategy

This comprehensive demo should:

1. **Validate rendering**: Ensure all template features work correctly
2. **Documentation**: Provide copyable examples for README and docs
3. **Regression testing**: Detect template breaking changes
4. **User reference**: Help users understand output format
5. **Docker testing**: Allow users to test tfplan2md immediately using the included example files

## Success Criteria

The comprehensive demo plan is complete when:

- [ ] All 10 required features are covered in plan.json
- [ ] Plan generates valid reports with default template
- [ ] Plan generates valid reports with summary template
- [ ] Report includes all action types with proper symbols
- [ ] Summary table shows resource type breakdown
- [ ] Module grouping displays correctly with nested modules
- [ ] Sensitive values are masked by default and revealed with `--show-sensitive`
- [ ] Azure role assignments display human-readable information
- [ ] Firewall rule collections show semantic diffs
- [ ] README documents all features covered and usage instructions
- [ ] demo-principals.json is included for role assignment feature
- [ ] Sample reports (report.md, report-with-sensitive.md, report-summary.md) are included
- [ ] Example is documented as a living artifact that must be updated with feature changes
- [ ] plan.json and demo-principals.json are included in the Docker image
- [ ] README includes Docker usage examples showing how to generate reports from the demo files

## Non-Goals

- **Actual Terraform files**: No .tf files are created; only the plan JSON output
- **Real Azure deployment**: The plan is demonstrative only and cannot be applied to real infrastructure
- **Provider testing**: No actual provider interaction or authentication testing
- **Performance testing**: Focus on feature coverage, not large-scale performance
- **All resource types**: Only include resource types that demonstrate specific features

## Notes

- The plan.json and demo-principals.json should be committed to the repository for easy testing without Terraform installed
- These files must also be included in the Docker image at `/examples/comprehensive-demo/` for immediate testing
- Use realistic but safe values (no real credentials, IDs, or sensitive data)
- Keep the overall plan size reasonable (<100 resource changes) to avoid excessive output
- The plan.json is handcrafted JSON, not generated from actual Terraform files
- **Maintenance requirement**: When new features are added to tfplan2md or existing features change, this example MUST be updated to reflect those changes
- The README should include a feature coverage matrix showing which resources/changes demonstrate which features
- The README must include Docker usage examples to help users quickly test tfplan2md with the demo files
