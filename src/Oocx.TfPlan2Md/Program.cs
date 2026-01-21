// Triggering release after CI fix
using System.Reflection;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.CLI;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

var options = ParseArguments(args);
if (options is null)
{
    return 1;
}

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

try
{
    var result = await RunAsync(options);
    return result;
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

static CliOptions? ParseArguments(string[] args)
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

static async Task<int> RunAsync(CliOptions options)
{
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

    // Create principal mapper for resolving principal names in role assignments
    var principalMapper = new PrincipalMapper(options.PrincipalMappingFile, diagnosticContext);

    // Build the report model
    var modelBuilder = new ReportModelBuilder(
        showSensitive: options.ShowSensitive,
        showUnchangedValues: options.ShowUnchangedValues,
        largeValueFormat: options.LargeValueFormat,
        reportTitle: options.ReportTitle,
        principalMapper: principalMapper,
        hideMetadata: options.HideMetadata);
    var model = modelBuilder.Build(plan);

    // Render to Markdown
    var renderer = new MarkdownRenderer(principalMapper, diagnosticContext);
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

    return 0;
}

static void PrintHelp()
{
    Console.WriteLine(HelpTextProvider.GetHelpText());
}

static void PrintVersion()
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "0.0.0";
    Console.WriteLine($"tfplan2md {version}");
}
