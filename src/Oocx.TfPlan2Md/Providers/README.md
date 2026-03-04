# Provider Development Guide

This directory contains Terraform provider-specific implementations for tfplan2md. Each provider is a self-contained module that encapsulates C# renderers, view models, and registration logic for specific Terraform resources.

## Provider Architecture

### What is a Provider?

A **provider** in tfplan2md corresponds to a Terraform provider (e.g., `azurerm`, `azapi`, `azuredevops`). Each provider module:

- Registers typed C# `IResourceRenderer` implementations for provider-specific resources
- Registers resource view model factories for complex resources
- Keeps provider concerns isolated and modular

### Provider Registration

Providers implement the `IProviderModule` interface and are explicitly registered in `ProviderRegistry` (no reflection-based discovery):

```csharp
internal interface IProviderModule
{
    /// <summary>Gets the unique name of the Terraform provider (e.g., "azurerm").</summary>
    string ProviderName { get; }

    /// <summary>Registers provider-specific resource view model factories.</summary>
    void RegisterFactories(IResourceViewModelFactoryRegistry registry);

    /// <summary>Registers provider-specific C# resource renderers.</summary>
    void RegisterResourceRenderers(ResourceRendererRegistry registry) { }

    // Additional optional registration methods (all have default no-op implementations):
    // RegisterValueFormatters, RegisterIconProviders, RegisterParentChildRelationships,
    // RegisterAttributeChangeFilters, RegisterPostMergeCallbacks
}
```

Resource types are rendered by `IResourceRenderer` implementations. For resource types without a registered renderer, `DefaultResourceRenderer` is used as fallback:

```csharp
internal interface IResourceRenderer
{
    /// <summary>The Terraform resource type handled by this renderer (e.g., "azapi_resource").</summary>
    string ResourceType { get; }

    /// <summary>Renders markdown for a resource change.</summary>
    void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context);
}
```

## Existing Providers

| Provider | Namespace | Resources |
|----------|-----------|-----------|
| **AzApi** | `Oocx.TfPlan2Md.Providers.AzApi` | `azapi_resource`, `azapi_update_resource` |
| **AzureAD** | `Oocx.TfPlan2Md.Providers.AzureAD` | `azuread_group`, `azuread_group_member`, etc. |
| **AzureRM** | `Oocx.TfPlan2Md.Providers.AzureRM` | `azurerm_*` (firewall, NSG, role assignments, etc.) |
| **AzureDevOps** | `Oocx.TfPlan2Md.Providers.AzureDevOps` | `azuredevops_variable_group`, `azuredevops_build_definition` |

## Provider Folder Structure

Each provider follows this structure:

```
Providers/{ProviderName}/
├── {ProviderName}Module.cs           # IProviderModule implementation
├── Models/                           # Resource view models and factories (optional)
│   └── {Resource}ViewModelFactory.cs
├── Renderers/                        # C# resource renderers
│   └── {ProviderName}ResourceRenderers.cs
└── Helpers/                          # Provider-specific helper utilities (optional)
    └── {helper logic files}
```

**Example: AzureDevOps Provider**

```
Providers/AzureDevOps/
├── AzureDevOpsModule.cs
├── Models/
│   └── VariableGroupViewModelFactory.cs
└── Renderers/
    └── AzureDevOpsResourceRenderers.cs   # VariableGroupRenderer, BuildDefinitionRenderer
```

## Adding a New Provider

### Step 1: Create Provider Folder

Create the provider folder structure:

```bash
mkdir -p src/Oocx.TfPlan2Md/Providers/{ProviderName}/{Models,Renderers}
```

### Step 2: Implement IProviderModule

Create `{ProviderName}Module.cs`:

```csharp
namespace Oocx.TfPlan2Md.Providers.{ProviderName};

using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Provider module for {Provider} resources.
/// Related feature: docs/features/047-provider-code-separation/
/// </summary>
internal sealed class {ProviderName}Module : IProviderModule
{
    /// <inheritdoc />
    public string ProviderName => "{providername}"; // lowercase Terraform provider name

    /// <inheritdoc />
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        // Register resource-specific view model factories (if any)
        // registry.Register("{providername}_{resource}", new CustomResourceViewModelFactory());
    }

    /// <inheritdoc />
    public void RegisterResourceRenderers(ResourceRendererRegistry registry)
    {
        // Register C# renderers for provider-specific resource types
        registry.Register(new {Resource}Renderer());
    }
}
```

### Step 3: Implement IResourceRenderer

Create renderer classes in `Renderers/`:

```csharp
namespace Oocx.TfPlan2Md.Providers.{ProviderName}.Renderers;

using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Renderer for {provider}_{resource} resources.
/// </summary>
internal sealed class {Resource}Renderer : IResourceRenderer
{
    /// <inheritdoc />
    public string ResourceType => "{providername}_{resource}";

    /// <inheritdoc />
    public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        writer.WriteHeading(change.ActionSymbol, change.Address);
        // Add provider-specific rendering here using MarkdownWriter methods:
        // writer.WriteTable(...), writer.WriteParagraph(...), writer.WriteDetails(...), etc.
    }
}
```

### Step 4: Add View Models (Optional)

For complex resources requiring semantic diffs, create view model factories in `Models/`:

```csharp
namespace Oocx.TfPlan2Md.Providers.{ProviderName}.Models;

using Oocx.TfPlan2Md.MarkdownGeneration;
using System.Text.Json;

/// <summary>
/// Factory for creating view models for {provider}_{resource} resources.
/// Related feature: docs/features/047-provider-code-separation/
/// </summary>
internal sealed class {Resource}ViewModelFactory : IResourceViewModelFactory
{
    /// <inheritdoc />
    public object? CreateViewModel(JsonElement? before, JsonElement? after)
    {
        // Parse before/after JSON and create a rich view model
        // Example:
        // return new {
        //     Rules = ParseRules(before, after)
        // };
    }
}
```

Register the factory in your module's `RegisterFactories` method:

```csharp
registry.Register("{providername}_{resource}", new {Resource}ViewModelFactory());
```

### Step 5: Add Helper Utilities (Optional)

For provider-specific formatting logic, create helper classes in `Helpers/`:

```csharp
namespace Oocx.TfPlan2Md.Providers.{ProviderName}.Helpers;

/// <summary>
/// Helper utilities for {Provider} resource rendering.
/// </summary>
internal static class {ProviderName}RenderingHelpers
{
    /// <summary>
    /// Custom formatting helper example.
    /// </summary>
    public static string FormatCustomValue(string input)
    {
        // Implementation
        return input;
    }
}
```

Call helpers directly from your renderer's `Render` method.

### Step 6: Register Provider

Add your provider to `ProviderRegistry.cs`:

```csharp
ProviderRegistry.RegisterProviders(
    new AzApiModule(),
    new AzureRMModule(),
    new AzureDevOpsModule(),
    new {ProviderName}Module()  // Add your provider here
);
```

### Step 7: Write Tests

Create tests in `src/tests/Oocx.TfPlan2Md.TUnit/Providers/{ProviderName}/`:

```
src/tests/Oocx.TfPlan2Md.TUnit/Providers/{ProviderName}/
├── {Resource}RendererTests.cs           # Renderer unit tests
└── {Resource}ViewModelFactoryTests.cs   # View model factory tests (if applicable)
```

**Example test:**

```csharp
namespace Oocx.TfPlan2Md.TUnit.Providers.{ProviderName};

using TUnit.Core;
using AwesomeAssertions;

/// <summary>
/// Tests for {provider}_{resource} rendering.
/// Related feature: docs/features/047-provider-code-separation/
/// </summary>
[TestClass]
public sealed class {Resource}RendererTests
{
    [Test]
    public async Task Render_{Resource}_ContainsExpectedContent()
    {
        // Arrange: build a minimal ResourceChangeModel
        // Act: render via the renderer
        // Assert
        await markdown.Should().ContainAsync("expected content");
    }
}
```

For end-to-end coverage, add a snapshot test using `SnapshotTestBase`.

### Step 8: Update Documentation

1. Update `docs/architecture.md` section 5.2.4 (Providers Component) to list your provider
2. Update this README with your provider in the "Existing Providers" table

## Best Practices

### DO:
- ✅ Keep provider logic isolated within the provider folder
- ✅ Use `IResourceRenderer` for resource-specific rendering
- ✅ Use view models (`IResourceViewModelFactory`) for complex semantic diffs
- ✅ Use `MarkdownHelpers` static methods for shared formatting logic
- ✅ Document all types with XML comments
- ✅ Write tests for renderers and view model factories
- ✅ Use `InternalsVisibleTo` for test access (already configured)

### DON'T:
- ❌ Add reflection-based provider discovery (use explicit registration in `ProviderRegistry`)
- ❌ Put complex logic inline in `Render()` — extract to view models or helper classes
- ❌ Create circular dependencies between providers
- ❌ Use mutable state in provider modules (keep them stateless or dependency-injected)
- ❌ Expose provider types as `public` (use `internal` throughout)

## Native AOT Compatibility

All providers must be compatible with Native AOT compilation (`IlcDisableReflection=true`):

- ✅ No runtime reflection on user types
- ✅ Use `System.Text.Json` source generators for JSON serialization (if needed)
- ✅ Avoid `dynamic`, `Activator.CreateInstance`, or other late-bound patterns
- ✅ All provider code paths must be statically reachable

**Verify AOT compatibility:**

```bash
docker build -f src/Dockerfile .
```

No new trimming warnings or AOT errors should appear after adding a provider.

## Example: Adding a New AWS Provider

**Scenario:** Add support for `aws_security_group` with a custom C# renderer.

### 1. Create folder structure:

```bash
mkdir -p src/Oocx.TfPlan2Md/Providers/AWS/Renderers
```

### 2. Create `AWSModule.cs`:

```csharp
namespace Oocx.TfPlan2Md.Providers.AWS;

using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AWS.Renderers;

/// <summary>
/// Provider module for AWS resources.
/// Related feature: docs/features/047-provider-code-separation/
/// </summary>
internal sealed class AWSModule : IProviderModule
{
    public string ProviderName => "aws";

    public void RegisterFactories(IResourceViewModelFactoryRegistry registry) { }

    public void RegisterResourceRenderers(ResourceRendererRegistry registry)
    {
        registry.Register(new AwsSecurityGroupRenderer());
    }
}
```

### 3. Create `Renderers/AwsSecurityGroupRenderer.cs`:

```csharp
namespace Oocx.TfPlan2Md.Providers.AWS.Renderers;

using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

internal sealed class AwsSecurityGroupRenderer : IResourceRenderer
{
    public string ResourceType => "aws_security_group";

    public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        writer.WriteHeading(change.ActionSymbol, change.Address);
        // Add security-group-specific rendering here
    }
}
```

### 4. Register in `ProviderRegistry.cs`:

```csharp
ProviderRegistry.RegisterProviders(
    new AzApiModule(),
    new AzureRMModule(),
    new AzureDevOpsModule(),
    new AWSModule()
);
```

### 5. Test:

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx --treenode-filter /*/*/AWSProviderTests/*
```

## Additional Resources

- **Architecture Documentation:** [docs/architecture.md](../../../docs/architecture.md)
- **Report Style Guide:** [docs/report-style-guide.md](../../../docs/report-style-guide.md)
- **Feature 107 (Pure C# Rendering):** [docs/features/107-remove-scriban/](../../../docs/features/107-remove-scriban/)
- **Feature 047 (Provider Code Separation):** [docs/features/047-provider-code-separation/specification.md](../../../docs/features/047-provider-code-separation/specification.md)

## Questions?

If you have questions about adding a new provider, please:

1. Review the existing providers (AzApi, AzureRM, AzureDevOps) for examples
2. Check the architecture documentation
3. Open an issue on GitHub with your questions

---

**Last Updated:** March 2026 (Feature 107: Remove Scriban — Pure C# Rendering)
