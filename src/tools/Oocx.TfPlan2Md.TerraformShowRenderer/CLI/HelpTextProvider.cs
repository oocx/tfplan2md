using System.Text;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.CLI;

/// <summary>
/// Provides usage text for the Terraform show approximation CLI.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
/// </summary>
internal static class HelpTextProvider
{
    /// <summary>
    /// Produces formatted usage and option descriptions.
    /// </summary>
    /// <returns>A formatted help text string.</returns>
    public static string GetHelpText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Usage: tfplan2md-terraform-show [options]");
        builder.AppendLine();
        builder.AppendLine("Options:");
        builder.AppendLine("  -i, --input <file>   Path to the Terraform plan JSON file (required)");
        builder.AppendLine("  -o, --output <file>  Output file path (defaults to stdout when omitted)");
        builder.AppendLine("      --no-color       Disable ANSI color codes");
        builder.AppendLine("  -h, --help           Show help information");
        builder.AppendLine("  -v, --version        Show tool version");
        return builder.ToString();
    }
}
