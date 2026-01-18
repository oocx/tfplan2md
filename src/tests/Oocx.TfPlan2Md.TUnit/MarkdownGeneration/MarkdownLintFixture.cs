using System.Diagnostics;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Provides Docker-based markdownlint execution for integration tests.
/// Uses the official markdownlint-cli2 Docker image to ensure consistent validation
/// across all environments without requiring local tool installation.
/// </summary>
/// <remarks>
/// This fixture runs markdownlint-cli2 via Docker to validate markdown output.
/// Using Docker eliminates the need for Node.js or npm on the development machine
/// or CI runner, ensuring consistent linting behavior across all environments.
/// </remarks>
public class MarkdownLintFixture
{
    /// <summary>
    /// The Docker image used for markdownlint validation.
    /// </summary>
    private const string MarkdownLintImage = "davidanson/markdownlint-cli2:v0.20.0";

    /// <summary>
    /// Gets whether Docker is available on the current system.
    /// </summary>
    public bool IsDockerAvailable { get; private set; }

    /// <summary>
    /// Gets whether the markdownlint image was successfully pulled.
    /// </summary>
    public bool ImageReady { get; private set; }

    /// <summary>
    /// Initializes the fixture by checking Docker availability and pulling the image.
    /// </summary>
    public async Task InitializeAsync()
    {
        IsDockerAvailable = await CheckDockerAvailableAsync();
        if (!IsDockerAvailable)
        {
            return;
        }

        ImageReady = await PullImageAsync();
    }

    /// <summary>
    /// Cleans up resources (no-op for this fixture).
    /// </summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Checks if Docker is available on the current system.
    /// </summary>
    /// <returns>True if Docker is available and responding.</returns>
    private static async Task<bool> CheckDockerAvailableAsync()
    {
        try
        {
            var psi = new ProcessStartInfo("docker", "version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Pulls the markdownlint Docker image.
    /// </summary>
    /// <returns>True if the image was successfully pulled or already exists.</returns>
    private async Task<bool> PullImageAsync()
    {
        var psi = new ProcessStartInfo("docker", $"pull {MarkdownLintImage}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            return false;
        }

        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    /// <summary>
    /// Runs markdownlint on the provided markdown content via Docker.
    /// </summary>
    /// <param name="markdown">The markdown content to validate.</param>
    /// <returns>A tuple containing the exit code, stdout, and stderr.</returns>
    /// <remarks>
    /// The markdown is passed via stdin to the container using the --stdin flag.
    /// Exit code 0 indicates no linting errors, non-zero indicates violations.
    /// </remarks>
    public async Task<MarkdownLintResult> LintAsync(string markdown)
    {
        var arguments = $"run --rm -i {MarkdownLintImage} --stdin";

        var psi = new ProcessStartInfo("docker", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            return new MarkdownLintResult(-1, "", "Failed to start docker process", []);
        }

        await process.StandardInput.WriteAsync(markdown);
        process.StandardInput.Close();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var violations = ParseViolations(stdout);

        return new MarkdownLintResult(process.ExitCode, stdout, stderr, violations);
    }

    /// <summary>
    /// Parses markdownlint output to extract individual violations.
    /// </summary>
    /// <param name="output">The raw output from markdownlint.</param>
    /// <returns>A list of parsed violations.</returns>
    private static List<MarkdownLintViolation> ParseViolations(string output)
    {
        var violations = new List<MarkdownLintViolation>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Format: stdin:LINE RULE/name MESSAGE
            // Example: stdin:359 MD012/no-multiple-blanks Multiple consecutive blank lines [Expected: 1; Actual: 2]
            var match = System.Text.RegularExpressions.Regex.Match(
                line,
                @"stdin:(\d+)\s+(\w+)/([^\s]+)\s+(.+)");

            if (match.Success)
            {
                violations.Add(new MarkdownLintViolation(
                    int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                    match.Groups[2].Value,
                    match.Groups[3].Value,
                    match.Groups[4].Value));
            }
        }

        return violations;
    }
}

/// <summary>
/// Represents the result of a markdownlint validation run.
/// </summary>
/// <param name="ExitCode">The exit code from markdownlint (0 = success).</param>
/// <param name="StdOut">The standard output from the linting process.</param>
/// <param name="StdErr">The standard error from the linting process.</param>
/// <param name="Violations">Parsed list of markdown violations found.</param>
public record MarkdownLintResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    IReadOnlyList<MarkdownLintViolation> Violations)
{
    /// <summary>
    /// Gets whether the markdown passed all linting rules.
    /// </summary>
    public bool IsValid => ExitCode == 0 && Violations.Count == 0;
}

/// <summary>
/// Represents a single markdownlint violation.
/// </summary>
/// <param name="Line">The line number where the violation occurred.</param>
/// <param name="RuleId">The rule ID (e.g., "MD012").</param>
/// <param name="RuleName">The rule name (e.g., "no-multiple-blanks").</param>
/// <param name="Message">The detailed violation message.</param>
public record MarkdownLintViolation(
    int Line,
    string RuleId,
    string RuleName,
    string Message);

/// <summary>
