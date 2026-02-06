# Architecture: Extensible Provider Registry System

## Status

Proposed

## Context

The specification (docs/features/061-extensible-provider-registry/specification.md) defines the need for a flexible registry system that allows providers to register multiple service types (resource view model factories, value formatters, icon providers) with pattern-based matching on four dimensions: provider name, resource type, attribute name, and value.

**Problem:** Today, value formatting and icon logic is hardcoded in `ScribanHelpers` partial classes (`SemanticFormatting.cs`, `SemanticFormatting.Helpers.cs`, `SemanticFormatting.Identity.cs`, `ValueFormatting.cs`). Adding a new icon or formatting rule requires modifying core C# files—a cascade of `TryFormat*` methods with no declarative configuration. The existing `ResourceViewModelFactoryRegistry` uses exact string matching and is limited to one service type.

**Existing Architecture:**
- `ProviderRegistry` → registers `IProviderModule` instances at startup (explicit, no reflection)
- `ResourceViewModelFactoryRegistry` → exact-match `Dictionary<string, IResourceViewModelFactory>` keyed by resource type
- `IProviderModule` → exposes `RegisterHelpers()` and `RegisterFactories()` per provider
- `ScribanHelpers.FormatAttributeValue*()` → hardcoded chain of `TryFormat*` methods for semantic icons
- `ScribanHelpers.FormatValue()` → hardcoded Azure resource ID detection

## Answers to Open Questions

### Question 1: Specificity Resolution

**Decision:** Use the proposed specificity algorithm from the specification.

**Rationale:**
- Count of non-null matchers (4 > 3 > 2 > 1) gives a natural specificity hierarchy
- Dimension priority (value > attribute > resource type > provider) matches real-world usage: a rule matching a specific value is more specific than one matching a broad provider
- The algorithm is deterministic and easy to reason about
- Stable sort order (registration order) breaks remaining ties, giving providers explicit control

### Question 2: Icon File Format

**Decision:** Use JSON for icon rule files.

**Rationale:**
- The project already uses `System.Text.Json` extensively (Terraform plan parsing, principal mapping, Azure role definitions, Azure API documentation mappings)
- The project has established patterns for AOT-compatible JSON source generators (`JsonSerializerContext` subclasses)
- No additional dependency required (YAML would require a new NuGet package)
- JSON is natively supported by IDE tooling (IntelliSense, validation, formatting)
- Consistency with existing configuration files in the project (`AzureRoleDefinitions.json`, `AzureApiDocumentationMappings.json`)

### Question 3: Error Handling for Invalid Regex

**Decision:** Fail fast at startup with a descriptive exception.

**Rationale:**
- This is a CLI tool with fast startup; invalid patterns indicate a programming error or malformed configuration, not user input
- Failing at registration time surfaces issues immediately rather than producing silently incorrect output
- Matches the project's error handling philosophy: throw specific exceptions (`TerraformPlanParseException`, `MarkdownRenderException`, `CliParseException`) with clear messages
- A new `ServiceRegistrationException` will be thrown with the invalid pattern and provider context

### Question 4: Performance / Regex Caching

**Decision:** Cache compiled `Regex` instances at registration time.

**Rationale:**
- Patterns are registered once at startup and evaluated many times during processing
- `new Regex(pattern, RegexOptions.Compiled)` amortizes compilation cost across all evaluations
- The number of registrations is small (dozens, not thousands) so memory overhead is negligible
- Each `ServiceRegistration<T>` stores pre-compiled `Regex?` fields (null for wildcard patterns)

## ADR: Registry Design Pattern

### Options Considered

#### Option 1: Generic Pattern-Matching Registry with Typed Wrappers (Selected)

A reusable `PatternMatchingRegistry<TService>` provides the core matching and specificity engine. Thin typed wrapper classes (`ValueFormatterRegistry`, `IconProviderRegistry`) give type safety. The existing `ResourceViewModelFactoryRegistry` stays as-is for exact-match scenarios; view model factories can optionally migrate in the stretch goal.

- **Pros:** DRY matching logic, follows existing explicit-registration patterns, type-safe, clean SRP, easiest to test in isolation
- **Cons:** Generic class adds a layer of indirection

#### Option 2: Monolithic Service Registry

A single `ExtensibleServiceRegistry` class with methods for all three service types (`RegisterValueFormatter`, `RegisterIconProvider`, `RegisterViewModelFactory`).

- **Pros:** Single entry point, simpler API surface
- **Cons:** Violates SRP, class grows large, harder to test service types independently, harder to extend with new service types

#### Option 3: Extend Existing Registries In-Place

Add pattern matching directly into the existing `ResourceViewModelFactoryRegistry`, plus new `FormatValue`/`GetIcon` methods.

- **Pros:** Fewer new classes
- **Cons:** Mixes exact-match and regex-match logic, higher regression risk, harder to evolve independently

### Decision

**Option 1: Generic Pattern-Matching Registry with Typed Wrappers.**

This follows the existing architecture's principle of typed registries (`ResourceViewModelFactoryRegistry` for factories, `ProviderRegistry` for modules) and keeps the matching engine reusable. New service types can be added by creating a thin wrapper around `PatternMatchingRegistry<TNewService>`.

### Rationale

- Aligns with the existing pattern of explicit registration at startup (no reflection, AOT-compatible)
- The generic core is testable independently of any specific service type
- Type-safe wrappers prevent accidentally registering a formatter as an icon provider
- The existing `ResourceViewModelFactoryRegistry` continues to work unchanged for the exact-match use case
- SRP: matching logic, value formatting, icon resolution, and view model creation are each owned by separate classes

## Detailed Design

### Component Structure

```
MarkdownGeneration/
  Models/
    IResourceViewModelFactory.cs          # existing, unchanged
    IResourceViewModelFactoryRegistry.cs  # existing, unchanged
    ResourceViewModelFactoryRegistry.cs   # existing, unchanged
  Services/
    MatchPattern.cs                       # Value object: (provider, resourceType, attribute, value) regex patterns
    ServiceRegistration.cs                # Record: (TService, MatchPattern, specificity score)
    PatternMatchingRegistry.cs            # Generic core: Register<TService>(), Resolve()
    IValueFormatter.cs                    # Interface: TryFormat(context) → string?
    ValueFormatterRegistry.cs             # Typed wrapper around PatternMatchingRegistry<IValueFormatter>
    IIconProvider.cs                      # Interface: TryGetIcon(context) → string?
    IconProviderRegistry.cs               # Typed wrapper around PatternMatchingRegistry<IIconProvider>
    ServiceResolutionContext.cs           # Record: (providerName, resourceType, attributeName, value)
    FileBasedIconProvider.cs              # IIconProvider loading rules from embedded JSON
    IconRule.cs                           # Model for JSON deserialization of icon rules
    IconRulesJsonContext.cs               # AOT-compatible JSON source generator
```

### Key Interfaces

```csharp
/// <summary>
/// Context passed to service resolution, providing the four matching dimensions.
/// </summary>
internal sealed record ServiceResolutionContext(
    string? ProviderName,
    string? ResourceType,
    string? AttributeName,
    string? Value);

/// <summary>
/// Formats attribute values with provider-aware logic.
/// </summary>
internal interface IValueFormatter
{
    /// <summary>
    /// Attempts to format the given value. Returns null to signal "cannot handle" (triggers fallback).
    /// </summary>
    string? TryFormat(ServiceResolutionContext context);
}

/// <summary>
/// Provides icons for attributes.
/// </summary>
internal interface IIconProvider
{
    /// <summary>
    /// Attempts to provide an icon. Returns null to signal "no icon" (triggers fallback).
    /// </summary>
    string? TryGetIcon(ServiceResolutionContext context);
}
```

### Pattern Matching Core

```csharp
/// <summary>
/// Immutable value object representing regex patterns for the four matching dimensions.
/// Null patterns match all values for that dimension.
/// </summary>
internal sealed class MatchPattern
{
    // Pre-compiled Regex? for each dimension (null = wildcard)
    public Regex? ProviderPattern { get; }
    public Regex? ResourceTypePattern { get; }
    public Regex? AttributeNamePattern { get; }
    public Regex? ValuePattern { get; }

    // Specificity score: count of non-null patterns (0–4)
    public int Specificity { get; }

    // Dimension priority for tie-breaking (value=8, attribute=4, resource=2, provider=1)
    public int DimensionPriority { get; }
}

/// <summary>
/// Generic registry that matches services by pattern and resolves by specificity.
/// </summary>
internal sealed class PatternMatchingRegistry<TService> where TService : class
{
    void Register(MatchPattern pattern, TService service);
    TService? Resolve(ServiceResolutionContext context);
    IReadOnlyList<TService> ResolveAll(ServiceResolutionContext context);
}
```

**Resolution algorithm in `Resolve()`:**
1. Find all registrations whose patterns match the context
2. Sort by specificity (descending), then by dimension priority (descending), then by registration order (ascending)
3. Return the first match

**Resolution algorithm in `ResolveAll()` (for fallback support):**
1. Same as above but returns the full sorted list
2. Callers iterate and try each service until one succeeds (returns non-null)

### Typed Wrappers

```csharp
internal sealed class ValueFormatterRegistry
{
    private readonly PatternMatchingRegistry<IValueFormatter> _inner = new();

    void Register(MatchPattern pattern, IValueFormatter formatter);

    /// <summary>
    /// Resolves and invokes formatters in specificity order until one succeeds.
    /// Returns null if none handle the value (caller uses default behavior).
    /// </summary>
    string? TryFormat(ServiceResolutionContext context);
}

internal sealed class IconProviderRegistry
{
    private readonly PatternMatchingRegistry<IIconProvider> _inner = new();

    void Register(MatchPattern pattern, IIconProvider provider);

    /// <summary>
    /// Resolves and invokes icon providers in specificity order until one succeeds.
    /// Returns null if none provide an icon.
    /// </summary>
    string? TryGetIcon(ServiceResolutionContext context);
}
```

### File-Based Icon Provider

A reusable `IIconProvider` implementation that reads rules from an embedded JSON resource.

**JSON format:**

```json
{
  "rules": [
    {
      "attributeNamePattern": "^name$",
      "icon": "📝"
    },
    {
      "attributeNamePattern": "^(id|.*_id)$",
      "icon": "🆔"
    },
    {
      "providerPattern": "^azurerm$",
      "attributeNamePattern": "^location$",
      "icon": "🌍"
    }
  ]
}
```

Each rule object has optional fields matching the four pattern dimensions plus a required `icon` field. Missing/null dimension fields match all values (wildcard).

**Implementation:** `FileBasedIconProvider` loads the JSON at construction, validates patterns, and delegates matching to an internal `PatternMatchingRegistry<IconRule>`. Each provider module can instantiate it with its own embedded JSON resource path.

### Integration with IProviderModule

Extend the `IProviderModule` interface with two new optional methods:

```csharp
internal interface IProviderModule
{
    // Existing members (unchanged):
    string ProviderName { get; }
    string TemplateResourcePrefix { get; }
    void RegisterHelpers(ScriptObject scriptObject);
    void RegisterFactories(IResourceViewModelFactoryRegistry registry);

    // New members:
    void RegisterValueFormatters(ValueFormatterRegistry registry) { }
    void RegisterIconProviders(IconProviderRegistry registry) { }
}
```

Default interface implementations (`{ }`) ensure existing provider modules compile without changes. New providers override these to register their formatters and icon providers.

### Integration with ProviderRegistry

Add two new methods to `ProviderRegistry`:

```csharp
internal sealed class ProviderRegistry
{
    // Existing methods stay unchanged...

    public void RegisterAllValueFormatters(ValueFormatterRegistry registry)
    {
        foreach (var provider in _providers)
            provider.RegisterValueFormatters(registry);
    }

    public void RegisterAllIconProviders(IconProviderRegistry registry)
    {
        foreach (var provider in _providers)
            provider.RegisterIconProviders(registry);
    }
}
```

### Integration with ScribanHelpers

The `ValueFormatterRegistry` and `IconProviderRegistry` are passed alongside the existing dependencies when registering helpers. New or updated Scriban helper functions call the registries:

- `format_value(value, providerName)` → updated to try `ValueFormatterRegistry.TryFormat()` first, falling back to the existing hardcoded logic
- `format_attribute_value_*()` → updated to try `IconProviderRegistry.TryGetIcon()` first, falling back to existing `TryFormatSemanticValue()` chain
- New helper `get_icon(providerName, resourceType, attributeName, value)` → directly exposes icon resolution to templates

### Startup Wiring (ProgramEntry.cs)

```csharp
var providerRegistry = new ProviderRegistry();
providerRegistry.RegisterProvider(new AzApiModule());
providerRegistry.RegisterProvider(new AzureADModule());
providerRegistry.RegisterProvider(new AzureRMModule(...));
providerRegistry.RegisterProvider(new AzureDevOpsModule(...));

// New: create and populate service registries
var valueFormatterRegistry = new ValueFormatterRegistry();
providerRegistry.RegisterAllValueFormatters(valueFormatterRegistry);

var iconProviderRegistry = new IconProviderRegistry();
providerRegistry.RegisterAllIconProviders(iconProviderRegistry);

// Pass registries to ReportModelBuilder and MarkdownRenderer
```

## Data Flow

```
Startup:
  ProgramEntry → ProviderRegistry.RegisterProvider(each module)
               → ValueFormatterRegistry ← module.RegisterValueFormatters()
               → IconProviderRegistry   ← module.RegisterIconProviders()

Processing (per attribute):
  ScribanHelpers.FormatValue(value, provider)
    → ValueFormatterRegistry.TryFormat(context)
      → PatternMatchingRegistry<IValueFormatter>.ResolveAll(context)
        → sorted by specificity → try each → return first non-null
      → null? → existing hardcoded fallback (AzureScopeParser, backtick wrapping)

  ScribanHelpers.FormatAttributeValue*(name, value, provider)
    → IconProviderRegistry.TryGetIcon(context)
      → PatternMatchingRegistry<IIconProvider>.ResolveAll(context)
        → sorted by specificity → try each → return first non-null
      → null? → existing TryFormatSemanticValue() chain
```

## Consequences

### Positive

- **Declarative icon management:** New icons can be added via JSON files without C# code changes
- **Extensible formatting:** New value formatters registered per provider without modifying `ScribanHelpers`
- **Decoupled providers:** Each provider owns its formatting rules and icon definitions
- **No breaking changes:** Existing interfaces and behavior preserved; new methods use default interface implementations
- **Testable in isolation:** Generic registry core tested independently of any specific service type
- **AOT-compatible:** No reflection; JSON deserialization uses source generators

### Negative

- **Added abstraction layer:** Introduction of generics adds indirection that developers must understand
- **Dual code paths during transition:** Existing hardcoded logic remains alongside new registry-based resolution until the stretch goal migration is complete
- **Pattern complexity:** Regex patterns require careful authoring; invalid patterns fail at startup (mitigated by fail-fast)

## Implementation Notes

### Components to Create

| File | Purpose |
|------|---------|
| `MarkdownGeneration/Services/MatchPattern.cs` | Pattern value object with compiled regexes |
| `MarkdownGeneration/Services/ServiceRegistration.cs` | Registration record pairing pattern with service |
| `MarkdownGeneration/Services/PatternMatchingRegistry.cs` | Generic matching and specificity engine |
| `MarkdownGeneration/Services/ServiceResolutionContext.cs` | Context record for the four dimensions |
| `MarkdownGeneration/Services/IValueFormatter.cs` | Value formatter interface |
| `MarkdownGeneration/Services/ValueFormatterRegistry.cs` | Typed wrapper for value formatters |
| `MarkdownGeneration/Services/IIconProvider.cs` | Icon provider interface |
| `MarkdownGeneration/Services/IconProviderRegistry.cs` | Typed wrapper for icon providers |
| `MarkdownGeneration/Services/FileBasedIconProvider.cs` | JSON-driven icon provider |
| `MarkdownGeneration/Services/IconRule.cs` | JSON model for icon rules |
| `MarkdownGeneration/Services/IconRulesJsonContext.cs` | AOT JSON source generator |

### Components to Modify

| File | Change |
|------|--------|
| `Providers/IProviderModule.cs` | Add `RegisterValueFormatters()` and `RegisterIconProviders()` with default implementations |
| `Providers/ProviderRegistry.cs` | Add `RegisterAllValueFormatters()` and `RegisterAllIconProviders()` |
| `ProgramEntry.cs` | Create and wire new registries at startup |
| `MarkdownGeneration/Helpers/ScribanHelpers/Registry.cs` | Accept and pass new registries |
| `MarkdownGeneration/Helpers/ScribanHelpers/ValueFormatting.cs` | Try `ValueFormatterRegistry` before hardcoded logic |
| `MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.cs` | Try `IconProviderRegistry` before hardcoded chain |

### Stretch Goal: Migration

When migrating existing functionality:
1. Extract each `TryFormat*` method in `SemanticFormatting.Helpers.cs` into a standalone `IValueFormatter` or `IIconProvider` implementation
2. Register these implementations declaratively (some in code, some in JSON)
3. Keep the existing hardcoded chain as final fallback until all rules are migrated
4. Remove old code only when tests confirm full equivalence

### Key Patterns to Follow

- **Explicit registration** (no reflection) — same as `ProviderRegistry`
- **Immutable records** for data models — same as `ReportModel`, `ResourceChangeModel`
- **`IReadOnlyList<T>`** for collections — per project conventions
- **`internal` access** for all new types — per `docs/spec.md` coding standards
- **XML doc comments** on all members — per commenting guidelines
- **AOT-compatible JSON** via `JsonSerializerContext` — same as `AzureRoleDefinitionsJsonContext`

## Components Affected

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/` — new directory with all registry infrastructure
- `src/Oocx.TfPlan2Md/Providers/IProviderModule.cs` — extended interface
- `src/Oocx.TfPlan2Md/Providers/ProviderRegistry.cs` — new registration methods
- `src/Oocx.TfPlan2Md/ProgramEntry.cs` — startup wiring
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Registry.cs` — helper registration
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/ValueFormatting.cs` — formatter integration
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.cs` — icon integration
- `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs` — pass registries
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs` — pass registries
- `src/tests/Oocx.TfPlan2Md.TUnit/` — new unit tests for pattern matching, specificity, fallback, file-based icons
