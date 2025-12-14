using System.Reflection;
using Oocx.TfPlan2Md.CLI;
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

    // Build the report model
    var modelBuilder = new ReportModelBuilder(options.ShowSensitive);
    var model = modelBuilder.Build(plan);

    // Render to Markdown
    var renderer = new MarkdownRenderer();
    string markdown;
    if (options.TemplatePath is not null)
    {
        if (!File.Exists(options.TemplatePath))
        {
            await Console.Error.WriteLineAsync($"Error: Template file not found: {options.TemplatePath}");
            return 1;
        }
        markdown = await renderer.RenderAsync(model, options.TemplatePath);
    }
    else
    {
        markdown = renderer.Render(model);
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
    Console.WriteLine("""
        tfplan2md - Convert Terraform plan JSON to Markdown
        
        Usage:
          tfplan2md [options] [input-file]
          terraform show -json plan.tfplan | tfplan2md
        
        Arguments:
          input-file           Path to the Terraform plan JSON file.
                               If omitted, reads from stdin.
        
        Options:
          -o, --output <file>  Write output to a file instead of stdout.
          -t, --template <file> Use a custom Scriban template file.
          --show-sensitive     Show sensitive values unmasked.
          -h, --help           Display this help message.
          -v, --version        Display version information.
        
        Examples:
          # From stdin
          terraform show -json plan.tfplan | tfplan2md
        
          # From file
          tfplan2md plan.json
        
          # With output file and custom template
          tfplan2md plan.json --output plan.md --template my-template.sbn
        """);
}

static void PrintVersion()
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "0.0.0";
    Console.WriteLine($"tfplan2md {version}");
}

