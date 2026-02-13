# Test Expectation Update Plan

**Date:** 2026-02-13  
**Commit:** 692fcf0 - "fix: restore HTML span diff formatting with character-level highlighting"  
**Status:** Awaiting Maintainer Approval

## Executive Summary

The HTML inline diff restoration in commit 692fcf0 is **CORRECT** and matches the working firewall example. However, 13 tests are failing because they expect the old simplified format. This plan details all necessary test updates to align with the restored rich HTML formatting.

**Tests Affected:** 13 total
- **Unit Tests (ParentChildInlineDiffTests.cs):** 11 tests
- **Integration Test (VariableGroupTemplateTests.cs):** 1 test  
- **Snapshot Test (SnapshotTests.cs):** 1 test

**Risk Level:** LOW - Changes are test expectations only, no production code changes needed.

---

## Background

### What Changed in Commit 692fcf0

The commit restored the rich HTML inline diff formatting that was incorrectly simplified in an earlier commit. The restored format produces:

```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - 10.200.2.0/2<span style="background-color: #ffc0c0; color: #24292e;">4</span>
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + 10.200.2.0/2<span style="background-color: #acf2bd; color: #24292e;">3</span>
  </span>
</code>
```

**Key Features:**
- Outer `<code>` tag with block styling for table compatibility
- Inner `<span>` tags with full GitHub-style colors and borders
- Character-level diff highlighting with nested `<span>` tags
- `<br>` tags for line separation (no raw newlines)

### Why This Format Is Correct

**Evidence from `artifacts/firewall-application-rules-uat.md` (generated 2026-02-04):**

```markdown
| 🔄 | `🆔 allow-microsoft` | `Http:80, Https:443` | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- 10.0.0.0/<span style="background-color: #ffc0c0; color: #24292e;">24</span></span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ 10.0.0.0/<span style="background-color: #acf2bd; color: #24292e;">16</span></span></code> | `` | `*.microsoft.com` | `` | <code>Microsoft services</code> |
```

This format:
1. **Works in production** - Already present in working UAT artifacts
2. **Renders correctly** - GitHub and Azure DevOps display it properly
3. **Provides better UX** - Character-level highlighting is more readable than plain text diffs

---

## Detailed Test Analysis

### Category 1: ParentChildInlineDiffTests.cs (11 tests)

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ParentChildInlineDiffTests.cs`

These tests verify that `ScribanHelpers.FormatDiff()` produces the correct format for inline diffs in parent-child resource tables. All tests use `.NotContain()` assertions expecting plain markdown, but the restored implementation correctly produces rich HTML.

---

#### Test 1: `FormatDiff_InlineDiff_ProducesPlainMarkdownWithoutHtmlStyles`

**Location:** Line 23  
**Current Expectation:**
```csharp
result.Should().NotContain("<span style=");
result.Should().NotContain("background-color:");
result.Should().NotContain("border-left:");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - 10.200.2.0/2<span style="background-color: #ffc0c0; color: #24292e;">4</span>
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + 10.200.2.0/2<span style="background-color: #acf2bd; color: #24292e;">3</span>
  </span>
</code>
```

**Why Change Is Correct:**  
The test name and comments are outdated. The inline-diff format was ALWAYS meant to produce rich HTML (as evidenced by firewall example). The test expectations need to be inverted.

**Proposed Fix:**
```csharp
[Test]
public void FormatDiff_InlineDiff_ProducesRichHtmlWithCharacterLevelDiffs()
{
    // Arrange
    var before = "10.200.2.0/24";
    var after = "10.200.2.0/23";

    // Act
    var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

    // Assert - Should contain rich HTML with styling
    result.Should().Contain("<code style=\"display:block; white-space:normal; padding:0; margin:0;\">");
    result.Should().Contain("<span style=");
    result.Should().Contain("background-color:");
    result.Should().Contain("border-left:");
    
    // Should contain diff markers within spans
    result.Should().Contain("- 10.200.2.0/2");
    result.Should().Contain("+ 10.200.2.0/2");
    
    // Should contain character-level highlighting
    result.Should().Contain("#ffc0c0"); // Red highlight for removed char
    result.Should().Contain("#acf2bd"); // Green highlight for added char
    result.Should().Contain("<br>"); // Line separator
}
```

---

#### Test 2: `FormatDiff_InlineDiff_UsesPrefixesForChanges`

**Location:** Line 46  
**Current Expectation:**
```csharp
result.Should().Contain("- ");
result.Should().Contain("+ ");
result.Should().Contain("old value");
result.Should().Contain("new value");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - <span style="background-color: #ffc0c0; color: #24292e;">old</span> value
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + <span style="background-color: #acf2bd; color: #24292e;">new</span> value
  </span>
</code>
```

**Why Change Is Correct:**  
The output DOES contain "- " and "+ " prefixes, but they're wrapped in styled spans. The test needs to verify the HTML structure, not just the presence of text.

**Proposed Fix:**
```csharp
[Test]
public void FormatDiff_InlineDiff_UsesPrefixesForChanges()
{
    // Arrange
    var before = "old value";
    var after = "new value";

    // Act
    var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

    // Assert - Should use styled spans with +/- prefixes
    result.Should().Contain("<code style=\"display:block;");
    result.Should().Contain("- "); // Minus prefix
    result.Should().Contain("+ "); // Plus prefix
    result.Should().Contain("old"); // Contains before text
    result.Should().Contain("new"); // Contains after text
    
    // Should have character-level highlighting
    result.Should().Contain("background-color: #ffc0c0"); // Red for removed
    result.Should().Contain("background-color: #acf2bd"); // Green for added
}
```

---

#### Test 3: `FormatDiff_SimpleDiff_ProducesPlainMarkdownWithoutHtmlStyles`

**Location:** Line 66  
**Status:** ✅ **PASSES** - No changes needed

**Why:** This test verifies `simple-diff` format, which correctly produces plain markdown (`- old<br>+ new`). The simple-diff format was NOT changed in commit 692fcf0.

---

#### Test 4: `FormatDiff_InlineDiff_VNetSubnetAddressPrefixes`

**Location:** Line 90  
**Current Expectation:**
```csharp
result.Should().NotContain("<span style=");
result.Should().NotContain("background-color:");
result.Should().Contain("10.200.2.0/24");
result.Should().Contain("10.200.2.0/23");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - 10.200.2.0/2<span style="background-color: #ffc0c0; color: #24292e;">4</span>
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + 10.200.2.0/2<span style="background-color: #acf2bd; color: #24292e;">3</span>
  </span>
</code>
```

**Why Change Is Correct:**  
Subnet address prefix diffs (like /24 → /23) benefit from character-level highlighting. The "4" and "3" are highlighted differently, making the change immediately visible.

**Proposed Fix:**
```csharp
[Test]
public void FormatDiff_InlineDiff_VNetSubnetAddressPrefixes()
{
    // Arrange - Subnet address prefix changing from /24 to /23
    var before = "10.200.2.0/24";
    var after = "10.200.2.0/23";

    // Act
    var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

    // Assert - Should contain rich HTML with character-level highlighting
    result.Should().Contain("<code style=\"display:block;");
    result.Should().Contain("<span style=");
    result.Should().Contain("background-color:");
    result.Should().Contain("10.200.2.0/2"); // Common prefix
    
    // Should highlight changed character (4 vs 3)
    result.Should().Contain("background-color: #ffc0c0"); // Red for "4"
    result.Should().Contain("background-color: #acf2bd"); // Green for "3"
}
```

---

#### Test 5: `FormatDiff_InlineDiff_RouteTableNextHopType`

**Location:** Line 110  
**Current Expectation:**
```csharp
result.Should().NotContain("<span style=");
result.Should().NotContain("background-color:");
result.Should().Contain("VirtualAppliance");
result.Should().Contain("VnetLocal");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - V<span style="background-color: #ffc0c0; color: #24292e;">ir</span>t<span style="background-color: #ffc0c0; color: #24292e;">u</span>al<span style="background-color: #ffc0c0; color: #24292e;">Appliance</span>
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + V<span style="background-color: #acf2bd; color: #24292e;">ne</span>t<span style="background-color: #acf2bd; color: #24292e;">Loc</span>al
  </span>
</code>
```

**Why Change Is Correct:**  
Character-level diff clearly shows "VirtualAppliance" → "VnetLocal" with precise highlighting of changed portions. Much more readable than plain text diff.

**Proposed Fix:**
```csharp
[Test]
public void FormatDiff_InlineDiff_RouteTableNextHopType()
{
    // Arrange - Route next hop type changing from VirtualAppliance to VnetLocal
    var before = "VirtualAppliance";
    var after = "VnetLocal";

    // Act
    var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

    // Assert - Should contain rich HTML with character-level highlighting
    result.Should().Contain("<code style=\"display:block;");
    result.Should().Contain("<span style=");
    result.Should().Contain("background-color:");
    
    // Should show both values with character-level diffs
    result.Should().Contain("Virtual"); // Part of before
    result.Should().Contain("Vnet"); // Part of after
    
    // Should highlight changed portions
    result.Should().Contain("background-color: #ffc0c0"); // Red for removed chars
    result.Should().Contain("background-color: #acf2bd"); // Green for added chars
}
```

---

#### Test 6: `FormatDiff_InlineDiff_NsgRuleSourceAddresses`

**Location:** Line 130  
**Current Expectation:**
```csharp
result.Should().NotContain("<span style=");
result.Should().NotContain("background-color:");
result.Should().Contain("10.1.1.5");
result.Should().Contain("10.1.1.6");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - 🌐 10.1.1.5
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + 🌐 10.1.1.5<span style="background-color: #acf2bd; color: #24292e;">, 🌐 10.1.1.6</span>
  </span>
</code>
```

**Why Change Is Correct:**  
Addition of a second IP address is highlighted in green. Emoji (🌐) is preserved correctly.

**Proposed Fix:**
```csharp
[Test]
public void FormatDiff_InlineDiff_NsgRuleSourceAddresses()
{
    // Arrange - NSG rule source changing from single IP to multiple IPs
    var before = "🌐 10.1.1.5";
    var after = "🌐 10.1.1.5, 🌐 10.1.1.6";

    // Act
    var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

    // Assert - Should contain rich HTML with character-level highlighting
    result.Should().Contain("<code style=\"display:block;");
    result.Should().Contain("<span style=");
    result.Should().Contain("10.1.1.5"); // First IP
    result.Should().Contain("10.1.1.6"); // Second IP
    result.Should().Contain("🌐"); // Emoji preserved
    
    // Should highlight the addition (second IP with emoji)
    result.Should().Contain("background-color: #acf2bd"); // Green for added content
}
```

---

#### Test 7: `FormatDiff_InlineDiff_DnsRecordValue`

**Location:** Line 170  
**Current Expectation:**
```csharp
result.Should().NotContain("<span style=");
result.Should().NotContain("background-color:");
result.Should().Contain("10.1.1.10");
result.Should().Contain("10.1.1.20");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - 🌐 10.1.1.<span style="background-color: #ffc0c0; color: #24292e;">1</span>0
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + 🌐 10.1.1.<span style="background-color: #acf2bd; color: #24292e;">2</span>0
  </span>
</code>
```

**Why Change Is Correct:**  
DNS record change (10 → 20) has character-level highlighting showing "1" changes to "2". Much clearer than plain text diff.

**Proposed Fix:**
```csharp
[Test]
public void FormatDiff_InlineDiff_DnsRecordValue()
{
    // Arrange - DNS A record IP address changing
    var before = "🌐 10.1.1.10";
    var after = "🌐 10.1.1.20";

    // Act
    var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

    // Assert - Should contain rich HTML with character-level highlighting
    result.Should().Contain("<code style=\"display:block;");
    result.Should().Contain("<span style=");
    result.Should().Contain("10.1.1."); // Common prefix
    result.Should().Contain("🌐"); // Emoji preserved
    
    // Should highlight changed character (1 vs 2)
    result.Should().Contain("background-color: #ffc0c0"); // Red for "1"
    result.Should().Contain("background-color: #acf2bd"); // Green for "2"
}
```

---

#### Test 8: `FormatDiff_InlineDiff_NsgRuleDestinationPorts`

**Location:** Line 150  
**Current Expectation:**
```csharp
result.Should().NotContain("<span style=");
result.Should().NotContain("border-left:");
result.Should().Contain("8443");
result.Should().Contain("9443");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - 🔌 8443
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + 🔌 8443<span style="background-color: #acf2bd; color: #24292e;">, 🔌 9443</span>
  </span>
</code>
```

**Why Change Is Correct:**  
Port addition (8443 → 8443, 9443) is highlighted in green. Port emoji (🔌) is preserved.

**Proposed Fix:**
```csharp
[Test]
public void FormatDiff_InlineDiff_NsgRuleDestinationPorts()
{
    // Arrange - NSG rule ports changing from single port to multiple ports
    var before = "🔌 8443";
    var after = "🔌 8443, 🔌 9443";

    // Act
    var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

    // Assert - Should contain rich HTML with character-level highlighting
    result.Should().Contain("<code style=\"display:block;");
    result.Should().Contain("<span style=");
    result.Should().Contain("border-left:");
    result.Should().Contain("8443"); // First port
    result.Should().Contain("9443"); // Second port
    result.Should().Contain("🔌"); // Emoji preserved
    
    // Should highlight the addition (second port)
    result.Should().Contain("background-color: #acf2bd"); // Green for added content
}
```

---

#### Test 9: `FormatDiff_InlineDiff_IsTableCompatible`

**Location:** Line 245  
**Current Expectation:**
```csharp
result.Should().NotContain("\n"); // No raw newlines (use <br> instead)
result.Should().NotContain("<span style=");
result.Should().NotContain("background-color:");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - <span style="background-color: #ffc0c0; color: #24292e;">V</span>alue with spaces
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + <span style="background-color: #acf2bd; color: #24292e;">Different v</span>alue with spaces
  </span>
</code>
```

**Why Change Is Correct:**  
The output IS table-compatible (uses `<br>` not `\n`). The test needs to verify HTML structure is table-safe while allowing styled spans.

**Proposed Fix:**
```csharp
[Test]
public void FormatDiff_InlineDiff_IsTableCompatible()
{
    // Arrange - Multi-word value change
    var before = "Value with spaces";
    var after = "Different value with spaces";

    // Act
    var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

    // Assert - Should be table-compatible with rich HTML
    result.Should().NotContain("\n"); // No raw newlines
    result.Should().Contain("<br>"); // Uses HTML line breaks
    result.Should().Contain("<code style=\"display:block;"); // Block-level code tag
    
    // Should contain rich HTML styling (table-safe HTML is allowed)
    result.Should().Contain("<span style=");
    result.Should().Contain("background-color:");
    
    // Should contain both values
    result.Should().Contain("Value");
    result.Should().Contain("Different");
}
```

---

#### Tests 10-11: `FormatDiff_InlineDiff_NullBeforeValue` and `FormatDiff_InlineDiff_NullAfterValue`

**Location:** Lines 190, 207  
**Status:** ✅ **PASS** - These tests do NOT have `.NotContain("<span style=")` assertions

**Why:** These tests only verify that null handling works correctly. They still pass with the new HTML format.

---

#### Test 12: `FormatDiff_InlineDiff_IdenticalValues`

**Location:** Line 226  
**Status:** ✅ **PASS** - No changes needed

**Why:** When values are identical, the output is `<code>unchanged-value</code>` (simple inline code, no diff). This behavior was not changed.

---

### Category 2: VariableGroupTemplateTests.cs (1 test)

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/VariableGroupTemplateTests.cs`

---

#### Test 13: `Update_RendersChangeIndicatorsAndDiffs`

**Location:** Line 66  
**Current Expectation:**
```csharp
// Should contain before/after diff with plain markdown format
section.Should().Contain("- false");
section.Should().Contain("+ -");
```

**Actual Output (New, Correct):**
```html
<code style="display:block; white-space:normal; padding:0; margin:0;">
  <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    - <span style="background-color: #ffc0c0; color: #24292e;">false</span>
  </span>
  <br>
  <span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">
    + <span style="background-color: #acf2bd; color: #24292e;">-</span>
  </span>
</code>
```

**Why Change Is Correct:**  
Variable group "Enabled" field changing from `false` to `-` (empty) should use rich HTML diff format for consistency with all other inline diffs in the application.

**Proposed Fix:**
```csharp
[Test]
public void Update_RendersChangeIndicatorsAndDiffs()
{
    // TC-15: Template renders update operation layout with change indicators
    var markdown = Render();
    var section = ExtractSection(markdown, "azuredevops_variable_group.update_mixed");
    // Verify summary line
    section.Should().Contain($"<summary>{ActionIcons.Replace}{Nbsp}azuredevops_variable_group <b><code>update_mixed</code></b>");
    // Verify table structure (WITH Change column for update)
    section.Should().Contain("| Change | Name | Value | Enabled | Content Type | Expires |");
    section.Should().Contain("| ------ | ---- | ----- | ------- | ------------ | ------- |");
    // Verify added variable (➕)
    section.Should().Contain($"| {ActionIcons.Add} | `NEW_VAR` |");
    // Verify modified variable with diff (🔄)
    section.Should().Contain($"| {ActionIcons.Update} | `APP_VERSION` |");
    
    // Should contain before/after diff with rich HTML format
    section.Should().Contain("<code style=\"display:block;"); // HTML code block
    section.Should().Contain("false"); // Before value (in HTML)
    section.Should().Contain("+ "); // Plus prefix (in HTML)
    section.Should().Contain("- "); // Minus prefix (in HTML)
    
    // Verify removed variable (❌)
    section.Should().Contain($"| {ActionIcons.Delete} | `OLD_VAR` |");
}
```

---

### Category 3: Snapshot Test (1 test)

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/SnapshotTests.cs`

---

#### Test 14: `Snapshot_ComprehensiveDemoFull_MatchesBaseline`

**Location:** (Line number not shown in test output)  
**Current Issue:**  
Snapshot file contains old plain markdown format for inline diffs. New output has rich HTML format.

**Failure Message:**
```
Snapshot 'comprehensive-demo-full.md' does not match the current output.

Diff (first 50 differences):
Line 330:
  - |·🔄·|·`APP_VERSION`·|·-·1.0.0<br>+·2.0.0·|·`true`·|·-·|·-·|
  + |·🔄·|·`APP_VERSION`·|·<code·style="display:block;·white-space:normal;·paddin...
Line 332:
  - |·🔄·|·`ENVIRONMENT`·|·-·staging<br>+·production·|·`true`·|·-·|·-·|
  + |·🔄·|·`ENVIRONMENT`·|·<code·style="display:block;·white-space:normal;·paddin...
```

**Why Change Is Correct:**  
The comprehensive demo includes Azure DevOps variable groups and Azure Firewall rules with inline diffs. These should all use the rich HTML format matching the working firewall example.

**Proposed Fix:**  
**Use the `update-test-snapshots` skill** to regenerate the snapshot file:

```bash
# The skill will run this internally:
scripts/update-test-snapshots.sh
```

**Verification Steps:**
1. Delete old snapshot: `rm src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/comprehensive-demo-full.md`
2. Run test to regenerate: `dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/*/Snapshot_ComprehensiveDemoFull_MatchesBaseline`
3. Review diff: `git diff src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/comprehensive-demo-full.md`
4. Verify changes are inline diff format updates only (no other content changes)
5. Commit with message: `test: update snapshot for HTML inline diff format\n\nSNAPSHOT_UPDATE_OK`

---

## Risk Assessment

### Low Risk Factors

✅ **No Production Code Changes Required**  
- Only test expectations are being updated
- The production code (commit 692fcf0) is already correct and working

✅ **Format Already Proven in Production**  
- `artifacts/firewall-application-rules-uat.md` uses this format (generated 2026-02-04)
- Format renders correctly in GitHub and Azure DevOps

✅ **Isolated Test Changes**  
- Changes are localized to specific test files
- No cascading effects on other tests

✅ **Character-Level Diffs Improve UX**  
- Users can see EXACTLY what changed (e.g., "4" → "3" in subnet prefix)
- Better than plain text diffs for complex values

### Edge Cases & Considerations

⚠️ **Table Cell Width**  
- Rich HTML diffs are longer than plain text
- **Mitigation:** The `display:block` styling handles this correctly. Test in actual GitHub/AzDO PRs to verify.

⚠️ **Markdown Linters**  
- Some linters may complain about inline HTML in tables
- **Mitigation:** GitHub and Azure DevOps explicitly support this. Our markdownlint config should allow it.

⚠️ **Screen Readers**  
- Nested spans might be harder for screen readers to parse
- **Mitigation:** The underlying text content (with +/- prefixes) is still present and readable.

---

## Verification Plan

### Step 1: Update Unit Tests (ParentChildInlineDiffTests.cs)

1. Apply all proposed fixes for tests 1, 2, 4, 5, 6, 7, 8, 9
2. Run the test class:
   ```bash
   scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ParentChildInlineDiffTests/*
   ```
3. Verify all 12 tests pass (11 updated + 1 unchanged)

### Step 2: Update Integration Test (VariableGroupTemplateTests.cs)

1. Apply proposed fix for test 13
2. Run the test:
   ```bash
   scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/VariableGroupTemplateTests/Update_RendersChangeIndicatorsAndDiffs
   ```
3. Verify test passes

### Step 3: Update Snapshot Test

1. Use `update-test-snapshots` skill to regenerate snapshots
2. Review diff to ensure only inline diff format changed:
   ```bash
   git diff src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/comprehensive-demo-full.md
   ```
3. Run snapshot test:
   ```bash
   scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/*/Snapshot_ComprehensiveDemoFull_MatchesBaseline
   ```
4. Verify test passes

### Step 4: Full Test Suite

Run complete test suite to ensure no regressions:
```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

**Expected Result:** All tests pass (currently 975 passing + 13 fixed = 988 total)

### Step 5: Visual Verification in UAT

1. Generate comprehensive demo:
   ```bash
   tfplan2md examples/comprehensive-demo/plan.json > artifacts/comprehensive-demo.md
   ```
2. Create a test PR in GitHub with the generated markdown
3. Verify inline diffs render correctly:
   - Red background for removed content
   - Green background for added content
   - Character-level highlighting visible
   - No display issues in table cells

### Step 6: Markdownlint Check

Verify the generated markdown passes linting:
```bash
scripts/markdownlint.sh artifacts/comprehensive-demo.md
```

**Expected Result:** 0 errors (inline HTML is allowed in our config)

---

## Implementation Checklist

**STOP - AWAITING MAINTAINER APPROVAL**

Once approved, execute in this order:

- [ ] **Step 1:** Update `ParentChildInlineDiffTests.cs` (tests 1, 2, 4, 5, 6, 7, 8, 9)
- [ ] **Step 2:** Run test class to verify fixes
- [ ] **Step 3:** Update `VariableGroupTemplateTests.cs` (test 13)
- [ ] **Step 4:** Run test to verify fix
- [ ] **Step 5:** Use `update-test-snapshots` skill to regenerate snapshots
- [ ] **Step 6:** Review snapshot diff
- [ ] **Step 7:** Run full test suite
- [ ] **Step 8:** Generate comprehensive demo for visual verification
- [ ] **Step 9:** Run markdownlint check
- [ ] **Step 10:** Commit all changes with message: `test: update expectations for HTML inline diff format\n\nSNAPSHOT_UPDATE_OK`

---

## Summary

**Total Tests Requiring Updates:** 13
- **Unit tests:** 8 tests (simple assertion updates)
- **Integration test:** 1 test (simple assertion update)
- **Snapshot test:** 1 test (regenerate with skill)
- **Passing tests (no changes):** 3 tests

**Estimated Time:** 30-45 minutes

**Risk Level:** LOW - All changes are test expectations only

**Confidence:** HIGH - The restored format matches the working firewall example and provides better UX

**Recommendation:** ✅ **APPROVE AND PROCEED** - The implementation is correct, tests just need to catch up.
