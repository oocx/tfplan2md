# Azure DevOps Inline Diff - Final Design Options

This document contains refined design options for the Azure DevOps inline-diff format, based on our testing results.

**Context:** These options all use inline HTML styles which work in Azure DevOps. We know GitHub will strip the styles, so these are optimized for Azure DevOps users.

---

## Option A: Full Line Backgrounds (Simpler Implementation)

Shows entire lines with light backgrounds. No character-level highlighting.

<details>
<summary>Large attributes (1 attribute, 7 lines)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;rate-limit calls="20" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;rate-limit calls="50" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

</details>

**Pros:**
- Simple to implement (no character-level diff algorithm needed)
- Clean visual separation
- Good performance

**Cons:**
- Can't see exactly what changed within a line
- May highlight large sections for small changes

---

## Option B: Character-Level Highlighting (VS Code Style)

Shows line backgrounds with darker highlighting for specific changes within lines.

<details>
<summary>Large attributes (1 attribute, 7 lines)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;rate-limit calls="<span style="background-color: #ffc0c0; color: #24292e;">20</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;rate-limit calls="<span style="background-color: #acf2bd; color: #24292e;">50</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

</details>

**Pros:**
- Precisely shows what changed (like VS Code)
- Most informative for reviewers
- Professional appearance

**Cons:**
- Complex to implement (requires character-level diff)
- May be overkill for large blocks of new/deleted content

---

## Option C: Prefix Markers with Full Backgrounds

Uses `- ` and `+ ` prefixes combined with line backgrounds (hybrid approach).

<details>
<summary>Large attributes (1 attribute, 8 lines)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #d73a49; font-weight: bold;">-</span>   &lt;rate-limit calls="20" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #28a745; font-weight: bold;">+</span>   &lt;rate-limit calls="50" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #28a745; font-weight: bold;">+</span>   &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #28a745; font-weight: bold;">+</span>     &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #28a745; font-weight: bold;">+</span>   &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

</details>

**Pros:**
- Clear diff markers (familiar to developers)
- Works even if colors are muted/grayscale
- Easy to implement

**Cons:**
- More visual noise
- Takes up extra horizontal space

---

## Option D: Subtle Backgrounds with Border Accent

Light backgrounds with colored left border for emphasis.

<details>
<summary>Large attributes (1 attribute, 7 lines)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #fff5f5; color: #24292e; border-left: 3px solid #d73a49; display: block; padding-left: 8px; margin-left: -4px;">    &lt;rate-limit calls="20" renewal-period="90" /&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;rate-limit calls="50" renewal-period="90" /&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

</details>

**Pros:**
- Elegant and modern look
- Clear visual hierarchy
- Easy to scan

**Cons:**
- Border may not render consistently across platforms
- Lighter backgrounds may be too subtle for some users

---

## Option E: Full Backgrounds with Darker Text

Similar to Option A but with adjusted text color for better contrast.

<details>
<summary>Large attributes (1 attribute, 7 lines)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;rate-limit calls="20" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;rate-limit calls="50" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

</details>

**Pros:**
- Better contrast than Option A
- Simple implementation
- Clean appearance

**Cons:**
- No character-level detail
- Similar to Option A

---

## Option F: Alternating Line Style (Strikethrough for Deletions)

Shows deletions with strikethrough styling.

<details>
<summary>Large attributes (1 attribute, 7 lines)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #ffeef0; text-decoration: line-through; color: #6a737d; display: block; padding-left: 4px; margin-left: -4px;">    &lt;rate-limit calls="20" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;rate-limit calls="50" renewal-period="90" /&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

</details>

**Pros:**
- Clear visual indication of deletion
- Familiar pattern (strikethrough = removed)
- Distinct from additions

**Cons:**
- Harder to read struck-through text
- Not standard for code diffs

---

## Side-by-Side Comparison (Multiple Changes)

Here's how each option looks with a more complex example:

### Option A - Full Line Backgrounds

<details>
<summary>Large attributes (1 attribute, 15 lines)</summary>

### `inbound_nat_rules`

<pre style="font-family: monospace; line-height: 1.5;"><code>[
  {
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "name": "http",</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "name": "https",</span>
    "protocol": "Tcp",
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "frontend_port": 80,</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "frontend_port": 443,</span>
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "backend_port": 8080</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "backend_port": 8443,</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "idle_timeout_in_minutes": 10</span>
  },
  {
    "name": "ssh",
    "protocol": "Tcp",
    "frontend_port": 22,
    "backend_port": 22
  }
]</code></pre>

</details>

### Option B - Character-Level Highlighting

<details>
<summary>Large attributes (1 attribute, 15 lines)</summary>

### `inbound_nat_rules`

<pre style="font-family: monospace; line-height: 1.5;"><code>[
  {
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "name": "<span style="background-color: #ffc0c0; color: #24292e;">http</span>",</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "name": "<span style="background-color: #acf2bd; color: #24292e;">https</span>",</span>
    "protocol": "Tcp",
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "frontend_port": <span style="background-color: #ffc0c0; color: #24292e;">80</span>,</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "frontend_port": <span style="background-color: #acf2bd; color: #24292e;">443</span>,</span>
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "backend_port": <span style="background-color: #ffc0c0; color: #24292e;">8080</span></span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "backend_port": <span style="background-color: #acf2bd; color: #24292e;">8443</span>,</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;">    "idle_timeout_in_minutes": 10</span>
  },
  {
    "name": "ssh",
    "protocol": "Tcp",
    "frontend_port": 22,
    "backend_port": 22
  }
]</code></pre>

</details>

### Option C - Prefix Markers

<details>
<summary>Large attributes (1 attribute, 16 lines)</summary>

### `inbound_nat_rules`

<pre style="font-family: monospace; line-height: 1.5;"><code>[
  {
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #d73a49; font-weight: bold;">-</span>   "name": "http",</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #28a745; font-weight: bold;">+</span>   "name": "https",</span>
    "protocol": "Tcp",
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #d73a49; font-weight: bold;">-</span>   "frontend_port": 80,</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #28a745; font-weight: bold;">+</span>   "frontend_port": 443,</span>
<span style="background-color: #ffeef0; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #d73a49; font-weight: bold;">-</span>   "backend_port": 8080</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #28a745; font-weight: bold;">+</span>   "backend_port": 8443,</span>
<span style="background-color: #e6ffed; color: #24292e; display: block; padding-left: 4px; margin-left: -4px;"><span style="color: #28a745; font-weight: bold;">+</span>   "idle_timeout_in_minutes": 10</span>
  },
  {
    "name": "ssh",
    "protocol": "Tcp",
    "frontend_port": 22,
    "backend_port": 22
  }
]</code></pre>

</details>

---

## Evaluation Questions

When testing in Azure DevOps, please consider:

1. **Visual Clarity**: Which option makes changes easiest to spot?
2. **Readability**: Which option is easiest to read for both small and large changes?
3. **Implementation**: Option B requires character-diff algorithm - is it worth the complexity?
4. **Professionalism**: Which looks most polished in your PR reviews?
5. **Accessibility**: Which works best if you're colorblind or using different themes?

---

## Option G: Hybrid B+D - Character Highlighting with Border Accent (RECOMMENDED)

Combines character-level highlighting (Option B) with border accent (Option D) for maximum clarity.

<details>
<summary>Large attributes: policy (7 lines, 5 changed)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #fff5f5; color: #24292e; border-left: 3px solid #d73a49; display: block; padding-left: 8px; margin-left: -4px;">    &lt;rate-limit calls="<span style="background-color: #ffc0c0; color: #24292e;">20</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;rate-limit calls="<span style="background-color: #acf2bd; color: #24292e;">50</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

</details>

**Pros:**
- Best of both worlds: precise character-level diff + clear border emphasis
- Professional and elegant appearance
- Easy to scan for changes (border acts as visual guide)
- Character highlighting shows exact changes

**Cons:**
- Most complex to implement
- Border may not render on all platforms

---

## Option H: Multiple Attributes with Detailed Summary

Shows how the new summary format looks with multiple large attributes:

<details>
<summary>Large attributes: policy (125 lines, 87 changed), custom_data (15 lines, 8 changed)</summary>

### `policy`

<pre style="font-family: monospace; line-height: 1.5;"><code>&lt;policies&gt;
  &lt;inbound&gt;
<span style="background-color: #fff5f5; color: #24292e; border-left: 3px solid #d73a49; display: block; padding-left: 8px; margin-left: -4px;">    &lt;rate-limit calls="<span style="background-color: #ffc0c0; color: #24292e;">20</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;rate-limit calls="<span style="background-color: #acf2bd; color: #24292e;">50</span>" renewal-period="90" /&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;set-header name="X-Custom" exists-action="override"&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">      &lt;value&gt;custom-value&lt;/value&gt;</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">    &lt;/set-header&gt;</span>
  &lt;/inbound&gt;
&lt;/policies&gt;</code></pre>

### `custom_data`

<pre style="font-family: monospace; line-height: 1.5;"><code><span style="background-color: #fff5f5; color: #24292e; border-left: 3px solid #d73a49; display: block; padding-left: 8px; margin-left: -4px;">#!/bin/bash</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">#!/usr/bin/env bash</span>
apt-get update
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">apt-get install -y docker.io</span>
apt-get install -y nginx
<span style="background-color: #fff5f5; color: #24292e; border-left: 3px solid #d73a49; display: block; padding-left: 8px; margin-left: -4px;">apt-get install -y curl</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">systemctl enable docker</span>
<span style="background-color: #f0fff4; color: #24292e; border-left: 3px solid #28a745; display: block; padding-left: 8px; margin-left: -4px;">systemctl start docker</span></code></pre>

</details>

**Summary format explanation:**
- Lists each large attribute by name
- Shows total line count (before + after combined, deduplicated)
- Shows number of changed lines (lines that differ between before and after)
- Example: `policy (125 lines, 87 changed)` means 125 total lines with 87 having changes

---

## Option I: Complete Replacement (Separate Before/After)

When content changes completely (no common lines), show separate code blocks instead of inline diff:

<details>
<summary>Large attributes: connection_string (3 lines, 3 changed)</summary>

### `connection_string`

**Before:**
```
Server=tcp:old-server.database.windows.net,1433;Database=OldDB;User=oldadmin;Password=***;
```

**After:**
```
Server=tcp:new-server.database.windows.net,1433;Database=NewDB;User=newadmin;Password=***;Connection Timeout=30;Encrypt=True;
```

</details>

**When to use separate blocks:**
- All lines are different (no common lines between before and after)
- Makes it clearer that this is a complete replacement rather than incremental changes
- Easier to read than showing every line highlighted

---

## Recommendation

After reviewing the options, I lean toward **Option B (Character-Level Highlighting)** because:
- It provides the most information (exactly what changed)
- It matches VS Code's familiar UX
- It looks professional and modern
- The implementation complexity is justified by the value

However, **Option A** or **Option E** would be excellent simpler alternatives if implementation time is a concern.

## FINAL RECOMMENDATION (Based on User Feedback)

**Use Option G (Hybrid B+D)** with the following specifications:

1. **Visual Style**: Character-level highlighting with colored left border
   - Light backgrounds: `#fff5f5` (red) and `#f0fff4` (green)
   - Dark highlights: `#ffc0c0` (red) and `#acf2bd` (green)
   - Borders: `3px solid #d73a49` (red) and `3px solid #28a745` (green)
   - Text color: `#24292e` (dark gray, explicit to ensure readability in both light and dark modes)

2. **Summary Format**: 
   ```
   Large attributes: attr1 (X lines, Y changed), attr2 (A lines, B changed)
   ```
   - List all large attribute names
   - Show total line count (deduplicated union of before/after lines)
   - Show changed line count (lines that differ)

3. **Complete Replacement Logic**:
   - If no common lines exist between before and after, use separate code blocks (Option I)
   - Otherwise, use inline diff with character-level highlighting (Option G)

**Benefits:**
- Maximum information density (character-level changes)
- Clear visual hierarchy (border guides the eye)
- Intelligent handling of complete replacements
- Detailed summary helps users quickly assess scope
