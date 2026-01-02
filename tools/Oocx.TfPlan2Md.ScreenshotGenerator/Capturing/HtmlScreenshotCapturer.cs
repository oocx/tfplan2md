using Microsoft.Playwright;
using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator.Capturing;

/// <summary>
/// Generates screenshots from HTML files using Playwright/Chromium.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal sealed class HtmlScreenshotCapturer
{
    private const string InstallHint = "playwright install chromium --with-deps";

    /// <summary>
    /// Captures a screenshot for the provided settings.
    /// </summary>
    /// <param name="settings">Capture settings describing input, output, and format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ScreenshotCaptureException">Thrown when Playwright fails to launch or navigate.</exception>
    public async Task CaptureAsync(CaptureSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            }).ConfigureAwait(false);

            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = settings.Width, Height = settings.Height },
            }).ConfigureAwait(false);

            var page = await context.NewPageAsync().ConfigureAwait(false);

            EnsureOutputDirectory(settings.OutputPath);

            cancellationToken.ThrowIfCancellationRequested();

            var fileUri = new Uri(Path.GetFullPath(settings.InputPath)).AbsoluteUri;
            await page.GotoAsync(fileUri, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000,
            }).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var screenshotOptions = BuildScreenshotOptions(settings);
            await page.ScreenshotAsync(screenshotOptions).ConfigureAwait(false);
        }
        catch (PlaywrightException ex)
        {
            throw new ScreenshotCaptureException($"Chromium is not available. Install it with '{InstallHint}'. Details: {ex.Message}", ex);
        }
        catch (TimeoutException ex)
        {
            throw new ScreenshotCaptureException("Timed out while loading the HTML page before capture.", ex);
        }
    }

    /// <summary>
    /// Ensures the output directory exists before writing the image file.
    /// </summary>
    /// <param name="outputPath">Target output file path.</param>
    private static void EnsureOutputDirectory(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Builds Playwright screenshot options based on capture settings.
    /// </summary>
    /// <param name="settings">User-supplied capture settings.</param>
    /// <returns>Configured screenshot options.</returns>
    private static PageScreenshotOptions BuildScreenshotOptions(CaptureSettings settings)
    {
        var options = new PageScreenshotOptions
        {
            Path = settings.OutputPath,
            FullPage = settings.FullPage,
        };

        if (settings.Format == ScreenshotFormat.Jpeg)
        {
            options.Type = ScreenshotType.Jpeg;
            if (settings.Quality.HasValue)
            {
                options.Quality = settings.Quality;
            }
        }

        return options;
    }
}
