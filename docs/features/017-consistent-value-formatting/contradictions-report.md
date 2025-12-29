# Style Guide vs Implementation Contradictions Report

## Summary

This report identifies specific contradictions between the newly created `docs/report-style-guide.md` and the actual implementation in templates and helpers.

---

## Contradiction #1: Small Value Diff Formatting

### Style Guide Says:
> **Small Values (In-Table)**
> 
> For modified attributes that fit within a table cell:
> 
> - **Format**: Stacked lines separated by `<br>`.
> - **Prefixes**: `- ` for the old value, `+ ` for the new value.
> - **Styling**: Values are code-formatted.
> 
> **Example:**
> ```markdown
> - `80`<br>+ `8080`
> ```

### Implementation Does:

The `FormatDiff` helper in `ScribanHelpers.cs` (lines 666-688) wraps the **entire diff output** in `<code>` tags (via `WrapInlineCode`), producing:

**Standard diff format:**
```markdown
<code>- 80<br>+ 8080</code>
```

**Inline diff format (with styled HTML spans):**
```markdown
<code><span style="...">10.0.1.0/24</span><br><span style="...">10.0.1.0/24, 10.0.3.0/24</span></code>
```

**Actual snapshot example** from `firewall-rules.md`:
```markdown
| 🔄 | `allow-http` | <code>TCP</code> | <code><span style="background-color: #fff5f5; ...">10.0.1.0/24</span><br><span style="background-color: #f0fff4; ...">10.0.1.0/24, 10.0.3.0/24</span></code> | ...
```

### The Difference:
- **Style guide**: Individual values wrapped in backticks: `` - `80`<br>+ `8080` ``
- **Implementation**: Entire diff wrapped in `<code>` tags: `<code>- 80<br>+ 8080</code>`

Both produce inline code formatting, but the syntax differs. The implementation's approach wraps the diff markers and line breaks inside the code block.

### Visual Comparison

**Option A: Style Guide Approach** (individual values in backticks)

| Status | Name | Protocol | Port |
|--------|------|----------|------|
| 🔄 | `allow-http` | `TCP` | - `80`<br>+ `8080` |

**Option B: Implementation Approach** (entire diff in `<code>` tags)

| Status | Name | Protocol | Port |
|--------|------|----------|------|
| 🔄 | `allow-http` | `TCP` | <code>- 80<br>+ 8080</code> |

**Option C: Implementation with Styled Spans** (actual inline diff with highlighting)

| Status | Name | Protocol | Source Address Prefix |
|--------|------|----------|----------------------|
| 🔄 | `allow-http` | `TCP` | <code><span style="background-color: #fff5f5; text-decoration: line-through;">10.0.1.0/24</span><br><span style="background-color: #f0fff4; font-weight: bold;">10.0.1.0/24, 10.0.3.0/24</span></code> |

---

## Contradiction #2: NSG Header Formatting

### Style Guide Says:
> **Resource-Specific Templates**
> 
> Custom templates (e.g., Firewall, NSG) must adhere to these guidelines:
> 
> 1. **Headers**: Use code formatting for dynamic values (e.g., `**Collection:** `public-egress``).

### Implementation Does:

The NSG template (`azurerm/network_security_group.sbn`, lines 5-9) renders:

```scriban
{{~ if after_json ~}}
**Network Security Group:** {{ after_json.name | escape_markdown }}
{{~ else if before_json ~}}
**Network Security Group:** {{ before_json.name | escape_markdown }}
{{~ end ~}}
```

This produces **plain text** for the NSG name (no backticks):
```markdown
**Network Security Group:** nsg-app
```

By contrast, the **firewall template** (`azurerm/firewall_network_rule_collection.sbn`) does code-format its header values:
```scriban
**Collection:** `{{ after_json.name | escape_markdown }}` | **Priority:** `{{ after_json.priority | escape_markdown }}` | **Action:** `{{ after_json.action | escape_markdown }}`
```

Producing:
```markdown
**Collection:** `public-egress` | **Priority:** `110` | **Action:** `Allow`
```

### The Difference:
- **Style guide**: Says resource-specific headers should code-format dynamic values
- **NSG implementation**: Renders NSG name as plain text (inconsistent with firewall)
- **Firewall implementation**: Follows the style guide (code-formats collection name, priority, action)

This appears to be an intentional design choice per the original feature spec for NSG templates, which stated: "Headers use plain text for names" (see `docs/features/015-nsg-security-rule-template/specification.md`).

### Visual Comparison

**Option A: NSG Current Implementation** (plain text name)

**Network Security Group:** nsg-app

---

**Option B: Style Guide Approach** (code-formatted name like firewall)

**Network Security Group:** `nsg-app`

---

**For context, Firewall template currently uses:**

**Collection:** `public-egress` | **Priority:** `110` | **Action:** `Allow`

---

## Recommendation

For each contradiction, we need to decide:
1. **Update the style guide** to match current implementation (documenting the actual behavior)
2. **Update the implementation** to match the style guide (changing code/templates to follow the documented standard)

The choice depends on which approach better serves readability, consistency, and user expectations.

---

## Resolution Decisions

### Contradiction #1: RESOLVED

**Decision:** Update implementation to match style guide approach
- **Standard diff mode**: Use Option A (individual values in backticks: `` - `80`<br>+ `8080` ``)
- **Inline diff mode**: Keep Option C (entire diff in `<code>` tags with styled spans)

**Rationale:** Different diff modes should use different formatting - standard diffs are simpler and benefit from backtick-wrapped values, while inline diffs use styled HTML spans that need the outer `<code>` wrapper.

### Contradiction #2: RESOLVED

**Decision:** Update implementation to Option B (code-format NSG names)

**Rationale:** Consistency with firewall template - all resource-specific headers should code-format dynamic values like names, priorities, and actions.

---

## Implementation Requirements

### Task 1: Update `BuildStandardDiffTable` Method
- **File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs` (line 212)
- **Change:** Modify to return `` - `{escapedBefore}`<br>+ `{escapedAfter}` `` instead of `- {escapedBefore}<br>+ {escapedAfter}`
- **Impact:** Standard diff mode will wrap individual values in backticks

### Task 2: Update `FormatDiff` Method
- **File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs` (line 685)
- **Change:** For `StandardDiff` format, do NOT call `WrapInlineCode` (since values now have backticks)
- **Impact:** Prevents double-wrapping of standard diffs

### Task 3: Update NSG Template Header
- **File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn` (lines 5-9)
- **Change:** Wrap NSG name in backticks: `` `{{ after_json.name | escape_markdown }}` ``
- **Impact:** NSG headers will match firewall template formatting

### Task 4: Update Style Guide
- **File:** `docs/report-style-guide.md`
- **Change:** No changes needed - current examples already show correct behavior for both modes

### Task 5: Regenerate Test Snapshots
- **Action:** Run test suite and regenerate snapshots to capture new formatting
- **Files affected:** All snapshot files in `tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/`
