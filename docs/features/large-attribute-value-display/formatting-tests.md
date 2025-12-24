# Large Attribute Value Display - Formatting Tests

This document contains various formatting options to test in Azure DevOps and GitHub.

## Test Scenario

Resource: `azurerm_api_management.example`  
Attribute: `policy` (100+ lines of XML)

---

## Option 1: Standard Diff Code Fence

<details>
<summary>Large attributes (1 attribute, 8 lines)</summary>

### `policy`

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

---

## Option 2: Separate Before/After Code Blocks

<details>
<summary>Large attributes (1 attribute, 8 lines)</summary>

### `policy`

**Before:**
```xml
<policies>
  <inbound>
    <rate-limit calls="20" renewal-period="90" />
  </inbound>
</policies>
```

**After:**
```xml
<policies>
  <inbound>
    <rate-limit calls="50" renewal-period="90" />
    <set-header name="X-Custom" exists-action="override">
      <value>custom-value</value>
    </set-header>
  </inbound>
</policies>
```

</details>

---

## Option 3: Inline HTML with Background Colors (Red/Green)

<details>
<summary>Large attributes (1 attribute, 8 lines)</summary>

### `policy`

<pre><code><span style="background-color: #ffdddd; color: #000000;">&lt;policies&gt;
  &lt;inbound&gt;
    &lt;rate-limit calls="20" renewal-period="90" /&gt;
  &lt;/inbound&gt;
&lt;/policies&gt;</span>

<span style="background-color: #ddffdd; color: #000000;">&lt;policies&gt;
  &lt;inbound&gt;
    &lt;rate-limit calls="50" renewal-period="90" /&gt;
    &lt;set-header name="X-Custom" exists-action="override"&gt;
      &lt;value&gt;custom-value&lt;/value&gt;
    &lt;/set-header&gt;
  &lt;/inbound&gt;
&lt;/policies&gt;</span></code></pre>

</details>

---

## Option 4: Table-Style Before/After (for shorter values)

<details>
<summary>Large attributes (1 attribute)</summary>

### `policy`

| Before | After |
|--------|-------|
| <pre>&lt;policies&gt;<br>&nbsp;&nbsp;&lt;inbound&gt;<br>&nbsp;&nbsp;&nbsp;&nbsp;&lt;rate-limit calls="20" /&gt;<br>&nbsp;&nbsp;&lt;/inbound&gt;<br>&lt;/policies&gt;</pre> | <pre>&lt;policies&gt;<br>&nbsp;&nbsp;&lt;inbound&gt;<br>&nbsp;&nbsp;&nbsp;&nbsp;&lt;rate-limit calls="50" /&gt;<br>&nbsp;&nbsp;&nbsp;&nbsp;&lt;set-header name="X-Custom"&gt;<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&lt;value&gt;custom-value&lt;/value&gt;<br>&nbsp;&nbsp;&nbsp;&nbsp;&lt;/set-header&gt;<br>&nbsp;&nbsp;&lt;/inbound&gt;<br>&lt;/policies&gt;</pre> |

</details>

---

## Option 5: Diff with Line Markers (+ and -)

<details>
<summary>Large attributes (1 attribute, 13 lines)</summary>

### `policy`

```
  <policies>
    <inbound>
-     <rate-limit calls="20" renewal-period="90" />
+     <rate-limit calls="50" renewal-period="90" />
+     <set-header name="X-Custom" exists-action="override">
+       <value>custom-value</value>
+     </set-header>
    </inbound>
  </policies>
```

</details>

---

## Option 6: HTML Details with Color Spans (Inline)

<details>
<summary>Large attributes (1 attribute, 8 lines)</summary>

### `policy`

<p><span style="background-color:#ffdddd;display:block;padding:2px 4px;font-family:monospace;">- &lt;policies&gt;<br/>- &nbsp;&nbsp;&lt;inbound&gt;<br/>- &nbsp;&nbsp;&nbsp;&nbsp;&lt;rate-limit calls="20" renewal-period="90" /&gt;<br/>- &nbsp;&nbsp;&lt;/inbound&gt;<br/>- &lt;/policies&gt;</span></p>

<p><span style="background-color:#ddffdd;display:block;padding:2px 4px;font-family:monospace;">+ &lt;policies&gt;<br/>+ &nbsp;&nbsp;&lt;inbound&gt;<br/>+ &nbsp;&nbsp;&nbsp;&nbsp;&lt;rate-limit calls="50" renewal-period="90" /&gt;<br/>+ &nbsp;&nbsp;&nbsp;&nbsp;&lt;set-header name="X-Custom" exists-action="override"&gt;<br/>+ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&lt;value&gt;custom-value&lt;/value&gt;<br/>+ &nbsp;&nbsp;&nbsp;&nbsp;&lt;/set-header&gt;<br/>+ &nbsp;&nbsp;&lt;/inbound&gt;<br/>+ &lt;/policies&gt;</span></p>

</details>

---

## Option 7: Hybrid - Table + Details for Large

Small attributes remain in table:

| Attribute | Before | After |
|-----------|--------|-------|
| `name` | api-mgmt-prod | api-mgmt-prod |
| `sku_name` | Developer_1 | Premium_1 |
| `location` | eastus | eastus |

<details>
<summary>Large attributes (1 attribute, 8 lines)</summary>

### `policy`

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

---

## Option 8: Create/Delete Operations (Single Value)

For create operations, only show the new value:

<details>
<summary>Large attributes (1 attribute, 7 lines)</summary>

### `policy`

```xml
<policies>
  <inbound>
    <rate-limit calls="50" renewal-period="90" />
    <set-header name="X-Custom" exists-action="override">
      <value>custom-value</value>
    </set-header>
  </inbound>
</policies>
```

</details>

---

## Option 9: VS Code-Style Inline Diff

<details>
<summary>Large attributes (1 attribute, 7 lines)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #ffdddd;">    &lt;rate-limit calls="<span style="background-color: #ffaaaa;">20</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #ddffdd;">    &lt;rate-limit calls="<span style="background-color: #aaffaa;">50</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #ddffdd;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #ddffdd;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #ddffdd;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

**Legend:**
- No background = unchanged line
- Light red background (#ffdddd) = deleted/old line
- Dark red background (#ffaaaa) = specific changed part within deleted line
- Light green background (#ddffdd) = added/new line
- Dark green background (#aaffaa) = specific changed part within added line

</details>

---

## Option 10: VS Code-Style Inline Diff (Alternative Colors)

<details>
<summary>Large attributes (1 attribute, 7 lines)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #ffeef0;">    &lt;rate-limit calls="<span style="background-color: #ffc0c0;">20</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed;">    &lt;rate-limit calls="<span style="background-color: #acf2bd;">50</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #e6ffed;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #e6ffed;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

**Legend:**
- No background = unchanged line
- Light red/pink (#ffeef0) = deleted/old line
- Medium red (#ffc0c0) = specific changed part within deleted line
- Light green (#e6ffed) = added/new line  
- Medium green (#acf2bd) = specific changed part within added line

</details>

---

## Option 11: VS Code-Style with Multiple Changes

<details>
<summary>Large attributes (1 attribute, 15 lines)</summary>

### `inbound_nat_rules`

<pre style="font-family: monospace; line-height: 1.5;"><code>[
  {
<span style="background-color: #ffeef0;">    "name": "<span style="background-color: #ffc0c0;">http</span>",</span>
<span style="background-color: #e6ffed;">    "name": "<span style="background-color: #acf2bd;">https</span>",</span>
    "protocol": "Tcp",
<span style="background-color: #ffeef0;">    "frontend_port": <span style="background-color: #ffc0c0;">80</span>,</span>
<span style="background-color: #e6ffed;">    "frontend_port": <span style="background-color: #acf2bd;">443</span>,</span>
<span style="background-color: #ffeef0;">    "backend_port": <span style="background-color: #ffc0c0;">8080</span></span>
<span style="background-color: #e6ffed;">    "backend_port": <span style="background-color: #acf2bd;">8443</span>,</span>
<span style="background-color: #e6ffed;">    "idle_timeout_in_minutes": 10</span>
  },
  {
    "name": "ssh",
    "protocol": "Tcp",
    "frontend_port": 22,
    "backend_port": 22
  }
]</code></pre>

**Legend:**
- Unchanged lines (no background): "protocol", "Tcp", structure elements
- Changed lines with specific diff highlighting within the line

</details>

---

## Test Instructions

1. Copy this entire document
2. Paste into an Azure DevOps PR comment
3. Note which options render correctly:
   - Does syntax highlighting work for diff/xml?
   - Do inline styles (background colors) render?
   - Are code blocks readable?
   - Which format is most intuitive?
4. Test the same in a GitHub issue/PR comment

## Evaluation Criteria

- ✅ Visual clarity (can you quickly see what changed?)
- ✅ Compatibility (renders on both GitHub and Azure DevOps)
- ✅ Readability (not too cluttered or hard to parse)
- ✅ Collapsible (doesn't overwhelm the page)
- ✅ Context preservation (maintains enough unchanged lines for context)
