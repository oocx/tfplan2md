namespace Oocx.TfPlan2Md.HtmlRenderer.CLI;

/// <summary>
/// Provides usage information for the HTML renderer CLI.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
internal static class HelpTextProvider
{
    /// <summary>
    /// Builds the help text string for the CLI.
    /// </summary>
    /// <returns>A formatted help text block.</returns>
    // SonarAnalyzer S3400: Method returning constant is intentional
    // Justification: Keeping help text in a method (not const field) improves readability and follows CLI tool conventions
#pragma warning disable S3400 // Methods should not return constants
    public static string GetHelpText()
#pragma warning restore S3400
    {
        return """
        tfplan2md-html - Convert tfplan2md markdown to HTML

        Usage:
          tfplan2md-html --input <file> --flavor <github|azdo> [options]

        Options:
          -i, --input <file>        Path to the input markdown file (required)
          -o, --output <file>       Output HTML file path (derived if omitted)
          -f, --flavor <github|azdo>Target HTML flavor to approximate (required)
          -t, --template <file>     Wrapper template file containing {{content}} placeholder
          -h, --help                Show this help text
          -v, --version             Show version information
        """;
    }
}
