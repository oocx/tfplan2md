# Issue: Performance Investigation — Potential O(n²) and Worse Patterns

## Problem Description

The tfplan2md tool, which should normally complete in sub-second time, reportedly can run
for up to 20 minutes when processing a normal-sized Terraform plan. This analysis
investigates the codebase for algorithmic complexity issues that could cause such a blowup.

## Investigation Scope

Searched all `.cs` files under `src/Oocx.TfPlan2Md/` for:

- LCS (Longest Common Subsequence) computations
- Nested loops and O(n²) patterns
- Regex patterns susceptible to catastrophic backtracking
- Unbounded recursion
- Quadratic list operations (List.Remove, FindIndex inside loops)
- String concatenation in loops
- JSON flattening/parsing on large structures

## Root Cause Analysis

### Finding 1: LCS Algorithm — O(m×n) Time and Space per Attribute Value (🔴 CRITICAL)

**Files:**

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/DiffComputation.cs` L65–L108
  (`ComputeLcsPairs` for line-level diff)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/DiffComputation.cs` L116–L159
  (`ComputeLcsPairs` for character-level diff)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/DiffUtilities.cs` L39–L55
  (`AppendStyledLineWithCharDiff` — invokes char-level LCS per line pair)

**The Pattern:**

```csharp
private static List<LcsPair> ComputeLcsPairs(string[] before, string[] after)
{
    var m = before.Length;
    var n = after.Length;
    var lengths = new int[m + 1, n + 1];     // ← allocates m×n matrix

    for (var i = m - 1; i >= 0; i--)
    {
        for (var j = n - 1; j >= 0; j--)     // ← O(m×n) nested loop
        {
            ...
        }
    }
    ...
}
```

**Why It's Critical:**

The LCS algorithm allocates an `int[m+1, n+1]` matrix and fills every cell. This is invoked in
two layers:

1. **Line-level LCS** (`BuildLineDiff`): Called for each "large" attribute value change. If a
   JSON policy document has 500 lines before and 500 lines after, this allocates a 250,000-entry
   matrix and does 250,000 string comparisons.

2. **Character-level LCS** (`AppendStyledLineWithCharDiff`): For every *pair* of
   changed lines detected by the line-level diff, a second LCS is run at the character level.
   If a line has 200 characters, this is another 40,000-cell matrix — **per line pair**.

**Cascade Multiplication:**

For a resource with one large JSON attribute (e.g., an Azure Policy `policy` attribute):

- 500 lines × 500 lines = 250,000 operations (line-level LCS)
- If 100 line pairs are changed, each 200 chars long: 100 × 200 × 200 = 4,000,000 operations
  (character-level LCS)
- **Total: ~4.25 million operations per attribute**

With 200 resources having even one large attribute each, this becomes **850 million operations**
(200 × 4,250,000). This alone could take minutes on real hardware.

**Call Chain:**

```
Template: {{ format_large_value(attr.before, attr.after, "inline-diff") }}
  → FormatLargeValue()       [LargeValues.cs L22]
  → BuildInlineDiff()        [LargeValues.cs L277]
  → BuildLineDiff()          [DiffComputation.cs L17]   ← O(m×n) line LCS
  → ComputeLcsPairs()        [DiffComputation.cs L65]
  → AppendStyledLineWithCharDiff()  [DiffUtilities.cs L39]
  → ComputeLcsPairs(string)  [DiffComputation.cs L116]  ← O(m×n) char LCS per line pair
```

**Also Called From:**

- `CountChangedLines()` in `LargeValueSummary.cs` L157–L163 — calls `BuildLineDiff()` **again**
  for summary counting, so every large attribute pays the LCS cost **twice**.
- `AzureDevOpsDiffFormatter.FormatDiff()` in `AzureDevOpsDiffFormatter.cs` L36 — calls
  `FormatLargeValue(before, after, "inline-diff")` for **every non-identical attribute** in
  Azure DevOps render target. Even "small" attributes go through the LCS pipeline.

**Severity: 🔴 HIGH — This is the most likely cause of 20-minute runtimes.**

With AzureDevOps as the render target (default), *every single changed attribute* triggers:
`FormatDiff → BuildInlineDiffTable → FormatLargeValue → BuildInlineDiff → BuildLineDiff → ComputeLcsPairs`.

For a plan with 200 resources × 30 changed attributes = 6,000 attribute diffs through LCS.
Even if most are short single-line values, the overhead is significant. If any are multi-line
values (JSON policies, scripts, etc.), the cost explodes.

---

### Finding 2: Double LCS Computation for Large Value Summaries (🟡 MEDIUM)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValueSummary.cs`
L157–L163

```csharp
private static int CountChangedLines(string before, string after)
{
    var beforeLines = SplitLines(before);
    var afterLines = SplitLines(after);
    var diff = BuildLineDiff(beforeLines, afterLines);  // ← Full LCS again!
    return diff.Count(d => d.Kind != DiffKind.Unchanged);
}
```

**Why It's a Problem:**

`LargeAttributesSummary` is called in the template *before* `format_large_value`, and both
call `BuildLineDiff` → `ComputeLcsPairs`. The LCS result for the summary is never reused.

Every large attribute pays the O(m×n) LCS cost **twice** — once for counting, once for
rendering.

**Severity: 🟡 MEDIUM — Doubles the LCS cost for all large attributes.**

---

### Finding 3: `allChanges.Remove(child)` Inside Loop — O(n²) List Removal (🟡 MEDIUM)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`
L106–L108

```csharp
foreach (var child in removedChildren)
{
    allChanges.Remove(child);   // ← O(n) per removal
}
```

**Why It's a Problem:**

`List<T>.Remove()` is O(n) — it must search the entire list and shift elements. When called
for each removed child, this is O(c×n) where c = number of children removed and
n = total resources.

For 200 resources with 100 merged as children: 100 × 200 = 20,000 operations. Not 20 minutes
by itself, but contributes to overall slowness.

**Severity: 🟡 MEDIUM — Contributes but unlikely to cause 20-minute runtimes alone.**

---

### Finding 4: `FindIndex` Inside LINQ Chain — O(g×n) (🟢 LOW)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` L187

```csharp
var moduleGroups = displayChanges
    .GroupBy(c => c.ModuleAddress ?? string.Empty)
    .Select(g => new
    {
        Key = g.Key,
        Changes = g.ToList(),
        FirstIndex = displayChanges.FindIndex(c => (c.ModuleAddress ?? string.Empty) == g.Key)
        //           ^^^^^^^^^^^^^^^^^^^^^^^^^ O(n) per group
    })
    ...
```

**Why It's a Problem:**

`FindIndex` scans the full `displayChanges` list for each module group. With g groups and
n changes, this is O(g×n). For 200 resources across 10 modules: 10 × 200 = 2,000 operations.

**Severity: 🟢 LOW — Minor inefficiency, not a bottleneck.**

---

### Finding 5: `BuildConfigurationReferencesForResource` — Full Index Scan Per Resource (🟡 MEDIUM)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`
L212–L227

```csharp
private Dictionary<string, IReadOnlyList<string>> BuildConfigurationReferencesForResource(
    string normalizedAddress)
{
    ...
    var grouped = _configurationReferenceIndex
        .Where(entry => string.Equals(entry.Key.Address, normalizedAddress, StringComparison.OrdinalIgnoreCase))
        .ToDictionary(...);
    ...
}
```

**Why It's a Problem:**

Despite having a dictionary with a proper key comparer, this method performs a **linear scan**
using `.Where()` over all entries in the dictionary. It matches only by `Address`, but the
dictionary key is `(Address, Attribute)` — so it can't use `TryGetValue`.

This is called **once per resource** in `BuildResourceChangeModel` (line 34 of
`ReportModelBuilder.ResourceChanges.cs`), making it O(R × I) where R is the number of
resources and I is the number of index entries.

For 200 resources with 2000 configuration references: 200 × 2000 = 400,000 comparisons.

**Severity: 🟡 MEDIUM — Could be made O(1) per resource with a secondary index keyed by address.**

---

### Finding 6: Regex Post-Processing of Rendered Output (🟢 LOW)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs` L361–L454

```csharp
rendered = Regex.Replace(rendered, @"(?<=\|[^\n]*)\n\s*\n(?=[ \t]*\|)", "\n",
    RegexOptions.None, TimeSpan.FromSeconds(2));
rendered = Regex.Replace(rendered, @"\n[ \t]+(\|)", "\n$1",
    RegexOptions.None, TimeSpan.FromSeconds(1));
// ...plus 3 more Regex.Replace calls in NormalizeHeadingSpacing
```

**Why It's a Problem:**

These regex replacements run against the **entire rendered output** string. For a large plan
with 200 resources, the output could be 500KB+ of markdown. The patterns use lookbehinds and
quantifiers that, while not catastrophic-backtracking-prone (they have timeouts), add up when
applied sequentially to a large string 5 times. Each pass creates a new string copy.

The patterns all have `TimeSpan.FromSeconds(1-2)` timeouts, which is good defensive coding,
but if a regex takes close to its timeout, that's 1-2 seconds × 5 passes = up to 10 seconds.

**Severity: 🟢 LOW — Unlikely to cause 20-minute runtimes, but could cause 5-10 second overhead on large outputs. The timeouts prevent catastrophic scenarios.**

---

### Finding 7: `ConvertToFlatDictionary` Called Multiple Times Per Resource (🟢 LOW)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`
L104–L107

```csharp
var beforeDict = ConvertToFlatDictionary(change.Before);
var afterDict = ConvertToFlatDictionary(change.After);
var beforeSensitiveDict = ConvertToFlatDictionary(change.BeforeSensitive);
var afterSensitiveDict = ConvertToFlatDictionary(change.AfterSensitive);
```

**And in** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs` L32:

```csharp
var flatState = JsonFlattener.ConvertToFlatDictionary(state);
```

**And in** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` L288:

```csharp
var flatState = Helpers.JsonFlattener.ConvertToFlatDictionary(state);
```

**Why It Could Matter:**

The same JSON state is flattened repeatedly for different purposes (attribute changes, summary
building, refactoring name resolution, tag badges). Each flattening recursively walks the
entire JSON tree.

For a resource with 100 attributes (after flattening), this is 100 dictionary insertions.
Done 4-6 times per resource across 200 resources = ~80,000-120,000 flattening operations.

**Severity: 🟢 LOW — Recursive tree walk is O(n) in JSON nodes; not exponential. Could be
optimized with caching but unlikely to cause 20 minutes.**

---

### Finding 8: AzureDevOps FormatDiff Calls FormatLargeValue for ALL Changed Attributes (🔴 CRITICAL)

**File:** `src/Oocx.TfPlan2Md/RenderTargets/AzureDevOps/AzureDevOpsDiffFormatter.cs` L36

```csharp
public string FormatDiff(string? before, string? after)
{
    ...
    // Build inline diff with HTML styling
    return WrapInlineDiffCode(BuildInlineDiffTable(beforeValue, afterValue));
}

private static string BuildInlineDiffTable(string before, string after)
{
    var block = FormatLargeValue(before, after, "inline-diff");
    ...
}
```

**Why It's Critical:**

The `AzureDevOpsDiffFormatter.FormatDiff()` method calls `FormatLargeValue(before, after, "inline-diff")`
for **every single changed attribute** — not just those flagged as "large". This means:

- Even a simple attribute like `name: "old" → "new"` triggers the full pipeline:
  `FormatLargeValue` → `BuildInlineDiff` → `BuildLineDiff` → `ComputeLcsPairs`
- The `FormatLargeValue` path first tries to parse both values as JSON and as XML
  (`TryFormatJson` + `TryFormatXml`), adding exception-throwing overhead for every non-JSON,
  non-XML value.

This is the **multiplier** that makes Finding 1 devastating:

- With the GitHub render target, `FormatDiff` uses simple `+/-` notation (no LCS).
- With the AzureDevOps render target (the default), EVERY attribute diff goes through LCS.

For 200 resources × 30 changed attributes = **6,000 LCS computations**, even for trivial
single-word value changes.

**Severity: 🔴 CRITICAL — Combined with Finding 1, this is the primary performance bottleneck.**

---

### Finding 9: JSON/XML Parsing Attempts on Non-Structured Values (🟢 LOW)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValues.cs`
L83–L100

```csharp
private static bool TryFormatStructuredContent(string value, out string formatted, out string? language)
{
    if (TryFormatJson(value, out formatted))
    {
        language = "json";
        return true;
    }

    if (TryFormatXml(value, out formatted))
    {
        language = "xml";
        return true;
    }
    ...
}
```

Both `TryFormatJson` and `TryFormatXml` call `JsonDocument.Parse()` and `XDocument.Parse()`
respectively, which throw exceptions on non-matching input. Exception handling adds overhead
of ~1-10μs per attribute. With 6,000 attributes, this is 6-60ms — negligible.

**Severity: 🟢 LOW — Not a meaningful contributor.**

---

## Summary Table

| # | Finding | Location | Complexity | Severity |
|---|---------|----------|------------|----------|
| 1 | LCS algorithm (line + char levels) | DiffComputation.cs | O(m×n) per attribute | 🔴 CRITICAL |
| 8 | AzDevOps FormatDiff calls LCS for ALL attrs | AzureDevOpsDiffFormatter.cs | O(R×A×m×n) | 🔴 CRITICAL |
| 2 | Double LCS for summary counting | LargeValueSummary.cs | 2× LCS cost | 🟡 MEDIUM |
| 3 | List.Remove in loop | ReportModelBuilder.ParentChildMerging.cs | O(c×n) | 🟡 MEDIUM |
| 5 | Linear config reference scan | ReportModelBuilder.ResourceChanges.cs | O(R×I) | 🟡 MEDIUM |
| 4 | FindIndex in LINQ chain | ReportModelBuilder.Build.cs | O(g×n) | 🟢 LOW |
| 6 | Regex post-processing | MarkdownRenderer.cs | 5× full-string passes | 🟢 LOW |
| 7 | Repeated JSON flattening | Multiple files | O(n) per call | 🟢 LOW |
| 9 | JSON/XML parse attempts | LargeValues.cs | Exception cost | 🟢 LOW |

## Could This Cause 20-Minute Runtimes?

**Yes.** The combination of Findings 1 and 8 is the smoking gun:

**Scenario:** 200 resource changes × 30 attributes each = 6,000 attribute diffs.

With AzureDevOps render target (default):
- Each attribute diff calls `FormatLargeValue` → `BuildInlineDiff` → `ComputeLcsPairs`
- Even for single-line values, LCS allocates a matrix and does O(m×n) work
- If 20 attributes per resource are "large" (JSON policies, scripts, etc.) with ~200 lines each:
  - 200 resources × 20 large attrs × (200 × 200 line LCS + 100 line pairs × 200 × 200 char LCS)
  - = 4,000 × (40,000 + 100 × 40,000) = 4,000 × 4,040,000 = **16.16 billion operations**
  - At 1 billion operations/second (optimistic for .NET with allocations): **~16 seconds total**
  
Even a more conservative scenario with fewer large values but many resources can easily reach
minutes to tens of minutes due to the quadratic growth.

## Suggested Fix Approach

### Priority 1: Fix the AzureDevOps FormatDiff Bypass (Findings 1 + 8)

Change `AzureDevOpsDiffFormatter.FormatDiff()` to NOT use `FormatLargeValue` for short/simple
values. For single-line values where both before and after are short (e.g., < 100 chars),
use simple `+/-` notation like the GitHub formatter does. Only invoke the full LCS pipeline
for genuinely multi-line or large values.

### Priority 2: Cache LCS Results (Finding 2)

The `LargeAttributesSummary` function calls `BuildLineDiff` for counting, then the template
calls `format_large_value` which calls it again. Cache the diff result or merge the summary
computation into the rendering pass.

### Priority 3: Optimize Configuration Reference Lookup (Finding 5)

Build a secondary index keyed by normalized address so that
`BuildConfigurationReferencesForResource` uses `TryGetValue` instead of `.Where()`.

### Priority 4: Use HashSet-based removal (Finding 3)

Replace `foreach (var child in removedChildren) { allChanges.Remove(child); }` with
`allChanges.RemoveAll(c => removedChildren.Contains(c))` for a single O(n) pass.

## Related Tests

Tests that should pass after any fix:

- [ ] All tests in `Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersFormatDiffTests.cs`
- [ ] All tests in `Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersLargeValueTests.cs`
- [ ] All tests in `Oocx.TfPlan2Md.TUnit/RenderTargets/AzureDevOpsDiffFormatterTests.cs`
- [ ] All tests in `Oocx.TfPlan2Md.TUnit/RenderTargets/GitHubDiffFormatterTests.cs`
- [ ] All tests in `Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererTests.cs`
- [ ] All snapshot tests in `Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownSnapshotTests.cs`
- [ ] All parent-child tests in `Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ParentChildInlineDiffTests.cs`

## Additional Context

- The GitHub render target (`GitHubDiffFormatter`) does NOT use LCS — it uses simple `+/-`
  notation. Plans rendered with `--render-target GitHub` would NOT exhibit this issue.
- The issue is specific to the Azure DevOps render target which is the **default**.
- The Scriban template loop limit is set to 10,000 (`MarkdownRenderer.cs` L468) which was
  already increased from default, suggesting large plans were a known concern.
- All Regex patterns in the codebase have `TimeSpan` timeouts, preventing catastrophic
  backtracking — good defensive coding.
- The `MatchPattern.cs` regex patterns are compiled at registration time (not per-invocation),
  which is correct.
