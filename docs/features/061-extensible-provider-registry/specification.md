# Feature: Extensible Provider Registry System

## Overview

Create a flexible provider registry system that allows providers and platforms to register multiple types of services (resource view model factories, value formatters, icon providers) with pattern-based matching rules. This consolidates cross-cutting concerns and reduces boilerplate code, particularly for icon management.

## User Goals

- **Provider/Platform Authors** want to register services that apply across multiple providers, resource types, or attributes without writing repetitive code for each case
- **Maintainers** want to simplify icon management by using declarative configuration files instead of custom code for each icon
- **Contributors** want to add new formatting or icon rules without modifying core code
- **All Users** benefit from consistent formatting and iconography across different providers through shared service implementations

## Scope

### In Scope

- Extensible registry infrastructure supporting three service types:
  - Resource view model factories (existing functionality, adapted to new system)
  - Value formatters (for transforming attribute values like Azure resource IDs, JSON/XML, readable names)
  - Icon providers (for determining which icon to display for attributes)
- Flexible pattern matching on four dimensions:
  - Provider name (null for all, or regex pattern)
  - Resource type (null for all, or regex pattern)
  - Attribute name (null for all, or regex pattern)
  - Value (null for all, or regex pattern for specific values)
- Specificity-based resolution when multiple services match
- Fallback mechanism allowing services to decline and trigger next match
- File-based icon provider implementation with declarative rule format
- Service registration during application startup/initialization
- (Stretch Goal) Migration of existing functionality to the new registry system

### Out of Scope

- Runtime modification of registered services (registration happens at startup only)
- UI or command-line interface for managing service registrations
- Migration of existing template-specific logic unrelated to formatters/icons/view models
- Performance optimization beyond basic matching algorithm
- Versioning or compatibility management between service implementations

## User Experience

### Service Registration

Providers and platforms register services during application startup by providing:
- Service implementation (factory, formatter, or icon provider)
- Matching criteria tuple: (provider pattern, resource type pattern, attribute name pattern, value pattern)
- Each pattern can be:
  - `null` to match all
  - Regex pattern to match specific cases

**Example registrations:**
```csharp
// Azure resource ID formatter - matches azurerm/azapi providers, any resource/attribute, values that look like resource IDs
registry.RegisterValueFormatter(
    providerPattern: "^(azurerm|azapi)$",
    resourceTypePattern: null,
    attributeNamePattern: null,
    valuePattern: @"^/subscriptions/[^/]+/.*",
    formatter: new AzureResourceIdFormatter()
);

// Generic name attribute icon - matches any provider/resource, only "name" attributes
registry.RegisterIconProvider(
    providerPattern: null,
    resourceTypePattern: null,
    attributeNamePattern: "^name$",
    valuePattern: null,
    iconProvider: new NameAttributeIconProvider()
);

// Resource-specific view model factory - matches specific provider and resource type
registry.RegisterViewModelFactory(
    providerPattern: "^azurerm$",
    resourceTypePattern: "^azurerm_network_security_group$",
    factory: new NetworkSecurityGroupViewModelFactory()
);
```

### Service Resolution

When processing attributes or resources:
1. Registry finds all matching services based on patterns
2. If multiple match, selects most specific:
   - First by count of non-null matchers (4 > 3 > 2 > 1)
   - Then by matcher priority: value > attribute > resource type > provider
3. Invokes selected service
4. If service declines (returns "cannot handle"), tries next match
5. If no service succeeds, uses default behavior (raw value, no icon, etc.)

### Value Formatter Behavior

**Input:**
- Raw value string
- Context: provider name, resource type, attribute name

**Output:**
- Formatted value string (on success)
- Signal "cannot format" to trigger fallback to next formatter

**Default fallback:** Display raw value as-is

**Use cases:**
- Format Azure resource IDs as human-readable names
- Pretty-print JSON/XML values
- Convert role/principal IDs to readable names

### Icon Provider Behavior

**Input:**
- Context: provider name, resource type, attribute name, value

**Output:**
- Icon string (emoji or Unicode character) on success
- Signal "no icon available" to trigger fallback to next provider

**Default fallback:** No icon displayed

**Use cases:**
- Show 🆔 for "id" attributes
- Show 📝 for "name" attributes
- Show 🔑 for authentication-related attributes

### File-Based Icon Provider

A reusable icon provider class that reads rules from a configuration file:
- Each provider (azurerm, azapi, etc.) creates an instance with its own rule file
- Format to be determined by architect (JSON, YAML, or other)
- Rules specify same matching criteria as programmatic registration
- Simplifies adding new icons without code changes

## Success Criteria

### Core Infrastructure
- [ ] Service registry supports registration of resource view model factories, value formatters, and icon providers
- [ ] Pattern matching correctly evaluates regex patterns for provider/resource type/attribute/value
- [ ] Null patterns correctly match all values for that dimension
- [ ] Specificity resolution selects the most specific service when multiple match
- [ ] Services can decline and trigger fallback to next match
- [ ] Default behavior (raw value, no icon) used when no services match or all decline

### File-Based Icon Provider
- [ ] File-based icon provider implementation loads rules from configuration file
- [ ] Icon rule file format is documented and easy to understand
- [ ] Providers can instantiate file-based icon provider with their rule file path
- [ ] File parsing handles errors gracefully with clear error messages

### Integration
- [ ] Services are registered during application startup
- [ ] Existing functionality continues to work correctly
- [ ] No breaking changes to public APIs (additive only)

### Testing
- [ ] Unit tests verify pattern matching logic for all four dimensions
- [ ] Unit tests verify specificity resolution rules
- [ ] Unit tests verify fallback behavior when services decline
- [ ] Integration tests verify service registration and resolution during startup
- [ ] Tests verify file-based icon provider loads and applies rules correctly

### Stretch Goal: Migration
- [ ] Existing resource view model factories migrated to new registry
- [ ] Existing value formatting logic (JSON/XML, readable names) migrated to value formatters
- [ ] Existing icon logic migrated to icon providers or file-based rules
- [ ] Old registration mechanisms deprecated (but kept if migration encounters issues)

## Open Questions

1. **Specificity resolution**: The proposed rule (count of matchers, then value > attribute > resource type > provider) is a design recommendation. The architect may choose a different approach if it provides better behavior.

2. **Icon file format**: Format choice (JSON, YAML, custom text format) deferred to architect based on:
   - Ease of editing
   - Tooling support
   - Performance considerations
   - Consistency with other configuration in the project

3. **Migration strategy**: If migrating existing functionality encounters significant issues (API compatibility, test failures, template changes), is it acceptable to keep both old and new systems temporarily? **Answer**: Yes, stretch goal means best effort - keep old system if problems arise.

4. **Error handling**: How should the system handle invalid regex patterns at registration time? Fail fast at startup or log warning and skip the rule?

5. **Performance**: Should the registry cache compiled regex patterns, or is pattern compilation overhead acceptable for the expected number of registrations?
