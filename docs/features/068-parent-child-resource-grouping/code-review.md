# Code Review: Parent-Child Resource Grouping

## Summary

This review covers Feature 068 (Parent-Child Resource Grouping), which introduces a generic framework for rendering child Terraform resources (like group members) inline as tables within their parent resource sections. The implementation includes core abstractions, merging logic, provider-specific row extractors for Azure AD and Azure DevOps, and template updates.

While the test suite passes (928 tests, 100% pass rate) and coverage meets thresholds (89.27% line, 80.27% branch), there are **two critical Blocker issues** that prevent approval:

1. **Docker build fails** with code analysis errors
2. **Missing fallback logic** for `(known after apply)` parent IDs, causing separate children to not be merged in the most common scenario

## Verification Results

- **Tests**: ✅ Pass (928 passed, 0 failed)
- **Coverage**: ✅ Line 89.27% (threshold ≥84.48%), Branch 80.27% (threshold ≥72.80%)
- **Build**: ❌ **Docker build fails** with 10 CA1875 code analysis errors
- **Errors**: ❌ **10 code analysis errors blocking Docker build**
- **Markdownlint**: ✅ Pass (0 errors in comprehensive demo)

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| Registry Complete: Cataloged patterns with status | ✅ | ✅ | parent-child-resource-catalog.md is comprehensive |
| Inline Rendering: Initial targets render as tables | ❌ | ❌ | **Blocker**: Separate children not merged when parent ID is `(known after apply)` |
| Change Indicators: Tables show emojis (➕, 🔄, etc.) | ✅ | ✅ | Verified in unit tests |
| Resource Address: Separate children show address | ✅ | ✅ | Verified in ReportModelBuilderParentChildTests |
| Inline Source: Show attribute name for inline children | ✅ | ✅ | Verified in unit tests |
| Mixed Handling: Both inline and separate children | ✅ | ✅ | HasMixedSources flag tested |
| Formatting: Use existing formatters for table cells | ✅ | ✅ | ScribanHelpers.FormatAttributeValueTableWithRegistry used |
| Summary Line: Parent summary includes child counts | ✅ | ✅ | Verified in tests |
| Merged-Child Findings: Findings appear in parent section | ✅ | ✅ | MoveFindingsToParent tested |

**Spec Deviations Found:** 

1. **Critical**: Architecture document (lines 320-323) states: 
   > **Fallback for `(known after apply)`**: When the parent's ID is not yet known, use Terraform address-based heuristics (same module scope + matching type). This handles the common create-parent-and-children-together scenario.
   
   **Reality**: This fallback is NOT implemented. In [ReportModelBuilder.ParentChildMerging.cs](src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs#L230-L234), the code simply returns empty if the parent ID is not present:
   
   ```csharp
   var parentId = GetFlatValue(parentState, relationship.ParentIdAttribute);
   if (string.IsNullOrWhiteSpace(parentId))
   {
       return [];
   }
   ```
   
   **Impact**: The most common scenario (creating parent and children together) doesn't work. Test plans like `azuread-group-members-plan.json` and the comprehensive demo have parent groups with `"after_unknown": { "id": true }`, so separate children are never merged. Snapshots show no child tables.

2. **Examples 1-6A are NOT covered in UAT/snapshot tests**: The specification states "Examples 1–6A match the initial implementation targets for Feature 068 and are required for UAT report + snapshot coverage." However, only generic unit tests exist. No UAT-specific artifact or snapshot demonstrates the exact rendering format from Examples 1-6A.

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Empty input | Not Tested | No test data for empty/null scenarios |
| Null values | Pass | TC-E5 covers null attributes in edge case tests |
| Special characters | Not Tested | No test for special characters in member IDs |
| Very large input | Not Tested | No performance test for 100+ children |
| Error conditions | Pass | Edge case tests cover non-existent parent (TC-E1) |
| Parent ID (known after apply) | **Fail** | **This is the critical missing scenario** |

## Review Decision

**Status:** ❌ **Changes Requested**

The implementation has solid architecture and unit test coverage, but two **Blocker** issues prevent approval:

1. Docker build must succeed (CA1875 code analysis errors)
2. Fallback for `(known after apply)` parent IDs must be implemented per the architecture spec

## Snapshot Changes

- Snapshot files changed: ✅ Yes (multiple files updated)
- Commit message token `SNAPSHOT_UPDATE_OK` present: ✅ Yes (in commit "docs: Update work protocol for Feature 068 (parent-child merging tasks)")
- Why the snapshot diff is correct: Snapshots updated to reflect new inline child table rendering. However, **many snapshots show NO child tables** due to the missing fallback logic (Blocker issue #2).

## Issues Found

### Blockers

#### 1. Docker Build Failure (CA1875 Code Analysis Errors)

**Location**: [src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownInvariantTests.cs](src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownInvariantTests.cs) (lines 151, 327, 328, 353, 354) and [TemplateIsolationTests.cs](src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/TemplateIsolationTests.cs) (lines 372, 385, 386, 389, 390)

**Description**: The Docker build fails during the `dotnet build` step with 10 CA1875 errors:

```
error CA1875: Use 'Regex.Count' instead of 'Regex.Matches(...).Count'
```

Build output excerpt:
```
/workspace/src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownInvariantTests.cs(151,42): 
error CA1875: Use 'Regex.Count' instead of 'Regex.Matches(...).Count'
```

**Example (line 151)**:
```csharp
var expectedResourceTables = Regex.Matches(markdown, @"\| Attribute \|", RegexOptions.None, TimeSpan.FromSeconds(1)).Count;
```

**Fix Required**: Replace all 10 occurrences of `Regex.Matches(...).Count` with `Regex.Count(...)` (available in .NET 7+):

```csharp
var expectedResourceTables = Regex.Count(markdown, @"\| Attribute \|", RegexOptions.None, TimeSpan.FromSeconds(1));
```

**Why it's a Blocker**: The Docker build is part of the CI/CD pipeline and must pass. Tests pass locally but fail in Docker due to stricter code analysis settings (`TreatWarningsAsErrors`).

---

#### 2. Missing Fallback for `(known after apply)` Parent IDs

**Location**: [src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs](src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs#L218-L268)

**Description**: The architecture document (section "Separate child matching strategy", updated Section 3a) specifies configuration reference matching as the fallback when parent IDs are `(known after apply)`. The original architecture incorrectly specified a module-address heuristic, which has been replaced with precise reference matching using the plan's `configuration` block.

The implementation in `BuildSeparateRows` method does NOT include any fallback. When a parent's ID is marked as `(known after apply)` (which is the most common case for creating resources), the code returns an empty list:

```csharp
private List<ChildResourceRow> BuildSeparateRows(
    ResourceChangeModel parent,
    ParentChildRelationship relationship,
    Dictionary<string, List<ResourceChangeModel>> changesByType,
    HashSet<ResourceChangeModel> removedChildren)
{
    // ... (validation code)
    
    var parentState = ResolveStateForAction(parent);
    var parentId = GetFlatValue(parentState, relationship.ParentIdAttribute);
    if (string.IsNullOrWhiteSpace(parentId))
    {
        return [];  // ❌ Returns empty without trying fallback!
    }
    
    // ... (rest of matching logic)
}
```

**Evidence of Impact**:

1. **Test Data**: [examples/comprehensive-demo/plan.json](examples/comprehensive-demo/plan.json#L1517-1541) has:
   ```json
   {
     "address": "azuread_group.platform_engineers",
     "after": {
       "display_name": "Platform Engineers",
       "members": ["user-100", "user-101", "group-200", "spn-300"]
     },
     "after_unknown": { "id": true, "object_id": true }
   }
   ```
   The child resource references `"group_object_id": "group-200"`, but the parent's `id` is `(known after apply)`.

2. **Snapshot Evidence**: [azuread-group-members.md](src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azuread-group-members.md) shows only 29 lines with NO member table despite the plan containing:
   - `azuread_group` with inline members
   - 2 separate `azuread_group_member` resources (create one, delete one)

3. **Comprehensive Demo**: Running `dotnet run --project src/Oocx.TfPlan2Md` on comprehensive-demo produces NO "#### Members" sections (verified by grep).

**Fix Required**: Implement configuration reference matching as specified in the updated architecture (Section 3a):

1. **Add `Configuration` to `TerraformPlan`**: Add `[property: JsonPropertyName("configuration")] JsonElement? Configuration = null` to the `TerraformPlan` record. This captures the plan's `configuration` block, which contains Terraform expression references.

2. **Create `ConfigurationReferenceResolver`**: A utility that walks `configuration.root_module.resources[].expressions` (and recursively `module_calls`) to build a reference index: `(child_address, attribute_name) → list of referenced parent addresses`.

3. **Use the reference index in `BuildSeparateRows`**: When `parentId` is null/empty, consult the reference index to check if the child's `ChildReferenceAttribute` references the parent's address:

```csharp
if (string.IsNullOrWhiteSpace(parentId))
{
    // Fallback: match via configuration expression references
    return BuildSeparateRowsByReference(parent, relationship, candidates, removedChildren);
}
```

Where `BuildSeparateRowsByReference` looks up `(child.Address, relationship.ChildReferenceAttribute)` in the reference index and checks if any reference matches `parent.Address` or `parent.Address + "." + relationship.ParentIdAttribute`.

4. **Graceful degradation**: If no configuration block is present OR no reference match is found, the child remains in the change list and renders as a standalone resource section (same as pre-Feature 068 behavior). The system must never guess — incorrect merging is worse than no merging.

5. **Update test data**: Add `configuration` blocks to synthetic test plans (`azuread-group-members-plan.json`, `comprehensive-demo/plan.json`) with appropriate `expressions.*.references` entries.

**Why it's a Blocker**: This is the MOST COMMON scenario for Terraform plans (creating parent+children together). Without this fallback, the feature doesn't work for its primary use case. All acceptance criteria related to "separate children" are effectively untested and non-functional.

---

### Major Issues

#### 3. Missing UAT Artifact for Examples 1-6A

**Location**: [docs/features/068-parent-child-resource-grouping/specification.md](docs/features/068-parent-child-resource-grouping/specification.md#L156-158) and [test-plan.md](docs/features/068-parent-child-resource-grouping/test-plan.md#L35-72)

**Description**: The specification explicitly states:

> **Examples 1–6A** match the initial implementation targets for Feature 068 and are required for **UAT report + snapshot coverage**.

The test plan includes User Acceptance Scenarios requiring visual verification on GitHub/Azure DevOps PRs. However:

1. No UAT-specific artifact exists (expected: `artifacts/parent-child-resource-grouping-uat.md` or similar)
2. Existing snapshots (azuread-group-members.md, azuredevops-team-members.md) do NOT match Examples 1-6A format
3. No test plan step verifies that Examples 1-6A are rendered exactly as documented

**Impact**: 
- Cannot verify rendering matches the documented examples
- UAT Tester cannot validate cross-platform rendering without a proper artifact
- Gaps between documented examples and actual output are hidden

**Fix Required**: 
1. Create test data for Examples 1-6A (azuread group with inline members, separate members, mixed, azuredevops team, azuredevops group, and with findings)
2. Generate artifact: `artifacts/parent-child-resource-grouping-uat.md`
3. Update test plan to reference this artifact

---

### Minor Issues

#### 4. Inconsistent Property Name Casing in ChildTableColumn

**Location**: [src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ChildTableColumn.cs](src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ChildTableColumn.cs)

**Description**: `ChildTableColumn` uses properties instead of positional record parameters, but this is inconsistent with similar lightweight models in the codebase (e.g., `ActionSummaryModel`, `ModuleChangeGroup`).

Current:
```csharp
internal record ChildTableColumn
{
    required public string Header { get; init; }
    required public string PropertyName { get; init; }
}
```

Suggested (for consistency):
```csharp
internal record ChildTableColumn(string Header, string PropertyName);
```

**Impact**: Low (style/consistency only, no functional impact)

---

#### 5. Missing Performance Test for Large Child Counts

**Location**: Test plan mentions performance NFR but no test exists

**Description**: The specification includes an NFR: "No measurable degradation in plan processing time" and the test plan states:

> **Performance**: Building a report with 100+ separate child resources should not significantly increase processing time (<500ms overhead).

However, no test exists to verify this. Task 7 acceptance criteria include "lightweight performance check" but the implementation only mentions indexed lookups without a quantitative benchmark.

**Fix Required**: Add a performance test (similar to existing architecture boundary tests) that:
1. Creates a plan with 150+ child resources
2. Measures build time
3. Asserts overhead < 500ms compared to baseline

---

### Suggestions

#### 6. Consider Extracting Magic Numbers for Summary Indicator Order

**Location**: [ReportModelBuilder.ParentChildMerging.cs](src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs#L338-347)

**Description**: The `GetSummaryIndicatorOrder` method hardcodes the order. This is fine for now, but if summary ordering becomes configurable in the future, consider extracting to a constant.

Current:
```csharp
private static IReadOnlyList<string> GetSummaryIndicatorOrder()
{
    return
    [
        ActionIcons.Add,
        ActionIcons.Update,
        ActionIcons.Replace,
        ActionIcons.Delete
    ];
}
```

**Suggestion**: Extract to a const field if reused, or document why this order is canonical.

---

#### 7. Template Could Benefit from Additional Comments

**Location**: [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_child_resources.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_child_resources.sbn)

**Description**: The template logic is clear but lacks comments explaining why the Change column is omitted for pure create/delete actions.

**Suggestion**: Add a comment:
```scriban
{{~ ## For pure create/delete, all children share parent action, so no Change column needed ~}}
{{~ if change.action == "create" || change.action == "delete" ~}}
```

---

## Critical Questions Answered

### What could make this code fail?

1. **`(known after apply)` parent IDs** (already confirmed failure) — separate children are never merged
2. **Circular parent-child relationships** — architecture mentions this but implementation doesn't explicitly guard against it (though TC-E2 tests it)
3. **Resource type name collisions** — if multiple providers have resources with the same type name
4. **Very large child collections** — performance was not quantitatively tested

### What edge cases might not be handled?

1. **Parent has no ID attribute** — `GetFlatValue` returns null, fallback should handle this
2. **Child references multiple parents** — architecture mentions this is "usually not possible" but doesn't handle it
3. **Inline attribute is not an array** — code checks for `ValueKind.Array` but could add logging
4. **Child row extractor throws exception** — no try/catch in merging logic

### Are all error paths tested?

- ✅ Non-existent parent (TC-E1)
- ✅ Circular relationships (TC-E2)
- ✅ Empty inline attributes (TC-E4)
- ✅ Null child attributes (TC-E5)
- ❌ **Extractor exceptions** — not tested
- ❌ **Invalid JSON in child state** — not tested

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ❌ (Blocker: fallback not implemented) |
| Spec Compliance | ❌ (Blocker: fallback missing, UAT examples not covered) |
| Code Quality | ✅ |
| Architecture | ✅ (design is sound, just missing implementation) |
| Testing | ⚠️ (Unit tests good, functional gaps due to Blocker #2) |
| Documentation | ✅ |

---

## Work Protocol & Documentation Verification

### Work Protocol Status

✅ `work-protocol.md` exists in [docs/features/068-parent-child-resource-grouping/](docs/features/068-parent-child-resource-grouping/work-protocol.md)

✅ All required agents have logged entries:
- Requirements Engineer ✅
- Architect ✅
- Quality Engineer ✅
- Task Planner ✅
- Developer ✅ (multiple entries)
- Technical Writer ✅

### Global Documentation Updates

| Document | Updated | Notes |
|----------|---------|-------|
| `docs/features.md` | ✅ | Added "Parent-Child Resource Grouping (Inline Child Tables)" section |
| `docs/architecture.md` | N/A | No architectural changes to core system |
| `docs/testing-strategy.md` | N/A | No new test patterns introduced |
| `README.md` | ✅ | Updated feature list to mention "inline parent-child tables for memberships" |
| `docs/agents.md` | N/A | No workflow changes |

---

## Next Steps

### For Developer Agent

1. **Fix Blocker #1 (CA1875)**: Replace all 10 `Regex.Matches(...).Count` with `Regex.Count(...)`:
   - [MarkdownInvariantTests.cs](src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownInvariantTests.cs): lines 151, 327, 328, 353, 354
   - [TemplateIsolationTests.cs](src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/TemplateIsolationTests.cs): lines 372, 385, 386, 389, 390

2. **Fix Blocker #2 (Configuration Reference Matching)**: 
   - Add `Configuration` as `JsonElement?` to `TerraformPlan` record
   - Create `ConfigurationReferenceResolver` utility (see architecture Section 3a)
   - Integrate reference index into `BuildSeparateRows` as fallback when parent ID is null
   - Add `configuration` blocks to synthetic test plans (`azuread-group-members-plan.json`, `comprehensive-demo/plan.json`)
   - Add unit tests for `ConfigurationReferenceResolver` (root module, nested modules, absent configuration)
   - Add unit test verifying fallback works when parent ID is `(known after apply)`
   - Verify graceful degradation: absent configuration → child renders as standalone section
   - Update snapshots to show child tables

3. **Fix Major Issue #3 (UAT Artifact)**:
   - Create test data for Examples 1-6A
   - Generate `artifacts/parent-child-resource-grouping-uat.md`
   - Run snapshot update to include proper child tables

4. **Verify Docker build succeeds** after fixes

5. **Re-run all tests** and confirm snapshots now show child tables

After fixes, return to Code Reviewer for re-approval.
