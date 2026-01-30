// Triggering release after CI fix
// Baseline suppression for code-quality metrics rollout.
// Related feature: docs/features/046-code-quality-metrics-enforcement/.
#pragma warning disable CA1506

using System.Reflection;
using Oocx.TfPlan2Md.CLI;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration;
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
        string json;
        if (options.InputFile is not null)
        {
            if (!File.Exists(options.InputFile))
            {
                await Console.Error.WriteLineAsync($"Error: Input file not found: {options.InputFile}");
                return 1;
            }
            json = await File.ReadAllTextAsync(options.InputFile);
        }
        else
        {
            // Read from stdin
            using var reader = new StreamReader(Console.OpenStandardInput());
            json = await reader.ReadToEndAsync();
        }

        // Parse the Terraform plan
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(json);

        CodeAnalysisInput? codeAnalysisInput = null;
        if (options.CodeAnalysisResultsPatterns.Count > 0)
        {
            var loader = new CodeAnalysisLoader(new SarifParser());
            var loadResult = loader.Load(options.CodeAnalysisResultsPatterns);
            var minimumLevel = CodeAnalysisSeverityParser.ParseOptional(options.CodeAnalysisMinimumLevel);
            var failOnLevel = CodeAnalysisSeverityParser.ParseOptional(options.FailOnStaticCodeAnalysisErrorsLevel);

            codeAnalysisInput = new CodeAnalysisInput
            {
                Model = loadResult.Model,
                Warnings = loadResult.Warnings,
                MinimumLevel = minimumLevel,
                FailOnLevel = failOnLevel
            };
        }

        // Create principal mapper for resolving principal names in role assignments
        var principalMapper = new PrincipalMapper(options.PrincipalMappingFile, diagnosticContext);

        // Create and configure provider registry
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzApiModule());
        providerRegistry.RegisterProvider(new AzureADModule());
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(options.RenderTarget),
            principalMapper: principalMapper));
        providerRegistry.RegisterProvider(new AzureDevOpsModule(
            largeValueFormat: ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(options.RenderTarget)));

        // Build the report model
        var modelBuilder = new ReportModelBuilder(
            showSensitive: options.ShowSensitive,
            showUnchangedValues: options.ShowUnchangedValues,
            renderTarget: options.RenderTarget,
            reportTitle: options.ReportTitle,
            principalMapper: principalMapper,
            hideMetadata: options.HideMetadata,
            providerRegistry: providerRegistry,
            codeAnalysisInput: codeAnalysisInput);
        var model = modelBuilder.Build(plan);

        // Render to Markdown
        var renderer = new MarkdownRenderer(principalMapper, diagnosticContext, providerRegistry);
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

        if (codeAnalysisInput?.FailOnLevel is not null)
        {
            var failureCount = CodeAnalysisFailureEvaluator.CountFindingsAtOrAbove(
                codeAnalysisInput.Model,
                codeAnalysisInput.FailOnLevel.Value);
            if (failureCount > 0)
            {
                var severityLabel = CodeAnalysisFailureEvaluator.FormatSeverityLabel(codeAnalysisInput.FailOnLevel.Value);
                Console.Error.WriteLine(
                    $"Static code analysis found {failureCount} {severityLabel} or higher findings");
                Console.Error.Flush();
                return 10;
            }
        }

        return 0;
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
}

#pragma warning restore CA1506
