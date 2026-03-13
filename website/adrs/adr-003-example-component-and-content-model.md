# ADR-003: Example Component and Content Model

## Status

Accepted

## Context

The current site duplicates large rendered/source example blocks across many pages. These blocks repeat wrapper markup, controls, view panes, and page-local scripts. The new site needs a central model for reusable examples.

## Decision to make

Choose how repeated examples are stored and rendered.

## Options considered

### Option A: Keep example markup inline in each page

Pros:

1. No indirection.
2. Every page is self-contained.

Cons:

1. Recreates the current duplication problem.
2. Hard to update shared behavior consistently.
3. Easy for pages to drift apart.

### Option B: Store example data centrally and render through a shared component

Pros:

1. Shared markup lives in one component.
2. Shared examples can be reused across multiple pages.
3. Behavior and accessibility can be fixed once for all usages.
4. Clear mapping from example identifiers to real artifact sources.

Cons:

1. Introduces an additional data layer.
2. Authors need conventions for where rendered HTML and source Markdown live.

### Option C: Isolate examples in iframes

Pros:

1. Strong style isolation.
2. Easier to preserve exact rendering contexts.

Cons:

1. Heavier implementation and more fragile sizing behavior.
2. Worse authoring ergonomics.
3. More JavaScript and coordination overhead.

## Decision

Store example definitions centrally and render them through a shared `example-block` component.

## Rationale

This is the strongest match for the stated need to define repeated content once and reuse it across pages. It also fits the current website's repeated rendered/source pattern and makes accessibility and behavior easier to standardize.

This decision is accepted.

## Recommended model

1. Keep example metadata in shared data files.
2. Keep rendered HTML and source Markdown in reusable content fragments or generated files.
3. Reference examples by stable identifiers from Markdown pages.
4. Bind tabs and fullscreen through one shared enhancement script.

## Consequences

Positive:

1. Reuse becomes explicit and maintainable.
2. Example-heavy pages become much smaller.
3. Fixes to markup or behavior apply consistently.

Negative:

1. Requires clear conventions for naming and storage.
2. Some one-off examples may still need inline overrides.
