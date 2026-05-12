# Issue: False Positive "Already Imported" Warning for Pending Import Blocks

## Problem Description

tfplan2md can report `⚠️ Already imported` for resources managed by Terraform `import` blocks even when the import has not been applied yet. The user reports this happening across many import blocks in the same configuration.

## Steps to Reproduce

1. Create a Terraform plan JSON that includes resources with `change.importing.id`.
2. Ensure one or more of those resources arrives in the plan with `actions: ["no-op"]` before `terraform apply`.
3. Render the plan with tfplan2md.
4. Observe that tfplan2md shows `⚠️ Already imported`.

### Repository Reproduction Evidence

The repository already contains a fixture that exercises the same classification path:

- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/refactoring-comprehensive.json#L42-L58`
- Snapshot output: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/refactoring-comprehensive.md#L35-L47`

That fixture renders:

- resource summary suffix: `📥 Imported (⚠️ already imported)`
- refactoring table status: `⚠️ Already imported`

## Expected Behavior

Pending imports should not be labeled `Already imported` unless the plan provides positive evidence that the import block has already been applied and is unnecessary.

## Actual Behavior

The current pipeline treats any import/move metadata paired with normalized action `no-op` as already applied, so the warning is shown even for plans that still represent pending imports.

## Root Cause Analysis

### Affected Components

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.cs#L168-L170`
  - Derives `importId`, `movedFromAddress`, and `isRefactoringAlreadyApplied`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs#L164-L166`
  - Appends the `already imported` / `already moved` suffix in resource summaries
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs#L185-L202`
  - Converts the shared boolean into warning text
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ReportAssemblyStage.cs#L165-L176`
  - Propagates the same flag into `RefactoringOperationModel`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs#L522-L527`
  - Renders `⚠️ Already imported` in the Refactoring Summary table

### What's Broken

The current classification rule is:

```csharp
var isRefactoringAlreadyApplied = action == NoOpAction && (importId is not null || movedFromAddress is not null);
```

That heuristic is too broad for imports. The code already handles Terraform `read` actions explicitly, so the older `read`-action bug is not the likely cause here. The remaining false positive comes from assuming that `importing.id + no-op` is always proof that the import block is unnecessary.

### Why It Happened

The staged pipeline still encodes an older assumption: `no-op` means the refactoring operation was already applied. That assumption is now reinforced by test fixtures and snapshots:

- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderRefactoringOperationTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ResourceSummaryHtmlBuilderRefactoringTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/refactoring-comprehensive.json`

If Terraform can emit `actions: ["no-op"]` for pending imports that have no configuration drift, tfplan2md has no safer discriminator today and will over-report `Already imported`.

An additional design constraint is that the model uses one shared flag (`IsRefactoringAlreadyApplied`) for both imports and moves, which makes it harder to fix import-specific false positives without also changing move behavior.

## Suggested Fix Approach

1. Add a regression test using the smallest real plan JSON that reproduces the user's case.
2. Stop treating `importing.id + no-op` as sufficient evidence for `Already imported`.
3. Introduce import-specific status instead of a single shared `IsRefactoringAlreadyApplied` flag, so import and move warnings can diverge safely.
4. Update summary rendering and refactoring-table rendering to consume the new import-specific status.
5. Regenerate snapshots if markdown output changes.

## Files the Developer Should Likely Change

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ReportAssemblyStage.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`

## Related Tests

- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderRefactoringOperationTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ResourceSummaryHtmlBuilderRefactoringTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Stages/ReportAssemblyStageTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportRendererTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/refactoring-comprehensive.json`
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/refactoring-comprehensive.md`

## Additional Context

- Existing documentation in `docs/issues/063-already-imported-false-positive/analysis.md` describes an older root cause (`read` actions falling through to `no-op`). That no longer matches the current code because `ResourceChangeStage` already defines and handles `TerraformActions.Read`.
- `scripts/next-issue-number.sh` returned `123`, but also emitted `integer expression expected` while computing the next number. The numbering result appears usable, but the script warning should be tracked separately if it recurs.

## Resolution Status

Fixed in commit `ec524808`.

- Pending imports that include `change.importing.id` and `actions: ["no-op"]` now stay `✅ Ready` instead of showing `⚠️ Already imported`.
- Already-applied state is tracked separately for imports and moves, so no-op moved resources can still render `already moved` without leaking that status to imports.
- The intentional snapshot update in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/refactoring-comprehensive.md` is correct because it captures this user-visible status change from false-positive warning to ready import.
