# ADR-004 (feature 122): Deprecation warnings via the existing code-analysis warnings pipeline; rename heading to "Warnings"

## Status

Proposed

## Context

Terraform 1.15 added a `deprecated` field (string deprecation message) on `configuration.root_module.variables[<name>]` and `configuration.root_module.outputs[<name>]`. The maintainer's locked decision (spec § In Scope — M2 and FR-M2.4 / FR-M2.5) is that these deprecations MUST flow through the **existing** code-analysis warnings mechanism rather than a new parallel system. Specifically:

- Source model: `src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisWarning.cs`
- Production model: `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/CodeAnalysisWarningModel.cs`
- Builder: `BuildWarningModels` in `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.CodeAnalysis.cs`
- Renderer: the `Heading("Code Analysis Warnings", 3)` block in `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/CodeAnalysisSectionRenderer.cs`

The current `CodeAnalysisWarningModel` is purpose-built for SARIF processing failures:

```csharp
public sealed class CodeAnalysisWarningModel
{
    public required string FilePath { get; init; }
    public required string Message { get; init; }
}
```

…and the renderer hard-codes the SARIF-failure phrasing:

```csharp
writer.Paragraph($"⚠️ **Warning:** Unable to process code analysis file {MarkdownHelpers.FormatCodeTable(warning.FilePath)}");
writer.Paragraph($"- Error: {MarkdownHelpers.EscapeMarkdown(warning.Message)}");
```

A deprecation warning has different semantics: there is no file path; the message is *the deprecation text from Terraform*; the subject is a variable or output name. Two open questions:

1. How to extend the warning model to carry a deprecation entry without breaking SARIF-failure rendering.
2. Whether to keep the heading `"Code Analysis Warnings"` or rename it now that the section carries entries from a non-SARIF source.

## Options considered

### (1) Extending the warning model

**Option 1A (chosen): Add an optional `Source` discriminator to `CodeAnalysisWarningModel`; renderer branches per source.**

```csharp
public enum CodeAnalysisWarningSource
{
    SarifProcessingFailure = 0, // default — preserves existing behaviour
    PlanDeprecation,
}

public sealed class CodeAnalysisWarningModel
{
    public required string Message { get; init; }
    public string? FilePath { get; init; }            // now optional
    public CodeAnalysisWarningSource Source { get; init; } = CodeAnalysisWarningSource.SarifProcessingFailure;
    public string? SubjectName { get; init; }         // "deprecated_var" / "deprecated_output"
    public string? SubjectKind { get; init; }         // "variable" / "output"
}
```

The renderer branches on `Source`:

- `SarifProcessingFailure` → existing two-paragraph rendering, unchanged.
- `PlanDeprecation` → `⚠️ **Deprecated <kind>** ` `` `<name>` `` `: <message>`, single paragraph.

- Pros: strictly additive; no parallel renderer; existing snapshot output for SARIF warnings is unchanged; the renderer remains the single owner of warning markdown.
- Cons: `FilePath` becomes nullable (it is currently `required`). Mitigation: keep `required` with an empty-string sentinel for deprecation entries, OR drop `required` and update existing call-sites to set `FilePath` explicitly. The latter is cleaner; the former is more conservative.

**Option 1B (rejected): A second warning model (`PlanWarningModel`) with its own collection on `ReportModel` and its own renderer call-site.** Rejected — directly violates FR-M2.5 ("Do not introduce a parallel warnings model, renderer, or section").

**Option 1C (rejected): Re-purpose `FilePath` to carry the variable/output name.** Rejected — it would force the existing renderer's "Unable to process code analysis file `<filepath>`" sentence to be reworded for every entry, leaking deprecation semantics into the SARIF code path.

### (2) Heading wording

**Option 2A (chosen): Rename the H3 from "Code Analysis Warnings" to "Warnings".**

- Pros: honest — the section now carries entries from at least two unrelated sources (SARIF processing failures, Terraform deprecations) and is likely to grow further (e.g. future plan-side warning categories). "Warnings" generalises cleanly without changing the parent H2 ("Code Analysis Summary"), which remains accurate as a SARIF-specific summary.
- Cons: any existing snapshot fixture that renders a SARIF processing failure will need its expected output updated. Spec AC-9 explicitly carves this out: "verified by existing snapshot tests passing without diff, **except for any intentional warnings-heading rename**".

**Option 2B (rejected): Keep "Code Analysis Warnings".** Rejected — deprecation warnings are not code-analysis warnings; the heading would be misleading.

**Option 2C (rejected): Render two adjacent H3s — "Code Analysis Warnings" and "Plan Warnings".** Rejected because it creates the parallel structure FR-M2.5 forbids in spirit, and reviewers would have to scan two near-identical sections.

### (3) When to emit a deprecation warning

**Option 3A (chosen): Emit one warning per *referenced* deprecated variable or output.**

A variable / output is "referenced" when it appears in the plan's top-level `variables` map (for variables) or in `output_changes` (for outputs). Variables / outputs that are declared in `configuration.root_module` but unused in this plan do NOT generate a warning — declaring something as deprecated does not force a warning when nobody uses it in this plan.

- Pros: noise-free reports; matches reviewer mental model ("warn me about what *this plan* uses").
- Cons: requires correlating `configuration.root_module.variables` / `outputs` with `plan.Variables` / `plan.OutputChanges`. The correlation is by name and is straightforward.

**Option 3B (rejected): Emit one warning per declared deprecation, regardless of use.** Rejected — produces noisy warnings for transitive modules whose deprecated outputs aren't read by anything in the current plan.

## Decision

1. Extend `CodeAnalysisWarningModel` with an optional `Source` discriminator (`SarifProcessingFailure` default; `PlanDeprecation` for Terraform deprecations) and optional `SubjectName` / `SubjectKind` properties. `FilePath` becomes nullable. The renderer branches per source.
2. Rename the H3 heading from `"Code Analysis Warnings"` to `"Warnings"`.
3. Emit one warning per referenced deprecated variable or output. A variable is referenced if it appears in `plan.Variables`; an output is referenced if it appears in `plan.OutputChanges`.

## Consequences

### Positive

- No parallel warnings system (FR-M2.5 satisfied; AC-8 satisfied).
- Reviewers see Terraform deprecation messages with the same `⚠️` styling they already recognise.
- The renamed heading honestly reflects multi-source warnings and accommodates future warning categories without further rename churn.

### Negative

- `CodeAnalysisWarningModel` becomes slightly less type-safe: `FilePath` is now nullable, and consumers must handle both shapes via the `Source` discriminator. Mitigation: the discriminator defaults to `SarifProcessingFailure`, preserving existing call-site behaviour.
- Snapshot fixtures that render the SARIF-warnings heading will need their expected text updated from "Code Analysis Warnings" to "Warnings". This is the *only* output change for plans that don't carry the new 1.15 fields. (Spec AC-9 explicitly permits this.)
- The "Code Analysis Summary" H2 still says "Code Analysis Summary" — only the inner H3 changes. This deliberate split keeps SARIF-specific summary semantics (counts by severity, tools used) under an accurate heading while generalising the warnings list.

## Implementation notes

For the Developer:

- Extend `CodeAnalysisWarningModel` (additive) with `Source`, `SubjectName`, `SubjectKind`. Make `FilePath` nullable. Preserve existing `BuildWarningModels` behaviour for SARIF processing failures (default `Source` keeps the existing rendering).
- Add a new partial `ReportModelBuilder.Deprecations.cs` (or fold into the existing `ReportModelBuilder.CodeAnalysis.cs` if cohesion improves). It should:
  1. Use the `ConfigurationDeprecationReader` helper introduced in ADR-001 to walk `configuration.root_module.{variables,outputs}` and yield deprecated entries.
  2. Filter those entries to ones that appear in `plan.Variables` / `plan.OutputChanges`.
  3. Append a `CodeAnalysisWarningModel { Source = PlanDeprecation, SubjectKind = "variable" | "output", SubjectName = name, Message = deprecationMessage, FilePath = null }` to the existing warnings collection on `CodeAnalysisReportModel`.
- In `CodeAnalysisSectionRenderer.RenderSummary`:
  - Change `writer.Heading("Code Analysis Warnings", 3)` to `writer.Heading("Warnings", 3)`.
  - Branch on `warning.Source`:
    - `SarifProcessingFailure` → existing two-paragraph rendering, unchanged.
    - `PlanDeprecation` → `⚠️ **Deprecated {kind}** ` `` `{name}` `` `: {message}`.
- The deprecation rendering must escape the deprecation `Message` via `MarkdownHelpers.EscapeMarkdown` (the message is author-controlled text and can contain markdown-active characters).
- The `outputs[*].type` field (parsed in ADR-001) is rendered as part of the existing outputs table only if it improves clarity — implementation detail left to the Developer; one option is to append the type as a small inline annotation to the `Description` cell when no description is provided.
- Snapshot fixtures (per AC-11): one plan with a deprecated variable that is referenced; one plan with a deprecated output that is referenced; one plan with a deprecated variable that is NOT referenced (asserts no warning is emitted); one plan with a `output[*].type` present.

## References

- Specification: [docs/features/122-terraform-1-15-support/specification.md](specification.md) §§ FR-M2.1–FR-M2.5, AC-8, AC-9
- Existing warnings model: `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/CodeAnalysisWarningModel.cs`
- Existing builder: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.CodeAnalysis.cs`
- Existing renderer: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/CodeAnalysisSectionRenderer.cs`
- Plan-JSON model extensions: [adr-001-plan-json-model-extensions.md](adr-001-plan-json-model-extensions.md)
