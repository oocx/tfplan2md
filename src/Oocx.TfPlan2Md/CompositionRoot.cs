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
/// Related ADR: docs/adr-006-dependency-injection.md.
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
    /// Creates the Azure DevOps user mapper for user ID resolution.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </summary>
    /// <param name="mappingResult">The Azure mapping data loaded from file.</param>
    /// <param name="diagnostics">Optional diagnostic context for troubleshooting.</param>
    /// <returns>A configured Azure DevOps user mapper instance.</returns>
    internal AzdoUserMapper CreateAzdoUserMapper(
        AzureMappingFileResult mappingResult,
        DiagnosticContext? diagnostics)
    {
        return new AzdoUserMapper(mappingResult.AzdoUsers, diagnostics);
    }

    /// <summary>
    /// Creates the Azure DevOps group mapper for group descriptor resolution.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </summary>
    /// <param name="mappingResult">The Azure mapping data loaded from file.</param>
    /// <param name="diagnostics">Optional diagnostic context for troubleshooting.</param>
    /// <returns>A configured Azure DevOps group mapper instance.</returns>
    internal AzdoGroupMapper CreateAzdoGroupMapper(
        AzureMappingFileResult mappingResult,
        DiagnosticContext? diagnostics)
    {
        return new AzdoGroupMapper(mappingResult.AzdoGroups, diagnostics);
    }

    /// <summary>
    /// Creates the Azure DevOps project mapper for project ID resolution.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </summary>
    /// <param name="mappingResult">The Azure mapping data loaded from file.</param>
    /// <param name="diagnostics">Optional diagnostic context for troubleshooting.</param>
    /// <returns>A configured Azure DevOps project mapper instance.</returns>
    internal AzdoProjectMapper CreateAzdoProjectMapper(
        AzureMappingFileResult mappingResult,
        DiagnosticContext? diagnostics)
    {
        return new AzdoProjectMapper(mappingResult.AzdoProjects, diagnostics);
    }

    /// <summary>
    /// Creates the Azure DevOps repository mapper for repository ID resolution.
    /// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
    /// </summary>
    /// <param name="mappingResult">The Azure mapping data loaded from file.</param>
    /// <param name="diagnostics">Optional diagnostic context for troubleshooting.</param>
    /// <returns>A configured Azure DevOps repository mapper instance.</returns>
    internal AzdoRepositoryMapper CreateAzdoRepositoryMapper(
        AzureMappingFileResult mappingResult,
        DiagnosticContext? diagnostics)
    {
        return new AzdoRepositoryMapper(mappingResult.AzdoRepositories, diagnostics);
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
    /// Creates the run-scoped Azure role definition resolver.
    /// </summary>
    /// <param name="mappingResult">The Azure mapping data loaded from file.</param>
    /// <param name="diagnostics">Optional diagnostic context for failed role resolution tracking.</param>
    /// <returns>A configured role definition resolver instance.</returns>
    internal IRoleDefinitionResolver CreateRoleDefinitionResolver(
        AzureMappingFileResult mappingResult,
        DiagnosticContext? diagnostics)
    {
        return new AzureRoleDefinitionResolver(mappingResult.Roles, diagnostics);
    }

    /// <summary>
    /// Creates the Microsoft Graph app role resolver for app role assignment resolution.
    /// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
    /// </summary>
    /// <returns>A resolver for Microsoft Graph app role GUIDs.</returns>
    internal IAppRoleResolver CreateAppRoleResolver()
    {
        return MicrosoftGraphAppRoleResolver.CreateBuiltIn();
    }

    /// <summary>
    /// Creates and configures the provider registry with all supported Terraform providers.
    /// Registers AzApi, AzureAD, AzureRM, and AzureDevOps modules.
    /// </summary>
    /// <param name="principalMapper">The principal mapper for role assignment resolution.</param>
    /// <param name="scopeFormatter">The scope formatter for Azure resource scopes.</param>
    /// <param name="entityMapper">The mapper for tenant and management group display names.</param>
    /// <param name="roleDefinitionResolver">The resolver for Azure role definition display names.</param>
    /// <param name="azdoUserMapper">The mapper for Azure DevOps user display names.</param>
    /// <param name="azdoGroupMapper">The mapper for Azure DevOps group display names.</param>
    /// <param name="azdoProjectMapper">The mapper for Azure DevOps project display names.</param>
    /// <param name="azdoRepositoryMapper">The mapper for Azure DevOps repository display names.</param>
    /// <param name="appRoleResolver">The resolver for Microsoft Graph app role GUIDs.</param>
    /// <returns>A configured provider registry with all modules registered.</returns>
    internal ProviderRegistry CreateProviderRegistry(
        IPrincipalMapper principalMapper,
        EnrichedAzureScopeFormatter scopeFormatter,
        AzureEntityMapper entityMapper,
        IRoleDefinitionResolver roleDefinitionResolver,
        AzdoUserMapper azdoUserMapper,
        AzdoGroupMapper azdoGroupMapper,
        AzdoProjectMapper azdoProjectMapper,
        AzdoRepositoryMapper azdoRepositoryMapper,
        IAppRoleResolver appRoleResolver)
    {
        var registry = new ProviderRegistry();
        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(options.RenderTarget);

        registry.RegisterProvider(new AzApiModule(scopeFormatter, entityMapper));
        registry.RegisterProvider(new AzureADModule(entityMapper, principalMapper, appRoleResolver));
        registry.RegisterProvider(new AzureRMModule(
            largeValueFormat: largeValueFormat,
            principalMapper: principalMapper,
            scopeFormatter: scopeFormatter,
            entityMapper: entityMapper,
            roleDefinitionResolver: roleDefinitionResolver));
        registry.RegisterProvider(new AzureDevOpsModule(
            entityMapper: entityMapper,
            azdoUserMapper: azdoUserMapper,
            azdoGroupMapper: azdoGroupMapper,
            azdoProjectMapper: azdoProjectMapper,
            azdoRepositoryMapper: azdoRepositoryMapper));

        return registry;
    }

    /// <summary>
    /// Creates the centralized provider contribution set used by composition code.
    /// </summary>
    /// <param name="providerRegistry">The provider registry containing registered providers.</param>
    /// <returns>A contribution set that can build provider-specific registries on demand.</returns>
    internal ProviderContributionSet CreateProviderContributionSet(ProviderRegistry providerRegistry)
    {
        return providerRegistry.CreateContributionSet();
    }

    /// <summary>
    /// Creates the report model builder with all required dependencies.
    /// </summary>
    /// <param name="valueFormatterRegistry">The value formatter registry for resource summaries.</param>
    /// <param name="principalMapper">The principal mapper for role assignment resolution.</param>
    /// <param name="providerRegistry">The provider registry for module-specific formatting.</param>
    /// <param name="providerContributionSet">The centralized provider contribution set.</param>
    /// <param name="codeAnalysisInput">Optional code analysis results to include in the report.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for resource icons.</param>
    /// <returns>A configured report model builder instance.</returns>
    internal ReportModelBuilder CreateReportModelBuilder(
        ValueFormatterRegistry valueFormatterRegistry,
        IPrincipalMapper principalMapper,
        ProviderRegistry providerRegistry,
        ProviderContributionSet providerContributionSet,
        CodeAnalysisInput? codeAnalysisInput,
        IconProviderRegistry iconProviderRegistry)
    {
        return new ReportModelBuilder(
            options: new ReportModelBuilderOptions(
                ShowSensitive: options.ShowSensitive,
                ShowUnchangedValues: options.ShowUnchangedValues,
                RenderTarget: options.RenderTarget,
                ReportTitle: options.ReportTitle,
                HideMetadata: options.HideMetadata,
                DetailsDisplayMode: options.DetailsDisplayMode,
                IgnoreAzureIdCaseChanges: options.IgnoreAzureIdCaseChanges),
            services: new ReportModelBuilderServices(
                SummaryBuilder: new ResourceSummaryBuilder(valueFormatterRegistry),
                PrincipalMapper: principalMapper,
                ProviderRegistry: providerRegistry,
                ProviderContributions: providerContributionSet,
                CodeAnalysisInput: codeAnalysisInput,
                IconProviderRegistry: iconProviderRegistry,
                AttributeChangeFilterRegistry: providerContributionSet.CreateAttributeChangeFilterRegistry()));
    }

    /// <summary>
    /// Creates the markdown renderer with all required dependencies.
    /// </summary>
    /// <param name="diagnosticContext">Optional diagnostic context for troubleshooting.</param>
    /// <param name="providerRegistry">The provider registry for module-specific rendering.</param>
    /// <param name="providerContributionSet">The centralized provider contribution set.</param>
    /// <param name="valueFormatterRegistry">The value formatter registry for value rendering.</param>
    /// <param name="iconProviderRegistry">The icon provider registry for resource icons.</param>
    /// <returns>A configured markdown renderer instance.</returns>
    internal MarkdownRenderer CreateMarkdownRenderer(
        DiagnosticContext? diagnosticContext,
        ProviderRegistry providerRegistry,
        ProviderContributionSet providerContributionSet,
        ValueFormatterRegistry valueFormatterRegistry,
        IconProviderRegistry iconProviderRegistry)
    {
        return new MarkdownRenderer(
            diagnosticContext,
            providerRegistry,
            providerContributions: providerContributionSet,
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

        // Load Azure mapping file and build run-scoped Azure lookup services
        var mappingResult = AzureMappingFileLoader.Load(options.PrincipalMappingFile, diagnosticContext);
        var roleDefinitionResolver = CreateRoleDefinitionResolver(mappingResult, diagnosticContext);

        // Create app role resolver for Microsoft Graph permissions
        var appRoleResolver = CreateAppRoleResolver();

        // Create Azure-specific mappers and formatters
        var principalMapper = CreatePrincipalMapper(mappingResult, diagnosticContext);
        var entityMapper = CreateEntityMapper(mappingResult, diagnosticContext);
        var scopeFormatter = CreateScopeFormatter(entityMapper);

        // Create Azure DevOps-specific mappers
        var azdoUserMapper = CreateAzdoUserMapper(mappingResult, diagnosticContext);
        var azdoGroupMapper = CreateAzdoGroupMapper(mappingResult, diagnosticContext);
        var azdoProjectMapper = CreateAzdoProjectMapper(mappingResult, diagnosticContext);
        var azdoRepositoryMapper = CreateAzdoRepositoryMapper(mappingResult, diagnosticContext);

        // Create provider registry and dependent registries
        var providerRegistry = CreateProviderRegistry(
            principalMapper,
            scopeFormatter,
            entityMapper,
            roleDefinitionResolver,
            azdoUserMapper,
            azdoGroupMapper,
            azdoProjectMapper,
            azdoRepositoryMapper,
            appRoleResolver);
        var providerContributionSet = CreateProviderContributionSet(providerRegistry);
        var valueFormatterRegistry = providerContributionSet.CreateValueFormatterRegistry();
        var iconProviderRegistry = providerContributionSet.CreateIconProviderRegistry();

        // Create model builder and renderer
        var modelBuilder = CreateReportModelBuilder(
            valueFormatterRegistry,
            principalMapper,
            providerRegistry,
            providerContributionSet,
            codeAnalysisInput,
            iconProviderRegistry);

        var renderer = CreateMarkdownRenderer(
            diagnosticContext,
            providerRegistry,
            providerContributionSet,
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
