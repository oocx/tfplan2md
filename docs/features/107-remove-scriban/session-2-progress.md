# Session 2 Progress — Snapshot Fix Continuation

## Starting State
- 39 snapshot test failures at session start (down from 48 original)
- Goal: fix code so all snapshot tests pass WITHOUT modifying snapshot files

## Fixes Applied This Session

### Fix 1 — `_No attribute changes._` condition (already done in prev session)
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`
- Changed condition to `change.ChildResourceGroups.Count == 0`

### Fix 2 — Child table separator dashes (Scriban used hardcoded 8 dashes)
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`
- Old code: `writer.TableHeaderPadded(headers.ToArray())` — used `header_len + 2` dashes
- New code: hardcoded `"--------"` for all columns except "Terraform Resource" which uses `"--------------------"` (20 dashes)
- This matches the original `_child_resources.sbn` Scriban template exactly
- Fixes: `Snapshot_AzureDevOps_TeamMembers`, `Snapshot_AzureAd_GroupMembers`, etc.

### Fix 3 — Warning text line break
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`
- Old: single-line paragraph `"⚠️\u00A0**Warning:** This resource has children managed..."`
- New: split across two lines with `\n` to match Scriban template which outputted them as two separate lines
- The `Paragraph()` method appends text + `\n`, so splitting with `\n` creates a two-line paragraph

### Fix 4 — AzureAD `display_name` icon — resource-type-specific lookup
- Files: `DefaultResourceRenderer.cs`, `AzureAdResourceRenderers.cs`
- Old code always passed `change.Type` to `FormatAttributeValueTableWithRegistryResource`
- Problem: `group.sbn`, `group_member.sbn`, `service_principal.sbn`, etc. all used `format_attribute_value_table` (WITHOUT resource type), only `user.sbn` used `format_attribute_value_table_resource` (WITH resource type)
- Fix: Added `bool useResourceTypeForAttributeIcons = false` param to `DefaultResourceRenderer` constructor and threaded through `RenderAttributeTable`, `RenderSingleValueTable`, `RenderBeforeAfterTable`
- `UserRenderer` sets `useResourceTypeForAttributeIcons: true`; all other AzureAD renderers use default `false`
- Result: `display_name` gets `👤` icon for `azuread_user`, but NOT for `azuread_group` (which expects raw `Platform Engineers` without icon in the snapshot)

### Fix 5 — AzureDevOps VariableGroup missing blank line (applied in prev session)
- File: `AzureDevOpsResourceRenderers.cs`
- Added `writer.BlankLine()` after `**Variable Group:**` paragraph

### Fix 6 — AzureRM RoleAssignment null row skipping (applied in prev session)
- File: `AzureRmResourceRenderers.cs`
- Skip rows where value is null (matches Scriban `{{ if attr.after_table }}` null=false behavior)

## Current Test Results
- Before session: 39 failures
- After fixes: **35 failures** remaining
- Non-AzApi tests passing: AzureAD (4), TeamMembers, GroupMembers (pass now)

## Remaining Failures (35 total)

### 1. AzApi — 30 tests
All `Snapshot_Azapi*` tests fail. These need a custom `AzApiResourceRenderer` implementation.
Status: NOT INVESTIGATED YET this session.

### 2. AzureDevOps_GroupMembers — 1 test
Snapshot expects `_No attribute changes._` text BEFORE the child resource group section.
The `azuredevops-group-members.md` snapshot shows:
```
_No attribute changes._

#### Members
```
But current code does NOT print `_No attribute changes._` when there are child resource groups (per Fix 1).
**Root cause**: The `_resource.sbn` Scriban template checks `small_attrs.size == 0 && large_attrs.size == 0 && tags_badges == null` — independent of `child_resource_groups`! Child resources are rendered after the "no attribute changes" marker. So "No attribute changes" SHOULD appear even when there are child resource groups (when there are no actual attribute changes).
**Fix needed**: In `DefaultResourceRenderer.cs`, change the condition for printing `_No attribute changes._` back to check `smallAttributes.Length == 0 && largeAttributes.Length == 0 && TagsBadges is null/empty` — WITHOUT checking `change.ChildResourceGroups.Count == 0`.

Wait — but that would break `azuread-group-members.md` which has child resources AND starts directly with the child table (no "No attribute changes" before it). Let me re-examine...

Looking at the snapshots:
- `azuread-group-members.md` — AzureAD group with child members. Has children, no attributes. Does NOT show "_No attribute changes._". Uses `group.sbn` template (NOT `_resource.sbn`).
- `azuredevops-group-members.md` — AzureDevOps group with child members. Has children, no attributes. DOES show "_No attribute changes._". Uses `_resource.sbn` (default template).

So the difference is: AzureAD `group.sbn` omitted `_No attribute changes._` because it's a custom template that doesn't include that check. The default `_resource.sbn` template DOES show it regardless of child resources.

**The Fix 1 I applied was WRONG for non-AzureAD resources.** The condition should be:
```
smallAttributes.Length == 0 && largeAttributes.Length == 0 && string.IsNullOrWhiteSpace(change.TagsBadges)
```
(no ChildResourceGroups check at all — same as Scriban)

But AzureAD group/group_member/service_principal renderers that use the default renderer need to suppress it. Looking at `group.sbn`:
```
{{ if small_attrs.size == 0 && large_attrs.size == 0 && (change.tags_badges == null || change.tags_badges == "") }}
_No attribute changes._
{{ end }}
```
Actually `group.sbn` DOES have this check too! Let me re-examine...

Actually looking at `group.sbn` content again — it explicitly has the same condition. So the group IS showing "_No attribute changes._" in the Scriban output? But `azuread-group-members.md` snapshot does NOT have it.

Wait, `azuread-group-members.md` has a resource with action "update" (summary says `🔄 azuread_group`) and it has 0 attribute changes in the table but it DOES have a summary HTML with member count. The resource has `AttributeChanges.Count == 0` but has child resources.

LOOKING at the `group.sbn` template — it says `{{ if small_attrs.size == 0 && large_attrs.size == 0 ...}}` — but the action is "update" and there are literally NO attribute changes for that group (the member changes are child resources). So this SHOULD print "_No attribute changes._" per Scriban...

But the `azuread-group-members.md` snapshot does NOT show "_No attribute changes._" — it goes straight to the `#### Members` section.

This means AzureAD `group.sbn` must be handling this differently. Let me re-check...

Actually wait — it's possible that the AzureAD group child resources get merged and the resource appears as a "no-op" parent. In that case, `isNoOpParentWithChildren = true`. The old Fix 1 condition was:
`(change.ChildResourceGroups.Count == 0 || !isNoOpParentWithChildren)`

The current (after Fix 1) condition is just:
`change.ChildResourceGroups.Count == 0`

The CORRECT behavior from Scriban is: show "_No attribute changes._" regardless of child resources, EXCEPT when this is an AzureAD resource. But in `_resource.sbn`, there's NO exception for AzureAD.

Unless... `AzureAdDelegatingRenderer.ShouldUseMultilineDetailsSummary` returns true, which changes the format. But that doesn't affect the "_No attribute changes._" text...

Actually wait — I need to re-read the AzureAD `group.sbn` more carefully. The group template DOES have:
```
{{ if small_attrs.size == 0 && large_attrs.size == 0 && (change.tags_badges == null || change.tags_badges == "") }}
{{ ... }}
_No attribute changes._
{{ end }}
```

This would print "_No attribute changes._" for the AzureAD group too! So why doesn't `azuread-group-members.md` have it?

Maybe the azuread group resource actually HAS some attributes that show up in `small_attrs`? Let me check the plan data...

OR maybe the AzureAD group NO-OP detection suppresses something differently. Actually, looking at `AzureAdGroupSummaryRebuilder.cs` — when the group is a no-op parent, it rebuilds the summary. Let me check if there's a `_No attribute changes._` suppression there.

I DON'T know for certain. Need to investigate `azueread-group-members-plan.json` to see if the group has attributes.

### 3. AzureDevOps_Comprehensive — 1 test
Not yet diagnosed.

### 4. ParentChildUat — 1 test  
Not yet diagnosed.

### 5. ComprehensiveDemo — 1 test
Not yet diagnosed.

### 6. ComprehensiveDemoFull — 1 test
Not yet diagnosed.

## What To Do Next Session

### Step 1: Fix the `_No attribute changes._` logic

The original condition in DefaultResourceRenderer was:
```csharp
if (smallAttributes.Length == 0
    && largeAttributes.Length == 0
    && change.ChildResourceGroups.Count == 0  // <-- Fix 1 added this
    && string.IsNullOrWhiteSpace(change.TagsBadges))
```

The Scriban `_resource.sbn` template has:
```scriban
{{ if small_attrs.size == 0 && large_attrs.size == 0 && (change.tags_badges == null || change.tags_badges == "") }}
_No attribute changes._
{{ end }}
```
NO child resource groups check — it always shows when no attrs/tags.

BUT `azuread-group-members.md` doesn't show it. This could be because:
a) The AzureAD group resource has attrs that get filtered somehow
b) The AzureAD group uses a custom template that differs from `_resource.sbn`

**Investigation needed**: Look at `azuread-group-members-plan.json` to see if the `azuread_group` resource has any attribute changes. If it has attrs but all format to empty string `""`, then the Scriban template's `{{ if value != "" }}` would skip them but `small_attrs.size` would still be > 0 (since they're in the list).

Actually that's key: Scriban `{{ for attr in small_attrs }}{{ if value != "" }}...{{ end }}` means attrs are in `small_attrs` but nothing is rendered. But `small_attrs.size > 0` is checked first, so the table header IS printed, but then no rows appear... OR maybe the condition is AROUND the table header too. Let me check `group.sbn`:

```scriban
{{ if small_attrs.size > 0 }}
{{ if change.action == "create" }}
| Attribute | Value |
...
{{ end }} {{# end if small_attrs.size > 0 #}}
```

So if `small_attrs.size > 0`, the table is printed. For the group to NOT show "_No attribute changes._", it must have at least one attr in `small_attrs`.

**Conclusion**: In `azuread-group-members-plan.json`, the `azuread_group` resource likely HAS attribute changes (like `display_name`), so `small_attrs.size > 0` and consequently "_No attribute changes._" is not shown. The attribute table is rendered but the only attr has `value == ""` so no rows appear... OR it does have visible attributes.

Wait, looking at `azuread-group-members.md`:
```
<summary>🔄 azuread_group ... </summary>
<br>

#### Members
```
There is NO attribute table at all! So there ARE no attributes — `small_attrs.size == 0`. Yet "_No attribute changes._" is also not shown.

This means the group.sbn template DOES check `small_attrs.size == 0` for `_No attribute changes._` but must NOT show it here. Looking again at `group.sbn`:
```
{{ if small_attrs.size == 0 && large_attrs.size == 0 && (change.tags_badges == null || change.tags_badges == "") }}
...
_No attribute changes._
{{ end }}
```

Hmm — so for the AzureAD group with no attrs and child members, this condition IS true and "_No attribute changes._" SHOULD be printed. Unless the AzureAD group template has been MODIFIED to NOT show it when there are child resources.

Let me check if there's an UPDATED version (from the Scriban removal commit) of group.sbn... Actually the templates were removed in commit 6f517161. I need to check what commit BEFORE 6f517161 last touched group.sbn.

Actually — I need to just check `azuread-group-members.md` context more carefully. The snapshot file IS the ground truth. If it doesn't have "_No attribute changes._", then the code must NOT print it. The question is: what CURRENTLY produces that snapshot?

Currently (feature/107-remove-scriban branch), AzureAD group uses `GroupRenderer` → `AzureAdDelegatingRenderer` → `DefaultResourceRenderer`. The `DefaultResourceRenderer` currently has:
```csharp
if (smallAttributes.Length == 0
    && largeAttributes.Length == 0
    && change.ChildResourceGroups.Count == 0  // <-- Fix 1
    && string.IsNullOrWhiteSpace(change.TagsBadges))
```

So with Fix 1, it won't print when there are child resource groups — which matches `azuread-group-members.md` (no "_No attribute changes._"). But it BREAKS `azuredevops-group-members.md` which DOES use the default template and DOES expect "_No attribute changes._" even with child resources.

**The real fix**: The AzureAD group renderer needs to SUPPRESS "_No attribute changes._" explicitly. This means the `DefaultResourceRenderer` should NOT have the `ChildResourceGroups.Count == 0` check (revert Fix 1 to original Scriban behavior), and instead AzureAD group/group_without_members renderer should use a flag to suppress it when there are child resources.

OR: Look at actual Scriban `group.sbn` output... Actually re-reading `group.sbn`:
```scriban
{{ if small_attrs.size == 0 && large_attrs.size == 0 && (change.tags_badges == null || change.tags_badges == "") }}
{{ if change.has_whole_resource_unknown_after_apply }}
_(all values known after apply)_
{{ else }}
_No attribute changes._
{{ end }}
{{ end }}
```

This template WOULD print "_No attribute changes._" for the group. But if it used to be part of the output, then `azuread-group-members.md` should have it. Since it doesn't... maybe the snapshot was created AFTER a change that suppressed it? Or maybe the AzureAD group has a `ShouldUseMultilineDetailsSummary = true` which changes blank line behavior such that the Scriban template skips it.

Actually wait — I see it now in `group.sbn`. After `_child_resources.sbn` is included, which renders child tables. But the MESSAGE "_No attribute changes._" is printed BEFORE child resources in `_resource.sbn`, whereas in `group.sbn` it's printed **between the attribute table section and the child resources include**. Let me re-check:

Looking at `group.sbn`:
1. `{{ if small_attrs.size > 0 }}` — attribute table
2. `{{ if change.tags_badges }}` — tags
3. `{{ if small_attrs.size == 0 && large_attrs.size == 0 && ... }}_No attribute changes._{{ end }}`
4. `{{ include "/_child_resources.sbn" }}`
5. child stuff...

So YES, in `group.sbn`, "_No attribute changes._" IS printed BEFORE child resources when there are no attrs. Yet the snapshot doesn't have it. This is contradictory unless the snapshot was created incorrectly, OR the group in the test data DOES have attributes.

**TO INVESTIGATE**: Check `azuread-group-members-plan.json` — does the `azuread_group` resource have any `attribute_changes` in the plan JSON?

### Investigation needed for other failing tests

- Run `Snapshot_AzureDevOps_Comprehensive_MatchesBaseline` diff
- Run `Snapshot_ParentChildUat_MatchesBaseline` diff  
- Run `Snapshot_ComprehensiveDemo_MatchesBaseline` diff

### AzApi — 30 tests

Investigate what the diff looks like for a simple AzApi test (e.g. `Snapshot_AzapiCreateMinimal_MatchesBaseline`).

Key files to look at:
- `src/Oocx.TfPlan2Md/Providers/AzApi/Renderers/` — does a renderer exist here?
- Old Scriban templates: `6f517161^:src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn`
- Old helpers: `6f517161^:src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.cs`

## Key Files

| File | Purpose |
|------|---------|
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs` | Main renderer — most fixes are here |
| `src/Oocx.TfPlan2Md/Providers/AzureAD/Renderers/AzureAdResourceRenderers.cs` | AzureAD delegates; UserRenderer uses `useResourceTypeForAttributeIcons: true` |
| `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Renderers/AzureDevOpsResourceRenderers.cs` | VariableGroup, BuildDefinition renderers |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs` | RoleAssignment renderer |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/MarkdownWriter.cs` | TableHeaderPadded, TableRow, Paragraph, BlankLine |
| `src/Oocx.TfPlan2Md/Providers/AzApi/` | AzApi provider — renderer may need to be created |

## Test Commands

```bash
# Run all snapshot tests
scripts/test-with-timeout.sh -- dotnet test --project tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter "/*/*/*/Snapshot*"

# Run specific test
scripts/test-with-timeout.sh -- dotnet test --project tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter "/*/*/*/Snapshot_AzureDevOps_GroupMembers_MatchesBaseline"

# Get diff output from failing test
scripts/test-with-timeout.sh -- dotnet test ... 2>&1 | grep -E "^  (Line [0-9]|  [+-])" | head -40

# List failing tests
scripts/test-with-timeout.sh -- dotnet test ... 2>&1 | grep "failed Snapshot_"
```

## Important Notes

1. **NEVER modify snapshot files** in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/`
2. The `_No attribute changes._` question is the TRICKY one — need to understand per-template behavior
3. AzApi is the biggest body of work (30 tests)
4. The Scriban templates are accessible in git via `git show 6f517161^:path/to/file.sbn`
5. The migration commit is `6f517161` ("refactor: remove Scriban and migrate to pure C# rendering")
