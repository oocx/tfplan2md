using System.Threading.Tasks;

namespace Oocx.TfPlan2Md.ScreenshotGenerator;

/// <summary>
/// Entry point for the screenshot generator tool.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
public static class Program
{
    /// <summary>
    /// Executes the screenshot generator application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code indicating success or failure.</returns>
    public static Task<int> Main(string[] args)
    {
        var app = new ScreenshotGeneratorApp(Console.Out, Console.Error);
        return app.RunAsync(args);
    }
}
