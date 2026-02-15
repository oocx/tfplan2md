# Feature: Parent-Child Resource Grouping and Inline Rendering

## Overview

Many Terraform resources follow a parent-child pattern where child resources can be defined either as inline attributes within a parent resource or as separate standalone child resources. Examples include group members, firewall rules, network security rules, and route table routes.

Currently, tfplan2md renders each resource as a separate section in the markdown output. This creates excessive scrolling and makes it difficult to understand the relationship between parent and child resources. This feature improves readability by rendering child resources inline as tables within their parent resource sections, making it immediately clear which children belong to which parents.

## User Goals

- **Improve Readability**: View parent resources and their children together in a single section, reducing context switching
- **Reduce Scrolling**: Eliminate separate sections for simple child resources that can be represented as table rows
- **Understand Structure**: Clearly see whether children are defined inline (in parent attributes) or as separate resources
- **Comprehensive Information**: Maintain access to all information from the plan, even in table format
- **Consistent Patterns**: Experience the same inline rendering pattern for all similar parent-child relationships across providers

## Problem Statement

**Current Behavior:**
When a Terraform plan includes resources like:
- `azuread_group.admins` with 10 separate `azuread_group_member` resources
- `azurerm_network_security_group.app` with 5 separate `azurerm_network_security_rule` resources
- `azuredevops_team.platform` with separate `azuredevops_team_members` resources

The markdown report shows each of these as separate collapsible sections (11 sections for the group, 6 for the NSG, etc.), requiring significant scrolling to review all related changes.

**Desired Behavior:**
Child resources should be rendered as rows in a table within the parent resource's section, similar to how firewall network rules and application rules are currently displayed. The table should clearly indicate whether each child came from an inline attribute or a separate resource.

## Scope

### In Scope

#### Core Infrastructure

1. **Parent-Child Relationship Registry**
   - Comprehensive catalog of all parent-child patterns in azurerm, azuread, and azuredevops providers
   - Configuration for each relationship:
     - Parent resource type
     - Inline attribute name (if applicable)
     - Child resource type(s)
     - Reference attribute names (how children reference parents)
   - Implementation status tracking (implemented, planned, not applicable)

2. **Child Resource Detection**
   - Detect inline children by parsing parent resource attributes
   - Detect separate children by matching child resource references to parent IDs
   - Resolve Terraform references in the plan JSON
   - Handle both direct ID matching and reference expressions

3. **Inline Table Rendering**
   - Render child resources as table rows within parent sections
   - Include change indicators (➕, 🔄, ❌, ⏺️) for each child
   - Show Terraform resource address for separate child resources
   - For children from inline attributes, show the inline attribute name in the "Terraform Resource" column (e.g., `members` attribute)
   - Handle mixed scenarios (both inline and separate children for same parent)

4. **Static Code Analysis Findings for Merged Children**
   - If static code analysis findings are mapped to a child resource address that is rendered inline (i.e., the child does not have its own standalone section), the findings MUST still be displayed within the parent resource section
   - The findings display MUST preserve the original Terraform resource address for each finding (so users can attribute findings to the correct resource even when it is rendered inline)
   - Report-level sections such as "Code Analysis Summary" and "Other Findings" continue to behave as they do today (this feature only defines where mapped child findings appear when the child resource section is merged)

#### Initial Implementation Targets

The feature will initially implement inline rendering for:

1. **azurerm_firewall_network_rule_collection** (already implemented in features 026 & 060)
   - Rules can be defined inline within the collection
   - No separate `azurerm_firewall_network_rule` resource exists
   
2. **azurerm_firewall_application_rule_collection** (already implemented in features 026 & 060)
   - Rules can be defined inline within the collection
   - No separate `azurerm_firewall_application_rule` resource exists

3. **azuread_group** / **azuread_group_member**
   - Inline via `members` attribute
   - Separate via `azuread_group_member` resources
   - Show member object IDs with readable formatting

4. **azuredevops_group** / **azuredevops_group_membership**
   - Inline via `members` attribute
   - Separate via `azuredevops_group_membership` resources
   - Show member descriptors

5. **azuredevops_team** / **azuredevops_team_members** / **azuredevops_team_administrators**
   - Inline via `members` and `administrators` attributes
   - Separate via dedicated resources
   - Render two tables: one for administrators, one for members

#### Documentation

- Complete catalog of all parent-child patterns (see `parent-child-resource-catalog.md`)
- Rendering examples showing expected output (see `rendering-examples.md`)
- Architecture documentation explaining the extensibility design
- Guidelines for adding new parent-child patterns in the future

### Out of Scope

#### Not in Initial Implementation

The following parent-child patterns are cataloged but NOT implemented in this feature release:

1. **azurerm_virtual_network** / **azurerm_subnet**
2. **azurerm_network_security_group** / **azurerm_network_security_rule**
3. **azurerm_route_table** / **azurerm_route**
4. **azurerm_dns_zone** / **azurerm_dns_[type]_record**
5. **azuread_application** and its many child resources
6. **azuredevops_variable_group** / **azuredevops_variable_group_variable**
7. Other patterns documented in the catalog

These will be added incrementally in future releases once the architecture is proven with the initial targets.

#### Implementation Constraints

- **No new command-line options**: This feature is always enabled for supported resources
- **No opt-out mechanism**: Users cannot disable inline rendering for specific resource types
- **No template customization**: Users cannot customize which child resources are inlined vs separate

#### Non-Goals

- Rendering children for ALL resource types automatically (only explicitly configured patterns)
- Inferring parent-child relationships from plan data alone (relationships must be registered)
- Handling complex nested hierarchy beyond parent-child (e.g., grandchildren)

## User Experience

### Viewing Group with Members

**Scenario:** User reviews a plan that creates an Azure AD group with 5 members defined as separate `azuread_group_member` resources.

**Current Experience:**
- Scrolls through 6 separate collapsible sections
- Must mentally track which members belong to which group
- Member resources show group ID but not formatted group name

**New Experience:**
- Single section for the group
- Members shown in a table directly under group attributes
- Clear indication of Terraform resource address for each member
- Change indicators show which members are being added
- Summary line shows "➕ azuread_group ... | ➕ 5 members"

### Updating Network Security Rules

**Scenario:** User reviews a plan that modifies NSG rules (adds 2, removes 1, modifies 1).

**Note:** This scenario is illustrative for a future follow-up feature and is out of scope for the initial implementation targets of Feature 068.

**Current Experience:**
- Separate sections for the NSG and each rule resource
- Must cross-reference rule names with the parent NSG
- Difficult to see the overall rule set at a glance

**New Experience:**
- Single section for the NSG
- All rule changes shown in a single table
- Change indicators (➕, 🔄, ❌) in the first column
- Can immediately see the complete rule set and what's changing

### Mixed Inline and Separate Resources

**Scenario:** A plan contains a virtual network with subnets defined both inline and as separate resources (during a migration).

**Note:** This scenario is illustrative for a future follow-up feature and is out of scope for the initial implementation targets of Feature 068.

**Experience:**
- Table shows all subnets together
- "Terraform Resource" column shows `azurerm_subnet.xyz` for separate, and the inline attribute name for inline (e.g., `subnet` attribute)
- Warning message indicates potential conflict: "This resource has children managed both inline and as separate resources"

### Table Column Overflow

**Scenario:** A child resource has many attributes that don't fit well in a table.

**Note:** This scenario is illustrative for a future follow-up feature and is out of scope for the initial implementation targets of Feature 068.

**Experience:**
- Essential attributes shown in table columns
- Complex or large values shown below the table as collapsible sections
- Each complex child resource gets its own subsection with full details

## Success Criteria

### Functional Requirements

- [ ] **Registry Complete**: All parent-child patterns in azurerm, azuread, and azuredevops providers are cataloged with implementation status
- [ ] **Inline Rendering**: The 5 initial target resources (firewall rules x2, azuread groups, azuredevops groups, azuredevops teams) render children inline as tables
- [ ] **Change Indicators**: Tables include change indicators (➕, 🔄, ❌, ⏺️) for each child resource
- [ ] **Resource Address**: Separate child resources show their Terraform address in the table
- [ ] **Inline Source**: Children from inline attributes show the inline attribute name in the table (e.g., `members` attribute)
- [ ] **Mixed Handling**: Plans with both inline and separate children show warning and render both
- [ ] **Formatting**: All table values use existing formatting (emojis, highlighting, truncation)
- [ ] **Summary Line**: Parent resource summary line includes child change counts
- [ ] **Merged-Child Findings**: Findings mapped to inlined/merged child resources are displayed within the parent resource section while preserving the child resource address

### Quality Requirements

- [ ] **Snapshot Tests**: Updated snapshot tests demonstrate the inline rendering for all target resources
- [ ] **UAT Test Coverage**: UAT test report covers the in-scope scenarios from `rendering-examples.md` (Examples 1–6A) and is used to generate a new snapshot test
- [ ] **Example Artifacts**: Demo artifacts in `artifacts/` folder show realistic examples
- [ ] **Documentation**: Catalog and rendering examples are complete and accurate
- [ ] **Architecture**: Code structure makes it easy to add new parent-child patterns
- [ ] **No Regressions**: Existing resource rendering (non-parent-child) remains unchanged

### Non-Functional Requirements

- [ ] **Performance**: No measurable degradation in plan processing time
- [ ] **Maintainability**: New parent-child patterns can be added with < 50 lines of code per pattern
- [ ] **Testability**: Each parent-child pattern has dedicated test coverage

## Related Work

### Existing Features

- **Feature 026**: azurerm_firewall_network_rule_collection template (already implements inline rendering for network rules)
- **Feature 060**: azurerm_firewall_application_rule_collection template (already implements inline rendering for application rules)

These features established the pattern that this feature extends to other resource types.

### Future Features

After this feature is implemented, future features can:
- Add remaining parent-child patterns from the catalog (one at a time)
- Implement table rendering for complex nested structures
- Add configuration options for table column visibility

## Open Questions

*None.*

**Resolved Decision:** Examples 7–10 in `rendering-examples.md` are illustrative for future follow-up features. UAT report + snapshot coverage for Feature 068 initial implementation is required for Examples 1–6A only.

---

## Glossary

**Parent Resource**: A Terraform resource that can contain child resources via inline attributes (e.g., `azuread_group` with `members` attribute)

**Child Resource**: A Terraform resource that can be defined either inline within a parent or as a separate standalone resource (e.g., `azuread_group_member`)

**Inline Attribute**: An attribute on a parent resource that accepts a list or set of child resource definitions (e.g., `members` on `azuread_group`)

**Separate Resource**: A standalone Terraform resource that references a parent resource via ID or reference (e.g., `azuread_group_member` with `group_object_id`)

**Inline Rendering**: Displaying child resources as table rows within the parent resource's markdown section rather than as separate sections

**Parent-Child Pattern**: A Terraform provider design where functionally related resources can be managed either inline or separately, with documentation explicitly warning against mixing both approaches
