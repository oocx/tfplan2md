# Architecture: Low-Risk Code Quality Improvements

## Status

No architectural changes required.

## Analysis

Feature 112 is a behavior-preserving refactoring pass. The existing architecture already provides
the right seams for this work: render-target-specific logic is isolated under
`src/Oocx.TfPlan2Md/RenderTargets/`, provider-specific logic is isolated under
`src/Oocx.TfPlan2Md/Providers/`, and shared core behavior remains outside those boundaries.

A fresh review of the current codebase identified several viable low-risk cleanup slices. The best
fit for a single surgical pull request is the duplicated markdown-escaping logic in the diff
formatters:

- `src/Oocx.TfPlan2Md/RenderTargets/AzureDevOps/AzureDevOpsDiffFormatter.cs`
- `src/Oocx.TfPlan2Md/RenderTargets/GitHub/GitHubDiffFormatter.cs`

Both classes currently own materially identical `EscapeMarkdown` implementations while differing in
their actual diff rendering strategies:

- `GitHubDiffFormatter` renders compact `-` / `+` lines for GitHub markdown tables.
- `AzureDevOpsDiffFormatter` preserves its Azure DevOps-specific HTML-based rendering pipeline,
  including the short-value fast path and large-value inline diff adaptation.

This duplication is a better Feature 112 slice than broader refactors because it:

- removes repeated implementation from an actively used rendering path;
- does not require changing dependency injection, public contracts, or provider boundaries;
- keeps the PR small enough to review and revert safely;
- preserves the hard architectural constraint that provider-specific logic must not leak into core
  markdown-generation modules.

Additional candidates were considered but are less suitable for the first surgical slice:

1. **Further reducing Azure DevOps formatter boilerplate** in `Providers/AzureDevOps/`
   - Valuable, but Feature 111 already extracted the main shared helper (`AzdoFormatterHelper`).
   - A second pass here would add another abstraction layer for less immediate value.
2. **Consolidating repeated diagnostic-recording helpers across mappers**
   - Also viable, but it spans multiple mapper families and introduces a wider coordination surface
     than necessary for the first Feature 112 implementation.

## Implementation Guidance

This feature can be implemented using existing patterns and architecture:

- Keep the refactoring entirely inside the `RenderTargets` area.
- Extract the duplicated markdown escaping into a shared internal helper located under
  `src/Oocx.TfPlan2Md/RenderTargets/`.
- Preserve the `IDiffFormatter` contract and both formatter classes as the render-target-specific
  orchestration boundaries.
- Do **not** move this logic into `MarkdownGeneration/`; diff escaping remains a render-target
  concern because it is coupled to platform output behavior.
- Preserve current observable behavior covered by existing tests:
  - unchanged values remain wrapped in `<code>...</code>`;
  - GitHub changed values remain rendered as `- before<br>+ after`;
  - Azure DevOps changed values continue to use the existing HTML diff output and fast-path/full
    pipeline split.
- If the implementation introduces a shared helper file, keep it `internal` and narrowly scoped to
  diff formatter escaping rather than creating a broad cross-cutting utility.

### Recommended Minimal Implementation Scope

The downstream implementation should be limited to:

1. Add one shared render-target helper for markdown escaping used by diff formatters.
2. Replace the duplicated private `EscapeMarkdown` methods in:
   - `AzureDevOpsDiffFormatter`
   - `GitHubDiffFormatter`
3. Leave all other formatter behavior unchanged:
   - GitHub simple diff table generation
   - Azure DevOps inline HTML diff rendering
   - Azure DevOps short-value fast path
4. Update or extend unit tests only if needed to pin the extracted shared escaping behavior without
   broadening the feature scope.

## Components Affected

- `src/Oocx.TfPlan2Md/RenderTargets/GitHub/GitHubDiffFormatter.cs`
- `src/Oocx.TfPlan2Md/RenderTargets/AzureDevOps/AzureDevOpsDiffFormatter.cs`
- `src/Oocx.TfPlan2Md/RenderTargets/` (new internal helper file expected)
- Potentially the existing render-target unit tests in
  `src/tests/Oocx.TfPlan2Md.TUnit/RenderTargets/`
