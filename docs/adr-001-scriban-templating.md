# ADR-001: Use Scriban for Markdown Templating

## Status

Accepted

## Context

tfplan2md needs to generate Markdown reports from Terraform plan data. Users should be able to customize the output format to fit their specific needs (e.g., different CI/CD systems, documentation styles).

We evaluated several options:

1. **Hardcoded string generation** - Simple but inflexible
2. **Razor templates** - Powerful but heavyweight, designed for HTML
3. **Liquid templates** - Popular in static site generators
4. **Scriban templates** - Lightweight, fast, designed for text generation

## Decision

Use [Scriban](https://github.com/scriban/scriban) as the templating engine.

## Rationale

- **Lightweight**: Minimal dependencies, fast startup time suitable for CLI tools
- **Text-focused**: Designed for text generation, not HTML-specific like Razor
- **Familiar syntax**: Similar to Liquid, widely known in DevOps communities
- **Embeddable**: Easy to embed default templates as resources
- **Well-maintained**: Active development and good documentation
- **No runtime compilation**: Templates are interpreted, avoiding complexity

## Consequences

### Positive

- Users can create custom templates without deep .NET knowledge
- Default template can be embedded as a resource for zero-config usage
- Template syntax is familiar to users of Jekyll, Hugo, and other static site generators

### Negative

- Another dependency to maintain
- Template errors surface at runtime, not compile time
- Users need to learn Scriban syntax for advanced customization
