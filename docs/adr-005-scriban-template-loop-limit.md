# ADR-005: Increase Scriban Template Loop Limit

## Status

Accepted

## Context

tfplan2md renders markdown using Scriban templates (see [docs/adr-001-scriban-templating.md](adr-001-scriban-templating.md)). Scriban enforces a loop/iteration limit (default behavior is effectively ~1000 iterations) to prevent runaway templates.

In practice:
- Large Terraform plans can legitimately require more than 1000 iterations across nested loops in templates.
- The codebase already mitigates this by excluding most `no-op` resources from the list passed to templates (see comment in `ReportModelBuilder.Build(...)`).
- Despite that mitigation, the iteration limit has been hit in the past with a large plan.

Upcoming and existing features may increase template iteration pressure (more sections, more per-resource rendering), making the current limit a reliability risk.

## Options Considered

### Option 1: Keep the default loop limit (~1000)
- Pros
  - Strongest guardrail against pathological templates
  - Predictable worst-case rendering cost
- Cons
  - Can fail on legitimate large plans, causing report generation to abort
  - Forces more aggressive filtering or restructuring solely to satisfy the limit

### Option 2: Increase loop limit to 10000 (recommended)
- Pros
  - Reduces false failures on large-but-valid plans
  - Preserves existing template approach without requiring heavy filtering
  - Simple, localized change
- Cons
  - Higher worst-case rendering cost if templates accidentally loop excessively
  - Slightly larger “blast radius” for poorly written custom templates

### Option 3: Make the loop limit configurable (CLI flag / env var)
- Pros
  - Allows CI/CD to tune for plan size and platform constraints
  - Keeps a conservative default while supporting power users
- Cons
  - More surface area (CLI/docs/testing)
  - Easy to misconfigure; harder to support

## Decision

Choose **Option 2**: set Scriban’s loop limit to **10000**.

## Rationale

- The tool’s primary purpose is to render plans reliably in CI/CD; failing on a legitimate large plan is worse than a modest increase in worst-case rendering budget.
- 10000 provides an order-of-magnitude increase while still preserving a safety limit.
- Existing mitigations (filtering most `no-op` resources; avoiding extremely heavy template logic) remain valuable and should be retained.

## Consequences

### Positive
- Fewer rendering failures on large plans.
- Less need for template workarounds that reduce report completeness.

### Negative
- If a template (especially a custom one) accidentally loops too much, it can consume more CPU before Scriban stops execution.

## Implementation Notes

For the Developer agent:
- Set `TemplateContext.LoopLimit = 10000` when creating the Scriban `TemplateContext`.
- Primary integration point: `MarkdownRenderer.CreateTemplateContext(...)` in `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs`.
- Keep the existing “exclude most no-op resources” behavior as a complementary mitigation; increasing the limit is not a replacement for sensible filtering.
