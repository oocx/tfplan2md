namespace Oocx.TfPlan2Md.HtmlRenderer;

/// <summary>
/// Entry point for the HTML renderer console application.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
public static class Program
{
    /// <summary>
    /// Starts the HTML renderer CLI in its initial placeholder form.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md
    /// </summary>
    /// <param name="args">Command-line arguments supplied by the host process.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation and containing the process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var app = new HtmlRendererApp(Console.Out, Console.Error);
        return await app.RunAsync(args).ConfigureAwait(false);
    }
}
