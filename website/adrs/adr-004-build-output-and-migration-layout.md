# ADR-004: Build Output and Migration Layout

## Status

Accepted

## Context

`website2` must coexist with the current `website/` until migration is complete. The build layout must make this separation explicit and keep authored source distinct from generated output.

## Decision to make

Choose the source and output layout for `website2` during migration.

## Options considered

### Option A: Source and generated files mixed in the same folder

Pros:

1. Fewer directories.
2. Simple mental model for very small sites.

Cons:

1. Harder to distinguish authored source from generated output.
2. Easier to accidentally edit generated files.
3. Poor fit for migration alongside the legacy site.

### Option B: `website2/src` for source and `website2/dist` for generated output

Pros:

1. Clean separation between source and build artifacts.
2. Safe coexistence with legacy `website/`.
3. Clear deployment boundary.
4. Easy to reason about in CI.

Cons:

1. Slightly more structure to understand.
2. Preview tooling must point at generated output.

### Option C: Generate directly into `website/` during migration

Pros:

1. Fewer moving parts at deployment time.
2. Simplifies final cutover mechanically.

Cons:

1. High risk of interfering with the legacy site.
2. Blurs migration boundaries.
3. Makes rollback and parity comparison harder.

## Decision

Use `website2/src` for authored source and `website2/dist` for generated output until cutover.

## Rationale

The migration needs a hard boundary between the legacy site, the new source, and the generated output. This layout keeps those concerns separate and reduces the risk of editing or deploying the wrong files.

This decision is accepted.

## Consequences

Positive:

1. Cleaner migration process.
2. Easier CI and local preview reasoning.
3. Safer cutover.

Negative:

1. Requires preview and deployment scripts to target `dist` explicitly.
