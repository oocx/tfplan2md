# Feature Analysis: Azure DevOps Has-Changes Variable

## Overview

When the render target is Azure DevOps, tfplan2md should emit an Azure DevOps logging command
to set a pipeline variable named `tfplan2md_haschanges` with the value `true` or `false`,
reflecting whether the analysed Terraform plan contains any actionable changes after all
configured filters have been applied.

This document provides a codebase investigation for the Developer implementing this feature.

---

## Codebase Investigation

### 1. Azure DevOps Render Target — How It Works

#### `RenderTarget` enum
**File:** `src/Oocx.TfPlan2Md/RenderTargets/RenderTarget.cs`

```csharp
internal enum RenderTarget
{
    GitHub,
    AzureDevOps   // The DEFAULT value (line 142 in CliParser.cs)
}
```

**Key finding:** Azure DevOps is the *default* render target. No explicit `--render-target` flag
is needed for the standard Azure DevOps pipeline use-case.

#### `AzureDevOpsDiffFormatter`
**File:** `src/Oocx.TfPlan2Md/RenderTargets/AzureDevOps/AzureDevOpsDiffFormatter.cs`

This class handles the *diff formatting* for inline HTML within PR comments. It is only
concerned with rendering; it has no role in the logging-command feature. The new feature is
orthogonal to this class.

#### CLI parsing
**File:** `src/Oocx.TfPlan2Md/CLI/CliParser.cs` — lines 142, 257–266

```csharp
var renderTarget = RenderTarget.AzureDevOps; // default

case "--render-target":
    renderTarget = ParseRenderTarget(args[++i]);
    break;
```

The parsed `RenderTarget` value flows into `CliOptions.RenderTarget`.

---

### 2. CLI Processing and Workflow Entry Point

**File:** `src/Oocx.TfPlan2Md/ProgramEntry.cs`

`ProgramEntry.RunWorkflowAsync` is the top-level orchestrator. The relevant workflow steps
in order are:

```
1. Read CliOptions (render target, output file, etc.)
2. Compose services (CompositionRoot.ComposeServices)
3. Read input JSON
4. Parse Terraform plan → TerraformPlan
5. Build report model → ReportModel  ← "has changes" is determinable HERE
6. Render to markdown
7. Append debug section (if --debug)
8. Write markdown to file OR Console.WriteLine   ← logging command emitted AFTER this
9. Handle code-analysis failure threshold (may return exit code 10)
10. Return exit code 0
```

**The optimal insertion point for the ADO logging command is between steps 8 and 9**
(after the markdown output is written, before the code-analysis failure check).

Currently, the `model` variable is in scope at step 8, so `model.Summary` and
`model.FilteredResourceCount` are available.

---

### 3. "Has Changes" — What It Means and How to Detect It

#### The `SummaryModel`
**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/SummaryModel.cs`

| Property | Meaning |
|---|---|
| `ToAdd.Count` | Resources to be created |
| `ToChange.Count` | Resources to be updated or unknown |
| `ToDestroy.Count` | Resources to be deleted or forgotten |
| `ToReplace.Count` | Resources to be replaced (destroy+create) |
| `NoOp.Count` | Resources with no planned change |
| `Total` | Sum of above four (excludes NoOp) |

**Important:** `Summary.Total` is calculated in `ReportModelBuilder.Build.cs` (lines 44–48)
**before** the `--ignore-azure-id-case-changes` display filter is applied. It already
excludes no-op resources but does NOT exclude resources whose changes were suppressed by the
casing filter.

#### The `FilteredResourceCount` property
**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs` — line 134

```csharp
public int FilteredResourceCount { get; init; }
```

This is set in `ReportModelBuilder.Build.cs` (line 75):
```csharp
var filteredResourceCount = afterNoOpFilter.Count - displayChanges.Count;
```

It holds the number of resources removed from the display list solely because
`--ignore-azure-id-case-changes` suppressed all their attribute changes. These resources
appear in `Summary.Total` but are absent from `model.Changes`.

#### "Has changes after all filters" — the correct formula

Given the above, the most accurate post-filter has-changes check is:

```csharp
var hasChanges = model.Summary.Total - model.FilteredResourceCount > 0;
```

This evaluates to `true` when at least one resource with real Terraform-level changes
(create / update / delete / replace) survived all filtering and will appear in the report.

**Worked examples:**

| Scenario | `Summary.Total` | `FilteredResourceCount` | `hasChanges` |
|---|---|---|---|
| Empty plan (no changes) | 0 | 0 | `false` |
| Plan with 3 additions | 3 | 0 | `true` |
| Plan with 1 casing-only update (filter on) | 1 | 1 | `false` |
| Plan with 1 casing-only + 1 real update | 2 | 1 | `true` |
| Plan with only no-op resources | 0 | 0 | `false` |

**Alternative formula using `model.Changes`:**

```csharp
// model.Changes holds displayChanges (after all filters), so this is equivalent:
var hasChanges = model.Changes.Any(c => c.Action is not "no-op");
```

However, the `Summary.Total - FilteredResourceCount` formula avoids a LINQ enumeration and is
consistent with the data already computed by `ReportModelBuilder`.

---

### 4. The Azure DevOps Logging Command Format

Azure DevOps Pipeline agents intercept lines written to stdout that match the logging command
protocol. The standard formats are:

```
##vso[task.setvariable variable=tfplan2md_haschanges]true
##vso[task.setvariable variable=tfplan2md_haschanges]false
```

For variables that need to be available in *downstream jobs* (not just downstream steps in the
same job), the `isoutput=true` attribute is added:

```
##vso[task.setvariable variable=tfplan2md_haschanges;isoutput=true]true
```

**Recommendation:** Start with the simple (non-output) form. The pipeline author can reference
it in the same job with `$(tfplan2md_haschanges)`. Users needing cross-job access can raise a
follow-up.

The command must:
- Appear on its own line in stdout
- Use **lowercase** `true`/`false` (consistent with how most YAML condition expressions in ADO
  work: `eq(variables['tfplan2md_haschanges'], 'true')`)

---

### 5. Where to Write the Logging Command

The logging command must go to **stdout** — that is how the Azure DevOps agent intercepts it.

Two common usage scenarios:

| Scenario | Markdown destination | Logging command goes to |
|---|---|---|
| `tfplan2md plan.json --output plan.md` | `plan.md` (file) | stdout (clean, no mixing) |
| `tfplan2md plan.json` (no `--output`) | stdout | stdout (mixed with markdown) |

When markdown is written to a file, stdout carries only the logging command — this is the
expected production pattern for Azure DevOps pipelines.

When no `--output` is used, the logging command will appear embedded in the markdown stream.
The ADO agent will still parse and act on the `##vso[...]` line correctly; the rest of stdout
is typically not used in this scenario. However, this mixing is undesirable for users piping
to files manually.

**Recommendation:** Emit the logging command **only when the render target is `AzureDevOps`**.
Write it to **stdout after the markdown is written**, so that file-based workflows see a clean
stdout.

---

### 6. Proposed Implementation (for Developer)

#### Modified file: `src/Oocx.TfPlan2Md/ProgramEntry.cs`

In `RunWorkflowAsync`, after the markdown output block (step 8) and before the code-analysis
exit-code check (step 9), add:

```csharp
// Emit Azure DevOps logging command when targeting Azure DevOps.
// Related feature: docs/features/116-azdo-has-changes-variable/
if (options.RenderTarget == RenderTargets.RenderTarget.AzureDevOps)
{
    var hasChanges = model.Summary.Total - model.FilteredResourceCount > 0;
    var hasChangesValue = hasChanges ? "true" : "false";
    Console.WriteLine($"##vso[task.setvariable variable=tfplan2md_haschanges]{hasChangesValue}");
}
```

No new CLI flags, no new classes, no new interfaces — this is a self-contained addition
to the existing workflow.

#### No changes required in:
- `CliParser.cs` — the feature is implicit when `--render-target azuredevops` (the default)
- `CliOptions.cs` — no new option property needed
- `AzureDevOpsDiffFormatter.cs` — unrelated to logging commands
- `RenderTarget.cs` — no new enum value needed
- `ReportModel.cs` / `SummaryModel.cs` — existing data is sufficient

#### Optional: Help text update
**File:** `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs`

Document the behaviour in the `--render-target` option description or in the examples section.
For example, add an example showing how to consume the variable in an ADO pipeline YAML snippet.

---

### 7. Existing Tests and What New Tests Are Needed

#### Existing test files relevant to this feature

| File | What it tests | Relevance |
|---|---|---|
| `CLI/ProgramMainTests.cs` | Full-stack CLI invocation with stdin/stdout capture | **Primary** — models the new test approach |
| `CLI/CliParserTests.cs` | `RenderTarget` parsing (`--render-target azuredevops`) | Confirms render target is set correctly |
| `RenderTargets/AzureDevOpsDiffFormatterTests.cs` | ADO diff formatter | Unrelated — no changes needed |
| `MarkdownGeneration/AzureDevOpsSnapshotTests.cs` | Azure DevOps markdown snapshots | Unrelated to stdout logging command |
| `EndToEnd/DebugOutputIntegrationTests.cs` | Debug section appended to report | Good structural analogy for new test |

#### Key test infrastructure to reuse

`ProgramMainTests.RunMainAsync` (lines 239–263) captures stdout and stderr while preserving
the real console. The new tests should use the same pattern:

```csharp
var result = await RunMainAsync([inputPath, "--render-target", "azuredevops"]);
result.StdOut.Should().Contain("##vso[task.setvariable variable=tfplan2md_haschanges]");
```

#### Recommended new tests in `CLI/ProgramMainTests.cs`

| Test name | Scenario | Expected result |
|---|---|---|
| `Main_WithAzureDevOpsRenderTarget_EmitsHasChangesVariableTrue` | Plan with resource changes + `--render-target azuredevops` | `StdOut` contains `##vso[task.setvariable variable=tfplan2md_haschanges]true` |
| `Main_WithAzureDevOpsRenderTarget_EmitsHasChangesVariableFalse` | No-change plan + `--render-target azuredevops` | `StdOut` contains `##vso[task.setvariable variable=tfplan2md_haschanges]false` |
| `Main_WithAzureDevOpsDefaultRenderTarget_EmitsHasChangesVariable` | Plan with changes + no explicit `--render-target` (default is AzureDevOps) | `StdOut` contains `##vso[task.setvariable variable=tfplan2md_haschanges]true` |
| `Main_WithGitHubRenderTarget_DoesNotEmitHasChangesVariable` | Plan with changes + `--render-target github` | `StdOut` does **not** contain `##vso[task.setvariable` |
| `Main_WithAzureDevOpsTarget_AllFilteredChanges_EmitsFalse` | Plan whose only changes are Azure ID casing (all filtered) | `StdOut` contains `##vso[task.setvariable variable=tfplan2md_haschanges]false` |

**Test data files available:**
- `TestData/azapi-create-plan.json` — plan with resource additions (useful for `hasChanges=true`)
- `TestData/minimal-plan.json` — minimal plan (should produce `hasChanges=false`)
- `TestData/azurerm-case-only-ids-plan.json` — used in casing-filter tests (useful for the
  all-filtered scenario, but only when `--ignore-azure-id-case-changes` is in effect)

---

### 8. Data Flow Summary

```
CLI args
  └─ CliParser.Parse()
       └─ CliOptions { RenderTarget = AzureDevOps }
            └─ CompositionRoot.ComposeServices()
                 └─ ReportModelBuilder.Build(plan)
                      ├─ SummaryModel { Total = N }          ← changes excl. no-op, pre-casing-filter
                      ├─ FilteredResourceCount = F           ← removed by --ignore-azure-id-case-changes
                      └─ ReportModel returned to ProgramEntry
                           └─ ProgramEntry.RunWorkflowAsync
                                ├─ Write markdown
                                └─ [NEW] if AzureDevOps:
                                       hasChanges = (Total - FilteredResourceCount) > 0
                                       Console.WriteLine("##vso[task.setvariable variable=tfplan2md_haschanges]{hasChanges}")
```

---

## Affected Files

| File | Change type | Effort |
|---|---|---|
| `src/Oocx.TfPlan2Md/ProgramEntry.cs` | Add ~6 lines after markdown output | Low |
| `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs` | Optional: document behaviour in help text | Low |
| `src/tests/Oocx.TfPlan2Md.TUnit/CLI/ProgramMainTests.cs` | Add 5 new test methods | Medium |

---

## Open Questions for Developer / Architect

1. **Output variable vs regular variable**: Should the logging command include `isoutput=true`
   to allow cross-job use? The simpler form (no `isoutput=true`) is recommended for the first
   iteration; cross-job use can be added later if requested.

2. **What constitutes "has changes" for outputs?** The feature request is silent on whether
   Terraform output changes (captured in `model.GlobalOutputs` / `model.ModuleChanges[].Outputs`)
   should set `hasChanges=true` even when `Summary.Total == 0`. Recommended: follow the same
   semantics as the report's Summary section — only resource-level changes count.

3. **Suppress variable when `--output` not specified?** Some users might prefer the logging
   command to be suppressed when writing to stdout to avoid corrupting piped markdown. However,
   Azure DevOps agents correctly strip `##vso[...]` lines from the output stream. Recommend
   always emitting when render target is AzureDevOps regardless of `--output`.

---

## Related Documentation

- `docs/features/047-provider-code-separation/specification.md` — the render-target feature
  (introduced `RenderTarget` enum and `--render-target` flag)
- `docs/features/107-azure-id-case-insensitive-filter/specification.md` — the casing filter
  (introduced `FilteredResourceCount`)
- [Azure DevOps logging commands reference](https://learn.microsoft.com/en-us/azure/devops/pipelines/scripts/logging-commands)
