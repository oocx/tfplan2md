namespace Oocx.TfPlan2Md.TerraformShowRenderer;

/// <summary>
/// Entry point for the Terraform show approximation console application.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
public static class Program
{
    /// <summary>
    /// Starts the Terraform show renderer CLI.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md.
    /// </summary>
    /// <param name="args">Command-line arguments supplied by the host process.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation and containing the process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var app = new TerraformShowRendererApp(Console.Out, Console.Error);
        return await app.RunAsync(args).ConfigureAwait(false);
    }
}
