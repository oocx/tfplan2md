# Feature: Large Attribute Value Display

## Overview

Currently, all attribute values are displayed in tables, which works well for short values but breaks down when values are large (multi-line content or very long single-line values). This feature introduces alternative presentation formats for large attribute values while keeping small attributes in their familiar table format.

## User Goals

- Quickly scan resource changes without being overwhelmed by large values
- View large attribute values (like Azure API Management policies with 100+ lines) in a readable format
- See clear before/after diffs for large values in update/replace operations
- Choose between Azure DevOps-optimized (inline diff with colors) and GitHub-compatible (standard diff) formats

## Scope

### In Scope

**Detection of Large Values:**
- Automatic detection based on value size:
  - Multi-line values (containing `\n`, `\r`, or `\r\n`)
  - Single-line values exceeding 100 characters

**Hybrid Display Format:**
- Small attributes remain in the existing table format
- Large attributes are moved to a separate collapsible `<details>` section below the table
- All large attributes for a resource are grouped in a single collapsible section

**Collapsible Section Format:**
- Summary line shows:
  - Count of large attributes
  - Total line count across all large values
  - Example: `Large attributes (2 attributes, 147 lines)`
- Each large attribute displayed with:
  - Attribute name as a heading (`### attribute_name`)
  - Value(s) in code blocks

**Display Formats for Large Values:**

**Create/Delete operations (single value):**
- Display the value in a code block with appropriate syntax highlighting
- No before/after comparison needed

**Update/Replace operations (before and after values):**
- Two format options controlled by `--large-value-format` CLI option:
  
  1. **`inline-diff`** (default) - Azure DevOps optimized:
     - Character-level diff highlighting with colored left border (hybrid of visual clarity + precision)
     - Light backgrounds: `#fff5f5` (deleted) and `#f0fff4` (added)
     - Dark character-level highlights: `#ffc0c0` (deleted parts) and `#acf2bd` (added parts)
     - Colored left borders: `3px solid #d73a49` (red) and `3px solid #28a745` (green)
     - Text color: `#24292e` (dark gray, explicit to ensure readability in both light and dark modes)
     - Unchanged lines shown without background colors or borders for context
     - Works beautifully in Azure DevOps, degrades gracefully in GitHub (colors stripped but content readable)
     - **Complete replacement logic**: If no common lines exist between before and after, use separate "Before:" and "After:" code blocks instead of inline diff
  
  2. **`standard-diff`** - Cross-platform compatible:
     - Standard diff code fence with `diff` syntax highlighting
     - Uses `-` prefix for deleted lines (shows in red via syntax highlighting)
     - Uses `+` prefix for added lines (shows in green via syntax highlighting)
     - Works reliably on both GitHub and Azure DevOps

**Summary Line Format:**
```
Large values: attr1 (X lines, Y changed), attr2 (A lines, B changed)
```
- Lists each large attribute by name
- Shows total line count (deduplicated union of before/after lines)
- Shows changed line count (lines that differ between before and after)
- Example: `policy (125 lines, 87 changed)` means 125 total lines with 87 lines having changes

**CLI Option:**
- New option: `--large-value-format <format>`
- Valid values: `inline-diff` (default), `standard-diff`
- Default behavior: `inline-diff` for best Azure DevOps experience

### Out of Scope

- Configurable threshold for what constitutes a "large" value (hardcoded at 100 characters)
- Unified diff format with limited context lines (full before/after values always shown)
- Custom color schemes (fixed Azure DevOps-compatible palette)
- Syntax detection and language-specific highlighting for inline-diff mode (uses plain monospace text)
- Collapsing small attributes section (table remains expanded)
- Advanced diff algorithms beyond simple line-by-line and word-level comparison

## User Experience

### Command Line

```bash
# Default behavior (inline-diff for Azure DevOps)
tfplan2md plan.json

# GitHub-compatible mode
tfplan2md plan.json --large-value-format standard-diff

# Azure DevOps-optimized mode (explicit)
tfplan2md plan.json --large-value-format inline-diff
```

### Example Output - Update Operation with Large Values

#### 🔄 azurerm_api_management.example

<details>

| Attribute | Before | After |
|-----------|--values: policy (7 lines, 5 changed)</summary>

### `policy`

**Inline-diff mode (default):**

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">    &lt;rate-limit calls="<span style="background-color: #ffc0c0;">20</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">    &lt;rate-limit calls="<span style="background-color: #acf2bd;">50</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

**Standard-diff mode:**

```diff
- <policies>
-   <inbound>
-     <rate-limit calls="20" renewal-period="90" />
-   </inbound>
- </policies>
+ <policies>
+   <inbound>
+     <rate-limit calls="50" renewal-period="90" />
+     <set-header name="X-Custom" exists-action="override">
+       <value>custom-value</value>
+     </set-header>
+   </inbound>
+ </policies>
```

</details>

### Example Output - Create Operation with Large Value

#### ➕ azurerm_key_vault_secret.connection_string

<details>

| Attribute | Value |
|-----------|-------|
| `name` | sql-connection |
| `key_vault_id` | /subscriptions/.../vaults/kv-prod |
| `content_type` | connection-string |

</details>

<details>
<summary>Large values: value (1 line)</summary>

### `value`

```
Server=tcp:sqlserver-prod.database.windows.net,1433;Initial Catalog=AppDB;Persist Security Info=False;User ID=appadmin;Password=***;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

</details>

### Example Output - Multiple Large Attributes

#### 🔄 azurerm_linux_virtual_machine.app_server

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | vm-app-01 | vm-app-01 |
| `size` | Standard_D2s_v3 | Standard_D4s_v3 |
| `location` | eastus | eastus |

</details>

<details>
<summary>Large values: custom_data (7 lines, 3 changed), user_data (6 lines, 3 changed)</summary>

### `custom_data`

```diff
- #!/bin/bash
- apt-get update
- apt-get install -y nginx
+ #!/bin/bash
+ apt-get update
+ apt-get install -y nginx docker.io
+ systemctl enable docker
+ systemctl start docker
```

### `user_data`

```diff
- #cloud-config
- packages:
-   - curl
+ #cloud-config
+ packages:
+   - curl
+   - wget
+   - jq
```

</details>

### Example Output - Complete Replacement (No Common Lines)

#### 🔄 azurerm_key_vault_secret.db_connection

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | db-connection | db-connection |
| `content_type` | connection-string | connection-string |

</details>

<details>
<summary>Large values: value (2 lines, 2 changed)</summary>

### `value`

**Before:**
```
Server=tcp:old-server.database.windows.net,1433;Database=OldDB;User=oldadmin;Password=***;
```

**After:**
```
Server=tcp:new-server.database.windows.net,1433;Database=NewDB;User=newadmin;Password=***;Connection Timeout=30;Encrypt=True;
```

</details>

## Success Criteria

- [ ] Values with newlines are automatically treated as large
- [ ] Values exceeding 100 characters (single line) are automatically treated as large
- [ ] Small attributes continue to display in tables unchanged
- [ ] Large attributes are grouped in a single collapsible section per resource
- [ ] Summary line accurately shows count and total line count
- [ ] `--large-value-format inline-diff` produces VS Code-style output with HTML colors
- [ ] `--large-value-format standard-diff` produces standard diff code fences
- [ ] Default format is `inline-diff`
- [ ] Inline-diff format renders correctly in Azure DevOps with colors
- [ ] Standard-diff format renders correctly on both GitHub and Azure DevOps
- [ ] Create/delete operations show single value in code block
- [ ] Update/replace operations show before/after comparison
- [ ] All markdown output passes markdownlint validation
- [ ] Sensitive values in large attributes are masked unless `--show-sensitive` is used
- [ ] Existing tests remain passing with small values in tables
- [ ] New tests cover large value detection and formatting

## Open Questions

None - all requirements clarified through testing and discussion.
