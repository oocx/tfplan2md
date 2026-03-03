# Snapshot Fix Notes (Working Notes for Developer Agent)

**Last Updated**: 2026-03-03
**Branch**: `feature/107-remove-scriban`
**Status**: 48 snapshot tests failing after snapshots were restored to origin/main versions

> These are working notes to preserve analysis across session timeouts.
> This file should be deleted once all 48 tests pass.

---

## Failing Tests by Category

### Category A: AzApi (31 tests)
All `Snapshot_Azapi*` tests. Root cause: `AzApiResourceRenderer` / `AzApiUpdateResourceRenderer`
delegates to `DefaultResourceRenderer`, which does NOT produce the specialized azapi format.

### Category B: AzureAD (4 tests)
`Snapshot_AzureAd_Comprehensive`, `Snapshot_AzureAd_GroupMembersKnownAfterApply`,
`Snapshot_AzureAd_GroupMembers`, `Snapshot_AzureAd_GroupMembers_NoConfiguration`.
Root cause: `azuread_*` resources need blank line between `<details>` and `<summary>`.
Also need correct attribute table separators.

### Category C: AzureDevOps (3 tests)
`Snapshot_AzureDevOps_Comprehensive`, `Snapshot_AzureDevOps_GroupMembers`,
`Snapshot_AzureDevOps_TeamMembers`.
Root cause: wrong variable table separators (need exact header-length, no `+2` padding).

### Category D: Mixed (10+ tests)
`AzureDisplayEnhancements`, `AzureIdsWithPrincipals`, `BreakingPlan`, `ComprehensiveDemoFull`,
`ComprehensiveDemo`, `FirewallRules`, `MultiModule`, `ParentChildUat`, `RefactoringSummary`,
`RoleAssignments`, `SummaryTemplate`.
Root cause mix: summary separator, attribute table separators, firewall/NSG separators,
role assignment description rendering.

---

## Issue 1: SummaryRenderer — summary table header separator + Total row

**File**: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/SummaryRenderer.cs`

**Current** (line 32):
```csharp
writer.TableHeader("Action", "Count", "Resource Types");
// ...
writer.TableRow(["**Total**", $"**{totalText}**", string.Empty]);
// OR non-bold:
writer.TableRow(["Total", totalText, string.Empty]);
```

**Expected** (from all summary-containing snapshots):
```
| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
...
| **Total** | **5** | |
```

Separator formula: `Action`(6)+2=8, `Count`(5)+2=7, `Resource Types`(14)+2=16.
Total row: last cell is `|` with NO space inside (not `|  |`).

**Fix**:
```csharp
writer.Raw("| Action | Count | Resource Types |\n");
writer.Raw("| -------- | ------- | ---------------- |\n");
// ...
if (boldTotal)
    writer.Raw($"| **Total** | **{totalText}** | |\n");
else
    writer.Raw($"| Total | {totalText} | |\n");
```

Also check: `ReportRenderer.cs` wide-path `RenderSummary` — already uses `writer.Raw()` for 
summary table with correct format. Do NOT change that method.

---

## Issue 2: DefaultResourceRenderer — attribute table separators

**File**: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`

`RenderSingleValueTable()` (around line 305) and `RenderBeforeAfterTable()` (around line 342):

**Current** (2-column):
```csharp
if (useKnownAfterApplyFormatting)
{
    writer.Raw("| Attribute | Value |\n");
    writer.Raw("| ----------- | ------- |\n");
}
else
{
    writer.TableHeader("Attribute", "Value");  // produces | --- | --- |
}
```

**Fix** (2-column): Always use Raw, remove the else branch:
```csharp
writer.Raw("| Attribute | Value |\n");
writer.Raw("| ----------- | ------- |\n");
```

**Fix** (3-column): Always use Raw:
```csharp
writer.Raw("| Attribute | Before | After |\n");
writer.Raw("| ----------- | -------- | ------- |\n");
```

Separator formula: `Attribute`(9)+2=11, `Value`(5)+2=7, `Before`(6)+2=8, `After`(5)+2=7.

**Also fix** the static analysis security findings table (line ~427):
`writer.TableHeader("Severity", "Tool", "Attribute", "Finding", "Remediation");`
Expected: `| -------- | ---- | --------- | ------- | ----------- |`
`Severity`(8)+2=10, `Tool`(4)+2=6, `Attribute`(9)+2=11, `Finding`(7)+2=9, `Remediation`(11)+2=13
→ `| ---------- | ------ | ----------- | --------- | ------------- |`

**Also fix** the dynamic child resource headers table (line ~491):
`writer.TableHeader(headers.ToArray());` — check expected format in NSG child table.

---

## Issue 3: azuread_* multiline details format

**File**: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`

Expected format for ALL `azuread_*` resources:
```
<details style=...>

<summary>➕ azuread_user...</summary>
<br>

| Attribute | Value |
...
```

Note: blank line between `<details>` and `<summary>`.

This requires:
1. `ShouldUseMultilineDetailsSummary()` → return `true` for azuread
2. `ShouldUseExtraBlankLineBeforeSummary()` → return `true` for azuread

Current `ShouldUseExtraBlankLineBeforeSummary`:
```csharp
return useKnownAfterApplyFormatting
    && useMultilineDetailsSummary
    && IsKnownAfterApplyAzureAdMemberScenario(change);
```

**Fix** `ShouldUseMultilineDetailsSummary()`: add condition:
```csharp
if (change.Type.StartsWith("azuread_", StringComparison.Ordinal))
    return true;
```

**Fix** `ShouldUseExtraBlankLineBeforeSummary()`: add azuread catch-all:
```csharp
if (change.Type.StartsWith("azuread_", StringComparison.Ordinal))
    return true;
return useKnownAfterApplyFormatting
    && useMultilineDetailsSummary
    && IsKnownAfterApplyAzureAdMemberScenario(change);
```

⚠️ CAUTION: The `azuread_group_member` known-after-apply scenario (`Snapshot_AzureAd_GroupMembersKnownAfterApply`)
already returns true from the KAA path. Need to verify adding the general `azuread_*` condition 
doesn't cause double blank lines for the KAA case.
Looking at the snapshot: `azuread-group-members-known-after-apply.md` — the `azuread_group` resource
uses multiline+blank too. The `azuread_group_member` KAA resources: need to check if they also
expect blank line. The snapshot only showed the azuread_group, not the members. Looking at 
the test name `GroupMembersKnownAfterApply` it likely has group_member with KAA markers.

---

## Issue 4: AzApi custom rendering

**File**: `src/Oocx.TfPlan2Md/Providers/AzApi/Renderers/AzApiResourceRenderers.cs`

Currently delegates to `DefaultResourceRenderer`. Needs complete custom implementation.

**Expected format** (from `azapi-create-complete.md` snapshot):
```markdown
<details style=...>
<summary>➕ azapi_resource <b><code>name</code></b> — ...</summary>
<br>

**Type:** `Microsoft.Automation/automationAccounts@2021-06-22`

📚 [View API Documentation](https://learn.microsoft.com/rest/api/automation/...)

| Attribute | Value |
|-----------|-------|
| name | `🆔 completeAccount` |
| parent_id | `/subscriptions/.../resourceGroups/example-resources` |
| location | `🌍 westeurope` |

**🏷️ Tags:**
 `environment: production`
 `project: demo`
 `owner: team-platform`

#### Body

| Property | Value |
|----------|-------|
| body.properties.disableLocalAuth | `✅ true` |
...

</details>
```

Key differences from DefaultResourceRenderer:
1. `<details>` + `<summary>` on DIFFERENT lines (multiline format)
2. `**Type:** \"resourceType\"` paragraph after `<br>`
3. `📚 [View API Documentation](url)` paragraph
4. "Attribute" table has NO SPACES around separators: `|-----------|-------|` (compact)
   - `Attribute`(9)+2=11 dashes, but NO surrounding spaces
5. **🏷️ Tags:** section rendered one-per-line, NOT inline
6. `#### Body` heading with body properties in SEPARATE table (key = full property path without "body.")
   Wait — snapshot shows `body.properties.disableLocalAuth` which INCLUDES the "body." prefix.
7. Body property table also uses compact format: `|----------|-------|` (Property(8)+2=10)
8. Tags in azapi: multiline, each tag on its own line with leading space:
   ```
   **🏷️ Tags:**
    `environment: production`
    `project: demo`
   ```

⚠️ for `azapi_output_values`: check if it uses DefaultResourceRenderer format or azapi format.

**Model access**: Need to find where `ResourceChangeModel` exposes:
- `change.Type` for the Azure resource type (e.g., `Microsoft.Automation/automationAccounts@2021-06-22`)
  - This is NOT the same as `change.Type` which is `azapi_resource`. Need to find property name.
  - Likely in `change.AzureResourceType` or parsed from attributes
- API doc URL: from `AzureApiDocumentationMapper`
- Body attributes: those prefixed with `body.`
- Non-body attributes: `name`, `parent_id`, `location`
- Tags: those prefixed with `tags.`

**Files to examine**:
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ResourceChangeModel.cs`
- `src/Oocx.TfPlan2Md/Providers/AzApi/AzApiModelBuilder.cs` or similar

---

## Issue 5: NSG Security Rules table separators

**File**: `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs`

Lines 187, 260, 270, 280 — calls to `writer.TableHeader(...)` for NSG and security rule tables.

For NSG rules, ALL columns use uniform 8-dash separators regardless of header length.

**Expected** (from NSG snapshot, need to verify):
```
| -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
```
11 columns, all `--------` (8 dashes).

That's for the line `writer.TableHeader("Change", "Name", "Priority", "Direction", "Access", "Protocol", "Source Addresses", "Source Ports", "Destination Addresses", "Destination Ports", "Description")`.

Column widths: Change(6), Name(4), Priority(8), Direction(9), Access(6), Protocol(8), 
Source Addresses(16), Source Ports(12), Destination Addresses(21), Destination Ports(17), Description(11).

If it were `+2`: 8, 6, 10, 11, 8, 10, 18, 14, 23, 19, 13 (variable lengths = NOT uniform 8).
So NSG uses **uniform 8** regardless of header.

For the firewall application/network rules (lines 367, 377, 387):
```
writer.TableHeader("Rule Name", "Protocols", "Source Addresses", ...)
```
Need to check if firewall uses `+2` or uniform.

---

## Issue 6: Firewall table separators

**File**: `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs`

Lines 367, 377, 387 — firewall rule tables.

Need to check snapshot: `src/tests/.../Snapshots/firewall-rules.md` or similar.

Headers for firewall app rules: `Rule Name`(9), `Protocols`(9), `Source Addresses`(16),
`Source IP Groups`(15), `Target FQDNs`(12), `FQDN Tags`(9), `Description`(11).

If `+2`: `| ----------- | ----------- | ------------------ | ----------------- | -------------- | ----------- | ------------- |`

---

## Issue 7: AzureDevOps variable group table separators

**File**: `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Renderers/AzureDevOpsResourceRenderers.cs`

Line 131: `writer.TableHeader("Name", "Value", "Enabled", "Content Type", "Expires");`
Line 149: `writer.TableHeader("Change", "Name", "Value", "Enabled", "Content Type", "Expires");`

**Expected** (from `azuredevops-snapshot.md` verified above):
```
| Name | Value | Enabled | Content Type | Expires |
| ---- | ----- | ------- | ------------ | ------- |
```

Separator format: EXACT header length (no `+2` padding):
`Name`(4)=4, `Value`(5)=5, `Enabled`(7)=7, `Content Type`(12)=12, `Expires`(7)=7.

For the 6-column variant with "Change":
`Change`(6)=6, `Name`(4)=4, `Value`(5)=5, `Enabled`(7)=7, `Content Type`(12)=12, `Expires`(7)=7.
→ `| ------ | ---- | ----- | ------- | ------------ | ------- |`

**Also**: Sensitive value format must be `(sensitive / hidden)` per code review finding B1.
Need to verify current code behavior.

---

## Issue 8: Refactoring table separator in ReportRenderer

**File**: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`

Line 457: `writer.TableHeader("Operation", "Resource", "Details", "Status");`

**Expected** (from refactoring-summary snapshot):
`| --------- | -------- | ------- | ------ |`
`Operation`(9)=9, `Resource`(8)=8, `Details`(7)=7, `Status`(6)=6 (EXACT, no `+2`).

**Fix**: Replace with:
```csharp
writer.Raw("| Operation | Resource | Details | Status |\n");
writer.Raw("| --------- | -------- | ------- | ------ |\n");
```

---

## Issue 9: Role assignment description rendering

**File**: Unknown — need to find the role assignment renderer.

**Expected** (from `role-assignments.md` snapshot, lines 37-50):
```
Allow DevOps team to read logs from the storage account

| Attribute | Value |
| ----------- | ------- |
...
| description | `Allow DevOps team...` |
```

The description text appears BOTH as a standalone paragraph BEFORE the table AND as a 
`description` attribute row INSIDE the table.

**Current** behavior: only renders `description` as a table attribute (no standalone paragraph).

This suggests there's a role assignment-specific renderer that renders the description text
before the attribute table. Check `AzureRmResourceRenderers.cs` for a role assignment renderer.

---

## Issue 10: CodeAnalysisSectionRenderer table separators

**File**: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/CodeAnalysisSectionRenderer.cs`

Line 48: `writer.TableHeader("Severity", "Count", "Resource Types");`
Line 116: `writer.TableHeader("Severity", "Tool", "Finding", "Remediation");`

Need to verify separator format from static-analysis snapshots.

---

## Implementation Order (Recommended)

1. ✅ Issue 1: Fix `SummaryRenderer.Render()` — universal fix, quick
2. ✅ Issue 2: Fix `DefaultResourceRenderer` attribute separators — affects ~30 tests
3. ✅ Issue 3: Fix `azuread_*` multiline — fixes AzureAD category
4. ✅ Issue 7: Fix AzureDevOps variable table separators + sensitive values
5. ✅ Issue 8: Fix refactoring table
6. ✅ Issue 5/6: Fix NSG/firewall separators  
7. ✅ Issue 9: Fix role assignment description
8. ✅ Issue 10: Fix CodeAnalysisSectionRenderer
9. ✅ Issue 4: Implement AzApi custom renderer (most complex, last)

---

## Files to Modify

| File | Issues |
|------|--------|
| `SummaryRenderer.cs` | 1 |
| `DefaultResourceRenderer.cs` | 2, 3 |
| `AzApiResourceRenderers.cs` | 4 |
| `AzureRmResourceRenderers.cs` | 5, 6 |
| `AzureDevOpsResourceRenderers.cs` | 7 |
| `ReportRenderer.cs` | 8 |
| `CodeAnalysisSectionRenderer.cs` | 10 |
| Role assignment renderer (TBD) | 9 |

---

## AzApi Model Investigation Needed

Before implementing Issue 4, need to find:
- How to get the Azure resource type (e.g., `Microsoft.Automation/automationAccounts@2021-06-22`)
  from `ResourceChangeModel` — NOT the same as `change.Type` = `azapi_resource`
- How `AzureApiDocumentationMapper` is accessed from a renderer
- What attributes prefix the "body" properties (e.g., `body.properties.X`)
- How tags are stored in the model for azapi resources

Likely files to check:
- `src/Oocx.TfPlan2Md/Providers/AzApi/AzApiModelBuilder.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ResourceChangeModel.cs`
- `src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMapper.cs`

---

## Key Observations About Separator Patterns

| Table Context | Separator Style | Formula |
|---------------|-----------------|---------|
| Summary table | Padded | `header_len + 2` |
| Attribute table (Attribute/Value) | Padded | `header_len + 2` |
| Attribute table (Attribute/Before/After) | Padded | `header_len + 2` |
| Static analysis findings | Padded | `header_len + 2` |
| Firewall rule tables | Padded | `header_len + 2` |
| AzApi attribute table | Compact, Padded | `header_len + 2` but NO spaces around dashes |
| NSG security rules | Uniform | 8 dashes for all columns |
| AzureDevOps variable table | Exact | `header_len` (no padding) |
| Refactoring table | Exact | `header_len` (no padding) |
| Child resource tables (Members, etc.) | Need to check | Likely `header_len + 2` |

---

## Current Test Run State

As of last run: 48 failures, 1067 passing, 0 skipped.
Non-snapshot tests: all pass.

Run command: `scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/`
