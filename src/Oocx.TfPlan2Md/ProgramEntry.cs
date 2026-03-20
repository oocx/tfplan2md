using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.CLI;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.RenderTargets;
using Oocx.TfPlan2Md.RenderTargets.Bitbucket;


namespace Oocx.TfPlan2Md;

/// <summary>
/// Executes the tfplan2md CLI workflow using explicit entry point helpers.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Entry point orchestrates CLI, parsing, markdown generation, and rendering — coupling is intentional.")]
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

        // Compose application services using Pure DI
        var compositionRoot = new CompositionRoot(options);
        var services = compositionRoot.ComposeServices();

        // Read input
        var json = await ReadInputAsync(options);
        if (json is null)
        {
            return 1;
        }

        // Parse the Terraform plan
        var plan = services.Parser.Parse(json);

        // Build the report model
        var model = services.ModelBuilder.Build(plan);

        // Render to Markdown
        string markdown;
        if (options.TemplatePath is not null)
        {
            markdown = await services.Renderer.RenderAsync(model, options.TemplatePath);
        }
        else
        {
            markdown = services.Renderer.Render(model);
        }

        // Append debug section if diagnostic context exists
        if (services.DiagnosticContext is not null)
        {
            markdown += "\n\n" + DiagnosticMarkdownFormatter.Format(services.DiagnosticContext.CreateSnapshot());
        }

        if (options.RenderTarget == RenderTarget.Bitbucket)
        {
            markdown = BitbucketMarkdownPostProcessor.Process(markdown);
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

        // Emit Azure DevOps pipeline variable for downstream steps (only when writing to file).
        // Related feature: docs/features/109-azdo-has-changes-variable/analysis.md.
        if (options.OutputFile is not null && options.RenderTarget == RenderTarget.AzureDevOps)
        {
            var hasChangesValue = model.Summary.Total - model.FilteredResourceCount > 0 ? "true" : "false";
            Console.WriteLine($"##vso[task.setvariable variable=tfplan2md_haschanges]{hasChangesValue}");
        }

        // Handle code analysis failure threshold
        if (services.CodeAnalysisInput?.FailOnLevel is not null
            && await HandleCodeAnalysisFailureAsync(services.CodeAnalysisInput))
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
        Console.WriteLine($"tfplan2md {BuildInfo.InformationalVersion}");
    }

    /// <summary>
    /// Evaluates code analysis results and writes failure output when thresholds are exceeded.
    /// </summary>
    /// <param name="codeAnalysisInput">The code analysis input model.</param>
    /// <returns>True when a failure should terminate execution; otherwise false.</returns>
    private static async Task<bool> HandleCodeAnalysisFailureAsync(CodeAnalysisInput codeAnalysisInput)
    {
        if (codeAnalysisInput.Warnings.Count > 0)
        {
            await Console.Error.WriteLineAsync("Static code analysis input warnings detected:");
            foreach (var warning in codeAnalysisInput.Warnings)
            {
                await Console.Error.WriteLineAsync($"- {warning.FilePath}: {warning.Message}");
            }

            await Console.Error.FlushAsync();
            return true;
        }

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
