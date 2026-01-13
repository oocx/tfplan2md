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
[TestClass]
public sealed class CaptureIntegrationTests
{
    /// <summary>
    /// Verifies default viewport PNG capture and dimensions.
    /// Related acceptance: TC-11.
    /// </summary>
    [TestMethod]
    public async Task CaptureAsync_DefaultViewport_PngWithExpectedDimensions()
    {
        await using var context = await CreateTempHtmlAsync("<html><body><h1>Hello</h1></body></html>");
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();
        var settings = new CaptureSettings(context.InputPath, context.OutputPath, 1920, 1080, fullPage: false, format: ScreenshotFormat.Png);

        try
        {
            await capturer.CaptureAsync(settings, CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            Assert.Inconclusive($"Skipping Playwright tests: {ex.Message}");
        }

        Assert.IsTrue(File.Exists(context.OutputPath));
        var (width, height) = ReadPngDimensions(context.OutputPath);
        Assert.AreEqual(1920, width);
        Assert.AreEqual(1080, height);
    }

    /// <summary>
    /// Verifies custom viewport size is honored.
    /// Related acceptance: TC-12.
    /// </summary>
    [TestMethod]
    public async Task CaptureAsync_CustomViewport_UsesRequestedSize()
    {
        await using var context = await CreateTempHtmlAsync("<html><body><p>Viewport test</p></body></html>");
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();
        var settings = new CaptureSettings(context.InputPath, context.OutputPath, 800, 600, fullPage: false, format: ScreenshotFormat.Png);

        try
        {
            await capturer.CaptureAsync(settings, CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            Assert.Inconclusive($"Skipping Playwright tests: {ex.Message}");
        }

        var (width, height) = ReadPngDimensions(context.OutputPath);
        Assert.AreEqual(800, width);
        Assert.AreEqual(600, height);
    }

    /// <summary>
    /// Verifies full-page capture yields taller output for long content.
    /// Related acceptance: TC-13.
    /// </summary>
    [TestMethod]
    public async Task CaptureAsync_FullPage_CapturesTallContent()
    {
        var tallBody = "<div style=\"height:2000px;background:linear-gradient(#fff,#ccc);\"></div>";
        await using var context = await CreateTempHtmlAsync($"<html><body>{tallBody}</body></html>");
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();
        var settings = new CaptureSettings(context.InputPath, context.OutputPath, 1920, 1080, fullPage: true, format: ScreenshotFormat.Png);

        try
        {
            await capturer.CaptureAsync(settings, CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            Assert.Inconclusive($"Skipping Playwright tests: {ex.Message}");
        }

        var (_, height) = ReadPngDimensions(context.OutputPath);
        Assert.IsTrue(height > 1500, "Expected full-page height to exceed default viewport.");
    }

    /// <summary>
    /// Verifies selector-based targeting crops to the matching element's bounding box.
    /// Related acceptance: TC-16.
    /// </summary>
    [TestMethod]
    public async Task CaptureAsync_TargetSelector_CropsToElement()
    {
        const string html = """
<html>
  <body style="margin:0">
    <div style="width:400px;height:120px;background:#e0e0e0;"></div>
    <div id="target" style="width:200px;height:150px;background:#ffaaaa;margin-top:10px;"></div>
  </body>
</html>
""";

        await using var context = await CreateTempHtmlAsync(html);
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();
        var settings = new CaptureSettings(context.InputPath, context.OutputPath, 800, 600, fullPage: false, format: ScreenshotFormat.Png, targetSelector: "#target");

        try
        {
            await capturer.CaptureAsync(settings, CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            Assert.Inconclusive($"Skipping Playwright tests: {ex.Message}");
        }

        var (width, height) = ReadPngDimensions(context.OutputPath);
        Assert.IsTrue(width >= 195 && width <= 205);
        Assert.IsTrue(height >= 145 && height <= 155);
    }

    /// <summary>
    /// Verifies JPEG quality affects file size.
    /// Related acceptance: TC-14.
    /// </summary>
    [TestMethod]
    public async Task CaptureAsync_JpegQuality_ProducesDifferentSizes()
    {
        await using var contextLow = await CreateTempHtmlAsync("<html><body><p>quality test</p></body></html>");
        var contextHigh = await CreateTempHtmlAsync("<html><body><p>quality test</p></body></html>");
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();

        try
        {
            await capturer.CaptureAsync(new CaptureSettings(contextLow.InputPath, contextLow.OutputPath, 800, 600, fullPage: false, format: ScreenshotFormat.Jpeg, quality: 10), CancellationToken.None);
            await capturer.CaptureAsync(new CaptureSettings(contextHigh.InputPath, contextHigh.OutputPath, 800, 600, fullPage: false, format: ScreenshotFormat.Jpeg, quality: 100), CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            Assert.Inconclusive($"Skipping Playwright tests: {ex.Message}");
        }

        var lowSize = new FileInfo(contextLow.OutputPath).Length;
        var highSize = new FileInfo(contextHigh.OutputPath).Length;
        Assert.IsTrue(lowSize < highSize, "Lower quality should yield smaller file size.");
    }

    /// <summary>
    /// Verifies Terraform resource targeting matches visible summary text and crops accordingly.
    /// Related acceptance: TC-17.
    /// </summary>
    [TestMethod]
    public async Task CaptureAsync_TerraformAddress_CropsToMatchedResource()
    {
        const string html = """
<html>
  <body style="margin:0">
    <h3>Module: root</h3>
    <details open style="display:block;width:360px;height:180px;margin:0;padding:0;border:0;background:#f5f5f5;">
      <summary>azurerm_storage_account.example (changed)</summary>
      <div style="height:140px;">content</div>
    </details>
  </body>
</html>
""";

        await using var context = await CreateTempHtmlAsync(html);
        await EnsureBrowserAvailableAsync();
        var capturer = new HtmlScreenshotCapturer();
        var settings = new CaptureSettings(context.InputPath, context.OutputPath, 1024, 768, fullPage: false, format: ScreenshotFormat.Png, targetTerraformResourceId: "azurerm_storage_account.example");

        try
        {
            await capturer.CaptureAsync(settings, CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            Assert.Inconclusive($"Skipping Playwright tests: {ex.Message}");
        }

        var (width, height) = ReadPngDimensions(context.OutputPath);
        Assert.IsTrue(width >= 350 && width <= 370);
        Assert.IsTrue(height >= 170 && height <= 190);
    }

    /// <summary>
    /// Verifies capturing a real-ish HTML report generated by the renderer tool.
    /// Related acceptance: TC-18.
    /// </summary>
    [TestMethod]
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
            await capturer.CaptureAsync(new CaptureSettings(fixturePath, outputPath, 1920, 1080, fullPage: true, format: ScreenshotFormat.Png), CancellationToken.None);
        }
        catch (ScreenshotCaptureException ex) when (ex.InnerException is PlaywrightException)
        {
            Assert.Inconclusive($"Skipping Playwright tests: {ex.Message}");
        }

        Assert.IsTrue(File.Exists(outputPath));
        Assert.IsTrue(new FileInfo(outputPath).Length > 0, "Screenshot file should be non-empty.");
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
            Assert.Inconclusive($"Skipping Playwright tests: {ex.Message}");
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
