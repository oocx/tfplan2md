# Feature: Network Security Group Security Rules Template

## Overview

Create a specialized template for Azure Network Security Groups (`azurerm_network_security_group`) that displays security rules with semantic diffing, similar to the existing firewall rule collection template. Currently, when security rules are modified within an NSG, the default template shows confusing index-based diffs (e.g., `security_rule[2].name: "allow-https" → "allow-dns"`). This obscures the actual changes when rules are added, removed, or reordered. The new template will show which rules were added, modified, removed, or unchanged, making NSG changes easy to understand during Terraform plan reviews.

## User Goals

- Users need to see which security rules were added to, removed from, or modified within a Network Security Group
- Users want to understand what specific attributes changed within modified rules (e.g., source addresses, ports, access type)
- Users need rules displayed in priority order to understand the evaluation sequence
- Users want this information presented clearly in CI/CD pipeline comments and reports, without navigating through confusing index-based array diffs

## Scope

### In Scope

- Specialized template for `azurerm_network_security_group` resource type
- Display security rules in a single table showing all rule changes at once
- Categorize rules as: Added (➕), Modified (🔄), Removed (❌), or Unchanged (⏺️)
- Match rules by `name` attribute for semantic diffing (not priority, to handle reordering correctly)
- Sort rules by ascending priority within each category
- Display comprehensive rule attributes in separate columns:
  - Name
  - Priority
  - Direction (Inbound/Outbound)
  - Access (Allow/Deny)
  - Protocol
  - Source Addresses
  - Source Ports
  - Destination Addresses
  - Destination Ports
  - Description
- For modified rules: show before/after values with `-` and `+` prefixes for changed attributes, separated by `<br>` tags
- For modified rules: show single value without prefix for unchanged attributes
- Apply to both inline security rules (within `security_rule` blocks) and standalone `azurerm_network_security_rule` resources

### Out of Scope

- Changes to how NSG-level attributes (name, location, resource group) are displayed - these use the default template
- Application security groups (ASGs) - focus on standard security rules only
- Other Azure networking resources beyond NSGs
- Expandable/collapsible sections or separate detail tables (maintain single-table format)
- Custom sorting options beyond priority-based ordering

## User Experience

### Current Behavior (Default Template)

When an NSG is modified with rule changes, users see confusing index-based diffs:

```markdown
### 🔄 azurerm_network_security_group.app

| Attribute | Before | After |
|-----------|--------|-------|
| `security_rule[0].name` | allow-http | allow-https |
| `security_rule[0].destination_port_range` | 80 | 443 |
| `security_rule[1].name` | allow-ssh | allow-http |
| `security_rule[1].destination_port_range` | 22 | 80 |
```

This makes it unclear whether rules were added/removed or just reordered.

### New Behavior (NSG Template)

With the specialized template, the same changes are rendered semantically:

```markdown
### 🔄 azurerm_network_security_group.app

**Network Security Group:** `nsg-app`

#### Security Rules

| | Name | Priority | Direction | Access | Protocol | Source Addresses | Source Ports | Destination Addresses | Destination Ports | Description |
|---|------|----------|-----------|--------|----------|------------------|--------------|----------------------|-------------------|-------------|
| ➕ | allow-https | 100 | Inbound | Allow | TCP | * | * | * | 443 | Allow HTTPS traffic |
| 🔄 | allow-http | 110 | Inbound | Allow | TCP | * | * | * | - 80<br>+ 8080 | - Allow HTTP<br>+ Allow alternate HTTP |
| ❌ | allow-ssh | 120 | Inbound | Allow | TCP | 10.0.0.0/8 | * | * | 22 | Legacy SSH access |
| ⏺️ | allow-dns | 130 | Outbound | Allow | UDP | * | * | 168.63.129.16 | 53 | Azure DNS |
```

### Visual Formatting

- Rules are **ordered by priority** (ascending) within each status category (Added, Modified, Removed, Unchanged)
- Status icon in first column clearly indicates change type
- Modified rules show before/after values on separate lines with `-` and `+` prefixes
- Unchanged attributes show single value (no prefix or duplication)
- Multi-value fields (address prefixes, port ranges) are comma-separated
- Empty/wildcard values shown as `*` for clarity

### Handling Multiple Values

NSG rules can have multiple values for addresses and ports (e.g., `source_address_prefixes` array). These will be:
- Joined with commas: `10.0.1.0/24, 10.0.2.0/24`
- For single-value properties (`source_address_prefix`), display the value directly
- Prefer plural properties (`prefixes`, `ranges`) over singular when both exist

## Success Criteria

- [ ] Template created for `azurerm_network_security_group` resource type
- [ ] Rules categorized correctly as Added, Modified, Removed, or Unchanged using `diff_array` helper with `name` as the key
- [ ] Rules sorted by ascending priority within the rendered table
- [ ] All specified columns displayed: Name, Priority, Direction, Access, Protocol, Source Addresses, Source Ports, Destination Addresses, Destination Ports, Description
- [ ] Modified rules show before/after values with `-` and `+` prefixes for changed attributes
- [ ] Unchanged attributes in modified rules show single value without prefix
- [ ] Multi-value fields (address prefixes, port ranges) rendered as comma-separated lists
- [ ] Template uses `format_diff` helper for displaying changed attributes
- [ ] All existing tests pass
- [ ] New tests verify NSG rule rendering with added/modified/removed/unchanged rules
- [ ] Documentation updated in [docs/features.md](../features.md) and [docs/features/resource-specific-templates.md](resource-specific-templates.md)
- [ ] Example output included in documentation

## Open Questions

None - all requirements are clearly defined.
