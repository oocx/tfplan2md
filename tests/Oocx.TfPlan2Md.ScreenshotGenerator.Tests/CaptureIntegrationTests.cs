using System.Globalization;
using System.Text;
using Microsoft.Playwright;
using Oocx.TfPlan2Md.ScreenshotGenerator.Capturing;
using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator.Tests;

/// <summary>
/// Integration tests for Playwright-backed screenshot capture.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
public sealed class CaptureIntegrationTests
{
    /// <summary>
    /// Verifies default viewport PNG capture and dimensions.
    /// Related acceptance: TC-11.
    /// </summary>
    [SkippableFact]
    public async Task CaptureAsync_DefaultViewport_PngWithExpectedDimensions()
    {
        await using var context = await CreateTempHtmlAsync("<html><body><h1>Hello</h1></body></html>");
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();
        var settings = new CaptureSettings(context.InputPath, context.OutputPath, 1920, 1080, false, ScreenshotFormat.Png, null);

        try
        {
            await capturer.CaptureAsync(settings, CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            throw new Xunit.SkipException($"Skipping Playwright tests: {ex.Message}");
        }

        Assert.True(File.Exists(context.OutputPath));
        var (width, height) = ReadPngDimensions(context.OutputPath);
        Assert.Equal(1920, width);
        Assert.Equal(1080, height);
    }

    /// <summary>
    /// Verifies custom viewport size is honored.
    /// Related acceptance: TC-12.
    /// </summary>
    [SkippableFact]
    public async Task CaptureAsync_CustomViewport_UsesRequestedSize()
    {
        await using var context = await CreateTempHtmlAsync("<html><body><p>Viewport test</p></body></html>");
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();
        var settings = new CaptureSettings(context.InputPath, context.OutputPath, 800, 600, false, ScreenshotFormat.Png, null);

        try
        {
            await capturer.CaptureAsync(settings, CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            throw new Xunit.SkipException($"Skipping Playwright tests: {ex.Message}");
        }

        var (width, height) = ReadPngDimensions(context.OutputPath);
        Assert.Equal(800, width);
        Assert.Equal(600, height);
    }

    /// <summary>
    /// Verifies full-page capture yields taller output for long content.
    /// Related acceptance: TC-13.
    /// </summary>
    [SkippableFact]
    public async Task CaptureAsync_FullPage_CapturesTallContent()
    {
        var tallBody = "<div style=\"height:2000px;background:linear-gradient(#fff,#ccc);\"></div>";
        await using var context = await CreateTempHtmlAsync($"<html><body>{tallBody}</body></html>");
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();
        var settings = new CaptureSettings(context.InputPath, context.OutputPath, 1920, 1080, true, ScreenshotFormat.Png, null);

        try
        {
            await capturer.CaptureAsync(settings, CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            throw new Xunit.SkipException($"Skipping Playwright tests: {ex.Message}");
        }

        var (_, height) = ReadPngDimensions(context.OutputPath);
        Assert.True(height > 1500, "Expected full-page height to exceed default viewport.");
    }

    /// <summary>
    /// Verifies JPEG quality affects file size.
    /// Related acceptance: TC-14.
    /// </summary>
    [SkippableFact]
    public async Task CaptureAsync_JpegQuality_ProducesDifferentSizes()
    {
        await using var contextLow = await CreateTempHtmlAsync("<html><body><p>quality test</p></body></html>");
        var contextHigh = await CreateTempHtmlAsync("<html><body><p>quality test</p></body></html>");
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();

        try
        {
            await capturer.CaptureAsync(new CaptureSettings(contextLow.InputPath, contextLow.OutputPath, 800, 600, false, ScreenshotFormat.Jpeg, 10), CancellationToken.None);
            await capturer.CaptureAsync(new CaptureSettings(contextHigh.InputPath, contextHigh.OutputPath, 800, 600, false, ScreenshotFormat.Jpeg, 100), CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            throw new Xunit.SkipException($"Skipping Playwright tests: {ex.Message}");
        }

        var lowSize = new FileInfo(contextLow.OutputPath).Length;
        var highSize = new FileInfo(contextHigh.OutputPath).Length;
        Assert.True(lowSize < highSize, "Lower quality should yield smaller file size.");
    }

    /// <summary>
    /// Verifies capturing a real-ish HTML report generated by the renderer tool.
    /// Related acceptance: TC-18.
    /// </summary>
    [SkippableFact]
    public async Task CaptureAsync_ComprehensiveDemoHtml_Succeeds()
    {
        await EnsureBrowserAvailableAsync();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "TestData", "comprehensive-demo.github.html");
        var outputRoot = Path.Combine(AppContext.BaseDirectory, "capture-tests", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(outputRoot);
        var outputPath = Path.Combine(outputRoot, "comprehensive-demo.github.png");
        var capturer = new HtmlScreenshotCapturer();

        try
        {
            await capturer.CaptureAsync(new CaptureSettings(fixturePath, outputPath, 1920, 1080, true, ScreenshotFormat.Png, null), CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            throw new Xunit.SkipException($"Skipping Playwright tests: {ex.Message}");
        }

        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0, "Screenshot file should be non-empty.");
    }

    /// <summary>
    /// Ensures Playwright/Chromium is available; otherwise skips tests with guidance.
    /// </summary>
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

    /// <summary>
    /// Reads PNG dimensions from the IHDR chunk.
    /// </summary>
    /// <param name="path">PNG file path.</param>
    /// <returns>Tuple of width and height.</returns>
    private static (int Width, int Height) ReadPngDimensions(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        var signature = reader.ReadBytes(8);
        // IHDR chunk length (4 bytes) + 'IHDR' (4 bytes)
        reader.ReadBytes(8);
        var widthBytes = reader.ReadBytes(4);
        var heightBytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(widthBytes);
            Array.Reverse(heightBytes);
        }

        var width = BitConverter.ToInt32(widthBytes, 0);
        var height = BitConverter.ToInt32(heightBytes, 0);
        return (width, height);
    }

    /// <summary>
    /// Creates a temporary HTML file and output path for capture.
    /// </summary>
    /// <param name="html">HTML content to persist.</param>
    /// <returns>Disposable context containing input and output paths.</returns>
    private static async Task<TempCaptureContext> CreateTempHtmlAsync(string html)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "capture-tests", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        var inputPath = Path.Combine(root, "input.html");
        var outputPath = Path.Combine(root, "output.png");
        await File.WriteAllTextAsync(inputPath, html, Encoding.UTF8).ConfigureAwait(false);
        return new TempCaptureContext(root, inputPath, outputPath);
    }

    /// <summary>
    /// Represents disposable temp file context for capture tests.
    /// </summary>
    private sealed class TempCaptureContext : IAsyncDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempCaptureContext"/> class.
        /// </summary>
        /// <param name="root">Root temp directory.</param>
        /// <param name="inputPath">Path to HTML input.</param>
        /// <param name="outputPath">Path to image output.</param>
        public TempCaptureContext(string root, string inputPath, string outputPath)
        {
            Root = root;
            InputPath = inputPath;
            OutputPath = outputPath;
        }

        /// <summary>
        /// Gets the root temp directory.
        /// </summary>
        public string Root { get; }

        /// <summary>
        /// Gets the input HTML path.
        /// </summary>
        public string InputPath { get; }

        /// <summary>
        /// Gets the output image path.
        /// </summary>
        public string OutputPath { get; }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
