# ADR-003: Use Modern C# 13 Patterns

## Status

Accepted

## Context

tfplan2md is built with .NET 10 and C# 13. Modern C# offers many features that can improve code quality, but adopting them requires consistent patterns across the codebase.

Key patterns to decide on:

1. **Records vs classes** for data types
2. **File-scoped namespaces** vs block-scoped
3. **Nullable reference types** enabled or disabled
4. **Primary constructors** vs traditional constructors

## Decision

Adopt the following C# 13 patterns consistently:

1. **Records** for immutable data transfer objects (DTOs) and models
2. **File-scoped namespaces** for all files
3. **Nullable reference types** enabled project-wide
4. **Primary constructors** for simple record types

## Rationale

### Records

- Immutable by default, reducing bugs from unintended mutations
- Built-in value equality, useful for testing
- Concise syntax with positional parameters
- Perfect for Terraform plan data models that shouldn't change after parsing

### File-scoped namespaces

- Reduces indentation by one level across entire files
- Cleaner code with less visual noise
- Recommended by .NET team for new projects

### Nullable reference types

- Catches null reference bugs at compile time
- Self-documenting: `string?` clearly indicates optional values
- Aligns with Terraform plan structure where many fields are optional

### Primary constructors

- Concise syntax for simple types
- Reduces boilerplate for dependency injection

## Consequences

### Positive

- Cleaner, more concise code
- Fewer runtime null reference exceptions
- Better IDE support and warnings
- Easier testing with value equality on records

### Negative

- Team must be familiar with modern C# patterns
- Some edge cases with records (e.g., inheritance) require care
- Nullable annotations require discipline to maintain correctly
