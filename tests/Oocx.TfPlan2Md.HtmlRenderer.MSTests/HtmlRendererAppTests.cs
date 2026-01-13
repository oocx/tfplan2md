using System.Globalization;
using System.Text;
using Oocx.TfPlan2Md.HtmlRenderer;

namespace Oocx.TfPlan2Md.HtmlRenderer.Tests;

/// <summary>
/// Verifies the initial behavior of the HTML renderer console application.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
[TestClass]
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
    [TestMethod]
    public async Task RunAsync_WithHelp_PrintsUsage()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new HtmlRendererApp(output, error);

        var exitCode = await app.RunAsync(HelpArguments);

        Assert.AreEqual(0, exitCode);
        Assert.Contains("Usage", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.IsTrue(string.IsNullOrEmpty(error.ToString()), "Help mode should not produce errors.");
    }

    /// <summary>
    /// Ensures the application applies a wrapper template when provided and writes combined output.
    /// Related acceptance: TC-10.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test execution.</returns>
    [TestMethod]
    public async Task RunAsync_WithTemplate_WritesWrappedHtml()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "app-template-tests", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        var inputPath = Path.Combine(root, "plan.md");
        var templatePath = Path.Combine(root, "wrapper.html");
        await File.WriteAllTextAsync(inputPath, "# Title");
        await File.WriteAllTextAsync(templatePath, "<html><body>{{content}}</body></html>");

        var app = new HtmlRendererApp(new StringWriter(new StringBuilder()), new StringWriter(new StringBuilder()));
        var outputPath = Path.Combine(root, "out.html");

        var exitCode = await app.RunAsync(new[] { "--input", inputPath, "--flavor", "github", "--template", templatePath, "--output", outputPath });

        var rendered = await File.ReadAllTextAsync(outputPath);
        Assert.AreEqual(0, exitCode);
        Assert.Contains("<html>", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<body>", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Title", rendered, StringComparison.OrdinalIgnoreCase);

        Directory.Delete(root, true);
    }

    /// <summary>
    /// Ensures the application fails when the provided template does not contain the required placeholder.
    /// Related acceptance: TC-11.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test execution.</returns>
    [TestMethod]
    public async Task RunAsync_TemplateMissingPlaceholder_ReturnsError()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "app-template-tests", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        var inputPath = Path.Combine(root, "plan.md");
        var templatePath = Path.Combine(root, "wrapper.html");
        await File.WriteAllTextAsync(inputPath, "Content");
        await File.WriteAllTextAsync(templatePath, "<html><body>No slot</body></html>");

        var errorWriter = new StringWriter(new StringBuilder());
        var app = new HtmlRendererApp(new StringWriter(new StringBuilder()), errorWriter);

        var exitCode = await app.RunAsync(new[] { "--input", inputPath, "--flavor", "github", "--template", templatePath });

        Assert.AreEqual(1, exitCode);
        Assert.Contains("placeholder", errorWriter.ToString(), StringComparison.OrdinalIgnoreCase);

        Directory.Delete(root, true);
    }
}
