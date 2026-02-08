// Triggering release after CI fix
// Baseline suppression for code-quality metrics rollout.
// Related feature: docs/features/046-code-quality-metrics-enforcement/.
#pragma warning disable CA1506

using System.Reflection;
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
/// Executes the tfplan2md CLI workflow using explicit entry point helpers.
/// </summary>
internal static class ProgramEntry
{
    /// <summary>
    /// Executes the CLI entry point and returns the exit code.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The exit code for the process.</returns>
    internal static async Task<int> RunAsync(string[] args)
    {
        var options = ParseArguments(args);
        if (options is null)
        {
            return 1;
        }

        return await RunWithErrorHandlingAsync(options);
    }

    /// <summary>
    /// Parses command-line arguments with error handling.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <returns>The parsed options or <c>null</c> when parsing fails.</returns>
    private static CliOptions? ParseArguments(string[] args)
    {
        try
        {
            return CliParser.Parse(args);
        }
        catch (CliParseException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine("Use --help for usage information.");
            return null;
        }
    }

    /// <summary>
    /// Executes the CLI workflow while handling known exceptions.
    /// </summary>
    /// <param name="options">The parsed CLI options.</param>
    /// <returns>The exit code produced by the workflow.</returns>
    private static async Task<int> RunWithErrorHandlingAsync(CliOptions options)
    {
        try
        {
            return await RunWorkflowAsync(options);
        }
        catch (TerraformPlanParseException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }
        catch (MarkdownRenderException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Unexpected error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Runs the main report generation workflow.
    /// </summary>
    /// <param name="options">The parsed CLI options.</param>
    /// <returns>The exit code for the workflow.</returns>
    private static async Task<int> RunWorkflowAsync(CliOptions options)
    {
        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (options.ShowVersion)
        {
            PrintVersion();
            return 0;
        }

        // Create diagnostic context if debug mode is enabled
        var diagnosticContext = options.Debug ? new DiagnosticContext() : null;

        // Read input
        var json = await ReadInputAsync(options);
        if (json is null)
        {
            return 1;
        }

        // Parse the Terraform plan
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(json);

        var codeAnalysisInput = CreateCodeAnalysisInput(options);

        // Load Azure mapping file once and create principal mapper for role assignment resolution
        var mappingResult = AzureMappingFileLoader.Load(options.PrincipalMappingFile, diagnosticContext);
        AzureRoleDefinitionMapper.MergeCustomRoles(mappingResult.Roles, diagnosticContext);
        var principalMapper = new PrincipalMapper(mappingResult.Principals, mappingResult.PrincipalTypes, diagnosticContext);
        var entityMapper = new AzureEntityMapper(
            mappingResult.Subscriptions,
            mappingResult.ManagementGroups,
            mappingResult.Tenants,
            diagnosticContext);
        var scopeFormatter = new EnrichedAzureScopeFormatter(entityMapper);

        // Create and configure provider registry
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzApiModule(scopeFormatter));
        providerRegistry.RegisterProvider(new AzureADModule());
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(options.RenderTarget),
            principalMapper: principalMapper,
            scopeFormatter: scopeFormatter));
        providerRegistry.RegisterProvider(new AzureDevOpsModule(
            largeValueFormat: ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(options.RenderTarget)));

        var valueFormatterRegistry = new ValueFormatterRegistry();
        providerRegistry.RegisterAllValueFormatters(valueFormatterRegistry);

        var iconProviderRegistry = new IconProviderRegistry();
        providerRegistry.RegisterAllIconProviders(iconProviderRegistry);

        // Build the report model
        var modelBuilder = new ReportModelBuilder(
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
        var model = modelBuilder.Build(plan);

        // Render to Markdown
        var renderer = new MarkdownRenderer(principalMapper, diagnosticContext, providerRegistry, valueFormatterRegistry, iconProviderRegistry);
        string markdown;
        if (options.TemplatePath is not null)
        {
            markdown = await renderer.RenderAsync(model, options.TemplatePath);
        }
        else
        {
            markdown = renderer.Render(model);
        }

        // Append debug section if diagnostic context exists
        if (diagnosticContext is not null)
        {
            markdown += "\n\n" + diagnosticContext.GenerateMarkdownSection();
        }

        // Write output
        if (options.OutputFile is not null)
        {
            await File.WriteAllTextAsync(options.OutputFile, markdown);
        }
        else
        {
            Console.WriteLine(markdown);
        }

        if (codeAnalysisInput?.FailOnLevel is not null
            && await HandleCodeAnalysisFailureAsync(codeAnalysisInput))
        {
            return 10;
        }

        return 0;
    }

    /// <summary>
    /// Reads the Terraform plan input from a file or standard input.
    /// </summary>
    /// <param name="options">The parsed CLI options.</param>
    /// <returns>The input content, or <c>null</c> when the input cannot be read.</returns>
    private static async Task<string?> ReadInputAsync(CliOptions options)
    {
        if (options.InputFile is null)
        {
            using var reader = new StreamReader(Console.OpenStandardInput());
            return await reader.ReadToEndAsync();
        }

        if (!File.Exists(options.InputFile))
        {
            await Console.Error.WriteLineAsync($"Error: Input file not found: {options.InputFile}");
            return null;
        }

        return await File.ReadAllTextAsync(options.InputFile);
    }

    /// <summary>
    /// Loads optional code analysis results based on CLI options.
    /// </summary>
    /// <param name="options">The parsed CLI options.</param>
    /// <returns>The code analysis input model when provided; otherwise <c>null</c>.</returns>
    private static CodeAnalysisInput? CreateCodeAnalysisInput(CliOptions options)
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
    /// Writes the CLI help text to stdout.
    /// </summary>
    private static void PrintHelp()
    {
        Console.WriteLine(HelpTextProvider.GetHelpText());
    }

    /// <summary>
    /// Writes the CLI version text to stdout.
    /// </summary>
    private static void PrintVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";
        Console.WriteLine($"tfplan2md {version}");
    }

    /// <summary>
    /// Evaluates code analysis results and writes failure output when thresholds are exceeded.
    /// </summary>
    /// <param name="codeAnalysisInput">The code analysis input model.</param>
    /// <returns>True when a failure should terminate execution; otherwise false.</returns>
    private static async Task<bool> HandleCodeAnalysisFailureAsync(CodeAnalysisInput codeAnalysisInput)
    {
        var failureCount = CodeAnalysisFailureEvaluator.CountFindingsAtOrAbove(
            codeAnalysisInput.Model,
            codeAnalysisInput.FailOnLevel!.Value);
        if (failureCount <= 0)
        {
            return false;
        }

        var severityLabel = CodeAnalysisFailureEvaluator.FormatSeverityLabel(codeAnalysisInput.FailOnLevel.Value);
        await Console.Error.WriteLineAsync(
            $"Static code analysis found {failureCount} {severityLabel} or higher findings");
        await Console.Error.FlushAsync();
        return true;
    }
}

#pragma warning restore CA1506
