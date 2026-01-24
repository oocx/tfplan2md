# Provider Development Guide

This directory contains Terraform provider-specific implementations for tfplan2md. Each provider is a self-contained module that encapsulates templates, helpers, and view models for rendering specific Terraform resources.

## Provider Architecture

### What is a Provider?

A **provider** in tfplan2md corresponds to a Terraform provider (e.g., `azurerm`, `azapi`, `azuredevops`). Each provider module:

- Registers custom Scriban helper functions for provider-specific logic
- Registers resource view model factories for complex resources
- Provides embedded Scriban templates for provider-specific resources
- Keeps provider concerns isolated and modular

### Provider Registration

Providers implement the `IProviderModule` interface and are explicitly registered in `ProviderRegistry` (no reflection):

```csharp
public interface IProviderModule
{
    /// <summary>
    /// Gets the name of the provider (e.g., "azurerm", "azapi").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers provider-specific Scriban helper functions.
    /// </summary>
    void RegisterHelpers(Scriban.TemplateContext context);

    /// <summary>
    /// Registers provider-specific resource view model factories.
    /// </summary>
    void RegisterFactories(IResourceViewModelFactoryRegistry registry);
}
```

## Existing Providers

| Provider | Namespace | Resources | Key Features |
|----------|-----------|-----------|--------------|
| **AzApi** | `Oocx.TfPlan2Md.Providers.AzApi` | `azapi_resource`, `azapi_update_resource` | Templates for Azure API resources |
| **AzureRM** | `Oocx.TfPlan2Md.Providers.AzureRM` | `azurerm_*` (firewall, NSG, role assignments, etc.) | Resource-specific view models, semantic diffs |
| **AzureDevOps** | `Oocx.TfPlan2Md.Providers.AzureDevOps` | `azuredevops_variable_group` | Variable group templates |

## Provider Folder Structure

Each provider follows this structure:

```
Providers/{ProviderName}/
├── {ProviderName}Module.cs           # IProviderModule implementation
├── Models/                           # Resource view models and factories
│   └── {Resource}ViewModelFactory.cs # Optional: for complex resources
├── Helpers/                          # Provider-specific Scriban helpers (optional)
│   └── ScribanHelpers.{Provider}.*.cs
└── Templates/                        # Embedded .sbn templates
    └── {provider}_{resource}.sbn     # One file per resource type
```

**Example: AzureRM Provider**

```
Providers/AzureRM/
├── AzureRMModule.cs
├── Models/
│   ├── FirewallNetworkRuleCollectionViewModelFactory.cs
│   ├── NetworkSecurityGroupViewModelFactory.cs
│   └── RoleAssignmentViewModelFactory.cs
├── Helpers/
│   └── ScribanHelpers.AzureRM.RoleAssignments.cs
└── Templates/
    ├── azurerm_network_security_group.sbn
    ├── azurerm_firewall_network_rule_collection.sbn
    └── azurerm_role_assignment.sbn
```

## Adding a New Provider

### Step 1: Create Provider Folder

Create the provider folder structure:

```bash
mkdir -p src/Oocx.TfPlan2Md/Providers/{ProviderName}/{Models,Helpers,Templates}
```

### Step 2: Implement IProviderModule

Create `{ProviderName}Module.cs`:

```csharp
namespace Oocx.TfPlan2Md.Providers.{ProviderName};

using Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Provider module for {Provider} resources.
/// Related feature: docs/features/047-provider-code-separation/
/// </summary>
internal sealed class {ProviderName}Module : IProviderModule
{
    /// <inheritdoc />
    public string Name => "{providername}"; // lowercase provider name

    /// <inheritdoc />
    public void RegisterHelpers(Scriban.TemplateContext context)
    {
        // Register provider-specific helper functions (if any)
        // Example:
        // var helpers = new ScribanHelpers.{ProviderName}.CustomHelpers();
        // context.PushGlobal(helpers);
    }

    /// <inheritdoc />
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        // Register resource-specific view model factories (if any)
        // Example:
        // registry.Register("{providername}_{resource}", new CustomResourceViewModelFactory());
    }
}
```

### Step 3: Add Templates

Create Scriban templates in `Templates/` for each resource type you want to customize:

**File naming:** `{provider}_{resource}.sbn` (e.g., `azurerm_network_security_group.sbn`)

**Template structure:**

```scriban
{{~ # Provider: {providername}, Resource: {resource} ~}}
{{~ # Related feature: docs/features/047-provider-code-separation/ ~}}

<details>
<summary>{{ action_symbol }} <code>{{ address }}</code></summary>

{{~ # Add provider-specific rendering here ~}}
{{ for attribute in attribute_changes }}
- **{{ attribute.name }}**: {{ attribute.before }} → {{ attribute.after }}
{{ end }}

</details>
```

**Template embedding:**

Templates must be marked as embedded resources in the `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Providers\{ProviderName}\Templates\*.sbn" />
</ItemGroup>
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

### Step 5: Add Helper Functions (Optional)

For provider-specific logic, create helper classes in `Helpers/`:

```csharp
namespace Oocx.TfPlan2Md.Providers.{ProviderName}.Helpers;

using Scriban.Runtime;

/// <summary>
/// Scriban helper functions for {Provider} resources.
/// Related feature: docs/features/047-provider-code-separation/
/// </summary>
internal sealed class ScribanHelpers{ProviderName}
{
    /// <summary>
    /// Custom helper function description.
    /// </summary>
    [ScriptMemberIgnore]
    public static string CustomFunction(string input)
    {
        // Implementation
        return input;
    }
}
```

Register helpers in your module's `RegisterHelpers` method:

```csharp
var helpers = new ScribanHelpers{ProviderName}();
context.PushGlobal(helpers);
```

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

Create tests in `tests/Oocx.TfPlan2Md.TUnit/Providers/{ProviderName}/`:

```
tests/Oocx.TfPlan2Md.TUnit/Providers/{ProviderName}/
├── {Resource}TemplateTests.cs      # Template rendering tests
└── {Resource}ViewModelFactoryTests.cs  # View model factory tests (if applicable)
```

**Example test:**

```csharp
namespace Oocx.TfPlan2Md.TUnit.Providers.{ProviderName};

using TUnit.Assertions.Extensions;
using TUnit.Core;

/// <summary>
/// Tests for {provider}_{resource} template rendering.
/// Related feature: docs/features/047-provider-code-separation/
/// </summary>
[TestClass]
public sealed class {Resource}TemplateTests
{
    [Test]
    public async Task Render_{Resource}_Success()
    {
        // Arrange: Create test plan with {provider}_{resource} resource
        // Act: Render markdown
        // Assert: Verify output contains expected content
        await Assert.That(markdown).Contains("expected content");
    }
}
```

### Step 8: Update Documentation

1. Update `docs/architecture.md` section 5.2.4 (Providers Component) to list your provider
2. Update this README with your provider in the "Existing Providers" table
3. Add provider-specific documentation (if needed)

## Template Loading Order

The `ScribanTemplateLoader` searches for templates in this order:

1. **Custom templates** (if `--template` directory specified): `{customDir}/{provider}/{resource}.sbn`
2. **Provider templates** (embedded): `Providers.{Provider}.Templates.{provider}_{resource}.sbn`
3. **Core fallback** (embedded): `MarkdownGeneration.Templates._resource.sbn`

This allows:
- Providers to override the default resource template for specific resource types
- Users to override any template with custom templates
- Graceful fallback to `_resource.sbn` for resources without custom templates

## Best Practices

### DO:
- ✅ Keep provider logic isolated within the provider folder
- ✅ Use meaningful resource view models for complex semantic diffs
- ✅ Document all helper functions with XML comments
- ✅ Write comprehensive tests for templates and view models
- ✅ Follow the existing naming conventions (`{provider}_{resource}.sbn`)
- ✅ Use `InternalsVisibleTo` for test access (already configured)

### DON'T:
- ❌ Add reflection-based provider discovery (use explicit registration)
- ❌ Put complex logic in templates (use helper functions or view models instead)
- ❌ Create circular dependencies between providers
- ❌ Use mutable state in provider modules (keep them stateless)
- ❌ Expose provider types as `public` (use `internal` for all provider code)

## Native AOT Compatibility

All providers must be compatible with Native AOT compilation:

- ✅ No reflection on user types
- ✅ All Scriban helper functions must be statically discoverable
- ✅ Use `System.Text.Json` source generators for JSON serialization (if needed)
- ✅ Avoid dynamic code generation

**Verify AOT compatibility:**

```bash
dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -r linux-x64 -c Release /p:PublishAot=true
```

No new trimming warnings should appear after adding a provider.

## Example: Adding a New AWS Provider

**Scenario:** Add support for `aws_security_group` with custom template.

### 1. Create folder structure:

```bash
mkdir -p src/Oocx.TfPlan2Md/Providers/AWS/Templates
```

### 2. Create `AWSModule.cs`:

```csharp
namespace Oocx.TfPlan2Md.Providers.AWS;

using Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Provider module for AWS resources.
/// Related feature: docs/features/047-provider-code-separation/
/// </summary>
internal sealed class AWSModule : IProviderModule
{
    public string Name => "aws";

    public void RegisterHelpers(Scriban.TemplateContext context)
    {
        // No custom helpers needed for this example
    }

    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        // No custom view models needed for this example
    }
}
```

### 3. Create `Templates/aws_security_group.sbn`:

```scriban
{{~ # Provider: aws, Resource: security_group ~}}

<details>
<summary>{{ action_symbol }} <code>{{ address }}</code> ({{ name }})</summary>

**Security Group Rules:**
{{ for attribute in attribute_changes }}
  {{ if attribute.name == "ingress" || attribute.name == "egress" }}
- **{{ attribute.name }}**: {{ attribute.before }} → {{ attribute.after }}
  {{ end }}
{{ end }}

</details>
```

### 4. Add to `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Providers\AWS\Templates\*.sbn" />
</ItemGroup>
```

### 5. Register in `ProviderRegistry.cs`:

```csharp
ProviderRegistry.RegisterProviders(
    new AzApiModule(),
    new AzureRMModule(),
    new AzureDevOpsModule(),
    new AWSModule()
);
```

### 6. Test:

```bash
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/AWSProviderTests/*
```

## Additional Resources

- **Architecture Documentation:** [docs/architecture.md](../../../docs/architecture.md)
- **Scriban Language Reference:** [Scriban Docs](https://github.com/scriban/scriban/blob/master/doc/language.md)
- **Report Style Guide:** [docs/report-style-guide.md](../../../docs/report-style-guide.md)
- **Feature Specification:** [docs/features/047-provider-code-separation/specification.md](../../../docs/features/047-provider-code-separation/specification.md)

## Questions?

If you have questions about adding a new provider, please:

1. Review the existing providers (AzApi, AzureRM, AzureDevOps) for examples
2. Check the architecture documentation
3. Open an issue on GitHub with your questions

---

**Last Updated:** January 2026 (Feature 047: Provider Code Separation)
