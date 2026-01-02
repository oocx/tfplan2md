using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator.Tests;

/// <summary>
/// Validates help text content for the screenshot generator CLI.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
public sealed class HelpTextProviderTests
{
    /// <summary>
    /// Ensures the generated help text contains required options and usage guidance.
    /// Related acceptance: Task 2 help text requirements.
    /// </summary>
    [Fact]
    public void GetHelpText_IncludesCoreOptions()
    {
        var help = HelpTextProvider.GetHelpText();

        Assert.Contains("Usage", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--input", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--output", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--width", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--height", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--full-page", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--format", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--quality", help, StringComparison.OrdinalIgnoreCase);
    }
}
