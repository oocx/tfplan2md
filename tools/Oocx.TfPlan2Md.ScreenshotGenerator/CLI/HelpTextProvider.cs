namespace Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

/// <summary>
/// Provides CLI help text for the screenshot generator tool.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal static class HelpTextProvider
{
    /// <summary>
    /// Generates help text that describes usage and available options.
    /// </summary>
    /// <returns>A formatted help string.</returns>
    public static string GetHelpText()
    {
        return """
        tfplan2md-screenshot - Capture screenshots from HTML reports

        Usage:
            tfplan2md-screenshot --input <file> [options]

        Options:
            -i, --input <file>       Path to input HTML file (required)
            -o, --output <file>      Path to output image file (derived when omitted)
            -w, --width <px>         Viewport width (default: 1920)
            -h, --height <px>        Viewport height (default: 1080)
            -f, --full-page          Capture full scrollable page (default: false)
            --target-terraform-resource-id <address>
                                     Capture only the resource block matching the Terraform address
            --target-selector <selector>
                                     Capture only elements matching a selector (Playwright syntax)
            --format <png|jpeg>      Image format (default: png)
            -q, --quality <0-100>    Quality for JPEG (default: 90)
            --help                   Show help information
            --version                Show version information

        Examples:
            tfplan2md-screenshot --input report.html
            tfplan2md-screenshot --input report.html --output report.png --full-page
            tfplan2md-screenshot --input report.html --output mobile.png --width 375 --height 667
            tfplan2md-screenshot --input report.html --output report.jpg --format jpeg --quality 80
            tfplan2md-screenshot --input report.html --target-terraform-resource-id "azurerm_firewall.example"
            tfplan2md-screenshot --input report.html --target-selector "summary:has-text('azurerm_firewall')"
        """;
    }
}
