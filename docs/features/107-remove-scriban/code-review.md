# Code Review: Remove Scriban and Replace with Pure C# Rendering

## Summary

Reviewed feature 107 against the specification, architecture, test plan, and task list. The
core infrastructure (MarkdownWriter, registries, context, pipeline wiring) is well-implemented
and all 1115 tests pass. However, multiple acceptance criteria are directly violated:
several provider-specific renderers silently delegate to `DefaultResourceRenderer` instead of
implementing their own rendering, producing user-visible and security-impacting regressions
relative to the Scriban templates they replaced.

---

## Verification Results

| Check | Status | Notes |
|-------|--------|-------|
| Tests | ✅ Pass | 1115 passed, 0 failed, 0 skipped |
| Coverage | ✅ Pass | Line 86.75% (≥84.48%), Branch 78.35% (≥72.80%) |
| Build | ✅ Pass | `dotnet build` succeeded |
| Docker | ✅ Pass | Multi-arch image builds successfully |
| Markdownlint | ❌ **2 errors** | `MD031/blanks-around-fences` in `artifacts/comprehensive-demo.md` |
| CHANGELOG.md | ✅ Not modified | Correct |

---

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|:-----------:|:------:|-------|
| `Scriban` NuGet package reference removed | ✅ | ✅ TC-S01 | Confirmed no `Scriban` assembly reference |
| All 27 `.sbn` template files deleted | ✅ | ✅ TC-S02 | Zero `.sbn` files in `src/` |
| `AotScriptObjectMapper`, `TemplateLoader`, `TemplateResolver` deleted | ✅ | ✅ TC-S02 | Deleted |
| `TrimmerRootDescriptor.xml` has no Scriban entries | ✅ | ✅ TC-S03 | File cleaned |
| No C# file imports `using Scriban` or `Scriban.*` | ✅ | ✅ TC-S04 | Zero matches in `src/Oocx.TfPlan2Md/` |
| All existing snapshot tests pass without snapshot modification | ❌ | ❌ TC-S05 | 50+ snapshot files modified — see Blockers B3–B4 |
| NativeAOT binary builds successfully | ✅ | ✅ TC-S06 | Docker build confirmed |
| Zero third-party NuGet references | ✅ | ✅ TC-S01 | Confirmed |
| Rendering logic is statically typed | ⚠️ | | `IScenarioRenderContext` cast pattern reintroduces runtime type checks — see Major M3 |
| Provider modules implement typed `IResourceRenderer` | ✅ | ✅ TC-S07 | All modules register renderers |

**Spec Deviations Found:**

- TC-S05 violated: 50+ snapshot files changed; several changes are content regressions, not
  cosmetic equivalents (firewall rules, variable group secrets, summary template content)
- The spec requires "byte-for-byte identical" output; multiple provider renderers fall back to
  `DefaultResourceRenderer`, producing fundamentally different markdown

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Secrets/sensitive values | ❌ **Fail** | AzureDevOps secret variable values exposed as plain text |
| Summary template | ❌ **Fail** | Refactoring Summary section and filtered-changes note missing |
| Firewall rule collections | ❌ **Fail** | Rich rule table replaced by generic flat attribute diff |
| Role assignment create with principal mapping | ⚠️ Partial | Works for narrow `create` + no-attribute case only |
| Large value code fences | ❌ **Fail** | MD031: missing blank line after closing fence |
| AzureAD display_name icon | ⚠️ | `👤` icon stripped from `display_name` table cell value |

---

## Review Decision

**Status: Changes Requested**

---

## Snapshot Changes

- Snapshot files changed: **Yes — 50+ files**
- Commit message token `SNAPSHOT_UPDATE_OK` present: **Yes** (commit `6f517161`)
- Why the diff is correct: **Insufficient justification.** The specification requires byte-for-
  byte identical output, but multiple snapshot diffs reveal content regressions (firewall rule
  tables replaced by flat attributes, variable group secrets exposed, summary template sections
  missing, role assignment display degraded). These are not "equivalent" formatting changes.
  Cosmetic diffs (blank line after `<details>`, table separator width) are acceptable; content-
  level diffs require fixes and re-snapshot.

---

## Issues Found

### Blockers

**B1 — Security regression: AzureDevOps secret variable values are exposed**

`VariableGroupRenderer` in
[src/Oocx.TfPlan2Md/Providers/AzureDevOps/Renderers/AzureDevOpsResourceRenderers.cs](../../../src/Oocx.TfPlan2Md/Providers/AzureDevOps/Renderers/AzureDevOpsResourceRenderers.cs)
delegates entirely to `DefaultResourceRenderer`. The old `variable_group.sbn` Scriban template
explicitly masked secret variable values as `(sensitive / hidden)`. The new renderer passes
the raw model value through, exposing secrets in the markdown output.

**Evidence from snapshot diff (`azuredevops-snapshot.md`):**
```
# OLD (correct)
| `API_KEY` | `(sensitive / hidden)` | - | - | - |

# NEW (regression — secret exposed)
| secret_variable[0].value | `secret-value` |
```

**Fix required:** `VariableGroupRenderer` must implement secret masking, rendering
`(sensitive / hidden)` for any attribute whose name matches the secret variable value field.
The old Scriban template in
[uat-repos/github/.../variable_group.sbn](../../../uat-repos/github/src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azuredevops/variable_group.sbn)
is the reference implementation.

---

**B2 — `summary` template is missing Refactoring Summary section and filtered-changes note**

`MarkdownRenderer.RenderSummaryTemplate` at
[src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs#L164)
renders: header → summary table → code-analysis summary. It is missing:

1. `## Refactoring Summary` table (import and move operations)
2. `> ℹ️ N resource(s) with only filtered changes…` note

The old `summary.sbn` (verified via `git show origin/main:src/.../Templates/summary.sbn`)
explicitly included both sections. The snapshot diff for `summary-template.md` confirms the
regression (removed 13 lines replacing them with nothing).

**Fix required:** Add rendering of `model.RefactoringOperations` and the filtered-resource-
count note to `RenderSummaryTemplate`, matching the logic in `ReportRenderer.Render`.

---

**B3 — Firewall rule collection rendering regressed to generic flat attributes**

`FirewallNetworkRuleRenderer` and `FirewallAppRuleRenderer` in
[src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs](../../../src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs#L159)
both just delegate to `DefaultResourceRenderer`. The old Scriban templates
(`firewall_network_rule_collection.sbn`, `firewall_application_rule_collection.sbn`) rendered:
- A header line showing **Collection**, **Priority**, **Action**
- A `#### Rule Changes` / `#### Network Rules` table with columns `Change | Rule Name | Protocols | Source | Destination | Ports | Description`

The new output replaces all of this with a generic `| Attribute | Before | After |` table with opaque keys like `rule[1].destination_addresses[0]`.

**Evidence from snapshot diff (`firewall-rules.md`):** The loss of 30+ structured lines is
confirmed; the test now passes only because the snapshot was updated to accept the regressed
output.

**Fix required:** Implement `FirewallNetworkRuleRenderer.Render` with the structured rule
table. TC-ARM-09 through TC-ARM-12 in the test plan define the expected behavior. The old
Scriban template is the reference implementation.

---

**B4 — Markdownlint MD031 errors in comprehensive-demo.md**

Running `scripts/markdownlint.sh artifacts/comprehensive-demo.md` reports:

```
artifacts/comprehensive-demo.md:136 error MD031/blanks-around-fences
artifacts/comprehensive-demo.md:206 error MD031/blanks-around-fences
```

**Root cause:** `ScribanHelpers.CodeFence` in
[src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValues.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValues.cs#L228)
uses `sb.Append("```")` for its closing fence (no trailing newline). When the returned string
is emitted via `writer.Raw(...)` followed by `writer.BlankLine()`, only one `\n` follows the
closing fence. The next element (`</details>` or heading) then abuts the fence with no blank
line, violating MD031.

**Fix required:** Change `sb.Append("```")` → `sb.AppendLine("```")` in `CodeFence`. This
ensures `writer.Raw(fence) + writer.BlankLine()` produces the required blank line.  Also
verify `MarkdownWriter.Code` at line 161 (used for JSON output sections) applies the same fix.

---

**B5 — Technical Writer documentation changes uncommitted**

The following files have uncommitted changes (unstaged):

- `README.md`
- `docs/architecture.md`
- `docs/features.md`
- `docs/features/107-remove-scriban/work-protocol.md`

All agent work must be committed before code review. The Technical Writer's work-protocol
entry is also only present in the working directory, not in any commit.

**Fix required:** Commit all Technical Writer changes in a single commit before re-requesting
review.

---

### Major Issues

**M1 — `NsgRenderer` delegates to default, losing structured security-rule rendering**

`NsgRenderer` in
[src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs](../../../src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs#L153)
just delegates to `DefaultResourceRenderer`. The old `network_security_group.sbn` template
rendered a structured inline security-rule table. TC-ARM-05 through TC-ARM-08 in the test
plan specify this behavior but are currently untested (the tests pass only because the snapshots
were updated to accept the generic output).

**Fix required:** Implement `NsgRenderer.Render` with the NSG-specific rule table.

---

**M2 — `RoleAssignmentRenderer` only handles the narrow create/no-attributes case**

The compatibility rendering path in `RoleAssignmentRenderer.ShouldUseCompatibilityRoleAssignmentRendering` at
[src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs](../../../src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs#L135)
requires all of: `action == "create"`, `AttributeChanges.Count == 0`, no children, no tags,
no code-analysis findings. All other scenarios (updates, deletes, creates with attributes) fall
through to `DefaultResourceRenderer`, losing the scope-and-principal summary display. The
snapshot diff for `role-assignments.md` confirms the regression for the `create_with_description`
and `update_assignment` cases.

**Fix required:** Extend `RoleAssignmentRenderer` to cover update/delete scenarios and creates
that have attribute data.

---

**M3 — `IScenarioRenderContext` cast pattern is fragile and untested**

`DefaultResourceRenderer.Render` performs several `context as IScenarioRenderContext` casts
(lines ~38–42 of the method) to detect compatibility scenarios. This pattern:
- Reintroduces runtime type checking that the spec aimed to eliminate
- Is not in the architecture document or test plan
- Has no dedicated unit tests — the scenario detection logic (`ShouldUseKnownAfterApplyFormatting`,
  `ShouldUseEphemeralOpenFormatting`) at the bottom of `DefaultResourceRenderer.cs` is a
  heuristic blob that's only exercised implicitly through snapshot tests

**Fix required:** Add explicit unit tests for each scenario detection condition, covering the
boundary cases that trigger or suppress the compatibility paths.

---

**M4 — `FirewallAppRuleRenderer` similarly unimplemented (TC-ARM-13 to TC-ARM-16)**

`FirewallAppRuleRenderer` delegates to `DefaultResourceRenderer`. Test cases TC-ARM-13 through
TC-ARM-16 in the test plan specify dedicated behavior for `azurerm_firewall_application_rule_collection`.

**Fix required:** Implement `FirewallAppRuleRenderer.Render` with the application-rule table
format from the old `firewall_application_rule_collection.sbn` Scriban template.

---

### Minor Issues

**m1 — Global table separator format changed from padded to minimal**

All 50+ snapshots have `| -------- |` (padded) → `| --- |` (minimal). While GitHub and Azure
DevOps render both identically, the spec says byte-for-byte identical. After all Blockers are
fixed and content is restored, assess whether the padded format should be restored in
`TableHeader` to minimize snapshot delta.

---

**m2 — Work-protocol references non-existent file**

The last Developer entry in `work-protocol.md` lists
`src/.../MarkdownGeneration/Rendering/ICompatibilityRenderContext.cs` as a produced artifact,
but this file does not exist on disk (it was renamed/merged into `IScenarioRenderContext.cs`).
The work-protocol should be corrected when committing the Technical Writer's changes.

---

**m3 — AzureAD `display_name` icon stripped from table value**

In `azuread-snapshot.md`, the old template rendered `| display_name | \`👤 Jane Doe\` |` but
the new output is `| display_name | \`Jane Doe\` |` (icon removed). The `👤` was applied by the
`azuread_user.sbn` template. This is a minor cosmetic regression that should be restored in
`AzureAdResourceRenderers`.

---

## Critical Questions Answered

- **What could make this code fail?**
  The `IScenarioRenderContext` cast-based scenario detection in `DefaultResourceRenderer`
  (`ShouldUseKnownAfterApplyFormatting`, `ShouldUseEphemeralOpenFormatting`) uses heuristics
  based on model properties. Any new test plan or resource with similar structural properties
  could accidentally trigger compatibility formatting.

- **What edge cases might not be handled?**
  All firewall, NSG, and AzureDevOps renderers delegate to `DefaultResourceRenderer`. Any
  scenario involving these resource types that was previously handled by a specialized template
  (including error paths, large values, sensitive fields) is now handled generically.

- **Are all error paths tested?**
  The `MarkdownWriter.Code` path via `ReportRenderer` for JSON output sections has no test
  verifying the blank line after the fence. MD031 tests catch the symptom but not the cause.

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ❌ |
| Spec Compliance | ❌ |
| Code Quality | ⚠️ |
| Architecture | ⚠️ |
| Testing | ❌ |
| Documentation | ❌ (uncommitted) |

---

## Work Protocol & Documentation Verification

| Item | Status | Notes |
|------|--------|-------|
| `work-protocol.md` exists | ✅ | Present |
| All required agents logged | ⚠️ | Technical Writer entry exists only in uncommitted working-directory state |
| `docs/features.md` updated | ⚠️ | Correct content, uncommitted |
| `docs/architecture.md` updated | ⚠️ | Correct content, uncommitted |
| `README.md` updated | ⚠️ | Correct content, uncommitted |
| CHANGELOG.md NOT modified | ✅ | Correct |

---

## Next Steps

Changes are requested. The Developer must address the following before re-review:

1. **[B1]** Implement secret masking in `VariableGroupRenderer` (security fix — highest priority)
2. **[B2]** Restore Refactoring Summary and filtered-changes note in `RenderSummaryTemplate`
3. **[B3]** Implement `FirewallNetworkRuleRenderer` with structured rule table
4. **[B4]** Fix `LargeValues.CodeFence` trailing newline (markdownlint)
5. **[B5]** Commit Technical Writer documentation changes
6. **[M1]** Implement `NsgRenderer` with security-rule table
7. **[M2]** Extend `RoleAssignmentRenderer` to handle update/delete/create-with-attributes
8. **[M4]** Implement `FirewallAppRuleRenderer` with application-rule table
9. **[M3]** Add unit tests for `IScenarioRenderContext` scenario detection logic

After fixes: regenerate snapshots with `scripts/update-test-snapshots.sh`, confirm
markdownlint passes, and re-request code review.
