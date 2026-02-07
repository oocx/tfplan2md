# ADR-006: Dependency Injection Strategy

## Status

Proposed

## Context

The tfplan2md CLI tool currently uses manual object composition in `ProgramEntry.RunWorkflowAsync()` — a ~230-line method that instantiates and wires all dependencies by hand. While this approach works and is inherently AOT-compatible (since it uses no reflection), it raises questions about maintainability, testability, and extensibility as the codebase grows.

This ADR investigates whether introducing a formal DI mechanism would benefit the project, what options exist, and how each interacts with the project's existing AOT and trimming requirements (`PublishAot=true`, `TrimMode=full`).

### Current State

The codebase already follows several DI-friendly patterns:

- **Constructor injection** is used consistently — all major classes (`ReportModelBuilder`, `MarkdownRenderer`, `PrincipalMapper`) accept dependencies via constructor parameters.
- **Interfaces exist** for key abstractions: `IPrincipalMapper`, `IProviderModule`, `IResourceSummaryBuilder`, `IMetadataProvider`, `IResourceViewModelFactoryRegistry`.
- **Registry pattern** is used for extensible service resolution: `ProviderRegistry`, `ValueFormatterRegistry`, `IconProviderRegistry`.
- **Composition root** is centralized in `ProgramEntry.RunWorkflowAsync()`.
- **No circular dependencies** exist in the object graph.
- **Immutable models** (records) are used for data flow (`TerraformPlan`, `ReportModel`, `CliOptions`).

### Constraints

- **AOT compilation** (`PublishAot=true`) is required — see [Feature 037](features/037-aot-trimmed-image/).
- **Full trimming** (`TrimMode=full`) produces a 14.7 MB image (89.6% reduction from baseline).
- **Single external dependency**: Scriban 6.5.2 (template engine).
- **Target framework**: .NET 10 with C# 13.
- **CLI tool** — single invocation, no long-running process, no request scoping needed.

### Related Decisions

- [ADR-003](adr-003-modern-csharp-patterns.md): Modern C# patterns (primary constructors, records)
- [Feature 047](features/047-provider-code-separation/): Provider code separation with `IProviderModule`
- [Feature 061](features/061-extensible-provider-registry/): Extensible provider registry with pattern-based matching

## Options Considered

### Option 1: Formalized Pure DI (Manual Composition Root — Recommended)

Refactor the existing manual wiring in `ProgramEntry` into a dedicated `CompositionRoot` class that follows Pure DI principles (as described by Mark Seemann). This approach keeps the manual composition but organizes it for clarity, testability, and maintainability.

**What changes:**

- Extract a `CompositionRoot` (or `ServiceComposer`) class from `ProgramEntry.RunWorkflowAsync()`
- Group service creation into logical methods (`CreateParser()`, `CreateProviderRegistry()`, `CreateRenderer()`, etc.)
- Expose the composed services via a structured result (e.g., `ApplicationServices` record)
- `ProgramEntry` becomes a thin orchestrator that calls the composition root and runs the pipeline

**Example structure:**

```csharp
internal sealed class CompositionRoot(CliOptions options)
{
    public TerraformPlanParser CreateParser() => new();

    public IPrincipalMapper CreatePrincipalMapper(DiagnosticContext? diagnostics)
        => string.IsNullOrEmpty(options.PrincipalMappingFile)
            ? NullPrincipalMapper.Instance
            : new PrincipalMapper(options.PrincipalMappingFile, diagnostics);

    public ProviderRegistry CreateProviderRegistry(IPrincipalMapper principalMapper)
    {
        var registry = new ProviderRegistry();
        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(options.RenderTarget);
        registry.RegisterProvider(new AzApiModule());
        registry.RegisterProvider(new AzureADModule());
        registry.RegisterProvider(new AzureRMModule(largeValueFormat, principalMapper));
        registry.RegisterProvider(new AzureDevOpsModule(largeValueFormat));
        return registry;
    }

    // ... etc.
}
```

**Pros:**

- Zero additional dependencies — no new NuGet packages
- 100% AOT and trimming compatible by definition (no reflection, no dynamic code)
- No impact on binary size or startup time
- Compile-time safety — all wiring errors are caught at build time
- Easy to understand — the entire object graph is visible in one place
- Aligns with the project's minimalist philosophy (only Scriban as external dependency)
- Gradual adoption — can be done incrementally without breaking changes
- Tests can instantiate services directly or use the composition root

**Cons:**

- Composition root grows linearly with the number of services
- No automatic lifetime management (singleton/transient/scoped) — but this is irrelevant for a CLI tool with a single invocation
- No automatic wiring — every new dependency must be manually added
- May feel "unsophisticated" compared to container-based DI

---

### Option 2: Source-Generated DI with Pure.DI

[Pure.DI](https://github.com/DevTeam/Pure.DI) is a compile-time DI framework that uses Roslyn source generators to create object composition code. It produces code equivalent to manual wiring but with a declarative configuration API.

**What changes:**

- Add `Pure.DI` NuGet package (compile-time only, no runtime dependency)
- Define a `Composition` partial class with DI bindings
- Source generator produces the wiring code at build time

**Example:**

```csharp
using Pure.DI;

internal partial class Composition
{
    static void Setup() => DI.Setup(nameof(Composition))
        .Bind<IPrincipalMapper>().To<PrincipalMapper>()
        .Bind<TerraformPlanParser>().As(Lifetime.Singleton).To<TerraformPlanParser>()
        .Bind<MarkdownRenderer>().To<MarkdownRenderer>()
        .Root<MarkdownRenderer>("Renderer");
}
```

**Pros:**

- Fully AOT compatible — generates plain C# code at compile time, no reflection
- No runtime dependency — the NuGet package is a pure source generator
- Compile-time validation of the entire dependency graph
- Declarative syntax is more maintainable than manual wiring for large graphs
- Automatic detection of missing or circular dependencies at build time

**Cons:**

- Adds an external compile-time dependency (source generator package)
- Requires learning Pure.DI's API and conventions
- Generated code may be harder to debug (though it's plain C#)
- Less mature ecosystem compared to Microsoft.Extensions.DependencyInjection
- Complex factory patterns (e.g., `PrincipalMapper` conditional creation) may need workarounds
- Provider module registration pattern doesn't fit the standard DI model neatly

---

### Option 3: Source-Generated DI with Jab

[Jab](https://github.com/pakrym/jab) is another compile-time DI source generator, using attribute-based configuration. Created by a Microsoft engineer (pakrym).

**What changes:**

- Add `Jab` NuGet package (compile-time only)
- Define a `[ServiceProvider]` partial class with `[Singleton]`/`[Transient]` attributes

**Example:**

```csharp
[ServiceProvider]
[Singleton(typeof(TerraformPlanParser))]
[Transient(typeof(IPrincipalMapper), typeof(PrincipalMapper))]
[Transient(typeof(MarkdownRenderer))]
internal partial class AppServiceProvider { }
```

**Pros:**

- Fully AOT compatible — source generator, no reflection
- No runtime dependency
- Simple attribute-based API
- Very fast resolution (benchmarks show 200x faster startup vs MS.Extensions.DI)
- Created by a Microsoft engineer, well-maintained

**Cons:**

- Similar external dependency concerns as Pure.DI
- Less flexible than Pure.DI for complex scenarios
- No support for `IServiceCollection` / `IServiceProvider` (different API)
- Limited documentation and smaller community
- Factory patterns require additional boilerplate

---

### Option 4: Microsoft.Extensions.DependencyInjection

The standard .NET DI container, with improving AOT support in .NET 9/10.

**What changes:**

- Add `Microsoft.Extensions.DependencyInjection` NuGet package
- Create a `ServiceCollection` and register all services
- Build a `ServiceProvider` and resolve the root service

**Example:**

```csharp
var services = new ServiceCollection();
services.AddSingleton<TerraformPlanParser>();
services.AddSingleton<IPrincipalMapper>(sp => CreatePrincipalMapper(options));
services.AddSingleton<ProviderRegistry>(sp => CreateProviderRegistry(sp, options));
services.AddTransient<ReportModelBuilder>();
services.AddTransient<MarkdownRenderer>();
var provider = services.BuildServiceProvider();
```

**Pros:**

- Industry standard — widely known and documented
- Rich ecosystem of extensions and integrations
- AOT support improving in .NET 9/10 (source-generated configuration binding)
- Keyed services support (.NET 8+)
- Familiar to most .NET developers

**Cons:**

- Adds a **runtime** dependency (not just compile-time)
- Container resolution still uses some reflection internally (even with AOT improvements)
- May produce trimming warnings that need `[DynamicallyAccessedMembers]` annotations
- Increases binary size (~100-200 KB for the DI assembly and its transitive dependencies)
- Overkill for a CLI tool with ~15 services and no scoping needs
- Lifetime management features (scoped, transient) are unnecessary for single-invocation CLI
- May conflict with the project's "FROM scratch" Docker image goal (adds assemblies)

---

### Option 5: No Change (Keep Current Manual Wiring)

Keep the existing pattern in `ProgramEntry.RunWorkflowAsync()` unchanged.

**Pros:**

- Zero effort
- Already works and is proven
- No risk of introducing issues

**Cons:**

- `ProgramEntry` class continues to grow as new services are added
- The 230-line wiring method mixes composition with orchestration concerns
- Less organized than a dedicated composition root
- Testing requires duplicating the wiring logic

## Decision

**Option 1: Formalized Pure DI** is recommended.

## Rationale

### Why Pure DI is the best fit

1. **AOT and trimming are first-class requirements**: The project already ships a 14.7 MB AOT-compiled Docker image. Pure DI is the only approach that adds absolutely zero risk to this — no new packages, no reflection, no trimmer annotations needed.

2. **The codebase is already 90% there**: Constructor injection is used consistently, interfaces exist for key abstractions, and the composition root is centralized. The refactoring is primarily organizational, not architectural.

3. **CLI tool lifecycle is simple**: With a single invocation and no request scoping, the advanced lifetime management features of DI containers provide no benefit. Every service is effectively a singleton for the duration of the process.

4. **Minimal dependency philosophy**: The project has exactly one external dependency (Scriban). Adding a DI framework — even a compile-time one — goes against this minimalist approach. A 15-service object graph does not need container automation.

5. **Compile-time safety already exists**: Manual wiring catches all dependency errors at compile time. Source-generated DI would provide the same guarantee but through a different mechanism.

6. **Feature 061 alignment**: The upcoming extensible provider registry (Feature 061) uses explicit registration patterns that are naturally compatible with Pure DI. The `IProviderModule` interface already defines a registration contract that doesn't need container integration.

### When to reconsider

Consider migrating to a source-generated DI framework (Option 2 or 3) if:

- The service count grows beyond ~30-40 services
- Plugin/extension loading from external assemblies is needed
- The composition root becomes difficult to maintain (> 300 lines)
- Cross-cutting concerns (logging, caching decorators) need AOP-style injection

### AOT Benefits Analysis

| Aspect | Current (Manual) | With DI Container | With Source-Gen DI |
|--------|------------------|-------------------|-------------------|
| Binary size | 14.7 MB | +100-200 KB | +0 KB |
| Startup time | ~50ms | +5-20ms | +0ms |
| Trimming safety | ✅ Full | ⚠️ Needs annotations | ✅ Full |
| Compile-time validation | ✅ Yes | ❌ Runtime errors | ✅ Yes |
| Reflection usage | None | Some (internal) | None |

## Consequences

### Positive

- Cleaner separation of concerns in `ProgramEntry`
- The composition root becomes a testable, documented unit
- No impact on binary size, startup time, or AOT/trimming compatibility
- Gradual refactoring path — can be done incrementally
- Easier onboarding — new developers can understand the object graph from one class
- Sets a pattern for Feature 061's registry initialization

### Negative

- Every new service requires manual registration in the composition root
- No automatic dependency graph visualization (containers can generate these)
- Developers accustomed to DI containers may find this approach less conventional

## Implementation Notes

High-level guidance for the Developer agent:

### Components to Create

1. **`CompositionRoot` class** (`src/Oocx.TfPlan2Md/CompositionRoot.cs`):
   - Accept `CliOptions` via primary constructor
   - Expose factory methods for each service category
   - Return an `ApplicationServices` record with all composed services

2. **`ApplicationServices` record** (can be nested in `CompositionRoot` or standalone):
   - Holds references to the fully-composed services needed by the pipeline
   - Immutable record type per ADR-003

### Components to Modify

1. **`ProgramEntry.RunWorkflowAsync()`**:
   - Replace inline object creation with `CompositionRoot` usage
   - Keep orchestration logic (parse → build model → render → output)
   - Reduce from ~230 lines to ~30-50 lines

### Key Patterns to Follow

- Use **primary constructors** for the `CompositionRoot` class (per ADR-003)
- Keep factory methods **internal** for test access via `InternalsVisibleTo`
- Follow existing **nullable reference type** patterns for optional dependencies (`DiagnosticContext?`, `IPrincipalMapper?`)
- Maintain the **explicit provider registration** pattern from `IProviderModule`

### Testing Strategy

- Unit test the `CompositionRoot` factory methods independently
- Integration test the full composition (all services resolve without error)
- Existing tests should continue to work unchanged (they instantiate services directly)

### Migration Path

The refactoring can be done in a single PR with these steps:

1. Create `CompositionRoot` class, extracting logic from `ProgramEntry`
2. Update `ProgramEntry` to use `CompositionRoot`
3. Verify all existing tests pass
4. Verify AOT build succeeds with no new trimming warnings
5. Verify Docker image size remains ≤ 15 MB
