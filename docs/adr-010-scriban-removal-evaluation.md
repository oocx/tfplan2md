# ADR-010: Evaluate Removing Scriban in Favor of Pure C# Rendering

## Status

Proposed

## Context

tfplan2md currently uses [Scriban](https://github.com/scriban/scriban) as its template engine for generating Markdown reports from Terraform plan data (see [ADR-001](adr-001-scriban-templating.md)). The original decision assumed that **user-customizable templates** were a core requirement — users would write their own `.sbn` files to tailor output for different CI/CD systems or documentation styles.

In practice, user-customizable templates are **no longer a requirement**. All templates and rendering logic are built-in. This changes the cost-benefit equation significantly. The question is whether Scriban still provides net value when the sole consumers of templates are the project maintainers themselves, or whether pure C# rendering would be a better fit.

This ADR evaluates the trade-offs of removing Scriban entirely and replacing it with C# code for all rendering.

## Current Scriban Footprint

To ground the evaluation, here is a quantitative summary of Scriban's presence in the codebase:

| Category | Files | Lines of Code |
|----------|------:|:--------------|
| Scriban template files (`.sbn`) | 27 | ~1,600 |
| ScribanHelpers C# files (helpers called from templates) | 32 | ~6,320 |
| AotScriptObjectMapper (C# ↔ ScriptObject translation) | 1 | ~683 |
| TemplateLoader + TemplateResolver (template infrastructure) | 2 | ~264 |
| MarkdownRenderer (Scriban orchestration) | 1 | ~542 |
| Provider Module registration (RegisterHelpers, etc.) | 4 | ~856 |
| Provider resource model mappers (ScriptObject enrichment) | 17 | ~2,099 |
| Test files referencing Scriban types | 40 | ~11,468 |
| C# files importing `using Scriban` | 57 | — |
| **TrimmerRootDescriptor entries** for Scriban | — | Preserves entire Scriban assembly |

**Key observation:** The ~1,600 lines of actual Scriban template syntax are dwarfed by the ~10,000+ lines of C# "glue" code that exists to support the Scriban integration: AOT mapping, helper registration, template loading, ScriptObject construction, and sensitivity masking on ScriptObject trees.

## Options Considered

### Option 1: Keep Scriban (Status Quo)

Continue using Scriban templates as the rendering layer, with all templates built-in.

#### Pros

- **No migration effort** — Zero cost to maintain the current approach.
- **Separation of concerns** — Templates separate layout (`.sbn`) from data computation (C# models and helpers). Template authors can reason about layout without seeing C# logic.
- **Declarative readability** — Template files are concise and readable for understanding the output structure at a glance. The 27 `.sbn` files are relatively short (average ~59 lines).
- **Resource-specific override pattern** — The `resolve_template` mechanism elegantly dispatches per-resource-type templates (e.g., `azurerm/role_assignment.sbn`) with fallback to `_resource.sbn`. This pattern is well-established and battle-tested.
- **Proven stability** — The current system has been used for dozens of features and produces correct output across GitHub and Azure DevOps render targets.

#### Cons

- **Heavy AOT glue layer** — NativeAOT compatibility requires `AotScriptObjectMapper` (683 lines) to manually map every model property to `ScriptObject` fields, since Scriban's reflection-based `Import` doesn't work under trimming. Every new model property requires a mapping addition.
- **Runtime-only error detection** — Template syntax errors (typos in variable names, missing helpers, broken loops) are only caught at runtime when the template is actually rendered. No compile-time safety.
- **Dual data model** — The application maintains both C# model types (`ReportModel`, `ResourceChangeModel`, etc.) and their Scriban representations (`ScriptObject`, `ScriptArray`). This duplication is a maintenance burden and source of subtle bugs when fields are added to one but not the other.
- **Template loop limit workarounds** — Large plans can hit Scriban's iteration limit, requiring [ADR-005](adr-005-scriban-template-loop-limit.md) and preemptive no-op filtering as mitigations.
- **Dependency size and trimming** — Scriban is the only third-party package. The `TrimmerRootDescriptor.xml` must preserve the entire Scriban assembly to prevent trimming failures, inflating the NativeAOT binary.
- **Complex helper registration** — 64 helper functions are registered via `scriptObject.Import(...)` calls with explicit delegate signatures. Adding a new helper requires modifying `Registry.cs`, adding the implementation, and ensuring the template uses the correct function name and parameter order.
- **Sensitivity masking on ScriptObject trees** — [ADR-009](adr-009-template-json-sensitivity-masking.md) required implementing recursive masking on `ScriptObject`/`ScriptArray` trees instead of on C# model types, adding complexity at the template boundary.

### Option 2: Remove Scriban, Replace with Pure C# Rendering (Recommended)

Replace all Scriban templates and infrastructure with C# methods that directly produce Markdown strings using `StringBuilder` or interpolated strings.

#### Pros

- **Compile-time safety** — All rendering logic becomes statically typed C# code. Typos in property names, missing data, or type mismatches are caught at compile time, not at runtime.
- **Eliminate the AOT glue layer** — Remove `AotScriptObjectMapper` (683 lines), the entire `ScriptObject`/`ScriptArray` mapping infrastructure, and `TrimmerRootDescriptor.xml` entries for Scriban. The C# models already exist and can be used directly.
- **Remove the only third-party dependency** — Scriban 6.5.2 is the sole `PackageReference`. Removing it eliminates dependency management, vulnerability scanning for that package, and the `preserve="all"` trimmer directive. The project becomes fully self-contained.
- **Smaller NativeAOT binary** — Removing the Scriban assembly and its trimmer preservation directive should reduce the NativeAOT binary size meaningfully (Scriban is ~1.5 MB when preserved fully).
- **Simplified provider extension pattern** — Provider modules currently implement `RegisterHelpers(ScriptObject)` to inject template-callable functions. With pure C#, providers would implement typed interfaces (e.g., `IRenderResource`) with method signatures, enabling IDE navigation and compile-time verification.
- **Single data model** — No more dual representation. C# renderers work directly with `ReportModel`, `ResourceChangeModel`, etc. Model properties are consumed via normal C# property access.
- **Better IDE experience** — Find All References, Go to Definition, refactoring, and code coverage analysis work natively on C# rendering methods. Currently, template variable names are opaque strings.
- **Simpler sensitivity handling** — Sensitivity masking can be applied at the model level or inline during rendering, rather than requiring a separate pass over `ScriptObject` trees.
- **Easier testing** — Rendering methods are regular C# methods that can be unit-tested with standard patterns. No need to construct `ScriptObject` trees or register helpers for test scenarios.
- **No loop limit concerns** — C# `foreach` has no iteration limit, eliminating the need for [ADR-005](adr-005-scriban-template-loop-limit.md) workarounds.

#### Cons

- **Significant migration effort** — 27 templates must be converted to C# rendering methods. 57 C# files import Scriban types. ~40 test files reference Scriban types. The migration touches a large surface area, though much of it is mechanical.
- **Loss of declarative layout readability** — Template files provide a visual representation of the output structure. C# rendering methods with `StringBuilder` or string interpolation are harder to visually map to the final Markdown output, especially for complex nested structures like `<details>` blocks with tables.
- **Risk of introducing bugs during migration** — Each template conversion is a potential source of rendering differences. Snapshot tests mitigate this, but subtle whitespace or ordering changes could appear.
- **Rendering logic mixed with business logic risk** — Without templates as a separation boundary, there's a risk that rendering concerns bleed into model-building code over time. Disciplined architecture (dedicated renderer classes) is needed.
- **Harder to quickly prototype layout changes** — Template edits are often faster for layout-only changes (moving a section, changing table structure) since the `.sbn` file directly represents the output. In C#, the output structure is encoded procedurally.
- **Existing Scriban helper functions are reusable** — The 32 `ScribanHelpers/*.cs` files contain substantial formatting logic (diff computation, large value formatting, Azure scope parsing, etc.) that is **not** Scriban-specific. These functions would be retained as-is; only their registration mechanism changes.

### Option 3: Hybrid — Keep Scriban for Core Templates, Remove for Provider Templates

Use Scriban for the 10 core templates (default, summary, _resource, etc.) but replace the 17 provider-specific templates with C# rendering.

#### Pros

- Reduces the AOT mapping surface (provider models no longer need `ScriptObject` enrichment).
- Preserves the declarative layout benefits for the most-edited templates.
- Smaller migration scope than full removal.

#### Cons

- Maintains the Scriban dependency and all its associated infrastructure.
- Creates an inconsistent rendering model (some templates, some C#) that is harder to reason about.
- Provider helpers still need to be registered with Scriban for the core templates that call them.
- Does not achieve the binary size or trimming benefits.

## Decision

**Recommended: Option 2 — Remove Scriban, Replace with Pure C# Rendering.**

The fundamental rationale is that Scriban was chosen to serve a requirement (user-customizable templates) that no longer exists. Without that requirement, Scriban's benefits (familiar template syntax for external users, runtime template loading) become irrelevant, while its costs (AOT glue, dual data model, runtime-only errors, dependency maintenance, binary size) remain.

The project has organically evolved toward a pattern where the vast majority of rendering logic is already in C#:

- ScribanHelpers: 6,320 lines of C# formatting logic
- AotScriptObjectMapper: 683 lines of C#-to-Scriban translation
- Provider mappers: 2,099 lines of ScriptObject enrichment
- ResourceSummaryHtmlBuilder: 268 lines of pure C# HTML/Markdown construction

The 1,600 lines of Scriban template syntax are the thin remaining layer, and they require ~10,000 lines of supporting C# infrastructure. The cost-benefit ratio no longer favors the abstraction.

## Rationale

1. **Scriban's value proposition was user extensibility.** Without user-customizable templates, the template engine is an internal implementation detail that provides no user-facing value.

2. **The AOT tax is substantial.** NativeAOT is a strategic requirement for this project (minimal Docker images, fast startup). Scriban requires preserving the entire assembly from trimming and maintaining a manual mapping layer — a cost that grows with every new model property.

3. **Compile-time safety matters for maintainability.** Template variable names are opaque strings that silently produce empty output when misspelled. C# property access is verified at compile time.

4. **The existing helper functions are the real value.** The 6,320 lines of `ScribanHelpers` are domain logic (diff formatting, Azure scope parsing, value formatting) that is equally useful in a pure C# rendering approach. Only the registration mechanism (`scriptObject.Import(...)`) needs to change.

5. **Snapshot tests provide a safety net.** The existing snapshot test infrastructure captures the exact rendered output for many scenarios. These snapshots serve as the migration's correctness oracle — any rendering difference is immediately detectable.

## Consequences

### Positive

- Eliminates the sole third-party package dependency.
- Reduces NativeAOT binary size (no Scriban assembly preservation).
- Removes ~1,500 lines of Scriban-specific infrastructure (AotScriptObjectMapper, TemplateLoader, TemplateResolver, TrimmerRootDescriptor entries).
- All rendering errors become compile-time errors.
- Simplifies the provider module interface (typed rendering methods instead of `ScriptObject` registration).
- Simplifies sensitivity masking architecture ([ADR-009](adr-009-template-json-sensitivity-masking.md)).
- Removes loop limit concerns ([ADR-005](adr-005-scriban-template-loop-limit.md)).

### Negative

- Large migration effort across ~57 C# files and ~40 test files.
- Risk of subtle rendering differences during migration (mitigated by snapshot tests).
- Loss of declarative template readability for layout structure.
- Requires architectural discipline to keep rendering logic separated from model-building logic.

## Implementation Notes

If this decision is approved, the migration should be executed incrementally:

1. **Phase 1 — Establish the C# rendering framework:**
   - Create a `MarkdownWriter` or `MarkdownBuilder` class with fluent methods for common markdown constructs (headings, tables, details blocks, code blocks).
   - Create an `IResourceRenderer` interface for provider-specific rendering, replacing the template dispatch pattern.
   - Keep the existing `ScribanHelpers` functions as static utility methods (rename namespace from `ScribanHelpers` to `RenderingHelpers` or similar).

2. **Phase 2 — Convert core templates:**
   - Convert `default.sbn`, `_resource.sbn`, `_header.sbn`, `summary.sbn`, etc. to C# renderers.
   - Validate each conversion against existing snapshot tests.
   - Remove `AotScriptObjectMapper` as templates no longer need `ScriptObject` construction.

3. **Phase 3 — Convert provider-specific templates:**
   - Convert each provider's `.sbn` files to typed `IResourceRenderer` implementations.
   - Simplify the provider module interface to remove `RegisterHelpers(ScriptObject)`.
   - Remove `TemplateLoader`, `TemplateResolver`, and embedded `.sbn` resources.

4. **Phase 4 — Cleanup:**
   - Remove Scriban NuGet dependency.
   - Remove `TrimmerRootDescriptor.xml` Scriban entries.
   - Update [ADR-001](adr-001-scriban-templating.md) status to "Superseded by ADR-010".
   - Update [ADR-005](adr-005-scriban-template-loop-limit.md) status to "Superseded by ADR-010".
   - Update [ADR-009](adr-009-template-json-sensitivity-masking.md) to reflect simplified masking approach.
   - Update `docs/architecture.md` and `docs/spec.md` to reflect the new rendering approach.

## References

- [ADR-001: Use Scriban for Markdown Templating](adr-001-scriban-templating.md) — Original decision to adopt Scriban
- [ADR-005: Increase Scriban Template Loop Limit](adr-005-scriban-template-loop-limit.md) — Workaround for Scriban loop limits
- [ADR-009: Mask Sensitive JSON Before Template Rendering](adr-009-template-json-sensitivity-masking.md) — Sensitivity masking at the template boundary
- [Feature 019: Template Rendering Simplification](features/019-template-rendering-simplification/specification.md) — Earlier effort to simplify template architecture
