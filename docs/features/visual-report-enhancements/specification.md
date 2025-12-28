# Feature: Visual Report Enhancements

## Overview

Improve the visual appearance of generated Terraform plan reports to enhance readability, professional appearance, and ability to quickly identify important information. The current reports can appear as a "wall of text" and benefit from strategic use of visual elements like icons, spacing, and formatting to make them more scannable and aesthetically pleasing.

## User Goals

- **Professional Appearance**: Reports should look polished and well-structured, not like dense blocks of text
- **Improved Readability**: Users should be able to quickly scan and find relevant information
- **Highlight Important Areas**: Critical information (security rules, IP addresses, changes) should stand out visually
- **Aesthetic Quality**: Reports should be visually pleasing while maintaining GitHub/Azure DevOps markdown compatibility

## Scope

### In Scope

1. **Module-Level Visual Enhancements**
   - Add horizontal rules (`---`) between modules for clear separation
   - Add module icon (📦) before module paths in headers

2. **Resource Display Format**
   - Collapse all resource details into `<details>` tags with summary lines
   - Add `<br>` spacing after summary tags before table content
   - Maintain action icons (➕, 🔄, ♻️, ❌) in summaries

3. **Resource Type and Name Formatting**
   - Resource type: Display as plain text
   - Resource name: Display in **bold** and `<code>` tags (e.g., **`hub`**)
   - Clear visual separation between type and name

4. **Location Display**
   - Format locations as: `(🌍 location)` where location is the region name
   - Use earth globe icon (🌍) to denote geographical regions

5. **IP Addresses and CIDR Blocks**
   - Add network globe icon (🌐) INSIDE code blocks
   - Format as: `🌐 10.0.0.0/16`
   - Apply consistently in summaries and table cells

6. **Security Rule Actions**
   - Allow rules: `✅ Allow` (checkmark + text)
   - Deny rules: `⛔ Deny` (prohibition sign + text)

7. **Boolean Values**
   - True: `✅ true` (checkmark + text)
   - False: `❌ false` (cross mark + text)

8. **Network Direction**
   - Inbound: `⬇️ Inbound` (down arrow + text)
   - Outbound: `⬆️ Outbound` (up arrow + text)

9. **Network Protocols**
   - TCP: `🔗 TCP` (link icon + text)
   - UDP: `📨 UDP` (envelope icon + text)
   - ICMP: `📡 ICMP` (satellite icon + text)
   - Any/All: `✳️ *` (asterisk icon + asterisk)

10. **Tags Display**
    - Display tags as inline badges below the main attributes table
    - Format: `**🏷️ Tags:** `key: value` `key: value` `key: value``
    - Each key-value pair in separate code backticks

11. **Changed Attributes List**
    - Show count and icon: `2 🔧 attribute1, attribute2`
    - Format: number (space) wrench icon (space) comma-separated attribute names
    - No parentheses around count or icon

### Out of Scope

- Custom CSS or styling beyond standard markdown/HTML
- Platform-specific markdown extensions (must work on both GitHub and Azure DevOps)
- Changes to the summary statistics tables
- Changes to the overall report structure or sections
- Interactive elements beyond native `<details>`/`<summary>` tags

## User Experience

### Module Separation
Users will see clear visual breaks between modules using horizontal rules, making it easier to distinguish where one module ends and another begins.

**Example:**
```markdown
---

### 📦 Module: ./modules/networking
```

### Resource Entries
Each resource will appear as a collapsible section with a descriptive summary line containing:
- Action icon (➕, 🔄, ♻️, ❌)
- Resource type (plain text)
- Resource name (bold + code)
- Key identifying attributes (name, location, IP ranges, etc.)
- Changed attributes (for update operations)

**Example Summary Lines:**
```markdown
<details>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code> in <code>rg-demo</code> (🌍 eastus) | <code>🌐 10.0.0.0/16</code></summary>

<details>
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>stdata</code> | 2 🔧 account_replication_type, tags.cost_center</summary>

<details>
<summary>➕ azurerm_network_security_rule <b><code>allow_https[2]</code></b> — <code>AllowHTTPS</code> | ✅ Allow | ⬇️ Inbound | 🔗 TCP | Priority 100</summary>
```

### Value Formatting in Tables
Within the expanded details, users will see enhanced formatting for specific value types:

**Security and Network Values:**
- `access: ✅ Allow` or `access: ⛔ Deny`
- `direction: ⬇️ Inbound` or `direction: ⬆️ Outbound`
- `protocol: 🔗 TCP`, `protocol: 📨 UDP`, `protocol: 📡 ICMP`, `protocol: ✳️ *`

**Boolean Values:**
- `enabled: ✅ true`
- `https_only: ❌ false`

**IP Addresses:**
- `address_space[0]: `🌐 10.0.0.0/16``
- `source_address_prefix: `🌐 10.1.0.0/16``

**Tags:**
```markdown
**🏷️ Tags:** `environment: production` `owner: devops` `cost_center: 1234`
```

### Error Handling
- If icons fail to render, the text labels remain readable (icon + space + text pattern)
- Markdown compatibility issues fall back to standard code formatting

## Success Criteria

- [x] All visual enhancements have been reviewed with rendered examples in markdown viewers
- [ ] Module separators (horizontal rules) are added between all modules
- [ ] Module icons (📦) appear before all module paths
- [ ] All resource entries are collapsed into `<details>` tags with proper spacing
- [ ] Resource types display as plain text and resource names as **bold** `code`
- [ ] Location icons (🌍) appear in format `(🌍 location)` for all geographical locations
- [ ] IP address/CIDR icons (🌐) appear inside code blocks for all network addresses
- [ ] Security rule actions display with appropriate icons (✅/⛔) and text
- [ ] Boolean values display with checkmark/cross (✅/❌) and text
- [ ] Network direction displays with arrows (⬇️/⬆️) and text
- [ ] Protocol values display with appropriate icons (🔗/📨/📡/✳️) and text
- [ ] Tags appear as inline badges with 🏷️ icon and separate code blocks per tag
- [ ] Changed attributes display as `count 🔧 attributes` format
- [ ] Reports render correctly in both GitHub and Azure DevOps markdown viewers
- [ ] All enhancements follow the "Data is Code, Labels are Text" styling philosophy
- [ ] Existing tests are updated to match new formatting

## Open Questions

None - all visual enhancement decisions have been finalized through rendered example comparisons.

## Implementation Notes

### Icon Placement Consistency
The following pattern must be applied consistently:
- Icons for **data values** (IP addresses, regions) go INSIDE code blocks: `` `🌐 10.0.0.0/16` ``
- Icons for **labels/categories** (location prefix, tags prefix) go OUTSIDE code: `(🌍 eastus)` or `**🏷️ Tags:**`
- Icons for **actions/attributes** (booleans, protocols, rules) combine icon + text as the complete value

### HTML Tag Requirements
When using markdown inside HTML `<summary>` tags, use HTML `<code>` tags instead of markdown backticks for cross-platform compatibility (especially Azure DevOps).

### Spacing
- Use `<br>` for vertical spacing after `<summary>` tags before table content
- Use horizontal rules (`---`) for module-level separation
- Maintain consistent spacing patterns throughout the report

## Reference Documents

- [spacing-options-comparison.md](spacing-options-comparison.md) - Module and resource spacing decisions
- [resource-display-options.md](resource-display-options.md) - Resource layout format decisions
- [resource-type-formatting-options.md](resource-type-formatting-options.md) - Type vs name formatting decisions
- [additional-visual-options.md](additional-visual-options.md) - IP addresses, tags, and changed attributes decisions
