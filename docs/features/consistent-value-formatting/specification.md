# Feature: Consistent Value Formatting

## Overview

Improve readability and consistency across all Markdown output by code-formatting actual data values (what changes) while keeping labels, attribute names, and action indicators as plain text. This makes the important information—the values—visually prominent and easy to scan. Large-value sections also get a consistent heading style so collapsible blocks remain readable.

## User Goals

- Quickly identify actual values in plan reports because they stand out visually with code formatting
- Have consistent formatting across all output types (attribute tables, firewall rules, NSG rules, summaries)
- Improve readability by focusing visual emphasis on the data that matters most

## Scope

### In Scope

**1. Attribute Change Tables (default.sbn, role_assignment.sbn)**
- Attribute names: plain text (remove backticks)
- Attribute values: code-formatted (add backticks)
- Applies to:
  - CREATE tables (Attribute | Value)
  - DELETE tables (Attribute | Value)
  - UPDATE tables (Attribute | Before | After)

**2. Firewall Network Rule Collection Headers (firewall_network_rule_collection.sbn)**
- Collection name: code-formatted (add backticks)
- Priority value: code-formatted (already has backticks, keep them)
- Action value: code-formatted (already has backticks, keep them)

**3. Network Security Group Headers (network_security_group.sbn)**
- NSG name: code-formatted (wrap in backticks), consistent with firewall headers

**4. Firewall Rule Tables (firewall_network_rule_collection.sbn)**
- All data values code-formatted: rule name, protocols, addresses, ports, descriptions
- Action indicators remain plain: ➕, 🔄, ❌, ⏺️
- Applies to: added rules, modified rules, removed rules, unchanged rules, CREATE/DELETE lists

**5. NSG Security Rule Tables (network_security_group.sbn)**
- All data values code-formatted: name, priority, direction, access, protocol, addresses, ports, description
- Action indicators remain plain: ➕, 🔄, ❌, ⏺️
- Applies to: added rules, modified rules, removed rules, unchanged rules, CREATE/DELETE lists

**6. Enhanced Diff Formatting**
- Replace simple `format_diff` with styled diff formatting like `format_large_value`
- Support both inline-diff (HTML with character-level highlighting) and standard-diff (+/- markers)
- Respect the `--large-value-format` CLI option
- Apply code formatting to diff outputs
- Standard diff mode wraps individual before/after values in backticks; inline diff mode uses a `<code>` wrapper around styled spans for table safety
- Applies to: all `format_diff` calls in firewall and NSG templates

**7. Resource Summary Builder (ResourceSummaryBuilder.cs)**
- Resource names in summaries: code-formatted (already implemented, keep as-is)
- Other contextual values (resource groups, locations, tiers): code-formatted (changed from plain text)

**8. Large Value Headings**
- Large value sections use H5 headings with bold labels and trailing colon: `##### **<attribute_name>:**`
- Applies to collapsible large-value blocks across templates

### Out of Scope

- Changes to the summary table format (Action | Count | Resource Types)
- Changes to resource addresses in headings
- Changes to module names
- Changes to action symbols (➕, 🔄, ❌, ♻️, ⏺️)
- Changes to markdown bold/italic formatting (other than the specified large-value headings style)

## User Experience

### Current Behavior

Attribute names are code-formatted (backticks), values are plain text:

```markdown
| `location` | eastus |
| `account_tier` | Standard |
```

Firewall collection header has mixed formatting:

```markdown
**Collection:** `public-egress` | **Priority:** 110 | **Action:** Allow
```

Format_diff shows inline before/after with `-`/`+` markers but no styling:

```markdown
| 🔄 | allow-dns | UDP | - 10.1.1.0/24<br>+ 10.1.1.0/24, 10.1.2.0/24 | 168.63.129.16 | 53 | DNS to Azure |
```

### New Behavior

Attribute values are code-formatted, names are plain text:

```markdown
| location | `eastus` |
| account_tier | `Standard` |
```

Firewall collection header has all values code-formatted:

```markdown
**Collection:** `public-egress` | **Priority:** `110` | **Action:** `Allow`
```

Format_diff uses styled diff formatting matching `format_large_value`:

**Inline-diff format** (HTML with background colors):
```markdown
| 🔄 | `allow-dns` | `UDP` | <span style="background-color: #ffdddd;">10.1.1.0/24</span> <span style="background-color: #ddffdd;">10.1.1.0/24, 10.1.2.0/24</span> | `168.63.129.16` | `53` | `DNS to Azure` |
```

**Standard-diff format** (+/- markers, backtick-wrapped values):
```markdown
| 🔄 | `allow-dns` | `UDP` | - `10.1.1.0/24`<br>+ `10.1.1.0/24, 10.1.2.0/24` | `168.63.129.16` | `53` | `DNS to Azure` |
```

### Command Line

No new CLI options. The existing `--large-value-format` option controls the diff style:

```bash
# Use inline-diff (HTML styling, default)
terraform show -json plan.tfplan | tfplan2md

# Use standard-diff (+/- markers)
terraform show -json plan.tfplan | tfplan2md --large-value-format standard-diff
```

## Success Criteria

- [ ] All attribute names in change tables are plain text (no backticks)
- [ ] All attribute values in change tables are code-formatted (backticks)
- [ ] Firewall collection headers have all values code-formatted
- [ ] NSG headers have names code-formatted
- [ ] All firewall rule table values are code-formatted
- [ ] All NSG rule table values are code-formatted
- [ ] Action symbols remain plain (no backticks)
- [ ] `format_diff` produces styled output matching `format_large_value`
- [ ] Both inline-diff and standard-diff formats work correctly
- [ ] Existing tests updated to reflect new formatting
- [ ] New tests verify styled diff formatting
- [ ] Documentation examples show new formatting

## Open Questions

None at this time.
