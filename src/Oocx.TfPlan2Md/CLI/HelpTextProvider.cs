using System.Text;

namespace Oocx.TfPlan2Md.CLI;

public static class HelpTextProvider
{
    private const int OptionPadding = 50;

    public static string GetHelpText()
    {
        var options = new (string Option, string Description)[]
        {
            ("-o, --output <file>", "Write output to a file instead of stdout."),
            ("-t, --template <name|file>", "Use a built-in template by name or a custom Scriban template file."),
            ("--report-title <title>", "Override the report title (level-1 heading) with a custom value."),
            ("-p, --principal-mapping <file>", "Map principal IDs to names using a JSON file."),
            ("--large-value-format <inline-diff|simple-diff>", "Controls rendering of large attribute values."),
            ("--show-unchanged-values", "Include unchanged attribute values in tables."),
            ("--hide-metadata", "Hide tfplan2md version/commit/timestamp metadata in the header."),
            ("--show-sensitive", "Show sensitive values unmasked."),
            ("--debug", "Append diagnostic information to the report."),
            ("-h, --help", "Display this help message."),
            ("-v, --version", "Display version information."),
        };

        var examples = new[]
        {
            "# From stdin",
            "terraform show -json plan.tfplan | tfplan2md",
            string.Empty,
            "# From file",
            "tfplan2md plan.json",
            string.Empty,
            "# With principal mapping",
            "tfplan2md --principal-mapping principals.json plan.json",
            string.Empty,
            "# With custom report title",
            "tfplan2md plan.json --report-title \"Drift Detection - repo\"",
            string.Empty,
            "# GitHub-friendly large value diff",
            "tfplan2md plan.json --large-value-format simple-diff",
            string.Empty,
            "# With output file and custom template",
            "tfplan2md plan.json --output plan.md --template my-template.sbn"
        };

        var sb = new StringBuilder();
        sb.AppendLine("tfplan2md - Convert Terraform plan JSON to Markdown");
        sb.AppendLine();
        sb.AppendLine("Usage:");
        sb.AppendLine("  tfplan2md [options] [input-file]");
        sb.AppendLine("  terraform show -json plan.tfplan | tfplan2md");
        sb.AppendLine();
        sb.AppendLine("Arguments:");
        sb.AppendLine("  input-file           Path to the Terraform plan JSON file.");
        sb.AppendLine("                       If omitted, reads from stdin.");
        sb.AppendLine();
        sb.AppendLine("Options:");
        foreach (var (option, description) in options)
        {
            sb.Append("  ");
            sb.Append(option.PadRight(OptionPadding));
            sb.AppendLine(description);
        }

        sb.AppendLine();
        sb.AppendLine("Built-in templates:");
        sb.AppendLine("  default  Full report with resource changes (default)");
        sb.AppendLine("  summary  Compact summary with counts and resource type breakdown");
        sb.AppendLine();
        sb.AppendLine("Examples:");
        foreach (var example in examples)
        {
            sb.AppendLine(example);
        }

        return sb.ToString();
    }
}
