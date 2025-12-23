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

- [x] Template created for `azurerm_network_security_group` resource type
- [x] Rules categorized correctly as Added, Modified, Removed, or Unchanged using `diff_array` helper with `name` as the key
- [x] Rules sorted by ascending priority within the rendered table
- [x] All specified columns displayed: Name, Priority, Direction, Access, Protocol, Source Addresses, Source Ports, Destination Addresses, Destination Ports, Description
- [x] Modified rules show before/after values with `-` and `+` prefixes for changed attributes
- [x] Unchanged attributes in modified rules show single value without prefix
- [x] Multi-value fields (address prefixes, port ranges) rendered as comma-separated lists
- [x] Template uses `format_diff` helper for displaying changed attributes
- [x] All existing tests pass
- [x] New tests verify NSG rule rendering with added/modified/removed/unchanged rules
- [x] Documentation updated in [docs/features.md](../features.md) and [docs/features/resource-specific-templates.md](resource-specific-templates.md)
- [x] Example output included in documentation

## Implementation Notes

### Template Design

The template is implemented at `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn` and uses:

1. **Helper Functions**: Four template-defined functions handle the singular/plural field precedence:
   - `source_addresses(rule)`: Returns source address prefix(es) or `*`
   - `destination_addresses(rule)`: Returns destination address prefix(es) or `*`
   - `source_ports(rule)`: Returns source port range(s) or `*`
   - `destination_ports(rule)`: Returns destination port range(s) or `*`

2. **Scriban `ret` Statement**: Functions use the `ret` statement to return clean string values without introducing whitespace into the output.

3. **Whitespace Control**: The template uses normal `{{ }}` delimiters (not `{{~ ~}}`) for table row loops to preserve proper newlines between rows and prevent table breakage.

4. **Three Rendering Modes**:
   - **Update mode**: Shows the full diff table with all four categories (Added, Modified, Removed, Unchanged)
   - **Create mode**: Shows a simpler table without the Change column (no icons needed)
   - **Delete mode**: Shows "Security Rules (being deleted)" with all rules listed
   - **Fallback**: Falls back to default attribute table if no security_rule array exists

### Testing

Comprehensive test coverage was added:

- **Unit tests** (`MarkdownRendererNsgTemplateTests.cs`):
  - Create, update, and delete scenarios
  - Priority-based sorting verification
  - Singular/plural field handling
  - Semantic diff verification

- **Integration tests** (`TemplateIsolationTests.cs`):
  - Template validity checks
  - Markdown structure validation (no blank lines between table rows)

- **Test data** (`nsg-rule-changes.json`):
  - Covers all four rule states (Added, Modified, Removed, Unchanged)
  - Tests both singular and plural address/port fields
  - Includes realistic NSG configurations

### Differences from Firewall Template

While similar in structure to the firewall rule template, the NSG template has some key differences:

1. **More columns**: NSG rules have separate source/destination addresses and ports (8 data columns vs 5 for firewall rules)
2. **Field handling**: NSG rules use both singular and plural forms for addresses and ports, requiring helper functions
3. **No nested structures**: NSG rules are simpler objects without nested application/fqdn target groups

## Open Questions

None - all requirements are clearly defined and implemented.
