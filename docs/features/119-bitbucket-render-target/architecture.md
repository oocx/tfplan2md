# Architecture: Bitbucket Render Target

## Status

Proposed

## Current Architecture Review

The current implementation is functionally correct but architecturally inconsistent with the rest of the rendering pipeline.

### What the current design gets right

- `RenderTarget` is already a first-class concept in CLI parsing, the report model, and `RenderContext`.
- Diff formatting is already target-aware through `IDiffFormatter` implementations.
- Large-value formatting already branches by render target during report/model construction.
- The Bitbucket-specific behavior is isolated in `RenderTargets/Bitbucket/BitbucketMarkdownPostProcessor.cs`, which keeps the workaround contained.

### What is architecturally weak

1. **Platform adaptation happens after rendering is complete**

   `ProgramEntry` renders markdown first and then rewrites the final string when `RenderTarget.Bitbucket` is selected. This means the rendering pipeline knowingly produces constructs that are invalid for one of its supported targets.

2. **The renderer boundary is inconsistent**

   The system already treats render target as a formatting concern for diffs and large values, but details blocks, summary markup, inline code, bold text, and line breaks are still emitted as Azure DevOps/GitHub-flavored HTML and repaired later.

3. **Semantic intent is lost in string rewriting**

   The post-processor only sees HTML-like text, not semantic concepts such as:
   - collapsible section
   - inline code span
   - multiline code block
   - emphasis
   - line break inside a summary or table cell

   Once rendering is flattened to a string, the Bitbucket adapter must recover meaning through regexes. That is brittle and makes correctness dependent on exact output shapes.

4. **Entry-point orchestration owns rendering policy**

   `ProgramEntry` currently decides whether the output needs a repair step. That knowledge belongs inside the rendering architecture, not in the top-level application workflow.

5. **Extensibility cost increases for future targets**

   The current pattern does not scale well to additional markdown dialects such as GitLab, plain Markdown export, or future platform variants. Every new target risks becoming another post-processing ruleset.

## Architectural Goal

Move from:

- **render invalid target output, then repair it**

to:

- **render target-appropriate output directly from semantic rendering operations**

## Options Considered

### Option 1: Keep post-processing and expand it

#### Description

Continue using the current renderer output and treat Bitbucket as a post-render adaptation layer, improving the post-processor as new edge cases appear.

#### Pros

- Minimal structural change
- Fastest short-term implementation path
- Localized changes for Bitbucket only

#### Cons

- Preserves the root architectural problem
- Regex/string rewrite logic remains brittle
- `ProgramEntry` continues to own platform adaptation policy
- Harder to reason about correctness because semantic intent is already flattened
- Scales poorly to new targets

#### Assessment

Acceptable only as a tactical bridge. Not recommended as the long-term architecture.

### Option 2: Introduce a Render Dialect / Markup Policy abstraction

#### Description

Introduce a first-class rendering dialect in `RenderTargets/` and expose it through `IRenderContext`. Renderers call semantic operations such as:

- `WriteDetailsStart(...)`
- `WriteSummary(...)`
- `WriteDetailsEnd()`
- `FormatInlineCode(...)`
- `FormatBlockCode(...)`
- `FormatStrong(...)`
- `FormatLineBreak(...)`

Each platform dialect decides how those concepts are emitted:

- **Azure DevOps**: HTML-enhanced markdown
- **GitHub**: current markdown-friendly output with tolerated HTML where needed
- **Bitbucket**: markdown-only output with no unsupported HTML generated

This can be implemented either as:

- a new `IMarkupDialect` service on `IRenderContext`, or
- an extended `MarkdownWriter` that delegates semantic constructs to a dialect implementation

#### Pros

- Fixes the root cause without a full renderer rewrite
- Keeps platform policy inside the rendering layer
- Reuses the existing `RenderTarget` and `RenderContext` design
- Improves testability by validating semantic operations per target
- Scales to additional targets better than post-processing
- Avoids generating incorrect output in the first place

#### Cons

- Requires touching shared rendering code such as `DefaultResourceRenderer`, `MarkdownWriter`, and helper classes like `ResourceSummaryHtmlBuilder`
- Some existing helpers return preformatted HTML strings and would need to shift toward semantic formatting APIs
- Intermediate migration period may require supporting both legacy HTML strings and dialect-aware calls

#### Assessment

Best balance of correctness, maintainability, and implementation cost.

### Option 3: Introduce a semantic document AST plus target-specific serializers

#### Description

Replace string-oriented renderer output with an intermediate document model, for example:

- `Document`
- `Section`
- `Paragraph`
- `DetailsBlock`
- `Table`
- `InlineCode`
- `CodeBlock`
- `StrongText`

Resource renderers would build document nodes, and a target-specific serializer would convert those nodes to Azure DevOps, GitHub, or Bitbucket markdown.

#### Pros

- Cleanest separation between semantic intent and platform serialization
- Strongest long-term extensibility story
- Best architectural foundation for future render targets and HTML renderer reuse
- Eliminates string-rewrite classes entirely

#### Cons

- Largest design and migration cost
- Requires wide refactoring across renderers, helpers, and writers
- Higher short-term delivery risk for a relatively focused feature gap

#### Assessment

Architecturally strongest, but too large as the immediate response to the Bitbucket requirement alone.

## Decision

Recommend **Option 2: Render Dialect / Markup Policy abstraction**.

## Rationale

Option 2 addresses the real architectural problem while staying aligned with the current codebase:

- The codebase already has a render-target abstraction.
- The rendering pipeline already passes `RenderTarget` through `RenderContext`.
- Diff formatting already uses target-specific strategy objects.

That means the architecture is already moving toward target-aware rendering; Bitbucket post-processing is the outlier. The right move is to complete that abstraction rather than replace the entire pipeline.

Option 2 also avoids a common trap: a full AST rewrite would be cleaner in theory, but it is disproportionate to the problem unless the project plans a broader rendering-platform expansion. The dialect approach gives most of the value at a fraction of the migration cost.

## Proposed Target Architecture

### Core idea

Move platform-specific output rules from `ProgramEntry` string post-processing into semantic rendering services.

### Proposed building blocks

#### `IMarkupDialect` in `RenderTargets/`

Owns platform-specific emission rules for semantic markdown constructs.

Example responsibilities:

- details block open/close and summary formatting
- inline code formatting
- block code formatting
- bold/strong emphasis formatting
- line-break formatting
- platform capability flags where needed

#### `MarkdownWriter` becomes dialect-aware

Instead of many renderers writing raw `<details>`, `<summary>`, `<br>`, and `<b>` fragments directly, the writer should expose semantic operations that delegate to the active dialect.

#### Renderers emit intent, not HTML

`DefaultResourceRenderer` and provider renderers should ask for semantic constructs:

- start a resource container
- write a summary line
- write formatted emphasis/code
- write a block for large values

They should not decide whether that becomes HTML, plain markdown, or another dialect-specific form.

#### Summary helpers stop returning HTML-specific strings

Helpers such as `ResourceSummaryHtmlBuilder` should evolve toward platform-neutral summary models or dialect-formatted fragments, rather than returning Azure DevOps/GitHub-oriented HTML by default.

## Runtime Impact

### Current runtime flow

1. Build report model
2. Render markdown string
3. If target is Bitbucket, post-process string with regex rewrites
4. Write final output

### Proposed runtime flow

1. Build report model
2. Resolve render dialect from `RenderTarget`
3. Render directly through the dialect-aware writer
4. Write final output

This removes the post-processing phase entirely.

## Implementation Notes

### Migration strategy

Use an incremental migration instead of a big-bang rewrite.

#### Phase 1: Introduce the dialect abstraction

- Add `IMarkupDialect` and target implementations in `RenderTargets/`
- Inject the active dialect through `RenderContext`
- Keep `BitbucketMarkdownPostProcessor` temporarily for unmigrated output paths

#### Phase 2: Migrate shared rendering paths first

- `DefaultResourceRenderer`
- `MarkdownWriter`
- large-value rendering helpers
- summary rendering helpers

This captures most user-visible output with the least provider-specific churn.

#### Phase 3: Migrate helper/model producers away from HTML-shaped strings

- replace `SummaryHtml`-style assumptions with semantic summary data or dialect formatting hooks
- remove direct `<details>`, `<summary>`, `<br>`, `<b>`, and `<code>` generation from shared helpers

#### Phase 4: Remove the Bitbucket post-processor

- once the renderer no longer emits unsupported HTML for Bitbucket, delete the post-processing step from `ProgramEntry`

## Consequences

### Positive

- Bitbucket output is correct by construction
- Rendering responsibilities are moved into the proper architectural layer
- New render targets become cheaper to add
- Tests can validate semantic rendering per dialect instead of regex repair behavior
- Entry-point orchestration becomes simpler

### Negative

- Shared rendering code will need refactoring
- Existing HTML-returning helpers create migration friction
- There will be temporary duplication while old and new rendering paths coexist

## Recommendation For This Feature Branch

Do **not** rewrite the implementation immediately in this branch unless the scope is intentionally expanded.

For PR #640, the current post-processing approach is an acceptable tactical solution because:

- it is isolated
- it is tested
- it preserves the contributor’s original approach with a small corrective follow-up

But the next architectural step should be to open a dedicated follow-up work item for the dialect-based rendering refactor.