# ADR-002: Authoring Model

## Status

Accepted

## Context

The new site should be easy to author primarily as content while still supporting centrally defined reusable blocks such as the rendered/source example widget.

## Decision to make

Choose the primary authoring model for pages and shared content.

## Options considered

### Option A: Markdown pages with template includes and shortcodes

Pros:

1. Readable in raw form.
2. Natural fit for documentation-heavy pages.
3. Easy for AI agents to edit small sections safely.
4. Encourages content-first authoring.
5. Shared components can still be used where needed.

Cons:

1. Some advanced layouts are less natural in pure Markdown.
2. Shortcodes and includes need clear conventions to avoid becoming opaque.

### Option B: MDX-style mixed content and components

Pros:

1. Flexible composition model.
2. Easy to embed components inline.
3. Familiar to many frontend developers.

Cons:

1. Blurs content and implementation.
2. Increases complexity for a site that does not need application-like composition.
3. Creates more cognitive load in code review.

### Option C: Template-first pages for most content

Pros:

1. Maximum control over markup.
2. Unified syntax for pages and components.

Cons:

1. Makes simple content edits more verbose.
2. Encourages markup-heavy pages.
3. Reduces the benefit of adopting a generator for maintainability.

## Decision

Use Markdown pages as the default authoring model, with one template language for layouts, partials, and reusable components.

## Rationale

This keeps most pages content-first while still allowing reusable building blocks for navigation, footer, hero sections, and example widgets. Raw HTML remains available as an escape hatch, but it should not be the default.

This decision is accepted on the condition that the implementation can recreate the current site layout and content presentation with 1:1 fidelity where required. Markdown is the default authoring path, but page-level template overrides and raw HTML escapes are explicitly allowed when they are needed to preserve parity with the current website.

## Consequences

Positive:

1. Most edits remain small and reviewable.
2. Authors can focus on content rather than page scaffolding.
3. Shared components become explicit and centralized.

Negative:

1. The project must document how and when to use shortcodes or includes.
2. Some highly custom pages may still need more template involvement.
3. A small number of pages may need template-first sections to preserve exact legacy layout behavior.
