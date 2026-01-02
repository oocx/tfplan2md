using System.Globalization;
using Microsoft.Playwright;
using Oocx.TfPlan2Md.ScreenshotGenerator;
using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator.Tests;

/// <summary>
/// Verifies application-level orchestration behaviors (output derivation, format detection, validation plumbing).
/// Related acceptance: TC-02, TC-05, TC-16.
/// </summary>
public sealed class AppOrchestrationTests
{
    [SkippableFact]
    public async Task RunAsync_WithDerivedOutput_UsesPngByDefault()
    {
        using var context = await CreateTempHtmlAsync("<html></html>");
        await EnsureBrowserAvailableAsync();
        var app = CreateApp(out _, out var error);

        var exitCode = await app.RunAsync(new[] { "--input", context.InputPath });

        SkipIfPlaywrightUnavailable(exitCode, error);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(context.ExpectedOutputPath));
        Assert.True(string.IsNullOrEmpty(error.ToString()), error.ToString());
    }

    [SkippableFact]
    public async Task RunAsync_WithJpegExtension_DetectsFormat()
    {
        using var context = await CreateTempHtmlAsync("<html></html>");
        await EnsureBrowserAvailableAsync();
        var app = CreateApp(out _, out var error);
        var outputPath = Path.Combine(Path.GetDirectoryName(context.InputPath)!, "out.jpg");

        var exitCode = await app.RunAsync(new[] { "--input", context.InputPath, "--output", outputPath });

        SkipIfPlaywrightUnavailable(exitCode, error);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputPath));
        Assert.True(string.IsNullOrEmpty(error.ToString()), error.ToString());
    }

    [Fact]
    public async Task RunAsync_MissingInput_ReturnsError()
    {
        var missing = Path.Combine(AppContext.BaseDirectory, "missing.html");
        var app = CreateApp(out _, out var error);

        var exitCode = await app.RunAsync(new[] { "--input", missing });

        Assert.Equal(1, exitCode);
        Assert.Contains("not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnsureBrowserAvailableAsync()
    {
        try
        {
            using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }).ConfigureAwait(false);
        }
        catch (PlaywrightException ex)
        {
            throw new Xunit.SkipException($"Skipping Playwright tests: {ex.Message}");
        }
    }

    private static void SkipIfPlaywrightUnavailable(int exitCode, StringWriter error)
    {
        var message = error.ToString();
        if (exitCode != 0 && message.Contains("Chromium is not available", StringComparison.OrdinalIgnoreCase))
        {
            throw new Xunit.SkipException($"Skipping Playwright tests: {message}");
        }
    }

    private static ScreenshotGeneratorApp CreateApp(out StringWriter output, out StringWriter error)
    {
        output = new StringWriter(CultureInfo.InvariantCulture);
        error = new StringWriter(CultureInfo.InvariantCulture);
        return new ScreenshotGeneratorApp(output, error);
    }

    private static async Task<TempContext> CreateTempHtmlAsync(string html)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "app-tests", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        var input = Path.Combine(root, "input.html");
        await File.WriteAllTextAsync(input, html).ConfigureAwait(false);
        var expectedOutput = Path.Combine(root, "input.png");
        return new TempContext(root, input, expectedOutput);
    }

    private sealed class TempContext : IDisposable
    {
        public TempContext(string root, string inputPath, string expectedOutputPath)
        {
            Root = root;
            InputPath = inputPath;
            ExpectedOutputPath = expectedOutputPath;
        }

        public string Root { get; }

        public string InputPath { get; }

        public string ExpectedOutputPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
        }
    }
}
