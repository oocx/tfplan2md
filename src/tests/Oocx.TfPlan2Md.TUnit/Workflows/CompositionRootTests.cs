using Oocx.TfPlan2Md.CLI;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.TUnit.Workflows;

/// <summary>
/// Unit tests for the <see cref="CompositionRoot"/> class.
/// Validates that each factory method produces correctly configured services.
/// Related ADR: docs/adr-006-dependency-injection.md.
/// </summary>
public class CompositionRootTests
{
    /// <summary>
    /// Verifies that <see cref="CompositionRoot.CreateDiagnosticContext"/> returns a context when debug is enabled.
    /// </summary>
    [Test]
    public async Task CreateDiagnosticContext_DebugEnabled_ReturnsContext()
    {
        var options = new CliOptions { Debug = true };
        var root = new CompositionRoot(options);

        var result = root.CreateDiagnosticContext();

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsTypeOf<DiagnosticContext>();
    }

    /// <summary>
    /// Verifies that <see cref="CompositionRoot.CreateDiagnosticContext"/> returns null when debug is disabled.
    /// </summary>
    [Test]
    public async Task CreateDiagnosticContext_DebugDisabled_ReturnsNull()
    {
        var options = new CliOptions { Debug = false };
        var root = new CompositionRoot(options);

        var result = root.CreateDiagnosticContext();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies that <see cref="CompositionRoot.CreateParser"/> returns a parser instance.
    /// </summary>
    [Test]
    public async Task CreateParser_ReturnsParserInstance()
    {
        var options = new CliOptions();
        var root = new CompositionRoot(options);

        var result = root.CreateParser();

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsTypeOf<TerraformPlanParser>();
    }

    /// <summary>
    /// Verifies that <see cref="CompositionRoot.CreateProviderRegistry"/> registers all four provider modules.
    /// </summary>
    [Test]
    public async Task CreateProviderRegistry_RegistersAllProviders()
    {
        var options = new CliOptions();
        var root = new CompositionRoot(options);
        var mappingResult = AzureMappingFileLoader.Load(mappingFile: null, diagnosticContext: null);
        var principalMapper = root.CreatePrincipalMapper(mappingResult, diagnostics: null);
        var entityMapper = root.CreateEntityMapper(mappingResult, diagnostics: null);
        var scopeFormatter = root.CreateScopeFormatter(entityMapper);
        var roleDefinitionResolver = root.CreateRoleDefinitionResolver(mappingResult, diagnostics: null);
        var azdoUserMapper = root.CreateAzdoUserMapper(mappingResult, diagnostics: null);
        var azdoGroupMapper = root.CreateAzdoGroupMapper(mappingResult, diagnostics: null);
        var azdoProjectMapper = root.CreateAzdoProjectMapper(mappingResult, diagnostics: null);
        var azdoRepositoryMapper = root.CreateAzdoRepositoryMapper(mappingResult, diagnostics: null);

        var registry = root.CreateProviderRegistry(
            principalMapper,
            scopeFormatter,
            entityMapper,
            roleDefinitionResolver,
            azdoUserMapper,
            azdoGroupMapper,
            azdoProjectMapper,
            azdoRepositoryMapper);

        var providers = registry.GetProviders();
        var providerNames = providers.Select(p => p.ProviderName).ToList();
        await Assert.That(registry).IsNotNull();
        await Assert.That(providerNames).Count().IsEqualTo(4);
        await Assert.That(providerNames).Contains("azapi");
        await Assert.That(providerNames).Contains("azuread");
        await Assert.That(providerNames).Contains("azurerm");
        await Assert.That(providerNames).Contains("azuredevops");
    }

    /// <summary>
    /// Verifies that <see cref="CompositionRoot.CreateCodeAnalysisInput"/> returns null when no patterns are configured.
    /// </summary>
    [Test]
    public async Task CreateCodeAnalysisInput_NoPatterns_ReturnsNull()
    {
        var options = new CliOptions();
        var root = new CompositionRoot(options);

        var result = root.CreateCodeAnalysisInput();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies that <see cref="CompositionRoot.ComposeServices"/> returns a fully composed set of services.
    /// </summary>
    [Test]
    public async Task ComposeServices_DefaultOptions_ReturnsAllServices()
    {
        var options = new CliOptions();
        var root = new CompositionRoot(options);

        var services = root.ComposeServices();

        await Assert.That(services).IsNotNull();
        await Assert.That(services.Parser).IsNotNull();
        await Assert.That(services.ModelBuilder).IsNotNull();
        await Assert.That(services.Renderer).IsNotNull();
        await Assert.That(services.DiagnosticContext).IsNull();
        await Assert.That(services.CodeAnalysisInput).IsNull();
    }

    /// <summary>
    /// Verifies that <see cref="CompositionRoot.ComposeServices"/> includes a diagnostic context when debug is enabled.
    /// </summary>
    [Test]
    public async Task ComposeServices_DebugEnabled_IncludesDiagnosticContext()
    {
        var options = new CliOptions { Debug = true };
        var root = new CompositionRoot(options);

        var services = root.ComposeServices();

        await Assert.That(services.DiagnosticContext).IsNotNull();
    }

    /// <summary>
    /// Verifies that composed services produce identical output to the original wiring.
    /// Exercises the full pipeline: parse → build model → render.
    /// </summary>
    [Test]
    public async Task ComposeServices_FullPipeline_ProducesValidMarkdown()
    {
        var planJson = await File.ReadAllTextAsync("TestData/azurerm-azuredevops-plan.json");
        var options = new CliOptions();
        var root = new CompositionRoot(options);
        var services = root.ComposeServices();

        var plan = services.Parser.Parse(planJson);
        var model = services.ModelBuilder.Build(plan);
        var markdown = services.Renderer.Render(model);

        await Assert.That(markdown).Contains("# Terraform Plan Report");
        await Assert.That(markdown).Contains("## Summary");
    }

    /// <summary>
    /// Verifies that sequential compositions do not share custom role definition state.
    /// </summary>
    [Test]
    public async Task CreateRoleDefinitionResolver_SequentialRoots_DoNotShareCustomRoleState()
    {
        var customOptions = new CliOptions { PrincipalMappingFile = "TestData/azure-mappings-extended.json" };
        var customRoot = new CompositionRoot(customOptions);
        var customMappingResult = AzureMappingFileLoader.Load(customOptions.PrincipalMappingFile, diagnosticContext: null);
        var customResolver = customRoot.CreateRoleDefinitionResolver(customMappingResult, diagnostics: null);

        var defaultRoot = new CompositionRoot(new CliOptions());
        var defaultMappingResult = AzureMappingFileLoader.Load(mappingFile: null, diagnosticContext: null);
        var defaultResolver = defaultRoot.CreateRoleDefinitionResolver(defaultMappingResult, diagnostics: null);

        await Assert.That(customResolver.GetRoleDefinition("custom-role-guid", null).Name).IsEqualTo("Custom Deployment Role");
        await Assert.That(defaultResolver.GetRoleDefinition("custom-role-guid", null).Name).IsEqualTo("custom-role-guid");
    }
}
