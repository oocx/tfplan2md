# Provider Development Guide

This directory contains Terraform provider-specific implementations for tfplan2md. Each provider
is a self-contained module that contributes factories plus any optional provider capabilities
needed by the report-generation and rendering pipeline.

## Provider Architecture

### Core model

A provider corresponds to a Terraform provider such as `azurerm`, `azapi`, or `azuredevops`.
Every provider implements the narrow `IProvider` contract:

```csharp
internal interface IProvider
{
    string ProviderName { get; }
    string TemplateResourcePrefix { get; }
    void RegisterFactories(IResourceViewModelFactoryRegistry registry);
}
```

Optional capabilities are expressed through separate interfaces instead of one broad module type:

- `IValueFormatterProvider`
- `IIconRegistrationProvider`
- `IParentChildRelationshipProvider`
- `IAttributeChangeFilterProvider`
- `IPostMergeCallbackProvider`
- `IResourceRendererProvider`

This keeps provider contracts narrow while still allowing a provider to contribute multiple kinds
of behavior.

### Centralized provider contributions

Providers are explicitly registered in `CompositionRoot.CreateProviderRegistry`. The runtime then
creates a `ProviderContributionSet`, which is the single place that materializes the registries
used by the rest of the application.

```csharp
var registry = new ProviderRegistry();
registry.RegisterProvider(new AzApiModule(scopeFormatter, entityMapper));
registry.RegisterProvider(new AzureADModule(entityMapper));
registry.RegisterProvider(new AzureRMModule(...));
registry.RegisterProvider(new AzureDevOpsModule(...));

var contributions = registry.CreateContributionSet();
```

`ProviderContributionSet` can then:

- register factories into `ResourceViewModelFactoryRegistry`
- create `ValueFormatterRegistry`
- create `IconProviderRegistry`
- create `ParentChildRelationshipRegistry`
- create `AttributeChangeFilterRegistry`
- create `ResourceRendererRegistry`
- register post-merge callbacks on `ReportModelBuilder`

No reflection-based discovery is used. Provider registration stays explicit and NativeAOT-safe.

## Existing Providers

| Provider | Namespace | Typical responsibilities |
|----------|-----------|--------------------------|
| `azapi` | `Oocx.TfPlan2Md.Providers.AzApi` | AzApi renderers, entity-aware formatting, attribute filters |
| `azuread` | `Oocx.TfPlan2Md.Providers.AzureAD` | Azure AD view models, summary helpers, renderers |
| `azurerm` | `Oocx.TfPlan2Md.Providers.AzureRM` | AzureRM factories, role/scope formatting, parent-child relationships |
| `azuredevops` | `Oocx.TfPlan2Md.Providers.AzureDevOps` | Azure DevOps factories, mappers, renderers, relationship rules |

## Provider Folder Structure

Most providers follow this structure:

```text
Providers/{ProviderName}/
|-- {ProviderName}Module.cs
|-- Models/
|   `-- {Resource}ViewModelFactory.cs
|-- Renderers/
|   `-- {ProviderName}ResourceRenderers.cs
`-- Helpers/
    `-- {helper logic files}
```

Not every provider needs every folder. Keep only the pieces the provider actually uses.

## Adding a New Provider

### 1. Create the provider module

Add a new folder under `src/Oocx.TfPlan2Md/Providers/` and create a module that implements
`IProvider` plus any optional capability interfaces it needs.

```csharp
namespace Oocx.TfPlan2Md.Providers.AWS;

using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AWS.Renderers;

internal sealed class AwsModule : IProvider, IResourceRendererProvider
{
    public string ProviderName => "aws";

    public string TemplateResourcePrefix => "Oocx.TfPlan2Md.Providers.AWS.Templates.";

    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
    }

    public void RegisterResourceRenderers(ResourceRendererRegistry registry)
    {
        registry.Register(new AwsSecurityGroupRenderer());
    }
}
```

### 2. Add renderers and view-model factories as needed

Use `IResourceRenderer` for provider-specific markdown rendering and
`IResourceViewModelFactory` when a resource needs semantic view-model projection before rendering.

```csharp
internal sealed class AwsSecurityGroupRenderer : IResourceRenderer
{
    public string ResourceType => "aws_security_group";

    public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        writer.WriteHeading(change.ActionSymbol, change.Address);
    }
}
```

### 3. Register the provider explicitly

Update `CompositionRoot.CreateProviderRegistry` to register the new provider instance. Do not add
reflection, assembly scanning, or dynamic loading.

### 4. Add tests

Add targeted tests under `src/tests/Oocx.TfPlan2Md.TUnit/Providers/{ProviderName}/` and any
needed snapshot coverage under the markdown-generation test suite.

Typical coverage areas:

- provider-specific view-model factories
- provider-specific renderers
- formatting or mapper helpers
- snapshot or end-to-end rendering behavior when the provider changes visible output

### 5. Verify with repository tooling

Run the supported test wrapper from the repository root:

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

If the provider affects published binaries or AOT compatibility, also verify the Docker build.

## Best Practices

### Do

- Keep provider logic isolated within the provider folder.
- Prefer narrow capability interfaces over broad one-off abstractions.
- Use `IResourceViewModelFactory` for complex semantic diffs.
- Keep provider modules stateless except for injected collaborators.
- Document provider types with XML comments.
- Write direct tests for factories, renderers, and helpers.

### Do Not

- Add reflection-based provider discovery.
- Put unrelated provider logic into `MarkdownGeneration` core modules.
- Introduce mutable static state for provider behavior.
- Expose provider types as `public`.
- Add a new provider capability path without wiring it through `ProviderContributionSet`.

## NativeAOT Compatibility

All providers must stay compatible with NativeAOT compilation:

- No runtime reflection on provider types.
- No `dynamic`, `Activator.CreateInstance`, or similar late-bound patterns.
- All provider code paths must remain statically reachable through explicit registration.

## Additional Resources

- [docs/architecture.md](../../../docs/architecture.md)
- [docs/report-style-guide.md](../../../docs/report-style-guide.md)
- [docs/features/107-remove-scriban/](../../../docs/features/107-remove-scriban/)
- [docs/features/110-refactoring-opportunities/specification.md](../../../docs/features/110-refactoring-opportunities/specification.md)

## Questions

If you need an example, start with one of the existing providers and follow its module,
factory, and renderer tests before introducing new patterns.
