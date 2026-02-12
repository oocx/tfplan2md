# Parent-Child Resource Catalog

This document catalogs all Terraform resources in the azurerm, azuread, and azuredevops providers that follow the parent-child pattern where resources can be defined either:
1. As inline attributes within a parent resource
2. As separate standalone child resources

## Implementation Status Legend

- ✅ **Implemented** - Fully implemented with inline rendering
- 🚧 **In Progress** - Currently being implemented
- ⏳ **Planned** - Not yet implemented but cataloged for future work

---

## Azure AD (azuread) Provider

### azuread_group

**Status:** ⏳ Planned (Initial Implementation Target)

**Parent Resource:** `azuread_group`
- **Inline Attribute:** `members` (set of member object IDs)
- **Child Resources:** 
  - `azuread_group_member` - Individual group membership
  - `azuread_group_owner` - Individual group ownership (via `owners` attribute or separate resource)

**Documentation Note:** "Do not use the `members` property at the same time as the azuread_group_member resource for the same group. Doing so will cause a conflict and group members will be removed."

**Inline Attributes:**
- `members` - A set of members who should be present in this group
- `owners` - A set of object IDs of principals that will be granted ownership

**Separate Resources:**
- `azuread_group_member` with `group_object_id` + `member_object_id`
- Similar pattern may exist for owners

**Rendering Strategy:** Table with columns: Change, Member/Owner ID (formatted), Role (Member/Owner), Terraform Address (if separate resource)

---

### azuread_application

**Status:** ⏳ Planned

**Parent Resource:** `azuread_application`

**Child Resources:**
- `azuread_application_app_role` - App roles
- `azuread_application_api_access` - API access permissions
- `azuread_application_certificate` - Certificates
- `azuread_application_federated_identity_credential` - Federated credentials
- `azuread_application_identifier_uri` - Identifier URIs
- `azuread_application_known_clients` - Known client applications
- `azuread_application_optional_claims` - Optional claims
- `azuread_application_owner` - Owners
- `azuread_application_password` - Passwords
- `azuread_application_permission_scope` - Permission scopes
- `azuread_application_pre_authorized` - Pre-authorized applications
- `azuread_application_redirect_uris` - Redirect URIs

**Note:** azuread_application has many inline attributes that can also be managed via separate resources. Each has specific attributes and would need individual analysis for table design.

**Rendering Strategy:** Would require resource-specific analysis to determine which child resources benefit from inline table rendering vs. keeping as separate resources.

---

## Azure RM (azurerm) Provider

### azurerm_virtual_network

**Status:** 🚧 In Progress (Batch 2)

**Parent Resource:** `azurerm_virtual_network`

**Inline Attribute:** `subnet` (list of subnet blocks)

**Child Resources:**
- `azurerm_subnet` - Individual subnet
- `azurerm_virtual_network_dns_servers` - DNS servers configuration (via `dns_servers` attribute)

**Documentation Note:** "Terraform currently provides both a standalone Subnet resource, and allows for Subnets to be defined in-line within the Virtual Network resource. At this time you cannot use a Virtual Network with in-line Subnets in conjunction with any Subnet resources. Doing so will cause a conflict of Subnet configurations and will overwrite subnets."

**Inline Attributes:**
- `subnet` - Can be specified multiple times to define multiple subnets, with fields:
  - `name`, `address_prefixes`, `security_group`, `delegation`, etc.
- `dns_servers` - List of IP addresses of DNS servers

**Rendering Strategy:** 
- Subnets: Table with columns: Change, Name, Address Prefixes, Security Group, Terraform Address (if separate)
- DNS Servers: Simple list or table depending on complexity

---

### azurerm_network_security_group

**Status:** 🚧 In Progress (Batch 2)

**Parent Resource:** `azurerm_network_security_group`

**Inline Attribute:** `security_rule` (list of security rule blocks)

**Child Resource:** `azurerm_network_security_rule`

**Documentation Note:** "Terraform currently provides both a standalone Network Security Rule resource, and allows for Network Security Rules to be defined in-line within the Network Security Group resource. At this time you cannot use a Network Security Group with in-line Network Security Rules in conjunction with any Network Security Rule resources. Doing so will cause a conflict of rule settings and will overwrite rules."

**Inline Attributes:**
- `security_rule` - List of security_rule objects with fields:
  - `name`, `priority`, `direction`, `access`, `protocol`, `source_port_range`, `destination_port_range`, `source_address_prefix`, `destination_address_prefix`, `description`

**Rendering Strategy:** Table similar to firewall rules - columns: Change, Name, Priority, Direction, Access, Protocol, Source, Destination, Ports, Description. May need to handle source/destination ranges appropriately.

**Note:** Currently partially implemented for NSG replacements, but not for inline rules vs separate resources.

---

### azurerm_route_table

**Status:** 🚧 In Progress (Batch 2)

**Parent Resource:** `azurerm_route_table`

**Inline Attribute:** `route` (list of route blocks)

**Child Resource:** `azurerm_route`

**Documentation Note:** "Terraform currently provides both a standalone Route resource, and allows for Routes to be defined in-line within the Route Table resource. At this time you cannot use a Route Table with in-line Routes in conjunction with any Route resources. Doing so will cause a conflict of Route configurations and will overwrite Routes."

**Inline Attributes:**
- `route` - List of route objects with fields:
  - `name`, `address_prefix`, `next_hop_type`, `next_hop_in_ip_address`

**Rendering Strategy:** Table with columns: Change, Name, Address Prefix, Next Hop Type, Next Hop Address, Terraform Address (if separate resource)

---

### azurerm_dns_zone

**Status:** 🚧 In Progress (Batch 2)

**Parent Resource:** `azurerm_dns_zone` (and `azurerm_private_dns_zone`)

**Child Resources (by type):**
- `azurerm_dns_a_record` / `azurerm_private_dns_a_record`
- `azurerm_dns_aaaa_record` / `azurerm_private_dns_aaaa_record`
- `azurerm_dns_cname_record` / `azurerm_private_dns_cname_record`
- `azurerm_dns_mx_record` / `azurerm_private_dns_mx_record`
- `azurerm_dns_ns_record`
- `azurerm_dns_ptr_record` / `azurerm_private_dns_ptr_record`
- `azurerm_dns_srv_record` / `azurerm_private_dns_srv_record`
- `azurerm_dns_txt_record` / `azurerm_private_dns_txt_record`
- `azurerm_dns_caa_record`

**Note:** DNS zones do NOT have inline record attributes - all records are separate resources. However, they are conceptually child resources that should be grouped under their parent zone for readability.

**Rendering Strategy:** Group by zone, then show table of records within that zone. Columns would vary by record type but generally: Change, Name, Type, TTL, Value/Target.

---

### azurerm_firewall

**Status:** ✅ Implemented (Features 026 & 060)

**Parent Resource:** `azurerm_firewall` (implicit parent via reference)

**Inline Attributes (in rule collection resources):**
- In `azurerm_firewall_network_rule_collection`: `rule` blocks
- In `azurerm_firewall_application_rule_collection`: `rule` blocks  

**Child Resources:**
- `azurerm_firewall_network_rule_collection` - with inline `rule` blocks
- `azurerm_firewall_application_rule_collection` - with inline `rule` blocks

**Note:** Firewall rules are always defined within collection resources. There are no separate `azurerm_firewall_network_rule` or `azurerm_firewall_application_rule` resources. The collections themselves can have inline rule blocks.

**Current Implementation:** 
- Network rules: Display as table with change indicators
- Application rules: Display as table with change indicators
- Both support mixed inline/attribute patterns

---

### azurerm_virtual_hub_route_table

**Status:** ⏳ Planned

**Parent Resource:** `azurerm_virtual_hub_route_table`

**Inline Attribute:** None explicitly mentioned

**Child Resource:** `azurerm_virtual_hub_route_table_route`

**Rendering Strategy:** Would need to investigate if routes can be defined inline or only as separate resources.

---

## Azure DevOps (azuredevops) Provider

### azuredevops_group

**Status:** ⏳ Planned (Initial Implementation Target)

**Parent Resource:** `azuredevops_group`

**Inline Attribute:** `members` (list of member descriptors)

**Child Resource:** `azuredevops_group_membership`

**Documentation Note:** "It's possible to define group members both within the azuredevops_group resource via the members block and by using the azuredevops_group_membership resource. However it's not possible to use both methods to manage group members, since there'll be conflicts."

**Inline Attributes:**
- `members` - The member descriptors of the Group

**Rendering Strategy:** Table with columns: Change, Member Descriptor, Member Name (if resolvable), Terraform Address (if separate resource)

---

### azuredevops_team

**Status:** ⏳ Planned (Initial Implementation Target)

**Parent Resource:** `azuredevops_team`

**Inline Attributes:**
- `administrators` (list of administrator descriptors)
- `members` (list of member descriptors)

**Child Resources:**
- `azuredevops_team_administrators`
- `azuredevops_team_members`

**Documentation Note:** "It's possible to define team members/administrators both within the azuredevops_team resource via the members/administrators block and by using the azuredevops_team_members/azuredevops_team_administrators resource. However it's not possible to use both methods to manage team members/administrators, since there'll be conflicts."

**Rendering Strategy:** Two tables within the team resource:
1. Administrators table: Change, Descriptor, Name (if resolvable), Terraform Address (if separate)
2. Members table: Change, Descriptor, Name (if resolvable), Terraform Address (if separate)

---

### azuredevops_variable_group

**Status:** ⏳ Planned

**Parent Resource:** `azuredevops_variable_group`

**Inline Attribute:** None (variables managed via `variable` blocks within resource)

**Child Resource:** `azuredevops_variable_group_variable`

**Note:** Would need to verify if variables can be defined inline or only via separate resources.

**Rendering Strategy:** Table of variables with columns: Change, Key, Value (sensitive handling), Terraform Address (if separate resource)

---

## Summary Statistics

**Total Parent-Child Patterns Identified:** 15+

**By Provider:**
- azuread: 2 main patterns (group, application with multiple sub-patterns)
- azurerm: 8+ patterns (virtual_network, network_security_group, route_table, dns_zone, firewall collections, virtual_hub_route_table)
- azuredevops: 3 patterns (group, team, variable_group)

**Implementation Targets for Initial Release:**
1. ✅ azurerm_firewall_network_rule_collection (already implemented)
2. ✅ azurerm_firewall_application_rule_collection (already implemented)
3. 🚧 azuread_group / azuread_group_member
4. 🚧 azuredevops_group / azuredevops_group_membership  
5. 🚧 azuredevops_team / azuredevops_team_members / azuredevops_team_administrators

---

## Design Principles

### When to Inline as Table

Child resources should be inlined as tables when:

1. **Limited Columns**: The child resource has few enough attributes to fit in a readable table (typically ≤8 columns)
2. **Scanability**: Table format improves readability compared to separate sections
3. **Natural Grouping**: The children are conceptually part of the parent (rules in a firewall, members in a group)
4. **Frequency**: Users commonly need to review all children when reviewing the parent

### When to Keep Separate

Child resources should remain as separate sections when:

1. **Complex Attributes**: The child resource has many attributes or nested structures
2. **Large Values**: Attributes contain large blocks of text/JSON that don't fit in tables
3. **Low Cohesion**: The relationship is more associative than compositional

### Hybrid Approach

For resources with both simple and complex children:
1. Show summary table with essential columns
2. Include full resource details below the table (as collapsible sections or paragraphs) for children with additional complex attributes

---

## Technical Implementation Notes

### Relationship Detection

For each parent-child pattern, the implementation must:

1. **Catalog the relationship** in a registry/configuration
2. **Detect inline children** by parsing the parent resource's attribute values
3. **Detect separate children** by matching child resource references to parent IDs
4. **Merge for display** - combine inline and separate children into a unified table

### Reference Resolution

Child resources reference parents via:
- Direct ID attributes (e.g., `group_object_id`, `network_security_group_name`)
- Terraform references (e.g., `azurerm_resource_group.example.name`)

The tool must resolve these references from the plan JSON to match children to parents.

### Terraform Address Tracking

When children are separate resources:
- Include the Terraform resource address in the table (e.g., `azuread_group_member.john`)
- This helps users understand the Terraform structure and troubleshoot

When children are inline attributes:
- No separate Terraform address exists
- The table row represents an element within the parent's attribute

---

## Related Features

- **Feature 026**: Template Rendering Simplification (azurerm_firewall_network_rule_collection)
- **Feature 060**: Custom Template for azurerm_firewall_application_rule_collection

## References

- [Terraform azuread Provider Documentation](https://registry.terraform.io/providers/hashicorp/azuread/latest/docs)
- [Terraform azurerm Provider Documentation](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Terraform azuredevops Provider Documentation](https://registry.terraform.io/providers/microsoft/azuredevops/latest/docs)
