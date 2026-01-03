using System.Text.Json;
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
    private static readonly JsonSerializerOptions SummarySerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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

            var clip = await ResolveClipAsync(page, settings, cancellationToken).ConfigureAwait(false);

            var screenshotOptions = BuildScreenshotOptions(settings, clip);
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
    /// <param name="clip">Optional clipping rectangle for partial capture.</param>
    /// <returns>Configured screenshot options.</returns>
    private static PageScreenshotOptions BuildScreenshotOptions(CaptureSettings settings, ClipBox? clip)
    {
        var options = new PageScreenshotOptions
        {
            Path = settings.OutputPath,
            FullPage = clip is null && settings.FullPage,
        };

        if (settings.Format == ScreenshotFormat.Jpeg)
        {
            options.Type = ScreenshotType.Jpeg;
            if (settings.Quality.HasValue)
            {
                options.Quality = settings.Quality;
            }
        }

        if (clip is not null)
        {
            options.Clip = new Clip
            {
                X = (float)clip.Value.X,
                Y = (float)clip.Value.Y,
                Width = (float)clip.Value.Width,
                Height = (float)clip.Value.Height,
            };
        }

        return options;
    }

    /// <summary>
    /// Resolves a clipping rectangle when a partial capture target is provided.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    /// <param name="page">Active Playwright page.</param>
    /// <param name="settings">Capture settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A clip rectangle or <see langword="null"/> for full-page capture.</returns>
    private static async Task<ClipBox?> ResolveClipAsync(IPage page, CaptureSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.TargetSelector) && string.IsNullOrWhiteSpace(settings.TargetTerraformResourceId))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(settings.TargetSelector))
        {
            return await ResolveSelectorClipAsync(page, settings.TargetSelector!, cancellationToken).ConfigureAwait(false);
        }

        return await ResolveTerraformClipAsync(page, settings.TargetTerraformResourceId!, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a clip rectangle for selector-based targeting using Playwright selectors.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="selector">Selector identifying elements to capture.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Union bounding box of matching elements.</returns>
    private static async Task<ClipBox> ResolveSelectorClipAsync(IPage page, string selector, CancellationToken cancellationToken)
    {
        var locator = page.Locator(selector);
        var handles = await locator.ElementHandlesAsync().ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var boxes = await ReadBoundingBoxesAsync(handles).ConfigureAwait(false);
        if (boxes.Count == 0)
        {
            throw new ScreenshotCaptureException(FormattableString.Invariant($"Target selector matched no visible elements: {selector}"));
        }

        return Union(boxes);
    }

    /// <summary>
    /// Builds a clip rectangle for a Terraform resource address by matching existing visible text.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="address">Terraform resource address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bounding box of the matched details element.</returns>
    private static async Task<ClipBox> ResolveTerraformClipAsync(IPage page, string address, CancellationToken cancellationToken)
    {
        var parsed = ParsedResourceAddress.Parse(address);
        var summaries = await CollectSummariesAsync(page).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var moduleLabel = string.IsNullOrWhiteSpace(parsed.ModuleAddress) ? "root" : parsed.ModuleAddress;

        var match = summaries.FirstOrDefault(summary =>
            ContainsInvariant(summary.ModuleText, moduleLabel) &&
            ContainsInvariant(summary.Text, parsed.ResourceType) &&
            ContainsInvariant(summary.Text, parsed.ResourceName));

        if (match is null)
        {
            throw new ScreenshotCaptureException(FormattableString.Invariant($"Could not locate resource '{address}' using visible text. Verify the module and resource name."));
        }

        var summaryLocator = page.Locator("summary").Nth(match.Index);
        var detailsHandle = await summaryLocator.Locator("xpath=ancestor::details").ElementHandleAsync().ConfigureAwait(false);
        if (detailsHandle is null)
        {
            throw new ScreenshotCaptureException(FormattableString.Invariant($"Found summary for '{address}' but could not locate its details container."));
        }

        var box = await detailsHandle.BoundingBoxAsync().ConfigureAwait(false);
        if (box is null)
        {
            throw new ScreenshotCaptureException(FormattableString.Invariant($"Target resource '{address}' is not visible for capture."));
        }

        return new ClipBox(box.X, box.Y, box.Width, box.Height);
    }

    /// <summary>
    /// Collects summaries with associated module headings for text-based matching.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <returns>Summaries with module context and index.</returns>
    private static async Task<IReadOnlyList<SummaryInfo>> CollectSummariesAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(@"() => {
            const nodes = Array.from(document.querySelectorAll('summary'));
            const summaries = nodes.map((summary, index) => {
                let moduleText = '';
                let cursor = summary;
                while (cursor && !moduleText) {
                    let sibling = cursor.previousElementSibling;
                    while (sibling && !moduleText) {
                        if (sibling.tagName && sibling.tagName.toLowerCase() === 'h3' && sibling.textContent && sibling.textContent.includes('Module:')) {
                            moduleText = sibling.textContent.trim();
                            break;
                        }
                        sibling = sibling.previousElementSibling;
                    }

                    if (moduleText) {
                        break;
                    }

                    cursor = cursor.parentElement;
                }

                if (!moduleText) {
                    let ancestor = summary.parentElement;
                    while (ancestor && !moduleText) {
                        const heading = ancestor.querySelector('h3');
                        if (heading && heading.textContent && heading.textContent.includes('Module:')) {
                            moduleText = heading.textContent.trim();
                            break;
                        }
                        ancestor = ancestor.parentElement;
                    }
                }

                return {
                    index,
                    text: (summary.textContent ?? '').trim(),
                    moduleText: moduleText || '',
                };
            });

            return JSON.stringify(summaries);
        }").ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<SummaryInfo>();
        }

        var summaries = JsonSerializer.Deserialize<List<SummaryInfo>>(json, SummarySerializerOptions);
        return summaries is not null ? summaries : Array.Empty<SummaryInfo>();
    }

    /// <summary>
    /// Reads bounding boxes from element handles, ignoring nulls.
    /// </summary>
    /// <param name="handles">Element handles to inspect.</param>
    /// <returns>Bounding boxes for visible elements.</returns>
    private static async Task<IReadOnlyList<ClipBox>> ReadBoundingBoxesAsync(IReadOnlyList<IElementHandle> handles)
    {
        var boxes = new List<ClipBox>();

        foreach (var handle in handles)
        {
            var box = await handle.BoundingBoxAsync().ConfigureAwait(false);
            if (box is not null)
            {
                boxes.Add(new ClipBox(box.X, box.Y, box.Width, box.Height));
            }
        }

        return boxes;
    }

    /// <summary>
    /// Computes the union rectangle for a set of bounding boxes.
    /// </summary>
    /// <param name="boxes">Bounding boxes to combine.</param>
    /// <returns>Union bounding rectangle.</returns>
    private static ClipBox Union(IReadOnlyCollection<ClipBox> boxes)
    {
        var minX = boxes.Min(b => b.X);
        var minY = boxes.Min(b => b.Y);
        var maxX = boxes.Max(b => b.X + b.Width);
        var maxY = boxes.Max(b => b.Y + b.Height);

        return new ClipBox(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// Performs case-insensitive containment checks.
    /// </summary>
    /// <param name="source">Source string.</param>
    /// <param name="value">Value to search for.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> exists in <paramref name="source"/>.</returns>
    private static bool ContainsInvariant(string? source, string value)
    {
        return !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Represents a parsed Terraform resource address.
    /// </summary>
    private sealed record ParsedResourceAddress(string? ModuleAddress, string ResourceType, string ResourceName)
    {
        public static ParsedResourceAddress Parse(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ScreenshotCaptureException("Terraform resource address is required when targeting by address.");
            }

            var parts = address.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                throw new ScreenshotCaptureException(FormattableString.Invariant($"Invalid Terraform resource address: {address}"));
            }

            var resourceName = parts[^1];
            var resourceType = parts[^2];
            var moduleParts = parts.Length > 2 ? parts[..^2] : Array.Empty<string>();
            var moduleAddress = moduleParts.Length == 0 ? null : string.Join('.', moduleParts);

            return new ParsedResourceAddress(moduleAddress, resourceType, resourceName);
        }
    }

    /// <summary>
    /// Represents summary text and owning module text for resource matching.
    /// </summary>
    private sealed record SummaryInfo
    {
        public int Index { get; init; }

        public string? ModuleText { get; init; }

        public string? Text { get; init; }
    }

    /// <summary>
    /// Represents a screenshot clip rectangle.
    /// </summary>
    private readonly record struct ClipBox(double X, double Y, double Width, double Height);
}
