// Baseline suppression for code-quality metrics rollout.
// Related feature: docs/features/046-code-quality-metrics-enforcement/.
#pragma warning disable CA1506

using Oocx.TfPlan2Md.CLI;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzApi;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Providers.AzureRM;

namespace Oocx.TfPlan2Md;

/// <summary>
/// Composes application services using Pure Dependency Injection.
/// Centralizes object graph construction and eliminates constructor-based DI container coupling.
/// Related ADR: docs/features/048-formalized-di-approach/adr-006-formalized-pure-di.md.
/// </summary>
internal sealed class CompositionRoot(CliOptions options)
{
    /// <summary>
    /// Creates a diagnostic context when debug mode is enabled.
    /// </summary>
    /// <returns>A diagnostic context when debug is enabled; otherwise <c>null</c>.</returns>
    internal DiagnosticContext? CreateDiagnosticContext()
    {
        return options.Debug ? new DiagnosticContext() : null;
    }

    /// <summary>
    /// Creates the Terraform plan parser.
    /// </summary>
    /// <returns>A new parser instance.</returns>
    internal TerraformPlanParser CreateParser()
    {
        return new TerraformPlanParser();
    }

    /// <summary>
    /// Creates the principal mapper for Azure role assignment resolution.
    /// </summary>
    /// <param name="mappingResult">The Azure mapping data loaded from file.</param>
    /// <param name="diagnostics">Optional diagnostic context for troubleshooting.</param>
    /// <returns>A configured principal mapper instance.</returns>
    internal PrincipalMapper CreatePrincipalMapper(
        AzureMappingFileResult mappingResult,
        DiagnosticContext? diagnostics)
    {
        return new PrincipalMapper(
            mappingResult.Principals,
            mappingResult.PrincipalTypes,
            diagnostics);
    }

    /// <summary>
    /// Creates the Azure entity mapper for subscription, management group, and tenant resolution.
    /// </summary>
    /// <param name="mappingResult">The Azure mapping data loaded from file.</param>
    /// <param name="diagnostics">Optional diagnostic context for troubleshooting.</param>
    /// <returns>A configured entity mapper instance.</returns>
    internal AzureEntityMapper CreateEntityMapper(
        AzureMappingFileResult mappingResult,
        DiagnosticContext? diagnostics)
    {
        return new AzureEntityMapper(
            mappingResult.Subscriptions,
            mappingResult.ManagementGroups,
            mappingResult.Tenants,
            diagnostics);
    }

    /// <summary>
    /// Creates the enriched Azure scope formatter.
    /// </summary>
    /// <param name="entityMapper">The entity mapper for resolving Azure entities.</param>
    /// <returns>A configured scope formatter instance.</returns>
    internal EnrichedAzureScopeFormatter CreateScopeFormatter(AzureEntityMapper entityMapper)
    {
        return new EnrichedAzureScopeFormatter(entityMapper);
    }

    /// <summary>
    /// Creates and configures the provider registry with all supported Terraform providers.
    /// Registers AzApi, AzureAD, AzureRM, and AzureDevOps modules.
    /// </summary>
    /// <param name="principalMapper">The principal mapper for role assignment resolution.</param>
    /// <param name="scopeFormatter">The scope formatter for Azure resource scopes.</param>
    /// <returns>A configured provider registry with all modules registered.</returns>
    internal ProviderRegistry CreateProviderRegistry(
        IPrincipalMapper principalMapper,
        EnrichedAzureScopeFormatter scopeFormatter)
    {
        var registry = new ProviderRegistry();
        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(options.RenderTarget);

        registry.RegisterProvider(new AzApiModule(scopeFormatter));
        registry.RegisterProvider(new AzureADModule());
        registry.RegisterProvider(new AzureRMModule(
            largeValueFormat: largeValueFormat,
            principalMapper: principalMapper,
            scopeFormatter: scopeFormatter));
        registry.RegisterProvider(new AzureDevOpsModule(largeValueFormat: largeValueFormat));

        return registry;
    }

    /// <summary>
    /// Creates the value formatter registry and populates it with all provider formatters.
    /// </summary>
    /// <param name="providerRegistry">The provider registry containing registered modules.</param>
    /// <returns>A configured value formatter registry.</returns>
    internal ValueFormatterRegistry CreateValueFormatterRegistry(ProviderRegistry providerRegistry)
    {
        var registry = new ValueFormatterRegistry();
        providerRegistry.RegisterAllValueFormatters(registry);
        return registry;
    }

    /// <summary>
    /// Creates the icon provider registry and populates it with all provider icon definitions.
    /// </summary>
    /// <param name="providerRegistry">The provider registry containing registered modules.</param>
    /// <returns>A configured icon provider registry.</returns>
    internal IconProviderRegistry CreateIconProviderRegistry(ProviderRegistry providerRegistry)
    {
        var registry = new IconProviderRegistry();
        providerRegistry.RegisterAllIconProviders(registry);
        return registry;
    }

    /// <summary>
    /// Creates the report model builder with all required dependencies.
    /// </summary>
    /// <param name="valueFormatterRegistry">The value formatter registry for resource summaries.</param>
    /// <param name="principalMapper">The principal mapper for role assignment resolution.</param>
    /// <param name="providerRegistry">The provider registry for module-specific formatting.</param>
    /// <param name="codeAnalysisInput">Optional code analysis results to include in the report.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for resource icons.</param>
    /// <returns>A configured report model builder instance.</returns>
    internal ReportModelBuilder CreateReportModelBuilder(
        ValueFormatterRegistry valueFormatterRegistry,
        IPrincipalMapper principalMapper,
        ProviderRegistry providerRegistry,
        CodeAnalysisInput? codeAnalysisInput,
        IconProviderRegistry iconProviderRegistry)
    {
        return new ReportModelBuilder(
            summaryBuilder: new ResourceSummaryBuilder(valueFormatterRegistry),
            showSensitive: options.ShowSensitive,
            showUnchangedValues: options.ShowUnchangedValues,
            renderTarget: options.RenderTarget,
            reportTitle: options.ReportTitle,
            principalMapper: principalMapper,
            hideMetadata: options.HideMetadata,
            providerRegistry: providerRegistry,
            codeAnalysisInput: codeAnalysisInput,
            iconProviderRegistry: iconProviderRegistry);
    }

    /// <summary>
    /// Creates the markdown renderer with all required dependencies.
    /// </summary>
    /// <param name="principalMapper">The principal mapper for role assignment resolution.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for troubleshooting.</param>
    /// <param name="providerRegistry">The provider registry for module-specific rendering.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for value rendering.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for resource icons.</param>
    /// <returns>A configured markdown renderer instance.</returns>
    internal MarkdownRenderer CreateMarkdownRenderer(
        IPrincipalMapper principalMapper,
        DiagnosticContext? diagnosticContext,
        ProviderRegistry providerRegistry,
        ValueFormatterRegistry valueFormatterRegistry,
        IconProviderRegistry iconProviderRegistry)
    {
        return new MarkdownRenderer(
            principalMapper,
            diagnosticContext,
            providerRegistry,
            valueFormatterRegistry,
            iconProviderRegistry);
    }

    /// <summary>
    /// Creates code analysis input from CLI options.
    /// Loads SARIF results and parses severity thresholds.
    /// </summary>
    /// <returns>Code analysis input when SARIF patterns are provided; otherwise <c>null</c>.</returns>
    internal CodeAnalysisInput? CreateCodeAnalysisInput()
    {
        if (options.CodeAnalysisResultsPatterns.Count == 0)
        {
            return null;
        }

        var loader = new CodeAnalysisLoader(new SarifParser());
        var loadResult = loader.Load(options.CodeAnalysisResultsPatterns);
        var minimumLevel = CodeAnalysisSeverityParser.ParseOptional(options.CodeAnalysisMinimumLevel);
        var failOnLevel = CodeAnalysisSeverityParser.ParseOptional(options.FailOnStaticCodeAnalysisErrorsLevel);

        return new CodeAnalysisInput
        {
            Model = loadResult.Model,
            Warnings = loadResult.Warnings,
            MinimumLevel = minimumLevel,
            FailOnLevel = failOnLevel
        };
    }

    /// <summary>
    /// Composes all application services in the correct dependency order.
    /// This is the main entry point for service composition.
    /// </summary>
    /// <returns>A fully composed set of application services ready for use.</returns>
    internal ApplicationServices ComposeServices()
    {
        // Create diagnostic context first (used by many subsequent services)
        var diagnosticContext = CreateDiagnosticContext();

        // Create parser (no dependencies)
        var parser = CreateParser();

        // Create code analysis input (independent of other services)
        var codeAnalysisInput = CreateCodeAnalysisInput();

        // Load Azure mapping file and merge custom roles
        var mappingResult = AzureMappingFileLoader.Load(options.PrincipalMappingFile, diagnosticContext);
        AzureRoleDefinitionMapper.MergeCustomRoles(mappingResult.Roles, diagnosticContext);

        // Create Azure-specific mappers and formatters
        var principalMapper = CreatePrincipalMapper(mappingResult, diagnosticContext);
        var entityMapper = CreateEntityMapper(mappingResult, diagnosticContext);
        var scopeFormatter = CreateScopeFormatter(entityMapper);

        // Create provider registry and dependent registries
        var providerRegistry = CreateProviderRegistry(principalMapper, scopeFormatter);
        var valueFormatterRegistry = CreateValueFormatterRegistry(providerRegistry);
        var iconProviderRegistry = CreateIconProviderRegistry(providerRegistry);

        // Create model builder and renderer
        var modelBuilder = CreateReportModelBuilder(
            valueFormatterRegistry,
            principalMapper,
            providerRegistry,
            codeAnalysisInput,
            iconProviderRegistry);

        var renderer = CreateMarkdownRenderer(
            principalMapper,
            diagnosticContext,
            providerRegistry,
            valueFormatterRegistry,
            iconProviderRegistry);

        return new ApplicationServices(
            Parser: parser,
            ModelBuilder: modelBuilder,
            Renderer: renderer,
            DiagnosticContext: diagnosticContext,
            CodeAnalysisInput: codeAnalysisInput);
    }
}

/// <summary>
/// Contains the fully composed application services ready for workflow execution.
/// Returned by <see cref="CompositionRoot.ComposeServices"/>.
/// </summary>
internal sealed record ApplicationServices(
    TerraformPlanParser Parser,
    ReportModelBuilder ModelBuilder,
    MarkdownRenderer Renderer,
    DiagnosticContext? DiagnosticContext,
    CodeAnalysisInput? CodeAnalysisInput);

#pragma warning restore CA1506
