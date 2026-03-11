# ADR-005: Browser Baseline and Styling Strategy

## Status

Accepted

## Context

The current website non-functional requirements name latest Edge and Firefox as supported browsers. The requested direction for `website2` is to use the latest CSS and HTML standards and target latest Chromium-based browsers without legacy compatibility work.

This creates a policy decision that should be explicit rather than implicit.

## Decision to make

Choose the browser baseline and styling strategy for `website2`.

## Options considered

### Option A: Keep broad evergreen browser support, including Firefox

Pros:

1. Wider compatibility.
2. More conservative for public documentation sites.
3. Aligns with the current NFR document.

Cons:

1. Limits adoption of some modern CSS features or requires more testing and fallbacks.
2. Slows down iteration on layout and component styling.

### Option B: Target latest Chromium-based browsers for website2

Pros:

1. Aligns with the requested direction.
2. Maximizes freedom to use current HTML and CSS capabilities.
3. Simplifies implementation by reducing compatibility work.

Cons:

1. Narrows compatibility versus the current site policy.
2. Needs explicit communication so expectations remain clear.
3. May exclude some contributors or readers using Firefox.

### Option C: Use latest features with graceful degradation outside the primary baseline

Pros:

1. Preserves freedom to use modern platform features.
2. Allows a strong primary fidelity target without blocking access elsewhere.
3. Supports reduced visual fidelity outside the primary browser baseline.

Cons:

1. Adds implementation and testing complexity.
2. Requires clearer documentation of what is guaranteed versus best-effort.
3. Some animations or advanced layouts may not match perfectly outside the primary baseline.

## Decision

Use semantic HTML and native CSS features without a CSS framework, with latest Chromium-based browsers as the primary fidelity baseline. Outside that baseline, including older Chromium versions and other evergreen browsers, reduced fidelity for visuals, animations, and advanced styling is acceptable as long as core content and navigation remain usable.

## Rationale

This matches the requested direction while avoiding an unnecessarily strict compatibility promise. It supports a modern styling model based on semantic HTML, custom properties, cascade layers, container queries, and other current native browser features, while allowing graceful degradation where exact fidelity is not required.

This decision is accepted.

## Consequences

Positive:

1. Cleaner implementation with fewer compromises.
2. Stronger alignment with the goal of using current platform features.
3. Better fit for a static site that is primarily maintained by repository contributors.
4. Allows reduced-fidelity handling for non-primary browsers instead of all-or-nothing compatibility work.

Negative:

1. Browser support expectations must be communicated clearly in user-facing docs.
2. Visual and animation differences must be treated as acceptable outside the primary baseline.
