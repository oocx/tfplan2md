# Feature: Bitbucket Render Target

## Overview

Add `bitbucket` as a supported value for `--render-target` so tfplan2md can generate markdown that renders reliably in Bitbucket pull request comments and descriptions.

The existing render targets assume HTML-enhanced markdown is acceptable (`azuredevops`) or that GitHub's markdown renderer will tolerate a subset of inline HTML (`github`). Bitbucket comments are stricter: they do not reliably render `details`, `summary`, inline `<code>`, block `<pre><code>`, inline `<b>`, or HTML-based line breaks in the same way. As a result, important plan details can disappear or render poorly when users post tfplan2md output to Bitbucket.

This feature introduces a Bitbucket-specific post-processing step that rewrites the already-rendered markdown into a markdown-only form that Bitbucket comments can display.

## User Goals

- Users need a supported way to post tfplan2md output into Bitbucket pull request comments without losing plan details.
- Users need large-value diffs to remain readable even when HTML-based formatting is unavailable.
- Users need inline values, bold labels, and collapsible resource summaries to degrade into plain markdown instead of raw HTML.
- Users need the CLI help, README, and feature catalog to document Bitbucket as a first-class render target.

## Scope

### In Scope

- Add `bitbucket` as a valid `--render-target` value.
- Route Bitbucket rendering through a dedicated `BitbucketMarkdownPostProcessor`.
- Rewrite HTML-only constructs into markdown-only equivalents:
  - `<details>` and `<summary>` to plain markdown headings/text blocks
  - `<pre><code>` to fenced code blocks
  - `<code>` to markdown code spans
  - `<b>` to `**bold**`
  - `<br/>` to plain-text separators or line breaks, depending on context
  - non-semantic `<span>` wrappers removed
- Reuse the GitHub/simple-diff format for large values and other markdown-only diff output.
- Add direct unit coverage for Bitbucket post-processing behavior.
- Document the feature in `docs/features.md` and feature-scoped docs.

### Out of Scope

- Bitbucket-specific screenshots or visual assets
- New provider-specific rendering logic unrelated to the render target
- A separate Bitbucket HTML renderer tool flavor
- Custom formatting controls beyond selecting `--render-target bitbucket`

## User Experience

### Example CLI Usage

```bash
tfplan2md plan.json --render-target bitbucket
```

### Expected Output Characteristics

- No raw `<details>`, `<summary>`, `<code>`, `<pre>`, `<b>`, or `<span>` tags remain in the final output.
- Resource sections remain readable as plain markdown blocks.
- Inline values remain readable even when they contain `&`, `|`, or backticks.
- Large-value blocks render as fenced code blocks rather than HTML code blocks.

## Acceptance Criteria

- `CliParser` accepts `--render-target bitbucket`.
- Help text lists `bitbucket` alongside `github` and `azuredevops`.
- Program output for Bitbucket contains no raw HTML tags that Bitbucket comments do not support.
- Inline code content preserves decoded literal characters rather than escaping them for normal markdown text.
- Block code content converts encoded or literal HTML break tags into readable multiline markdown output.
- The feature is documented in `docs/features.md` and this feature folder.
- Automated tests cover both CLI wiring and direct post-processor behavior.

## Verification

- Automated test project passes via `scripts/test-with-timeout.sh --timeout-seconds 300 -- dotnet test --project tests/Oocx.TfPlan2Md.TUnit/`.
- Direct unit tests verify Bitbucket post-processing for details blocks, inline code, fenced code blocks, and HTML-to-markdown rewrites.