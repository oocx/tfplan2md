using System.Text;
using Oocx.TfPlan2Md.HtmlRenderer;

namespace Oocx.TfPlan2Md.HtmlRenderer.Tests;

/// <summary>
/// Verifies the initial behavior of the HTML renderer console application.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
public sealed class HtmlRendererAppTests
{
    /// <summary>
    /// Provides reusable arguments for help-mode invocations to satisfy analyzer CA1861.
    /// </summary>
    private static readonly string[] HelpArguments = ["--help"];

    /// <summary>
    /// Ensures the application renders help text when requested.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test execution.</returns>
    [Fact]
    public async Task RunAsync_WithHelp_PrintsUsage()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new HtmlRendererApp(output, error);

        var exitCode = await app.RunAsync(HelpArguments);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrEmpty(error.ToString()), "Help mode should not produce errors.");
    }
}
