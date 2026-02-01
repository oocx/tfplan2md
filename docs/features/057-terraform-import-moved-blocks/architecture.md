# Architecture: Terraform Import and Moved Blocks

## Status

Proposed

## Context

Feature spec: [specification.md](specification.md)

Terraform supports refactoring workflows via:
- `import` blocks (bring existing infrastructure under Terraform management)
- `moved` blocks (rename/relocate resources without destroy/recreate)

Terraform Plan JSON (format_version 1.0+) exposes these signals per `resource_changes[]` entry:
- `resource_changes[].change.importing.id` for imports
- `resource_changes[].previous_address` for moved resources

The current tfplan2md pipeline does not surface these in the report:
- `Parsing/TerraformPlan.cs` does not model `previous_address` or `change.importing`.
- Resource summary lines are precomputed in C# (`ResourceChangeModel.SummaryHtml` via `ResourceSummaryHtmlBuilder.BuildSummaryHtml(...)`).
- The default report is rendered by Scriban templates (ADR-001) and is expected to be layout-focused; non-trivial logic should live in C# model building/helpers.

Report rendering constraints:
- GitHub and Azure DevOps are primary targets; inside `<summary>` tags we must use HTML `<code>` instead of markdown backticks (see [docs/report-style-guide.md](../../report-style-guide.md)).
- Icon+label sequences must use non-breaking spaces (U+00A0) to avoid wrapping.

## Problem to Solve

Reviewers need refactoring visibility without hunting through Terraform configuration:
1. Inline context: each affected resource’s `<summary>` line should indicate it is imported or moved.
2. Report-level overview: a consolidated Refactoring Summary table (imports and moves) should appear near the end of the report.
3. Hygiene warnings: identify refactoring blocks that appear to be already applied (Terraform reports `actions = ["no-op"]`), and warn that the block can be removed.

Non-goals (per spec):
- No generation/recommendations of refactoring blocks.
- No validation of import IDs or moved addresses.

## Options Considered

### Option 1: Template-only implementation
Implement detection, sorting, status classification, and rendering logic purely in Scriban templates.

- Pros
  - Minimal C# changes
- Cons
  - Violates the “templates are layout-focused” principle; complex logic becomes duplicated/hard to test
  - Harder to keep summary-line logic consistent (summary HTML is built in C#)
  - Requires expanding the template surface area (sorting/grouping) and increases fragility

### Option 2: Parse + enrich model in C#; templates render (recommended)
Extend parsing models to capture refactoring metadata, propagate it into the report model, and keep templates responsible for layout.

- Pros
  - Fits existing architecture: parsing → model building → templates
  - Keeps complex logic (classification/sorting) testable in C#
  - Allows resource summary line annotation via the existing C# summary builder
  - Minimizes template branching; reduces chance of rendering differences between targets
- Cons
  - Requires changes in cross-cutting model types (plan parsing and report model)

## Decision

Choose **Option 2**.

## Proposed Technical Design

### 1) Parsing: capture refactoring metadata from plan JSON

Extend `TerraformPlan` parsing models to include:
- `ResourceChange.PreviousAddress` mapped from `previous_address` (nullable)
- `Change.Importing` mapped from `change.importing` (nullable), with a nested model for `id`

Design notes:
- These fields must be optional to preserve compatibility with plans that don’t include them.
- Because parsing uses source generation (`TfPlanJsonContext`), updating the model types is sufficient; unknown JSON fields are already ignored.

### 2) Report model: propagate refactoring metadata

Introduce refactoring metadata at two levels:

#### 2.1 Resource-level metadata (for inline summary annotations)
Extend `ResourceChangeModel` with minimal, provider-agnostic fields:
- `ImportId` (nullable string)
- `MovedFromAddress` (nullable string)
- `IsRefactoringAlreadyApplied` (bool; true when the resource is import/move *and* action is `no-op`)

This keeps the resource template rendering simple and supports the requirement:
> “Resource summary lines without import/moved annotations render exactly as before.”

Implementation guidance for the Developer:
- `ReportModelBuilder.BuildResourceChangeModel(...)` should map the new parsing fields into `ResourceChangeModel`.
- Keep existing output unchanged when `ImportId` and `MovedFromAddress` are both null.

#### 2.2 Report-level metadata (for the Refactoring Summary table)
Add a dedicated list on `ReportModel`, e.g. `RefactoringOperations`, containing items with:
- `Operation` (Import | Move)
- `Address` (Terraform address for sorting)
- `ResourceDisplay` (type + local name) for table display
- `Details` (import ID or previous address)
- `Status` (Ready | AlreadyApplied)

Rationale:
- The table requires cross-resource sorting and grouping; computing this once in C# avoids template complexity.
- The same model can be reused by multiple templates (default and summary-only) without duplication.

### 3) Unnecessary block detection

Classification rule (per spec):
- If a resource has import/move metadata and `actions = ["no-op"]`, then mark it as **Already applied**.

Important interaction with current behavior:
- The current default report filters out `no-op` resources from `ReportModel.Changes` to avoid template iteration limits.
- For this feature, the Developer should selectively retain `no-op` resources *only when* they carry refactoring metadata, so the inline warning example can be represented.
  - This keeps existing performance behavior while satisfying refactoring visibility.

### 4) Rendering: resource `<summary>` annotations

Use the existing precomputed summary HTML path:
- `ResourceSummaryHtmlBuilder.BuildSummaryHtml(...)` should prepend a refactoring annotation when present.

Formatting rules (from spec + style guide):
- Inside `<summary>`, use HTML `<code>` tags for data and `<i>` tags for labels.
- Use non-breaking spaces for icon+label sequences.
- Preserve the existing summary output exactly when no refactoring metadata exists.

Recommended summary layout:
- Insert refactoring context immediately after the em dash (`—`) and before existing context values.
- When there is existing context, separate refactoring context from the rest with a plain-text `|` delimiter.

### 5) Rendering: Refactoring Summary table section

Update core templates to conditionally render the section when at least one refactoring operation exists.

Placement:
- At the end of the report content, after all resource changes (and after any code analysis “other findings” section), before any footer/metadata.

Table formatting:
- Use markdown tables with backticks for all code values (resource name/type, import ID, addresses).
- Operation and Status cells include icons + labels with non-breaking spaces.

Sorting rules (per spec intent):
- Primary: Imports first, then Moves.
- Within each operation group:
  - AlreadyApplied first (warnings)
  - then alphabetical by resource address (ordinal)

## Components Affected (Implementation Guidance)

Developer work is expected in:
- Parsing models:
  - `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs` (add `previous_address`, `change.importing` models)
  - `src/Oocx.TfPlan2Md/Parsing/TfPlanJsonContext.cs` (re-run source-gen via build; no manual changes usually required beyond model updates)
- Report model:
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs` (add refactoring fields)
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs` (add refactoring list)
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.*.cs` (populate refactoring list; adjust no-op filtering for refactoring resources)
- Summary HTML:
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs` (inject annotations without affecting non-refactoring output)
- Templates:
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/summary.sbn` (if the summary template is expected to show refactoring context even without resource listings)

## Testing Strategy

Add/extend tests to cover:
- Parsing:
  - `previous_address` is parsed when present and null otherwise
  - `change.importing.id` is parsed when present and null otherwise
- Model building:
  - Refactoring operations list is generated and sorted correctly
  - `IsRefactoringAlreadyApplied` is true only for import/move with `no-op`
  - No-op resources remain excluded unless they carry refactoring metadata
- Rendering (snapshot tests):
  - Default report unchanged when no import/move exists
  - Refactoring Summary section appears only when import/move exists
  - Resource summary lines gain the correct annotation and preserve existing context ordering

## Risks and Mitigations

- **Terraform JSON variance across versions:** Treat new fields as optional; fall back gracefully when missing.
- **Template iteration limits:** Continue excluding most `no-op` resources; only include refactoring no-ops.
- **Template iteration limits (global):** Increase Scriban’s loop limit to 10000 to reduce false failures on large plans (see proposed ADR in `docs/adr-005-scriban-template-loop-limit.md`).
- **Rendering differences between GitHub/Azure DevOps:** Use `<code>` inside `<summary>` and backticks in tables per style guide.
- **Spec/style-guide mismatch for no-op icon:** Prefer the project’s standard no-op icon (⏺️) unless the Maintainer requests the spec example’s ⚪ for this feature.

## Architecture Compliance Checklist

- No provider-specific logic added for this feature (imports/moves are Terraform core, provider-agnostic).
- Templates remain layout-focused; detection/sorting/status classification occurs in C# model building.
- Existing summary rendering stays byte-for-byte identical for resources without refactoring metadata.
