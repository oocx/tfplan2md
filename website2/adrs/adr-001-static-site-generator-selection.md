# ADR-001: Static Site Generator Selection

## Status

Accepted

## Context

The current `website/` is a hand-authored static site with repeated navigation, footer markup, and duplicated interactive example blocks. The replacement should preserve a static-first model while centralizing shared structures and keeping authoring readable for humans and AI agents.

## Decision to make

Choose the static site generator for `website2`.

## Options considered

### Option A: Eleventy

Pros:

1. Strong fit for content-first sites.
2. Markdown-first authoring model.
3. Minimal framework overhead.
4. Straightforward partials, layouts, data files, and shortcodes.
5. Easy incremental migration from existing static HTML.
6. Good fit for AI-agent readability and maintenance.

Cons:

1. Template ergonomics are less structured than a component-first framework.
2. Multiple template engine options can create inconsistency if not constrained.
3. Complex components require discipline in template design.

### Option B: Astro

Pros:

1. Excellent component model.
2. Strong ergonomics for reusable UI blocks.
3. Good Markdown and MDX support.
4. Modern tooling and strong ecosystem momentum.

Cons:

1. Heavier toolchain than necessary for the current site shape.
2. Tends to pull authors toward component-first authoring rather than content-first authoring.
3. More moving parts for agents to understand and modify safely.

### Option C: Hugo

Pros:

1. Very fast builds.
2. Small operational footprint.
3. Mature static site ecosystem.

Cons:

1. Template syntax is less readable and less pleasant to maintain.
2. Weaker fit for AI-assisted editing.
3. Less ergonomic for the example component authoring model envisioned here.

## Decision

Choose Eleventy for `website2`.

## Rationale

Eleventy best matches the actual problem in this repository: a static, documentation-style site that needs stronger reuse and cleaner authoring, not a full component application framework. It offers the simplest path to centralizing page chrome and reusable example blocks while keeping Markdown as the default authoring format.

This decision is accepted.

## Consequences

Positive:

1. Simpler migration from existing static HTML.
2. Lower implementation complexity.
3. Better alignment with AI-agent maintenance.

Negative:

1. Template conventions must be kept intentionally narrow.
2. Highly interactive UI patterns may require more custom work than Astro.
