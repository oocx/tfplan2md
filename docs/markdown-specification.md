# Markdown Specification (GitHub + Azure DevOps Compatible)

This document defines the markdown subset used by `tfplan2md` that renders reliably on **GitHub** and **Azure DevOps**.

## Supported Blocks

- Headings (`#`–`####`), with a blank line **before and after** each heading.
- Paragraphs with a blank line separating blocks.
- Tables with a blank line **before and after** the table.
- Lists (unordered/ordered) when needed inside descriptions (not used in built-in templates).
- Code fences (```), primarily for inline code spans we prefer backticks.
- HTML elements intentionally used by templates: `<details>`, `<summary>`, `<br/>`.

## Table Rules

- Surround each table with a blank line.
- Do **not** include raw newlines inside cells; replace newlines with `<br/>`.
- Escape pipes inside cells as `\|`.

## Heading Rules

- Always leave a blank line before and after headings.
- Avoid trailing spaces in heading text.

## Escaping Rules

The `escape_markdown` helper must be applied to **all external input** from Terraform plans (resource names, attribute values, tag keys/values, module addresses, role names, scopes, etc.). It performs only the escaping needed to keep tables and headings structurally valid:

- Backslash: `\\`
- Pipes: `\|`
- Backtick: ``\``
- Angle brackets: `\<`, `\>`
- Ampersand: `&amp;`
- Newlines (`\r`, `\n`, `\r\n`): replaced with `<br/>`

## Formatting Helpers

- Use `<br/>` for line breaks inside table cells or inline text that must stay within a single cell.
- Keep `<details>` blocks separated from surrounding content by blank lines.

## Linting

- `markdownlint-cli2` is the reference linter configuration for this subset.
- Developers must run the linter against the generated **comprehensive demo** report before opening a PR.
- CI runs the linter on the generated comprehensive demo report and fails on violations.

### Automated Validation

All generated markdown is automatically validated through:

1. **Docker-based markdownlint integration** - Uses `davidanson/markdownlint-cli2:v0.20.0` image
2. **Property-based invariant tests** - Verifies 12 markdown invariants across all test plans
3. **Snapshot testing** - Compares output against 6 approved baselines
4. **Template isolation testing** - Each template tested independently
5. **Fuzz testing** - Edge cases with special characters, Unicode, long values

See [testing-strategy.md](testing-strategy.md) for complete testing documentation.

## Out of Scope

- GitHub-only or Azure DevOps-only extensions (e.g., task lists, Mermaid) are **not** used in built-in templates.
- User-provided custom templates are not validated by CI.
