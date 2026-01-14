# Architecture Decisions for Initial Project Setup

This document contains the architecture decision records (ADRs) that were made during the initial setup of the tfplan2md project. These decisions establish the foundational technical choices that shape the project's architecture and implementation.

## Table of Contents

1. [ADR-001: Use Scriban for Markdown Templating](#adr-001-use-scriban-for-markdown-templating)
2. [ADR-002: Use .NET Chiseled (Distroless) Docker Image](#adr-002-use-net-chiseled-distroless-docker-image)
3. [ADR-003: Use Modern C# 13 Patterns](#adr-003-use-modern-c-13-patterns)
4. [ADR-004: Use CSS Layers for Example Style Isolation](#adr-004-use-css-layers-for-example-style-isolation)

---

## ADR-001: Use Scriban for Markdown Templating

### Status

Accepted

### Context

tfplan2md needs to generate Markdown reports from Terraform plan data. Users should be able to customize the output format to fit their specific needs (e.g., different CI/CD systems, documentation styles).

We evaluated several options:

1. **Hardcoded string generation** - Simple but inflexible
2. **Razor templates** - Powerful but heavyweight, designed for HTML
3. **Liquid templates** - Popular in static site generators
4. **Scriban templates** - Lightweight, fast, designed for text generation

### Decision

Use [Scriban](https://github.com/scriban/scriban) as the templating engine.

### Rationale

- **Lightweight**: Minimal dependencies, fast startup time suitable for CLI tools
- **Text-focused**: Designed for text generation, not HTML-specific like Razor
- **Familiar syntax**: Similar to Liquid, widely known in DevOps communities
- **Embeddable**: Easy to embed default templates as resources
- **Well-maintained**: Active development and good documentation
- **No runtime compilation**: Templates are interpreted, avoiding complexity

### Consequences

#### Positive

- Users can create custom templates without deep .NET knowledge
- Default template can be embedded as a resource for zero-config usage
- Template syntax is familiar to users of Jekyll, Hugo, and other static site generators

#### Negative

- Another dependency to maintain
- Template errors surface at runtime, not compile time
- Users need to learn Scriban syntax for advanced customization

---

## ADR-002: Use .NET Chiseled (Distroless) Docker Image

### Status

Superseded by Feature 037 (AOT-Compiled Trimmed Docker Image)

As of Feature 037, the Docker image uses FROM scratch with minimal musl libraries instead of the chiseled runtime image, achieving 14.7MB (89.6% reduction from baseline) with superior security posture.

### Context

tfplan2md is distributed as a Docker image for use in CI/CD pipelines. The choice of base image affects:

- Image size (download time, storage costs)
- Security (attack surface, CVE exposure)
- Compatibility (shell access, debugging capabilities)

Options considered:

1. **Full runtime image** (`mcr.microsoft.com/dotnet/runtime:10.0`) - ~200MB, includes shell
2. **Alpine-based image** (`mcr.microsoft.com/dotnet/runtime:10.0-alpine`) - ~100MB, musl libc
3. **Chiseled image** (`mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled`) - ~50MB, no shell

### Decision

Use the .NET Chiseled (distroless) image as the runtime base image.

### Rationale

- **Minimal attack surface**: No shell, no package manager, no unnecessary utilities
- **Smallest size**: Approximately 50MB, fastest to pull in CI/CD pipelines
- **Security-first**: Runs as non-root by default, fewer CVEs to patch
- **Microsoft-supported**: Official Microsoft image with regular security updates
- **Sufficient for CLI**: tfplan2md doesn't need shell access or debugging tools

### Consequences

#### Positive

- Smallest possible image size for faster CI/CD pipelines
- Minimal security vulnerabilities from OS components
- Follows security best practices for containerized applications

#### Negative

- Cannot shell into container for debugging (use multi-stage builds with debug target if needed)
- Some diagnostic tools unavailable at runtime
- Must ensure all dependencies are statically linked or included

---

## ADR-003: Use Modern C# 13 Patterns

### Status

Accepted

### Context

tfplan2md is built with .NET 10 and C# 13. Modern C# offers many features that can improve code quality, but adopting them requires consistent patterns across the codebase.

Key patterns to decide on:

1. **Records vs classes** for data types
2. **File-scoped namespaces** vs block-scoped
3. **Nullable reference types** enabled or disabled
4. **Primary constructors** vs traditional constructors

### Decision

Adopt the following C# 13 patterns consistently:

1. **Records** for immutable data transfer objects (DTOs) and models
2. **File-scoped namespaces** for all files
3. **Nullable reference types** enabled project-wide
4. **Primary constructors** for simple record types

### Rationale

#### Records

- Immutable by default, reducing bugs from unintended mutations
- Built-in value equality, useful for testing
- Concise syntax with positional parameters
- Perfect for Terraform plan data models that shouldn't change after parsing

#### File-scoped namespaces

- Reduces indentation by one level across entire files
- Cleaner code with less visual noise
- Recommended by .NET team for new projects

#### Nullable reference types

- Catches null reference bugs at compile time
- Self-documenting: `string?` clearly indicates optional values
- Aligns with Terraform plan structure where many fields are optional

#### Primary constructors

- Concise syntax for simple types
- Reduces boilerplate for dependency injection

### Consequences

#### Positive

- Cleaner, more concise code
- Fewer runtime null reference exceptions
- Better IDE support and warnings
- Easier testing with value equality on records

#### Negative

- Team must be familiar with modern C# patterns
- Some edge cases with records (e.g., inheritance) require care
- Nullable annotations require discipline to maintain correctly

---

## ADR-004: Use CSS Layers for Example Style Isolation

### Status

Accepted

### Context

The tfplan2md website (GitHub Pages) displays rendered examples of tool output to show users what the markdown will look like in Azure DevOps and GitHub. These rendered examples need to approximate the target platform's styling (Azure DevOps, GitHub) without interference from the website's own styles.

The problem surfaced when website CSS rules (like `border-radius: 6px` on `details` elements) interfered with rendered examples, making them look different from actual Azure DevOps rendering. Any change to website styles could break examples, and any fix for examples could break the website.

We need **style isolation** so that:
1. Website styles don't interfere with example rendering
2. Example styles don't interfere with website appearance
3. The solution is agent-maintainable (AI agents will maintain this website)

We evaluated six options:

### Option 1: iframe Isolation
Load rendered examples in `<iframe>` elements with separate stylesheets.

**Pros:**
- Complete style isolation (zero interference)
- Browser handles scoping automatically
- Can load actual platform CSS if needed

**Cons:**
- More complex to maintain (separate HTML files or data URIs)
- Height management requires JavaScript (ResizeObserver)
- Accessibility: screen readers treat as separate document
- More complex agent workflow

**Agent maintainability:** Medium

### Option 2: Shadow DOM
Attach Shadow DOM to `.rendered-view` containers with scoped styles.

**Pros:**
- Native browser encapsulation (styles don't leak)
- Clean separation without iframes
- Good browser support

**Cons:**
- Requires JavaScript to attach shadow roots
- Some CSS features behave differently in Shadow DOM
- Theme switching needs explicit handling
- Agent must understand Shadow DOM API

**Agent maintainability:** Medium-High

### Option 3: CSS Layers
Use `@layer` to create isolated style layers with controlled cascade priority.

```css
@layer website, examples;

@layer website {
  /* All website styles */
}

@layer examples {
  .rendered-view { /* isolated styles */ }
}
```

**Pros:**
- Native CSS feature (no JavaScript)
- Explicit cascade control
- Easy to understand and maintain
- Future-proof modern CSS solution

**Cons:**
- Requires refactoring existing CSS structure
- Browser support: Chrome 99+, Firefox 97+, Safari 15.4+ (sufficient for technical audience)
- All website styles must be wrapped in `@layer website`

**Agent maintainability:** High - clear, declarative CSS

### Option 4: CSS Reset (all: revert)
Apply `all: revert` to rendered examples, then rebuild needed styles.

**Pros:**
- Simple concept
- Works in all modern browsers

**Cons:**
- Aggressive - resets everything including display, position
- Must rebuild all needed styles from scratch
- Can break layout properties unexpectedly

**Agent maintainability:** Low - unpredictable side effects

### Option 5: Strict BEM/Namespacing
Use highly specific class naming like `.example-rendered__*` for all styles.

**Pros:**
- No special browser features needed
- Works everywhere
- Easy pattern for agents to follow

**Cons:**
- **No actual isolation** - just convention
- Website tag selectors (like `code`, `details`, `table`) still interfere
- Doesn't solve the current problem

**Agent maintainability:** High - but doesn't provide isolation

### Option 6: Separate Stylesheet + containment
Load `examples.css` after `style.css`, use `contain: style` property.

**Pros:**
- Clear separation of concerns (two files)
- Later stylesheet wins cascade conflicts

**Cons:**
- `contain: style` only prevents counter/quote leakage (limited scope)
- Doesn't prevent website tag selectors from affecting examples
- Still need specificity management

**Agent maintainability:** Medium - two files but clear boundaries

### Decision

Use **CSS Layers (`@layer`)** to isolate example styles from website styles.

### Rationale

CSS Layers provide the best balance of:
- **True isolation**: Explicit cascade control prevents interference
- **Agent maintainability**: Declarative CSS is easy for AI agents to understand and modify
- **Modern best practice**: CSS Layers are the standard solution for cascade management
- **No JavaScript**: Pure CSS solution keeps implementation simple
- **Incremental adoption**: Can refactor existing code gradually

Browser support is sufficient:
- Chrome 99+ (March 2022)
- Firefox 97+ (February 2022)
- Safari 15.4+ (March 2022)
- Edge 99+ (March 2022)

Our technical audience (DevOps engineers, developers) will have modern browsers.

### Implementation Plan

1. Define layer order at top of `style.css`:
   ```css
   @layer base, website, examples;
   ```

2. Wrap existing website styles in `@layer website { ... }`

3. Create `@layer examples { ... }` for `.rendered-view` styles

4. Base layer can contain CSS resets or shared utilities if needed

5. Layer order ensures examples layer always wins cascade conflicts

### Consequences

#### Positive

- **Style isolation**: Website styles cannot interfere with examples, and vice versa
- **Maintainability**: Clear separation makes it obvious which styles affect what
- **Agent-friendly**: Declarative CSS is easy for AI agents to understand and modify
- **Future-proof**: Modern CSS standard that will be supported long-term
- **No JavaScript**: Keeps implementation simple and performant

#### Negative

- **Refactoring required**: Must wrap all existing website CSS in `@layer website { ... }`
- **Browser support**: Drops IE11 (already unsupported, not a concern for technical audience)
- **Learning curve**: Team/agents must understand CSS cascade layers (well-documented feature)

### Related Decisions

- Website must remain agent-maintainable (core design goal)
- Examples must accurately represent Azure DevOps and GitHub rendering
- Solution must work without complex build tools (GitHub Pages constraint)
